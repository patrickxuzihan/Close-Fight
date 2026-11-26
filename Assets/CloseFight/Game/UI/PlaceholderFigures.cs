using CloseFight.Core.Units;
using UnityEngine;

namespace CloseFight.Game.UI
{
    /// <summary>
    /// Stand-in roster figures until Codex delivers the real ones (M18): a soldier
    /// seen from the front in his side's uniform and helmet, holding what he carries.
    /// Drawn once per side and role into a small texture with a dark outline.
    /// </summary>
    static class PlaceholderFigures
    {
        struct Palette
        {
            public Color32 Uniform, UniformDark, Helmet, Skin, Boots, Metal, Wood, Gear;
        }

        static Palette For(Side side)
        {
            return side == Side.German
                ? new Palette
                {
                    Uniform = new Color32(104, 110, 100, 255), UniformDark = new Color32(76, 82, 74, 255), Helmet = new Color32(82, 88, 80, 255),
                    Skin = new Color32(214, 178, 148, 255), Boots = new Color32(36, 30, 26, 255), Metal = new Color32(38, 38, 40, 255),
                    Wood = new Color32(112, 74, 42, 255), Gear = new Color32(60, 50, 38, 255),
                }
                : new Palette
                {
                    Uniform = new Color32(146, 132, 88, 255), UniformDark = new Color32(108, 98, 64, 255), Helmet = new Color32(86, 98, 60, 255),
                    Skin = new Color32(214, 178, 148, 255), Boots = new Color32(40, 32, 26, 255), Metal = new Color32(38, 38, 40, 255),
                    Wood = new Color32(118, 76, 40, 255), Gear = new Color32(88, 76, 52, 255),
                };
        }

        public static Texture2D Draw(Side side, FigureRole role, int width, int height)
        {
            var canvas = new Canvas(width, height);
            var p = For(side);
            bool german = side == Side.German;

            // Kit on the back first (it shows past the shoulders).
            if (role == FigureRole.Flamethrower)
            {
                canvas.Rect(9, 22, 14, 50, p.Gear);
                canvas.Rect(26, 22, 31, 50, p.Gear);
            }
            else if (role == FigureRole.Radio)
            {
                canvas.Rect(8, 20, 32, 46, p.Gear);
                canvas.Line(29, 20, 35, 1, 1, p.Metal);
            }

            // Legs and boots.
            canvas.Rect(14, 52, 19, 84, p.UniformDark);
            canvas.Rect(21, 52, 26, 84, p.UniformDark);
            canvas.Rect(13, german ? 72 : 76, 19, 90, p.Boots);
            canvas.Rect(21, german ? 72 : 76, 27, 90, p.Boots);

            // Tunic, belt, arms.
            canvas.Rect(13, 21, 27, 53, p.Uniform);
            canvas.Rect(13, 43, 27, 45, p.Gear);
            canvas.Line(14, 24, 15, 44, 3, p.Uniform);
            canvas.Line(26, 24, 25, 42, 3, p.Uniform);

            // Head and helmet (or a crewman's side cap).
            canvas.Disc(20, 15, 5.5f, p.Skin);
            if (role == FigureRole.Crew)
            {
                canvas.Rect(15, 8, 25, 11, p.UniformDark);
            }
            else if (german)
            {
                // Stahlhelm: dome with a flared skirt over the ears and neck.
                canvas.Disc(20, 12, 6.5f, p.Helmet);
                canvas.Rect(13, 12, 27, 15, p.Helmet);
                canvas.Rect(12, 14, 15, 17, p.Helmet);
                canvas.Rect(25, 14, 28, 17, p.Helmet);
            }
            else
            {
                // SSh-40: a plain round dome with a short brim.
                canvas.Disc(20, 12, 6.5f, p.Helmet);
                canvas.Rect(13, 12, 27, 14, p.Helmet);
            }

            canvas.Disc(15, 44, 2f, p.Skin);
            canvas.Disc(25, 42, 2f, p.Skin);

            // What he carries.
            switch (role)
            {
                case FigureRole.Rifleman:
                    canvas.Line(9, 62, 30, 10, 2, p.Wood);
                    canvas.Line(23, 28, 30, 10, 1, p.Metal);
                    break;
                case FigureRole.Sniper:
                    canvas.Line(9, 62, 30, 10, 2, p.Wood);
                    canvas.Line(23, 28, 30, 10, 1, p.Metal);
                    canvas.Line(20, 33, 24, 24, 2, p.Metal);
                    break;
                case FigureRole.Leader:
                    canvas.Line(13, 44, 33, 34, 3, p.Metal);
                    if (german)
                    {
                        canvas.Rect(21, 39, 23, 49, p.Metal); // MP 40's straight magazine
                    }
                    else
                    {
                        canvas.Disc(22, 42, 3.5f, p.Metal); // PPSh drum
                        canvas.Line(10, 46, 15, 43, 3, p.Wood);
                    }

                    break;
                case FigureRole.MachineGunner:
                    // Carried at the hip, bipod folded under the barrel.
                    canvas.Line(6, 40, 38, 32, 3, p.Metal);
                    canvas.Line(6, 40, 13, 38, 3, german ? p.Metal : p.Wood);
                    canvas.Line(34, 33, 36, 40, 1, p.Metal);
                    if (!german)
                    {
                        canvas.Disc(22, 33, 4.5f, p.Metal); // DP's pan magazine
                    }

                    break;
                case FigureRole.Assistant:
                    canvas.Rect(26, 46, 33, 56, p.Gear); // ammunition boxes
                    canvas.Rect(7, 46, 14, 56, p.Gear);
                    canvas.Rect(24, 44, 27, 48, p.Metal); // pistol holster
                    break;
                case FigureRole.HeavyGunner:
                    if (german)
                    {
                        // MG 34 on the Lafette tripod in front of him.
                        canvas.Line(1, 60, 34, 56, 3, p.Metal);
                        canvas.Line(14, 59, 6, 90, 2, p.Metal);
                        canvas.Line(16, 59, 26, 90, 2, p.Metal);
                        canvas.Line(15, 59, 15, 88, 2, p.Metal);
                    }
                    else
                    {
                        // Maxim: water jacket, shield and the Sokolov wheels.
                        canvas.Rect(2, 56, 30, 62, p.Metal);
                        canvas.Rect(26, 50, 34, 70, p.Helmet);
                        canvas.Disc(10, 84, 5.5f, p.Metal);
                        canvas.Disc(26, 84, 5.5f, p.Metal);
                    }

                    break;
                case FigureRole.Flamethrower:
                    canvas.Line(22, 42, 38, 34, 2, p.Metal);
                    canvas.Disc(38, 33, 1.8f, new Color32(240, 140, 40, 255));
                    break;
            }

            return canvas.ToTexture("RosterFigure " + side + " " + role);
        }

        /// <summary>A tiny raster with top-down coordinates.</summary>
        sealed class Canvas
        {
            readonly int _w, _h;
            readonly Color32[] _pixels;

            public Canvas(int w, int h)
            {
                _w = w;
                _h = h;
                _pixels = new Color32[w * h];
            }

            void Set(int x, int y, Color32 c)
            {
                if (x >= 0 && y >= 0 && x < _w && y < _h)
                {
                    _pixels[(_h - 1 - y) * _w + x] = c;
                }
            }

            public void Rect(int x0, int y0, int x1, int y1, Color32 c)
            {
                for (int y = y0; y < y1; y++)
                {
                    for (int x = x0; x < x1; x++)
                    {
                        Set(x, y, c);
                    }
                }
            }

            public void Disc(float cx, float cy, float r, Color32 c)
            {
                for (int y = Mathf.FloorToInt(cy - r); y <= Mathf.CeilToInt(cy + r); y++)
                {
                    for (int x = Mathf.FloorToInt(cx - r); x <= Mathf.CeilToInt(cx + r); x++)
                    {
                        float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                        if (dx * dx + dy * dy <= r * r)
                        {
                            Set(x, y, c);
                        }
                    }
                }
            }

            public void Line(float x0, float y0, float x1, float y1, int thickness, Color32 c)
            {
                int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)) * 2f) + 1;
                float r = thickness * 0.5f;
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    Disc(Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t), Mathf.Max(0.6f, r), c);
                }
            }

            public Texture2D ToTexture(string name)
            {
                // A one-pixel dark outline so the figure reads on any background.
                var outlined = (Color32[])_pixels.Clone();
                var ink = new Color32(22, 22, 20, 255);
                for (int y = 0; y < _h; y++)
                {
                    for (int x = 0; x < _w; x++)
                    {
                        if (_pixels[y * _w + x].a != 0)
                        {
                            continue;
                        }

                        bool edge = false;
                        for (int dy = -1; dy <= 1 && !edge; dy++)
                        {
                            for (int dx = -1; dx <= 1 && !edge; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                edge = nx >= 0 && ny >= 0 && nx < _w && ny < _h && _pixels[ny * _w + nx].a != 0;
                            }
                        }

                        if (edge)
                        {
                            outlined[y * _w + x] = ink;
                        }
                    }
                }

                var texture = new Texture2D(_w, _h, TextureFormat.RGBA32, false)
                {
                    name = name,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave,
                };
                texture.SetPixels32(outlined);
                texture.Apply(false, true);
                return texture;
            }
        }
    }
}
