using System.Collections.Generic;

namespace CloseFight.Core.Units
{
    /// <summary>What a platoon is; platoons of the same kind share one picture in the roster (M18).</summary>
    public enum PlatoonType : byte
    {
        /// <summary>Company or battalion headquarters, with what is attached to it (snipers, observers).</summary>
        Headquarters = 0,
        Rifle = 1,
        /// <summary>Heavy machine guns on tripods or wheeled mounts (German sMG, Soviet Maxim).</summary>
        MachineGun = 2,
        Mortar = 3,
        AntiTank = 4,
        /// <summary>Tanks or assault guns.</summary>
        Armor = 5,
        Pioneer = 6,
    }

    /// <summary>
    /// A platoon: the unit the player deploys and the roster shows first (M18).
    /// Its squads and teams are the units that take orders. The company is only a
    /// label for grouping, e.g. "9./IR 194".
    /// </summary>
    public sealed class Platoon
    {
        public int Id { get; }
        /// <summary>Stable key from the scenario file, e.g. "de_z1".</summary>
        public string Key { get; }
        public Side Side { get; }
        public PlatoonType Type { get; }
        /// <summary>Designation, e.g. "1. Zug" or "1-y vzvod".</summary>
        public string Name { get; }
        /// <summary>Company it belongs to, e.g. "9./IR 194"; empty if none.</summary>
        public string Company { get; }
        public List<Team> Teams { get; } = new List<Team>();

        public Platoon(int id, string key, Side side, PlatoonType type, string name, string company)
        {
            Id = id;
            Key = key ?? "";
            Side = side;
            Type = type;
            Name = name ?? "";
            Company = company ?? "";
        }

        /// <summary>Soldiers still able to fight in all its teams.</summary>
        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (var team in Teams)
                {
                    count += team.ActiveCount;
                }

                return count;
            }
        }
    }
}
