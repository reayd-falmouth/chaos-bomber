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
                await NetworkLobbyManager.Instance.CreateLobbyAsync();
                string code = NetworkLobbyManager.Instance.LobbyJoinCode;
                if (connectionStringDisplay != null)
                    connectionStringDisplay.text = code;
                SetStatus("Hosting — share the lobby code above with friends.");
            }
            catch (System.Exception e)
            {
                SetStatus($"Error: {e.Message}");
                SetButtons(true);
            }
        }

        async void OnJoinClicked()
        {
            string code = joinCodeInput.text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                SetStatus("Paste the host's lobby code first.");
                return;
            }

            SetStatus("Joining...");
            SetButtons(false);

            try
            {
                await NetworkLobbyManager.Instance.JoinLobbyAsync(code);
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
