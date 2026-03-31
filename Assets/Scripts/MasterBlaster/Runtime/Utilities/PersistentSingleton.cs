using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Utilities
{
    /// <summary>
    /// Singleton that persists across scene loads.
    /// Copied from MasterBlaster (Utilities.PersistentSingleton), namespace changed.
    /// </summary>
    public class PersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this as T;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (ReferenceEquals(Instance, this))
                Instance = null;
        }
    }
}
