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
    /// 3D / hybrid FPS arena shrink: same schedule + boustrophedon snake as <see cref="ArenaShrinker"/>,
    /// but uses <see cref="HybridArenaGrid"/> + <see cref="ArenaGrid3D"/> instead of Tilemaps.
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
        [SerializeField]
        private bool useRemainingSecondsSchedule = true;

        [SerializeField]
        private float alarmRemainingSeconds = 10f;

        [SerializeField]
        private float shrinkRemainingSeconds = 27f;

        [SerializeField]
        private float alarmThresholdFraction = 0.10f;

        [SerializeField]
        private float shrinkThresholdFraction = 0.15f;

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
        [SerializeField]
        private AudioClip alarmLoopClip;

        [Header("Shrinking")]
        [SerializeField]
        private GameObject indestructiblePrefab;

        [SerializeField]
        private Transform shrinkBlocksParent;

        [SerializeField]
        private float shrinkDelay = 0.08f;

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
                line2 = "Wall closing in (snake fill)";
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

                if (timeRemaining <= 0f)
                    break;

                timeRemaining -= Time.deltaTime;
                if (isOnline && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && IsSpawned)
                    _netTimeRemaining.Value = timeRemaining;

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

            if (shrinkingStarted && !shrinkingComplete)
            {
                while (timerRunning && !shrinkingComplete)
                    yield return null;
            }

            StopAlarmPresentation();
            RefreshBackgroundPulseCamera();
            if (_backgroundPulseCamera)
                _backgroundPulseCamera.backgroundColor = originalBg;

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

            ComputeInsideBoundsFromGrid();

            foreach (var cell in ArenaShrinkSnakeOrder.EnumerateCells(minX, maxX, minY, maxY))
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
            var parent = shrinkBlocksParent != null ? shrinkBlocksParent : transform;
            Instantiate(indestructiblePrefab, worldPos, Quaternion.identity, parent);
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

            Transform parent = shrinkBlocksParent != null ? shrinkBlocksParent : transform;
            var go = Instantiate(indestructiblePrefab, worldCenter, Quaternion.identity, parent);

            bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isOnline && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                PlaceBlockClientRpc(worldCenter);

            var src = go.GetComponent<AudioSource>();
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
