using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Runtime.Scenes.Character
{
    /// <summary>Legacy UI helper for showing the selected character's starting-perk icon.</summary>
    public static class AvatarPerkIconUi
    {
        /// <summary>Assigns <paramref name="sprite"/> and enables the graphic only when non-null.</summary>
        public static void ApplyPerkIcon(Image image, Sprite sprite)
        {
            if (image == null)
                return;
            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }
}
