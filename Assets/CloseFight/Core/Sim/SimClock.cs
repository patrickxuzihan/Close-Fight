using System;

namespace CloseFight.Core.Sim
{
    /// <summary>
    /// Fixed-step battle clock: the simulation always advances in 0.1 s ticks,
    /// independent of frame rate. Pausing stops ticks; orders may still be given
    /// while paused (D34). No fast-forward for now (D34).
    /// </summary>
    public sealed class SimClock
    {
        public const float TickSeconds = 0.1f;
        const int MaxTicksPerUpdate = 5;

        float _accumulator;

        public bool Paused { get; set; }
        public long TickCount { get; private set; }
        public float ElapsedSeconds => TickCount * TickSeconds;

        /// <summary>How far (0..1) real time is between the last tick and the next; used to interpolate drawing.</summary>
        public float Alpha => Math.Min(_accumulator / TickSeconds, 1f);

        /// <summary>Adds real time and returns how many ticks should run now (the battle runs them and counts them).</summary>
        public int Advance(float realDeltaSeconds)
        {
            if (Paused || realDeltaSeconds <= 0f)
            {
                return 0;
            }

            _accumulator += realDeltaSeconds;
            int ticks = 0;
            while (_accumulator >= TickSeconds && ticks < MaxTicksPerUpdate)
            {
                _accumulator -= TickSeconds;
                ticks++;
            }

            // After a long hitch, drop the backlog instead of spiralling.
            if (ticks == MaxTicksPerUpdate && _accumulator > TickSeconds)
            {
                _accumulator = 0f;
            }

            return ticks;
        }

        /// <summary>Called once per simulation step by the battle; keeps time in step with the simulation.</summary>
        public void CountTick()
        {
            TickCount++;
        }
    }
}
