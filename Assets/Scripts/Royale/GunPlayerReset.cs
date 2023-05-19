using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunPlayerReset : MonoBehaviour
{
    public Vector3 resetPos;

    public void OnTriggerEnter(Collider hit)
    {
        if (hit.attachedRigidbody != null)
        {
            if (hit.attachedRigidbody.gameObject.GetComponent<PhotonGun>() != null)
            {
                hit.attachedRigidbody.gameObject.GetComponent<PhotonGun>().ResetToSafety();
            }
            else if (hit.attachedRigidbody.gameObject.GetComponent<PhotonBullet>() != null)
            {
                hit.attachedRigidbody.gameObject.GetComponent<PhotonBullet>().Die();
            }
        }
        else if ((hit.tag == "Player" || hit.tag == "MainCamera" || hit.gameObject.layer == 7))
        {
            GorillaLocomotion.Player player = FindObjectOfType<GorillaLocomotion.Player>();
            player.transform.position = resetPos;
            player.InitializeValues();
        }
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(resetPos, 20.0f);
    }
}
