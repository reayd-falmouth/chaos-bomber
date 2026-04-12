using System.Collections.Generic;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Arena
{
    /// <summary>
    /// Pure helpers to validate <see cref="ArenaGrid3D"/> mapping against sampled wall world positions
    /// (diagnostics for shrink / gameplay alignment).
    /// </summary>
    public static class ArenaGridAlignment
    {
        public struct Report
        {
            public int SampleCount;
            public int OutOfBoundsCount;
            public Vector2Int MinCell;
            public Vector2Int MaxCell;

            /// <summary>
            /// Add this to current <see cref="ArenaGrid3D.GridOrigin"/> (XZ) so that the observed minimum
            /// cell index moves toward (0,0) — same convention as <see cref="ComputeOriginWorldDeltaToAnchorMinCellAtZero"/>.
            /// </summary>
            public Vector3 SuggestedGridOriginWorldDelta;

            public bool HasSamples => SampleCount > 0;
        }

        /// <summary>
        /// Maps each world position to a cell using <see cref="ArenaGrid3D.WorldToCell"/> with the given origin and cell size,
        /// then records min/max indices and how many lie outside [0, columns)×[0, rows).
        /// </summary>
        public static Report AnalyzeWorldSamples(
            IReadOnlyList<Vector3> worldPositions,
            Vector3 gridOrigin,
            float cellSize,
            int columns,
            int rows
        )
        {
            var prevOrigin = ArenaGrid3D.GridOrigin;
            var prevSize = ArenaGrid3D.CellSize;
            try
            {
                ArenaGrid3D.GridOrigin = gridOrigin;
                ArenaGrid3D.CellSize = cellSize;

                var report = new Report
                {
                    SampleCount = worldPositions != null ? worldPositions.Count : 0,
                    MinCell = new Vector2Int(int.MaxValue, int.MaxValue),
                    MaxCell = new Vector2Int(int.MinValue, int.MinValue),
                };

                if (worldPositions == null || worldPositions.Count == 0)
                    return report;

                for (int i = 0; i < worldPositions.Count; i++)
                {
                    var cell = ArenaGrid3D.WorldToCell(worldPositions[i]);
                    report.MinCell = new Vector2Int(
                        Mathf.Min(report.MinCell.x, cell.x),
                        Mathf.Min(report.MinCell.y, cell.y));
                    report.MaxCell = new Vector2Int(
                        Mathf.Max(report.MaxCell.x, cell.x),
                        Mathf.Max(report.MaxCell.y, cell.y));
                    if (cell.x < 0 || cell.x >= columns || cell.y < 0 || cell.y >= rows)
                        report.OutOfBoundsCount++;
                }

                report.SuggestedGridOriginWorldDelta = ComputeOriginWorldDeltaToAnchorMinCellAtZero(
                    report.MinCell,
                    cellSize);
                return report;
            }
            finally
            {
                ArenaGrid3D.GridOrigin = prevOrigin;
                ArenaGrid3D.CellSize = prevSize;
            }
        }

        /// <summary>
        /// If walls that should include cell (0,0) instead map with minimum index <paramref name="minCellObserved"/>,
        /// add the returned delta to <see cref="ArenaGrid3D.GridOrigin"/> (XZ) to pull indices down toward zero.
        /// Derived from: increasing origin.x by <c>cellSize</c> decreases <c>cell.x</c> by 1 for the same world X.
        /// </summary>
        public static Vector3 ComputeOriginWorldDeltaToAnchorMinCellAtZero(Vector2Int minCellObserved, float cellSize)
        {
            return new Vector3(
                minCellObserved.x * cellSize,
                0f,
                minCellObserved.y * cellSize);
        }

        /// <summary>
        /// Converts a world-space origin adjustment into <paramref name="destructibleWallsParent"/> local space
        /// (XZ), for editing <see cref="HybridArenaGrid.gridOriginLocal"/> when scale is uniform and translation-only.
        /// </summary>
        public static Vector3 WorldDeltaToGridOriginLocalDelta(Transform destructibleWallsParent, Vector3 worldDeltaXZ)
        {
            if (destructibleWallsParent == null)
                return new Vector3(worldDeltaXZ.x, 0f, worldDeltaXZ.z);
            var local = destructibleWallsParent.InverseTransformVector(new Vector3(worldDeltaXZ.x, 0f, worldDeltaXZ.z));
            return new Vector3(local.x, 0f, local.z);
        }
    }
}
