using UnityEngine;

// Holds one conversation and starts it when TriggerDialogue is called (e.g. from a TriggerZone event).
// originally made with help from this tutorial: https://www.youtube.com/watch?v=_nRzoTzeyxU
public class DialogueInstance : MonoBehaviour
{
    public Dialogue dialogue;

    public void TriggerDialogue()
    {
        DialoguePresenter presenter = DialoguePresenter.Instance;
        if (presenter == null)
            presenter = FindObjectOfType<DialoguePresenter>();

        if (presenter != null)
            presenter.StartDialogue(dialogue);
        else
            Debug.LogWarning("No DialoguePresenter in the scene.", this);
    }
}