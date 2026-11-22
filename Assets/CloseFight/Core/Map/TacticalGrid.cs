using Unity.Mathematics;

namespace CloseFight.Core.Map
{
    /// <summary>
    /// The gameplay grid rasterized from a <see cref="MapDefinition"/>.
    /// One cell is <see cref="CellSize"/> meters square (1 m by default).
    /// Data is stored in flat arrays (struct-of-arrays) so it can later be
    /// handed to Jobs and Burst without conversion.
    /// </summary>
    public sealed class TacticalGrid
    {
        public const short NoBuilding = -1;

        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }
        public int CellCount => Width * Height;

        readonly TerrainType[] _terrain;
        readonly float[] _obstacleHeight;
        readonly CellFlags[] _flags;
        readonly short[] _buildingIndex;
        readonly byte[] _cover;

        public TacticalGrid(int width, int height, float cellSize)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;

            int count = width * height;
            _terrain = new TerrainType[count];
            _obstacleHeight = new float[count];
            _flags = new CellFlags[count];
            _buildingIndex = new short[count];
            _cover = new byte[count];

            for (int i = 0; i < count; i++)
            {
                _buildingIndex[i] = NoBuilding;
            }
        }

        public bool InBounds(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;
        public bool InBounds(GridCoord c) => InBounds(c.X, c.Y);
        public int IndexOf(int x, int y) => y * Width + x;
        public int IndexOf(GridCoord c) => c.Y * Width + c.X;
        public GridCoord CoordOf(int index) => new GridCoord(index % Width, index / Width);

        public GridCoord WorldToCell(float2 world)
        {
            return new GridCoord((int)math.floor(world.x / CellSize), (int)math.floor(world.y / CellSize));
        }

        public float2 CellCenter(GridCoord c)
        {
            return new float2((c.X + 0.5f) * CellSize, (c.Y + 0.5f) * CellSize);
        }

        public TerrainType GetTerrain(int index) => _terrain[index];
        public void SetTerrain(int index, TerrainType value) => _terrain[index] = value;

        /// <summary>Height in meters of whatever stands in this cell (building, wall, rubble).</summary>
        public float GetObstacleHeight(int index) => _obstacleHeight[index];
        public void SetObstacleHeight(int index, float value) => _obstacleHeight[index] = value;

        public CellFlags GetFlags(int index) => _flags[index];
        public void SetFlags(int index, CellFlags value) => _flags[index] = value;
        public bool HasFlag(int index, CellFlags flag) => (_flags[index] & flag) != 0;
        public void AddFlags(int index, CellFlags flags) => _flags[index] |= flags;
        public void RemoveFlags(int index, CellFlags flags) => _flags[index] &= ~flags;

        /// <summary>Index into <see cref="TacticalMap.Buildings"/>, or <see cref="NoBuilding"/>.</summary>
        public short GetBuildingIndex(int index) => _buildingIndex[index];
        public void SetBuildingIndex(int index, short value) => _buildingIndex[index] = value;

        /// <summary>Cover 0..100 for a unit occupying this cell.</summary>
        public byte GetCover(int index) => _cover[index];
        public void SetCover(int index, byte value) => _cover[index] = value;
    }
}
