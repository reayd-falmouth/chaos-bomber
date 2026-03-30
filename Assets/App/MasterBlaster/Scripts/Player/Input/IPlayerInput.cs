using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Player.Input
{
    /// <summary>
    /// Abstraction for Bomberman-mode player input.
    /// Copied from MasterBlaster (Scenes.Arena.Player.IPlayerInput), namespace changed.
    /// </summary>
    public interface IPlayerInput
    {
        /// <summary>Current movement direction (normalized or zero).</summary>
        Vector2 GetMoveDirection();

        /// <summary>True on the frame the player wants to place a bomb.</summary>
        bool GetBombDown();

        /// <summary>True while the player is holding the detonate key.</summary>
        bool GetDetonateHeld();
    }
}
