using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Runtime.Scenes.Character
{
    /// <summary>
    /// Renders one of several 3D character preview roots into a <see cref="RenderTexture"/> for Avatar Select.
    /// Geometry should use the <c>CharacterPreview</c> layer. Built via
    /// <c>HybridGame → Setup → Build Avatar Preview Studio</c>.
    /// </summary>
    public sealed class AvatarPreviewRenderer : MonoBehaviour
    {
        public const string PreviewLayerName = "CharacterPreview";

        [SerializeField]
        private Camera previewCamera;

        [SerializeField]
        private RenderTexture renderTexture;

        [SerializeField]
        private RawImage targetRawImage;

        [Tooltip("One root per character index in AvatarController.characters.")]
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

        public int LastRenderedIndex => _lastRenderedIndex;

        public GameObject[] PreviewSlotRoots => previewSlotRoots;

        public Camera PreviewCamera => previewCamera;
    }
}
