using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Scenes.AvatarSelect;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Runtime.Scenes.Character
{
    /// <summary>
    /// Select / Back menu for the avatar screen. Attach to the SelectMenu prefab root.
    /// Navigation uses <see cref="SceneFlowManager"/> only (no direct scene loads).
    /// </summary>
    public class AvatarSelectMenuController : MonoBehaviour
    {
        [Header("Binding")]
        [SerializeField]
        AvatarController avatarController;

        [Tooltip("Assign the same Input Action Asset as AvatarController (Player map: Move.y, PlaceBomb).")]
        [SerializeField]
        InputActionAsset inputActions;

        [Header("Pointers")]
        [SerializeField]
        string pointCharacter = "> ";

        [SerializeField]
        string inactivePointer = "  ";

        [SerializeField]
        Text selectPointerText;

        [SerializeField]
        Text backPointerText;

        [Header("Optional buttons")]
        [SerializeField]
        Button selectButton;

        [SerializeField]
        Button backButton;

        InputAction _moveAction;
        InputAction _submitAction;
        Vector2 _lastMoveInput;
        int _menuRow;

        /// <summary>Optional explicit bind when the menu is not parented under the avatar root.</summary>
        public void BindAvatarController(AvatarController controller) => avatarController = controller;

        /// <summary>PlayMode tests: assign references before the menu GameObject is enabled.</summary>
        public void ConfigureForTests(
            InputActionAsset actions,
            Text selectPtr,
            Text backPtr,
            Button selectBtn,
            Button backBtn)
        {
            inputActions = actions;
            selectPointerText = selectPtr;
            backPointerText = backPtr;
            selectButton = selectBtn;
            backButton = backBtn;
        }

        void Awake()
        {
            if (avatarController == null)
                avatarController = GetComponentInParent<AvatarController>();

            if (inputActions == null)
            {
                Debug.LogWarning("[AvatarSelectMenuController] InputActionAsset not assigned.");
                return;
            }

            var playerMap = inputActions.FindActionMap("Player");
            if (playerMap == null)
            {
                Debug.LogWarning("[AvatarSelectMenuController] Could not find action map 'Player'.");
                return;
            }

            _moveAction = playerMap.FindAction("Move");
            _submitAction = playerMap.FindAction("PlaceBomb");
        }

        void OnEnable()
        {
            _moveAction?.Enable();
            _submitAction?.Enable();
            WireSelectButton(true);
            WireBackButton(true);
            UpdatePointers();
        }

        void OnDisable()
        {
            _moveAction?.Disable();
            _submitAction?.Disable();
            WireSelectButton(false);
            WireBackButton(false);
        }

        void WireSelectButton(bool add)
        {
            if (selectButton == null)
                return;
            if (add)
                selectButton.onClick.AddListener(OnSelectClicked);
            else
                selectButton.onClick.RemoveListener(OnSelectClicked);
        }

        void WireBackButton(bool add)
        {
            if (backButton == null)
                return;
            if (add)
                backButton.onClick.AddListener(OnBackClicked);
            else
                backButton.onClick.RemoveListener(OnBackClicked);
        }

        void Update()
        {
            if (_moveAction == null || _submitAction == null)
                return;

            if (!CanProcessMenuInput())
                return;

            Vector2 moveInput = _moveAction.ReadValue<Vector2>();

            if (moveInput.y < -0.5f && _lastMoveInput.y >= -0.5f)
            {
                _menuRow = (_menuRow + 1) % 2;
                UpdatePointers();
            }
            else if (moveInput.y > 0.5f && _lastMoveInput.y <= 0.5f)
            {
                _menuRow = (_menuRow - 1 + 2) % 2;
                UpdatePointers();
            }

            if (_submitAction.WasPressedThisFrame())
            {
                if (_menuRow == 0)
                    TryConfirmSelection();
                else
                    TryGoBack();
            }

            _lastMoveInput = moveInput;
        }

        /// <summary>For Inspector button OnClick.</summary>
        public void OnSelectClicked()
        {
            if (!CanProcessMenuInput())
                return;
            _menuRow = 0;
            UpdatePointers();
            TryConfirmSelection();
        }

        /// <summary>For Inspector button OnClick.</summary>
        public void OnBackClicked()
        {
            if (!CanProcessMenuInput())
                return;
            _menuRow = 1;
            UpdatePointers();
            TryGoBack();
        }

        protected virtual void RequestFlowNavigation(FlowState state)
        {
            var flow = SceneFlowManager.I;
            if (flow != null)
                flow.GoTo(state);
        }

        void TryConfirmSelection()
        {
            PersistCurrentAvatar();
            RequestFlowNavigation(FlowState.LevelSelect);
        }

        void TryGoBack()
        {
            RequestFlowNavigation(FlowState.Title);
        }

        void PersistCurrentAvatar()
        {
            if (avatarController == null)
                return;
            PlayerPrefs.SetInt(AvatarSelectController.SelectedAvatarPrefsKey, avatarController.CurrentIndex);
            PlayerPrefs.Save();
        }

        static bool CanProcessMenuInput()
        {
            if (GlobalPauseMenuController.IsPaused || GlobalPauseMenuController.WasClosedThisFrame)
                return false;
            var flow = SceneFlowManager.I;
            return flow == null || !flow.IsTransitioning;
        }

        void UpdatePointers()
        {
            if (selectPointerText != null)
                selectPointerText.text = _menuRow == 0 ? pointCharacter : inactivePointer;
            if (backPointerText != null)
                backPointerText.text = _menuRow == 1 ? pointCharacter : inactivePointer;
        }
    }
}
