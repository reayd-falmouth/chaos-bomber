namespace HybridGame.MasterBlaster.Scripts.Core
{
    /// <summary>
    /// Holds scene names for flow state mapping. Used by SceneFlowManager and tests.
    /// </summary>
    public class SceneNamesConfig
    {
        public string Controls { get; set; } = "Controls";
        public string Credits { get; set; } = "Credits";
        public string Prologue { get; set; } = "Prologue";
        public string Title { get; set; } = "Title";
        public string Menu { get; set; } = "Menu";
        public string Settings { get; set; } = "Settings";
        public string AvatarSelect { get; set; } = "AvatarSelect";
        public string LevelSelectOnline { get; set; } = "LevelSelectOnline";
        public string SearchingOnline { get; set; } = "SearchingOnline";
        public string LevelSelectLocal { get; set; } = "LevelSelectLocal";
        public string Countdown { get; set; } = "Countdown";
        public string Game { get; set; } = "Game";
        public string Standings { get; set; } = "Standings";
        public string Wheel { get; set; } = "Wheel";
        public string Shop { get; set; } = "Shop";
        public string Overs { get; set; } = "Overs";
    }

    /// <summary>
    /// Pure mapping between scene names and FlowState. Testable without Unity runtime.
    /// </summary>
    public static class SceneFlowMapper
    {
        public static FlowState StateForSceneName(string sceneName, SceneNamesConfig config)
        {
            if (config == null)
                return FlowState.Menu;
            if (string.Equals(sceneName, config.Controls, System.StringComparison.OrdinalIgnoreCase))
                return FlowState.Controls;
            if (sceneName == config.Credits)
                return FlowState.Credits;
            if (sceneName == config.Prologue)
                return FlowState.Prologue;
            if (sceneName == config.Title)
                return FlowState.Title;
            if (sceneName == config.Menu)
                return FlowState.Menu;
            if (sceneName == config.Settings)
                return FlowState.Settings;
            if (sceneName == config.AvatarSelect)
                return FlowState.AvatarSelect;
            if (sceneName == config.LevelSelectOnline)
                return FlowState.LevelSelectOnline;
            if (sceneName == config.SearchingOnline)
                return FlowState.SearchingOnline;
            if (sceneName == config.LevelSelectLocal)
                return FlowState.LevelSelectLocal;
            if (sceneName == config.Countdown)
                return FlowState.Countdown;
            if (sceneName == config.Game)
                return FlowState.Game;
            if (sceneName == config.Standings)
                return FlowState.Standings;
            if (sceneName == config.Wheel)
                return FlowState.Wheel;
            if (sceneName == config.Shop)
                return FlowState.Shop;
            if (sceneName == config.Overs)
                return FlowState.Overs;
            return FlowState.Menu;
        }

        public static string SceneFor(FlowState state, SceneNamesConfig config)
        {
            if (config == null)
                return "Menu";
            return state switch
            {
                FlowState.Controls => config.Controls,
                FlowState.Credits => config.Credits,
                FlowState.Prologue => config.Prologue,
                FlowState.Title => config.Title,
                FlowState.Menu => config.Menu,
                FlowState.Settings => config.Settings,
                FlowState.AvatarSelect => config.AvatarSelect,
                FlowState.LevelSelectOnline => config.LevelSelectOnline,
                FlowState.SearchingOnline => config.SearchingOnline,
                FlowState.LevelSelectLocal => config.LevelSelectLocal,
                FlowState.Countdown => config.Countdown,
                FlowState.Game => config.Game,
                FlowState.Standings => config.Standings,
                FlowState.Wheel => config.Wheel,
                FlowState.Shop => config.Shop,
                FlowState.Overs => config.Overs,
                _ => config.Menu
            };
        }
    }
}
