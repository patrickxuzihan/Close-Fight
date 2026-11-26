using System;
using System.Collections.Generic;
using CloseFight.Core.Map;
using CloseFight.Core.Sim;
using Unity.Mathematics;

namespace CloseFight.Core.Campaign
{
    /// <summary>
    /// Generates the battle map of a district that has no hand-made one (M16 step 4, D19, D55),
    /// from its kind: dense blocks of brick houses, wooden outskirts with gardens, walled
    /// factory yards, a railway with its wagons, a great square, the Volga bank, a bare hill.
    /// The same district and seed always give the same map, so only the damage of earlier
    /// battles needs saving (D33, <see cref="DistrictDamage"/>). These are simple first maps;
    /// M17 makes them denser and richer.
    /// Map coordinates are meters, origin south-west, Y north; the Volga, where there is one, is east.
    /// </summary>
    public static class DistrictMapGenerator
    {
        public static MapDefinition Generate(DistrictDefinition district, ulong seed)
        {
            var size = SizeOf(district.kind);
            var map = new MapDefinition
            {
                id = "district_" + district.id,
                nameKey = district.nameKey,
                width = size.x,
                height = size.y,
                cellSize = 1f,
                baseTerrain = district.kind == DistrictKind.Hill || district.kind == DistrictKind.Outskirts ? TerrainType.Grass : TerrainType.Open,
            };
            var sketch = new Sketch(map, new SimRandom(seed, 23UL));
            switch (district.kind)
            {
                case DistrictKind.Residential: Residential(sketch); break;
                case DistrictKind.Industrial: Industrial(sketch); break;
                case DistrictKind.Railway: Railway(sketch); break;
                case DistrictKind.Riverside: Riverside(sketch); break;
                case DistrictKind.Hill: Hill(sketch); break;
                case DistrictKind.Square: Square(sketch); break;
                default: Outskirts(sketch, 0f, 0f, size.x, size.y); break;
            }

            sketch.Scars(district.kind == DistrictKind.Hill ? 90 : district.kind == DistrictKind.Outskirts ? 40 : 55);
            return map;
        }

        /// <summary>Map size by kind: open ground is bigger, dense blocks smaller.</summary>
        public static float2 SizeOf(DistrictKind kind)
        {
            switch (kind)
            {
                case DistrictKind.Hill:
                case DistrictKind.Outskirts:
                    return new float2(560f, 460f);
                case DistrictKind.Industrial:
                case DistrictKind.Railway:
                    return new float2(520f, 420f);
                default:
                    return new float2(480f, 400f);
            }
        }

        // ---- Kinds ----------------------------------------------------------------------------

        /// <summary>The old centre: perimeter blocks of brick houses of two to five floors, one block in five a ruin.</summary>
        static void Residential(Sketch s)
        {
            foreach (var block in s.Streets(0f, 0f, s.W, s.H, 48f, 66f, 12f, 16f))
            {
                if (s.R.Chance(0.18f)) s.Ruin(block);
                else s.PerimeterBlock(block, 2, 5, 0.85f);
            }
        }

        static void Outskirts(Sketch s, float x0, float y0, float x1, float y1)
        {
            foreach (var block in s.Streets(x0, y0, x1, y1, 70f, 95f, 8f, 10f))
            {
                s.Plots(block);
            }
        }

        /// <summary>Walled factory yards with great halls, sheds and a rail spur.</summary>
        static void Industrial(Sketch s)
        {
            foreach (var block in s.Streets(0f, 0f, s.W, s.H, 95f, 135f, 14f, 18f))
            {
                s.Factory(block);
            }
        }

        /// <summary>A band of tracks across the middle with wagons on them, a goods shed beside it, houses either side.</summary>
        static void Railway(Sketch s)
        {
            float mid = s.H * s.R.Range(0.42f, 0.58f);
            float half = s.R.Range(22f, 30f);
            foreach (var block in s.Streets(0f, 0f, s.W, mid - half - 8f, 48f, 66f, 12f, 14f))
            {
                s.PerimeterBlock(block, 1, 4, 0.7f);
            }

            foreach (var block in s.Streets(0f, mid + half + 8f, s.W, s.H, 48f, 66f, 12f, 14f))
            {
                s.PerimeterBlock(block, 1, 4, 0.7f);
            }

            s.Area(TerrainType.Road, 0f, mid - half - 8f, s.W, mid - half);
            s.Area(TerrainType.Road, 0f, mid + half, s.W, mid + half + 8f);
            s.Area(TerrainType.Rail, 0f, mid - half, s.W, mid + half);
            int tracks = (int)(half * 2f / 7f);
            for (int t = 0; t < tracks; t++)
            {
                float y = mid - half + 3.5f + t * 7f;
                float x = s.R.Range(0f, 40f);
                while (x < s.W - 14f)
                {
                    if (s.R.Chance(0.45f))
                    {
                        int run = s.R.Range(1, 5);
                        for (int k = 0; k < run && x < s.W - 14f; k++)
                        {
                            s.Prop("wagon", x, y - 1.5f, x + 12f, y + 1.5f, 3.2f, 75f);
                            x += 13f;
                        }
                    }

                    x += s.R.Range(15f, 45f);
                }
            }

            float shedX = s.R.Range(60f, s.W - 160f);
            s.Building(shedX, mid + half - 20f, shedX + s.R.Range(70f, 100f), mid + half - 4f, 2, BuildingMaterial.Brick);
        }

        /// <summary>The Volga along the east edge, a steep grassy bank, a landing stage, riverside houses.</summary>
        static void Riverside(Sketch s)
        {
            float water = s.W - s.R.Range(60f, 80f);
            float bank = water - s.R.Range(28f, 40f);
            foreach (var block in s.Streets(0f, 0f, bank - 10f, s.H, 48f, 64f, 12f, 14f))
            {
                if (s.R.Chance(0.15f)) s.Ruin(block);
                else s.PerimeterBlock(block, 1, 4, 0.75f);
            }

            s.Area(TerrainType.Road, bank - 10f, 0f, bank, s.H);
            s.Area(TerrainType.Grass, bank, 0f, water, s.H);
            s.Area(TerrainType.Water, water, 0f, s.W, s.H);
            s.WaterFrom = water;
            float landing = s.R.Range(s.H * 0.3f, s.H * 0.7f);
            s.Building(bank + 4f, landing - 12f, water - 4f, landing + 12f, 2, BuildingMaterial.Wood);
            for (int i = 0; i < 4; i++)
            {
                float y = s.R.Range(20f, s.H - 20f);
                if (math.abs(y - landing) > 25f)
                {
                    s.Prop("wagon", water - 2f, y - 2f, water + 14f, y + 2f, 2f, 60f);
                }
            }
        }

        /// <summary>Bare slopes, shell holes, trench lines and the water tanks on the crest (Mamayev Kurgan).</summary>
        static void Hill(Sketch s)
        {
            float cx = s.W * 0.5f, cy = s.H * 0.5f;
            s.Tank(cx - 18f, cy + 6f, 8f);
            s.Tank(cx + 18f, cy - 6f, 8f);
            for (int line = 0; line < 4; line++)
            {
                float radius = 50f + line * 45f;
                var points = new List<float2>();
                float start = s.R.Range(0f, 6.28f);
                for (int k = 0; k <= 9; k++)
                {
                    float a = start + k * 0.33f;
                    float r = radius + (k % 2 == 0 ? 6f : -6f);
                    points.Add(new float2(cx + math.cos(a) * r, cy + math.sin(a) * r));
                }

                s.Wall(points, 1.0f, false, false);
            }

            // A few wooden houses on the lower slopes.
            for (int i = 0; i < 6; i++)
            {
                float x = s.R.Chance(0.5f) ? s.R.Range(15f, 90f) : s.R.Range(s.W - 90f, s.W - 25f);
                float y = s.R.Range(15f, s.H - 30f);
                s.Building(x, y, x + s.R.Range(7f, 11f), y + s.R.Range(6f, 9f), 1, BuildingMaterial.Wood);
            }
        }

        /// <summary>A great square with a fountain, lined with big public buildings; ordinary blocks beyond.</summary>
        static void Square(Sketch s)
        {
            float sw = s.R.Range(120f, 150f), sh = s.R.Range(100f, 120f);
            float x0 = (s.W - sw) / 2f, y0 = (s.H - sh) / 2f, x1 = x0 + sw, y1 = y0 + sh;
            float depth = 28f;
            // Ordinary blocks in the four bands around the square and its frontage.
            foreach (var band in new[]
                     {
                         new float4(0f, 0f, s.W, y0 - depth - 14f), new float4(0f, y1 + depth + 14f, s.W, s.H),
                         new float4(0f, y0 - depth - 14f, x0 - depth - 14f, y1 + depth + 14f),
                         new float4(x1 + depth + 14f, y0 - depth - 14f, s.W, y1 + depth + 14f),
                     })
            {
                if (band.z - band.x < 40f || band.w - band.y < 40f) continue;
                foreach (var block in s.Streets(band.x, band.y, band.z, band.w, 46f, 62f, 12f, 14f))
                {
                    s.PerimeterBlock(block, 2, 5, 0.9f);
                }
            }

            s.Area(TerrainType.Road, x0 - depth - 14f, y0 - depth - 14f, x1 + depth + 14f, y1 + depth + 14f);
            s.Area(TerrainType.Open, x0, y0, x1, y1);
            s.Area(TerrainType.Grass, x0 + 10f, y0 + 10f, x0 + sw * 0.35f, y1 - 10f);
            s.Area(TerrainType.Grass, x1 - sw * 0.35f, y0 + 10f, x1 - 10f, y1 - 10f);
            float fx = (x0 + x1) / 2f, fy = (y0 + y1) / 2f;
            s.PropCircle("fountain", fx, fy, 6f, 1.2f, 55f);
            // The big buildings fronting the square, a street's width back from it.
            s.Frontage(x0 - depth - 6f, y1 + 6f, x1 + depth + 6f, y1 + 6f + depth);
            s.Frontage(x0 - depth - 6f, y0 - 6f - depth, x1 + depth + 6f, y0 - 6f);
            s.Frontage(x0 - depth - 6f, y0, x0 - 6f, y1);
            s.Frontage(x1 + 6f, y0, x1 + depth + 6f, y1);
        }

        // ---- Drawing helpers --------------------------------------------------------------------

        sealed class Sketch
        {
            readonly MapDefinition _map;
            public readonly SimRandom R;
            int _nextId = 1;
            /// <summary>East of this the map is river: no shell holes there.</summary>
            public float WaterFrom = float.MaxValue;

            public float W => _map.width;
            public float H => _map.height;

            public Sketch(MapDefinition map, SimRandom random)
            {
                _map = map;
                R = random;
            }

            static List<MapPoint> Rect(float x0, float y0, float x1, float y1) => new List<MapPoint>
            {
                new MapPoint(x0, y0), new MapPoint(x1, y0), new MapPoint(x1, y1), new MapPoint(x0, y1),
            };

            public void Area(TerrainType terrain, float x0, float y0, float x1, float y1)
            {
                if (x1 - x0 < 1f || y1 - y0 < 1f) return;
                _map.areas.Add(new AreaDefinition { terrain = terrain, polygon = Rect(x0, y0, x1, y1) });
            }

            public void Circle(TerrainType terrain, float cx, float cy, float r)
            {
                var polygon = new List<MapPoint>();
                for (int k = 0; k < 8; k++)
                {
                    float a = k * math.PI / 4f;
                    polygon.Add(new MapPoint(cx + math.cos(a) * r, cy + math.sin(a) * r));
                }

                _map.areas.Add(new AreaDefinition { terrain = terrain, polygon = polygon });
            }

            public void Building(float x0, float y0, float x1, float y1, int floors, BuildingMaterial material)
            {
                if (x1 - x0 < 4f || y1 - y0 < 4f) return;
                x0 = math.max(1f, x0); y0 = math.max(1f, y0); x1 = math.min(W - 1f, x1); y1 = math.min(H - 1f, y1);
                _map.buildings.Add(new BuildingDefinition { id = _nextId++, floors = floors, material = material, polygon = Rect(x0, y0, x1, y1) });
            }

            /// <summary>A round concrete water tank (Mamayev Kurgan's two on the crest).</summary>
            public void Tank(float cx, float cy, float r)
            {
                var polygon = new List<MapPoint>();
                for (int k = 0; k < 10; k++)
                {
                    float a = k * math.PI / 5f;
                    polygon.Add(new MapPoint(cx + math.cos(a) * r, cy + math.sin(a) * r));
                }

                _map.buildings.Add(new BuildingDefinition { id = _nextId++, floors = 2, material = BuildingMaterial.Concrete, polygon = polygon });
            }

            public void Wall(List<float2> points, float height, bool blocksMovement, bool crushable)
            {
                var list = new List<MapPoint>();
                foreach (var p in points)
                {
                    list.Add(new MapPoint(math.clamp(p.x, 1f, W - 1f), math.clamp(p.y, 1f, H - 1f)));
                }

                _map.walls.Add(new WallDefinition { points = list, height = height, blocksMovement = blocksMovement, crushable = crushable });
            }

            public void Prop(string kind, float x0, float y0, float x1, float y1, float height, float cover)
            {
                if (x0 < 1f || y0 < 1f || x1 > W - 1f || y1 > H - 1f) return;
                _map.props.Add(new PropDefinition { kind = kind, polygon = Rect(x0, y0, x1, y1), height = height, cover = cover });
            }

            public void PropCircle(string kind, float cx, float cy, float r, float height, float cover)
            {
                var polygon = new List<MapPoint>();
                for (int k = 0; k < 8; k++)
                {
                    float a = k * math.PI / 4f;
                    polygon.Add(new MapPoint(cx + math.cos(a) * r, cy + math.sin(a) * r));
                }

                _map.props.Add(new PropDefinition { kind = kind, polygon = polygon, height = height, cover = cover });
            }

            /// <summary>
            /// Lays a street grid over a rectangle: roads between blocks of the given sizes (a little
            /// uneven, like a real town), and returns the blocks.
            /// </summary>
            public List<float4> Streets(float x0, float y0, float x1, float y1, float minBlock, float maxBlock, float minStreet, float maxStreet)
            {
                var blocks = new List<float4>();
                var xs = Cuts(x0, x1, minBlock, maxBlock, minStreet, maxStreet);
                var ys = Cuts(y0, y1, minBlock, maxBlock, minStreet, maxStreet);
                Area(TerrainType.Road, x0, y0, x1, y1);
                foreach (var (bx0, bx1) in xs)
                {
                    foreach (var (by0, by1) in ys)
                    {
                        blocks.Add(new float4(bx0, by0, bx1, by1));
                        Area(TerrainType.Open, bx0, by0, bx1, by1);
                    }
                }

                return blocks;
            }

            List<(float, float)> Cuts(float from, float to, float minBlock, float maxBlock, float minStreet, float maxStreet)
            {
                var cuts = new List<(float, float)>();
                float at = from + R.Range(minStreet, maxStreet) * 0.5f;
                while (at + minBlock <= to - 4f)
                {
                    float size = math.min(R.Range(minBlock, maxBlock), to - 4f - at);
                    cuts.Add((at, at + size));
                    at += size + R.Range(minStreet, maxStreet);
                }

                return cuts;
            }

            /// <summary>A city block: houses shoulder to shoulder along its four edges, sheds and fences in the yard.</summary>
            public void PerimeterBlock(float4 b, int minFloors, int maxFloors, float brickShare)
            {
                float depth = R.Range(10f, 15f);
                Row(b.x, b.z, b.y, b.y + depth, true, minFloors, maxFloors, brickShare);
                Row(b.x, b.z, b.w - depth, b.w, true, minFloors, maxFloors, brickShare);
                Row(b.y + depth + 1f, b.w - depth - 1f, b.x, b.x + depth, false, minFloors, maxFloors, brickShare);
                Row(b.y + depth + 1f, b.w - depth - 1f, b.z - depth, b.z, false, minFloors, maxFloors, brickShare);
                // The yard: a fence across it and a shed or two.
                float yx0 = b.x + depth + 2f, yx1 = b.z - depth - 2f, yy0 = b.y + depth + 2f, yy1 = b.w - depth - 2f;
                if (yx1 - yx0 > 12f && yy1 - yy0 > 12f)
                {
                    float fx = R.Range(yx0 + 4f, yx1 - 4f);
                    Wall(new List<float2> { new float2(fx, yy0), new float2(fx, yy1) }, 1.8f, false, true);
                    if (R.Chance(0.6f))
                    {
                        float sx = R.Range(yx0, yx1 - 8f), sy = R.Range(yy0, yy1 - 6f);
                        Building(sx, sy, sx + 7f, sy + 5f, 1, BuildingMaterial.Wood);
                    }
                }
            }

            /// <summary>Houses along one edge of a block, from <paramref name="from"/> to <paramref name="to"/>.</summary>
            void Row(float from, float to, float a0, float a1, bool alongX, int minFloors, int maxFloors, float brickShare)
            {
                float at = from;
                while (to - at >= 8f)
                {
                    float width = math.min(R.Range(10f, 22f), to - at);
                    if (to - at - width < 8f) width = to - at;
                    if (!R.Chance(0.08f))
                    {
                        int floors = R.Range(minFloors, maxFloors + 1);
                        var material = R.Chance(brickShare) ? BuildingMaterial.Brick : BuildingMaterial.Wood;
                        if (material == BuildingMaterial.Wood) floors = math.min(floors, 2);
                        if (alongX) Building(at, a0, at + width, a1, floors, material);
                        else Building(a0, at, a1, at + width, floors, material);
                    }

                    at += width + (R.Chance(0.3f) ? R.Range(1.5f, 4f) : 0f);
                }
            }

            /// <summary>A block bombed flat: a rubble field with the stumps of a few walls and houses.</summary>
            public void Ruin(float4 b)
            {
                Area(TerrainType.Rubble, b.x, b.y, b.z, b.w);
                for (int i = 0; i < 4; i++)
                {
                    float x = R.Range(b.x, b.z - 10f), y = R.Range(b.y, b.w - 10f);
                    if (R.Chance(0.5f)) Building(x, y, x + R.Range(6f, 10f), y + R.Range(6f, 10f), 1, BuildingMaterial.Brick);
                    else Wall(new List<float2> { new float2(x, y), new float2(x + R.Range(8f, 16f), y) }, 1.6f, false, false);
                }
            }

            /// <summary>Wooden houses on fenced plots with gardens.</summary>
            public void Plots(float4 b)
            {
                int cols = math.max(1, (int)((b.z - b.x) / 26f)), rows = math.max(1, (int)((b.w - b.y) / 24f));
                float pw = (b.z - b.x) / cols, ph = (b.w - b.y) / rows;
                for (int c = 0; c < cols; c++)
                {
                    for (int r = 0; r < rows; r++)
                    {
                        float x0 = b.x + c * pw, y0 = b.y + r * ph;
                        Area(TerrainType.Grass, x0 + 1f, y0 + 1f, x0 + pw - 1f, y0 + ph - 1f);
                        if (R.Chance(0.8f))
                        {
                            float w = R.Range(7f, 11f), d = R.Range(6f, 9f);
                            float hx = x0 + R.Range(2f, math.max(2.5f, pw - w - 2f)), hy = y0 + R.Range(2f, math.max(2.5f, ph - d - 2f));
                            bool brick = R.Chance(0.12f);
                            Building(hx, hy, hx + w, hy + d, brick ? 2 : 1, brick ? BuildingMaterial.Brick : BuildingMaterial.Wood);
                        }

                        if (R.Chance(0.7f))
                        {
                            Wall(new List<float2> { new float2(x0 + 1f, y0 + 1f), new float2(x0 + pw - 1f, y0 + 1f), new float2(x0 + pw - 1f, y0 + ph - 1f) },
                                1.5f, false, true);
                        }
                    }
                }
            }

            /// <summary>A walled works: one or two great halls, sheds, a gated wall round the yard, sometimes a rail spur.</summary>
            public void Factory(float4 b)
            {
                float w = b.z - b.x, h = b.w - b.y;
                // The wall, with a gate gap on two sides.
                float gate = R.Range(0.3f, 0.7f);
                Wall(new List<float2> { new float2(b.x, b.y), new float2(b.x + w * gate - 6f, b.y) }, 2.6f, true, false);
                Wall(new List<float2> { new float2(b.x + w * gate + 6f, b.y), new float2(b.z, b.y), new float2(b.z, b.w) }, 2.6f, true, false);
                Wall(new List<float2> { new float2(b.z, b.w), new float2(b.x + w * (1f - gate) + 6f, b.w) }, 2.6f, true, false);
                Wall(new List<float2> { new float2(b.x + w * (1f - gate) - 6f, b.w), new float2(b.x, b.w), new float2(b.x, b.y) }, 2.6f, true, false);

                float hall = R.Range(0.45f, 0.6f);
                Building(b.x + 8f, b.y + 8f, b.x + 8f + (w - 16f) * hall, b.w - 8f, R.Range(2, 4), BuildingMaterial.Concrete);
                float x = b.x + 12f + (w - 16f) * hall;
                if (b.z - 8f - x > 18f)
                {
                    float split = R.Range(b.y + h * 0.35f, b.y + h * 0.6f);
                    Building(x, b.y + 8f, b.z - 8f, split - 4f, R.Range(1, 3), BuildingMaterial.Brick);
                    if (R.Chance(0.6f)) Building(x, split + 4f, b.z - 8f, b.w - 8f, 1, BuildingMaterial.Brick);
                    else Area(TerrainType.Rubble, x, split + 4f, b.z - 8f, b.w - 8f);
                }
            }

            /// <summary>Big public buildings of three to five floors filling a strip.</summary>
            public void Frontage(float x0, float y0, float x1, float y1)
            {
                bool alongX = x1 - x0 >= y1 - y0;
                float from = alongX ? x0 : y0, to = alongX ? x1 : y1;
                float at = from;
                while (to - at >= 14f)
                {
                    float size = math.min(R.Range(26f, 48f), to - at);
                    if (to - at - size < 14f) size = to - at;
                    var material = R.Chance(0.5f) ? BuildingMaterial.Concrete : BuildingMaterial.Brick;
                    if (alongX) Building(at, y0, at + size, y1, R.Range(3, 6), material);
                    else Building(x0, at, x1, at + size, R.Range(3, 6), material);
                    at += size + R.Range(4f, 9f);
                }
            }

            /// <summary>Shell holes and rubble patches scattered over the map (the city after 23 August).</summary>
            public void Scars(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    float x = R.Range(6f, math.min(W, WaterFrom - 4f) - 6f), y = R.Range(6f, H - 6f);
                    if (R.Chance(0.7f)) Circle(TerrainType.Crater, x, y, R.Range(2f, 4.5f));
                    else Circle(TerrainType.Rubble, x, y, R.Range(3f, 7f));
                }
            }
        }
    }
}
