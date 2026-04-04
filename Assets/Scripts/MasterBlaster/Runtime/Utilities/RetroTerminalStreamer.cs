using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using MoreMountains.Feedbacks;

public class RetroTerminalStreamer : MonoBehaviour
{
    [Header("UI Component")]
    public Text displayField; 

    [Header("Settings")]
    public float streamSpeed = 0.02f; 
    public AudioSource typingSound;   

    [Header("Feel Integration")]
    public bool playFeedbackOnEveryChar = true;

    private string fullText;
    private MMF_Player mmfPlayer;

    void Awake()
    {
        mmfPlayer = GetComponent<MMF_Player>();
    }

    public void StartStreaming(string input)
    {
        fullText = input.ToUpper(); 
        StopAllCoroutines();
        StartCoroutine(TypeText());
    }

    IEnumerator TypeText()
    {
        displayField.text = "";
        
        foreach (char c in fullText)
        {
            displayField.text += c;

            // Trigger Feedback here so it happens WITH the text
            if (mmfPlayer != null)
            {
                // PlayFeedbacks() restarts the sequence
                mmfPlayer.PlayFeedbacks(); 
            }

            if (typingSound != null && displayField.text.Length % 2 == 0)
            {
                typingSound.PlayOneShot(typingSound.clip);
            }

            yield return new WaitForSeconds(streamSpeed);
        }
    }
}