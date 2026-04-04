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
        private int ResolveEffectiveIndex(int requested)
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
            ApplyArenaIndex(idx, rebindBaselineForRuntime: false);
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
            ApplyArenaIndex(idx, rebindBaselineForRuntime: Application.isPlaying);
        }

        private void ApplyArenaIndex(int index, bool rebindBaselineForRuntime)
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

            int idx = ResolveEffectiveIndex(index);
            var active = levelWallRoots[idx];

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
        }
    }
}
