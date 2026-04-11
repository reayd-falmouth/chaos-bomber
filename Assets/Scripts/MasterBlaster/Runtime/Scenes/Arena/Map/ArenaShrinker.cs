using System.Collections;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player;
using MoreMountains.Feedbacks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

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

        [Header("Schedule")]
        [Tooltip(
            "If true, use alarmRemainingSeconds / shrinkRemainingSeconds. If false, use fraction fields below."
        )]
        [SerializeField]
        private bool useRemainingSecondsSchedule = true;

        [Tooltip("Alarm when remaining time is less than or equal to this (seconds).")]
        [SerializeField]
        private float alarmRemainingSeconds = 10f;

        [Tooltip(
            "Shrink when remaining time is less than or equal to this (seconds). Ignored if Shrink Delay After Alarm is set."
        )]
        [SerializeField]
        private float shrinkRemainingSeconds = 27f;

        [Tooltip(
            "When remaining time <= this fraction, alarm starts & background pulses (e.g. 0.10 = last 10%). Used when Use Remaining Seconds is off."
        )]
        [SerializeField]
        private float alarmThresholdFraction = 0.10f;

        [Tooltip(
            "When remaining time <= this fraction, shrinking starts (e.g. 0.15 = last 15%). Used when Use Remaining Seconds is off."
        )]
        [SerializeField]
        private float shrinkThresholdFraction = 0.15f;

        [Tooltip(
            "If greater than zero, shrink starts this many seconds after the alarm starts (real time). When set, Shrink Remaining Seconds / fraction shrink threshold is not used to trigger shrink."
        )]
        [SerializeField]
        private float shrinkDelaySecondsAfterAlarm;

#if UNITY_EDITOR
        [Header("Editor quick test")]
        [Tooltip("If greater than zero in the Editor, overrides match duration for faster iteration (builds ignore this).")]
        [SerializeField]
        private float editorQuickTestMatchDuration;
#endif

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

        [Tooltip("Parent of spot/point lights to enable and pulse while the alarm is active (optional).")]
        [SerializeField]
        private Transform alarmLightsRoot;

        [Tooltip("Phase offset per light index (radians) for a staggered flash.")]
        [SerializeField]
        private float alarmLightPhaseSpreadPerIndex = 0.35f;

        [Header("Alarm Feedbacks")]
        [SerializeField]
        private MMF_Player alarmStartFeedbacks;

        [SerializeField]
        private MMF_Player alarmStopFeedbacks;

        [Header("Alarm audio")]
        [Tooltip("Optional loop played on the required AudioSource while the alarm is active.")]
        [SerializeField]
        private AudioClip alarmLoopClip;

        private AudioSource _arenaAudioSource;

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

        [Tooltip("Draw on-screen match / alarm countdown (uses OnGUI). Disable for shipping builds.")]
        [SerializeField]
        private bool showAlarmCountdownDebug = true;

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
        private UnityEngine.Camera _backgroundPulseCamera;
        private Light[] _alarmLights;
        private float[] _alarmLightBaseIntensities;
        private bool _alarmLightsCached;
        private float _alarmStartTime;
        private bool _alarmHasFiredForDelay;

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

            _arenaAudioSource = GetComponent<AudioSource>();

            RefreshBackgroundPulseCamera();

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
            AlarmEmergencyLightPresentation.TryCache(
                alarmLightsRoot,
                ref _alarmLights,
                ref _alarmLightBaseIntensities,
                ref _alarmLightsCached
            );
            if (shrinkingEnabled && autoStartTimer)
                StartTimer();
        }

        void Update()
        {
            RefreshBackgroundPulseCamera();
            SyncClientAlarmFromNetworkIfNeeded();

            if (alarmActive && _backgroundPulseCamera)
            {
                float t = AlarmPresentationPulse.SinPulse01(Time.time, pulseSpeed);
                _backgroundPulseCamera.backgroundColor = Color.Lerp(originalBg, alarmColor, t);
            }

            if (alarmActive && alarmLightsRoot != null && _alarmLightsCached)
            {
                AlarmEmergencyLightPresentation.ApplyIntensityPulse(
                    _alarmLights,
                    _alarmLightBaseIntensities,
                    Time.time,
                    pulseSpeed,
                    alarmLightPhaseSpreadPerIndex
                );
            }
        }

        private void RefreshBackgroundPulseCamera()
        {
            UnityEngine.Camera cam = targetCamera != null ? targetCamera : UnityEngine.Camera.main;
            if (cam == _backgroundPulseCamera)
                return;
            _backgroundPulseCamera = cam;
            if (cam != null)
                originalBg = cam.backgroundColor;
        }

        private void OnGUI()
        {
            if (!showAlarmCountdownDebug)
                return;

            float remaining = GetDisplayedTimeRemaining();
            float alarmTh = ComputeAlarmThresholdRemaining();
            string line1 = $"Match remaining: {remaining:F1}s";
            string line2;
            if (!shrinkingEnabled)
                line2 = "Shrinking / timer disabled";
            else if (!timerRunning && remaining <= 0f)
                line2 = "Timer not started";
            else if (remaining <= 0f)
                line2 = "Time's up";
            else if (ArenaShrinkSchedule.ShouldAlarmBeOn(remaining, alarmTh))
                line2 = "ALARM (last " + alarmTh.ToString("F0") + "s)";
            else
            {
                float untilAlarm = Mathf.Max(0f, remaining - alarmTh);
                line2 = "Until alarm: " + untilAlarm.ToString("F1") + "s";
            }

            const float pad = 12f;
            var box = new Rect(pad, pad, 420f, 52f);
            GUI.Box(box, GUIContent.none);
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white },
            };
            GUI.Label(new Rect(pad + 8f, pad + 6f, 400f, 22f), line1, style);
            GUI.Label(new Rect(pad + 8f, pad + 28f, 400f, 22f), line2, style);
        }

        /// <summary>Host/offline uses local timer; clients use replicated value.</summary>
        private float GetDisplayedTimeRemaining()
        {
            if (
                NetworkManager.Singleton != null
                && NetworkManager.Singleton.IsListening
                && !IsServer
            )
                return _netTimeRemaining.Value;
            return timeRemaining;
        }

        private void SyncClientAlarmFromNetworkIfNeeded()
        {
            if (!ShouldDriveAlarmFromNetworkVariable())
                return;

            float remaining = _netTimeRemaining.Value;
            float alarmThreshold = ComputeAlarmThresholdRemaining();
            bool shouldAlarm = ArenaShrinkSchedule.ShouldAlarmBeOn(remaining, alarmThreshold);
            ApplyAlarmPresentationState(shouldAlarm);
        }

        private bool ShouldDriveAlarmFromNetworkVariable()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return false;
            return !IsServer;
        }

        private float EffectiveMatchDuration()
        {
#if UNITY_EDITOR
            if (editorQuickTestMatchDuration > 0f)
                return editorQuickTestMatchDuration;
#endif
            return matchDuration;
        }

        private float ComputeAlarmThresholdRemaining()
        {
            float md = EffectiveMatchDuration();
            return ArenaShrinkSchedule.GetAlarmThresholdRemaining(
                md,
                useRemainingSecondsSchedule,
                alarmRemainingSeconds,
                alarmThresholdFraction
            );
        }

        private float ComputeShrinkThresholdRemaining()
        {
            float md = EffectiveMatchDuration();
            return ArenaShrinkSchedule.GetShrinkThresholdRemaining(
                md,
                useRemainingSecondsSchedule,
                shrinkRemainingSeconds,
                shrinkThresholdFraction
            );
        }

        // -------------------- Public API --------------------
        public void StartTimer()
        {
            if (!shrinkingEnabled || timerRunning)
                return;
            timeRemaining = Mathf.Max(1f, EffectiveMatchDuration());
            timerRunning = true;
            if (
                NetworkManager.Singleton != null
                && NetworkManager.Singleton.IsServer
                && IsSpawned
            )
                _netTimeRemaining.Value = timeRemaining;
            StartCoroutine(TimerRoutine());
        }

        public void StopTimer()
        {
            timerRunning = false;
            StopAlarmPresentation();
            RefreshBackgroundPulseCamera();
            if (_backgroundPulseCamera)
                _backgroundPulseCamera.backgroundColor = originalBg;
        }

        // -------------------- Internals --------------------
        private IEnumerator TimerRoutine()
        {
            // In online play, only the host runs the timer.
            bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isOnline && !IsServer)
            {
                timerRunning = false;
                yield break;
            }

            float md = EffectiveMatchDuration();
            float alarmThreshold = ArenaShrinkSchedule.GetAlarmThresholdRemaining(
                md,
                useRemainingSecondsSchedule,
                alarmRemainingSeconds,
                alarmThresholdFraction
            );
            float shrinkThreshold = ArenaShrinkSchedule.GetShrinkThresholdRemaining(
                md,
                useRemainingSecondsSchedule,
                shrinkRemainingSeconds,
                shrinkThresholdFraction
            );

            while (timerRunning && timeRemaining > 0f)
            {
                timeRemaining -= Time.deltaTime;

                if (isOnline && IsServer && IsSpawned)
                    _netTimeRemaining.Value = timeRemaining;

                if (!alarmActive && ArenaShrinkSchedule.ShouldAlarmBeOn(timeRemaining, alarmThreshold))
                {
                    _alarmHasFiredForDelay = true;
                    _alarmStartTime = Time.time;
                    StartAlarmPresentation();
                }

                if (!shrinkingStarted)
                {
                    bool startShrink = false;
                    if (shrinkDelaySecondsAfterAlarm > 0f)
                    {
                        startShrink = ArenaShrinkSchedule.ShouldStartShrinkAfterAlarmDelay(
                            _alarmHasFiredForDelay,
                            _alarmStartTime,
                            shrinkDelaySecondsAfterAlarm,
                            Time.time
                        );
                    }
                    else
                    {
                        startShrink = ArenaShrinkSchedule.ShouldStartShrinkByRemaining(
                            timeRemaining,
                            shrinkThreshold
                        );
                    }

                    if (startShrink)
                    {
                        shrinkingStarted = true;
                        shrinkingComplete = false;
                        StartCoroutine(ShrinkRoutine());
                    }
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
            StopAlarmPresentation();
            RefreshBackgroundPulseCamera();
            if (_backgroundPulseCamera)
                _backgroundPulseCamera.backgroundColor = originalBg;

            if (!endingTriggered)
            {
                endingTriggered = true;
                if (TrainingMode.IsActive)
                    yield break; // timer is disabled in training; episode resets are handled per-arena by BombermanAgent
                SceneFlowManager.I.GoTo(FlowState.Standings);
            }
        }

        private void StartAlarmPresentation()
        {
            ApplyAlarmPresentationState(true);
        }

        private void StopAlarmPresentation()
        {
            ApplyAlarmPresentationState(false);
        }

        private void ApplyAlarmPresentationState(bool active)
        {
            if (alarmActive == active)
                return;
            alarmActive = active;
            if (active)
            {
                AlarmEmergencyLightPresentation.TryCache(
                    alarmLightsRoot,
                    ref _alarmLights,
                    ref _alarmLightBaseIntensities,
                    ref _alarmLightsCached
                );
                AlarmEmergencyLightPresentation.ActivateAlarmRoot(alarmLightsRoot);
                alarmStartFeedbacks?.PlayFeedbacks();
                PlayAlarmAudio();
            }
            else
            {
                if (_alarmLightsCached && alarmLightsRoot != null)
                {
                    AlarmEmergencyLightPresentation.RestoreAndHideRoot(
                        alarmLightsRoot,
                        _alarmLights,
                        _alarmLightBaseIntensities
                    );
                }
                alarmStopFeedbacks?.PlayFeedbacks();
                StopAlarmAudio();
            }
        }

        private void PlayAlarmAudio()
        {
            if (_arenaAudioSource == null || alarmLoopClip == null)
                return;
            _arenaAudioSource.loop = true;
            _arenaAudioSource.clip = alarmLoopClip;
            _arenaAudioSource.Play();
        }

        private void StopAlarmAudio()
        {
            if (_arenaAudioSource == null)
                return;
            _arenaAudioSource.Stop();
            _arenaAudioSource.clip = null;
        }

        public void StartAlarm()
        {
            StartAlarmPresentation();
        }

        public void StopAlarm()
        {
            StopAlarmPresentation();
        }

        public override void OnDestroy()
        {
            if (_alarmLightsCached && alarmLightsRoot != null)
            {
                AlarmEmergencyLightPresentation.RestoreAndHideRoot(
                    alarmLightsRoot,
                    _alarmLights,
                    _alarmLightBaseIntensities
                );
            }
            RefreshBackgroundPulseCamera();
            if (_backgroundPulseCamera != null)
                _backgroundPulseCamera.backgroundColor = originalBg;
            base.OnDestroy();
            alarmStopFeedbacks?.PlayFeedbacks();
            StopAlarmAudio();
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
            {
                shrinkingComplete = true;
                yield break;
            }

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
            if (IsServer)
                return;
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
