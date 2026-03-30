using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Player.Abilities
{
    /// <summary>
    /// Superman ability — allows pushing destructible walls one cell along the grid when the far cell is empty.
    /// Actual push is driven by <see cref="PlayerDualModeController"/> (offline lerp) and
    /// <see cref="SupermanPushNetwork"/> when Netcode is listening.
    /// </summary>
    [DisallowMultipleComponent]
    public class Superman : MonoBehaviour
    {
        [Tooltip("Reserved for future variants; push uses existing WallBlock3D instances on the grid.")]
        public GameObject pushableBlockPrefab;

        private bool _active;

        public void Activate()
        {
            gameObject.SetActive(true);
            _active = true;
        }

        public bool IsActive => _active;
    }
}
