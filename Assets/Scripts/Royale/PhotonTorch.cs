using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class PhotonTorch : RoyaleNonStackedGrabbable
{
    public ScriptableTorchConfiguration torchData;
    public ParticleSystem torchSystem;
    public ParticleSystem snuffedSystem;
    public Light torchLight;
    public Collider torchCollider;
    public AudioSource torchSource;
    public int curHeal = 0;
    public float healRange = 1.0f;

    float lastTick = 0.0f;

    public bool activated = false;
    static bool gotCasts = false;

    public void Update()
    {
        #if UNITY_EDITOR
        bool leftGrip = Keyboard.current.gKey.wasPressedThisFrame;
        bool rightGrip = Keyboard.current.hKey.wasPressedThisFrame;
        bool leftTrigger = Keyboard.current.vKey.wasPressedThisFrame;
        bool rightTrigger = Keyboard.current.bKey.wasPressedThisFrame;
        #endif

        if (!photonView.IsMine)
        {
            grabbableRigidbody.isKinematic = true;
        }
        else if (!held && grabbableRigidbody.isKinematic)
        {
            grabbableRigidbody.isKinematic = false;
        }

        if (activated && !torchSystem.isEmitting)
        {
            torchSystem.Play();
            //torchLight.enabled = true;
            if (photonView.IsMine)
            {
                torchSource.Play();
            }
        }
        else if (!activated && torchSystem.isEmitting)
        {
            torchSystem.Stop();
            torchLight.enabled = false;
            if (photonView.IsMine)
            {
                torchSource.Stop();
            }
        }

        if (!PhotonRoyalePlayer.me.alive && torchSource.isPlaying)
        {
            torchSource.Stop();
        }

        if (held && playerID == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            transform.position = hands[rightHand ? 1 : 0].position;
            transform.rotation = hands[rightHand ? 1 : 0].rotation;

            #if UNITY_EDITOR
            if ((rightHand && rightTrigger) || (!rightHand && leftTrigger))
            #else
            if ((rightHand && InputManager.instance.rightHandTrigger.WasPressedThisFrame()) || (!rightHand && InputManager.instance.leftHandTrigger.WasPressedThisFrame()))
            #endif
            {
                photonView.RPC("ActivateTorch", RpcTarget.All, !activated);
            }

            if (!photonView.IsMine)
            {
                photonView.RequestOwnership();
            }

            if (activated)
            {
                if (Time.time - lastTick >= torchData.timePerTick)
                {
                    lastTick = Time.time;
                    curHeal += torchData.healPerTick;
                    royalePlayer.photonView.RPC("Heal", Photon.Pun.RpcTarget.All, torchData.healPerTick);

                    if (PhotonRoyaleLobby.instance.useTeams)
                    {
                        for (int i = 0; i < PhotonRoyalePlayer.me.playersInTeam.Count; i++)
                        {
                            if (royalePlayer != PhotonRoyalePlayer.me.playersInTeam[i] && 
                                Vector3.Distance(transform.position, PhotonRoyalePlayer.me.playersInTeam[i].renderersToTint[0].transform.position) < healRange)
                            {
                                PhotonRoyalePlayer.me.playersInTeam[i].photonView.RPC("Heal", Photon.Pun.RpcTarget.All, torchData.healPerTick);
                            }
                        }
                    }

                    if (curHeal > torchData.maxHeal)
                    {
                        photonView.RPC("BurnedOut", RpcTarget.All);
                    }
                }
            }
        }
        else if (!held && royalePlayer != null && royalePlayer.alive && PhotonRoyaleLobby.instance.activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            if (!gotCasts)
            {
                InputManager.instance.GetHandCasts(torchData.grabDistance);
                gotCasts = true;
            }

            if (InputManager.instance.GetLeftHandHit().collider == torchCollider || InputManager.instance.GetRightHandHit().collider == torchCollider)
            {
                #if UNITY_EDITOR
                if (royalePlayer.LeftHandHasSlot(grabbableItem) && InputManager.instance.GetLeftHandHit().collider == torchCollider && leftGrip)
                #else
                if (royalePlayer.LeftHandHasSlot(grabbableItem) && InputManager.instance.GetLeftHandHit().collider == torchCollider && InputManager.instance.leftHandGrip.WasPressedThisFrame())
                #endif
                {
                    photonView.RequestOwnership();
                    photonView.RPC("TorchGrabbed", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
                    royalePlayer.PutItemInSlot(true, grabbableItem, gameObject, transform);
                    for (int i = 0; i < grabbableRenderers.Length; i++)
                    {
                        grabbableRenderers[i].material.color = neutralColor;
                    }
                    rightHand = false;
                }
                #if UNITY_EDITOR
                else if (royalePlayer.RightHandHasSlot(grabbableItem) && InputManager.instance.GetRightHandHit().collider == torchCollider && rightGrip)
                #else
                else if (royalePlayer.RightHandHasSlot(grabbableItem) && InputManager.instance.GetRightHandHit().collider == torchCollider && InputManager.instance.rightHandGrip.WasPressedThisFrame())
                #endif
                {
                    photonView.RequestOwnership();
                    photonView.RPC("TorchGrabbed", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
                    royalePlayer.PutItemInSlot(false, grabbableItem, gameObject, transform);
                    for (int i = 0; i < grabbableRenderers.Length; i++)
                    {
                        grabbableRenderers[i].material.color = neutralColor;
                    }
                    rightHand = true;
                }
                else
                {
                    for (int i = 0; i < grabbableRenderers.Length; i++)
                    {
                        grabbableRenderers[i].material.color = highlightColor;
                    }
                }
            }
            else
            {
                for (int i = 0; i < grabbableRenderers.Length; i++)
                {
                    grabbableRenderers[i].material.color = neutralColor;
                }
            }
        }
    }

    [PunRPC]
    public void ResetTorch()
    {
        activated = false;
        held = false;
        playerID = -1;
        torchCollider.enabled = true;
        torchLight.enabled = false;
        torchSystem.Stop();
        torchSource.Stop();
        curHeal = 0;

        grabbableRigidbody.isKinematic = !photonView.IsMine;
    }

    [PunRPC]
    public void ActivateTorch(bool newActivated)
    {
        activated = newActivated;
    }

    [PunRPC]
    public void TorchGrabbed(int newID)
    {
        held = true;
        playerID = newID;
        torchCollider.enabled = false;
        grabbableRigidbody.isKinematic = true;
    }

    [PunRPC]
    public void BurnedOut()
    {
        curHeal = 0;
        held = false;
        playerID = -1;
        torchCollider.enabled = true;

        snuffedSystem.gameObject.SetActive(true);
        snuffedSystem.transform.position = transform.position;
        snuffedSystem.transform.rotation = transform.rotation;
        snuffedSystem.transform.SetParent(null);
        snuffedSystem.transform.localScale = Vector3.one;
        snuffedSystem.Play();

        if (photonView.IsMine)
        {
            if (rightHand)
            {
                royalePlayer.gunHands[1].DropAllFromSlot(royalePlayer.player.headCollider.transform.position);
                royalePlayer.gunHands[1].itemInSlot = royalePlayer.emptyItem;
            }
            else
            {
                royalePlayer.gunHands[0].DropAllFromSlot(royalePlayer.player.headCollider.transform.position);
                royalePlayer.gunHands[0].itemInSlot = royalePlayer.emptyItem;
            }
        }
        gameObject.SetActive(false);
    }

    [PunRPC]
    public void TorchDropped(int healRemaining)
    {
        held = false;
        playerID = -1;
        torchCollider.enabled = true;
        curHeal = healRemaining;
        torchLight.enabled = false;
        torchSystem.Stop();
        torchSource.Stop();

        if (photonView.IsMine)
        {
            grabbableRigidbody.isKinematic = false;
        }
    }

    [PunRPC]
    public void Stacked(bool onOff)
    {
        gameObject.SetActive(onOff);
    }
}
