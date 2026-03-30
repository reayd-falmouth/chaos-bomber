using HybridGame.MasterBlaster.Scripts.Bomb;
using HybridGame.MasterBlaster.Scripts.Player;
using HybridGame.MasterBlaster.Scripts.Player.Abilities;
using HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map;
using MoreMountains.Feedbacks;
using Unity.Netcode;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Arena
{
    /// <summary>
    /// 3D item block that spawns after a WallBlock3D is destroyed.
    /// Server-authoritative: only the server processes pickup and destroys the object.
    /// Item effects are applied later once the 3D player controller API is wired up.
    /// </summary>
    public class ItemPickup3D : NetworkBehaviour
    {
        public ItemPickup.ItemType itemType;

        private static readonly Color[] s_ItemColours =
        {
            new Color(1.0f, 1.0f, 1.0f), // ExtraBomb     — orange
            new Color(1.0f, 1.0f, 0.0f),  // BlastRadius   — yellow
            new Color(0.2f, 0.6f, 1.0f),  // Speed         — blue
            new Color(0.8f, 0.0f, 0.0f),  // Skull         — dark red
            new Color(0.2f, 0.9f, 0.2f),  // ExtraLife     — green
            new Color(0.9f, 0.1f, 0.9f),  // TimeBomb      — magenta
            new Color(0.0f, 0.9f, 0.9f),  // Detonator     — cyan
            new Color(1.0f, 0.6f, 0.7f),  // KickBomb      — pink
            new Color(0.7f, 0.4f, 1.0f),  // GoldBomb      — purple
            new Color(1.0f, 1.0f, 1.0f),  // Invincibility — white
            new Color(0.5f, 0.5f, 0.5f),  // Landmine      — grey
            new Color(0.1f, 0.5f, 0.1f),  // Mystery       — dark green
        };

        [Tooltip("One sprite per ItemType (same order as the enum). Assign from the objects.png sprite sheet slices.")]
        public Sprite[] itemSprites;

        [Tooltip("Optional SpriteRenderer child that displays the item icon on top of the cube. " +
                 "Add a child GameObject with a SpriteRenderer rotated to face up (X=-90).")]
        public SpriteRenderer iconRenderer;

        [Tooltip("Feedbacks to play when this item is picked up or destroyed by an explosion.")]
        [SerializeField] private MMF_Player pickupFeedbacks;

        private void Start()
        {
            int idx = (int)itemType;

            // Set optional top-face icon sprite
            if (iconRenderer != null && itemSprites != null
                && idx >= 0 && idx < itemSprites.Length && itemSprites[idx] != null)
                iconRenderer.sprite = itemSprites[idx];
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsSpawned && !IsServer) { /* non-networked: authority is always local */ }
            else if (IsSpawned && !IsServer) return; // networked client: not authoritative

            if (other.CompareTag("Player"))
            {
                ApplyEffect(other.gameObject);
                Collect();
                return;
            }

            if (other.gameObject.layer == LayerMask.NameToLayer("Explosion3D"))
                Collect();
        }

        private void ApplyEffect(GameObject player)
        {
            if (itemType == ItemPickup.ItemType.Random)
            {
                player.GetComponent<PlayerDualModeController>()?.ApplyRandom();
                return;
            }

            ApplyTo3DPlayer(player, itemType, gameObject);
        }

        /// <summary>
        /// Applies one concrete arena item (not <see cref="ItemPickup.ItemType.Random"/>) to a 3D hybrid player.
        /// </summary>
        /// <param name="pickupSource">Optional instigator for damage (e.g. death pickup).</param>
        public static void ApplyTo3DPlayer(GameObject player, ItemPickup.ItemType type, GameObject pickupSource)
        {
            if (type == ItemPickup.ItemType.Random)
            {
                player.GetComponent<PlayerDualModeController>()?.ApplyRandom();
                return;
            }

            var bombCtrl = player.GetComponent<BombController3D>();
            var dualCtrl = player.GetComponent<PlayerDualModeController>();
            var ghost = player.GetComponentInChildren<Ghost>(true);
            var protection = player.GetComponentInChildren<Protection>(true);
            var superman = player.GetComponentInChildren<Superman>(true);

            switch (type)
            {
                case ItemPickup.ItemType.ExtraBomb:
                    bombCtrl?.AddBomb();
                    break;
                case ItemPickup.ItemType.BlastRadius:
                    bombCtrl?.IncreaseBlastRadius();
                    break;
                case ItemPickup.ItemType.Superman:
                    if (superman != null)
                        superman.Activate();
                    else
                        UnityEngine.Debug.LogWarning(
                            "[ItemPickup3D] Superman item but no HybridGame.MasterBlaster.Scripts.Player.Abilities.Superman " +
                            "on player children — use 3D Superman on the player prefab, not the 2D tilemap Superman script.");
                    break;
                case ItemPickup.ItemType.Protection:
                    protection?.Activate();
                    break;
                case ItemPickup.ItemType.Ghost:
                    ghost?.Activate();
                    break;
                case ItemPickup.ItemType.SpeedIncrease:
                    dualCtrl?.IncreaseSpeed();
                    break;
                case ItemPickup.ItemType.Coin:
                    dualCtrl?.AddCoin();
                    break;
                case ItemPickup.ItemType.TimeBomb:
                    bombCtrl?.EnableTimeBomb();
                    break;
                case ItemPickup.ItemType.Stop:
                    dualCtrl?.ActivateStop();
                    break;
                case ItemPickup.ItemType.RemoteBomb:
                    bombCtrl?.EnableRemoteBomb();
                    break;
                case ItemPickup.ItemType.Death:
                    player.GetComponentInParent<Unity.FPS.Game.Health>()?.TakeDamage(9999f, pickupSource);
                    break;
            }
        }

        /// <summary>Play feedbacks then remove this item from the scene.</summary>
        public void Collect()
        {
            pickupFeedbacks?.PlayFeedbacks(transform.position);
            if (IsSpawned) NetworkObject.Despawn(true);
            else Destroy(gameObject);
        }
    }
}
