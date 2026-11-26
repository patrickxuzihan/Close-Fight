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
        /// <summary>The side on the offensive; its AI attacks (M10), the other side's AI defends (M7).</summary>
        public Side attacker = Side.German;
        /// <summary>Fought at night (M14): changes which aircraft can fly.</summary>
        public bool night;
        /// <summary>Battle length in game minutes (D33: adjustable, default 45).</summary>
        public float timeLimitMinutes = VictoryConditions.DefaultTimeLimitMinutes;
        public List<SideDefinition> sides = new List<SideDefinition>();
        public List<PlatoonDefinition> platoons = new List<PlatoonDefinition>();
        public List<TeamDefinition> teams = new List<TeamDefinition>();
        public List<ObjectiveDefinition> objectives = new List<ObjectiveDefinition>();
    }

    [Serializable]
    public sealed class SideDefinition
    {
        public Side side;
        public MapEdge retreatEdge = MapEdge.South;
        /// <summary>Polygon the side may place its teams in before the battle (empty: no deployment phase).</summary>
        public List<MapPoint> deployZone = new List<MapPoint>();
        /// <summary>Off-map support points for the battle (D36: later set by the strategic layer's supply).</summary>
        public int supportPoints;
        /// <summary>Off-map assets the side may call, by id (see SupportAssets), e.g. "lefh18", "stuka", "katyusha".</summary>
        public List<string> support = new List<string>();
        /// <summary>Parachute flares for a night battle (D35: a limited resource).</summary>
        public int flares;
    }

    [Serializable]
    /// <summary>A platoon in a scenario (M18). Teams name it in their <c>platoon</c> field.</summary>
    public sealed class PlatoonDefinition
    {
        public string key = "";
        public Side side;
        public PlatoonType type = PlatoonType.Rifle;
        /// <summary>Designation, e.g. "1. Zug".</summary>
        public string name = "";
        /// <summary>Company label, e.g. "9./IR 194".</summary>
        public string company = "";
    }

    public sealed class TeamDefinition
    {
        public string key = "";
        /// <summary>Key of the platoon it belongs to (M18); empty for none.</summary>
        public string platoon = "";
        public Side side;
        public TeamType type = TeamType.Rifle;
        public string displayName = "";
        /// <summary>0 means the default size for the team type.</summary>
        public int size;
        public MapPoint position;
        /// <summary>Compass heading in degrees the team faces at the start.</summary>
        public float facing;
        public int experience = 50;
        /// <summary>Vehicle or gun id for Vehicle / AntiTankGun / Mortar teams, e.g. "stug3f", "pak38", "grw34".</summary>
        public string equipment = "";
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

        /// <summary>
        /// Builds the battle. With <paramref name="withAi"/>, every side but the player's
        /// (<paramref name="playerSide"/>, or the scenario's) gets an AI commander:
        /// the attacker's AI attacks, the defender's defends. <paramref name="allAi"/>
        /// gives both sides an AI (for tests and the benchmark).
        /// With <paramref name="deployment"/> and deployment zones in the scenario, the
        /// battle opens in the deployment phase and the AI places its teams at once.
        /// </summary>
        public static BattleState CreateBattle(ScenarioDefinition scenario, TacticalMap map, bool withAi = true,
            Side? playerSide = null, bool deployment = false, bool allAi = false)
        {
            var player = playerSide ?? scenario.playerSide;
            var battle = new BattleState(map, scenario.seed) { IsNight = scenario.night };
            battle.Victory.TimeLimitSeconds = scenario.timeLimitMinutes * 60f;
            foreach (var side in scenario.sides)
            {
                battle.SetRetreatEdge(side.side, side.retreatEdge);
                if (side.deployZone != null && side.deployZone.Count >= 3)
                {
                    battle.AddDeployZone(side.side, side.deployZone.ConvertAll(p => (Unity.Mathematics.float2)p));
                }

                battle.Support.Configure(side.side, side.supportPoints, side.support);
                battle.Illumination.SetFlares(side.side, side.flares);
            }

            var platoons = new Dictionary<string, Platoon>();
            if (scenario.platoons != null)
            {
                foreach (var platoon in scenario.platoons)
                {
                    platoons[platoon.key] = battle.AddPlatoon(platoon.key, platoon.side, platoon.type, platoon.name, platoon.company);
                }
            }

            foreach (var team in scenario.teams)
            {
                int size = team.size > 0 ? team.size : BattleState.DefaultSize(team.type, team.side);
                var created = battle.AddTeam(team.key, team.side, team.type, team.displayName, team.position, team.facing, size, team.experience, team.equipment);
                if (!string.IsNullOrEmpty(team.platoon) && platoons.TryGetValue(team.platoon, out var owner))
                {
                    battle.AssignToPlatoon(created, owner);
                }

                if (team.initialOrder == OrderType.Defend || team.initialOrder == OrderType.Ambush || team.initialOrder == OrderType.Hide)
                {
                    battle.IssueOrder(created, team.initialOrder, created.Centroid);
                }
            }

            foreach (var objective in scenario.objectives)
            {
                battle.AddObjective(objective);
            }

            if (withAi)
            {
                foreach (var side in new[] { Side.German, Side.Soviet })
                {
                    if ((allAi || side != player) && scenario.teams.Exists(t => t.side == side))
                    {
                        battle.AddCommander(side == scenario.attacker
                            ? new AI.AttackCommander(battle, side)
                            : (IBattleCommander)new AI.DefenseCommander(battle, side));
                    }
                }
            }

            if (deployment && battle.HasDeployZones)
            {
                battle.BeginDeployment();
            }

            battle.Visibility.Recompute();
            return battle;
        }
    }
}
