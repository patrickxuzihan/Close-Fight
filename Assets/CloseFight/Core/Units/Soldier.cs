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

        // Combat (M4)
        public WeaponDefinition Weapon { get; internal set; }
        public int Ammo { get; internal set; }
        /// <summary>Stress from incoming fire, 0..100. Drives pinning now and the full psychology model in M5.</summary>
        public float Suppression { get; internal set; }
        internal float FireCooldown;
        /// <summary>Battle time of this soldier's last shot (muzzle flash makes him easier to spot).</summary>
        public float LastFiredAt { get; internal set; } = float.NegativeInfinity;
        /// <summary>Battle time when rounds last landed near him.</summary>
        public float LastUnderFireAt { get; internal set; } = float.NegativeInfinity;

        // Psychology (M5)
        /// <summary>Long-term nerve, 0..100. Starts at the Morale attribute; casualties and fire erode it.</summary>
        public float Morale { get; internal set; }
        public float BaseMorale { get; internal set; }
        public MoraleState MoraleState { get; internal set; } = MoraleState.Steady;
        internal readonly System.Collections.Generic.List<float2> FleePath = new System.Collections.Generic.List<float2>();
        internal readonly System.Collections.Generic.List<float> FleeCumulative = new System.Collections.Generic.List<float>();
        internal float FleeProgress;
        internal float2 PanicSpot;

        /// <summary>Cowering, panicking or routing: deaf to orders until he recovers.</summary>
        public bool IsOutOfControl => MoraleState >= MoraleState.Cowering;

        /// <summary>Suppression at which a soldier hugs the ground and stops moving and firing.</summary>
        public const float PinnedThreshold = 70f;
        public bool IsPinned => Suppression >= PinnedThreshold;
        public bool IsWounded => Status == SoldierStatus.Wounded;

        // Record (M8 after-action report)
        /// <summary>Enemy soldiers this man killed or put out of action.</summary>
        public int Kills { get; internal set; }
        public int VehiclesKnockedOut { get; internal set; }

        // Crew (M6)
        public CrewRole Role { get; internal set; }
        /// <summary>The vehicle he is riding in, while it has not been abandoned.</summary>
        public Vehicle MountedIn { get; internal set; }
        public bool IsMounted => MountedIn != null;

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
