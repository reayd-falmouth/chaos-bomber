using System.Collections;
using System.Collections.Generic;
using HybridGame.MasterBlaster.Scripts.Core;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.WheelOFortune
{
    public class WheelController : MonoBehaviour
    {
        [Header("UI References")]
        public Transform wheelPanel; // Vertical Layout
        public GameObject rowPrefab; // Prefab with Avatar + Pointer + Avatar

        [Header("Avatars")]
        public Sprite[] avatarSprites; // 5 sprites, one per player

        [Header("Wheel Settings")]
        [Header("Wheel Settings")]
        [Tooltip("Controls the spin frequency over time. X=time [0..1], Y=delay in seconds.")]
        public AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 0.01f, 1, 0.5f);

        [Tooltip("Minimum total spin duration (seconds)")]
        public float minSpinDuration = 2f;

        [Tooltip("Maximum total spin duration (seconds)")]
        public float maxSpinDuration = 5f;

        private float spinDuration; // chosen at runtime

        [Tooltip("Extra wait time before advancing to next scene")]
        [Range(0.5f, 5f)]
        public float postSpinDelay = 1.5f;

        [Header("Feedbacks")]
        [SerializeField] private MMF_Player tickFeedbacks;
        [SerializeField] private MMF_Player rewardFeedbacks;

        private const string PointerInactiveText = " ";
        private const string PointerActiveText = ">";

        private Text[] rowPointers;
        private int playerCount;

        private Coroutine _spinCoroutine;

        private void OnEnable()
        {
            if (wheelPanel == null)
            {
                UnityEngine.Debug.LogWarning("[WheelController] wheelPanel is not assigned; skipping setup.");
                return;
            }

            // How many rows exist under the panel (e.g. 5)
            int maxRows = wheelPanel.childCount;
            int requested = Mathf.Clamp(PlayerPrefs.GetInt("Players", 2), 0, maxRows);
            
            // Ensure the list is using <Text>
            var pointers = new List<Text>(); 

            for (int i = 0; i < maxRows; i++)
            {
                Transform row = wheelPanel.GetChild(i);
                bool wanted = i < requested;

                if (!wanted)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }

                // 1. Find the Pointer Transform first
                Transform pointerTf = row.Find("Pointer");

                if (pointerTf == null)
                {
                    UnityEngine.Debug.LogWarning($"[WheelController] Row '{row.name}' has no child 'Pointer'; deactivating row.");
                    row.gameObject.SetActive(false);
                    continue;
                }

                row.gameObject.SetActive(true);

                // 2. Pointer Text stays enabled; inactive rows use a space so layout width is stable.
                Text pText = pointerTf.GetComponent<Text>();
                if (pText != null)
                {
                    pointers.Add(pText);
                    pText.enabled = true;
                }

                // Keep the GameObject active so the Layout Group doesn't shift
                pointerTf.gameObject.SetActive(true);

                // WheelRow prefab historically used typo "Avater"; support both names.
                Transform avatarTf = FindChildByNames(row, "Avatar", "Avater");
                if (avatarTf != null)
                {
                    var avatar = avatarTf.GetComponent<Image>();
                    if (avatar != null && avatarSprites != null && avatarSprites.Length > i)
                        avatar.sprite = avatarSprites[i];
                }
            }

            playerCount = pointers.Count;
            rowPointers = pointers.ToArray();

            if (playerCount <= 0)
                return;

            ApplyPointerTexts(rowPointers, 0, PointerInactiveText, PointerActiveText);

            // Start spin
            _spinCoroutine = StartCoroutine(SpinAndStop());
        }

        private void OnDisable()
        {
            if (_spinCoroutine != null)
            {
                StopCoroutine(_spinCoroutine);
                _spinCoroutine = null;
            }
        }

        private IEnumerator SpinAndStop()
        {

            spinDuration = Random.Range(minSpinDuration, maxSpinDuration);

            int index = 0;

            // Pick random stopping index
            int stopIndex = Random.Range(0, playerCount);

            // Spin loop
            float elapsed = 0f;

            while (elapsed < spinDuration)
            {
                ApplyPointerTexts(rowPointers, index, PointerInactiveText, PointerActiveText);

                tickFeedbacks?.PlayFeedbacks();
                
                // Evaluate delay from curve
                float t = elapsed / spinDuration; // 0..1
                float delay = spinCurve.Evaluate(t);

                yield return new WaitForSeconds(delay);

                index = (index + 1) % playerCount;
                elapsed += delay;
            }

            ApplyPointerTexts(rowPointers, stopIndex, PointerInactiveText, PointerActiveText);

            rewardFeedbacks?.PlayFeedbacks();

            // Reward selected player with a coin (session-only, in SessionManager)
            int winningPlayer = stopIndex + 1;
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.AddCoins(winningPlayer, 1);
                int total = SessionManager.Instance.GetCoins(winningPlayer);
                UnityEngine.Debug.Log($"Player {winningPlayer} wins a coin! Total: {total}");
            }

            // Wait before moving on
            yield return new WaitForSeconds(postSpinDelay);

            SceneFlowManager.I.SignalScreenDone();
        }

        /// <summary>
        /// Sets each pointer <see cref="Text.text"/> to <paramref name="inactiveText"/> except
        /// <paramref name="activeIndex"/> which gets <paramref name="activeText"/> (layout-stable highlight).
        /// </summary>
        public static void ApplyPointerTexts(Text[] pointers, int activeIndex, string inactiveText, string activeText)
        {
            if (pointers == null || pointers.Length == 0)
                return;

            int idx = Mathf.Clamp(activeIndex, 0, pointers.Length - 1);
            for (int i = 0; i < pointers.Length; i++)
            {
                if (pointers[i] == null)
                    continue;
                pointers[i].text = i == idx ? activeText : inactiveText;
            }
        }

        /// <summary>Returns the first direct child whose name matches one of the candidates (in order).</summary>
        private static Transform FindChildByNames(Transform parent, params string[] names)
        {
            if (parent == null || names == null) return null;
            foreach (var n in names)
            {
                if (string.IsNullOrEmpty(n)) continue;
                var t = parent.Find(n);
                if (t != null) return t;
            }

            return null;
        }
    }
}
