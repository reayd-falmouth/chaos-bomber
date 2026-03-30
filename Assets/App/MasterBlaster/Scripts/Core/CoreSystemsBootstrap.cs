using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Core
{
    /// <summary>
    /// Ensures SessionManager and SceneFlowManager exist before any scene loads.
    /// They are PersistentSingletons (DontDestroyOnLoad), so they live for the full app lifecycle.
    /// </summary>
    public static class CoreSystemsBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureCoreSystemsExist()
        {
            if (SessionManager.Instance == null)
            {
                var go = new GameObject("SessionManager");
                go.AddComponent<SessionManager>();
                UnityEngine.Debug.Log(
                    "[CoreSystemsBootstrap] SessionManager created and will persist across scenes."
                );
            }

            if (SceneFlowManager.Instance == null)
            {
                var go = new GameObject("SceneFlowManager");
                go.AddComponent<SceneFlowManager>();
                UnityEngine.Debug.Log(
                    "[CoreSystemsBootstrap] SceneFlowManager created and will persist across scenes."
                );
            }
        }
    }
}
