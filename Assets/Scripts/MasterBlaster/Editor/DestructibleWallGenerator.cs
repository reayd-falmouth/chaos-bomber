using HybridGame.MasterBlaster.Scripts.Arena;
using UnityEditor;
using UnityEngine;

namespace HybridGame.Editor.MasterBlaster.Scripts.Editor
{
    /// <summary>
    /// Editor utility to auto-populate the flat Bomberman grid with destructible wall prefabs.
    /// Matches MasterBlaster's DestructibleGenerator concept but for 3D XZ layout.
    ///
    /// Usage: select the DestructibleWalls parent GameObject, then
    ///        HybridGame > Generate Destructible Walls.
    /// </summary>
    public static class DestructibleWallGenerator
    {
        /// <summary>Invoked from the HybridGame.Menus editor menu bootstrap.</summary>
        public static void GenerateWalls()
        {
            var grid = Object.FindFirstObjectByType<HybridArenaGrid>();
            if (grid == null)
            {
                EditorUtility.DisplayDialog("HybridGame", "No HybridArenaGrid found in scene.", "OK");
                return;
            }

            if (grid.destructibleWallsParent == null)
            {
                EditorUtility.DisplayDialog("HybridGame", "HybridArenaGrid.destructibleWallsParent is not assigned.", "OK");
                return;
            }

            var wallPrefab = Selection.activeGameObject;
            if (wallPrefab == null || !PrefabUtility.IsPartOfPrefabAsset(wallPrefab))
            {
                EditorUtility.DisplayDialog("HybridGame",
                    "Select a WallBlock3D prefab in the Project window first.", "OK");
                return;
            }

            int cols = grid.columns;
            int rows = grid.rows;

            // Standard Bomberman layout:
            // - Border cells = indestructible (skip)
            // - Odd-row, odd-col pillars = indestructible (skip)
            // - Player spawn corners (top-left 2x2, etc.) = empty
            // - All remaining interior cells = destructible (with some random gaps)
            Undo.SetCurrentGroupName("Generate Destructible Walls");
            int group = Undo.GetCurrentGroup();

            // Clear existing children
            for (int i = grid.destructibleWallsParent.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(grid.destructibleWallsParent.GetChild(i).gameObject);

            int placed = 0;
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    if (!IsCellDestructible(c, r, cols, rows)) continue;

                    var world = ArenaGrid3D.CellToWorld(new Vector2Int(c, r));
                    var go = PrefabUtility.InstantiatePrefab(wallPrefab, grid.destructibleWallsParent) as GameObject;
                    if (go == null) continue;

                    Undo.RegisterCreatedObjectUndo(go, "Place Wall");
                    go.transform.position = world;
                    go.name = $"DWall_{c}_{r}";
                    placed++;
                }
            }

            Undo.CollapseUndoOperations(group);
            Debug.Log($"[HybridGame] Placed {placed} destructible walls.");
        }

        private static bool IsCellDestructible(int c, int r, int cols, int rows)
        {
            // Border cells — always indestructible (handled by separate parent)
            if (c == 0 || r == 0 || c == cols - 1 || r == rows - 1) return false;

            // Pillar cells at every even grid position — indestructible
            if (c % 2 == 0 && r % 2 == 0) return false;

            // Player spawn corners — leave 2x2 open at each corner
            bool spawnCorner =
                (c <= 2 && r <= 2) ||
                (c <= 2 && r >= rows - 3) ||
                (c >= cols - 3 && r <= 2) ||
                (c >= cols - 3 && r >= rows - 3);
            if (spawnCorner) return false;

            // Random 70 % fill for remaining cells (repeatable seed)
            Random.InitState(c * 1000 + r);
            return Random.value < 0.7f;
        }
    }
}
