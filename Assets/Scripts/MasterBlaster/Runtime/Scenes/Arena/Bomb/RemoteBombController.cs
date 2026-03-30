// RemoteBombController.cs

using System.Collections;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Player;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb
{
    public class RemoteBombController : MonoBehaviour
    {
        public enum BombMode
        {
            Fuse,
            Time,
            Remote
        }

        [Header("Movement")]
        public float speed = 5f;

        [Header("Sprites")]
        public AnimatedSpriteRenderer spriteIdle;
        public AnimatedSpriteRenderer spriteUp;
        public AnimatedSpriteRenderer spriteDown;
        public AnimatedSpriteRenderer spriteLeft;
        public AnimatedSpriteRenderer spriteRight;

        private Rigidbody2D rb;
        private Vector2 direction = Vector2.zero;

        // wiring
        private PlayerController owner;
        private BombController spawner;
        private BombMode mode;
        private KeyCode detonateKey;
        private float fuseTime;

        [Header("Feedbacks")]
        [SerializeField] private MMF_Player moveFeedbacks;

        private bool detonated;
        private bool isMoving;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            SetDirection(Vector2.zero);
        }

        public void Init(
            PlayerController owner,
            BombController spawner,
            BombMode mode,
            KeyCode detonateKey,
            float fuseTime
        )
        {
            this.owner = owner;
            this.spawner = spawner;
            this.mode = mode;
            this.detonateKey = detonateKey;
            this.fuseTime = fuseTime;

            SetDirection(Vector2.zero);

            if (mode == BombMode.Remote)
            {
                owner.stop = true;
                owner.SetRemoteBombVisual(true);
            }
            else if (mode == BombMode.Fuse)
            {
                StartCoroutine(FuseRoutine());
            }
            // Time mode: wait for key release in Update()
        }

        private void Update()
        {
            if (detonated)
                return;

            // Detonation rules: detonate when key is not held (or when AI releases)
            bool detonateHeld = owner != null && owner.GetComponent<IPlayerInput>() != null
                ? owner.GetComponent<IPlayerInput>().GetDetonateHeld()
                : Input.GetKey(detonateKey);
            if (mode == BombMode.Time && !detonateHeld)
            {
                Detonate();
                return;
            }
            if (mode == BombMode.Remote && !detonateHeld)
            {
                Detonate();
                return;
            }

            // Remote mode movement via input
            if (mode == BombMode.Remote && owner != null)
            {
                Vector2 input = Vector2.zero;
                var provider = owner.GetComponent<IPlayerInput>();
                if (provider != null)
                {
                    input = provider.GetMoveDirection();
                }
                else
                {
                    if (Input.GetKey(owner.inputUp))
                        input = Vector2.up;
                    else if (Input.GetKey(owner.inputDown))
                        input = Vector2.down;
                    else if (Input.GetKey(owner.inputLeft))
                        input = Vector2.left;
                    else if (Input.GetKey(owner.inputRight))
                        input = Vector2.right;
                }

                SetDirection(input);
            }
            if (mode == BombMode.Remote && isMoving && direction == Vector2.zero)
            {
                StopMoveSound();
            }
        }

        private void FixedUpdate()
        {
            if (detonated || rb == null)
                return; //

            if (mode == BombMode.Remote && direction != Vector2.zero) //
            {
                Vector2 translation = ArenaPlane.MoveDeltaXY(direction, speed, Time.fixedDeltaTime);

                // Define layers that should block the bomb's movement: Walls and Destructibles.
                int blockingLayers = LayerMask.GetMask("Stage", "Destructible");

                Vector2 nextPos = rb.position + translation;
                float probeRadius = 0.3f;
                if (!Physics2D.OverlapCircle(nextPos, probeRadius, blockingLayers))
                {
                    rb.MovePosition(nextPos);
                    if (!isMoving)
                    {
                        PlayMoveSound();
                    }
                }
            }
        }

        private IEnumerator FuseRoutine()
        {
            float elapsed = 0f;
            var bombInfo = GetComponent<BombInfo>();
            while (elapsed < fuseTime)
            {
                elapsed += Time.deltaTime;
                if (bombInfo != null)
                    bombInfo.timeRemainingFraction = 1f - Mathf.Clamp01(elapsed / fuseTime);
                yield return null;
            }
            Detonate();
        }

        private void SetDirection(Vector2 newDirection)
        {
            direction = Cardinal(newDirection);

            // enable exactly one sprite
            spriteIdle.enabled =
                spriteUp.enabled =
                spriteDown.enabled =
                spriteLeft.enabled =
                spriteRight.enabled =
                    false;

            if (direction == Vector2.up)
                spriteUp.enabled = true;
            else if (direction == Vector2.down)
                spriteDown.enabled = true;
            else if (direction == Vector2.left)
                spriteLeft.enabled = true;
            else if (direction == Vector2.right)
                spriteRight.enabled = true;
            else
                spriteIdle.enabled = true;
        }

        private static Vector2 Cardinal(Vector2 v)
        {
            if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
                return v.x > 0 ? Vector2.right : Vector2.left;
            if (Mathf.Abs(v.y) > 0)
                return v.y > 0 ? Vector2.up : Vector2.down;
            return Vector2.zero;
        }

        public void Detonate()
        {
            if (detonated)
                return;
            detonated = true;

            if (mode == BombMode.Remote && owner != null)
            {
                owner.stop = false;
                owner.SetRemoteBombVisual(false);
            }

            spawner.ExplodeBomb(gameObject);
            Destroy(this);
        }

        private void OnDisable()
        {
            if (!detonated && mode == BombMode.Remote && owner != null)
            {
                owner.stop = false;
                owner.SetRemoteBombVisual(false);
            }
            if (isMoving)
            {
                StopMoveSound();
            }
        }

        // --- SUPERMAN PUSH LOGIC ---
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                var super =
                    collision.gameObject.GetComponentInChildren<Player.Abilities.Superman>();
                if (super != null && super.IsActive)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;

                    // Work out push direction from contact points
                    Vector2 pushDir = rb.position - (Vector2)collision.transform.position;
                    pushDir = pushDir.sqrMagnitude > 0.01f ? pushDir.normalized : Vector2.zero;
                    SetDirection(new Vector2(pushDir.x, pushDir.y));
                    if (!isMoving)
                    {
                        PlayMoveSound();
                    }
                }
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                var super =
                    collision.gameObject.GetComponentInChildren<Player.Abilities.Superman>();
                if (super != null && super.IsActive)
                {
                    Vector2 pushDir = rb.position - (Vector2)collision.transform.position;
                    pushDir = pushDir.sqrMagnitude > 0.01f ? pushDir.normalized : Vector2.zero;
                    SetDirection(new Vector2(pushDir.x, pushDir.y));
                    if (!isMoving && rb.linearVelocity.sqrMagnitude > 0.01f)
                    {
                        PlayMoveSound();
                    }
                }
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;

                // Reset to idle animation
                SetDirection(Vector2.zero);
                StopMoveSound();
            }
        }

        private void PlayMoveSound()
        {
            moveFeedbacks?.PlayFeedbacks();
            isMoving = true;
        }

        private void StopMoveSound()
        {
            moveFeedbacks?.StopFeedbacks();
            isMoving = false;
        }
    }
}
