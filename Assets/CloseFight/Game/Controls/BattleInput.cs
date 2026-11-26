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
using UnityEngine.SceneManagement;

namespace CloseFight.Game.Controls
{
    /// <summary>
    /// Close Combat style controls (D41):
    /// - Left-click a soldier to select his team; Shift adds; drag a box to select several.
    /// - Right-click opens the order menu; pick an order, then left-click the target.
    /// - Hotkeys skip the menu. Space pauses; orders still work while paused (D34).
    /// - The bottom bar lists the player's teams (CC layout); click a card to select it.
    /// Flow: briefing (choose a side) → deployment (place teams in the zone) → battle.
    /// UI is IMGUI for the prototype; it moves to UI Toolkit when the look is designed.
    /// </summary>
    public sealed partial class BattleInput : MonoBehaviour
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
            OrderType.Demolish, OrderType.LayMines, OrderType.ClearMines,
        };

        const float DragThreshold = 8f;
        const float TeamSpread = 8f;
        const float CardWidth = 170f;
        const float CardHeight = 64f;
        const float MenuWidth = 190f;
        const float MenuRowHeight = 24f;

        enum Stage : byte
        {
            Briefing,
            Deployment,
            Battle,
        }

        static readonly Color ZoneFill = new Color(0.95f, 0.85f, 0.35f, 0.16f);
        static readonly Color ZoneOutline = new Color(1f, 0.85f, 0.3f, 0.85f);

        BattleState _battle;
        Side _playerSide;
        Stage _stage = Stage.Battle;
        string _scenarioId = "";
        System.Action<Side> _chooseSide;
        public Side PlayerSide => _playerSide;
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
        // Off-map support (M13): the panel, and an asset waiting for its target.
        bool _supportOpen;
        Rect _supportRect;
        SupportAsset _pendingSupport;
        // Night (M14): a flare waiting for its point; the briefing's scenario switch.
        bool _pendingFlare;
        System.Action<int> _cycleScenario;
        Rect _rosterRect;
        Rect _infoRect;
        Texture2D _white;
        GUIStyle _label;
        GUIStyle _small;
        GUIStyle _smallButton;
        GUIStyle _center;
        GUIStyle _button;
        GUIStyle _smallCenter;
        GUIStyle _title;
        float _scale = 1f;

        /// <summary>
        /// Takes over a new battle. With <paramref name="chooseSide"/> the briefing opens
        /// first and reports the side the player picks; otherwise deployment (if the
        /// battle is deploying) or the battle itself starts right away.
        /// </summary>
        public void Bind(BattleState battle, Side playerSide, string scenarioId, System.Action<Side> chooseSide, System.Action<int> cycleScenario = null)
        {
            _cycleScenario = cycleScenario;
            _pendingFlare = false;
            _pendingSupport = null;
            _supportOpen = false;
            _battle = battle;
            _playerSide = playerSide;
            _scenarioId = scenarioId ?? "";
            _chooseSide = chooseSide;
            _reportHidden = false;
            _stage = chooseSide != null ? Stage.Briefing : battle.IsDeploying ? Stage.Deployment : Stage.Battle;
            if (_stage == Stage.Deployment)
            {
                ShowZones();
            }

            _playerTeams.Clear();
            foreach (var team in battle.Teams)
            {
                if (team.Side == playerSide)
                {
                    _playerTeams.Add(team);
                }
            }

            BuildRoster();
            ClearSelection();
        }

        /// <summary>The briefing closed without changing sides.</summary>
        public void EndBriefing()
        {
            _stage = _battle.IsDeploying ? Stage.Deployment : Stage.Battle;
            if (_stage == Stage.Deployment)
            {
                ShowZones();
            }
        }

        /// <summary>For <see cref="Battle.AutoScreenshot"/>: pick a side as if from the briefing.</summary>
        internal void DebugChooseSide(Side side)
        {
            if (_chooseSide != null)
            {
                _chooseSide(side);
            }
        }

        /// <summary>For <see cref="Battle.AutoScreenshot"/>: switch scenario in the briefing.</summary>
        internal void DebugCycleScenario(int delta) => _cycleScenario?.Invoke(delta);

        /// <summary>For <see cref="Battle.AutoScreenshot"/>: fire a flare for the player.</summary>
        internal void DebugFlare(float2 point) => _battle.Illumination.Fire(_playerSide, point, out _);

        internal void DebugStartBattle()
        {
            if (_stage == Stage.Deployment)
            {
                StartBattle();
            }
        }

        /// <summary>For <see cref="Battle.AutoScreenshot"/>: AI plan overlay and camera framing.</summary>
        internal void DebugView(bool aiPlan, string zoom, string center)
        {
            unitsView.ShowAiPlan = aiPlan;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            if (!string.IsNullOrEmpty(zoom))
            {
                worldCamera.orthographicSize = float.Parse(zoom, inv);
            }

            if (!string.IsNullOrEmpty(center))
            {
                var parts = center.Split(',');
                cameraController.FocusOn(new Vector3(float.Parse(parts[0], inv), 0f, float.Parse(parts[1], inv)));
            }
        }

        /// <summary>For <see cref="Battle.AutoScreenshot"/>: the player's engineers lay mines 40 m ahead, as if ordered.</summary>
        internal void DebugLayMines()
        {
            foreach (var team in _playerTeams)
            {
                if (team.Type == TeamType.Engineer)
                {
                    float2 ahead = team.Centroid + Soldier.DirectionOf(team.Facing) * 40f;
                    _battle.LayMinesBeforeBattle(team, ahead);
                }
            }
        }

        /// <summary>For <see cref="Battle.AutoScreenshot"/>: open the support panel and call the first asset on the first objective.</summary>
        internal void DebugSupport()
        {
            var assets = _battle.Support.Assets(_playerSide);
            if (assets.Count > 0 && _battle.Objectives.Count > 0)
            {
                CallSupport(assets[0], _battle.Objectives[0].Position);
            }

            _supportOpen = true;
        }

        void ShowZones() => unitsView.ShowDeployZones(_battle.DeployZones(_playerSide), ZoneFill, ZoneOutline);

        void StartBattle()
        {
            _battle.BeginBattle();
            unitsView.HideDeployZones();
            _stage = Stage.Battle;
            ShowToast(Loc.T("ui.battle_started"));
        }

        /// <summary>
        /// Deployment: the selected teams jump to the clicked spot, keeping their
        /// positions relative to each other, facing the enemy's side of the map.
        /// </summary>
        void DeploySelection(float2 target)
        {
            float2 center = float2.zero;
            foreach (var team in _selection)
            {
                center += Anchor(team);
            }

            center /= math.max(1, _selection.Count);
            float facing = Soldier.HeadingOf(math.normalizesafe(target - _battle.EdgePoint(_playerSide, target), new float2(1f, 0f)));
            var refusal = DeployRefusal.None;
            foreach (var team in _selection)
            {
                float2 spot = _selection.Count == 1 ? target : target + (Anchor(team) - center);
                var why = _battle.CheckDeploy(team, spot);
                if (why == DeployRefusal.None)
                {
                    _battle.Redeploy(team, spot, facing);
                    if (team.Delegation != null)
                    {
                        var stance = team.Delegation.Stance;
                        _battle.Undelegate(team);
                        _battle.Delegate(team, stance);
                    }
                }
                else if (refusal == DeployRefusal.None)
                {
                    refusal = why;
                }
            }

            if (refusal != DeployRefusal.None)
            {
                ShowToast(Loc.T(DeployRefusalKey(refusal)));
            }
        }

        static float2 Anchor(Team team) => team.Vehicle != null ? team.Vehicle.Position : team.Centroid;

        static string DeployRefusalKey(DeployRefusal refusal)
        {
            switch (refusal)
            {
                case DeployRefusal.OutsideZone: return "deploy.outside_zone";
                case DeployRefusal.NoRoomForVehicle: return "deploy.no_room";
                case DeployRefusal.GunInBuilding: return "deploy.gun_in_building";
                default: return "deploy.blocked";
            }
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

            if (keyboard.lKey.wasPressedThisFrame)
            {
                Loc.ToggleLanguage();
            }

            if (_stage == Stage.Briefing)
            {
                return;
            }

            if (_stage == Stage.Deployment && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                StartBattle();
                return;
            }

            if (_stage == Stage.Battle && keyboard.spaceKey.wasPressedThisFrame && !_battle.Victory.IsOver)
            {
                _battle.Clock.Paused = !_battle.Clock.Paused;
            }

            // Debug: show the enemy AI's plan (positions and fallbacks).
            if (keyboard.f9Key.wasPressedThisFrame)
            {
                unitsView.ShowAiPlan = !unitsView.ShowAiPlan;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (_pendingOrder != OrderType.None || _menuOpen || _pendingSupport != null || _supportOpen || _pendingFlare)
                {
                    _pendingOrder = OrderType.None;
                    _menuOpen = false;
                    _pendingSupport = null;
                    _supportOpen = false;
                    _pendingFlare = false;
                }
                else
                {
                    ClearSelection();
                }
            }

            // Number keys select what the roster shows: platoons, or the squads of the open platoon.
            for (int i = 0; i < 9; i++)
            {
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame)
                {
                    RosterHotkey(i, keyboard.shiftKey.isPressed);
                }
            }

            if (_selection.Count > 0 && _stage == Stage.Deployment && keyboard.yKey.wasPressedThisFrame)
            {
                ChooseOrder(OrderType.LayMines);
            }

            // M15: hand the selection to the AI, or change its stance.
            if (_selection.Count > 0 && (_stage == Stage.Battle || _stage == Stage.Deployment) && keyboard.zKey.wasPressedThisFrame)
            {
                DelegateSelection(null);
            }

            if (_selection.Count == 0 || _stage != Stage.Battle)
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
            if (keyboard.qKey.wasPressedThisFrame) ChooseOrder(OrderType.Demolish);
            if (keyboard.yKey.wasPressedThisFrame) ChooseOrder(OrderType.LayMines);
            if (keyboard.uKey.wasPressedThisFrame) ChooseOrder(OrderType.ClearMines);
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
                case OrderType.Demolish: return "Q";
                case OrderType.LayMines: return "Y";
                case OrderType.ClearMines: return "U";
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
            if (_stage == Stage.Briefing)
            {
                return;
            }

            if (_pendingFlare)
            {
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    _pendingFlare = false;
                }
                else if (mouse.leftButton.wasPressedThisFrame && !overUi)
                {
                    var flare = _battle.Illumination.Fire(_playerSide, ScreenToMap(screen), out var why);
                    ShowToast(Loc.T(flare != null ? "flare.fired" : why == FlareRefusal.Daylight ? "flare.daylight"
                        : why == FlareRefusal.NoFlaresLeft ? "flare.none_left" : "flare.out_of_reach"));
                    _pendingFlare = false;
                }

                return;
            }

            if (_pendingSupport != null)
            {
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    _pendingSupport = null;
                }
                else if (mouse.leftButton.wasPressedThisFrame && !overUi)
                {
                    CallSupport(_pendingSupport, ScreenToMap(screen));
                    _pendingSupport = null;
                }

                return;
            }

            // Ctrl + click works as a right click (Mac trackpads without secondary click).
            bool ctrl = Keyboard.current != null && Keyboard.current.ctrlKey.isPressed;
            bool secondary = mouse.rightButton.wasPressedThisFrame || (ctrl && mouse.leftButton.wasPressedThisFrame);
            // Right-click where the order should go (in deployment: mine laying and handing teams to the AI).
            if (secondary && !overUi && (_stage == Stage.Battle || _stage == Stage.Deployment))
            {
                if (_pendingOrder != OrderType.None)
                {
                    _pendingOrder = OrderType.None;
                }
                else if (_selection.Count > 0)
                {
                    OpenMenuAt(screen);
                }
            }

            if (mouse.leftButton.wasPressedThisFrame && !ctrl)
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
                else if (_stage == Stage.Deployment && !additive && _selection.Count > 0 && PickOwn(screen) == null)
                {
                    DeploySelection(ScreenToMap(screen));
                }
                else
                {
                    ClickSelect(screen, additive);
                }

                _dragging = false;
            }
        }

        Soldier PickOwn(Vector2 screen)
        {
            float radius = Mathf.Max(1.2f, worldCamera.orthographicSize * 0.025f);
            return unitsView.PickSoldier(ScreenToMap(screen), radius, _playerSide);
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
            if (_stage == Stage.Deployment)
            {
                // Mines prepared before the battle go down at once.
                if (order == OrderType.LayMines)
                {
                    bool laid = false;
                    foreach (var team in _selection)
                    {
                        if (_battle.LayMinesBeforeBattle(team, target))
                        {
                            laid = true;
                            break;
                        }
                    }

                    if (!laid)
                    {
                        ShowToast(Loc.T("deploy.mines_refused"));
                    }
                }

                return;
            }

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
                _battle.Undelegate(ordered[i]); // an order from the player takes the team back from the AI
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
            if (_stage == Stage.Briefing || (_battle.Victory.IsOver && !_reportHidden))
            {
                return true; // the briefing or the after-action report covers the screen
            }

            if (gui.y < 26f)
            {
                return true; // top bar
            }

            return (_menuOpen && _menuRect.Contains(gui)) || (_supportOpen && _supportRect.Contains(gui))
                || _rosterRect.Contains(gui) || _rosterSquadsRect.Contains(gui) || _infoRect.Contains(gui);
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
                _smallButton = new GUIStyle(GUI.skin.button) { fontSize = 11, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(2, 2, 1, 1) };
                _center = new GUIStyle(_label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                _button = new GUIStyle(GUI.skin.button) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
                _smallCenter = new GUIStyle(_small) { alignment = TextAnchor.MiddleCenter };
                _title = new GUIStyle(_center) { fontSize = 24 };
            }
        }

        /// <summary>Screenshots (-cfNoHud): the map alone, as a backdrop for HUD design work.</summary>
        internal bool DebugHideHud;

        void OnGUI()
        {
            if (_battle == null || DebugHideHud)
            {
                return;
            }

            EnsureStyles();
            GUI.matrix = Matrix4x4.Scale(new Vector3(_scale, _scale, 1f));
            float width = Screen.width / _scale;
            float height = Screen.height / _scale;

            if (_stage == Stage.Briefing)
            {
                DrawBriefing(width, height);
                return;
            }

            DrawObjectiveLabels();
            DrawMineLabels();
            DrawTeamLabels();
            DrawTopBar(width);
            DrawInfoPanel(width, height);
            DrawRoster(width, height);
            DrawBoxSelection();
            DrawPendingHint();
            DrawSightLabel();
            DrawSupportMarkers();
            DrawSupportPanel(width);
            DrawMenu(width, height);
            AnnounceRequests();
            DrawToast(width, height);
            DrawAfterAction(width, height);
        }

        float _ceasefireArmedUntil = float.NegativeInfinity;
        bool _reportHidden;

        static string Clock(float seconds)
        {
            int s = (int)seconds;
            return $"{s / 60:00}:{s % 60:00}";
        }

        void DrawTopBar(float width)
        {
            Fill(new Rect(0, 0, width, 26), new Color(0f, 0f, 0f, 0.55f));
            var victory = _battle.Victory;
            string night = _battle.IsNight ? " · " + Loc.T("ui.night") : "";
            GUI.Label(new Rect(10, 4, 240, 20), _stage == Stage.Deployment
                ? $"{Loc.T("ui.deployment")}  ·  {Clock(victory.TimeLimitSeconds)}{night}"
                : $"{Loc.T("ui.time")}  {Clock(_battle.Clock.ElapsedSeconds)} / {Clock(victory.TimeLimitSeconds)}{night}", _label);

            // Objective points and our own force morale (the enemy's stays hidden, as in CC).
            var enemy = _playerSide == Side.German ? Side.Soviet : Side.German;
            string score = $"{Loc.T("ui.objectives")} {Loc.T(SideKey(_playerSide))} {victory.Points(_playerSide)} : {victory.Points(enemy)} {Loc.T(SideKey(enemy))}";
            GUI.Label(new Rect(250, 4, 230, 20), score, _label);
            float morale = victory.ForceMorale(_playerSide);
            GUI.Label(new Rect(490, 5, 80, 20), Loc.T("ui.force_morale"), _small);
            DrawBar(new Rect(570, 10, 110, 8), morale / 100f, Color.Lerp(new Color(0.9f, 0.2f, 0.1f), new Color(0.3f, 0.8f, 0.3f), morale / 100f));
            GUI.Label(new Rect(688, 5, 50, 20), $"{morale:0}%", _small);

            GUI.Label(new Rect(745, 5, width - 1400, 20), Loc.T("ui.help"), _small);

            if (_stage == Stage.Battle && _battle.IsNight
                && GUI.Button(new Rect(width - 640, 2, 100, 22), $"{Loc.T("ui.flares")} {_battle.Illumination.FlaresLeft(_playerSide)}"))
            {
                _pendingFlare = true;
                _supportOpen = false;
                _menuOpen = false;
            }

            if (_stage == Stage.Battle && _battle.Support.Assets(_playerSide).Count > 0
                && GUI.Button(new Rect(width - 535, 2, 100, 22), $"{Loc.T("ui.support")} {_battle.Support.Points(_playerSide)}"))
            {
                _supportOpen = !_supportOpen;
                _menuOpen = false;
            }

            if (GUI.Button(new Rect(width - 425, 2, 70, 22), $"{GameSettings.FrameCap} FPS"))
            {
                GameSettings.ToggleFrameCap();
            }

            if (GUI.Button(new Rect(width - 350, 2, 95, 22), Loc.T(GameSettings.TrackpadMode ? "ui.trackpad_on" : "ui.trackpad_off")))
            {
                GameSettings.TrackpadMode = !GameSettings.TrackpadMode;
            }

            bool armed = Time.unscaledTime < _ceasefireArmedUntil;
            if (_stage == Stage.Deployment)
            {
                if (GUI.Button(new Rect(width - 250, 2, 150, 22), Loc.T("ui.start_battle")))
                {
                    StartBattle();
                }
            }
            else if (!victory.IsOver && GUI.Button(new Rect(width - 250, 2, 150, 22), Loc.T(armed ? "ui.ceasefire_confirm" : "ui.ceasefire")))
            {
                if (armed)
                {
                    victory.RequestCeasefire(_playerSide);
                }
                else
                {
                    _ceasefireArmedUntil = Time.unscaledTime + 3f; // click again within 3 s to confirm
                }
            }

            if (GUI.Button(new Rect(width - 90, 2, 80, 22), Loc.T("ui.language")))
            {
                Loc.ToggleLanguage();
            }

            if (_stage == Stage.Deployment)
            {
                Fill(new Rect(width * 0.5f - 440, 34, 880, 30), new Color(0.35f, 0.28f, 0.05f, 0.8f));
                GUI.Label(new Rect(width * 0.5f - 440, 34, 880, 30), Loc.T("ui.deploy_hint"), _smallCenter);
            }
            else if (_battle.Clock.Paused && !victory.IsOver)
            {
                Fill(new Rect(width * 0.5f - 160, 34, 320, 34), new Color(0.5f, 0.1f, 0.05f, 0.8f));
                GUI.Label(new Rect(width * 0.5f - 160, 34, 320, 34), Loc.T("ui.paused"), _center);
            }
        }

        /// <summary>The scenario's situation and goals, and the choice of side.</summary>
        void DrawBriefing(float width, float height)
        {
            var background = UiArt.BriefingBackground;
            if (background != null)
            {
                UiArt.DrawPicture(new Rect(0, 0, width, height), background, ScaleMode.ScaleAndCrop);
                Fill(new Rect(0, 0, width, height), new Color(0f, 0f, 0f, 0.25f));
            }
            else
            {
                Fill(new Rect(0, 0, width, height), new Color(0f, 0f, 0f, 0.55f));
            }

            var panel = new Rect(width * 0.5f - 340, height * 0.5f - 250, 680, 500);
            Fill(panel, new Color(0.1f, 0.09f, 0.08f, 0.95f));

            string key = "scenario." + _scenarioId;
            GUI.Label(new Rect(panel.x, panel.y + 18, panel.width, 34), Loc.T(key + ".title"), _title);
            GUI.Label(new Rect(panel.x, panel.y + 56, panel.width, 20), Loc.T(key + ".date"), _smallCenter);

            var wrap = new GUIStyle(_label) { wordWrap = true, fontSize = 14 };
            GUI.Label(new Rect(panel.x + 34, panel.y + 92, panel.width - 68, 150), Loc.T(key + ".brief"), wrap);

            var goal = new GUIStyle(_small) { wordWrap = true };
            GUI.Label(new Rect(panel.x + 34, panel.y + 250, panel.width - 68, 20), Loc.T("ui.goal_german"), _label);
            GUI.Label(new Rect(panel.x + 34, panel.y + 272, panel.width - 68, 44), Loc.T(key + ".goal_german"), goal);
            GUI.Label(new Rect(panel.x + 34, panel.y + 320, panel.width - 68, 20), Loc.T("ui.goal_soviet"), _label);
            GUI.Label(new Rect(panel.x + 34, panel.y + 342, panel.width - 68, 44), Loc.T(key + ".goal_soviet"), goal);

            if (GUI.Button(new Rect(panel.x + 40, panel.yMax - 92, 290, 36), Loc.T("ui.command_german")))
            {
                _chooseSide?.Invoke(Side.German);
            }

            if (GUI.Button(new Rect(panel.xMax - 330, panel.yMax - 92, 290, 36), Loc.T("ui.command_soviet")))
            {
                _chooseSide?.Invoke(Side.Soviet);
            }

            GUI.Label(new Rect(panel.x + 34, panel.yMax - 48, panel.width - 68, 36), Loc.T("ui.side_hint"), new GUIStyle(_small) { wordWrap = true, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.8f, 0.75f, 0.6f) } });

            if (GUI.Button(new Rect(panel.xMax - 90, panel.y + 10, 80, 22), Loc.T("ui.language")))
            {
                Loc.ToggleLanguage();
            }

            if (_cycleScenario != null)
            {
                if (GUI.Button(new Rect(panel.x + 10, panel.y + 10, 130, 22), Loc.T("ui.previous_scenario")))
                {
                    _cycleScenario(-1);
                }

                if (GUI.Button(new Rect(panel.x + 10, panel.y + 36, 130, 22), Loc.T("ui.next_scenario")))
                {
                    _cycleScenario(1);
                }
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
            string delegation = DelegationText(team);
            if (delegation != null)
            {
                state = delegation;
            }
            else if (vehicle != null && vehicle.Destroyed && team.ActiveCount == 0)
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
            else if (team.Task != null && team.Activity == TeamActivity.Idle)
            {
                // Engineering work on the spot, with how far along it is.
                state = $"{Loc.T("work." + Loc.OrderKey(team.Task.Type).Substring(6))} {team.Task.Progress * 100f:0}%";
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

            if (team.Type == TeamType.Engineer)
            {
                state += $" · {Loc.T("ui.charges")} {team.DemolitionCharges} · {Loc.T("ui.mines")} {team.MineLoads}";
            }

            return state;
        }

        static string SideKey(Side side) => side == Side.German ? "side.german" : "side.soviet";

        /// <summary>Objective names over their rings, colored by who holds them.</summary>
        void DrawObjectiveLabels()
        {
            for (int i = 0; i < _battle.Objectives.Count; i++)
            {
                var objective = _battle.Objectives[i];
                Vector3 screen = worldCamera.WorldToScreenPoint(unitsView.ObjectiveLabelPoint(i));
                if (screen.z < 0f)
                {
                    continue;
                }

                Vector2 gui = ToGui(new Vector2(screen.x, screen.y));
                string text = $"{Loc.T(objective.NameKey)} ({objective.Value})" + (objective.Contested ? " · " + Loc.T("ui.contested") : "");
                var rect = new Rect(gui.x - 90, gui.y - 22, 180, 18);
                var color = objective.Owner == Side.German ? new Color(0.25f, 0.25f, 0.25f, 0.8f) : new Color(0.55f, 0.08f, 0.05f, 0.8f);
                Fill(rect, color);
                GUI.Label(rect, text, _smallCenter);
            }
        }

        /// <summary>Small "minefield" tags over the minefields the player knows about.</summary>
        void DrawMineLabels()
        {
            foreach (var field in unitsView.KnownMineFields())
            {
                Vector3 screen = worldCamera.WorldToScreenPoint(new Vector3(field.Center.x, 0f, field.Center.y));
                if (screen.z < 0f)
                {
                    continue;
                }

                Vector2 gui = ToGui(new Vector2(screen.x, screen.y));
                var rect = new Rect(gui.x - 30, gui.y - 9, 60, 18);
                bool own = field.Owner == _playerSide;
                Fill(rect, own ? new Color(0.45f, 0.38f, 0.08f, 0.75f) : new Color(0.55f, 0.08f, 0.05f, 0.8f));
                GUI.Label(rect, Loc.T("ui.minefield"), _smallCenter);
            }
        }

        /// <summary>
        /// The after-action report (M8): result and why, objectives, both sides'
        /// losses, and the player's men with what became of them.
        /// </summary>
        void DrawAfterAction(float width, float height)
        {
            var victory = _battle.Victory;
            if (!victory.IsOver || _reportHidden)
            {
                return;
            }

            var panel = new Rect(width * 0.5f - 360, 60, 720, height - 170);
            Fill(new Rect(0, 0, width, height), new Color(0f, 0f, 0f, 0.45f));
            Fill(panel, new Color(0.10f, 0.10f, 0.09f, 0.96f));

            string title;
            switch (victory.Result)
            {
                case BattleResult.GermanVictory: title = Loc.T("result.german_victory"); break;
                case BattleResult.SovietVictory: title = Loc.T("result.soviet_victory"); break;
                default: title = Loc.T("result.contested"); break;
            }

            GUI.Label(new Rect(panel.x, panel.y + 10, panel.width, 30), title, _title);
            GUI.Label(new Rect(panel.x, panel.y + 42, panel.width, 20),
                $"{Loc.T(victory.EndReasonKey)} · {Loc.T("ui.time")} {Clock(victory.EndedAt)}", _smallCenter);

            float y = panel.y + 72;
            GUI.Label(new Rect(panel.x + 20, y, 300, 20), Loc.T("ui.objectives"), _label);
            y += 20;
            foreach (var objective in _battle.Objectives)
            {
                GUI.Label(new Rect(panel.x + 30, y, 400, 18), $"{Loc.T(objective.NameKey)} ({objective.Value}) — {Loc.T(SideKey(objective.Owner))}", _small);
                y += 17;
            }

            y += 8;
            GUI.Label(new Rect(panel.x + 20, y, 300, 20), Loc.T("ui.losses"), _label);
            y += 22;
            string[] headers = { "", "status.dead", "status.incapacitated", "status.wounded", "status.fled", "ui.unhurt", "ui.vehicles_lost" };
            float[] columns = { 30, 150, 240, 340, 430, 530, 620 };
            for (int i = 1; i < headers.Length; i++)
            {
                GUI.Label(new Rect(panel.x + columns[i], y, 95, 18), Loc.T(headers[i]), _small);
            }

            y += 18;
            foreach (var side in new[] { Side.German, Side.Soviet })
            {
                int dead = 0, disabled = 0, wounded = 0, fled = 0, unhurt = 0, vehicles = 0;
                foreach (var soldier in _battle.Soldiers)
                {
                    if (soldier.Team.Side != side) continue;
                    switch (soldier.Status)
                    {
                        case SoldierStatus.Dead: dead++; break;
                        case SoldierStatus.Incapacitated: disabled++; break;
                        case SoldierStatus.Wounded: wounded++; break;
                        case SoldierStatus.Fled: fled++; break;
                        default: unhurt++; break;
                    }
                }

                foreach (var vehicle in _battle.Vehicles)
                {
                    if (vehicle.Team.Side == side && !vehicle.IsOperational) vehicles++;
                }

                int[] values = { 0, dead, disabled, wounded, fled, unhurt, vehicles };
                GUI.Label(new Rect(panel.x + columns[0], y, 120, 18), Loc.T(SideKey(side)), _small);
                for (int i = 1; i < values.Length; i++)
                {
                    GUI.Label(new Rect(panel.x + columns[i], y, 90, 18), values[i].ToString(), _small);
                }

                y += 18;
            }

            y += 10;
            GUI.Label(new Rect(panel.x + 20, y, 400, 20), Loc.T("ui.your_men"), _label);
            y += 22;
            var men = new List<Soldier>();
            foreach (var soldier in _battle.Soldiers)
            {
                if (soldier.Team.Side == _playerSide) men.Add(soldier);
            }

            men.Sort((a, b) => (b.Kills * 10 + b.VehiclesKnockedOut * 30).CompareTo(a.Kills * 10 + a.VehiclesKnockedOut * 30));
            float listBottom = panel.yMax - 50;
            foreach (var soldier in men)
            {
                if (y > listBottom - 16)
                {
                    GUI.Label(new Rect(panel.x + 30, y, 300, 18), "…", _small);
                    break;
                }

                string record = soldier.VehiclesKnockedOut > 0
                    ? $"{Loc.T("ui.kills")} {soldier.Kills} · {Loc.T("ui.tanks")} {soldier.VehiclesKnockedOut}"
                    : $"{Loc.T("ui.kills")} {soldier.Kills}";
                GUI.Label(new Rect(panel.x + 30, y, 300, 18), $"{Loc.T(soldier.RankKey)} {soldier.FullName}", _small);
                GUI.Label(new Rect(panel.x + 330, y, 160, 18), soldier.Team.DisplayName, _small);
                GUI.Label(new Rect(panel.x + 490, y, 110, 18), Loc.T(Loc.StatusKey(soldier.Status)), _small);
                GUI.Label(new Rect(panel.x + 600, y, 110, 18), record, _small);
                y += 16;
            }

            if (GUI.Button(new Rect(panel.x + panel.width * 0.5f - 210, panel.yMax - 40, 200, 28), Loc.T("ui.play_again")))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            if (GUI.Button(new Rect(panel.x + panel.width * 0.5f + 10, panel.yMax - 40, 200, 28), Loc.T("ui.view_battlefield")))
            {
                _reportHidden = true;
            }
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
                // Tanks and guns live on their shells (a StuG's crew carries no small arms at all).
                bool outOfAmmo = team.Gun != null ? team.Gun.AmmoAp + team.Gun.AmmoHe == 0 : team.TotalAmmo == 0;
                if (outOfAmmo) tags.Add(Loc.T("ui.ammo_out"));
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
            bool delegated = team.Delegation != null;
            float panelHeight = 58f + team.Members.Count * rowHeight + (delegated ? 22f : 0f);
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
            if (delegated)
            {
                var d = team.Delegation;
                string line = string.Format(Loc.T("delegation.info"), Loc.T(StanceKey(d.Stance)));
                if (d.Withdrawals > 0)
                {
                    line += " · " + string.Format(Loc.T("delegation.lines"), d.Withdrawals);
                }

                GUI.Label(new Rect(panel.x + 8, y - 2f, 200f, 18f), line, _small);
                DrawRequestButtons(new Rect(panel.x + panelWidth - 128f, y - 3f, 120f, 19f), team, _smallButton);
                y += 22f;
            }

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

            // What fits the spot clicked comes first; engineering orders only with engineers selected.
            var orders = MenuOrderList();
            const float headerHeight = 20f;
            float menuHeight = headerHeight + (orders.Count + DelegationMenuRows()) * MenuRowHeight + 16f;
            float mx = Mathf.Min(_menuGui.x, width - MenuWidth - 4f);
            float my = Mathf.Min(_menuGui.y, height - menuHeight - 4f);
            _menuRect = new Rect(mx, my, MenuWidth, menuHeight);
            Fill(_menuRect, new Color(0.12f, 0.12f, 0.11f, 0.95f));
            GUI.Label(new Rect(mx + 8, my + 3, MenuWidth - 16, headerHeight - 2), MenuHeader(), _small);
            float top = my + headerHeight;

            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                bool available = BattleState.IsImplemented(order) && FitsSpot(order)
                    && (!Engineering.IsEngineeringOrder(order) || _selection.Exists(t => Engineering.CanAttempt(t, order)));
                GUI.enabled = available;
                string key = Hotkey(order);
                string label = Loc.T(Loc.OrderKey(order)) + (key.Length > 0 ? $"   [{key}]" : "");
                if (GUI.Button(new Rect(mx + 5, top + 5 + i * MenuRowHeight, MenuWidth - 10, MenuRowHeight - 2), label, _button))
                {
                    ChooseMenuOrder(order);
                }
            }

            GUI.enabled = true;

            // M15: hand to the AI, below a thin line.
            float delegationY = top + 5 + orders.Count * MenuRowHeight + 6f;
            Fill(new Rect(mx + 8, delegationY - 4f, MenuWidth - 16, 1f), new Color(1f, 1f, 1f, 0.25f));
            DrawDelegationMenu(mx + 5, delegationY);
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

        void CallSupport(SupportAsset asset, float2 target)
        {
            var mission = _battle.Support.Request(_playerSide, asset.Id, target, out var refusal);
            if (mission == null)
            {
                ShowToast(Loc.T(SupportRefusalKey(refusal)));
                return;
            }

            float wait = mission.ArrivesAt - _battle.Clock.ElapsedSeconds;
            ShowToast(string.Format(Loc.T("support.called"), Loc.T(asset.NameKey), mission.Observer.FullName, Clock(wait)));
        }

        static string SupportRefusalKey(SupportRefusal refusal)
        {
            switch (refusal)
            {
                case SupportRefusal.NoObserver: return "support.no_observer";
                case SupportRefusal.NotEnoughPoints: return "support.no_points";
                case SupportRefusal.WrongTimeOfDay: return "support.wrong_time";
                default: return "support.impossible";
            }
        }

        /// <summary>The support panel: each asset with its cost and how long it takes to arrive.</summary>
        void DrawSupportPanel(float width)
        {
            if (!_supportOpen || _stage != Stage.Battle)
            {
                _supportRect = Rect.zero;
                return;
            }

            var assets = _battle.Support.Assets(_playerSide);
            int points = _battle.Support.Points(_playerSide);
            _supportRect = new Rect(width - 420, 30, 410, 74 + assets.Count * 30);
            Fill(_supportRect, new Color(0.1f, 0.1f, 0.09f, 0.95f));
            GUI.Label(new Rect(_supportRect.x + 10, _supportRect.y + 6, 390, 20), string.Format(Loc.T("support.title"), points), _label);
            GUI.Label(new Rect(_supportRect.x + 10, _supportRect.y + 26, 390, 36), Loc.T("support.hint"), new GUIStyle(_small) { wordWrap = true });
            for (int i = 0; i < assets.Count; i++)
            {
                var asset = assets[i];
                bool available = _battle.Support.Available(asset);
                GUI.enabled = available && points >= asset.Cost;
                string when = available ? $"{asset.DelayMin / 60f:0.#}–{asset.DelayMax / 60f:0.#} {Loc.T("support.minutes")}" : Loc.T("support.wrong_time_short");
                string label = $"{Loc.T(asset.NameKey)}   {asset.Cost} {Loc.T("support.points")} · {when}";
                if (GUI.Button(new Rect(_supportRect.x + 8, _supportRect.y + 66 + i * 30, 394, 26), label, _button))
                {
                    _pendingSupport = asset;
                    _supportOpen = false;
                }
            }

            GUI.enabled = true;
        }

        /// <summary>Our own called missions: where they will land and how long until they do.</summary>
        void DrawSupportMarkers()
        {
            float now = _battle.Clock.ElapsedSeconds;
            foreach (var mission in _battle.Support.Missions)
            {
                if (mission.Side != _playerSide)
                {
                    continue;
                }

                Vector3 screen = worldCamera.WorldToScreenPoint(new Vector3(mission.Target.x, 0f, mission.Target.y));
                if (screen.z < 0f)
                {
                    continue;
                }

                Vector2 gui = ToGui(new Vector2(screen.x, screen.y));
                string text = mission.Firing ? $"{Loc.T(mission.Asset.NameKey)} · {Loc.T("support.firing")}"
                    : $"{Loc.T(mission.Asset.NameKey)} · {Clock(mission.ArrivesAt - now)}";
                var rect = new Rect(gui.x - 110, gui.y - 30, 220, 18);
                Fill(rect, new Color(0.5f, 0.25f, 0.05f, 0.85f));
                GUI.Label(rect, text, _smallCenter);
            }
        }

        void DrawPendingHint()
        {
            if (_pendingFlare && Mouse.current != null)
            {
                Vector2 f = ToGui(Mouse.current.position.ReadValue());
                var flareBox = new Rect(f.x + 16, f.y + 12, 380, 22);
                Fill(flareBox, new Color(0.3f, 0.3f, 0.1f, 0.85f));
                GUI.Label(new Rect(flareBox.x + 6, flareBox.y + 2, flareBox.width - 12, 20), Loc.T("flare.choose_target"), _small);
                return;
            }

            if (_pendingSupport != null && Mouse.current != null)
            {
                Vector2 m = ToGui(Mouse.current.position.ReadValue());
                var box = new Rect(m.x + 16, m.y + 12, 380, 22);
                Fill(box, new Color(0.35f, 0.15f, 0f, 0.8f));
                GUI.Label(new Rect(box.x + 6, box.y + 2, box.width - 12, 20), Loc.T(_pendingSupport.NameKey) + " — " + Loc.T("support.choose_target"), _small);
                return;
            }

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
            // Near the top, clear of the roster window wherever the player has put it.
            var rect = new Rect(width * 0.5f - 220, 76f, 440, 8 + lines * 24);
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
