using System;
using System.Collections.Generic;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.AI
{
    /// <summary>
    /// Everything the AI of one side is allowed to know (D17: the AI does not
    /// cheat). It sees its own troops, the enemies its side can currently see,
    /// the gunfire its side has heard, the map, the objectives and the edge the
    /// enemy attacks from (scenario knowledge). It never sees hidden enemies.
    /// The commander only talks to the battle through this class, so cheating
    /// is impossible by construction, not just by discipline.
    /// </summary>
    public sealed class AiView
    {
        readonly BattleState _battle;

        public Side Side { get; }
        public Side Enemy => Side == Side.German ? Side.Soviet : Side.German;

        public AiView(BattleState battle, Side side)
        {
            _battle = battle;
            Side = side;
        }

        public TacticalMap Map => _battle.Map;
        public float Time => _battle.Clock.ElapsedSeconds;
        /// <summary>Whether smoke now hangs across the line between two points (smoke is plain to see).</summary>
        public bool SmokeBlocks(float2 from, float2 to) => _battle.Smoke.Blocks(from, to, _battle.Clock.ElapsedSeconds);

        /// <summary>Battle time left (the clock is on every player's screen).</summary>
        public float TimeLeft => _battle.Victory.TimeLimitSeconds - _battle.Clock.ElapsedSeconds;
        /// <summary>Our own force morale (a commander knows the state of his own men; the enemy's stays hidden).</summary>
        public float OwnForceMorale => _battle.Victory.ForceMorale(Side);
        public IReadOnlyList<Objective> Objectives => _battle.Objectives;

        /// <summary>The map edge the enemy comes from.</summary>
        public MapEdge EnemyEdge => _battle.RetreatEdgeOf(Enemy);
        public MapEdge OwnEdge => _battle.RetreatEdgeOf(Side);
        public float2 OwnEdgePoint(float2 from) => _battle.EdgePoint(Side, from);

        public IEnumerable<Team> OwnTeams
        {
            get
            {
                foreach (var team in _battle.Teams)
                {
                    if (team.Side == Side)
                    {
                        yield return team;
                    }
                }
            }
        }

        /// <summary>Enemy soldiers this side can see right now (on foot or in visible vehicles).</summary>
        public IEnumerable<Soldier> VisibleEnemies
        {
            get
            {
                foreach (var soldier in _battle.Soldiers)
                {
                    if (soldier.Team.Side != Side && soldier.IsActive && _battle.Visibility.CanSee(Side, soldier))
                    {
                        yield return soldier;
                    }
                }
            }
        }

        public IEnumerable<Vehicle> VisibleEnemyVehicles
        {
            get
            {
                foreach (var vehicle in _battle.Vehicles)
                {
                    if (vehicle.Team.Side != Side && vehicle.IsOperational && _battle.Visibility.CanSee(Side, vehicle))
                    {
                        yield return vehicle;
                    }
                }
            }
        }

        /// <summary>Where this side has heard enemy fire it could not see.</summary>
        public IEnumerable<SoundMarker> HeardEnemies
        {
            get
            {
                foreach (var marker in _battle.Visibility.SoundMarkers)
                {
                    if (marker.Listener == Side)
                    {
                        yield return marker;
                    }
                }
            }
        }

        public bool Order(Team team, OrderType order, float2 target, Func<int, float> pathCost = null)
        {
            if (team.Side != Side)
            {
                throw new InvalidOperationException("An AI may only command its own side.");
            }

            return _battle.TryIssueOrderAvoiding(team, order, target, pathCost, out _);
        }

        /// <summary>Minefields we know about: our own, and the enemy's we have found (M12).</summary>
        public IEnumerable<MineField> KnownMineFields
        {
            get
            {
                foreach (var field in _battle.Engineering.MineFields)
                {
                    if (field.KnownTo(Side))
                    {
                        yield return field;
                    }
                }
            }
        }

        /// <summary>
        /// Deployment only: our engineers lay a minefield prepared before the battle
        /// (within 150 m of the team). False if not allowed or no mines are left.
        /// </summary>
        public bool LayMinesBeforeBattle(Team team, float2 point)
        {
            if (team.Side != Side)
            {
                throw new InvalidOperationException("An AI may only command its own side.");
            }

            return _battle.LayMinesBeforeBattle(team, point);
        }

        /// <summary>Our own deployment zones (we know where we may set up).</summary>
        public IReadOnlyList<float2[]> OwnDeployZones => _battle.DeployZones(Side);
        public bool IsDeploying => _battle.IsDeploying;

        /// <summary>During deployment: place one of our teams inside our own zone (false if not allowed there).</summary>
        public bool Redeploy(Team team, float2 position, float facing)
        {
            if (team.Side != Side)
            {
                throw new InvalidOperationException("An AI may only command its own side.");
            }

            return _battle.Redeploy(team, position, facing);
        }

        /// <summary>Fire discipline: small arms hold fire until an enemy is within <paramref name="range"/> (infinity: fire at will).</summary>
        public void SetHoldFire(Team team, float range)
        {
            if (team.Side != Side)
            {
                throw new InvalidOperationException("An AI may only command its own side.");
            }

            team.SmallArmsHoldRange = range;
        }

        /// <summary>
        /// Pull a team back to a prepared position even if it is shaken or broken.
        /// A battered team's wish (to get back) and its commander's (to reach the
        /// next line) agree, so no refusal applies.
        /// </summary>
        public bool PullBack(Team team, float2 target, Func<int, float> pathCost)
        {
            if (team.Side != Side)
            {
                throw new InvalidOperationException("An AI may only command its own side.");
            }

            return _battle.MoveTeam(team, target, OrderType.Move, pathCost);
        }
    }
}
