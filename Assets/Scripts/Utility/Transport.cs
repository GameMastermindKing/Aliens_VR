using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Transport : MonoBehaviour
{
    public Transform moveParent;
    public Transform playerParent;
    public Transform player;
    public Transform playerHead;
    public Transform distanceCenter;
    public float distanceToDrop = 3.0f;

    public bool inside = false;

    public void OnCollisionEnter(Collision hit)
    {
        if (hit.gameObject.GetComponentInParent<GorillaLocomotion.Player>(true) != null)
        {
            inside = true;
            //FindObjectOfType<GorillaLocomotion.Player>().ignoreHandCollision = true;
            hit.gameObject.GetComponentInParent<GorillaLocomotion.Player>(true).transform.SetParent(moveParent);
        }
    }

    public void OnCollisionExit(Collision hit)
    {
        if (hit.gameObject.GetComponentInParent<GorillaLocomotion.Player>(true) != null)
        {
            inside = false;
            //FindObjectOfType<GorillaLocomotion.Player>().ignoreHandCollision = true;
            hit.gameObject.GetComponentInParent<GorillaLocomotion.Player>(true).transform.SetParent(playerParent);
        }
    }

    public void Update()
    {
        if (inside && player.parent != playerParent && Vector3.Distance(distanceCenter.position, playerHead.position) > distanceToDrop)
        {
            //FindObjectOfType<GorillaLocomotion.Player>().ignoreHandCollision = false;
            inside = false;
            player.parent = playerParent;
        }
    }
}
