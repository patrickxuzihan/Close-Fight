using System;
using System.Collections.Generic;
using System.Linq;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.Campaign
{
    /// <summary>
    /// Where a campaign battle is fought (M16 step 4): the district's hand-made map if it has one
    /// (the station), otherwise its generated map; deployment zones laid out from the side the
    /// attacker comes in from; objectives in the middle and in the defender's rear.
    /// </summary>
    public static class DistrictBattlefields
    {
        /// <summary>Companies a side can bring onto a generated map.</summary>
        public const int GeneratedCapacity = 4;
        /// <summary>Companies a side can bring onto the station map (a company battle, M8).</summary>
        public const int HandMadeCapacity = 3;
        /// <summary>Depth of the attacker's deployment strip along its edge, in meters.</summary>
        public const float AttackerDepth = 40f;
        /// <summary>The defender deploys from this share of the map's depth onwards, leaving no-man's-land between.</summary>
        public const float DefenderStart = 0.3f;

        /// <summary>
        /// The map and battlefield of a pending battle. <paramref name="handMade"/> returns the map and
        /// a scenario on it for a hand-made map id, or null to have the district's map generated.
        /// </summary>
        public static (MapDefinition map, Battlefield field) For(CampaignState state, PendingBattle battle,
            Func<string, (MapDefinition map, ScenarioDefinition scenario)?> handMade)
        {
            var district = state.DistrictDefinition(battle.district);
            if (!string.IsNullOrEmpty(district.mapId) && handMade != null)
            {
                var source = handMade(district.mapId);
                if (source.HasValue)
                {
                    return (source.Value.map, Battlefield.FromScenario(source.Value.scenario, HandMadeCapacity));
                }
            }

            var map = DistrictMapGenerator.Generate(district, MapSeed(state, district.id));
            return (map, Orient(map, AttackEdge(state, battle)));
        }

        /// <summary>The seed of a district's generated map: the same map every time it is fought over.</summary>
        public static ulong MapSeed(CampaignState state, string district) => state.definition.seed ^ CampaignHash.Of(district);

        /// <summary>
        /// The map edge the attacker comes in from: towards the district it attacked from on the
        /// strategic map (strategic Y runs south, battle-map Y north). A fight that goes on in a
        /// contested district comes from the attacker's nearest district.
        /// </summary>
        public static MapEdge AttackEdge(CampaignState state, PendingBattle battle)
        {
            var district = state.DistrictDefinition(battle.district);
            string from = battle.attackers.Select(state.Regiment).Where(r => r != null && r.origin != battle.district)
                .Select(r => r.origin).FirstOrDefault();
            if (from == null)
            {
                from = district.neighbours.Where(n => state.District(n).owner == battle.attacker)
                    .OrderBy(n => math.distancesq((float2)state.DistrictDefinition(n).label, (float2)district.label))
                    .FirstOrDefault();
            }

            if (from == null)
            {
                return MapEdge.West;
            }

            float2 d = (float2)state.DistrictDefinition(from).label - (float2)district.label;
            d.y = -d.y;
            return math.abs(d.x) >= math.abs(d.y) ? (d.x > 0 ? MapEdge.East : MapEdge.West) : (d.y > 0 ? MapEdge.North : MapEdge.South);
        }

        /// <summary>Lays a generated map out for an attack from <paramref name="edge"/>.</summary>
        public static Battlefield Orient(MapDefinition map, MapEdge edge)
        {
            float w = map.width, h = map.height;
            // Work in "depth" (from the attacker's edge) and "across"; convert at the end.
            bool alongX = edge == MapEdge.West || edge == MapEdge.East;
            float depth = alongX ? w : h, across = alongX ? h : w;
            MapPoint P(float d, float a)
            {
                float dd = edge == MapEdge.East || edge == MapEdge.North ? depth - d : d;
                return alongX ? new MapPoint(dd, a) : new MapPoint(a, dd);
            }

            List<MapPoint> Band(float d0, float d1) => new List<MapPoint> { P(d0, 0f), P(d1, 0f), P(d1, across), P(d0, across) };

            var field = new Battlefield
            {
                mapId = map.id,
                attackerZone = Band(0f, AttackerDepth),
                defenderZone = Band(depth * DefenderStart, depth),
                attackerRetreat = edge,
                defenderRetreat = Opposite(edge),
                capacity = GeneratedCapacity,
            };

            // Objectives: the middle (worth most), and two in the defender's rear, left and right.
            var targets = new[]
            {
                (P(depth * 0.5f, across * 0.5f), 3, "objective.district_centre"),
                (P(depth * 0.72f, across * 0.25f), 2, "objective.district_left"),
                (P(depth * 0.72f, across * 0.75f), 2, "objective.district_right"),
            };
            int index = 0;
            foreach (var (point, value, name) in targets)
            {
                var spot = NearBuilding(map, point, 45f, out float radius);
                field.objectives.Add(new ObjectiveDefinition
                {
                    id = "o" + index++, nameKey = name, position = spot, radius = radius, value = value,
                });
            }

            return field;
        }

        static MapEdge Opposite(MapEdge edge)
        {
            switch (edge)
            {
                case MapEdge.West: return MapEdge.East;
                case MapEdge.East: return MapEdge.West;
                case MapEdge.North: return MapEdge.South;
                default: return MapEdge.North;
            }
        }

        /// <summary>The biggest building near a point (objectives are buildings worth holding), or the point itself on open ground.</summary>
        static MapPoint NearBuilding(MapDefinition map, MapPoint point, float reach, out float radius)
        {
            BuildingDefinition best = null;
            float bestArea = 0f;
            foreach (var building in map.buildings)
            {
                var c = Centre(building.polygon);
                if (math.distance(c, point) > reach)
                {
                    continue;
                }

                float area = Area(building.polygon);
                if (area > bestArea)
                {
                    bestArea = area;
                    best = building;
                }
            }

            if (best == null || bestArea < 60f)
            {
                radius = 15f;
                return point;
            }

            radius = math.clamp(math.sqrt(bestArea) * 0.7f, 10f, 22f);
            return Centre(best.polygon);
        }

        static float2 Centre(List<MapPoint> polygon)
        {
            float2 sum = float2.zero;
            foreach (var p in polygon) sum += (float2)p;
            return sum / math.max(1, polygon.Count);
        }

        static float Area(List<MapPoint> polygon)
        {
            float area = 0f;
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                area += a.x * b.y - b.x * a.y;
            }

            return math.abs(area) * 0.5f;
        }
    }

    /// <summary>
    /// The damage a district's map carries from battle to battle (D33): collapsed building cells and
    /// walls and fences that are gone. The map itself is rebuilt from its definition each time.
    /// </summary>
    public static class DistrictDamage
    {
        public static void Apply(TacticalMap map, DistrictState district)
        {
            var grid = map.Grid;
            foreach (int cell in district.collapsed)
            {
                if (cell >= 0 && cell < grid.CellCount) map.CollapseCell(cell);
            }

            foreach (int cell in district.wallsGone)
            {
                if (cell < 0 || cell >= grid.CellCount || !grid.HasFlag(cell, CellFlags.Wall)) continue;
                if (grid.HasFlag(cell, CellFlags.BlocksMovement)) map.DemolishWall(cell);
                else map.CrushCell(cell);
            }
        }

        /// <summary>Records what the battle destroyed, against a fresh build of the same map.</summary>
        public static void Capture(TacticalMap map, DistrictState district)
        {
            var fresh = MapBuilder.Build(map.Definition).Grid;
            var grid = map.Grid;
            district.collapsed.Clear();
            district.wallsGone.Clear();
            for (int i = 0; i < grid.CellCount; i++)
            {
                if (grid.HasFlag(i, CellFlags.Collapsed))
                {
                    district.collapsed.Add(i);
                }
                else if (fresh.HasFlag(i, CellFlags.Wall) && !grid.HasFlag(i, CellFlags.Wall))
                {
                    district.wallsGone.Add(i);
                }
            }
        }
    }

    /// <summary>A string hash that is the same on every run (string.GetHashCode is not guaranteed to be).</summary>
    public static class CampaignHash
    {
        public static ulong Of(string text)
        {
            ulong hash = 14695981039346656037UL;
            foreach (char c in text)
            {
                hash = (hash ^ c) * 1099511628211UL;
            }

            return hash;
        }
    }
}
