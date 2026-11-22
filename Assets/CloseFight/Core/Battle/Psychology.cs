using System.Collections.Generic;
using CloseFight.Core.Map;
using CloseFight.Core.Path;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.Battle
{
    /// <summary>
    /// The psychology model (M5), after Close Combat.
    ///
    /// Each soldier has two numbers: Suppression (short-term, from incoming fire,
    /// fades quickly) and Morale (long-term nerve, eroded by casualties, a fallen
    /// leader and prolonged fire, recovered slowly when calm near a leader).
    /// Together they set his state:
    ///   Steady → Suppressed → Pinned → Cowering → Panicked → Routed.
    /// Cowering, panicking and routing soldiers ignore orders: a panicked man
    /// dashes for the nearest cover away from the enemy, a routed one runs for
    /// his own map edge and leaves the battle.
    ///
    /// Teams (D14): half the team down → shaken (asks to pull back, may refuse
    /// to advance); two thirds down → broken (falls back on its own and forms a
    /// new line). Every state has a visible label (D29).
    /// </summary>
    public sealed class Psychology
    {
        public const float RoutMorale = 10f;
        public const float PanicMorale = 25f;
        public const float SuppressedThreshold = 35f;
        public const float CowerThreshold = 90f;
        public const float ShakenCasualties = 0.5f;
        public const float BrokenCasualties = 2f / 3f;
        public const float FallbackDistance = 40f;
        /// <summary>Morale lost per second while heavily suppressed.</summary>
        public const float ProlongedFireErosion = 0.25f;
        public const float WoundedSelfLoss = 10f;
        public const float ComradeDownLoss = 6f;
        public const float ComradeWoundedLoss = 2f;
        public const float LeaderDownLoss = 12f;

        readonly BattleState _battle;
        readonly TeamMovement _movement;
        readonly GridPathfinder _pathfinder;
        readonly List<GridCoord> _cells = new List<GridCoord>();

        public Psychology(BattleState battle, TeamMovement movement, GridPathfinder pathfinder)
        {
            _battle = battle;
            _movement = movement;
            _pathfinder = pathfinder;
        }

        /// <summary>Veterans lose less nerve: 1.2 for a recruit, 0.7 for a hardened veteran.</summary>
        public static float Nerve(Soldier soldier) => 1.2f - soldier.Attributes.Experience / 100f * 0.5f;

        public void Step(float dt)
        {
            float now = _battle.Clock.ElapsedSeconds;
            foreach (var team in _battle.Teams)
            {
                UpdateTeam(team, now);
            }

            foreach (var soldier in _battle.Soldiers)
            {
                if (!soldier.IsActive)
                {
                    continue;
                }

                UpdateMorale(soldier, dt, now);
                UpdateState(soldier);
                Act(soldier, dt);
            }
        }

        // ------------------------------------------------------------ team

        void UpdateTeam(Team team, float now)
        {
            float lost = 0f;
            float moraleSum = 0f;
            int active = 0;
            foreach (var member in team.Members)
            {
                if (!member.IsActive)
                {
                    lost += 1f;
                    continue;
                }

                if (member.IsWounded)
                {
                    lost += 0.5f;
                }

                moraleSum += member.Morale;
                active++;
            }

            team.CasualtyFraction = team.Members.Count == 0 ? 1f : lost / team.Members.Count;
            team.AverageMorale = active == 0 ? 0f : moraleSum / active;
            team.IsShaken = team.CasualtyFraction >= ShakenCasualties - 1e-3f || team.AverageMorale < 35f;
            team.IsBroken = team.CasualtyFraction >= BrokenCasualties - 1e-3f || team.AverageMorale < 15f;

            if (team.Members.Count > 0 && !team.Members[0].IsActive && !team.LeaderLost)
            {
                team.LeaderLostAt = now;
            }

            // D14: at two thirds casualties the team pulls back by itself and forms a new line.
            if (team.IsBroken && !team.HasFallenBack && active > 0)
            {
                team.HasFallenBack = true;
                team.FallingBack = _movement.PlanMove(team, _battle.FallbackPoint(team, FallbackDistance), OrderType.Retreat);
            }

            if (team.FallingBack && team.Activity == TeamActivity.Idle)
            {
                team.FallingBack = false;
                _movement.Halt(team, OrderType.Defend, Posture.Kneeling);
            }
        }

        // ------------------------------------------------------------ soldier

        void UpdateMorale(Soldier soldier, float dt, float now)
        {
            if (soldier.Suppression > 60f)
            {
                soldier.Morale -= ProlongedFireErosion * Nerve(soldier) * dt;
            }

            bool calm = now - soldier.LastUnderFireAt > 5f && soldier.Suppression < 10f;
            if (calm && soldier.Morale < soldier.BaseMorale && soldier.MoraleState != MoraleState.Routed)
            {
                float rate = 0.5f;
                var leader = soldier.Team.Leader;
                if (leader != null && leader != soldier && !leader.IsOutOfControl && math.distance(leader.Position, soldier.Position) < 15f)
                {
                    rate *= 1f + leader.Attributes.Leadership / 100f;
                }

                if (soldier.Team.LeaderLost)
                {
                    rate *= 0.6f;
                }

                soldier.Morale = math.min(soldier.BaseMorale, soldier.Morale + rate * dt);
            }

            soldier.Morale = math.clamp(soldier.Morale, 0f, 100f);
        }

        void UpdateState(Soldier soldier)
        {
            var old = soldier.MoraleState;
            float morale = soldier.Morale;
            float suppression = soldier.Suppression;

            MoraleState next;
            if (old == MoraleState.Routed || morale < RoutMorale)
            {
                next = MoraleState.Routed;
            }
            else if (old == MoraleState.Panicked && (suppression > 20f || morale < PanicMorale))
            {
                next = MoraleState.Panicked;
            }
            else if (morale < PanicMorale && suppression > 40f)
            {
                next = MoraleState.Panicked;
            }
            else if (suppression >= CowerThreshold || (suppression >= Soldier.PinnedThreshold && morale < 40f))
            {
                next = MoraleState.Cowering;
            }
            else if (suppression >= Soldier.PinnedThreshold)
            {
                next = MoraleState.Pinned;
            }
            else if (suppression >= SuppressedThreshold)
            {
                next = MoraleState.Suppressed;
            }
            else
            {
                next = MoraleState.Steady;
            }

            if (next == old)
            {
                return;
            }

            soldier.MoraleState = next;
            if (next == MoraleState.Panicked)
            {
                soldier.PanicSpot = FindCoverAwayFromEnemy(soldier);
            }
            else if (next == MoraleState.Routed)
            {
                PlanFlight(soldier);
            }
        }

        void Act(Soldier soldier, float dt)
        {
            switch (soldier.MoraleState)
            {
                case MoraleState.Pinned:
                case MoraleState.Cowering:
                    soldier.Posture = Posture.Prone;
                    soldier.IsMoving = false;
                    break;

                case MoraleState.Panicked:
                    if (math.distance(soldier.Position, soldier.PanicSpot) > 0.3f)
                    {
                        soldier.Posture = Posture.Standing;
                        _movement.Displace(soldier, soldier.PanicSpot, TeamMovement.RunSpeed, dt);
                    }
                    else
                    {
                        soldier.Posture = Posture.Prone;
                        soldier.IsMoving = false;
                    }

                    break;

                case MoraleState.Routed:
                    Flee(soldier, dt);
                    break;
            }
        }

        void Flee(Soldier soldier, float dt)
        {
            if (soldier.FleePath.Count < 2)
            {
                soldier.Posture = Posture.Prone;
                soldier.IsMoving = false;
                return;
            }

            float length = soldier.FleeCumulative[soldier.FleeCumulative.Count - 1];
            soldier.FleeProgress = math.min(length, soldier.FleeProgress + TeamMovement.RunSpeed * dt);
            float2 target = PathUtils.PointAt(soldier.FleePath, soldier.FleeCumulative, soldier.FleeProgress, out _);
            soldier.Posture = Posture.Standing;
            _movement.Displace(soldier, target, TeamMovement.RunSpeed * 1.3f, dt);

            // Off the map edge: he is gone for this battle.
            if (soldier.FleeProgress >= length - 1e-3f && math.distance(soldier.Position, target) < 1.5f)
            {
                soldier.Status = SoldierStatus.Fled;
                soldier.IsMoving = false;
            }
        }

        void PlanFlight(Soldier soldier)
        {
            soldier.FleePath.Clear();
            soldier.FleeCumulative.Clear();
            soldier.FleeProgress = 0f;

            var grid = _battle.Map.Grid;
            float2 edge = _battle.EdgePoint(soldier.Team.Side, soldier.Position);
            _pathfinder.FindPath(grid.WorldToCell(soldier.Position), grid.WorldToCell(edge), MovementClass.Infantry, _cells);
            if (_cells.Count < 2)
            {
                return;
            }

            PathUtils.Smooth(_pathfinder, grid, MovementClass.Infantry, _cells, soldier.Position, grid.CellCenter(_cells[_cells.Count - 1]), soldier.FleePath);
            PathUtils.Cumulate(soldier.FleePath, soldier.FleeCumulative);
        }

        /// <summary>The best nearby cover spot (5–13 m), weighted toward getting away from the nearest enemy he knows of.</summary>
        float2 FindCoverAwayFromEnemy(Soldier soldier)
        {
            var grid = _battle.Map.Grid;
            float2 away = AwayDirection(soldier);
            float2 best = soldier.Position;
            float bestScore = float.MinValue;

            for (int ring = 0; ring < 3; ring++)
            {
                float radius = 5f + ring * 4f;
                for (int k = 0; k < 12; k++)
                {
                    float angle = k / 12f * math.PI * 2f;
                    float2 direction = new float2(math.cos(angle), math.sin(angle));
                    float2 candidate = soldier.Position + direction * radius;
                    var cell = grid.WorldToCell(candidate);
                    if (!grid.InBounds(cell))
                    {
                        continue;
                    }

                    int index = grid.IndexOf(cell);
                    if (!_pathfinder.IsPassable(index, MovementClass.Infantry))
                    {
                        continue;
                    }

                    float score = grid.GetCover(index) + math.dot(direction, away) * 30f - radius * 0.5f;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = grid.CellCenter(cell);
                    }
                }
            }

            return best;
        }

        float2 AwayDirection(Soldier soldier)
        {
            Soldier nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (var enemy in _battle.Soldiers)
            {
                if (enemy.Team.Side == soldier.Team.Side || !enemy.IsActive || !_battle.Visibility.CanSee(soldier.Team.Side, enemy))
                {
                    continue;
                }

                float d = math.distance(enemy.Position, soldier.Position);
                if (d < nearestDistance)
                {
                    nearestDistance = d;
                    nearest = enemy;
                }
            }

            if (nearest != null)
            {
                return math.normalizesafe(soldier.Position - nearest.Position, new float2(0f, -1f));
            }

            // No enemy in sight: run toward home.
            return math.normalizesafe(_battle.EdgePoint(soldier.Team.Side, soldier.Position) - soldier.Position, new float2(0f, -1f));
        }

        // ------------------------------------------------------------ events

        /// <summary>A soldier was hit: he and the comrades around him lose nerve; losing the leader hurts most.</summary>
        public void OnCasualty(Soldier victim, SoldierStatus result, float now)
        {
            if (result == SoldierStatus.Wounded)
            {
                victim.Morale = math.max(0f, victim.Morale - WoundedSelfLoss * Nerve(victim));
            }

            bool leaderDown = victim.IsLeader && result != SoldierStatus.Wounded;
            foreach (var mate in victim.Team.Members)
            {
                if (mate == victim || !mate.IsActive)
                {
                    continue;
                }

                float loss = result == SoldierStatus.Wounded ? ComradeWoundedLoss : ComradeDownLoss;
                if (math.distance(mate.Position, victim.Position) > 30f)
                {
                    loss *= 0.5f;
                }

                if (leaderDown)
                {
                    loss += LeaderDownLoss;
                }

                mate.Morale = math.max(0f, mate.Morale - loss * Nerve(mate));
            }

            if (leaderDown && !victim.Team.LeaderLost)
            {
                victim.Team.LeaderLostAt = now;
            }
        }
    }
}
