using CloseFight.Core.Units;
using UnityEngine;

namespace CloseFight.Game.UI
{
    /// <summary>
    /// Codex UI art under Resources/Art/UI: NATO-style unit symbols drawn in ink
    /// (D42) for the team cards, and the briefing background. Null when missing.
    /// </summary>
    public static class UiArt
    {
        public static Texture2D BriefingBackground => Resources.Load<Texture2D>("Art/UI/briefing_station");

        public static Texture2D IconFor(Team team)
        {
            string name;
            switch (team.Type)
            {
                case TeamType.MachineGun: name = "icon_machinegun"; break;
                case TeamType.Mortar: name = "icon_mortar"; break;
                case TeamType.AntiTankGun: name = "icon_antitank"; break;
                case TeamType.Headquarters: name = "icon_headquarters"; break;
                case TeamType.Vehicle:
                    name = team.Vehicle != null && !team.Vehicle.Definition.HasTurret ? "icon_assaultgun" : "icon_armor";
                    break;
                // Sniper and engineer symbols are still to come from Codex; the infantry symbol stands in.
                default: name = "icon_infantry"; break;
            }

            return Resources.Load<Texture2D>("Art/UI/" + name);
        }
    }
}
