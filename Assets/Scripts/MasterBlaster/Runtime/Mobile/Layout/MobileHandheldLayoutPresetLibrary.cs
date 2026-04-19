using System.Collections.Generic;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Mobile.Layout
{
    /// <summary>Asset holding a list of handheld layout presets (screen key + captured rects).</summary>
    [CreateAssetMenu(
        fileName = "MobileHandheldLayoutPresetLibrary",
        menuName = "Master Blaster/Mobile Handheld Layout Preset Library",
        order = 100)]
    public class MobileHandheldLayoutPresetLibrary : ScriptableObject
    {
        [Tooltip("Ordered presets; wider/taller keys used by nearest-aspect selection.")]
        public List<MobileHandheldLayoutPresetEntry> entries = new List<MobileHandheldLayoutPresetEntry>();
    }
}
