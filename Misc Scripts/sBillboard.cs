using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Works with Unity QUADS!
 * Which means the Z is flipped.
 * 
 */
public class sBillboard : MonoBehaviour
{
    private Camera mainCamera;
    private void Start()
    {
        mainCamera = Camera.main;
    }
    void Update()
    {
        if (mainCamera != null)
        {
            // Rotate the object so that the forward vector points at the camera
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
    }
}
