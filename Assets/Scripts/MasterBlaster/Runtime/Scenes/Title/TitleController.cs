using System;
using HybridGame.MasterBlaster.Runtime.Scenes.Character;
using HybridGame.MasterBlaster.Scripts.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Title
{
    public class TitleController : MonoBehaviour
    {
        [System.Serializable]
        public class MenuOption
        {
            public Text pointerText;
            public Text optionLabel;
        }

        public String pointCharacter = "<";
        public MenuOption[] options;

        [Header("Input Setup")]
        [Tooltip("Assign UIMenus or PlayerControls Input Action Asset in Inspector.")]
        public InputActionAsset inputActions;

        private InputAction _moveAction;
        private InputAction _submitAction;
        private Vector2 _lastMoveInput;

        private int selectedIndex;

        private void Awake()
        {
            if (inputActions == null)
            {
                UnityEngine.Debug.LogWarning("[MainMenuController] InputActionAsset not assigned. Assign UIMenus (or PlayerControls) in Inspector.");
                return;
            }
            var playerMap = inputActions.FindActionMap("Player");
            _moveAction = playerMap.FindAction("Move");
            _submitAction = playerMap.FindAction("PlaceBomb");
        }

        private void OnEnable()
        {
            if (_moveAction != null)
                _moveAction.Enable();
            if (_submitAction != null)
                _submitAction.Enable();

            UpdatePointers();
        }

        private void OnDisable()
        {
            if (_moveAction != null)
                _moveAction.Disable();
            if (_submitAction != null)
                _submitAction.Disable();
        }

        private void Update()
        {
            if (SceneFlowManager.I != null && SceneFlowManager.I.IsTransitioning)
                return;

            if (_moveAction == null || _submitAction == null)
                return;

            if (options == null || options.Length == 0)
                return;

            Vector2 moveInput = _moveAction.ReadValue<Vector2>();

            if (moveInput.y < -0.5f && _lastMoveInput.y >= -0.5f)
            {
                selectedIndex = (selectedIndex + 1) % options.Length;
                UpdatePointers();
            }
            else if (moveInput.y > 0.5f && _lastMoveInput.y <= 0.5f)
            {
                selectedIndex = (selectedIndex - 1 + options.Length) % options.Length;
                UpdatePointers();
            }

            if (_submitAction.WasPressedThisFrame() && !GlobalPauseMenuController.IsPaused && !GlobalPauseMenuController.WasClosedThisFrame)
                HandleSubmit();

            _lastMoveInput = moveInput;
        }

        [Header("Online")]
        [Tooltip("Assign the OnlineLobbyUI panel GameObject. When Online is selected, this panel activates instead of starting locally.")]
        [SerializeField] private GameObject onlineLobbyPanel;

        private void HandleSubmit()
        {
            switch (selectedIndex)
            {
                // 0) Play Online
                case 0:
                    SceneFlowManager.I.GoTo(FlowState.AvatarSelect);
                    break;

                // 1) Play Local
                case 1:
                    PrepareNewLocalGameSessionFromPrefs();
                    SceneFlowManager.I.GoTo(FlowState.Menu);
                    break;

                // 2) Settings (main menu)
                case 2:
                    SceneFlowManager.I.GoTo(FlowState.Settings);
                    break;

                // 3) Credits
                case 3:
                    SceneFlowManager.I.GoTo(FlowState.Credits);
                    break;

                default:
                    UnityEngine.Debug.LogWarning($"[Title] Unknown menu index {selectedIndex}. No action.");
                    break;
            }
        }

        private static void PrepareNewLocalGameSessionFromPrefs()
        {
            // Keep whatever settings were last chosen in Settings/Menu (or defaults).
            // Title's job is just to start a new match using current prefs.
            int players = Mathf.Clamp(PlayerPrefs.GetInt("Players", 2), 2, 5);

            // Always reset session (wins, coins, upgrades) when starting a new game from the title.
            SessionManager.Instance.Initialize(players);

            // Local match: no avatar-select starting perk (inspector loadout only).
            PlayerPrefs.SetInt(AvatarSelectionPrefs.ApplyAvatarStartingPerkNextGameKey, 0);

            // Single-scene mode: mark that the next time we enter Game, we should fully reinitialize player state and arena.
            PlayerPrefs.SetInt("NewGamePending", 1);
            PlayerPrefs.SetInt("GiveStartMoneyNextArena", 1);
            PlayerPrefs.Save();
        }

        private void UpdatePointers()
        {
            if (options == null)
                return;
            for (int i = 0; i < options.Length; i++)
                options[i].pointerText.text = (i == selectedIndex) ? pointCharacter : " ";
        }
    }
}
