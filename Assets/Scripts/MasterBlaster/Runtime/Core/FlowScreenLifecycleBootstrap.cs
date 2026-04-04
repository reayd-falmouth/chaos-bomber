using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Core
{
    /// <summary>
    /// Runs once after the first scene load, before <see cref="MonoBehaviour.Awake"/> / <see cref="MonoBehaviour.OnEnable"/>,
    /// so <see cref="FlowCanvasRoot"/> managed controllers are off until <see cref="SceneFlowManager"/> applies the active <see cref="FlowState"/>.
    /// </summary>
    internal static class FlowScreenLifecycleBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void DisableAllManagedBehavioursBeforeLifecycle()
        {
            FlowCanvasRoot.DisableAllManagedBehavioursForInitialSceneLoad();
        }
    }
}
