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
                team.Members.Add(soldier);
                _soldiers.Add(soldier);
            }

            team.Facing = facing;
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

        /// <summary>Gives an order. Returns false when it cannot be carried out.</summary>
        public bool IssueOrder(Team team, OrderType order, float2 target)
        {
            if (team == null || team.ActiveCount == 0 || !IsImplemented(order))
            {
                return false;
            }

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

            Clock.CountTick();
            if (Clock.TickCount % Visibility.UpdateIntervalTicks == 0)
            {
                Visibility.Recompute();
            }

            Visibility.ExpireSoundMarkers();
            Ticked?.Invoke();
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
