using System.Collections.Generic;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Path;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.AI
{
    /// <summary>
    /// Teams the player hands to the AI (M15; D14, D22, D53). Defence only: the
    /// team holds where it was left, the way the defending AI holds its line.
    ///
    /// - It takes the best covered spot nearby that the known enemy cannot see,
    ///   and waits there in ambush (a delaying team fires at will instead).
    /// - It picks a fallback position about 70 m back, away from the enemy.
    /// - It gives ground by its stance. Elastic: when shaken, outnumbered up close,
    ///   flanked or under heavy fire for a while, it asks to pull back, and goes
    ///   if the player does not refuse within 20 s. Delay: it falls back as soon as
    ///   the enemy is within 80 m or it has lost a third of its men. Hold: never.
    ///   At two thirds down a team breaks and falls back by itself (Psychology),
    ///   to this fallback position.
    /// - At the fallback it forms a new line: new spot, new fallback further back.
    /// - Cornered (the enemy within 25 m and no way back, or holding at all costs)
    ///   its riflemen charge the enemy that broke in.
    /// - Pinned in poor cover, it crawls to a better spot nearby.
    ///
    /// It knows only what its side's soldiers see and hear (D17). Any order from
    /// the player ends the delegation (<see cref="BattleState.Undelegate"/>).
    /// </summary>
    public sealed class Autopilot
    {
        public const int IntervalTicks = 10;
        public const float SearchRadius = 20f;
        public const float FallbackDistance = 70f;
        public const float CloseQuarters = 25f;
        public const float DelayDistance = 80f;
        /// <summary>After the player refuses, a team holds on this long before asking again.</summary>
        public const float RefusalQuiet = 60f;
        /// <summary>Heavy fire this long without a break counts as being pressed.</summary>
        public const float PressureSeconds = 20f;
        public const float PressureSuppression = 50f;
        const float ArriveDistance = 5f;
        const float CounterattackCooldown = 60f;
        const float CounterattackSeconds = 40f;
        const float RepositionCooldown = 30f;
        const float DangerRefresh = 3f;
        const float Spacing = 7f;

        enum Want : byte { Stay, Ask, Go }

        sealed class Brain
        {
            public AiView View;
            public ContactMemory Contacts;
            public DangerMap Danger;
            public float DangerBuiltAt = float.NegativeInfinity;
        }

        readonly BattleState _battle;
        readonly GridPathfinder _pathfinder;
        readonly Dictionary<Side, Brain> _brains = new Dictionary<Side, Brain>();
        readonly List<Team> _teams = new List<Team>();

        internal Autopilot(BattleState battle, GridPathfinder pathfinder)
        {
            _battle = battle;
            _pathfinder = pathfinder;
        }

        float Now => _battle.Clock.ElapsedSeconds;

        internal void Think()
        {
            _teams.Clear();
            foreach (var team in _battle.Teams)
            {
                if (team.Delegation != null)
                {
                    _teams.Add(team);
                }
            }

            if (_teams.Count == 0)
            {
                return;
            }

            foreach (var side in new[] { Side.German, Side.Soviet })
            {
                if (_teams.Exists(t => t.Side == side))
                {
                    BrainOf(side).Contacts.Update();
                }
            }

            foreach (var team in _teams)
            {
                Run(team, BrainOf(team.Side));
            }
        }

        /// <summary>A team has just been handed over: choose its spot near <paramref name="anchor"/> and go there.</summary>
        internal void Begin(Team team, float2 anchor)
        {
            var brain = BrainOf(team.Side);
            brain.Contacts.Update();
            var d = team.Delegation;
            d.Position = BestSpot(team, brain, anchor, SearchRadius);
            d.MoveIssued = false;
            d.HasFallback = false;
            SetState(d, DelegationState.TakingPosition);
            if (!_battle.IsDeploying)
            {
                TakePosition(team, d, brain);
            }
        }

        /// <summary>The player's answer to a request to pull back (D53).</summary>
        internal bool Answer(Team team, bool approve)
        {
            var d = team.Delegation;
            if (d == null || !d.IsRequesting)
            {
                return false;
            }

            if (approve)
            {
                Withdraw(team, d, BrainOf(team.Side));
            }
            else
            {
                d.RefusedAt = Now;
                d.RequestDeadline = float.NaN;
                SetState(d, DelegationState.Holding);
            }

            return true;
        }

        void Run(Team team, Brain brain)
        {
            var d = team.Delegation;
            if (team.ActiveCount == 0)
            {
                team.Delegation = null;
                return;
            }

            float now = Now;
            d.PressedSince = team.AverageSuppression >= PressureSuppression
                ? (float.IsNaN(d.PressedSince) ? now : d.PressedSince)
                : float.NaN;

            // Broken: Psychology pulls the team back to our fallback position; form the new line once it is there.
            if (team.FallingBack)
            {
                if (d.State != DelegationState.Withdrawing)
                {
                    d.RequestDeadline = float.NaN;
                    SetState(d, DelegationState.Withdrawing);
                }

                return;
            }

            if (team.IsBroken && d.State != DelegationState.Withdrawing)
            {
                return; // already fell back once: it stays hidden where it is
            }

            switch (d.State)
            {
                case DelegationState.TakingPosition:
                    TakePosition(team, d, brain);
                    break;
                case DelegationState.Holding:
                    Hold(team, d, brain);
                    break;
                case DelegationState.RequestingWithdrawal:
                    Request(team, d, brain);
                    break;
                case DelegationState.Withdrawing:
                    if (team.Activity == TeamActivity.Idle)
                    {
                        FormNewLine(team, d, brain);
                    }

                    break;
                case DelegationState.Counterattacking:
                    Counterattack(team, d, brain);
                    break;
            }
        }

        // ------------------------------------------------------------ states

        void TakePosition(Team team, Delegation d, Brain brain)
        {
            if (team.Activity != TeamActivity.Idle)
            {
                // Caught under fire on the way in: get down and fight from here.
                if (!team.IsMechanized && team.AverageSuppression > 45f)
                {
                    TakeUp(team, d, brain);
                }

                return;
            }

            if (!d.MoveIssued && math.distance(Anchor(team), d.Position) > ArriveDistance)
            {
                d.MoveIssued = true;
                bool enemyClose = NearestEnemy(brain, Anchor(team), out _) < 150f;
                var order = !team.IsMechanized && team.GunTeam == null && enemyClose ? OrderType.Crawl : OrderType.Move;
                if (_battle.TryIssueOrderAvoiding(team, order, d.Position, DangerOf(brain).PathCost(6f), out _))
                {
                    return;
                }
            }

            TakeUp(team, d, brain);
        }

        /// <summary>In position: wait in ambush (a delaying team fires at will; mortars always; observers and snipers hide).</summary>
        void TakeUp(Team team, Delegation d, Brain brain)
        {
            var order = team.IsBroken ? OrderType.Hide
                : team.Type == TeamType.Observer || team.Type == TeamType.Sniper ? OrderType.Hide
                : team.Type == TeamType.Mortar || d.Stance == Stance.Delay ? OrderType.Defend
                : OrderType.Ambush;
            _battle.TryIssueOrder(team, order, Anchor(team), out _);
            d.Position = Anchor(team);
            ChooseFallback(team, d, brain);
            SetState(d, DelegationState.Holding);
        }

        void Hold(Team team, Delegation d, Brain brain)
        {
            if (TryCounterattack(team, d, brain))
            {
                return;
            }

            var want = Wants(team, d, brain);
            if (want == Want.Go)
            {
                Withdraw(team, d, brain);
                return;
            }

            if (want == Want.Ask && Now - d.RefusedAt >= RefusalQuiet)
            {
                d.RequestDeadline = Now + Delegation.RequestSeconds;
                SetState(d, DelegationState.RequestingWithdrawal);
                return;
            }

            TryBetterCover(team, d, brain);
        }

        void Request(Team team, Delegation d, Brain brain)
        {
            if (TryCounterattack(team, d, brain))
            {
                return;
            }

            var want = Wants(team, d, brain);
            if (want == Want.Go || Now >= d.RequestDeadline)
            {
                Withdraw(team, d, brain);
            }
            else if (want == Want.Stay)
            {
                // The pressure is off: hold on.
                d.RequestDeadline = float.NaN;
                SetState(d, DelegationState.Holding);
            }
        }

        void Withdraw(Team team, Delegation d, Brain brain)
        {
            d.RequestDeadline = float.NaN;
            if (!d.HasFallback)
            {
                ChooseFallback(team, d, brain);
            }

            // Run back along the covered route; a tank reverses, keeping its front to the enemy.
            var order = team.IsMechanized ? OrderType.Retreat : OrderType.MoveFast;
            if (d.HasFallback && _battle.MoveTeam(team, d.Fallback, order, DangerOf(brain).PathCost(6f)))
            {
                SetState(d, DelegationState.Withdrawing);
                return;
            }

            // Nowhere to go: hold on here, and do not ask again for a while.
            d.RefusedAt = Now;
            SetState(d, DelegationState.Holding);
        }

        /// <summary>At the fallback position: a new spot, a new fallback further back.</summary>
        void FormNewLine(Team team, Delegation d, Brain brain)
        {
            d.Withdrawals++;
            d.Position = BestSpot(team, brain, Anchor(team), SearchRadius * 0.75f);
            d.MoveIssued = false;
            d.HasFallback = false;
            SetState(d, DelegationState.TakingPosition);
            TakePosition(team, d, brain);
        }

        bool TryCounterattack(Team team, Delegation d, Brain brain)
        {
            bool riflemen = team.Type == TeamType.Rifle || team.Type == TeamType.Headquarters || team.Type == TeamType.Engineer;
            if (!riflemen || team.Vehicle != null || team.ActiveCount < 2 || Now - d.LastCounterattackAt < CounterattackCooldown)
            {
                return false;
            }

            float2 here = Anchor(team);
            if (NearestEnemy(brain, here, out float2 enemy) > CloseQuarters)
            {
                return false;
            }

            // Cornered: told to hold at all costs, nowhere to fall back to, or the enemy is in the way back.
            bool cornered = d.Stance == Stance.Hold || !d.HasFallback
                || math.dot(math.normalizesafe(enemy - here), math.normalizesafe(d.Fallback - here)) > 0.3f;
            if (!cornered)
            {
                return false;
            }

            var order = BuildingAt(enemy) ? OrderType.AssaultBuilding : OrderType.Move;
            if (!_battle.TryIssueOrder(team, order, enemy, out _))
            {
                return false;
            }

            d.LastCounterattackAt = Now;
            d.RequestDeadline = float.NaN;
            SetState(d, DelegationState.Counterattacking);
            return true;
        }

        void Counterattack(Team team, Delegation d, Brain brain)
        {
            bool enemyNear = NearestEnemy(brain, Anchor(team), out _) < 50f;
            if (team.Activity == TeamActivity.Idle || !enemyNear || Now - d.StateSince > CounterattackSeconds)
            {
                // The charge is over: hold where it ended up.
                TakeUp(team, d, brain);
            }
        }

        /// <summary>Pinned in poor cover: crawl to a better spot nearby.</summary>
        void TryBetterCover(Team team, Delegation d, Brain brain)
        {
            if (team.IsMechanized || team.Gun != null || team.Activity != TeamActivity.Idle
                || team.AverageSuppression < 25f || Now - d.LastRepositionAt < RepositionCooldown)
            {
                return;
            }

            float2 here = Anchor(team);
            float current = SpotScore(brain, here, here);
            var spot = BestSpot(team, brain, here, SearchRadius * 0.75f);
            if (SpotScore(brain, spot, here) < current + 1.5f || math.distance(spot, here) < 4f)
            {
                return;
            }

            d.LastRepositionAt = Now;
            if (_battle.TryIssueOrderAvoiding(team, OrderType.Crawl, spot, DangerOf(brain).PathCost(6f), out _))
            {
                d.Position = spot;
                d.MoveIssued = true;
                SetState(d, DelegationState.TakingPosition);
            }
        }

        /// <summary>D22: whether to give ground, by stance.</summary>
        Want Wants(Team team, Delegation d, Brain brain)
        {
            bool outOfAmmo = team.Gun != null && (team.Vehicle == null || team.Vehicle.IsOperational)
                ? team.Gun.AmmoAp + team.Gun.AmmoHe == 0
                : team.TotalAmmo == 0;
            if (outOfAmmo)
            {
                return Want.Go;
            }

            if (d.Stance == Stance.Hold)
            {
                return Want.Stay;
            }

            float2 here = Anchor(team);
            float2 toward = EnemyDirection(brain, here);
            int close = 0;
            bool flanked = false;
            float nearest = float.MaxValue;
            foreach (var enemy in brain.View.VisibleEnemies)
            {
                float distance = math.distance(enemy.Position, here);
                nearest = math.min(nearest, distance);
                if (distance < 60f) close++;
                if (distance < 150f && math.dot(math.normalizesafe(enemy.Position - here), toward) < math.cos(math.radians(100f)))
                {
                    flanked = true;
                }
            }

            bool underFire = !float.IsNaN(d.PressedSince) && Now - d.PressedSince >= PressureSeconds;
            bool pressed = team.IsShaken || flanked || underFire || (close >= 3 && close >= team.ActiveCount * 1.5f);
            if (d.Stance == Stance.Delay)
            {
                return pressed || nearest < DelayDistance || team.CasualtyFraction >= 1f / 3f - 1e-3f ? Want.Go : Want.Stay;
            }

            return pressed ? Want.Ask : Want.Stay;
        }

        // ------------------------------------------------------------ ground

        void ChooseFallback(Team team, Delegation d, Brain brain)
        {
            float2 here = d.Position;
            float2 away = -EnemyDirection(brain, here);
            var grid = _battle.Map.Grid;
            float2 size = new float2(grid.Width, grid.Height) * grid.CellSize;
            float2 target = math.clamp(here + away * FallbackDistance, new float2(2f, 2f), size - 2f);
            var spot = BestSpot(team, brain, target, SearchRadius * 1.25f);
            d.Fallback = spot;
            d.HasFallback = math.distance(spot, here) > 15f;
        }

        /// <summary>Where the enemy is, as this side knows it: toward its remembered contacts, else toward its map edge.</summary>
        float2 EnemyDirection(Brain brain, float2 from)
        {
            float2 sum = float2.zero;
            foreach (var contact in brain.Contacts.All)
            {
                float distance = math.distance(contact.Position, from);
                if (distance > 1f && distance < 500f)
                {
                    sum += (contact.Position - from) / distance * contact.Weight / math.max(30f, distance);
                }
            }

            if (math.lengthsq(sum) > 1e-8f)
            {
                return math.normalize(sum);
            }

            float2 edge = _battle.EdgePoint(brain.View.Enemy, from);
            return math.normalizesafe(edge - from, new float2(0f, 1f));
        }

        /// <summary>The best spot within <paramref name="radius"/>: cover, out of the known enemy's sight, close by, not on top of another team.</summary>
        float2 BestSpot(Team team, Brain brain, float2 around, float radius)
        {
            var grid = _battle.Map.Grid;
            var movement = team.IsMechanized ? MovementClass.Vehicle : MovementClass.Infantry;
            bool outdoors = team.IsMechanized || team.Gun != null;
            float best = float.MinValue;
            float2 bestPoint = around;
            for (float dy = -radius; dy <= radius; dy += 2f)
            {
                for (float dx = -radius; dx <= radius; dx += 2f)
                {
                    float2 p = around + new float2(dx, dy);
                    var cell = grid.WorldToCell(p);
                    if (!grid.InBounds(cell))
                    {
                        continue;
                    }

                    int index = grid.IndexOf(cell);
                    if (!_pathfinder.IsPassable(index, movement) || (outdoors && _battle.Map.BuildingAt(index) != null)
                        || Crowded(team, grid.CellCenter(cell)))
                    {
                        continue;
                    }

                    float score = SpotScore(brain, p, around);
                    if (score > best)
                    {
                        best = score;
                        bestPoint = grid.CellCenter(cell);
                    }
                }
            }

            return bestPoint;
        }

        float SpotScore(Brain brain, float2 p, float2 origin)
        {
            return CoverScore(p) - DangerOf(brain).At(p) * 2f - math.distance(p, origin) * 0.3f;
        }

        float CoverScore(float2 p)
        {
            var grid = _battle.Map.Grid;
            var cell = grid.WorldToCell(p);
            if (!grid.InBounds(cell))
            {
                return float.MinValue;
            }

            int index = grid.IndexOf(cell);
            var building = _battle.Map.BuildingAt(index);
            if (building != null && !grid.HasFlag(index, CellFlags.Collapsed))
            {
                return 12f + 2f * math.min(building.Floors, 4);
            }

            return grid.GetCover(index) * 0.1f;
        }

        bool Crowded(Team team, float2 p)
        {
            foreach (var other in _battle.Teams)
            {
                if (other != team && other.Side == team.Side && other.Delegation != null && other.ActiveCount > 0
                    && math.distance(other.Delegation.Position, p) < Spacing)
                {
                    return true;
                }
            }

            return false;
        }

        bool BuildingAt(float2 p)
        {
            var grid = _battle.Map.Grid;
            var cell = grid.WorldToCell(p);
            return grid.InBounds(cell) && _battle.Map.BuildingAt(grid.IndexOf(cell)) != null;
        }

        float NearestEnemy(Brain brain, float2 from, out float2 position)
        {
            float best = float.MaxValue;
            position = from;
            foreach (var enemy in brain.View.VisibleEnemies)
            {
                float distance = math.distance(enemy.Position, from);
                if (distance < best)
                {
                    best = distance;
                    position = enemy.Position;
                }
            }

            return best;
        }

        static float2 Anchor(Team team) => team.Vehicle != null ? team.Vehicle.Position : team.Centroid;

        void SetState(Delegation d, DelegationState state)
        {
            d.State = state;
            d.StateSince = Now;
        }

        Brain BrainOf(Side side)
        {
            if (!_brains.TryGetValue(side, out var brain))
            {
                var view = new AiView(_battle, side);
                brain = new Brain { View = view, Contacts = new ContactMemory(view), Danger = new DangerMap(_battle.Map) };
                _brains[side] = brain;
            }

            return brain;
        }

        /// <summary>The danger map, rebuilt from what the side remembers at most every few seconds (it is costly on a big map).</summary>
        DangerMap DangerOf(Brain brain)
        {
            if (Now - brain.DangerBuiltAt >= DangerRefresh)
            {
                brain.Danger.Rebuild(brain.Contacts.Threats());
                brain.DangerBuiltAt = Now;
            }

            return brain.Danger;
        }
    }
}
