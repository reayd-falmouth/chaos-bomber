using RMC.Core.ReadMe;
using UnityEditor;

namespace HybridGame.MasterBlaster.ReadMe
{
    public static class ReadMeMenuItems
    {
        public const string WindowPath = "Window/MasterBlaster/Documentation";
        public const int PriorityMenuItem = -1000;

        [MenuItem(WindowPath + "/Open ReadMe", false, PriorityMenuItem)]
        public static void SelectReadmes()
        {
            ReadMeHelper.SelectReadmes();
        }
    }
}
