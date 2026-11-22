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
        static readonly Color DeadColor = new Color(0.25f, 0.22f, 0.20f);

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
            float2 p = math.lerp(soldier.PreviousPosition, soldier.Position, alpha);
            float heading = Mathf.LerpAngle(soldier.PreviousHeading, soldier.Heading, alpha);

            marker.Transform.position = new Vector3(p.x, DrawHeight(p), p.y);
            // Lie flat, then turn clockwise to the compass heading.
            marker.Transform.rotation = Quaternion.Euler(90f, heading, 0f);

            float size = soldierSize * (soldier.IsLeader ? 1.1f : 1f);
            float length = soldier.Posture == Posture.Prone ? 1.7f : soldier.Posture == Posture.Kneeling ? 1.15f : 1f;
            marker.Transform.localScale = new Vector3(size, size * length, 1f);

            if (!soldier.IsActive)
            {
                marker.Body.color = DeadColor;
            }

            bool shown = !_fogEnabled || _battle.Visibility.CanSee(_viewSide, soldier);
            marker.Body.enabled = shown;
            marker.Ring.enabled = shown && _selected.Contains(soldier.Team) && soldier.IsActive;
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
                var building = map.BuildingAt(index);
                if (building != null && !grid.HasFlag(index, CellFlags.Collapsed))
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

        public void ClearView()
        {
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
