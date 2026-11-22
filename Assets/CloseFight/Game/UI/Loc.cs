using CloseFight.Core.Text;
using CloseFight.Core.Units;
using UnityEngine;

namespace CloseFight.Game.UI
{
    /// <summary>Game-side access to the translation table (D46). Chinese by default; L toggles English.</summary>
    public static class Loc
    {
        static LocalizationTable _table = LocalizationTable.Empty();

        public static string Language { get; private set; } = LocalizationTable.Chinese;

        public static void Load(TextAsset asset)
        {
            if (asset != null)
            {
                _table = LocalizationTable.FromJson(asset.text);
            }
        }

        public static string T(string key) => _table.Get(key, Language);

        public static void ToggleLanguage()
        {
            Language = Language == LocalizationTable.Chinese ? LocalizationTable.English : LocalizationTable.Chinese;
        }

        public static string OrderKey(OrderType order)
        {
            switch (order)
            {
                case OrderType.Move: return "order.move";
                case OrderType.MoveFast: return "order.move_fast";
                case OrderType.Crawl: return "order.crawl";
                case OrderType.Fire: return "order.fire";
                case OrderType.AreaFire: return "order.area_fire";
                case OrderType.Smoke: return "order.smoke";
                case OrderType.Defend: return "order.defend";
                case OrderType.Ambush: return "order.ambush";
                case OrderType.Hide: return "order.hide";
                case OrderType.AssaultBuilding: return "order.assault_building";
                case OrderType.Retreat: return "order.retreat";
                default: return "order.none";
            }
        }

        public static string TeamTypeKey(TeamType type)
        {
            switch (type)
            {
                case TeamType.MachineGun: return "team.machine_gun";
                case TeamType.Headquarters: return "team.headquarters";
                default: return "team.rifle";
            }
        }

        public static string ActivityKey(TeamActivity activity)
        {
            switch (activity)
            {
                case TeamActivity.Moving: return "activity.moving";
                case TeamActivity.Deploying: return "activity.deploying";
                default: return "activity.idle";
            }
        }
    }
}
