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

        /// <summary>
        /// Fire discipline for rifles and MGs: hold fire until an enemy is this close.
        /// Infinite means fire at will. The AI uses it to keep ambushes hidden while
        /// moving into position (M7).
        /// </summary>
        public float SmallArmsHoldRange { get; internal set; } = float.PositiveInfinity;

        /// <summary>Enemy team named by a Fire order (null: fire at will).</summary>
        public Team FireTarget { get; internal set; }
        public int SmokeGrenades { get; internal set; }

        // Heavy equipment (M6)
        /// <summary>The team's vehicle (tank or assault gun); stays set after it is knocked out or abandoned.</summary>
        public Vehicle Vehicle { get; internal set; }
        /// <summary>The team's heavy gun: a vehicle's main gun, an anti-tank gun or a mortar.</summary>
        public HeavyWeapon Gun { get; internal set; }
        public GunTeamDefinition GunTeam { get; internal set; }
        /// <summary>Bundled grenades / anti-tank grenades for close assault on tanks (D37).</summary>
        public int AntiTankGrenades { get; internal set; }
        /// <summary>Engineers: demolition charges left (M12).</summary>
        public int DemolitionCharges { get; internal set; }
        /// <summary>Engineers: minefields they can still lay (M12).</summary>
        public int MineLoads { get; internal set; }
        /// <summary>Engineering work in progress (demolition, laying or clearing mines), or null.</summary>
        public Battle.EngineerTask Task { get; internal set; }
        internal float CloseAssaultCooldown;

        /// <summary>True while the team fights as a vehicle (not knocked out or abandoned).</summary>
        public bool IsMechanized => Vehicle != null && Vehicle.IsOperational;

        // Team condition (M5, D14). Recomputed every tick by Psychology.
        /// <summary>Casualties as a fraction of the team: the fallen count fully, the wounded half.</summary>
        public float CasualtyFraction { get; internal set; }
        public float AverageMorale { get; internal set; } = 100f;
        /// <summary>Half the team down or morale low: asks to pull back and will not advance.</summary>
        public bool IsShaken { get; internal set; }
        /// <summary>Two thirds down or morale collapsed: falls back on its own and forms a new line.</summary>
        public bool IsBroken { get; internal set; }
        /// <summary>Battle time the original leader fell (NaN: still standing).</summary>
        public float LeaderLostAt { get; internal set; } = float.NaN;
        public OrderRefusal LastRefusal { get; internal set; }
        public float LastRefusalAt { get; internal set; } = float.NegativeInfinity;
        internal bool FallingBack;
        internal bool HasFallenBack;

        public bool LeaderLost => !float.IsNaN(LeaderLostAt);

        /// <summary>True if any member fired within the last couple of seconds.</summary>
        public bool IsFiring(float now)
        {
            foreach (var member in Members)
            {
                if (member.IsActive && now - member.LastFiredAt < 2f)
                {
                    return true;
                }
            }

            return false;
        }

        public int TotalAmmo
        {
            get
            {
                int total = 0;
                foreach (var member in Members)
                {
                    if (member.IsActive)
                    {
                        total += member.Ammo;
                    }
                }

                return total;
            }
        }

        public int StartingAmmo
        {
            get
            {
                int total = 0;
                foreach (var member in Members)
                {
                    if (member.IsActive && member.Weapon != null)
                    {
                        total += member.Weapon.StartingAmmo;
                    }
                }

                return total;
            }
        }

        /// <summary>Average suppression of active members, 0..100.</summary>
        public float AverageSuppression
        {
            get
            {
                float sum = 0f;
                int count = 0;
                foreach (var member in Members)
                {
                    if (member.IsActive)
                    {
                        sum += member.Suppression;
                        count++;
                    }
                }

                return count == 0 ? 0f : sum / count;
            }
        }

        // Movement state, owned by TeamMovement.
        internal readonly List<float2> Path = new List<float2>();
        internal readonly List<float> PathCumulative = new List<float>();
        internal float PathProgress;
        internal readonly List<float2> DeploySlots = new List<float2>();
        internal float DeployElapsed;

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
