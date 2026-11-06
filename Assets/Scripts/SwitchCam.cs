using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

//this was made with help from this tutorial https://www.youtube.com/watch?v=wmTCWMcjIzo&t=1s
public class SwitchCam : MonoBehaviour
{
    //this goes on the cameras trigger


    public CinemachineVirtualCamera thisCamera;


    private void OnTriggerEnter2D (Collider2D other)
    {
        if (other.CompareTag("Player"))
        {

            
            CameraManager.SwitchCamera(thisCamera);
             thisCamera.Follow = other.transform;
            
            
        }
    }
}
