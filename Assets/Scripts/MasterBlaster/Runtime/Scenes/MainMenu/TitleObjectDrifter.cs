using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.MainMenu
{
    [RequireComponent(typeof(Image))]
    public class TitleObjectDrifter : MonoBehaviour
    {
        public enum DriftPreset
        {
            Custom,
            HorizontalRight,
            HorizontalLeft,
            DiagonalDownRight,
            DiagonalDownLeft,
            StraightDown
        }

        [Header("Sprite Settings")]
        [SerializeField] private Sprite objectSprite;

        [Header("1 & 2. Speed and Direction")]
        [SerializeField] private DriftPreset directionPreset = DriftPreset.HorizontalRight;
        
        [Tooltip("Only used if Preset is set to Custom.")]
        [Range(0, 360)]
        [SerializeField] private float movementAngle = 0f;
        
        [Tooltip("The speed in pixels per second.")]
        [SerializeField] private float speed = 150f;

        [Header("3. Positioning (Start/End)")]
        [SerializeField] private Vector2 startPosition = new Vector2(-1200, 0);
        [SerializeField] private Vector2 resetThreshold = new Vector2(1200, 0);
        [SerializeField] private float verticalRandomness = 300f;

        [Header("4. Frequency (Cooldown)")]
        [SerializeField] private float minWaitTime = 0.5f;
        [SerializeField] private float maxWaitTime = 3.0f;

        private RectTransform _rt;
        private Image _image;
        private Vector3 _velocity;
        private bool _isWaiting = false;

        private void Awake() 
        {
            _rt = GetComponent<RectTransform>();
            _image = GetComponent<Image>();

            if (objectSprite != null)
                _image.sprite = objectSprite;

            CalculateVelocity();
            ResetObject();
        }

        private void LateUpdate()
        {
            if (_isWaiting) return;

            _rt.anchoredPosition += (Vector2)_velocity * Time.unscaledDeltaTime;

            if (HasPassedThreshold())
                StartCoroutine(WaitAndReset());
        }

        private System.Collections.IEnumerator WaitAndReset()
        {
            _isWaiting = true;
            _image.enabled = false; 
            _rt.anchoredPosition = new Vector2(-9999, -9999);

            yield return new WaitForSecondsRealtime(Random.Range(minWaitTime, maxWaitTime));
    
            ResetObject();
            _image.enabled = true;
            _isWaiting = false;
        }

        private void CalculateVelocity()
        {
            float finalAngle = movementAngle;

            // Apply preset angles
            switch (directionPreset)
            {
                case DriftPreset.HorizontalRight:   finalAngle = 0f;   break;
                case DriftPreset.HorizontalLeft:    finalAngle = 180f; break;
                case DriftPreset.DiagonalDownRight: finalAngle = 315f; break;
                case DriftPreset.DiagonalDownLeft:  finalAngle = 225f; break;
                case DriftPreset.StraightDown:      finalAngle = 270f; break;
                // case Custom: uses movementAngle slider
            }

            float radians = finalAngle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            _velocity = dir * speed;
        }

        private bool HasPassedThreshold()
        {
            // Past X check
            bool pastX = (_velocity.x > 0) ? (_rt.anchoredPosition.x > resetThreshold.x) : (_rt.anchoredPosition.x < resetThreshold.x);
            // Past Y check
            bool pastY = (_velocity.y > 0) ? (_rt.anchoredPosition.y > resetThreshold.y) : (_rt.anchoredPosition.y < resetThreshold.y);
            
            // Logic: if we aren't moving much in one axis, don't let that axis trigger the reset.
            bool significantX = Mathf.Abs(_velocity.x) > 0.1f;
            bool significantY = Mathf.Abs(_velocity.y) > 0.1f;

            if (significantX && significantY) return pastX || pastY;
            if (significantX) return pastX;
            return pastY;
        }

        private void ResetObject()
        {
            float randomY = Random.Range(-verticalRandomness, verticalRandomness);
            _rt.anchoredPosition = new Vector2(startPosition.x, startPosition.y + randomY);
            CalculateVelocity(); 
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 20f);
        }
    }
}