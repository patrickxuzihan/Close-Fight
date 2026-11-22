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

        static Texture2D _atlas;
        static Sprite _body;
        static Sprite _ring;

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

        static void EnsureCreated()
        {
            if (_atlas != null && _body != null && _ring != null)
            {
                return;
            }

            _atlas = new Texture2D(Cell * 2, Cell, TextureFormat.RGBA32, false)
            {
                name = "UnitMarkerAtlas",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };

            var pixels = new Color32[Cell * 2 * Cell];
            DrawBody(pixels, 0);
            DrawRing(pixels, Cell);
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

                    pixels[y * Cell * 2 + offsetX + x] = c;
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
                    pixels[y * Cell * 2 + offsetX + x] = new Color32(255, 255, 255, a);
                }
            }
        }
    }
}
