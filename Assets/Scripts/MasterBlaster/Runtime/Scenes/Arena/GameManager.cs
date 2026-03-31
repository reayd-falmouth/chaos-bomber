using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HybridGame.MasterBlaster.Scripts.Arena;
using HybridGame.MasterBlaster.Scripts.Bomb;
using HybridGame.MasterBlaster.Scripts.Debug;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player;
using HybridGame.MasterBlaster.Scripts.Player;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player.AI;
using Unity.FPS.Game;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena
{
    [DefaultExecutionOrder(-1)]
    public class GameManager : NetworkBehaviour
    {
        private static Vector3 AdjustSpawnToFloor(GameObject player, Vector3 desiredWorldOnGridPlane)
        {
            // We define spawn positions by their XZ cell center on the arena plane.
            // If the player has a CharacterController, its transform is typically the capsule center,
            // not the feet. Convert the desired "feet on plane" position into a correct transform position.
            float floorY = ArenaGrid3D.GridOrigin.y;
            desiredWorldOnGridPlane.y = floorY;

            if (player != null && player.TryGetComponent<CharacterController>(out var cc) && cc != null)
            {
                // feetWorld = transform.position + (center.y - height/2) * up  (ignoring rotation)
                // CharacterController's height/center are in the object's local space; account for world scaling.
                float scaleY = Mathf.Abs(player.transform.lossyScale.y);
                float feetOffsetWorld = (cc.center.y - (cc.height * 0.5f)) * scaleY;
                desiredWorldOnGridPlane.y = floorY - feetOffsetWorld;
            }

            return desiredWorldOnGridPlane;
        }

        private IEnumerator ClampPlayerFeetToFloorNextFrame(GameObject player)
        {
            // Some components (notably FPS PlayerCharacterController) adjust CharacterController height/center in Start,
            // which can change the implied "feet" offset after we've positioned the player. Re-clamp next frame.
            yield return null;
            if (player == null) yield break;
            player.transform.position = AdjustSpawnToFloor(player, player.transform.position);
        }

        private struct PlayerCandidate
        {
            public GameObject go;
            public int playerId;
            public Vector3 localPos;
        }

        /// <summary>
        /// Convenience accessor for single-arena scenes. In multi-arena training each arena has its own
        /// GameManager — use local references (cached via transform.root) instead of this property there.
        /// </summary>
        public static GameManager Instance { get; private set; }

        [SerializeField]
        private GameObject[] players;

        [SerializeField]
        private bool shrinkingEnabled;

        [SerializeField]
        private bool normalLevel;

        [SerializeField]
        private bool startMoney;

        [Header("Hybrid 3D arena")]
        [Tooltip("After one frame (so HybridArenaGrid.Start has run), calls ApplySceneDestructibleThinning. " +
                 "Use with HybridArenaGrid: thinSceneDestructibles + thinOnlyWhenInvokedExplicitly.")]
        [SerializeField]
        private bool applyHybridDestructibleThinningAfterLoad;

        [Header("AI")]
        [Tooltip("If true, AI players use reinforcement learning (ML-Agents). Requires Behavior Parameters on agent and a trained model for best results. If false, uses scripted AI.")]
        [SerializeField] private bool useReinforcementLearning;

        [Tooltip("When true, RL agents use the built-in Heuristic (rule-based) so they move without the Python trainer — good for testing the setup. When false, use Default so the trainer sends actions (for actual RL training).")]
        [SerializeField] public bool useHeuristicOnlyForAgents = true;

        [Tooltip("Optional: assign the Training Academy GameObject (the one with the ML-Agents Academy component) for reference. Not used for logic; Academy is accessed via Academy.Instance.")]
        [SerializeField] private GameObject trainingAcademyObject;

        [Header("Input")]
        [Tooltip("Assign PlayerControls (or UIMenus) here so human players can use gamepad/keyboard. If empty, we try Resources.Load('PlayerControls').")]
        [SerializeField]
        private InputActionAsset playerInputActions;

        [Header("Assign the 5 players in inspector")]
        public GameObject topLeftPlayer;
        public GameObject topRightPlayer;
        public GameObject bottomLeftPlayer;
        public GameObject bottomRightPlayer;
        public GameObject middlePlayer;

        /// <summary>Prevents AddWin and transition from running more than once per round (e.g. when multiple deaths trigger CheckWinState).</summary>
        private bool _roundEndProcessed;

        // ── Online multiplayer ───────────────────────────────────────────────────────
        /// <summary>Maps NGO client IDs → arena player IDs. Host-only.</summary>
        private readonly Dictionary<ulong, int> _clientToPlayerId = new Dictionary<ulong, int>();

        // ── Training episode reset ───────────────────────────────────────────────────
        // Captured when entering the arena so ResetArenaForTraining()/ResetArenaForNewRound()
        // can restore the arena without reloading the scene.
        private Tilemap _destructibleTilemap;
        private Tilemap _indestructibleTilemap;
        private readonly Dictionary<Vector3Int, TileBase> _initialDestructibleTiles = new Dictionary<Vector3Int, TileBase>();
        private readonly Dictionary<GameObject, Vector3> _initialPlayerLocalPositionsForTraining = new Dictionary<GameObject, Vector3>();
        private readonly Dictionary<GameObject, Vector3> _initialPlayerLocalPositionsForNonTraining = new Dictionary<GameObject, Vector3>();

        private bool _initialArenaCaptured;
        private bool _initialArenaCapturedForTraining;
        private bool _randomizeSpawnPositions;

        public void SetRandomizeSpawnPositions(bool value) => _randomizeSpawnPositions = value;
        private bool _lastCapturedNormalLevel;

        // ── Arena object registries ──────────────────────────────────────────────────
        // BombInfo, Explosion, and ItemPickup self-register here so agents can do an
        // O(1) list read instead of a scene-wide FindObjectsByType scan every step.
        private readonly List<BombInfo>   _arenaBombs      = new List<BombInfo>();
        private readonly List<Explosion>  _arenaExplosions  = new List<Explosion>();
        private readonly List<ItemPickup> _arenaItems       = new List<ItemPickup>();

        public IReadOnlyList<BombInfo>   ArenaBombs      => _arenaBombs;
        public IReadOnlyList<Explosion>  ArenaExplosions  => _arenaExplosions;
        public IReadOnlyList<ItemPickup> ArenaItems       => _arenaItems;

        public void RegisterBomb(BombInfo b)         { if (b != null && !_arenaBombs.Contains(b))       _arenaBombs.Add(b); }
        public void UnregisterBomb(BombInfo b)       => _arenaBombs.Remove(b);
        public void RegisterExplosion(Explosion e)   { if (e != null && !_arenaExplosions.Contains(e))  _arenaExplosions.Add(e); }
        public void UnregisterExplosion(Explosion e) => _arenaExplosions.Remove(e);
        public void RegisterItem(ItemPickup item)    { if (item != null && !_arenaItems.Contains(item)) _arenaItems.Add(item); }
        public void UnregisterItem(ItemPickup item)  => _arenaItems.Remove(item);

        private void Awake()
        {
            // Soft singleton: first arena wins. Multi-arena training scenes use local references instead.
            if (Instance == null)
                Instance = this;
        }

        private void OnDisable()
        {
            CancelInvoke(); // Cancel pending round-end transitions (e.g. Standings after 3s delay).
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            _roundEndProcessed = false;
            float t0 = Time.realtimeSinceStartup;
            UnityEngine.Debug.Log($"[GameManager] OnEnable() began at t={t0:F2}s (Game flow state active)");
            if (TrainingMode.IsActive)
                UnityEngine.Debug.Log("[GameManager] ML-Agents training: this scene (Game/Train) must be the one that loads when you press Play, or the Python trainer will timeout. Open the Game (or Train) scene, then press Play.");

            int playerCount = PlayerPrefs.GetInt("Players", 2);
            if (playerCount <= 0)
            {
                UnityEngine.Debug.LogWarning($"[GameManager] PlayerPrefs Players was {playerCount}; clamping to 2.");
                playerCount = 2;
                PlayerPrefs.SetInt("Players", playerCount);
                PlayerPrefs.Save();
            }
            else if (playerCount > 5)
            {
                UnityEngine.Debug.LogWarning($"[GameManager] PlayerPrefs Players was {playerCount}; clamping to 5.");
                playerCount = 5;
                PlayerPrefs.SetInt("Players", playerCount);
                PlayerPrefs.Save();
            }

            // #region agent log
            AgentDebugNdjson_88a510.Log(
                hypothesisId: "H1-playerCount",
                location: "GameManager.OnEnable",
                message: "playerCount read",
                dataJsonObject:
                    "{\"playerCount\":" + playerCount +
                    ",\"trainingMode\":" + (TrainingMode.IsActive ? "true" : "false") +
                    ",\"sessionManagerPresent\":" + (SessionManager.Instance != null ? "true" : "false") + "}",
                runId: "pre"
            );
            // #endregion

            players = DiscoverPlayersForCount(playerCount);

            // Ensure SessionManager has structure for this game (e.g. first round from menu); do not re-initialize when returning from shop
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

            // Controller check is done in Menu; empty slots become AI. When SessionManager is null
            // (e.g. Game scene opened directly), we skip device assignment so AttachInputProvider gives all slots AI.
            if (!TrainingMode.IsActive && SessionManager.Instance != null)
            {
                SessionManager.Instance.AssignInputDevices(playerCount);
                SessionManager.Instance.ApplyShopControllerOverride(playerCount);
            }

            SetupPlayers(playerCount);

            // 🔹 Force upgrades and wins to be reapplied every new game round (only for players in this game)
            foreach (var p in players)
            {
                if (p == null)
                    continue;
                var pc = p.GetComponent<PlayerController>();
                if (pc != null && pc.playerId > 0 && SessionManager.Instance != null)
                {
                    pc.wins = SessionManager.Instance.GetWins(pc.playerId);
                    pc.ApplyUpgrades();
                    var bc = p.GetComponent<BombController>();
                    if (bc != null)
                        bc.ApplyUpgrades(pc.playerId);
                }
            }

            // Load settings; in training mode shrinking and start money are always off,
            // but map layout is controlled by TrainingAcademyHelper via PlayerPrefs
            shrinkingEnabled = !TrainingMode.IsActive && PlayerPrefs.GetInt("Shrinking", 1) == 1;
            normalLevel = PlayerPrefs.GetInt("NormalLevel", 1) == 1;
            startMoney = !TrainingMode.IsActive && PlayerPrefs.GetInt("StartMoney", 0) == 1;

            if (!normalLevel)
                LoadAlternateLevelSettings();

            // In single-scene mode we toggle the Game root, so Unity won't re-run MapSelector.Awake/Start.
            // Re-apply the selected map variant and (optionally) restart the match timer every time we enter Game.
            var mapSelector = GetComponentInChildren<MapSelector>(true);
            mapSelector?.Apply(normalLevel);

            // Recentre letterbox camera on active arena tilemaps (single-scene flow does not reload scene).
            var letterbox = FindFirstObjectByType<AmigaLetterboxCamera>();
            letterbox?.RefreshAndApply();

            // Start money only on first game from menu, not when returning from shop for another round
            if (startMoney && PlayerPrefs.GetInt("GiveStartMoneyNextArena", 0) == 1)
            {
                GivePlayersStartCoin();
                PlayerPrefs.SetInt("GiveStartMoneyNextArena", 0);
                PlayerPrefs.Save();
            }

            // Capture the initial arena state when needed:
            // - training: capture once for ResetArenaForTraining()
            // - non-training single-scene mode: capture so ResetArenaForNewRound() can restore layout.
            bool currentNormalLevel = PlayerPrefs.GetInt("NormalLevel", 1) == 1;

            if (
                !_initialArenaCaptured
                || _initialArenaCapturedForTraining != TrainingMode.IsActive
                || _lastCapturedNormalLevel != currentNormalLevel
            )
            {
                CaptureInitialArenaState();
                _initialArenaCaptured = true;
                _initialArenaCapturedForTraining = TrainingMode.IsActive;
                _lastCapturedNormalLevel = currentNormalLevel;
            }

            if (!TrainingMode.IsActive)
                ResetArenaForNewRound();

            if (applyHybridDestructibleThinningAfterLoad)
                StartCoroutine(ApplyHybridSceneDestructibleThinningDeferred());

            UnityEngine.Debug.Log($"[GameManager] Start() finished at t={Time.realtimeSinceStartup:F2}s (took {Time.realtimeSinceStartup - t0:F2}s)");
        }

        /// <summary>
        /// Deferred so <see cref="HybridArenaGrid"/> runs <c>Start</c> first (initial <c>BuildGrid</c>).
        /// Guarded so automatic Start-time thinning is not applied twice.
        /// </summary>
        private static IEnumerator ApplyHybridSceneDestructibleThinningDeferred()
        {
            yield return null;
            var grid = HybridArenaGrid.Instance;
            if (grid == null || !grid.thinSceneDestructibles || !grid.thinOnlyWhenInvokedExplicitly)
                yield break;
            grid.ApplySceneDestructibleThinning();
        }

        void SetupPlayers(int count)
        {
            if (players == null || players.Length != count)
                players = DiscoverPlayersForCount(count);

            // Disable all slots we discovered (they may still be active from previous rounds).
            if (players != null)
            {
                for (int i = 0; i < players.Length; i++)
                    players[i]?.SetActive(false);
            }

            int nullCount = 0;
            if (players != null)
            {
                for (int i = 0; i < players.Length; i++)
                    if (players[i] == null)
                        nullCount++;
            }

            // #region agent log
            AgentDebugNdjson_88a510.Log(
                hypothesisId: "H3-nullPlayers",
                location: "GameManager.SetupPlayers",
                message: "players discovered after discovery",
                dataJsonObject:
                    "{\"requestedCount\":" + count +
                    ",\"playersLen\":" + (players != null ? players.Length : -1) +
                    ",\"nullCount\":" + nullCount + "}",
                runId: "pre"
            );
            // #endregion

            // Enable players in id order (ArenaLogic.GetPlayerSetup maps 1..count deterministically to slots).
            for (int playerId = 1; playerId <= count; playerId++)
            {
                var playerObj = (players != null && playerId - 1 < players.Length) ? players[playerId - 1] : null;
                if (playerObj != null)
                    EnablePlayer(playerObj, playerId);
                else
                    UnityEngine.Debug.LogWarning($"[GameManager] Missing player GameObject for playerId={playerId} (count={count}).");
            }
        }

        private GameObject[] DiscoverPlayersForCount(int count)
        {
            var result = new GameObject[count];
            if (count <= 0)
                return result;

            // Some scenes only have `PlayerDualModeController` (no arena-only `PlayerController`).
            // We always want to discover the *objects we can attach HumanPlayerInput to*.
            var candidates = new List<PlayerCandidate>();

            var dualModes = FindObjectsByType<PlayerDualModeController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (dualModes != null && dualModes.Length > 0)
            {
                foreach (var c in dualModes)
                {
                    if (c == null) continue;
                    candidates.Add(new PlayerCandidate { go = c.gameObject, playerId = c.playerId, localPos = c.transform.localPosition });
                }
            }
            else
            {
                var controllers = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (controllers != null && controllers.Length > 0)
                {
                    foreach (var c in controllers)
                    {
                        if (c == null) continue;
                        candidates.Add(new PlayerCandidate { go = c.gameObject, playerId = c.playerId, localPos = c.transform.localPosition });
                    }
                }
            }

            if (candidates.Count == 0)
            {
                // #region agent log
                AgentDebugNdjson_88a510.Log(
                    hypothesisId: "H2-noPlayerControllersFound",
                    location: "GameManager.DiscoverPlayersForCount",
                    message: "no player candidates found (dual-mode + PlayerController absent)",
                    dataJsonObject: "{\"count\":" + count + ",\"candidatesFound\":0}",
                    runId: "pre"
                );
                // #endregion

                UnityEngine.Debug.LogWarning("[GameManager] DiscoverPlayersForCount: no player candidates found (dual-mode or PlayerController).");
                return result;
            }

            // 1) Preferred: use playerId mapping if present/complete.
            var byId = new Dictionary<int, GameObject>();
            foreach (var c in candidates)
            {
                if (c.playerId < 1 || c.playerId > count) continue;
                if (!byId.ContainsKey(c.playerId))
                    byId[c.playerId] = c.go;
            }

            bool complete = true;
            int byIdMissingCount = 0;
            for (int id = 1; id <= count; id++)
            {
                if (!byId.ContainsKey(id) || byId[id] == null)
                {
                    complete = false;
                    byIdMissingCount++;
                }
            }

            // #region agent log
            AgentDebugNdjson_88a510.Log(
                hypothesisId: "H2-idMismatchOrNoControllers",
                location: "GameManager.DiscoverPlayersForCount",
                message: "candidates found and byId completeness",
                dataJsonObject:
                    "{\"count\":" + count +
                    ",\"candidatesFound\":" + candidates.Count +
                    ",\"byIdComplete\":" + (complete ? "true" : "false") +
                    ",\"byIdMissingCountApprox\":" + byIdMissingCount + "}",
                runId: "pre"
            );
            // #endregion

            if (complete)
            {
                for (int id = 1; id <= count; id++)
                    result[id - 1] = byId[id];
                return result;
            }

            // 2) Fallback: spatial heuristics (use row extremes + left/right).
            const float eps = 0.01f;

            float maxY = float.MinValue;
            float minY = float.MaxValue;
            float centerX = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                var lp = candidates[i].localPos;
                maxY = Mathf.Max(maxY, lp.y);
                minY = Mathf.Min(minY, lp.y);
                centerX += lp.x;
            }
            centerX /= Mathf.Max(1, candidates.Count);

            var topRow = new List<PlayerCandidate>();
            var bottomRow = new List<PlayerCandidate>();
            foreach (var c in candidates)
            {
                var lp = c.localPos;
                if (Mathf.Abs(lp.y - maxY) <= eps) topRow.Add(c);
                else if (Mathf.Abs(lp.y - minY) <= eps) bottomRow.Add(c);
            }

            PlayerCandidate PickTopLeft()
            {
                PlayerCandidate best = default;
                float bestX = float.MaxValue;
                for (int i = 0; i < topRow.Count; i++)
                {
                    float x = topRow[i].localPos.x;
                    if (x < bestX)
                    {
                        bestX = x;
                        best = topRow[i];
                    }
                }
                return best;
            }

            PlayerCandidate PickTopRight()
            {
                PlayerCandidate best = default;
                float bestX = float.MinValue;
                for (int i = 0; i < topRow.Count; i++)
                {
                    float x = topRow[i].localPos.x;
                    if (x > bestX)
                    {
                        bestX = x;
                        best = topRow[i];
                    }
                }
                return best;
            }

            PlayerCandidate PickBottomLeft()
            {
                PlayerCandidate best = default;
                float bestX = float.MaxValue;
                for (int i = 0; i < bottomRow.Count; i++)
                {
                    float x = bottomRow[i].localPos.x;
                    if (x < bestX)
                    {
                        bestX = x;
                        best = bottomRow[i];
                    }
                }
                return best;
            }

            PlayerCandidate PickBottomRight()
            {
                PlayerCandidate best = default;
                float bestX = float.MinValue;
                for (int i = 0; i < bottomRow.Count; i++)
                {
                    float x = bottomRow[i].localPos.x;
                    if (x > bestX)
                    {
                        bestX = x;
                        best = bottomRow[i];
                    }
                }
                return best;
            }

            PlayerCandidate PickMiddle()
            {
                float midY = (maxY + minY) * 0.5f;
                PlayerCandidate best = default;
                float bestScore = float.MaxValue;
                for (int i = 0; i < candidates.Count; i++)
                {
                    var c = candidates[i];
                    var lp = c.localPos;
                    if (Mathf.Abs(lp.y - maxY) <= eps || Mathf.Abs(lp.y - minY) <= eps)
                        continue;

                    float dy = Mathf.Abs(lp.y - midY);
                    float dx = Mathf.Abs(lp.x - centerX);
                    float score = dy + dx * 0.5f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = c;
                    }
                }
                return best;
            }

            if (count == 1)
                result[0] = PickTopLeft().go;
            else if (count == 2)
            {
                // ArenaLogic: id1=TopLeft, id2=BottomRight
                result[0] = PickTopLeft().go;
                result[1] = PickBottomRight().go;
            }
            else if (count == 3)
            {
                // ArenaLogic: id1=TopLeft, id2=BottomRight, id3=Middle
                result[0] = PickTopLeft().go;
                result[1] = PickBottomRight().go;
                result[2] = PickMiddle().go;
            }
            else if (count == 4)
            {
                // ArenaLogic: id1=TopLeft, id2=TopRight, id3=BottomLeft, id4=BottomRight
                result[0] = PickTopLeft().go;
                result[1] = PickTopRight().go;
                result[2] = PickBottomLeft().go;
                result[3] = PickBottomRight().go;
            }
            else
            {
                // count >= 5: id1=TopLeft, id2=TopRight, id3=BottomLeft, id4=BottomRight, id5=Middle
                result[0] = PickTopLeft().go;
                result[1] = PickTopRight().go;
                result[2] = PickBottomLeft().go;
                result[3] = PickBottomRight().go;
                result[4] = PickMiddle().go;
            }

            return result;
        }

        GameObject GetPlayerObject(PlayerSlot slot)
        {
            switch (slot)
            {
                case PlayerSlot.TopLeft:
                    return topLeftPlayer;
                case PlayerSlot.TopRight:
                    return topRightPlayer;
                case PlayerSlot.BottomLeft:
                    return bottomLeftPlayer;
                case PlayerSlot.BottomRight:
                    return bottomRightPlayer;
                case PlayerSlot.Middle:
                    return middlePlayer;
                default:
                    return null;
            }
        }

        private void EnablePlayer(GameObject playerObj, int id)
        {
            var dualMode = playerObj.GetComponent<PlayerDualModeController>();
            if (dualMode != null)
                dualMode.playerId = id;

            var movement = playerObj.GetComponent<PlayerController>();
            if (movement != null)
            {
                movement.playerId = id;
                movement.wins =
                    SessionManager.Instance != null ? SessionManager.Instance.GetWins(id) : 0;
            }

            AttachInputProvider(playerObj, id, movement);

            // Ensure any scene/prefab offsets don't lift the player off the arena plane.
            var lp = playerObj.transform.localPosition;
            lp.y = ArenaGrid3D.GridOrigin.y;
            playerObj.transform.localPosition = lp;

            playerObj.SetActive(true);
            StartCoroutine(ClampPlayerFeetToFloorNextFrame(playerObj));
        }

        private void AttachInputProvider(GameObject playerObj, int id, PlayerController movement)
        {
            // Remove any existing input components so we don't duplicate when re-entering scene
            var existingHuman = playerObj.GetComponent<HumanPlayerInput>();
            if (existingHuman != null)
                Destroy(existingHuman);
            var existingAI = playerObj.GetComponent<AIPlayerInput>();
            if (existingAI != null)
                Destroy(existingAI);
            var existingBrain = playerObj.GetComponent<ScriptedAIBrain>();
            if (existingBrain != null)
                Destroy(existingBrain);
            var existingMLAgent = playerObj.GetComponent<BombermanAgent>();
            if (existingMLAgent != null)
                Destroy(existingMLAgent);
            var existingMLBrain = playerObj.GetComponent<MLAgentsBrain>();
            if (existingMLBrain != null)
                Destroy(existingMLBrain);

            // Session device index (1+ = gamepad). Do not null this out when TrainingMode is active:
            // TrainingMode uses scene/CLI heuristics that can disagree with AssignInputDevices timing;
            // skipping the lookup forces ScriptedAIBrain for everyone even after AssignInputDevices ran.
            int? device = SessionManager.Instance != null ? SessionManager.Instance.GetAssignedDevice(id) : null;

            // #region agent log
            AgentDebugNdjson_88a510.Log(
                hypothesisId: "H4-deviceAssignmentForPlayerId",
                location: "GameManager.AttachInputProvider",
                message: "device assignment queried",
                dataJsonObject:
                    "{\"playerId\":" + id +
                    ",\"deviceAssigned\":" + (device.HasValue ? "true" : "false") +
                    ",\"deviceIndex\":" + (device.HasValue ? device.Value : -1) + "}",
                runId: "pre"
            );
            // #endregion

            UnityEngine.Debug.Log(
                $"[GameManager] AttachInputProvider playerId={id} trainingMode={TrainingMode.IsActive} " +
                $"session={(SessionManager.Instance != null ? "ok" : "null")} assignedDevice={(device.HasValue ? device.Value.ToString() : "none")}");

            if (device.HasValue)
            {
                var human = playerObj.AddComponent<HumanPlayerInput>();
                var asset = playerInputActions != null ? playerInputActions : Resources.Load<InputActionAsset>("PlayerControls");
                if (asset != null)
                    human.inputActions = asset;
                else
                    UnityEngine.Debug.LogWarning("[GameManager] No Input Action Asset for human player. Assign GameManager's 'Player Input Actions' in the Game scene, or put PlayerControls.inputactions in a Resources folder.");

                // Lock this player to their specific gamepad so controllers don't cross-control.
                int gpIndex = device.Value - 1;
                if (gpIndex >= 0 && gpIndex < Gamepad.all.Count)
                    human.SetGamepad(Gamepad.all[gpIndex]);

                KeyCode up = KeyCode.W, down = KeyCode.S, left = KeyCode.A, right = KeyCode.D;
                KeyCode bombKey = playerObj.GetComponent<BombController>()?.inputKey ?? KeyCode.LeftShift;
                if (movement != null)
                {
                    up = movement.inputUp;
                    down = movement.inputDown;
                    left = movement.inputLeft;
                    right = movement.inputRight;
                }

                human.Init(device.Value, up, down, left, right, bombKey, bombKey);
                UnityEngine.Debug.Log($"[GameManager] Player {id} → HumanPlayerInput (device {device.Value}, PlayerController={(movement != null ? "yes" : "no")})");
            }
            else
            {
                // Player 2+ in training: always a static dummy regardless of other settings.
                if (TrainingMode.IsActive && id != 1)
                {
                    playerObj.AddComponent<Player.StaticPlayerInput>();
                    UnityEngine.Debug.Log($"[GameManager] Player {id} → StaticPlayerInput (training dummy target)");
                }
                // useHeuristicOnlyForAgents = true  → ScriptedAIBrain (no Python trainer needed; instant visual testing).
                // useHeuristicOnlyForAgents = false → BombermanAgent + Default behavior (connect mlagents-learn to train).
                else if (useHeuristicOnlyForAgents)
                {
                    var brain = playerObj.AddComponent<ScriptedAIBrain>();
                    var aiInput = playerObj.AddComponent<AIPlayerInput>();
                    aiInput.Init(brain);
                    UnityEngine.Debug.Log($"[GameManager] Player {id} → ScriptedAIBrain (heuristic preview mode)");
                }
                else
                {
                    var agent = playerObj.AddComponent<BombermanAgent>();
                    var mlBrain = playerObj.AddComponent<MLAgentsBrain>();
                    var aiInput = playerObj.AddComponent<AIPlayerInput>();
                    aiInput.Init(mlBrain);
                    UnityEngine.Debug.Log($"[GameManager] Player {id} → BombermanAgent (RL training mode), TrainingMode={TrainingMode.IsActive}");
                }
            }
        }

        /// <summary>
        /// Hybrid FPS players often remain active in the hierarchy when dead; use <see cref="Health.IsDead"/>.
        /// </summary>
        private static bool IsSlotAliveForArenaRound(GameObject p)
        {
            if (p == null || !p.activeSelf)
                return false;
            var health = p.GetComponent<Health>();
            return health == null || !health.IsDead;
        }

        public void CheckWinState()
        {
            // In online play, only the host drives win-state and scene transitions.
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer)
                return;

            if (players == null || players.Length == 0)
                return;
            if (_roundEndProcessed)
                return;

            var playerActive = new bool[players.Length];
            int lastAlivePlayerId = 0;
            int lastAlivePcWins = 0;
            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                playerActive[i] = IsSlotAliveForArenaRound(p);
                if (!playerActive[i] || p == null)
                    continue;

                var pc = p.GetComponent<PlayerController>();
                if (pc != null)
                {
                    lastAlivePlayerId = pc.playerId;
                    lastAlivePcWins = pc.wins;
                }
            }

            // Use SessionManager as source of truth for win count when deciding Overs vs Standings
            int currentWinsOfLastAlive =
                (lastAlivePlayerId != 0 && SessionManager.Instance != null)
                    ? SessionManager.Instance.GetWins(lastAlivePlayerId)
                    : lastAlivePcWins;

            int winsNeeded = PlayerPrefs.GetInt("WinsNeeded", 3);
            var result = ArenaLogic.EvaluateWinState(
                playerActive,
                currentWinsOfLastAlive,
                winsNeeded
            );

            if (result.Outcome == WinOutcome.NoChange)
                return;

            // When exactly one player is left, we always transition to Standings or Overs.
            if (result.LastAliveIndex.HasValue)
            {
                _roundEndProcessed = true;
                var lastAlive = players[result.LastAliveIndex.Value];
                var movement = lastAlive.GetComponent<PlayerController>();
                if (movement != null)
                {
                    if (SessionManager.Instance != null)
                    {
                        SessionManager.Instance.AddWin(movement.playerId);
                        movement.wins = SessionManager.Instance.GetWins(movement.playerId);
                    }
                    else
                        movement.wins++;

                    // Defensive: re-check from SessionManager in case we were given stale wins
                    int winsAfterThisRound =
                        SessionManager.Instance != null
                            ? SessionManager.Instance.GetWins(movement.playerId)
                            : movement.wins;
                    bool shouldGoToOvers = winsAfterThisRound >= winsNeeded;

                    if (result.Outcome == WinOutcome.GoToOvers || shouldGoToOvers)
                    {
                        if (TrainingMode.IsActive)
                        {
                            var winnerAgent = lastAlive.GetComponent<BombermanAgent>();
                            if (winnerAgent != null)
                                winnerAgent.AddReward(0.5f);
                            Invoke(nameof(ResetTrainingEpisode), 0.5f);
                            return;
                        }
                        if (SessionManager.Instance != null)
                            SessionManager.Instance.SetMatchWinner(
                                movement.playerId,
                                lastAlive.name
                            );
                        ClearMatchTransientObjects();
                        if (IsNetworked)
                            GoToOversClientRpc();
                        else
                            SceneFlowManager.I.GoToOvers();
                        return;
                    }
                }
            }

            if (result.Outcome != WinOutcome.NoChange)
                _roundEndProcessed = true;

            if (TrainingMode.IsActive)
            {
                Invoke(nameof(ResetTrainingEpisode), 0.5f);
                return;
            }
            else if (PlayerPrefs.GetInt("QuickRestart", 0) == 1)
                ReloadRoundAfterDelay(3f);
            else
                Invoke(nameof(Standings), 3f);
        }

        private void ReloadGameSceneAfterDelay(float delay)
        {
            Invoke(nameof(ReloadGameScene), delay);
        }

        private void CaptureInitialArenaState()
        {
            // Save spawn positions:
            // - training: only for players active at capture time (avoid enabling extra slots later)
            // - non-training: for all player slots (we only reset the active subset later)
            _initialPlayerLocalPositionsForTraining.Clear();
            _initialPlayerLocalPositionsForNonTraining.Clear();

            foreach (var p in players)
            {
                if (p == null) continue;
                // Scene instances can have stray Y offsets. Treat the arena as XZ only and
                // clamp initial spawn positions to the arena floor plane.
                var lp = p.transform.localPosition;
                lp.y = ArenaGrid3D.GridOrigin.y;
                _initialPlayerLocalPositionsForNonTraining[p] = lp;

                if (TrainingMode.IsActive && p.activeInHierarchy)
                    _initialPlayerLocalPositionsForTraining[p] = lp;
            }

            // Save every tile in the destructible tilemap.
            BombController bc = null;
            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (p == null) continue;
                var candidate = p.GetComponent<BombController>();
                if (candidate != null && candidate.destructibleTiles != null)
                {
                    bc = candidate;
                    break;
                }
            }

            if (bc == null)
            {
                // Scene authoring fallback: pick any BombController that owns the destructible tilemaps.
                var bombs = FindObjectsByType<BombController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < bombs.Length; i++)
                {
                    var candidate = bombs[i];
                    if (candidate != null && candidate.destructibleTiles != null)
                    {
                        bc = candidate;
                        break;
                    }
                }
            }

            if (bc != null && bc.destructibleTiles != null)
            {
                _destructibleTilemap    = bc.destructibleTiles;
                _indestructibleTilemap  = bc.indestructibleTiles;
                _initialDestructibleTiles.Clear();
                foreach (var pos in _destructibleTilemap.cellBounds.allPositionsWithin)
                {
                    var tile = _destructibleTilemap.GetTile(pos);
                    if (tile != null) _initialDestructibleTiles[pos] = tile;
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("[GameManager] CaptureInitialArenaState: could not find destructible tilemap.");
            }
        }

        /// <summary>
        /// Destroys bombs, explosions, pickups, debris, and any orphan arena spawns still at scene root.
        /// Call when leaving the match (Standings/Shop/Overs) so single-scene mode does not leave world sprites over UI.
        /// </summary>
        public void ClearMatchTransientObjects()
        {
            for (int i = _arenaBombs.Count - 1; i >= 0; i--)
                if (_arenaBombs[i] != null)
                    Destroy(_arenaBombs[i].gameObject);
            _arenaBombs.Clear();

            for (int i = _arenaExplosions.Count - 1; i >= 0; i--)
                if (_arenaExplosions[i] != null)
                    Destroy(_arenaExplosions[i].gameObject);
            _arenaExplosions.Clear();

            for (int i = _arenaItems.Count - 1; i >= 0; i--)
                if (_arenaItems[i] != null)
                    Destroy(_arenaItems[i].gameObject);
            _arenaItems.Clear();

            // Loose Instantiate(..., no parent) pickups / VFX — not deactivated with the Game root.
            foreach (var ip in FindObjectsByType<ItemPickup>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None
                     ))
            {
                if (ip != null)
                    Destroy(ip.gameObject);
            }

            foreach (var ex in FindObjectsByType<Explosion>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None
                     ))
            {
                if (ex != null)
                    Destroy(ex.gameObject);
            }

            foreach (var d in FindObjectsByType<Destructible>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None
                     ))
            {
                if (d == null)
                    continue;
                // Explosion debris (isDebris) and Superman pushable blocks (runtime Instantiate → "(Clone)").
                if (d.isDebris || d.gameObject.name.Contains("(Clone)"))
                    Destroy(d.gameObject);
            }

            // 3D hybrid arena: item drops are ItemPickup3D (not ItemPickup) and are often parented under the grid.
            foreach (var ip3 in FindObjectsByType<ItemPickup3D>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None
                     ))
            {
                if (ip3 == null) continue;
                if (ip3.TryGetComponent<NetworkObject>(out var netObj)
                    && netObj.IsSpawned
                    && NetworkManager.Singleton != null
                    && NetworkManager.Singleton.IsServer)
                    netObj.Despawn(true);
                else
                    Destroy(ip3.gameObject);
            }

            // Loose explosion visuals from BombController3D (not legacy Explosion).
            foreach (var ex3 in FindObjectsByType<Explosion3D>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None
                     ))
            {
                if (ex3 != null)
                    Destroy(ex3.gameObject);
            }
        }

        /// <summary>
        /// Resets this arena for a new training episode without reloading the scene.
        /// Called from BombermanAgent.OnEpisodeBegin().
        /// </summary>
        public void ResetArenaForTraining()
        {
            _roundEndProcessed = false;

            ClearMatchTransientObjects();

            // Restore the destructible tilemap to its initial layout.
            if (_destructibleTilemap != null && _initialDestructibleTiles.Count > 0)
            {
                _destructibleTilemap.ClearAllTiles();
                foreach (var kvp in _initialDestructibleTiles)
                    _destructibleTilemap.SetTile(kvp.Key, kvp.Value);
            }

            // Re-enable players, optionally placing them at fully random grid cells.
            var spawnPlayers   = new List<GameObject>(_initialPlayerLocalPositionsForTraining.Keys);
            var spawnPositions = new List<Vector3>(_initialPlayerLocalPositionsForTraining.Values);

            List<Vector3> worldPositions = _randomizeSpawnPositions
                ? GetRandomSpawnWorldPositions(spawnPlayers)
                : null;

            for (int i = 0; i < spawnPlayers.Count; i++)
            {
                var p = spawnPlayers[i];
                if (p == null) continue;

                var pc = p.GetComponent<PlayerController>();
                if (pc != null) pc.ResetForEpisode();

                if (worldPositions != null)
                {
                    // Random world pos (tilemap-based) — align player's feet to the arena plane.
                    var wp = AdjustSpawnToFloor(p, worldPositions[i]);
                    p.transform.position = wp;
                }
                else
                {
                    // Fixed slot shuffle — stored as local positions; convert to world and align to plane.
                    var lp = spawnPositions[i];
                    if (p.transform.parent != null)
                        p.transform.position = AdjustSpawnToFloor(p, p.transform.parent.TransformPoint(lp));
                    else
                        p.transform.position = AdjustSpawnToFloor(p, lp);
                }

                if (!p.activeInHierarchy) p.SetActive(true);
                if (pc != null) pc.enabled = true;
                StartCoroutine(ClampPlayerFeetToFloorNextFrame(p));

                var rb = p.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;

                var bc = p.GetComponent<BombController>();
                if (bc != null) bc.enabled = true; // OnEnable resets bombsRemaining
            }
        }

        /// <summary>
        /// Picks one clear world-space position per player on the playable grid.
        /// A cell is clear if it has no indestructible tile, no destructible tile, and no
        /// other player has already claimed it. Falls back to the player's current position
        /// after 200 failed attempts.
        /// </summary>
        private List<Vector3> GetRandomSpawnWorldPositions(List<GameObject> spawnPlayers)
        {
            if (_indestructibleTilemap == null || _destructibleTilemap == null)
            {
                UnityEngine.Debug.LogWarning("[GameManager] GetRandomSpawnWorldPositions: tilemap(s) not captured — falling back to current positions.");
                var fallback = new List<Vector3>(spawnPlayers.Count);
                foreach (var p in spawnPlayers)
                    fallback.Add(p != null ? p.transform.position : Vector3.zero);
                return fallback;
            }

            // Playable bounds (mirrors DestructibleGenerator conventions).
            var bounds = _indestructibleTilemap.cellBounds;
            int xMin = bounds.xMin + 1;
            int xMax = bounds.xMax - 2;
            int yMin = bounds.yMin + 1;
            int yMax = bounds.yMax - 2;

            var claimed = new List<Vector3Int>(spawnPlayers.Count);
            var result  = new List<Vector3>(spawnPlayers.Count);

            foreach (var p in spawnPlayers)
            {
                Vector3Int chosen = Vector3Int.zero;
                bool found = false;

                for (int attempt = 0; attempt < 200; attempt++)
                {
                    int x = UnityEngine.Random.Range(xMin, xMax + 1);
                    int y = UnityEngine.Random.Range(yMin, yMax + 1);
                    var cell = new Vector3Int(x, y, 0);

                    if (_indestructibleTilemap.GetTile(cell) != null)  continue; // wall
                    if (_destructibleTilemap.GetTile(cell) != null)    continue; // breakable block
                    if (claimed.Contains(cell))                        continue; // taken by earlier player

                    chosen = cell;
                    found  = true;
                    break;
                }

                if (found)
                {
                    claimed.Add(chosen);
                    var wp = _destructibleTilemap.CellToWorld(chosen) + _destructibleTilemap.tileAnchor;
                    result.Add(AdjustSpawnToFloor(p, wp));
                }
                else
                {
                    // Fallback: keep the player where they currently are (but clamp to floor).
                    var wp = p != null ? p.transform.position : Vector3.zero;
                    result.Add(AdjustSpawnToFloor(p, wp));
                }
            }

            return result;
        }

        private void ResetArenaForNewRound()
        {
            _roundEndProcessed = false;

            ClearMatchTransientObjects();

            // Restore the destructible tilemap to its initial layout.
            if (_destructibleTilemap != null && _initialDestructibleTiles.Count > 0)
            {
                _destructibleTilemap.ClearAllTiles();
                foreach (var kvp in _initialDestructibleTiles)
                    _destructibleTilemap.SetTile(kvp.Key, kvp.Value);
            }

            // Reset only the currently active player subset for this round.
            foreach (var kvp in _initialPlayerLocalPositionsForNonTraining)
            {
                var p = kvp.Key;
                if (p == null) continue;
                if (!p.activeInHierarchy) continue;

                var pc = p.GetComponent<PlayerController>();
                if (pc != null) pc.ResetForEpisode();

                p.transform.localPosition = kvp.Value;

                if (pc != null) pc.enabled = true;

                var bc = p.GetComponent<BombController>();
                if (bc != null) bc.enabled = true; // OnEnable resets bombsRemaining
            }
        }

        /// <summary>
        /// Ends the current training episode. If a BombermanAgent exists, EndEpisode() handles
        /// the reset via OnEpisodeBegin(). Otherwise (ScriptedAIBrain / heuristic mode) reset directly.
        /// </summary>
        private void ResetTrainingEpisode()
        {
            foreach (var p in players)
            {
                if (p == null) continue;
                var agent = p.GetComponent<BombermanAgent>();
                if (agent != null)
                {
                    agent.EndEpisode(); // triggers OnEpisodeBegin → ResetArenaForTraining
                    return;
                }
            }
            // No BombermanAgent (ScriptedAIBrain mode) — reset the arena directly
            ResetArenaForTraining();
        }

        private void ReloadGameScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>Load Countdown scene to start the next round (skip Standings/Shop). Used when QuickRestart is enabled.</summary>
        private void ReloadRoundAfterDelay(float delay)
        {
            Invoke(nameof(GoToCountdown), delay);
        }

        private void GoToCountdown()
        {
            ClearMatchTransientObjects();
            SceneFlowManager.I.GoTo(FlowState.Countdown);
        }

        /// <summary>True when NGO is active (host/server/client session running).</summary>
        private bool IsNetworked =>
            NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        private void Standings()
        {
            SyncCoinsToSessionManager();
            if (IsNetworked)
                GoToStandingsClientRpc();
            else
                SceneFlowManager.I.GoTo(FlowState.Standings);
        }

        // ------------------- Custom behaviours -------------------

        private void EndGame()
        {
            UnityEngine.Debug.Log("[GameManager] Timer expired → game over!");
            if (TrainingMode.IsActive)
                return;   // individual episode resets are handled by Academy / BombermanAgent.OnEpisodeBegin
            if (IsNetworked && !IsServer)
                return;
            ClearMatchTransientObjects();
            SyncCoinsToSessionManager();
            if (IsNetworked)
                GoToStandingsClientRpc();
            else
                SceneFlowManager.I.GoTo(FlowState.Standings);
        }

        void LoadAlternateLevelSettings()
        {
            UnityEngine.Debug.Log("[GameManager] NormalLevel disabled → loading alternate map settings");
            // TODO: load alternate spawn points, layouts, etc.
        }

        void GivePlayersStartCoin()
        {
            if (SessionManager.Instance == null)
                return;
            if (players == null)
                return;

            foreach (var playerObj in players)
            {
                if (playerObj == null)
                    continue;

                var movement = playerObj.GetComponent<PlayerController>();
                if (movement == null)
                    continue;

                int playerId = movement.playerId;
                SessionManager.Instance.AddCoins(playerId, 1);
                movement.coins = SessionManager.Instance.GetCoins(playerId);
                UnityEngine.Debug.Log(
                    $"{playerObj.name} (Player {playerId}) given 1 start coin (total: {movement.coins})"
                );
            }
        }

        /// <summary>Push each active player's in-memory coins to SessionManager so Shop sees current totals.</summary>
        void SyncCoinsToSessionManager()
        {
            if (SessionManager.Instance == null || players == null)
                return;
            foreach (var p in players)
            {
                if (p == null || !p.activeInHierarchy)
                    continue;
                var pc = p.GetComponent<PlayerController>();
                if (pc != null)
                    SessionManager.Instance.SetCoins(pc.playerId, pc.coins);
            }
        }

        public GameObject[] GetPlayers() => players;

        // ── Online: client-to-player mapping ────────────────────────────────────────

        /// <summary>Host-only: register which arena player ID belongs to a connected client.</summary>
        public void AssignNetworkClient(ulong clientId, int playerId)
        {
            _clientToPlayerId[clientId] = playerId;
        }

        // ── Online: RPCs ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Clients call this every FixedUpdate to send their input to the host.
        /// The host pushes it into that player's NetworkPlayerInput component.
        /// </summary>
        /// <summary>Client input: <paramref name="move"/> is logical XZ (x → world X, y → world Z).</summary>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SendInputServerRpc(Vector2 move, bool bombDown, bool detonate, RpcParams rpc = default)
        {
            ulong clientId = rpc.Receive.SenderClientId;
            if (!_clientToPlayerId.TryGetValue(clientId, out int playerId))
                return;

            foreach (var p in players)
            {
                if (p == null) continue;
                var pc = p.GetComponent<Player.PlayerController>();
                if (pc == null || pc.playerId != playerId) continue;
                var netInput = p.GetComponent<Online.NetworkPlayerInput>();
                netInput?.ReceiveInput(move, bombDown, detonate);
                break;
            }
        }

        [ClientRpc]
        private void GoToStandingsClientRpc()
        {
            ClearMatchTransientObjects();
            SyncCoinsToSessionManager();
            SceneFlowManager.I.GoTo(FlowState.Standings);
        }

        [ClientRpc]
        private void GoToOversClientRpc()
        {
            ClearMatchTransientObjects();
            if (SessionManager.Instance != null)
            {
                // Ensure winner info is propagated — host already set it
            }
            SceneFlowManager.I.GoToOvers();
        }

        /// <summary>
        /// Respawns a single player at their initial spawn position without disturbing
        /// the arena tiles, bombs, or other players. Used when an agent dies but opponents
        /// are still alive.
        /// </summary>
        public void ResetSinglePlayerForTraining(GameObject player)
        {
            if (
                !_initialPlayerLocalPositionsForTraining.TryGetValue(player, out var spawnPos)
            )
                return;

            spawnPos.y = ArenaGrid3D.GridOrigin.y;
            player.transform.localPosition = spawnPos;

            var pc = player.GetComponent<PlayerController>();
            if (pc != null) { pc.ResetForEpisode(); pc.enabled = true; }

            var bc = player.GetComponent<BombController>();
            if (bc != null) bc.enabled = true;
        }
    }
}
