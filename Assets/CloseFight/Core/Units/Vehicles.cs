using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace CloseFight.Core.Units
{
    public enum CrewRole : byte
    {
        None = 0,
        Commander = 1,
        Gunner = 2,
        Driver = 3,
        Loader = 4,
        Radio = 5,
    }

    public enum ArmorFacing : byte
    {
        Front = 0,
        Side = 1,
        Rear = 2,
    }

    /// <summary>Effective armor in mm per facing, slope already included (D37).</summary>
    public readonly struct ArmorProfile
    {
        public readonly float Front;
        public readonly float Side;
        public readonly float Rear;

        public ArmorProfile(float front, float side, float rear)
        {
            Front = front;
            Side = side;
            Rear = rear;
        }

        public float Of(ArmorFacing facing) => facing == ArmorFacing.Front ? Front : facing == ArmorFacing.Side ? Side : Rear;
    }

    /// <summary>
    /// A heavy gun: tank main gun, anti-tank gun or mortar. Penetration is
    /// armor-piercing performance against vertical plate at 100/500/1000 m.
    /// First-pass prototype numbers, close to period figures but tuned for play.
    /// </summary>
    public sealed class GunDefinition
    {
        public string Id { get; }
        public string NameKey { get; }
        public float Penetration100 { get; }
        public float Penetration500 { get; }
        public float Penetration1000 { get; }
        public float Accuracy { get; }
        public float Falloff { get; }
        public float ReloadSeconds { get; }
        public float MinRange { get; }
        public float MaxRange { get; }
        /// <summary>High-explosive blast: radius of likely casualties.</summary>
        public float HeLethalRadius { get; }
        /// <summary>High-explosive blast: radius of suppression.</summary>
        public float HeSuppressionRadius { get; }
        /// <summary>Damage to a building cell per HE hit (100 collapses it).</summary>
        public float StructureDamage { get; }
        public int StartingAp { get; }
        public int StartingHe { get; }
        public int StartingSmoke { get; }
        /// <summary>Degrees the gun can swing either side of its mount (180 = full turret).</summary>
        public float TraverseLimit { get; }
        /// <summary>Degrees per second the turret or gun swings.</summary>
        public float TraverseRate { get; }
        /// <summary>Mortars: lobbed shells, no line of sight needed from the gun itself.</summary>
        public bool Indirect { get; }
        /// <summary>Seconds to set up after moving before it can fire (towed guns, mortars).</summary>
        public float SetupSeconds { get; }

        public GunDefinition(string id, string nameKey, float pen100, float pen500, float pen1000, float accuracy, float falloff,
            float reloadSeconds, float minRange, float maxRange, float heLethalRadius, float heSuppressionRadius, float structureDamage,
            int startingAp, int startingHe, int startingSmoke, float traverseLimit, float traverseRate, bool indirect, float setupSeconds)
        {
            Id = id;
            NameKey = nameKey;
            Penetration100 = pen100;
            Penetration500 = pen500;
            Penetration1000 = pen1000;
            Accuracy = accuracy;
            Falloff = falloff;
            ReloadSeconds = reloadSeconds;
            MinRange = minRange;
            MaxRange = maxRange;
            HeLethalRadius = heLethalRadius;
            HeSuppressionRadius = heSuppressionRadius;
            StructureDamage = structureDamage;
            StartingAp = startingAp;
            StartingHe = startingHe;
            StartingSmoke = startingSmoke;
            TraverseLimit = traverseLimit;
            TraverseRate = traverseRate;
            Indirect = indirect;
            SetupSeconds = setupSeconds;
        }

        public bool CanPierce => Penetration100 > 0f;

        /// <summary>Armor penetration in mm at the given range, interpolated between the table points.</summary>
        public float PenetrationAt(float range)
        {
            if (range <= 100f)
            {
                return Penetration100;
            }

            if (range <= 500f)
            {
                return math.lerp(Penetration100, Penetration500, (range - 100f) / 400f);
            }

            float beyond = math.lerp(Penetration500, Penetration1000, (range - 500f) / 500f);
            return math.max(0f, beyond);
        }

        public float HitChanceAtRange(float range) => Accuracy * math.exp(-range / Falloff);
    }

    public sealed class VehicleDefinition
    {
        public string Id { get; }
        public string NameKey { get; }
        public ArmorProfile Armor { get; }
        public GunDefinition Gun { get; }
        /// <summary>Coaxial machine gun, fired by the gunner; null if none.</summary>
        public WeaponDefinition CoaxMg { get; }
        public CrewRole[] Crew { get; }
        /// <summary>Meters per second on roads / across open ground.</summary>
        public float RoadSpeed { get; }
        public float CrossSpeed { get; }
        /// <summary>Hull turn rate, degrees per second.</summary>
        public float TurnRate { get; }
        public float Length { get; }
        public float Width { get; }
        public bool HasTurret { get; }

        public VehicleDefinition(string id, string nameKey, ArmorProfile armor, GunDefinition gun, WeaponDefinition coaxMg, CrewRole[] crew,
            float roadSpeed, float crossSpeed, float turnRate, float length, float width, bool hasTurret)
        {
            Id = id;
            NameKey = nameKey;
            Armor = armor;
            Gun = gun;
            CoaxMg = coaxMg;
            Crew = crew;
            RoadSpeed = roadSpeed;
            CrossSpeed = crossSpeed;
            TurnRate = turnRate;
            Length = length;
            Width = width;
            HasTurret = hasTurret;
        }
    }

    /// <summary>Crew-served gun that is not a vehicle: anti-tank guns and mortars.</summary>
    public sealed class GunTeamDefinition
    {
        public string Id { get; }
        public string NameKey { get; }
        public GunDefinition Gun { get; }
        public int CrewSize { get; }
        /// <summary>Movement speed multiplier while moving the gun (towed guns are pushed by hand).</summary>
        public float SpeedFactor { get; }
        /// <summary>Anti-tank guns have a shield that protects the crew from the front.</summary>
        public bool HasShield { get; }

        public GunTeamDefinition(string id, string nameKey, GunDefinition gun, int crewSize, float speedFactor, bool hasShield)
        {
            Id = id;
            NameKey = nameKey;
            Gun = gun;
            CrewSize = crewSize;
            SpeedFactor = speedFactor;
            HasShield = hasShield;
        }
    }

    /// <summary>All vehicles and guns of the prototype (art-asset-list.md section 3), looked up by id from scenario files.</summary>
    public static class Equipment
    {
        // --- Guns
        public static readonly GunDefinition StuK40 = new GunDefinition("stuk40", "gun.stuk40",
            96f, 85f, 72f, accuracy: 0.6f, falloff: 900f, reloadSeconds: 6.5f, minRange: 0f, maxRange: 1500f,
            heLethalRadius: 3.5f, heSuppressionRadius: 12f, structureDamage: 40f, startingAp: 20, startingHe: 24, startingSmoke: 0,
            traverseLimit: 10f, traverseRate: 12f, indirect: false, setupSeconds: 0f);

        public static readonly GunDefinition KwK39 = new GunDefinition("kwk39", "gun.kwk39",
            67f, 57f, 44f, accuracy: 0.6f, falloff: 850f, reloadSeconds: 4.5f, minRange: 0f, maxRange: 1500f,
            heLethalRadius: 2.5f, heSuppressionRadius: 9f, structureDamage: 25f, startingAp: 40, startingHe: 44, startingSmoke: 0,
            traverseLimit: 180f, traverseRate: 25f, indirect: false, setupSeconds: 0f);

        public static readonly GunDefinition F34 = new GunDefinition("f34", "gun.f34",
            75f, 65f, 55f, accuracy: 0.5f, falloff: 800f, reloadSeconds: 7f, minRange: 0f, maxRange: 1500f,
            heLethalRadius: 3.5f, heSuppressionRadius: 12f, structureDamage: 40f, startingAp: 19, startingHe: 58, startingSmoke: 0,
            traverseLimit: 180f, traverseRate: 22f, indirect: false, setupSeconds: 0f);

        public static readonly GunDefinition TNSh = new GunDefinition("tnsh20", "gun.tnsh20",
            35f, 25f, 15f, accuracy: 0.5f, falloff: 500f, reloadSeconds: 1.5f, minRange: 0f, maxRange: 1000f,
            heLethalRadius: 0.8f, heSuppressionRadius: 4f, structureDamage: 4f, startingAp: 300, startingHe: 400, startingSmoke: 0,
            traverseLimit: 180f, traverseRate: 30f, indirect: false, setupSeconds: 0f);

        public static readonly GunDefinition Pak38 = new GunDefinition("pak38", "gun.pak38",
            72f, 61f, 50f, accuracy: 0.65f, falloff: 1000f, reloadSeconds: 4f, minRange: 0f, maxRange: 1500f,
            heLethalRadius: 1.5f, heSuppressionRadius: 6f, structureDamage: 12f, startingAp: 30, startingHe: 20, startingSmoke: 0,
            traverseLimit: 32f, traverseRate: 15f, indirect: false, setupSeconds: 10f);

        public static readonly GunDefinition M1937 = new GunDefinition("m1937_45", "gun.m1937_45",
            51f, 43f, 35f, accuracy: 0.6f, falloff: 900f, reloadSeconds: 3.5f, minRange: 0f, maxRange: 1200f,
            heLethalRadius: 1.8f, heSuppressionRadius: 7f, structureDamage: 14f, startingAp: 40, startingHe: 30, startingSmoke: 0,
            traverseLimit: 30f, traverseRate: 15f, indirect: false, setupSeconds: 10f);

        public static readonly GunDefinition GrW34 = new GunDefinition("grw34", "gun.grw34",
            0f, 0f, 0f, accuracy: 1f, falloff: 1e6f, reloadSeconds: 4f, minRange: 60f, maxRange: 2400f,
            heLethalRadius: 4f, heSuppressionRadius: 15f, structureDamage: 35f, startingAp: 0, startingHe: 30, startingSmoke: 6,
            traverseLimit: 180f, traverseRate: 360f, indirect: true, setupSeconds: 15f);

        public static readonly GunDefinition BM37 = new GunDefinition("bm37", "gun.bm37",
            0f, 0f, 0f, accuracy: 1f, falloff: 1e6f, reloadSeconds: 4f, minRange: 60f, maxRange: 3000f,
            heLethalRadius: 4f, heSuppressionRadius: 15f, structureDamage: 35f, startingAp: 0, startingHe: 30, startingSmoke: 6,
            traverseLimit: 180f, traverseRate: 360f, indirect: true, setupSeconds: 15f);

        static readonly CrewRole[] Crew5 = { CrewRole.Commander, CrewRole.Gunner, CrewRole.Driver, CrewRole.Loader, CrewRole.Radio };
        static readonly CrewRole[] Crew4 = { CrewRole.Commander, CrewRole.Gunner, CrewRole.Driver, CrewRole.Loader };
        static readonly CrewRole[] Crew2 = { CrewRole.Commander, CrewRole.Driver };

        // --- Vehicles
        public static readonly VehicleDefinition StuG3F = new VehicleDefinition("stug3f", "vehicle.stug3f",
            new ArmorProfile(80f, 30f, 30f), StuK40, null, Crew4,
            roadSpeed: 8f, crossSpeed: 4.5f, turnRate: 30f, length: 6.4f, width: 2.9f, hasTurret: false);

        public static readonly VehicleDefinition PzIIIJ = new VehicleDefinition("pz3j", "vehicle.pz3j",
            new ArmorProfile(50f, 30f, 50f), KwK39, Weapons.Mg34Coax, Crew5,
            roadSpeed: 8f, crossSpeed: 4.5f, turnRate: 32f, length: 6.4f, width: 2.9f, hasTurret: true);

        public static readonly VehicleDefinition T34 = new VehicleDefinition("t34_76", "vehicle.t34_76",
            new ArmorProfile(90f, 50f, 45f), F34, Weapons.DtCoax, Crew4,
            roadSpeed: 9f, crossSpeed: 6f, turnRate: 30f, length: 6.7f, width: 3.0f, hasTurret: true);

        public static readonly VehicleDefinition T60 = new VehicleDefinition("t60", "vehicle.t60",
            new ArmorProfile(40f, 25f, 25f), TNSh, Weapons.DtCoax, Crew2,
            roadSpeed: 9f, crossSpeed: 5.5f, turnRate: 40f, length: 4.1f, width: 2.4f, hasTurret: true);

        // --- Gun teams
        public static readonly GunTeamDefinition Pak38Team = new GunTeamDefinition("pak38", "gun.pak38", Pak38, 4, 0.35f, true);
        public static readonly GunTeamDefinition M1937Team = new GunTeamDefinition("m1937_45", "gun.m1937_45", M1937, 4, 0.35f, true);
        public static readonly GunTeamDefinition GrW34Team = new GunTeamDefinition("grw34", "gun.grw34", GrW34, 3, 0.8f, false);
        public static readonly GunTeamDefinition BM37Team = new GunTeamDefinition("bm37", "gun.bm37", BM37, 3, 0.8f, false);

        static readonly Dictionary<string, VehicleDefinition> VehiclesById = new Dictionary<string, VehicleDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            { StuG3F.Id, StuG3F }, { PzIIIJ.Id, PzIIIJ }, { T34.Id, T34 }, { T60.Id, T60 },
        };

        static readonly Dictionary<string, GunTeamDefinition> GunTeamsById = new Dictionary<string, GunTeamDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            { Pak38Team.Id, Pak38Team }, { M1937Team.Id, M1937Team }, { GrW34Team.Id, GrW34Team }, { BM37Team.Id, BM37Team },
        };

        public static VehicleDefinition Vehicle(string id)
        {
            if (string.IsNullOrEmpty(id) || !VehiclesById.TryGetValue(id, out var definition))
            {
                throw new ArgumentException($"Unknown vehicle '{id}'.");
            }

            return definition;
        }

        public static GunTeamDefinition GunTeam(string id)
        {
            if (string.IsNullOrEmpty(id) || !GunTeamsById.TryGetValue(id, out var definition))
            {
                throw new ArgumentException($"Unknown gun '{id}'.");
            }

            return definition;
        }
    }
}
