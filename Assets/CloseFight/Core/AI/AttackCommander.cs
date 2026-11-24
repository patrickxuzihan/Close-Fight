using System.Collections.Generic;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Path;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.AI
{
    public enum AttackRole : byte
    {
        Assault,
        Support,
        Armor,
        AntiTank,
        Mortar,
        Headquarters,
        Reserve,
        Garrison,
        /// <summary>Sniper (M12): beside the base of fire, hidden, picking officers and gunners.</summary>
        Sniper,
    }

    /// <summary>Where the attack on the current objective stands.</summary>
    public enum AttackPhase : byte
    {
        /// <summary>The assault teams gather in cover before moving off together (mass on one point).</summary>
        Assemble,
        /// <summary>Moving up by bounds as one group; the lead finds out where the enemy is.</summary>
        Advance,
        /// <summary>Contact: the base of fire pins the enemy while the assault group rushes from cover to cover.</summary>
        Suppress,
        /// <summary>Smoke down, the assault teams storm the objective together.</summary>
        Assault,
        /// <summary>Objective taken: hold it, reorganize, pick the next one.</summary>
        Consolidate,
        /// <summary>Nothing left to take, or no strength left to take it: hold what we have.</summary>
        Hold,
    }

    public sealed class AttackTeamPlan
    {
        public Team Team;
        public AttackRole Role;
        public float2 Goal;
        public bool HasGoal;
        public float LastOrderAt = float.NegativeInfinity;
        internal float LastFireOrderAt = float.NegativeInfinity;
        internal int BurstStartAmmo;
        internal float CooldownUntil = float.NegativeInfinity;
    }

    public readonly struct AttackEvent
    {
        public readonly float Time;
        public readonly AttackPhase Phase;
        public readonly string ObjectiveId;
        /// <summary>Why the phase changed (for the duel log and tests).</summary>
        public readonly string Note;

        public AttackEvent(float time, AttackPhase phase, string objectiveId, string note)
        {
            Time = time;
            Phase = phase;
            ObjectiveId = objectiveId;
            Note = note;
        }
    }

    /// <summary>
    /// The attacking AI (M10), after the attack principles of D16: reconnoitre,
    /// suppress, smoke, go round the flank, and mass on one point instead of
    /// pushing along the whole line.
    ///
    /// One objective at a time is the main effort (value against distance and
    /// the enemy strength known around it). The rifle teams first gather at an
    /// assembly point in cover, then move up together by bounds toward a
    /// jump-off point chosen on the side the known enemy covers least and at an
    /// angle to the base of fire (the flank). Machine guns take an overwatch that
    /// sees the objective; the assault gun takes a firing position away from
    /// known anti-tank guns and shells the buildings the enemy fights from.
    ///
    /// On contact the group stops and the support, the gun and the mortar fire
    /// on what has been seen or heard (in bursts, keeping rounds for the final
    /// assault). The group rushes on only to the next cover, and across ground
    /// the enemy watches only behind smoke or once the enemy has gone quiet.
    /// At the jump-off, after the defenders have been under fire for a while,
    /// smoke goes down and the teams storm the objective together. A failed
    /// assault falls back to suppression; after two failures the main effort
    /// shifts. Captured objectives keep a garrison.
    ///
    /// Everything goes through <see cref="AiView"/> and <see cref="ContactMemory"/>:
    /// the AI knows only what its soldiers have seen or heard (D17).
    /// </summary>
    public sealed class AttackCommander : IBattleCommander
    {
        const float BoundLength = 60f;
        const float RushLength = 45f;
        const float ArriveDistance = 6f;
        const float SuppressSeconds = 25f;
        const float SmokeSettle = 6f;
        const float RushInterval = 15f;
        /// <summary>A watched stretch may be crossed without smoke once the enemy watching it has not been seen for this long.</summary>
        const float QuietSeconds = 15f;
        /// <summary>Waiting longer than this for smoke or quiet, the group rushes anyway.</summary>
        const float MaxWait = 90f;
        const float ReorderInterval = 20f;
        const float ConsolidateSeconds = 15f;
        /// <summary>Force morale below which the attack stops and holds what it has...</summary>
        const float GiveUpMorale = 30f;
        /// <summary>...and the morale it needs to try again.</summary>
        const float ResumeMorale = 38f;
        const float HoldRecheck = 60f;

        readonly AiView _view;
        readonly GridPathfinder _pathfinder;
        readonly ContactMemory _contacts;
        readonly List<AttackTeamPlan> _plans = new List<AttackTeamPlan>();
        readonly List<AttackEvent> _events = new List<AttackEvent>();
        readonly Dictionary<Objective, int> _failures = new Dictionary<Objective, int>();
        readonly List<GridCoord> _cells = new List<GridCoord>();
        DangerMap _danger;
        bool _planned;

        Objective _target;
        AttackPhase _phase = AttackPhase.Assemble;
        float _phaseSince;
        float2 _assembly;
        float2 _overwatch;
        float2 _jumpOff;
        float2 _armorSpot;
        float2 _sniperSpot;
        float _armorSpotAt = float.NegativeInfinity;
        bool _hasGeometry;
        float _geometryAt = float.NegativeInfinity;
        int _assaultStrength;
        float _lastRushAt = float.NegativeInfinity;
        /// <summary>Since when the group has been ready but held up by enemy watching the next stretch.</summary>
        float _blockedSince = float.NegativeInfinity;
        float _smokeAt = float.NegativeInfinity;
        bool _smokeForAssault;

        public AttackCommander(BattleState battle, Side side)
        {
            _view = new AiView(battle, side);
            _pathfinder = new GridPathfinder(battle.Map);
            _contacts = new ContactMemory(_view);
        }

        public Side Side => _view.Side;
        public IReadOnlyList<AttackTeamPlan> Plans => _plans;
        public IReadOnlyList<AttackEvent> Events => _events;
        public IReadOnlyList<Contact> Contacts => _contacts.All;
        public Objective Target => _target;
        public AttackPhase Phase => _phase;
        public bool HasGeometry => _hasGeometry;
        public float2 Assembly => _assembly;
        public float2 Overwatch => _overwatch;
        public float2 JumpOff => _jumpOff;
        public float2 ArmorSpot => _armorSpot;
        public DangerMap Danger => _danger;
        /// <summary>When smoke was last called for (tests and the debug view).</summary>
        public float LastSmokeAt => _smokeAt;

        public void Think()
        {
            if (!_planned)
            {
                Plan();
            }

            _contacts.Update();
            _danger.Rebuild(_contacts.Threats());

            UpdateTarget();
            // The plan of attack stands once made: troops marching to and fro under fire is
            // how attacks bleed out. It is revised only when a machine gun turns out to
            // watch the assembly area or the jump-off, at most every 90 s, before contact.
            bool beforeContact = _phase == AttackPhase.Assemble || _phase == AttackPhase.Advance;
            if (_target != null && (!_hasGeometry || (beforeContact && _view.Time - _geometryAt > 90f && PlanCompromised())))
            {
                ComputeGeometry(keepOverwatch: true);
            }

            switch (_phase)
            {
                case AttackPhase.Assemble: Assemble(); break;
                case AttackPhase.Advance: Advance(); break;
                case AttackPhase.Suppress: Suppress(); break;
                case AttackPhase.Assault: Assault(); break;
                case AttackPhase.Consolidate: Consolidate(); break;
            }

            foreach (var plan in _plans)
            {
                if (plan.Team.ActiveCount == 0)
                {
                    continue;
                }

                switch (plan.Role)
                {
                    case AttackRole.Support: RunSupport(plan); break;
                    case AttackRole.Sniper: RunSniper(plan); break;
                    case AttackRole.Armor: RunArmor(plan); break;
                    case AttackRole.AntiTank: RunAntiTank(plan); break;
                    case AttackRole.Mortar: RunMortar(plan); break;
                    case AttackRole.Headquarters: RunHeadquarters(plan); break;
                    case AttackRole.Reserve: RunReserve(plan); break;
                    case AttackRole.Garrison: RunGarrison(plan); break;
                    case AttackRole.Assault when _phase == AttackPhase.Hold: HoldHere(plan); break;
                }
            }
        }

        // ------------------------------------------------------------ deployment

        /// <summary>
        /// Deployment: the attack sets up for its first objective before the battle
        /// opens: the assault teams already gathered at the assembly point (or as
        /// near as the zone allows), the guns in their firing positions, everyone in
        /// cover the objectives cannot see. No marching about under the enemy's eyes.
        /// </summary>
        public void Deploy()
        {
            if (!_planned)
            {
                Plan();
            }

            ChooseTarget();
            if (_target == null || _view.OwnDeployZones.Count == 0)
            {
                return;
            }

            float facing = Soldier.HeadingOf(EnemyDirection());
            var assault = AssaultTeams();
            var taken = new List<float2>();
            for (int i = 0; i < assault.Count; i++)
            {
                Place(assault[i].Team, _assembly + Spread(i, assault.Count, 12f), facing, taken, false);
            }

            foreach (var plan in _plans)
            {
                var team = plan.Team;
                switch (plan.Role)
                {
                    case AttackRole.Support:
                        Place(team, _overwatch + Spread(IndexInRole(plan), CountRole(AttackRole.Support), 12f), facing, taken, false);
                        break;
                    case AttackRole.Sniper:
                        Place(team, SniperSpot(), facing, taken, false);
                        break;
                    case AttackRole.Armor:
                        Place(team, _armorSpot, facing, taken, true);
                        break;
                    case AttackRole.AntiTank:
                        Place(team, _overwatch + Rotate(EnemyDirection(), 90f) * 20f, facing, taken, true);
                        break;
                    case AttackRole.Headquarters:
                    case AttackRole.Reserve:
                        Place(team, _assembly - EnemyDirection() * 40f, facing, taken, false);
                        break;
                    case AttackRole.Mortar:
                        Place(team, Anchor(team), facing, taken, true);
                        break;
                }
            }

            if (NeedsAssembly())
            {
                return;
            }

            SetPhase(AttackPhase.Advance, "set up during deployment");
        }

        /// <summary>The best hidden, covered spot in our zone near <paramref name="wanted"/>; the team is put there.</summary>
        void Place(Team team, float2 wanted, float facing, List<float2> taken, bool outdoors)
        {
            float2 inZone = wanted;
            float nearest = float.MaxValue;
            foreach (var zone in _view.OwnDeployZones)
            {
                float2 q = Geometry.PolygonMath.Nearest(zone, wanted);
                float d = math.distance(q, wanted);
                if (d < nearest)
                {
                    nearest = d;
                    inZone = q;
                }
            }

            var grid = _view.Map.Grid;
            var movement = team.IsMechanized ? MovementClass.Vehicle : MovementClass.Infantry;
            float best = float.MinValue;
            float2 bestPoint = inZone;
            for (float dy = -25f; dy <= 25f; dy += 3f)
            {
                for (float dx = -25f; dx <= 25f; dx += 3f)
                {
                    float2 p = inZone + new float2(dx, dy);
                    var cell = grid.WorldToCell(p);
                    if (!grid.InBounds(cell) || !InOwnZone(p))
                    {
                        continue;
                    }

                    int index = grid.IndexOf(cell);
                    if (!_pathfinder.IsPassable(index, movement) || (outdoors && _view.Map.BuildingAt(index) != null))
                    {
                        continue;
                    }

                    bool crowded = false;
                    foreach (var other in taken)
                    {
                        if (math.distance(other, p) < 8f) crowded = true;
                    }

                    if (crowded)
                    {
                        continue;
                    }

                    float score = (outdoors ? grid.GetCover(index) * 0.1f : CoverScore(p)) - SeenFromObjectives(p) * 10f - math.length(new float2(dx, dy)) * 0.3f;
                    if (score > best)
                    {
                        best = score;
                        bestPoint = grid.CellCenter(cell);
                    }
                }
            }

            if (_view.Redeploy(team, bestPoint, facing))
            {
                taken.Add(bestPoint);
            }
        }

        bool InOwnZone(float2 p)
        {
            foreach (var zone in _view.OwnDeployZones)
            {
                if (Geometry.PolygonMath.Contains(zone, p)) return true;
            }

            return false;
        }

        /// <summary>How many objectives (where defenders are likely, perhaps upstairs) can see a point.</summary>
        int SeenFromObjectives(float2 p)
        {
            int seen = 0;
            foreach (var objective in _view.Objectives)
            {
                if (objective.Owner != Side && math.distance(objective.Position, p) < 400f
                    && LineOfSight.Check(_view.Map, objective.Position, 4.5f, p, LineOfSight.EyeKneeling).Visible)
                {
                    seen++;
                }
            }

            return seen;
        }

        // ------------------------------------------------------------ planning

        void Plan()
        {
            _planned = true;
            _danger = new DangerMap(_view.Map);
            var rifles = new List<AttackTeamPlan>();
            foreach (var team in _view.OwnTeams)
            {
                var plan = new AttackTeamPlan { Team = team, Role = RoleOf(team) };
                _plans.Add(plan);
                if (plan.Role == AttackRole.Assault)
                {
                    rifles.Add(plan);
                }
            }

            // With three or more rifle teams, the last plain rifle team is kept back as the reserve
            // (engineers stay with the assault: their charges and flamethrower are needed up front).
            if (rifles.Count >= 3)
            {
                for (int i = rifles.Count - 1; i >= 0; i--)
                {
                    if (rifles[i].Team.Type == TeamType.Rifle)
                    {
                        rifles[i].Role = AttackRole.Reserve;
                        break;
                    }
                }
            }

            _phaseSince = _view.Time;
        }

        static AttackRole RoleOf(Team team)
        {
            switch (team.Type)
            {
                case TeamType.MachineGun: return AttackRole.Support;
                case TeamType.Vehicle: return AttackRole.Armor;
                case TeamType.AntiTankGun: return AttackRole.AntiTank;
                case TeamType.Mortar: return AttackRole.Mortar;
                case TeamType.Headquarters: return AttackRole.Headquarters;
                case TeamType.Sniper: return AttackRole.Sniper;
                default: return AttackRole.Assault; // engineers go in with the assault teams
            }
        }

        void SetPhase(AttackPhase phase, string note = "")
        {
            _phase = phase;
            _phaseSince = _view.Time;
            _events.Add(new AttackEvent(_view.Time, phase, _target != null ? _target.Id : "", note));
        }

        float InPhase => _view.Time - _phaseSince;

        /// <summary>Picks the main effort and decides when to stop (or resume) attacking.</summary>
        void UpdateTarget()
        {
            if (_phase == AttackPhase.Hold)
            {
                // Recovered men and morale: try again.
                if (InPhase < HoldRecheck || _view.OwnForceMorale < ResumeMorale || (AssaultTeams().Count == 0 && !CommitReserve()) || !AnyToTake())
                {
                    if (InPhase >= HoldRecheck)
                    {
                        _phaseSince = _view.Time;
                    }

                    return;
                }

                _target = null;
                ChooseTarget();
                return;
            }

            if (_view.OwnForceMorale < GiveUpMorale)
            {
                SetPhase(AttackPhase.Hold, "force morale low");
                return;
            }

            if (AssaultTeams().Count == 0 && !CommitReserve())
            {
                SetPhase(AttackPhase.Hold, "no assault teams left");
                return;
            }

            if (_target != null && _phase != AttackPhase.Consolidate && Held(_target))
            {
                SetPhase(AttackPhase.Consolidate, "objective taken");
                return;
            }

            if (_target != null && _phase != AttackPhase.Consolidate)
            {
                return;
            }

            if (_target != null && InPhase < ConsolidateSeconds)
            {
                return; // still consolidating the last one
            }

            ChooseTarget();
        }

        /// <summary>A known machine gun or gun watches the assembly point or the jump-off.</summary>
        bool PlanCompromised()
        {
            foreach (var c in _contacts.All)
            {
                if (!(c.MachineGun || c.AntiTankGun || c.Vehicle) || c.Heard)
                {
                    continue;
                }

                float eye = LineOfSight.EyeHeightAt(_view.Map, c.Position, Posture.Kneeling);
                foreach (var p in new[] { _assembly, _jumpOff })
                {
                    if (math.distance(c.Position, p) < 350f && LineOfSight.Check(_view.Map, c.Position, eye, p, LineOfSight.EyeKneeling).Visible)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        bool AnyToTake()
        {
            foreach (var objective in _view.Objectives)
            {
                if (!Held(objective)) return true;
            }

            return false;
        }

        /// <summary>The objective worth the most for the least distance, open ground and known resistance.</summary>
        void ChooseTarget()
        {
            var front = Front();
            Objective best = null;
            float bestScore = float.MinValue;
            foreach (var objective in _view.Objectives)
            {
                if (Held(objective))
                {
                    continue;
                }

                _failures.TryGetValue(objective, out int failed);
                float score = objective.Value * 25f
                    - math.distance(front, objective.Position) * 0.8f
                    - Openness(front, objective.Position) * 30f
                    - _contacts.StrengthNear(objective.Position, objective.Radius + 40f) * 5f
                    - failed * 60f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = objective;
                }
            }

            _target = best;
            _hasGeometry = false;
            _smokeForAssault = false;
            _breach = BreachState.None;
            _blockedSince = float.NegativeInfinity;
            if (best == null)
            {
                SetPhase(AttackPhase.Hold);
                return;
            }

            ComputeGeometry();
            SetPhase(NeedsAssembly() ? AttackPhase.Assemble : AttackPhase.Advance);
        }

        bool Held(Objective objective) => objective.Owner == Side && !objective.Contested;

        /// <summary>Share of the straight line between two points that crosses open ground (no building, little cover).</summary>
        float Openness(float2 from, float2 to)
        {
            var grid = _view.Map.Grid;
            int open = 0, total = 0;
            float length = math.distance(from, to);
            for (float t = 0f; t <= length; t += 4f)
            {
                var cell = grid.WorldToCell(math.lerp(from, to, t / math.max(length, 1f)));
                if (!grid.InBounds(cell))
                {
                    continue;
                }

                int index = grid.IndexOf(cell);
                total++;
                if (_view.Map.BuildingAt(index) == null && grid.GetCover(index) < 30f)
                {
                    open++;
                }
            }

            return total == 0 ? 0f : open / (float)total;
        }

        /// <summary>
        /// The attack's ground for the current objective: an assembly point in cover
        /// 110–170 m out, an overwatch for the machine guns that sees the objective, a
        /// firing position for the assault gun away from known anti-tank guns, and the
        /// jump-off point for the assault, on the flank the known enemy covers least.
        /// </summary>
        void ComputeGeometry(bool keepOverwatch = false)
        {
            _geometryAt = _view.Time;
            float2 o = _target.Position;
            float2 front = Front();
            float2 toFront = math.normalizesafe(front - o, -EnemyDirection());

            var support = FirstOf(AttackRole.Support);
            float2 supportAt = support != null ? Anchor(support.Team) : front;
            bool overwatchStillGood = keepOverwatch && _hasGeometry && OverwatchScore(_overwatch, o, supportAt) > float.MinValue;
            if (!overwatchStillGood)
            {
                _overwatch = BestPoint(o, toFront, 90f, 240f, 75f, p => OverwatchScore(p, o, supportAt), supportAt, MovementClass.Infantry);
            }

            var sniper = FirstOf(AttackRole.Sniper);
            if (sniper != null)
            {
                float2 sniperAt = Anchor(sniper.Team);
                _sniperSpot = BestPoint(o, toFront, 120f, 320f, 85f, p => SniperScore(p, o, sniperAt), sniperAt, MovementClass.Infantry);
            }

            var armor = FirstOf(AttackRole.Armor);
            float2 armorAt = armor != null ? Anchor(armor.Team) : front;
            _armorSpot = BestPoint(o, toFront, 80f, 200f, 70f, p => ArmorScore(p, o, armorAt), armorAt, MovementClass.Vehicle);

            float2 fireLine = math.normalizesafe(_overwatch - o, toFront);
            float inner = _target.Radius + 20f;
            var candidates = new List<(float2 p, float score)>();
            for (float r = inner; r <= inner + 45f; r += 10f)
            {
                for (float a = -110f; a <= 110f; a += 10f)
                {
                    float2 p = o + Rotate(toFront, a) * r;
                    if (!Passable(p, MovementClass.Infantry))
                    {
                        continue;
                    }

                    // Flank: approach at an angle to the base of fire, where the known enemy sees least.
                    float angle = math.degrees(math.acos(math.clamp(math.dot(math.normalizesafe(p - o), fireLine), -1f, 1f)));
                    float score = -_danger.At(p) * 3f - SeenByContacts(p) * 12f + CoverScore(p)
                        + math.min(angle, 90f) / 9f
                        - math.distance(front, p) * 0.05f;
                    candidates.Add((p, score));
                }
            }

            if (candidates.Count == 0)
            {
                _jumpOff = o + toFront * inner;
                ComputeAssembly(o, front);
                _hasGeometry = true;
                return;
            }

            // The best few by spot, then by how much known fire the route to them crosses.
            candidates.Sort((x, y) => y.score.CompareTo(x.score));
            float bestScore = float.MinValue;
            _jumpOff = candidates[0].p;
            for (int i = 0; i < candidates.Count && i < 4; i++)
            {
                float score = candidates[i].score - RouteDanger(front, candidates[i].p) * 0.05f;
                if (score > bestScore)
                {
                    bestScore = score;
                    _jumpOff = candidates[i].p;
                }
            }

            ComputeAssembly(o, front);
            _hasGeometry = true;
        }

        /// <summary>The assembly point: in cover and out of the known enemy's sight, 50–110 m behind the jump-off.</summary>
        void ComputeAssembly(float2 objective, float2 front)
        {
            float2 back = math.normalizesafe(_jumpOff - objective, math.normalizesafe(front - objective));
            _assembly = BestPoint(_jumpOff, back, 50f, 110f, 70f,
                p => CoverScore(p) - SeenByContacts(p) * 30f - _danger.At(p) * 2f - math.distance(p, front) * 0.04f, _jumpOff, MovementClass.Infantry);
        }

        float OverwatchScore(float2 p, float2 objective, float2 from)
        {
            float eye = LineOfSight.EyeHeightAt(_view.Map, p, Posture.Kneeling);
            if (!LineOfSight.Check(_view.Map, p, eye, objective, LineOfSight.EyeKneeling).Visible)
            {
                return float.MinValue;
            }

            float score = CoverScore(p) - math.distance(p, from) * 0.08f - _danger.At(p) * 0.5f;
            foreach (var c in _contacts.All)
            {
                if (math.distance(c.Position, p) < 350f
                    && LineOfSight.Check(_view.Map, p, eye, c.Position, LineOfSight.EyeKneeling).Visible)
                {
                    score += 8f; // can reach a known enemy with fire
                }
            }

            return score;
        }

        /// <summary>A sniper's nest: sees the objective, upstairs and in cover if possible, apart from the machine guns.</summary>
        float SniperScore(float2 p, float2 objective, float2 from)
        {
            float eye = LineOfSight.EyeHeightAt(_view.Map, p, Posture.Kneeling);
            if (!LineOfSight.Check(_view.Map, p, eye, objective, LineOfSight.EyeKneeling).Visible)
            {
                return float.MinValue;
            }

            float score = CoverScore(p) * 1.5f - math.distance(p, from) * 0.04f - _danger.At(p) * 0.5f;
            if (math.distance(p, _overwatch) < 25f)
            {
                score -= 15f; // do not share the machine guns' spot: they draw fire
            }

            foreach (var c in _contacts.All)
            {
                if (math.distance(c.Position, p) < 500f
                    && LineOfSight.Check(_view.Map, p, eye, c.Position, LineOfSight.EyeKneeling).Visible)
                {
                    score += 8f;
                }
            }

            return score;
        }

        /// <summary>A gun position: sees the objective and the known enemy, unseen by known anti-tank guns and tanks.</summary>
        float ArmorScore(float2 p, float2 objective, float2 from)
        {
            const float eye = 2.0f;
            if (!LineOfSight.Check(_view.Map, p, eye, objective, LineOfSight.EyeKneeling).Visible)
            {
                return float.MinValue;
            }

            float score = -math.distance(p, from) * 0.05f - _danger.At(p) * 0.5f;
            foreach (var c in _contacts.All)
            {
                float d = math.distance(c.Position, p);
                bool sees = d < 600f && LineOfSight.Check(_view.Map, c.Position, 1.0f, p, eye).Visible;
                if ((c.AntiTankGun || c.Vehicle) && sees)
                {
                    score -= 25f;
                }
                else if (sees && d < 350f)
                {
                    score += 6f;
                }
            }

            return score;
        }

        /// <summary>Best point on a ring sector around <paramref name="center"/> facing <paramref name="facing"/>.</summary>
        float2 BestPoint(float2 center, float2 facing, float minRadius, float maxRadius, float halfAngle,
            System.Func<float2, float> score, float2 fallback, MovementClass movement)
        {
            float best = float.MinValue;
            float2 bestPoint = fallback;
            for (float r = minRadius; r <= maxRadius; r += 25f)
            {
                for (float a = -halfAngle; a <= halfAngle; a += 10f)
                {
                    float2 p = center + Rotate(facing, a) * r;
                    if (!Passable(p, movement))
                    {
                        continue;
                    }

                    float s = score(p);
                    if (s > best)
                    {
                        best = s;
                        bestPoint = p;
                    }
                }
            }

            return bestPoint;
        }

        bool NeedsAssembly()
        {
            var assault = AssaultTeams();
            if (assault.Count < 2)
            {
                return false;
            }

            // Already gathered and close: no need to fall back to an assembly point.
            float2 front = Front();
            foreach (var plan in assault)
            {
                if (math.distance(Anchor(plan.Team), front) > 40f)
                {
                    return true;
                }
            }

            return math.distance(front, _target.Position) > 180f;
        }

        // ------------------------------------------------------------ phases

        void Assemble()
        {
            var assault = AssaultTeams();

            bool gathered = true;
            for (int i = 0; i < assault.Count; i++)
            {
                var plan = assault[i];
                float2 slot = CoverNear(_assembly + Spread(i, assault.Count, 12f), 8f, MovementClass.Infantry, false);
                if (math.distance(Anchor(plan.Team), slot) > ArriveDistance + 4f)
                {
                    gathered = false;
                    MoveTo(plan, slot, OrderType.Move, ArriveDistance + 4f);
                }
                else
                {
                    TakeCover(plan);
                }
            }

            if (gathered || InPhase > 240f)
            {
                SetPhase(AttackPhase.Advance, gathered ? "assembled" : "assembly timed out");
            }
        }

        void Advance()
        {
            var assault = AssaultTeams();
            if (ContactNear(_target.Position, 250f) || ContactNear(Front(), 150f) || UnderFire(assault, 15f))
            {
                SetPhase(AttackPhase.Suppress, "contact");
                return;
            }

            if (!GroupIdle(assault))
            {
                return;
            }

            // Everyone at the jump-off and still nobody there: walk in.
            if (AllNear(assault, _jumpOff, 25f))
            {
                StartAssault(assault, "no enemy at the objective");
                return;
            }

            float2 next = NextBound(Front(), _jumpOff, BoundLength);
            if (ClearMinesOnTheWay(assault, Front(), next))
            {
                return;
            }

            MoveGroup(assault, next, OrderType.Move);
        }

        /// <summary>
        /// In contact: the base of fire works while the group rushes from cover to
        /// cover; a stretch the enemy watches is crossed behind smoke, or once the
        /// enemy watching it has gone quiet (or after waiting too long).
        /// </summary>
        void Suppress()
        {
            var assault = AssaultTeams();
            if (!GroupIdle(assault))
            {
                return;
            }

            foreach (var plan in assault)
            {
                TakeCover(plan);
            }

            bool hurry = _view.TimeLeft < 8f * 60f;
            float suppressFor = hurry ? 10f : SuppressSeconds;
            if (AllNear(assault, _jumpOff, 30f))
            {
                if (InPhase < suppressFor)
                {
                    return;
                }

                if (Breach(assault))
                {
                    return; // engineers are blowing the building open first
                }

                // The last stretch: smoke on whoever watches it, go when it blinds them (or it did not come).
                var watching = Watchers(_jumpOff, _target.Position);
                if (watching.Count > 0 && !_smokeForAssault)
                {
                    _smokeForAssault = true;
                    if (LaySmokeOn(assault, watching, (_jumpOff + _target.Position) * 0.5f))
                    {
                        return;
                    }
                }

                if (watching.Count == 0 || _view.Time - _smokeAt > 30f)
                {
                    StartAssault(assault, watching.Count == 0 ? "at the jump-off, unwatched" : "at the jump-off, going anyway");
                }

                return;
            }

            if (_view.Time - _lastRushAt < RushInterval || InPhase < 10f)
            {
                return;
            }

            float2 from = Front();
            float2 bound = NextBound(from, _jumpOff, RushLength);
            if (ClearMinesOnTheWay(assault, from, bound))
            {
                return;
            }

            var watchers = Watchers(from, bound);
            if (watchers.Count == 0)
            {
                // Unwatched (or screened by smoke still hanging): go, running if the smoke is what hides us.
                bool screened = _view.Time - _smokeAt < 50f;
                MoveGroup(assault, bound, screened ? OrderType.MoveFast : _danger.At(bound) > 2f ? OrderType.Crawl : OrderType.Move);
                _blockedSince = float.NegativeInfinity;
                return;
            }

            if (float.IsNegativeInfinity(_blockedSince))
            {
                _blockedSince = _view.Time;
            }

            // Watched: blind them first, then run once the smoke actually blocks their view.
            if (_view.Time - _smokeAt > 40f && LaySmokeOn(assault, watchers, (from + bound) * 0.5f))
            {
                return;
            }

            if (_view.Time - _smokeAt < 40f)
            {
                return; // smoke called for: wait for it to build up
            }

            float waited = _view.Time - _blockedSince;
            if (waited > (hurry ? 60f : MaxWait))
            {
                // No way across without smoke: try another objective rather than run into the guns.
                if (OtherObjectiveAvailable())
                {
                    Fail("no covered way forward");
                }
                else
                {
                    MoveGroup(assault, bound, OrderType.MoveFast);
                }
            }
        }

        bool OtherObjectiveAvailable()
        {
            foreach (var objective in _view.Objectives)
            {
                _failures.TryGetValue(objective, out int failed);
                if (objective != _target && !Held(objective) && failed < 2)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The attack on the current objective has failed: regroup, and after two failures try elsewhere.</summary>
        void Fail(string why)
        {
            _failures.TryGetValue(_target, out int failed);
            _failures[_target] = failed + 1;
            _smokeForAssault = false;
            foreach (var plan in AssaultTeams())
            {
                TakeCover(plan);
            }

            if (failed + 1 >= 2 || why == "no covered way forward")
            {
                SetPhase(AttackPhase.Consolidate, why);
                _target = null; // shift the main effort elsewhere
                _phaseSince -= ConsolidateSeconds;
            }
            else
            {
                SetPhase(AttackPhase.Suppress, why);
                _lastRushAt = _view.Time;
            }
        }

        void StartAssault(List<AttackTeamPlan> assault, string note)
        {
            SetPhase(AttackPhase.Assault, note);
            _assaultStrength = 0;
            foreach (var plan in assault)
            {
                _assaultStrength += plan.Team.ActiveCount;
                plan.HasGoal = false;
            }

            Storm(assault);
        }

        void Assault()
        {
            var assault = AssaultTeams();
            int strength = 0;
            bool anyActive = false;
            foreach (var plan in assault)
            {
                strength += plan.Team.ActiveCount;
                if (plan.Team.Activity != TeamActivity.Idle || _target.Contains(plan.Team.Centroid))
                {
                    anyActive = true;
                }
            }

            bool beaten = strength < _assaultStrength * 0.6f || assault.Count == 0 || (!anyActive && InPhase > 30f);
            if (beaten)
            {
                Fail(assault.Count == 0 ? "assault teams broken" : strength < _assaultStrength * 0.6f ? "assault lost too many men" : "assault stalled");
                return;
            }

            Storm(assault);
        }

        /// <summary>Assault teams that are idle and not yet on the objective go (again).</summary>
        void Storm(List<AttackTeamPlan> assault)
        {
            bool building = BuildingAt(_target.Position);
            for (int i = 0; i < assault.Count; i++)
            {
                var plan = assault[i];
                var team = plan.Team;
                _view.SetHoldFire(team, float.PositiveInfinity);
                if (team.Activity != TeamActivity.Idle || _view.Time - plan.LastOrderAt < 4f)
                {
                    continue;
                }

                if (_target.Contains(team.Centroid))
                {
                    if (team.Order.Type != OrderType.Defend) _view.Order(team, OrderType.Defend, team.Centroid);
                    continue;
                }

                float2 spot = _target.Position + Spread(i, assault.Count, _target.Radius * 0.5f);
                var order = building && BuildingAt(spot) ? OrderType.AssaultBuilding : OrderType.MoveFast;
                if (!_view.Order(team, order, spot, _danger.PathCost(2f)) && order == OrderType.AssaultBuilding)
                {
                    _view.Order(team, OrderType.MoveFast, spot, _danger.PathCost(2f));
                }

                plan.Goal = spot;
                plan.HasGoal = true;
                plan.LastOrderAt = _view.Time;
            }
        }

        void Consolidate()
        {
            if (_target == null)
            {
                return;
            }

            var assault = AssaultTeams();
            if (InPhase < 1f && assault.Count >= 3)
            {
                // Leave the most battered team to hold the objective.
                var weakest = assault[0];
                foreach (var plan in assault)
                {
                    if (plan.Team.ActiveCount < weakest.Team.ActiveCount) weakest = plan;
                }

                weakest.Role = AttackRole.Garrison;
                weakest.Goal = _target.Position;
                weakest.HasGoal = true;
                assault.Remove(weakest);
            }

            for (int i = 0; i < assault.Count; i++)
            {
                var plan = assault[i];
                if (plan.Team.Activity != TeamActivity.Idle)
                {
                    continue;
                }

                // Into the objective and dig in.
                float2 spot = _target.Position + Spread(i, assault.Count, _target.Radius * 0.6f);
                if (!_target.Contains(plan.Team.Centroid) && _view.Time - plan.LastOrderAt > 4f)
                {
                    _view.Order(plan.Team, OrderType.Move, spot, _danger.PathCost(4f));
                    plan.Goal = spot;
                    plan.HasGoal = true;
                    plan.LastOrderAt = _view.Time;
                }
                else if (plan.Team.Order.Type != OrderType.Defend)
                {
                    _view.Order(plan.Team, OrderType.Defend, plan.Team.Centroid);
                }
            }

            if (assault.Count < 2)
            {
                CommitReserve();
            }
        }

        /// <summary>Moves the reserve up into the assault group. False when there is none left.</summary>
        bool CommitReserve()
        {
            foreach (var plan in _plans)
            {
                if (plan.Role == AttackRole.Reserve && plan.Team.ActiveCount > 0 && !plan.Team.IsBroken)
                {
                    plan.Role = AttackRole.Assault;
                    plan.HasGoal = false;
                    return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------------ assault group

        static bool GroupIdle(List<AttackTeamPlan> assault)
        {
            foreach (var plan in assault)
            {
                if (plan.Team.Activity != TeamActivity.Idle)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The whole group moves to <paramref name="bound"/>, side by side, each to the best cover by its slot.</summary>
        void MoveGroup(List<AttackTeamPlan> assault, float2 bound, OrderType order)
        {
            _lastRushAt = _view.Time;
            _blockedSince = float.NegativeInfinity;
            for (int i = 0; i < assault.Count; i++)
            {
                var plan = assault[i];
                var team = plan.Team;
                float2 slot = CoverNear(bound + Spread(i, assault.Count, 10f), 6f, MovementClass.Infantry, false);
                if (math.distance(Anchor(team), slot) <= ArriveDistance)
                {
                    continue;
                }

                var teamOrder = order == OrderType.Crawl && team.GunTeam != null ? OrderType.Move : order;
                _view.SetHoldFire(team, float.PositiveInfinity);
                if (_view.Order(team, teamOrder, slot, _danger.PathCost(6f)))
                {
                    plan.Goal = slot;
                    plan.HasGoal = true;
                    plan.LastOrderAt = _view.Time;
                }
            }
        }

        // ------------------------------------------------------------ engineers (M12)

        enum BreachState : byte { None, Ordered, Planted, Done }

        BreachState _breach;
        AttackTeamPlan _breacher;
        int _chargesBefore;
        float _blastAt;
        float _breachOrderedAt;

        /// <summary>
        /// Before the assault on a building the enemy holds, engineers plant a
        /// charge on it (D39): the blast wrecks the rooms and the men in them, and
        /// the assault goes in right after. True while the group should wait for it.
        /// </summary>
        bool Breach(List<AttackTeamPlan> assault)
        {
            switch (_breach)
            {
                case BreachState.None:
                {
                    Contact inBuilding = null;
                    foreach (var c in _contacts.All)
                    {
                        if (!c.Heard && math.distance(c.Position, _target.Position) < _target.Radius + 15f && BuildingAt(c.Position)
                            && (inBuilding == null || c.Weight > inBuilding.Weight))
                        {
                            inBuilding = c;
                        }
                    }

                    if (inBuilding == null)
                    {
                        _breach = BreachState.Done;
                        return false;
                    }

                    foreach (var plan in assault)
                    {
                        var team = plan.Team;
                        if (team.Type == TeamType.Engineer && team.DemolitionCharges > 0 && team.Task == null
                            && math.distance(Anchor(team), inBuilding.Position) < 80f
                            && _view.Order(team, OrderType.Demolish, inBuilding.Position, _danger.PathCost(4f)))
                        {
                            _breacher = plan;
                            _chargesBefore = team.DemolitionCharges;
                            _breachOrderedAt = _view.Time;
                            plan.LastOrderAt = _view.Time;
                            _breach = BreachState.Ordered;
                            return true;
                        }
                    }

                    _breach = BreachState.Done; // no engineers at hand
                    return false;
                }

                case BreachState.Ordered:
                {
                    var team = _breacher.Team;
                    if (team.DemolitionCharges < _chargesBefore)
                    {
                        _blastAt = _view.Time + Engineering.FuseSeconds + 1f;
                        _breach = BreachState.Planted;
                        return true;
                    }

                    if (team.Task == null || team.ActiveCount == 0 || _view.Time - _breachOrderedAt > 120f)
                    {
                        _breach = BreachState.Done; // the attempt failed: go without it
                        return false;
                    }

                    return true;
                }

                case BreachState.Planted:
                    if (_view.Time < _blastAt)
                    {
                        return true;
                    }

                    _breach = BreachState.Done;
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// A known enemy minefield across the next bound: the engineers clear it
        /// while the rest wait (without engineers the route planner goes round it).
        /// True while the group should wait.
        /// </summary>
        bool ClearMinesOnTheWay(List<AttackTeamPlan> assault, float2 from, float2 to)
        {
            MineField blocking = null;
            foreach (var field in _view.KnownMineFields)
            {
                if (field.Owner != Side && DistanceToSegment(field.Center, from, to) < field.Radius + 8f)
                {
                    blocking = field;
                    break;
                }
            }

            if (blocking == null)
            {
                return false;
            }

            AttackTeamPlan engineers = null;
            foreach (var plan in assault)
            {
                if (plan.Team.Type == TeamType.Engineer)
                {
                    engineers = plan;
                    break;
                }
            }

            if (engineers == null)
            {
                return false;
            }

            var task = engineers.Team.Task;
            if (task != null && task.Type == OrderType.ClearMines)
            {
                return true; // still clearing
            }

            if (engineers.Team.Activity == TeamActivity.Idle && _view.Time - engineers.LastOrderAt > 4f
                && _view.Order(engineers.Team, OrderType.ClearMines, blocking.Center, _danger.PathCost(4f)))
            {
                engineers.LastOrderAt = _view.Time;
                return true;
            }

            return false;
        }

        static float DistanceToSegment(float2 p, float2 a, float2 b)
        {
            float2 ab = b - a;
            float t = math.saturate(math.dot(p - a, ab) / math.max(math.lengthsq(ab), 1e-4f));
            return math.distance(p, a + ab * t);
        }

        // ------------------------------------------------------------ snipers (M12)

        /// <summary>The sniper's nest for this objective (see <see cref="SniperScore"/>), in the best cover right there.</summary>
        float2 SniperSpot() => CoverNear(_sniperSpot, 6f, MovementClass.Infantry, false);

        void RunSniper(AttackTeamPlan plan)
        {
            var team = plan.Team;
            if (_target != null && _hasGeometry && _phase != AttackPhase.Hold && MoveTo(plan, SniperSpot(), OrderType.Move, 8f))
            {
                return;
            }

            // Lie in wait: a hidden sniper still takes officers and machine gunners (see Combat).
            if (team.Activity == TeamActivity.Idle && team.Order.Type != OrderType.Ambush)
            {
                _view.Order(team, OrderType.Ambush, team.Centroid);
            }
        }

        /// <summary>Holding between bounds: fire if the enemy is known, otherwise lie low.</summary>
        void TakeCover(AttackTeamPlan plan)
        {
            var team = plan.Team;
            if (team.Activity != TeamActivity.Idle)
            {
                return;
            }

            // Lie low between bounds; fire back only at an enemy close enough to matter.
            bool close = false;
            float2 at = team.Centroid;
            foreach (var enemy in _view.VisibleEnemies)
            {
                if (math.distance(enemy.Position, at) < 120f)
                {
                    close = true;
                    break;
                }
            }

            var stance = close ? OrderType.Defend : OrderType.Ambush;
            if (team.Order.Type != stance && team.Order.Type != OrderType.AreaFire && team.Order.Type != OrderType.Fire)
            {
                _view.Order(team, stance, team.Centroid);
            }

            _view.SetHoldFire(team, close ? float.PositiveInfinity : Combat.AmbushRange);
        }

        void HoldHere(AttackTeamPlan plan)
        {
            var team = plan.Team;
            if (team.Activity == TeamActivity.Idle && team.Order.Type != OrderType.Defend && team.Order.Type != OrderType.AreaFire)
            {
                _view.Order(team, OrderType.Defend, team.Centroid);
            }

            _view.SetHoldFire(team, float.PositiveInfinity);
        }

        /// <summary>The next bound toward <paramref name="goal"/>: about <paramref name="length"/> m along the route, at the best cover nearby.</summary>
        float2 NextBound(float2 from, float2 goal, float length)
        {
            if (math.distance(from, goal) <= length * 1.3f)
            {
                return goal;
            }

            var grid = _view.Map.Grid;
            _pathfinder.ExtraCost = _danger.PathCost(6f);
            _pathfinder.FindPath(grid.WorldToCell(from), grid.WorldToCell(goal), MovementClass.Infantry, _cells);
            _pathfinder.ExtraCost = null;
            if (_cells.Count == 0)
            {
                return goal;
            }

            float travelled = 0f;
            float2 previous = from;
            float2 point = grid.CellCenter(_cells[_cells.Count - 1]);
            foreach (var cell in _cells)
            {
                float2 c = grid.CellCenter(cell);
                travelled += math.distance(previous, c);
                previous = c;
                if (travelled >= length)
                {
                    point = c;
                    break;
                }
            }

            return CoverNear(point, 10f, MovementClass.Infantry, false);
        }

        /// <summary>Known enemies that would see the group on the way from <paramref name="from"/> to <paramref name="to"/>, smoke aside.</summary>
        List<Contact> Watchers(float2 from, float2 to)
        {
            var list = new List<Contact>();
            float2 mid = (from + to) * 0.5f;
            foreach (var c in _contacts.All)
            {
                if (math.distance(c.Position, mid) > 400f)
                {
                    continue;
                }

                float eye = LineOfSight.EyeHeightAt(_view.Map, c.Position, Posture.Kneeling);
                foreach (var p in new[] { mid, to })
                {
                    if (LineOfSight.Check(_view.Map, c.Position, eye, p, LineOfSight.EyeKneeling).Visible && !_view.SmokeBlocks(c.Position, p))
                    {
                        list.Add(c);
                        break;
                    }
                }
            }

            return list;
        }

        // ------------------------------------------------------------ support roles

        /// <summary>Machine guns: move to the overwatch, then fire on the known enemy.</summary>
        void RunSupport(AttackTeamPlan plan)
        {
            if (_phase == AttackPhase.Hold || _target == null || !_hasGeometry)
            {
                HoldHere(plan);
                return;
            }

            int index = IndexInRole(plan);
            float2 spot = CoverNear(_overwatch + Spread(index, CountRole(AttackRole.Support), 12f), 8f, MovementClass.Infantry, false);
            if (MoveTo(plan, spot, OrderType.Move, 6f))
            {
                return;
            }

            SuppressFrom(plan, 0, 0);
        }

        /// <summary>
        /// Fire on the most important known enemy the team can reach (D16: suppress
        /// before moving). Enemies in the team's own sight it engages by itself; area
        /// fire is for those hidden or gone to ground. With a burst size, a heavy weapon
        /// fires that many rounds and then rests, so shells remain for the assault.
        /// </summary>
        void SuppressFrom(AttackTeamPlan plan, int burst, int keep)
        {
            var team = plan.Team;
            _view.SetHoldFire(team, float.PositiveInfinity);
            if (team.Activity != TeamActivity.Idle)
            {
                return;
            }

            var gun = team.Gun;
            if (burst > 0 && gun != null && team.Order.Type == OrderType.AreaFire
                && (plan.BurstStartAmmo - gun.AmmoHe >= burst || _view.Time - plan.LastFireOrderAt > 40f))
            {
                _view.Order(team, OrderType.Defend, team.Centroid);
                plan.CooldownUntil = _view.Time + 30f;
                return;
            }

            if (_view.Time < plan.CooldownUntil || _view.Time - plan.LastFireOrderAt < 12f)
            {
                return;
            }

            float2 from = Anchor(team);
            var target = MainContact(from, team.IsMechanized || team.GunTeam != null);
            if (target != null && !SeesEnemyNear(from, target.Position)
                && (gun == null || gun.AmmoHe > keep)
                && _view.Order(team, OrderType.AreaFire, target.Position))
            {
                plan.LastFireOrderAt = _view.Time;
                plan.BurstStartAmmo = gun != null ? gun.AmmoHe : 0;
                return;
            }

            if (team.Order.Type != OrderType.Defend && team.Order.Type != OrderType.AreaFire)
            {
                _view.Order(team, OrderType.Defend, team.Centroid);
            }
        }

        /// <summary>Whether the team itself can see an enemy near the target (then it fires at him directly).</summary>
        bool SeesEnemyNear(float2 from, float2 target)
        {
            float eye = LineOfSight.EyeHeightAt(_view.Map, from, Posture.Kneeling);
            foreach (var enemy in _view.VisibleEnemies)
            {
                if (math.distance(enemy.Position, target) < 25f
                    && LineOfSight.Check(_view.Map, from, eye, enemy.Position, LineOfSight.EyeKneeling).Visible)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The enemy group to suppress: machine guns and guns first, the more recent
        /// and the closer to the objective and the assault route, the better.
        /// </summary>
        Contact MainContact(float2 shooter, bool heavy)
        {
            Contact best = null;
            float bestScore = float.MinValue;
            float now = _view.Time;
            float eye = LineOfSight.EyeHeightAt(_view.Map, shooter, Posture.Kneeling);
            foreach (var c in _contacts.All)
            {
                float range = math.distance(c.Position, shooter);
                if (range > 450f || !LineOfSight.Check(_view.Map, shooter, eye, c.Position, LineOfSight.EyeKneeling).Visible)
                {
                    continue;
                }

                float score = c.Weight * 3f - (now - c.SeenAt) * 0.05f - range * 0.01f;
                if (_target != null)
                {
                    score -= math.distance(c.Position, _target.Position) * 0.05f;
                    score -= math.distance(c.Position, Front()) * 0.03f;
                }

                if (heavy && BuildingAt(c.Position)) score += 6f; // shells bring the house down on them
                if (heavy && c.Vehicle) score += 10f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }

            return best;
        }

        /// <summary>
        /// The assault gun shells the enemy from its firing position until the
        /// assault goes in, then follows the infantry 25 m behind. It moves off
        /// when a known anti-tank gun or tank turns out to see it.
        /// </summary>
        void RunArmor(AttackTeamPlan plan)
        {
            var team = plan.Team;
            if (team.Vehicle == null || !team.Vehicle.IsOperational || _phase == AttackPhase.Hold || _target == null || !_hasGeometry)
            {
                HoldHere(plan);
                return;
            }

            float2 spot = _armorSpot;
            if (_phase == AttackPhase.Assault || _phase == AttackPhase.Consolidate)
            {
                var lead = LeadAssaultTeam();
                if (lead != null)
                {
                    float2 basis = Anchor(lead.Team);
                    spot = CoverNear(basis - math.normalizesafe(_target.Position - basis, EnemyDirection()) * 25f, 15f, MovementClass.Vehicle, true);
                }
            }
            else if (ThreatenedByAntiTank(Anchor(team)) && _view.Time - _armorSpotAt > 30f)
            {
                _armorSpotAt = _view.Time;
                float2 toFront = math.normalizesafe(Front() - _target.Position, -EnemyDirection());
                float2 at = Anchor(team);
                _armorSpot = BestPoint(_target.Position, toFront, 80f, 200f, 70f, p => ArmorScore(p, _target.Position, at), at, MovementClass.Vehicle);
                spot = _armorSpot;
            }

            if (MoveTo(plan, spot, OrderType.Move, 8f))
            {
                return;
            }

            SuppressFrom(plan, 3, _phase == AttackPhase.Assault ? 0 : 6);
        }

        bool ThreatenedByAntiTank(float2 at)
        {
            foreach (var c in _contacts.All)
            {
                if ((c.AntiTankGun || c.Vehicle) && math.distance(c.Position, at) < 600f
                    && LineOfSight.Check(_view.Map, c.Position, 1.0f, at, 2.0f).Visible)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Anti-tank guns set up beside the overwatch, outside buildings, facing the objective.</summary>
        void RunAntiTank(AttackTeamPlan plan)
        {
            if (_phase == AttackPhase.Hold || _target == null || !_hasGeometry)
            {
                HoldHere(plan);
                return;
            }

            float2 side = Rotate(math.normalizesafe(_overwatch - _target.Position, -EnemyDirection()), 90f) * 20f;
            float2 spot = CoverNear(_overwatch + side, 15f, MovementClass.Infantry, true);
            if (!MoveTo(plan, spot, OrderType.Move, 8f))
            {
                HoldHere(plan);
            }
        }

        /// <summary>Mortars: stay back within range, shell the main contact in bursts, lay smoke when asked.</summary>
        void RunMortar(AttackTeamPlan plan)
        {
            var team = plan.Team;
            var gun = team.Gun;
            if (gun == null || _target == null)
            {
                HoldHere(plan);
                return;
            }

            // Out of range of the objective: move closer, staying well back.
            float range = math.distance(gun.Position, _target.Position);
            if (range > gun.Definition.MaxRange - 20f)
            {
                float2 toward = math.normalizesafe(_target.Position - gun.Position);
                float2 spot = CoverNear(_target.Position - toward * (gun.Definition.MaxRange * 0.7f), 15f, MovementClass.Infantry, true);
                if (MoveTo(plan, spot, OrderType.Move, 8f))
                {
                    return;
                }
            }

            if (team.Activity != TeamActivity.Idle)
            {
                return;
            }

            // A burst of four, then a rest; keep six rounds for the assault unless it is going in now.
            if (team.Order.Type == OrderType.AreaFire && (plan.BurstStartAmmo - gun.AmmoHe >= 4 || _view.Time - plan.LastFireOrderAt > 40f))
            {
                _view.Order(team, OrderType.Defend, team.Centroid);
                plan.CooldownUntil = _view.Time + 30f;
                return;
            }

            bool assaultSoon = _phase == AttackPhase.Assault || (_phase == AttackPhase.Suppress && AllNear(AssaultTeams(), _jumpOff, 30f));
            int keep = assaultSoon ? 0 : 8;
            if (_phase != AttackPhase.Consolidate && gun.AmmoHe > keep
                && _view.Time >= plan.CooldownUntil && team.Order.Type != OrderType.AreaFire)
            {
                var target = MainContactInRange(gun);
                if (target != null && _view.Order(team, OrderType.AreaFire, target.Position))
                {
                    plan.LastFireOrderAt = _view.Time;
                    plan.BurstStartAmmo = gun.AmmoHe;
                    return;
                }
            }

            if (team.Order.Type != OrderType.Defend && team.Order.Type != OrderType.AreaFire)
            {
                _view.Order(team, OrderType.Defend, team.Centroid);
            }
        }

        Contact MainContactInRange(HeavyWeapon gun)
        {
            Contact best = null;
            float bestScore = float.MinValue;
            foreach (var c in _contacts.All)
            {
                float d = math.distance(c.Position, gun.Position);
                if (d < gun.Definition.MinRange || d > gun.Definition.MaxRange)
                {
                    continue;
                }

                // Only what bears on this attack: near the objective or the assault group.
                float relevance = math.min(math.distance(c.Position, _target.Position), math.distance(c.Position, Front()));
                if (relevance > 150f)
                {
                    continue;
                }

                float score = c.Weight * 3f - (_view.Time - c.SeenAt) * 0.05f - relevance * 0.05f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }

            return best;
        }

        void RunHeadquarters(AttackTeamPlan plan)
        {
            if (_phase == AttackPhase.Hold || _target == null || !_hasGeometry)
            {
                HoldHere(plan);
                return;
            }

            // Behind the assault group, toward our own edge, where it can steady the men.
            float2 front = Front();
            float2 back = math.normalizesafe(_view.OwnEdgePoint(front) - front, -EnemyDirection());
            float2 spot = CoverNear(front + back * 40f, 12f, MovementClass.Infantry, false);
            if (!MoveTo(plan, spot, OrderType.Move, 12f))
            {
                TakeCover(plan);
            }
        }

        void RunReserve(AttackTeamPlan plan)
        {
            if (_phase == AttackPhase.Hold || _target == null || !_hasGeometry)
            {
                HoldHere(plan);
                return;
            }

            // Out of sight about 70 m behind the assault group.
            float2 front = Front();
            float2 back = math.normalizesafe(_view.OwnEdgePoint(front) - front, -EnemyDirection());
            float2 spot = CoverNear(front + back * 70f, 15f, MovementClass.Infantry, false);
            if (!MoveTo(plan, spot, OrderType.Move, 15f))
            {
                TakeCover(plan);
            }
        }

        void RunGarrison(AttackTeamPlan plan)
        {
            if (!MoveTo(plan, plan.Goal, OrderType.Move, 8f))
            {
                HoldHere(plan);
            }
        }

        /// <summary>
        /// Sends the team to <paramref name="spot"/> unless it is already there (within
        /// <paramref name="tolerance"/>). True while it is on its way.
        /// </summary>
        bool MoveTo(AttackTeamPlan plan, float2 spot, OrderType order, float tolerance)
        {
            var team = plan.Team;
            if (math.distance(Anchor(team), spot) <= tolerance)
            {
                return false;
            }

            if (team.Activity != TeamActivity.Idle && plan.HasGoal && math.distance(plan.Goal, spot) < 12f)
            {
                return true;
            }

            if (team.Activity != TeamActivity.Idle && _view.Time - plan.LastOrderAt < ReorderInterval)
            {
                return true;
            }

            // Got as close as it can: do not keep trying every couple of seconds.
            if (plan.HasGoal && math.distance(plan.Goal, spot) < 12f && _view.Time - plan.LastOrderAt < 30f)
            {
                return false;
            }

            plan.LastOrderAt = _view.Time;
            plan.Goal = spot;
            plan.HasGoal = true;
            _view.SetHoldFire(team, float.PositiveInfinity);
            return _view.Order(team, order, spot, _danger.PathCost(6f));
        }

        // ------------------------------------------------------------ smoke

        /// <summary>A known enemy near the objective sees the stretch from <paramref name="from"/> to <paramref name="to"/>.</summary>
        bool NeedsSmoke(float2 from, float2 to)
        {
            return Watchers(from, to).Count > 0;
        }

        /// <summary>Smoke against whoever watches the final stretch to the objective.</summary>
        bool LaySmoke(List<AttackTeamPlan> assault, float2 from, float2 to)
        {
            return LaySmokeOn(assault, Watchers(from, to), (from + to) * 0.5f);
        }

        /// <summary>
        /// Mortar smoke between the most dangerous watcher and the group's path, and
        /// grenades from teams close enough to throw. False when nothing could be laid.
        /// </summary>
        bool LaySmokeOn(List<AttackTeamPlan> assault, List<Contact> watchers, float2 path)
        {
            Contact worst = null;
            foreach (var c in watchers)
            {
                if (worst == null || c.Weight > worst.Weight) worst = c;
            }

            if (worst == null)
            {
                return false;
            }

            bool laid = false;
            // Between the enemy and the path: blinds him without blinding us at the objective.
            float2 screen = math.lerp(worst.Position, path, 0.3f);
            foreach (var plan in _plans)
            {
                var gun = plan.Team.Gun;
                if (plan.Role == AttackRole.Mortar && gun != null && gun.AmmoSmoke > 0 && plan.Team.ActiveCount > 0)
                {
                    laid |= _view.Order(plan.Team, OrderType.Smoke, screen);
                }
            }

            foreach (var plan in assault)
            {
                var leader = plan.Team.Leader;
                if (leader == null || plan.Team.SmokeGrenades <= 0)
                {
                    continue;
                }

                float2 toward = math.normalizesafe(worst.Position - leader.Position);
                float reach = math.min(BattleState.MaxSmokeThrow - 3f, math.distance(leader.Position, worst.Position) * 0.5f);
                if (reach > 8f && _view.Order(plan.Team, OrderType.Smoke, leader.Position + toward * reach))
                {
                    laid = true;
                    break; // one grenade at a time
                }
            }

            if (laid)
            {
                _smokeAt = _view.Time;
            }

            return laid;
        }

        // ------------------------------------------------------------ helpers

        List<AttackTeamPlan> AssaultTeams()
        {
            var list = new List<AttackTeamPlan>();
            foreach (var plan in _plans)
            {
                if (plan.Role == AttackRole.Assault && plan.Team.ActiveCount > 0 && !plan.Team.IsBroken)
                {
                    list.Add(plan);
                }
            }

            return list;
        }

        AttackTeamPlan LeadAssaultTeam()
        {
            AttackTeamPlan best = null;
            float bestDistance = float.MaxValue;
            foreach (var plan in AssaultTeams())
            {
                float d = math.distance(Anchor(plan.Team), _target.Position);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = plan;
                }
            }

            return best;
        }

        AttackTeamPlan FirstOf(AttackRole role)
        {
            foreach (var plan in _plans)
            {
                if (plan.Role == role && plan.Team.ActiveCount > 0)
                {
                    return plan;
                }
            }

            return null;
        }

        int IndexInRole(AttackTeamPlan of)
        {
            int index = 0;
            foreach (var plan in _plans)
            {
                if (plan == of) return index;
                if (plan.Role == of.Role && plan.Team.ActiveCount > 0) index++;
            }

            return index;
        }

        int CountRole(AttackRole role)
        {
            int count = 0;
            foreach (var plan in _plans)
            {
                if (plan.Role == role && plan.Team.ActiveCount > 0) count++;
            }

            return count;
        }

        /// <summary>Where the assault group is (all teams if it has none left).</summary>
        float2 Front()
        {
            float2 sum = float2.zero;
            int n = 0;
            foreach (var plan in _plans)
            {
                if (plan.Role == AttackRole.Assault && plan.Team.ActiveCount > 0 && !plan.Team.IsBroken)
                {
                    sum += Anchor(plan.Team);
                    n++;
                }
            }

            if (n == 0)
            {
                foreach (var plan in _plans)
                {
                    if (plan.Team.ActiveCount > 0)
                    {
                        sum += Anchor(plan.Team);
                        n++;
                    }
                }
            }

            return n > 0 ? sum / n : float2.zero;
        }

        /// <summary>Direction from our edge toward the enemy's.</summary>
        float2 EnemyDirection()
        {
            switch (_view.EnemyEdge)
            {
                case MapEdge.North: return new float2(0f, 1f);
                case MapEdge.South: return new float2(0f, -1f);
                case MapEdge.East: return new float2(1f, 0f);
                default: return new float2(-1f, 0f);
            }
        }

        bool ContactNear(float2 point, float radius)
        {
            foreach (var c in _contacts.All)
            {
                if (math.distance(c.Position, point) <= radius)
                {
                    return true;
                }
            }

            return false;
        }

        bool NewContactSince(float time)
        {
            foreach (var c in _contacts.All)
            {
                if (c.SeenAt > time && !c.Heard)
                {
                    return true;
                }
            }

            return false;
        }

        static bool UnderFire(List<AttackTeamPlan> teams, float threshold)
        {
            foreach (var plan in teams)
            {
                if (plan.Team.AverageSuppression > threshold)
                {
                    return true;
                }
            }

            return false;
        }

        bool AllNear(List<AttackTeamPlan> teams, float2 point, float radius)
        {
            if (teams.Count == 0)
            {
                return false;
            }

            foreach (var plan in teams)
            {
                if (math.distance(Anchor(plan.Team), point) > radius + 5f * teams.Count)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>How many known enemy groups can see a point.</summary>
        int SeenByContacts(float2 p)
        {
            int seen = 0;
            foreach (var c in _contacts.All)
            {
                if (math.distance(c.Position, p) > 400f)
                {
                    continue;
                }

                float eye = LineOfSight.EyeHeightAt(_view.Map, c.Position, Posture.Kneeling);
                if (LineOfSight.Check(_view.Map, c.Position, eye, p, LineOfSight.EyeKneeling).Visible)
                {
                    seen++;
                }
            }

            return seen;
        }

        float RouteDanger(float2 from, float2 to)
        {
            var grid = _view.Map.Grid;
            _pathfinder.ExtraCost = null;
            _pathfinder.FindPath(grid.WorldToCell(from), grid.WorldToCell(to), MovementClass.Infantry, _cells);
            float total = 0f;
            for (int i = 0; i < _cells.Count; i += 2)
            {
                total += _danger.At(grid.CellCenter(_cells[i])) * 2f;
            }

            return total;
        }

        /// <summary>Cover value of a spot: inside a building best, then walls and rubble.</summary>
        float CoverScore(float2 p)
        {
            var grid = _view.Map.Grid;
            var cell = grid.WorldToCell(p);
            if (!grid.InBounds(cell))
            {
                return float.MinValue;
            }

            int index = grid.IndexOf(cell);
            var building = _view.Map.BuildingAt(index);
            if (building != null && !grid.HasFlag(index, CellFlags.Collapsed))
            {
                return 12f + 2f * math.min(building.Floors, 4);
            }

            return grid.GetCover(index) * 0.1f;
        }

        /// <summary>The best covered, passable spot within <paramref name="radius"/> of a point.</summary>
        float2 CoverNear(float2 point, float radius, MovementClass movement, bool outdoors)
        {
            var grid = _view.Map.Grid;
            float best = float.MinValue;
            float2 bestPoint = point;
            for (float dy = -radius; dy <= radius; dy += 2f)
            {
                for (float dx = -radius; dx <= radius; dx += 2f)
                {
                    float2 p = point + new float2(dx, dy);
                    var cell = grid.WorldToCell(p);
                    if (!grid.InBounds(cell))
                    {
                        continue;
                    }

                    int index = grid.IndexOf(cell);
                    if (!_pathfinder.IsPassable(index, movement) || (outdoors && _view.Map.BuildingAt(index) != null))
                    {
                        continue;
                    }

                    float score = (outdoors ? grid.GetCover(index) * 0.1f : CoverScore(p))
                        - math.length(new float2(dx, dy)) * 0.3f - _danger.At(p) * 2f;
                    if (score > best)
                    {
                        best = score;
                        bestPoint = grid.CellCenter(cell);
                    }
                }
            }

            return bestPoint;
        }

        bool Passable(float2 p, MovementClass movement)
        {
            var grid = _view.Map.Grid;
            var cell = grid.WorldToCell(p);
            return grid.InBounds(cell) && _pathfinder.IsPassable(grid.IndexOf(cell), movement);
        }

        bool BuildingAt(float2 p)
        {
            var grid = _view.Map.Grid;
            var cell = grid.WorldToCell(p);
            return grid.InBounds(cell) && _view.Map.BuildingAt(grid.IndexOf(cell)) != null;
        }

        /// <summary>Teams side by side across the approach: offsets of <paramref name="spacing"/> m around a point.</summary>
        float2 Spread(int index, int count, float spacing)
        {
            float2 across = Rotate(EnemyDirection(), 90f);
            return across * ((index - (count - 1) * 0.5f) * spacing);
        }

        static float2 Rotate(float2 v, float degrees)
        {
            float r = math.radians(degrees);
            float c = math.cos(r), s = math.sin(r);
            return new float2(v.x * c - v.y * s, v.x * s + v.y * c);
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
    }
}
