using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonBullet : MonoBehaviourPunCallbacks
{
    public enum DamageType
    {
        MySide,
        YourSide,
        DidIHit
    }

    public static DamageType damageType = DamageType.DidIHit;
    public PhotonGun gunMaster;
    public Rigidbody bulletRigidbody;
    public ParticleSystem hitSystem;
    public ScriptableBulletConfiguration bulletData;
    public int playerID;
    public bool hitSomething = false;

    public override void OnEnable()
    {
        base.OnEnable();
        bulletRigidbody.velocity = transform.forward * bulletData.bulletSpeed;

        if (hitSystem != null)
        {
            hitSystem.transform.SetParent(transform);
            hitSystem.gameObject.SetActive(false);
        }
        StartCoroutine(DisableAfterDeath());
        hitSomething = false;
    }

    public virtual void OnCollisionEnter(Collision hit)
    {
        if (hitSomething)
        {
            return;
        }
        hitSomething = true;
        hitSystem.transform.position = hit.contacts[0].point;
        hitSystem.transform.SetParent(null);
        hitSystem.transform.localScale = Vector3.one;
        hitSystem.gameObject.SetActive(true);
        hitSystem.Play();

        bool playerOnTeam = false;

        if (PhotonRoyaleLobby.instance != null)
        {
            playerOnTeam = PhotonRoyaleLobby.instance != null && PhotonRoyaleLobby.instance.useTeams;
        }

        PhotonRoyalePlayer hitPlayer = null;
        if (hit.collider.gameObject.GetComponent<ColliderLink>() != null)
        {
            hitPlayer = hit.collider.gameObject.GetComponent<ColliderLink>().player;
        }

        if (damageType == DamageType.MySide && 
            (hit.collider.tag == "Player" || hit.collider.tag == "MainCamera" || hit.gameObject.layer == 7) && gunMaster.playerID != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].photonView.IsMine)
                {
                    players[i].TakeDamage(bulletData.freezeDamage);
                    gunMaster.bulletPool.Add(this);
                    transform.SetParent(gunMaster.bulletNeutral);
                    gameObject.SetActive(false);
                    break;
                }
            }
        }
        else if (damageType == DamageType.YourSide && (hit.collider.tag == "Hitbox1" || hitPlayer != null))
        {
            hitPlayer = hitPlayer == null ? hit.transform.parent.parent.gameObject.GetComponent<PhotonRoyalePlayer>() : hitPlayer;
            if (hitPlayer.photonView.ControllerActorNr != playerID && PhotonNetwork.LocalPlayer.ActorNumber == playerID && hitPlayer.alive)
            {
                hitPlayer.photonView.RPC("TookHit", RpcTarget.All, hitPlayer.health - bulletData.freezeDamage);
                gunMaster.bulletPool.Add(this);
                gameObject.SetActive(false);
            }
        }
        else if (damageType == DamageType.DidIHit && (hit.collider.tag == "Hitbox1" || hitPlayer != null))
        {
            hitPlayer = hitPlayer == null ? hit.transform.parent.parent.gameObject.GetComponent<PhotonRoyalePlayer>() : hitPlayer;
            if (playerOnTeam)
            {
                if (PhotonNetwork.LocalPlayer.ActorNumber == playerID)
                {
                    bool foundFriend = false;
                    for (int i = 0; i < PhotonRoyalePlayer.me.playersInTeam.Count; i++)
                    {
                        if (PhotonRoyalePlayer.me.playersInTeam[i] == hitPlayer)
                        {
                            foundFriend = true;
                            break;
                        }
                    }

                    if (!foundFriend)
                    {
                        hitPlayer.photonView.RPC("CheckHit", hitPlayer.photonView.Controller, hit.contacts[0].point, bulletData.freezeDamage, playerID);
                    }
                }
                gunMaster.bulletPool.Add(this);
                gameObject.SetActive(false);
            }
            else
            {
                if (hitPlayer.photonView.ControllerActorNr != playerID && PhotonNetwork.LocalPlayer.ActorNumber == playerID && hitPlayer.alive)
                {
                    hitPlayer.photonView.RPC("CheckHit", hitPlayer.photonView.Controller, hit.contacts[0].point, bulletData.freezeDamage, playerID);
                    gunMaster.bulletPool.Add(this);
                    gameObject.SetActive(false);
                }
            }
        }
        else if (hit.collider.tag == "Shield")
        {
            PhotonIceShield shield = hit.collider.attachedRigidbody.gameObject.GetComponent<PhotonIceShield>();
            if (shield.photonView.ControllerActorNr != playerID && PhotonNetwork.LocalPlayer.ActorNumber == playerID)
            {
                shield.photonView.RPC("CheckHit", shield.photonView.Controller, hit.contacts[0].point, bulletData.freezeDamage);
            }
            gunMaster.bulletPool.Add(this);
            gameObject.SetActive(false);
        }
        else
        {
            gunMaster.bulletPool.Add(this);
            gameObject.SetActive(false);
        }
    }

    public virtual void Die()
    {
        gunMaster.bulletPool.Add(this);
        transform.SetParent(gunMaster.bulletNeutral);
        gameObject.SetActive(false);
    }

    public virtual IEnumerator DisableAfterDeath()
    {
        yield return new WaitForSeconds(bulletData.lifeTime);
        gunMaster.bulletPool.Add(this);
        transform.SetParent(gunMaster.bulletNeutral);
        gameObject.SetActive(false);
    }
}
