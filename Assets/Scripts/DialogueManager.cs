using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//this script was made with help from this tutorial: https://www.youtube.com/watch?v=_nRzoTzeyxU
public class DialogueManager : MonoBehaviour
{

    private Queue<string> sentences;

    // Start is called before the first frame update
    void Start()
    {
        sentences = new Queue<string>();
    }


    public void StartDialogue(Dialogue dialogue)
    {
        Debug.Log("starting converstation with" + dialogue.name);
    }

    
}
