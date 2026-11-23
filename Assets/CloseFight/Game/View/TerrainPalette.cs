using CloseFight.Core.Map;
using UnityEngine;

namespace CloseFight.Game.View
{
    /// <summary>
    /// Placeholder colors for the aerial view. Dusty, desaturated tones of a
    /// bombed city in late summer. Replaced by photo textures later (D20).
    /// </summary>
    public static class TerrainPalette
    {
        public static Color Ground(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Road: return new Color(0.42f, 0.41f, 0.39f);
                case TerrainType.Rubble: return new Color(0.56f, 0.48f, 0.41f);
                case TerrainType.Rail: return new Color(0.27f, 0.25f, 0.23f);
                case TerrainType.Grass: return new Color(0.43f, 0.45f, 0.30f);
                case TerrainType.Water: return new Color(0.20f, 0.28f, 0.31f);
                case TerrainType.BuildingFloor: return new Color(0.33f, 0.30f, 0.28f);
                case TerrainType.Crater: return new Color(0.30f, 0.26f, 0.22f);
                default: return new Color(0.52f, 0.48f, 0.40f);
            }
        }

        /// <summary>How strongly per-cell noise varies this terrain (0 = flat color).</summary>
        public static float Noise(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Rubble: return 0.16f;
                case TerrainType.Crater: return 0.10f;
                case TerrainType.Grass: return 0.08f;
                case TerrainType.Road: return 0.04f;
                case TerrainType.Water: return 0.02f;
                default: return 0.06f;
            }
        }

        public static Color Building(BuildingMaterial material)
        {
            switch (material)
            {
                case BuildingMaterial.Wood: return new Color(0.42f, 0.34f, 0.26f);
                case BuildingMaterial.Concrete: return new Color(0.60f, 0.59f, 0.56f);
                default: return new Color(0.55f, 0.40f, 0.32f);
            }
        }

        public static readonly Color Wall = new Color(0.50f, 0.45f, 0.40f);

        public static Color Prop(string kind)
        {
            switch (kind)
            {
                case "wagon": return new Color(0.42f, 0.24f, 0.18f);     // oxide-red freight car
                case "fountain": return new Color(0.70f, 0.68f, 0.62f);  // pale concrete
                case "barricade": return new Color(0.36f, 0.30f, 0.24f);
                default: return new Color(0.45f, 0.42f, 0.38f);
            }
        }
        public static readonly Color Fence = new Color(0.38f, 0.31f, 0.24f);

        /// <summary>Deterministic hash in [0, 1) for color variation.</summary>
        public static float Hash01(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }
    }
}
