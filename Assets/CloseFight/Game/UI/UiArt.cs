using CloseFight.Core.Units;
using UnityEngine;

namespace CloseFight.Game.UI
{
    /// <summary>
    /// Codex UI art under Resources/Art/UI: NATO-style unit symbols drawn in ink
    /// (D42) for the team cards, and the briefing background. Null when missing.
    /// </summary>
    public static class UiArt
    {
        public static Texture2D BriefingBackground => Resources.Load<Texture2D>("Art/UI/briefing_station");

        static Material _pictureMaterial;
        static bool _pictureMaterialTried;

        /// <summary>The material that shows a picture in its true colours (Resources/Shaders/GuiPicture).</summary>
        static Material PictureMaterial
        {
            get
            {
                if (!_pictureMaterialTried)
                {
                    _pictureMaterialTried = true;
                    var shader = Resources.Load<Shader>("Shaders/GuiPicture");
                    if (shader != null && shader.isSupported)
                    {
                        _pictureMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
                    }
                }

                return _pictureMaterial;
            }
        }

        /// <summary>
        /// Draws a picture (imported as a plain, non-sRGB texture, like everything in
        /// Resources/Art/Roster and Art/UI) in its true colours: plain GUI.DrawTexture shows it
        /// one sRGB encoding too bright in this linear-colour project. Clipping by GUI groups
        /// works as usual.
        /// </summary>
        public static void DrawPicture(Rect position, Texture image, ScaleMode scaleMode = ScaleMode.ScaleToFit)
        {
            if (image == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            var screen = position;
            var source = new Rect(0f, 0f, 1f, 1f);
            float aspect = (float)image.width / image.height;
            bool wider = position.width / position.height > aspect;
            if (scaleMode == ScaleMode.ScaleToFit)
            {
                if (wider)
                {
                    float width = position.height * aspect;
                    screen = new Rect(position.x + (position.width - width) * 0.5f, position.y, width, position.height);
                }
                else
                {
                    float height = position.width / aspect;
                    screen = new Rect(position.x, position.y + (position.height - height) * 0.5f, position.width, height);
                }
            }
            else if (scaleMode == ScaleMode.ScaleAndCrop)
            {
                // Fill the rect, cutting the picture's overhang evenly from both sides.
                float share = wider ? aspect / (position.width / position.height) : (position.width / position.height) / aspect;
                source = wider ? new Rect(0f, (1f - share) * 0.5f, 1f, share) : new Rect((1f - share) * 0.5f, 0f, share, 1f);
            }

            var material = PictureMaterial;
            if (material == null)
            {
                GUI.DrawTexture(position, image, scaleMode, true);
                return;
            }

            Graphics.DrawTexture(screen, image, source, 0, 0, 0, 0, GUI.color, material);
        }

        public static Texture2D IconFor(Team team)
        {
            string name;
            switch (team.Type)
            {
                case TeamType.MachineGun: name = "icon_machinegun"; break;
                case TeamType.Mortar: name = "icon_mortar"; break;
                case TeamType.AntiTankGun: name = "icon_antitank"; break;
                case TeamType.Headquarters: name = "icon_headquarters"; break;
                case TeamType.Vehicle:
                    name = team.Vehicle != null && !team.Vehicle.Definition.HasTurret ? "icon_assaultgun" : "icon_armor";
                    break;
                // Sniper and engineer symbols are still to come from Codex; the infantry symbol stands in.
                default: name = "icon_infantry"; break;
            }

            return Resources.Load<Texture2D>("Art/UI/" + name);
        }
    }
}
