using HybridGame.MasterBlaster.Scripts;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Camera
{
    /// <summary>
    /// Marker on a <see cref="Unity.Cinemachine.CinemachineCamera"/> that should be live in
    /// <see cref="GameModeManager.GameMode.ArenaPerspective"/>. Add to an angled / isometric arena rig.
    /// If none are present, <see cref="CinemachineModeSwitcher"/> falls back to Bomberman-style priorities.
    /// </summary>
    public sealed class ArenaPerspectiveCinemachineCamera : MonoBehaviour
    {
    }
}
