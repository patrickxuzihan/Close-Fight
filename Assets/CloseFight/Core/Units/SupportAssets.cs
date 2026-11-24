using System;
using System.Collections.Generic;

namespace CloseFight.Core.Units
{
    /// <summary>When an off-map asset may fly or fire.</summary>
    public enum SupportTime : byte
    {
        Any,
        DayOnly,
        NightOnly,
    }

    /// <summary>
    /// An off-map support asset (D36): a battery firing a short barrage, a salvo
    /// of rockets, or aircraft dropping bombs. It costs support points, arrives
    /// after a delay, and its rounds scatter around the aiming point, so it can
    /// fall on friendly troops too close to the enemy.
    /// </summary>
    public sealed class SupportAsset
    {
        public string Id { get; }
        public string NameKey { get; }
        public Side Side { get; }
        public int Cost { get; }
        /// <summary>Seconds from the call to the first round: a random time in this range.</summary>
        public float DelayMin { get; }
        public float DelayMax { get; }
        public int Rounds { get; }
        /// <summary>Seconds between rounds (a rocket salvo lands almost at once).</summary>
        public float Interval { get; }
        /// <summary>Rounds land within this radius of the (already off-target) aim point.</summary>
        public float Spread { get; }
        /// <summary>Aiming error per metre between the observer and the target (the first round is rarely spot on).</summary>
        public float ErrorPerMeter { get; }
        public SupportTime Time { get; }
        /// <summary>Blast of one round: radius, suppression and damage to buildings.</summary>
        public GunDefinition Round { get; }
        public bool IsAircraft { get; }

        public SupportAsset(string id, string nameKey, Side side, int cost, float delayMin, float delayMax, int rounds, float interval,
            float spread, float errorPerMeter, SupportTime time, GunDefinition round, bool isAircraft)
        {
            Id = id;
            NameKey = nameKey;
            Side = side;
            Cost = cost;
            DelayMin = delayMin;
            DelayMax = delayMax;
            Rounds = rounds;
            Interval = interval;
            Spread = spread;
            ErrorPerMeter = errorPerMeter;
            Time = time;
            Round = round;
            IsAircraft = isAircraft;
        }
    }

    /// <summary>The off-map support of September 1942 (D36). First-pass numbers.</summary>
    public static class SupportAssets
    {
        static GunDefinition Shell(string id, float lethal, float suppression, float structure) => new GunDefinition(id, "support." + id, 0f, 0f, 0f,
            accuracy: 1f, falloff: 1e6f, reloadSeconds: 0f, minRange: 0f, maxRange: 0f,
            heLethalRadius: lethal, heSuppressionRadius: lethal * 3f, structureDamage: structure,
            startingAp: 0, startingHe: 0, startingSmoke: 0, traverseLimit: 0f, traverseRate: 0f, indirect: true, setupSeconds: 0f);

        // German
        public static readonly SupportAsset Lefh18 = new SupportAsset("lefh18", "support.lefh18", Side.German, cost: 3,
            delayMin: 60f, delayMax: 150f, rounds: 8, interval: 3f, spread: 22f, errorPerMeter: 0.06f, SupportTime.Any,
            Shell("lefh18_round", 7f, 25f, 110f), isAircraft: false);

        public static readonly SupportAsset Sfh18 = new SupportAsset("sfh18", "support.sfh18", Side.German, cost: 5,
            delayMin: 75f, delayMax: 180f, rounds: 6, interval: 4f, spread: 28f, errorPerMeter: 0.07f, SupportTime.Any,
            Shell("sfh18_round", 11f, 35f, 220f), isAircraft: false);

        public static readonly SupportAsset Stuka = new SupportAsset("stuka", "support.stuka", Side.German, cost: 4,
            delayMin: 90f, delayMax: 180f, rounds: 2, interval: 1.5f, spread: 10f, errorPerMeter: 0.03f, SupportTime.DayOnly,
            Shell("stuka_bomb", 14f, 45f, 400f), isAircraft: true);

        // Soviet
        public static readonly SupportAsset Ml20 = new SupportAsset("ml20", "support.ml20", Side.Soviet, cost: 5,
            delayMin: 75f, delayMax: 180f, rounds: 6, interval: 4f, spread: 32f, errorPerMeter: 0.08f, SupportTime.Any,
            Shell("ml20_round", 11f, 35f, 220f), isAircraft: false);

        public static readonly SupportAsset Katyusha = new SupportAsset("katyusha", "support.katyusha", Side.Soviet, cost: 6,
            delayMin: 60f, delayMax: 150f, rounds: 16, interval: 0.25f, spread: 45f, errorPerMeter: 0.08f, SupportTime.Any,
            Shell("katyusha_rocket", 8f, 28f, 110f), isAircraft: false);

        public static readonly SupportAsset Po2 = new SupportAsset("po2", "support.po2", Side.Soviet, cost: 2,
            delayMin: 90f, delayMax: 180f, rounds: 2, interval: 2f, spread: 25f, errorPerMeter: 0.08f, SupportTime.NightOnly,
            Shell("po2_bomb", 7f, 22f, 100f), isAircraft: true);

        static readonly Dictionary<string, SupportAsset> ById = new Dictionary<string, SupportAsset>(StringComparer.OrdinalIgnoreCase)
        {
            { Lefh18.Id, Lefh18 }, { Sfh18.Id, Sfh18 }, { Stuka.Id, Stuka },
            { Ml20.Id, Ml20 }, { Katyusha.Id, Katyusha }, { Po2.Id, Po2 },
        };

        public static SupportAsset Get(string id) => id != null && ById.TryGetValue(id, out var asset) ? asset : null;
    }
}
