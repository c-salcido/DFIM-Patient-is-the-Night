using System.Collections;
using TMPro;
using UnityEngine;

// VIEW - the dialogue box on screen. It only does what the presenter tells it
// (show, hide, type this line, play this sound) and has no idea what the conversation is.
public class DialogueView : MonoBehaviour
{
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private Animator boxAnimator;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private AudioSource voiceSource;

    private static readonly int IsOpenParam = Animator.StringToHash("isOpen");
    private Coroutine typingRoutine;

    private void Reset() => voiceSource = GetComponent<AudioSource>();

    public void Show(string speakerName)
    {
        dialogueBox.SetActive(true);
        boxAnimator.SetBool(IsOpenParam, true);
        nameText.text = speakerName;
    }

    public void Hide()
    {
        StopTyping();
        boxAnimator.SetBool(IsOpenParam, false);
        dialogueBox.SetActive(false);
    }

    // Used at startup, before the box's animator is running.
    public void HideImmediately() => dialogueBox.SetActive(false);

    public void PlayVoice(AudioClip clip)
    {
        if (clip != null && voiceSource != null)
            voiceSource.PlayOneShot(clip);
    }

    public void TypeSentence(string sentence)
    {
        StopTyping();
        typingRoutine = StartCoroutine(TypeRoutine(sentence));
    }

    // Reveals one letter per frame, same as before.
    private IEnumerator TypeRoutine(string sentence)
    {
        bodyText.text = "";
        foreach (char letter in sentence)
        {
            bodyText.text += letter;
            yield return null;
        }
        typingRoutine = null;
    }

    private void StopTyping()
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);
        typingRoutine = null;
    }
}