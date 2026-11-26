using System;
using System.IO;
using System.Linq;
using CloseFight.Core.Campaign;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace CloseFight.EditorTools
{
    /// <summary>
    /// Writes the generated battle maps of the Stalingrad campaign's districts, with their
    /// battlefields for an attack from the west, to a folder for previews (M16 step 4):
    ///   -executeMethod CloseFight.EditorTools.CampaignMapExport.RunFromCommandLine -exportDir path [-exportDistricts a,b]
    /// tools/campaign/preview_maps.py draws them.
    /// </summary>
    public static class CampaignMapExport
    {
        public static void RunFromCommandLine()
        {
            string dir = Arg("-exportDir", "Temp/district_maps");
            string only = Arg("-exportDistricts", "");
            Directory.CreateDirectory(dir);
            var state = CampaignState.Create(CampaignLoader.FromJson(File.ReadAllText("Assets/CloseFight/Data/Campaigns/stalingrad_center.json")));
            foreach (var district in state.definition.districts)
            {
                if (only.Length > 0 && !only.Split(',').Contains(district.id))
                {
                    continue;
                }

                var map = DistrictMapGenerator.Generate(district, DistrictBattlefields.MapSeed(state, district.id));
                var field = DistrictBattlefields.Orient(map, MapEdge.West);
                File.WriteAllText(Path.Combine(dir, district.id + ".map.json"), MapSerializer.ToJson(map));
                File.WriteAllText(Path.Combine(dir, district.id + ".field.json"), JsonConvert.SerializeObject(new
                {
                    kind = district.kind.ToString(), field.attackerZone, field.defenderZone, field.objectives,
                }));
            }

            Debug.Log("Close Fight: district maps written to " + dir);
        }

        static string Arg(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }

            return fallback;
        }
    }
}
