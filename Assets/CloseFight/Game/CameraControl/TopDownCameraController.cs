using CloseFight.Game.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CloseFight.Game.CameraControl
{
    /// <summary>
    /// Fixed straight-down orthographic camera (D23).
    /// Pan: WASD / arrow keys, middle-mouse drag, optional screen-edge scroll;
    /// in trackpad mode also two-finger scroll.
    /// Zoom: mouse wheel (or Cmd/Ctrl + two-finger scroll in trackpad mode), toward
    /// the cursor, in proportion to the scroll so a trackpad zooms smoothly.
    /// Uses unscaled time so the camera still moves while the battle is paused (D34).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class TopDownCameraController : MonoBehaviour
    {
        [Tooltip("Pan speed in screen heights per second.")]
        [SerializeField] float panSpeed = 0.9f;
        [SerializeField] float minOrthographicSize = 8f;
        [SerializeField] float maxOrthographicSize = 260f;
        [Tooltip("Fraction of the current size changed per wheel notch.")]
        [SerializeField] float zoomStep = 0.15f;
        [Tooltip("Trackpad mode: screen heights panned per unit of scroll.")]
        [SerializeField] float trackpadPan = 0.04f;
        [SerializeField] bool edgeScroll = false;
        [SerializeField] float edgeScrollBorder = 6f;
        [SerializeField] float cameraHeight = 300f;

        Camera _camera;
        Rect _bounds = new Rect(0f, 0f, 100f, 100f);
        bool _dragging;
        Vector3 _dragAnchorWorld;

        Camera Cam
        {
            get
            {
                if (_camera == null)
                {
                    _camera = GetComponent<Camera>();
                }

                return _camera;
            }
        }

        /// <summary>
        /// Shows the map filling the whole screen and keeps the view on the map: the
        /// battlefield always runs to the screen edges, with no empty margins beside
        /// it whatever the window's shape (zoom out stops where the map would no longer
        /// cover the screen).
        /// </summary>
        public void Configure(Rect worldBounds)
        {
            _bounds = worldBounds;
            var cam = Cam;
            cam.orthographic = true;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = cameraHeight + 50f;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            cam.orthographicSize = MaxSize();
            var center = worldBounds.center;
            transform.position = new Vector3(center.x, cameraHeight, center.y);
            ClampToBounds();
        }

        /// <summary>The widest zoom at which the map still covers the whole screen.</summary>
        float MaxSize()
        {
            var cam = Cam;
            float aspect = cam.aspect > 0f ? cam.aspect : 16f / 9f;
            float cover = Mathf.Min(_bounds.height * 0.5f, _bounds.width * 0.5f / aspect);
            return Mathf.Clamp(cover, minOrthographicSize, maxOrthographicSize);
        }

        /// <summary>Centers the view on a ground point, keeping the zoom.</summary>
        public void FocusOn(Vector3 groundPoint)
        {
            transform.position = new Vector3(groundPoint.x, cameraHeight, groundPoint.z);
            ClampToBounds();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            float dt = Time.unscaledDeltaTime;
            var cam = Cam;

            Vector2 move = Vector2.zero;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            }

            if (mouse != null)
            {
                Vector2 pointer = mouse.position.ReadValue();

                if (edgeScroll && Application.isFocused)
                {
                    if (pointer.x <= edgeScrollBorder) move.x -= 1f;
                    if (pointer.x >= Screen.width - edgeScrollBorder) move.x += 1f;
                    if (pointer.y <= edgeScrollBorder) move.y -= 1f;
                    if (pointer.y >= Screen.height - edgeScrollBorder) move.y += 1f;
                }

                // One wheel notch is 1; a trackpad sends many small fractions per gesture.
                Vector2 scroll = mouse.scroll.ReadValue();
                bool zoomModifier = keyboard != null && (keyboard.leftCommandKey.isPressed || keyboard.rightCommandKey.isPressed || keyboard.ctrlKey.isPressed);
                if (GameSettings.TrackpadMode && !zoomModifier)
                {
                    if (scroll.sqrMagnitude > 1e-6f)
                    {
                        float step = trackpadPan * cam.orthographicSize * 2f;
                        transform.position += new Vector3(scroll.x, 0f, scroll.y) * step;
                    }
                }
                else if (Mathf.Abs(scroll.y) > 1e-3f)
                {
                    ZoomTowards(pointer, Mathf.Pow(1f - zoomStep, Mathf.Clamp(scroll.y, -4f, 4f)));
                }

                if (mouse.middleButton.wasPressedThisFrame)
                {
                    _dragging = true;
                    _dragAnchorWorld = ScreenToGround(pointer);
                }
                else if (!mouse.middleButton.isPressed)
                {
                    _dragging = false;
                }

                if (_dragging)
                {
                    Vector3 current = ScreenToGround(pointer);
                    transform.position += _dragAnchorWorld - current;
                }
            }

            if (move.sqrMagnitude > 0f)
            {
                move = Vector2.ClampMagnitude(move, 1f);
                float speed = panSpeed * cam.orthographicSize * 2f;
                transform.position += new Vector3(move.x, 0f, move.y) * (speed * dt);
            }

            ClampToBounds();
        }

        void ZoomTowards(Vector2 screenPoint, float factor)
        {
            var cam = Cam;
            Vector3 before = ScreenToGround(screenPoint);
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * factor, minOrthographicSize, MaxSize());
            Vector3 after = ScreenToGround(screenPoint);
            transform.position += before - after;
        }

        Vector3 ScreenToGround(Vector2 screenPoint)
        {
            Vector3 world = Cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, cameraHeight));
            world.y = 0f;
            return world;
        }

        /// <summary>Keeps the whole view on the map (or centred on it, along an axis where the map is narrower than the view).</summary>
        void ClampToBounds()
        {
            var cam = Cam;
            if (cam.orthographicSize > MaxSize())
            {
                cam.orthographicSize = MaxSize(); // the window changed shape
            }

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * (cam.aspect > 0f ? cam.aspect : 16f / 9f);
            Vector3 p = transform.position;
            p.x = halfWidth * 2f >= _bounds.width ? _bounds.center.x : Mathf.Clamp(p.x, _bounds.xMin + halfWidth, _bounds.xMax - halfWidth);
            p.z = halfHeight * 2f >= _bounds.height ? _bounds.center.y : Mathf.Clamp(p.z, _bounds.yMin + halfHeight, _bounds.yMax - halfHeight);
            p.y = cameraHeight;
            transform.position = p;
        }
    }
}
