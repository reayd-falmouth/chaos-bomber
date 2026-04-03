using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Levels
{
    [CreateAssetMenu(
        menuName = "HybridGame/MasterBlaster/Levels/Level Definition",
        fileName = "LevelDefinition"
    )]
    public class LevelDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable key used for persistence and selection (e.g. 'arena_01').")]
        public string levelId;

        [Tooltip("Display name shown in the Level Selector UI.")]
        public string displayName;

        [Tooltip("Where this level takes place (e.g. 'Neon Docks').")]
        public string location;

        [Header("Lore")]
        [TextArea(2, 4)]
        public string description;

        [TextArea(3, 8)]
        public string backstory;

        [Header("Gameplay content")]
        [Tooltip("Prefab or map-root GameObject that represents the level layout (ProBuilder-built is fine).")]
        public GameObject levelPrefabOrRoot;

        [Header("Preview (optional)")]
        [Tooltip("Optional static thumbnail used when realtime preview is unavailable.")]
        public Sprite fallbackThumbnail;

        [Tooltip("Optional manual look-at offset added to auto-computed bounds center when framing the preview camera.")]
        public Vector3 previewLookAtOffset = Vector3.zero;

        [Tooltip("Optional multiplier applied to the auto-computed camera distance when framing the preview camera.")]
        [Min(0.1f)]
        public float previewDistanceMultiplier = 1f;

        [Tooltip("Optional yaw/pitch/roll for the preview camera when framing this level.")]
        public Vector3 previewCameraEuler = new Vector3(25f, 35f, 0f);
    }
}

