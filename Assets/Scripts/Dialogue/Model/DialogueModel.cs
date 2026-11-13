using System.Collections.Generic;

// MODEL - which conversation is running and which lines are left.
// Plain C#, no UI: the presenter asks it for the next line and tells the view to show it.
public class DialogueModel
{
    private readonly Queue<string> sentences = new Queue<string>();

    public Dialogue Current { get; private set; }
    public bool IsActive => Current != null;

    public void Begin(Dialogue dialogue)
    {
        Current = dialogue;
        sentences.Clear();
        foreach (string sentence in dialogue.sentences)
            sentences.Enqueue(sentence);
    }

    public bool TryGetNextSentence(out string sentence)
    {
        if (sentences.Count == 0)
        {
            sentence = null;
            return false;
        }

        sentence = sentences.Dequeue();
        return true;
    }

    public void End()
    {
        Current = null;
        sentences.Clear();
    }
}