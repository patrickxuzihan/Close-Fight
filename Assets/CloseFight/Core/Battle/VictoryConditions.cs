using System.Collections.Generic;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.Battle
{
    public enum BattleResult : byte
    {
        InProgress = 0,
        GermanVictory = 1,
        SovietVictory = 2,
        /// <summary>No decision: the sector stays contested (D33).</summary>
        Contested = 3,
    }

    /// <summary>
    /// How a battle ends (M8), after Close Combat:
    /// - Objectives belong to whichever side alone has men inside them.
    /// - Each side's force morale falls with its losses and with objectives it
    ///   has lost; at the break point the side asks for a ceasefire and loses.
    /// - When time runs out (45 minutes by default, adjustable, D33) or a side
    ///   requests a ceasefire, objective points decide; a tie leaves the
    ///   sector contested.
    /// </summary>
    public sealed class VictoryConditions
    {
        public const float BreakPoint = 25f;
        public const float DefaultTimeLimitMinutes = 45f;
        const int ControlIntervalTicks = 10;

        readonly BattleState _battle;
        readonly Dictionary<Side, float> _startingStrength = new Dictionary<Side, float>();
        readonly Dictionary<Objective, Side> _startingOwner = new Dictionary<Objective, Side>();
        bool _initialized;

        public float TimeLimitSeconds { get; set; } = DefaultTimeLimitMinutes * 60f;
        public BattleResult Result { get; private set; } = BattleResult.InProgress;
        /// <summary>Localization key of why the battle ended.</summary>
        public string EndReasonKey { get; private set; } = "";
        public float EndedAt { get; private set; }
        public bool IsOver => Result != BattleResult.InProgress;

        public VictoryConditions(BattleState battle)
        {
            _battle = battle;
        }

        /// <summary>Fighting strength: men count 1 (wounded or out of control 0.5), a working vehicle 6.</summary>
        public float Strength(Side side)
        {
            float strength = 0f;
            foreach (var soldier in _battle.Soldiers)
            {
                if (soldier.Team.Side != side || !soldier.IsActive)
                {
                    continue;
                }

                strength += soldier.IsWounded || soldier.IsOutOfControl ? 0.5f : 1f;
            }

            foreach (var vehicle in _battle.Vehicles)
            {
                if (vehicle.Team.Side == side && vehicle.IsOperational)
                {
                    strength += 6f;
                }
            }

            return strength;
        }

        /// <summary>0..100. Losses and objectives taken by the enemy bring it down.</summary>
        public float ForceMorale(Side side)
        {
            EnsureInitialized();
            if (!_startingStrength.TryGetValue(side, out float start) || start <= 0f)
            {
                return 0f;
            }

            float morale = 100f * Strength(side) / start;
            foreach (var objective in _battle.Objectives)
            {
                if (_startingOwner[objective] == side && objective.Owner != side)
                {
                    morale -= objective.Value * 5f;
                }
            }

            return math.clamp(morale, 0f, 100f);
        }

        public int Points(Side side)
        {
            int points = 0;
            foreach (var objective in _battle.Objectives)
            {
                if (objective.Owner == side)
                {
                    points += objective.Value;
                }
            }

            return points;
        }

        public float StartingStrength(Side side)
        {
            EnsureInitialized();
            return _startingStrength.TryGetValue(side, out float s) ? s : 0f;
        }

        void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            foreach (var side in new[] { Side.German, Side.Soviet })
            {
                _startingStrength[side] = Strength(side);
            }

            foreach (var objective in _battle.Objectives)
            {
                _startingOwner[objective] = objective.Owner;
            }
        }

        /// <summary>Called by the battle after every tick.</summary>
        public void Step()
        {
            EnsureInitialized();
            if (IsOver)
            {
                return;
            }

            if (_battle.Clock.TickCount % ControlIntervalTicks == 0)
            {
                UpdateControl();
            }

            foreach (var side in new[] { Side.German, Side.Soviet })
            {
                if (_startingStrength[side] > 0f && ForceMorale(side) <= BreakPoint)
                {
                    End(side == Side.German ? BattleResult.SovietVictory : BattleResult.GermanVictory,
                        side == Side.German ? "result.reason.german_morale" : "result.reason.soviet_morale");
                    return;
                }
            }

            if (_battle.Clock.ElapsedSeconds >= TimeLimitSeconds)
            {
                End(ByPoints(), "result.reason.time");
            }
        }

        /// <summary>A side asks for a ceasefire: the battle ends now and objectives decide.</summary>
        public void RequestCeasefire(Side requester)
        {
            if (!IsOver)
            {
                UpdateControl();
                End(ByPoints(), requester == Side.German ? "result.reason.german_ceasefire" : "result.reason.soviet_ceasefire");
            }
        }

        BattleResult ByPoints()
        {
            int german = Points(Side.German);
            int soviet = Points(Side.Soviet);
            return german > soviet ? BattleResult.GermanVictory : soviet > german ? BattleResult.SovietVictory : BattleResult.Contested;
        }

        /// <summary>An objective changes hands when only one side has men inside it.</summary>
        public void UpdateControl()
        {
            foreach (var objective in _battle.Objectives)
            {
                bool german = false, soviet = false;
                foreach (var soldier in _battle.Soldiers)
                {
                    if (!soldier.IsActive || !objective.Contains(soldier.Position))
                    {
                        continue;
                    }

                    if (soldier.Team.Side == Side.German) german = true;
                    else soviet = true;
                }

                objective.Contested = german && soviet;
                if (german && !soviet) objective.Owner = Side.German;
                else if (soviet && !german) objective.Owner = Side.Soviet;
            }
        }

        void End(BattleResult result, string reasonKey)
        {
            Result = result;
            EndReasonKey = reasonKey;
            EndedAt = _battle.Clock.ElapsedSeconds;
            _battle.Clock.Paused = true;
        }
    }
}
