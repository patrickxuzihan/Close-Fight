using System.Collections.Generic;
using CloseFight.Core.Geometry;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloseFight.Game.View
{
    /// <summary>
    /// Builds flat-shaded prisms (a footprint polygon extruded upward): building
    /// blocks and wall segments. Map (x, y) maps to world (x, z); Y is up.
    /// </summary>
    public sealed class PrismMeshBuilder
    {
        readonly List<Vector3> _vertices = new List<Vector3>();
        readonly List<Vector3> _normals = new List<Vector3>();
        readonly List<int> _triangles = new List<int>();
        readonly List<int> _scratchTriangles = new List<int>();
        readonly List<float2> _scratchPolygon = new List<float2>();

        public bool IsEmpty => _vertices.Count == 0;

        public void Clear()
        {
            _vertices.Clear();
            _normals.Clear();
            _triangles.Clear();
        }

        /// <summary>Adds a prism with a roof cap and side walls (no floor).</summary>
        public void AddPrism(IReadOnlyList<float2> footprint, float baseHeight, float topHeight)
        {
            if (footprint.Count < 3 || topHeight <= baseHeight)
            {
                return;
            }

            // Work on a counter-clockwise copy.
            _scratchPolygon.Clear();
            bool ccw = PolygonTriangulator.SignedArea(footprint) > 0f;
            for (int i = 0; i < footprint.Count; i++)
            {
                _scratchPolygon.Add(footprint[ccw ? i : footprint.Count - 1 - i]);
            }

            AddRoof(_scratchPolygon, topHeight);
            AddSides(_scratchPolygon, baseHeight, topHeight);
        }

        void AddRoof(List<float2> polygon, float height)
        {
            if (!PolygonTriangulator.Triangulate(polygon, _scratchTriangles))
            {
                return;
            }

            int start = _vertices.Count;
            for (int i = 0; i < polygon.Count; i++)
            {
                _vertices.Add(new Vector3(polygon[i].x, height, polygon[i].y));
                _normals.Add(Vector3.up);
            }

            // Map-space CCW triangles are clockwise seen from above once x,y become x,z,
            // which is Unity's front face winding when we flip the order.
            for (int i = 0; i < _scratchTriangles.Count; i += 3)
            {
                _triangles.Add(start + _scratchTriangles[i]);
                _triangles.Add(start + _scratchTriangles[i + 2]);
                _triangles.Add(start + _scratchTriangles[i + 1]);
            }
        }

        void AddSides(List<float2> polygon, float bottom, float top)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                float2 a = polygon[i];
                float2 b = polygon[(i + 1) % polygon.Count];
                float2 d = b - a;
                if (math.lengthsq(d) < 1e-8f)
                {
                    continue;
                }

                // Outward normal of a CCW polygon edge points to the right of the edge.
                var normal = new Vector3(d.y, 0f, -d.x).normalized;

                int start = _vertices.Count;
                _vertices.Add(new Vector3(a.x, bottom, a.y));
                _vertices.Add(new Vector3(b.x, bottom, b.y));
                _vertices.Add(new Vector3(b.x, top, b.y));
                _vertices.Add(new Vector3(a.x, top, a.y));
                for (int k = 0; k < 4; k++)
                {
                    _normals.Add(normal);
                }

                _triangles.Add(start + 0);
                _triangles.Add(start + 3);
                _triangles.Add(start + 2);
                _triangles.Add(start + 0);
                _triangles.Add(start + 2);
                _triangles.Add(start + 1);
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (_vertices.Count > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
