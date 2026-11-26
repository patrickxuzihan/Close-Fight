using System.Collections.Generic;
using CloseFight.Core.Units;
using CloseFight.Game.UI;
using UnityEngine;

namespace CloseFight.Game.Controls
{
    /// <summary>
    /// Handing teams to the AI (M15, D14/D22/D53). Z hands the selected teams over
    /// (elastic defence first; Z again changes the stance: elastic, delay, hold);
    /// the order menu has the three stances and "take back command". Any order
    /// takes a team back. A delegated team that asks to pull back says so in a
    /// toast and in the roster, where the player approves or refuses; without an
    /// answer it goes after 20 seconds.
    /// </summary>
    public sealed partial class BattleInput
    {
        static readonly Stance[] StanceCycle = { Stance.Elastic, Stance.Delay, Stance.Hold };
        static readonly Color RequestFlash = new Color(0.95f, 0.55f, 0.05f, 1f);

        readonly HashSet<Team> _announcedRequests = new HashSet<Team>();
        GUIStyle _badge;

        public static string StanceKey(Stance stance)
        {
            switch (stance)
            {
                case Stance.Delay: return "stance.delay";
                case Stance.Hold: return "stance.hold";
                default: return "stance.elastic";
            }
        }

        /// <summary>
        /// Hands the selection to the AI with <paramref name="stance"/>, or (null) the next stance in
        /// the cycle, to hold near <paramref name="anchor"/> (a spot right-clicked) or where it is.
        /// </summary>
        void DelegateSelection(Stance? stance, Unity.Mathematics.float2? anchor = null)
        {
            _menuOpen = false;
            if (_selection.Count == 0)
            {
                return;
            }

            if (stance == null)
            {
                var first = _selection[0].Delegation;
                stance = first == null ? Stance.Elastic : StanceCycle[(System.Array.IndexOf(StanceCycle, first.Stance) + 1) % StanceCycle.Length];
            }

            int handed = 0;
            foreach (var team in _selection)
            {
                if (_battle.Delegate(team, stance.Value, anchor) != null)
                {
                    handed++;
                }
            }

            if (handed > 0)
            {
                string who = _selection.Count == 1 ? Loc.TeamKind(_selection[0]) : string.Format(Loc.T("ui.teams_count"), handed);
                ShowToast(string.Format(Loc.T("delegation.handed"), who, Loc.T(StanceKey(stance.Value))));
            }
        }

        void UndelegateSelection()
        {
            _menuOpen = false;
            foreach (var team in _selection)
            {
                _battle.Undelegate(team);
            }

            ShowToast(Loc.T("delegation.taken_back"));
        }

        /// <summary>A toast the first time each delegated team asks to pull back.</summary>
        void AnnounceRequests()
        {
            if (_stage != Stage.Battle)
            {
                return;
            }

            float now = _battle.Clock.ElapsedSeconds;
            foreach (var team in _playerTeams)
            {
                var d = team.Delegation;
                if (d != null && d.IsRequesting)
                {
                    if (_announcedRequests.Add(team))
                    {
                        ShowToast(string.Format(Loc.T("delegation.request_toast"), RosterName(team), Mathf.CeilToInt(d.SecondsToWithdraw(now))));
                    }
                }
                else
                {
                    _announcedRequests.Remove(team);
                }
            }
        }

        string RosterName(Team team)
        {
            string platoon = team.Platoon != null ? team.Platoon.Name + " · " : "";
            return platoon + team.DisplayName;
        }

        /// <summary>What a delegated team is doing, for the status line; null when its ordinary state says it better.</summary>
        string DelegationText(Team team)
        {
            var d = team.Delegation;
            if (d == null || team.ActiveCount == 0)
            {
                return null;
            }

            float now = _battle.Clock.ElapsedSeconds;
            switch (d.State)
            {
                case DelegationState.RequestingWithdrawal:
                    return string.Format(Loc.T("delegation.requesting"), Mathf.CeilToInt(d.SecondsToWithdraw(now)));
                case DelegationState.Withdrawing:
                    return Loc.T("delegation.withdrawing");
                case DelegationState.Counterattacking:
                    return Loc.T("delegation.counterattacking");
                case DelegationState.TakingPosition:
                    return team.Activity != Core.Units.TeamActivity.Idle ? Loc.T("delegation.taking_position") : null;
                default:
                    return null;
            }
        }

        /// <summary>The order menu's rows for delegation, below the orders. Returns the rows it added.</summary>
        int DrawDelegationMenu(float x, float y)
        {
            int rows = 0;
            foreach (var stance in StanceCycle)
            {
                string key = stance == Stance.Elastic ? "   [Z]" : "";
                string label = string.Format(Loc.T("delegation.menu"), Loc.T(StanceKey(stance))) + key;
                if (GUI.Button(new Rect(x, y + rows * MenuRowHeight, MenuWidth - 10, MenuRowHeight - 2), label, _button))
                {
                    DelegateSelection(stance, MenuAnchor);
                }

                rows++;
            }

            if (_selection.Exists(t => t.Delegation != null))
            {
                if (GUI.Button(new Rect(x, y + rows * MenuRowHeight, MenuWidth - 10, MenuRowHeight - 2), Loc.T("delegation.take_back"), _button))
                {
                    UndelegateSelection();
                }

                rows++;
            }

            return rows;
        }

        int DelegationMenuRows() => StanceCycle.Length + (_selection.Exists(t => t.Delegation != null) ? 1 : 0);

        /// <summary>The stance in the corner of a delegated team's picture.</summary>
        void DrawStanceBadge(Rect tile, Team team)
        {
            var d = team.Delegation;
            if (d == null || team.ActiveCount == 0)
            {
                return;
            }

            if (_badge == null)
            {
                _badge = new GUIStyle(_rosterStatus) { alignment = TextAnchor.MiddleCenter, fontSize = 10, padding = new RectOffset(0, 0, 0, 0) };
            }

            string text = Loc.T(StanceKey(d.Stance) + "_short");
            float width = Mathf.Max(24f, _badge.CalcSize(new GUIContent(text)).x + 6f);
            var rect = new Rect(tile.x + 2f, tile.y + 2f, width, 13f);
            Fill(rect, new Color(0.10f, 0.18f, 0.32f, 0.92f));
            GUI.Label(rect, text, _badge);
        }

        /// <summary>
        /// Approve / refuse buttons for a request to pull back, in <paramref name="area"/>.
        /// Returns true if it drew them (the team is asking).
        /// </summary>
        bool DrawRequestButtons(Rect area, Team team, GUIStyle style)
        {
            var d = team.Delegation;
            if (d == null || !d.IsRequesting)
            {
                return false;
            }

            float half = Mathf.Floor(area.width * 0.5f);
            if (GUI.Button(new Rect(area.x, area.y, half - 1f, area.height), Loc.T("delegation.approve"), style))
            {
                _battle.AnswerWithdrawal(team, true);
            }

            if (GUI.Button(new Rect(area.x + half, area.y, area.width - half, area.height), Loc.T("delegation.refuse"), style))
            {
                _battle.AnswerWithdrawal(team, false);
                ShowToast(string.Format(Loc.T("delegation.refused_toast"), RosterName(team)));
            }

            return true;
        }

        /// <summary>
        /// Screenshots (-cfDelegate): hands the open platoon's squads to the AI with the
        /// three stances in turn, and shakes the first one (comrades lost) so that it asks
        /// to pull back; selects it so the info panel shows the answer buttons too.
        /// </summary>
        internal void DebugDelegate()
        {
            if (_rosterOpen == null)
            {
                return;
            }

            var teams = _rosterOpen.Teams;
            for (int i = 0; i < teams.Count; i++)
            {
                _battle.Delegate(teams[i], StanceCycle[i % StanceCycle.Length]);
            }

            if (teams.Count > 0)
            {
                var first = teams[0];
                for (int i = 0; i < 8; i++)
                {
                    _battle.Psychology.OnCasualty(first.Members[first.Members.Count - 1], SoldierStatus.Dead, _battle.Clock.ElapsedSeconds);
                }

                SelectTeam(first, false);
            }
        }

        /// <summary>The name bar's colour: flashing amber while the team asks to pull back.</summary>
        static Color RequestBarColor(Team team, Color normal)
        {
            var d = team.Delegation;
            if (d == null || !d.IsRequesting)
            {
                return normal;
            }

            return Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.4f ? RequestFlash : normal;
        }
    }
}
