using UnityEngine;

namespace CloseFight.Game.View
{
    /// <summary>
    /// Placeholder unit sprites drawn in code: a disc with a facing notch, and a
    /// selection ring. White, tinted per side by the SpriteRenderer. They are
    /// replaced by the Codex sprite frames once a full set is ready.
    /// Both shapes live in one atlas texture: when sprite batching merged two
    /// separate runtime textures, the ring was drawn with the disc's texture.
    /// </summary>
    public static class MarkerSprites
    {
        const int Cell = 64;
        const int Columns = 4;

        static Texture2D _atlas;
        static Sprite _body;
        static Sprite _ring;
        static Sprite _softDisc;
        static Sprite _box;

        public static Sprite Body
        {
            get
            {
                EnsureCreated();
                return _body;
            }
        }

        public static Sprite Ring
        {
            get
            {
                EnsureCreated();
                return _ring;
            }
        }

        /// <summary>Filled square with a dark border; scaled into hulls, turrets and barrels.</summary>
        public static Sprite Box
        {
            get
            {
                EnsureCreated();
                return _box;
            }
        }

        /// <summary>Round blob with soft edges (smoke).</summary>
        public static Sprite SoftDisc
        {
            get
            {
                EnsureCreated();
                return _softDisc;
            }
        }

        static void EnsureCreated()
        {
            if (_atlas != null && _body != null && _ring != null && _softDisc != null && _box != null)
            {
                return;
            }

            _atlas = new Texture2D(Cell * Columns, Cell, TextureFormat.RGBA32, false)
            {
                name = "UnitMarkerAtlas",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };

            var pixels = new Color32[Cell * Columns * Cell];
            DrawBody(pixels, 0);
            DrawRing(pixels, Cell);
            DrawSoftDisc(pixels, Cell * 2);
            DrawBox(pixels, Cell * 3);
            _atlas.SetPixels32(pixels);
            _atlas.Apply(false, true);

            // 64 px = 1 world unit; the view scales markers to soldier size.
            // FullRect: the default "tight" mesh is traced from alpha and breaks on hollow shapes like the ring.
            _body = Sprite.Create(_atlas, new Rect(0, 0, Cell, Cell), new Vector2(0.5f, 0.5f), Cell, 0, SpriteMeshType.FullRect);
            _body.name = "UnitBody";
            _body.hideFlags = HideFlags.DontSave;

            _ring = Sprite.Create(_atlas, new Rect(Cell, 0, Cell, Cell), new Vector2(0.5f, 0.5f), Cell, 0, SpriteMeshType.FullRect);
            _ring.name = "UnitRing";
            _ring.hideFlags = HideFlags.DontSave;

            _softDisc = Sprite.Create(_atlas, new Rect(Cell * 2, 0, Cell, Cell), new Vector2(0.5f, 0.5f), Cell, 0, SpriteMeshType.FullRect);
            _softDisc.name = "SoftDisc";
            _softDisc.hideFlags = HideFlags.DontSave;

            _box = Sprite.Create(_atlas, new Rect(Cell * 3 + 1, 1, Cell - 2, Cell - 2), new Vector2(0.5f, 0.5f), Cell - 2, 0, SpriteMeshType.FullRect);
            _box.name = "Box";
            _box.hideFlags = HideFlags.DontSave;
        }

        static void DrawBox(Color32[] pixels, int offsetX)
        {
            for (int y = 0; y < Cell; y++)
            {
                for (int x = 0; x < Cell; x++)
                {
                    bool border = x < 5 || y < 5 || x >= Cell - 5 || y >= Cell - 5;
                    pixels[y * Cell * Columns + offsetX + x] = border ? new Color32(35, 35, 35, 255) : new Color32(255, 255, 255, 255);
                }
            }
        }

        static void DrawSoftDisc(Color32[] pixels, int offsetX)
        {
            float center = (Cell - 1) * 0.5f;
            for (int y = 0; y < Cell; y++)
            {
                for (int x = 0; x < Cell; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 30f;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a); // smoothstep falloff
                    pixels[y * Cell * Columns + offsetX + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
        }

        static void DrawBody(Color32[] pixels, int offsetX)
        {
            float center = (Cell - 1) * 0.5f;
            const float radius = 22f;
            const float outline = 3f;

            for (int y = 0; y < Cell; y++)
            {
                for (int x = 0; x < Cell; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    // Facing notch: a wedge sticking out of the top of the disc.
                    bool notch = dy > 0f && dy < radius + 9f && Mathf.Abs(dx) < (radius + 9f - dy) * 0.45f && d > radius - 2f;

                    Color32 c = new Color32(0, 0, 0, 0);
                    if (d <= radius - outline)
                    {
                        c = new Color32(255, 255, 255, 255);
                    }
                    else if (d <= radius || notch)
                    {
                        c = new Color32(40, 40, 40, 255);
                    }

                    pixels[y * Cell * Columns + offsetX + x] = c;
                }
            }
        }

        static void DrawRing(Color32[] pixels, int offsetX)
        {
            float center = (Cell - 1) * 0.5f;
            for (int y = 0; y < Cell; y++)
            {
                for (int x = 0; x < Cell; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // Stay one pixel inside the cell so bilinear filtering never bleeds into the neighbor.
                    byte a = (byte)(d >= 26f && d <= 30f ? 255 : 0);
                    pixels[y * Cell * Columns + offsetX + x] = new Color32(255, 255, 255, a);
                }
            }
        }
    }
}
