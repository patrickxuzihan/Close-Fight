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
                case OrderType.Reverse: return "order.reverse";
                case OrderType.Demolish: return "order.demolish";
                case OrderType.LayMines: return "order.lay_mines";
                case OrderType.ClearMines: return "order.clear_mines";
                default: return "order.none";
            }
        }

        public static string TeamTypeKey(TeamType type)
        {
            switch (type)
            {
                case TeamType.MachineGun: return "team.machine_gun";
                case TeamType.Headquarters: return "team.headquarters";
                case TeamType.Vehicle: return "team.vehicle";
                case TeamType.AntiTankGun: return "team.anti_tank_gun";
                case TeamType.Mortar: return "team.mortar";
                case TeamType.Sniper: return "team.sniper";
                case TeamType.Engineer: return "team.engineer";
                case TeamType.Observer: return "team.observer";
                default: return "team.rifle";
            }
        }

        /// <summary>What a team is in one word: the vehicle or gun name if it has one.</summary>
        public static string TeamKind(Team team)
        {
            if (team.Vehicle != null)
            {
                return T(team.Vehicle.Definition.NameKey);
            }

            if (team.GunTeam != null)
            {
                return T(team.GunTeam.NameKey);
            }

            return T(TeamTypeKey(team.Type));
        }

        public static string MoraleKey(MoraleState state)
        {
            switch (state)
            {
                case MoraleState.Suppressed: return "morale.suppressed";
                case MoraleState.Pinned: return "morale.pinned";
                case MoraleState.Cowering: return "morale.cowering";
                case MoraleState.Panicked: return "morale.panicked";
                case MoraleState.Routed: return "morale.routed";
                default: return "morale.steady";
            }
        }

        public static string StatusKey(SoldierStatus status)
        {
            switch (status)
            {
                case SoldierStatus.Wounded: return "status.wounded";
                case SoldierStatus.Incapacitated: return "status.incapacitated";
                case SoldierStatus.Dead: return "status.dead";
                case SoldierStatus.Fled: return "status.fled";
                default: return "status.healthy";
            }
        }

        public static string RefusalKey(OrderRefusal refusal)
        {
            switch (refusal)
            {
                case OrderRefusal.Shaken: return "refusal.shaken";
                case OrderRefusal.Broken: return "refusal.broken";
                default: return "refusal.impossible";
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
