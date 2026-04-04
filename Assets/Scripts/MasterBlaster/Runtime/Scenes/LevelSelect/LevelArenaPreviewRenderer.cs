using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Runtime.Scenes.LevelSelect
{
    /// <summary>
    /// Renders one of several duplicated arena wall roots (preview studio) into a <see cref="RenderTexture"/>
    /// for the Level Select UI. The preview geometry must live on the <c>ArenaPreview</c> layer and stay active
    /// while the Game flow root is disabled.
    /// </summary>
    public sealed class LevelArenaPreviewRenderer : MonoBehaviour
    {
        public const string PreviewLayerName = "ArenaPreview";

        [SerializeField]
        private Camera previewCamera;

        [SerializeField]
        private RenderTexture renderTexture;

        [SerializeField]
        private RawImage targetRawImage;

        [Tooltip("One root per level index; only one is active at a time. Built via HybridGame → Setup → Build Level Select Arena Preview Studio.")]
        [SerializeField]
        private GameObject[] previewSlotRoots;

        private int _lastRenderedIndex = -1;

        private void Awake()
        {
            ApplyTextureToUi();
        }

        private void OnValidate()
        {
            ApplyTextureToUi();
        }

        private void ApplyTextureToUi()
        {
            if (targetRawImage != null && renderTexture != null)
                targetRawImage.texture = renderTexture;
        }

        /// <summary>Updates visible slot and refreshes the render target once.</summary>
        public void SetPreviewIndex(int index)
        {
            if (previewSlotRoots == null || previewSlotRoots.Length == 0)
                return;

            int clamped = Mathf.Clamp(index, 0, previewSlotRoots.Length - 1);

            for (int i = 0; i < previewSlotRoots.Length; i++)
            {
                var root = previewSlotRoots[i];
                if (root == null)
                    continue;
                root.SetActive(i == clamped);
            }

            if (previewCamera == null || renderTexture == null)
                return;

            bool wasEnabled = previewCamera.enabled;
            previewCamera.enabled = true;
            previewCamera.targetTexture = renderTexture;
            previewCamera.Render();
            previewCamera.enabled = wasEnabled;
            _lastRenderedIndex = clamped;
        }

        /// <summary>Used by tests and editor tooling.</summary>
        public int LastRenderedIndex => _lastRenderedIndex;

        public GameObject[] PreviewSlotRoots => previewSlotRoots;

        public Camera PreviewCamera => previewCamera;
    }
}
