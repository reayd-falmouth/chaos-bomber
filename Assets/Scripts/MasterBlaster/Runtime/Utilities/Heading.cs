using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways] // Allows changes to update in the Scene view without pressing Play
[RequireComponent(typeof(Text))]
public class Heading : MonoBehaviour
{
    [Header("Text Content")]
    // [TextArea(minLines, maxLines)] creates a larger box in the Inspector
    [TextArea(3, 10)] 
    public string headingText = "NEW\nHEADING";

    [Header("Styling")]
    public Color headingColor = new Color(0f, 0.305f, 1f);
    public int fontSize = 26;
    public FontStyle fontStyle = FontStyle.Bold;
    public TextAnchor alignment = TextAnchor.MiddleCenter;

    private Text _textComponent;

    void Awake()
    {
        ApplySettings();
    }

    // This updates the UI in real-time as you change values in the Inspector
    void OnValidate()
    {
        ApplySettings();
    }

    public void ApplySettings()
    {
        if (_textComponent == null)
            _textComponent = GetComponent<Text>();

        _textComponent.text = headingText;
        _textComponent.color = headingColor;
        _textComponent.fontSize = fontSize;
        _textComponent.fontStyle = fontStyle;
        _textComponent.alignment = alignment;
        
        // Ensure the text doesn't hide if the box is too small
        _textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        _textComponent.verticalOverflow = VerticalWrapMode.Overflow;
    }
}