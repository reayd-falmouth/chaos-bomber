using HybridGame.MasterBlaster.Scripts.Debug;
using HybridGame.MasterBlaster.Scripts.Levels;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Arena
{
    /// <summary>
    /// Enables one of several <see cref="HybridArenaGrid"/> wall-root pairs under <c>Plane</c> based on
    /// <see cref="LevelSelectionPrefs.SelectedArenaIndexKey"/>. Runs before <see cref="HybridArenaGrid.Awake"/>
    /// so grid origin and baseline capture see the correct parents.
    /// </summary>
    [DefaultExecutionOrder(-560)]
    public class HybridArenaLevelRootSwitcher : MonoBehaviour
    {
        [System.Serializable]
        public struct LevelWallRoots
        {
            public Transform destructibleWallsRoot;
            public Transform indestructibleWallsRoot;
        }

        [Tooltip("Index 0 = first arena, … Order must match Level Select carousel. Leave empty until you run the editor setup menu.")]
        [SerializeField]
        private LevelWallRoots[] levelWallRoots = new LevelWallRoots[0];

        private HybridArenaGrid _grid;

        private bool HasAnyConfiguredPair()
        {
            if (levelWallRoots == null)
                return false;
            for (int i = 0; i < levelWallRoots.Length; i++)
            {
                if (levelWallRoots[i].destructibleWallsRoot != null
                    && levelWallRoots[i].indestructibleWallsRoot != null)
                    return true;
            }
            return false;
        }

        private int ResolveClampIndex(int requested)
        {
            if (levelWallRoots == null || levelWallRoots.Length == 0)
                return 0;
            return Mathf.Clamp(requested, 0, levelWallRoots.Length - 1);
        }

        /// <summary>Picks the requested index, or the first slot with both roots assigned if that slot is incomplete.</summary>
        private int ResolveEffectiveIndexWithFallback(int requested)
        {
            int idx = ResolveClampIndex(requested);
            if (levelWallRoots == null || levelWallRoots.Length == 0)
                return 0;
            if (levelWallRoots[idx].destructibleWallsRoot != null
                && levelWallRoots[idx].indestructibleWallsRoot != null)
                return idx;
            for (int i = 0; i < levelWallRoots.Length; i++)
            {
                if (levelWallRoots[i].destructibleWallsRoot != null
                    && levelWallRoots[i].indestructibleWallsRoot != null)
                    return i;
            }
            return idx;
        }

        private void Awake()
        {
            _grid = GetComponent<HybridArenaGrid>();
            if (_grid == null)
            {
                UnityEngine.Debug.LogWarning("[HybridArenaLevelRootSwitcher] No HybridArenaGrid on this GameObject.");
                return;
            }

            if (!HasAnyConfiguredPair())
                return;

            int idx = PlayerPrefs.GetInt(LevelSelectionPrefs.SelectedArenaIndexKey, 0);
            ApplyArenaIndex(idx, rebindBaselineForRuntime: false, allowIncompleteSlotFallback: true);
        }

        /// <summary>
        /// Call when re-entering the arena in single-scene mode (Awake does not run again).
        /// </summary>
        public void ReapplyFromPrefs()
        {
            if (_grid == null)
                _grid = GetComponent<HybridArenaGrid>();
            if (_grid == null || !HasAnyConfiguredPair())
                return;

            int idx = PlayerPrefs.GetInt(LevelSelectionPrefs.SelectedArenaIndexKey, 0);
            // Menu / level-select prefs must match the chosen index exactly; do not remap 0 → first “other” complete slot.
            ApplyArenaIndex(idx, rebindBaselineForRuntime: Application.isPlaying, allowIncompleteSlotFallback: false);
        }

        private void ApplyArenaIndex(int index, bool rebindBaselineForRuntime, bool allowIncompleteSlotFallback = true)
        {
            if (_grid.destructibleWallsLayoutPrefab != null
                && levelWallRoots != null
                && levelWallRoots.Length > 1)
            {
                UnityEngine.Debug.LogWarning(
                    "[HybridArenaLevelRootSwitcher] HybridArenaGrid.destructibleWallsLayoutPrefab is set while multiple arena slots are configured. "
                    + "Baseline restore always clones that single prefab, so every selected level matches the same destructible layout. "
                    + "Clear the prefab when using per-slot wall roots authored in the scene.");
            }

            int idx = allowIncompleteSlotFallback
                ? ResolveEffectiveIndexWithFallback(index)
                : ResolveClampIndex(index);
            var active = levelWallRoots[idx];
            if (active.destructibleWallsRoot == null || active.indestructibleWallsRoot == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[HybridArenaLevelRootSwitcher] Slot " + idx +
                    " has missing wall roots; using legacy fallback. Fix levelWallRoots in the Inspector.");
                idx = ResolveEffectiveIndexWithFallback(index);
                active = levelWallRoots[idx];
            }

            for (int i = 0; i < levelWallRoots.Length; i++)
            {
                var pair = levelWallRoots[i];
                if (pair.destructibleWallsRoot == null || pair.indestructibleWallsRoot == null)
                    continue;
                bool on = i == idx;
                pair.destructibleWallsRoot.gameObject.SetActive(on);
                pair.indestructibleWallsRoot.gameObject.SetActive(on);
            }

            _grid.destructibleWallsParent = active.destructibleWallsRoot;
            _grid.indestructibleWallsParent = active.indestructibleWallsRoot;
            _grid.RepublishGridOrigin();
            _grid.ResetDestructibleBaselineState();

            if (rebindBaselineForRuntime)
                _grid.RecaptureBaselineAndRestoreLayout();

            // #region agent log
            AgentDebugNdjson_a63d36.Log(
                "H_apply",
                "HybridArenaLevelRootSwitcher.ApplyArenaIndex",
                "applied",
                "{\"requested\":" + index + ",\"effective\":" + idx + ",\"pairCount\":" +
                (levelWallRoots != null ? levelWallRoots.Length : 0) +
                ",\"allowIncompleteSlotFallback\":" + (allowIncompleteSlotFallback ? "true" : "false") +
                ",\"slot0DNull\":" +
                (levelWallRoots != null && levelWallRoots.Length > 0 && levelWallRoots[0].destructibleWallsRoot == null
                    ? "true"
                    : "false") +
                ",\"slot0INull\":" +
                (levelWallRoots != null && levelWallRoots.Length > 0 && levelWallRoots[0].indestructibleWallsRoot == null
                    ? "true"
                    : "false") +
                "}");
            // #endregion
        }
    }
}
