using CloseFight.Core.Map;
using UnityEngine;

namespace CloseFight.Game.View
{
    /// <summary>
    /// Codex terrain art under Resources/Art/Terrain: six ground tiles and four
    /// roof tiles, 512 px covering 8 m (64 px = 1 m), seamless. The ground shader
    /// is CloseFight/TerrainTiles (Resources/Shaders). Missing art falls back to
    /// the plain colored ground and roofs.
    /// </summary>
    static class TerrainArt
    {
        public const float TileMeters = 8f;

        static readonly string[] GroundTiles = { "ground_dirt", "ground_grass", "ground_road", "ground_rubble", "ground_rail", "ground_water" };
        static readonly string[] ShaderSlots = { "_Dirt", "_Grass", "_Road", "_Rubble", "_Rail", "_Water" };

        public static Shader GroundShader => Resources.Load<Shader>("Shaders/TerrainTiles");

        public static bool HasGroundTiles
        {
            get
            {
                foreach (var name in GroundTiles)
                {
                    if (Tile(name) == null) return false;
                }

                return true;
            }
        }

        public static void AssignGroundTiles(Material material)
        {
            for (int i = 0; i < GroundTiles.Length; i++)
            {
                material.SetTexture(ShaderSlots[i], Tile(GroundTiles[i]));
            }
        }

        /// <summary>
        /// Roof by material: sheds and wooden houses wood; brick houses sheet iron or
        /// clay tiles (picked per building); concrete blocks a flat tar roof.
        /// </summary>
        public static Texture2D Roof(BuildingMaterial material, int buildingId)
        {
            switch (material)
            {
                case BuildingMaterial.Wood: return Tile("roof_wood");
                case BuildingMaterial.Concrete: return Tile("roof_flat");
                default: return TerrainPalette.Hash01(buildingId, 31) < 0.6f ? Tile("roof_iron") : Tile("roof_tile");
            }
        }

        static Texture2D Tile(string name) => Resources.Load<Texture2D>("Art/Terrain/" + name);
    }
}
