using System.Collections.Generic;
using CloseFight.Core.Geometry;
using NUnit.Framework;
using Unity.Mathematics;

namespace CloseFight.Tests
{
    public class PolygonTriangulatorTests
    {
        static float TriangulatedArea(IReadOnlyList<float2> polygon, List<int> triangles)
        {
            float total = 0f;
            for (int i = 0; i < triangles.Count; i += 3)
            {
                var tri = new List<float2> { polygon[triangles[i]], polygon[triangles[i + 1]], polygon[triangles[i + 2]] };
                float area = PolygonTriangulator.SignedArea(tri);
                Assert.Greater(area, 0f, "every output triangle is counter-clockwise");
                total += area;
            }

            return total;
        }

        [Test]
        public void SquareMakesTwoTriangles()
        {
            var square = new List<float2> { new float2(0, 0), new float2(10, 0), new float2(10, 10), new float2(0, 10) };
            var triangles = new List<int>();

            Assert.IsTrue(PolygonTriangulator.Triangulate(square, triangles));
            Assert.AreEqual(6, triangles.Count);
            Assert.AreEqual(100f, TriangulatedArea(square, triangles), 1e-3f);
        }

        [Test]
        public void ClockwiseLShapeKeepsItsArea()
        {
            // L-shaped building given clockwise; area = 30*12 + 13*20 = 620.
            var shape = new List<float2>
            {
                new float2(35, 40), new float2(48, 40), new float2(48, 20),
                new float2(65, 20), new float2(65, 8), new float2(35, 8),
            };
            var triangles = new List<int>();

            Assert.IsTrue(PolygonTriangulator.Triangulate(shape, triangles));
            Assert.AreEqual(12, triangles.Count, "6 vertices -> 4 triangles");
            Assert.AreEqual(620f, TriangulatedArea(shape, triangles), 1e-2f);
        }

        [Test]
        public void CollinearVertexIsHandled()
        {
            var shape = new List<float2> { new float2(0, 0), new float2(5, 0), new float2(10, 0), new float2(10, 10), new float2(0, 10) };
            var triangles = new List<int>();

            Assert.IsTrue(PolygonTriangulator.Triangulate(shape, triangles));
            Assert.AreEqual(100f, TriangulatedArea(shape, triangles), 1e-3f);
        }
    }
}
