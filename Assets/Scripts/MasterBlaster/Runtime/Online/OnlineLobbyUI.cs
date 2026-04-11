using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// Simple UI panel for hosting or joining an online game via PlayFab Lobby.
    /// Wire buttons, input field, and text labels in the Inspector.
    /// The host shares the connection string displayed after creating a lobby;
    /// the client pastes it into the join field.
    /// </summary>
    public class OnlineLobbyUI : MonoBehaviour
    {
        [Header("Create Game")]
        [SerializeField] private Button createButton;

        [Header("Join Game")]
        [SerializeField] private InputField joinCodeInput;
        [SerializeField] private Button joinButton;

        [Header("Status")]
        [SerializeField] private Text statusText;

        /// <summary>Displays the connection string after hosting. Players copy this to join.</summary>
        [SerializeField] private Text connectionStringDisplay;

        void Awake()
        {
            createButton.onClick.AddListener(OnCreateClicked);
            joinButton.onClick.AddListener(OnJoinClicked);
        }

        async void OnCreateClicked()
        {
            SetStatus("Creating lobby...");
            SetButtons(false);

            try
            {
                if (PlayFabLobbyManager.Instance == null)
                    throw new System.InvalidOperationException("PlayFabLobbyManager missing in scene (add it on the NetworkManager object).");

                await PlayFabLobbyManager.Instance.CreateLobbyAsync();
                string connectionString = PlayFabLobbyManager.Instance.LobbyConnectionString;
                if (connectionStringDisplay != null)
                    connectionStringDisplay.text = connectionString ?? string.Empty;
                SetStatus("Hosting — share the connection string above with friends.");
            }
            catch (System.Exception e)
            {
                SetStatus($"Error: {e.Message}");
                SetButtons(true);
            }
        }

        async void OnJoinClicked()
        {
            string code = OnlineLobbyConnectionString.Normalize(joinCodeInput != null ? joinCodeInput.text : string.Empty);
            if (string.IsNullOrEmpty(code))
            {
                SetStatus("Paste the host's connection string first.");
                return;
            }

            SetStatus("Joining...");
            SetButtons(false);

            try
            {
                if (PlayFabLobbyManager.Instance == null)
                    throw new System.InvalidOperationException("PlayFabLobbyManager missing in scene (add it on the NetworkManager object).");

                await PlayFabLobbyManager.Instance.JoinLobbyAsync(code);
                SetStatus("Connected!");
            }
            catch (System.Exception e)
            {
                SetStatus($"Error: {e.Message}");
                SetButtons(true);
            }
        }

        void SetButtons(bool interactable)
        {
            createButton.interactable = interactable;
            joinButton.interactable   = interactable;
        }

        void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
            UnityEngine.Debug.Log($"[OnlineLobbyUI] {msg}");
        }
    }
}
