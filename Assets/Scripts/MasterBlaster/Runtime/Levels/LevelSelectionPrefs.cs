namespace HybridGame.MasterBlaster.Scripts.Levels
{
    /// <summary>PlayerPrefs keys for local level selection.</summary>
    public static class LevelSelectionPrefs
    {
        public const string SelectedLevelIdKey = "SelectedLevelId";

        /// <summary>0-based index into serialized arena level root pairs (hybrid FPS multi-arena).</summary>
        public const string SelectedArenaIndexKey = "SelectedArenaIndex";

        /// <summary>
        /// Maps main-menu <c>NormalLevel</c> (YES = normal layout) to <see cref="SelectedArenaIndexKey"/> for
        /// <see cref="HybridGame.MasterBlaster.Scripts.Arena.HybridArenaLevelRootSwitcher"/>.
        /// </summary>
        public static int ArenaIndexFromNormalLevel(bool useNormalLevel) => useNormalLevel ? 0 : 1;
    }
}
