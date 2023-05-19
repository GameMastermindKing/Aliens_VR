using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class HealingRoom : MonoBehaviour
{
    PhotonRoyalePlayer royalePlayer;
    public Transform healCenter;
    public Mesh cylinderMesh;
    public Vector3 healRadius;

    public int healPerTick = 5;
    public float timePerTick = 1f;

    float lastTick = 0.0f;
    Vector3 neutralCenter;

    public void Update()
    {
        if (royalePlayer != null && PhotonRoyaleLobby.instance.activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber) && royalePlayer.alive)
        {
            neutralCenter = healCenter.position;
            neutralCenter.y = royalePlayer.player.bodyCollider.transform.position.y;
            if (Vector3.Distance(neutralCenter, royalePlayer.player.bodyCollider.transform.position) < healRadius.x / 2.0f && 
                Mathf.Abs(royalePlayer.player.bodyCollider.transform.position.y - healCenter.position.y) < healRadius.y)
            {
                if (Time.time - lastTick >= timePerTick)
                {
                    lastTick = Time.time;
                    royalePlayer.photonView.RPC("Heal", Photon.Pun.RpcTarget.All, healPerTick);
                }
            }
        }
        else if (royalePlayer == null)
        {
            PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].photonView.IsMine)
                {
                    royalePlayer = players[i];
                    break;
                }
            }
        }
    }

    public void OnDrawGizmos()
    {
        if (healCenter != null && cylinderMesh != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireMesh(cylinderMesh, healCenter.position, healCenter.rotation, healRadius);
        }
    }
}
