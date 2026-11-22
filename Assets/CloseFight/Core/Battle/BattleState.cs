using System;
using System.Collections.Generic;
using CloseFight.Core.Map;
using CloseFight.Core.Path;
using CloseFight.Core.Sim;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.Battle
{
    /// <summary>
    /// The whole running battle: map, teams, soldiers, clock and random source.
    /// The presentation layer only reads it and sends orders through
    /// <see cref="IssueOrder"/>; it never changes simulation state directly.
    /// </summary>
    public sealed class BattleState
    {
        public TacticalMap Map { get; }
        public SimClock Clock { get; } = new SimClock();
        public SimRandom Random { get; }
        public IReadOnlyList<Team> Teams => _teams;
        public IReadOnlyList<Soldier> Soldiers => _soldiers;
        public Visibility Visibility { get; }
        public Combat Combat { get; }
        public Psychology Psychology { get; }
        public SmokeField Smoke { get; } = new SmokeField();

        readonly List<Team> _teams = new List<Team>();
        readonly List<Soldier> _soldiers = new List<Soldier>();
        readonly Dictionary<Side, MapEdge> _retreatEdges = new Dictionary<Side, MapEdge>();
        readonly GridPathfinder _pathfinder;
        readonly TeamMovement _movement;

        /// <summary>Raised after every simulation tick.</summary>
        public event Action Ticked;

        public BattleState(TacticalMap map, ulong seed)
        {
            Map = map;
            Random = new SimRandom(seed);
            _pathfinder = new GridPathfinder(map);
            _movement = new TeamMovement(map, _pathfinder);
            Visibility = new Visibility(this);
            Combat = new Combat(this);
            Psychology = new Psychology(this, _movement, _pathfinder);
            _retreatEdges[Side.German] = MapEdge.South;
            _retreatEdges[Side.Soviet] = MapEdge.North;
        }

        public void SetRetreatEdge(Side side, MapEdge edge) => _retreatEdges[side] = edge;
        public MapEdge RetreatEdgeOf(Side side) => _retreatEdges[side];

        public static int DefaultSize(TeamType type) => type == TeamType.Rifle ? 6 : 3;

        /// <summary>Creates a team with generated soldiers, deployed in a line around <paramref name="position"/>.</summary>
        public Team AddTeam(string key, Side side, TeamType type, string displayName, float2 position, float facing, int size, int experience)
        {
            var team = new Team(_teams.Count, key, side, type, displayName);
            size = math.max(1, size);

            for (int i = 0; i < size; i++)
            {
                var (first, last) = NameGenerator.Next(side, Random);
                var soldier = new Soldier(_soldiers.Count, first, last)
                {
                    Team = team,
                    IsLeader = i == 0,
                    RankKey = RankKey(side, type, i),
                    Attributes = RollAttributes(experience, i == 0),
                };
                soldier.Morale = soldier.BaseMorale = soldier.Attributes.Morale;
                soldier.Weapon = Weapons.Issue(side, type, i);
                soldier.Ammo = soldier.Weapon.StartingAmmo;
                team.Members.Add(soldier);
                _soldiers.Add(soldier);
            }

            team.Facing = facing;
            team.SmokeGrenades = type == TeamType.Rifle ? 2 : 1;
            _movement.ComputeLineSlots(team, position, Soldier.DirectionOf(facing));
            for (int i = 0; i < team.Members.Count; i++)
            {
                var soldier = team.Members[i];
                soldier.SetPosition(i < team.DeploySlots.Count ? team.DeploySlots[i] : position);
                soldier.SetHeading(facing);
                soldier.Posture = Posture.Kneeling;
            }

            team.DeploySlots.Clear();
            _teams.Add(team);
            return team;
        }

        /// <summary>True for the orders that work in the current milestone.</summary>
        public static bool IsImplemented(OrderType order)
        {
            switch (order)
            {
                case OrderType.Fire:
                case OrderType.AreaFire:
                case OrderType.Smoke:
                case OrderType.Move:
                case OrderType.MoveFast:
                case OrderType.Crawl:
                case OrderType.Defend:
                case OrderType.Ambush:
                case OrderType.Hide:
                case OrderType.AssaultBuilding:
                case OrderType.Retreat:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>True if the order needs a target point chosen on the map.</summary>
        public static bool NeedsTarget(OrderType order)
        {
            switch (order)
            {
                case OrderType.Defend:
                case OrderType.Ambush:
                case OrderType.Hide:
                case OrderType.Retreat:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>Gives an order. Returns false when it is refused or cannot be carried out.</summary>
        public bool IssueOrder(Team team, OrderType order, float2 target) => TryIssueOrder(team, order, target, out _);

        /// <summary>
        /// Gives an order and says why it was not carried out (D29: the player
        /// must see the reason). Shaken teams may refuse to advance; broken
        /// teams only accept Retreat or Hide.
        /// </summary>
        public bool TryIssueOrder(Team team, OrderType order, float2 target, out OrderRefusal refusal)
        {
            refusal = OrderRefusal.None;
            if (team == null || team.ActiveCount == 0 || !IsImplemented(order))
            {
                refusal = OrderRefusal.Impossible;
                return false;
            }

            if (team.IsBroken && order != OrderType.Retreat && order != OrderType.Hide)
            {
                return Refuse(team, OrderRefusal.Broken, out refusal);
            }

            if (team.IsShaken && IsAdvance(team, order, target))
            {
                var leader = team.Leader;
                float chance = 0.7f - (leader != null ? leader.Attributes.Leadership : 0) / 200f;
                if (Random.Chance(chance))
                {
                    return Refuse(team, OrderRefusal.Shaken, out refusal);
                }
            }

            team.FallingBack = false;
            if (!ExecuteOrder(team, order, target))
            {
                refusal = OrderRefusal.Impossible;
                return false;
            }

            return true;
        }

        bool Refuse(Team team, OrderRefusal reason, out OrderRefusal refusal)
        {
            refusal = reason;
            team.LastRefusal = reason;
            team.LastRefusalAt = Clock.ElapsedSeconds;
            return false;
        }

        /// <summary>True for a move that takes the team closer to the nearest enemy it can see.</summary>
        bool IsAdvance(Team team, OrderType order, float2 target)
        {
            if (order != OrderType.Move && order != OrderType.MoveFast && order != OrderType.Crawl && order != OrderType.AssaultBuilding)
            {
                return false;
            }

            float2 from = team.Centroid;
            float best = float.MaxValue;
            float2 enemyAt = float2.zero;
            foreach (var soldier in _soldiers)
            {
                if (soldier.Team.Side == team.Side || !soldier.IsActive || !Visibility.CanSee(team.Side, soldier))
                {
                    continue;
                }

                float d = math.distance(soldier.Position, from);
                if (d < best)
                {
                    best = d;
                    enemyAt = soldier.Position;
                }
            }

            return best < float.MaxValue && math.distance(target, enemyAt) < best - 5f;
        }

        bool ExecuteOrder(Team team, OrderType order, float2 target)
        {

            float now = Clock.ElapsedSeconds;
            switch (order)
            {
                case OrderType.Fire:
                {
                    var enemy = FindVisibleEnemyNear(team.Side, target, 4f);
                    if (enemy == null)
                    {
                        return false;
                    }

                    _movement.Halt(team, order, Posture.Kneeling);
                    team.FireTarget = enemy.Team;
                    return true;
                }

                case OrderType.AreaFire:
                {
                    var leader = team.Leader;
                    var sight = LineOfSight.Check(Map, leader.Position, leader.Posture, target, Posture.Standing, 1000f);
                    if (!sight.Visible || Smoke.Blocks(leader.Position, target, now))
                    {
                        return false;
                    }

                    _movement.Halt(team, order, Posture.Kneeling);
                    team.FireTarget = null;
                    team.Order = new TeamOrder(OrderType.AreaFire, target);
                    return true;
                }

                case OrderType.Smoke:
                {
                    // A thrown smoke grenade: needs one left and a target within throwing range.
                    var leader = team.Leader;
                    if (team.SmokeGrenades <= 0 || math.distance(leader.Position, target) > MaxSmokeThrow)
                    {
                        return false;
                    }

                    Smoke.Add(target, now);
                    team.SmokeGrenades--;
                    return true;
                }
            }

            // Any other order cancels a standing fire order.
            team.FireTarget = null;

            switch (order)
            {
                case OrderType.Move:
                case OrderType.MoveFast:
                case OrderType.Crawl:
                    return _movement.PlanMove(team, target, order);

                case OrderType.AssaultBuilding:
                {
                    // Only meaningful with a building as the target.
                    var cell = Map.Grid.WorldToCell(target);
                    if (!Map.Grid.InBounds(cell) || Map.BuildingAt(Map.Grid.IndexOf(cell)) == null)
                    {
                        return false;
                    }

                    return _movement.PlanMove(team, target, order);
                }

                case OrderType.Retreat:
                    return _movement.PlanMove(team, RetreatPoint(team), order);

                case OrderType.Defend:
                    _movement.Halt(team, order, Posture.Kneeling);
                    return true;

                case OrderType.Ambush:
                case OrderType.Hide:
                    _movement.Halt(team, order, Posture.Prone);
                    return true;
            }

            return false;
        }

        public const float MaxSmokeThrow = 35f;

        /// <summary>The nearest active enemy soldier that <paramref name="side"/> can see, within <paramref name="radius"/> of a point.</summary>
        public Soldier FindVisibleEnemyNear(Side side, float2 point, float radius)
        {
            Soldier best = null;
            float bestDistance = radius;
            foreach (var soldier in _soldiers)
            {
                if (soldier.Team.Side == side || !soldier.IsActive || !Visibility.CanSee(side, soldier))
                {
                    continue;
                }

                float d = math.distance(soldier.Position, point);
                if (d <= bestDistance)
                {
                    best = soldier;
                    bestDistance = d;
                }
            }

            return best;
        }

        /// <summary>Feeds real time to the clock and runs the ticks that are due.</summary>
        public void Update(float realDeltaSeconds)
        {
            int ticks = Clock.Advance(realDeltaSeconds);
            for (int i = 0; i < ticks; i++)
            {
                Step();
            }
        }

        /// <summary>Runs exactly one simulation tick.</summary>
        public void Step()
        {
            foreach (var soldier in _soldiers)
            {
                soldier.PreviousPosition = soldier.Position;
                soldier.PreviousHeading = soldier.Heading;
            }

            foreach (var team in _teams)
            {
                _movement.Step(team, SimClock.TickSeconds);
            }

            Psychology.Step(SimClock.TickSeconds);
            Combat.Step(SimClock.TickSeconds);
            Clock.CountTick();
            Smoke.Expire(Clock.ElapsedSeconds);
            if (Clock.TickCount % Visibility.UpdateIntervalTicks == 0)
            {
                Visibility.Recompute();
            }

            Visibility.ExpireSoundMarkers();
            Ticked?.Invoke();
        }

        /// <summary>The point on <paramref name="side"/>'s own map edge nearest to <paramref name="from"/>.</summary>
        public float2 EdgePoint(Side side, float2 from)
        {
            var grid = Map.Grid;
            float width = grid.Width * grid.CellSize;
            float height = grid.Height * grid.CellSize;
            switch (_retreatEdges[side])
            {
                case MapEdge.North: return new float2(from.x, height - 0.5f);
                case MapEdge.West: return new float2(0.5f, from.y);
                case MapEdge.East: return new float2(width - 0.5f, from.y);
                default: return new float2(from.x, 0.5f);
            }
        }

        /// <summary>A spot <paramref name="distance"/> meters back toward the team's own edge (D14 "new line").</summary>
        public float2 FallbackPoint(Team team, float distance)
        {
            float2 from = team.Centroid;
            float2 edge = EdgePoint(team.Side, from);
            float2 toward = math.normalizesafe(edge - from, new float2(0f, -1f));
            float available = math.distance(edge, from);
            return from + toward * math.min(distance, math.max(0f, available - 2f));
        }

        float2 RetreatPoint(Team team)
        {
            var grid = Map.Grid;
            float width = grid.Width * grid.CellSize;
            float height = grid.Height * grid.CellSize;
            float2 c = team.Centroid;
            switch (_retreatEdges[team.Side])
            {
                case MapEdge.North: return new float2(c.x, height - 1f);
                case MapEdge.West: return new float2(1f, c.y);
                case MapEdge.East: return new float2(width - 1f, c.y);
                default: return new float2(c.x, 1f);
            }
        }

        SoldierAttributes RollAttributes(int experience, bool leader)
        {
            byte Roll(int center, int spread) => (byte)math.clamp(center + Random.Range(-spread, spread + 1), 5, 100);
            return new SoldierAttributes
            {
                Experience = Roll(experience + (leader ? 10 : 0), 12),
                Morale = Roll(60, 15),
                Stamina = Roll(70, 15),
                Marksmanship = Roll(experience, 15),
                Leadership = leader ? Roll(experience, 15) : (byte)0,
            };
        }

        static string RankKey(Side side, TeamType type, int index)
        {
            string prefix = side == Side.German ? "rank.de." : "rank.su.";
            if (index == 0)
            {
                return prefix + (type == TeamType.Headquarters ? "lieutenant" : "squad_leader");
            }

            return prefix + (index == 1 ? "corporal" : "private");
        }
    }
}
