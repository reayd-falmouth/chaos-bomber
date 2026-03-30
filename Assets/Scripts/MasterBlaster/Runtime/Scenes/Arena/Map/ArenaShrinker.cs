using System.Collections;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player;
using MoreMountains.Feedbacks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

// AudioController

// SceneFlowManager etc.

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Grid), typeof(AudioSource))]
    public class ArenaShrinker : NetworkBehaviour
    {
        /// <summary>Replicated timer so client UIs stay in sync with the host.</summary>
        private NetworkVariable<float> _netTimeRemaining = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        // -------------------- Timer & Alarm --------------------
        [Header("Timer")]
        [Tooltip("Only run timer/alarm/shrinking when enabled.")]
        [SerializeField]
        private bool shrinkingEnabled = true;

        [Tooltip("Total match length (seconds).")]
        [SerializeField]
        private float matchDuration = 180f; // 3:00

        [Tooltip(
            "When remaining time <= this fraction, alarm starts & background pulses (e.g. 0.10 = last 10%)."
        )]
        [SerializeField]
        private float alarmThresholdFraction = 0.10f;

        [Tooltip("When remaining time <= this fraction, shrinking starts (e.g. 0.15 = last 15%).")]
        [SerializeField]
        private float shrinkThresholdFraction = 0.15f;

        [Tooltip("Start timer automatically on Start().")]
        [SerializeField]
        private bool autoStartTimer = true;

        [Header("Alarm Visuals")]
        [SerializeField]
        private UnityEngine.Camera targetCamera;

        [SerializeField]
        private Color alarmColor = Color.red;

        [SerializeField]
        private float pulseSpeed = 5f; // sin speed

        // --- Timer & Alarm (existing fields stay the same) ---

        [Header("Alarm Feedbacks")]
        [SerializeField] private MMF_Player alarmStartFeedbacks;
        [SerializeField] private MMF_Player alarmStopFeedbacks;

        private AudioSource sirenSource; // local to Arena; dies with the scene

        // -------------------- Shrinking --------------------
        [Header("Shrinking")]
        [Tooltip("Prefab to spawn for each indestructible block.")]
        [SerializeField]
        private GameObject indestructiblePrefab;

        [Tooltip("Tilemap with the OUTER WALL. We compute inside-of-wall bounds from this.")]
        [SerializeField]
        private Tilemap indestructiblesTilemap; // usually child: "Indestructibles"

        [Tooltip("Tilemap that holds destructible tiles. Optional—clears tiles as wall advances.")]
        [SerializeField]
        private Tilemap destructiblesTilemap; // usually child: "Destructibles"

        [Tooltip("Delay between placing each block (snake speed).")]
        [SerializeField]
        private float shrinkDelay = 0.08f;

        [Tooltip("3D overlap half-extents (XZ + height) used to resolve bombs/items/players under each new block.")]
        [SerializeField]
        private Vector3 overlapHalfExtents = new Vector3(0.45f, 0.45f, 0.45f);

        [SerializeField]
        private LayerMask overlapMask = ~0;

        [Header("Auto-Detect Children By Name (optional)")]
        [SerializeField]
        private string indestructiblesName = "Indestructibles";

        [SerializeField]
        private string destructiblesName = "Destructibles";

        [Header("Debug")]
        [SerializeField]
        private bool drawGizmos;

        [SerializeField]
        private Color gizmoColor = new Color(1f, 0.3f, 0.2f, 0.35f);

        // Fix 5: pre-allocated overlap buffer — avoids OverlapBoxAll array alloc during shrink
        private static readonly Collider[] _overlapBuffer = new Collider[16];

        // internal state
        private float timeRemaining;
        private bool alarmActive;
        private bool shrinkingStarted;
        private bool shrinkingComplete;
        private bool timerRunning;
        private bool endingTriggered;
        private Color originalBg;
        private Coroutine alarmLoopCo;

        // shrink bounds (inclusive)
        private int minX,
            maxX,
            minY,
            maxY;

        void Awake()
        {
            // In training mode use overrides; otherwise pull from PlayerPrefs
            if (TrainingMode.IsActive)
                shrinkingEnabled = false;
            else if (PlayerPrefs.HasKey("Shrinking"))
                shrinkingEnabled = PlayerPrefs.GetInt("Shrinking", 1) == 1;

            if (!targetCamera)
                targetCamera = UnityEngine.Camera.main;
            if (targetCamera)
                originalBg = targetCamera.backgroundColor;

            if (!indestructiblesTilemap)
            {
                var t = transform.Find(indestructiblesName);
                indestructiblesTilemap = t ? t.GetComponent<Tilemap>() : null;
            }

            if (!destructiblesTilemap)
            {
                var t = transform.Find(destructiblesName);
                destructiblesTilemap = t ? t.GetComponent<Tilemap>() : null;
            }

            if (!indestructiblePrefab)
                UnityEngine.Debug.LogWarning("[ArenaShrinker] Indestructible prefab not assigned.");
            if (!indestructiblesTilemap)
                UnityEngine.Debug.LogError("[ArenaShrinker] Indestructibles Tilemap not found/assigned.");

            ComputeInsideBounds();
        }

        void Start()
        {
            if (shrinkingEnabled && autoStartTimer)
                StartTimer();
        }

        void Update()
        {
            // background pulse while alarm is active
            if (alarmActive && targetCamera)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                targetCamera.backgroundColor = Color.Lerp(originalBg, alarmColor, t);
            }
        }

        // -------------------- Public API --------------------
        public void StartTimer()
        {
            if (!shrinkingEnabled || timerRunning)
                return;
            timeRemaining = Mathf.Max(1f, matchDuration);
            timerRunning = true;
            StartCoroutine(TimerRoutine());
        }

        public void StopTimer()
        {
            timerRunning = false;
            StopAlarm();
            if (targetCamera)
                targetCamera.backgroundColor = originalBg;
        }

        // -------------------- Internals --------------------
        private IEnumerator TimerRoutine()
        {
            // In online play, only the host runs the timer.
            bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isOnline && !IsServer)
                yield break;

            float alarmTime = matchDuration * Mathf.Clamp01(alarmThresholdFraction);
            float shrinkTime = matchDuration * Mathf.Clamp01(shrinkThresholdFraction);

            while (timerRunning && timeRemaining > 0f)
            {
                timeRemaining -= Time.deltaTime;

                if (isOnline)
                    _netTimeRemaining.Value = timeRemaining;

                if (!alarmActive && timeRemaining <= alarmTime)
                    StartAlarm();

                if (!shrinkingStarted && timeRemaining <= shrinkTime)
                {
                    shrinkingStarted = true;
                    shrinkingComplete = false;
                    StartCoroutine(ShrinkRoutine());
                }

                yield return null;
            }

            // When shrinking has started, wait for it to reach the center before ending the round
            if (shrinkingStarted && !shrinkingComplete)
            {
                while (!shrinkingComplete)
                    yield return null;
            }

            // timer expired
            StopAlarm();
            if (targetCamera)
                targetCamera.backgroundColor = originalBg;

            if (!endingTriggered)
            {
                endingTriggered = true;
                if (TrainingMode.IsActive)
                    yield break;   // timer is disabled in training; episode resets are handled per-arena by BombermanAgent
                SceneFlowManager.I.GoTo(FlowState.Standings);
            }
        }

        public void StartAlarm()
        {
            alarmActive = true;
            alarmStartFeedbacks?.PlayFeedbacks();
        }

        public void StopAlarm()
        {
            alarmActive = false;
            alarmStopFeedbacks?.PlayFeedbacks();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            alarmStopFeedbacks?.PlayFeedbacks();
        }

        // ------------ Shrinking (clockwise snake, inside border) ------------
        // How many cells to step inside from the outer wall (usually 1)
        [SerializeField]
        private int inset = 1;

        private void ComputeInsideBounds()
        {
            if (!indestructiblesTilemap)
            {
                UnityEngine.Debug.LogError("[ArenaShrinker] No Indestructibles Tilemap assigned.");
                return;
            }

            // IMPORTANT: cellBounds.xMax/yMax are EXCLUSIVE
            BoundsInt b = indestructiblesTilemap.cellBounds;

            // Inside-of-wall, generalized for any inset:
            minX = b.xMin + inset;
            maxX = b.xMax - inset - 1; // exclusive -> inclusive
            minY = b.yMin + inset;
            maxY = b.yMax - inset - 1;

            // Safety clamp
            if (minX > maxX)
            {
                int mid = (minX + maxX) / 2;
                minX = maxX = mid;
            }
            if (minY > maxY)
            {
                int mid = (minY + maxY) / 2;
                minY = maxY = mid;
            }

            UnityEngine.Debug.Log(
                $"[ArenaShrinker] Inside bounds from compressed Tilemap: X:{minX}..{maxX}  Y:{minY}..{maxY}  (cellBounds: {b})"
            );
        }

        private IEnumerator ShrinkRoutine()
        {
            if (!indestructiblesTilemap || !indestructiblePrefab)
                yield break;

            int left = minX;
            int right = maxX;
            int bottom = minY;
            int top = maxY;

            while (left <= right && bottom <= top)
            {
                // top row (left -> right)
                for (int x = left; x <= right; x++)
                {
                    PlaceBlock(new Vector3Int(x, top, 0));
                    yield return new WaitForSeconds(shrinkDelay);
                }
                top--;

                // right col (top -> bottom)
                for (int y = top; y >= bottom; y--)
                {
                    PlaceBlock(new Vector3Int(right, y, 0));
                    yield return new WaitForSeconds(shrinkDelay);
                }
                right--;

                if (bottom > top || left > right)
                    break;

                // bottom row (right -> left)
                for (int x = right; x >= left; x--)
                {
                    PlaceBlock(new Vector3Int(x, bottom, 0));
                    yield return new WaitForSeconds(shrinkDelay);
                }
                bottom++;

                // left col (bottom -> top)
                for (int y = bottom; y <= top; y++)
                {
                    PlaceBlock(new Vector3Int(left, y, 0));
                    yield return new WaitForSeconds(shrinkDelay);
                }
                left++;
            }

            shrinkingComplete = true;
        }

        /// <summary>Tells clients to place an indestructible block at <paramref name="worldPos"/>.</summary>
        [ClientRpc]
        private void PlaceBlockClientRpc(Vector3 worldPos)
        {
            if (IsServer) return;
            if (indestructiblePrefab != null)
                Instantiate(indestructiblePrefab, worldPos, Quaternion.identity, indestructiblesTilemap.transform);
        }

        private void PlaceBlock(Vector3Int cell)
        {
            Vector3 worldCenter = indestructiblesTilemap.GetCellCenterWorld(cell);

            if (destructiblesTilemap && destructiblesTilemap.HasTile(cell))
                destructiblesTilemap.SetTile(cell, null);

            // Fix 5: NonAlloc reuses static buffer instead of allocating a new array each block
            int hitCount = Physics.OverlapBoxNonAlloc(
                worldCenter,
                overlapHalfExtents,
                _overlapBuffer,
                Quaternion.identity,
                overlapMask,
                QueryTriggerInteraction.Collide);
            for (int hi = 0; hi < hitCount; hi++)
            {
                var h = _overlapBuffer[hi];
                if (!h)
                    continue;

                var rbc = h.GetComponent<RemoteBombController>();
                if (rbc != null)
                {
                    rbc.Detonate(); // preferred path
                    continue;
                }
                if (h.GetComponent<ItemPickup>() != null || h.GetComponent<Destructible>() != null)
                {
                    Destroy(h.gameObject);
                    continue;
                }
                var pc = h.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.ApplyDeath();
                }
            }

            // 2) Spawn the new indestructible block
            var go = Instantiate(
                indestructiblePrefab,
                worldCenter,
                Quaternion.identity,
                indestructiblesTilemap.transform
            );

            bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isOnline && IsServer)
                PlaceBlockClientRpc(worldCenter);

            // 3) Play its SFX
            var src = go.GetComponent<AudioSource>();
            if (src != null)
            {
                if (src.clip != null)
                {
                    // prefab already has clip assigned
                    src.Play();
                }
            }
        }

        // ---- Debug gizmo for inside bounds ----
        void OnDrawGizmosSelected()
        {
            if (!drawGizmos || !indestructiblesTilemap)
                return;

            Vector3 a = indestructiblesTilemap.GetCellCenterWorld(new Vector3Int(minX, minY, 0));
            Vector3 b = indestructiblesTilemap.GetCellCenterWorld(new Vector3Int(maxX, maxY, 0));
            Vector3 size = b - a;
            size.x += indestructiblesTilemap.cellSize.x;
            size.y += indestructiblesTilemap.cellSize.y;
            size.z += indestructiblesTilemap.cellSize.z;

            Vector3 center = (a + b) * 0.5f;

            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(center, size);
        }
    }
}
