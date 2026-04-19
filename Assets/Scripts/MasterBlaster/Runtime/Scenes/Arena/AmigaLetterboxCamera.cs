using HybridGame.MasterBlaster.Scripts.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena
{
    /// <summary>
    /// Keeps the camera viewport at the design aspect ratio (e.g. 640:512), adding
    /// letterboxing so the game view matches the Amiga look on 16:9 and other ratios.
    /// Also centers the camera on the arena each time ApplyLetterbox() is called.
    /// Attach to the Main Camera in the Game scene.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class AmigaLetterboxCamera : MonoBehaviour
    {
        [Tooltip("Design aspect width (default from DesignResolution).")]
        [SerializeField]
        private int designWidth = DesignResolution.Width;

        [Tooltip("Design aspect height (default from DesignResolution).")]
        [SerializeField]
        private int designHeight = DesignResolution.Height;

        [Tooltip("World-space center override. Leave at (0,0) to auto-detect from scene tilemaps.")]
        [SerializeField]
        private Vector2 arenaCenter = Vector2.zero;

        private UnityEngine.Camera _camera;
        private float _designAspect;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private Vector2 _resolvedCenter;

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            _designAspect = designWidth / (float)designHeight;
            // Ensure local position starts at zero so the CameraShaker parent drives world XY.
            transform.localPosition = new Vector3(0f, 0f, transform.localPosition.z);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Re-detect arena center and re-centre the camera every time a new scene loads.
            RefreshAndApply();
        }

        private void OnEnable()
        {
            RefreshAndApply();
        }

        private void Start()
        {
            RefreshAndApply();
        }

        private void Update()
        {
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;
                ApplyLetterbox();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_camera != null && designWidth > 0 && designHeight > 0)
            {
                _designAspect = designWidth / (float)designHeight;
                ApplyLetterbox();
            }
        }
#endif

        /// <summary>Re-detect the arena center and reapply letterbox + centering. Call this after the arena is fully built.</summary>
        public void RefreshAndApply()
        {
            _lastScreenWidth  = Screen.width;
            _lastScreenHeight = Screen.height;
            _resolvedCenter   = arenaCenter != Vector2.zero ? arenaCenter : DetectArenaCenter();
            ApplyLetterbox();
        }

        /// <summary>Sets design aspect (handheld layout presets) and reapplies letterbox.</summary>
        public void SetDesignResolution(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                UnityEngine.Debug.LogWarning(
                    "[MasterBlaster][AmigaLetterbox] SetDesignResolution ignored invalid size " + width + "x" + height + ".");
                return;
            }

            designWidth = width;
            designHeight = height;
            _designAspect = designWidth / (float)designHeight;
            RefreshAndApply();
        }

        public int GetDesignWidth() => designWidth;

        public int GetDesignHeight() => designHeight;

        /// <summary>
        /// Centers the camera on the arena and sets the viewport rect to the design aspect ratio,
        /// adding letterbox / pillarbox bars as needed.
        /// </summary>
        public void ApplyLetterbox()
        {
            if (_camera == null)
                return;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            if (screenWidth <= 0 || screenHeight <= 0)
                return;

            // Keep the camera centered on the arena every time letterbox is applied.
            transform.position = new Vector3(_resolvedCenter.x, _resolvedCenter.y, transform.position.z);

            float screenAspect = screenWidth / screenHeight;

            float w, h, x, y;
            if (screenAspect > _designAspect)
            {
                // Screen is wider: pillarbox (horizontal bars on sides)
                w = _designAspect / screenAspect;
                h = 1f;
                x = (1f - w) * 0.5f;
                y = 0f;
            }
            else
            {
                // Screen is taller: letterbox (vertical bars top/bottom)
                w = 1f;
                h = screenAspect / _designAspect;
                x = 0f;
                y = (1f - h) * 0.5f;
            }

            _camera.rect = new Rect(x, y, w, h);
        }

        /// <summary>Computes the world-space center of all Tilemaps in the scene.</summary>
        private Vector2 DetectArenaCenter()
        {
            var tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            if (tilemaps.Length == 0)
                return Vector2.zero;

            Bounds combined = default;
            bool first = true;
            foreach (var tm in tilemaps)
            {
                tm.CompressBounds();
                if (tm.cellBounds.size == Vector3Int.zero)
                    continue;
                var worldCenter = tm.transform.TransformPoint(tm.localBounds.center);
                var worldBounds = new Bounds(worldCenter, tm.localBounds.size);
                if (first) { combined = worldBounds; first = false; }
                else combined.Encapsulate(worldBounds);
            }

            return first ? Vector2.zero : (Vector2)combined.center;
        }
    }
}
