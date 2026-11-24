using System.Collections.Generic;
using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using Unity.Mathematics;
using UnityEngine;

namespace CloseFight.Game.View
{
    /// <summary>
    /// Draws soldiers and team routes. Reads <see cref="BattleState"/> only and
    /// interpolates between simulation ticks so movement looks smooth at any
    /// frame rate. Soldiers inside a building are drawn on its roof, the way
    /// Close Combat shows units in buildings.
    /// </summary>
    public sealed class UnitsView : MonoBehaviour
    {
        const string RootName = "Units";

        [SerializeField] Material lineMaterial;
        [Tooltip("Marker diameter in meters (a bit larger than life so soldiers read at a distance).")]
        [SerializeField] float soldierSize = 0.9f;
        [SerializeField] float lineWidth = 0.35f;

        static readonly Color GermanColor = new Color(0.52f, 0.55f, 0.48f);
        static readonly Color SovietColor = new Color(0.70f, 0.62f, 0.40f);
        static readonly Color SelectedRing = new Color(1f, 0.85f, 0.2f);
        static readonly Color OutOfControlRing = new Color(0.95f, 0.3f, 0.95f);
        static readonly Color DeadColor = new Color(0.22f, 0.20f, 0.18f);
        static readonly Color DisabledColor = new Color(0.45f, 0.16f, 0.12f);
        static readonly Color SmokeColor = new Color(0.78f, 0.78f, 0.75f);

        sealed class Marker
        {
            public Soldier Soldier;
            public Transform Transform;
            public SpriteRenderer Body;
            public SpriteRenderer Ring;
        }

        BattleState _battle;
        GameObject _root;
        readonly List<Marker> _markers = new List<Marker>();
        readonly Dictionary<Team, LineRenderer> _lines = new Dictionary<Team, LineRenderer>();
        readonly HashSet<Team> _selected = new HashSet<Team>();
        readonly List<float2> _scratchPath = new List<float2>();
        readonly List<Vector3> _scratchLine = new List<Vector3>();

        // Fog of war (M3): enemies are drawn only while the viewing side sees them.
        bool _fogEnabled;
        Side _viewSide;
        LineRenderer _sightClear;
        LineRenderer _sightBlocked;
        readonly List<SpriteRenderer> _soundPool = new List<SpriteRenderer>();

        // Combat (M4)
        readonly List<LineRenderer> _tracerPool = new List<LineRenderer>();
        readonly List<SpriteRenderer> _smokePool = new List<SpriteRenderer>();
        /// <summary>Enemy soldiers the viewing side has seen; their bodies stay visible after they fall.</summary>
        readonly HashSet<int> _seenEver = new HashSet<int>();
        readonly EquipmentRenderer _equipment = new EquipmentRenderer();

        // Objectives and the AI's plan (M7)
        /// <summary>One flat, still flag per objective, of the side holding it (replaces the old rings).</summary>
        readonly List<SpriteRenderer> _flags = new List<SpriteRenderer>();
        readonly List<LineRenderer> _planLines = new List<LineRenderer>();
        readonly List<SpriteRenderer> _planRings = new List<SpriteRenderer>();
        const float FlagHeight = 23.5f;
        static readonly Color PlanPrimary = new Color(0.2f, 0.9f, 1f, 0.9f);
        static readonly Color PlanFallback = new Color(0.3f, 0.45f, 1f, 0.9f);

        /// <summary>Debug: draw where the enemy AI has sent each team (F9). It shows the enemy's plan, so it is off in normal play.</summary>
        public bool ShowAiPlan { get; set; }

        /// <summary>How long a tracer stays on screen (the screenshot tool lengthens it).</summary>
        public float TracerLifetime { get; set; } = 0.25f;

        static readonly Color SoundColor = new Color(1f, 0.35f, 0.15f);
        static readonly Color SightClearColor = new Color(0.3f, 1f, 0.3f, 0.95f);
        static readonly Color SightBlockedColor = new Color(1f, 0.25f, 0.2f, 0.95f);

        public void Build(BattleState battle)
        {
            ClearView();
            _battle = battle;
            _root = new GameObject(RootName);
            _root.transform.SetParent(transform, false);

            foreach (var soldier in battle.Soldiers)
            {
                _markers.Add(CreateMarker(soldier));
            }

            foreach (var team in battle.Teams)
            {
                _lines[team] = CreateLine("Route " + team.Key);
            }

            _equipment.Build(_root.transform, battle);

            if (FlagSprites.Available)
            {
                foreach (var objective in battle.Objectives)
                {
                    var flag = NewOverlaySprite("Flag " + objective.Id, FlagSprites.Get(objective.Owner), 13);
                    flag.sharedMaterial = FlagSprites.MaterialFor(flag.sharedMaterial);
                    flag.transform.position = new Vector3(objective.Position.x, FlagHeight, objective.Position.y);
                    _flags.Add(flag);
                }
            }

            _sightClear = CreateLine("Sight (clear)");
            _sightBlocked = CreateLine("Sight (blocked)");
            SetLineColor(_sightClear, SightClearColor);
            SetLineColor(_sightBlocked, SightBlockedColor);

            if (!Application.isPlaying)
            {
                foreach (var t in _root.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.hideFlags = HideFlags.DontSave;
                }
            }

            Sync(1f);
        }

        Marker CreateMarker(Soldier soldier)
        {
            var go = new GameObject(soldier.FullName);
            go.transform.SetParent(_root.transform, false);
            var body = go.AddComponent<SpriteRenderer>();
            body.sprite = MarkerSprites.Body;
            body.color = soldier.Team.Side == Side.German ? GermanColor : SovietColor;
            body.sortingOrder = 1;

            var ringObject = new GameObject("Selection");
            ringObject.transform.SetParent(go.transform, false);
            ringObject.transform.localScale = Vector3.one * 1.35f;
            var ring = ringObject.AddComponent<SpriteRenderer>();
            ring.sprite = MarkerSprites.Ring;
            ring.color = SelectedRing;
            ring.sortingOrder = 2;
            ring.enabled = false;

            return new Marker { Soldier = soldier, Transform = go.transform, Body = body, Ring = ring };
        }

        LineRenderer CreateLine(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.widthMultiplier = lineWidth;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = 0;
            line.enabled = false;
            return line;
        }

        static void SetLineColor(LineRenderer line, Color color)
        {
            line.startColor = color;
            line.endColor = color;
        }

        /// <summary>Turns on fog of war for the given side: enemies it cannot see are hidden.</summary>
        public void SetViewSide(Side side)
        {
            _fogEnabled = true;
            _viewSide = side;
        }

        /// <summary>
        /// Close Combat's line-of-sight tool: green from the observer to where the
        /// view is blocked, red beyond it.
        /// </summary>
        public void ShowSightLine(float2 from, float2 to, float blockedAt)
        {
            blockedAt = math.saturate(blockedAt);
            float2 split = math.lerp(from, to, blockedAt);
            DrawSegment(_sightClear, from, split);
            if (blockedAt < 0.999f)
            {
                DrawSegment(_sightBlocked, split, to);
            }
            else
            {
                _sightBlocked.enabled = false;
            }
        }

        public void HideSightLine()
        {
            if (_sightClear != null)
            {
                _sightClear.enabled = false;
                _sightBlocked.enabled = false;
            }
        }

        void DrawSegment(LineRenderer line, float2 a, float2 b)
        {
            if (math.distancesq(a, b) < 1e-4f)
            {
                line.enabled = false;
                return;
            }

            line.enabled = true;
            line.positionCount = 2;
            line.SetPosition(0, new Vector3(a.x, 25f, a.y));
            line.SetPosition(1, new Vector3(b.x, 25f, b.y));
        }

        public void SetSelection(IEnumerable<Team> teams)
        {
            _selected.Clear();
            foreach (var team in teams)
            {
                _selected.Add(team);
            }
        }

        /// <summary>Updates every marker and route. <paramref name="alpha"/> blends from the previous tick (0) to the current one (1).</summary>
        public void Sync(float alpha)
        {
            if (_battle == null)
            {
                return;
            }

            foreach (var marker in _markers)
            {
                SyncMarker(marker, alpha);
            }

            foreach (var pair in _lines)
            {
                SyncLine(pair.Key, pair.Value);
            }

            SyncSoundMarkers();
            SyncTracers();
            SyncSmoke();
            _equipment.Sync(alpha, _fogEnabled, _viewSide, _selected);
            SyncObjectives();
            SyncMineFields();
            SyncMissions();
            SyncAiPlan();
        }

        SpriteRenderer NewOverlaySprite(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            if (!Application.isPlaying)
            {
                go.hideFlags = HideFlags.DontSave;
            }

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>
        /// Each objective shows the flag of the side holding it, lying still at its
        /// centre; a little larger when zoomed out so it stays readable.
        /// </summary>
        void SyncObjectives()
        {
            float scale = FlagScale();
            for (int i = 0; i < _flags.Count && i < _battle.Objectives.Count; i++)
            {
                var flag = _flags[i];
                flag.sprite = FlagSprites.Get(_battle.Objectives[i].Owner);
                flag.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        Camera _camera;

        /// <summary>Flag size: the cloth about 8–22 m wide, growing as the camera zooms out.</summary>
        float FlagScale()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            float ortho = _camera != null && _camera.orthographic ? _camera.orthographicSize : 60f;
            return math.clamp(ortho * 0.1f, 8f, 22f) / FlagSprites.ClothWidth;
        }

        /// <summary>World point just above an objective's flag, for its name label.</summary>
        public Vector3 ObjectiveLabelPoint(int index)
        {
            var objective = _battle.Objectives[index];
            float top = FlagSprites.ClothHeight * 0.5f * FlagScale() + 1f;
            return new Vector3(objective.Position.x, 0f, objective.Position.y + top);
        }

        static readonly Color OwnMines = new Color(1f, 0.85f, 0.25f, 0.85f);
        static readonly Color EnemyMines = new Color(1f, 0.25f, 0.2f, 0.9f);
        readonly List<SpriteRenderer> _minePool = new List<SpriteRenderer>();

        /// <summary>Minefields the viewing side knows about: its own in yellow, the enemy's it found in red.</summary>
        void SyncMineFields()
        {
            int used = 0;
            foreach (var field in _battle.Engineering.MineFields)
            {
                bool known = !_fogEnabled || field.KnownTo(_viewSide);
                if (!known)
                {
                    continue;
                }

                if (used == _minePool.Count)
                {
                    _minePool.Add(NewOverlaySprite("Minefield", MarkerSprites.Ring, 9));
                }

                var ring = _minePool[used++];
                ring.enabled = true;
                ring.color = _fogEnabled && field.Owner != _viewSide ? EnemyMines : OwnMines;
                float size = field.Radius * 2f / 0.94f;
                ring.transform.position = new Vector3(field.Center.x, 23f, field.Center.y);
                ring.transform.localScale = new Vector3(size, size, 1f);
            }

            for (int i = used; i < _minePool.Count; i++)
            {
                _minePool[i].enabled = false;
            }
        }

        readonly List<SpriteRenderer> _missionPool = new List<SpriteRenderer>();
        static readonly Color MissionColor = new Color(1f, 0.45f, 0.1f, 0.9f);

        /// <summary>The viewing side's own called fire missions: a ring the size of the expected scatter.</summary>
        void SyncMissions()
        {
            int used = 0;
            foreach (var mission in _battle.Support.Missions)
            {
                if (_fogEnabled && mission.Side != _viewSide)
                {
                    continue;
                }

                if (used == _missionPool.Count)
                {
                    _missionPool.Add(NewOverlaySprite("Fire mission", MarkerSprites.Ring, 9));
                }

                var ring = _missionPool[used++];
                ring.enabled = true;
                ring.color = MissionColor;
                float size = mission.Asset.Spread * 2f / 0.94f;
                ring.transform.position = new Vector3(mission.Target.x, 23f, mission.Target.y);
                ring.transform.localScale = new Vector3(size, size, 1f);
            }

            for (int i = used; i < _missionPool.Count; i++)
            {
                _missionPool[i].enabled = false;
            }
        }

        /// <summary>For labels: the minefields the viewing side knows about.</summary>
        public IEnumerable<CloseFight.Core.Battle.MineField> KnownMineFields()
        {
            if (_battle == null)
            {
                yield break;
            }

            foreach (var field in _battle.Engineering.MineFields)
            {
                if (!_fogEnabled || field.KnownTo(_viewSide))
                {
                    yield return field;
                }
            }
        }

        void SyncAiPlan()
        {
            int lines = 0, rings = 0;
            if (ShowAiPlan)
            {
                foreach (var commander in _battle.Commanders)
                {
                    if (commander is CloseFight.Core.AI.AttackCommander attack)
                    {
                        DrawAttackPlan(attack, ref lines, ref rings);
                        continue;
                    }

                    if (!(commander is CloseFight.Core.AI.DefenseCommander defense))
                    {
                        continue;
                    }

                    foreach (var plan in defense.Plans)
                    {
                        if (plan.Team.ActiveCount == 0)
                        {
                            continue;
                        }

                        var from = plan.Team.Centroid;
                        var line = lines < _planLines.Count ? _planLines[lines] : AddPlanLine();
                        lines++;
                        line.enabled = true;
                        line.positionCount = 2;
                        line.SetPosition(0, new Vector3(from.x, 24f, from.y));
                        line.SetPosition(1, new Vector3(plan.Primary.x, 24f, plan.Primary.y));
                        SetLineColor(line, PlanPrimary);

                        PlaceRing(ref rings, plan.Primary, PlanPrimary, 3f);
                        if (plan.HasFallback)
                        {
                            PlaceRing(ref rings, plan.Fallback, PlanFallback, 2f);
                        }
                    }
                }
            }

            for (int i = lines; i < _planLines.Count; i++) _planLines[i].enabled = false;
            for (int i = rings; i < _planRings.Count; i++) _planRings[i].enabled = false;
        }

        static readonly Color PlanAssembly = new Color(1f, 0.6f, 0.1f, 0.9f);
        static readonly Color PlanJumpOff = new Color(1f, 0.2f, 0.2f, 0.9f);
        static readonly Color PlanGun = new Color(0.75f, 0.75f, 0.75f, 0.9f);

        /// <summary>
        /// Debug view of the attacking AI: orange = assembly point, cyan = machine-gun
        /// overwatch, red = jump-off, grey = assault gun position; lines show where
        /// each team is heading and the objective under attack.
        /// </summary>
        void DrawAttackPlan(CloseFight.Core.AI.AttackCommander attack, ref int lines, ref int rings)
        {
            if (!attack.HasGeometry || attack.Target == null)
            {
                return;
            }

            PlaceRing(ref rings, attack.Assembly, PlanAssembly, 4f);
            PlaceRing(ref rings, attack.Overwatch, PlanPrimary, 4f);
            PlaceRing(ref rings, attack.JumpOff, PlanJumpOff, 4f);
            PlaceRing(ref rings, attack.ArmorSpot, PlanGun, 4f);

            PlanLine(ref lines, attack.JumpOff, attack.Target.Position, PlanJumpOff);
            foreach (var plan in attack.Plans)
            {
                if (plan.Team.ActiveCount > 0 && plan.HasGoal)
                {
                    PlanLine(ref lines, plan.Team.Centroid, plan.Goal, PlanFallback);
                }
            }
        }

        void PlanLine(ref int lines, float2 from, float2 to, Color color)
        {
            var line = lines < _planLines.Count ? _planLines[lines] : AddPlanLine();
            lines++;
            line.enabled = true;
            line.positionCount = 2;
            line.SetPosition(0, new Vector3(from.x, 24f, from.y));
            line.SetPosition(1, new Vector3(to.x, 24f, to.y));
            SetLineColor(line, color);
        }

        LineRenderer AddPlanLine()
        {
            var line = CreateLine("AI plan");
            line.widthMultiplier = 0.25f;
            if (!Application.isPlaying)
            {
                line.gameObject.hideFlags = HideFlags.DontSave;
            }

            _planLines.Add(line);
            return line;
        }

        void PlaceRing(ref int used, float2 at, Color color, float size)
        {
            var ring = used < _planRings.Count ? _planRings[used] : AddPlanRing();
            used++;
            ring.enabled = true;
            ring.color = color;
            ring.transform.position = new Vector3(at.x, 24f, at.y);
            ring.transform.localScale = new Vector3(size, size, 1f);
        }

        SpriteRenderer AddPlanRing()
        {
            var ring = NewOverlaySprite("AI plan spot", MarkerSprites.Ring, 11);
            _planRings.Add(ring);
            return ring;
        }

        void SyncTracers()
        {
            float now = _battle.Clock.ElapsedSeconds;
            int used = 0;
            foreach (var shot in _battle.Combat.RecentShots)
            {
                float age = now - shot.Time;
                if (age > TracerLifetime)
                {
                    continue;
                }

                // Under fog, only draw fire the viewing side can see or that is aimed at it.
                var shooter = _battle.Soldiers[shot.ShooterId];
                if (_fogEnabled && !_battle.Visibility.CanSee(_viewSide, shooter))
                {
                    continue;
                }

                if (used == _tracerPool.Count)
                {
                    var line = CreateLine("Tracer");
                    line.widthMultiplier = 0.12f;
                    line.numCapVertices = 0;
                    if (!Application.isPlaying)
                    {
                        line.gameObject.hideFlags = HideFlags.DontSave;
                    }

                    _tracerPool.Add(line);
                }

                var tracer = _tracerPool[used++];
                tracer.widthMultiplier = shot.Weapon == WeaponClass.Cannon ? 0.35f : shot.Weapon == WeaponClass.Flamethrower ? 1.2f : 0.12f;
                var color = TracerColor(shot.Weapon);
                color.a *= 1f - age / TracerLifetime * 0.7f;
                SetLineColor(tracer, color);
                tracer.enabled = true;
                tracer.positionCount = 2;
                tracer.SetPosition(0, new Vector3(shot.From.x, DrawHeight(shot.From) + 1f, shot.From.y));
                tracer.SetPosition(1, new Vector3(shot.Impact.x, DrawHeight(shot.Impact) + 0.3f, shot.Impact.y));
            }

            for (int i = used; i < _tracerPool.Count; i++)
            {
                _tracerPool[i].enabled = false;
            }
        }

        static Color TracerColor(WeaponClass weapon)
        {
            switch (weapon)
            {
                case WeaponClass.Cannon: return new Color(1f, 1f, 0.85f, 1f);
                case WeaponClass.Flamethrower: return new Color(1f, 0.55f, 0.1f, 0.95f);
                case WeaponClass.SniperRifle: return new Color(1f, 1f, 0.75f, 0.9f);
                case WeaponClass.MachineGun: return new Color(1f, 0.45f, 0.15f, 0.9f);
                case WeaponClass.SubmachineGun: return new Color(1f, 0.85f, 0.35f, 0.8f);
                default: return new Color(1f, 0.95f, 0.7f, 0.85f);
            }
        }

        void SyncSmoke()
        {
            float now = _battle.Clock.ElapsedSeconds;
            int used = 0;
            foreach (var cloud in _battle.Smoke.Clouds)
            {
                if (used == _smokePool.Count)
                {
                    var go = new GameObject("Smoke");
                    go.transform.SetParent(_root.transform, false);
                    go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    if (!Application.isPlaying)
                    {
                        go.hideFlags = HideFlags.DontSave;
                    }

                    var sprite = go.AddComponent<SpriteRenderer>();
                    sprite.sprite = MarkerSprites.SoftDisc;
                    sprite.sortingOrder = 4;
                    _smokePool.Add(sprite);
                }

                var smoke = _smokePool[used++];
                float size = cloud.Radius(now) * 2.3f;
                smoke.enabled = true;
                smoke.transform.position = new Vector3(cloud.Center.x, 3f, cloud.Center.y);
                smoke.transform.localScale = new Vector3(size, size, 1f);
                var color = SmokeColor;
                color.a = 0.85f * cloud.Density(now);
                smoke.color = color;
            }

            for (int i = used; i < _smokePool.Count; i++)
            {
                _smokePool[i].enabled = false;
            }
        }

        /// <summary>Expanding, fading ripples where the viewing side heard unseen gunfire.</summary>
        void SyncSoundMarkers()
        {
            int used = 0;
            if (_fogEnabled)
            {
                float now = _battle.Clock.ElapsedSeconds;
                foreach (var marker in _battle.Visibility.SoundMarkers)
                {
                    if (marker.Listener != _viewSide)
                    {
                        continue;
                    }

                    if (used == _soundPool.Count)
                    {
                        _soundPool.Add(CreateSoundSprite());
                    }

                    var sprite = _soundPool[used++];
                    float age = math.saturate((now - marker.CreatedAt) / Visibility.SoundMarkerLifetime);
                    float size = math.lerp(4f, 9f, age);
                    sprite.enabled = true;
                    sprite.transform.position = new Vector3(marker.Position.x, 24f, marker.Position.y);
                    sprite.transform.localScale = new Vector3(size, size, 1f);
                    var color = SoundColor;
                    color.a = 1f - age * 0.85f;
                    sprite.color = color;
                }
            }

            for (int i = used; i < _soundPool.Count; i++)
            {
                _soundPool[i].enabled = false;
            }
        }

        SpriteRenderer CreateSoundSprite()
        {
            var go = new GameObject("Sound marker");
            go.transform.SetParent(_root.transform, false);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            if (!Application.isPlaying)
            {
                go.hideFlags = HideFlags.DontSave;
            }

            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = MarkerSprites.Ring;
            sprite.sortingOrder = 3;
            return sprite;
        }

        void SyncMarker(Marker marker, float alpha)
        {
            var soldier = marker.Soldier;
            // Crew inside a vehicle are drawn as the vehicle.
            if (soldier.IsMounted)
            {
                marker.Body.enabled = false;
                marker.Ring.enabled = false;
                return;
            }

            float2 p = math.lerp(soldier.PreviousPosition, soldier.Position, alpha);
            float heading = Mathf.LerpAngle(soldier.PreviousHeading, soldier.Heading, alpha);

            marker.Transform.position = new Vector3(p.x, DrawHeight(p), p.y);
            // Lie flat, then turn clockwise to the compass heading.
            marker.Transform.rotation = Quaternion.Euler(90f, heading, 0f);

            float size = soldierSize * (soldier.IsLeader ? 1.1f : 1f);
            float length = soldier.Posture == Posture.Prone ? 1.7f : soldier.Posture == Posture.Kneeling ? 1.15f : 1f;
            marker.Transform.localScale = new Vector3(size, size * length, 1f);

            var baseColor = soldier.Team.Side == Side.German ? GermanColor : SovietColor;
            switch (soldier.Status)
            {
                case SoldierStatus.Dead: marker.Body.color = DeadColor; break;
                case SoldierStatus.Incapacitated: marker.Body.color = DisabledColor; break;
                case SoldierStatus.Wounded: marker.Body.color = new Color(baseColor.r, baseColor.g * 0.72f, baseColor.b * 0.65f); break;
                default: marker.Body.color = baseColor; break;
            }

            bool shown = !_fogEnabled || _battle.Visibility.CanSee(_viewSide, soldier);
            if (shown && soldier.IsActive)
            {
                _seenEver.Add(soldier.Id);
            }
            else if (!soldier.IsActive && _seenEver.Contains(soldier.Id))
            {
                shown = true; // a body seen falling stays where it fell
            }

            marker.Body.enabled = shown;
            // Yellow ring: selected. Magenta ring: out of control (cowering, panicking, routing).
            bool selected = _selected.Contains(soldier.Team);
            bool wild = soldier.IsOutOfControl && (!_fogEnabled || soldier.Team.Side == _viewSide);
            marker.Ring.enabled = shown && soldier.IsActive && (selected || wild);
            marker.Ring.color = wild ? OutOfControlRing : SelectedRing;
        }

        void SyncLine(Team team, LineRenderer line)
        {
            bool show = _selected.Contains(team) && team.Activity == TeamActivity.Moving;
            if (!show)
            {
                line.enabled = false;
                return;
            }

            team.GetRemainingPath(_scratchPath);
            if (_scratchPath.Count < 2)
            {
                line.enabled = false;
                return;
            }

            _scratchLine.Clear();
            foreach (var point in _scratchPath)
            {
                _scratchLine.Add(new Vector3(point.x, DrawHeight(point) + 0.2f, point.y));
            }

            line.enabled = true;
            line.positionCount = _scratchLine.Count;
            line.SetPositions(_scratchLine.ToArray());
            var color = RouteColor(team.Order.Type);
            line.startColor = color;
            line.endColor = color;
        }

        static Color RouteColor(OrderType order)
        {
            switch (order)
            {
                case OrderType.MoveFast: return new Color(1f, 0.75f, 0.2f, 0.9f);
                case OrderType.Crawl: return new Color(0.4f, 0.8f, 1f, 0.9f);
                case OrderType.AssaultBuilding: return new Color(1f, 0.45f, 0.2f, 0.9f);
                case OrderType.Retreat: return new Color(0.95f, 0.35f, 0.9f, 0.9f);
                default: return new Color(0.35f, 0.9f, 0.35f, 0.9f);
            }
        }

        /// <summary>Ground level, or just above the roof when standing inside an intact building.</summary>
        float DrawHeight(float2 position)
        {
            var map = _battle.Map;
            var grid = map.Grid;
            var cell = grid.WorldToCell(position);
            if (grid.InBounds(cell))
            {
                int index = grid.IndexOf(cell);
                // Above the roof, even where it has collapsed: the roof mesh still stands under the rubble decal.
                var building = map.BuildingAt(index);
                if (building != null)
                {
                    return building.RoofHeight + 0.15f;
                }
            }

            return 0.08f;
        }

        /// <summary>The active soldier nearest to a ground point within <paramref name="radius"/> meters, optionally of one side.</summary>
        public Soldier PickSoldier(float2 point, float radius, Side side)
        {
            Soldier best = null;
            float bestDistance = radius;
            foreach (var marker in _markers)
            {
                var soldier = marker.Soldier;
                if (!soldier.IsActive || soldier.Team.Side != side)
                {
                    continue;
                }

                float d = math.distance(soldier.Position, point);
                if (d <= bestDistance)
                {
                    bestDistance = d;
                    best = soldier;
                }
            }

            return best;
        }

        /// <summary>World position where a soldier's marker is drawn (for screen-space box selection).</summary>
        public Vector3 DrawPosition(Soldier soldier)
        {
            return new Vector3(soldier.Position.x, DrawHeight(soldier.Position), soldier.Position.y);
        }

        GameObject _zones;

        /// <summary>Tints the player's deployment zones on the ground, with an outline.</summary>
        public void ShowDeployZones(IReadOnlyList<float2[]> zones, Color fill, Color outline)
        {
            HideDeployZones();
            if (_root == null || zones.Count == 0)
            {
                return;
            }

            _zones = new GameObject("Deployment zones");
            _zones.transform.SetParent(_root.transform, false);
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var indices = new List<int>();
            var triangles = new List<int>();
            foreach (var zone in zones)
            {
                triangles.Clear();
                if (!CloseFight.Core.Geometry.PolygonTriangulator.Triangulate(zone, triangles))
                {
                    continue;
                }

                int start = vertices.Count;
                foreach (var p in zone)
                {
                    vertices.Add(new Vector3(p.x, 0.2f, p.y));
                    colors.Add(fill);
                }

                // Map triangles are counter-clockwise seen from above (x east, y north); Unity wants clockwise from the camera.
                for (int i = 0; i < triangles.Count; i += 3)
                {
                    indices.Add(start + triangles[i]);
                    indices.Add(start + triangles[i + 2]);
                    indices.Add(start + triangles[i + 1]);
                }

                var line = CreateLine("Zone outline");
                line.transform.SetParent(_zones.transform, false);
                line.enabled = true;
                line.loop = true;
                line.widthMultiplier = 0.8f;
                line.positionCount = zone.Length;
                for (int i = 0; i < zone.Length; i++)
                {
                    line.SetPosition(i, new Vector3(zone[i].x, 0.3f, zone[i].y));
                }

                SetLineColor(line, outline);
            }

            var mesh = new Mesh { name = "Deployment zone" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0);
            var fillObject = new GameObject("Zone fill");
            fillObject.transform.SetParent(_zones.transform, false);
            fillObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = fillObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = lineMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 1;
        }

        public void HideDeployZones()
        {
            SafeDestroy(_zones);
            _zones = null;
        }

        public void ClearView()
        {
            _zones = null;
            _equipment.Clear();
            _flags.Clear();
            _minePool.Clear();
            _missionPool.Clear();
            _planLines.Clear();
            _planRings.Clear();
            _tracerPool.Clear();
            _smokePool.Clear();
            _seenEver.Clear();
            _soundPool.Clear();
            _sightClear = null;
            _sightBlocked = null;
            _markers.Clear();
            _lines.Clear();
            _selected.Clear();
            _battle = null;
            SafeDestroy(_root);
            _root = null;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (child.name == RootName)
                {
                    SafeDestroy(child);
                }
            }
        }

        void OnDestroy()
        {
            ClearView();
        }

        static void SafeDestroy(Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }
    }
}
