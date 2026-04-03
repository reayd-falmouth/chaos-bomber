using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Levels
{
    [CreateAssetMenu(
        menuName = "HybridGame/MasterBlaster/Levels/Level Library",
        fileName = "LevelLibrary"
    )]
    public class LevelLibrary : ScriptableObject
    {
        public LevelDefinition[] levels;

        public bool TryGetById(string levelId, out LevelDefinition definition)
        {
            definition = null;
            if (levels == null || levels.Length == 0)
                return false;
            if (string.IsNullOrEmpty(levelId))
                return false;

            for (int i = 0; i < levels.Length; i++)
            {
                var d = levels[i];
                if (d == null)
                    continue;
                if (string.Equals(d.levelId, levelId, System.StringComparison.Ordinal))
                {
                    definition = d;
                    return true;
                }
            }
            return false;
        }
    }
}

