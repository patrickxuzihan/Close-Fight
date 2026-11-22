using System;
using System.Collections.Generic;

namespace CloseFight.Core.Map
{
    /// <summary>
    /// The authored map: vector data saved as JSON. Everything the player sees and
    /// everything the simulation uses is generated from this file. Destruction
    /// changes the runtime state built from it, never the file itself.
    /// Coordinates are meters; origin is the south-west corner; Y points north.
    /// </summary>
    [Serializable]
    public sealed class MapDefinition
    {
        public string id = "untitled";
        /// <summary>Localization key for the display name.</summary>
        public string nameKey = "";
        public float width = 100f;
        public float height = 100f;
        public float cellSize = 1f;
        /// <summary>Terrain used for any cell no area covers.</summary>
        public TerrainType baseTerrain = TerrainType.Open;
        /// <summary>Terrain polygons, painted in order (later ones overwrite earlier ones).</summary>
        public List<AreaDefinition> areas = new List<AreaDefinition>();
        public List<BuildingDefinition> buildings = new List<BuildingDefinition>();
        public List<WallDefinition> walls = new List<WallDefinition>();
    }

    [Serializable]
    public sealed class AreaDefinition
    {
        public TerrainType terrain = TerrainType.Open;
        public List<MapPoint> polygon = new List<MapPoint>();
    }

    [Serializable]
    public sealed class BuildingDefinition
    {
        public int id;
        public string nameKey = "";
        public BuildingMaterial material = BuildingMaterial.Brick;
        public int floors = 2;
        /// <summary>Footprint outline, counter-clockwise or clockwise.</summary>
        public List<MapPoint> polygon = new List<MapPoint>();
    }

    /// <summary>A wall, fence or barricade drawn as a polyline.</summary>
    [Serializable]
    public sealed class WallDefinition
    {
        public List<MapPoint> points = new List<MapPoint>();
        public float height = 2f;
        public bool blocksMovement = true;
        /// <summary>Fences and wooden sheds can be crushed by tanks (D37).</summary>
        public bool crushable;
    }
}
