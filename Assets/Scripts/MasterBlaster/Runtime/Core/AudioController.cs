using System.Collections;
using HybridGame.MasterBlaster.Scripts.Utilities;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace HybridGame.MasterBlaster.Scripts.Core
{
    [System.Serializable]
    public struct SceneMusicEntry
    {
        public string sceneName;

        [Tooltip("Clips played in order, cycling back to the first. A single clip loops as before.")]
        public AudioClip[] clips;

        [Tooltip("Playback pitch. 0 = default (1.0).")]
        [Range(0f, 2f)]  public float pitch;
        [Tooltip("Playback volume. 0 = default (1.0).")]
        [Range(0f, 1f)]  public float volume;
        [Tooltip("Crossfade into this scene's music. 0 = use global default.")]
        [Range(0f, 5f)]  public float crossfadeSeconds;
        [Tooltip("Skip the loop queue and crossfade immediately when this scene loads (e.g. Countdown).")]
        public bool playImmediately;
        [Tooltip("Play the first clip once and stop — do not loop (e.g. Countdown).")]
        public bool playOnce;
        [Tooltip("Clip crossfaded in when PlayArenaOutro() is called (e.g. plays when the last player dies).")]
        public AudioClip outroClip;

        [Space(4)]
        [Tooltip("Enable pitch escalation — music speeds up every N plays (e.g. arena).")]
        public bool  pitchEscalation;
        [Tooltip("Starting pitch when escalation begins.")]
        [Range(0.5f, 2f)]  public float pitchBase;
        [Tooltip("Pitch added per step.")]
        [Range(0f, 0.5f)]  public float pitchStep;
        [Tooltip("How many plays before each pitch step.")]
        [Range(1, 20)]     public int   pitchStepEvery;
        [Tooltip("Pitch cap — never exceeds this value.")]
        [Range(0.5f, 4f)]  public float pitchMax;

        /// <summary>First clip in the playlist, or null if none assigned.</summary>
        public AudioClip FirstClip => (clips != null && clips.Length > 0) ? clips[0] : null;
    }

    /// <summary>
    /// Centralised audio manager.
    ///
    /// Music system:
    ///  - Each scene maps to one or more clips in sceneMusicMap. Multiple clips play in order,
    ///    cycling back to the first (playlist mode). A single clip loops as before.
    ///  - Loops are scheduled via AudioSettings.dspTime for sample-accurate gapless repetition.
    ///  - Scene transitions are QUEUED: the new clip always waits for the current loop's
    ///    natural end-point so musical phrases are never cut mid-bar.
    ///  - Only entries marked playImmediately bypass the queue and crossfade straight away.
    ///  - Crossfades use two AudioSources (A/B ping-pong) — simultaneous fade, no silence.
    ///  - Game/Arena supports optional pitch escalation configured per-entry.
    /// </summary>
    public class AudioController : PersistentSingleton<AudioController>
    {
        public static AudioController I => Instance;

        // ══════════════════════════════════════════════════════════════════════
        // Inspector — Music
        // ══════════════════════════════════════════════════════════════════════

        [Header("Music — Intro")]
        [Tooltip("Plays once at game start, then hands off to the first scene's loop.")]
        [SerializeField] private AudioClip introClip;

        [Header("Music — Scene Map")]
        [Tooltip("Maps each scene name to a music playlist. Includes Game/Arena.")]
        [SerializeField] private SceneMusicEntry[] sceneMusicMap;

        [Tooltip("How far ahead (seconds) to schedule the next loop iteration.")]
        [SerializeField, Range(0.1f, 2f)]  private float scheduleAheadSeconds = 0.5f;

        [Header("Music — Crossfade")]
        [Tooltip("Default duration when crossfading between scenes.")]
        [SerializeField, Range(0f, 5f)]    private float defaultCrossfadeSeconds = 1f;
        [Tooltip("Short fade-in at every loop boundary to prevent amplitude clicks. 0.05 s is inaudible.")]
        [SerializeField, Range(0f, 0.2f)]  private float loopCrossfadeSeconds   = 0.05f;

        // ══════════════════════════════════════════════════════════════════════
        // Inspector — Audio Sources
        // ══════════════════════════════════════════════════════════════════════

        [Header("Audio Sources")]
        [Tooltip("Assign the same AudioMixerGroup that MMSoundManager uses for its Music track.")]
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private AudioSource musicSource;   // Source A
        [SerializeField] private AudioSource musicSourceB;  // Source B — crossfade partner

        /// <summary>Length in seconds of the clip currently playing on the active music source. 0 if nothing is playing.</summary>
        public float ActiveClipLength
        {
            get
            {
                var clip  = ActiveMusicSrc?.clip;
                var pitch = ActiveMusicSrc?.pitch;
                return (clip != null && pitch.HasValue && pitch.Value > 0f)
                    ? clip.length / pitch.Value
                    : 0f;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Private state
        // ══════════════════════════════════════════════════════════════════════

        // Dual-source crossfade
        private bool _useA = true;
        private AudioSource ActiveMusicSrc   => _useA ? musicSource  : musicSourceB;
        private AudioSource InactiveMusicSrc => _useA ? musicSourceB : musicSource;
        private Coroutine   _crossfadeRoutine;

        // Gapless loop scheduler
        private AudioClip _loopClip;
        private float     _loopPitch;
        private float     _loopVolume;
        private double    _nextLoopDspTime;
        private bool      _isLooping;
        private bool      _loopScheduled;

        // Playlist (multi-clip sequence per scene)
        private AudioClip[] _playlistClips;
        private int         _playlistIndex;

        // Queued scene transition
        private AudioClip   _queuedClip;
        private float       _queuedPitch;
        private float       _queuedVolume;
        private float       _queuedFade;
        private AudioClip[] _queuedPlaylistClips;

        // Intro
        private bool _introPlayed;

        // One-shot completion
        /// <summary>Fired when a playOnce clip finishes playing naturally.</summary>
        public static event System.Action OnOneShotComplete;
        private bool _inOneShot;

        // Arena pitch escalation
        private SceneMusicEntry _arenaEntry;
        private int             _arenaMusicPlayCount;

        // Single-scene flow detection: when flow canvases exist in the active Unity scene,
        // we don't want SceneManager.sceneLoaded to drive music routing.
        private bool _singleSceneModeDetected;
        private bool _singleSceneMode;

        private bool IsSingleSceneFlowMode()
        {
            if (_singleSceneModeDetected)
                return _singleSceneMode;

            _singleSceneModeDetected = true;
            _singleSceneMode = FindObjectsByType<FlowCanvasRoot>(FindObjectsSortMode.None).Length > 0;
            return _singleSceneMode;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════════════

        protected override void Awake()
        {
            base.Awake();
            EnsureSource(ref musicSource,  "MusicA", loop: false, output: musicMixerGroup);
            EnsureSource(ref musicSourceB, "MusicB", loop: false, output: musicMixerGroup);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start() { }

        private void Update()
        {
            if (_inOneShot && !ActiveMusicSrc.isPlaying)
            {
                _inOneShot = false;
                OnOneShotComplete?.Invoke();
            }

            bool inArenaFlow =
                SceneFlowManager.I != null && SceneFlowManager.I.CurrentState == FlowState.Game;
            if (inArenaFlow)
            {
                if (!_isLooping && !ActiveMusicSrc.isPlaying)
                    PlayArenaMusic();
                return;
            }
            TickLoopScheduler();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Scene events
        // ══════════════════════════════════════════════════════════════════════

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsSingleSceneFlowMode())
                return;

            if (scene.name == "Game")
            {
                var entry = FindSceneMusic("Game");
                if (!entry.HasValue || entry.Value.FirstClip == null)
                {
                    UnityEngine.Debug.LogWarning("[AudioController] No 'Game' entry in sceneMusicMap — arena will be silent.");
                    CancelLoop();
                    StopAllMusicSources();
                    return;
                }
                // PreviewSceneMusic may have already started arena music during loading — don't restart it.
                if (ActiveMusicSrc.isPlaying && ActiveMusicSrc.clip == entry.Value.FirstClip)
                {
                    _arenaEntry = entry.Value;
                    return;
                }
                _arenaEntry          = entry.Value;
                _arenaMusicPlayCount = 0;
                CancelLoop();
                StopAllMusicSources();
                PlayArenaMusic();
                return;
            }

            SceneMusicEntry? found = FindSceneMusic(scene.name);
            if (found.HasValue)
                RequestSceneMusic(found.Value);
            else
                StopMusic();
        }

        /// <summary>
        /// Called by SceneFlowManager before LoadScene — starts the next scene's music
        /// immediately so there is no gap during scene loading.
        /// </summary>
        public void PreviewSceneMusic(string sceneName)
        {
            if (sceneName == "Game")
            {
                var entry = FindSceneMusic("Game");
                if (!entry.HasValue || entry.Value.FirstClip == null) return;
                _arenaEntry          = entry.Value;
                _arenaMusicPlayCount = 0;
                CancelLoop();
                StopAllMusicSources();
                PlayArenaMusic();
                return;
            }

            var found = FindSceneMusic(sceneName);
            if (found.HasValue)
                RequestSceneMusic(found.Value);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Gapless loop scheduler
        // ══════════════════════════════════════════════════════════════════════

        private void TickLoopScheduler()
        {
            if (!_isLooping || _loopClip == null) return;

            double timeUntilBoundary = _nextLoopDspTime - AudioSettings.dspTime;
            if (timeUntilBoundary > scheduleAheadSeconds) return;
            if (_loopScheduled) return;

            bool      hasQueued  = _queuedClip != null;
            AudioClip nextClip;
            float     nextPitch;
            float     nextVolume;
            float     nextFade;

            if (hasQueued)
            {
                nextClip   = _queuedClip;
                nextPitch  = _queuedPitch;
                nextVolume = _queuedVolume;
                nextFade   = _queuedFade;
            }
            else
            {
                nextClip   = AdvancePlaylist();
                nextPitch  = _loopPitch;
                nextVolume = _loopVolume;
                nextFade   = loopCrossfadeSeconds;
            }

            var incoming = InactiveMusicSrc;
            incoming.clip   = nextClip;
            incoming.pitch  = nextPitch;
            incoming.loop   = false;
            incoming.volume = 0f;
            incoming.PlayScheduled(_nextLoopDspTime);

            double clipDuration  = ClipDuration(nextClip, nextPitch);
            double boundary      = _nextLoopDspTime;
            _nextLoopDspTime    += clipDuration;
            _loopScheduled       = true;

            _useA = !_useA;
            InactiveMusicSrc.SetScheduledEndTime(boundary);

            if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
            _crossfadeRoutine = StartCoroutine(FadeInAtDspTime(incoming, nextFade, boundary, nextVolume));

            // Always track which clip is now the "current" loop
            _loopClip   = nextClip;
            _loopVolume = nextVolume;

            if (hasQueued)
            {
                _loopPitch           = nextPitch;
                _queuedClip          = null;
                _queuedPitch         = 1f;
                _queuedVolume        = 1f;
                _queuedFade          = 0f;
                // Switch to the queued playlist, starting at index 0
                _playlistClips       = _queuedPlaylistClips ?? new[] { nextClip };
                _playlistIndex       = 0;
                _queuedPlaylistClips = null;
            }

            StartCoroutine(ResetScheduleFlagAfter((float)clipDuration - scheduleAheadSeconds - 0.05f));
        }

        /// <summary>
        /// Advances the playlist index and returns the next clip to play.
        /// For a single-clip playlist, returns the same clip (normal loop).
        /// </summary>
        private AudioClip AdvancePlaylist()
        {
            if (_playlistClips == null || _playlistClips.Length <= 1)
                return _loopClip;
            _playlistIndex = (_playlistIndex + 1) % _playlistClips.Length;
            return _playlistClips[_playlistIndex] ?? _loopClip;
        }

        private IEnumerator FadeInAtDspTime(AudioSource src, float fadeSeconds, double dspStartTime, float targetVol = 1f)
        {
            while (AudioSettings.dspTime < dspStartTime) yield return null;

            float elapsed    = 0f;
            float actualFade = Mathf.Max(fadeSeconds, 0.01f);
            while (elapsed < actualFade)
            {
                elapsed    += Time.unscaledDeltaTime;
                src.volume  = Mathf.Lerp(0f, targetVol, elapsed / actualFade);
                yield return null;
            }
            src.volume        = targetVol;
            _crossfadeRoutine = null;
        }

        private IEnumerator ResetScheduleFlagAfter(float seconds)
        {
            if (seconds > 0f) yield return new WaitForSecondsRealtime(seconds);
            _loopScheduled = false;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Scene music routing
        // ══════════════════════════════════════════════════════════════════════

        private void RequestSceneMusic(SceneMusicEntry entry)
        {
            var clips = entry.clips;
            if (clips == null || clips.Length == 0 || clips[0] == null) return;

            float pitch  = entry.pitch  > 0f ? entry.pitch  : 1f;
            float vol    = entry.volume > 0f ? entry.volume : 1f;
            float fade   = entry.crossfadeSeconds > 0f ? entry.crossfadeSeconds : defaultCrossfadeSeconds;

            if (introClip != null && !_introPlayed)
            {
                _introPlayed = true;
                StartIntroThenLoop(introClip, clips, pitch, vol);
                return;
            }

            if (entry.playOnce)
            {
                StartOneShot(clips[0], pitch, vol, fade);
                return;
            }

            if (!_isLooping || _loopClip == null)
            {
                StartPlaylist(clips, pitch, vol, fade);
                return;
            }

            // Guard: already playing this exact playlist
            if (_playlistClips == clips) return;

            if (entry.playImmediately)
            {
                StartPlaylist(clips, pitch, vol, fade);
                return;
            }

            // Always wait for the natural loop boundary — only playImmediately can interrupt
            _queuedClip          = clips[0];
            _queuedPitch         = pitch;
            _queuedVolume        = vol;
            _queuedFade          = fade;
            _queuedPlaylistClips = clips;
        }

        private void StartPlaylist(AudioClip[] clips, float pitch, float vol, float fadeSeconds)
        {
            _playlistClips = clips;
            _playlistIndex = 0;
            StartLoop(clips[0], pitch, vol, fadeSeconds);
        }

        private void StartLoop(AudioClip clip, float pitch, float vol, float fadeSeconds)
        {
            if (clip == null) return;

            if (_crossfadeRoutine != null) { StopCoroutine(_crossfadeRoutine); _crossfadeRoutine = null; }

            _queuedClip    = null;
            _loopClip      = clip;
            _loopPitch     = pitch;
            _loopVolume    = vol;
            _loopScheduled = false;

            var outgoing = ActiveMusicSrc;
            var incoming = InactiveMusicSrc;

            double startTime = AudioSettings.dspTime + 0.05;
            incoming.clip   = clip;
            incoming.pitch  = pitch;
            incoming.loop   = false;
            incoming.volume = 0f;
            incoming.PlayScheduled(startTime);

            _nextLoopDspTime = startTime + ClipDuration(clip, pitch);
            _isLooping       = true;
            _useA            = !_useA;

            _crossfadeRoutine = StartCoroutine(CrossfadeVolumes(outgoing, incoming, fadeSeconds, vol));
        }

        private void StartIntroThenLoop(AudioClip intro, AudioClip[] clips, float pitch, float vol)
        {
            CancelLoop();
            _playlistClips = clips;
            _playlistIndex = 0;

            double introStart    = AudioSettings.dspTime + 0.05;
            double introDuration = ClipDuration(intro, pitch);
            double loopStart     = introStart + introDuration;

            var srcA = ActiveMusicSrc;
            srcA.clip   = intro;
            srcA.pitch  = pitch;
            srcA.loop   = false;
            srcA.volume = vol;
            srcA.PlayScheduled(introStart);
            srcA.SetScheduledEndTime(loopStart);

            var srcB = InactiveMusicSrc;
            srcB.clip   = clips[0];
            srcB.pitch  = pitch;
            srcB.loop   = false;
            srcB.volume = vol;
            srcB.PlayScheduled(loopStart);

            _useA            = !_useA;
            _loopClip        = clips[0];
            _loopPitch       = pitch;
            _loopVolume      = vol;
            _nextLoopDspTime = loopStart + ClipDuration(clips[0], pitch);
            _isLooping       = true;
            _loopScheduled   = false;
        }

        /// <summary>Crossfades to a clip and plays it once — no loop scheduling.
        /// Fires <see cref="OnOneShotComplete"/> when the clip finishes naturally.</summary>
        private void StartOneShot(AudioClip clip, float pitch, float vol, float fadeSeconds)
        {
            if (clip == null) return;

            if (_crossfadeRoutine != null) { StopCoroutine(_crossfadeRoutine); _crossfadeRoutine = null; }
            CancelLoop();

            var outgoing = ActiveMusicSrc;
            var incoming = InactiveMusicSrc;

            incoming.clip   = clip;
            incoming.pitch  = pitch;
            incoming.loop   = false;
            incoming.volume = 0f;
            incoming.Play();

            _useA             = !_useA;
            _inOneShot        = true;
            _crossfadeRoutine = StartCoroutine(CrossfadeVolumes(outgoing, incoming, fadeSeconds, vol));
        }

        private void CancelLoop()
        {
            _isLooping           = false;
            _loopScheduled       = false;
            _loopClip            = null;
            _queuedClip          = null;
            _playlistClips       = null;
            _playlistIndex       = 0;
            _queuedPlaylistClips = null;
            if (_crossfadeRoutine != null) { StopCoroutine(_crossfadeRoutine); _crossfadeRoutine = null; }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Public — Music
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Arena pitch-escalation loop — called by Update() when in Game scene.</summary>
        public void PlayArenaMusic()
        {
            var clip = _arenaEntry.FirstClip;
            if (ActiveMusicSrc == null || clip == null) return;

            ActiveMusicSrc.loop = false;
            _arenaMusicPlayCount++;

            float pitch = _arenaEntry.pitch > 0f ? _arenaEntry.pitch : 1f;

            if (_arenaEntry.pitchEscalation && _arenaEntry.pitchStep > 0f && _arenaEntry.pitchStepEvery > 0)
            {
                float pitchBase = _arenaEntry.pitchBase > 0f ? _arenaEntry.pitchBase : 1f;
                float pitchMax  = _arenaEntry.pitchMax  > 0f ? _arenaEntry.pitchMax  : pitchBase;
                int   cycles    = (_arenaMusicPlayCount - 1) / _arenaEntry.pitchStepEvery;
                pitch = Mathf.Min(pitchBase + cycles * _arenaEntry.pitchStep, pitchMax);
            }

            ActiveMusicSrc.pitch = pitch;
            UnityEngine.Debug.Log($"[ArenaMusic] play={_arenaMusicPlayCount} pitch={pitch:F2}");

            ActiveMusicSrc.clip   = clip;
            ActiveMusicSrc.volume = _arenaEntry.volume > 0f ? _arenaEntry.volume : 1f;
            ActiveMusicSrc.Play();
        }

        /// <summary>
        /// Crossfades from the running arena loop into the outro clip (plays once, no loop).
        /// Fires <see cref="OnOneShotComplete"/> when the outro finishes.
        /// </summary>
        /// <param name="crossfadeSeconds">Override crossfade duration. 0 = use defaultCrossfadeSeconds.</param>
        public void PlayArenaOutro(float crossfadeSeconds = 0f)
        {
            var clip = _arenaEntry.outroClip;
            if (clip == null)
            {
                UnityEngine.Debug.LogWarning("[AudioController] PlayArenaOutro called but no outroClip is set on the Game scene entry.");
                return;
            }
            float pitch = _arenaEntry.pitch > 0f ? _arenaEntry.pitch : 1f;
            float vol   = _arenaEntry.volume > 0f ? _arenaEntry.volume : 1f;
            float fade  = crossfadeSeconds > 0f ? crossfadeSeconds : defaultCrossfadeSeconds;
            StartOneShot(clip, pitch, vol, fade);
        }

        public void StopMusic()
        {
            CancelLoop();
            StopAllMusicSources();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Internals
        // ══════════════════════════════════════════════════════════════════════

        private SceneMusicEntry? FindSceneMusic(string sceneName)
        {
            if (sceneMusicMap == null) return null;
            foreach (var e in sceneMusicMap)
                if (e.sceneName == sceneName && e.clips != null && e.clips.Length > 0 && e.clips[0] != null)
                    return e;
            return null;
        }

        private static double ClipDuration(AudioClip clip, float pitch)
            => clip.samples / (double)(clip.frequency * Mathf.Max(pitch, 0.01f));

        private IEnumerator CrossfadeVolumes(AudioSource outgoing, AudioSource incoming, float seconds, float targetVol = 1f)
        {
            float startVol = outgoing.volume;
            float t        = 0f;
            if (seconds < 0.05f)
            {
                outgoing.volume = 0f; outgoing.Stop(); outgoing.clip = null;
                incoming.volume = targetVol;
                _crossfadeRoutine = null;
                yield break;
            }
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float pct       = Mathf.Clamp01(t / seconds);
                outgoing.volume = Mathf.Lerp(startVol,  0f,        pct);
                incoming.volume = Mathf.Lerp(0f,        targetVol, pct);
                yield return null;
            }
            outgoing.volume   = 0f;
            outgoing.Stop();
            outgoing.clip     = null;
            incoming.volume   = targetVol;
            _crossfadeRoutine = null;
        }

        private void StopAllMusicSources()
        {
            StopSource(musicSource);
            StopSource(musicSourceB);
        }

        private static void StopSource(AudioSource src)
        {
            if (src == null) return;
            src.Stop();
            src.clip = null;
        }

        private void EnsureSource(ref AudioSource src, string goName, bool loop, AudioMixerGroup output)
        {
            if (src == null)
            {
                var child = new GameObject($"Audio_{goName}");
                child.transform.SetParent(transform);
                src = child.AddComponent<AudioSource>();
            }
            src.playOnAwake           = false;
            src.loop                  = loop;
            src.outputAudioMixerGroup = output;
            src.spatialBlend          = 0f;
        }

    }
}
