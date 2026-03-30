using HybridGame.MasterBlaster.Scripts.Arena;
using Unity.Netcode;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Player
{
    /// <summary>
    /// Server-authoritative Superman block push when Netcode is active. Updates the logical grid on all peers.
    /// Offline / unspawned players use <see cref="PlayerDualModeController"/> lerp path only.
    /// </summary>
    [RequireComponent(typeof(PlayerDualModeController))]
    public class SupermanPushNetwork : NetworkBehaviour
    {
        private PlayerDualModeController _dual;

        private void Awake()
        {
            _dual = GetComponent<PlayerDualModeController>();
        }

        /// <summary>
        /// When listening and spawned: server applies push; client sends ServerRpc. Returns true if handled here
        /// (caller must not start offline Superman lerp).
        /// </summary>
        public bool TryNetworkSupermanPush(Vector2Int playerCell, Vector2Int dir)
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsListening || !IsSpawned)
                return false;

            var grid = HybridArenaGrid.Instance;
            if (grid == null || _dual == null) return false;
            if (!grid.TryEvaluateSupermanPush(playerCell, dir, out var blockCell, out var destCell, out var wall))
                return false;

            if (!wall.IsSpawned)
                return false;

            if (IsServer)
                ApplyPushAuthoritative(blockCell, destCell, wall);
            else
                RequestSupermanPushServerRpc(playerCell.x, playerCell.y, dir.x, dir.y);

            return true;
        }

        private void ApplyPushAuthoritative(Vector2Int blockCell, Vector2Int destCell, WallBlock3D wall)
        {
            var grid = HybridArenaGrid.Instance;
            if (grid == null || wall == null) return;

            Vector3 blockWorld = ArenaGrid3D.CellToWorld(destCell);
            blockWorld.y = wall.transform.position.y;
            wall.transform.position = blockWorld;

            grid.ApplySupermanPushGrid(blockCell, destCell, wall);

            Vector3 playerWorld = ArenaGrid3D.CellToWorld(blockCell);
            playerWorld.y = _dual.transform.position.y;
            _dual.TeleportForSupermanGridSnap(playerWorld);

            SyncSupermanPushGridClientRpc(blockCell.x, blockCell.y, destCell.x, destCell.y, wall.NetworkObjectId);
        }

        [ServerRpc(RequireOwnership = true)]
        private void RequestSupermanPushServerRpc(int pcx, int pcy, int dirX, int dirY)
        {
            var reported = new Vector2Int(pcx, pcy);
            var dir = new Vector2Int(dirX, dirY);
            var actual = ArenaGrid3D.WorldToCell(_dual.transform.position);
            if ((actual - reported).sqrMagnitude > 1) return;

            var grid = HybridArenaGrid.Instance;
            if (grid == null || !grid.TryEvaluateSupermanPush(reported, dir, out var blockCell, out var destCell, out var wall))
                return;

            if (!wall.IsSpawned) return;

            ApplyPushAuthoritative(blockCell, destCell, wall);
        }

        [ClientRpc]
        private void SyncSupermanPushGridClientRpc(int fromX, int fromY, int toX, int toY, ulong wallNetId)
        {
            if (IsServer) return;

            var grid = HybridArenaGrid.Instance;
            if (grid == null) return;
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(wallNetId, out var no)) return;
            var wall = no.GetComponent<WallBlock3D>();
            if (wall == null) return;

            grid.ApplySupermanPushGrid(new Vector2Int(fromX, fromY), new Vector2Int(toX, toY), wall);
        }
    }
}
