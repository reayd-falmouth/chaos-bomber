namespace HybridGame.MasterBlaster.Scripts.Levels
{
    /// <summary>
    /// PlayerPrefs-backed flags for "player X has won / collected level Y" (gamification).
    /// </summary>
    public static class LevelWinPersistence
    {
        public static string MakePrefsKey(string levelId, int playerId) =>
            $"LevelWin:{levelId}:{playerId}";

        public static bool HasPlayerWonLevel(string levelId, int playerId)
        {
            if (string.IsNullOrEmpty(levelId) || playerId <= 0)
                return false;
            return UnityEngine.PlayerPrefs.GetInt(MakePrefsKey(levelId, playerId), 0) == 1;
        }

        public static void MarkPlayerWonLevel(string levelId, int playerId)
        {
            if (string.IsNullOrEmpty(levelId) || playerId <= 0)
                return;
            UnityEngine.PlayerPrefs.SetInt(MakePrefsKey(levelId, playerId), 1);
            UnityEngine.PlayerPrefs.Save();
        }
    }
}
