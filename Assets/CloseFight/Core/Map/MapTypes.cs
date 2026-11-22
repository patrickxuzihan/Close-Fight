using System;
using Unity.Mathematics;

namespace CloseFight.Core.Map
{
    /// <summary>Ground type of one grid cell.</summary>
    public enum TerrainType : byte
    {
        Open = 0,
        Road = 1,
        Rubble = 2,
        Rail = 3,
        Grass = 4,
        Water = 5,
        BuildingFloor = 6,
        Crater = 7,
    }

    /// <summary>Construction material; drives cover, durability and whether tanks can crush it.</summary>
    public enum BuildingMaterial : byte
    {
        Wood = 0,
        Brick = 1,
        Concrete = 2,
    }

    [Flags]
    public enum CellFlags : byte
    {
        None = 0,
        BlocksMovement = 1 << 0,
        BlocksSight = 1 << 1,
        Building = 1 << 2,
        Collapsed = 1 << 3,
        Wall = 1 << 4,
    }

    public enum MovementClass : byte
    {
        Infantry = 0,
        Vehicle = 1,
    }

    /// <summary>Integer cell coordinate. X grows east, Y grows north.</summary>
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        public readonly int X;
        public readonly int Y;

        public GridCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridCoord other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridCoord other && Equals(other);
        public override int GetHashCode() => (X * 73856093) ^ (Y * 19349663);
        public override string ToString() => $"({X}, {Y})";

        public static bool operator ==(GridCoord a, GridCoord b) => a.Equals(b);
        public static bool operator !=(GridCoord a, GridCoord b) => !a.Equals(b);
    }

    /// <summary>
    /// A point in map space, in meters. Used in JSON map files instead of float2,
    /// because float2 exposes swizzle properties that would pollute the JSON.
    /// </summary>
    [Serializable]
    public struct MapPoint
    {
        public float x;
        public float y;

        public MapPoint(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static implicit operator float2(MapPoint p) => new float2(p.x, p.y);
        public static implicit operator MapPoint(float2 p) => new MapPoint(p.x, p.y);
    }
}
