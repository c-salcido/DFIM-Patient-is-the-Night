using UnityEngine;

// PRESENTER - the go-between for the dialogue model and the dialogue box (view).
// Triggers (DialogueInstance) and the "next" button talk to this; it updates the model,
// then tells the view exactly what to show. Model and view never talk to each other.
// originally made with help from this tutorial: https://www.youtube.com/watch?v=_nRzoTzeyxU
public class DialoguePresenter : MonoBehaviour
{
    public static DialoguePresenter Instance { get; private set; }

    public static event System.Action DialogueStarted;
    public static event System.Action DialogueEnded;



    [SerializeField] private DialogueView view;

    private readonly DialogueModel model = new DialogueModel();

    public bool IsOpen => model.IsActive;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        if (view == null)
            view = GetComponent<DialogueView>();
    }

    private void Start() => view.HideImmediately();

    private void OnDestroy()
    {
        if(model.IsActive)
        {
            model.End();
            DialogueEnded?.Invoke();
        }
        if(Instance == this)
            Instance = null;

    }

    public void StartDialogue(Dialogue dialogue)
    {
        bool wasOpen = model.IsActive;

        model.Begin(dialogue);
        view.Show(dialogue.name);
        view.PlayVoice(dialogue.audio);

        if(!wasOpen)
            DialogueStarted?.Invoke();

        DisplayNextSentence();

    }

    // Hooked up to the button in the dialogue box (method name kept so the button still works).
    public void DisplayNextSentence()
    {
        if (!model.IsActive)
            return;

        if (model.TryGetNextSentence(out string sentence))
            view.TypeSentence(sentence);
        else
            EndDialogue();
    }

    public void EndDialogue()
    {
         if (!model.IsActive)
            return;

        model.End();
        view.Hide();
        DialogueEnded?.Invoke();
    }
}