namespace CloseFight.Core.Units
{
    public enum Side : byte
    {
        German = 0,
        Soviet = 1,
    }

    public enum TeamType : byte
    {
        Rifle = 0,
        MachineGun = 1,
        Headquarters = 2,
        /// <summary>A tank or assault gun and its crew.</summary>
        Vehicle = 3,
        AntiTankGun = 4,
        Mortar = 5,
    }

    public enum SoldierStatus : byte
    {
        Healthy = 0,
        Wounded = 1,
        Incapacitated = 2,
        Dead = 3,
        /// <summary>Ran off the battlefield in a rout; out of the battle.</summary>
        Fled = 4,
    }

    /// <summary>
    /// Psychological state (M5), from steady to routed. Escalates with
    /// suppression and falling morale; the worse three take the soldier out of
    /// the player's control (D29: disobedience is the charm, but always visible).
    /// </summary>
    public enum MoraleState : byte
    {
        Steady = 0,
        Suppressed = 1,
        Pinned = 2,
        Cowering = 3,
        Panicked = 4,
        Routed = 5,
    }

    /// <summary>Why a team would not carry out an order.</summary>
    public enum OrderRefusal : byte
    {
        None = 0,
        /// <summary>The order itself is impossible (no target, no route, out of range).</summary>
        Impossible = 1,
        /// <summary>Half the team is down: they will not advance.</summary>
        Shaken = 2,
        /// <summary>Two thirds down or morale collapsed: they only want out.</summary>
        Broken = 3,
    }

    /// <summary>Commands from D31. Some are only wired up in later milestones.</summary>
    public enum OrderType : byte
    {
        None = 0,
        Move,
        MoveFast,
        Crawl,
        Fire,
        AreaFire,
        Smoke,
        Defend,
        Ambush,
        Hide,
        AssaultBuilding,
        Retreat,
        /// <summary>Vehicles only: back away while keeping the thick front armor toward the enemy.</summary>
        Reverse,
    }

    public enum TeamActivity : byte
    {
        Idle = 0,
        Moving = 1,
        Deploying = 2,
    }

    public enum MapEdge : byte
    {
        South = 0,
        North = 1,
        West = 2,
        East = 3,
    }

    /// <summary>The five soldier attributes of D30, each 0..100.</summary>
    public struct SoldierAttributes
    {
        public byte Experience;
        public byte Morale;
        public byte Stamina;
        public byte Marksmanship;
        /// <summary>Only meaningful for leaders (officers and squad leaders).</summary>
        public byte Leadership;
    }
}
