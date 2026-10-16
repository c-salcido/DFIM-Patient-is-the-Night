using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//this script was made with help from this tutorial: https://www.youtube.com/watch?v=_nRzoTzeyxU
[System.Serializable]
public class Dialogue 
{
    public string name;
    public float talkingSpeed;

    [TextArea(3,10)]
    public string[] sentences;

    
}
