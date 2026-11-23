using UnityEngine;

namespace CloseFight.Game.UI
{
    /// <summary>
    /// Player options kept between sessions (PlayerPrefs): a frame-rate cap so a
    /// laptop does not render hundreds of frames a second, and trackpad mode
    /// (two-finger scroll pans the map, Cmd/Ctrl + scroll zooms).
    /// </summary>
    public static class GameSettings
    {
        const string FrameCapKey = "closefight.frame_cap";
        const string TrackpadKey = "closefight.trackpad";

        static bool _loaded;
        static int _frameCap = 60;
        static bool _trackpad;

        public static int FrameCap
        {
            get { Load(); return _frameCap; }
        }

        public static bool TrackpadMode
        {
            get { Load(); return _trackpad; }
            set
            {
                Load();
                _trackpad = value;
                PlayerPrefs.SetInt(TrackpadKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Switches between 60 and 30 frames per second (battery saving).</summary>
        public static void ToggleFrameCap()
        {
            Load();
            _frameCap = _frameCap == 60 ? 30 : 60;
            PlayerPrefs.SetInt(FrameCapKey, _frameCap);
            PlayerPrefs.Save();
            ApplyFrameCap();
        }

        public static void ApplyFrameCap()
        {
            Load();
            // VSync would follow the display (120 Hz on recent MacBooks); cap explicitly instead.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = _frameCap;
        }

        static void Load()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            _frameCap = PlayerPrefs.GetInt(FrameCapKey, 60) == 30 ? 30 : 60;
            _trackpad = PlayerPrefs.GetInt(TrackpadKey, 0) == 1;
        }
    }
}
