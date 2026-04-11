using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using HybridGame.MasterBlaster.Scripts;
using HybridGame.MasterBlaster.Scripts.Arena;
using HybridGame.MasterBlaster.Scripts.Bomb;
using HybridGame.MasterBlaster.Scripts.Camera;
using HybridGame.MasterBlaster.Scripts.Player.Abilities;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Debug;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player;
using MoreMountains.Feedbacks;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HybridGame.MasterBlaster.Scripts.Player
{
    /// <summary>
    /// Added alongside the existing PlayerCharacterController on the player prefab.
    /// In FPS mode              : defers to PlayerCharacterController (enabled).
    /// In Bomberman / ArenaPerspective : disables FPS components; grid movement + 3D bombs.
    ///
    /// Also manages directional sprite selection for both modes.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerDualModeController : MonoBehaviour, IFpsArenaMovementLock
    {
        [Header("Bomberman Movement")]
        public float bombermanSpeed = 5f;
        public float snapThreshold = 0.05f;
        public int coins = 0;
        public bool stop = false;

        /// <inheritdoc />
        public bool IsArenaStopActive => stop;

        [Header("Session")]
        [Tooltip("Synced from PlayerController (Awake/Start). Used for coins and shared-input ownership.")]
        public int playerId;

        [Tooltip("Only this arena player id enables the shared InputActionAsset (Move / SwitchMode / bomb) in Bomberman. " +
                 "Set playerId on each instance when not using PlayerController (e.g. 1 vs 2).")]
        [SerializeField]
        private int bombermanKeyboardOwnerPlayerId = 1;

        [Tooltip("Enable on exactly one player for local multiplayer (others off). Defaults true so single-player scenes work; old scene instances without this field saved also get keyboard.")]
        [SerializeField]
        private bool receiveSharedKeyboardInput = true;

        [Header("Bomberman Input (assign PlayerControls InputActionAsset)")]
        public InputActionAsset inputActions;

        [Header("Feedbacks")]
        [SerializeField] private MMF_Player randomItemFeedbacks;

        [Header("Sprite Direction")]
        public AnimatedSpriteRenderer spriteUp;
        public AnimatedSpriteRenderer spriteDown;
        public AnimatedSpriteRenderer spriteLeft;
        public AnimatedSpriteRenderer spriteRight;
        public AnimatedSpriteRenderer spriteDeath;
        [Tooltip("Shown while steering a 3D remote bomb (assign a child like MasterBlaster Player RemoteBomb).")]
        public AnimatedSpriteRenderer spriteRemoteBomb;

        [Header("Walk sprite timing (grid)")]
        [Tooltip("Movement speed at which walkAnimBaseFrameInterval matches the prefab-tuned per-frame delay (typically same as default bombermanSpeed).")]
        [SerializeField] private float walkAnimReferenceSpeed = 5f;
        [Tooltip("Seconds between walk sprite frames when movement speed equals walkAnimReferenceSpeed (e.g. 1/6 for 6 ticks per second).")]
        [SerializeField] private float walkAnimBaseFrameInterval = 1f / 6f;

        [Header("Billbox (hybrid sprite root)")]
        [Tooltip("Local Y of the Billbox transform in Bomberman / ArenaPerspective (top-down or grid camera).")]
        [SerializeField] private float billboxLocalYGrid = 2f;
        [Tooltip("Local Y of the Billbox transform in FPS mode (eye-level billboard).")]
        [SerializeField] private float billboxLocalYFps = 1f;

        [Header("Debug")]
        [Tooltip("When enabled, one log per mode change on the next frame (after cameras update): player Billbox, BillboardSprites on the player, and each bomb you placed (BillBox reorientation vs mode).")]
        [SerializeField] private bool debugLogBillboxOnModeChange;

        // ── private state ──────────────────────────────────────────────────────────
        private Transform m_BillboxRoot;
        private CharacterController m_CC;
        private PlayerCharacterController m_FPSController;
        private PlayerWeaponsManager m_WeaponsManager;
        private PlayerInputHandler m_FPSInputHandler;
        private BombController3D m_BombController;
        private Health m_Health;
        private GameModeManager.GameMode m_CurrentMode = GameModeManager.GameMode.Bomberman;
        private bool m_IsDead;
        private bool m_GhostVisualActive;
        private bool m_RemoteBombVisualActive;
        /// <summary>True while a <see cref="HybridGame.MasterBlaster.Scripts.Bomb.RemoteBombController3D"/> is active on the player.</summary>
        private bool m_RemoteBombSteerSession;

        // Bomberman grid movement
        private Vector3 m_BombermanTarget;
        private bool m_BombermanMoving;
        private Vector2Int m_LastDir = Vector2Int.down;
        private float m_BombermanMoveElapsed;

        private Superman m_Superman;
        // We use reflection here to avoid hard compile-time dependency issues between asmdef boundaries.
        // This prevents editor/build failures if SupermanPushNetwork is excluded from the current compile assembly.
        private MonoBehaviour m_SupermanNet;
        private MethodInfo m_TryNetworkSupermanPush;
        private bool m_SupermanPushing;
        private WallBlock3D m_SupermanPushWall;
        private Vector3 m_SupermanBlockFrom;
        private Vector3 m_SupermanBlockTo;
        private Vector2Int m_SupermanPushFromCell;
        private Vector2Int m_SupermanPushToCell;
        private float m_SupermanPushPlayerTravelDist = 1f;

        /// <summary>The cardinal direction the player last moved in Bomberman mode (zero when idle).</summary>
        public Vector2Int LastBombermanDir => m_BombermanMoving ? m_LastDir : Vector2Int.zero;

        // Input System actions
        private InputAction m_MoveAction;
        private InputAction m_SwitchModeAction;
        private bool m_ActionsEnabled;

        private float m_InitialBombermanSpeed;

        // #region agent log
        private bool _agentLoggedFirstBmInputProbe;
        // #endregion

        // ── Unity lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            m_CC = GetComponent<CharacterController>();
            m_FPSController = GetComponent<PlayerCharacterController>();
            m_WeaponsManager = GetComponent<PlayerWeaponsManager>();
            m_FPSInputHandler = GetComponent<PlayerInputHandler>();
            m_BombController = GetComponent<BombController3D>();
            m_Health = GetComponent<Health>();
            m_Superman = GetComponentInChildren<Superman>(true);
            CacheSupermanPushNetwork();
            CacheBillboxRoot();

            // Hybrid: 2D tile BombController shares IPlayerInput.GetBombDown() and may have no tilemaps — disable so 3D bombs work.
            var bomb2d = GetComponent<BombController>();
            if (bomb2d != null && m_BombController != null)
                bomb2d.enabled = false;

            if (m_Health != null)
                m_Health.OnDie += OnPlayerHealthDie;

            SyncPlayerIdFromPlayerController();
            BindInputActions();

            m_InitialBombermanSpeed = bombermanSpeed;
        }

        private void CacheBillboxRoot()
        {
            m_BillboxRoot = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name == "Billbox")
                {
                    m_BillboxRoot = t;
                    return;
                }
            }
        }

        private void ApplyBillboxLocalYForPresentationMode(bool gridPresentationMode)
        {
            if (m_BillboxRoot == null) return;
            var lp = m_BillboxRoot.localPosition;
            lp.y = gridPresentationMode ? billboxLocalYGrid : billboxLocalYFps;
            m_BillboxRoot.localPosition = lp;
        }

        private void CacheSupermanPushNetwork()
        {
            m_SupermanNet = null;
            m_TryNetworkSupermanPush = null;

            // Search every MonoBehaviour under this player to find the runtime instance by type name.
            // (Avoids CS0246/CS0234 issues when compilation excludes the script via asmdefs.)
            var components = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < components.Length; i++)
            {
                var c = components[i];
                if (c == null) continue;

                if (c.GetType().Name != "SupermanPushNetwork") continue;

                m_SupermanNet = c;
                m_TryNetworkSupermanPush = c.GetType().GetMethod("TryNetworkSupermanPush");
                return;
            }
        }

        private void OnDestroy()
        {
            if (m_Health != null)
                m_Health.OnDie -= OnPlayerHealthDie;
        }

        private void OnPlayerHealthDie()
        {
            m_IsDead = true;
            stop = true;
            if (m_BombController != null)
                m_BombController.enabled = false;

            if (GameModeManager.IsGridPresentationMode(m_CurrentMode))
                ShowBombermanDeathVisual();

            // Trigger win-state check — mirrors PlayerController.OnDeathSequenceEnded()
            GameManager.Instance?.CheckWinState();
        }

        public void ResetForNewGame()
        {
            m_IsDead = false;
            stop = false;

            var spriteApplier = GetComponent<PlayerSpriteSetApplier>();
            if (spriteApplier != null)
                spriteApplier.ApplyForPlayerId(playerId);

            if (m_Health != null)
                m_Health.ResetToFullHealth();

            // Clear Bomberman death visual if it was shown
            if (spriteDeath != null)
            {
                spriteDeath.StopAnimation();
                spriteDeath.gameObject.SetActive(false);
            }

            // Ensure the normal directional sprites are visible again in grid presentation modes
            if (GameModeManager.IsGridPresentationMode(m_CurrentMode))
                SetSpriteDirection(m_LastDir, moving: false);

            bombermanSpeed = m_InitialBombermanSpeed;
            RefreshDirectionalWalkAnimationIntervals();
            m_FPSController?.ResetArenaSpeedPickups();
        }

        private void ShowBombermanDeathVisual()
        {
            GetComponentInChildren<Ghost>(true)?.DeactivateNow();

            m_RemoteBombSteerSession = false;
            m_RemoteBombVisualActive = false;
            if (spriteRemoteBomb) spriteRemoteBomb.gameObject.SetActive(false);
            if (spriteUp)    spriteUp.gameObject.SetActive(false);
            if (spriteDown)  spriteDown.gameObject.SetActive(false);
            if (spriteLeft)  spriteLeft.gameObject.SetActive(false);
            if (spriteRight) spriteRight.gameObject.SetActive(false);

            if (spriteDeath == null) return;

            spriteDeath.gameObject.SetActive(true);
            spriteDeath.idle = false;
            spriteDeath.StartAnimation();
        }

        private void Start()
        {
            SyncPlayerIdFromPlayerController();
            if (GetComponent<PlayerController>() == null && playerId <= 0)
            {
                UnityEngine.Debug.LogWarning(
                    $"[PlayerDualModeController] {gameObject.name} has no playerId and no PlayerController; defaulting to 1"
                );
            }

            if (playerId <= 0)
                playerId = 1;

            // #region agent log
            AgentDebugNdjson.Log("E", "PlayerDualModeController.Start", "after_playerId_fix",
                "{\"playerId\":" + playerId + ",\"receiveSharedKeyboardInput\":" +
                (receiveSharedKeyboardInput ? "true" : "false") + ",\"bombermanKeyboardOwnerPlayerId\":" +
                bombermanKeyboardOwnerPlayerId + "}");
            // #endregion

            m_BombController?.RefreshBombInputFromDualMode();

            var spriteApplier = GetComponent<PlayerSpriteSetApplier>();
            if (spriteApplier != null)
            {
                // #region agent log
                try
                {
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "debug-9c4db8.log"),
                        "{\"sessionId\":\"9c4db8\",\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() +
                        ",\"runId\":\"run1\",\"hypothesisId\":\"H3\",\"location\":\"PlayerDualModeController.cs:Start\",\"message\":\"sprite_applier_present\",\"data\":{\"go\":\"" +
                        gameObject.name.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"playerId\":" + playerId + "}}\n"
                    );
                }
                catch { }
                // #endregion
                UnityEngine.Debug.Log($"[SPRITEAPPLY] applier_present go={gameObject.name} playerId={playerId}", this);
                spriteApplier.ApplyForPlayerId(playerId);
            }
            else
            {
                // #region agent log
                try
                {
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "debug-9c4db8.log"),
                        "{\"sessionId\":\"9c4db8\",\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() +
                        ",\"runId\":\"run1\",\"hypothesisId\":\"H3\",\"location\":\"PlayerDualModeController.cs:Start\",\"message\":\"sprite_applier_missing\",\"data\":{\"go\":\"" +
                        gameObject.name.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"playerId\":" + playerId + "}}\n"
                    );
                }
                catch { }
                // #endregion
                UnityEngine.Debug.LogWarning($"[SPRITEAPPLY] applier_missing go={gameObject.name} playerId={playerId}", this);
            }

            // Stay aligned with GameModeManager even if ApplyMode ran before this object existed
            // (runtime spawn) or could not find this controller (inactive at first ApplyMode).
            if (GameModeManager.Instance != null)
                OnModeChanged(GameModeManager.Instance.CurrentMode);
            else
            {
                // Pure-FPS scene: lock cursor and let PlayerInputHandler.CanProcessInput() work.
                OnModeChanged(GameModeManager.GameMode.FPS);
            }

            RefreshDirectionalWalkAnimationIntervals();
        }

        private void OnEnable()
        {
            // Same as Start — covers re-enabled pooled players and ordering vs GameModeManager.Start.
            if (GameModeManager.Instance != null)
                OnModeChanged(GameModeManager.Instance.CurrentMode);

            if (!m_ActionsEnabled) EnableActions();
        }

        private void OnDisable()
        {
            DisableActions();
        }

        private void Update()
        {
            if (m_IsDead) return;

            if (MayProcessSwitchModeInput())
            {
                bool pressed = false;
                var hip = GetComponent<HumanPlayerInput>();
                if (hip != null)
                    pressed = hip.WasSwitchModePressedThisFrame();
                else if (m_SwitchModeAction != null && m_SwitchModeAction.enabled)
                    pressed = m_SwitchModeAction.WasPressedThisFrame();

                if (pressed)
                    GameModeManager.Instance?.SwitchMode(GameModeCycle.GetNext(m_CurrentMode));
            }

            if (!GameModeManager.IsGridPresentationMode(m_CurrentMode)) return;

            // Keep the cursor unlocked every frame in grid presentation modes.
            // PlayerInputHandler.Start() locks the cursor unconditionally; if it runs
            // after GameModeManager.Start() the cursor ends up locked, CanProcessInput()
            // returns true, and mouse input rotates the player's Y axis even though
            // PlayerCharacterController is disabled. Enforcing the unlock here wins.
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }

            // #region agent log
            if (!_agentLoggedFirstBmInputProbe && GameModeManager.IsGridPresentationMode(m_CurrentMode) &&
                Time.frameCount >= 5)
            {
                _agentLoggedFirstBmInputProbe = true;
                Vector2 mv = m_MoveAction != null ? m_MoveAction.ReadValue<Vector2>() : Vector2.zero;
                string d = "{\"timeScale\":" + Time.timeScale.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                           ",\"moveEnabled\":" + (m_MoveAction != null && m_MoveAction.enabled ? "true" : "false") +
                           ",\"moveX\":" + mv.x.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                           ",\"moveY\":" + mv.y.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                           ",\"owns\":" + (OwnsBombermanSharedInput() ? "true" : "false") +
                           ",\"m_ActionsEnabled\":" + (m_ActionsEnabled ? "true" : "false") + "}";
                AgentDebugNdjson.Log("D", "PlayerDualModeController.Update", "first_bm_probe_after_frame5", d);
            }
            // #endregion

            UpdateBombermanMovement();
        }

        // ── Mode switching ─────────────────────────────────────────────────────────

        public void OnModeChanged(GameModeManager.GameMode newMode)
        {
            UnityEngine.Debug.Log($"[PlayerDualModeController] OnModeChanged → {newMode}");
            m_CurrentMode = newMode;

            // --- ADD THIS LINE ---
            // Use the Singleton Instance instead of FindObjectOfType for better performance
            if (CinemachineModeSwitcher.Instance != null)
            {
                CinemachineModeSwitcher.Instance.UpdateAllCameras(newMode);
            }
            // ---------------------
            
            bool useGrid = GameModeManager.IsGridPresentationMode(newMode);
            bool isFps = newMode == GameModeManager.GameMode.FPS;

            ApplyBillboxLocalYForPresentationMode(useGrid);

            // FPS controller + weapons: enabled in FPS mode only
            if (m_FPSController != null)
                m_FPSController.enabled = isFps;
            if (m_WeaponsManager != null)
                m_WeaponsManager.enabled = isFps;
            if (m_FPSInputHandler != null)
                m_FPSInputHandler.enabled = isFps;
            if (m_BombController != null)
                m_BombController.enabled = useGrid;

            if (useGrid)
            {
                // In Bomberman mode the FPS controller is disabled (and it normally recenters the capsule).
                // Ensure the CharacterController capsule is centered on its own height so the transform origin
                // represents "feet on floor" and doesn't appear to pop up when entering play mode.
                if (m_CC != null)
                {
                    // Force a deterministic capsule shape for Bomberman (grid plane gameplay).
                    // This avoids legacy/scene overrides leaving the capsule with centerY==height (which lifts the transform).
                    m_CC.height = 1.8f;
                    m_CC.center = Vector3.up * (m_CC.height * 0.5f);
                }

                // Snap to grid on entry
                SnapToGrid();
                m_BombermanMoving = false;
                EnableActions();
                // Show idle sprite for current facing direction
                SetSpriteDirection(m_LastDir, moving: false);
                RefreshDirectionalWalkAnimationIntervals();
            }
            else
            {
                EndRemoteBombSteerSession();
                DisableActions();
                EnableActions(); // FPS: only SwitchMode for owner; never shared Move (FPS uses PlayerInputHandler)
            }

            if (debugLogBillboxOnModeChange)
                StartCoroutine(CoLogBillboxAfterModeChangeCamerasReady(newMode));
        }

        /// <summary>
        /// <see cref="GameModeManager.ApplyMode"/> runs player <see cref="OnModeChanged"/> before <see cref="HybridCameraManager.SetMode"/>,
        /// so we wait one frame so <see cref="Camera.main"/> matches the new view when logging.
        /// </summary>
        private IEnumerator CoLogBillboxAfterModeChangeCamerasReady(GameModeManager.GameMode newMode)
        {
            yield return null;
            if (!debugLogBillboxOnModeChange)
                yield break;

            var main = UnityEngine.Camera.main;
            string camInfo = main != null ? $"{main.name} ortho={main.orthographic}" : "Camera.main=null";

            string billboxLine = m_BillboxRoot != null
                ? $"localPos={m_BillboxRoot.localPosition} localEuler={m_BillboxRoot.localRotation.eulerAngles}"
                : "Billbox not found";

            var boards = GetComponentsInChildren<BillboardSprite>(true);
            var boardsSb = new StringBuilder();
            for (int i = 0; i < boards.Length; i++)
            {
                if (i > 0) boardsSb.Append("; ");
                boardsSb.Append(boards[i].gameObject.name).Append(" euler=").Append(boards[i].transform.rotation.eulerAngles);
            }

            var bombsSb = new StringBuilder();
            int bombCount = 0;
            var placedBombs = FindObjectsByType<BombPassThroughGrid3D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < placedBombs.Length; i++)
            {
                var pt = placedBombs[i];
                if (pt == null || pt.Placer != transform) continue;
                bombCount++;
                if (bombsSb.Length > 0) bombsSb.Append("\n");
                bombsSb.Append(BombController3D.FormatBombBillBoxDebugBlock(pt.gameObject, "    "));
            }

            string bombsSection = bombCount == 0
                ? "\n  --- Placed bombs (this player): (none) — compare after placing a bomb and switching modes ---"
                : "\n  --- Placed bombs (this player), BillBox after mode change ---\n" + bombsSb;

            UnityEngine.Debug.Log(
                "[PlayerDualModeController] Billbox mode debug (next frame after mode change) player=" + gameObject.name +
                " mode=" + newMode + " mainCam=" + camInfo +
                "\n  " + billboxLine +
                "\n  BillboardSprite (" + boards.Length + "): " + (boards.Length == 0 ? "(none)" : boardsSb.ToString()) +
                bombsSection,
                this);
        }

        // ── Bomberman grid movement ────────────────────────────────────────────────

        private void UpdateBombermanMovement()
        {
            if (m_IsDead || stop) return;
            if (!ShouldProcessBombermanMovement()) return;

            if (!m_BombermanMoving)
            {
                // Ensure we sit exactly on a grid cell
                SnapToGrid();

                Vector2 rawDir = Vector2.zero;
                if (OwnsBombermanSharedInput() && m_MoveAction != null)
                    rawDir = m_MoveAction.ReadValue<Vector2>();
                if (rawDir.sqrMagnitude < 0.25f)
                {
                    var ip = GetComponent<IPlayerInput>();
                    if (ip != null)
                        rawDir = ip.GetMoveDirection();
                }

                if (rawDir.sqrMagnitude < 0.25f) return;

                Vector2Int dir4 = GetCardinalDirection(rawDir);
                Vector2Int currentCell = ArenaGrid3D.WorldToCell(transform.position);
                Vector2Int nextCell = currentCell + dir4;
                Vector3 nextWorld = ArenaGrid3D.CellToWorld(nextCell);

                var grid = HybridArenaGrid.Instance;
                bool isWalkable = grid == null || grid.IsWalkable(nextCell);
                bool hasDestructible = grid != null && grid.GetDestructible(nextCell) != null;
                // Ghost should let the player walk "through walls" (destructible blocks).
                // The grid still marks those cells as non-walkable, so we must override the intent gate.
                bool canMove = isWalkable || (m_GhostVisualActive && hasDestructible);

                UnityEngine.Debug.Log($"[BombermanMove] rawDir={rawDir}  dir4={dir4}  cell={currentCell}→{nextCell}  canMove={canMove}  actionEnabled={m_MoveAction?.enabled}");

                // #region agent log
                LogGhostCanMoveDecisionPreFix(nextCell, isWalkable, hasDestructible, canMove);
                // #endregion

                if (canMove)
                {
                    m_BombermanTarget = nextWorld;
                    m_BombermanMoving = true;
                    m_BombermanMoveElapsed = 0f;
                    m_LastDir = dir4;
                    SetSpriteDirection(dir4, moving: true);
                }
                else if (TryBeginSupermanGridPush(dir4, currentCell, nextWorld))
                {
                    // Offline: m_BombermanMoving set inside. Network: instant server path, no local step.
                }
            }

            if (m_BombermanMoving)
            {
                m_BombermanMoveElapsed += Time.deltaTime;

                // Safety: if the CharacterController is blocked by a wall and can't reach
                // the target, snap back to the nearest cell rather than pushing forever.
                float maxMoveTime = ArenaGrid3D.CellSize / Mathf.Max(bombermanSpeed, 0.01f) * 2.5f;
                if (m_BombermanMoveElapsed > maxMoveTime)
                {
                    FinalizeSupermanPushIfNeeded(arrived: false);
                    SnapToGrid();
                    m_BombermanMoving = false;
                    SetSpriteDirection(m_LastDir, moving: false);
                    return;
                }

                Vector3 toTarget = m_BombermanTarget - transform.position;
                toTarget.y = 0f;

                float step = bombermanSpeed * Time.deltaTime;

                if (toTarget.magnitude <= step || toTarget.magnitude <= snapThreshold)
                {
                    // Arrived — snap exactly
                    var delta = m_BombermanTarget - transform.position;
                    delta.y = 0f;
                    if (CanMoveWithCharacterController())
                        m_CC.Move(delta);
                    else
                        transform.position = m_BombermanTarget;
                    FinalizeSupermanPushIfNeeded(arrived: true);
                    m_BombermanMoving = false;
                    SetSpriteDirection(m_LastDir, moving: false);
                }
                else
                {
                    if (m_SupermanPushing && m_SupermanPushWall != null)
                    {
                        float t = 1f - Mathf.Clamp01(toTarget.magnitude / m_SupermanPushPlayerTravelDist);
                        m_SupermanPushWall.transform.position =
                            Vector3.Lerp(m_SupermanBlockFrom, m_SupermanBlockTo, t);
                    }

                    var posBefore = transform.position;
                    if (CanMoveWithCharacterController())
                        m_CC.Move(toTarget.normalized * step);
                    else
                        transform.position += toTarget.normalized * step;
                    // If CharacterController was blocked by a wall, cancel immediately
                    float actualSqr = (transform.position - posBefore).sqrMagnitude;
                    if (actualSqr < (step * 0.1f) * (step * 0.1f))
                    {
                        FinalizeSupermanPushIfNeeded(arrived: false);
                        SnapToGrid();
                        m_BombermanMoving = false;
                        SetSpriteDirection(m_LastDir, moving: false);
                    }
                }
            }

            // Hard-clamp Y to arena floor — CharacterController may drift; floor may not be world Y=0
            float floorY = ArenaGrid3D.GridOrigin.y;
            if (Mathf.Abs(transform.position.y - floorY) > 0.01f)
            {
                var p = transform.position;
                p.y = floorY;
                transform.position = p;
            }
        }

        // Logs to debug-731a3a.log in NDJSON format for this debug workflow.
        private void LogGhostCanMoveDecisionPreFix(Vector2Int nextCell, bool isWalkable, bool hasDestructible, bool canMove)
        {
            try
            {
                string logPath = Path.Combine(Application.dataPath, "..", "debug-731a3a.log");
                long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                int ccExcludeLayers = m_CC != null ? m_CC.excludeLayers : 0;

                string data =
                    "{"
                    + "\"ghostVisualActive\":" + (m_GhostVisualActive ? "true" : "false")
                    + ",\"nextCell\":{" + "\"x\":" + nextCell.x + ",\"y\":" + nextCell.y + "}"
                    + ",\"isWalkable\":" + (isWalkable ? "true" : "false")
                    + ",\"hasDestructible\":" + (hasDestructible ? "true" : "false")
                    + ",\"canMove\":" + (canMove ? "true" : "false")
                    + ",\"ccExcludeLayers\":" + ccExcludeLayers
                    + "}";

                string line =
                    "{"
                    + "\"sessionId\":\"731a3a\""
                    + ",\"runId\":\"pre-fix\""
                    + ",\"hypothesisId\":\"ghost-canMoveOverride\""
                    + ",\"location\":\"PlayerDualModeController.UpdateBombermanMovement\""
                    + ",\"message\":\"ghost-canmove-decision\""
                    + ",\"data\":" + data
                    + ",\"timestamp\":" + ts
                    + "}\n";

                File.AppendAllText(logPath, line);
            }
            catch
            {
                // ignore logging failures
            }
        }

        private bool TryBeginSupermanGridPush(Vector2Int dir4, Vector2Int currentCell, Vector3 nextWorld)
        {
            if (m_Superman == null || !m_Superman.IsActive) return false;
            var grid = HybridArenaGrid.Instance;
            if (grid == null || !grid.TryEvaluateSupermanPush(currentCell, dir4, out _, out var destCell, out var wall))
                return false;

            if (m_TryNetworkSupermanPush != null && m_SupermanNet != null)
            {
                try
                {
                    bool handled = (bool)m_TryNetworkSupermanPush.Invoke(m_SupermanNet, new object[] { currentCell, dir4 });
                    if (handled)
                        return true;
                }
                catch { /* Reflection invoke failed; fall back to offline push path. */ }
            }

            wall.SetPhysicsColliderEnabled(false);
            m_SupermanPushing = true;
            m_SupermanPushWall = wall;
            m_SupermanPushFromCell = currentCell + dir4;
            m_SupermanPushToCell = destCell;
            m_SupermanBlockFrom = wall.transform.position;
            m_SupermanBlockTo = ArenaGrid3D.CellToWorld(destCell);
            m_SupermanBlockTo.y = wall.transform.position.y;
            m_BombermanTarget = nextWorld;
            m_BombermanMoving = true;
            m_BombermanMoveElapsed = 0f;
            m_LastDir = dir4;
            m_SupermanPushPlayerTravelDist = Mathf.Max((m_BombermanTarget - transform.position).magnitude, 0.01f);
            SetSpriteDirection(dir4, moving: true);
            return true;
        }

        private void FinalizeSupermanPushIfNeeded(bool arrived)
        {
            if (!m_SupermanPushing || m_SupermanPushWall == null)
            {
                m_SupermanPushing = false;
                m_SupermanPushWall = null;
                return;
            }

            var wall = m_SupermanPushWall;
            var grid = HybridArenaGrid.Instance;
            if (arrived && grid != null)
            {
                wall.transform.position = m_SupermanBlockTo;
                grid.ApplySupermanPushGrid(m_SupermanPushFromCell, m_SupermanPushToCell, wall);
            }
            else
                wall.transform.position = m_SupermanBlockFrom;

            wall.SetPhysicsColliderEnabled(true);
            m_SupermanPushing = false;
            m_SupermanPushWall = null;
        }

        /// <summary>Used by <see cref="HybridGame.MasterBlaster.Scripts.Player.SupermanPushNetwork"/> after server-authoritative push.</summary>
        public void TeleportForSupermanGridSnap(Vector3 worldOnGridPlane)
        {
            float floorY = ArenaGrid3D.GridOrigin.y;
            var p = worldOnGridPlane;
            p.y = floorY;
            if (!CanMoveWithCharacterController())
            {
                transform.position = p;
                return;
            }

            m_CC.enabled = false;
            transform.position = p;
            m_CC.enabled = true;
        }

        private void SnapToGrid()
        {
            Vector3 snapped = ArenaGrid3D.SnapToCell(transform.position);
            if (Vector3.Distance(snapped, transform.position) <= 0.001f) return;

            // GameModeManager.ApplyMode notifies inactive players too — Move() is illegal on an inactive CharacterController.
            if (!CanMoveWithCharacterController())
            {
                transform.position = snapped;
                return;
            }

            // When switching back from FPS, the CharacterController can be intersecting or blocked,
            // causing Move() to fail and leaving the player (and billboard sprite) off-center.
            // Hard-snap by temporarily disabling the controller.
            m_CC.enabled = false;
            transform.position = snapped;
            m_CC.enabled = true;
        }

        private bool CanMoveWithCharacterController()
        {
            return m_CC != null && m_CC.enabled && m_CC.gameObject.activeInHierarchy;
        }

        private static Vector2Int GetCardinalDirection(Vector2 input)
        {
            // Choose dominant axis
            if (Mathf.Abs(input.x) >= Mathf.Abs(input.y))
                return input.x > 0 ? Vector2Int.right : Vector2Int.left;
            else
                // Vector2.y+ → grid row+ → world Z+ (Vector3.forward)
                return input.y > 0 ? Vector2Int.up : Vector2Int.down;
        }

        /// <summary>
        /// Same input path as starting a Bomberman step, but returns a direction even when the next cell is blocked
        /// (e.g. destructible wall). Used by <see cref="Abilities.Superman"/>.
        /// </summary>
        public Vector2Int GetBombermanMoveIntentCardinal()
        {
            if (!GameModeManager.IsGridPresentationMode(m_CurrentMode)) return Vector2Int.zero;
            if (m_IsDead || stop) return Vector2Int.zero;
            if (!ShouldProcessBombermanMovement()) return Vector2Int.zero;

            Vector2 rawDir = Vector2.zero;
            if (OwnsBombermanSharedInput() && m_MoveAction != null)
                rawDir = m_MoveAction.ReadValue<Vector2>();
            if (rawDir.sqrMagnitude < 0.25f)
            {
                var ip = GetComponent<IPlayerInput>();
                if (ip != null)
                    rawDir = ip.GetMoveDirection();
            }

            if (rawDir.sqrMagnitude < 0.25f) return Vector2Int.zero;
            return GetCardinalDirection(rawDir);
        }

        /// <summary>
        /// Same Move + <see cref="IPlayerInput"/> path as <see cref="UpdateBombermanMovement"/>, but ignores
        /// <see cref="stop"/> so <see cref="HybridGame.MasterBlaster.Scripts.Bomb.RemoteBombController3D"/> can read steer input while the placer is frozen.
        /// </summary>
        public Vector2 GetBombermanMoveInputForRemoteBomb()
        {
            Vector2 rawDir = Vector2.zero;
            if (OwnsBombermanSharedInput() && m_MoveAction != null)
                rawDir = m_MoveAction.ReadValue<Vector2>();
            if (rawDir.sqrMagnitude < 0.25f)
            {
                var ip = GetComponent<IPlayerInput>();
                if (ip != null)
                    rawDir = ip.GetMoveDirection();
            }

            return rawDir;
        }

        // ── Sprite direction ──────────────────────────────────────────────────────

        /// <summary>
        /// Show the correct directional sprite and set its idle/walk state.
        /// dir uses Vector2Int.up = Z+ (north), Vector2Int.right = X+ (east).
        /// </summary>
        public void SetSpriteDirection(Vector2Int dir, bool moving)
        {
            if (m_RemoteBombVisualActive) return;
            if (m_GhostVisualActive) return;

            var up    = spriteUp;
            var down  = spriteDown;
            var left  = spriteLeft;
            var right = spriteRight;

            // Disable all
            if (up)    up.gameObject.SetActive(false);
            if (down)  down.gameObject.SetActive(false);
            if (left)  left.gameObject.SetActive(false);
            if (right) right.gameObject.SetActive(false);

            AnimatedSpriteRenderer active = null;
            if (dir == Vector2Int.up)    active = up;
            else if (dir == Vector2Int.down)  active = down;
            else if (dir == Vector2Int.left)  active = left;
            else if (dir == Vector2Int.right) active = right;

            if (active == null) return;
            active.gameObject.SetActive(true);
            active.idle = !moving;
        }

        /// <summary>
        /// When true, hides directional Bomberman sprites so the Ghost overlay can show without being overridden each frame.
        /// </summary>
        public void SetGhostVisualActive(bool active)
        {
            if (m_GhostVisualActive == active)
                return;

            m_GhostVisualActive = active;
            if (active)
            {
                if (spriteUp) spriteUp.gameObject.SetActive(false);
                if (spriteDown) spriteDown.gameObject.SetActive(false);
                if (spriteLeft) spriteLeft.gameObject.SetActive(false);
                if (spriteRight) spriteRight.gameObject.SetActive(false);
            }
            else if (GameModeManager.IsGridPresentationMode(m_CurrentMode) && !m_IsDead)
                SetSpriteDirection(m_LastDir, m_BombermanMoving);
        }

        /// <summary>
        /// Matches MasterBlaster <see cref="HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player.PlayerController.SetRemoteBombVisual"/> for hybrid players that use this controller only.
        /// </summary>
        public void SetRemoteBombVisual(bool active)
        {
            if (!active)
            {
                m_RemoteBombSteerSession = false;
                m_RemoteBombVisualActive = false;
                if (spriteRemoteBomb != null)
                    spriteRemoteBomb.gameObject.SetActive(false);
                if (GameModeManager.IsGridPresentationMode(m_CurrentMode) && !m_IsDead)
                    SetSpriteDirection(m_LastDir, m_BombermanMoving);
                return;
            }

            if (spriteRemoteBomb == null)
            {
                m_RemoteBombVisualActive = false;
                return;
            }

            m_RemoteBombVisualActive = true;
            if (spriteUp)    spriteUp.gameObject.SetActive(false);
            if (spriteDown)  spriteDown.gameObject.SetActive(false);
            if (spriteLeft)  spriteLeft.gameObject.SetActive(false);
            if (spriteRight) spriteRight.gameObject.SetActive(false);
            if (spriteDeath) spriteDeath.gameObject.SetActive(false);

            spriteRemoteBomb.gameObject.SetActive(true);
            spriteRemoteBomb.idle = true;
        }

        /// <summary>
        /// Called when a remote bomb is placed: player stays frozen but uses normal facing until the bomb actually moves.
        /// </summary>
        public void BeginRemoteBombSteerSession()
        {
            m_RemoteBombSteerSession = true;
            m_RemoteBombVisualActive = false;
            if (spriteRemoteBomb != null)
                spriteRemoteBomb.gameObject.SetActive(false);
            if (GameModeManager.IsGridPresentationMode(m_CurrentMode) && !m_IsDead)
                SetSpriteDirection(m_LastDir, false);
        }

        /// <summary>
        /// While steering, show the remote-controller pose only when detonate is held and the bomb is moving.
        /// </summary>
        public void SetRemoteBombSteerPose(bool showRemoteControllerSprite)
        {
            if (!m_RemoteBombSteerSession || spriteRemoteBomb == null)
                return;

            if (showRemoteControllerSprite)
            {
                m_RemoteBombVisualActive = true;
                if (spriteUp)    spriteUp.gameObject.SetActive(false);
                if (spriteDown)  spriteDown.gameObject.SetActive(false);
                if (spriteLeft)  spriteLeft.gameObject.SetActive(false);
                if (spriteRight) spriteRight.gameObject.SetActive(false);
                if (spriteDeath) spriteDeath.gameObject.SetActive(false);

                spriteRemoteBomb.gameObject.SetActive(true);
                spriteRemoteBomb.idle = true;
            }
            else
            {
                m_RemoteBombVisualActive = false;
                spriteRemoteBomb.gameObject.SetActive(false);
                if (GameModeManager.IsGridPresentationMode(m_CurrentMode) && !m_IsDead)
                    SetSpriteDirection(m_LastDir, false);
            }
        }

        /// <summary>Ends remote steering (bomb detonated / destroyed / mode switch).</summary>
        public void EndRemoteBombSteerSession()
        {
            if (!m_RemoteBombSteerSession)
            {
                if (m_RemoteBombVisualActive)
                    SetRemoteBombVisual(false);
                return;
            }

            m_RemoteBombSteerSession = false;
            m_RemoteBombVisualActive = false;
            if (spriteRemoteBomb != null)
                spriteRemoteBomb.gameObject.SetActive(false);
            if (GameModeManager.IsGridPresentationMode(m_CurrentMode) && !m_IsDead)
                SetSpriteDirection(m_LastDir, m_BombermanMoving);
        }

        // ── Item upgrades ─────────────────────────────────────────────────────────

        public void IncreaseSpeed()
        {
            bombermanSpeed += 1f;
            RefreshDirectionalWalkAnimationIntervals();
            m_FPSController?.ApplyArenaSpeedPickupBonus(1f);
        }

        /// <summary>Walk frame interval scaled for current <see cref="bombermanSpeed"/>; used by remote bomb billboards.</summary>
        public float GetScaledWalkAnimationFrameInterval() =>
            AnimatedSpriteRenderer.ComputeScaledFrameInterval(
                walkAnimBaseFrameInterval, walkAnimReferenceSpeed, bombermanSpeed);

        private void RefreshDirectionalWalkAnimationIntervals()
        {
            float t = GetScaledWalkAnimationFrameInterval();
            spriteUp?.SetFrameInterval(t);
            spriteDown?.SetFrameInterval(t);
            spriteLeft?.SetFrameInterval(t);
            spriteRight?.SetFrameInterval(t);
        }

        /// <summary>Mystery / Random pickup: same flow as 2D <see cref="PlayerController.ApplyRandom"/>.</summary>
        public void ApplyRandom()
        {
            ItemPickup.ItemType randomType;
            do
            {
                randomType = (ItemPickup.ItemType)
                    UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(ItemPickup.ItemType)).Length);
            } while (randomType == ItemPickup.ItemType.Random);

            randomItemFeedbacks?.PlayFeedbacks(transform.position);

            ItemPickup3D.ApplyTo3DPlayer(gameObject, randomType, null);
        }

        public void AddCoin()
        {
            if (SessionManager.Instance != null)
                SessionManager.Instance.AddCoins(playerId, 1);
            coins =
                SessionManager.Instance != null
                    ? SessionManager.Instance.GetCoins(playerId)
                    : coins + 1;
            UnityEngine.Debug.Log($"[PlayerDualModeController] Player {playerId} coins increased to {coins}");
        }

        public void ActivateStop(float duration = 10f) { StartCoroutine(StopRoutine(duration)); }

        private System.Collections.IEnumerator StopRoutine(float duration)
        {
            stop = true;
            yield return new WaitForSeconds(duration);
            stop = false;
        }

        // ── Input binding ─────────────────────────────────────────────────────────

        private void BindInputActions()
        {
            // #region agent log
            if (inputActions == null)
            {
                AgentDebugNdjson.Log("B", "PlayerDualModeController.BindInputActions", "inputActions_null", "{}");
                return;
            }

            var map = inputActions.FindActionMap("Player");
            if (map == null)
            {
                AgentDebugNdjson.Log("B", "PlayerDualModeController.BindInputActions", "player_map_missing", "{}");
                return;
            }

            m_MoveAction       = map.FindAction("Move");
            m_SwitchModeAction = map.FindAction("SwitchMode");
            string bd = "{\"moveNull\":" + (m_MoveAction == null ? "true" : "false") +
                        ",\"switchNull\":" + (m_SwitchModeAction == null ? "true" : "false") + "}";
            AgentDebugNdjson.Log("B", "PlayerDualModeController.BindInputActions", "actions_resolved", bd);
            // #endregion
        }

        /// <summary>True if this instance may Enable() shared <see cref="inputActions"/> (keyboard / default bindings).</summary>
        public bool OwnsBombermanSharedInput() =>
            receiveSharedKeyboardInput && playerId == bombermanKeyboardOwnerPlayerId;

        /// <summary>SessionManager assigned a physical device (gamepad) to this player slot.</summary>
        private bool HasSessionAssignedDevice() =>
            SessionManager.Instance != null && SessionManager.Instance.GetAssignedDevice(playerId).HasValue;

        /// <summary>Human keyboard owner OR the human with a controller slot from SessionManager.</summary>
        public bool CanDriveBombermanLocally() =>
            OwnsBombermanSharedInput() || HasSessionAssignedDevice();

        private bool ShouldProcessBombermanMovement() =>
            CanDriveBombermanLocally() || GetComponent<AIPlayerInput>() != null;

        private void SyncPlayerIdFromPlayerController()
        {
            var pc = GetComponent<PlayerController>();
            if (pc != null)
                playerId = pc.playerId;
            else
            {
                // Fallback for scenes that use PlayerDualModeController without PlayerController:
                // infer playerId from GameObject name suffix (e.g. "Player3" -> 3).
                int inferred = TryInferPlayerIdFromName(gameObject.name);
                if (inferred > 0)
                    playerId = inferred;
            }
        }

        private static int TryInferPlayerIdFromName(string goName)
        {
            if (string.IsNullOrEmpty(goName)) return -1;
            int i = goName.Length - 1;
            while (i >= 0 && char.IsDigit(goName[i])) i--;
            int start = i + 1;
            if (start >= goName.Length) return -1;
            if (int.TryParse(goName.Substring(start), out int n) && n > 0) return n;
            return -1;
        }

        private bool IsAiControlled() => GetComponent<AIPlayerInput>() != null;

        /// <summary>Humans that drive this slot may cycle view mode; AI must not consume SwitchMode.</summary>
        private bool MayProcessSwitchModeInput() =>
            !IsAiControlled() && CanDriveBombermanLocally();

        private void EnableActions()
        {
            if (IsAiControlled())
            {
                m_MoveAction?.Disable();
                m_SwitchModeAction?.Disable();
                m_ActionsEnabled = false;
                m_BombController?.RefreshBombInputFromDualMode();
                AgentLogEnableActionsState("ai_no_actions");
                return;
            }

            var humanPad = GetComponent<HumanPlayerInput>();
            bool useSharedSwitch = humanPad == null || !humanPad.UsesDedicatedGamepad;

            if (GameModeManager.IsGridPresentationMode(m_CurrentMode))
            {
                if (!ShouldProcessBombermanMovement())
                {
                    m_MoveAction?.Disable();
                    m_SwitchModeAction?.Disable();
                    m_ActionsEnabled = false;
                    m_BombController?.RefreshBombInputFromDualMode();
                    AgentLogEnableActionsState("bomberman_no_input_slot");
                    return;
                }

                if (OwnsBombermanSharedInput())
                    m_MoveAction?.Enable();
                else
                    m_MoveAction?.Disable();

                if (useSharedSwitch)
                    m_SwitchModeAction?.Enable();
                else
                    m_SwitchModeAction?.Disable();

                m_ActionsEnabled = true;
                m_BombController?.RefreshBombInputFromDualMode();
                AgentLogEnableActionsState("bomberman_local_input");
                return;
            }

            // FPS: movement is PlayerInputHandler — do not enable shared Move (would duplicate input)
            m_MoveAction?.Disable();
            if (useSharedSwitch)
                m_SwitchModeAction?.Enable();
            else
                m_SwitchModeAction?.Disable();

            m_ActionsEnabled = useSharedSwitch ? m_SwitchModeAction != null : true;
            m_BombController?.RefreshBombInputFromDualMode();
            AgentLogEnableActionsState("fps_switch_only");
        }

        // #region agent log
        private void AgentLogEnableActionsState(string branch)
        {
            bool moveEn = m_MoveAction != null && m_MoveAction.enabled;
            bool swEn = m_SwitchModeAction != null && m_SwitchModeAction.enabled;
            string data = "{\"branch\":\"" + branch + "\",\"currentMode\":\"" + m_CurrentMode +
                          "\",\"playerId\":" + playerId + ",\"bombermanKeyboardOwnerPlayerId\":" +
                          bombermanKeyboardOwnerPlayerId + ",\"receiveSharedKeyboardInput\":" +
                          (receiveSharedKeyboardInput ? "true" : "false") + ",\"owns\":" +
                          (OwnsBombermanSharedInput() ? "true" : "false") + ",\"canDriveLocal\":" +
                          (CanDriveBombermanLocally() ? "true" : "false") + ",\"m_ActionsEnabled\":" +
                          (m_ActionsEnabled ? "true" : "false") + ",\"moveNull\":" +
                          (m_MoveAction == null ? "true" : "false") + ",\"switchNull\":" +
                          (m_SwitchModeAction == null ? "true" : "false") + ",\"moveEnabled\":" +
                          (moveEn ? "true" : "false") + ",\"switchEnabled\":" + (swEn ? "true" : "false") +
                          ",\"inputActionsNull\":" + (inputActions == null ? "true" : "false") + "}";
            AgentDebugNdjson.Log("A", "PlayerDualModeController.EnableActions", "state", data);
        }
        // #endregion

        private void DisableActions()
        {
            m_MoveAction?.Disable();
            m_SwitchModeAction?.Disable();
            m_ActionsEnabled = false;
        }
    }
}
