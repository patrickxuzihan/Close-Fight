using System.Collections.Generic;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.AI
{
    public enum DefenseRole : byte
    {
        MachineGun,
        Rifle,
        AntiTank,
        Armor,
        Mortar,
        Headquarters,
        Reserve,
        /// <summary>Sniper (M12): high, concealed, long view; stays hidden and picks officers and gunners.</summary>
        Sniper,
        /// <summary>Forward observer (M13): the longest view of the approach, hidden; directs the guns, does not shoot.</summary>
        Observer,
    }

    /// <summary>When a team gives ground (D22).</summary>
    public enum DefensePosture : byte
    {
        /// <summary>Stay until broken or out of ammunition.</summary>
        Hold,
        /// <summary>Fall back to the second position when shaken, outnumbered up close or flanked.</summary>
        Elastic,
        /// <summary>Forward outpost: fall back as soon as the enemy gets close.</summary>
        Delay,
    }

    public enum PlanState : byte
    {
        Deploying,
        Holding,
        Withdrawing,
        HoldingFallback,
        Retreating,
        Counterattacking,
    }

    /// <summary>What one team is meant to do. Exposed for the debug overlay.</summary>
    public sealed class TeamPlan
    {
        public Team Team;
        public DefenseRole Role;
        public DefensePosture Posture;
        public float2 Primary;
        public float2 Fallback;
        public bool HasFallback;
        public PlanState State;
        public float StateSince;
        internal float LastMortarOrder = float.NegativeInfinity;
    }

    /// <summary>
    /// The defending AI (M7). Plans once, then reacts every couple of seconds.
    ///
    /// Planning (D8): every team gets a role and a position from
    /// <see cref="PositionAnalysis"/>. Machine guns take the longest fields of
    /// fire, anti-tank guns cover tank lanes from concealment, mortars hide out
    /// of sight, one rifle team stays back as a reserve. Positions are spread
    /// out and chosen so their fields of fire cross from different angles;
    /// valuable, defensible objectives draw more teams, poor ones are given up.
    /// Each team also gets a fallback position further back.
    ///
    /// Execution: teams move in along routes that avoid ground the known enemy
    /// covers, wait in ambush (machine guns open up once the enemy is in the
    /// open within 250 m), give ground according to their posture (D22), and
    /// the reserve counterattacks an objective the enemy holds weakly. Mortars
    /// shell gunfire they can hear but not see. Everything goes through
    /// <see cref="AiView"/>: the AI knows only what its soldiers know (D17).
    /// </summary>
    public sealed class DefenseCommander : IBattleCommander
    {
        const float MinSpacing = 12f;
        const float ArriveDistance = 4f;
        const float MachineGunOpenFire = 250f;
        const float MortarRetask = 20f;
        /// <summary>A new position must beat where the team already is by this much to be worth the move...</summary>
        const float RelocateMargin = 15f;
        /// <summary>...and by this much if the enemy is already in sight (moving under observation is how teams die).</summary>
        const float RelocateMarginInContact = 45f;
        /// <summary>Average suppression at which a team moving in gives up and digs in where it is.</summary>
        const float DeployUnderFire = 20f;

        readonly AiView _view;
        readonly List<TeamPlan> _plans = new List<TeamPlan>();
        PositionAnalysis _analysis;
        DangerMap _danger;
        /// <summary>Average depth of the objectives: reserves, HQ and mortars stay behind it.</summary>
        float _rearLine;

        public Side Side => _view.Side;
        public IReadOnlyList<TeamPlan> Plans => _plans;
        public PositionAnalysis Analysis => _analysis;

        public DefenseCommander(BattleState battle, Side side)
        {
            _view = new AiView(battle, side);
        }

        public void Think()
        {
            EnsurePlanned();
            _danger.Rebuild(KnownThreats());
            CallSupport();
            foreach (var plan in _plans)
            {
                if (plan.Team.ActiveCount > 0)
                {
                    Execute(plan);
                }
            }
        }

        void EnsurePlanned()
        {
            if (_analysis != null)
            {
                return;
            }

            _analysis = new PositionAnalysis(_view.Map, _view.EnemyEdge, _view.Objectives);
            _danger = new DangerMap(_view.Map);
            MakePlan();
        }

        /// <summary>
        /// Deployment phase: the defenders are already in their positions when the
        /// battle opens (as the Guards held the station before the 71st attacked),
        /// facing the enemy. A position outside our zone is reached on foot once
        /// the battle starts.
        /// </summary>
        public void Deploy()
        {
            EnsurePlanned();
            float facing = Soldier.HeadingOf(_analysis.TowardEnemy);
            foreach (var plan in _plans)
            {
                if (plan.Team.ActiveCount > 0)
                {
                    _view.Redeploy(plan.Team, plan.Primary, facing);
                }
            }

            LayMines();
        }

        /// <summary>
        /// Before the battle our engineers mine the approaches our guns cover best
        /// (D39): the enemy has to cross them under fire or stop to clear them.
        /// Tank lanes count double. Chosen from the map alone, like the positions.
        /// </summary>
        void LayMines()
        {
            var fire = new List<float2>();
            foreach (var plan in _plans)
            {
                if (plan.Role == DefenseRole.MachineGun || plan.Role == DefenseRole.Rifle || plan.Role == DefenseRole.AntiTank || plan.Role == DefenseRole.Armor)
                {
                    fire.Add(plan.Primary);
                }
            }

            var chosen = new List<float2>();
            foreach (var plan in _plans)
            {
                var team = plan.Team;
                while (team.Type == TeamType.Engineer && team.MineLoads > 0)
                {
                    float best = 0f;
                    float2 spot = float2.zero;
                    var points = _analysis.ApproachPoints;
                    for (int i = 0; i < points.Count; i++)
                    {
                        float2 p = points[i];
                        if (math.distance(p, team.Centroid) > 145f || TooClose(p, chosen, 35f) || TooClose(p, fire, 30f))
                        {
                            continue;
                        }

                        int covering = 0;
                        foreach (var f in fire)
                        {
                            float d = math.distance(f, p);
                            if (d < 250f && LineOfSight.Check(_view.Map, f, LineOfSight.EyeHeightAt(_view.Map, f, Posture.Kneeling), p, LineOfSight.EyeKneeling).Visible)
                            {
                                covering++;
                            }
                        }

                        float score = covering * (_analysis.ApproachForVehicles[i] ? 2f : 1f);
                        if (score > best)
                        {
                            best = score;
                            spot = p;
                        }
                    }

                    if (best <= 0f || !_view.LayMinesBeforeBattle(team, spot))
                    {
                        break;
                    }

                    chosen.Add(spot);
                }
            }
        }

        static bool TooClose(float2 p, List<float2> others, float distance)
        {
            foreach (var o in others)
            {
                if (math.distance(o, p) < distance) return true;
            }

            return false;
        }

        // ------------------------------------------------------------ off-map support (M13)

        float _lastSupportAt = float.NegativeInfinity;

        /// <summary>
        /// The guns across the Volga on the enemy where he bunches up: at least five
        /// men seen together (or a tank), far enough from our own positions that the
        /// scatter will not hit our men. Rockets for the bigger crowds.
        /// </summary>
        void CallSupport()
        {
            if (_view.SupportPoints <= 0 || _view.Time - _lastSupportAt < 90f)
            {
                return;
            }

            var seen = new List<float2>();
            foreach (var enemy in _view.VisibleEnemies)
            {
                seen.Add(enemy.Position);
            }

            foreach (var vehicle in _view.VisibleEnemyVehicles)
            {
                for (int i = 0; i < 4; i++) seen.Add(vehicle.Position); // a tank is worth a crowd
            }

            float2 best = float2.zero;
            int bestCount = 0;
            foreach (var p in seen)
            {
                int count = 0;
                float2 sum = float2.zero;
                foreach (var q in seen)
                {
                    if (math.distance(p, q) < 35f)
                    {
                        count++;
                        sum += q;
                    }
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    best = sum / count;
                }
            }

            if (bestCount < 5)
            {
                return;
            }

            SupportAsset choice = null;
            foreach (var asset in _view.SupportAssets)
            {
                if (asset.Cost > _view.SupportPoints || !_view.SupportAvailable(asset) || !SafeFromScatter(best, asset))
                {
                    continue;
                }

                bool rockets = asset.Rounds >= 10;
                if (choice == null || (bestCount >= 10 ? rockets && choice.Rounds < 10 : !rockets && choice.Rounds >= 10))
                {
                    choice = asset;
                }
            }

            if (choice != null && _view.RequestSupport(choice, best, out _))
            {
                _lastSupportAt = _view.Time;
            }
        }

        bool SafeFromScatter(float2 target, SupportAsset asset)
        {
            float safe = asset.Spread + asset.Round.HeLethalRadius + 20f;
            foreach (var team in _view.OwnTeams)
            {
                foreach (var member in team.Members)
                {
                    if (member.IsActive && math.distance(member.Position, target) < safe)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // ------------------------------------------------------------ planning

        static DefenseRole RoleOf(Team team)
        {
            switch (team.Type)
            {
                case TeamType.MachineGun: return DefenseRole.MachineGun;
                case TeamType.AntiTankGun: return DefenseRole.AntiTank;
                case TeamType.Vehicle: return DefenseRole.Armor;
                case TeamType.Mortar: return DefenseRole.Mortar;
                case TeamType.Headquarters: return DefenseRole.Headquarters;
                case TeamType.Sniper: return DefenseRole.Sniper;
                case TeamType.Observer: return DefenseRole.Observer;
                default: return DefenseRole.Rifle; // engineers fight as riflemen once their mines are down
            }
        }

        static int PlanningOrder(DefenseRole role)
        {
            switch (role)
            {
                case DefenseRole.MachineGun: return 0;
                case DefenseRole.Sniper: return 1;
                case DefenseRole.Observer: return 1;
                case DefenseRole.AntiTank: return 1;
                case DefenseRole.Armor: return 2;
                case DefenseRole.Rifle: return 3;
                case DefenseRole.Mortar: return 4;
                case DefenseRole.Headquarters: return 5;
                default: return 6;
            }
        }

        void MakePlan()
        {
            _rearLine = 0f;
            foreach (var objective in _view.Objectives)
            {
                _rearLine += _analysis.Depth(objective.Position) / _view.Objectives.Count;
            }

            var teams = new List<Team>(_view.OwnTeams);
            var roles = new Dictionary<Team, DefenseRole>();
            int rifles = 0;
            foreach (var team in teams)
            {
                roles[team] = RoleOf(team);
                if (roles[team] == DefenseRole.Rifle) rifles++;
            }

            // With two or more rifle teams, the last one is held back as the reserve.
            if (rifles >= 2)
            {
                for (int i = teams.Count - 1; i >= 0; i--)
                {
                    if (roles[teams[i]] == DefenseRole.Rifle)
                    {
                        roles[teams[i]] = DefenseRole.Reserve;
                        break;
                    }
                }
            }

            teams.Sort((a, b) => PlanningOrder(roles[a]).CompareTo(PlanningOrder(roles[b])));

            var placed = new List<PositionAnalysis.Candidate>();
            var firePositions = new List<PositionAnalysis.Candidate>();
            float frontDepth = float.MaxValue;
            foreach (var objective in _view.Objectives)
            {
                frontDepth = math.min(frontDepth, _analysis.Depth(objective.Position) - objective.Radius - 20f);
            }

            bool contact = false;
            foreach (var _ in _view.VisibleEnemies)
            {
                contact = true;
                break;
            }
            foreach (var team in teams)
            {
                var role = roles[team];
                var best = Choose(team, role, placed, firePositions, null, 0f);
                best = StayIfGoodEnough(team, role, best, placed, firePositions, contact ? RelocateMarginInContact : RelocateMargin);
                var plan = new TeamPlan
                {
                    Team = team,
                    Role = role,
                    Primary = best != null ? best.Position : team.Centroid,
                    State = PlanState.Deploying,
                };

                if (best != null)
                {
                    placed.Add(best);
                    if (role == DefenseRole.MachineGun || role == DefenseRole.Rifle || role == DefenseRole.AntiTank || role == DefenseRole.Armor)
                    {
                        firePositions.Add(best);
                    }
                }

                plan.Posture = PostureFor(role, best, frontDepth);
                _plans.Add(plan);
            }

            // Second line: a fallback for every team, at least 20 m further back.
            var fallbacks = new List<PositionAnalysis.Candidate>();
            foreach (var plan in _plans)
            {
                var primary = plan.Primary;
                var fallback = Choose(plan.Team, plan.Role, fallbacks, null, (float2?)primary, _analysis.Depth(primary) + 20f);
                if (fallback != null)
                {
                    plan.Fallback = fallback.Position;
                    plan.HasFallback = true;
                    fallbacks.Add(fallback);
                }
            }
        }

        /// <summary>Keeps a team where it is unless the best new spot is clearly better.</summary>
        PositionAnalysis.Candidate StayIfGoodEnough(Team team, DefenseRole role, PositionAnalysis.Candidate best,
            List<PositionAnalysis.Candidate> placed, List<PositionAnalysis.Candidate> firePositions, float margin)
        {
            if (best == null)
            {
                return null;
            }

            PositionAnalysis.Candidate here = null;
            float nearest = float.MaxValue;
            float2 at = team.IsMechanized ? team.Vehicle.Position : team.Centroid;
            foreach (var c in _analysis.Candidates)
            {
                float d = math.distancesq(c.Position, at);
                if (d < nearest)
                {
                    nearest = d;
                    here = c;
                }
            }

            if (here == null || here == best || !Allowed(role, here) || nearest > 36f)
            {
                return best;
            }

            foreach (var other in placed)
            {
                if (math.distance(other.Position, here.Position) < MinSpacing)
                {
                    return best; // someone already took this spot
                }
            }

            float hereScore = RoleScore(role, here);
            float bestScore = RoleScore(role, best) - math.distance(at, best.Position) * (role == DefenseRole.AntiTank ? 0.35f : 0.12f);
            if (firePositions != null && (role == DefenseRole.MachineGun || role == DefenseRole.Rifle))
            {
                float weight = role == DefenseRole.MachineGun ? 40f : 30f;
                hereScore += weight * _analysis.Crossfire(here, firePositions);
                bestScore += weight * _analysis.Crossfire(best, firePositions);
            }

            return bestScore - hereScore >= margin ? best : here;
        }

        static DefensePosture PostureFor(DefenseRole role, PositionAnalysis.Candidate position, float frontDepth)
        {
            if (position != null && position.Depth < frontDepth)
            {
                return DefensePosture.Delay; // ahead of the objectives: an outpost
            }

            switch (role)
            {
                case DefenseRole.MachineGun:
                case DefenseRole.Rifle:
                case DefenseRole.Armor:
                case DefenseRole.Sniper:
                    return DefensePosture.Elastic;
                default:
                    return DefensePosture.Hold;
            }
        }

        /// <summary>
        /// Best candidate for a team in a role. <paramref name="near"/> limits the
        /// search to within 120 m of a point and <paramref name="minDepth"/> keeps it
        /// behind a line (for fallbacks).
        /// </summary>
        PositionAnalysis.Candidate Choose(Team team, DefenseRole role, List<PositionAnalysis.Candidate> placed,
            List<PositionAnalysis.Candidate> firePositions, float2? near, float minDepth)
        {
            PositionAnalysis.Candidate best = null;
            float bestScore = float.MinValue;
            float2 from = team.Centroid;

            foreach (var c in _analysis.Candidates)
            {
                if (c.Depth < minDepth || !Allowed(role, c))
                {
                    continue;
                }

                // Support and reserve stay behind the line of objectives, not in front of it.
                bool rearRole = role == DefenseRole.Reserve || role == DefenseRole.Headquarters || role == DefenseRole.Mortar;
                if (rearRole && c.Depth < _rearLine)
                {
                    continue;
                }

                if (near.HasValue && math.distance(c.Position, near.Value) > 120f)
                {
                    continue;
                }

                bool crowded = false;
                foreach (var other in placed)
                {
                    if (math.distance(other.Position, c.Position) < MinSpacing)
                    {
                        crowded = true;
                        break;
                    }
                }

                if (crowded)
                {
                    continue;
                }

                float score = RoleScore(role, c);
                if (firePositions != null && (role == DefenseRole.MachineGun || role == DefenseRole.Rifle))
                {
                    score += (role == DefenseRole.MachineGun ? 40f : 30f) * _analysis.Crossfire(c, firePositions);
                }

                // Prefer what can be reached quickly (slow guns even more so); fallbacks stay close behind the primary.
                float travel = math.distance(near ?? from, c.Position);

                float travelWeight = role == DefenseRole.AntiTank ? 0.35f : 0.12f;
                score -= travel * travelWeight;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }

            return best;
        }

        static bool Allowed(DefenseRole role, PositionAnalysis.Candidate c)
        {
            switch (role)
            {
                case DefenseRole.AntiTank:
                case DefenseRole.Mortar:
                    return !c.InBuilding; // guns stay outside
                case DefenseRole.Armor:
                    return c.VehicleOk;
                default:
                    return true;
            }
        }

        /// <summary>How good a spot is for a role, before crossfire and travel.</summary>
        public static float RoleScore(DefenseRole role, PositionAnalysis.Candidate c)
        {
            float height = math.min(c.Floors, 4);
            float concealed = c.InBuilding ? 1f : c.Cover / 100f;
            switch (role)
            {
                case DefenseRole.MachineGun:
                    return 80f * c.Coverage + 0.25f * c.Cover + 4f * height + 8f * concealed + 6f * c.ObjectiveScore;
                case DefenseRole.Rifle:
                    return 45f * c.Coverage + 0.4f * c.Cover + 2f * height + 8f * concealed + 15f * c.ObjectiveScore;
                case DefenseRole.AntiTank:
                    return 80f * c.VehicleCoverage + 0.2f * c.Cover + 12f * concealed - 20f * c.Exposure + 6f * c.ObjectiveScore;
                case DefenseRole.Armor:
                    return 60f * c.VehicleCoverage + 0.3f * c.Cover - 15f * c.Exposure + 6f * c.ObjectiveScore;
                case DefenseRole.Mortar:
                    return -120f * c.Exposure + 0.2f * c.Cover + 0.15f * c.Depth + 4f * c.ObjectiveScore;
                case DefenseRole.Sniper:
                    return 70f * c.Coverage + 0.3f * c.Cover + 6f * height + 14f * concealed + 4f * c.ObjectiveScore;
                case DefenseRole.Observer:
                    return 90f * c.Coverage + 0.2f * c.Cover + 8f * height + 14f * concealed - 20f * c.Exposure;
                default: // headquarters and reserve: hidden, covered, near what matters
                    return -60f * c.Exposure + 0.4f * c.Cover + 20f * c.ObjectiveScore;
            }
        }

        // ------------------------------------------------------------ execution

        IEnumerable<(float2, float)> KnownThreats()
        {
            // Group what we see by enemy team: one threat per team at its visible centroid.
            var sums = new Dictionary<Team, (float2 sum, int count, bool mg)>();
            foreach (var enemy in _view.VisibleEnemies)
            {
                sums.TryGetValue(enemy.Team, out var entry);
                bool mg = entry.mg || (enemy.Weapon != null && enemy.Weapon.Class == WeaponClass.MachineGun);
                sums[enemy.Team] = (entry.sum + enemy.Position, entry.count + 1, mg);
            }

            foreach (var pair in sums)
            {
                var (sum, count, mg) = pair.Value;
                yield return (sum / count, count * (mg ? 2f : 1f));
            }

            foreach (var marker in _view.HeardEnemies)
            {
                yield return (marker.Position, 1f);
            }
        }

        void Execute(TeamPlan plan)
        {
            var team = plan.Team;
            switch (plan.State)
            {
                case PlanState.Deploying:
                    Deploy(plan);
                    break;

                case PlanState.Holding:
                case PlanState.HoldingFallback:
                    Hold(plan);
                    break;

                case PlanState.Withdrawing:
                case PlanState.Counterattacking:
                    if (team.Activity == TeamActivity.Idle)
                    {
                        _view.Order(team, OrderType.Defend, team.Centroid);
                        SetState(plan, plan.State == PlanState.Withdrawing ? PlanState.HoldingFallback : PlanState.Holding);
                    }

                    break;
            }
        }

        void Deploy(TeamPlan plan)
        {
            var team = plan.Team;
            // Moving in: do not give the positions away by opening fire early.
            _view.SetHoldFire(team, Combat.AmbushRange);

            // Caught in the open on the way: dig in where we are and fight back.
            if (team.AverageSuppression > DeployUnderFire)
            {
                _view.Order(team, OrderType.Defend, team.Centroid);
                _view.SetHoldFire(team, float.PositiveInfinity);
                plan.Primary = team.Centroid;
                SetState(plan, PlanState.Holding);
                return;
            }

            if (team.Activity != TeamActivity.Idle)
            {
                return;
            }

            if (math.distance(Anchor(team), plan.Primary) <= ArriveDistance)
            {
                TakeUpPosition(plan);
                return;
            }


            // Crawl in if the enemy is already close; otherwise walk (or drive) in along a covered route.
            bool enemyClose = NearestEnemyDistance(team.Centroid) < 150f;
            var order = team.IsMechanized ? OrderType.Move
                : enemyClose && team.GunTeam == null ? OrderType.Crawl
                : OrderType.Move;
            if (!_view.Order(team, order, plan.Primary, _danger.PathCost(6f)))
            {
                TakeUpPosition(plan); // cannot get there: hold where it is
            }
        }

        void TakeUpPosition(TeamPlan plan)
        {
            var team = plan.Team;
            // Wait in ambush unless the enemy is already in the open (mortars fire whenever they can);
            // observers hide: their job is to watch and call the guns, not to give themselves away.
            var stance = plan.Role == DefenseRole.Mortar ? OrderType.Defend
                : plan.Role == DefenseRole.Observer ? OrderType.Hide
                : OrderType.Ambush;
            _view.Order(team, stance, team.Centroid);
            SetState(plan, PlanState.Holding);
        }

        void Hold(TeamPlan plan)
        {
            var team = plan.Team;

            // Fire discipline: machine guns open up once the enemy is in the killing ground.
            if (plan.Role == DefenseRole.MachineGun && team.Order.Type == OrderType.Ambush
                && NearestEnemyDistance(team.Centroid) < MachineGunOpenFire)
            {
                _view.Order(team, OrderType.Defend, team.Centroid);
            }

            // Once a team has opened fire (ambush sprung or told to), its rifles fire at will.
            // Mortar crews keep their rifles quiet so as not to give the tube away.
            bool sprung = team.Order.Type != OrderType.Ambush && team.Order.Type != OrderType.Hide;
            _view.SetHoldFire(team, sprung && plan.Role != DefenseRole.Mortar ? float.PositiveInfinity : Combat.AmbushRange);

            if (plan.Role == DefenseRole.Mortar)
            {
                TaskMortar(plan);
            }

            if (plan.Role == DefenseRole.Reserve && plan.State == PlanState.Holding && TryCounterattack(plan))
            {
                return;
            }

            if (!ShouldGiveGround(plan))
            {
                return;
            }

            if (plan.State == PlanState.Holding && plan.HasFallback)
            {
                if (_view.PullBack(team, plan.Fallback, _danger.PathCost(6f)))
                {
                    SetState(plan, PlanState.Withdrawing);
                }
            }
            else if (_view.Order(team, OrderType.Retreat, team.Centroid))
            {
                SetState(plan, PlanState.Retreating);
            }
        }

        /// <summary>D22: when to give ground, by posture.</summary>
        bool ShouldGiveGround(TeamPlan plan)
        {
            var team = plan.Team;
            bool outOfAmmo = team.Gun != null && (team.Vehicle == null || team.Vehicle.IsOperational)
                ? team.Gun.AmmoAp + team.Gun.AmmoHe == 0
                : team.TotalAmmo == 0;
            if (team.IsBroken || outOfAmmo)
            {
                return true;
            }

            if (plan.Posture == DefensePosture.Hold)
            {
                return false;
            }

            float2 here = team.Centroid;
            int close = 0;
            bool flanked = false;
            foreach (var enemy in _view.VisibleEnemies)
            {
                float d = math.distance(enemy.Position, here);
                if (d < 60f) close++;

                // Flanked: an enemy well off to the side or behind, relative to the enemy's main direction.
                if (d < 150f)
                {
                    float2 toEnemy = math.normalizesafe(enemy.Position - here);
                    if (math.dot(toEnemy, _analysis.TowardEnemy) < math.cos(math.radians(100f)))
                    {
                        flanked = true;
                    }
                }
            }

            bool pressed = team.IsShaken || (close >= 3 && close >= team.ActiveCount * 1.5f) || flanked;
            if (plan.Posture == DefensePosture.Elastic)
            {
                return pressed;
            }

            return pressed || NearestEnemyDistance(here) < 80f; // Delay
        }

        /// <summary>Mortars shell gunfire they can hear but nobody can see.</summary>
        void TaskMortar(TeamPlan plan)
        {
            var team = plan.Team;
            var gun = team.Gun;
            if (gun == null || gun.AmmoHe <= 5 || _view.Time - plan.LastMortarOrder < MortarRetask)
            {
                return;
            }

            if (NearestEnemyDistance(gun.Position) < float.MaxValue)
            {
                if (team.Order.Type == OrderType.AreaFire)
                {
                    _view.Order(team, OrderType.Defend, team.Centroid); // targets in sight: fire on them instead
                }

                return;
            }

            SoundMarker? newest = null;
            foreach (var marker in _view.HeardEnemies)
            {
                float d = math.distance(marker.Position, gun.Position);
                if (d < gun.Definition.MinRange || d > gun.Definition.MaxRange)
                {
                    continue;
                }

                if (!newest.HasValue || marker.CreatedAt > newest.Value.CreatedAt)
                {
                    newest = marker;
                }
            }

            if (newest.HasValue && _view.Order(team, OrderType.AreaFire, newest.Value.Position))
            {
                plan.LastMortarOrder = _view.Time;
            }
        }

        /// <summary>
        /// D22 counterattack: if the enemy holds an objective with fewer men than the
        /// reserve has, and none of ours are there, the reserve storms it.
        /// </summary>
        bool TryCounterattack(TeamPlan plan)
        {
            var team = plan.Team;
            if (team.IsShaken)
            {
                return false;
            }

            foreach (var objective in _view.Objectives)
            {
                int enemies = 0;
                foreach (var enemy in _view.VisibleEnemies)
                {
                    if (math.distance(enemy.Position, objective.Position) < objective.Radius + 10f)
                    {
                        enemies++;
                    }
                }

                if (enemies == 0 || enemies * 1.5f > team.ActiveCount || OwnTroopsAt(objective, team))
                {
                    continue;
                }

                var grid = _view.Map.Grid;
                var cell = grid.WorldToCell(objective.Position);
                bool building = grid.InBounds(cell) && _view.Map.BuildingAt(grid.IndexOf(cell)) != null;
                var order = building ? OrderType.AssaultBuilding : OrderType.MoveFast;
                if (_view.Order(team, order, objective.Position, _danger.PathCost(3f)))
                {
                    plan.Primary = objective.Position;
                    SetState(plan, PlanState.Counterattacking);
                    return true;
                }
            }

            return false;
        }

        bool OwnTroopsAt(Objective objective, Team except)
        {
            foreach (var team in _view.OwnTeams)
            {
                if (team != except && team.ActiveCount > 0 && objective.Contains(team.Centroid))
                {
                    return true;
                }
            }

            return false;
        }

        float NearestEnemyDistance(float2 from)
        {
            float best = float.MaxValue;
            foreach (var enemy in _view.VisibleEnemies)
            {
                best = math.min(best, math.distance(enemy.Position, from));
            }

            return best;
        }

        static float2 Anchor(Team team)
        {
            if (team.IsMechanized)
            {
                return team.Vehicle.Position;
            }

            var leader = team.Leader;
            return leader != null ? leader.Position : team.Centroid;
        }

        void SetState(TeamPlan plan, PlanState state)
        {
            plan.State = state;
            plan.StateSince = _view.Time;
        }
    }
}
