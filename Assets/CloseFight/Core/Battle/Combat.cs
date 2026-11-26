using System.Collections.Generic;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.Battle
{
    /// <summary>What caused a casualty: snipers and flamethrowers frighten men more (D39, M12).</summary>
    public enum HitCause : byte
    {
        Gunfire,
        Sniper,
        Flame,
        Blast,
    }

    /// <summary>One round fired, kept briefly so the view can draw tracers.</summary>
    public readonly struct ShotEvent
    {
        public readonly int ShooterId;
        public readonly float2 From;
        public readonly float2 Impact;
        public readonly bool Hit;
        public readonly WeaponClass Weapon;
        public readonly float Time;

        public ShotEvent(int shooterId, float2 from, float2 impact, bool hit, WeaponClass weapon, float time)
        {
            ShooterId = shooterId;
            From = from;
            Impact = impact;
            Hit = hit;
            Weapon = weapon;
            Time = time;
        }
    }

    /// <summary>
    /// Small-arms fire (M4). Every soldier picks a target he can see and shoot
    /// at, fires bursts on his weapon's rhythm, and each round either hits
    /// (wounding, disabling or killing) or lands nearby. Hits and near misses
    /// both pile suppression on the soldiers around the impact; a soldier past
    /// the pinned threshold hugs the ground and stops moving and shooting.
    /// The full psychology model (panic, rout, disobedience) builds on this in M5.
    /// </summary>
    public sealed class Combat
    {
        public const float SuppressionRadius = 3f;
        public const float SuppressionDecayPerSecond = 8f;
        /// <summary>Suppression only starts to fade after this long without incoming fire.</summary>
        public const float DecayDelay = 2f;
        /// <summary>A team on Ambush holds fire until an enemy is this close.</summary>
        public const float AmbushRange = 50f;
        /// <summary>A hiding team only fires in self-defense, at enemies this close.</summary>
        public const float HideRange = 15f;
        const float ShotHistorySeconds = 1f;
        const float SoundFuzz = 15f;
        /// <summary>A sniper's single shot is hard to place by ear.</summary>
        const float SniperSoundFuzz = 45f;
        /// <summary>Flame engulfs everyone this close to where the jet lands.</summary>
        public const float FlameRadius = 3.5f;

        readonly BattleState _battle;
        readonly List<ShotEvent> _recentShots = new List<ShotEvent>();

        public IReadOnlyList<ShotEvent> RecentShots => _recentShots;

        public Combat(BattleState battle)
        {
            _battle = battle;
        }

        public void Step(float dt)
        {
            float now = _battle.Clock.ElapsedSeconds;
            DecaySuppression(dt, now);

            foreach (var shooter in _battle.Soldiers)
            {
                if (!shooter.IsActive || shooter.Weapon == null)
                {
                    continue;
                }

                if (shooter.FireCooldown > 0f)
                {
                    shooter.FireCooldown -= dt;
                    continue;
                }

                TryFire(shooter, now);
            }

            _recentShots.RemoveAll(s => now - s.Time > ShotHistorySeconds);
        }

        void TryFire(Soldier shooter, float now)
        {
            if (shooter.Ammo <= 0 || shooter.IsPinned || shooter.IsOutOfControl)
            {
                return;
            }

            var team = shooter.Team;
            var order = team.Order.Type;
            bool moving = team.Activity != TeamActivity.Idle;
            // A tank's coaxial MG fires on the move unless the driver has his foot down.
            bool canFireMoving = shooter.IsMounted
                ? order != OrderType.MoveFast && order != OrderType.Reverse && order != OrderType.Retreat
                : CanFireWhileMoving(order, shooter.Weapon);
            if (moving && !canFireMoving)
            {
                return;
            }

            if (order == OrderType.AreaFire && !moving)
            {
                FireAtArea(shooter, team.Order.Target, now);
                return;
            }

            float holdRange = order == OrderType.Ambush ? AmbushRange
                : order == OrderType.Hide ? HideRange
                : float.PositiveInfinity;
            holdRange = math.min(holdRange, team.SmallArmsHoldRange);

            // A sniper lying in wait still takes the shots that matter: officers and machine gunners.
            bool sniper = shooter.Weapon.Class == WeaponClass.SniperRifle;
            bool priorityOnly = false;
            if (sniper && (order == OrderType.Ambush || order == OrderType.Hide))
            {
                holdRange = shooter.Weapon.EffectiveRange;
                priorityOnly = true;
            }
            else if (!moving && !float.IsPositiveInfinity(holdRange) && team.Type != TeamType.Observer)
            {
                // D54: holding still and holding fire, the team shoots first once the enemy is about to find it.
                holdRange = math.max(holdRange, _battle.Visibility.PreemptRange(team, shooter, now));
            }

            var target = ChooseTarget(shooter, holdRange, priorityOnly);
            if (target == null)
            {
                return;
            }

            if (shooter.Weapon.Class == WeaponClass.Flamethrower)
            {
                FlameAt(shooter, target, now);
            }
            else
            {
                FireAt(shooter, target, now, moving);
            }
        }

        /// <summary>Walking riflemen may fire on the move (badly); running, crawling teams and machine guns may not.</summary>
        static bool CanFireWhileMoving(OrderType order, WeaponDefinition weapon)
        {
            return order == OrderType.Move && !weapon.NeedsSetup;
        }

        /// <summary>Records a shot for the view's tracers (also used by heavy guns).</summary>
        public void RecordShot(int shooterId, float2 from, float2 impact, bool hit, WeaponClass weapon, float now)
        {
            _recentShots.Add(new ShotEvent(shooterId, from, impact, hit, weapon, now));
        }

        Soldier ChooseTarget(Soldier shooter, float holdRange, bool priorityOnly = false)
        {
            var team = shooter.Team;
            Soldier best = null;
            float bestScore = float.MaxValue;

            // A Fire order names a team; engage it out to the weapon's maximum range.
            if (team.FireTarget != null && team.FireTarget.ActiveCount > 0)
            {
                foreach (var enemy in team.FireTarget.Members)
                {
                    Consider(shooter, enemy, shooter.Weapon.MaxRange, holdRange, false, ref best, ref bestScore);
                }

                if (best != null)
                {
                    return best;
                }
            }

            foreach (var enemy in _battle.Soldiers)
            {
                if (enemy.Team.Side != team.Side)
                {
                    Consider(shooter, enemy, shooter.Weapon.EffectiveRange, holdRange, priorityOnly, ref best, ref bestScore);
                }
            }

            return best;
        }

        void Consider(Soldier shooter, Soldier enemy, float range, float holdRange, bool priorityOnly, ref Soldier best, ref float bestScore)
        {
            // Rifles and machine guns cannot hurt the crew inside a tank.
            if (!enemy.IsActive || enemy.IsMounted || !_battle.Visibility.CanSee(shooter.Team.Side, enemy))
            {
                return;
            }

            float distance = math.distance(shooter.Position, enemy.Position);
            if (distance > range || distance > holdRange)
            {
                return;
            }

            // Machine guns are the most dangerous thing on the field: shoot them first.
            float score = enemy.Weapon != null && enemy.Weapon.Class == WeaponClass.MachineGun ? distance * 0.7f : distance;
            if (shooter.Weapon.Class == WeaponClass.SniperRifle)
            {
                // D39: snipers pick officers, machine gunners and gun crews.
                float priority = SniperPriority(enemy);
                if (priorityOnly && priority >= 1f && distance > AmbushRange)
                {
                    return;
                }

                score = distance * priority;
            }
            if (score >= bestScore || !HasLineOfFire(shooter.Position, shooter.Posture, enemy.Position, enemy.Posture, range))
            {
                return;
            }

            best = enemy;
            bestScore = score;
        }

        /// <summary>Multiplier on distance when a sniper picks a target: lower is more wanted.</summary>
        public static float SniperPriority(Soldier enemy)
        {
            if (enemy.IsLeader) return 0.25f;
            if (enemy.Weapon != null && enemy.Weapon.Class == WeaponClass.MachineGun) return 0.3f;
            if (enemy.Role == CrewRole.Gunner) return 0.35f;
            if (enemy.Weapon != null && enemy.Weapon.Class == WeaponClass.SniperRifle) return 0.4f;
            return 1f;
        }

        bool HasLineOfFire(float2 from, Posture fromPosture, float2 to, Posture toPosture, float range)
        {
            var sight = LineOfSight.Check(_battle.Map, from, fromPosture, to, toPosture, range);
            return sight.Visible && !_battle.Smoke.Blocks(from, to, _battle.Clock.ElapsedSeconds);
        }

        void FireAt(Soldier shooter, Soldier target, float now, bool moving)
        {
            var weapon = shooter.Weapon;
            int rounds = math.min(weapon.Burst, shooter.Ammo);
            float range = math.distance(shooter.Position, target.Position);
            float cover = CoverOf(target, shooter.Position);
            float chance = HitChance(weapon, range, shooter, target, cover, moving);

            var cause = weapon.Class == WeaponClass.SniperRifle ? HitCause.Sniper : HitCause.Gunfire;
            for (int i = 0; i < rounds; i++)
            {
                bool hit = target.IsActive && _battle.Random.Chance(chance);
                float2 impact = hit ? target.Position : target.Position + RandomOffset(0.8f, 3.5f);
                if (hit)
                {
                    InflictHit(target, weapon.Lethality, now, shooter, cause);
                }

                ApplySuppression(impact, weapon.Suppression, shooter.Team.Side, now, target);
                _recentShots.Add(new ShotEvent(shooter.Id, shooter.Position, impact, hit, weapon.Class, now));
            }

            FinishBurst(shooter, rounds, now, Soldier.HeadingOf(target.Position - shooter.Position), moving);
        }

        /// <summary>
        /// A flamethrower burst (M12, D38): everyone within a few metres of the target
        /// is caught. Walls and windows help little against flame poured in, and the
        /// terror spreads further than the fire; nothing keeps burning afterwards.
        /// </summary>
        void FlameAt(Soldier shooter, Soldier target, float now)
        {
            var weapon = shooter.Weapon;
            float2 point = target.Position;
            foreach (var victim in _battle.Soldiers)
            {
                if (victim.Team.Side == shooter.Team.Side || !victim.IsActive || victim.IsMounted)
                {
                    continue;
                }

                float d = math.distance(victim.Position, point);
                if (d <= FlameRadius)
                {
                    float cover = CoverOf(victim, shooter.Position);
                    float chance = weapon.Accuracy * (1f - d / FlameRadius * 0.4f) * (1f - cover / 100f * 0.3f);
                    if (_battle.Random.Chance(chance))
                    {
                        InflictHit(victim, weapon.Lethality, now, shooter, HitCause.Flame);
                    }
                }

                if (victim.IsActive && d <= FlameRadius * 2.5f)
                {
                    victim.Suppression = math.min(100f, victim.Suppression + weapon.Suppression * (1f - d / (FlameRadius * 2.5f) * 0.5f));
                    victim.Morale = math.max(0f, victim.Morale - Psychology.FlameTerrorLoss * 0.5f * Psychology.Nerve(victim));
                    victim.LastUnderFireAt = now;
                }
            }

            _recentShots.Add(new ShotEvent(shooter.Id, shooter.Position, point, true, weapon.Class, now));
            FinishBurst(shooter, 1, now, Soldier.HeadingOf(point - shooter.Position), false);
        }

        /// <summary>Area fire: rake a point to keep heads down, whether or not anyone is seen there.</summary>
        void FireAtArea(Soldier shooter, float2 point, float now)
        {
            var weapon = shooter.Weapon;
            float distance = math.distance(shooter.Position, point);
            if (distance > weapon.MaxRange || !HasLineOfFire(shooter.Position, shooter.Posture, point, Posture.Standing, weapon.MaxRange))
            {
                return;
            }

            int rounds = math.min(weapon.Burst, shooter.Ammo);
            for (int i = 0; i < rounds; i++)
            {
                float2 impact = point + RandomOffset(0f, 4f);
                bool hit = false;
                foreach (var victim in _battle.Soldiers)
                {
                    if (victim.Team.Side == shooter.Team.Side || !victim.IsActive || victim.IsMounted || math.distance(victim.Position, impact) > 1.5f)
                    {
                        continue;
                    }

                    float cover = CoverOf(victim, shooter.Position);
                    float chance = 0.25f * PostureFactor(victim.Posture) * (1f - cover / 100f * 0.85f);
                    if (_battle.Random.Chance(chance))
                    {
                        ApplyHit(victim, weapon, now, shooter);
                        hit = true;
                        break;
                    }
                }

                ApplySuppression(impact, weapon.Suppression, shooter.Team.Side, now, null);
                _recentShots.Add(new ShotEvent(shooter.Id, shooter.Position, impact, hit, weapon.Class, now));
            }

            FinishBurst(shooter, rounds, now, Soldier.HeadingOf(point - shooter.Position), false);
        }

        void FinishBurst(Soldier shooter, int rounds, float now, float heading, bool moving)
        {
            var weapon = shooter.Weapon;
            shooter.Ammo -= rounds;
            shooter.FireCooldown = weapon.Interval * _battle.Random.Range(0.85f, 1.2f) * (shooter.IsWounded ? 1.3f : 1f);
            shooter.LastFiredAt = now;
            shooter.Heading = heading;
            if (!moving && shooter.Posture == Posture.Standing)
            {
                shooter.Posture = Posture.Kneeling;
            }

            // An ambush or a hiding team that opens fire has given itself away: it now fights normally.
            // A sniper's single shot from cover does not: he stays hidden and keeps picking targets.
            var team = shooter.Team;
            bool sniper = weapon.Class == WeaponClass.SniperRifle;
            if (!sniper && (team.Order.Type == OrderType.Ambush || team.Order.Type == OrderType.Hide))
            {
                team.Order = new TeamOrder(OrderType.Defend, team.Centroid);
            }

            _battle.Visibility.ReportSound(shooter, shooter.Position, weapon.AudibleRange, sniper ? SniperSoundFuzz : SoundFuzz);
        }

        /// <summary>Chance that one round hits. Public so tests and the info panel can explain it.</summary>
        public static float HitChance(WeaponDefinition weapon, float range, Soldier shooter, Soldier target, float cover, bool shooterMoving)
        {
            float p = weapon.HitChanceAtRange(range);
            p *= 0.6f + shooter.Attributes.Marksmanship / 100f * 0.7f;
            p *= PostureFactor(target.Posture);
            p *= 1f - math.saturate(cover / 100f) * 0.85f;
            p *= math.max(0.2f, 1f - shooter.Suppression / 140f);
            if (shooterMoving)
            {
                p *= 0.5f;
            }

            if (target.IsMoving && target.Posture == Posture.Standing)
            {
                p *= 0.75f;
            }

            if (shooter.IsWounded)
            {
                p *= 0.7f;
            }

            return math.clamp(p, 0f, 0.95f);
        }

        static float PostureFactor(Posture posture)
        {
            switch (posture)
            {
                case Posture.Prone: return 0.45f;
                case Posture.Kneeling: return 0.75f;
                default: return 1f;
            }
        }

        /// <summary>
        /// Cover a target has against fire from <paramref name="from"/>: his own cell
        /// (building, rubble, crater) or anything tall enough within 2.5 m on the
        /// shooter's side of him (a wall, a rubble heap).
        /// </summary>
        public float CoverOf(Soldier target, float2 from)
        {
            var grid = _battle.Map.Grid;
            var cell = grid.WorldToCell(target.Position);
            float cover = grid.InBounds(cell) ? grid.GetCover(grid.IndexOf(cell)) : 0f;

            float2 toward = math.normalizesafe(from - target.Position);
            var visitor = new CoverVisitor
            {
                Grid = grid,
                TargetIndex = grid.InBounds(cell) ? grid.IndexOf(cell) : -1,
                NeededHeight = BodyHeight(target.Posture) * 0.6f,
                Best = cover,
            };
            GridTraversal.Traverse(grid, target.Position, target.Position + toward * 2.5f, ref visitor);

            // Anti-tank gun crews shelter behind the gun shield against fire from the front.
            var gun = target.Team.Gun;
            if (target.Team.GunTeam != null && target.Team.GunTeam.HasShield && gun != null && !gun.Destroyed
                && math.distance(target.Position, gun.Position) < 3f
                && math.abs(VehicleMovement.DeltaAngle(gun.Heading, Soldier.HeadingOf(from - gun.Position))) < 60f)
            {
                return math.max(visitor.Best, 60f);
            }

            return visitor.Best;
        }

        static float BodyHeight(Posture posture)
        {
            switch (posture)
            {
                case Posture.Prone: return 0.4f;
                case Posture.Kneeling: return 1.0f;
                default: return 1.7f;
            }
        }

        struct CoverVisitor : ICellVisitor
        {
            public TacticalGrid Grid;
            public int TargetIndex;
            public float NeededHeight;
            public float Best;

            public bool Visit(int x, int y, float tEnter, float tExit)
            {
                int index = Grid.IndexOf(x, y);
                if (index != TargetIndex && Grid.GetObstacleHeight(index) >= NeededHeight)
                {
                    Best = math.max(Best, Grid.GetCover(index));
                }

                return true;
            }
        }

        void ApplyHit(Soldier target, WeaponDefinition weapon, float now, Soldier shooter) =>
            InflictHit(target, weapon.Lethality, now, shooter, weapon.Class == WeaponClass.SniperRifle ? HitCause.Sniper : HitCause.Gunfire);

        /// <summary>
        /// A hit on a soldier: wounds, disables or kills him depending on
        /// <paramref name="lethality"/>, and shakes the comrades around him.
        /// Shared by small arms, shell bursts and knocked-out tanks.
        /// </summary>
        internal void InflictHit(Soldier target, float lethality, float now, Soldier by = null, HitCause cause = HitCause.Gunfire)
        {
            if (!target.IsActive)
            {
                return;
            }

            float roll = _battle.Random.NextFloat();
            SoldierStatus result;
            if (target.IsWounded)
            {
                result = roll < 0.5f ? SoldierStatus.Dead : SoldierStatus.Incapacitated;
            }
            else if (roll < lethality * 0.55f)
            {
                result = SoldierStatus.Dead;
            }
            else if (roll < lethality)
            {
                result = SoldierStatus.Incapacitated;
            }
            else
            {
                result = SoldierStatus.Wounded;
            }

            target.Status = result;
            target.LastUnderFireAt = now;
            if (by != null && by.Team.Side != target.Team.Side && (result == SoldierStatus.Dead || result == SoldierStatus.Incapacitated))
            {
                by.Kills++;
            }

            _battle.Psychology.OnCasualty(target, result, now, cause);
            if (!target.IsActive)
            {
                target.IsMoving = false;
                target.Posture = Posture.Prone;
            }

            // Seeing a comrade fall shakes everyone near him (M5 deepens this).
            foreach (var mate in target.Team.Members)
            {
                if (mate != target && mate.IsActive && math.distance(mate.Position, target.Position) < 8f)
                {
                    mate.Suppression = math.min(100f, mate.Suppression + 12f);
                    mate.LastUnderFireAt = now;
                }
            }
        }

        void ApplySuppression(float2 impact, float amount, Side shooterSide, float now, Soldier primary)
        {
            var grid = _battle.Map.Grid;
            foreach (var soldier in _battle.Soldiers)
            {
                if (soldier.Team.Side == shooterSide || !soldier.IsActive || soldier.IsMounted)
                {
                    continue;
                }

                float distance = math.distance(soldier.Position, impact);
                float proximity;
                if (distance <= SuppressionRadius)
                {
                    proximity = 1f - distance / SuppressionRadius * 0.5f;
                }
                else if (soldier == primary)
                {
                    proximity = 0.5f; // the man being shot at always feels it
                }
                else
                {
                    continue;
                }

                var cell = grid.WorldToCell(soldier.Position);
                float cover = grid.InBounds(cell) ? grid.GetCover(grid.IndexOf(cell)) : 0f;
                float coverFactor = 1f - cover / 100f * 0.5f;
                // Veterans shrug off more.
                float nerve = 1.2f - soldier.Attributes.Experience / 100f * 0.5f;

                soldier.Suppression = math.min(100f, soldier.Suppression + amount * proximity * coverFactor * nerve);
                soldier.LastUnderFireAt = now;
            }
        }

        void DecaySuppression(float dt, float now)
        {
            foreach (var soldier in _battle.Soldiers)
            {
                if (!soldier.IsActive || soldier.Suppression <= 0f || now - soldier.LastUnderFireAt < DecayDelay)
                {
                    continue;
                }

                float rate = SuppressionDecayPerSecond * (soldier.IsWounded ? 0.6f : 1f);
                soldier.Suppression = math.max(0f, soldier.Suppression - rate * dt);
            }
        }

        float2 RandomOffset(float minDistance, float maxDistance)
        {
            float angle = _battle.Random.Range(0f, math.PI * 2f);
            float distance = _battle.Random.Range(minDistance, maxDistance);
            return new float2(math.cos(angle), math.sin(angle)) * distance;
        }
    }
}
