using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;


//this script was made using this Camera Switching tutorial https://www.youtube.com/watch?v=wmTCWMcjIzo

//this script goes on individual virtual cameras

public class CameraRegister : MonoBehaviour
{
    private void OnEnable()
    {
        CameraManager.Register(GetComponent<CinemachineVirtualCamera>());

    }

    void Start()
    {
        FollowPlayer();
        


    }

    private void FollowPlayer()
    {
        var cam = GetComponent<CinemachineVirtualCamera>();
        cam.Follow = FindObjectOfType<CharacterController2D>().transform;

    }
    private void OnDisable()
    {
        CameraManager.Unregister(GetComponent<CinemachineVirtualCamera>());
    }


}
