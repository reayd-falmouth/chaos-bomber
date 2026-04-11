using System.Collections;
using HybridGame.MasterBlaster.Scripts;
using HybridGame.MasterBlaster.Scripts.Arena;
using Unity.Netcode;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// When a Random <see cref="ItemPickup3D"/> is collected: if Netcode is listening, the host runs a
    /// replicated FPS interlude; if not listening (local/solo), an optional offline interlude drives
    /// <see cref="GameModeManager"/> only on this machine. Place on the same object as <see cref="GameModeManager"/>.
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

        [Header("Offline / solo")]
        [Tooltip("When Netcode is not listening, Random pickups still run FPS interlude + countdown locally. " +
                 "Disable to use classic ApplyRandom() only when offline.")]
        [SerializeField]
        private bool fpsInterludeWhenOffline = true;

        [Header("Debug")]
        [Tooltip("Logs [FPSInterlude] lines to the Console (filter by FPSInterlude). Disable to silence.")]
        [SerializeField]
        private bool logFpsInterludeDiagnostics = true;

        /// <summary>True when the scene’s controller has diagnostics enabled (requires Instance).</summary>
        public static bool DiagnosticsEnabled =>
            Instance != null && Instance.logFpsInterludeDiagnostics;

        private bool _interludeRoutineRunning;

        private bool _offlineInterludeActive;
        private int _offlineCountdown;

        private static void LogDiag(string message)
        {
            if (!DiagnosticsEnabled)
                return;
            UnityEngine.Debug.Log("[FPSInterlude] " + message);
        }

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
            LogDiag($"OnNetworkSpawn name={gameObject.name} NetworkObjectId={NetworkObjectId}");
            _syncedGameMode.OnValueChanged += OnSyncedGameModeChanged;
            ApplySyncedGameMode(_syncedGameMode.Value);
        }

        public override void OnNetworkDespawn()
        {
            LogDiag($"OnNetworkDespawn name={gameObject.name}");
            _syncedGameMode.OnValueChanged -= OnSyncedGameModeChanged;
            base.OnNetworkDespawn();
        }

        private void OnEnable()
        {
            // In-scene NetworkObjects that start disabled (common in single-scene flow) may never auto-spawn;
            // spawn when the GameObject becomes active on the host.
            TrySpawnNetworkObjectIfServer();
        }

        /// <summary>
        /// Ensures this in-scene <see cref="NetworkObject"/> is spawned on the server so
        /// <see cref="NetworkVariable{T}"/> writes and <see cref="IsServer"/> are valid.
        /// </summary>
        private void TrySpawnNetworkObjectIfServer()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || !NetworkManager.Singleton.IsServer)
                return;
            if (IsSpawned)
                return;
            if (!TryGetComponent<NetworkObject>(out var netObj) || netObj == null)
            {
                LogDiag("TrySpawnNetworkObjectIfServer: no NetworkObject on this GameObject");
                return;
            }

            if (netObj.IsSpawned)
                return;

            LogDiag($"TrySpawnNetworkObjectIfServer: calling Spawn() on {gameObject.name}");
            netObj.Spawn();
            LogDiag($"TrySpawnNetworkObjectIfServer: after Spawn IsSpawned={IsSpawned} netObj.IsSpawned={netObj.IsSpawned}");
        }

        private void OnSyncedGameModeChanged(byte previous, byte next)
        {
            ApplySyncedGameMode(next);
        }

        private static void ApplySyncedGameMode(byte value)
        {
            var mode = ByteToGameMode(value);
            LogDiag($"ApplySyncedGameMode byte={value} -> {mode} GameModeManager.Instance={(GameModeManager.Instance != null ? "ok" : "null")}");
            var gmm = GameModeManager.Instance;
            if (gmm == null)
                return;

            var before = gmm.CurrentMode;
            gmm.SwitchMode(mode);
            if (GameModeManager.Instance != null)
                LogDiag($"ApplySyncedGameMode after SwitchMode CurrentMode={GameModeManager.Instance.CurrentMode} (was {before})");
        }

        private static GameModeManager.GameMode ByteToGameMode(byte value)
        {
            if (value > (byte)GameModeManager.GameMode.ArenaPerspective)
                return GameModeManager.GameMode.Bomberman;
            return (GameModeManager.GameMode)value;
        }

        /// <summary>True when the HUD should show the current countdown number.</summary>
        public bool IsCountdownDisplayActive =>
            (_offlineInterludeActive && _offlineCountdown > 0)
            || (IsSpawned && _interludeActive.Value && _countdownRemaining.Value > 0);

        /// <summary>Current number to show (0 = hide number).</summary>
        public int CountdownDisplayValue
        {
            get
            {
                if (_offlineInterludeActive && _offlineCountdown > 0)
                    return _offlineCountdown;
                return IsSpawned ? _countdownRemaining.Value : 0;
            }
        }

        /// <summary>
        /// Server-only: starts the FPS interlude. Returns true if this pickup should not apply a random roll.
        /// Returns false if offline / not server / not listening — caller should use ApplyRandom on the collector.
        /// </summary>
        public bool TryBeginInterludeFromServer()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                LogDiag("TryBeginInterludeFromServer: fail NetworkManager null or not IsListening");
                return false;
            }

            // Do not use NetworkBehaviour.IsServer before spawn — it is false when not spawned even on the host.
            if (!NetworkManager.Singleton.IsServer)
            {
                LogDiag("TryBeginInterludeFromServer: fail not NetworkManager.IsServer");
                return false;
            }

            LogDiag($"TryBeginInterludeFromServer: enter IsSpawned={IsSpawned} interludeRunning={_interludeRoutineRunning}");
            TrySpawnNetworkObjectIfServer();

            if (!IsSpawned)
            {
                UnityEngine.Debug.LogWarning(
                    "[OnlineFpsInterludeController] Random pickup on server but this NetworkObject is not spawned — " +
                    "cannot run FPS interlude. Ensure the GameManager NetworkObject is in-scene and spawnable.");
                LogDiag("TryBeginInterludeFromServer: fail still !IsSpawned after TrySpawn");
                return false;
            }

            if (_interludeRoutineRunning)
            {
                LogDiag("TryBeginInterludeFromServer: already running (no new interlude)");
                return true;
            }

            _interludeRoutineRunning = true;
            LogDiag("TryBeginInterludeFromServer: starting ServerInterludeCoroutine");
            StartCoroutine(ServerInterludeCoroutine());
            return true;
        }

        private IEnumerator ServerInterludeCoroutine()
        {
            LogDiag($"ServerInterludeCoroutine: start countdownSeconds={countdownSeconds} secondsPerTick={secondsPerTick}");
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
            LogDiag("ServerInterludeCoroutine: end returned to Bomberman, lock off");
        }

        /// <summary>
        /// Local/solo: when <see cref="NetworkManager"/> is not listening, runs FPS interlude on this machine only
        /// (no Netcode). Returns false if interlude is disabled or a session is active (use online path instead).
        /// </summary>
        public bool TryBeginInterludeWhenOffline()
        {
            if (!fpsInterludeWhenOffline)
            {
                LogDiag("TryBeginInterludeWhenOffline: skipped (fpsInterludeWhenOffline is false)");
                return false;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                LogDiag("TryBeginInterludeWhenOffline: skipped (IsListening — use TryBeginInterludeFromServer)");
                return false;
            }

            if (_interludeRoutineRunning)
            {
                LogDiag("TryBeginInterludeWhenOffline: already running");
                return true;
            }

            _interludeRoutineRunning = true;
            LogDiag("TryBeginInterludeWhenOffline: starting OfflineInterludeCoroutine");
            StartCoroutine(OfflineInterludeCoroutine());
            return true;
        }

        private IEnumerator OfflineInterludeCoroutine()
        {
            GameModeManager.Instance?.SetExternalModeLock(true);
            _offlineInterludeActive = true;
            GameModeManager.Instance?.SwitchMode(GameModeManager.GameMode.FPS);
            LogDiag("OfflineInterludeCoroutine: switched to FPS");

            int[] sequence = FpsInterludeCountdownMath.BuildCountdownSequence(countdownSeconds);
            foreach (int n in sequence)
            {
                _offlineCountdown = n;
                yield return new WaitForSeconds(secondsPerTick);
            }

            _offlineCountdown = 0;
            _offlineInterludeActive = false;
            GameModeManager.Instance?.SwitchMode(GameModeManager.GameMode.Bomberman);
            GameModeManager.Instance?.SetExternalModeLock(false);
            _interludeRoutineRunning = false;
            LogDiag("OfflineInterludeCoroutine: back to Bomberman");
        }
    }
}
