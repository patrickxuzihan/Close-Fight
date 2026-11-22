using System.Collections.Generic;
using CloseFight.Core.Map;

namespace CloseFight.Tests
{
    /// <summary>Small hand-built maps shared by the tests.</summary>
    static class TestMaps
    {
        public static List<MapPoint> Rect(float x0, float y0, float x1, float y1)
        {
            return new List<MapPoint>
            {
                new MapPoint(x0, y0),
                new MapPoint(x1, y0),
                new MapPoint(x1, y1),
                new MapPoint(x0, y1),
            };
        }

        public static MapDefinition Empty(float size = 50f)
        {
            return new MapDefinition { id = "test", width = size, height = size, cellSize = 1f };
        }

        /// <summary>
        /// 50 x 50 m. A 2-storey brick house in the middle (20..30, 20..30) and a
        /// 5-storey concrete block in the west (2..8, 20..30).
        /// </summary>
        public static MapDefinition HouseAndTower()
        {
            var map = Empty();
            map.buildings.Add(new BuildingDefinition
            {
                id = 1,
                material = BuildingMaterial.Brick,
                floors = 2,
                polygon = Rect(20f, 20f, 30f, 30f),
            });
            map.buildings.Add(new BuildingDefinition
            {
                id = 2,
                material = BuildingMaterial.Concrete,
                floors = 5,
                polygon = Rect(2f, 20f, 8f, 30f),
            });
            return map;
        }
    }
}
