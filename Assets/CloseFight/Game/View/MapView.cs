using System.Collections.Generic;
using CloseFight.Core.Map;
using Unity.Mathematics;
using UnityEngine;

namespace CloseFight.Game.View
{
    /// <summary>
    /// Draws a <see cref="TacticalMap"/> with placeholder art (M1):
    /// a ground texture with one pixel per grid cell, extruded building blocks
    /// and wall segments. Everything is generated from map data; nothing is
    /// modeled by hand. Building shadows from the sun give the aerial depth.
    /// </summary>
    public sealed class MapView : MonoBehaviour
    {
        const float WallThickness = 0.4f;
        const float FenceThickness = 0.15f;

        [SerializeField] Material groundMaterial;
        [SerializeField] Material buildingMaterial;
        [SerializeField] Material wallMaterial;
        [Tooltip("Transparent unlit material (Sprites/Default) for the rubble decal over collapsed buildings and crushed fences.")]
        [SerializeField] Material overlayMaterial;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        TacticalMap _map;
        Texture2D _groundTexture;
        Texture2D _overlayTexture;
        Material _overlayInstance;
        bool[] _hadWall;
        static readonly Color32 RubbleDecal = new Color32(128, 112, 96, 255);
        Material _groundInstance;
        GameObject _root;
        readonly List<Mesh> _meshes = new List<Mesh>();

        public TacticalMap Map => _map;

        const string RootName = "MapGeometry";

        public void Build(TacticalMap map)
        {
            ClearView();
            _map = map;

            _root = new GameObject(RootName);
            _root.transform.SetParent(transform, false);

            BuildGround();
            BuildBuildings();
            BuildWalls();
            BuildProps();
            BuildOverlay();

            if (!Application.isPlaying)
            {
                MarkAsEditorPreview();
            }

            _map.CellChanged += OnCellChanged;
        }

        /// <summary>
        /// Edit-mode preview objects must never be written into the scene file:
        /// they are regenerated from the map data every time.
        /// </summary>
        void MarkAsEditorPreview()
        {
            foreach (var t in _root.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.hideFlags = HideFlags.DontSave;
            }

            foreach (var mesh in _meshes)
            {
                mesh.hideFlags = HideFlags.DontSave;
            }

            if (_groundTexture != null)
            {
                _groundTexture.hideFlags = HideFlags.DontSave;
            }

            if (_groundInstance != null)
            {
                _groundInstance.hideFlags = HideFlags.DontSave;
            }

            if (_overlayTexture != null)
            {
                _overlayTexture.hideFlags = HideFlags.DontSave;
                _overlayInstance.hideFlags = HideFlags.DontSave;
            }
        }

        void BuildGround()
        {
            var grid = _map.Grid;
            _groundTexture = new Texture2D(grid.Width, grid.Height, TextureFormat.RGBA32, false)
            {
                name = "GroundTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[grid.CellCount];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = GroundColor(i);
            }

            _groundTexture.SetPixels32(pixels);
            _groundTexture.Apply(false);

            float width = grid.Width * grid.CellSize;
            float height = grid.Height * grid.CellSize;
            var mesh = new Mesh { name = "Ground" };
            mesh.SetVertices(new List<Vector3>
            {
                new Vector3(0f, 0f, 0f), new Vector3(width, 0f, 0f),
                new Vector3(width, 0f, height), new Vector3(0f, 0f, height),
            });
            mesh.SetNormals(new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.RecalculateBounds();
            _meshes.Add(mesh);

            _groundInstance = new Material(groundMaterial) { name = "Ground (instance)" };
            _groundInstance.SetTexture(BaseMapId, _groundTexture);
            _groundInstance.SetColor(BaseColorId, Color.white);

            CreateRendererObject("Ground", mesh, _groundInstance, castShadows: false);
        }

        Color32 GroundColor(int index)
        {
            var grid = _map.Grid;
            var terrain = grid.GetTerrain(index);
            var coord = grid.CoordOf(index);

            Color color = TerrainPalette.Ground(terrain);
            float noise = (TerrainPalette.Hash01(coord.X, coord.Y) - 0.5f) * 2f * TerrainPalette.Noise(terrain);

            // Rail: darker ties every 0.6 m read as track at close zoom.
            if (terrain == TerrainType.Rail && coord.X % 2 == 0)
            {
                noise -= 0.05f;
            }

            color.r = Mathf.Clamp01(color.r + noise);
            color.g = Mathf.Clamp01(color.g + noise);
            color.b = Mathf.Clamp01(color.b + noise);
            return color;
        }

        /// <summary>
        /// Collapse decal (D38): a transparent texture, one pixel per cell, floating
        /// above every roof. Collapsed building cells and crushed fences are painted
        /// into it, so the 3D blocks never have to be rebuilt.
        /// </summary>
        void BuildOverlay()
        {
            var grid = _map.Grid;
            _hadWall = new bool[grid.CellCount];
            for (int i = 0; i < grid.CellCount; i++)
            {
                _hadWall[i] = grid.HasFlag(i, CellFlags.Wall);
            }

            if (overlayMaterial == null)
            {
                return;
            }

            _overlayTexture = new Texture2D(grid.Width, grid.Height, TextureFormat.RGBA32, false)
            {
                name = "CollapseOverlay",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            _overlayTexture.SetPixels32(new Color32[grid.CellCount]);
            for (int i = 0; i < grid.CellCount; i++)
            {
                if (grid.HasFlag(i, CellFlags.Collapsed))
                {
                    _overlayTexture.SetPixel(i % grid.Width, i / grid.Width, RubbleDecalAt(i));
                }
            }

            _overlayTexture.Apply(false);

            float maxRoof = 1f;
            foreach (var building in _map.Buildings)
            {
                maxRoof = Mathf.Max(maxRoof, building.RoofHeight);
            }

            float width = grid.Width * grid.CellSize;
            float height = grid.Height * grid.CellSize;
            float y = maxRoof + 0.5f;
            var mesh = new Mesh { name = "CollapseOverlay" };
            mesh.SetVertices(new List<Vector3> { new Vector3(0f, y, 0f), new Vector3(width, y, 0f), new Vector3(width, y, height), new Vector3(0f, y, height) });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.RecalculateBounds();
            _meshes.Add(mesh);

            _overlayInstance = new Material(overlayMaterial) { name = "CollapseOverlay (instance)", mainTexture = _overlayTexture };
            var renderer = CreateRendererObject("CollapseOverlay", mesh, _overlayInstance, castShadows: false);
            renderer.receiveShadows = false;
            renderer.sortingOrder = -1; // under every unit sprite
        }

        /// <summary>Broken brick and plaster: the rubble color with strong per-cell variation.</summary>
        Color32 RubbleDecalAt(int index)
        {
            var coord = _map.Grid.CoordOf(index);
            float n = TerrainPalette.Hash01(coord.X * 3 + 11, coord.Y * 5 + 7) - 0.5f;
            Color c = RubbleDecal;
            c.r = Mathf.Clamp01(c.r + n * 0.35f);
            c.g = Mathf.Clamp01(c.g + n * 0.3f);
            c.b = Mathf.Clamp01(c.b + n * 0.25f);
            c.a = 1f;
            return c;
        }

        void BuildBuildings()
        {
            var builder = new PrismMeshBuilder();
            var footprint = new List<float2>();
            var block = new MaterialPropertyBlock();

            foreach (var building in _map.Buildings)
            {
                footprint.Clear();
                foreach (var point in building.Definition.polygon)
                {
                    footprint.Add(point);
                }

                builder.Clear();
                builder.AddPrism(footprint, 0f, building.RoofHeight);
                if (builder.IsEmpty)
                {
                    continue;
                }

                var mesh = builder.ToMesh($"Building {building.Definition.id}");
                _meshes.Add(mesh);
                var renderer = CreateRendererObject(mesh.name, mesh, buildingMaterial, castShadows: true);

                // Slight per-building tint so blocks of the same material read apart.
                Color tint = TerrainPalette.Building(building.Material);
                float shade = 0.9f + 0.2f * TerrainPalette.Hash01(building.Definition.id, 7);
                block.SetColor(BaseColorId, tint * shade);
                renderer.SetPropertyBlock(block);
            }
        }

        void BuildWalls()
        {
            var solid = new PrismMeshBuilder();
            var fences = new PrismMeshBuilder();
            var quad = new List<float2>(4);

            foreach (var wall in _map.Definition.walls)
            {
                if (wall.points == null || wall.points.Count < 2)
                {
                    continue;
                }

                var target = wall.crushable ? fences : solid;
                float half = (wall.crushable ? FenceThickness : WallThickness) * 0.5f;

                for (int i = 0; i < wall.points.Count - 1; i++)
                {
                    float2 a = wall.points[i];
                    float2 b = wall.points[i + 1];
                    float2 dir = math.normalizesafe(b - a);
                    if (math.lengthsq(dir) < 1e-8f)
                    {
                        continue;
                    }

                    // Extend each segment by half the thickness so corners close.
                    a -= dir * half;
                    b += dir * half;
                    float2 side = new float2(-dir.y, dir.x) * half;

                    quad.Clear();
                    quad.Add(a - side);
                    quad.Add(b - side);
                    quad.Add(b + side);
                    quad.Add(a + side);
                    target.AddPrism(quad, 0f, wall.height);
                }
            }

            AddWallMesh(solid, "Walls", TerrainPalette.Wall);
            AddWallMesh(fences, "Fences", TerrainPalette.Fence);
        }

        void BuildProps()
        {
            var byKind = new Dictionary<string, PrismMeshBuilder>();
            var footprint = new List<float2>();
            foreach (var prop in _map.Definition.props)
            {
                if (prop.polygon == null || prop.polygon.Count < 3)
                {
                    continue;
                }

                string kind = string.IsNullOrEmpty(prop.kind) ? "prop" : prop.kind;
                if (!byKind.TryGetValue(kind, out var builder))
                {
                    builder = new PrismMeshBuilder();
                    byKind[kind] = builder;
                }

                footprint.Clear();
                foreach (var point in prop.polygon)
                {
                    footprint.Add(point);
                }

                builder.AddPrism(footprint, 0f, prop.height);
            }

            foreach (var pair in byKind)
            {
                AddWallMesh(pair.Value, "Props " + pair.Key, TerrainPalette.Prop(pair.Key));
            }
        }

        void AddWallMesh(PrismMeshBuilder builder, string name, Color color)
        {
            if (builder.IsEmpty)
            {
                return;
            }

            var mesh = builder.ToMesh(name);
            _meshes.Add(mesh);
            var renderer = CreateRendererObject(name, mesh, wallMaterial, castShadows: true);
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, color);
            renderer.SetPropertyBlock(block);
        }

        MeshRenderer CreateRendererObject(string name, Mesh mesh, Material material, bool castShadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return renderer;
        }

        void OnCellChanged(int index)
        {
            if (_groundTexture == null)
            {
                return;
            }

            var coord = _map.Grid.CoordOf(index);
            _groundTexture.SetPixel(coord.X, coord.Y, GroundColor(index));
            _groundTexture.Apply(false);

            if (_overlayTexture != null)
            {
                var grid = _map.Grid;
                Color32 decal = default;
                if (grid.HasFlag(index, CellFlags.Collapsed))
                {
                    decal = RubbleDecalAt(index);
                }
                else if (_hadWall[index] && !grid.HasFlag(index, CellFlags.Wall))
                {
                    decal = GroundColor(index); // fence crushed flat
                }

                _overlayTexture.SetPixel(coord.X, coord.Y, decal);
                _overlayTexture.Apply(false);
            }
        }

        /// <summary>Destroys all generated geometry.</summary>
        public void ClearView()
        {
            if (_map != null)
            {
                _map.CellChanged -= OnCellChanged;
                _map = null;
            }

            SafeDestroy(_root);

            // Leftovers from before a script reload (the field reference is lost then).
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (child.name == RootName)
                {
                    SafeDestroy(child);
                }
            }

            SafeDestroy(_groundTexture);
            SafeDestroy(_groundInstance);
            SafeDestroy(_overlayTexture);
            SafeDestroy(_overlayInstance);
            _overlayTexture = null;
            _overlayInstance = null;
            foreach (var mesh in _meshes)
            {
                SafeDestroy(mesh);
            }

            _meshes.Clear();
            _root = null;
            _groundTexture = null;
            _groundInstance = null;
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
