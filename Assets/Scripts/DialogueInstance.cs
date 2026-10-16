using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//this script was made with help from this tutorial: https://www.youtube.com/watch?v=_nRzoTzeyxU
public class DialogueInstance : MonoBehaviour
{
    public Dialogue dialogue;


    public void TriggerDialogue()
    {
        FindObjectOfType<DialogueManager>().StartDialogue(dialogue);
    }

}
