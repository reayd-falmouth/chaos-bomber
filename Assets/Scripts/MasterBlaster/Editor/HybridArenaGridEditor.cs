using HybridGame.MasterBlaster.Scripts.Arena;
using UnityEditor;
using UnityEngine;

namespace HybridGame.Editor
{
    [CustomEditor(typeof(HybridArenaGrid))]
    public class HybridArenaGridEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var grid = (HybridArenaGrid)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid alignment (shrink / walls)", EditorStyles.boldLabel);
            if (GUILayout.Button("Log Grid Alignment Diagnostics"))
            {
                grid.LogGridAlignmentDiagnostics();
            }

            EditorGUILayout.HelpBox(
                "Samples destructible WallBlock3D and indestructible collider positions. " +
                "If MinCell should be (0,0) at your authored corner but is not, add the logged " +
                "local delta to gridOriginLocal, or use the button below.",
                MessageType.Info);

            if (GUILayout.Button("Apply suggested gridOriginLocal delta (XZ)"))
            {
                var report = grid.GetGridAlignmentReport();
                if (!report.HasSamples)
                {
                    EditorUtility.DisplayDialog(
                        "HybridArenaGrid",
                        "No wall samples — assign destructible/indestructible parents and ensure the active level roots are enabled.",
                        "OK");
                    return;
                }

                var localDelta = ArenaGridAlignment.WorldDeltaToGridOriginLocalDelta(
                    grid.destructibleWallsParent,
                    report.SuggestedGridOriginWorldDelta);
                if (localDelta.sqrMagnitude < 1e-8f)
                {
                    EditorUtility.DisplayDialog(
                        "HybridArenaGrid",
                        "Suggested delta is zero (MinCell already aligns with 0,0 origin anchor). No change applied.",
                        "OK");
                    return;
                }

                if (!EditorUtility.DisplayDialog(
                        "Apply grid origin delta",
                        "Add local delta " + localDelta + " to gridOriginLocal?\n" +
                        "(Undo available.)",
                        "Apply",
                        "Cancel"))
                    return;

                Undo.RecordObject(grid, "Apply grid origin alignment delta");
                grid.gridOriginLocal += localDelta;
                grid.RepublishGridOrigin();
                EditorUtility.SetDirty(grid);
            }
        }
    }
}
