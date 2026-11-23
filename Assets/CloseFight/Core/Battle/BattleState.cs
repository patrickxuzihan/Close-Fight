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
        public IReadOnlyList<Vehicle> Vehicles => _vehicles;
        public IReadOnlyList<Objective> Objectives => _objectives;
        /// <summary>AI commanders run every <see cref="AiIntervalTicks"/> ticks (the AI does not think every frame).</summary>
        public const int AiIntervalTicks = 20;
        public Gunnery Gunnery { get; }
        public VictoryConditions Victory { get; }
        public Visibility Visibility { get; }
        public Combat Combat { get; }
        public Psychology Psychology { get; }
        public SmokeField Smoke { get; } = new SmokeField();

        readonly List<Team> _teams = new List<Team>();
        readonly List<Soldier> _soldiers = new List<Soldier>();
        readonly List<Vehicle> _vehicles = new List<Vehicle>();
        readonly List<Objective> _objectives = new List<Objective>();
        readonly List<IBattleCommander> _commanders = new List<IBattleCommander>();
        VehicleMovement _vehicleMovement;
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
            _vehicleMovement = new VehicleMovement(map, _pathfinder);
            Gunnery = new Gunnery(this);
            Victory = new VictoryConditions(this);
            _retreatEdges[Side.German] = MapEdge.South;
            _retreatEdges[Side.Soviet] = MapEdge.North;
        }

        public void SetRetreatEdge(Side side, MapEdge edge) => _retreatEdges[side] = edge;

        public Objective AddObjective(ObjectiveDefinition definition)
        {
            var objective = new Objective(definition);
            _objectives.Add(objective);
            return objective;
        }

        /// <summary>Hands a side to a computer commander (the AI). Several sides may have one.</summary>
        public void AddCommander(IBattleCommander commander) => _commanders.Add(commander);
        public IReadOnlyList<IBattleCommander> Commanders => _commanders;
        public MapEdge RetreatEdgeOf(Side side) => _retreatEdges[side];

        public static int DefaultSize(TeamType type) => type == TeamType.Rifle ? 6 : 3;

        /// <summary>Creates a team with generated soldiers, deployed in a line around <paramref name="position"/>.</summary>
        public Team AddTeam(string key, Side side, TeamType type, string displayName, float2 position, float facing, int size, int experience,
            string equipment = null)
        {
            var team = new Team(_teams.Count, key, side, type, displayName);
            VehicleDefinition vehicleDefinition = null;
            GunTeamDefinition gunTeam = null;
            if (type == TeamType.Vehicle)
            {
                vehicleDefinition = Equipment.Vehicle(equipment);
                size = vehicleDefinition.Crew.Length;
            }
            else if (type == TeamType.AntiTankGun || type == TeamType.Mortar)
            {
                gunTeam = Equipment.GunTeam(equipment);
                size = gunTeam.CrewSize;
            }

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
                if (vehicleDefinition != null)
                {
                    // Inside the tank only the gunner shoots, with the coaxial MG.
                    soldier.Role = vehicleDefinition.Crew[i];
                    soldier.Weapon = soldier.Role == CrewRole.Gunner ? vehicleDefinition.CoaxMg : null;
                }
                else if (gunTeam != null)
                {
                    soldier.Role = i == 0 ? CrewRole.Commander : i == 1 ? CrewRole.Gunner : i == 2 ? CrewRole.Loader : CrewRole.None;
                    soldier.Weapon = soldier.Role == CrewRole.Gunner ? null : Weapons.Issue(side, TeamType.Headquarters, i);
                }
                else
                {
                    soldier.Weapon = Weapons.Issue(side, type, i);
                }

                soldier.Ammo = soldier.Weapon != null ? soldier.Weapon.StartingAmmo : 0;
                team.Members.Add(soldier);
                _soldiers.Add(soldier);
            }

            team.Facing = facing;
            team.SmokeGrenades = type == TeamType.Rifle ? 2 : type == TeamType.Vehicle ? 0 : 1;
            team.AntiTankGrenades = type == TeamType.Rifle ? 1 : 0;

            if (vehicleDefinition != null)
            {
                var vehicle = new Vehicle(_vehicles.Count, vehicleDefinition, team);
                vehicle.SetPose(position, facing);
                vehicle.Gun = new HeavyWeapon(vehicleDefinition.Gun, team, vehicle) { Heading = facing, PreviousHeading = facing };
                team.Vehicle = vehicle;
                team.Gun = vehicle.Gun;
                foreach (var crewman in team.Members)
                {
                    crewman.MountedIn = vehicle;
                    crewman.SetPosition(position);
                    crewman.SetHeading(facing);
                    crewman.Posture = Posture.Standing;
                }

                _vehicles.Add(vehicle);
                _teams.Add(team);
                return team;
            }

            _movement.ComputeLineSlots(team, position, Soldier.DirectionOf(facing));
            for (int i = 0; i < team.Members.Count; i++)
            {
                var soldier = team.Members[i];
                soldier.SetPosition(i < team.DeploySlots.Count ? team.DeploySlots[i] : position);
                soldier.SetHeading(facing);
                soldier.Posture = Posture.Kneeling;
            }

            team.DeploySlots.Clear();
            if (gunTeam != null)
            {
                team.GunTeam = gunTeam;
                team.Gun = new HeavyWeapon(gunTeam.Gun, team, null)
                {
                    Heading = facing,
                    PreviousHeading = facing,
                    Emplacement = team.Members.Count > 1 ? team.Members[1].Position : position,
                };
            }

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
                case OrderType.Reverse:
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

        /// <summary>
        /// Gives an order whose route is planned with an extra cost per cell, so
        /// the team avoids ground the enemy covers (used by the AI with its danger map).
        /// </summary>
        public bool TryIssueOrderAvoiding(Team team, OrderType order, float2 target, Func<int, float> pathCost, out OrderRefusal refusal)
        {
            _pathfinder.ExtraCost = pathCost;
            try
            {
                return TryIssueOrder(team, order, target, out refusal);
            }
            finally
            {
                _pathfinder.ExtraCost = null;
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
            if (team.IsMechanized)
            {
                return ExecuteVehicleOrder(team, order, target);
            }

            if (order == OrderType.Reverse)
            {
                return false; // only vehicles back up
            }

            // Mortars fire smoke rounds instead of throwing grenades, at any range they can reach.
            if (order == OrderType.Smoke && team.Gun != null && team.Gun.Definition.Indirect)
            {
                float distance = math.distance(team.Gun.Position, target);
                if (team.Gun.AmmoSmoke <= 0 || distance < team.Gun.Definition.MinRange || distance > team.Gun.Definition.MaxRange)
                {
                    return false;
                }

                team.Gun.SmokePending = true;
                team.Gun.SmokeTarget = target;
                return true;
            }

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

                case OrderType.AreaFire when team.Gun != null && team.Gun.Definition.Indirect:
                {
                    // Indirect fire needs no line of sight from the mortar, only range.
                    float distance = math.distance(team.Gun.Position, target);
                    if (distance < team.Gun.Definition.MinRange || distance > team.Gun.Definition.MaxRange)
                    {
                        return false;
                    }

                    _movement.Halt(team, order, Posture.Kneeling);
                    team.FireTarget = null;
                    team.Order = new TeamOrder(OrderType.AreaFire, target);
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

        /// <summary>Orders for a team fighting as a vehicle (D31 vehicle command set).</summary>
        bool ExecuteVehicleOrder(Team team, OrderType order, float2 target)
        {
            var vehicle = team.Vehicle;
            switch (order)
            {
                case OrderType.Move:
                case OrderType.MoveFast:
                case OrderType.Reverse:
                    team.FireTarget = null;
                    return _vehicleMovement.PlanMove(team, target, order);

                case OrderType.Retreat:
                    team.FireTarget = null;
                    return _vehicleMovement.PlanMove(team, RetreatPoint(team), OrderType.Retreat);

                case OrderType.Defend:
                case OrderType.Ambush:
                case OrderType.Hide:
                    team.FireTarget = null;
                    _vehicleMovement.Halt(team, order);
                    return true;

                case OrderType.Fire:
                {
                    var enemy = FindVisibleEnemyNear(team.Side, target, 5f);
                    if (enemy == null)
                    {
                        return false;
                    }

                    _vehicleMovement.Halt(team, order);
                    team.FireTarget = enemy.Team;
                    return true;
                }

                case OrderType.AreaFire:
                {
                    var sight = LineOfSight.Check(Map, vehicle.Position, Posture.Standing, target, Posture.Standing, 1500f);
                    if (!sight.Visible || Smoke.Blocks(vehicle.Position, target, Clock.ElapsedSeconds))
                    {
                        return false;
                    }

                    _vehicleMovement.Halt(team, order);
                    team.FireTarget = null;
                    team.Order = new TeamOrder(OrderType.AreaFire, target);
                    return true;
                }

                default:
                    return false; // vehicles do not crawl, throw smoke or storm buildings
            }
        }

        /// <summary>Like <see cref="MoveTeam(Team, float2, OrderType)"/>, with a danger cost on the route.</summary>
        internal bool MoveTeam(Team team, float2 target, OrderType order, Func<int, float> pathCost)
        {
            _pathfinder.ExtraCost = pathCost;
            try
            {
                return MoveTeam(team, target, order);
            }
            finally
            {
                _pathfinder.ExtraCost = null;
            }
        }

        /// <summary>Moves a team the right way for what it is (vehicle or on foot). Used by Psychology for fallback.</summary>
        internal bool MoveTeam(Team team, float2 target, OrderType order)
        {
            return team.IsMechanized
                ? _vehicleMovement.PlanMove(team, target, order == OrderType.Retreat ? OrderType.Reverse : order)
                : _movement.PlanMove(team, target, order);
        }

        internal void HaltTeam(Team team, OrderType order, Posture posture)
        {
            if (team.IsMechanized)
            {
                _vehicleMovement.Halt(team, order);
            }
            else
            {
                _movement.Halt(team, order, posture);
            }
        }

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

        /// <summary>Runs exactly one simulation tick. Does nothing once the battle is over.</summary>
        public void Step()
        {
            if (Victory.IsOver)
            {
                return;
            }

            foreach (var soldier in _soldiers)
            {
                soldier.PreviousPosition = soldier.Position;
                soldier.PreviousHeading = soldier.Heading;
            }

            foreach (var vehicle in _vehicles)
            {
                _vehicleMovement.Step(vehicle, SimClock.TickSeconds);
            }

            foreach (var team in _teams)
            {
                if (!team.IsMechanized)
                {
                    _movement.Step(team, SimClock.TickSeconds);
                }
            }

            Psychology.Step(SimClock.TickSeconds);
            Combat.Step(SimClock.TickSeconds);
            Gunnery.Step(SimClock.TickSeconds);

            if (Clock.TickCount % AiIntervalTicks == 0)
            {
                foreach (var commander in _commanders)
                {
                    commander.Think();
                }
            }

            Victory.Step();
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
