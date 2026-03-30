using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Bomb;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player.Abilities;
using MoreMountains.Feedbacks;
using Unity.Netcode;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    public class ItemPickup : NetworkBehaviour
    {
        public enum ItemType
        {
            ExtraBomb,
            BlastRadius,
            Superman,
            Protection,
            Ghost,
            SpeedIncrease,
            Death,
            Random,
            TimeBomb,
            Stop,
            Coin,
            RemoteBomb,
        }

        public ItemType type;

        [Header("Feedbacks")]
        [SerializeField] private MMF_Player pickupFeedbacks;

        private GameManager _gameManager;

        private void OnEnable()
        {
            var root = transform.root != transform ? transform.root : null;
            _gameManager = (root != null ? root.GetComponentInChildren<GameManager>() : null)
                           ?? GameManager.Instance;
            _gameManager?.RegisterItem(this);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            _gameManager?.UnregisterItem(this);
        }

        // ItemPickup.cs
        public static void ApplyItem(GameObject player, ItemType type)
        {
            var bombCtrl = player.GetComponent<BombController>();
            var playerCtrl = player.GetComponent<PlayerController>();
            var ghostCtrl = player.GetComponentInChildren<Ghost>();
            var protectionCtrl = player.GetComponentInChildren<Protection>();
            var supermanCtrl = player.GetComponentInChildren<Superman>();

            switch (type)
            {
                case ItemType.ExtraBomb:
                    bombCtrl.AddBomb();
                    break;
                case ItemType.BlastRadius:
                    bombCtrl.IncreaseBlastRadius();
                    break;
                case ItemType.Superman:
                    supermanCtrl.Activate();
                    break;
                case ItemType.Protection:
                    protectionCtrl.Activate();
                    break;
                case ItemType.Ghost:
                    ghostCtrl.Activate();
                    break;
                case ItemType.SpeedIncrease:
                    playerCtrl.IncreaseSpeed();
                    break;
                case ItemType.Coin:
                    playerCtrl.AddCoin();
                    break;
                case ItemType.TimeBomb:
                    bombCtrl.EnableTimeBomb();
                    break;
                case ItemType.Stop:
                    playerCtrl.ActivateStop();
                    break;
                case ItemType.RemoteBomb:
                    bombCtrl.EnableRemoteBomb();
                    break;
                case ItemType.Death:
                    playerCtrl.ApplyDeath();
                    break;
                case ItemType.Random:
                    playerCtrl.ApplyRandom();
                    break;
            }
        }

        private void OnItemPickup(GameObject player)
        {
            pickupFeedbacks?.PlayFeedbacks(transform.position);
            bool isOnline = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isOnline)
            {
                // Online: only host resolves pickup; broadcasts to all clients.
                if (!IsServer) return;
                var pc = player.GetComponent<PlayerController>();
                if (pc != null)
                    ApplyItemClientRpc((int)type, pc.playerId);
                Destroy(gameObject);
            }
            else
            {
                ApplyItem(player, type);
                Destroy(gameObject);
            }
        }

        [ClientRpc]
        private void ApplyItemClientRpc(int itemType, int playerId)
        {
            // Find the player with this ID and apply the item.
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var pc in players)
            {
                if (pc.playerId == playerId)
                {
                    ApplyItem(pc.gameObject, (ItemType)itemType);
                    return;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                OnItemPickup(other.gameObject);
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("Explosion"))
            {
                // Explosion destroys the item
                Destroy(gameObject);
            }
        }
    }
}
