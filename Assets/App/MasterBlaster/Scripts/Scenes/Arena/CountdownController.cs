using System.Collections;
using HybridGame.MasterBlaster.Scripts.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena
{
    public class CountdownController : MonoBehaviour
    {
        public Text  countdownText;
        public float interval = 1f; // seconds between counts (visual only)

        private Coroutine _countdownCoroutine;

        private void OnEnable()
        {
            AudioController.OnOneShotComplete += OnMusicFinished;

            // Defensive: ensure we don't run multiple countdown coroutines if this object
            // is enabled repeatedly during single-scene root toggling.
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }

            float clipLength = AudioController.I != null ? AudioController.I.ActiveClipLength : 0f;
            if (clipLength > 0f)
                interval = clipLength / 3f;

            // Restart visual countdown each time this state root is enabled.
            _countdownCoroutine = StartCoroutine(RunVisualCountdown());
        }

        private void OnDisable()
        {
            AudioController.OnOneShotComplete -= OnMusicFinished;
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }
        }

        // Drives the visual 3-2-1 display independently of the music duration
        IEnumerator RunVisualCountdown()
        {
            int count = 3;
            while (count > 0)
            {
                // UI may be destroyed when leaving this state in single-scene mode.
                if (countdownText == null)
                    yield break;

                countdownText.text = count.ToString();
                yield return new WaitForSeconds(interval);
                count--;
            }
            if (countdownText != null)
                countdownText.text = "";
        }

        // Scene ends when the music clip finishes — length of scene matches length of clip
        private void OnMusicFinished()
        {
            SceneFlowManager.I.SignalScreenDone();
        }
    }
}
