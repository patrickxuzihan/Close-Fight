using System.Collections.Generic;
using System.IO;
using CloseFight.Core.Map;
using CloseFight.Game.Battle;
using UnityEditor;
using UnityEngine;

namespace CloseFight.EditorTools
{
    /// <summary>
    /// Map editor (D19): edit a map JSON in the Scene view. Drag vertices of
    /// buildings, terrain areas, props and walls; change floors, material,
    /// height and terrain in this window; add, duplicate and delete elements;
    /// save back to the JSON and rebuild the battlefield preview.
    /// Menu: Close Fight > Map Editor. Map Y (north) is the world Z axis.
    /// </summary>
    public sealed class MapEditorWindow : EditorWindow
    {
        enum Kind { Building, Area, Prop, Wall }

        TextAsset _mapAsset;
        MapDefinition _map;
        Kind _kind = Kind.Building;
        int _selected = -1;
        float _snap = 0.5f;
        Vector2 _scroll;
        bool _dirty;

        [MenuItem("Close Fight/Map Editor")]
        static void Open()
        {
            GetWindow<MapEditorWindow>("Map Editor");
        }

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            if (_mapAsset == null)
            {
                var bootstrap = FindAnyObjectByType<BattleBootstrap>();
                if (bootstrap != null)
                {
                    _mapAsset = new SerializedObject(bootstrap).FindProperty("mapJson").objectReferenceValue as TextAsset;
                }
            }

            Load();
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void Load()
        {
            _map = _mapAsset != null ? MapSerializer.FromJson(_mapAsset.text) : null;
            _selected = -1;
            _dirty = false;
        }

        // ------------------------------------------------------------ window

        void OnGUI()
        {
            var asset = (TextAsset)EditorGUILayout.ObjectField("Map JSON", _mapAsset, typeof(TextAsset), false);
            if (asset != _mapAsset)
            {
                _mapAsset = asset;
                Load();
            }

            if (_map == null)
            {
                EditorGUILayout.HelpBox("Pick a map JSON (Assets/CloseFight/Data/Maps).", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"{_map.id}  {_map.width} x {_map.height} m", EditorStyles.boldLabel);
            _snap = EditorGUILayout.Slider("Snap (m)", _snap, 0.1f, 5f);
            var kind = (Kind)GUILayout.Toolbar((int)_kind, new[] { "Buildings", "Areas", "Props", "Walls" });
            if (kind != _kind)
            {
                _kind = kind;
                _selected = -1;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add")) AddElement();
                GUI.enabled = _selected >= 0;
                if (GUILayout.Button("Duplicate")) DuplicateElement();
                if (GUILayout.Button("Delete")) DeleteElement();
                GUI.enabled = true;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(180));
            int count = Count();
            for (int i = 0; i < count; i++)
            {
                bool on = GUILayout.Toggle(i == _selected, Label(i), "Button");
                if (on && i != _selected)
                {
                    _selected = i;
                    FrameSelected();
                }
            }

            EditorGUILayout.EndScrollView();

            if (_selected >= 0 && _selected < count)
            {
                DrawSelectedFields();
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = _dirty;
                if (GUILayout.Button("Save JSON")) Save();
                GUI.enabled = true;
                if (GUILayout.Button("Rebuild preview")) RebuildPreview();
                if (GUILayout.Button("Revert")) Load();
            }

            EditorGUILayout.HelpBox("Scene view: drag the white dots to move corners. Shift+click on an edge inserts a corner; Ctrl/Cmd+click a corner deletes it.", MessageType.None);
        }

        int Count()
        {
            switch (_kind)
            {
                case Kind.Building: return _map.buildings.Count;
                case Kind.Area: return _map.areas.Count;
                case Kind.Prop: return _map.props.Count;
                default: return _map.walls.Count;
            }
        }

        string Label(int i)
        {
            switch (_kind)
            {
                case Kind.Building:
                    var b = _map.buildings[i];
                    return $"#{b.id} {b.material} {b.floors}F {b.nameKey}";
                case Kind.Area: return $"{i}: {_map.areas[i].terrain}";
                case Kind.Prop: return $"{i}: {_map.props[i].kind} {_map.props[i].height:0.#} m";
                default: return $"{i}: wall {_map.walls[i].height:0.#} m{(_map.walls[i].crushable ? " (fence)" : "")}";
            }
        }

        List<MapPoint> Points(int i)
        {
            switch (_kind)
            {
                case Kind.Building: return _map.buildings[i].polygon;
                case Kind.Area: return _map.areas[i].polygon;
                case Kind.Prop: return _map.props[i].polygon;
                default: return _map.walls[i].points;
            }
        }

        void DrawSelectedFields()
        {
            EditorGUI.BeginChangeCheck();
            switch (_kind)
            {
                case Kind.Building:
                    var b = _map.buildings[_selected];
                    b.nameKey = EditorGUILayout.TextField("Name key", b.nameKey);
                    b.material = (BuildingMaterial)EditorGUILayout.EnumPopup("Material", b.material);
                    b.floors = EditorGUILayout.IntSlider("Floors", b.floors, 1, 8);
                    break;
                case Kind.Area:
                    _map.areas[_selected].terrain = (TerrainType)EditorGUILayout.EnumPopup("Terrain", _map.areas[_selected].terrain);
                    break;
                case Kind.Prop:
                    var p = _map.props[_selected];
                    p.kind = EditorGUILayout.TextField("Kind (wagon/fountain/barricade)", p.kind);
                    p.height = EditorGUILayout.Slider("Height (m)", p.height, 0.3f, 8f);
                    p.cover = EditorGUILayout.Slider("Cover", p.cover, 0f, 100f);
                    break;
                default:
                    var w = _map.walls[_selected];
                    w.height = EditorGUILayout.Slider("Height (m)", w.height, 0.3f, 6f);
                    w.blocksMovement = EditorGUILayout.Toggle("Blocks movement", w.blocksMovement);
                    w.crushable = EditorGUILayout.Toggle("Crushable (fence)", w.crushable);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                MarkDirty();
            }
        }

        // ------------------------------------------------------------ editing

        void AddElement()
        {
            var view = SceneView.lastActiveSceneView;
            Vector3 center = view != null ? view.pivot : new Vector3(_map.width * 0.5f, 0f, _map.height * 0.5f);
            float x = Mathf.Clamp(Snap(center.x), 10f, _map.width - 10f);
            float y = Mathf.Clamp(Snap(center.z), 10f, _map.height - 10f);
            var square = new List<MapPoint> { new MapPoint(x - 5, y - 4), new MapPoint(x + 5, y - 4), new MapPoint(x + 5, y + 4), new MapPoint(x - 5, y + 4) };

            switch (_kind)
            {
                case Kind.Building:
                    int nextId = 1;
                    foreach (var b in _map.buildings) nextId = Mathf.Max(nextId, b.id + 1);
                    _map.buildings.Add(new BuildingDefinition { id = nextId, floors = 2, material = BuildingMaterial.Brick, polygon = square });
                    break;
                case Kind.Area:
                    _map.areas.Add(new AreaDefinition { terrain = TerrainType.Rubble, polygon = square });
                    break;
                case Kind.Prop:
                    _map.props.Add(new PropDefinition { kind = "wagon", height = 3.2f, cover = 75f, polygon = new List<MapPoint> { new MapPoint(x - 1.5f, y - 5.5f), new MapPoint(x + 1.5f, y - 5.5f), new MapPoint(x + 1.5f, y + 5.5f), new MapPoint(x - 1.5f, y + 5.5f) } });
                    break;
                default:
                    _map.walls.Add(new WallDefinition { height = 2.2f, points = new List<MapPoint> { new MapPoint(x - 6, y), new MapPoint(x + 6, y) } });
                    break;
            }

            _selected = Count() - 1;
            MarkDirty();
        }

        void DuplicateElement()
        {
            string json = MapSerializer.ToJson(new MapDefinition
            {
                buildings = _kind == Kind.Building ? new List<BuildingDefinition> { _map.buildings[_selected] } : new List<BuildingDefinition>(),
                areas = _kind == Kind.Area ? new List<AreaDefinition> { _map.areas[_selected] } : new List<AreaDefinition>(),
                props = _kind == Kind.Prop ? new List<PropDefinition> { _map.props[_selected] } : new List<PropDefinition>(),
                walls = _kind == Kind.Wall ? new List<WallDefinition> { _map.walls[_selected] } : new List<WallDefinition>(),
            });
            var copy = MapSerializer.FromJson(json);

            void Shift(List<MapPoint> points)
            {
                for (int i = 0; i < points.Count; i++) points[i] = new MapPoint(points[i].x + 6f, points[i].y + 6f);
            }

            switch (_kind)
            {
                case Kind.Building:
                    var b = copy.buildings[0];
                    int nextId = 1;
                    foreach (var existing in _map.buildings) nextId = Mathf.Max(nextId, existing.id + 1);
                    b.id = nextId;
                    Shift(b.polygon);
                    _map.buildings.Add(b);
                    break;
                case Kind.Area: Shift(copy.areas[0].polygon); _map.areas.Add(copy.areas[0]); break;
                case Kind.Prop: Shift(copy.props[0].polygon); _map.props.Add(copy.props[0]); break;
                default: Shift(copy.walls[0].points); _map.walls.Add(copy.walls[0]); break;
            }

            _selected = Count() - 1;
            MarkDirty();
        }

        void DeleteElement()
        {
            switch (_kind)
            {
                case Kind.Building: _map.buildings.RemoveAt(_selected); break;
                case Kind.Area: _map.areas.RemoveAt(_selected); break;
                case Kind.Prop: _map.props.RemoveAt(_selected); break;
                default: _map.walls.RemoveAt(_selected); break;
            }

            _selected = -1;
            MarkDirty();
        }

        float Snap(float value) => Mathf.Round(value / _snap) * _snap;

        void MarkDirty()
        {
            _dirty = true;
            Repaint();
            SceneView.RepaintAll();
        }

        void Save()
        {
            string path = AssetDatabase.GetAssetPath(_mapAsset);
            File.WriteAllText(path, MapSerializer.ToJson(_map));
            AssetDatabase.ImportAsset(path);
            _mapAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            _dirty = false;
            RebuildPreview();
        }

        void RebuildPreview()
        {
            var bootstrap = FindAnyObjectByType<BattleBootstrap>();
            if (bootstrap != null && !Application.isPlaying)
            {
                bootstrap.Build();
            }
        }

        void FrameSelected()
        {
            var points = Points(_selected);
            if (points.Count == 0 || SceneView.lastActiveSceneView == null)
            {
                return;
            }

            var bounds = new Bounds(new Vector3(points[0].x, 0f, points[0].y), Vector3.one);
            foreach (var p in points) bounds.Encapsulate(new Vector3(p.x, 0f, p.y));
            bounds.Expand(10f);
            SceneView.lastActiveSceneView.Frame(bounds, false);
        }

        // ------------------------------------------------------------ scene view

        void OnSceneGUI(SceneView view)
        {
            if (_map == null)
            {
                return;
            }

            const float drawHeight = 20f; // above every roof
            int count = Count();
            for (int i = 0; i < count; i++)
            {
                var points = Points(i);
                if (points.Count < 2)
                {
                    continue;
                }

                var line = new Vector3[_kind == Kind.Wall ? points.Count : points.Count + 1];
                for (int k = 0; k < points.Count; k++) line[k] = new Vector3(points[k].x, drawHeight, points[k].y);
                if (_kind != Kind.Wall) line[points.Count] = line[0];

                Handles.color = i == _selected ? Color.yellow : new Color(0.3f, 0.9f, 1f, 0.6f);
                Handles.DrawAAPolyLine(i == _selected ? 4f : 2f, line);

                // Click near the outline's first corner to select it.
                if (i != _selected && Handles.Button(line[0], Quaternion.identity, 0.8f, 1.2f, Handles.DotHandleCap))
                {
                    _selected = i;
                    Repaint();
                }
            }

            if (_selected < 0 || _selected >= count)
            {
                return;
            }

            var selected = Points(_selected);
            var e = Event.current;
            for (int k = 0; k < selected.Count; k++)
            {
                var world = new Vector3(selected[k].x, drawHeight, selected[k].y);
                float size = HandleUtility.GetHandleSize(world) * 0.08f;

                if (e.type == EventType.MouseDown && e.button == 0 && (e.control || e.command)
                    && HandleUtility.DistanceToCircle(world, size) < 5f && selected.Count > (_kind == Kind.Wall ? 2 : 3))
                {
                    Undo.RecordObject(this, "Delete corner");
                    selected.RemoveAt(k);
                    MarkDirty();
                    e.Use();
                    return;
                }

                Handles.color = Color.white;
                EditorGUI.BeginChangeCheck();
                var moved = Handles.FreeMoveHandle(world, size, Vector3.zero, Handles.DotHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    // Project the drag onto the ground plane at the draw height.
                    var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                    var plane = new Plane(Vector3.up, new Vector3(0f, drawHeight, 0f));
                    if (plane.Raycast(ray, out float t))
                    {
                        moved = ray.GetPoint(t);
                    }

                    selected[k] = new MapPoint(Mathf.Clamp(Snap(moved.x), 0f, _map.width), Mathf.Clamp(Snap(moved.z), 0f, _map.height));
                    MarkDirty();
                }
            }

            // Shift+click on an edge inserts a corner there.
            if (e.type == EventType.MouseDown && e.button == 0 && e.shift)
            {
                int edges = _kind == Kind.Wall ? selected.Count - 1 : selected.Count;
                for (int k = 0; k < edges; k++)
                {
                    var a = new Vector3(selected[k].x, drawHeight, selected[k].y);
                    var b = new Vector3(selected[(k + 1) % selected.Count].x, drawHeight, selected[(k + 1) % selected.Count].y);
                    if (HandleUtility.DistanceToLine(a, b) < 6f)
                    {
                        var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                        var plane = new Plane(Vector3.up, new Vector3(0f, drawHeight, 0f));
                        if (plane.Raycast(ray, out float t))
                        {
                            var p = ray.GetPoint(t);
                            selected.Insert(k + 1, new MapPoint(Snap(p.x), Snap(p.z)));
                            MarkDirty();
                            e.Use();
                        }

                        break;
                    }
                }
            }
        }
    }
}
