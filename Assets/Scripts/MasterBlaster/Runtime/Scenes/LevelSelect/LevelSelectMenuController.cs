using HybridGame.MasterBlaster.Runtime.Scenes.Character;
using HybridGame.MasterBlaster.Scripts.Core;

namespace HybridGame.MasterBlaster.Runtime.Scenes.LevelSelect
{
    /// <summary>
    /// Select / Back menu on Level Select: Select → Countdown (with arena prefs from <see cref="LevelSelectController"/>),
    /// Back → Avatar Select. No avatar row persistence here.
    /// </summary>
    public class LevelSelectMenuController : AvatarSelectMenuController
    {
        protected override void Awake()
        {
            ConfigureFlowForTests(FlowState.Countdown, FlowState.AvatarSelect, persistAvatarPrefs: false);
            base.Awake();
        }
    }
}
