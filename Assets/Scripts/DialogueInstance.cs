using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//this script was made with help from this tutorial: https://www.youtube.com/watch?v=_nRzoTzeyxU
public class DialogueInstance : MonoBehaviour
{
   // public Animator characterAnimator;
    //public string talkAnimationName;
    //public AudioClip talkAudio;

    public Dialogue dialogue;
  

    public void TriggerDialogue()
    {
        FindObjectOfType<DialogueManager>().StartDialogue(dialogue);
    }

}
