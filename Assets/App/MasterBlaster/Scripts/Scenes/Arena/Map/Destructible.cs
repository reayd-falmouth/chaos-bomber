using HybridGame.MasterBlaster.Scripts.Player;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player.Abilities;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    [RequireComponent(typeof(Collider2D))]
    public class Destructible : MonoBehaviour
    {
        public float destructionTime = 0.5f; // how long the destroy anim plays

        [Range(0f, 1f)]
        public float itemSpawnChance = 0.2f;
        public GameObject[] spawnableItems;

        private Rigidbody2D rb;
        public bool isDebris = false;
        private bool destroyed = false;

        private AnimatedSpriteRenderer anim;

        [Header("Feedbacks")]
        [SerializeField] private MMF_Player moveFeedbacks;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody2D>();

            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            anim = GetComponent<AnimatedSpriteRenderer>();
        }

        private void Start()
        {
            if (isDebris)
            {
                // debris disappears after a short time
                if (anim != null)
                    anim.StartAnimation();
                Destroy(gameObject, destructionTime);
            }
        }

        private bool wasMoving = false;
        public float movementThreshold = 0.05f; // tweak this

        private void Update()
        {
            if (rb == null)
                return;

            bool isMoving =
                rb.bodyType != RigidbodyType2D.Kinematic
                && rb.linearVelocity.magnitude > movementThreshold;

            if (isMoving && !wasMoving)
                moveFeedbacks?.PlayFeedbacks();
            else if (wasMoving && !isMoving)
                moveFeedbacks?.StopFeedbacks();

            wasMoving = isMoving;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (destroyed)
                return;

            if (other.gameObject.layer == LayerMask.NameToLayer("Explosion"))
            {
                DestroyBlock();
            }
        }

        public void DestroyBlock()
        {
            UnityEngine.Debug.Log("Block being destroyed");
            destroyed = true;

            // play animation if present
            if (anim != null)
            {
                anim.playOnStart = true;
                anim.StartAnimation();
            }

            // schedule actual removal after anim
            Destroy(gameObject, destructionTime);
        }

        private void OnDestroy()
        {
            if (!gameObject.scene.isLoaded)
                return;

            // Debris clones from explosions should not roll item drops (and must not respawn pickups when mass-cleaned).
            if (isDebris)
                return;

            SpawnItem();
        }

        private void SpawnItem()
        {
            if (spawnableItems.Length > 0 && Random.value < itemSpawnChance)
            {
                int randomIndex = Random.Range(0, spawnableItems.Length);
                // Parent under the active arena (MapSelector) so pickups are disabled with the Game
                // root in single-scene mode. Unparented drops stay at scene root and can still render
                // over Standings/Wheel/Shop UI after the arena is toggled off.
                Transform parent = null;
                var map = FindFirstObjectByType<MapSelector>(FindObjectsInactive.Include);
                if (map != null)
                    parent = map.transform;

                Instantiate(
                    spawnableItems[randomIndex],
                    transform.position,
                    Quaternion.identity,
                    parent
                );

                // This log should now appear in your console when an item drops
                UnityEngine.Debug.Log(
                    $"SUCCESS: Instantiated item {spawnableItems[randomIndex].name} via Invoke."
                );
            }
            else
            {
                UnityEngine.Debug.Log("FAIL: Item spawn condition failed (chance roll or empty list).");
            }
        }

        // --- Superman push logic ---
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                var super = collision.gameObject.GetComponentInChildren<Superman>();
                if (super != null && super.IsActive)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }
    }
}
