using System.Collections.Generic;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.Battle
{
    /// <summary>A shell burst, kept briefly for the view.</summary>
    public readonly struct ExplosionEvent
    {
        public readonly float2 Position;
        public readonly float Radius;
        public readonly float Time;
        public readonly bool Smoke;

        public ExplosionEvent(float2 position, float radius, float time, bool smoke)
        {
            Position = position;
            Radius = radius;
            Time = time;
            Smoke = smoke;
        }
    }

    /// <summary>
    /// Heavy weapons (M6): tank guns, anti-tank guns and mortars.
    ///
    /// Armor (D37): a hit's facing comes from the angle between the hull and
    /// the shooter; effective armor grows as the hit gets more oblique, so
    /// angling the hull helps. Penetration falls with range. A penetrating hit
    /// may brew the tank up, kill crew, wreck the gun or immobilize it.
    /// High explosive kills and suppresses infantry in a radius (both sides:
    /// shells do not care whose men are near) and wears buildings down until
    /// they collapse (D38). Mortar shells fly for several seconds and scatter.
    /// Crews bail out when half of them are down, the vehicle is wrecked, or
    /// they lose their nerve; infantry close-assault tanks with grenades.
    /// </summary>
    public sealed class Gunnery
    {
        public const float CloseAssaultRange = 25f;
        const float CloseAssaultCooldown = 8f;
        const float HeavyAmbushRange = 150f;
        const float DirectHeRange = 800f;
        const float ExplosionHistorySeconds = 1.5f;

        struct PendingShell
        {
            public float2 Point;
            public float ImpactAt;
            public GunDefinition Gun;
            public bool Smoke;
            public int ShooterId;
        }

        struct Aim
        {
            public bool Valid;
            public Vehicle Vehicle;
            public float2 Point;
            public bool ArmorPiercing;
            public bool Smoke;
        }

        readonly BattleState _battle;
        readonly List<PendingShell> _shells = new List<PendingShell>();
        readonly List<ExplosionEvent> _explosions = new List<ExplosionEvent>();

        public IReadOnlyList<ExplosionEvent> RecentExplosions => _explosions;

        /// <summary>Records a burst for the view (mines and charges set off by engineers).</summary>
        internal void AddExplosion(float2 point, float radius, float now) => _explosions.Add(new ExplosionEvent(point, radius, now, false));
        public int ShellsInFlight => _shells.Count;

        public Gunnery(BattleState battle)
        {
            _battle = battle;
        }

        public void Step(float dt)
        {
            float now = _battle.Clock.ElapsedSeconds;

            foreach (var vehicle in _battle.Vehicles)
            {
                if (vehicle.IsOperational && ShouldBailOut(vehicle))
                {
                    BailOut(vehicle, now);
                }
            }

            foreach (var team in _battle.Teams)
            {
                if (team.Gun != null)
                {
                    StepWeapon(team.Gun, dt, now);
                }

                StepCloseAssault(team, dt, now);
            }

            for (int i = _shells.Count - 1; i >= 0; i--)
            {
                if (_shells[i].ImpactAt <= now)
                {
                    var shell = _shells[i];
                    _shells.RemoveAt(i);
                    if (shell.Smoke)
                    {
                        _battle.Smoke.Add(shell.Point, now);
                        _explosions.Add(new ExplosionEvent(shell.Point, 2f, now, true));
                    }
                    else
                    {
                        Blast(shell.Point, shell.Gun, now, _battle.Soldiers[shell.ShooterId]);
                    }
                }
            }

            _explosions.RemoveAll(e => now - e.Time > ExplosionHistorySeconds);
        }

        // ------------------------------------------------------------ one gun

        void StepWeapon(HeavyWeapon weapon, float dt, float now)
        {
            var team = weapon.Team;
            var vehicle = weapon.Vehicle;
            weapon.PreviousHeading = weapon.Heading;

            if (weapon.Destroyed || (vehicle != null && (!vehicle.IsOperational || vehicle.GunDamaged)))
            {
                return;
            }

            var op = weapon.Operator;
            if (op == null)
            {
                return;
            }

            // Towed guns and mortars move with their crew and must be set up again after moving.
            if (vehicle == null)
            {
                if (team.Activity != TeamActivity.Idle)
                {
                    weapon.Emplacement = op.Position;
                    weapon.SetupRemaining = weapon.Definition.SetupSeconds;
                    return;
                }

                weapon.SetupRemaining = math.max(0f, weapon.SetupRemaining - dt);
                if (op.IsPinned)
                {
                    return; // crew hugging the ground cannot serve an unarmored gun
                }
            }
            else if (team.Activity != TeamActivity.Idle && (team.Order.Type == OrderType.MoveFast || team.Order.Type == OrderType.Reverse || team.Order.Type == OrderType.Retreat))
            {
                return; // driving flat out
            }

            weapon.Reload = math.max(0f, weapon.Reload - dt);
            if (weapon.SetupRemaining > 0f)
            {
                return;
            }

            var aim = ChooseTarget(weapon, op, now);
            if (!aim.Valid)
            {
                return;
            }

            bool aligned = Traverse(weapon, aim.Point, dt);
            if (!aligned || weapon.Reload > 0f)
            {
                return;
            }

            Fire(weapon, op, aim, now);
        }

        Aim ChooseTarget(HeavyWeapon weapon, Soldier op, float now)
        {
            var team = weapon.Team;
            var gun = weapon.Definition;
            float2 from = weapon.Position;
            var aim = new Aim();

            if (weapon.SmokePending)
            {
                weapon.SmokePending = false;
                if (weapon.AmmoSmoke > 0 && InRange(gun, math.distance(from, weapon.SmokeTarget)))
                {
                    aim.Valid = true;
                    aim.Smoke = true;
                    aim.Point = weapon.SmokeTarget;
                    return aim;
                }
            }

            if (team.Order.Type == OrderType.AreaFire && team.Activity == TeamActivity.Idle && weapon.AmmoHe > 0)
            {
                float2 point = team.Order.Target;
                if (InRange(gun, math.distance(from, point)) && (gun.Indirect || HasLineOfFire(weapon, point, Posture.Standing)))
                {
                    aim.Valid = true;
                    aim.Point = point;
                    return aim;
                }
            }

            float holdRange = team.Order.Type == OrderType.Ambush ? HeavyAmbushRange
                : team.Order.Type == OrderType.Hide ? 15f
                : float.PositiveInfinity;

            // A named target first, then any enemy tank, then infantry.
            if (team.FireTarget != null && TryAimAtTeam(weapon, team.FireTarget, float.PositiveInfinity, ref aim))
            {
                return aim;
            }

            if (gun.CanPierce && weapon.AmmoAp > 0)
            {
                Vehicle best = null;
                float bestDistance = math.min(gun.MaxRange, holdRange);
                foreach (var enemy in _battle.Vehicles)
                {
                    if (enemy.Team.Side == team.Side || !enemy.IsOperational || !_battle.Visibility.CanSee(team.Side, enemy))
                    {
                        continue;
                    }

                    float d = math.distance(from, enemy.Position);
                    if (d < bestDistance && (gun.Indirect || HasLineOfFire(weapon, enemy.Position, Posture.Standing)))
                    {
                        best = enemy;
                        bestDistance = d;
                    }
                }

                if (best != null)
                {
                    aim.Valid = true;
                    aim.Vehicle = best;
                    aim.Point = best.Position;
                    aim.ArmorPiercing = true;
                    return aim;
                }
            }

            if (weapon.AmmoHe > 0)
            {
                Soldier best = null;
                float maxRange = gun.Indirect ? gun.MaxRange : math.min(DirectHeRange, gun.MaxRange);
                float bestDistance = math.min(maxRange, holdRange);
                foreach (var enemy in _battle.Soldiers)
                {
                    if (enemy.Team.Side == team.Side || !enemy.IsActive || enemy.IsMounted || !_battle.Visibility.CanSee(team.Side, enemy))
                    {
                        continue;
                    }

                    float d = math.distance(from, enemy.Position);
                    if (d < bestDistance && d >= gun.MinRange && (gun.Indirect || HasLineOfFire(weapon, enemy.Position, enemy.Posture)))
                    {
                        best = enemy;
                        bestDistance = d;
                    }
                }

                if (best != null)
                {
                    aim.Valid = true;
                    aim.Point = best.Position;
                    return aim;
                }
            }

            return aim;
        }

        bool TryAimAtTeam(HeavyWeapon weapon, Team target, float holdRange, ref Aim aim)
        {
            var side = weapon.Team.Side;
            float2 from = weapon.Position;
            var gun = weapon.Definition;

            if (target.IsMechanized && _battle.Visibility.CanSee(side, target.Vehicle) && InRange(gun, math.distance(from, target.Vehicle.Position))
                && (gun.Indirect || HasLineOfFire(weapon, target.Vehicle.Position, Posture.Standing)))
            {
                aim.Valid = true;
                aim.Vehicle = target.Vehicle;
                aim.Point = target.Vehicle.Position;
                aim.ArmorPiercing = gun.CanPierce && weapon.AmmoAp > 0;
                return aim.ArmorPiercing || weapon.AmmoHe > 0;
            }

            if (weapon.AmmoHe <= 0)
            {
                return false;
            }

            foreach (var member in target.Members)
            {
                if (!member.IsActive || member.IsMounted || !_battle.Visibility.CanSee(side, member))
                {
                    continue;
                }

                float d = math.distance(from, member.Position);
                if (d <= holdRange && InRange(gun, d) && (gun.Indirect || HasLineOfFire(weapon, member.Position, member.Posture)))
                {
                    aim.Valid = true;
                    aim.Point = member.Position;
                    return true;
                }
            }

            return false;
        }

        static bool InRange(GunDefinition gun, float distance) => distance >= gun.MinRange && distance <= gun.MaxRange;

        bool HasLineOfFire(HeavyWeapon weapon, float2 point, Posture targetPosture)
        {
            float2 from = weapon.Position;
            var posture = weapon.Vehicle != null ? Posture.Standing : Posture.Kneeling;
            var sight = LineOfSight.Check(_battle.Map, from, posture, point, targetPosture, weapon.Definition.MaxRange);
            return sight.Visible && !_battle.Smoke.Blocks(from, point, _battle.Clock.ElapsedSeconds);
        }

        /// <summary>Swings the gun (or, for a casemate assault gun, the whole hull) toward the target. True when on target.</summary>
        bool Traverse(HeavyWeapon weapon, float2 point, float dt)
        {
            var gun = weapon.Definition;
            float2 toTarget = point - weapon.Position;
            if (math.lengthsq(toTarget) < 0.01f)
            {
                return true;
            }

            float wanted = Soldier.HeadingOf(toTarget);
            var vehicle = weapon.Vehicle;

            if (gun.Indirect)
            {
                weapon.Heading = wanted;
                return true;
            }

            if (vehicle != null && gun.TraverseLimit < 180f)
            {
                // Casemate (StuG): beyond the narrow mount, the driver must swing the hull.
                float relative = VehicleMovement.DeltaAngle(vehicle.HullHeading, wanted);
                if (math.abs(relative) > gun.TraverseLimit && !vehicle.IsMoving && !vehicle.Immobilized)
                {
                    float maxTurn = vehicle.Definition.TurnRate * dt;
                    vehicle.HullHeading = VehicleMovement.Wrap(vehicle.HullHeading + math.clamp(relative, -maxTurn, maxTurn));
                    VehicleMovement.SyncCrew(vehicle);
                    relative = VehicleMovement.DeltaAngle(vehicle.HullHeading, wanted);
                }

                weapon.Heading = VehicleMovement.Wrap(vehicle.HullHeading + math.clamp(relative, -gun.TraverseLimit, gun.TraverseLimit));
            }
            else
            {
                float delta = VehicleMovement.DeltaAngle(weapon.Heading, wanted);
                // Towed guns turn slowly once the target is outside the trail's arc.
                float rate = gun.TraverseRate * (vehicle == null && math.abs(delta) > gun.TraverseLimit ? 0.4f : 1f);
                float step = rate * dt;
                weapon.Heading = VehicleMovement.Wrap(weapon.Heading + math.clamp(delta, -step, step));
            }

            return math.abs(VehicleMovement.DeltaAngle(weapon.Heading, wanted)) < 2f;
        }

        void Fire(HeavyWeapon weapon, Soldier op, Aim aim, float now)
        {
            var gun = weapon.Definition;
            var team = weapon.Team;
            float2 from = weapon.Position;
            float range = math.distance(from, aim.Point);

            weapon.Reload = gun.ReloadSeconds * ReloadFactor(weapon, op);
            op.LastFiredAt = now;
            if (weapon.Vehicle != null)
            {
                weapon.Vehicle.LastFiredAt = now;
            }

            if (team.Order.Type == OrderType.Ambush || team.Order.Type == OrderType.Hide)
            {
                team.Order = new TeamOrder(OrderType.Defend, team.Order.Target);
            }

            _battle.Visibility.ReportSound(op, from, 1200f, 30f);

            if (gun.Indirect)
            {
                if (aim.Smoke)
                {
                    weapon.AmmoSmoke--;
                }
                else
                {
                    weapon.AmmoHe--;
                }

                float scatter = 4f + range * 0.03f;
                float2 impact = aim.Point + RandomInDisk(scatter);
                _shells.Add(new PendingShell { Point = impact, ImpactAt = now + 4f + range / 150f, Gun = gun, Smoke = aim.Smoke, ShooterId = op.Id });
                return;
            }

            float chance = DirectHitChance(weapon, op, aim, range);
            bool hit = _battle.Random.Chance(chance);

            if (aim.ArmorPiercing && aim.Vehicle != null)
            {
                weapon.AmmoAp--;
                if (hit)
                {
                    bool pierced = ArmorHit(aim.Vehicle, from, gun.PenetrationAt(range), now, op);
                    _battle.Combat.RecordShot(op.Id, from, aim.Vehicle.Position, pierced, WeaponClass.Cannon, now);
                }
                else
                {
                    _battle.Combat.RecordShot(op.Id, from, aim.Point + RandomOffset(3f, 8f), false, WeaponClass.Cannon, now);
                }

                return;
            }

            weapon.AmmoHe--;
            float2 burst = hit ? aim.Point : aim.Point + RandomOffset(2f, 4f + range * 0.02f);
            _battle.Combat.RecordShot(op.Id, from, burst, hit, WeaponClass.Cannon, now);
            Blast(burst, gun, now, op);
        }

        float DirectHitChance(HeavyWeapon weapon, Soldier op, Aim aim, float range)
        {
            float p = weapon.Definition.HitChanceAtRange(range);
            p *= 0.6f + op.Attributes.Marksmanship / 100f * 0.7f;
            p *= math.max(0.3f, 1f - op.Suppression / 160f);
            if (aim.Vehicle != null && aim.Vehicle.IsMoving)
            {
                p *= 0.6f;
            }

            if (aim.Vehicle == null)
            {
                p *= 0.7f; // a man is a small target for a cannon
            }

            if (weapon.Vehicle != null && weapon.Vehicle.IsMoving)
            {
                p *= 0.35f;
            }

            return math.clamp(p, 0.03f, 0.95f);
        }

        float ReloadFactor(HeavyWeapon weapon, Soldier op)
        {
            float factor = 1f;
            if (op.Role != CrewRole.Gunner)
            {
                factor *= 1.3f;
            }

            var vehicle = weapon.Vehicle;
            if (vehicle != null)
            {
                bool hasLoaderSeat = System.Array.IndexOf(vehicle.Definition.Crew, CrewRole.Loader) >= 0;
                if (hasLoaderSeat && vehicle.CrewIn(CrewRole.Loader) == null)
                {
                    factor *= 1.5f;
                }
            }
            else if (weapon.Team.ActiveCount < 2)
            {
                factor *= 2f; // one man serving a gun alone
            }

            return factor;
        }

        // ------------------------------------------------------------ armor

        /// <summary>
        /// Which facing a shot from <paramref name="from"/> strikes, and the
        /// effective armor there: plate thickness divided by the cosine of how far
        /// off square the hit lands (capped at double). No randomness.
        /// </summary>
        public static float EffectiveArmor(VehicleDefinition definition, float hullHeading, float2 position, float2 from, out ArmorFacing facing)
        {
            float bearing = Soldier.HeadingOf(from - position);
            float relative = math.abs(VehicleMovement.DeltaAngle(hullHeading, bearing));
            float obliquity;
            if (relative < 45f)
            {
                facing = ArmorFacing.Front;
                obliquity = relative;
            }
            else if (relative > 135f)
            {
                facing = ArmorFacing.Rear;
                obliquity = 180f - relative;
            }
            else
            {
                facing = ArmorFacing.Side;
                obliquity = math.abs(relative - 90f);
            }

            return definition.Armor.Of(facing) / math.max(math.cos(math.radians(obliquity)), 0.5f);
        }

        /// <summary>Resolves an armor-piercing hit. Returns true if it went through.</summary>
        public bool ArmorHit(Vehicle vehicle, float2 from, float penetration, float now, Soldier by = null)
        {
            float armor = EffectiveArmor(vehicle.Definition, vehicle.HullHeading, vehicle.Position, from, out var facing);
            armor *= _battle.Random.Range(0.9f, 1.1f);
            penetration *= _battle.Random.Range(0.9f, 1.1f);

            if (penetration <= armor)
            {
                ShakeCrew(vehicle, 15f, now);
                if (facing != ArmorFacing.Front && _battle.Random.Chance(0.1f))
                {
                    vehicle.Immobilized = true; // a track knocked off
                }

                return false;
            }

            ShakeCrew(vehicle, 50f, now);
            float roll = _battle.Random.NextFloat();
            if (roll < 0.25f)
            {
                Destroy(vehicle, now, by);
            }
            else if (roll < 0.55f)
            {
                HitCrew(vehicle, _battle.Random.Range(1, 3), now, by);
            }
            else if (roll < 0.70f)
            {
                vehicle.GunDamaged = true;
            }
            else if (roll < 0.90f)
            {
                vehicle.Immobilized = true;
            }

            return true;
        }

        void ShakeCrew(Vehicle vehicle, float suppression, float now)
        {
            foreach (var member in vehicle.Team.Members)
            {
                if (member.IsActive && member.IsMounted)
                {
                    member.Suppression = math.min(100f, member.Suppression + suppression * Psychology.Nerve(member));
                    member.LastUnderFireAt = now;
                }
            }
        }

        void HitCrew(Vehicle vehicle, int count, float now, Soldier by = null)
        {
            for (int i = 0; i < count; i++)
            {
                var aboard = new List<Soldier>();
                foreach (var member in vehicle.Team.Members)
                {
                    if (member.IsActive && member.IsMounted)
                    {
                        aboard.Add(member);
                    }
                }

                if (aboard.Count == 0)
                {
                    return;
                }

                _battle.Combat.InflictHit(aboard[_battle.Random.Range(0, aboard.Count)], 0.8f, now, by);
            }
        }

        void Destroy(Vehicle vehicle, float now, Soldier by = null)
        {
            vehicle.Destroyed = true;
            if (by != null && by.Team.Side != vehicle.Team.Side)
            {
                by.VehiclesKnockedOut++;
            }

            vehicle.IsMoving = false;
            foreach (var member in vehicle.Team.Members)
            {
                if (member.IsActive && member.IsMounted)
                {
                    if (_battle.Random.Chance(0.55f))
                    {
                        member.Status = SoldierStatus.Dead;
                        if (by != null)
                        {
                            by.Kills++;
                        }

                        _battle.Psychology.OnCasualty(member, SoldierStatus.Dead, now);
                    }
                    else
                    {
                        member.Status = SoldierStatus.Wounded;
                    }
                }
            }

            BailOut(vehicle, now);
        }

        // ------------------------------------------------------------ crews

        bool ShouldBailOut(Vehicle vehicle)
        {
            int crew = vehicle.Definition.Crew.Length;
            int aboard = 0;
            bool lostNerve = false;
            foreach (var member in vehicle.Team.Members)
            {
                if (!member.IsActive || !member.IsMounted)
                {
                    continue;
                }

                aboard++;
                if (member.MoraleState == MoraleState.Panicked || member.MoraleState == MoraleState.Routed)
                {
                    lostNerve = true;
                }
            }

            return aboard <= crew / 2 || lostNerve || (vehicle.GunDamaged && vehicle.Immobilized);
        }

        /// <summary>The crew abandon the vehicle and fight on foot with their sidearms (D37).</summary>
        public void BailOut(Vehicle vehicle, float now)
        {
            vehicle.Abandoned = true;
            vehicle.IsMoving = false;
            vehicle.Path.Clear();
            vehicle.PathCumulative.Clear();
            var team = vehicle.Team;
            team.Activity = TeamActivity.Idle;
            team.FireTarget = null;
            team.Order = new TeamOrder(OrderType.Defend, vehicle.Position);

            var grid = _battle.Map.Grid;
            int index = 0;
            foreach (var member in team.Members)
            {
                if (!member.IsMounted)
                {
                    continue;
                }

                member.MountedIn = null;
                float angle = (index++ / 5f) * math.PI * 2f + 0.4f;
                float2 spot = vehicle.Position + new float2(math.cos(angle), math.sin(angle)) * 2.8f;
                var cell = grid.WorldToCell(spot);
                if (!grid.InBounds(cell) || grid.HasFlag(grid.IndexOf(cell), CellFlags.BlocksMovement))
                {
                    spot = vehicle.Position;
                }

                member.Position = spot;
                member.PreviousPosition = spot;
                member.IsMoving = false;
                member.Posture = Posture.Prone;
                member.Weapon = Weapons.CrewSidearm(team.Side, member.Role == CrewRole.Commander);
                member.Ammo = member.Weapon.StartingAmmo;
                if (member.IsActive)
                {
                    member.Suppression = math.min(100f, member.Suppression + 20f);
                    member.Morale = math.max(0f, member.Morale - 10f);
                    member.LastUnderFireAt = now;
                }
            }
        }

        /// <summary>
        /// Close assault (D37): a team with anti-tank grenades and an enemy tank
        /// within 25 m sends its nearest steady man in with a bundled charge.
        /// </summary>
        void StepCloseAssault(Team team, float dt, float now)
        {
            team.CloseAssaultCooldown = math.max(0f, team.CloseAssaultCooldown - dt);
            if (team.AntiTankGrenades <= 0 || team.CloseAssaultCooldown > 0f || team.IsMechanized
                || team.Order.Type == OrderType.Hide || team.Activity == TeamActivity.Moving && team.Order.Type == OrderType.MoveFast)
            {
                return;
            }

            foreach (var vehicle in _battle.Vehicles)
            {
                if (vehicle.Team.Side == team.Side || !vehicle.IsOperational || !_battle.Visibility.CanSee(team.Side, vehicle))
                {
                    continue;
                }

                Soldier thrower = null;
                float best = CloseAssaultRange;
                foreach (var member in team.Members)
                {
                    if (!member.IsActive || member.IsOutOfControl || member.IsPinned)
                    {
                        continue;
                    }

                    float d = math.distance(member.Position, vehicle.Position);
                    if (d < best)
                    {
                        best = d;
                        thrower = member;
                    }
                }

                if (thrower == null)
                {
                    continue;
                }

                team.AntiTankGrenades--;
                team.CloseAssaultCooldown = CloseAssaultCooldown;
                thrower.LastFiredAt = now;
                thrower.Heading = Soldier.HeadingOf(vehicle.Position - thrower.Position);
                float chance = vehicle.IsMoving ? 0.3f : 0.5f;
                bool hit = _battle.Random.Chance(chance);
                float2 burst = hit ? vehicle.Position : vehicle.Position + RandomOffset(2f, 5f);
                _explosions.Add(new ExplosionEvent(burst, 2f, now, false));
                if (hit)
                {
                    CloseAssaultHit(vehicle, now, thrower);
                }

                return;
            }
        }

        void CloseAssaultHit(Vehicle vehicle, float now, Soldier by)
        {
            ShakeCrew(vehicle, 50f, now);
            float roll = _battle.Random.NextFloat();
            if (roll < 0.2f)
            {
                Destroy(vehicle, now, by);
            }
            else if (roll < 0.45f)
            {
                HitCrew(vehicle, 1, now, by);
            }
            else if (roll < 0.55f)
            {
                vehicle.GunDamaged = true;
            }
            else if (roll < 0.9f)
            {
                vehicle.Immobilized = true;
            }
        }

        // ------------------------------------------------------------ blast

        /// <summary>A high-explosive burst: casualties and suppression around it, damage to the building it lands in.</summary>
        public void Blast(float2 point, GunDefinition gun, float now, Soldier by = null)
        {
            _explosions.Add(new ExplosionEvent(point, gun.HeLethalRadius, now, false));
            var map = _battle.Map;
            var grid = map.Grid;

            foreach (var soldier in _battle.Soldiers)
            {
                if (!soldier.IsActive || soldier.IsMounted)
                {
                    continue;
                }

                float d = math.distance(soldier.Position, point);
                if (d > gun.HeSuppressionRadius)
                {
                    continue;
                }

                var cell = grid.WorldToCell(soldier.Position);
                float cover = grid.InBounds(cell) ? grid.GetCover(grid.IndexOf(cell)) : 0f;

                if (d < gun.HeLethalRadius)
                {
                    float chance = 0.65f * (1f - d / gun.HeLethalRadius * 0.5f) * (1f - cover / 100f * 0.7f);
                    if (soldier.Posture == Posture.Prone)
                    {
                        chance *= 0.6f;
                    }

                    if (_battle.Random.Chance(chance))
                    {
                        _battle.Combat.InflictHit(soldier, 0.7f, now, by);
                    }
                }

                if (soldier.IsActive)
                {
                    float suppression = 50f * (1f - d / gun.HeSuppressionRadius) * (1f - cover / 100f * 0.4f) * Psychology.Nerve(soldier);
                    soldier.Suppression = math.min(100f, soldier.Suppression + suppression);
                    soldier.LastUnderFireAt = now;
                }
            }

            foreach (var vehicle in _battle.Vehicles)
            {
                if (vehicle.IsOperational && math.distance(vehicle.Position, point) < 3f)
                {
                    ShakeCrew(vehicle, 10f, now);
                    if (_battle.Random.Chance(0.05f))
                    {
                        vehicle.Immobilized = true;
                    }
                }
            }

            // Full damage where it lands, half to the cells around it.
            var impact = grid.WorldToCell(point);
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (grid.InBounds(impact.X + dx, impact.Y + dy))
                    {
                        float share = dx == 0 && dy == 0 ? 1f : 0.5f;
                        map.DamageCell(grid.IndexOf(impact.X + dx, impact.Y + dy), gun.StructureDamage * share);
                    }
                }
            }
        }

        float2 RandomOffset(float minDistance, float maxDistance)
        {
            float angle = _battle.Random.Range(0f, math.PI * 2f);
            float distance = _battle.Random.Range(minDistance, maxDistance);
            return new float2(math.cos(angle), math.sin(angle)) * distance;
        }

        float2 RandomInDisk(float radius)
        {
            float angle = _battle.Random.Range(0f, math.PI * 2f);
            float distance = radius * math.sqrt(_battle.Random.NextFloat());
            return new float2(math.cos(angle), math.sin(angle)) * distance;
        }
    }
}
