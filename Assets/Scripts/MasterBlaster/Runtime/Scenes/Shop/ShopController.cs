using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Debug;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Shop
{
    public class ShopController : MonoBehaviour
    {
        [System.Serializable]
        public class ShopItem
        {
            public ShopItemType type;
            public string name;
            public int cost;
            public Text pointerText; // purple arrow
            public Text labelText; // "Speed Boost"
            public Text costText; // "x3"
        }

        public ShopItem[] items;

        // Direct gamepad reference for the current player (refreshed when player changes).
        private Gamepad _currentGamepad;
        private int? _currentDeviceIndex;
        private int? _arenaOwnerPlayerIdBeforeShop;
        private Vector2 _lastMoveInput;
        private bool _bombHeldLastFrame;

        private int selectedIndex = 0;
        private int playerCount;
        private int currentPlayer = 1; // 1-based index

        [Header("UI References")]
        public Transform coinContainer;
        public Sprite coinSprite;
        public Text headingText;

        [Header("Feedbacks")]
        [SerializeField] private MMF_Player buyFeedbacks;
        [SerializeField] private MMF_Player noBuyFeedbacks;

        /// <summary>
        /// Returns the pointer text for an item at the given index ("> " if selected, "  " otherwise).
        /// Used by UpdatePointers and by tests.
        /// </summary>
        public static string GetPointerTextForIndex(int index, int selectedIndex)
        {
            return index == selectedIndex ? "> " : "  ";
        }

        /// <summary>
        /// Returns the coin count to display for a player (from SessionManager). Used by RefreshCoinsDisplay and by tests.
        /// </summary>
        public static int GetCoinsToDisplayForPlayer(int playerId)
        {
            return SessionManager.Instance != null ? SessionManager.Instance.GetCoins(playerId) : 0;
        }

        private void Awake()
        {
            if (items != null && items.Length > 0)
                UpdatePointers();
        }

        private void OnEnable()
        {
            playerCount = PlayerPrefs.GetInt("Players", 2);
            currentPlayer = 1; // start with Player 1
            // Only initialize SessionManager if not yet set (e.g. first time); do not wipe state between rounds
            if (
                SessionManager.Instance != null
                && (
                    SessionManager.Instance.PlayerUpgrades == null
                    || SessionManager.Instance.PlayerUpgrades.Count == 0
                    || !SessionManager.Instance.PlayerUpgrades.ContainsKey(1)
                )
            )
            {
                SessionManager.Instance.Initialize(playerCount);
            }

            UpdateMenuText();
            UpdatePointers();
            RefreshCoinsDisplay();
            UpdateHeading();
            RefreshGamepad();

            // Remember who owned this controller in the arena so we can restore it when leaving the shop.
            _arenaOwnerPlayerIdBeforeShop = null;
            if (SessionManager.Instance != null && _currentDeviceIndex.HasValue)
                _arenaOwnerPlayerIdBeforeShop = SessionManager.Instance.GetPlayerIdAssignedToDevice(_currentDeviceIndex.Value);
        }

        /// <summary>Lock input to the current player's assigned gamepad. Falls back to the first connected gamepad.</summary>
        private void RefreshGamepad()
        {
            _currentGamepad = null;
            _currentDeviceIndex = null;
            if (SessionManager.Instance != null)
            {
                int? deviceIndex = SessionManager.Instance.GetAssignedDevice(currentPlayer);
                if (deviceIndex.HasValue)
                {
                    int gpIndex = deviceIndex.Value - 1;
                    if (gpIndex >= 0 && gpIndex < Gamepad.all.Count)
                        _currentGamepad = Gamepad.all[gpIndex];
                }
            }
            // Fallback: use first available gamepad if player has none assigned (e.g. AI slot or unset).
            if (_currentGamepad == null && Gamepad.all.Count > 0)
                _currentGamepad = Gamepad.all[0];

            if (_currentGamepad != null)
            {
                // SessionManager uses 1-based device indices matching Gamepad.all order.
                for (int i = 0; i < Gamepad.all.Count; i++)
                {
                    if (Gamepad.all[i] == _currentGamepad)
                    {
                        _currentDeviceIndex = i + 1;
                        break;
                    }
                }
            }
            _lastMoveInput = Vector2.zero;
            _bombHeldLastFrame = false;
            UnityEngine.Debug.Log(
                $"[ShopController] Player {currentPlayer} → gamepad: {(_currentGamepad != null ? _currentGamepad.displayName : "NONE")} deviceIndex: {(_currentDeviceIndex.HasValue ? _currentDeviceIndex.Value.ToString() : "NONE")}"
            );
        }

        private void Update()
        {
            Vector2 moveInput = Vector2.zero;
            bool submitDown = false;
            bool controllerUp = false;
            bool controllerDown = false;

            if (_currentGamepad != null)
            {
                var stick = _currentGamepad.leftStick.ReadValue();
                var dpad  = _currentGamepad.dpad.ReadValue();
                moveInput = stick.sqrMagnitude >= dpad.sqrMagnitude ? stick : dpad;

                bool bombHeld = _currentGamepad.buttonSouth.isPressed;
                submitDown = bombHeld && !_bombHeldLastFrame;
                _bombHeldLastFrame = bombHeld;

                controllerUp = moveInput.y > 0.5f && _lastMoveInput.y <= 0.5f;
                controllerDown = moveInput.y < -0.5f && _lastMoveInput.y >= -0.5f;
            }
            else
            {
                // keep last move input stable when controller is disconnected
                moveInput = Vector2.zero;
            }

            // Keyboard fallback for menu navigation (works even if a gamepad exists).
            var keyboard = Keyboard.current;
            bool keyboardUp = keyboard != null
                               && (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame);
            bool keyboardDown = keyboard != null
                                 && (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame);
            bool keyboardSubmitDown = keyboard != null
                                       && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);

            // Update pointer selection: controller has priority if it moved this frame.
            if (controllerUp)
            {
                selectedIndex = (selectedIndex - 1 + items.Length) % items.Length;
                UpdatePointers();
            }
            else if (controllerDown)
            {
                selectedIndex = (selectedIndex + 1) % items.Length;
                UpdatePointers();
            }
            else if (keyboardUp)
            {
                selectedIndex = (selectedIndex - 1 + items.Length) % items.Length;
                UpdatePointers();
            }
            else if (keyboardDown)
            {
                selectedIndex = (selectedIndex + 1) % items.Length;
                UpdatePointers();
            }

            // Submit: controller has priority if it submitted this frame.
            if (submitDown)
            {
                AttemptPurchase(selectedIndex);
                RefreshCoinsDisplay();
            }
            else if (keyboardSubmitDown)
            {
                AttemptPurchase(selectedIndex);
                RefreshCoinsDisplay();
            }

            _lastMoveInput = moveInput;
        }

        private void InitialisePlayerPrefs(int playerCount)
        {
            for (int playerId = 1; playerId <= playerCount; playerId++)
            {
                foreach (ShopItemType type in System.Enum.GetValues(typeof(ShopItemType)))
                {
                    if (type == ShopItemType.Exit)
                        continue; // no prefs needed

                    string key = $"Player{playerId}_{type}";
                    PlayerPrefs.SetInt(key, 0);
                }
            }

            PlayerPrefs.Save();
            UnityEngine.Debug.Log(
                "[ShopController] PlayerPrefs initialised for all players (coins unchanged)."
            );
        }

        void UpdateMenuText()
        {
            foreach (var item in items)
            {
                item.labelText.text = item.name;

                if (item.costText != null) // only update cost if it exists
                    item.costText.text = $"{item.cost}";
                else
                    item.labelText.text = item.name; // just show "Exit"
            }
        }

        void UpdatePointers()
        {
            if (items == null)
                return;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].pointerText != null)
                    items[i].pointerText.text = GetPointerTextForIndex(i, selectedIndex);
            }
        }

        void UpdateHeading()
        {
            if (headingText != null)
            {
                headingText.text = $"PLAYER {currentPlayer} ENTERS SHOP";
            }
        }

        /// <summary>
        /// Clears the coin container and repopulates from SessionManager for the current player.
        /// Uses DestroyImmediate so the display updates correctly when switching players (no deferred-destroy race).
        /// </summary>
        void RefreshCoinsDisplay()
        {
            if (coinContainer == null)
                return;
            ClearCoinContainer();
            RepopulateCoinsFromSession();
        }

        void ClearCoinContainer()
        {
            if (coinContainer == null)
                return;
            // Clear immediately so repopulate shows correct count for current player (avoids player 2 showing player 1's + own coins)
            for (int i = coinContainer.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(coinContainer.GetChild(i).gameObject);
        }

        void RepopulateCoinsFromSession()
        {
            if (coinContainer == null)
                return;
            int playerId = currentPlayer;
            int coins = GetCoinsToDisplayForPlayer(playerId);
            for (int i = 0; i < coins; i++)
            {
                var coinGO = new GameObject($"Coin{i}");
                var img = coinGO.AddComponent<Image>();
                img.sprite = coinSprite;
                coinGO.transform.SetParent(coinContainer, false);
            }
        }

        void AttemptPurchase(int index)
        {
            var item = items[index];

            // Exit option: go to next player
            if (item.name == "EXIT")
            {
                if (currentPlayer < playerCount)
                {
                    currentPlayer++;
                    UnityEngine.Debug.Log($"Next shop turn: Player {currentPlayer}");
                    selectedIndex = 0;
                    UpdatePointers();
                    RefreshCoinsDisplay();
                    UpdateHeading();
                    RefreshGamepad();
                }
                else
                {
                    // Last player done → leave shop
                    if (SessionManager.Instance != null && _currentDeviceIndex.HasValue)
                    {
                        int restorePlayerId = _arenaOwnerPlayerIdBeforeShop ?? currentPlayer;
                        SessionManager.Instance.SetShopControllerOverride(restorePlayerId, _currentDeviceIndex.Value);
                        UnityEngine.Debug.Log($"[ShopController] Leaving shop: restore arena controller owner to player {restorePlayerId} (deviceIndex={_currentDeviceIndex.Value}).");

                        // #region agent log
                        AgentDebugNdjson_88a510.Log(
                            hypothesisId: "H5-shopOverrideSet",
                            location: "ShopController.AttemptPurchase(EXIT)",
                            message: "setting shop->arena controller override",
                            dataJsonObject:
                                "{\"restorePlayerId\":" + restorePlayerId +
                                ",\"deviceIndex\":" + _currentDeviceIndex.Value + "}",
                            runId: "pre"
                        );
                        // #endregion
                    }
                    SceneFlowManager.I.SignalScreenDone();
                }
                return;
            }

            // Normal purchase flow (coins in SessionManager)
            int playerId = currentPlayer;
            int coins =
                SessionManager.Instance != null ? SessionManager.Instance.GetCoins(playerId) : 0;

            if (ShopPurchaseLogic.CanAfford(coins, item.cost))
            {
                coins -= item.cost;
                if (SessionManager.Instance != null)
                    SessionManager.Instance.SetCoins(playerId, coins);
                buyFeedbacks?.PlayFeedbacks();

                ApplyUpgrade(playerId, item.type);

                UnityEngine.Debug.Log($"Player {playerId} bought {item.name}!");
            }
            else
            {
                noBuyFeedbacks?.PlayFeedbacks();
                UnityEngine.Debug.Log($"Player {playerId} cannot afford {item.name}!");
            }
        }

        void ApplyUpgrade(int playerId, ShopItemType type)
        {
            if (type == ShopItemType.Exit)
                return;
            if (SessionManager.Instance == null)
            {
                UnityEngine.Debug.LogWarning("[ShopController] ApplyUpgrade skipped: SessionManager is null.");
                return;
            }

            int currentLevel = SessionManager.Instance.GetUpgradeLevel(playerId, type);
            int newLevel = ShopPurchaseLogic.GetNewLevelAfterPurchase(type, currentLevel);
            SessionManager.Instance.SetUpgradeLevel(playerId, type, newLevel);
        }
    }
}
