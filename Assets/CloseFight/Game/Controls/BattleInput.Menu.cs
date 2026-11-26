using System.Collections.Generic;
using CloseFight.Core.Battle;
using CloseFight.Core.Units;
using CloseFight.Game.UI;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CloseFight.Game.Controls
{
    /// <summary>
    /// The order menu, Close Combat style: right-click where the order should go.
    /// The menu puts first what fits the spot clicked (an enemy: fire, suppress,
    /// smoke; a building: assault it; open ground: move) and greys out what cannot
    /// work there; an order that needs a target goes to that spot at once. Opened
    /// from the roster (right-click a squad or a platoon), there is no spot: such
    /// orders then wait for a left click on the map, as with the hotkeys.
    /// </summary>
    public sealed partial class BattleInput
    {
        enum MenuSpot : byte { None, Ground, Enemy, Building }

        float2 _menuTarget;
        MenuSpot _menuSpot;
        Soldier _menuEnemy;
        int _menuFloors;

        /// <summary>Opens the menu at a spot on the map (right-click on the battlefield).</summary>
        void OpenMenuAt(Vector2 screen)
        {
            _menuOpen = true;
            _menuGui = ToGui(screen);
            _menuTarget = ScreenToMap(screen);
            _menuEnemy = _battle.FindVisibleEnemyNear(_playerSide, _menuTarget, 6f);
            _menuFloors = 0;
            if (_menuEnemy != null)
            {
                _menuSpot = MenuSpot.Enemy;
                return;
            }

            var grid = _battle.Map.Grid;
            var cell = grid.WorldToCell(_menuTarget);
            var building = grid.InBounds(cell) ? _battle.Map.BuildingAt(grid.IndexOf(cell)) : null;
            _menuSpot = building != null ? MenuSpot.Building : MenuSpot.Ground;
            _menuFloors = building != null ? building.Floors : 0;
        }

        /// <summary>Opens the menu for what the roster row selected (no spot on the map).</summary>
        void OpenMenuFromRoster()
        {
            if (Mouse.current == null || _selection.Count == 0)
            {
                return;
            }

            _menuOpen = true;
            _menuGui = ToGui(Mouse.current.position.ReadValue());
            _menuSpot = MenuSpot.None;
            _menuEnemy = null;
        }

        /// <summary>The menu's orders, with those that fit the spot first.</summary>
        List<OrderType> MenuOrderList()
        {
            var orders = new List<OrderType>();
            foreach (var order in MenuOrders)
            {
                bool engineering = Engineering.IsEngineeringOrder(order);
                if (_stage == Stage.Deployment ? order == OrderType.LayMines && _selection.Exists(t => t.Type == TeamType.Engineer)
                    : !engineering || _selection.Exists(t => t.Type == TeamType.Engineer))
                {
                    orders.Add(order);
                }
            }

            OrderType[] first;
            switch (_menuSpot)
            {
                case MenuSpot.Enemy:
                    first = new[] { OrderType.Fire, OrderType.AreaFire, OrderType.Smoke };
                    break;
                case MenuSpot.Building:
                    first = new[] { OrderType.AssaultBuilding, OrderType.Demolish, OrderType.Move, OrderType.MoveFast, OrderType.Crawl };
                    break;
                default:
                    return orders;
            }

            var sorted = new List<OrderType>();
            foreach (var order in first)
            {
                if (orders.Remove(order))
                {
                    sorted.Add(order);
                }
            }

            sorted.AddRange(orders);
            return sorted;
        }

        /// <summary>False for an order that cannot work at the spot clicked (fire at nobody, assault open ground).</summary>
        bool FitsSpot(OrderType order)
        {
            switch (_menuSpot)
            {
                case MenuSpot.None:
                    return true;
                case MenuSpot.Enemy:
                    return order != OrderType.AssaultBuilding;
                case MenuSpot.Building:
                    return order != OrderType.Fire;
                default:
                    return order != OrderType.Fire && order != OrderType.AssaultBuilding;
            }
        }

        /// <summary>A line at the top of the menu saying what was clicked.</summary>
        string MenuHeader()
        {
            switch (_menuSpot)
            {
                case MenuSpot.Enemy:
                    return string.Format(Loc.T("menu.target_enemy"), Loc.TeamKind(_menuEnemy.Team));
                case MenuSpot.Building:
                    return string.Format(Loc.T("menu.target_building"), _menuFloors);
                case MenuSpot.Ground:
                    return Loc.T("menu.target_ground");
                default:
                    return _selection.Count == 1 ? Loc.TeamKind(_selection[0]) : string.Format(Loc.T("ui.teams_count"), _selection.Count);
            }
        }

        /// <summary>An order chosen in the menu: at the spot clicked if it needs one, else as from a hotkey.</summary>
        void ChooseMenuOrder(OrderType order)
        {
            _menuOpen = false;
            if (_menuSpot != MenuSpot.None && BattleState.NeedsTarget(order) && BattleState.IsImplemented(order))
            {
                var target = order == OrderType.Fire && _menuEnemy != null ? _menuEnemy.Position : _menuTarget;
                IssueToSelection(order, target);
                return;
            }

            ChooseOrder(order);
        }

        /// <summary>Screenshots (-cfMenu x,y): the selection's menu, opened at a map point as if right-clicked there.</summary>
        internal void DebugMenu(float2 point)
        {
            if (_selection.Count == 0 && _playerTeams.Count > 0)
            {
                SelectTeam(_playerTeams[1 % _playerTeams.Count], false);
            }

            var screen = worldCamera.WorldToScreenPoint(new Vector3(point.x, 0f, point.y));
            OpenMenuAt(new Vector2(screen.x, screen.y));
        }

        /// <summary>Where a stance chosen in the menu should be held: the spot clicked, or (from the roster) where the team is.</summary>
        float2? MenuAnchor => _menuSpot == MenuSpot.None || _menuSpot == MenuSpot.Enemy ? (float2?)null : _menuTarget;
    }
}
