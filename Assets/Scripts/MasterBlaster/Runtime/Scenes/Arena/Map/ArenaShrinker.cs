using System.Collections;
using System.Collections.Generic;
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

        [Tooltip(
            "Pre-shrink countdown (seconds). Alarm/shrink thresholds are derived from this. " +
            "After shrinking starts, the round continues until the snake finishes (match end is not the clock alone)."
        )]
        [SerializeField]
        private float matchDuration = 180f; // 3:00

        [Header("Schedule")]
        [Tooltip("Seconds left on the match clock when the wall shrink begins.")]
        [SerializeField]
        private float shrinkRemainingSeconds = 27f;

        [Tooltip(
            "Alarm starts when time left ≤ (Shrink Remaining Seconds + this). Keeps the alarm slightly before shrink on the clock."
        )]
        [SerializeField]
        private float alarmLeadSecondsBeforeShrink = 3f;

        [Tooltip(
            "If greater than zero, shrink starts this many seconds after the alarm starts (real time). When set, clock-based shrink trigger is not used."
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

        [Tooltip("Parent for runtime shrink cubes (ephemeral; cleared when the match ends). If unset, a child named ArenaShrinkBlocks is created under the indestructibles tilemap.")]
        [SerializeField]
        private Transform shrinkBlocksParent;

        [Tooltip("Seconds of real time between each new block (lower = faster fill). Same units as match duration.")]
        [SerializeField]
        private float shrinkDelay = 0.08f;

        [Tooltip("How cells are visited: boustrophedon rows vs perimeter spiral inward.")]
        [SerializeField]
        private ArenaShrinkPattern shrinkPattern = ArenaShrinkPattern.BoustrophedonSnake;

        [Tooltip(
            "Snake only: if true, boustrophedon walks Y from min→max (use when tilemap Y+ points down). " +
            "If false, Y runs max→min (typical when Y+ is up), matching DestructibleGenerator’s outer-row-first feel."
        )]
        [SerializeField]
        private bool snakeIterateYFromMinToMax;

        [Tooltip(
            "When true, fill order is the same snake/spiral as below, but rotated so Manual Snake Start Cell is visited first. " +
            "Coordinates are tilemap cell X/Y (inclusive inner bounds after inset). If the cell is not in the order, rotation is skipped."
        )]
        [SerializeField]
        private bool useManualSnakeStart;

        [Tooltip("Tilemap grid cell (x, y) for the first shrink block when Use Manual Snake Start is enabled.")]
        [SerializeField]
        private Vector2Int manualSnakeStartCell;

        [Tooltip("Optional one-shot when a block is placed. If set, overrides relying on AudioSource on the block prefab.")]
        [SerializeField]
        private AudioClip blockPlaceClip;

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

        private const string ShrinkBlocksRootName = "ArenaShrinkBlocks";

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
            EnsureShrinkBlocksRoot();
        }

        private void EnsureShrinkBlocksRoot()
        {
            if (shrinkBlocksParent != null)
                return;
            if (!indestructiblesTilemap)
                return;
            var go = new GameObject(ShrinkBlocksRootName);
            go.transform.SetParent(indestructiblesTilemap.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            shrinkBlocksParent = go.transform;
        }

        private Transform GetShrinkBlocksParent()
        {
            EnsureShrinkBlocksRoot();
            return shrinkBlocksParent;
        }

        /// <summary>Destroys all blocks spawned during arena shrink (children of the shrink root).</summary>
        public void ClearShrinkSpawnedBlocks()
        {
            if (shrinkBlocksParent == null)
                return;
            for (int i = shrinkBlocksParent.childCount - 1; i >= 0; i--)
            {
                var c = shrinkBlocksParent.GetChild(i);
                if (c != null)
                    Destroy(c.gameObject);
            }
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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && IsSpawned)
                _netTimeRemaining.Value = timeRemaining;
            if (shrinkingEnabled && autoStartTimer && !timerRunning)
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
            string line1;
            if (shrinkingStarted && !shrinkingComplete)
                line1 = "Arena shrinking — survive!";
            else
                line1 = $"Match remaining: {remaining:F1}s";
            string line2;
            if (!shrinkingEnabled)
                line2 = "Shrinking / timer disabled";
            else if (!timerRunning && remaining <= 0f && !shrinkingStarted)
                line2 = "Timer not started";
            else if (remaining <= 0f)
                line2 = "Time's up";
            else if (ArenaShrinkSchedule.ShouldAlarmBeOn(remaining, alarmTh))
                line2 = shrinkingStarted && !shrinkingComplete
                    ? "ALARM — wall closing"
                    : "ALARM — starts before shrink";
            else if (shrinkingStarted && !shrinkingComplete)
                line2 = "Wall closing in (snake fill)";
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
                && !NetworkManager.Singleton.IsServer
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
            return !NetworkManager.Singleton.IsServer;
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
            return ArenaShrinkSchedule.GetAlarmThresholdRemainingBeforeShrink(
                shrinkRemainingSeconds,
                alarmLeadSecondsBeforeShrink
            );
        }

        private float ComputeShrinkThresholdRemaining()
        {
            return ArenaShrinkSchedule.GetShrinkThresholdRemaining(shrinkRemainingSeconds);
        }

        // -------------------- Public API --------------------
        public void StartTimer()
        {
            if (!shrinkingEnabled || timerRunning)
                return;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
                return;
            timeRemaining = Mathf.Max(1f, EffectiveMatchDuration());
            timerRunning = true;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && IsSpawned)
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

        /// <summary>
        /// Call after <see cref="ResetMatchStateForNewRound"/> when the same loaded scene begins a new round.
        /// </summary>
        public void TryStartTimerAfterRoundReset()
        {
            if (!isActiveAndEnabled)
                return;
            if (shrinkingEnabled && autoStartTimer)
                StartTimer();
        }

        // -------------------- Internals --------------------
        private IEnumerator TimerRoutine()
        {
            bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isOnline && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                timerRunning = false;
                yield break;
            }

            float alarmThreshold = ArenaShrinkSchedule.GetAlarmThresholdRemainingBeforeShrink(
                shrinkRemainingSeconds,
                alarmLeadSecondsBeforeShrink
            );
            float shrinkThreshold = ArenaShrinkSchedule.GetShrinkThresholdRemaining(shrinkRemainingSeconds);

            while (timerRunning)
            {
                if (shrinkingStarted && shrinkingComplete)
                    break;

                if (timeRemaining > 0f)
                {
                    timeRemaining -= Time.deltaTime;
                    if (timeRemaining < 0f)
                        timeRemaining = 0f;
                }

                if (isOnline && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && IsSpawned)
                    _netTimeRemaining.Value = timeRemaining;

                if (ArenaShrinkSchedule.ShouldExitMainTimerWhenTimeReachesZero(shrinkingEnabled) && timeRemaining <= 0f)
                    break;

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

            if (shrinkingEnabled && shrinkingStarted && !shrinkingComplete)
            {
                while (!shrinkingComplete)
                    yield return null;
            }

            // timer expired or shrink finished
            StopAlarmPresentation();
            RefreshBackgroundPulseCamera();
            if (_backgroundPulseCamera)
                _backgroundPulseCamera.backgroundColor = originalBg;

            ClearShrinkSpawnedBlocks();

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

        /// <summary>
        /// Clears timer/shrink coroutines and restores alarm lights, camera tint, and audio.
        /// Call when the same scene starts a new round (e.g. GameManager after countdown).
        /// </summary>
        public void ResetMatchStateForNewRound()
        {
            StopAllCoroutines();
            timerRunning = false;
            endingTriggered = false;
            shrinkingStarted = false;
            shrinkingComplete = false;
            _alarmHasFiredForDelay = false;
            timeRemaining = 0f;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && IsSpawned)
                _netTimeRemaining.Value = 0f;
            ClearShrinkSpawnedBlocks();
            ForceRestoreAlarmPresentationAndCamera();
        }

        private void ForceRestoreAlarmPresentationAndCamera()
        {
            if (_alarmLightsCached && alarmLightsRoot != null)
            {
                AlarmEmergencyLightPresentation.RestoreAndHideRoot(
                    alarmLightsRoot,
                    _alarmLights,
                    _alarmLightBaseIntensities
                );
            }
            else if (alarmLightsRoot != null)
                alarmLightsRoot.gameObject.SetActive(false);

            alarmStopFeedbacks?.PlayFeedbacks();
            StopAlarmAudio();
            RefreshBackgroundPulseCamera();
            if (_backgroundPulseCamera != null)
                _backgroundPulseCamera.backgroundColor = originalBg;
            alarmActive = false;
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

        // ------------ Shrinking (boustrophedon snake over inner bounds) ------------
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

            // Match DestructibleGenerator / spawn bounds — shrink bounds to painted tiles first.
            indestructiblesTilemap.CompressBounds();

            // IMPORTANT: cellBounds.xMax/yMax are EXCLUSIVE
            BoundsInt b = indestructiblesTilemap.cellBounds;

            // Inside-of-wall, generalized for any inset:
            minX = b.xMin + inset;
            maxX = b.xMax - inset - 1; // exclusive -> inclusive
            minY = b.yMin + inset;
            maxY = b.yMax - inset - 1;

            // Include one more ring of cells toward the compressed bounds (matches playable floor vs. wall thickness).
            minX = Mathf.Max(b.xMin, minX - 1);
            maxX = Mathf.Min(b.xMax - 1, maxX + 1);
            minY = Mathf.Max(b.yMin, minY - 1);
            maxY = Mathf.Min(b.yMax - 1, maxY + 1);

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

            EnsureShrinkBlocksRoot();
            ComputeInsideBounds();

            IEnumerable<Vector3Int> visit;
            if (useManualSnakeStart)
            {
                var order = ArenaShrinkOrderUtilities.ToOrderedList(
                    shrinkPattern,
                    minX,
                    maxX,
                    minY,
                    maxY,
                    snakeIterateYFromMinToMax
                );
                if (
                    ArenaShrinkOrderUtilities.TryRotateToStart(
                        order,
                        manualSnakeStartCell.x,
                        manualSnakeStartCell.y,
                        out var rotated
                    )
                )
                {
                    visit = rotated;
                }
                else
                {
                    UnityEngine.Debug.LogWarning(
                        $"[ArenaShrinker] Manual snake start ({manualSnakeStartCell.x},{manualSnakeStartCell.y}) "
                        + "not found in computed shrink order; using unrotated order."
                    );
                    visit = order;
                }
            }
            else
            {
                visit = ArenaShrinkCellOrder.EnumerateCells(
                    shrinkPattern,
                    minX,
                    maxX,
                    minY,
                    maxY,
                    snakeIterateYFromMinToMax
                );
            }

            foreach (var cell in visit)
            {
                if (indestructiblesTilemap.HasTile(cell))
                    continue;

                PlaceBlock(cell);
                yield return new WaitForSeconds(shrinkDelay);
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
                Instantiate(indestructiblePrefab, worldPos, Quaternion.identity, GetShrinkBlocksParent());
            PlayBlockPlaceSound();
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
                GetShrinkBlocksParent()
            );

            bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isOnline && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                PlaceBlockClientRpc(worldCenter);

            // 3) Block placement SFX (explicit clip preferred; else prefab AudioSource)
            PlayBlockPlaceSoundOnBlock(go);
        }

        private void PlayBlockPlaceSound()
        {
            if (blockPlaceClip == null || _arenaAudioSource == null)
                return;
            _arenaAudioSource.PlayOneShot(blockPlaceClip);
        }

        private void PlayBlockPlaceSoundOnBlock(GameObject blockInstance)
        {
            if (blockPlaceClip != null && _arenaAudioSource != null)
            {
                _arenaAudioSource.PlayOneShot(blockPlaceClip);
                return;
            }

            var src = blockInstance != null ? blockInstance.GetComponent<AudioSource>() : null;
            if (src != null && src.clip != null)
                src.Play();
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
