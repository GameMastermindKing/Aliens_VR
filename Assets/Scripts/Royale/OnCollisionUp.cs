using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnCollisionUp : MonoBehaviour
{
    public Vector3 resetSpot;

    public void OnCollisionEnter(Collision hit)
    {
        PhotonRoyalePlayer player = hit.collider.gameObject.GetComponentInParent<PhotonRoyalePlayer>();
        if (player != null && player.photonView.IsMine && player.alive)
        {
            GorillaLocomotion.Player.Instance.transform.position = resetSpot;
            GorillaLocomotion.Player.Instance.InitializeValues();
        }
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(resetSpot, 2.0f);
    }
}
