namespace HybridGame.MasterBlaster.Scripts
{
    /// <summary>
    /// Single place for the Bomberman / FPS / angled arena cycle (used by input and tests).
    /// </summary>
    public static class GameModeCycle
    {
        public static GameModeManager.GameMode GetNext(GameModeManager.GameMode current)
        {
            switch (current)
            {
                case GameModeManager.GameMode.Bomberman:
                    return GameModeManager.GameMode.FPS;
                case GameModeManager.GameMode.FPS:
                    return GameModeManager.GameMode.ArenaPerspective;
                case GameModeManager.GameMode.ArenaPerspective:
                default:
                    return GameModeManager.GameMode.Bomberman;
            }
        }
    }
}
