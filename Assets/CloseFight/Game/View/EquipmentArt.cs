using System.Collections.Generic;
using UnityEngine;

namespace CloseFight.Game.View
{
    /// <summary>
    /// Codex top-down art for vehicles and guns, loaded from Resources/Art by the
    /// equipment id (veh_&lt;id&gt;_hull, veh_&lt;id&gt;_turret, gun_&lt;id&gt;). Sprites are
    /// 128 px per meter with the pivot at the canvas center, so they are drawn at
    /// scale 1. Returns null when an image has not been delivered yet; the
    /// renderer then falls back to placeholder shapes.
    /// </summary>
    static class EquipmentArt
    {
        const float PixelsPerMeter = 128f;

        /// <summary>
        /// Turret ring centers on the hull canvases (512 × 1024, from the Codex notes),
        /// as meters from the hull center: x to the vehicle's right, y forward.
        /// </summary>
        static readonly Dictionary<string, Vector2> RingPixels = new Dictionary<string, Vector2>
        {
            { "pz3j", new Vector2(256f, 499f) },
            { "t34_76", new Vector2(256f, 397f) },
            { "t60", new Vector2(211f, 474f) },
        };

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static readonly Dictionary<Texture, Material> Materials = new Dictionary<Texture, Material>();

        /// <summary>
        /// A sprite material of its own for each art texture. Sprites sharing the
        /// default material with the marker atlas were sometimes drawn with the
        /// atlas texture (seen in Unity 6.6 with the SRP Batcher); a separate material
        /// per texture keeps every draw bound to its own image.
        /// </summary>
        public static Material MaterialFor(Sprite sprite, Material template)
        {
            var texture = sprite.texture;
            if (!Materials.TryGetValue(texture, out var material) || material == null)
            {
                material = new Material(template) { name = "Art " + texture.name, mainTexture = texture, hideFlags = HideFlags.DontSave };
                Materials[texture] = material;
            }

            return material;
        }

        public static Sprite Hull(string vehicleId) => Load("Art/Vehicles/veh_" + vehicleId + "_hull");
        public static Sprite Turret(string vehicleId) => Load("Art/Vehicles/veh_" + vehicleId + "_turret");
        public static Sprite Gun(string gunId) => Load("Art/Guns/gun_" + gunId);

        public static Vector2? TurretRing(string vehicleId)
        {
            if (!RingPixels.TryGetValue(vehicleId, out var pixel))
            {
                return null;
            }

            return new Vector2((pixel.x - 256f) / PixelsPerMeter, (512f - pixel.y) / PixelsPerMeter);
        }

        static Sprite Load(string path)
        {
            if (!Cache.TryGetValue(path, out var sprite) || sprite == null)
            {
                sprite = Resources.Load<Sprite>(path);
                Cache[path] = sprite;
            }

            return sprite;
        }
    }
}
