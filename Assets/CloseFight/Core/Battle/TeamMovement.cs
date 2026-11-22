using System.Collections.Generic;
using CloseFight.Core.Map;
using CloseFight.Core.Path;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.Battle
{
    /// <summary>
    /// Moves teams. The leader walks a smoothed A* route; the others follow in
    /// file behind him along the same route (so nobody walks through walls), and
    /// at the destination the team spreads into a line facing the direction of
    /// travel. Cover-seeking deployment comes with the AI work (M5/M7).
    /// </summary>
    public sealed class TeamMovement
    {
        // Speeds in m/s on cost-1 terrain (roads).
        public const float WalkSpeed = 1.4f;
        public const float RunSpeed = 3.2f;
        public const float CrawlSpeed = 0.35f;

        /// <summary>Distance between soldiers walking in file.</summary>
        public const float FileSpacing = 2.0f;
        /// <summary>Distance between soldiers in the deployed line.</summary>
        public const float LineSpacing = 2.5f;
        /// <summary>Followers may go this much faster than the team to catch up.</summary>
        const float CatchUpFactor = 1.3f;
        /// <summary>The leader waits when a follower lags further than this behind his place.</summary>
        const float CohesionDistance = 8f;
        const float ArriveDistance = 0.15f;

        readonly TacticalMap _map;
        readonly GridPathfinder _pathfinder;
        readonly List<GridCoord> _cells = new List<GridCoord>();

        public TeamMovement(TacticalMap map, GridPathfinder pathfinder)
        {
            _map = map;
            _pathfinder = pathfinder;
        }

        public static float BaseSpeed(OrderType order)
        {
            switch (order)
            {
                case OrderType.MoveFast:
                case OrderType.AssaultBuilding:
                case OrderType.Retreat:
                    return RunSpeed;
                case OrderType.Crawl:
                    return CrawlSpeed;
                default:
                    return WalkSpeed;
            }
        }

        static Posture MovingPosture(OrderType order) => order == OrderType.Crawl ? Posture.Prone : Posture.Standing;
        static Posture ArrivedPosture(OrderType order) => order == OrderType.Crawl ? Posture.Prone : Posture.Kneeling;

        /// <summary>Plans a route for the team. Returns false if no step toward the target is possible.</summary>
        public bool PlanMove(Team team, float2 target, OrderType order)
        {
            var leader = team.Leader;
            if (leader == null)
            {
                return false;
            }

            var grid = _map.Grid;
            target = ClampToMap(target);
            var start = grid.WorldToCell(leader.Position);
            var goal = grid.WorldToCell(target);

            bool reached = _pathfinder.FindPath(start, goal, MovementClass.Infantry, _cells);
            if (_cells.Count == 0)
            {
                return false;
            }

            float2 end = reached ? target : grid.CellCenter(_cells[_cells.Count - 1]);
            PathUtils.Smooth(_pathfinder, grid, MovementClass.Infantry, _cells, leader.Position, end, team.Path);
            PathUtils.Cumulate(team.Path, team.PathCumulative);

            team.PathProgress = 0f;
            team.DeploySlots.Clear();
            team.Activity = TeamActivity.Moving;
            team.Order = new TeamOrder(order, end);

            var posture = MovingPosture(order);
            foreach (var member in team.Members)
            {
                if (member.IsActive)
                {
                    member.Posture = posture;
                }
            }

            return true;
        }

        /// <summary>Stops in place, for Defend / Ambush / Hide.</summary>
        public void Halt(Team team, OrderType order, Posture posture)
        {
            ClearPath(team);
            team.Activity = TeamActivity.Idle;
            team.Order = new TeamOrder(order, team.Centroid);
            foreach (var member in team.Members)
            {
                if (member.IsActive)
                {
                    member.Posture = posture;
                    member.IsMoving = false;
                }
            }
        }

        public void Step(Team team, float dt)
        {
            switch (team.Activity)
            {
                case TeamActivity.Moving:
                    StepMoving(team, dt);
                    break;
                case TeamActivity.Deploying:
                    StepDeploying(team, dt);
                    break;
            }
        }

        void StepMoving(Team team, float dt)
        {
            var leader = team.Leader;
            if (leader == null)
            {
                ClearPath(team);
                team.Activity = TeamActivity.Idle;
                return;
            }

            float speed = BaseSpeed(team.Order.Type);

            // Cohesion: the leader waits for stragglers.
            bool waiting = false;
            int slot = 1;
            foreach (var member in team.Members)
            {
                if (member == leader || !member.IsActive)
                {
                    continue;
                }

                float behind = math.max(0f, team.PathProgress - slot * FileSpacing);
                float2 place = PathUtils.PointAt(team.Path, team.PathCumulative, behind, out _);
                if (math.distance(member.Position, place) > CohesionDistance)
                {
                    waiting = true;
                }

                slot++;
            }

            if (!waiting)
            {
                float leaderSpeed = speed / TerrainCostAt(leader.Position);
                team.PathProgress = math.min(team.PathLength, team.PathProgress + leaderSpeed * dt);
            }

            float2 leaderTarget = PathUtils.PointAt(team.Path, team.PathCumulative, team.PathProgress, out float2 leaderDirection);
            MoveTowards(leader, leaderTarget, speed * CatchUpFactor, dt);
            if (!waiting)
            {
                leader.Heading = Soldier.HeadingOf(leaderDirection);
            }

            slot = 1;
            foreach (var member in team.Members)
            {
                if (member == leader || !member.IsActive)
                {
                    continue;
                }

                float behind = math.max(0f, team.PathProgress - slot * FileSpacing);
                float2 place = PathUtils.PointAt(team.Path, team.PathCumulative, behind, out _);
                MoveTowards(member, place, speed * CatchUpFactor, dt);
                slot++;
            }

            if (team.PathProgress >= team.PathLength - 1e-3f && math.distance(leader.Position, leaderTarget) < 0.5f)
            {
                BeginDeploy(team, leaderDirection);
            }
        }

        void BeginDeploy(Team team, float2 direction)
        {
            team.Facing = Soldier.HeadingOf(direction);
            ComputeLineSlots(team, team.Order.Target, direction);
            ClearPath(team);
            team.Activity = TeamActivity.Deploying;
        }

        void StepDeploying(Team team, float dt)
        {
            float speed = BaseSpeed(team.Order.Type);
            bool allArrived = true;
            int index = 0;
            foreach (var member in team.Members)
            {
                if (!member.IsActive)
                {
                    continue;
                }

                if (index < team.DeploySlots.Count)
                {
                    float2 slot = team.DeploySlots[index];
                    MoveTowards(member, slot, speed, dt);
                    if (math.distance(member.Position, slot) > ArriveDistance)
                    {
                        allArrived = false;
                    }
                }

                index++;
            }

            if (allArrived)
            {
                var posture = ArrivedPosture(team.Order.Type);
                foreach (var member in team.Members)
                {
                    if (member.IsActive)
                    {
                        member.IsMoving = false;
                        member.Posture = posture;
                        member.Heading = team.Facing;
                    }
                }

                team.DeploySlots.Clear();
                team.Activity = TeamActivity.Idle;
            }
        }

        /// <summary>
        /// Places soldiers in a line abreast across the direction of travel:
        /// leader in the middle, then alternating right and left. Blocked spots are
        /// replaced by the nearest free spot behind the line.
        /// </summary>
        public void ComputeLineSlots(Team team, float2 center, float2 direction)
        {
            team.DeploySlots.Clear();
            var grid = _map.Grid;
            direction = math.normalizesafe(direction, new float2(0f, 1f));
            float2 right = new float2(direction.y, -direction.x);
            var taken = new HashSet<int>();

            int count = team.ActiveCount;
            for (int i = 0; i < count; i++)
            {
                int k = (i + 1) / 2;
                float sideSign = i % 2 == 1 ? 1f : -1f;
                float2 wanted = center + right * (k * sideSign * LineSpacing);
                team.DeploySlots.Add(FindFreeSpot(grid, wanted, direction, taken));
            }
        }

        float2 FindFreeSpot(TacticalGrid grid, float2 wanted, float2 direction, HashSet<int> taken)
        {
            // Try the wanted spot, then step back and sideways in widening rings.
            for (int ring = 0; ring <= 6; ring++)
            {
                for (int k = -ring; k <= ring; k++)
                {
                    float2 candidate = wanted - direction * (ring * 1.0f) + new float2(direction.y, -direction.x) * (k * 1.0f);
                    if (IsFree(grid, candidate, taken, out int index))
                    {
                        taken.Add(index);
                        return grid.CellCenter(grid.CoordOf(index));
                    }
                }
            }

            return wanted;
        }

        bool IsFree(TacticalGrid grid, float2 point, HashSet<int> taken, out int index)
        {
            index = -1;
            var cell = grid.WorldToCell(point);
            if (!grid.InBounds(cell))
            {
                return false;
            }

            index = grid.IndexOf(cell);
            return !taken.Contains(index) && _pathfinder.IsPassable(index, MovementClass.Infantry);
        }

        /// <summary>Moves a soldier straight toward a point, sliding along walls instead of entering them.</summary>
        void MoveTowards(Soldier soldier, float2 target, float maxSpeed, float dt)
        {
            float2 delta = target - soldier.Position;
            float distance = math.length(delta);
            if (distance < 1e-4f)
            {
                soldier.IsMoving = false;
                return;
            }

            float speed = maxSpeed / TerrainCostAt(soldier.Position);
            float step = math.min(distance, speed * dt);
            float2 move = delta / distance * step;

            if (!TryMove(soldier, move) && !TryMove(soldier, new float2(move.x, 0f)) && !TryMove(soldier, new float2(0f, move.y)))
            {
                soldier.IsMoving = false;
                return;
            }

            soldier.IsMoving = true;
            soldier.Heading = Soldier.HeadingOf(move);
        }

        bool TryMove(Soldier soldier, float2 move)
        {
            if (math.lengthsq(move) < 1e-10f)
            {
                return false;
            }

            float2 next = soldier.Position + move;
            var grid = _map.Grid;
            var cell = grid.WorldToCell(next);
            if (!grid.InBounds(cell) || !_pathfinder.IsPassable(grid.IndexOf(cell), MovementClass.Infantry))
            {
                return false;
            }

            soldier.Position = next;
            return true;
        }

        float TerrainCostAt(float2 position)
        {
            var grid = _map.Grid;
            var cell = grid.WorldToCell(position);
            if (!grid.InBounds(cell))
            {
                return 1f;
            }

            float cost = _pathfinder.CellCost(grid.IndexOf(cell), MovementClass.Infantry);
            return float.IsPositiveInfinity(cost) ? 1f : math.max(cost, 0.1f);
        }

        float2 ClampToMap(float2 point)
        {
            var grid = _map.Grid;
            float maxX = grid.Width * grid.CellSize - 0.01f;
            float maxY = grid.Height * grid.CellSize - 0.01f;
            return new float2(math.clamp(point.x, 0f, maxX), math.clamp(point.y, 0f, maxY));
        }

        static void ClearPath(Team team)
        {
            team.Path.Clear();
            team.PathCumulative.Clear();
            team.PathProgress = 0f;
        }
    }
}
