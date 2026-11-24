using System;
using System.Collections.Generic;
using CloseFight.Core.Map;
using CloseFight.Core.Path;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.Battle
{
    /// <summary>Engineering work a team is doing: it walks to the site, then works there for a while.</summary>
    public sealed class EngineerTask
    {
        public OrderType Type;
        public float2 Target;
        public float2 Site;
        public float WorkLeft;
        public float WorkTotal;
        public MineField Field;
        public float Progress => WorkTotal <= 0f ? 1f : 1f - WorkLeft / WorkTotal;
    }

    /// <summary>
    /// A minefield: a few mines in a circle. Its owner always knows where it is;
    /// the other side finds out when a mine goes off or an engineer spots it.
    /// </summary>
    public sealed class MineField
    {
        public int Id { get; }
        public Side Owner { get; }
        public float2 Center { get; }
        public float Radius { get; }
        public int Mines { get; internal set; }
        readonly bool[] _knownTo = new bool[2];

        public MineField(int id, Side owner, float2 center, float radius, int mines)
        {
            Id = id;
            Owner = owner;
            Center = center;
            Radius = radius;
            Mines = mines;
            _knownTo[(int)owner] = true;
        }

        public bool KnownTo(Side side) => _knownTo[(int)side];
        internal void Reveal(Side side) => _knownTo[(int)side] = true;
        public bool Contains(float2 point) => math.distancesq(point, Center) <= Radius * Radius;
    }

    /// <summary>
    /// Engineers (M12, D38/D39): demolition charges that blow walls away (a new
    /// line of attack) or wreck a building with the men inside; minefields laid
    /// and cleared. Work takes time on the spot, and stops while the team is
    /// pinned. A planted charge has a short fuse, so the team pulls back first.
    /// Mines go off under enemy soldiers and vehicles that move across them; a
    /// side avoids the minefields it knows about when it plans a route.
    /// </summary>
    public sealed class Engineering
    {
        public const float DemolishSeconds = 12f;
        public const float LayMinesSeconds = 20f;
        public const float ClearMinesSeconds = 25f;
        public const float FuseSeconds = 6f;
        public const float WithdrawDistance = 15f;
        public const float MineFieldRadius = 6f;
        public const int MinesPerField = 5;
        /// <summary>How far the work site may be from the target (an engineer has to be next to the wall).</summary>
        public const float SiteReach = 3f;
        /// <summary>Chance per metre moved inside an enemy minefield that a soldier sets off a mine.</summary>
        public const float SoldierTriggerPerMeter = 0.06f;
        public const float VehicleTriggerPerMeter = 0.08f;
        /// <summary>Engineers notice a minefield within this distance.</summary>
        public const float DetectRange = 10f;
        /// <summary>Path cost per cell inside a minefield the moving side knows about.</summary>
        public const float KnownMineCost = 60f;
        /// <summary>A team this suppressed cannot work.</summary>
        const float WorkSuppression = 50f;

        public static readonly GunDefinition Charge = new GunDefinition("charge", "weapon.charge", 0f, 0f, 0f,
            accuracy: 1f, falloff: 1e6f, reloadSeconds: 0f, minRange: 0f, maxRange: 0f,
            heLethalRadius: 6f, heSuppressionRadius: 20f, structureDamage: 250f,
            startingAp: 0, startingHe: 0, startingSmoke: 0, traverseLimit: 0f, traverseRate: 0f, indirect: false, setupSeconds: 0f);

        static readonly GunDefinition MineBlast = new GunDefinition("mine", "weapon.mine", 0f, 0f, 0f,
            accuracy: 1f, falloff: 1e6f, reloadSeconds: 0f, minRange: 0f, maxRange: 0f,
            heLethalRadius: 2.5f, heSuppressionRadius: 10f, structureDamage: 0f,
            startingAp: 0, startingHe: 0, startingSmoke: 0, traverseLimit: 0f, traverseRate: 0f, indirect: false, setupSeconds: 0f);

        readonly struct PendingCharge
        {
            public readonly float2 Point;
            public readonly float At;
            public readonly Soldier By;

            public PendingCharge(float2 point, float at, Soldier by)
            {
                Point = point;
                At = at;
                By = by;
            }
        }

        readonly BattleState _battle;
        readonly TeamMovement _movement;
        readonly GridPathfinder _pathfinder;
        readonly List<MineField> _fields = new List<MineField>();
        readonly List<PendingCharge> _charges = new List<PendingCharge>();
        int _nextFieldId = 1;

        public IReadOnlyList<MineField> MineFields => _fields;

        internal Engineering(BattleState battle, TeamMovement movement, GridPathfinder pathfinder)
        {
            _battle = battle;
            _movement = movement;
            _pathfinder = pathfinder;
        }

        /// <summary>Lays a minefield at once (scenario set-up and deployment).</summary>
        public MineField AddMineField(Side owner, float2 center, int mines = MinesPerField)
        {
            var field = new MineField(_nextFieldId++, owner, center, MineFieldRadius, mines);
            _fields.Add(field);
            return field;
        }

        public static bool IsEngineeringOrder(OrderType order) =>
            order == OrderType.Demolish || order == OrderType.LayMines || order == OrderType.ClearMines;

        /// <summary>Whether the team could take this engineering order at all (the order menu asks).</summary>
        public static bool CanAttempt(Team team, OrderType order)
        {
            if (team.Type != TeamType.Engineer || team.ActiveCount == 0)
            {
                return false;
            }

            switch (order)
            {
                case OrderType.Demolish: return team.DemolitionCharges > 0;
                case OrderType.LayMines: return team.MineLoads > 0;
                case OrderType.ClearMines: return true;
                default: return false;
            }
        }

        /// <summary>Starts an engineering order: picks the work site and walks there.</summary>
        internal bool Start(Team team, OrderType order, float2 target)
        {
            if (!CanAttempt(team, order))
            {
                return false;
            }

            var task = new EngineerTask { Type = order, Target = target };
            switch (order)
            {
                case OrderType.Demolish:
                    if (!IsDemolitionTarget(target))
                    {
                        return false;
                    }

                    task.Site = SiteNear(target, team.Centroid);
                    task.WorkTotal = DemolishSeconds;
                    break;

                case OrderType.LayMines:
                    task.Site = target;
                    task.WorkTotal = LayMinesSeconds;
                    break;

                case OrderType.ClearMines:
                    task.Field = NearestKnownField(team.Side, target, 15f);
                    if (task.Field == null)
                    {
                        return false;
                    }

                    // Work from the near edge inwards.
                    float2 toTeam = math.normalizesafe(team.Centroid - task.Field.Center, new float2(0f, -1f));
                    task.Site = task.Field.Center + toTeam * (task.Field.Radius + 1f);
                    task.WorkTotal = ClearMinesSeconds;
                    break;
            }

            task.WorkLeft = task.WorkTotal;
            var anchor = Anchor(team);
            if (math.distance(anchor, task.Site) > 2f && !_movement.PlanMove(team, task.Site, OrderType.Move))
            {
                return false;
            }

            if (math.distance(anchor, task.Site) <= 2f)
            {
                _movement.Halt(team, order, Posture.Kneeling);
            }

            // The order's point is where the team goes (movement steers by it); the work's target is kept in the task.
            team.Order = new TeamOrder(order, task.Site);
            team.FireTarget = null;
            team.Task = task;
            return true;
        }

        /// <summary>A wall, or a building cell, at the point (or within a metre of it).</summary>
        public bool IsDemolitionTarget(float2 point)
        {
            var grid = _battle.Map.Grid;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    var cell = grid.WorldToCell(point + new float2(dx, dy));
                    if (!grid.InBounds(cell))
                    {
                        continue;
                    }

                    int index = grid.IndexOf(cell);
                    if (grid.HasFlag(index, CellFlags.Wall)
                        || (grid.HasFlag(index, CellFlags.Building) && !grid.HasFlag(index, CellFlags.Collapsed)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>A walkable cell within reach of the target, on the side the team comes from.</summary>
        float2 SiteNear(float2 target, float2 from)
        {
            var grid = _battle.Map.Grid;
            float best = float.MaxValue;
            float2 site = target;
            for (float dy = -SiteReach; dy <= SiteReach; dy += 1f)
            {
                for (float dx = -SiteReach; dx <= SiteReach; dx += 1f)
                {
                    float2 p = target + new float2(dx, dy);
                    var cell = grid.WorldToCell(p);
                    if (!grid.InBounds(cell))
                    {
                        continue;
                    }

                    int index = grid.IndexOf(cell);
                    if (!_pathfinder.IsPassable(index, MovementClass.Infantry) || grid.HasFlag(index, CellFlags.Wall))
                    {
                        continue;
                    }

                    float score = math.distance(p, target) * 2f + math.distance(p, from) * 0.1f;
                    if (score < best)
                    {
                        best = score;
                        site = grid.CellCenter(cell);
                    }
                }
            }

            return site;
        }

        public MineField NearestKnownField(Side side, float2 point, float within)
        {
            MineField best = null;
            float bestDistance = within;
            foreach (var field in _fields)
            {
                float d = math.distance(field.Center, point) - field.Radius;
                if (field.KnownTo(side) && d <= bestDistance)
                {
                    bestDistance = d;
                    best = field;
                }
            }

            return best;
        }

        /// <summary>Path cost of the minefields <paramref name="side"/> knows about (null when none).</summary>
        public Func<int, float> KnownMineCostFor(Side side)
        {
            bool any = false;
            foreach (var field in _fields)
            {
                if (field.KnownTo(side))
                {
                    any = true;
                    break;
                }
            }

            if (!any)
            {
                return null;
            }

            var grid = _battle.Map.Grid;
            return index =>
            {
                float2 p = grid.CellCenter(grid.CoordOf(index));
                foreach (var field in _fields)
                {
                    if (field.KnownTo(side) && field.Contains(p))
                    {
                        return KnownMineCost;
                    }
                }

                return 0f;
            };
        }

        public void Step(float dt)
        {
            float now = _battle.Clock.ElapsedSeconds;
            StepTasks(dt, now);
            StepCharges(now);
            StepMines(now);
            if (_battle.Clock.TickCount % 10 == 0)
            {
                Detect();
            }
        }

        void StepTasks(float dt, float now)
        {
            foreach (var team in _battle.Teams)
            {
                var task = team.Task;
                if (task == null)
                {
                    continue;
                }

                if (team.ActiveCount == 0 || team.IsBroken || team.Order.Type != task.Type)
                {
                    team.Task = null; // cancelled by a new order, panic or losses
                    continue;
                }

                if (team.Activity != TeamActivity.Idle)
                {
                    continue; // still on the way
                }

                if (math.distance(Anchor(team), task.Site) > 6f)
                {
                    team.Task = null; // could not get there
                    _movement.Halt(team, OrderType.Defend, Posture.Kneeling);
                    continue;
                }

                if (team.AverageSuppression > WorkSuppression)
                {
                    continue; // heads down: no work gets done under this fire
                }

                task.WorkLeft -= dt;
                if (task.WorkLeft > 0f)
                {
                    continue;
                }

                Complete(team, task, now);
            }
        }

        void Complete(Team team, EngineerTask task, float now)
        {
            team.Task = null;
            switch (task.Type)
            {
                case OrderType.Demolish:
                {
                    team.DemolitionCharges--;
                    _charges.Add(new PendingCharge(task.Target, now + FuseSeconds, team.Leader));
                    // Fuse lit: get clear of the blast.
                    float2 away = math.normalizesafe(task.Site - task.Target, math.normalizesafe(_battle.EdgePoint(team.Side, task.Site) - task.Site));
                    if (!_movement.PlanMove(team, task.Site + away * WithdrawDistance, OrderType.MoveFast))
                    {
                        _movement.Halt(team, OrderType.Hide, Posture.Prone);
                    }

                    break;
                }

                case OrderType.LayMines:
                    team.MineLoads--;
                    AddMineField(team.Side, task.Target);
                    _movement.Halt(team, OrderType.Defend, Posture.Kneeling);
                    break;

                case OrderType.ClearMines:
                    _fields.Remove(task.Field);
                    _movement.Halt(team, OrderType.Defend, Posture.Kneeling);
                    break;
            }
        }

        void StepCharges(float now)
        {
            for (int i = _charges.Count - 1; i >= 0; i--)
            {
                var charge = _charges[i];
                if (now < charge.At)
                {
                    continue;
                }

                _charges.RemoveAt(i);
                _battle.Gunnery.Blast(charge.Point, Charge, now, charge.By);
                var grid = _battle.Map.Grid;
                for (float dy = -2.5f; dy <= 2.5f; dy += 1f)
                {
                    for (float dx = -2.5f; dx <= 2.5f; dx += 1f)
                    {
                        if (dx * dx + dy * dy > 6.25f)
                        {
                            continue;
                        }

                        var cell = grid.WorldToCell(charge.Point + new float2(dx, dy));
                        if (grid.InBounds(cell))
                        {
                            _battle.Map.DemolishWall(grid.IndexOf(cell));
                        }
                    }
                }
            }
        }

        void StepMines(float now)
        {
            if (_fields.Count == 0)
            {
                return;
            }

            foreach (var soldier in _battle.Soldiers)
            {
                if (!soldier.IsActive || soldier.IsMounted)
                {
                    continue;
                }

                float moved = math.distance(soldier.Position, soldier.PreviousPosition);
                if (moved < 1e-3f)
                {
                    continue;
                }

                for (int i = _fields.Count - 1; i >= 0; i--)
                {
                    var field = _fields[i];
                    if (field.Owner == soldier.Team.Side || !field.Contains(soldier.Position))
                    {
                        continue;
                    }

                    // Crawling men and engineers who know the field step far more carefully.
                    float chance = SoldierTriggerPerMeter * moved;
                    if (soldier.Team.Order.Type == OrderType.Crawl) chance *= 0.4f;
                    if (soldier.Team.Type == TeamType.Engineer && field.KnownTo(soldier.Team.Side)) chance *= 0.3f;
                    if (_battle.Random.Chance(chance))
                    {
                        Detonate(field, soldier.Position, soldier.Team.Side, now);
                        _battle.Combat.InflictHit(soldier, 0.85f, now, null, HitCause.Blast);
                        break;
                    }
                }
            }

            foreach (var vehicle in _battle.Vehicles)
            {
                if (!vehicle.IsOperational || vehicle.Immobilized)
                {
                    continue;
                }

                float moved = math.distance(vehicle.Position, vehicle.PreviousPosition);
                if (moved < 1e-3f)
                {
                    continue;
                }

                for (int i = _fields.Count - 1; i >= 0; i--)
                {
                    var field = _fields[i];
                    if (field.Owner == vehicle.Team.Side || !field.Contains(vehicle.Position))
                    {
                        continue;
                    }

                    if (_battle.Random.Chance(VehicleTriggerPerMeter * moved))
                    {
                        Detonate(field, vehicle.Position, vehicle.Team.Side, now);
                        vehicle.Immobilized = true; // a track blown off
                        foreach (var crew in vehicle.Team.Members)
                        {
                            if (crew.IsActive)
                            {
                                crew.Suppression = math.min(100f, crew.Suppression + 30f);
                                crew.LastUnderFireAt = now;
                            }
                        }

                        break;
                    }
                }
            }
        }

        void Detonate(MineField field, float2 point, Side victimSide, float now)
        {
            field.Mines--;
            field.Reveal(victimSide);
            _battle.Gunnery.Blast(point, MineBlast, now);
            if (field.Mines <= 0)
            {
                _fields.Remove(field);
            }
        }

        /// <summary>Engineers notice minefields close to them.</summary>
        void Detect()
        {
            foreach (var field in _fields)
            {
                foreach (var soldier in _battle.Soldiers)
                {
                    var side = soldier.Team.Side;
                    if (soldier.Team.Type == TeamType.Engineer && soldier.IsActive && !field.KnownTo(side)
                        && math.distance(soldier.Position, field.Center) < field.Radius + DetectRange)
                    {
                        field.Reveal(side);
                    }
                }
            }
        }

        static float2 Anchor(Team team)
        {
            var leader = team.Leader;
            return leader != null ? leader.Position : team.Centroid;
        }
    }
}
