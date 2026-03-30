using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.MainMenu
{
    /// <summary>
    /// Moves a UI element across the screen, then respawns it at a random off-screen position.
    /// Designed for Title scene ambient objects like asteroid/comet images.
    /// Optional randomized cooldown: object waits off-screen before moving again.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class TitleObjectDrifter : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private Vector2 direction = new Vector2(1f, 0f);
        [SerializeField] private float speed = 140f;

        [Header("Bounds (Screen-space margin in pixels)")]
        [SerializeField] private float offscreenMargin = 200f;

        [Header("Cooldown (after each pass)")]
        [Tooltip("When enabled, waits a random time at the spawn edge before moving again.")]
        [SerializeField] private bool useRandomCooldown = true;
        [SerializeField] private Vector2 cooldownSecondsRange = new Vector2(0.35f, 1.75f);

        [Header("Random Respawn")]
        [SerializeField] private bool randomizeSpeed = true;
        [SerializeField] private Vector2 speedRange = new Vector2(90f, 220f);
        [SerializeField] private bool randomizeVerticalSpawn = true;
        [SerializeField] private Vector2 spawnYRange = new Vector2(-450f, 450f);

        private RectTransform _rt;
        private Vector2 _moveDir;
        private float _currentSpeed;
        private float _cooldownUntilUnscaled;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _moveDir = direction.sqrMagnitude < 0.0001f ? Vector2.right : direction.normalized;
            _currentSpeed = Mathf.Max(1f, speed);
        }

        private void OnEnable()
        {
            _cooldownUntilUnscaled = 0f;
            ApplyRandomSpeed();
        }

        private void Update()
        {
            if (_rt == null)
                return;

            if (Time.unscaledTime < _cooldownUntilUnscaled)
                return;

            _rt.anchoredPosition += _moveDir * (_currentSpeed * Time.unscaledDeltaTime);

            if (HasExitedScreen(_rt.anchoredPosition))
                Respawn();
        }

        private bool HasExitedScreen(Vector2 anchoredPos)
        {
            float halfW = Screen.width * 0.5f;
            float halfH = Screen.height * 0.5f;

            float left = -halfW - offscreenMargin;
            float right = halfW + offscreenMargin;
            float bottom = -halfH - offscreenMargin;
            float top = halfH + offscreenMargin;

            return anchoredPos.x < left || anchoredPos.x > right || anchoredPos.y < bottom || anchoredPos.y > top;
        }

        private void Respawn()
        {
            float halfW = Screen.width * 0.5f;
            float halfH = Screen.height * 0.5f;

            float spawnX;
            if (_moveDir.x >= 0f)
                spawnX = -halfW - offscreenMargin;
            else
                spawnX = halfW + offscreenMargin;

            float spawnY;
            if (randomizeVerticalSpawn)
                spawnY = Random.Range(spawnYRange.x, spawnYRange.y);
            else
                spawnY = _rt.anchoredPosition.y;

            float minY = -halfH - offscreenMargin;
            float maxY = halfH + offscreenMargin;
            spawnY = Mathf.Clamp(spawnY, minY, maxY);

            _rt.anchoredPosition = new Vector2(spawnX, spawnY);
            ApplyRandomSpeed();

            if (useRandomCooldown)
            {
                float lo = Mathf.Min(cooldownSecondsRange.x, cooldownSecondsRange.y);
                float hi = Mathf.Max(cooldownSecondsRange.x, cooldownSecondsRange.y);
                _cooldownUntilUnscaled = Time.unscaledTime + Random.Range(lo, hi);
            }
            else
                _cooldownUntilUnscaled = 0f;
        }

        private void ApplyRandomSpeed()
        {
            if (randomizeSpeed)
                _currentSpeed = Mathf.Max(1f, Random.Range(speedRange.x, speedRange.y));
            else
                _currentSpeed = Mathf.Max(1f, speed);
        }
    }
}
