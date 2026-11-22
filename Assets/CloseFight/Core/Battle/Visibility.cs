using System.Collections.Generic;
using CloseFight.Core.Map;
using CloseFight.Core.Sim;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.Battle
{
    /// <summary>A rough "I heard something over there" marker for a side that cannot see the source (D32).</summary>
    public readonly struct SoundMarker
    {
        public readonly Side Listener;
        public readonly float2 Position;
        public readonly float CreatedAt;
        public readonly float ExpiresAt;

        public SoundMarker(Side listener, float2 position, float createdAt, float expiresAt)
        {
            Listener = listener;
            Position = position;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
        }
    }

    /// <summary>
    /// Fog of war. Each side sees an enemy soldier when at least one of its own
    /// soldiers has line of sight to him and he is within spotting range.
    /// What one soldier sees, the whole side sees (as in Close Combat).
    /// Enemies out of sight simply vanish (D32); firing that cannot be seen
    /// leaves a fuzzy sound marker instead. The AI uses exactly the same
    /// information as the player (D17).
    /// </summary>
    public sealed class Visibility
    {
        /// <summary>Recompute every N ticks (0.5 s) rather than every tick.</summary>
        public const int UpdateIntervalTicks = 5;
        /// <summary>Daylight limit of sight in meters.</summary>
        public const float MaxSightRange = 500f;
        public const float SoundMarkerLifetime = 8f;

        readonly BattleState _battle;
        readonly Dictionary<Side, HashSet<int>> _visible = new Dictionary<Side, HashSet<int>>
        {
            { Side.German, new HashSet<int>() },
            { Side.Soviet, new HashSet<int>() },
        };

        readonly List<SoundMarker> _soundMarkers = new List<SoundMarker>();

        public IReadOnlyList<SoundMarker> SoundMarkers => _soundMarkers;

        public Visibility(BattleState battle)
        {
            _battle = battle;
        }

        /// <summary>True if <paramref name="side"/> currently sees <paramref name="soldier"/>. Own soldiers are always visible.</summary>
        public bool CanSee(Side side, Soldier soldier)
        {
            return soldier.Team.Side == side || _visible[side].Contains(soldier.Id);
        }

        public int VisibleEnemyCount(Side side) => _visible[side].Count;

        /// <summary>
        /// How far away a target can be spotted, before line of sight is checked.
        /// Lying still in cover is the hardest to spot; running upright the easiest.
        /// </summary>
        public static float SpottingRange(Soldier target, TacticalMap map)
        {
            float range;
            switch (target.Posture)
            {
                case Posture.Prone: range = 120f; break;
                case Posture.Kneeling: range = 250f; break;
                default: range = 400f; break;
            }

            if (target.IsMoving)
            {
                range *= 1.5f;
            }

            switch (target.Team.Order.Type)
            {
                case OrderType.Hide: range *= 0.4f; break;
                case OrderType.Ambush: range *= 0.5f; break;
            }

            // Inside an intact building: dark window openings.
            var cell = map.Grid.WorldToCell(target.Position);
            if (map.Grid.InBounds(cell))
            {
                int index = map.Grid.IndexOf(cell);
                if (map.BuildingAt(index) != null && !map.Grid.HasFlag(index, CellFlags.Collapsed))
                {
                    range *= 0.6f;
                }
            }

            return math.min(range, MaxSightRange);
        }

        /// <summary>Recomputes what both sides can see.</summary>
        public void Recompute()
        {
            Recompute(Side.German);
            Recompute(Side.Soviet);
        }

        void Recompute(Side side)
        {
            var visible = _visible[side];
            visible.Clear();
            var map = _battle.Map;

            foreach (var target in _battle.Soldiers)
            {
                if (target.Team.Side == side || !target.IsActive)
                {
                    continue;
                }

                float range = SpottingRange(target, map);
                foreach (var observer in _battle.Soldiers)
                {
                    if (observer.Team.Side != side || !observer.IsActive)
                    {
                        continue;
                    }

                    if (math.distancesq(observer.Position, target.Position) > range * range)
                    {
                        continue;
                    }

                    var sight = LineOfSight.Check(map, observer.Position, observer.Posture, target.Position, target.Posture, range);
                    if (sight.Visible)
                    {
                        visible.Add(target.Id);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// A noise (gunfire, an explosion) at <paramref name="position"/> made by
        /// <paramref name="source"/>. Every other side that cannot see the source
        /// and is within earshot gets a marker, offset randomly so it only gives
        /// a rough direction.
        /// </summary>
        public void ReportSound(Soldier source, float2 position, float audibleRange, float fuzz)
        {
            float now = _battle.Clock.ElapsedSeconds;
            foreach (Side listener in new[] { Side.German, Side.Soviet })
            {
                if (listener == source.Team.Side || CanSee(listener, source) || !AnyoneWithin(listener, position, audibleRange))
                {
                    continue;
                }

                float angle = _battle.Random.Range(0f, math.PI * 2f);
                float distance = _battle.Random.Range(0f, fuzz);
                float2 offset = new float2(math.cos(angle), math.sin(angle)) * distance;
                _soundMarkers.Add(new SoundMarker(listener, position + offset, now, now + SoundMarkerLifetime));
            }
        }

        bool AnyoneWithin(Side side, float2 position, float range)
        {
            foreach (var soldier in _battle.Soldiers)
            {
                if (soldier.Team.Side == side && soldier.IsActive && math.distancesq(soldier.Position, position) <= range * range)
                {
                    return true;
                }
            }

            return false;
        }

        public void ExpireSoundMarkers()
        {
            float now = _battle.Clock.ElapsedSeconds;
            _soundMarkers.RemoveAll(m => m.ExpiresAt <= now);
        }
    }
}
