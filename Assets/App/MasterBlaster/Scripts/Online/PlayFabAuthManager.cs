using System;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// Persistent singleton. Sets the PlayFab Title ID and signs in anonymously
    /// using the device's unique identifier as soon as the game boots.
    /// All other systems (PlayFabLobbyManager, etc.) wait on IsLoggedIn / OnLoggedIn.
    /// </summary>
    public class PlayFabAuthManager : MonoBehaviour
    {
        public static PlayFabAuthManager Instance { get; private set; }

        /// <summary>Fires once when the login response is received successfully.</summary>
        public event Action OnLoggedIn;

        public bool   IsLoggedIn { get; private set; }
        public string PlayFabId  { get; private set; }

        /// <summary>Entity key used by the Multiplayer / Lobby APIs.</summary>
        public PlayFab.ClientModels.EntityKey EntityKey { get; private set; }

        const string TitleId = "17918A";

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            PlayFabSettings.staticSettings.TitleId = TitleId;
            Login();
        }

        void Login()
        {
            // LoginWithCustomID creates an anonymous account tied to this device.
            // CreateAccount = true silently creates one if it doesn't exist yet.
            PlayFabClientAPI.LoginWithCustomID(
                new LoginWithCustomIDRequest
                {
                    CustomId      = SystemInfo.deviceUniqueIdentifier,
                    CreateAccount = true
                },
                result =>
                {
                    IsLoggedIn = true;
                    PlayFabId  = result.PlayFabId;
                    EntityKey  = result.EntityToken.Entity;
                    UnityEngine.Debug.Log($"[PlayFabAuth] Signed in: {PlayFabId}");
                    OnLoggedIn?.Invoke();
                },
                error =>
                {
                    UnityEngine.Debug.LogError($"[PlayFabAuth] Login failed: {error.GenerateErrorReport()}");
                    Invoke(nameof(Login), 5f); // retry in 5 s
                }
            );
        }
    }
}
