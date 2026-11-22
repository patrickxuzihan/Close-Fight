using System.Collections.Generic;
using CloseFight.Core.Path;
using Unity.Mathematics;

namespace CloseFight.Core.Units
{
    /// <summary>The order a team is currently carrying out.</summary>
    public readonly struct TeamOrder
    {
        public readonly OrderType Type;
        public readonly float2 Target;

        public TeamOrder(OrderType type, float2 target)
        {
            Type = type;
            Target = target;
        }

        public static TeamOrder None => new TeamOrder(OrderType.None, float2.zero);
    }

    /// <summary>
    /// A team: the unit the player commands (rifle team, MG team, HQ).
    /// The leader is the first active member flagged as leader; if he falls,
    /// the next active member takes over.
    /// </summary>
    public sealed class Team
    {
        public int Id { get; }
        /// <summary>Stable key from the scenario file, e.g. "de_1".</summary>
        public string Key { get; }
        public Side Side { get; }
        public TeamType Type { get; }
        public string DisplayName { get; }
        public List<Soldier> Members { get; } = new List<Soldier>();

        public TeamOrder Order { get; internal set; } = TeamOrder.None;
        public TeamActivity Activity { get; internal set; } = TeamActivity.Idle;
        /// <summary>Compass heading the team faces when it stops.</summary>
        public float Facing { get; internal set; }

        // Movement state, owned by TeamMovement.
        internal readonly List<float2> Path = new List<float2>();
        internal readonly List<float> PathCumulative = new List<float>();
        internal float PathProgress;
        internal readonly List<float2> DeploySlots = new List<float2>();

        /// <summary>Remaining route (world points) while moving; empty otherwise.</summary>
        public IReadOnlyList<float2> CurrentPath => Path;
        public float PathLength => PathCumulative.Count == 0 ? 0f : PathCumulative[PathCumulative.Count - 1];
        public float RemainingDistance => math.max(0f, PathLength - PathProgress);

        /// <summary>Fills <paramref name="output"/> with the route still ahead of the leader (empty when not moving).</summary>
        public void GetRemainingPath(List<float2> output)
        {
            output.Clear();
            if (Path.Count < 2)
            {
                return;
            }

            output.Add(PathUtils.PointAt(Path, PathCumulative, PathProgress, out _));
            for (int i = 1; i < Path.Count; i++)
            {
                if (PathCumulative[i] > PathProgress)
                {
                    output.Add(Path[i]);
                }
            }
        }

        public Team(int id, string key, Side side, TeamType type, string displayName)
        {
            Id = id;
            Key = key;
            Side = side;
            Type = type;
            DisplayName = displayName;
        }

        public Soldier Leader
        {
            get
            {
                foreach (var member in Members)
                {
                    if (member.IsLeader && member.IsActive)
                    {
                        return member;
                    }
                }

                foreach (var member in Members)
                {
                    if (member.IsActive)
                    {
                        return member;
                    }
                }

                return null;
            }
        }

        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (var member in Members)
                {
                    if (member.IsActive)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public float2 Centroid
        {
            get
            {
                float2 sum = float2.zero;
                int count = 0;
                foreach (var member in Members)
                {
                    if (member.IsActive)
                    {
                        sum += member.Position;
                        count++;
                    }
                }

                return count == 0 ? float2.zero : sum / count;
            }
        }
    }
}
