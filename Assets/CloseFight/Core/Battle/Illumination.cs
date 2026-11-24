using System.Collections.Generic;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.Battle
{
    public enum FlareRefusal : byte
    {
        None,
        /// <summary>By day there is nothing to light up.</summary>
        Daylight,
        NoFlaresLeft,
        /// <summary>No soldier of ours close enough to fire it.</summary>
        OutOfReach,
    }

    /// <summary>
    /// A parachute flare (D35): it hangs high, drifts with the wind and sinks
    /// as it burns, so its pool of light starts wide and shrinks, moving as it goes.
    /// </summary>
    public sealed class Flare
    {
        public const float BurnSeconds = 40f;
        public const float StartHeight = 70f;
        public const float EndHeight = 12f;

        public Side FiredBy { get; }
        public float2 LaunchPoint { get; }
        public float LaunchedAt { get; }
        public float2 Drift { get; }

        public Flare(Side firedBy, float2 point, float time, float2 drift)
        {
            FiredBy = firedBy;
            LaunchPoint = point;
            LaunchedAt = time;
            Drift = drift;
        }

        public float Age(float now) => now - LaunchedAt;
        public bool BurntOut(float now) => Age(now) >= BurnSeconds;
        public float2 Position(float now) => LaunchPoint + Drift * math.max(0f, Age(now));
        public float Height(float now) => math.lerp(StartHeight, EndHeight, math.saturate(Age(now) / BurnSeconds));
    }

    /// <summary>
    /// Night and flares (M14, D35). At night men are seen only close up, unless
    /// they fire (the muzzle flash carries) or stand in the light of a flare,
    /// where they are as plain to see as by day. Light from a flare falls off as
    /// a lamp's does: bright straight below, fading over a radius of about 1.2
    /// times its height. Each side has a few flares; a soldier fires one over a
    /// point within reach of his flare pistol.
    /// </summary>
    public sealed class Illumination
    {
        /// <summary>How far from our nearest man a flare can be put (a flare pistol's reach).</summary>
        public const float FlareReach = 200f;
        /// <summary>Light at which a man counts as fully lit.</summary>
        public const float LitThreshold = 0.35f;
        /// <summary>
        /// In the dark a man is seen at this share of his daylight range: about 100 m
        /// standing, 75 m kneeling, 50 m prone in the open (D35: 50–100 m).
        /// </summary>
        public const float DarkStanding = 0.25f;
        public const float DarkKneeling = 0.3f;
        public const float DarkProne = 0.42f;
        /// <summary>A muzzle flash shows this far in the dark.</summary>
        public const float MuzzleFlashRange = 350f;
        /// <summary>A tank in the dark: heard and glimpsed this close, unless it fires or stands in the light.</summary>
        public const float DarkVehicleRange = 150f;

        readonly BattleState _battle;
        readonly List<Flare> _flares = new List<Flare>();
        readonly int[] _left = new int[2];
        float2 _wind;

        public Illumination(BattleState battle)
        {
            _battle = battle;
        }

        public IReadOnlyList<Flare> Flares => _flares;
        public int FlaresLeft(Side side) => _left[(int)side];
        public void SetFlares(Side side, int count) => _left[(int)side] = math.max(0, count);

        /// <summary>The night wind the flares drift on (a gentle breeze, fixed for the battle).</summary>
        public float2 Wind
        {
            get
            {
                if (math.lengthsq(_wind) < 1e-6f)
                {
                    float angle = _battle.Random.Range(0f, math.PI * 2f);
                    _wind = new float2(math.cos(angle), math.sin(angle)) * _battle.Random.Range(0.6f, 1.4f);
                }

                return _wind;
            }
        }

        /// <summary>Light falling on a point, 0 (dark) to about 1.3 (right under a flare). 1 by day.</summary>
        public float LightAt(float2 point)
        {
            if (!_battle.IsNight)
            {
                return 1f;
            }

            float now = _battle.Clock.ElapsedSeconds;
            float best = 0f;
            foreach (var flare in _flares)
            {
                float h = flare.Height(now);
                float2 p = flare.Position(now);
                float d2 = math.distancesq(p, point) + h * h;
                float d = math.sqrt(d2);
                // A point source: inverse square, times the cosine of the angle of incidence.
                best = math.max(best, 1.3f * h * h * h / (d2 * d));
            }

            return best;
        }

        public bool IsLit(float2 point) => LightAt(point) >= LitThreshold;

        /// <summary>Fires a flare over <paramref name="point"/> for <paramref name="side"/>.</summary>
        public Flare Fire(Side side, float2 point, out FlareRefusal refusal)
        {
            refusal = FlareRefusal.None;
            if (!_battle.IsNight)
            {
                refusal = FlareRefusal.Daylight;
                return null;
            }

            if (_left[(int)side] <= 0)
            {
                refusal = FlareRefusal.NoFlaresLeft;
                return null;
            }

            if (!WithinReach(side, point))
            {
                refusal = FlareRefusal.OutOfReach;
                return null;
            }

            _left[(int)side]--;
            var flare = new Flare(side, point, _battle.Clock.ElapsedSeconds, Wind);
            _flares.Add(flare);
            return flare;
        }

        public bool WithinReach(Side side, float2 point)
        {
            foreach (var soldier in _battle.Soldiers)
            {
                if (soldier.Team.Side == side && soldier.IsActive && !soldier.IsMounted
                    && math.distance(soldier.Position, point) <= FlareReach)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Night spotting range for a vehicle: short in the dark, full in the light or when it fires.</summary>
        public float NightVehicleRange(float2 position, float dayRange, bool firing)
        {
            float light = math.saturate(LightAt(position) / LitThreshold);
            float range = math.lerp(math.min(DarkVehicleRange, dayRange), dayRange, light);
            return firing ? math.max(range, MuzzleFlashRange * 1.3f) : range;
        }

        public void Step(float now)
        {
            _flares.RemoveAll(f => f.BurntOut(now));
        }

        /// <summary>
        /// Night spotting range for a man whose day range is <paramref name="dayRange"/>:
        /// short in the dark, his day range in a flare's light, and far if he has just fired.
        /// </summary>
        public float NightRange(Soldier target, float dayRange, bool firing)
        {
            float share;
            switch (target.Posture)
            {
                case Posture.Prone: share = DarkProne; break;
                case Posture.Kneeling: share = DarkKneeling; break;
                default: share = DarkStanding; break;
            }

            // The day range already carries concealment, cover and movement; darkness scales it down.
            float light = math.saturate(LightAt(target.Position) / LitThreshold);
            float range = math.lerp(dayRange * share, dayRange, light);
            if (firing)
            {
                range = math.max(range, target.Team.Type == TeamType.Sniper ? MuzzleFlashRange * 0.5f : MuzzleFlashRange);
            }

            return range;
        }
    }
}
