using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Player
{
    [CreateAssetMenu(menuName = "HybridGame/MasterBlaster/Player Sprite Sheet", fileName = "PlayerSpriteSheet")]
    public class PlayerSpriteSheet : ScriptableObject
    {
        [Tooltip("Sprites in a single ordered array: player1 block [0..29], player2 block [30..59], etc.")]
        public Sprite[] orderedSprites;

        [Tooltip("Sprites per player block. Default matches the 10x3 layout (30).")]
        public int spritesPerPlayer = 30;
    }
}

