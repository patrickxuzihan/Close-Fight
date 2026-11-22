using CloseFight.Core.Map;
using Unity.Mathematics;

namespace CloseFight.Core.Units
{
    /// <summary>One named soldier (D30). Position is in map meters; heading is a compass angle in degrees (0 = north, 90 = east).</summary>
    public sealed class Soldier
    {
        public int Id { get; }
        public Team Team { get; internal set; }
        public string FirstName { get; }
        public string LastName { get; }
        /// <summary>Localization key of the rank, e.g. "rank.de.gefreiter".</summary>
        public string RankKey { get; internal set; }
        public bool IsLeader { get; internal set; }
        public SoldierAttributes Attributes;
        public SoldierStatus Status { get; internal set; } = SoldierStatus.Healthy;

        public float2 Position { get; internal set; }
        /// <summary>Position at the previous tick, for smooth drawing between ticks.</summary>
        public float2 PreviousPosition { get; internal set; }
        public float Heading { get; internal set; }
        public float PreviousHeading { get; internal set; }
        public Posture Posture { get; internal set; } = Posture.Standing;
        public bool IsMoving { get; internal set; }

        public bool IsActive => Status == SoldierStatus.Healthy || Status == SoldierStatus.Wounded;
        public string FullName => FirstName + " " + LastName;

        public Soldier(int id, string firstName, string lastName)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
        }

        internal void SetPosition(float2 position)
        {
            Position = position;
            PreviousPosition = position;
        }

        internal void SetHeading(float heading)
        {
            Heading = heading;
            PreviousHeading = heading;
        }

        /// <summary>Compass heading (degrees) of a direction vector in map space.</summary>
        public static float HeadingOf(float2 direction)
        {
            float degrees = math.degrees(math.atan2(direction.x, direction.y));
            return degrees < 0f ? degrees + 360f : degrees;
        }

        /// <summary>Unit vector for a compass heading in degrees.</summary>
        public static float2 DirectionOf(float heading)
        {
            float radians = math.radians(heading);
            return new float2(math.sin(radians), math.cos(radians));
        }
    }
}
