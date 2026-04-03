using System.Collections.Generic;
using HybridGame.MasterBlaster.Scripts.Core;
using HybridGame.MasterBlaster.Scripts.Levels;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.LevelSelectLocal
{
    public class LevelSelectController : MonoBehaviour, IFlowScreen
    {
        /// <summary>Alias for tests and external readers; value matches <see cref="LevelSelectionPrefs.SelectedLevelIdKey"/>.</summary>
        public const string SelectedLevelIdPrefsKey = LevelSelectionPrefs.SelectedLevelIdKey;

        [Header("Data")]
        [SerializeField]
        private LevelLibrary levelLibrary;

        [Header("UI (optional if Create Default UI is enabled)")]
        [SerializeField]
        private Text titleText;

        [SerializeField]
        private Text hintText;

        [SerializeField]
        private Text nameText;

        [SerializeField]
        private Text locationText;

        [SerializeField]
        private Text descriptionText;

        [SerializeField]
        private Text backstoryText;

        [SerializeField]
        private Text backLabelText;

        [SerializeField]
        private RawImage previewRawImage;

        [SerializeField]
        private Image[] playerSlotIcons;

        [SerializeField]
        private Color collectedIconColor = Color.white;

        [SerializeField]
        private Color uncollectedIconColor = new Color(1f, 1f, 1f, 0.25f);

        [Header("Preview")]
        [SerializeField]
        private Camera previewCamera;

        [SerializeField]
        private Transform previewInstanceParent;

        [Tooltip("World-space layer used only for the 3D preview (set the same on the preview camera).")]
        [SerializeField]
        private int previewCullingLayer = 30;

        [SerializeField]
        private int previewRenderTextureWidth = 512;

        [SerializeField]
        private int previewRenderTextureHeight = 384;

        [Header("Bootstrap")]
        [Tooltip("If UI references are missing, builds a minimal layout under this root at runtime.")]
        [SerializeField]
        private bool createDefaultUiIfNeeded = true;

        [Header("Input Setup")]
        [SerializeField]
        private InputActionAsset inputActions;

        private InputAction _moveAction;
        private InputAction _submitAction;
        private Vector2 _lastMoveInput;
        private int _levelIndex;
        private bool _backFocused;
        private GameObject _previewInstance;
        private RenderTexture _previewRt;
        private readonly List<LevelDefinition> _levels = new List<LevelDefinition>();
        private static Transform _sharedPreviewWorldRoot;

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("[LevelSelectController] InputActionAsset not assigned.");
                return;
            }

            var playerMap = inputActions.FindActionMap("Player");
            _moveAction = playerMap?.FindAction("Move");
            _submitAction = playerMap?.FindAction("PlaceBomb");
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _submitAction?.Enable();

            if (createDefaultUiIfNeeded)
                EnsureDefaultUi();

            RebuildLevelList();
            SyncIndexFromPrefs();
            EnsurePreviewRig();
            EnsureRenderTexture();
            RefreshAll();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _submitAction?.Disable();
            ClearPreviewInstance();
        }

        private void OnDestroy()
        {
            if (previewCamera != null)
                previewCamera.targetTexture = null;
            if (_previewRt != null)
            {
                _previewRt.Release();
                Destroy(_previewRt);
                _previewRt = null;
            }
        }

        public void OnFlowPresented()
        {
            RebuildLevelList();
            SyncIndexFromPrefs();
            RefreshAll();
        }

        public void OnFlowDismissed()
        {
            ClearPreviewInstance();
        }

        private void Update()
        {
            if (SceneFlowManager.I != null && SceneFlowManager.I.IsTransitioning)
                return;

            if (_moveAction == null || _submitAction == null)
                return;

            Vector2 move = _moveAction.ReadValue<Vector2>();

            if (_levels.Count > 0 && !_backFocused)
            {
                if (move.x < -0.5f && _lastMoveInput.x >= -0.5f)
                {
                    _levelIndex = (_levelIndex - 1 + _levels.Count) % _levels.Count;
                    RefreshAll();
                }
                else if (move.x > 0.5f && _lastMoveInput.x <= 0.5f)
                {
                    _levelIndex = (_levelIndex + 1) % _levels.Count;
                    RefreshAll();
                }
            }

            if (move.y < -0.5f && _lastMoveInput.y >= -0.5f)
                SetBackFocused(true);
            else if (move.y > 0.5f && _lastMoveInput.y <= 0.5f)
                SetBackFocused(false);

            if (_submitAction.WasPressedThisFrame()
                && !GlobalPauseMenuController.IsPaused
                && !GlobalPauseMenuController.WasClosedThisFrame)
            {
                HandleSubmit();
            }

            _lastMoveInput = move;
        }

        private void HandleSubmit()
        {
            if (SceneFlowManager.I == null)
                return;

            if (_backFocused)
            {
                SceneFlowManager.I.GoTo(FlowState.Title);
                return;
            }

            if (_levels.Count == 0)
                return;

            var def = _levels[_levelIndex];
            if (def == null || string.IsNullOrEmpty(def.levelId))
                return;

            PlayerPrefs.SetString(SelectedLevelIdPrefsKey, def.levelId);
            PlayerPrefs.Save();
            SceneFlowManager.I.GoTo(FlowState.Menu);
        }

        private void SetBackFocused(bool on)
        {
            _backFocused = on;
            if (backLabelText != null)
                backLabelText.fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
        }

        private void RebuildLevelList()
        {
            _levels.Clear();
            if (levelLibrary == null || levelLibrary.levels == null)
                return;
            for (int i = 0; i < levelLibrary.levels.Length; i++)
            {
                var l = levelLibrary.levels[i];
                if (l != null && !string.IsNullOrEmpty(l.levelId))
                    _levels.Add(l);
            }
        }

        private void SyncIndexFromPrefs()
        {
            if (_levels.Count == 0)
            {
                _levelIndex = 0;
                return;
            }

            string want = PlayerPrefs.GetString(SelectedLevelIdPrefsKey, _levels[0].levelId);
            int found = 0;
            for (int i = 0; i < _levels.Count; i++)
            {
                if (string.Equals(_levels[i].levelId, want, System.StringComparison.Ordinal))
                {
                    found = i;
                    break;
                }
            }

            _levelIndex = Mathf.Clamp(found, 0, _levels.Count - 1);
            _backFocused = false;
        }

        private void RefreshAll()
        {
            if (_levels.Count == 0)
            {
                if (nameText != null)
                    nameText.text = "No levels in Level Library.";
                ClearPreviewInstance();
                return;
            }

            var def = _levels[_levelIndex];
            if (titleText != null)
                titleText.text = "SELECT LEVEL";
            if (hintText != null)
                hintText.text = "← →  choose level   ↓  BACK   PLACE BOMB  confirm";
            if (nameText != null)
                nameText.text = def.displayName;
            if (locationText != null)
                locationText.text = string.IsNullOrEmpty(def.location) ? string.Empty : $"Location: {def.location}";
            if (descriptionText != null)
                descriptionText.text = def.description ?? string.Empty;
            if (backstoryText != null)
                backstoryText.text = def.backstory ?? string.Empty;
            if (backLabelText != null)
                backLabelText.text = _backFocused ? "> BACK" : "  BACK";

            RefreshPlayerIcons(def.levelId);
            RefreshPreview(def);
        }

        private void RefreshPlayerIcons(string levelId)
        {
            if (playerSlotIcons == null)
                return;
            for (int i = 0; i < playerSlotIcons.Length; i++)
            {
                var img = playerSlotIcons[i];
                if (img == null)
                    continue;
                int pid = i + 1;
                bool won = LevelWinPersistence.HasPlayerWonLevel(levelId, pid);
                img.color = won ? collectedIconColor : uncollectedIconColor;
            }
        }

        private void EnsurePreviewRig()
        {
            if (previewInstanceParent == null)
            {
                if (_sharedPreviewWorldRoot == null)
                {
                    var go = new GameObject("LevelSelectPreviewWorld");
                    go.transform.position = new Vector3(1000f, 200f, 1000f);
                    _sharedPreviewWorldRoot = go.transform;
                }

                previewInstanceParent = _sharedPreviewWorldRoot;
            }

            if (previewCamera == null)
            {
                var camGo = new GameObject("LevelSelectPreviewCamera");
                camGo.transform.SetParent(previewInstanceParent, false);
                camGo.transform.localPosition = new Vector3(0f, 8f, -14f);
                camGo.transform.localRotation = Quaternion.Euler(25f, 35f, 0f);
                previewCamera = camGo.AddComponent<Camera>();
                previewCamera.clearFlags = CameraClearFlags.SolidColor;
                previewCamera.backgroundColor = new Color(0.04f, 0.04f, 0.08f, 1f);
                previewCamera.nearClipPlane = 0.1f;
                previewCamera.farClipPlane = 200f;
                previewCamera.orthographic = false;
                previewCamera.fieldOfView = 40f;
            }

            int layer = previewCullingLayer;
            previewCamera.cullingMask = 1 << layer;
        }

        private void EnsureRenderTexture()
        {
            if (previewRawImage == null || previewCamera == null)
                return;

            if (_previewRt != null
                && _previewRt.width == previewRenderTextureWidth
                && _previewRt.height == previewRenderTextureHeight)
            {
                previewCamera.targetTexture = _previewRt;
                previewRawImage.texture = _previewRt;
                return;
            }

            if (_previewRt != null)
            {
                previewCamera.targetTexture = null;
                _previewRt.Release();
                Destroy(_previewRt);
                _previewRt = null;
            }

            _previewRt = new RenderTexture(
                Mathf.Max(16, previewRenderTextureWidth),
                Mathf.Max(16, previewRenderTextureHeight),
                16,
                RenderTextureFormat.ARGB32)
            {
                name = "LevelSelectPreviewRT",
                antiAliasing = 1
            };
            _previewRt.Create();
            previewCamera.targetTexture = _previewRt;
            previewRawImage.texture = _previewRt;
        }

        private void RefreshPreview(LevelDefinition def)
        {
            ClearPreviewInstance();
            if (def == null || def.levelPrefabOrRoot == null || previewCamera == null)
                return;

            _previewInstance = Instantiate(def.levelPrefabOrRoot, previewInstanceParent);
            _previewInstance.transform.localPosition = Vector3.zero;
            _previewInstance.transform.localRotation = Quaternion.identity;
            _previewInstance.transform.localScale = Vector3.one;

            int layer = previewCullingLayer;
            SetLayerRecursively(_previewInstance, layer);
            DisableGameplayScripts(_previewInstance);

            FrameCameraOnInstance(def);
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            var t = go.transform;
            for (int i = 0; i < t.childCount; i++)
                SetLayerRecursively(t.GetChild(i).gameObject, layer);
        }

        private static void DisableGameplayScripts(GameObject root)
        {
            var mbs = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < mbs.Length; i++)
            {
                var mb = mbs[i];
                if (mb == null)
                    continue;
                // Keep tilemaps rendering; disable gameplay/simulation scripts.
                if (mb is Tilemap || mb is TilemapRenderer)
                    continue;
                mb.enabled = false;
            }
        }

        private void FrameCameraOnInstance(LevelDefinition def)
        {
            if (previewCamera == null || _previewInstance == null)
                return;

            var renderers = _previewInstance.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                previewCamera.transform.localPosition = new Vector3(0f, 8f, -14f);
                previewCamera.transform.localRotation = Quaternion.Euler(def.previewCameraEuler.x, def.previewCameraEuler.y, def.previewCameraEuler.z);
                return;
            }

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            Vector3 center = b.center + def.previewLookAtOffset;
            float radius = b.extents.magnitude;
            float dist = Mathf.Max(6f, radius * 2.2f * Mathf.Max(0.1f, def.previewDistanceMultiplier));

            var rot = Quaternion.Euler(def.previewCameraEuler.x, def.previewCameraEuler.y, def.previewCameraEuler.z);
            Vector3 camPos = center + rot * (Vector3.back * dist);
            previewCamera.transform.position = camPos;
            previewCamera.transform.rotation = Quaternion.LookRotation(center - camPos, Vector3.up);
        }

        private void ClearPreviewInstance()
        {
            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
            }
        }

        private void EnsureDefaultUi()
        {
            if (nameText != null && previewRawImage != null)
                return;

            var rt = GetComponent<RectTransform>();
            if (rt == null)
                return;

            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            void MakeText(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, int fontSize, TextAnchor align)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var r = go.GetComponent<RectTransform>();
                r.anchorMin = anchorMin;
                r.anchorMax = anchorMax;
                r.pivot = pivot;
                r.anchoredPosition = anchoredPos;
                r.sizeDelta = size;
                var tx = go.AddComponent<Text>();
                tx.font = font;
                tx.fontSize = fontSize;
                tx.color = Color.white;
                tx.alignment = align;
                tx.supportRichText = false;
                tx.horizontalOverflow = HorizontalWrapMode.Wrap;
                tx.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (titleText == null)
            {
                MakeText("Title", new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, 22, TextAnchor.UpperCenter);
                titleText = transform.Find("Title")?.GetComponent<Text>();
            }

            if (hintText == null)
            {
                MakeText("Hint", new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.88f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, 14, TextAnchor.UpperCenter);
                hintText = transform.Find("Hint")?.GetComponent<Text>();
            }

            if (nameText == null)
            {
                MakeText("Name", new Vector2(0.05f, 0.72f), new Vector2(0.52f, 0.82f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero, 20, TextAnchor.UpperLeft);
                nameText = transform.Find("Name")?.GetComponent<Text>();
            }

            if (locationText == null)
            {
                MakeText("Location", new Vector2(0.05f, 0.66f), new Vector2(0.52f, 0.72f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero, 14, TextAnchor.UpperLeft);
                locationText = transform.Find("Location")?.GetComponent<Text>();
            }

            if (descriptionText == null)
            {
                MakeText("Description", new Vector2(0.05f, 0.42f), new Vector2(0.52f, 0.66f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero, 14, TextAnchor.UpperLeft);
                descriptionText = transform.Find("Description")?.GetComponent<Text>();
            }

            if (backstoryText == null)
            {
                MakeText("Backstory", new Vector2(0.05f, 0.12f), new Vector2(0.52f, 0.42f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero, 13, TextAnchor.UpperLeft);
                backstoryText = transform.Find("Backstory")?.GetComponent<Text>();
            }

            if (backLabelText == null)
            {
                MakeText("BackLabel", new Vector2(0.05f, 0.02f), new Vector2(0.4f, 0.1f), new Vector2(0f, 0f), Vector2.zero, Vector2.zero, 16, TextAnchor.LowerLeft);
                backLabelText = transform.Find("BackLabel")?.GetComponent<Text>();
            }

            if (previewRawImage == null)
            {
                var go = new GameObject("Preview", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var r = go.GetComponent<RectTransform>();
                r.anchorMin = new Vector2(0.54f, 0.35f);
                r.anchorMax = new Vector2(0.95f, 0.88f);
                r.offsetMin = Vector2.zero;
                r.offsetMax = Vector2.zero;
                var img = go.AddComponent<RawImage>();
                img.color = Color.white;
                previewRawImage = img;
            }

            if (playerSlotIcons == null || playerSlotIcons.Length == 0)
            {
                var row = new GameObject("PlayerSlots", typeof(RectTransform));
                row.transform.SetParent(transform, false);
                var rr = row.GetComponent<RectTransform>();
                rr.anchorMin = new Vector2(0.54f, 0.22f);
                rr.anchorMax = new Vector2(0.95f, 0.32f);
                rr.offsetMin = Vector2.zero;
                rr.offsetMax = Vector2.zero;
                var imgs = new Image[5];
                float w = 1f / 5f;
                for (int i = 0; i < 5; i++)
                {
                    var slot = new GameObject($"P{i + 1}", typeof(RectTransform));
                    slot.transform.SetParent(row.transform, false);
                    var srt = slot.GetComponent<RectTransform>();
                    srt.anchorMin = new Vector2(i * w, 0f);
                    srt.anchorMax = new Vector2((i + 1) * w, 1f);
                    srt.offsetMin = new Vector2(4f, 4f);
                    srt.offsetMax = new Vector2(-4f, -4f);
                    var image = slot.AddComponent<Image>();
                    image.color = uncollectedIconColor;
                    imgs[i] = image;
                }

                playerSlotIcons = imgs;
            }
        }

        // ---- Editor / test hooks (used by EditMode tests) ----
        public void Editor_SetLevelLibrary(LevelLibrary lib)
        {
            levelLibrary = lib;
            RebuildLevelList();
        }

        public void Editor_SetLevelIndex(int index)
        {
            _levelIndex = Mathf.Clamp(index, 0, Mathf.Max(0, _levels.Count - 1));
            _backFocused = false;
        }

        public int Editor_GetLevelIndex() => _levelIndex;

        /// <summary>Negative = previous, positive = next (wraps).</summary>
        public void Editor_StepLevelHorizontal(int delta)
        {
            if (_levels.Count == 0)
                return;
            if (delta < 0)
                _levelIndex = (_levelIndex - 1 + _levels.Count) % _levels.Count;
            else if (delta > 0)
                _levelIndex = (_levelIndex + 1) % _levels.Count;
            RefreshAll();
        }
    }
}
