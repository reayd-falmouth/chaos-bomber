using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HybridGame.MasterBlaster.EditorDocs
{
    [CustomEditor(typeof(ProjectArchitectureReadme))]
    public sealed class ProjectArchitectureReadmeEditor : UnityEditor.Editor
    {
        private Vector2 _scroll;

        private GUIStyle _titleStyle;
        private GUIStyle _h2Style;
        private GUIStyle _bodyStyle;
        private GUIStyle _codeStyle;

        private void EnsureStyles()
        {
            _titleStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                richText = true,
                wordWrap = true
            };

            _h2Style ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                richText = true,
                wordWrap = true
            };

            _bodyStyle ??= new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true
            };

            _codeStyle ??= new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = false
            };
        }

        public override void OnInspectorGUI()
        {
            EnsureStyles();

            var readme = (ProjectArchitectureReadme)target;
            if (readme == null)
                return;

            using var scroll = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scroll.scrollPosition;

            DrawHeader(readme);
            EditorGUILayout.Space(10);

            DrawCourseworkDeliverables(readme);
            EditorGUILayout.Space(10);

            DrawQuickActions(readme);
            EditorGUILayout.Space(12);

            DrawSection("Architecture overview", readme.architectureOverview);
            EditorGUILayout.Space(10);

            DrawSection("How it works", readme.howItWorksDetails);
            EditorGUILayout.Space(10);

            DrawSection("Third-party packages/plugins", readme.thirdPartyNotes);
            EditorGUILayout.Space(10);

            DrawMermaidSection(readme);
            EditorGUILayout.Space(8);

            DrawRawAssetFieldsFallback();
        }

        private void DrawHeader(ProjectArchitectureReadme readme)
        {
            EditorGUILayout.LabelField(readme.title ?? "Project Architecture Readme", _titleStyle);

            if (!string.IsNullOrWhiteSpace(readme.shortDescription))
                EditorGUILayout.LabelField(readme.shortDescription, _bodyStyle);
        }

        private void DrawCourseworkDeliverables(ProjectArchitectureReadme readme)
        {
            EditorGUILayout.LabelField("Prototype / coursework deliverables", _h2Style);
            EditorGUILayout.HelpBox(
                "Keep this in sync with README.md at the repo root. Replace placeholder URLs after you upload your Windows build and source zip.",
                MessageType.Info
            );

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Download links", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                var wUrl = EditorGUILayout.TextField("Windows build URL", readme.windowsBuildUrl ?? "");
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(readme, "Edit Windows build URL");
                    readme.windowsBuildUrl = wUrl;
                    EditorUtility.SetDirty(readme);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = UriLooksHttp(readme.windowsBuildUrl);
                    if (GUILayout.Button("Open in browser", GUILayout.MaxWidth(120)))
                        UnityEngine.Application.OpenURL(readme.windowsBuildUrl.Trim());
                    GUI.enabled = true;
                }

                EditorGUI.BeginChangeCheck();
                var sUrl = EditorGUILayout.TextField("Source backup URL", readme.sourceZipUrl ?? "");
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(readme, "Edit source backup URL");
                    readme.sourceZipUrl = sUrl;
                    EditorUtility.SetDirty(readme);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = UriLooksHttp(readme.sourceZipUrl);
                    if (GUILayout.Button("Open in browser", GUILayout.MaxWidth(120)))
                        UnityEngine.Application.OpenURL(readme.sourceZipUrl.Trim());
                    GUI.enabled = true;
                }
            }

            EditorGUILayout.Space(6);
            DrawEditableCourseworkText("Control scheme", readme, r => r.controlSchemeNotes, (r, v) => r.controlSchemeNotes = v, 100);
            EditorGUILayout.Space(4);
            DrawEditableCourseworkText("Known bugs / issues", readme, r => r.knownIssuesNotes, (r, v) => r.knownIssuesNotes = v, 88);
            EditorGUILayout.Space(4);
            DrawEditableCourseworkText("Third-party assets (summary)", readme, r => r.thirdPartySummary, (r, v) => r.thirdPartySummary = v, 120);
            EditorGUILayout.Space(4);
            DrawEditableCourseworkText(
                "Generative AI disclosure (summary)",
                readme,
                r => r.generativeAiDisclosureNotes,
                (r, v) => r.generativeAiDisclosureNotes = v,
                100
            );
            EditorGUILayout.Space(4);
            DrawEditableCourseworkText(
                "References",
                readme,
                r => r.referencesNotes,
                (r, v) => r.referencesNotes = v,
                88
            );
        }

        private void DrawEditableCourseworkText(
            string label,
            ProjectArchitectureReadme readme,
            Func<ProjectArchitectureReadme, string> getValue,
            Action<ProjectArchitectureReadme, string> setValue,
            float minHeight)
        {
            EditorGUILayout.LabelField(label, _h2Style);
            EditorGUI.BeginChangeCheck();
            var text = EditorGUILayout.TextArea(getValue(readme) ?? "", _codeStyle, GUILayout.MinHeight(minHeight));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(readme, label);
                setValue(readme, text);
                EditorUtility.SetDirty(readme);
            }
        }

        private static bool UriLooksHttp(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;
            var t = url.Trim();
            return t.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                   || t.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        private void DrawQuickActions(ProjectArchitectureReadme readme)
        {
            EditorGUILayout.LabelField("Quick actions", _h2Style);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ping asset", GUILayout.MaxWidth(120)))
                {
                    EditorGUIUtility.PingObject(readme);
                    Selection.activeObject = readme;
                }

                if (GUILayout.Button("Copy Mermaid", GUILayout.MaxWidth(120)))
                {
                    EditorGUIUtility.systemCopyBuffer = readme.mermaidDiagram ?? "";
                }

                if (GUILayout.Button("Open Packages/manifest.json", GUILayout.MaxWidth(200)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Packages/manifest.json");
                    if (obj != null)
                        AssetDatabase.OpenAsset(obj);
                }
            }

            DrawAssetPathButtons("Key scripts", readme.keyScriptAssetPaths, openAs: AssetKind.Script);
            DrawAssetPathButtons("Key scenes", readme.keySceneAssetPaths, openAs: AssetKind.Scene);
        }

        private enum AssetKind
        {
            Any,
            Script,
            Scene
        }

        private void DrawAssetPathButtons(string label, string[] assetPaths, AssetKind openAs)
        {
            if (assetPaths == null || assetPaths.Length == 0)
                return;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(label, _h2Style);

            using var wrap = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            foreach (var rawPath in assetPaths)
            {
                var path = (rawPath ?? "").Trim();
                if (string.IsNullOrEmpty(path))
                    continue;

                using var row = new EditorGUILayout.HorizontalScope();

                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                GUI.enabled = obj != null;
                if (GUILayout.Button(path, GUILayout.ExpandWidth(true)))
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);

                    bool shouldOpen = openAs switch
                    {
                        AssetKind.Scene => true,
                        AssetKind.Script => true,
                        _ => false
                    };

                    if (shouldOpen)
                        AssetDatabase.OpenAsset(obj);
                }
                GUI.enabled = true;

                if (obj != null && GUILayout.Button("Select", GUILayout.MaxWidth(60)))
                    Selection.activeObject = obj;
            }
        }

        private void DrawSection(string title, string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return;

            EditorGUILayout.LabelField(title, _h2Style);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                EditorGUILayout.LabelField(body, _bodyStyle);
        }

        private void DrawMermaidSection(ProjectArchitectureReadme readme)
        {
            if (string.IsNullOrWhiteSpace(readme.mermaidDiagram))
                return;

            EditorGUILayout.LabelField("Mermaid diagram", _h2Style);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "This is plain Mermaid text. Paste it into GitHub/GitLab or a Mermaid live editor.",
                    _bodyStyle
                );

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Copy diagram"))
                        EditorGUIUtility.systemCopyBuffer = readme.mermaidDiagram ?? "";

                    if (GUILayout.Button("Copy diagram (with ```mermaid)"))
                        EditorGUIUtility.systemCopyBuffer = "```mermaid\n" + (readme.mermaidDiagram ?? "") + "\n```";
                }

                using var change = new EditorGUI.ChangeCheckScope();
                var newText = EditorGUILayout.TextArea(readme.mermaidDiagram, _codeStyle, GUILayout.MinHeight(220));
                if (change.changed)
                {
                    Undo.RecordObject(readme, "Edit Mermaid diagram");
                    readme.mermaidDiagram = newText;
                    EditorUtility.SetDirty(readme);
                }
            }
        }

        private void DrawRawAssetFieldsFallback()
        {
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Raw fields", _h2Style);
            EditorGUILayout.HelpBox(
                "If you need to edit content, you can also edit the fields below (Unity serialization).",
                MessageType.None
            );
            DrawDefaultInspector();
        }
    }
}

