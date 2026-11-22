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
    }

    public enum SoldierStatus : byte
    {
        Healthy = 0,
        Wounded = 1,
        Incapacitated = 2,
        Dead = 3,
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
