using Unity.Mathematics;

namespace CloseFight.Core.Units
{
    public enum WeaponClass : byte
    {
        Rifle = 0,
        SubmachineGun = 1,
        MachineGun = 2,
        Pistol = 3,
        /// <summary>Tank and anti-tank guns (used for tracers and sounds).</summary>
        Cannon = 4,
        Mortar = 5,
    }

    /// <summary>
    /// Small-arms data. Numbers are first-pass prototype values, tuned for feel
    /// rather than ballistics; they all live here so balancing is one file.
    /// Combat hit rates are deliberately low: most fire suppresses rather than
    /// kills, so firefights last minutes, as in Close Combat.
    /// Hit chance per round = Accuracy * exp(-range / Falloff), before soldier,
    /// posture, cover and suppression modifiers (see Combat).
    /// </summary>
    public sealed class WeaponDefinition
    {
        public string Id { get; }
        public string NameKey { get; }
        public WeaponClass Class { get; }
        /// <summary>Rounds fired per trigger pull / burst.</summary>
        public int Burst { get; }
        /// <summary>Seconds between bursts (aiming, working the bolt, re-laying the gun).</summary>
        public float Interval { get; }
        /// <summary>Base hit chance per round at point-blank range.</summary>
        public float Accuracy { get; }
        /// <summary>Range in meters over which hit chance falls to ~37%.</summary>
        public float Falloff { get; }
        /// <summary>Beyond this range the soldier will not open fire on his own.</summary>
        public float EffectiveRange { get; }
        public float MaxRange { get; }
        public int StartingAmmo { get; }
        /// <summary>Suppression added to a soldier a round strikes near.</summary>
        public float Suppression { get; }
        /// <summary>Chance that a hit is fatal or disabling rather than a light wound.</summary>
        public float Lethality { get; }
        /// <summary>How far away the shots are heard (sound markers, D32).</summary>
        public float AudibleRange { get; }
        /// <summary>Machine guns must be set up: they cannot fire on the move.</summary>
        public bool NeedsSetup { get; }

        public WeaponDefinition(string id, string nameKey, WeaponClass weaponClass, int burst, float interval, float accuracy,
            float falloff, float effectiveRange, float maxRange, int startingAmmo, float suppression, float lethality,
            float audibleRange, bool needsSetup)
        {
            Id = id;
            NameKey = nameKey;
            Class = weaponClass;
            Burst = burst;
            Interval = interval;
            Accuracy = accuracy;
            Falloff = falloff;
            EffectiveRange = effectiveRange;
            MaxRange = maxRange;
            StartingAmmo = startingAmmo;
            Suppression = suppression;
            Lethality = lethality;
            AudibleRange = audibleRange;
            NeedsSetup = needsSetup;
        }

        public float HitChanceAtRange(float range) => Accuracy * math.exp(-range / Falloff);
    }

    public static class Weapons
    {
        // German
        public static readonly WeaponDefinition Kar98k = new WeaponDefinition("kar98k", "weapon.kar98k", WeaponClass.Rifle,
            burst: 1, interval: 3.5f, accuracy: 0.22f, falloff: 200f, effectiveRange: 400f, maxRange: 600f,
            startingAmmo: 60, suppression: 7f, lethality: 0.55f, audibleRange: 500f, needsSetup: false);

        public static readonly WeaponDefinition Mp40 = new WeaponDefinition("mp40", "weapon.mp40", WeaponClass.SubmachineGun,
            burst: 5, interval: 1.8f, accuracy: 0.18f, falloff: 45f, effectiveRange: 120f, maxRange: 200f,
            startingAmmo: 192, suppression: 4f, lethality: 0.40f, audibleRange: 350f, needsSetup: false);

        public static readonly WeaponDefinition Mg34 = new WeaponDefinition("mg34", "weapon.mg34", WeaponClass.MachineGun,
            burst: 8, interval: 1.4f, accuracy: 0.12f, falloff: 260f, effectiveRange: 700f, maxRange: 1000f,
            startingAmmo: 1000, suppression: 6f, lethality: 0.45f, audibleRange: 700f, needsSetup: true);

        // Soviet
        public static readonly WeaponDefinition Mosin = new WeaponDefinition("mosin", "weapon.mosin", WeaponClass.Rifle,
            burst: 1, interval: 3.7f, accuracy: 0.22f, falloff: 200f, effectiveRange: 400f, maxRange: 600f,
            startingAmmo: 60, suppression: 7f, lethality: 0.55f, audibleRange: 500f, needsSetup: false);

        public static readonly WeaponDefinition Ppsh41 = new WeaponDefinition("ppsh41", "weapon.ppsh41", WeaponClass.SubmachineGun,
            burst: 7, interval: 1.7f, accuracy: 0.16f, falloff: 40f, effectiveRange: 120f, maxRange: 200f,
            startingAmmo: 213, suppression: 4f, lethality: 0.40f, audibleRange: 350f, needsSetup: false);

        public static readonly WeaponDefinition Dp28 = new WeaponDefinition("dp28", "weapon.dp28", WeaponClass.MachineGun,
            burst: 6, interval: 1.5f, accuracy: 0.12f, falloff: 230f, effectiveRange: 600f, maxRange: 900f,
            startingAmmo: 600, suppression: 6f, lethality: 0.45f, audibleRange: 650f, needsSetup: true);

        // Vehicle-mounted machine guns: fixed mounts, so they may fire on the move.
        public static readonly WeaponDefinition Mg34Coax = new WeaponDefinition("mg34_coax", "weapon.mg34", WeaponClass.MachineGun,
            burst: 8, interval: 1.4f, accuracy: 0.12f, falloff: 260f, effectiveRange: 600f, maxRange: 900f,
            startingAmmo: 2000, suppression: 6f, lethality: 0.45f, audibleRange: 700f, needsSetup: false);

        public static readonly WeaponDefinition DtCoax = new WeaponDefinition("dt_coax", "weapon.dt", WeaponClass.MachineGun,
            burst: 6, interval: 1.5f, accuracy: 0.12f, falloff: 230f, effectiveRange: 600f, maxRange: 900f,
            startingAmmo: 1500, suppression: 6f, lethality: 0.45f, audibleRange: 650f, needsSetup: false);

        // Crew sidearms, for tank crews who bail out.
        public static readonly WeaponDefinition P08 = new WeaponDefinition("p08", "weapon.p08", WeaponClass.Pistol,
            burst: 1, interval: 2f, accuracy: 0.14f, falloff: 25f, effectiveRange: 40f, maxRange: 60f,
            startingAmmo: 24, suppression: 3f, lethality: 0.35f, audibleRange: 250f, needsSetup: false);

        public static readonly WeaponDefinition Tt33 = new WeaponDefinition("tt33", "weapon.tt33", WeaponClass.Pistol,
            burst: 1, interval: 2f, accuracy: 0.14f, falloff: 25f, effectiveRange: 40f, maxRange: 60f,
            startingAmmo: 24, suppression: 3f, lethality: 0.35f, audibleRange: 250f, needsSetup: false);

        /// <summary>Personal weapon of a vehicle or gun crewman (commanders get an SMG).</summary>
        public static WeaponDefinition CrewSidearm(Side side, bool commander)
        {
            if (commander)
            {
                return side == Side.German ? Mp40 : Ppsh41;
            }

            return side == Side.German ? P08 : Tt33;
        }

        /// <summary>Standard issue for the given seat in a team (index 0 is the leader).</summary>
        public static WeaponDefinition Issue(Side side, TeamType type, int index)
        {
            bool german = side == Side.German;
            switch (type)
            {
                case TeamType.MachineGun:
                    if (index == 0)
                    {
                        return german ? Mg34 : Dp28;
                    }

                    return german ? Kar98k : Mosin;

                case TeamType.Headquarters:
                    if (index == 0)
                    {
                        return german ? Mp40 : Ppsh41;
                    }

                    return german ? Kar98k : Mosin;

                default:
                    // German squad leaders carried the MP40; Soviet rifle sections often had two PPSh.
                    if (index == 0 || (!german && index == 1))
                    {
                        return german ? Mp40 : Ppsh41;
                    }

                    return german ? Kar98k : Mosin;
            }
        }
    }
}
