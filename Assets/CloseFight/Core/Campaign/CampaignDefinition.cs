using System;
using System.Collections.Generic;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CloseFight.Core.Campaign
{
    /// <summary>What a district is like; the battle map generator builds its streets from it (M16 step 4).</summary>
    public enum DistrictKind : byte
    {
        /// <summary>Dense blocks of brick and wooden houses.</summary>
        Residential = 0,
        /// <summary>Workshops, sheds and walled yards.</summary>
        Industrial = 1,
        /// <summary>Tracks, platforms, goods sheds, a station building.</summary>
        Railway = 2,
        /// <summary>The Volga bank: steep slope, landing stages, riverside buildings.</summary>
        Riverside = 3,
        /// <summary>A hill or ravine with little cover, such as Mamayev Kurgan.</summary>
        Hill = 4,
        /// <summary>A large square with big public buildings around it.</summary>
        Square = 5,
        /// <summary>Scattered wooden houses and gardens at the edge of the city.</summary>
        Outskirts = 6,
    }

    /// <summary>
    /// A campaign as saved in Data/Campaigns (M16, D4, D15, D55–D62): the strategic map's
    /// districts, the regiments on it at the start, and the historical arrivals.
    /// </summary>
    [Serializable]
    public sealed class CampaignDefinition
    {
        public string id = "untitled";
        /// <summary>Localization key of the campaign's title.</summary>
        public string nameKey = "";
        /// <summary>Date of turn 1, "yyyy-MM-dd". Each turn is half a day (D15), turn 1 is a day turn.</summary>
        public string startDate = "1942-09-13";
        /// <summary>Number of turns; the campaign ends after the last (D60: the front is where it is).</summary>
        public int turns = 28;
        public ulong seed = 1;
        /// <summary>The side the player commands (D58: German first).</summary>
        public Side playerSide = Side.German;
        /// <summary>The side on the offensive; it attacks when two moves collide head-on.</summary>
        public Side attacker = Side.German;
        public List<DistrictDefinition> districts = new List<DistrictDefinition>();
        public List<RegimentDefinition> regiments = new List<RegimentDefinition>();
        /// <summary>Regiments that join on a historical date (D62: free, unlike bought units).</summary>
        public List<ArrivalDefinition> arrivals = new List<ArrivalDefinition>();
        /// <summary>Companies a side may buy with supply points (D62: new units).</summary>
        public List<PurchaseDefinition> purchases = new List<PurchaseDefinition>();
    }

    [Serializable]
    public sealed class DistrictDefinition
    {
        public string id = "";
        public string nameKey = "";
        public DistrictKind kind = DistrictKind.Residential;
        /// <summary>Who holds it on turn 1.</summary>
        public Side owner = Side.Soviet;
        /// <summary>Ids of the districts that border it; the graph is made symmetric on load.</summary>
        public List<string> neighbours = new List<string>();
        /// <summary>A landmark that matters more (station, landing stage, grain elevator, Mamayev Kurgan).</summary>
        public bool key;
        /// <summary>A supply point of this side (D61): supplies reach only regiments connected to one.</summary>
        public Side? depotOf;
        /// <summary>A supply point across the Volga: its supplies come over the river only on night turns (D40).</summary>
        public bool crossing;
        /// <summary>A landing stage on the city bank, where the river routes come ashore (shown on the map).</summary>
        public bool landing;
        /// <summary>A side that cannot enter it: the Germans never crossed the Volga, so the east bank and the islands are closed to them.</summary>
        public Side? closedTo;
        /// <summary>Hand-made battle map for it (Data/Maps), or empty to generate one (M16 step 4).</summary>
        public string mapId = "";
        /// <summary>Its outline on the strategic map, in strategic map units (the UI draws it).</summary>
        public List<MapPoint> outline = new List<MapPoint>();
        /// <summary>Where its label and counters go on the strategic map.</summary>
        public MapPoint label;
    }

    [Serializable]
    public sealed class RegimentDefinition
    {
        public string id = "";
        public Side side;
        /// <summary>Designation, e.g. "IR 194" or "42-y gv. sp".</summary>
        public string name = "";
        /// <summary>Its division, e.g. "71. ID" or "13-ya gv. sd".</summary>
        public string division = "";
        /// <summary>District it stands in on turn 1, or arrives at.</summary>
        public string district = "";
        /// <summary>Average experience of its men, 0..100.</summary>
        public int experience = 50;
        /// <summary>Share of the table strength actually present (D13: real strengths, not paper ones).</summary>
        public float strength = 1f;
        /// <summary>Regimental morale, 0..100: it falls in battle and recovers at rest (D61).</summary>
        public int morale = 70;
        public List<CompanyDefinition> companies = new List<CompanyDefinition>();
    }

    [Serializable]
    public sealed class CompanyDefinition
    {
        /// <summary>A company template in <see cref="CompanyTemplates"/>, e.g. "de_rifle".</summary>
        public string template = "";
        /// <summary>Designation, e.g. "9./IR 194".</summary>
        public string name = "";
    }

    [Serializable]
    public sealed class ArrivalDefinition
    {
        /// <summary>Turn on which it appears (1 = the first day turn).</summary>
        public int turn;
        public RegimentDefinition regiment = new RegimentDefinition();
    }

    [Serializable]
    public sealed class PurchaseDefinition
    {
        public Side side;
        /// <summary>The company template bought, e.g. "de_stug".</summary>
        public string template = "";
        /// <summary>Designation the bought company gets, e.g. "StuG-Batterie".</summary>
        public string name = "";
        public int cost;
    }

    public static class CampaignLoader
    {
        static readonly JsonSerializerSettings Settings = CreateSettings();

        static JsonSerializerSettings CreateSettings()
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
            };
            settings.Converters.Add(new StringEnumConverter());
            return settings;
        }

        public static CampaignDefinition FromJson(string json)
        {
            var campaign = JsonConvert.DeserializeObject<CampaignDefinition>(json, Settings);
            if (campaign == null)
            {
                throw new FormatException("Campaign JSON is empty or invalid.");
            }

            Validate(campaign);
            return campaign;
        }

        public static string ToJson(CampaignDefinition campaign) => JsonConvert.SerializeObject(campaign, Settings);

        /// <summary>Saves and loads a running campaign (M16 step 7).</summary>
        public static string SaveToJson(CampaignState state) => JsonConvert.SerializeObject(state, Settings);

        public static CampaignState LoadFromJson(string json)
        {
            var state = JsonConvert.DeserializeObject<CampaignState>(json, Settings);
            if (state == null)
            {
                throw new FormatException("Campaign save is empty or invalid.");
            }

            state.AfterLoad();
            return state;
        }

        /// <summary>Makes the neighbour graph symmetric and checks every reference.</summary>
        public static void Validate(CampaignDefinition campaign)
        {
            var ids = new Dictionary<string, DistrictDefinition>();
            foreach (var district in campaign.districts)
            {
                if (string.IsNullOrEmpty(district.id) || ids.ContainsKey(district.id))
                {
                    throw new FormatException($"District id '{district.id}' is empty or repeated.");
                }

                ids[district.id] = district;
            }

            foreach (var district in campaign.districts)
            {
                foreach (var neighbour in district.neighbours.ToArray())
                {
                    if (!ids.TryGetValue(neighbour, out var other) || neighbour == district.id)
                    {
                        throw new FormatException($"District '{district.id}' names an unknown neighbour '{neighbour}'.");
                    }

                    if (!other.neighbours.Contains(district.id))
                    {
                        other.neighbours.Add(district.id);
                    }
                }
            }

            foreach (var side in new[] { Side.German, Side.Soviet })
            {
                if (!campaign.districts.Exists(d => d.depotOf == side))
                {
                    throw new FormatException($"The {side} side has no supply point (D61: at least one each).");
                }
            }

            void CheckRegiment(RegimentDefinition regiment)
            {
                if (!ids.ContainsKey(regiment.district))
                {
                    throw new FormatException($"Regiment '{regiment.id}' stands in an unknown district '{regiment.district}'.");
                }

                foreach (var company in regiment.companies)
                {
                    if (!CompanyTemplates.Exists(company.template))
                    {
                        throw new FormatException($"Regiment '{regiment.id}' uses an unknown company template '{company.template}'.");
                    }

                    if (CompanyTemplates.SideOf(company.template) != regiment.side)
                    {
                        throw new FormatException($"Regiment '{regiment.id}' is {regiment.side} but its company '{company.template}' is not.");
                    }
                }
            }

            foreach (var regiment in campaign.regiments)
            {
                CheckRegiment(regiment);
            }

            foreach (var arrival in campaign.arrivals)
            {
                CheckRegiment(arrival.regiment);
            }

            foreach (var purchase in campaign.purchases)
            {
                if (!CompanyTemplates.Exists(purchase.template))
                {
                    throw new FormatException($"Purchase uses an unknown company template '{purchase.template}'.");
                }
            }
        }
    }
}
