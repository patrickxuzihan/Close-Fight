using CloseFight.Core.Units;
using UnityEngine;

namespace CloseFight.Game.View
{
    /// <summary>
    /// Objective flags: the Soviet flag and, for the Germans, the Wehrmacht's
    /// identification cross (Balkenkreuz) on a field-grey cloth; no Nazi symbols.
    /// Both from public-domain Wikimedia Commons images, built into one atlas by
    /// tools/flags/make_flags.py: Soviet in the upper half, German in the lower.
    /// Flat and still; each sprite's pivot is its centre.
    /// </summary>
    static class FlagSprites
    {
        const int CellWidth = 512;
        const int CellHeight = 256;
        const float PixelsPerMeter = 128f;
        /// <summary>Width of the cloth (448 px) in meters at scale 1.</summary>
        public const float ClothWidth = 448f / PixelsPerMeter;
        /// <summary>Height of the cloth (224 px) in meters at scale 1.</summary>
        public const float ClothHeight = 224f / PixelsPerMeter;

        static Sprite _soviet;
        static Sprite _german;
        static Material _material;

        public static bool Available => Load();

        public static Sprite Get(Side side)
        {
            Load();
            return side == Side.Soviet ? _soviet : _german;
        }

        public static Material MaterialFor(Material template)
        {
            Load();
            if (_material == null)
            {
                _material = EquipmentArt.MaterialFor(_soviet, template);
            }

            return _material;
        }

        static bool Load()
        {
            if (_soviet != null && _german != null)
            {
                return true;
            }

            var atlas = Resources.Load<Texture2D>("Art/Flags/flags_atlas");
            if (atlas == null)
            {
                return false;
            }

            // Texture coordinates start at the bottom: the Soviet flag is the upper half.
            var pivot = new Vector2(0.5f, 0.5f);
            _soviet = Sprite.Create(atlas, new Rect(0, CellHeight, CellWidth, CellHeight), pivot, PixelsPerMeter, 0, SpriteMeshType.FullRect);
            _soviet.name = "flag_soviet";
            _german = Sprite.Create(atlas, new Rect(0, 0, CellWidth, CellHeight), pivot, PixelsPerMeter, 0, SpriteMeshType.FullRect);
            _german.name = "flag_german";
            _material = null;
            return true;
        }
    }
}
