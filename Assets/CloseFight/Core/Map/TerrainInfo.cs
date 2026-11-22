namespace CloseFight.Core.Map
{
    /// <summary>
    /// Gameplay properties of terrain and building materials. Tuning lives here,
    /// in one table, so balancing never means hunting through code.
    /// </summary>
    public static class TerrainInfo
    {
        /// <summary>Height of one building storey in meters.</summary>
        public const float FloorHeight = 3.5f;
        /// <summary>Height of the rubble heap a collapsed building cell leaves behind.</summary>
        public const float RubbleHeight = 1.2f;

        /// <summary>Movement cost multiplier; float.PositiveInfinity means impassable.</summary>
        public static float MoveCost(TerrainType terrain, MovementClass movement)
        {
            if (movement == MovementClass.Infantry)
            {
                switch (terrain)
                {
                    case TerrainType.Road: return 1.0f;
                    case TerrainType.Open: return 1.1f;
                    case TerrainType.Grass: return 1.2f;
                    case TerrainType.Rail: return 1.3f;
                    case TerrainType.BuildingFloor: return 1.5f;
                    case TerrainType.Crater: return 1.6f;
                    case TerrainType.Rubble: return 1.8f;
                    case TerrainType.Water: return float.PositiveInfinity;
                }
            }
            else
            {
                switch (terrain)
                {
                    case TerrainType.Road: return 1.0f;
                    case TerrainType.Open: return 1.3f;
                    case TerrainType.Grass: return 1.4f;
                    case TerrainType.Rail: return 1.8f;
                    case TerrainType.Crater: return 2.5f;
                    case TerrainType.Rubble: return 3.0f;
                    case TerrainType.BuildingFloor: return float.PositiveInfinity;
                    case TerrainType.Water: return float.PositiveInfinity;
                }
            }

            return 1.0f;
        }

        /// <summary>Lowest move cost of any terrain; keeps the A* heuristic admissible.</summary>
        public const float MinMoveCost = 1.0f;

        /// <summary>Cover value 0..100 granted by standing on this terrain in the open.</summary>
        public static byte TerrainCover(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Rubble: return 45;
                case TerrainType.Crater: return 50;
                case TerrainType.Rail: return 20;
                case TerrainType.Grass: return 10;
                default: return 0;
            }
        }

        /// <summary>Height in meters of the obstacle formed by the terrain itself.</summary>
        public static float TerrainObstacleHeight(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Rubble: return RubbleHeight;
                case TerrainType.Rail: return 0.3f;
                default: return 0f;
            }
        }

        /// <summary>Cover 0..100 for a unit inside a building of this material.</summary>
        public static byte BuildingCover(BuildingMaterial material)
        {
            switch (material)
            {
                case BuildingMaterial.Wood: return 35;
                case BuildingMaterial.Brick: return 70;
                case BuildingMaterial.Concrete: return 85;
                default: return 50;
            }
        }

        /// <summary>Cover 0..100 behind a wall or fence of the given height.</summary>
        public static byte WallCover(float wallHeight, bool crushable)
        {
            if (crushable)
            {
                return 25;
            }

            return wallHeight >= 1.0f ? (byte)65 : (byte)40;
        }
    }
}
