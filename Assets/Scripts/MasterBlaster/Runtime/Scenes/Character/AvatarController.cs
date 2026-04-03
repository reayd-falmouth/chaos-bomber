using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Runtime.Scenes.Character
{
    [System.Serializable]
    public class CharacterData
    {
        public string characterName;
        [TextArea(3, 10)]
        public string characterDescription;
        public Sprite characterSprite;

        [Tooltip("Optional 3D root for Avatar Select RenderTexture preview. Run HybridGame → Setup → Build Avatar Preview Studio after assigning.")]
        public GameObject previewPrefab;
    }

    public class AvatarController : MonoBehaviour
    {
        [Header("UI References")]
        public Image displayImage;
        public Text descriptionText;

        [Tooltip("Drag the RetroTerminalStreamer component here.")]
        public RetroTerminalStreamer retroStreamer;

        [Tooltip("Optional 3D viewport (RenderTexture). Wired by Build Avatar Preview Studio.")]
        [SerializeField]
        private AvatarPreviewRenderer avatarPreview;

        [Header("Input Setup")]
        [Tooltip("Assign the UIMenus Input Action Asset in the Inspector.")]
        public InputActionAsset inputActions;

        [Header("Character Library")]
        public CharacterData[] characters;
        private int currentIndex = 0;

        /// <summary>Index into <see cref="characters"/>; used when <see cref="AvatarSelectMenuController"/> has avatar persistence enabled.</summary>
        public int CurrentIndex => currentIndex;

        private InputAction _moveAction;
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
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            RefreshAvatarPreview();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
        }

        void Start()
        {
            UpdateUI();
        }

        private void Update()
        {
            if (_moveAction == null)
                return;

            // Select/Back flow is handled by AvatarSelectMenuController (or another screen menu); character browse is allowed
            // even when SceneFlowManager.IsTransitioning (menu still gates confirm/back).

            if (characters == null || characters.Length == 0)
                return;

            Vector2 moveInput = _moveAction.ReadValue<Vector2>();

            if (moveInput.x < -0.5f && _lastMoveInput.x >= -0.5f)
            {
                PreviousCharacter();
            }
            else if (moveInput.x > 0.5f && _lastMoveInput.x <= 0.5f)
            {
                NextCharacter();
            }

            _lastMoveInput = moveInput;
        }

        public void NextCharacter()
        {
            currentIndex = (currentIndex + 1) % characters.Length;
            UpdateUI();
        }

        public void PreviousCharacter()
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = characters.Length - 1;
            UpdateUI();
        }

        void UpdateUI()
        {
            if (characters == null || characters.Length == 0)
                return;

            CharacterData current = characters[currentIndex];
        
            if (displayImage != null)
                displayImage.sprite = current.characterSprite;
            if (retroStreamer != null)
            {
                retroStreamer.StartStreaming(current.characterDescription);
            }
            else if (descriptionText != null)
            {
                descriptionText.text = current.characterDescription;
            }

            RefreshAvatarPreview();
        
            // Reflects the "retro-reboot" identity mentioned in your doc
            Debug.Log("Loading Player Type: " + current.characterName);
        }

        void RefreshAvatarPreview()
        {
            if (avatarPreview != null && characters != null && characters.Length > 0)
                avatarPreview.SetPreviewIndex(currentIndex);
        }
    }
}