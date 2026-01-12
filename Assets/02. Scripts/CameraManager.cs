using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour
{
    public CinemachineVirtualCamera defaultCam;
    public CinemachineVirtualCamera targetCam;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            targetCam.Priority = 20;
            defaultCam.Priority = 10;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            targetCam.Priority = 0;
            defaultCam.Priority = 10;
        }
    }


}
