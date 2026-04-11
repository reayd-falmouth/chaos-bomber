using HybridGame.MasterBlaster.Scripts.Arena;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Bomb
{
    /// <summary>
    /// Keeps the bomb's blocking collider in trigger (non-blocking) until the placer leaves
    /// the bomb's grid cell — classic Bomberman walk-off-tile behaviour.
    /// </summary>
    public class BombPassThroughGrid3D : MonoBehaviour
    {
        private Transform m_Placer;
        private BoxCollider m_Box;
        private bool m_Armed;

        /// <summary>Player who placed this bomb (for pass-through until they leave the cell).</summary>
        public Transform Placer => m_Placer;

        public void Init(Transform placer)
        {
            m_Placer = placer;
            m_Box = GetComponent<BoxCollider>() ?? GetComponentInChildren<BoxCollider>(true);
            if (m_Box != null)
                m_Box.isTrigger = true;
        }

        private void LateUpdate()
        {
            if (m_Armed || m_Box == null) return;

            if (m_Placer == null)
            {
                Arm();
                return;
            }

            var bombCell = ArenaGrid3D.WorldToCell(transform.position);
            var placerCell = ArenaGrid3D.WorldToCell(m_Placer.position);
            if (placerCell != bombCell)
                Arm();
        }

        private void Arm()
        {
            m_Armed = true;
            if (m_Box != null)
                m_Box.isTrigger = false;
        }
    }
}
