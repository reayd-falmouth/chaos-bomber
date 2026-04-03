namespace HybridGame.MasterBlaster.Scripts.Levels
{
    /// <summary>PlayerPrefs keys for local level selection.</summary>
    public static class LevelSelectionPrefs
    {
        public const string SelectedLevelIdKey = "SelectedLevelId";

        /// <summary>0-based index into serialized arena level root pairs (hybrid FPS multi-arena).</summary>
        public const string SelectedArenaIndexKey = "SelectedArenaIndex";
    }
}
