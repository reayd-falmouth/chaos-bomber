using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player
{
    /// <summary>
    /// Abstraction for player input so the same movement and bomb logic works for human (keyboard/gamepad) and AI.
    /// </summary>
    public interface IPlayerInput
    {
        /// <summary>Current movement direction (normalized or zero).</summary>
        Vector2 GetMoveDirection();

        /// <summary>True on the frame the player wants to place a bomb.</summary>
        bool GetBombDown();

        /// <summary>True while the player is "holding" the detonate key (remote/time bombs: do not detonate). When false, the bomb may detonate.</summary>
        bool GetDetonateHeld();
    }
}
