#if UNITY_EDITOR
using System.Collections.Generic;
using HybridGame.MasterBlaster.Scripts.Arena;
using UnityEditor;
using UnityEngine;

namespace HybridGame.Editor.MasterBlaster.Scripts.Editor
{
    /// <summary>
    /// One-shot editor setup: duplicate arena wall roots to ten slots under Plane, apply appendix-style
    /// patterns to slots 2–9, add <see cref="HybridArenaLevelRootSwitcher"/> and wire references.
    /// Run with the MasterBlaster_FPS scene open.
    /// </summary>
    public static class HybridArenaMultiLevelSetup
    {
        private const string DestructiblePrefabGuid = "743c1258fd76aee46850f782dc2f7220";
        private const string IndestructiblePrefabGuid = "2646c645e7c358f4b9befd72888daed3";

        [MenuItem("HybridGame/Setup/Master Blaster FPS — Build 10 Arena Slots And Wire Switcher")]
        private static void RunSetup()
        {
            var grid = Object.FindFirstObjectByType<HybridArenaGrid>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            if (grid == null)
            {
                EditorUtility.DisplayDialog(
                    "HybridArenaMultiLevelSetup",
                    "No HybridArenaGrid found. Open the hybrid scene (e.g. MasterBlaster_FPS); the arena may be under an inactive Game root.",
                    "OK");
                return;
            }

            if (grid.destructibleWallsParent == null || grid.indestructibleWallsParent == null)
            {
                EditorUtility.DisplayDialog("HybridArenaMultiLevelSetup", "Assign destructible and indestructible parents on HybridArenaGrid first.", "OK");
                return;
            }

            var dPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(DestructiblePrefabGuid));
            var iPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(IndestructiblePrefabGuid));
            if (dPrefab == null || iPrefab == null)
            {
                EditorUtility.DisplayDialog("HybridArenaMultiLevelSetup", "Could not load DestructibleCube / IndestructibleCube prefabs by GUID.", "OK");
                return;
            }

            var plane = grid.destructibleWallsParent.parent;
            if (plane == null)
            {
                EditorUtility.DisplayDialog("HybridArenaMultiLevelSetup", "destructibleWallsParent has no parent (expected Plane).", "OK");
                return;
            }

            Undo.SetCurrentGroupName("Build 10 Arena Slots");
            int undoGroup = Undo.GetCurrentGroup();

            var templateD = grid.destructibleWallsParent;
            var templateI = grid.indestructibleWallsParent;

            if (!templateD.name.Contains("_L"))
            {
                Undo.RecordObject(templateD.gameObject, "Rename DestructibleWalls");
                templateD.name = "DestructibleWalls_L0";
            }
            if (!templateI.name.Contains("_L"))
            {
                Undo.RecordObject(templateI.gameObject, "Rename IndestructibleWalls");
                templateI.name = "IndestructibleWalls_L0";
            }

            Transform[] dRoots = new Transform[10];
            Transform[] iRoots = new Transform[10];
            dRoots[0] = templateD;
            iRoots[0] = templateI;

            for (int i = 1; i < 10; i++)
            {
                string dn = $"DestructibleWalls_L{i}";
                string ind = $"IndestructibleWalls_L{i}";
                var existingD = plane.Find(dn);
                var existingI = plane.Find(ind);
                if (existingD != null)
                {
                    dRoots[i] = existingD;
                    iRoots[i] = existingI;
                    continue;
                }

                var copyD = Object.Instantiate(templateD.gameObject, plane);
                copyD.name = dn;
                Undo.RegisterCreatedObjectUndo(copyD, "Duplicate destructible root");
                var copyI = Object.Instantiate(templateI.gameObject, plane);
                copyI.name = ind;
                Undo.RegisterCreatedObjectUndo(copyI, "Duplicate indestructible root");
                dRoots[i] = copyD.transform;
                iRoots[i] = copyI.transform;
                copyD.SetActive(false);
                copyI.SetActive(false);
            }

            templateD.gameObject.SetActive(true);
            templateI.gameObject.SetActive(true);

            grid.destructibleWallsParent = templateD;
            grid.indestructibleWallsParent = templateI;
            Undo.RecordObject(grid, "Point grid at L0");

            int cols = grid.columns;
            int rows = grid.rows;

            for (int slot = 2; slot < 10; slot++)
            {
                int patternId = slot - 2;
                ClearChildren(dRoots[slot]);
                ClearChildren(iRoots[slot]);
                HybridArenaPatternFill.ApplyPattern(grid, dRoots[slot], iRoots[slot], dPrefab, iPrefab, patternId, cols, rows);
            }

            var switcher = grid.GetComponent<HybridArenaLevelRootSwitcher>();
            if (switcher == null)
                switcher = Undo.AddComponent<HybridArenaLevelRootSwitcher>(grid.gameObject);

            var so = new SerializedObject(switcher);
            var arr = so.FindProperty("levelWallRoots");
            arr.arraySize = 10;
            for (int i = 0; i < 10; i++)
            {
                var el = arr.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("destructibleWallsRoot").objectReferenceValue = dRoots[i];
                el.FindPropertyRelative("indestructibleWallsRoot").objectReferenceValue = iRoots[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.DisplayDialog("HybridArenaMultiLevelSetup",
                "Done. Saved 10 wall-root pairs (L0–L9). Slots L2–L9 use procedural appendix patterns. " +
                "Save the scene. Add 10 LevelData entries on Level Select UI if needed.",
                "OK");
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var ch = parent.GetChild(i).gameObject;
                Undo.DestroyObjectImmediate(ch);
            }
        }
    }

    internal static class HybridArenaPatternFill
    {
        internal static void ApplyPattern(
            HybridArenaGrid grid,
            Transform destParent,
            Transform hardParent,
            GameObject dPrefab,
            GameObject iPrefab,
            int patternId,
            int cols,
            int rows)
        {
            SyncGridOrigin(grid, destParent);

            var shell = BuildShellCells(cols, rows);
            var innerHard = new HashSet<Vector2Int>();
            var soft = new HashSet<Vector2Int>();
            var empty = new HashSet<Vector2Int>();

            Random.InitState(1000 + patternId * 31);

            switch (patternId)
            {
                case 0: PatternLattice(cols, rows, innerHard, soft, empty); break;
                case 1: PatternCrossroads(cols, rows, innerHard, soft, empty); break;
                case 2: PatternFourRooms(cols, rows, innerHard, soft, empty); break;
                case 3: PatternRing(cols, rows, innerHard, soft, empty); break;
                case 4: PatternScatter(cols, rows, innerHard, soft, empty); break;
                case 5: PatternDiagonal(cols, rows, innerHard, soft, empty); break;
                case 6: PatternMazeBars(cols, rows, innerHard, soft, empty); break;
                case 7: PatternStrongholds(cols, rows, innerHard, soft, empty); break;
                default:
                    PatternLattice(cols, rows, innerHard, soft, empty);
                    break;
            }

            var allHard = new HashSet<Vector2Int>(shell);
            foreach (var h in innerHard)
                allHard.Add(h);

            foreach (var c in allHard)
                PlaceIndestructible(hardParent, iPrefab, c, grid);

            foreach (var c in soft)
            {
                if (allHard.Contains(c) || empty.Contains(c) || IsSpawnSafe(c.x, c.y, cols, rows))
                    continue;
                if (Random.value > GetSoftKeepChance(patternId))
                    continue;
                PlaceDestructible(destParent, dPrefab, c, grid);
            }
        }

        private static float GetSoftKeepChance(int patternId)
        {
            return patternId switch
            {
                1 => 0.58f,
                2 => 0.78f,
                4 => 0.72f,
                5 => 0.32f,
                6 => 0.52f,
                7 => 0.75f,
                _ => 0.68f,
            };
        }

        private static void SyncGridOrigin(HybridArenaGrid grid, Transform destructibleParent)
        {
            var w = destructibleParent.TransformPoint(grid.gridOriginLocal);
            ArenaGrid3D.GridOrigin = new Vector3(w.x, 0f, w.z);
        }

        private static bool IsSpawnSafe(int c, int r, int cols, int rows)
        {
            const int arm = 2;
            return (c <= arm && r <= arm)
                || (c <= arm && r >= rows - 1 - arm)
                || (c >= cols - 1 - arm && r <= arm)
                || (c >= cols - 1 - arm && r >= rows - 1 - arm);
        }

        private static HashSet<Vector2Int> BuildShellCells(int cols, int rows)
        {
            var s = new HashSet<Vector2Int>();
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    if (c == 0 || r == 0 || c == cols - 1 || r == rows - 1)
                        s.Add(new Vector2Int(c, r));
                }
            }
            return s;
        }

        private static void PatternLattice(int cols, int rows, HashSet<Vector2Int> innerHard, HashSet<Vector2Int> soft, HashSet<Vector2Int> empty)
        {
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    if (c == 0 || r == 0 || c == cols - 1 || r == rows - 1) continue;
                    if (c % 2 == 0 && r % 2 == 0)
                    {
                        if (!IsSpawnSafe(c, r, cols, rows))
                            innerHard.Add(new Vector2Int(c, r));
                    }
                    else if (!IsSpawnSafe(c, r, cols, rows))
                        soft.Add(new Vector2Int(c, r));
                }
            }
        }

        private static void PatternCrossroads(int cols, int rows, HashSet<Vector2Int> innerHard, HashSet<Vector2Int> soft, HashSet<Vector2Int> empty)
        {
            for (int c = 3; c <= 15; c++)
                empty.Add(new Vector2Int(c, 7));
            for (int r = 3; r <= 11; r++)
                empty.Add(new Vector2Int(9, r));

            innerHard.Add(new Vector2Int(6, 5));
            innerHard.Add(new Vector2Int(12, 5));
            innerHard.Add(new Vector2Int(6, 9));
            innerHard.Add(new Vector2Int(12, 9));

            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    if (c == 0 || r == 0 || c == cols - 1 || r == rows - 1) continue;
                    var cell = new Vector2Int(c, r);
                    if (empty.Contains(cell) || innerHard.Contains(cell) || IsSpawnSafe(c, r, cols, rows))
                        continue;
                    soft.Add(cell);
                }
            }
        }

        private static void PatternFourRooms(int cols, int rows, HashSet<Vector2Int> innerHard, HashSet<Vector2Int> soft, HashSet<Vector2Int> empty)
        {
            for (int r = 4; r <= 10; r++)
            {
                if (r == 6 || r == 10)
                {
                    empty.Add(new Vector2Int(9, r));
                    continue;
                }
                innerHard.Add(new Vector2Int(9, r));
            }

            for (int c = 3; c <= 15; c++)
            {
                if (c == 7 || c == 11)
                {
                    empty.Add(new Vector2Int(c, 7));
                    continue;
                }
                innerHard.Add(new Vector2Int(c, 7));
            }

            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    if (c == 0 || r == 0 || c == cols - 1 || r == rows - 1) continue;
                    var cell = new Vector2Int(c, r);
                    if (innerHard.Contains(cell) || empty.Contains(cell) || IsSpawnSafe(c, r, cols, rows))
                        continue;
                    soft.Add(cell);
                }
            }
        }

        private static void PatternRing(int cols, int rows, HashSet<Vector2Int> innerHard, HashSet<Vector2Int> soft, HashSet<Vector2Int> empty)
        {
            innerHard.Add(new Vector2Int(6, 6));
            innerHard.Add(new Vector2Int(12, 6));
            innerHard.Add(new Vector2Int(6, 8));
            innerHard.Add(new Vector2Int(12, 8));

            int cx = 9, cy = 7;
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    if (c == 0 || r == 0 || c == cols - 1 || r == rows - 1) continue;
                    if (IsSpawnSafe(c, r, cols, rows)) continue;
                    int d = Mathf.Max(Mathf.Abs(c - cx), Mathf.Abs(r - cy));
                    if (d <= 3)
                        empty.Add(new Vector2Int(c, r));
                    else if (d == 4 || d == 5)
                        soft.Add(new Vector2Int(c, r));
                }
            }
        }

        private static void PatternScatter(int cols, int rows, HashSet<Vector2Int> innerHard, HashSet<Vector2Int> soft, HashSet<Vector2Int> empty)
        {
            int cx = 9, cy = 7;
            void Add4(int x, int y)
            {
                innerHard.Add(new Vector2Int(x, y));
                innerHard.Add(new Vector2Int(2 * cx - x, y));
                innerHard.Add(new Vector2Int(x, 2 * cy - y));
                innerHard.Add(new Vector2Int(2 * cx - x, 2 * cy - y));
            }
            Add4(5, 5);
            Add4(7, 4);
            Add4(11, 4);
            Add4(13, 5);

            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    if (c == 0 || r == 0 || c == cols - 1 || r == rows - 1) continue;
                    var cell = new Vector2Int(c, r);
                    if (innerHard.Contains(cell) || IsSpawnSafe(c, r, cols, rows))
                        continue;
                    soft.Add(cell);
                }
            }
        }

        private static void PatternDiagonal(int cols, int rows, HashSet<Vector2Int> innerHard, HashSet<Vector2Int> soft, HashSet<Vector2Int> empty)
        {
            for (int k = 0; k <= 6; k++)
            {
                int c1 = 3 + k, r1 = 3 + k;
                int c2 = 15 - k, r2 = 3 + k;
                if (!IsSpawnSafe(c1, r1, cols, rows)) innerHard.Add(new Vector2Int(c1, r1));
                if (!IsSpawnSafe(c2, r2, cols, rows)) innerHard.Add(new Vector2Int(c2, r2));
            }

            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    if (c == 0 || r == 0 || c == cols - 1 || r == rows - 1) continue;
                    var cell = new Vector2Int(c, r);
                    if (innerHard.Contains(cell) || IsSpawnSafe(c, r, cols, rows)) continue;
                    bool nearDiag = false;
                    foreach (var h in innerHard)
                    {
                        if (Mathf.Abs(h.x - c) + Mathf.Abs(h.y - r) == 1)
                        {
                            nearDiag = true;
                            break;
                        }
                    }
                    if (nearDiag && (c + r) % 2 == 0)
                        soft.Add(cell);
                    else if (!nearDiag && Random.value < 0.28f)
                        soft.Add(cell);
                }
            }
        }

        private static void PatternMazeBars(int cols, int rows, HashSet<Vector2Int> innerHard, HashSet<Vector2Int> soft, HashSet<Vector2Int> empty)
        {
            void Bar(int row, int c0, int c1, int gapStart)
            {
                for (int c = c0; c <= c1; c++)
                {
                    if (c >= gapStart && c <= gapStart + 1) continue;
                    innerHard.Add(new Vector2Int(c, row));
                }
            }
            Bar(4, 3, 11, 5);
            Bar(7, 5, 15, 9);
            Bar(10, 4, 12, 7);

            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    if (c == 0 || r == 0 || c == cols - 1 || r == rows - 1) continue;
                    var cell = new Vector2Int(c, r);
                    if (innerHard.Contains(cell) || IsSpawnSafe(c, r, cols, rows)) continue;
                    if (r >= 5 && r <= 9 && !innerHard.Contains(cell))
                        soft.Add(cell);
                }
            }
        }

        private static void PatternStrongholds(int cols, int rows, HashSet<Vector2Int> innerHard, HashSet<Vector2Int> soft, HashSet<Vector2Int> empty)
        {
            void Fort2x2(int c0, int r0)
            {
                innerHard.Add(new Vector2Int(c0, r0));
                innerHard.Add(new Vector2Int(c0 + 1, r0));
                innerHard.Add(new Vector2Int(c0, r0 + 1));
                innerHard.Add(new Vector2Int(c0 + 1, r0 + 1));
            }
            Fort2x2(5, 4);
            Fort2x2(12, 4);
            Fort2x2(5, 9);
            Fort2x2(12, 9);

            for (int c = 7; c <= 11; c++) empty.Add(new Vector2Int(c, 7));
            for (int r = 5; r <= 9; r++) empty.Add(new Vector2Int(9, r));

            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    if (c == 0 || r == 0 || c == cols - 1 || r == rows - 1) continue;
                    var cell = new Vector2Int(c, r);
                    if (innerHard.Contains(cell) || empty.Contains(cell) || IsSpawnSafe(c, r, cols, rows))
                        continue;
                    int dc = Mathf.Abs(c - 9), dr = Mathf.Abs(r - 7);
                    if (dc <= 2 && dr <= 2 && (dc == 0 || dr == 0))
                        continue;
                    if (dc <= 2 && dr <= 2)
                        soft.Add(cell);
                    else if (Random.value < 0.35f)
                        soft.Add(cell);
                }
            }
        }

        private static void PlaceDestructible(Transform parent, GameObject prefab, Vector2Int cell, HybridArenaGrid grid)
        {
            SyncGridOrigin(grid, parent);
            var world = ArenaGrid3D.CellToWorld(cell);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(go, "Place D");
            go.transform.position = world;
            go.name = $"DWall_{cell.x}_{cell.y}";
        }

        private static void PlaceIndestructible(Transform parent, GameObject prefab, Vector2Int cell, HybridArenaGrid grid)
        {
            SyncGridOrigin(grid, parent);
            var world = ArenaGrid3D.CellToWorld(cell);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(go, "Place I");
            go.transform.position = world;
            go.name = $"IWall_{cell.x}_{cell.y}";
        }
    }
}
#endif
