using System.Collections;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena;
using MoreMountains.Feedbacks;
using Unity.Netcode;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    /// <summary>
    /// Match time limit, alarm audio/visuals, and transition to Standings when time hits zero.
    /// For hybrid FPS / 3D arenas that do not use <see cref="ArenaShrinker"/> tilemap shrink.
    /// Attach next to <see cref="GameManager"/> (same GameObject needs <see cref="NetworkObject"/> for net sync).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class HybridMatchAlarmTimer : NetworkBehaviour
    {
        private NetworkVariable<float> _netTimeRemaining = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [Header("Timer")]
        [SerializeField]
        private bool matchTimerEnabled = true;

        [SerializeField]
        private float matchDuration = 180f;

        [Header("Schedule (same semantics as ArenaShrinker)")]
        [SerializeField]
        private bool useRemainingSecondsSchedule = true;

        [SerializeField]
        private float alarmRemainingSeconds = 10f;

        [SerializeField]
        private float alarmThresholdFraction = 0.1f;

#if UNITY_EDITOR
        [SerializeField]
        private float editorQuickTestMatchDuration;
#endif

        [SerializeField]
        private bool autoStartTimer = true;

        [Header("Alarm visuals")]
        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private Color alarmColor = Color.red;

        [SerializeField]
        private float pulseSpeed = 5f;

        [Header("Alarm feedbacks (optional)")]
        [SerializeField]
        private MMF_Player alarmStartFeedbacks;

        [SerializeField]
        private MMF_Player alarmStopFeedbacks;

        [Header("Alarm audio")]
        [SerializeField]
        private AudioClip alarmLoopClip;

        [Header("Debug")]
        [SerializeField]
        private bool showAlarmCountdownDebug = true;

        private AudioSource _audioSource;
        private float timeRemaining;
        private bool alarmActive;
        private bool timerRunning;
        private bool endingTriggered;
        private Color originalBg;

        private void Awake()
        {
            if (TrainingMode.IsActive)
                matchTimerEnabled = false;
            else if (PlayerPrefs.HasKey("Shrinking"))
                matchTimerEnabled = PlayerPrefs.GetInt("Shrinking", 1) == 1;

            _audioSource = GetComponent<AudioSource>();

            if (!targetCamera)
                targetCamera = Camera.main;
            if (targetCamera)
                originalBg = targetCamera.backgroundColor;
        }

        private void Start()
        {
            if (matchTimerEnabled && autoStartTimer)
                StartTimer();
        }

        private void Update()
        {
            SyncClientAlarmFromNetworkIfNeeded();

            if (alarmActive && targetCamera)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                targetCamera.backgroundColor = Color.Lerp(originalBg, alarmColor, t);
            }
        }

        private void OnGUI()
        {
            if (!showAlarmCountdownDebug)
                return;

            float remaining = GetDisplayedTimeRemaining();
            float alarmTh = ComputeAlarmThresholdRemaining();
            string line1 = $"[HybridMatch] remaining: {remaining:F1}s";
            string line2;
            if (!matchTimerEnabled)
                line2 = "Match timer disabled (training or menu prefs)";
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
            var box = new Rect(pad, pad, 440f, 52f);
            GUI.Box(box, GUIContent.none);
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white },
            };
            GUI.Label(new Rect(pad + 8f, pad + 6f, 420f, 22f), line1, style);
            GUI.Label(new Rect(pad + 8f, pad + 28f, 420f, 22f), line2, style);
        }

        public void StartTimer()
        {
            if (!matchTimerEnabled || timerRunning)
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
            if (targetCamera)
                targetCamera.backgroundColor = originalBg;
        }

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
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || IsServer)
                return;

            float remaining = _netTimeRemaining.Value;
            float alarmThreshold = ComputeAlarmThresholdRemaining();
            bool shouldAlarm = ArenaShrinkSchedule.ShouldAlarmBeOn(remaining, alarmThreshold);
            ApplyAlarmPresentationState(shouldAlarm);
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

        private IEnumerator TimerRoutine()
        {
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

            while (timerRunning && timeRemaining > 0f)
            {
                timeRemaining -= Time.deltaTime;

                if (isOnline && IsServer && IsSpawned)
                    _netTimeRemaining.Value = timeRemaining;

                if (!alarmActive && ArenaShrinkSchedule.ShouldAlarmBeOn(timeRemaining, alarmThreshold))
                    StartAlarmPresentation();

                yield return null;
            }

            StopAlarmPresentation();
            if (targetCamera)
                targetCamera.backgroundColor = originalBg;

            if (!endingTriggered)
            {
                endingTriggered = true;
                if (TrainingMode.IsActive)
                    yield break;
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
                alarmStartFeedbacks?.PlayFeedbacks();
                if (_audioSource != null && alarmLoopClip != null)
                {
                    _audioSource.loop = true;
                    _audioSource.clip = alarmLoopClip;
                    _audioSource.Play();
                }
            }
            else
            {
                alarmStopFeedbacks?.PlayFeedbacks();
                if (_audioSource != null)
                {
                    _audioSource.Stop();
                    _audioSource.clip = null;
                }
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            alarmStopFeedbacks?.PlayFeedbacks();
            if (_audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.clip = null;
            }
        }
    }
}
