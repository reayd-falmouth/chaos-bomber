using System;
using UnityEngine;

namespace HybridGame.MasterBlaster.EditorDocs
{
    [CreateAssetMenu(
        fileName = "ProjectArchitectureReadme",
        menuName = "HybridGame/MasterBlaster/Project Architecture Readme"
    )]
    public sealed class ProjectArchitectureReadme : ScriptableObject
    {
        [Header("Header")]
        public string title = "MasterBlaster – Architecture Readme";
        [TextArea(2, 8)]
        public string shortDescription =
            "Hybrid Bomberman + FPS built on Unity FPS Microgame, with a single-scene flow option.\n" +
            "Coursework deliverables (links, controls, bugs, third-party) are in the section below; use the buttons to jump to scripts/scenes and copy the Mermaid diagram for technical docs.";

        [Header("Prototype / coursework deliverables")]
        [Tooltip("Windows (or similar) playable build — replace after upload.")]
        public string windowsBuildUrl = "https://REPLACE-WITH-YOUR-WINDOWS-BUILD-LINK.example.com";

        [Tooltip("Project source archive — replace after upload.")]
        public string sourceZipUrl = "https://REPLACE-WITH-YOUR-SOURCE-BACKUP-LINK.example.com";

        [TextArea(8, 24)]
        public string controlSchemeNotes;

        [TextArea(6, 20)]
        public string knownIssuesNotes;

        [TextArea(10, 30)]
        public string thirdPartySummary;

        [Header("Architecture")]
        [TextArea(10, 60)]
        public string architectureOverview;

        [Header("Key Scripts (paths under Assets/)")]
        public string[] keyScriptAssetPaths = Array.Empty<string>();

        [Header("Key Scenes (paths under Assets/)")]
        public string[] keySceneAssetPaths = Array.Empty<string>();

        [Header("Third-party packages/plugins")]
        [TextArea(10, 60)]
        public string thirdPartyNotes;

        [Header("Mermaid diagram (copy/paste)")]
        [TextArea(10, 80)]
        public string mermaidDiagram;

        [Header("More details")]
        [TextArea(10, 80)]
        public string howItWorksDetails;
    }
}

