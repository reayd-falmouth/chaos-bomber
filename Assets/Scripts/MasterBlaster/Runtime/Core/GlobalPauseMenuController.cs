using System;
using System.Reflection;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
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
        const int VolumeMax = 10;

        [Serializable]
        public class MenuOption
        {
            public Text pointerText;
            public Text optionLabel;
            public Text valueLabel;
        }

        [Header("Mode Configuration")]
        [Tooltip("True = In-game Pause (listens for Escape, stops time, exits to title). False = Main Menu Settings (acts as a UI panel, saves and closes).")]
        [SerializeField] private bool isPauseMenu = true;

        [Header("Root")]
        [FormerlySerializedAs("pausePanel")]
        [SerializeField] GameObject menuPanel;
        [SerializeField] Volume globalVolume;
        [SerializeField] GameObject controlsPanel;

        [Header("Visuals")]
        [FormerlySerializedAs("pauseMenuTitleText")]
        [SerializeField] Text menuTitleText;
        [FormerlySerializedAs("pauseBackgroundImage")]
        [SerializeField] Image backgroundImage;
        [SerializeField] string settingsTitle = "SETTINGS\n\n------";
        [SerializeField] string pauseTitle = "PAUSED\n\n------";

        [Header("Menu Setup")]
        [Tooltip("Assign rows: Master, SFX, Music, Scanlines, Controls, Exit/Back (6 entries).")]
        [SerializeField] MenuOption[] options;

        [Header("Input")]
        [SerializeField] InputActionAsset inputActions;

        private static GlobalPauseMenuController _instance;
        public static bool IsPaused => _instance != null && _instance._active;
        public static GlobalPauseMenuController I => _instance;
        public static bool WasClosedThisFrame { get; private set; }

        private int _selectedIndex;
        private int _masterVolInt, _musicVolInt, _sfxVolInt;
        private bool _scanlines, _active;
        private bool _showingControls;
        private bool _moveWasEnabled, _submitWasEnabled;
        private Vector2 _lastMoveInput;
        private InputAction _moveAction, _submitAction;

        // ── Static Helpers (Kept for Unit Test Compatibility) ───────────────
        public static string ResolveTitle(string originalTitle, string settingsTitleText)
        {
            return !string.IsNullOrWhiteSpace(settingsTitleText) ? settingsTitleText : originalTitle;
        }

        public static Sprite ResolveBackgroundSprite(Sprite originalSprite, Sprite settingsBackgroundSprite)
        {
            return settingsBackgroundSprite != null ? settingsBackgroundSprite : originalSprite;
        }

        public static (int showControlsIndex, int exitIndex, int valueRowCount) ComputeMenuIndices(int optionsLength)
        {
            // Layout: [value rows...][SHOW_CONTROLS][EXIT]
            int exitIndex = optionsLength - 1;
            int showControlsIndex = optionsLength - 2;
            int valueRowCount = Mathf.Max(0, showControlsIndex);
            return (showControlsIndex, exitIndex, valueRowCount);
        }

        // ── Core Unity Methods ────────────────────────────────────────────────
        void Awake()
        {
            _instance = this;
            LoadPrefs();
            ApplyScanlines();
            
            var playerMap = inputActions.FindActionMap("Player");
            _moveAction = playerMap?.FindAction("Move");
            _submitAction = playerMap?.FindAction("PlaceBomb");

            if (menuTitleText != null)
                menuTitleText.text = isPauseMenu ? pauseTitle : settingsTitle;

            // Only force the panels off if this is acting as a Pause Menu
            if (isPauseMenu)
            {
                if (menuPanel) menuPanel.SetActive(false);
                if (controlsPanel) controlsPanel.SetActive(false);
            }
        }

        void OnEnable()
        {
            // If this is acting as a Settings Menu, automatically open it when the GameObject is enabled
            if (!isPauseMenu)
            {
                OpenMenu();
            }
        }

        void Start()
        {
            ApplyVolumes();
        }

        void Update()
        {
            WasClosedThisFrame = false;

            // Only listen for the pause button if this instance is acting as the Global Pause Menu
            if (isPauseMenu && CanTogglePauseMenu() && WasGlobalPausePressedThisFrame())
            {
                if (_active) Resume(); else OpenMenu();
            }

            if (_active)
            {
                HandleNavigation();
            }
        }

        public void OpenMenu()
        {
            // Re-load prefs so the settings screen reflects any external changes 
            LoadPrefs();
            ApplyVolumes();
            ApplyScanlines();

            _active = true;
            if (menuPanel) menuPanel.SetActive(true);
            _showingControls = false;
            
            if (controlsPanel) controlsPanel.SetActive(false);
            
            if (isPauseMenu)
            {
                MMTimeScaleEvent.Trigger(MMTimeScaleMethods.For, 0f, 0f, false, 0f, true);
            }
            
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
            _active = false;
            
            if (menuPanel) menuPanel.SetActive(false);
            SavePrefs();

            if (isPauseMenu)
            {
                MMTimeScaleEvent.Unfreeze();
            }

            // In Settings mode, closing the menu panel should probably also disable the parent object 
            // so it can be re-opened via OnEnable later.
            // if (!isPauseMenu)
            // {
            //     gameObject.SetActive(false);
            // }
        }

        void HandleNavigation()
        {
            if (options == null || options.Length == 0) return;

            // Controls overlay: submit closes it; no menu navigation while showing.
            if (_showingControls)
            {
                if (_submitAction != null && _submitAction.WasPressedThisFrame())
                {
                    ToggleControls(false);
                    UpdatePointers();
                }
                return;
            }

            Vector2 moveInput = _moveAction.ReadValue<Vector2>();

            // Vertical - Up/Down
            if (moveInput.y < -0.5f && _lastMoveInput.y >= -0.5f) { MovePointer(1); }
            else if (moveInput.y > 0.5f && _lastMoveInput.y <= 0.5f) { MovePointer(-1); }

            // Horizontal - Change Values
            if (moveInput.x < -0.5f && _lastMoveInput.x >= -0.5f) { ChangeValue(false); }
            else if (moveInput.x > 0.5f && _lastMoveInput.x <= 0.5f) { ChangeValue(true); }

            if (_submitAction != null && _submitAction.WasPressedThisFrame()) HandleSubmit();
            
            _lastMoveInput = moveInput;
        }

        void MovePointer(int direction)
        {
            _selectedIndex = (_selectedIndex + direction + options.Length) % options.Length;
            UpdatePointers();
        }

        void ChangeValue(bool increase)
        {
            if (options == null || options.Length == 0) return;

            var (_, _, valueRowCount) = ComputeMenuIndices(options.Length);
            if (_selectedIndex < 0 || _selectedIndex >= valueRowCount) return;

            int delta = increase ? 1 : -1;
            switch (_selectedIndex)
            {
                case 0: _masterVolInt = Mathf.Clamp(_masterVolInt + delta, 0, VolumeMax); break;
                case 1: _sfxVolInt = Mathf.Clamp(_sfxVolInt + delta, 0, VolumeMax); break;
                case 2: _musicVolInt = Mathf.Clamp(_musicVolInt + delta, 0, VolumeMax); break;
                case 3: _scanlines = !_scanlines; ApplyScanlines(); break;
            }
            ApplyVolumes();
            UpdateMenuUI();
        }

        void HandleSubmit()
        {
            if (options == null || options.Length < 2) return;

            var (showControlsIndex, exitIndex, _) = ComputeMenuIndices(options.Length);

            if (_selectedIndex == showControlsIndex)
            {
                ToggleControls(true);
            }
            else if (_selectedIndex == exitIndex)
            {
                if (isPauseMenu) 
                    ExitToTitle();
                else 
                    ExitSettingsToTitle(); // In settings mode, 'Exit' acts as a 'Back' button and returns to Title flow.
            }
        }

        void ExitSettingsToTitle()
        {
            // Settings mode is a UI panel overlay on the Title/menu flow.
            // We still need to tell SceneFlowManager to return to Title, otherwise the settings root remains active/selected.
            Resume();

            var sfm = SceneFlowManager.I;
            if (sfm != null)
                sfm.GoTo(FlowState.Title);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
        }

        void ToggleControls(bool show)
        {
            _showingControls = show;
            if (controlsPanel)
            {
                if (show) controlsPanel.transform.localScale = Vector3.one;
                controlsPanel.SetActive(show);
                if (show) controlsPanel.transform.localScale = Vector3.one;
            }
        }

        void UpdateMenuUI()
        {
            if (options == null || options.Length == 0) return;

            if (options.Length > 0 && options[0].valueLabel != null) options[0].valueLabel.text = _masterVolInt.ToString();
            if (options.Length > 1 && options[1].valueLabel != null) options[1].valueLabel.text = _sfxVolInt.ToString();
            if (options.Length > 2 && options[2].valueLabel != null) options[2].valueLabel.text = _musicVolInt.ToString();
            if (options.Length > 3 && options[3].valueLabel != null) options[3].valueLabel.text = _scanlines ? "ON" : "OFF";
            UpdatePointers();
        }

        void UpdatePointers()
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i].pointerText != null) 
                    options[i].pointerText.text = (i == _selectedIndex) ? "> " : "  ";
            }
        }

        // ── Logic Helpers ─────────────────────────────────────────────────────
        void LoadPrefs()
        {
            _masterVolInt = PlayerPrefs.GetInt(PrefMasterInt, 10);
            _musicVolInt = PlayerPrefs.GetInt(PrefMusicInt, 10);
            _sfxVolInt = PlayerPrefs.GetInt(PrefSfxInt, 10);
            _scanlines = PlayerPrefs.GetInt(PrefScanlines, 1) == 1;
        }

        void SavePrefs()
        {
            PlayerPrefs.SetInt(PrefMasterInt, _masterVolInt);
            PlayerPrefs.SetInt(PrefMusicInt, _musicVolInt);
            PlayerPrefs.SetInt(PrefSfxInt, _sfxVolInt);
            PlayerPrefs.SetInt(PrefScanlines, _scanlines ? 1 : 0);
            PlayerPrefs.Save();
        }

        void ApplyScanlines()
        {
            var vol = ResolveScanlinesVolume();
            if (vol == null)
            {
                UnityEngine.Debug.LogWarning("[PauseMenu] Cannot toggle scanlines: No Volume with ScanlinesVol found in the scene!");
                return;
            }

            var profile = vol.profile ?? vol.sharedProfile;
            if (profile == null || profile.components == null)
            {
                UnityEngine.Debug.LogWarning("[PauseMenu] Cannot toggle scanlines: Volume has no profile.");
                return;
            }

            for (int i = 0; i < profile.components.Count; i++)
            {
                var c = profile.components[i];
                if (c == null) continue;
                var t = c.GetType();
                if (t.FullName == "VolFx.ScanlinesVol" || t.Name == "ScanlinesVol")
                    TrySetVolumeComponentActive(c, _scanlines);
            }
        }

        /// <summary>
        /// Picks the <see cref="Volume"/> whose profile contains <c>VolFx.ScanlinesVol</c>.
        /// Uses the inspector-assigned <see cref="globalVolume"/> when it already has that component.
        /// </summary>
        Volume ResolveScanlinesVolume()
        {
            if (globalVolume != null)
            {
                var p = globalVolume.profile ?? globalVolume.sharedProfile;
                if (p != null && ProfileContainsScanlinesVol(p))
                    return globalVolume;
            }

            var found = FindVolumeWithScanlinesProfile();
            if (found != null)
                return found;

            return globalVolume;
        }

        static Volume FindVolumeWithScanlinesProfile()
        {
            var volumes = FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < volumes.Length; i++)
            {
                var v = volumes[i];
                if (v == null) continue;
                var profile = v.profile ?? v.sharedProfile;
                if (profile != null && ProfileContainsScanlinesVol(profile))
                    return v;
            }

            return null;
        }

        static bool ProfileContainsScanlinesVol(VolumeProfile profile)
        {
            if (profile?.components == null) return false;
            for (int i = 0; i < profile.components.Count; i++)
            {
                var c = profile.components[i];
                if (c == null) continue;
                var t = c.GetType();
                if (t.FullName == "VolFx.ScanlinesVol" || t.Name == "ScanlinesVol")
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Sets <c>active</c> on a volume profile component (e.g. <c>ScanlinesVol</c>) via reflection so this assembly does not reference VolFx types.
        /// </summary>
        static bool TrySetVolumeComponentActive(object volumeComponent, bool active)
        {
            if (volumeComponent == null) return false;

            var t = volumeComponent.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var prop = t.GetProperty("active", flags);
            if (prop != null && prop.PropertyType == typeof(bool) && prop.CanWrite)
            {
                try
                {
                    prop.SetValue(volumeComponent, active);
                    return true;
                }
                catch
                {
                    // fall through
                }
            }

            var field = t.GetField("active", flags);
            if (field != null && field.FieldType == typeof(bool))
            {
                try
                {
                    field.SetValue(volumeComponent, active);
                    return true;
                }
                catch
                {
                    // fall through
                }
            }

            field = t.GetField("m_Active", flags);
            if (field != null && field.FieldType == typeof(bool))
            {
                try
                {
                    field.SetValue(volumeComponent, active);
                    return true;
                }
                catch
                {
                    // ignored
                }
            }

            return false;
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

        void ExitToTitle()
        {
            GameManager.Instance?.CancelInvoke(); // Cancel pending Standings/round-end Invoke before resuming.
            var sfm = SceneFlowManager.I;
            UnityEngine.Debug.Log($"[PauseMenu] ExitToTitle called. SceneFlowManager.I={sfm}, singleSceneMode={sfm?.IsSingleSceneMode}");
            Resume();
            SessionManager.Instance?.ResetSession();
            
            if (sfm != null)
                sfm.GoTo(FlowState.Title);
            else
            {
                UnityEngine.Debug.LogWarning("[PauseMenu] SceneFlowManager not found — falling back to direct scene load.");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
            }
        }

        static bool CanTogglePauseMenu() => !TrainingMode.IsActive && !(NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);
        
        static bool WasGlobalPausePressedThisFrame() 
        {
            if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame)) return true;
            foreach (var pad in Gamepad.all) if (pad.startButton.wasPressedThisFrame) return true;
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() 
        {
            // Apply persisted audio levels globally even if the pause menu UI remains inactive.
            ApplySavedVolumesFromPrefs();
            if (FindAnyObjectByType<GlobalPauseMenuController>() != null) return;
            var prefab = Resources.Load<GameObject>("GlobalPauseMenu");
            if (prefab) DontDestroyOnLoad(Instantiate(prefab));
        }
    }
}