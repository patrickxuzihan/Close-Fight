namespace CloseFight.Core.Sim
{
    /// <summary>
    /// Deterministic random number generator (PCG32).
    /// Every random roll in the simulation goes through one of these, so a battle
    /// replays identically from the same seed. Never use System.Random or
    /// UnityEngine.Random inside the simulation.
    /// </summary>
    public sealed class SimRandom
    {
        const ulong Multiplier = 6364136223846793005UL;

        ulong _state;
        readonly ulong _increment;

        public SimRandom(ulong seed, ulong stream = 54UL)
        {
            _increment = (stream << 1) | 1UL;
            _state = 0UL;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            ulong old = _state;
            _state = unchecked(old * Multiplier + _increment);
            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rotation = (int)(old >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }

        /// <summary>Uniform float in [0, 1).</summary>
        public float NextFloat()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            uint span = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % span);
        }

        /// <summary>Uniform float in [min, max).</summary>
        public float Range(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        /// <summary>True with the given probability (0..1).</summary>
        public bool Chance(float probability)
        {
            return NextFloat() < probability;
        }
    }
}
