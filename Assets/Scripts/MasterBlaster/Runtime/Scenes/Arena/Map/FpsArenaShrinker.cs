using System.Collections;
using System.Collections.Generic;
using HybridGame.MasterBlaster.Scripts.Arena;
using HybridGame.MasterBlaster.Scripts.Bomb;
using HybridGame.MasterBlaster.Scripts.Debug;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player;
using MoreMountains.Feedbacks;
using Unity.FPS.Game;
using Unity.Netcode;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    /// <summary>
    /// 3D / hybrid FPS arena shrink: same schedule as <see cref="ArenaShrinker"/> (snake or spiral fill),
    /// using <see cref="HybridArenaGrid"/> + <see cref="ArenaGrid3D"/> instead of Tilemaps.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class FpsArenaShrinker : NetworkBehaviour
    {
        private NetworkVariable<float> _netTimeRemaining = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        [Header("Timer")]
        [SerializeField]
        private bool shrinkingEnabled = true;

        [SerializeField]
        private float matchDuration = 180f;

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
            "If > 0: shrink starts this many seconds after the alarm starts (real time). When set, clock-based shrink trigger is not used."
        )]
        [SerializeField]
        private float shrinkDelaySecondsAfterAlarm;

#if UNITY_EDITOR
        [SerializeField]
        private float editorQuickTestMatchDuration;
#endif

        [SerializeField]
        private bool autoStartTimer = true;

        [Header("Integration")]
        [Tooltip("When true, disables HybridMatchAlarmTimer in the scene so only one system ends the match.")]
        [SerializeField]
        private bool disableLegacyHybridMatchTimer = true;

        [SerializeField]
        private HybridArenaGrid arenaGrid;

        [Header("Alarm Visuals")]
        [SerializeField]
        private UnityEngine.Camera targetCamera;

        [SerializeField]
        private Color alarmColor = Color.red;

        [SerializeField]
        private float pulseSpeed = 5f;

        [SerializeField]
        private Transform alarmLightsRoot;

        [SerializeField]
        private float alarmLightPhaseSpreadPerIndex = 0.35f;

        [Header("Alarm Feedbacks")]
        [SerializeField]
        private MMF_Player alarmStartFeedbacks;

        [SerializeField]
        private MMF_Player alarmStopFeedbacks;

        [Header("Alarm audio")]
        [Tooltip("Loop played on the required AudioSource while the alarm is active. If empty, visuals still run but no alarm sound.")]
        [SerializeField]
        private AudioClip alarmLoopClip;

        [Header("Shrinking")]
        [SerializeField]
        private GameObject indestructiblePrefab;

        [Tooltip("Parent for runtime shrink cubes (ephemeral; cleared when the match ends). If unset, a child named ArenaShrinkBlocks is created under this object.")]
        [SerializeField]
        private Transform shrinkBlocksParent;

        private const string ShrinkBlocksRootName = "ArenaShrinkBlocks";

        [Tooltip("Seconds of real time between each new block (lower = faster fill). Same units as Match Duration.")]
        [SerializeField]
        private float shrinkDelay = 0.08f;

        [Tooltip("How cells are visited: boustrophedon rows vs perimeter spiral inward.")]
        [SerializeField]
        private ArenaShrinkPattern shrinkPattern = ArenaShrinkPattern.BoustrophedonSnake;

        [Tooltip(
            "Snake only: if true, boustrophedon walks row index from min→max (match when grid row 0 is the outer ‘top’ edge). " +
            "If false, rows run max→min (fills from the opposite edge first in index space)."
        )]
        [SerializeField]
        private bool snakeIterateYFromMinToMax;

        [Tooltip("Optional one-shot when a block is placed. If set, overrides AudioSource.clip on the spawned prefab.")]
        [SerializeField]
        private AudioClip blockPlaceClip;

        [Tooltip(
            "Per-axis half-extent for the physics overlap query when a shrink block is placed, as a fraction of " +
            nameof(ArenaGrid3D) + "." + nameof(ArenaGrid3D.CellSize) + " (default 0.45 ⇒ ~0.9 m box for 1 m cells). " +
            "Slightly below 0.5 avoids grabbing colliders in neighboring cells."
        )]
        [SerializeField]
        [Range(0.35f, 0.49f)]
        private float overlapQueryHalfExtentFactor = 0.45f;

        [Header("Advanced")]
        [Tooltip("Layers included when querying bombs, walls, pickups, and players in the shrink cell. Default: everything.")]
        [SerializeField]
        private LayerMask overlapMask = ~0;

        [Header("Irregular Grid Padding")]
        [Tooltip("How many cells to skip on the left edge (Min X)")]
        [SerializeField] private int padLeft = 0;

        [Tooltip("How many cells to skip on the right edge (Max X)")]
        [SerializeField] private int padRight = 1;

        [Tooltip("How many cells to skip on the bottom edge (Min Y/Z)")]
        [SerializeField] private int padBottom = 0;

        [Tooltip("How many cells to skip on the top edge (Max Y/Z)")]
        [SerializeField] private int padTop = 1;

        [Header("Debug")]
        [SerializeField]
        private bool showAlarmCountdownDebug = true;

        private static readonly Collider[] _overlapBuffer = new Collider[16];

        private AudioSource _arenaAudioSource;
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

        private int minX, maxX, minY, maxY;

        private HybridArenaGrid Grid => arenaGrid != null ? arenaGrid : (arenaGrid = GetComponent<HybridArenaGrid>());

        private void Awake()
        {
            if (TrainingMode.IsActive)
                shrinkingEnabled = false;
            else if (PlayerPrefs.HasKey("Shrinking"))
                shrinkingEnabled = PlayerPrefs.GetInt("Shrinking", 1) == 1;

            _arenaAudioSource = GetComponent<AudioSource>();
            RefreshBackgroundPulseCamera();

            if (disableLegacyHybridMatchTimer)
            {
                foreach (var h in FindObjectsByType<HybridMatchAlarmTimer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (h != null)
                        h.enabled = false;
                }
            }

            EnsureShrinkBlocksRoot();
        }

        private void EnsureShrinkBlocksRoot()
        {
            if (shrinkBlocksParent != null)
                return;
            var go = new GameObject(ShrinkBlocksRootName);
            go.transform.SetParent(transform, false);
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

        private void Start()
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

        private void Update()
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
            string line1 = shrinkingStarted && !shrinkingComplete
                ? "[FPS] Arena shrinking — survive!"
                : $"[FPS] Match remaining: {remaining:F1}s";
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
                line2 = shrinkPattern == ArenaShrinkPattern.SpiralInward
                    ? "Wall closing in (spiral)"
                    : "Wall closing in (snake)";
            else
                line2 = "Until alarm: " + Mathf.Max(0f, remaining - alarmTh).ToString("F1") + "s";

            const float pad = 12f;
            var box = new Rect(pad, pad, 460f, 52f);
            GUI.Box(box, GUIContent.none);
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white },
            };
            GUI.Label(new Rect(pad + 8f, pad + 6f, 440f, 22f), line1, style);
            GUI.Label(new Rect(pad + 8f, pad + 28f, 440f, 22f), line2, style);
        }

        private float GetDisplayedTimeRemaining()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
                return _netTimeRemaining.Value;
            return timeRemaining;
        }

        private void SyncClientAlarmFromNetworkIfNeeded()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsServer)
                return;
            float remaining = _netTimeRemaining.Value;
            float alarmThreshold = ComputeAlarmThresholdRemaining();
            ApplyAlarmPresentationState(ArenaShrinkSchedule.ShouldAlarmBeOn(remaining, alarmThreshold));
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

        public void StartTimer()
        {
            // #region agent log
            AgentDebugNdjson_624424.Log(
                "H2",
                "FpsArenaShrinker.StartTimer",
                "entry",
                "{\"shrinkingEnabled\":" + (shrinkingEnabled ? "true" : "false") +
                ",\"timerRunning\":" + (timerRunning ? "true" : "false") +
                ",\"isServer\":" + (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsServer ? "true" : "false") + "}",
                "pre"
            );
            // #endregion
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
        /// Call after <see cref="ResetMatchStateForNewRound"/> when the same loaded scene begins a new round
        /// (e.g. <see cref="GameManager"/> after countdown). Mirrors <see cref="Start"/> / <see cref="OnNetworkSpawn"/> auto-start.
        /// </summary>
        public void TryStartTimerAfterRoundReset()
        {
            if (!isActiveAndEnabled)
                return;
            if (shrinkingEnabled && autoStartTimer)
                StartTimer();
        }

        /// <summary>
        /// Stops timer/shrink coroutines and restores alarm lights, camera tint, and audio for a new round in the same scene.
        /// </summary>
        public void ResetMatchStateForNewRound()
        {
            // #region agent log
            AgentDebugNdjson_624424.Log(
                "H3",
                "FpsArenaShrinker.ResetMatchStateForNewRound",
                "called",
                "{}",
                "pre"
            );
            // #endregion
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
                    bool startShrink = shrinkDelaySecondsAfterAlarm > 0f
                        ? ArenaShrinkSchedule.ShouldStartShrinkAfterAlarmDelay(
                            _alarmHasFiredForDelay,
                            _alarmStartTime,
                            shrinkDelaySecondsAfterAlarm,
                            Time.time
                        )
                        : ArenaShrinkSchedule.ShouldStartShrinkByRemaining(timeRemaining, shrinkThreshold);

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

            StopAlarmPresentation();
            RefreshBackgroundPulseCamera();
            if (_backgroundPulseCamera)
                _backgroundPulseCamera.backgroundColor = originalBg;

            ClearShrinkSpawnedBlocks();

            if (!endingTriggered)
            {
                endingTriggered = true;
                if (TrainingMode.IsActive)
                    yield break;
                SceneFlowManager.I.GoTo(FlowState.Standings);
            }
        }

        private void ComputeInsideBoundsFromGrid()
        {
            var g = Grid;
            if (g == null) return;

            // Use the asymmetrical padding to trim the bounds perfectly
            minX = padLeft;
            maxX = g.columns - 1 - padRight;
            minY = padBottom;
            maxY = g.rows - 1 - padTop;

            // Safety catch to prevent inverted boundaries
            if (minX > maxX) minX = maxX = (minX + maxX) / 2;
            if (minY > maxY) minY = maxY = (minY + maxY) / 2;
        }

        private IEnumerator ShrinkRoutine()
        {
            var g = Grid;
            if (!indestructiblePrefab || g == null)
            {
                shrinkingComplete = true;
                yield break;
            }

            EnsureShrinkBlocksRoot();
            ComputeInsideBoundsFromGrid();

            foreach (var cell in ArenaShrinkCellOrder.EnumerateCells(
                         shrinkPattern,
                         minX,
                         maxX,
                         minY,
                         maxY,
                         snakeIterateYFromMinToMax
                     ))
            {
                var c2 = new Vector2Int(cell.x, cell.y);
        
                if (g.IsIndestructible(c2))
                {
                    // By yielding here, the shrinker sweeps over permanent walls 
                    // at the exact same visual speed as empty floor tiles.
                    // No more teleporting blocks!
                    yield return new WaitForSeconds(shrinkDelay);
                    continue;
                }

                PlaceBlock(c2);
                yield return new WaitForSeconds(shrinkDelay);
            }

            shrinkingComplete = true;
        }

        [ClientRpc]
        private void PlaceBlockClientRpc(Vector3 worldPos)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                return;
            if (indestructiblePrefab == null)
                return;
            Instantiate(indestructiblePrefab, worldPos, Quaternion.identity, GetShrinkBlocksParent());
            PlayBlockPlaceSound();
        }

        private void PlaceBlock(Vector2Int cell)
        {
            var g = Grid;
            // Use standard CellToWorld to prevent the unwanted 0.5 X/Z offset, 
// but manually add 0.5 to the Y axis so the cube rests on top of the floor
            Vector3 worldCenter = ArenaGrid3D.CellToWorld(cell);
            worldCenter.y = ArenaGrid3D.GridOrigin.y + (0.5f * ArenaGrid3D.CellSize);
            // #region agent log
            AgentDebugNdjson_624424.Log(
                "H4",
                "FpsArenaShrinker.PlaceBlock",
                "worldCenter_shrinkBlock",
                "{\"cellX\":" + cell.x + ",\"cellY\":" + cell.y +
                ",\"wx\":" + worldCenter.x.ToString("F4") +
                ",\"wy\":" + worldCenter.y.ToString("F4") +
                ",\"wz\":" + worldCenter.z.ToString("F4") +
                ",\"gridOriginY\":" + ArenaGrid3D.GridOrigin.y.ToString("F4") + "}",
                "post-fix"
            );
            // #endregion

            var dest = g != null ? g.GetDestructible(cell) : null;
            if (dest != null)
                dest.DestroyBlock();

            float half = ArenaGrid3D.CellSize * overlapQueryHalfExtentFactor;
            var overlapHalfExtents = new Vector3(half, half, half);

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

                var rbc2 = h.GetComponent<RemoteBombController>();
                if (rbc2 != null)
                {
                    rbc2.Detonate();
                    continue;
                }

                var rbc3 = h.GetComponent<RemoteBombController3D>();
                if (rbc3 != null)
                {
                    rbc3.ForceDetonateFromArenaShrink();
                    continue;
                }

                var wb = h.GetComponent<WallBlock3D>();
                if (wb != null)
                {
                    wb.DestroyBlock();
                    continue;
                }

                if (h.GetComponent<ItemPickup>() != null || h.GetComponent<Destructible>() != null)
                {
                    Destroy(h.gameObject);
                    continue;
                }

                var ip3 = h.GetComponent<ItemPickup3D>();
                if (ip3 != null)
                {
                    Destroy(h.gameObject);
                    continue;
                }

                var pc = h.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.ApplyDeath();
                    continue;
                }

                var health = h.GetComponentInParent<Health>();
                if (health != null && !health.IsDead)
                    health.TakeDamage(health.MaxHealth * 2f, gameObject);
            }

            var go = Instantiate(indestructiblePrefab, worldCenter, Quaternion.identity, GetShrinkBlocksParent());

            bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isOnline && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                PlaceBlockClientRpc(worldCenter);

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
    }
}
