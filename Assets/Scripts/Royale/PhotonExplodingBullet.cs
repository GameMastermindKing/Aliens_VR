using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.VR.Player;

public class PhotonExplodingBullet : PhotonBullet
{
    public ScriptableGrenadeConfiguration grenadeConfig;
    public ParticleSystem explodeSystem;

    public static List<PhotonExplodingBullet> bullets = new List<PhotonExplodingBullet>();

    public bool sentAlready = false;
    public bool deadAlready = false;

    public int id = 0;

    public override void OnCollisionEnter(Collision hit)
    {
        if (hitSomething)
        {
            return;
        }

        hitSomething = true;
        if (photonView.IsMine)
        {
            DieOnImpact();
        }
        else
        {
            photonView.RPC("ReportedCollision", photonView.Controller);
        }
    }

    [PunRPC]
    public void ReportedCollision()
    {
        if (photonView.IsMine && !sentAlready)
        {
            sentAlready = true;
            Explode();
            photonView.RPC("ExplodeEffect", RpcTarget.All);
        }
    }

    [PunRPC]
    public void SetPlayerID(int id)
    {
        playerID = id;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        deadAlready = false;
        sentAlready = false;
    }

    public void DieOnImpact()
    {
        if (photonView.IsMine && !sentAlready)
        {
            sentAlready = true;
            Explode();
            photonView.RPC("ExplodeEffect", RpcTarget.All);
        }
    }

    [PunRPC]
    public void ExplodeEffect()
    {
        explodeSystem.transform.SetParent(null);
        explodeSystem.gameObject.SetActive(true);
        explodeSystem.transform.position = transform.position;
        explodeSystem.transform.localScale = Vector3.one;
        explodeSystem.Play();

        if (!gunMaster.bulletPool.Contains(this))
        {
            gunMaster.bulletPool.Add(this);
        }
        transform.SetParent(gunMaster.bulletNeutral);
        gameObject.SetActive(false);
    }

    public void Explode()
    {
        if (!deadAlready)
        {
            deadAlready = true;
            if (photonView.IsMine)
            {
                PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
                for (int i = 0; i < players.Length; i++)
                {
                    float distance = Vector3.Distance(transform.position, players[i].gameObject.GetComponent<PhotonVRPlayer>().Head.position);
                    float force = distance < grenadeConfig.minDamageDistance ? grenadeConfig.maxForce : 
                        Mathf.Lerp(grenadeConfig.maxForce, grenadeConfig.minForce, (distance - grenadeConfig.minDamageDistance) / (grenadeConfig.maxDamageDistance - grenadeConfig.minDamageDistance));
                    float damage = distance < grenadeConfig.minDamageDistance ? grenadeConfig.maxDamage : 
                        Mathf.Lerp(grenadeConfig.maxDamage, grenadeConfig.minDamage, (distance - grenadeConfig.minDamageDistance) / (grenadeConfig.maxDamageDistance - grenadeConfig.minDamageDistance));
                    if (PhotonRoyaleLobby.instance.activePlayersList.Contains(players[i].photonView.ControllerActorNr) && 
                        players[i].alive &&
                        distance < grenadeConfig.maxDamageDistance)
                    {
                        players[i].photonView.RPC("ExplosiveKnockback", players[i].photonView.Controller, 
                                                (players[i].gameObject.GetComponent<PhotonVRPlayer>().Head.position - transform.position).normalized, force);
                        players[i].photonView.RPC("CheckHit", players[i].photonView.Controller, players[i].gameObject.GetComponent<PhotonVRPlayer>().Head.position, (int)damage, playerID);
                    }
                }
            }
        }
    }

    public override IEnumerator DisableAfterDeath()
    {
        yield return new WaitForSeconds(bulletData.lifeTime);
        if (!gunMaster.bulletPool.Contains(this))
        {
            gunMaster.bulletPool.Add(this);
        }
        transform.SetParent(gunMaster.bulletNeutral);
        gameObject.SetActive(false);
    }
}
