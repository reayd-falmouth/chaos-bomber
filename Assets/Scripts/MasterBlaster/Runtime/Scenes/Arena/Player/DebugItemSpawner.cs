#if UNITY_EDITOR
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Player
{
    public class DebugItemSpawner : MonoBehaviour
    {
        [Header("Item Prefabs")]
        public GameObject extraBombPrefab;
        public GameObject blastRadiusPrefab;
        public GameObject supermanPrefab;
        public GameObject protectionPrefab;
        public GameObject ghostPrefab;
        public GameObject speedIncreasePrefab;
        public GameObject deathPrefab;
        public GameObject randomPrefab;
        public GameObject timeBombPrefab;
        public GameObject stopPrefab;
        public GameObject coinPrefab;
        public GameObject remoteBombPrefab;

        [Header("Spawn Offset")]
        public Vector3 offset = new Vector3(1, 0, 0); // spawn 1 unit to the right

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                Spawn(extraBombPrefab);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                Spawn(blastRadiusPrefab);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                Spawn(supermanPrefab);
            if (Input.GetKeyDown(KeyCode.Alpha4))
                Spawn(protectionPrefab);
            if (Input.GetKeyDown(KeyCode.Alpha5))
                Spawn(ghostPrefab);
            if (Input.GetKeyDown(KeyCode.Alpha6))
                Spawn(speedIncreasePrefab);
            if (Input.GetKeyDown(KeyCode.Alpha7))
                Spawn(deathPrefab);
            if (Input.GetKeyDown(KeyCode.Alpha8))
                Spawn(randomPrefab);
            if (Input.GetKeyDown(KeyCode.Alpha9))
                Spawn(timeBombPrefab);
            if (Input.GetKeyDown(KeyCode.Alpha0))
                Spawn(stopPrefab);
            if (Input.GetKeyDown(KeyCode.Minus))
                Spawn(coinPrefab);
            if (Input.GetKeyDown(KeyCode.Equals))
                Spawn(remoteBombPrefab);
        }

        private void Spawn(GameObject prefab)
        {
            if (prefab == null)
            {
                UnityEngine.Debug.LogError("[DebugItemSpawner] Missing prefab assignment!");
                return;
            }

            Vector3 spawnPos = transform.position + offset;
            Instantiate(prefab, spawnPos, Quaternion.identity);
            UnityEngine.Debug.Log($"[DebugItemSpawner] Spawned {prefab.name} at {spawnPos}");
        }
    }
}
#endif
