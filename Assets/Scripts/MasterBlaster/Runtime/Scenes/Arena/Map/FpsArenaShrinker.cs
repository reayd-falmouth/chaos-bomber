using System.Collections;
using HybridGame.MasterBlaster.Scripts.Arena;
using HybridGame.MasterBlaster.Scripts.Bomb;
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
        [Tooltip(
            "If true: Alarm Remaining Seconds / Shrink Remaining Seconds are absolute seconds (same unit as Match Duration). " +
            "If false: use the fraction fields — thresholds are matchDuration × fraction (e.g. 0.10 = last 10% of the match)."
        )]
        [SerializeField]
        private bool useRemainingSecondsSchedule = true;

        [Tooltip("When Use Remaining Seconds Schedule is on: alarm audio/visuals when time left ≤ this many seconds.")]
        [SerializeField]
        private float alarmRemainingSeconds = 10f;

        [Tooltip(
            "When Use Remaining Seconds Schedule is on: shrink can start when time left ≤ this (unless Shrink Delay After Alarm is set)."
        )]
        [SerializeField]
        private float shrinkRemainingSeconds = 27f;

        [Tooltip("Used only when Use Remaining Seconds Schedule is off: alarm when remaining ≤ matchDuration × this.")]
        [SerializeField]
        private float alarmThresholdFraction = 0.10f;

        [Tooltip("Used only when Use Remaining Seconds Schedule is off: shrink when remaining ≤ matchDuration × this.")]
        [SerializeField]
        private float shrinkThresholdFraction = 0.15f;

        [Tooltip(
            "If > 0: shrink starts this many seconds after the alarm starts (real time). When set, Shrink Remaining Seconds / fraction shrink threshold is not used."
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

        [SerializeField]
        private Vector3 overlapHalfExtents = new Vector3(0.45f, 0.45f, 0.45f);

        [SerializeField]
        private LayerMask overlapMask = ~0;

        [SerializeField]
        private int inset = 1;

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
            else if (shrinkingStarted && !shrinkingComplete)
                line2 = shrinkPattern == ArenaShrinkPattern.SpiralInward
                    ? "Wall closing in (spiral)"
                    : "Wall closing in (snake)";
            else if (remaining <= 0f)
                line2 = "Time's up";
            else if (ArenaShrinkSchedule.ShouldAlarmBeOn(remaining, alarmTh))
                line2 = "ALARM (last " + alarmTh.ToString("F0") + "s)";
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
        /// Stops timer/shrink coroutines and restores alarm lights, camera tint, and audio for a new round in the same scene.
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

        private IEnumerator TimerRoutine()
        {
            bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isOnline && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
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

            while (timerRunning)
            {
                if (shrinkingStarted)
                {
                    timeRemaining = 0f;
                    if (isOnline && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && IsSpawned)
                        _netTimeRemaining.Value = 0f;
                    if (shrinkingComplete)
                        break;
                    yield return null;
                    continue;
                }

                timeRemaining -= Time.deltaTime;
                if (timeRemaining < 0f)
                    timeRemaining = 0f;

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
            if (g == null)
            {
                minX = maxX = minY = maxY = 0;
                return;
            }

            int c = g.columns;
            int r = g.rows;
            minX = inset;
            maxX = c - 1 - inset;
            minY = inset;
            maxY = r - 1 - inset;
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

            foreach (
                var cell in ArenaShrinkCellOrder.EnumerateCells(
                    shrinkPattern,
                    minX,
                    maxX,
                    minY,
                    maxY,
                    snakeIterateYFromMinToMax
                )
            )
            {
                var c2 = new Vector2Int(cell.x, cell.y);
                if (g.IsIndestructible(c2))
                    continue;

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
            Vector3 worldCenter = ArenaGrid3D.CellToWorld(cell);

            var dest = g != null ? g.GetDestructible(cell) : null;
            if (dest != null)
                dest.DestroyBlock();

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
