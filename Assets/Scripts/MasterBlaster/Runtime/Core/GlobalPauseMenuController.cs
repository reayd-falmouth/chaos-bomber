using System;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Core
{
    [DisallowMultipleComponent]
    public sealed class GlobalPauseMenuController : MonoBehaviour
    {
        const string PrefMasterInt = "VolMasterInt";
        const string PrefMusicInt = "VolMusicInt";
        const string PrefSfxInt = "VolSfxInt";
        const string PrefScanlines = "Scanlines";
        public const string PrefOnlinePlay = "OnlinePlay";
        const int VolumeMax = 10;

        [Serializable]
        public class MenuOption
        {
            public Text pointerText;
            public Text optionLabel;
            public Text valueLabel;
        }

        [Header("Root")]
        [SerializeField] GameObject pausePanel;
        [SerializeField] Volume globalVolume;

        [Header("Menu Setup")]
        [Tooltip("Assign 7 rows: Master, SFX, Music, Online, Scanlines, Resume, Quit.")]
        [SerializeField] MenuOption[] options;

        [Header("Input")]
        [SerializeField] InputActionAsset inputActions;

        private static GlobalPauseMenuController _instance;
        public static bool IsPaused => _instance != null && _instance._paused;
        public static bool WasClosedThisFrame { get; private set; }

        private int _selectedIndex;
        private int _masterVolInt, _musicVolInt, _sfxVolInt;
        private bool _onlinePlay, _scanlines, _paused;
        private bool _moveWasEnabled, _submitWasEnabled;
        private Vector2 _lastMoveInput;
        private InputAction _moveAction, _submitAction;

        void Awake()
        {
            _instance = this;
            LoadPrefs();
            ApplyScanlines();
            var playerMap = inputActions.FindActionMap("Player");
            _moveAction = playerMap?.FindAction("Move");
            _submitAction = playerMap?.FindAction("PlaceBomb");

            if (pausePanel) pausePanel.SetActive(false);
        }

        void Start()
        {
            // ApplyVolumes() is called here instead of Awake() so that MMSoundManager
            // has completed its OnEnable() and registered its event listeners before
            // MMSoundManagerTrackEvent.Trigger() fires. (Awake → OnEnable → Start)
            ApplyVolumes();
        }

        void Update()
        {
            WasClosedThisFrame = false;
            if (CanTogglePauseMenu() && WasGlobalPausePressedThisFrame())
                TogglePause();

            if (_paused) UpdateNavigation();
        }

        void TogglePause() { if (_paused) Resume(); else Pause(); }

        void Pause()
        {
            _paused = true;
            pausePanel.SetActive(true);
            MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, 0f, 0f, false, 0f, true);
            ApplyVolumes();
            _selectedIndex = 0;
            UpdateMenuUI();
            _moveWasEnabled = _moveAction is { enabled: true };
            _submitWasEnabled = _submitAction is { enabled: true };
            _moveAction?.Enable();
            _submitAction?.Enable();
        }

        public void Resume()
        {
            WasClosedThisFrame = true;
            _paused = false;
            MMTimeScaleEvent.Unfreeze();
            pausePanel.SetActive(false);
            SavePrefs();
            if (!_moveWasEnabled) _moveAction?.Disable();
            if (!_submitWasEnabled) _submitAction?.Disable();
        }

        void UpdateNavigation()
        {
            if (options == null || options.Length == 0) return;
            Vector2 moveInput = _moveAction.ReadValue<Vector2>();

            // Vertical - Up/Down
            if (moveInput.y < -0.5f && _lastMoveInput.y >= -0.5f) { _selectedIndex = (_selectedIndex + 1) % options.Length; UpdatePointers(); }
            else if (moveInput.y > 0.5f && _lastMoveInput.y <= 0.5f) { _selectedIndex = (_selectedIndex - 1 + options.Length) % options.Length; UpdatePointers(); }

            // Horizontal - Change Values
            if (moveInput.x < -0.5f && _lastMoveInput.x >= -0.5f) { ChangeValue(false); }
            else if (moveInput.x > 0.5f && _lastMoveInput.x <= 0.5f) { ChangeValue(true); }

            if (_submitAction.WasPressedThisFrame()) HandleSubmit();
            _lastMoveInput = moveInput;
        }

        void ChangeValue(bool increase)
        {
            int delta = increase ? 1 : -1;
            switch (_selectedIndex)
            {
                case 0: _masterVolInt = Mathf.Clamp(_masterVolInt + delta, 0, VolumeMax); break;
                case 1: _sfxVolInt = Mathf.Clamp(_sfxVolInt + delta, 0, VolumeMax); break; // SFX
                case 2: _musicVolInt = Mathf.Clamp(_musicVolInt + delta, 0, VolumeMax); break;
                case 3: _onlinePlay = !_onlinePlay; break;
                case 4: _scanlines = !_scanlines; ApplyScanlines(); break;
            }
            ApplyVolumes();
            UpdateMenuUI();
        }

        void HandleSubmit()
        {
            if (_selectedIndex == 5) QuitToMenu(); // QUIT row
        }

        void UpdateMenuUI()
        {
            if (options.Length < 5) return;
            options[0].valueLabel.text = _masterVolInt.ToString();
            options[1].valueLabel.text = _sfxVolInt.ToString();
            options[2].valueLabel.text = _musicVolInt.ToString();
            options[3].valueLabel.text = _onlinePlay ? "YES" : "NO";
            options[4].valueLabel.text = _scanlines ? "ON" : "OFF";
            UpdatePointers();
        }

        void UpdatePointers()
        {
            for (int i = 0; i < options.Length; i++)
                if (options[i].pointerText != null) options[i].pointerText.text = (i == _selectedIndex) ? "> " : "  ";
        }

        // ── Logic Helpers ─────────────────────────────────────────────────────
        void LoadPrefs()
        {
            _masterVolInt = PlayerPrefs.GetInt(PrefMasterInt, 10);
            _musicVolInt = PlayerPrefs.GetInt(PrefMusicInt, 10);
            _sfxVolInt = PlayerPrefs.GetInt(PrefSfxInt, 10);
            _onlinePlay = PlayerPrefs.GetInt(PrefOnlinePlay, 1) == 1;
            _scanlines = PlayerPrefs.GetInt(PrefScanlines, 1) == 1;
        }

        void SavePrefs()
        {
            PlayerPrefs.SetInt(PrefMasterInt, _masterVolInt);
            PlayerPrefs.SetInt(PrefMusicInt, _musicVolInt);
            PlayerPrefs.SetInt(PrefSfxInt, _sfxVolInt);
            PlayerPrefs.SetInt(PrefOnlinePlay, _onlinePlay ? 1 : 0);
            PlayerPrefs.SetInt(PrefScanlines, _scanlines ? 1 : 0);
            PlayerPrefs.Save();
        }

        void ApplyScanlines()
        {
            if (globalVolume != null) globalVolume.enabled = _scanlines;
        }

        void ApplyVolumes()
        {
            AudioListener.volume = _masterVolInt / 10f;
            MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.SetVolumeTrack, MMSoundManager.MMSoundManagerTracks.Music, _musicVolInt / 10f);
            MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.SetVolumeTrack, MMSoundManager.MMSoundManagerTracks.Sfx, _sfxVolInt / 10f);
        }

        static void ApplySavedVolumesFromPrefs()
        {
            float master = Mathf.Clamp01(PlayerPrefs.GetInt(PrefMasterInt, 10) / 10f);
            float music = Mathf.Clamp01(PlayerPrefs.GetInt(PrefMusicInt, 10) / 10f);
            float sfx = Mathf.Clamp01(PlayerPrefs.GetInt(PrefSfxInt, 10) / 10f);

            AudioListener.volume = master;
            MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.SetVolumeTrack, MMSoundManager.MMSoundManagerTracks.Music, music);
            MMSoundManagerTrackEvent.Trigger(MMSoundManagerTrackEventTypes.SetVolumeTrack, MMSoundManager.MMSoundManagerTracks.Sfx, sfx);
        }

        void QuitToMenu()
        {
            GameManager.Instance?.CancelInvoke(); // Cancel pending Standings/round-end Invoke before resuming.
            var sfm = SceneFlowManager.I;
            UnityEngine.Debug.Log($"[PauseMenu] QuitToMenu called. SceneFlowManager.I={sfm}, singleSceneMode={sfm?.IsSingleSceneMode}");
            Resume();
            SessionManager.Instance?.ResetSession();
            if (sfm != null)
                sfm.GoTo(FlowState.Menu);
            else
            {
                UnityEngine.Debug.LogWarning("[PauseMenu] SceneFlowManager not found — falling back to direct scene load.");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
            }
        }

        static bool CanTogglePauseMenu() => !TrainingMode.IsActive && !(NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);
        static bool WasGlobalPausePressedThisFrame() {
            if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame)) return true;
            foreach (var pad in Gamepad.all) if (pad.startButton.wasPressedThisFrame) return true;
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() {
            // Apply persisted audio levels globally even if the pause menu UI remains inactive.
            ApplySavedVolumesFromPrefs();
            if (FindAnyObjectByType<GlobalPauseMenuController>() != null) return;
            var prefab = Resources.Load<GameObject>("GlobalPauseMenu");
            if (prefab) DontDestroyOnLoad(Instantiate(prefab));
        }
    }
}