using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Core
{
    /// <summary>
    /// Marker for the single-scene flow system.
    ///
    /// When <see cref="Core.SceneFlowManager"/> detects these markers in the active Unity scene,
    /// it will toggle exactly one root active per <see cref="FlowState"/> instead of calling
    /// <c>SceneManager.LoadScene</c>.
    /// </summary>
    public class FlowCanvasRoot : MonoBehaviour
    {
        [Tooltip("Which flow state this root represents.")]
        public FlowState state;
    }
}

