using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player.AI
{
    /// <summary>
    /// AI brain: given game state, produces move and bomb decision each tick. Implemented by ScriptedAIBrain or later ML-Agents.
    /// </summary>
    public interface IAIBrain
    {
        void Tick(
            Transform self,
            Bomb.BombController bombController,
            GameObject[] allPlayers,
            out Vector2 move,
            out bool placeBomb,
            out bool detonateHeld
        );
    }
}
