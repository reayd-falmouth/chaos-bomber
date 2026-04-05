using RMC.Core.ReadMe;
using UnityEditor;

namespace HybridGame.MasterBlaster.ReadMe
{
    /// <summary>
    /// Select RMC ReadMe assets when the editor opens (same pattern as unity-project-template-uitoolkit).
    /// </summary>
    [InitializeOnLoad]
    public static class ReadMeInitializeOnLoad
    {
        private static readonly string HasShownReadMe = "HybridGame.MasterBlaster.HasShownReadMe";

        static ReadMeInitializeOnLoad()
        {
            if (!SessionState.GetBool(HasShownReadMe, false))
                EditorApplication.update += WaitOneFrame;
        }

        private static void WaitOneFrame()
        {
            EditorApplication.update -= WaitOneFrame;
            ReadMeHelper.SelectReadmes();
            SessionState.SetBool(HasShownReadMe, true);
        }
    }
}
