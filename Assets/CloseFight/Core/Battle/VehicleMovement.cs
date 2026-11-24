using System.Collections.Generic;
using CloseFight.Core.Map;
using CloseFight.Core.Path;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.Battle
{
    /// <summary>
    /// Drives tanks and assault guns (M6). A vehicle follows a route planned
    /// with vehicle clearance, turns its hull at its turn rate (pivoting in
    /// place for sharp turns), and flattens fences and wooden sheds it drives
    /// over (D37). Reverse backs away with the front armor still facing forward.
    /// </summary>
    public sealed class VehicleMovement
    {
        /// <summary>A cautious Move runs at this fraction of full speed; Move Fast at full speed.</summary>
        const float CautiousFactor = 0.55f;
        const float ReverseSpeed = 2.0f;
        /// <summary>Extra route cost per side (its known minefields), set by the battle.</summary>
        internal System.Func<Side, System.Func<int, float>> SideCost { get; set; }

        /// <summary>Drive forward only when the hull points within this many degrees of the route.</summary>
        const float DriveAlignment = 30f;
        const float LookAhead = 3f;

        readonly TacticalMap _map;
        readonly GridPathfinder _pathfinder;
        readonly List<GridCoord> _cells = new List<GridCoord>();

        public VehicleMovement(TacticalMap map, GridPathfinder pathfinder)
        {
            _map = map;
            _pathfinder = pathfinder;
        }

        public bool PlanMove(Team team, float2 target, OrderType order)
        {
            var vehicle = team.Vehicle;
            if (vehicle == null || !vehicle.IsOperational || vehicle.Immobilized || DriverOf(vehicle) == null)
            {
                return false;
            }

            var grid = _map.Grid;
            target = new float2(
                math.clamp(target.x, 0f, grid.Width * grid.CellSize - 0.01f),
                math.clamp(target.y, 0f, grid.Height * grid.CellSize - 0.01f));
            _pathfinder.SideCost = SideCost?.Invoke(team.Side); // e.g. minefields this side knows about
            bool reached = _pathfinder.FindPath(grid.WorldToCell(vehicle.Position), grid.WorldToCell(target), MovementClass.Vehicle, _cells);
            _pathfinder.SideCost = null;
            if (_cells.Count < 2)
            {
                return false;
            }

            float2 end = reached ? target : grid.CellCenter(_cells[_cells.Count - 1]);
            PathUtils.Smooth(_pathfinder, grid, MovementClass.Vehicle, _cells, vehicle.Position, end, vehicle.Path);
            PathUtils.Cumulate(vehicle.Path, vehicle.PathCumulative);
            vehicle.PathProgress = 0f;
            vehicle.Reversing = order == OrderType.Reverse;
            team.Activity = TeamActivity.Moving;
            team.Order = new TeamOrder(order, end);
            return true;
        }

        public void Halt(Team team, OrderType order)
        {
            var vehicle = team.Vehicle;
            if (vehicle != null)
            {
                Stop(vehicle);
            }

            team.Activity = TeamActivity.Idle;
            team.Order = new TeamOrder(order, vehicle != null ? vehicle.Position : team.Centroid);
        }

        public void Step(Vehicle vehicle, float dt)
        {
            var team = vehicle.Team;
            vehicle.PreviousPosition = vehicle.Position;
            vehicle.PreviousHullHeading = vehicle.HullHeading;

            if (team.Activity != TeamActivity.Moving)
            {
                vehicle.IsMoving = false;
                return;
            }

            var driver = DriverOf(vehicle);
            if (!vehicle.IsOperational || vehicle.Immobilized || driver == null || vehicle.Path.Count < 2)
            {
                Stop(vehicle);
                team.Activity = TeamActivity.Idle;
                return;
            }

            float length = vehicle.PathCumulative[vehicle.PathCumulative.Count - 1];
            float2 aim = PathUtils.PointAt(vehicle.Path, vehicle.PathCumulative, math.min(length, vehicle.PathProgress + LookAhead), out _);
            float2 toAim = aim - vehicle.Position;

            if (math.lengthsq(toAim) > 0.01f)
            {
                float travelHeading = Soldier.HeadingOf(toAim);
                float wantedHull = vehicle.Reversing ? Wrap(travelHeading + 180f) : travelHeading;
                float delta = DeltaAngle(vehicle.HullHeading, wantedHull);
                float maxTurn = vehicle.Definition.TurnRate * dt;
                vehicle.HullHeading = Wrap(vehicle.HullHeading + math.clamp(delta, -maxTurn, maxTurn));

                if (math.abs(DeltaAngle(vehicle.HullHeading, wantedHull)) > DriveAlignment)
                {
                    vehicle.IsMoving = true; // pivoting in place
                    SyncCrew(vehicle);
                    return;
                }
            }

            float speed = vehicle.Reversing ? ReverseSpeed : SpeedOn(vehicle, team.Order.Type);
            // A substitute at the controls drives badly.
            if (driver.Role != CrewRole.Driver)
            {
                speed *= 0.5f;
            }

            vehicle.PathProgress = math.min(length, vehicle.PathProgress + speed * dt);
            vehicle.Position = PathUtils.PointAt(vehicle.Path, vehicle.PathCumulative, vehicle.PathProgress, out _);
            vehicle.IsMoving = true;
            CrushUnder(vehicle);
            SyncCrew(vehicle);

            if (vehicle.PathProgress >= length - 1e-3f)
            {
                Stop(vehicle);
                team.Activity = TeamActivity.Idle;
            }
        }

        float SpeedOn(Vehicle vehicle, OrderType order)
        {
            var grid = _map.Grid;
            var cell = grid.WorldToCell(vehicle.Position);
            float baseSpeed = vehicle.Definition.CrossSpeed;
            float cost = 1f;
            if (grid.InBounds(cell))
            {
                int index = grid.IndexOf(cell);
                if (grid.GetTerrain(index) == TerrainType.Road)
                {
                    baseSpeed = vehicle.Definition.RoadSpeed;
                }

                float c = _pathfinder.CellCost(index, MovementClass.Vehicle);
                cost = float.IsPositiveInfinity(c) ? 1f : math.max(1f, c);
            }

            float factor = order == OrderType.MoveFast ? 1f : CautiousFactor;
            return baseSpeed * factor / cost;
        }

        /// <summary>Flattens fences and wooden sheds under the hull.</summary>
        void CrushUnder(Vehicle vehicle)
        {
            var grid = _map.Grid;
            var center = grid.WorldToCell(vehicle.Position);
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (grid.InBounds(center.X + dx, center.Y + dy))
                    {
                        _map.CrushCell(grid.IndexOf(center.X + dx, center.Y + dy));
                    }
                }
            }
        }

        /// <summary>Crew ride with the hull; they face where it faces.</summary>
        public static void SyncCrew(Vehicle vehicle)
        {
            foreach (var member in vehicle.Team.Members)
            {
                if (member.IsMounted)
                {
                    member.PreviousPosition = member.Position;
                    member.Position = vehicle.Position;
                    member.Heading = vehicle.HullHeading;
                    member.IsMoving = vehicle.IsMoving;
                    member.Posture = Posture.Standing;
                }
            }
        }

        static void Stop(Vehicle vehicle)
        {
            vehicle.Path.Clear();
            vehicle.PathCumulative.Clear();
            vehicle.PathProgress = 0f;
            vehicle.IsMoving = false;
            vehicle.Reversing = false;
            SyncCrew(vehicle);
        }

        /// <summary>The driver, or anyone aboard who can take the controls.</summary>
        public static Soldier DriverOf(Vehicle vehicle)
        {
            var driver = vehicle.CrewIn(CrewRole.Driver);
            if (driver != null)
            {
                return driver;
            }

            foreach (var member in vehicle.Team.Members)
            {
                if (member.IsActive && member.IsMounted && !member.IsOutOfControl)
                {
                    return member;
                }
            }

            return null;
        }

        public static float DeltaAngle(float from, float to)
        {
            float delta = (to - from) % 360f;
            if (delta > 180f) delta -= 360f;
            if (delta < -180f) delta += 360f;
            return delta;
        }

        public static float Wrap(float angle)
        {
            angle %= 360f;
            return angle < 0f ? angle + 360f : angle;
        }
    }
}
