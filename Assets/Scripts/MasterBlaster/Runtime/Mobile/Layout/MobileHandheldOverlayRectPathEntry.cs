using System;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Mobile.Layout
{
    /// <summary>RectTransform layout keyed by a path relative to <see cref="MobileHandheldLayoutController"/> mobileOverlayRoot.</summary>
    [Serializable]
    public sealed class MobileHandheldOverlayRectPathEntry
    {
        [Tooltip("Path from mobileOverlayRoot; segments are \"{siblingIndex}-{GameObject.name}\" joined by '/'.")]
        public string relativePath = string.Empty;

        public MobileHandheldRectSnapshot rect;
    }
}
