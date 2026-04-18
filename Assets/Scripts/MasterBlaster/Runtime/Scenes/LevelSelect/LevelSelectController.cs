using System.IO;
using System.Text;
using HybridGame.MasterBlaster.Runtime.Scenes.Character;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Mobile;
using HybridGame.MasterBlaster.Scripts.Levels;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Runtime.Scenes.LevelSelect
{
    [System.Serializable]
    public class LevelData
    {
        public string levelName;
        [TextArea(3, 10)]
        public string levelDescription;
        public Sprite levelSprite;
    }

    public class LevelSelectController : MonoBehaviour
    {
        [Header("UI References")]
        public Image displayImage;
        public Text descriptionText;
        public Text levelName;

        [Tooltip("Drag the RetroTerminalStreamer component here.")]
        public RetroTerminalStreamer retroStreamer;

        [Tooltip("Optional: top-down arena preview (RenderTexture). Assign after running HybridGame → Setup → Build Level Select Arena Preview Studio.")]
        [SerializeField]
        private LevelArenaPreviewRenderer arenaPreview;
        
        [Header("Input Setup")]
        [Tooltip("Assign the UIMenus Input Action Asset in the Inspector.")]
        public InputActionAsset inputActions;

        [Header("Level Library")]
        public LevelData[] levels;
        private int currentIndex = 0;

        private InputAction _moveAction;
        private InputAction _submitAction;
        private Vector2 _lastMoveInput;
        private bool _mobileBombHeldLastFrame;

        [SerializeField, Tooltip("Optional. When set (or found in children), PlaceBomb does not start countdown while the menu highlight is on Back.")]
        AvatarSelectMenuController _selectMenu;

        private void Awake()
        {
            if (retroStreamer == null)
                retroStreamer = GetComponent<RetroTerminalStreamer>();

            if (_selectMenu == null)
                _selectMenu = GetComponentInChildren<AvatarSelectMenuController>(true);
            
            if (inputActions == null)
            {
                Debug.LogWarning("[AvatarController] InputActionAsset not assigned.");
                return;
            }

            var playerMap = inputActions.FindActionMap("Player");
            if (playerMap == null)
            {
                Debug.LogWarning("[AvatarController] Could not find action map 'Player' on InputActionAsset.");
                return;
            }

            _moveAction = playerMap.FindAction("Move");
            _submitAction = playerMap.FindAction("PlaceBomb");
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _submitAction?.Enable();
            _mobileBombHeldLastFrame = false;
            RefreshArenaPreview();

            RefreshAvatarPortraitBar();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _submitAction?.Disable();
        }

        void Start()
        {
            UpdateUI();
        }

        private void Update()
        {
            if (_moveAction == null || _submitAction == null)
                return;

            // Gate input during flow transitions to avoid double-advances.
            if (HybridGame.MasterBlaster.Scripts.Core.SceneFlowManager.I != null
                && HybridGame.MasterBlaster.Scripts.Core.SceneFlowManager.I.IsTransitioning)
                return;

            if (levels == null || levels.Length == 0)
                return;

            Vector2 moveInput = MobileMenuInputBridge.MergeMove(_moveAction.ReadValue<Vector2>());

            if (moveInput.x < -0.5f && _lastMoveInput.x >= -0.5f)
            {
                PreviousLevel();
            }
            else if (moveInput.x > 0.5f && _lastMoveInput.x <= 0.5f)
            {
                NextLevel();
            }

            if (MobileMenuInputBridge.SubmitPressedThisFrame(_submitAction, ref _mobileBombHeldLastFrame))
            {
                // Same binding as SelectMenu; when user confirms Back, only the menu must navigate — not countdown.
                if (_selectMenu != null && _selectMenu.IsBackRowActive)
                    return;
                AdvanceToNextFlowState();
            }

            _lastMoveInput = moveInput;
        }

        public void NextLevel()
        {
            currentIndex = (currentIndex + 1) % levels.Length;
            UpdateUI();
        }

        public void PreviousLevel()
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = levels.Length - 1;
            UpdateUI();
        }

        void UpdateUI()
        {
            if (levels == null || levels.Length == 0)
                return;

            LevelData current = levels[currentIndex];
        
            if (displayImage != null)
                displayImage.sprite = current.levelSprite;
            if (levelName != null)
                levelName.text = current.levelName;
            if (retroStreamer != null)
            {
                retroStreamer.StartStreaming(current.levelDescription);
            }
            else if (descriptionText != null)
            {
                descriptionText.text = current.levelDescription;
            }

            RefreshArenaPreview();
            RefreshAvatarPortraitBar();

            // Reflects the "retro-reboot" identity mentioned in your doc
            Debug.Log("Loading Player Type: " + current.levelName);
        }

        void RefreshAvatarPortraitBar()
        {
            var portraitBar = GetComponentInChildren<LevelSelectAvatarPortraits>(true);
            if (portraitBar == null)
                return;
            portraitBar.Apply(currentIndex);
            // #region agent log
            try
            {
                int mask = PlayerPrefs.GetInt(AvatarPortraitUnlockPersistence.MaskKeyForArena(currentIndex), 0);
                var sb = new StringBuilder(220);
                sb.Append("{\"sessionId\":\"6c4413\",\"runId\":\"avatar-ui\",\"hypothesisId\":\"H3\",\"location\":\"LevelSelectController.RefreshAvatarPortraitBar\",");
                sb.Append("\"message\":\"portrait_apply\",\"data\":{\"arenaIndex\":").Append(currentIndex).Append(",\"unlockMask\":");
                sb.Append(mask).Append("},\"timestamp\":");
                sb.Append(System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).Append("}\n");
                File.AppendAllText(Path.Combine(Application.dataPath, "..", "debug-6c4413.log"), sb.ToString());
            }
            catch { }
            // #endregion
        }

        void RefreshArenaPreview()
        {
            if (arenaPreview != null && levels != null && levels.Length > 0)
                arenaPreview.SetPreviewIndex(currentIndex);
        }

        protected virtual void AdvanceToNextFlowState()
        {
            var flow = SceneFlowManager.I;
            if (flow == null)
                return;

            PlayerPrefs.SetInt(LevelSelectionPrefs.SelectedArenaIndexKey, currentIndex);
            PlayerPrefs.DeleteKey(LevelSelectionPrefs.SelectedLevelIdKey);
            PlayerPrefs.Save();

            flow.GoTo(FlowState.Countdown);
        }
    }
}
