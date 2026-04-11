using UnityEngine;
using UnityEngine.UI;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// Displays the online FPS interlude countdown. Assign an optional <see cref="UnityEngine.UI.Text"/>; if unset, uses <see cref="OnGUI"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class FpsInterludeCountdownHud : MonoBehaviour
    {
        [Tooltip("Defaults to OnlineFpsInterludeController.Instance if null.")]
        [SerializeField]
        private OnlineFpsInterludeController interludeController;

        [SerializeField]
        private Text countdownText;

        [Header("OnGUI fallback (when countdownText is null)")]
        [SerializeField]
        private int onGuiFontSize = 96;

        [SerializeField]
        private Color onGuiColor = Color.white;

        private GUIStyle _onGuiStyle;

        private void Update()
        {
            var ctrl = interludeController != null ? interludeController : OnlineFpsInterludeController.Instance;
            if (ctrl == null)
            {
                SetVisible(false, string.Empty);
                return;
            }

            if (!ctrl.IsCountdownDisplayActive)
            {
                SetVisible(false, string.Empty);
                return;
            }

            string s = ctrl.CountdownDisplayValue.ToString();
            SetVisible(true, s);
        }

        private void SetVisible(bool visible, string text)
        {
            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(visible);
                if (visible)
                    countdownText.text = text;
            }
        }

        private void OnGUI()
        {
            if (countdownText != null)
                return;

            var ctrl = interludeController != null ? interludeController : OnlineFpsInterludeController.Instance;
            if (ctrl == null || !ctrl.IsCountdownDisplayActive)
                return;

            if (_onGuiStyle == null)
            {
                _onGuiStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = onGuiFontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = onGuiColor },
                };
            }

            string s = ctrl.CountdownDisplayValue.ToString();
            const float w = 320f;
            const float h = 160f;
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Label(r, s, _onGuiStyle);
        }
    }
}
