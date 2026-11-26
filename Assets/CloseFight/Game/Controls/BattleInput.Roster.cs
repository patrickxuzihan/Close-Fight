using System.Collections.Generic;
using CloseFight.Core.Units;
using CloseFight.Game.UI;
using UnityEngine;

namespace CloseFight.Game.Controls
{
    /// <summary>
    /// The roster (M18), laid out like the unit column of the recent Close Combat
    /// games but one level deeper: a slim column of platoons in the top-left corner,
    /// and beside it the squads of the platoon the player clicked. Each row is a
    /// small tile with the unit's picture, a name bar coloured by its state
    /// (green good, yellow pinned, orange shaken, red broken, grey gone) and a dark
    /// band below with one head per man and what the unit is doing.
    ///
    /// Platoon: click shows its squads; double-click selects the whole platoon and
    /// brings the camera to it. Squad: click selects (Shift adds); double-click also
    /// brings the camera to it. Drag the strip above the column to move the roster.
    /// </summary>
    public sealed partial class BattleInput
    {
        const float RosterTile = 76f;
        const float RosterBar = 140f;
        const float RosterRow = 54f;
        const float RosterRowGap = 2f;
        const float RosterHandle = 12f;
        const float RosterColumnGap = 6f;
        const string RosterPrefs = "CloseFight.RosterOrigin.v3";

        // The project renders in linear colour, and IMGUI fills take their colour as linear, so a colour
        // written as on screen (sRGB) shows a step too light unless converted. These are the olive the
        // unit pictures were judged on, RGB 153, 158, 117 on screen.
        static readonly Color RosterOlive = new Color(0.60f, 0.62f, 0.46f, 0.96f).linear;
        static readonly Color RosterOliveDark = new Color(0.30f, 0.31f, 0.22f, 1f).linear;
        static readonly Color RosterOliveLight = new Color(0.76f, 0.78f, 0.62f, 1f).linear;
        static readonly Color RosterBand = new Color(0.20f, 0.20f, 0.19f, 0.92f);
        static readonly Color RosterSelected = new Color(1f, 0.85f, 0.2f, 1f);

        static readonly Color StateGood = new Color(0.03f, 0.55f, 0.03f, 1f);
        static readonly Color StatePinned = new Color(0.75f, 0.62f, 0.02f, 1f);
        static readonly Color StateShaken = new Color(0.82f, 0.32f, 0.02f, 1f);
        static readonly Color StateBroken = new Color(0.62f, 0.05f, 0.03f, 1f);
        static readonly Color StateGone = new Color(0.38f, 0.38f, 0.36f, 1f);

        /// <summary>A platoon as the roster shows it; teams outside any platoon go in one group of their own.</summary>
        sealed class RosterGroup
        {
            public Platoon Platoon;
            public PlatoonType Type;
            public Side Side;
            public string Name;
            public string Company;
            public readonly List<Team> Teams = new List<Team>();
        }

        readonly List<RosterGroup> _rosterGroups = new List<RosterGroup>();
        RosterGroup _rosterOpen;
        Vector2 _rosterOrigin;
        bool _rosterPlaced;
        bool _rosterCollapsed;
        float _rosterScrollPlatoons;
        float _rosterScrollSquads;
        bool _rosterDragging;
        Vector2 _rosterDragOffset;
        Rect _rosterSquadsRect;
        GUIStyle _rosterName;
        GUIStyle _rosterStatus;
        GUIStyle _rosterHandleStyle;

        void BuildRoster()
        {
            _rosterGroups.Clear();
            _rosterOpen = null;
            _rosterScrollPlatoons = _rosterScrollSquads = 0f;
            RosterGroup others = null;
            var byPlatoon = new Dictionary<Platoon, RosterGroup>();
            foreach (var team in _playerTeams)
            {
                RosterGroup group;
                if (team.Platoon != null)
                {
                    if (!byPlatoon.TryGetValue(team.Platoon, out group))
                    {
                        group = new RosterGroup
                        {
                            Platoon = team.Platoon,
                            Type = team.Platoon.Type,
                            Side = team.Side,
                            Name = team.Platoon.Name,
                            Company = team.Platoon.Company,
                        };
                        byPlatoon.Add(team.Platoon, group);
                        _rosterGroups.Add(group);
                    }
                }
                else
                {
                    if (others == null)
                    {
                        others = new RosterGroup { Type = PlatoonType.Headquarters, Side = team.Side, Company = "" };
                    }

                    group = others;
                }

                group.Teams.Add(team);
            }

            _rosterGroups.Sort((a, b) => a.Platoon.Id.CompareTo(b.Platoon.Id));
            if (others != null)
            {
                _rosterGroups.Add(others);
            }

            // Open straight into the only group when there is just one (small test battles).
            if (_rosterGroups.Count == 1)
            {
                _rosterOpen = _rosterGroups[0];
            }
        }

        static string GroupName(RosterGroup group) => group.Platoon != null ? group.Name : Loc.T("platoon.other");

        static string PlatoonTypeKey(PlatoonType type)
        {
            switch (type)
            {
                case PlatoonType.Headquarters: return "platoon.headquarters";
                case PlatoonType.MachineGun: return "platoon.machine_gun";
                case PlatoonType.Mortar: return "platoon.mortar";
                case PlatoonType.AntiTank: return "platoon.anti_tank";
                case PlatoonType.Armor: return "platoon.armor";
                case PlatoonType.Pioneer: return "platoon.pioneer";
                default: return "platoon.rifle";
            }
        }

        /// <summary>For <see cref="Battle.AutoScreenshot"/>: open the n-th platoon in the roster.</summary>
        internal void DebugOpenPlatoon(int index)
        {
            if (index >= 0 && index < _rosterGroups.Count)
            {
                _rosterOpen = _rosterGroups[index];
            }
        }

        /// <summary>Number keys: the n-th squad of the open platoon, or else the n-th platoon.</summary>
        void RosterHotkey(int index, bool additive)
        {
            if (_rosterOpen != null)
            {
                if (index < _rosterOpen.Teams.Count)
                {
                    SelectTeam(_rosterOpen.Teams[index], additive);
                }
            }
            else if (index < _rosterGroups.Count)
            {
                SelectTeams(_rosterGroups[index].Teams, additive);
            }
        }

        /// <summary>Selects every team of a platoon that still has men (Shift: adds them).</summary>
        void SelectTeams(List<Team> teams, bool additive)
        {
            if (!additive)
            {
                _selection.Clear();
            }

            foreach (var team in teams)
            {
                if (team.ActiveCount > 0 && !_selection.Contains(team))
                {
                    _selection.Add(team);
                }
            }

            unitsView.SetSelection(_selection);
        }

        void FocusOn(Team team)
        {
            if (cameraController != null)
            {
                var c = team.ActiveCount > 0 ? team.Centroid : team.Members[0].Position;
                cameraController.FocusOn(new Vector3(c.x, 0f, c.y));
            }
        }

        void FocusOn(RosterGroup group)
        {
            Vector2 sum = Vector2.zero;
            int n = 0;
            foreach (var team in group.Teams)
            {
                if (team.ActiveCount > 0)
                {
                    sum += (Vector2)team.Centroid;
                    n++;
                }
            }

            if (n > 0 && cameraController != null)
            {
                cameraController.FocusOn(new Vector3(sum.x / n, 0f, sum.y / n));
            }
        }

        // ---------------------------------------------------------------- state colours

        /// <summary>How the unit is doing, 0 (good) to 4 (no men left): the name bar's colour.</summary>
        static int StateLevel(Team team)
        {
            if (team.ActiveCount == 0) return 4;
            if (team.IsBroken) return 3;
            if (team.IsShaken) return 2;

            int pinned = 0;
            foreach (var member in team.Members)
            {
                if (member.IsActive && member.IsPinned) pinned++;
            }

            return pinned * 2 >= team.ActiveCount || team.AverageSuppression > 45f ? 1 : 0;
        }

        static Color StateColor(int level)
        {
            switch (level)
            {
                case 1: return StatePinned;
                case 2: return StateShaken;
                case 3: return StateBroken;
                case 4: return StateGone;
                default: return StateGood;
            }
        }

        /// <summary>A platoon shows the state of its worst squad that still has men (grey once none has).</summary>
        static int StateLevel(RosterGroup group)
        {
            int worst = -1;
            foreach (var team in group.Teams)
            {
                int level = StateLevel(team);
                if (level < 4 && level > worst)
                {
                    worst = level;
                }
            }

            return worst < 0 ? 4 : worst;
        }

        // ---------------------------------------------------------------- drawing

        void RosterStyles()
        {
            if (_rosterName != null)
            {
                return;
            }

            _rosterName = new GUIStyle(_label)
            {
                fontSize = 13, fontStyle = FontStyle.Bold, clipping = TextClipping.Clip, wordWrap = false,
                alignment = TextAnchor.MiddleLeft, padding = new RectOffset(4, 2, 0, 0), margin = new RectOffset(0, 0, 0, 0),
                normal = { textColor = Color.white },
            };
            _rosterStatus = new GUIStyle(_rosterName) { fontSize = 11, alignment = TextAnchor.MiddleRight, padding = new RectOffset(2, 5, 0, 0) };
            _rosterHandleStyle = new GUIStyle(_rosterStatus) { fontSize = 10, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(4, 2, 0, 0) };
        }

        void PlaceRoster(float width, float height)
        {
            if (!_rosterPlaced)
            {
                _rosterPlaced = true;
                _rosterOrigin = new Vector2(4f, 30f);
                var parts = PlayerPrefs.GetString(RosterPrefs, "").Split(',');
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                if (parts.Length == 2
                    && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, inv, out float x)
                    && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, inv, out float y))
                {
                    _rosterOrigin = new Vector2(x, y);
                }
            }

            float columnWidth = RosterTile + RosterBar;
            _rosterOrigin.x = Mathf.Clamp(_rosterOrigin.x, 0f, Mathf.Max(0f, width - columnWidth));
            _rosterOrigin.y = Mathf.Clamp(_rosterOrigin.y, 26f, Mathf.Max(26f, height - RosterHandle - RosterRow));
        }

        void DrawRoster(float width, float height)
        {
            RosterStyles();
            PlaceRoster(width, height);
            var e = Event.current;
            float columnWidth = RosterTile + RosterBar;

            // The handle: drag it to move the roster; the arrow folds it away.
            var handle = new Rect(_rosterOrigin.x, _rosterOrigin.y, columnWidth, RosterHandle);
            var fold = new Rect(handle.xMax - 18f, handle.y, 18f, RosterHandle);
            if (e.type == EventType.MouseDown && e.button == 0 && fold.Contains(e.mousePosition))
            {
                _rosterCollapsed = !_rosterCollapsed;
                e.Use();
            }

            HandleRosterDrag(e, handle);
            Fill(handle, RosterOliveDark);
            GUI.Label(new Rect(handle.x, handle.y - 1f, handle.width - 18f, RosterHandle + 2f), Loc.T("ui.roster"), _rosterHandleStyle);
            GUI.Label(new Rect(fold.x, fold.y - 1f, fold.width, fold.height + 2f), _rosterCollapsed ? "▼" : "▲", _rosterHandleStyle);

            _rosterRect = handle;
            _rosterSquadsRect = Rect.zero;
            if (_rosterCollapsed)
            {
                return;
            }

            // Platoons.
            float top = handle.yMax + RosterRowGap;
            float maxHeight = height - top - 6f;
            var platoons = new Rect(_rosterOrigin.x, top, columnWidth, Mathf.Min(maxHeight, ColumnHeight(_rosterGroups.Count)));
            _rosterScrollPlatoons = ScrollColumn(e, platoons, _rosterScrollPlatoons, _rosterGroups.Count);
            GUI.BeginGroup(platoons);
            for (int i = 0; i < _rosterGroups.Count; i++)
            {
                var row = new Rect(0f, i * (RosterRow + RosterRowGap) - _rosterScrollPlatoons, columnWidth, RosterRow);
                if (row.yMax > 0f && row.y < platoons.height)
                {
                    DrawPlatoonRow(row, _rosterGroups[i]);
                }
            }

            GUI.EndGroup();
            _rosterRect = new Rect(handle.x, handle.y, columnWidth, platoons.yMax - handle.y);

            // The open platoon's squads, beside it.
            if (_rosterOpen != null)
            {
                var squads = new Rect(platoons.xMax + RosterColumnGap, top, columnWidth, Mathf.Min(maxHeight, ColumnHeight(_rosterOpen.Teams.Count)));
                if (squads.xMax > width)
                {
                    squads.x = _rosterOrigin.x - columnWidth - RosterColumnGap; // no room on the right: open to the left
                }

                _rosterScrollSquads = ScrollColumn(e, squads, _rosterScrollSquads, _rosterOpen.Teams.Count);
                GUI.BeginGroup(squads);
                for (int i = 0; i < _rosterOpen.Teams.Count; i++)
                {
                    var row = new Rect(0f, i * (RosterRow + RosterRowGap) - _rosterScrollSquads, columnWidth, RosterRow);
                    if (row.yMax > 0f && row.y < squads.height)
                    {
                        DrawSquadRow(row, _rosterOpen.Teams[i]);
                    }
                }

                GUI.EndGroup();
                _rosterSquadsRect = squads;
            }
        }

        static float ColumnHeight(int rows) => Mathf.Max(0f, rows * (RosterRow + RosterRowGap) - RosterRowGap);

        /// <summary>The mouse wheel scrolls a column that is taller than the screen.</summary>
        static float ScrollColumn(Event e, Rect column, float scroll, int rows)
        {
            float overflow = Mathf.Max(0f, ColumnHeight(rows) - column.height);
            if (e.type == EventType.ScrollWheel && column.Contains(e.mousePosition) && overflow > 0f)
            {
                scroll += e.delta.y * 12f;
                e.Use();
            }

            return Mathf.Clamp(scroll, 0f, overflow);
        }

        void HandleRosterDrag(Event e, Rect handle)
        {
            var grip = new Rect(handle.x, handle.y, handle.width - 18f, handle.height);
            switch (e.type)
            {
                case EventType.MouseDown when e.button == 0 && grip.Contains(e.mousePosition):
                    _rosterDragging = true;
                    _rosterDragOffset = e.mousePosition - _rosterOrigin;
                    e.Use();
                    break;
                case EventType.MouseDrag when _rosterDragging:
                    _rosterOrigin = e.mousePosition - _rosterDragOffset;
                    e.Use();
                    break;
                case EventType.MouseUp when _rosterDragging:
                    _rosterDragging = false;
                    var inv = System.Globalization.CultureInfo.InvariantCulture;
                    PlayerPrefs.SetString(RosterPrefs, string.Format(inv, "{0:0},{1:0}", _rosterOrigin.x, _rosterOrigin.y));
                    e.Use();
                    break;
            }
        }

        /// <summary>The olive tile with a bevelled edge that holds a unit's picture.</summary>
        void DrawTile(Rect tile)
        {
            Fill(tile, RosterOlive);
            Fill(new Rect(tile.x, tile.y, tile.width, 1f), RosterOliveLight);
            Fill(new Rect(tile.x, tile.y, 1f, tile.height), RosterOliveLight);
            Fill(new Rect(tile.x, tile.yMax - 1f, tile.width, 1f), RosterOliveDark);
            Fill(new Rect(tile.xMax - 1f, tile.y, 1f, tile.height), RosterOliveDark);
        }

        /// <summary>The name bar (coloured by state) over the dark status band.</summary>
        void DrawBars(Rect row, Color state, string name, out Rect band)
        {
            float half = Mathf.Floor(row.height * 0.5f);
            var bar = new Rect(row.x + RosterTile + 1f, row.y, RosterBar - 1f, half);
            band = new Rect(bar.x, bar.yMax, bar.width, row.height - half);
            Fill(bar, state);
            Fill(new Rect(bar.x, bar.y, bar.width, 1f), Color.Lerp(state, Color.white, 0.35f));
            Fill(band, RosterBand);
            ShadowLabel(bar, name, _rosterName);
        }

        /// <summary>White bold text with a dark drop shadow, readable on any bar colour.</summary>
        void ShadowLabel(Rect rect, string text, GUIStyle style)
        {
            var colour = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
            style.normal.textColor = colour;
            GUI.Label(rect, text, style);
        }

        void DrawPlatoonRow(Rect row, RosterGroup group)
        {
            var tile = new Rect(row.x, row.y, RosterTile, row.height);
            DrawTile(tile);
            RosterArt.DrawPlatoon(new Rect(tile.x + 2f, tile.y + 2f, tile.width - 4f, tile.height - 4f), group.Type, group.Side,
                group.Teams.Count > 0 ? group.Teams[0] : null);
            DrawBars(row, StateColor(StateLevel(group)), GroupName(group), out var band);
            ShadowLabel(band, Loc.T(PlatoonTypeKey(group.Type)), _rosterStatus);

            int selected = 0, alive = 0;
            foreach (var team in group.Teams)
            {
                if (team.ActiveCount == 0) continue;
                alive++;
                if (_selection.Contains(team)) selected++;
            }

            if (group == _rosterOpen)
            {
                // The open platoon: a white edge pointing to its squads on the right.
                Fill(new Rect(row.xMax - 4f, row.y, 4f, row.height), Color.white);
            }

            if (selected > 0)
            {
                Outline(row, selected == alive ? RosterSelected : new Color(RosterSelected.r, RosterSelected.g, RosterSelected.b, 0.5f));
            }

            if (alive == 0)
            {
                Fill(row, new Color(0f, 0f, 0f, 0.4f));
            }

            var e = Event.current;
            if (e.type == EventType.MouseDown && IsSecondary(e) && row.Contains(e.mousePosition))
            {
                // Right-click: the whole platoon, and the order menu.
                _rosterOpen = group;
                SelectTeams(group.Teams, false);
                OpenMenuFromRoster();
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 0 && row.Contains(e.mousePosition))
            {
                if (e.clickCount >= 2)
                {
                    _rosterOpen = group;
                    SelectTeams(group.Teams, e.shift);
                    FocusOn(group);
                }
                else
                {
                    _rosterOpen = _rosterOpen == group ? null : group;
                    _rosterScrollSquads = 0f;
                }

                e.Use();
            }
        }

        void DrawSquadRow(Rect row, Team team)
        {
            var tile = new Rect(row.x, row.y, RosterTile, row.height);
            DrawTile(tile);
            SquadPictures.Draw(new Rect(tile.x + 2f, tile.y + 2f, tile.width - 4f, tile.height - 4f), team);
            DrawStanceBadge(tile, team);
            DrawBars(row, RequestBarColor(team, StateColor(StateLevel(team))), Loc.TeamKind(team), out var band);

            // In the band: one head per man on the left, what the team is doing on the right
            // (or, while a delegated team asks to pull back, the buttons to answer it).
            SquadPictures.DrawHeads(new Rect(band.x + 3f, band.y + (band.height - 11f) * 0.5f, 80f, 11f), team, 8f, 11f);
            var right = new Rect(band.x + 80f, band.y, band.width - 80f, band.height);
            if (!DrawRequestButtons(new Rect(right.x + 1f, right.y + 3f, right.width - 4f, right.height - 6f), team, _smallButton))
            {
                ShadowLabel(right, TeamStateText(team), _rosterStatus);
            }

            bool selected = _selection.Contains(team);
            if (selected)
            {
                Outline(row, RosterSelected);
            }

            if (team.ActiveCount == 0)
            {
                Fill(row, new Color(0f, 0f, 0f, 0.4f));
            }

            var e = Event.current;
            if (e.type == EventType.MouseDown && IsSecondary(e) && row.Contains(e.mousePosition))
            {
                // Right-click: this squad (or the selection it belongs to), and the order menu.
                if (team.ActiveCount > 0 && !selected)
                {
                    SelectTeam(team, false);
                }

                OpenMenuFromRoster();
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 0 && row.Contains(e.mousePosition))
            {
                if (e.clickCount >= 2)
                {
                    FocusOn(team);
                    if (!selected)
                    {
                        SelectTeam(team, false);
                    }
                }
                else if (team.ActiveCount > 0 || selected)
                {
                    SelectTeam(team, e.shift);
                }

                e.Use();
            }
        }

        /// <summary>A right click, or Ctrl + click (Mac trackpads without secondary click).</summary>
        static bool IsSecondary(Event e) => e.button == 1 || (e.button == 0 && e.control);

        void Outline(Rect rect, Color color)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, 2f), color);
            Fill(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), color);
            Fill(new Rect(rect.x, rect.y, 2f, rect.height), color);
            Fill(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), color);
        }
    }
}
