using System;
using System.Collections.Generic;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CloseFight.Core.Battle
{
    /// <summary>A battle setup saved as JSON: which map, which sides, which teams start where.</summary>
    [Serializable]
    public sealed class ScenarioDefinition
    {
        public string id = "untitled";
        public string mapId = "";
        public ulong seed = 1;
        public Side playerSide = Side.German;
        public List<SideDefinition> sides = new List<SideDefinition>();
        public List<TeamDefinition> teams = new List<TeamDefinition>();
    }

    [Serializable]
    public sealed class SideDefinition
    {
        public Side side;
        public MapEdge retreatEdge = MapEdge.South;
    }

    [Serializable]
    public sealed class TeamDefinition
    {
        public string key = "";
        public Side side;
        public TeamType type = TeamType.Rifle;
        public string displayName = "";
        /// <summary>0 means the default size for the team type.</summary>
        public int size;
        public MapPoint position;
        /// <summary>Compass heading in degrees the team faces at the start.</summary>
        public float facing;
        public int experience = 50;
        /// <summary>Optional starting stance: Defend, Ambush or Hide.</summary>
        public OrderType initialOrder = OrderType.None;
    }

    public static class ScenarioLoader
    {
        static readonly JsonSerializerSettings Settings = CreateSettings();

        static JsonSerializerSettings CreateSettings()
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                MissingMemberHandling = MissingMemberHandling.Ignore,
            };
            settings.Converters.Add(new StringEnumConverter());
            return settings;
        }

        public static ScenarioDefinition FromJson(string json)
        {
            var scenario = JsonConvert.DeserializeObject<ScenarioDefinition>(json, Settings);
            if (scenario == null)
            {
                throw new FormatException("Scenario JSON is empty or invalid.");
            }

            return scenario;
        }

        public static string ToJson(ScenarioDefinition scenario) => JsonConvert.SerializeObject(scenario, Settings);

        public static BattleState CreateBattle(ScenarioDefinition scenario, TacticalMap map)
        {
            var battle = new BattleState(map, scenario.seed);
            foreach (var side in scenario.sides)
            {
                battle.SetRetreatEdge(side.side, side.retreatEdge);
            }

            foreach (var team in scenario.teams)
            {
                int size = team.size > 0 ? team.size : BattleState.DefaultSize(team.type);
                var created = battle.AddTeam(team.key, team.side, team.type, team.displayName, team.position, team.facing, size, team.experience);
                if (team.initialOrder == OrderType.Defend || team.initialOrder == OrderType.Ambush || team.initialOrder == OrderType.Hide)
                {
                    battle.IssueOrder(created, team.initialOrder, created.Centroid);
                }
            }

            battle.Visibility.Recompute();
            return battle;
        }
    }
}
