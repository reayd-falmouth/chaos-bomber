using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map;
using HybridGame.MasterBlaster.Scripts.Scenes.Shop;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player.Abilities
{
    [DisallowMultipleComponent]
    public class Superman : MonoBehaviour
    {
        private PlayerController pc;
        private BombController bombController;
        private bool active;

        private void Awake()
        {
            pc = GetComponentInParent<PlayerController>();
            bombController = GetComponentInParent<BombController>();
        }

        private void Start()
        {
            SyncFromSession();
        }

        private void FixedUpdate()
        {
            if (!active)
                return;

            // Only push if the player is moving
            if (pc.Direction != Vector2.zero)
            {
                TryConvertDestructibleAhead(pc.Direction);
            }
        }

        public void Activate()
        {
            active = true;
        }

        /// <summary>
        /// Arena pickups call <see cref="Activate"/> without writing SessionManager. Call this whenever
        /// shop/session state is reapplied (e.g. new round) so arena-only Superman does not persist.
        /// </summary>
        public void SyncFromSession()
        {
            if (pc == null)
                return;
            if (SessionManager.Instance == null || pc.playerId <= 0)
            {
                active = false;
                return;
            }
            int playerId = pc.playerId;
            active = SessionManager.Instance.GetUpgradeLevel(playerId, ShopItemType.Superman) == 1;
            UnityEngine.Debug.Log($"[PlayerController] Player {playerId} superman applied.");
        }

        private void TryConvertDestructibleAhead(Vector2 direction)
        {
            if (bombController == null || bombController.destructibleTiles == null)
                return;

            // look one cell ahead — direction is logical XY (x→worldX, y→worldY)
            Vector3 ahead = transform.position + ArenaPlane.FromLogicalXY(direction);
            Vector3Int cell = bombController.destructibleTiles.WorldToCell(ahead);
            var tile = bombController.destructibleTiles.GetTile(cell);

            if (tile != null)
            {
                // remove the tile
                bombController.destructibleTiles.SetTile(cell, null);

                // spawn destructible prefab in its place
                Vector3 worldPos = bombController.destructibleTiles.GetCellCenterWorld(cell);
                var newBlock = Instantiate(
                    bombController.destructiblePrefab,
                    worldPos,
                    Quaternion.identity
                );

                // mark this as a pushable block, not debris
                var destructible = newBlock.GetComponent<Destructible>();
                if (destructible != null)
                {
                    destructible.isDebris = false;
                }
            }
        }

        public bool IsActive => active;
    }
}
