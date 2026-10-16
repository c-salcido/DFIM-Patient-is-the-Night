using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


//this script was made with help from this tutorial: https://www.youtube.com/watch?v=_nRzoTzeyxU
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    private Queue<string> sentences;

    //ui components
    public GameObject dialogueBox;
    public TextMeshProUGUI name;
    public TextMeshProUGUI dialogueText;


    // Start is called before the first frame update
    void Start()
    {
        dialogueBox.SetActive(false);
        if (Instance == null) Instance = this; //allows us to access this from anywhere
        sentences = new Queue<string>();
    }


    public void StartDialogue(Dialogue dialogue)
    {

        dialogueBox.SetActive(true);

        Debug.Log("starting converstation with" + dialogue.name);
        name.text = dialogue.name;
        sentences.Clear();


        foreach (string sentence in dialogue.sentences)
        {
            sentences.Enqueue(sentence);
        }
        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;

        }
        string sentence = sentences.Dequeue();
        dialogueText.text = sentence;
        Debug.Log(sentence);

    }

    void EndDialogue()
    {
        Debug.Log("End of Conversation");

        dialogueBox.SetActive(false);
    }


    
}
