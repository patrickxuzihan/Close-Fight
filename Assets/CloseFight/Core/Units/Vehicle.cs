using System.Collections.Generic;
using Unity.Mathematics;

namespace CloseFight.Core.Units
{
    /// <summary>
    /// A tank or assault gun on the battlefield (M6). The crew are ordinary
    /// <see cref="Soldier"/>s with crew roles, riding inside until they bail out.
    /// </summary>
    public sealed class Vehicle
    {
        public int Id { get; }
        public VehicleDefinition Definition { get; }
        public Team Team { get; }
        public HeavyWeapon Gun { get; internal set; }

        public float2 Position { get; internal set; }
        public float2 PreviousPosition { get; internal set; }
        /// <summary>Hull compass heading in degrees.</summary>
        public float HullHeading { get; internal set; }
        public float PreviousHullHeading { get; internal set; }
        public bool IsMoving { get; internal set; }
        public bool Reversing { get; internal set; }

        public bool Immobilized { get; internal set; }
        public bool GunDamaged { get; internal set; }
        /// <summary>Penetrated and burning: a total loss.</summary>
        public bool Destroyed { get; internal set; }
        /// <summary>The crew bailed out; the hull is left standing.</summary>
        public bool Abandoned { get; internal set; }
        public float LastFiredAt { get; internal set; } = float.NegativeInfinity;

        // Route (owned by VehicleMovement).
        internal readonly List<float2> Path = new List<float2>();
        internal readonly List<float> PathCumulative = new List<float>();
        internal float PathProgress;

        public Vehicle(int id, VehicleDefinition definition, Team team)
        {
            Id = id;
            Definition = definition;
            Team = team;
        }

        /// <summary>Still a fighting vehicle: neither knocked out nor abandoned.</summary>
        public bool IsOperational => !Destroyed && !Abandoned;

        public IReadOnlyList<float2> CurrentPath => Path;

        internal void SetPose(float2 position, float heading)
        {
            Position = position;
            PreviousPosition = position;
            HullHeading = heading;
            PreviousHullHeading = heading;
        }

        /// <summary>Crew member in a role who is alive, aboard and still under control, or null.</summary>
        public Soldier CrewIn(CrewRole role)
        {
            foreach (var member in Team.Members)
            {
                if (member.Role == role && member.IsActive && member.IsMounted && !member.IsOutOfControl)
                {
                    return member;
                }
            }

            return null;
        }

        public int CrewAboard
        {
            get
            {
                int count = 0;
                foreach (var member in Team.Members)
                {
                    if (member.IsActive && member.IsMounted)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }

    /// <summary>
    /// A heavy gun in action: a vehicle's main gun, a towed anti-tank gun or a
    /// mortar. Tracks where it points, its ammunition and its reload.
    /// </summary>
    public sealed class HeavyWeapon
    {
        public GunDefinition Definition { get; }
        public Team Team { get; }
        /// <summary>Set for a vehicle's main gun; null for towed guns and mortars.</summary>
        public Vehicle Vehicle { get; }

        /// <summary>Compass heading the barrel points to.</summary>
        public float Heading { get; internal set; }
        public float PreviousHeading { get; internal set; }
        public int AmmoAp { get; internal set; }
        public int AmmoHe { get; internal set; }
        public int AmmoSmoke { get; internal set; }
        public bool Destroyed { get; internal set; }
        internal float Reload;
        internal float SetupRemaining;
        /// <summary>A Smoke order waiting to be fired (mortars).</summary>
        internal bool SmokePending;
        internal float2 SmokeTarget;
        /// <summary>Where a towed gun or mortar stands (it moves with its crew).</summary>
        internal float2 Emplacement;

        public HeavyWeapon(GunDefinition definition, Team team, Vehicle vehicle)
        {
            Definition = definition;
            Team = team;
            Vehicle = vehicle;
            AmmoAp = definition.StartingAp;
            AmmoHe = definition.StartingHe;
            AmmoSmoke = definition.StartingSmoke;
        }

        public bool IsReady => SetupRemaining <= 0f && Reload <= 0f;

        /// <summary>Who serves the gun: the gunner, or another crewman standing in for him (slower).</summary>
        public Soldier Operator
        {
            get
            {
                Soldier fallback = null;
                foreach (var member in Team.Members)
                {
                    if (!member.IsActive || member.IsOutOfControl)
                    {
                        continue;
                    }

                    if (Vehicle != null && !member.IsMounted)
                    {
                        continue;
                    }

                    if (member.Role == CrewRole.Gunner)
                    {
                        return member;
                    }

                    if (fallback == null && member.Role != CrewRole.Driver)
                    {
                        fallback = member;
                    }
                }

                return fallback;
            }
        }

        public float2 Position
        {
            get
            {
                if (Vehicle != null)
                {
                    return Vehicle.Position;
                }

                return Emplacement;
            }
        }
    }
}
