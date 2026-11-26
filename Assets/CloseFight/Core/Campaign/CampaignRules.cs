namespace CloseFight.Core.Campaign
{
    /// <summary>
    /// The numbers of the strategic layer (M16, D61, D62), in one place for balancing.
    /// The rule that matters: whatever a regiment gets back each turn is less than what a
    /// battle costs it, so a regiment in the line wears down until it is pulled out to rest.
    /// </summary>
    public static class CampaignRules
    {
        /// <summary>At most this many regiments of a side in one district, and so in one battle (D13).</summary>
        public const int MaxRegimentsPerSide = 2;

        /// <summary>At most this share of a regiment's companies goes into one battle (D21).</summary>
        public const float MaxCompanyShareInBattle = 2f / 3f;

        // Supply points (D61, D62)
        /// <summary>Points a held supply point brings each turn.</summary>
        public const int DepotIncome = 120;
        /// <summary>Points a held Volga crossing brings on a night turn; nothing by day (D40).</summary>
        public const int CrossingIncome = 260;

        /// <summary>Points per replacement man (D62); new men are green.</summary>
        public const int ReplacementCost = 1;
        public const int ReplacementExperience = 25;
        /// <summary>Most men a resting regiment can absorb in one turn.</summary>
        public const int MaxReplacementsPerTurn = 120;

        /// <summary>Points to bring one man from an empty to a full basic load of ammunition.</summary>
        public const float AmmoCostPerMan = 0.25f;

        /// <summary>Supply points per off-map support point bought for the next battles (D36).</summary>
        public const int SupportCost = 1;

        // Recovery each turn while connected to a supply point (D61)
        public const float AmmoRecoveryInLine = 0.08f;
        public const float AmmoRecoveryAtRest = 0.30f;
        public const float MoraleRecoveryInLine = 3f;
        public const float MoraleRecoveryAtRest = 12f;
        /// <summary>A regiment cut off from supply loses heart each turn.</summary>
        public const float MoraleLossCutOff = 2f;

        // What a battle costs when the result does not say (auto-resolve and the real battle report the true figures)
        public const float DefaultAmmoUsed = 0.35f;
        public const float DefaultMoraleLoss = 12f;
        public const float MaxMorale = 100f;

        /// <summary>A lightly wounded man rejoins after this many turns (D39), at random within the range.</summary>
        public const int WoundedReturnMinTurns = 2;
        public const int WoundedReturnMaxTurns = 4;
    }
}
