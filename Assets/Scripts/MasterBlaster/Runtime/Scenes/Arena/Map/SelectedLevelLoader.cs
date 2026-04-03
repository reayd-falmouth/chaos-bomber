using HybridGame.MasterBlaster.Scripts.Levels;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    /// <summary>
    /// Instantiates the level prefab chosen in <see cref="LevelSelectLocal.LevelSelectController"/> before
    /// <see cref="MapSelector"/> applies normal/alt roots. Disables both map roots when a custom level is active.
    /// </summary>
    [DefaultExecutionOrder(-550)]
    public class SelectedLevelLoader : MonoBehaviour
    {
        /// <summary>When true, <see cref="MapSelector"/> should not enable normal/alt roots.</summary>
        public static bool SuppressDefaultMapSelector { get; private set; }

        [SerializeField]
        private LevelLibrary levelLibrary;

        [SerializeField]
        private MapSelector mapSelector;

        private GameObject _spawnedInstance;

        private void Awake()
        {
            if (mapSelector == null)
                mapSelector = GetComponent<MapSelector>();
            ApplyFromPrefs();
        }

        private void OnDestroy()
        {
            SuppressDefaultMapSelector = false;
        }

        /// <summary>Call when re-entering the arena in single-scene mode (Awake does not run again).</summary>
        public void ReapplyFromPrefs()
        {
            ApplyFromPrefs();
        }

        private void ApplyFromPrefs()
        {
            DestroySpawned();
            SuppressDefaultMapSelector = false;

            if (levelLibrary == null)
                return;

            string id = PlayerPrefs.GetString(LevelSelectionPrefs.SelectedLevelIdKey, string.Empty);
            if (string.IsNullOrEmpty(id))
                return;

            if (!levelLibrary.TryGetById(id, out var def) || def == null || def.levelPrefabOrRoot == null)
                return;

            _spawnedInstance = Instantiate(def.levelPrefabOrRoot, transform);
            _spawnedInstance.name = $"Level_{id}";
            _spawnedInstance.SetActive(true);

            SuppressDefaultMapSelector = true;
            mapSelector?.DisableBothRoots();
        }

        private void DestroySpawned()
        {
            if (_spawnedInstance != null)
            {
                Destroy(_spawnedInstance);
                _spawnedInstance = null;
            }
        }
    }
}
