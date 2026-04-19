using System;
using Unity.Cinemachine;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Mobile.Layout
{
    /// <summary>Serializable lens + priority for a <see cref="CinemachineCamera"/> (matched by <see cref="gameObjectName"/>).</summary>
    [Serializable]
    public sealed class MobileHandheldCinemachineVcamSnapshotEntry
    {
        [Tooltip("CinemachineCamera GameObject.name — used to find the vcam when applying presets.")]
        public string gameObjectName = string.Empty;

        public int priority;

        public LensSettings.OverrideModes lensModeOverride = LensSettings.OverrideModes.None;

        public float fieldOfView;

        public float orthographicSize;

        public float nearClipPlane;

        public float farClipPlane;

        public float dutch;

        public static MobileHandheldCinemachineVcamSnapshotEntry Capture(CinemachineCamera vcam)
        {
            if (vcam == null)
                return new MobileHandheldCinemachineVcamSnapshotEntry();
            var lens = vcam.Lens;
            return new MobileHandheldCinemachineVcamSnapshotEntry
            {
                gameObjectName = vcam.gameObject.name,
                priority = vcam.Priority,
                lensModeOverride = lens.ModeOverride,
                fieldOfView = lens.FieldOfView,
                orthographicSize = lens.OrthographicSize,
                nearClipPlane = lens.NearClipPlane,
                farClipPlane = lens.FarClipPlane,
                dutch = lens.Dutch,
            };
        }

        public static void Apply(CinemachineCamera vcam, MobileHandheldCinemachineVcamSnapshotEntry s)
        {
            if (vcam == null || string.IsNullOrEmpty(s.gameObjectName))
                return;
            vcam.Priority = s.priority;
            var lens = vcam.Lens;
            lens.ModeOverride = s.lensModeOverride;
            lens.FieldOfView = s.fieldOfView;
            lens.OrthographicSize = s.orthographicSize;
            lens.NearClipPlane = s.nearClipPlane;
            lens.FarClipPlane = s.farClipPlane;
            lens.Dutch = s.dutch;
            vcam.Lens = lens;
        }
    }
}
