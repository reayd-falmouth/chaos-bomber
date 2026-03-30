using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player
{
    /// <summary>
    /// A no-op player input that makes a player stand completely still.
    /// Used in training mode so one player acts as a stationary target.
    /// </summary>
    public class StaticPlayerInput : MonoBehaviour, IPlayerInput
    {
        public Vector2 GetMoveDirection() => Vector2.zero;
        public bool GetBombDown() => false;
        public bool GetDetonateHeld() => true; // hold = never detonate
    }
}
