using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CloseFight.Core.AI;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Sim;
using CloseFight.Core.Units;
using UnityEditor;
using UnityEngine;

namespace CloseFight.EditorTools
{
    /// <summary>
    /// Plays a scenario AI against AI with no graphics and logs a timeline (attack
    /// phases, objectives, losses), to tune the attacking and defending AIs:
    ///   -executeMethod CloseFight.EditorTools.AiDuel.RunFromCommandLine
    ///   [-duelScenario stalingrad_station_sept14] [-duelSeeds 1,2,3] [-duelMinutes 45]
    /// </summary>
    public static class AiDuel
    {
        const string DataRoot = "Assets/CloseFight/Data";

        [MenuItem("Close Fight/Run AI Duel (station)")]
        public static void RunFromMenu() => Run("stalingrad_station_sept14", new ulong[] { 1 }, 45f);

        public static void RunFromCommandLine()
        {
            string scenario = Arg("-duelScenario", "stalingrad_station_sept14");
            var seeds = Arg("-duelSeeds", "1").Split(',').Select(s => ulong.Parse(s, CultureInfo.InvariantCulture)).ToArray();
            float minutes = float.Parse(Arg("-duelMinutes", "45"), CultureInfo.InvariantCulture);
            Run(scenario, seeds, minutes);
        }

        static void Run(string scenarioId, ulong[] seeds, float minutes)
        {
            foreach (ulong seed in seeds)
            {
                var scenario = ScenarioLoader.FromJson(File.ReadAllText(Path.Combine(DataRoot, "Scenarios", scenarioId + ".json")));
                scenario.seed = seed;
                scenario.timeLimitMinutes = minutes;
                var map = MapBuilder.Build(MapSerializer.FromJson(File.ReadAllText(Path.Combine(DataRoot, "Maps", scenario.mapId + ".json"))));
                var battle = ScenarioLoader.CreateBattle(scenario, map, deployment: true, allAi: true);
                battle.BeginBattle();
                Debug.Log($"Close Fight duel: minefields at start: {string.Join(" ", battle.Engineering.MineFields.Select(f => $"{f.Owner}@({f.Center.x:0},{f.Center.y:0})"))}");
                var attack = battle.Commanders.OfType<AttackCommander>().FirstOrDefault();

                var log = new StringBuilder();
                log.AppendLine($"Close Fight duel: {scenarioId} seed {seed}");
                int events = 0;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                int ticks = Mathf.CeilToInt(minutes * 60f / SimClock.TickSeconds) + 2;
                float traceUntil = float.Parse(Arg("-duelTrace", "0"), CultureInfo.InvariantCulture) * 60f;
                for (int i = 0; i < ticks && !battle.Victory.IsOver; i++)
                {
                    battle.Step();
                    if (attack != null)
                    {
                        for (; events < attack.Events.Count; events++)
                        {
                            var e = attack.Events[events];
                            log.AppendLine($"  {Clock(e.Time)} attack {e.Phase} {e.ObjectiveId} ({e.Note})");
                        }
                    }

                    if (battle.Clock.TickCount % 600 == 0)
                    {
                        log.AppendLine($"  {Clock(battle.Clock.ElapsedSeconds)} {Status(battle)}");
                    }

                    if (battle.Clock.ElapsedSeconds < traceUntil && battle.Clock.TickCount % 150 == 0 && attack != null)
                    {
                        log.AppendLine($"    -- {Clock(battle.Clock.ElapsedSeconds)} {attack.Phase} target {attack.Target?.Id} assembly ({attack.Assembly.x:0},{attack.Assembly.y:0}) overwatch ({attack.Overwatch.x:0},{attack.Overwatch.y:0}) jump-off ({attack.JumpOff.x:0},{attack.JumpOff.y:0}) gun ({attack.ArmorSpot.x:0},{attack.ArmorSpot.y:0}) smoke@{attack.LastSmokeAt:0} clouds {battle.Smoke.Clouds.Count} contacts {string.Join(" ", attack.Contacts.Select(c => $"[{c.Position.x:0},{c.Position.y:0} n{c.Count}{(c.MachineGun ? " MG" : "")}{(c.Heard ? " heard" : "")} {battle.Clock.ElapsedSeconds - c.SeenAt:0}s]"))}");
                        foreach (var plan in attack.Plans)
                        {
                            var t = plan.Team;
                            if (t.ActiveCount == 0) continue;
                            string gun = t.Gun != null ? $" HE {t.Gun.AmmoHe} AP {t.Gun.AmmoAp} smoke {t.Gun.AmmoSmoke}" : "";
                            log.AppendLine($"       {t.Key,-9} {plan.Role,-12} ({Anchor(t).x:0},{Anchor(t).y:0}) {t.Activity,-9} {t.Order.Type,-10} sup {t.AverageSuppression:0} men {t.ActiveCount} shaken {t.IsShaken} broken {t.IsBroken}{gun}");
                        }
                    }
                }

                watch.Stop();
                log.AppendLine($"  minefields left: {battle.Engineering.MineFields.Count} ({string.Join(" ", battle.Engineering.MineFields.Select(f => $"{f.Owner} {f.Mines} known DE {f.KnownTo(Side.German)}"))})");
                log.AppendLine($"  END {Clock(battle.Clock.ElapsedSeconds)} result {battle.Victory.Result} ({battle.Victory.EndReasonKey}) {Status(battle)} — {watch.ElapsedMilliseconds} ms");
                foreach (var team in battle.Teams)
                {
                    log.AppendLine($"    {team.Key,-10} {team.ActiveCount}/{team.Members.Count} kills {team.Members.Sum(m => m.Kills),2} broken {team.IsBroken} order {team.Order.Type} at ({team.Centroid.x:0},{team.Centroid.y:0})"
                                   + (attack != null ? " " + RoleOf(attack, team) : ""));
                }

                Debug.Log(log.ToString());
            }
        }

        static Unity.Mathematics.float2 Anchor(Team t) => t.Vehicle != null ? t.Vehicle.Position : t.Centroid;

        static string RoleOf(AttackCommander attack, Team team)
        {
            var plan = attack.Plans.FirstOrDefault(p => p.Team == team);
            return plan != null ? plan.Role.ToString() : "";
        }

        static string Status(BattleState battle)
        {
            int de = battle.Soldiers.Count(s => s.Team.Side == Side.German && s.IsActive);
            int su = battle.Soldiers.Count(s => s.Team.Side == Side.Soviet && s.IsActive);
            string owners = string.Join(" ", battle.Objectives.Select(o => $"{o.Id}:{(o.Owner == Side.German ? "DE" : "SU")}{(o.Contested ? "*" : "")}"));
            return $"DE {de} SU {su} | morale DE {battle.Victory.ForceMorale(Side.German):0} SU {battle.Victory.ForceMorale(Side.Soviet):0} | {owners}";
        }

        static string Clock(float seconds) => $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";

        static string Arg(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return fallback;
        }
    }
}
