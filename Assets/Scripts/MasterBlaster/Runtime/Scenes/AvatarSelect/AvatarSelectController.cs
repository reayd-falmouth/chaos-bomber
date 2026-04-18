using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Mobile;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.AvatarSelect
{
    public class AvatarSelectController : MonoBehaviour, IFlowScreen
    {
        public const string SelectedAvatarPrefsKey = "SelectedAvatar";

        [System.Serializable]
        public class AvatarOption
        {
            [Tooltip("Value persisted to PlayerPrefs when this option is confirmed.")]
            public int avatarId;
            public Text pointerText;
            public Text optionLabel;
        }

        [Header("UI")]
        public string pointCharacter = "> ";
        [Tooltip("Last row can be used for BACK (recommended).")]
        public AvatarOption[] options;

        [Header("Input Setup")]
        [Tooltip("Assign UIMenus or PlayerControls Input Action Asset in Inspector.")]
        public InputActionAsset inputActions;

        private InputAction _moveAction;
        private InputAction _submitAction;
        private Vector2 _lastMoveInput;
        private int _selectedIndex;

        private void Awake()
        {
            if (inputActions == null)
            {
                UnityEngine.Debug.LogWarning("[AvatarSelectController] InputActionAsset not assigned.");
                return;
            }
            var playerMap = inputActions.FindActionMap("Player");
            _moveAction = playerMap.FindAction("Move");
            _submitAction = playerMap.FindAction("PlaceBomb");
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _submitAction?.Enable();

            _mobileBombHeldLastFrame = false;
            SyncSelectedIndexFromPrefs();
            UpdatePointers();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _submitAction?.Disable();
        }

        public void OnFlowPresented()
        {
            SyncSelectedIndexFromPrefs();
            UpdatePointers();
        }

        public void OnFlowDismissed() { }

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
                _selectedIndex = (_selectedIndex + 1) % options.Length;
                UpdatePointers();
            }
            else if (moveInput.y > 0.5f && _lastMoveInput.y <= 0.5f)
            {
                _selectedIndex = (_selectedIndex - 1 + options.Length) % options.Length;
                UpdatePointers();
            }

            if (MobileMenuInputBridge.SubmitPressedThisFrame(_submitAction, ref _mobileBombHeldLastFrame)
                && !GlobalPauseMenuController.IsPaused
                && !GlobalPauseMenuController.WasClosedThisFrame)
                HandleSubmit();

            _lastMoveInput = moveInput;
        }

        private void HandleSubmit()
        {
            // Convention: last row is BACK (returns to Title).
            if (options != null && options.Length > 0 && _selectedIndex == options.Length - 1)
            {
                SceneFlowManager.I.GoTo(FlowState.Title);
                return;
            }

            var opt = options[_selectedIndex];
            PlayerPrefs.SetInt(SelectedAvatarPrefsKey, opt.avatarId);
            PlayerPrefs.Save();
            SceneFlowManager.I.GoTo(FlowState.LevelSelect);
        }

        private void SyncSelectedIndexFromPrefs()
        {
            if (options == null || options.Length == 0)
            {
                _selectedIndex = 0;
                return;
            }

            int avatarId = PlayerPrefs.GetInt(SelectedAvatarPrefsKey, options[0].avatarId);
            int found = 0;
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] != null && options[i].avatarId == avatarId)
                {
                    found = i;
                    break;
                }
            }
            _selectedIndex = Mathf.Clamp(found, 0, options.Length - 1);
        }

        private void UpdatePointers()
        {
            if (options == null)
                return;
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i]?.pointerText != null)
                    options[i].pointerText.text = (i == _selectedIndex) ? pointCharacter : "  ";
            }
        }
    }
}

