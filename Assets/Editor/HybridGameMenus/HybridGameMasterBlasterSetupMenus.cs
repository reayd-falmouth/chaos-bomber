using HybridGame.Editor.MasterBlaster.Scripts.Editor;
using UnityEditor;

namespace HybridGame.Menus
{
    /// <summary>
    /// Menu items must live in an assembly that Unity always pulls into the Editor domain. Scripts in
    /// <c>HybridGame.Editor</c> are not referenced by <c>Assembly-CSharp-Editor</c>, so
    /// <see cref="MenuItem"/> there never registers. This bootstrap references <c>HybridGame.Editor</c> and forwards here.
    /// </summary>
    public static class HybridGameMasterBlasterSetupMenus
    {
        [MenuItem("HybridGame/Setup/Master Blaster FPS — Build 10 Arena Slots And Wire Switcher", false, 500)]
        public static void MenuHybridGame_BuildArenaSlots() => HybridArenaMultiLevelSetup.RunSetup();

        [MenuItem("Tools/Master Blaster/Build 10 Arena Slots And Wire Switcher", false, 500)]
        public static void MenuTools_BuildArenaSlots() => HybridArenaMultiLevelSetup.RunSetup();

        [MenuItem("HybridGame/Setup/Build Level Select Arena Preview Studio", false, 510)]
        public static void MenuHybridGame_ArenaPreview() => ArenaPreviewStudioSetup.RunBuild();

        [MenuItem("Tools/Master Blaster/Build Level Select Arena Preview Studio", false, 510)]
        public static void MenuTools_ArenaPreview() => ArenaPreviewStudioSetup.RunBuild();

        [MenuItem("HybridGame/Setup/Build Avatar Preview Studio", false, 520)]
        public static void MenuHybridGame_AvatarPreview() => AvatarPreviewStudioSetup.RunBuild();

        [MenuItem("Tools/Master Blaster/Build Avatar Preview Studio", false, 520)]
        public static void MenuTools_AvatarPreview() => AvatarPreviewStudioSetup.RunBuild();

        [MenuItem("HybridGame/Generate Destructible Walls", false, 530)]
        public static void MenuHybridGame_GenerateWalls() => DestructibleWallGenerator.GenerateWalls();

        [MenuItem("Tools/Master Blaster/Generate Destructible Walls", false, 530)]
        public static void MenuTools_GenerateWalls() => DestructibleWallGenerator.GenerateWalls();
    }
}
