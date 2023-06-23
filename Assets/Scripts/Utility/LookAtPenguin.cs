using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtPenguin : MonoBehaviour
{
    public Transform lookTransform;

    Transform player;

    public void Start()
    {
    }

    public void Update()
    {
        if (player == null)
        {
            player = FindObjectOfType<GorillaLocomotion.Player>().transform;
        }
        lookTransform.LookAt(new Vector3(player.position.x, lookTransform.position.y, player.position.z));
        lookTransform.Rotate(Vector3.up * 180.0f, Space.World);
    }
}
