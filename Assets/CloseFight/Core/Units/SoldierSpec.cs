namespace CloseFight.Core.Units
{
    /// <summary>
    /// A man to put into a battle team: who he is and how he starts. The battle generates these
    /// for scenario teams; the campaign fills them from its rolls so the same men fight on (M16, D1, D30).
    /// </summary>
    public struct SoldierSpec
    {
        /// <summary>His place in the team's table (0 = leader); it decides his weapon and, in a crew, his job.</summary>
        public int Seat;
        public string FirstName;
        public string LastName;
        /// <summary>Localization key of the rank.</summary>
        public string RankKey;
        public SoldierAttributes Attributes;
        /// <summary>Morale at the start, 0..100; 0 takes it from the Morale attribute.</summary>
        public float Morale;
        /// <summary>Share of his weapon's full starting ammunition he carries, 0..1 (D40).</summary>
        public float AmmoShare;
    }
}
