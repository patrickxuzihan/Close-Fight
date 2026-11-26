using Unity.Mathematics;

namespace CloseFight.Core.Units
{
    /// <summary>How long a team handed to the AI holds its ground (D22).</summary>
    public enum Stance : byte
    {
        /// <summary>Give ground when pressed hard: asks to pull back when shaken, outnumbered up close or flanked.</summary>
        Elastic = 0,
        /// <summary>Delaying action: fire, then fall back as soon as the enemy gets close or the team takes losses.</summary>
        Delay = 1,
        /// <summary>Hold at all costs: never asks to pull back; counterattacks when the enemy closes in.</summary>
        Hold = 2,
    }

    /// <summary>What a team handed to the AI is doing (M15).</summary>
    public enum DelegationState : byte
    {
        /// <summary>Moving to the covered spot it chose.</summary>
        TakingPosition = 0,
        /// <summary>In position, waiting in ambush or fighting.</summary>
        Holding = 1,
        /// <summary>Asked to pull back; goes when the player approves or when the time runs out (D53).</summary>
        RequestingWithdrawal = 2,
        /// <summary>Falling back to its prepared position, where it forms a new line.</summary>
        Withdrawing = 3,
        /// <summary>Cornered: charging the enemy that broke in (D22).</summary>
        Counterattacking = 4,
    }

    /// <summary>
    /// A team handed to the AI (M15, D14/D22/D53): it defends where the player
    /// left it, choosing cover and a fallback position itself, and gives ground
    /// according to its stance. Any order from the player takes it back.
    /// </summary>
    public sealed class Delegation
    {
        /// <summary>How long a request to pull back waits for the player before it goes ahead (D53).</summary>
        public const float RequestSeconds = 20f;

        public Stance Stance { get; internal set; }
        public DelegationState State { get; internal set; }
        public float StateSince { get; internal set; }

        /// <summary>Where the team holds: the covered spot it chose near the point it was given.</summary>
        public float2 Position { get; internal set; }

        /// <summary>The prepared position behind, away from the enemy: where it falls back to and forms a new line.</summary>
        public float2 Fallback { get; internal set; }
        public bool HasFallback { get; internal set; }

        /// <summary>When a request to pull back goes ahead unless the player refuses it.</summary>
        public float RequestDeadline { get; internal set; } = float.NaN;

        /// <summary>When the player last refused a request: the team holds on for a while before asking again.</summary>
        public float RefusedAt { get; internal set; } = float.NegativeInfinity;

        public float LastCounterattackAt { get; internal set; } = float.NegativeInfinity;
        public float LastRepositionAt { get; internal set; } = float.NegativeInfinity;

        /// <summary>Since when the team has been under heavy fire without a break (NaN: not now).</summary>
        public float PressedSince { get; internal set; } = float.NaN;

        /// <summary>How many times it has fallen back and formed a new line.</summary>
        public int Withdrawals { get; internal set; }

        /// <summary>The move to <see cref="Position"/> has been ordered (once: if it stops short, the team holds where it is).</summary>
        internal bool MoveIssued;

        public bool IsRequesting => State == DelegationState.RequestingWithdrawal;

        /// <summary>Seconds until a pending request goes ahead.</summary>
        public float SecondsToWithdraw(float now) => IsRequesting ? math.max(0f, RequestDeadline - now) : 0f;

        internal Delegation(Stance stance, float now)
        {
            Stance = stance;
            StateSince = now;
        }
    }
}
