using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mirror : MonoBehaviour
{
    public Transform mirrorTransform;
    public Transform playerTransform;
    public Transform cameraTransform;
    public float minY;
    public float maxY;

    public bool flipForwardReflection = false;
    public bool trackVertical = false;

    public void Update()
    {
        if (trackVertical)
        {
            cameraTransform.position = new Vector3(cameraTransform.position.x, Mathf.Clamp(playerTransform.position.y, minY, maxY), cameraTransform.position.z);
        }
        
        Vector3 lookVec = cameraTransform.position - playerTransform.position;
        if (!trackVertical)
        {
            lookVec.y = 0.0f;
        }
        cameraTransform.LookAt(cameraTransform.position + Vector3.Reflect(lookVec, flipForwardReflection ? -mirrorTransform.forward : mirrorTransform.forward));
    }
}
