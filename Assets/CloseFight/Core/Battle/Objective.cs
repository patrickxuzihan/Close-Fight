using System;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.Battle
{
    /// <summary>A victory location as written in the scenario file.</summary>
    [Serializable]
    public sealed class ObjectiveDefinition
    {
        public string id = "";
        public string nameKey = "";
        public MapPoint position;
        public float radius = 12f;
        /// <summary>Points it is worth; also how hard the AI fights for it (D8).</summary>
        public int value = 1;
        public Side owner = Side.Soviet;
    }

    /// <summary>
    /// A victory location (Close Combat VL). M7: the defending AI decides which
    /// to hold hard and which to give up. M8 adds capture and scoring.
    /// </summary>
    public sealed class Objective
    {
        public string Id { get; }
        public string NameKey { get; }
        public float2 Position { get; }
        public float Radius { get; }
        public int Value { get; }
        public Side Owner { get; internal set; }

        public Objective(ObjectiveDefinition definition)
        {
            Id = definition.id;
            NameKey = definition.nameKey;
            Position = definition.position;
            Radius = definition.radius;
            Value = math.max(1, definition.value);
            Owner = definition.owner;
        }

        public bool Contains(float2 point) => math.distancesq(point, Position) <= Radius * Radius;
    }
}
