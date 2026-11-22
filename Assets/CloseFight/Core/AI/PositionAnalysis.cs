using System.Collections.Generic;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Path;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.AI
{
    /// <summary>
    /// Reads the battlefield the way a defending officer would, from the map
    /// alone (no enemy positions). Candidate positions every few meters are
    /// rated for cover toward the enemy, height, concealment, and field of fire:
    /// which points of the approach (the ground the enemy must cross) each
    /// position can see. Points the enemy's tanks could use are tracked too.
    /// Computed once at the start; later milestones may refresh it as buildings fall.
    /// </summary>
    public sealed class PositionAnalysis
    {
        public const float CandidateSpacing = 4f;
        public const float ApproachSpacing = 10f;
        public const float FieldOfFireRange = 350f;

        public sealed class Candidate
        {
            public float2 Position;
            public bool InBuilding;
            public int Floors;
            /// <summary>Cover 0..100 against fire from the enemy's direction.</summary>
            public float Cover;
            /// <summary>Share of all approach points this position can see (0..1).</summary>
            public float Coverage;
            /// <summary>Share of tank-passable approach points it can see.</summary>
            public float VehicleCoverage;
            /// <summary>Share of approach points that can see it (how exposed it is).</summary>
            public float Exposure;
            public bool VehicleOk;
            /// <summary>Distance from the enemy's edge: larger is further back.</summary>
            public float Depth;
            /// <summary>Closeness to objectives, weighted by their value.</summary>
            public float ObjectiveScore;
            public bool[] Sees;
        }

        readonly List<Candidate> _candidates = new List<Candidate>();
        readonly List<float2> _approach = new List<float2>();
        readonly List<bool> _approachVehicle = new List<bool>();

        public IReadOnlyList<Candidate> Candidates => _candidates;
        public IReadOnlyList<float2> ApproachPoints => _approach;
        public IReadOnlyList<bool> ApproachForVehicles => _approachVehicle;
        public MapEdge ThreatEdge { get; }
        public float2 TowardEnemy { get; }

        readonly TacticalMap _map;

        public PositionAnalysis(TacticalMap map, MapEdge threatEdge, IReadOnlyList<Objective> objectives)
        {
            _map = map;
            ThreatEdge = threatEdge;
            TowardEnemy = EdgeNormal(threatEdge);
            var pathfinder = new GridPathfinder(map);
            var grid = map.Grid;
            float width = grid.Width * grid.CellSize;
            float height = grid.Height * grid.CellSize;
            float mapDepth = threatEdge == MapEdge.North || threatEdge == MapEdge.South ? height : width;

            // The approach: ground in front of the objectives (or the near 60% of the map).
            float zoneDepth = mapDepth * 0.6f;
            if (objectives.Count > 0)
            {
                zoneDepth = 0f;
                foreach (var objective in objectives)
                {
                    zoneDepth = math.max(zoneDepth, Depth(objective.Position, width, height) - objective.Radius * 0.5f);
                }
            }

            for (float y = ApproachSpacing * 0.5f; y < height; y += ApproachSpacing)
            {
                for (float x = ApproachSpacing * 0.5f; x < width; x += ApproachSpacing)
                {
                    var point = new float2(x, y);
                    var cell = grid.WorldToCell(point);
                    int index = grid.IndexOf(cell);
                    if (Depth(point, width, height) > zoneDepth || !pathfinder.IsPassable(index, MovementClass.Infantry))
                    {
                        continue;
                    }

                    _approach.Add(point);
                    _approachVehicle.Add(pathfinder.IsPassable(index, MovementClass.Vehicle));
                }
            }

            int vehiclePoints = 0;
            foreach (bool v in _approachVehicle)
            {
                if (v) vehiclePoints++;
            }

            for (float y = CandidateSpacing * 0.5f; y < height; y += CandidateSpacing)
            {
                for (float x = CandidateSpacing * 0.5f; x < width; x += CandidateSpacing)
                {
                    var point = new float2(x, y);
                    int index = grid.IndexOf(grid.WorldToCell(point));
                    if (!pathfinder.IsPassable(index, MovementClass.Infantry))
                    {
                        continue;
                    }

                    var building = map.BuildingAt(index);
                    bool inBuilding = building != null && !grid.HasFlag(index, CellFlags.Collapsed);
                    float eye = inBuilding ? (building.Floors - 1) * TerrainInfo.FloorHeight + LineOfSight.EyeStanding : LineOfSight.EyeKneeling;

                    var candidate = new Candidate
                    {
                        Position = point,
                        InBuilding = inBuilding,
                        Floors = inBuilding ? building.Floors : 0,
                        Cover = DirectionalCover(point, index),
                        VehicleOk = pathfinder.IsPassable(index, MovementClass.Vehicle),
                        Depth = Depth(point, width, height),
                        Sees = new bool[_approach.Count],
                    };

                    int seen = 0, seenVehicle = 0, seenBy = 0;
                    for (int i = 0; i < _approach.Count; i++)
                    {
                        float2 p = _approach[i];
                        if (math.distance(p, point) > FieldOfFireRange)
                        {
                            continue;
                        }

                        var sight = LineOfSight.Check(map, point, eye, p, LineOfSight.EyeKneeling);
                        if (!sight.Visible)
                        {
                            continue;
                        }

                        seenBy++;
                        if (Depth(p, width, height) < candidate.Depth - 5f)
                        {
                            candidate.Sees[i] = true;
                            seen++;
                            if (_approachVehicle[i]) seenVehicle++;
                        }
                    }

                    candidate.Coverage = _approach.Count == 0 ? 0f : seen / (float)_approach.Count;
                    candidate.VehicleCoverage = vehiclePoints == 0 ? 0f : seenVehicle / (float)vehiclePoints;
                    candidate.Exposure = _approach.Count == 0 ? 0f : seenBy / (float)_approach.Count;

                    foreach (var objective in objectives)
                    {
                        float d = math.distance(point, objective.Position);
                        candidate.ObjectiveScore += objective.Value * math.max(0f, 1f - d / (objective.Radius + 40f));
                    }

                    _candidates.Add(candidate);
                }
            }
        }

        /// <summary>Distance from the enemy's edge into the map.</summary>
        public float Depth(float2 point)
        {
            var grid = _map.Grid;
            return Depth(point, grid.Width * grid.CellSize, grid.Height * grid.CellSize);
        }

        float Depth(float2 point, float width, float height)
        {
            switch (ThreatEdge)
            {
                case MapEdge.North: return height - point.y;
                case MapEdge.West: return point.x;
                case MapEdge.East: return width - point.x;
                default: return point.y;
            }
        }

        static float2 EdgeNormal(MapEdge edge)
        {
            switch (edge)
            {
                case MapEdge.North: return new float2(0f, 1f);
                case MapEdge.West: return new float2(-1f, 0f);
                case MapEdge.East: return new float2(1f, 0f);
                default: return new float2(0f, -1f);
            }
        }

        /// <summary>Own cell cover, or a wall / rubble heap within 2.5 m toward the enemy.</summary>
        float DirectionalCover(float2 point, int index)
        {
            var grid = _map.Grid;
            float cover = grid.GetCover(index);
            var visitor = new CoverVisitor { Grid = grid, Own = index, Best = cover };
            GridTraversal.Traverse(grid, point, point + TowardEnemy * 2.5f, ref visitor);
            return visitor.Best;
        }

        struct CoverVisitor : ICellVisitor
        {
            public TacticalGrid Grid;
            public int Own;
            public float Best;

            public bool Visit(int x, int y, float tEnter, float tExit)
            {
                int index = Grid.IndexOf(x, y);
                if (index != Own && Grid.GetObstacleHeight(index) >= 0.6f)
                {
                    Best = math.max(Best, Grid.GetCover(index));
                }

                return true;
            }
        }

        /// <summary>
        /// Crossfire value of putting a weapon at <paramref name="c"/> given weapons
        /// already placed: approach points it sees that another placed position also
        /// sees from an angle at least 40° different. Two guns covering the same ground
        /// from different directions leave the enemy no side to hide on.
        /// </summary>
        public float Crossfire(Candidate c, IReadOnlyList<Candidate> placed)
        {
            if (placed.Count == 0 || _approach.Count == 0)
            {
                return 0f;
            }

            int crossed = 0;
            float minCos = math.cos(math.radians(40f));
            for (int i = 0; i < _approach.Count; i++)
            {
                if (!c.Sees[i])
                {
                    continue;
                }

                float2 toC = math.normalizesafe(c.Position - _approach[i]);
                foreach (var other in placed)
                {
                    if (!other.Sees[i])
                    {
                        continue;
                    }

                    float2 toOther = math.normalizesafe(other.Position - _approach[i]);
                    if (math.dot(toC, toOther) < minCos)
                    {
                        crossed++;
                        break;
                    }
                }
            }

            return crossed / (float)_approach.Count;
        }
    }
}
