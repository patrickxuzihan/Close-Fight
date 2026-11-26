using System;
using System.Collections.Generic;
using CloseFight.Core.Units;
using Newtonsoft.Json;

namespace CloseFight.Core.Campaign
{
    public enum RegimentAction : byte
    {
        /// <summary>Stay and hold the district.</summary>
        Hold = 0,
        /// <summary>Move to a neighbouring district: an attack if the enemy holds it or is in it.</summary>
        Move = 1,
        /// <summary>Stay and rest (D61): faster recovery and replacements, if supplied.</summary>
        Rest = 2,
    }

    [Serializable]
    public sealed class RegimentOrder
    {
        public string regiment = "";
        public RegimentAction action;
        /// <summary>Destination of a move.</summary>
        public string district = "";
    }

    /// <summary>How a side spends supply points on one regiment this turn (D62).</summary>
    [Serializable]
    public sealed class SupplyRequest
    {
        public string regiment = "";
        /// <summary>Replacement men wanted; only a resting, supplied regiment receives them.</summary>
        public int replacements;
        /// <summary>Top up its ammunition to a full basic load.</summary>
        public bool ammo;
    }

    [Serializable]
    public sealed class PurchaseRequest
    {
        /// <summary>The company template bought (it must be in the campaign's purchase list).</summary>
        public string template = "";
        /// <summary>The supplied regiment it joins.</summary>
        public string regiment = "";
    }

    /// <summary>Everything one side decides in a turn; both sides decide at once (D57).</summary>
    [Serializable]
    public sealed class SideOrders
    {
        public Side side;
        public List<RegimentOrder> regiments = new List<RegimentOrder>();
        public List<SupplyRequest> supply = new List<SupplyRequest>();
        /// <summary>Supply points turned into off-map support points for this side's next battles (D36).</summary>
        public int support;
        public List<PurchaseRequest> purchases = new List<PurchaseRequest>();

        public RegimentOrder For(string regiment) => regiments.Find(o => o.regiment == regiment);

        public void Move(string regiment, string district) =>
            Set(new RegimentOrder { regiment = regiment, action = RegimentAction.Move, district = district });

        public void Rest(string regiment) => Set(new RegimentOrder { regiment = regiment, action = RegimentAction.Rest });

        public void Hold(string regiment) => Set(new RegimentOrder { regiment = regiment, action = RegimentAction.Hold });

        void Set(RegimentOrder order)
        {
            regiments.RemoveAll(o => o.regiment == order.regiment);
            regiments.Add(order);
        }
    }

    /// <summary>A battle the turn has produced, waiting to be fought or auto-resolved (D59).</summary>
    [Serializable]
    public sealed class PendingBattle
    {
        public string id = "";
        public string district = "";
        public Side attacker;
        public List<string> attackers = new List<string>();
        public List<string> defenders = new List<string>();
        public bool night;
        /// <summary>The fight goes on in a district left contested by an undecided battle (D33).</summary>
        public bool continued;
        public bool resolved;

        [JsonIgnore] public Side Defender => attacker == Side.German ? Side.Soviet : Side.German;
    }

    /// <summary>
    /// What came of a battle. The men's own fates (killed, wounded, fled) are written into the
    /// regiments' rolls by whoever fought it; this carries the rest.
    /// </summary>
    public sealed class BattleOutcome
    {
        /// <summary>The side left holding the district, or null when it is undecided (D33: contested).</summary>
        public Side? winner;
        /// <summary>Share of a basic load each regiment fired, by regiment id; missing ones use the default.</summary>
        public Dictionary<string, float> ammoUsed = new Dictionary<string, float>();
        /// <summary>Regimental morale each regiment lost, by regiment id; missing ones use the default.</summary>
        public Dictionary<string, float> moraleLost = new Dictionary<string, float>();
    }

    /// <summary>Fights a battle without the player (D59: a fast simulation in the background).</summary>
    public interface IBattleResolver
    {
        BattleOutcome Resolve(CampaignState state, PendingBattle battle);
    }

    /// <summary>Decides a side's orders for a turn (the strategic AI, M16 step 6).</summary>
    public interface ICampaignCommander
    {
        Side Side { get; }
        SideOrders Plan(CampaignState state);
    }
}
