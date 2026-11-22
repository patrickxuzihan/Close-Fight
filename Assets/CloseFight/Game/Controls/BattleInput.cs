using System.Collections.Generic;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using CloseFight.Game.CameraControl;
using CloseFight.Game.UI;
using CloseFight.Game.View;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CloseFight.Game.Controls
{
    /// <summary>
    /// Close Combat style controls (D41):
    /// - Left-click a soldier to select his team; Shift adds; drag a box to select several.
    /// - Right-click opens the order menu; pick an order, then left-click the target.
    /// - Hotkeys skip the menu. Space pauses; orders still work while paused (D34).
    /// - The bottom bar lists the player's teams (CC layout); click a card to select it.
    /// UI is IMGUI for the prototype; it moves to UI Toolkit when the look is designed.
    /// </summary>
    public sealed class BattleInput : MonoBehaviour
    {
        [SerializeField] Camera worldCamera;
        [SerializeField] UnitsView unitsView;
        [SerializeField] TopDownCameraController cameraController;

        static readonly OrderType[] MenuOrders =
        {
            OrderType.Move, OrderType.MoveFast, OrderType.Crawl,
            OrderType.Fire, OrderType.AreaFire, OrderType.Smoke,
            OrderType.Defend, OrderType.Ambush, OrderType.Hide,
            OrderType.AssaultBuilding, OrderType.Retreat, OrderType.Reverse,
        };

        const float DragThreshold = 8f;
        const float TeamSpread = 8f;
        const float CardWidth = 170f;
        const float CardHeight = 64f;
        const float MenuWidth = 190f;
        const float MenuRowHeight = 24f;

        BattleState _battle;
        Side _playerSide;
        readonly List<Team> _selection = new List<Team>();
        readonly List<Team> _playerTeams = new List<Team>();

        bool _pressing;
        bool _dragging;
        Vector2 _pressScreen;

        bool _menuOpen;
        Vector2 _menuGui;
        OrderType _pendingOrder = OrderType.None;

        string _toast;
        float _toastUntil;

        Rect _menuRect;
        Rect _rosterRect;
        Rect _infoRect;
        Texture2D _white;
        GUIStyle _label;
        GUIStyle _small;
        GUIStyle _center;
        GUIStyle _button;
        float _scale = 1f;

        public void Bind(BattleState battle, Side playerSide)
        {
            _battle = battle;
            _playerSide = playerSide;
            _playerTeams.Clear();
            foreach (var team in battle.Teams)
            {
                if (team.Side == playerSide)
                {
                    _playerTeams.Add(team);
                }
            }

            ClearSelection();
        }

        void Update()
        {
            if (_battle == null)
            {
                return;
            }

            _scale = Mathf.Max(1f, Screen.height / 900f);
            HandleKeyboard();
            HandleMouse();
            UpdateSightTool();
        }

        // ---------------------------------------------------------------- line-of-sight tool

        bool _sightActive;
        float _sightDistance;
        bool _sightVisible;

        /// <summary>Hold V: line of sight from the selected team's leader to the cursor (CC's LOS tool).</summary>
        void UpdateSightTool()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var leader = _selection.Count > 0 ? _selection[0].Leader : null;
            _sightActive = keyboard != null && mouse != null && keyboard.vKey.isPressed && leader != null;
            if (!_sightActive)
            {
                unitsView.HideSightLine();
                return;
            }

            float2 target = ScreenToMap(mouse.position.ReadValue());
            var sight = LineOfSight.Check(_battle.Map, leader.Position, leader.Posture, target, Posture.Standing, Visibility.MaxSightRange);
            _sightDistance = sight.Distance;
            _sightVisible = sight.Visible;
            unitsView.ShowSightLine(leader.Position, target, sight.Visible ? 1f : sight.BlockedAt);
        }

        // ---------------------------------------------------------------- keyboard

        void HandleKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                _battle.Clock.Paused = !_battle.Clock.Paused;
            }

            if (keyboard.lKey.wasPressedThisFrame)
            {
                Loc.ToggleLanguage();
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (_pendingOrder != OrderType.None || _menuOpen)
                {
                    _pendingOrder = OrderType.None;
                    _menuOpen = false;
                }
                else
                {
                    ClearSelection();
                }
            }

            // Number keys select teams in roster order.
            for (int i = 0; i < 9 && i < _playerTeams.Count; i++)
            {
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame)
                {
                    SelectTeam(_playerTeams[i], keyboard.shiftKey.isPressed);
                }
            }

            if (_selection.Count == 0)
            {
                return;
            }

            if (keyboard.mKey.wasPressedThisFrame) ChooseOrder(OrderType.Move);
            if (keyboard.rKey.wasPressedThisFrame) ChooseOrder(OrderType.MoveFast);
            if (keyboard.cKey.wasPressedThisFrame) ChooseOrder(OrderType.Crawl);
            if (keyboard.bKey.wasPressedThisFrame) ChooseOrder(OrderType.Defend);
            if (keyboard.nKey.wasPressedThisFrame) ChooseOrder(OrderType.Ambush);
            if (keyboard.hKey.wasPressedThisFrame) ChooseOrder(OrderType.Hide);
            if (keyboard.gKey.wasPressedThisFrame) ChooseOrder(OrderType.AssaultBuilding);
            if (keyboard.tKey.wasPressedThisFrame) ChooseOrder(OrderType.Retreat);
            if (keyboard.fKey.wasPressedThisFrame) ChooseOrder(OrderType.Fire);
            if (keyboard.kKey.wasPressedThisFrame) ChooseOrder(OrderType.Smoke);
            if (keyboard.eKey.wasPressedThisFrame) ChooseOrder(OrderType.AreaFire);
            if (keyboard.xKey.wasPressedThisFrame) ChooseOrder(OrderType.Reverse);
        }

        static string Hotkey(OrderType order)
        {
            switch (order)
            {
                case OrderType.Move: return "M";
                case OrderType.MoveFast: return "R";
                case OrderType.Crawl: return "C";
                case OrderType.Defend: return "B";
                case OrderType.Ambush: return "N";
                case OrderType.Hide: return "H";
                case OrderType.AssaultBuilding: return "G";
                case OrderType.Retreat: return "T";
                case OrderType.Fire: return "F";
                case OrderType.AreaFire: return "E";
                case OrderType.Smoke: return "K";
                case OrderType.Reverse: return "X";
                default: return "";
            }
        }

        // ---------------------------------------------------------------- mouse

        void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 screen = mouse.position.ReadValue();
            bool overUi = IsOverUi(screen);

            if (mouse.rightButton.wasPressedThisFrame && !overUi)
            {
                if (_pendingOrder != OrderType.None)
                {
                    _pendingOrder = OrderType.None;
                }
                else if (_selection.Count > 0)
                {
                    _menuOpen = true;
                    _menuGui = ToGui(screen);
                }
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (_menuOpen && !_menuRect.Contains(ToGui(screen)))
                {
                    _menuOpen = false; // click outside closes the menu
                    return;
                }

                if (!overUi)
                {
                    _pressing = true;
                    _dragging = false;
                    _pressScreen = screen;
                }
            }

            if (_pressing && mouse.leftButton.isPressed && _pendingOrder == OrderType.None)
            {
                if (!_dragging && (screen - _pressScreen).magnitude > DragThreshold)
                {
                    _dragging = true;
                }
            }

            if (_pressing && mouse.leftButton.wasReleasedThisFrame)
            {
                _pressing = false;
                bool additive = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;

                if (_dragging)
                {
                    BoxSelect(_pressScreen, screen, additive);
                }
                else if (_pendingOrder != OrderType.None)
                {
                    IssueToSelection(_pendingOrder, ScreenToMap(screen));
                    _pendingOrder = OrderType.None;
                }
                else
                {
                    ClickSelect(screen, additive);
                }

                _dragging = false;
            }
        }

        void ClickSelect(Vector2 screen, bool additive)
        {
            float radius = Mathf.Max(1.2f, worldCamera.orthographicSize * 0.025f);
            var soldier = unitsView.PickSoldier(ScreenToMap(screen), radius, _playerSide);
            if (soldier == null)
            {
                if (!additive)
                {
                    ClearSelection();
                }

                return;
            }

            SelectTeam(soldier.Team, additive);
        }

        void BoxSelect(Vector2 a, Vector2 b, bool additive)
        {
            var rect = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            if (!additive)
            {
                _selection.Clear();
            }

            foreach (var team in _playerTeams)
            {
                if (_selection.Contains(team))
                {
                    continue;
                }

                foreach (var soldier in team.Members)
                {
                    if (!soldier.IsActive)
                    {
                        continue;
                    }

                    Vector3 p = worldCamera.WorldToScreenPoint(unitsView.DrawPosition(soldier));
                    if (rect.Contains(new Vector2(p.x, p.y)))
                    {
                        _selection.Add(team);
                        break;
                    }
                }
            }

            unitsView.SetSelection(_selection);
        }

        void SelectTeam(Team team, bool additive)
        {
            if (additive)
            {
                if (!_selection.Remove(team))
                {
                    _selection.Add(team);
                }
            }
            else
            {
                _selection.Clear();
                _selection.Add(team);
            }

            unitsView.SetSelection(_selection);
        }

        void ClearSelection()
        {
            _selection.Clear();
            _menuOpen = false;
            _pendingOrder = OrderType.None;
            if (unitsView != null)
            {
                unitsView.SetSelection(_selection);
            }
        }

        // ---------------------------------------------------------------- orders

        void ChooseOrder(OrderType order)
        {
            _menuOpen = false;
            if (!BattleState.IsImplemented(order))
            {
                ShowToast(Loc.T("ui.not_available"));
                return;
            }

            if (BattleState.NeedsTarget(order))
            {
                _pendingOrder = order;
                return;
            }

            IssueToSelection(order, float2.zero);
        }

        /// <summary>
        /// Several teams ordered to one point spread out side by side across the
        /// direction of travel, instead of piling onto the same spot.
        /// </summary>
        void IssueToSelection(OrderType order, float2 target)
        {
            float2 from = float2.zero;
            foreach (var team in _selection)
            {
                from += team.Centroid;
            }

            from /= math.max(1, _selection.Count);
            float2 forward = math.normalizesafe(target - from, new float2(0f, 1f));
            float2 right = new float2(forward.y, -forward.x);

            // Order teams left-to-right by their current position so their routes don't cross.
            var ordered = new List<Team>(_selection);
            ordered.Sort((a, b) => math.dot(a.Centroid - from, right).CompareTo(math.dot(b.Centroid - from, right)));

            string refusals = "";
            for (int i = 0; i < ordered.Count; i++)
            {
                float offset = (i - (ordered.Count - 1) * 0.5f) * TeamSpread;
                bool spread = order == OrderType.Move || order == OrderType.MoveFast || order == OrderType.Crawl;
                float2 teamTarget = spread ? target + right * offset : target;
                if (!_battle.TryIssueOrder(ordered[i], order, teamTarget, out var refusal))
                {
                    // D29: always say why a team will not obey.
                    refusals += (refusals.Length > 0 ? "\n" : "") + $"{ordered[i].DisplayName}: {Loc.T(Loc.RefusalKey(refusal))}";
                }
            }

            if (refusals.Length > 0)
            {
                ShowToast(refusals);
            }
        }

        void ShowToast(string text)
        {
            _toast = text;
            _toastUntil = Time.unscaledTime + 3f;
        }

        // ---------------------------------------------------------------- helpers

        float2 ScreenToMap(Vector2 screen)
        {
            var ray = worldCamera.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
            if (math.abs(ray.direction.y) < 1e-5f)
            {
                return new float2(ray.origin.x, ray.origin.z);
            }

            float t = -ray.origin.y / ray.direction.y;
            Vector3 hit = ray.origin + ray.direction * t;
            return new float2(hit.x, hit.z);
        }

        /// <summary>Input System screen coordinates (origin bottom-left, pixels) to scaled IMGUI coordinates (origin top-left).</summary>
        Vector2 ToGui(Vector2 screen) => new Vector2(screen.x / _scale, (Screen.height - screen.y) / _scale);

        bool IsOverUi(Vector2 screen)
        {
            var gui = ToGui(screen);
            return (_menuOpen && _menuRect.Contains(gui)) || _rosterRect.Contains(gui) || _infoRect.Contains(gui);
        }

        // ---------------------------------------------------------------- drawing

        void EnsureStyles()
        {
            if (_white == null)
            {
                _white = new Texture2D(1, 1) { hideFlags = HideFlags.DontSave };
                _white.SetPixel(0, 0, Color.white);
                _white.Apply();
            }

            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white } };
                _small = new GUIStyle(_label) { fontSize = 12 };
                _center = new GUIStyle(_label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                _button = new GUIStyle(GUI.skin.button) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
            }
        }

        void OnGUI()
        {
            if (_battle == null)
            {
                return;
            }

            EnsureStyles();
            GUI.matrix = Matrix4x4.Scale(new Vector3(_scale, _scale, 1f));
            float width = Screen.width / _scale;
            float height = Screen.height / _scale;

            DrawTeamLabels();
            DrawTopBar(width);
            DrawInfoPanel(width, height);
            DrawRoster(width, height);
            DrawBoxSelection();
            DrawPendingHint();
            DrawSightLabel();
            DrawMenu(width, height);
            DrawToast(width, height);
        }

        void DrawTopBar(float width)
        {
            Fill(new Rect(0, 0, width, 26), new Color(0f, 0f, 0f, 0.55f));
            int seconds = (int)_battle.Clock.ElapsedSeconds;
            GUI.Label(new Rect(10, 4, 220, 20), $"{Loc.T("ui.time")}  {seconds / 60:00}:{seconds % 60:00}", _label);
            GUI.Label(new Rect(240, 5, width - 340, 20), Loc.T("ui.help"), _small);
            if (GUI.Button(new Rect(width - 90, 2, 80, 22), Loc.T("ui.language")))
            {
                Loc.ToggleLanguage();
            }

            if (_battle.Clock.Paused)
            {
                Fill(new Rect(width * 0.5f - 160, 34, 320, 34), new Color(0.5f, 0.1f, 0.05f, 0.8f));
                GUI.Label(new Rect(width * 0.5f - 160, 34, 320, 34), Loc.T("ui.paused"), _center);
            }
        }

        void DrawRoster(float width, float height)
        {
            float barHeight = CardHeight + 16f;
            _rosterRect = new Rect(0, height - barHeight, width, barHeight);
            Fill(_rosterRect, new Color(0.08f, 0.08f, 0.07f, 0.85f));

            float x = 8f;
            float y = height - barHeight + 8f;
            for (int i = 0; i < _playerTeams.Count; i++)
            {
                var team = _playerTeams[i];
                var card = new Rect(x, y, CardWidth, CardHeight);
                bool selected = _selection.Contains(team);
                Fill(card, selected ? new Color(0.55f, 0.45f, 0.15f, 0.95f) : new Color(0.22f, 0.22f, 0.20f, 0.95f));

                GUI.Label(new Rect(card.x + 6, card.y + 3, CardWidth - 12, 20), $"{i + 1}  {team.DisplayName}", _label);
                GUI.Label(new Rect(card.x + 6, card.y + 22, CardWidth - 12, 18),
                    $"{Loc.TeamKind(team)} · {team.ActiveCount}/{team.Members.Count} {Loc.T("ui.men")}", _small);
                GUI.Label(new Rect(card.x + 6, card.y + 40, CardWidth - 12, 18), TeamStateText(team), _small);

                // Suppression bar along the bottom of the card: green (calm) to red (pinned).
                float suppression = team.AverageSuppression / 100f;
                Fill(new Rect(card.x, card.yMax - 4, CardWidth, 4), new Color(0f, 0f, 0f, 0.5f));
                Fill(new Rect(card.x, card.yMax - 4, CardWidth * suppression, 4), Color.Lerp(new Color(0.3f, 0.8f, 0.3f), new Color(0.9f, 0.15f, 0.1f), suppression));

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && card.Contains(Event.current.mousePosition))
                {
                    bool additive = Event.current.shift;
                    if (selected && !additive && cameraController != null)
                    {
                        var c = team.Centroid;
                        cameraController.FocusOn(new Vector3(c.x, 0f, c.y));
                    }

                    SelectTeam(team, additive);
                    Event.current.Use();
                }

                x += CardWidth + 8f;
            }
        }

        /// <summary>The most important thing about a team, in one line (D29: the player must see why).</summary>
        string TeamStateText(Team team)
        {
            float now = _battle.Clock.ElapsedSeconds;
            int pinned = 0;
            foreach (var member in team.Members)
            {
                if (member.IsActive && member.IsPinned)
                {
                    pinned++;
                }
            }

            string state;
            var vehicle = team.Vehicle;
            if (vehicle != null && vehicle.Destroyed && team.ActiveCount == 0)
            {
                state = Loc.T("vehicle.destroyed");
            }
            else if (team.ActiveCount == 0)
            {
                state = Loc.T("team.destroyed");
            }
            else if (vehicle != null && vehicle.Abandoned)
            {
                state = Loc.T(vehicle.Destroyed ? "vehicle.destroyed" : "vehicle.abandoned");
            }
            else if (team.IsBroken)
            {
                state = Loc.T("team.broken");
            }
            else if (team.IsShaken)
            {
                state = Loc.T("team.shaken");
            }
            else if (pinned * 2 >= team.ActiveCount && team.ActiveCount > 0)
            {
                state = Loc.T("activity.pinned");
            }
            else if (team.IsFiring(now))
            {
                state = Loc.T("activity.firing");
            }
            else if (team.Activity == TeamActivity.Idle && team.Order.Type != OrderType.None)
            {
                state = Loc.T(Loc.OrderKey(team.Order.Type));
            }
            else
            {
                state = Loc.T(Loc.ActivityKey(team.Activity));
            }

            if (vehicle != null && vehicle.IsOperational)
            {
                if (vehicle.Immobilized) state += " · " + Loc.T("vehicle.immobilized");
                if (vehicle.GunDamaged) state += " · " + Loc.T("vehicle.gun_damaged");
            }

            var gun = team.Gun;
            if (gun != null && (vehicle == null || vehicle.IsOperational))
            {
                if (gun.AmmoAp + gun.AmmoHe + gun.AmmoSmoke == 0)
                {
                    state += " · " + Loc.T("ui.shells_out");
                }

                return state; // gun teams: the gun's ammunition is what matters
            }

            int ammo = team.TotalAmmo;
            int starting = team.StartingAmmo;
            if (ammo == 0)
            {
                state += " · " + Loc.T("ui.ammo_out");
            }
            else if (starting > 0 && ammo < starting / 4)
            {
                state += " · " + Loc.T("ui.ammo_low");
            }

            return state;
        }

        /// <summary>
        /// Short tags floating above each of the player's teams whenever something
        /// is wrong (D29): the player sees at a glance why a team behaves as it does.
        /// </summary>
        void DrawTeamLabels()
        {
            float now = _battle.Clock.ElapsedSeconds;
            foreach (var team in _playerTeams)
            {
                if (team.ActiveCount == 0)
                {
                    continue;
                }

                var tags = new List<string>();
                if (team.IsBroken) tags.Add(Loc.T("team.broken"));
                else if (team.IsShaken) tags.Add(Loc.T("team.shaken"));
                if (team.LeaderLost && now - team.LeaderLostAt < 60f) tags.Add(Loc.T("team.leader_lost"));

                int pinned = 0, wild = 0;
                foreach (var member in team.Members)
                {
                    if (!member.IsActive) continue;
                    if (member.IsOutOfControl) wild++;
                    else if (member.IsPinned) pinned++;
                }

                if (wild > 0) tags.Add(Loc.T("team.out_of_control") + $" ×{wild}");
                if (pinned * 2 >= team.ActiveCount) tags.Add(Loc.T("activity.pinned"));
                if (team.TotalAmmo == 0) tags.Add(Loc.T("ui.ammo_out"));
                if (now - team.LastRefusalAt < 5f) tags.Add(Loc.T(Loc.RefusalKey(team.LastRefusal)));

                if (tags.Count == 0)
                {
                    continue;
                }

                var c = team.Centroid;
                Vector3 screen = worldCamera.WorldToScreenPoint(new Vector3(c.x, 0f, c.y));
                if (screen.z < 0f)
                {
                    continue;
                }

                Vector2 gui = ToGui(new Vector2(screen.x, screen.y));
                string text = string.Join(" · ", tags);
                float w = Mathf.Min(320f, 14f + text.Length * 8f);
                var rect = new Rect(gui.x - w * 0.5f, gui.y - 46f, w, 20f);
                Fill(rect, new Color(0.45f, 0.08f, 0.05f, 0.8f));
                GUI.Label(new Rect(rect.x + 6, rect.y + 1, rect.width - 12, 18), text, _small);
            }
        }

        /// <summary>Selected team in detail: every soldier's name, weapon, ammo, wounds, state, suppression and morale.</summary>
        void DrawInfoPanel(float width, float height)
        {
            _infoRect = Rect.zero;
            if (_selection.Count != 1)
            {
                return;
            }

            var team = _selection[0];
            const float panelWidth = 330f;
            float rowHeight = 34f;
            float panelHeight = 58f + team.Members.Count * rowHeight;
            var panel = new Rect(width - panelWidth - 8f, 34f, panelWidth, panelHeight);
            _infoRect = panel;
            Fill(panel, new Color(0.08f, 0.08f, 0.07f, 0.88f));

            GUI.Label(new Rect(panel.x + 8, panel.y + 4, panelWidth - 16, 20), $"{team.DisplayName} — {Loc.TeamKind(team)}", _label);
            if (team.Gun != null)
            {
                var g = team.Gun;
                string shells = g.Definition.Indirect
                    ? $"{Loc.T("ui.he")} {g.AmmoHe} · {Loc.T("ui.smoke")} {g.AmmoSmoke}"
                    : $"{Loc.T("ui.ap")} {g.AmmoAp} · {Loc.T("ui.he")} {g.AmmoHe}";
                GUI.Label(new Rect(panel.x + 180, panel.y + 4, panelWidth - 188, 20), shells, _small);
            }
            GUI.Label(new Rect(panel.x + 8, panel.y + 24, panelWidth - 16, 18),
                $"{Loc.T("ui.casualties")} {team.CasualtyFraction * 100f:0}% · {Loc.T("ui.morale")} {team.AverageMorale:0} · {TeamStateText(team)}", _small);

            float y = panel.y + 48f;
            foreach (var soldier in team.Members)
            {
                string rank = Loc.T(soldier.RankKey);
                string weapon = soldier.Role != CrewRole.None ? Loc.T("role." + soldier.Role.ToString().ToLowerInvariant()) : "";
                if (soldier.Weapon != null)
                {
                    weapon = (weapon.Length > 0 ? weapon + " · " : "") + Loc.T(soldier.Weapon.NameKey);
                }
                string state = soldier.IsActive ? Loc.T(Loc.MoraleKey(soldier.MoraleState)) : Loc.T(Loc.StatusKey(soldier.Status));
                if (soldier.IsWounded)
                {
                    state = Loc.T("status.wounded") + " · " + state;
                }

                var color = soldier.IsActive ? (soldier.IsOutOfControl ? new Color(1f, 0.6f, 1f) : Color.white) : new Color(0.6f, 0.6f, 0.6f);
                var previous = GUI.color;
                GUI.color = color;
                GUI.Label(new Rect(panel.x + 8, y, panelWidth - 16, 18), $"{rank} {soldier.FullName}", _small);
                GUI.Label(new Rect(panel.x + 8, y + 15, 190, 18), $"{weapon} · {soldier.Ammo}", _small);
                GUI.Label(new Rect(panel.x + 200, y, 122, 18), state, _small);
                GUI.color = previous;

                if (soldier.IsActive)
                {
                    // Suppression (red) and morale (blue) bars.
                    DrawBar(new Rect(panel.x + 200, y + 19, 120, 4), soldier.Suppression / 100f, new Color(0.9f, 0.2f, 0.1f));
                    DrawBar(new Rect(panel.x + 200, y + 25, 120, 4), soldier.Morale / 100f, new Color(0.3f, 0.6f, 1f));
                }

                y += rowHeight;
            }
        }

        void DrawBar(Rect rect, float value, Color color)
        {
            Fill(rect, new Color(0f, 0f, 0f, 0.6f));
            Fill(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height), color);
        }

        void DrawMenu(float width, float height)
        {
            if (!_menuOpen)
            {
                _menuRect = Rect.zero;
                return;
            }

            float menuHeight = MenuOrders.Length * MenuRowHeight + 10f;
            float mx = Mathf.Min(_menuGui.x, width - MenuWidth - 4f);
            float my = Mathf.Min(_menuGui.y, height - menuHeight - 4f);
            _menuRect = new Rect(mx, my, MenuWidth, menuHeight);
            Fill(_menuRect, new Color(0.12f, 0.12f, 0.11f, 0.95f));

            for (int i = 0; i < MenuOrders.Length; i++)
            {
                var order = MenuOrders[i];
                bool available = BattleState.IsImplemented(order);
                GUI.enabled = available;
                string key = Hotkey(order);
                string label = Loc.T(Loc.OrderKey(order)) + (key.Length > 0 ? $"   [{key}]" : "");
                if (GUI.Button(new Rect(mx + 5, my + 5 + i * MenuRowHeight, MenuWidth - 10, MenuRowHeight - 2), label, _button))
                {
                    ChooseOrder(order);
                }
            }

            GUI.enabled = true;
        }

        void DrawBoxSelection()
        {
            if (!_dragging || Mouse.current == null)
            {
                return;
            }

            Vector2 a = ToGui(_pressScreen);
            Vector2 b = ToGui(Mouse.current.position.ReadValue());
            var rect = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            Fill(rect, new Color(1f, 0.85f, 0.2f, 0.12f));
            Fill(new Rect(rect.x, rect.y, rect.width, 1), new Color(1f, 0.85f, 0.2f, 0.9f));
            Fill(new Rect(rect.x, rect.yMax - 1, rect.width, 1), new Color(1f, 0.85f, 0.2f, 0.9f));
            Fill(new Rect(rect.x, rect.y, 1, rect.height), new Color(1f, 0.85f, 0.2f, 0.9f));
            Fill(new Rect(rect.xMax - 1, rect.y, 1, rect.height), new Color(1f, 0.85f, 0.2f, 0.9f));
        }

        void DrawPendingHint()
        {
            if (_pendingOrder == OrderType.None || Mouse.current == null)
            {
                return;
            }

            Vector2 p = ToGui(Mouse.current.position.ReadValue());
            string hint = _pendingOrder == OrderType.Fire ? "ui.choose_enemy"
                : _pendingOrder == OrderType.Smoke ? "ui.choose_smoke"
                : "ui.choose_target";
            string text = Loc.T(Loc.OrderKey(_pendingOrder)) + " — " + Loc.T(hint);
            var rect = new Rect(p.x + 16, p.y + 12, 330, 22);
            Fill(rect, new Color(0f, 0f, 0f, 0.6f));
            GUI.Label(new Rect(rect.x + 6, rect.y + 2, rect.width - 12, 20), text, _small);
        }

        void DrawSightLabel()
        {
            if (!_sightActive || Mouse.current == null)
            {
                return;
            }

            Vector2 p = ToGui(Mouse.current.position.ReadValue());
            string text = $"{_sightDistance:0} m · {Loc.T(_sightVisible ? "ui.los_visible" : "ui.los_blocked")}";
            var rect = new Rect(p.x + 16, p.y - 30, 170, 22);
            Fill(rect, new Color(0f, 0f, 0f, 0.6f));
            GUI.Label(new Rect(rect.x + 6, rect.y + 2, rect.width - 12, 20), text, _small);
        }

        void DrawToast(float width, float height)
        {
            if (string.IsNullOrEmpty(_toast) || Time.unscaledTime > _toastUntil)
            {
                return;
            }

            int lines = _toast.Split('\n').Length;
            var rect = new Rect(width * 0.5f - 220, height - CardHeight - 40 - lines * 24, 440, 8 + lines * 24);
            Fill(rect, new Color(0f, 0f, 0f, 0.75f));
            GUI.Label(rect, _toast, _center);
        }

        void Fill(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _white);
            GUI.color = previous;
        }

        void OnDestroy()
        {
            if (_white != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_white);
                }
                else
                {
                    DestroyImmediate(_white);
                }
            }
        }
    }
}
