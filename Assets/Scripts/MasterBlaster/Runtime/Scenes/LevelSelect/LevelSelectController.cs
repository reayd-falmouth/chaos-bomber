using HybridGame.MasterBlaster.Scripts.Core;
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
        
        [Header("Input Setup")]
        [Tooltip("Assign the UIMenus Input Action Asset in the Inspector.")]
        public InputActionAsset inputActions;

        [Header("Level Library")]
        public LevelData[] levels;
        private int currentIndex = 0;

        private InputAction _moveAction;
        private InputAction _submitAction;
        private Vector2 _lastMoveInput;

        private void Awake()
        {
            if (retroStreamer == null)
                retroStreamer = GetComponent<RetroTerminalStreamer>();
            
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

            Vector2 moveInput = _moveAction.ReadValue<Vector2>();

            if (moveInput.x < -0.5f && _lastMoveInput.x >= -0.5f)
            {
                PreviousLevel();
            }
            else if (moveInput.x > 0.5f && _lastMoveInput.x <= 0.5f)
            {
                NextLevel();
            }

            if (_submitAction.WasPressedThisFrame())
                AdvanceToNextFlowState();

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
        
            // Reflects the "retro-reboot" identity mentioned in your doc
            Debug.Log("Loading Player Type: " + current.levelName);
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