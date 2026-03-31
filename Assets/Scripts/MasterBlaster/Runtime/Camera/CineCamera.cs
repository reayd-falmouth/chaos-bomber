using UnityEngine;
using Unity.Cinemachine;

namespace HybridGame.MasterBlaster.Scripts.Camera
{
    [RequireComponent(typeof(CinemachineCamera))]
    public class CineCameraRegister : MonoBehaviour
    {
        private CinemachineCamera m_Cam;

        private void Awake()
        {
            m_Cam = GetComponent<CinemachineCamera>();

            // Prefer registering in Awake so priorities are applied before the first Brain update.
            if (CinemachineModeSwitcher.Instance != null)
                CinemachineModeSwitcher.Instance.Register(m_Cam);
        }

        private void Start()
        {
            // Tell the manager we exist
            if (CinemachineModeSwitcher.Instance != null && m_Cam != null)
                CinemachineModeSwitcher.Instance.Register(m_Cam);
        }

        private void OnDestroy()
        {
            // Clean up the list if the player/object is destroyed
            if (CinemachineModeSwitcher.Instance != null)
                CinemachineModeSwitcher.Instance.Unregister(m_Cam);
        }
    }
}