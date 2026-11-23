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

        /// <summary>Frames the whole map and limits panning to it.</summary>
        public void Configure(Rect worldBounds)
        {
            _bounds = worldBounds;
            var cam = Cam;
            cam.orthographic = true;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = cameraHeight + 50f;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            float aspect = cam.aspect > 0f ? cam.aspect : 16f / 9f;
            float fitHeight = worldBounds.height * 0.5f;
            float fitWidth = worldBounds.width * 0.5f / aspect;
            cam.orthographicSize = Mathf.Clamp(Mathf.Max(fitHeight, fitWidth) * 1.05f, minOrthographicSize, maxOrthographicSize);

            var center = worldBounds.center;
            transform.position = new Vector3(center.x, cameraHeight, center.y);
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
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * factor, minOrthographicSize, maxOrthographicSize);
            Vector3 after = ScreenToGround(screenPoint);
            transform.position += before - after;
        }

        Vector3 ScreenToGround(Vector2 screenPoint)
        {
            Vector3 world = Cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, cameraHeight));
            world.y = 0f;
            return world;
        }

        void ClampToBounds()
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, _bounds.xMin, _bounds.xMax);
            p.z = Mathf.Clamp(p.z, _bounds.yMin, _bounds.yMax);
            p.y = cameraHeight;
            transform.position = p;
        }
    }
}
