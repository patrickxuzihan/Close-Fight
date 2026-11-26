using CloseFight.Core.Units;
using UnityEngine;

namespace CloseFight.Game.UI
{
    /// <summary>
    /// Platoon pictures for the roster (M18): one per platoon type and side, shared by
    /// every platoon of that type (Resources/Art/Roster/platoon_&lt;de|su&gt;_&lt;type&gt;).
    /// Without one, the platoon shows its first unit's picture; without that either,
    /// temporary silhouettes typical of the platoon stand in.
    /// </summary>
    public static class RosterArt
    {
        public static string TypeName(PlatoonType type)
        {
            switch (type)
            {
                case PlatoonType.Headquarters: return "headquarters";
                case PlatoonType.MachineGun: return "machinegun";
                case PlatoonType.Mortar: return "mortar";
                case PlatoonType.AntiTank: return "antitank";
                case PlatoonType.Armor: return "armor";
                case PlatoonType.Pioneer: return "pioneer";
                default: return "rifle";
            }
        }

        /// <summary>
        /// Draws a platoon's picture: its own (platoon_&lt;de|su&gt;_&lt;type&gt;), else the picture of
        /// <paramref name="first"/> (its first unit), else temporary silhouettes.
        /// </summary>
        public static void DrawPlatoon(Rect area, PlatoonType type, Side side, Team first = null)
        {
            var art = SquadPictures.Picture("platoon_" + (side == Side.German ? "de_" : "su_") + TypeName(type));
            if (art == null && first != null)
            {
                art = SquadPictures.Picture(SquadPictures.PictureKey(first));
            }

            if (art != null)
            {
                UiArt.DrawPicture(area, art);
                return;
            }

            bool german = side == Side.German;
            switch (type)
            {
                case PlatoonType.Headquarters:
                    SquadPictures.DrawRanks(area, side, new[] { FigureRole.Leader, FigureRole.Radio, FigureRole.Rifleman });
                    break;
                case PlatoonType.MachineGun:
                    SquadPictures.DrawRanks(area, side, new[] { FigureRole.HeavyGunner, FigureRole.Assistant });
                    break;
                case PlatoonType.Mortar:
                    SquadPictures.DrawEquipment(new Rect(area.x, area.y + 4f, area.width * 0.55f, area.height - 4f), german ? "grw34" : "bm37");
                    SquadPictures.DrawRanks(new Rect(area.x + area.width * 0.5f, area.y, area.width * 0.5f, area.height), side, new[] { FigureRole.Crew, FigureRole.Crew });
                    break;
                case PlatoonType.AntiTank:
                    SquadPictures.DrawEquipment(area, german ? "pak38" : "m1937_45");
                    break;
                case PlatoonType.Armor:
                    SquadPictures.DrawEquipment(area, german ? "stug3f" : "t34_76");
                    break;
                case PlatoonType.Pioneer:
                    SquadPictures.DrawRanks(area, side, new[] { FigureRole.Flamethrower, FigureRole.Leader, FigureRole.Rifleman });
                    break;
                default:
                    SquadPictures.DrawRanks(area, side, new[] { FigureRole.Leader, FigureRole.MachineGunner, FigureRole.Rifleman, FigureRole.Rifleman });
                    break;
            }
        }
    }
}
