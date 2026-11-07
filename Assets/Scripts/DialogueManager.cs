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
    public Animator animator;
    public TextMeshProUGUI name;
    public TextMeshProUGUI dialogueText;

    public Animator characterAnimator;
    


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
        animator.SetBool("isOpen", true);
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
        StopAllCoroutines();
        StartCoroutine(TypeSentence(sentence));

    }

    IEnumerator TypeSentence (string sentence)
    {
        dialogueText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return null;
        }
    }

    void EndDialogue()
    {
        animator.SetBool("isOpen", false);
        dialogueBox.SetActive(false);
    }


    
}
