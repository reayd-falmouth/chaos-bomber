using System.Collections;
using HybridGame.MasterBlaster.Scripts;
using HybridGame.MasterBlaster.Scripts.Arena;
using Unity.Netcode;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// Online-only: when a Random <see cref="ItemPickup3D"/> is collected, switches all peers to FPS,
    /// runs a server-replicated countdown, then returns to Bomberman. Place on the same networked object as
    /// <see cref="GameModeManager"/> (e.g. GameManager with <see cref="NetworkObject"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public class OnlineFpsInterludeController : NetworkBehaviour
    {
        public static OnlineFpsInterludeController Instance { get; private set; }

        private readonly NetworkVariable<byte> _syncedGameMode =
            new NetworkVariable<byte>(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> _countdownRemaining =
            new NetworkVariable<int>(
                0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> _interludeActive =
            new NetworkVariable<bool>(
                false,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        [Header("Interlude")]
        [SerializeField]
        [Min(1)]
        private int countdownSeconds = 5;

        [SerializeField]
        [Min(0.05f)]
        private float secondsPerTick = 1f;

        private bool _interludeRoutineRunning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _syncedGameMode.OnValueChanged += OnSyncedGameModeChanged;
            ApplySyncedGameMode(_syncedGameMode.Value);
        }

        public override void OnNetworkDespawn()
        {
            _syncedGameMode.OnValueChanged -= OnSyncedGameModeChanged;
            base.OnNetworkDespawn();
        }

        private void OnSyncedGameModeChanged(byte previous, byte next)
        {
            ApplySyncedGameMode(next);
        }

        private static void ApplySyncedGameMode(byte value)
        {
            var gmm = GameModeManager.Instance;
            if (gmm == null)
                return;

            gmm.SwitchMode(ByteToGameMode(value));
        }

        private static GameModeManager.GameMode ByteToGameMode(byte value)
        {
            if (value > (byte)GameModeManager.GameMode.ArenaPerspective)
                return GameModeManager.GameMode.Bomberman;
            return (GameModeManager.GameMode)value;
        }

        /// <summary>True when the HUD should show the current countdown number.</summary>
        public bool IsCountdownDisplayActive =>
            IsSpawned && _interludeActive.Value && _countdownRemaining.Value > 0;

        /// <summary>Current number to show (0 = hide number).</summary>
        public int CountdownDisplayValue => IsSpawned ? _countdownRemaining.Value : 0;

        /// <summary>
        /// Server-only: starts the FPS interlude. Returns true if this pickup should not apply a random roll.
        /// Returns false if offline / not server / not listening — caller should use ApplyRandom on the collector.
        /// </summary>
        public bool TryBeginInterludeFromServer()
        {
            if (!IsSpawned || !IsServer)
                return false;
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return false;
            if (_interludeRoutineRunning)
                return true;

            _interludeRoutineRunning = true;
            StartCoroutine(ServerInterludeCoroutine());
            return true;
        }

        private IEnumerator ServerInterludeCoroutine()
        {
            GameModeManager.Instance?.SetExternalModeLock(true);

            _interludeActive.Value = true;
            _syncedGameMode.Value = (byte)GameModeManager.GameMode.FPS;

            int[] sequence = FpsInterludeCountdownMath.BuildCountdownSequence(countdownSeconds);
            foreach (int n in sequence)
            {
                _countdownRemaining.Value = n;
                yield return new WaitForSeconds(secondsPerTick);
            }

            _countdownRemaining.Value = 0;
            _syncedGameMode.Value = (byte)GameModeManager.GameMode.Bomberman;
            _interludeActive.Value = false;

            GameModeManager.Instance?.SetExternalModeLock(false);
            _interludeRoutineRunning = false;
        }
    }
}
