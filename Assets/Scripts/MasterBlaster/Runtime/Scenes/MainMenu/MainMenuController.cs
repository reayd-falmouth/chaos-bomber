using HybridGame.MasterBlaster.Scripts.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.MainMenu
{
    public class MainMenuController : MonoBehaviour
    {
        [System.Serializable]
        public class MenuOption
        {
            public Text pointerText;
            public Text optionLabel;
            public Text valueLabel;
        }

        public MenuOption[] options;

        [Header("Input Setup")]
        [Tooltip("Assign UIMenus or PlayerControls Input Action Asset in Inspector.")]
        public InputActionAsset inputActions;

        private InputAction _moveAction;
        private InputAction _submitAction;
        private Vector2 _lastMoveInput;

        private int selectedIndex;

        // Default values
        private int winsNeeded = 3;
        private int players = 2;
        private bool shop = true;
        private bool shrinking = true;
        private bool fastIgnition = true;
        private bool startMoney = false;
        private bool normalLevel = true;
        private bool gambling = true;
        private bool quickRestart = false;

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

            // In single-scene mode the menu root is toggled on/off, so we must re-initialize
            // UI state every time this controller is enabled.
            LoadPrefs();
            UpdateMenuText();
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
            if (_moveAction == null || _submitAction == null)
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

            if (moveInput.x < -0.5f && _lastMoveInput.x >= -0.5f)
            {
                ChangeOption(false);
                UpdateMenuText();
            }
            else if (moveInput.x > 0.5f && _lastMoveInput.x <= 0.5f)
            {
                ChangeOption(true);
                UpdateMenuText();
            }

            if (_submitAction.WasPressedThisFrame() && !GlobalPauseMenuController.IsPaused && !GlobalPauseMenuController.WasClosedThisFrame)
                HandleSubmit();

            _lastMoveInput = moveInput;
        }

        [Header("Online")]
        [Tooltip("Assign the OnlineLobbyUI panel GameObject. When Online is selected, this panel activates instead of starting locally.")]
        [SerializeField] private GameObject onlineLobbyPanel;

        private bool _onlineSelected;

        private void HandleSubmit()
        {
            if (_onlineSelected)
            {
                // Show the online lobby panel; NetworkLobbyManager handles the rest.
                if (onlineLobbyPanel != null)
                    onlineLobbyPanel.SetActive(true);
                return;
            }

            SavePrefs();
            SceneFlowManager.I.SignalMenuStart();
        }

        private void ChangeOption(bool rightArrow)
        {
            switch (selectedIndex)
            {
                case 0:
                    winsNeeded = Mathf.Clamp(winsNeeded + (rightArrow ? 1 : -1), 3, 9);
                    break;
                case 1:
                    players = Mathf.Clamp(players + (rightArrow ? 1 : -1), 2, 5);
                    break;
                case 2:
                    shop = !shop;
                    break;
                case 3:
                    shrinking = !shrinking;
                    break;
                case 4:
                    fastIgnition = !fastIgnition;
                    break;
                case 5:
                    startMoney = !startMoney;
                    break;
                case 6:
                    normalLevel = !normalLevel;
                    break;
                case 7:
                    gambling = !gambling;
                    break;
            }
        }

        private void UpdateMenuText()
        {
            options[0].valueLabel.text = winsNeeded.ToString();
            options[1].valueLabel.text = players.ToString();
            options[2].valueLabel.text = shop ? "ON" : "OFF";
            options[3].valueLabel.text = shrinking ? "ON" : "OFF";
            options[4].valueLabel.text = fastIgnition ? "ON" : "OFF";
            options[5].valueLabel.text = startMoney ? "ON" : "OFF";
            options[6].valueLabel.text = normalLevel ? "YES" : "NO";
            options[7].valueLabel.text = gambling ? "YES" : "NO";
            if (options.Length > 8)
                options[8].valueLabel.text = quickRestart ? "ON" : "OFF";
        }

        private void UpdatePointers()
        {
            for (int i = 0; i < options.Length; i++)
                options[i].pointerText.text = (i == selectedIndex) ? "> " : "  ";
        }

        private void SavePrefs()
        {
            PlayerPrefs.SetInt("WinsNeeded", winsNeeded);
            PlayerPrefs.SetInt("Players", players);
            PlayerPrefs.SetInt("Shop", shop ? 1 : 0);
            PlayerPrefs.SetInt("Shrinking", shrinking ? 1 : 0);
            PlayerPrefs.SetInt("FastIgnition", fastIgnition ? 1 : 0);
            PlayerPrefs.SetInt("StartMoney", startMoney ? 1 : 0);
            PlayerPrefs.SetInt("NormalLevel", normalLevel ? 1 : 0);
            PlayerPrefs.SetInt("Gambling", gambling ? 1 : 0);
            PlayerPrefs.SetInt("QuickRestart", quickRestart ? 1 : 0);

            // Always reset session (wins, coins, upgrades) when starting a new game from the menu
            SessionManager.Instance.Initialize(players);

            // Single-scene mode: mark that the next time we enter Game, we should fully reinitialize player state and arena.
            PlayerPrefs.SetInt("NewGamePending", 1);

            PlayerPrefs.SetInt("GiveStartMoneyNextArena", 1);
            PlayerPrefs.Save();
        }

        private void LoadPrefs()
        {
            winsNeeded = PlayerPrefs.GetInt("WinsNeeded", 3);
            players = PlayerPrefs.GetInt("Players", 2);
            shop = PlayerPrefs.GetInt("Shop", 1) == 1;
            shrinking = PlayerPrefs.GetInt("Shrinking", 1) == 1;
            fastIgnition = PlayerPrefs.GetInt("FastIgnition", 1) == 1;
            startMoney = PlayerPrefs.GetInt("StartMoney", 0) == 1;
            normalLevel = PlayerPrefs.GetInt("NormalLevel", 1) == 1;
            gambling = PlayerPrefs.GetInt("Gambling", 1) == 1;
            quickRestart = PlayerPrefs.GetInt("QuickRestart", 0) == 1;
        }
    }
}
