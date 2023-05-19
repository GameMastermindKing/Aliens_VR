using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.InputSystem;

public class PhotonIceShield : RoyaleNonStackedGrabbable
{
    public Collider shieldCollider;
    public Texture2D[] textures;
    public Texture2D[] textureNorms;
    public Transform[] shieldBases;
    public int[] textureLevels;
    public int curHealth;
    public int maxHealth;
    public AudioSource crackSource;

    [Header("Shatter")]
    public GameObject shatteredShield;
    public Transform[] shatteredPieces;
    public Vector3[] shatteredPositions;
    public Quaternion[] shatteredRotations;

    public Vector3[] baseLocations;
    public Vector3[] baseRotations;

    public override IEnumerator Start()
    {
        shatteredPositions = new Vector3[shatteredPieces.Length];
        shatteredRotations = new Quaternion[shatteredPieces.Length];
        for (int i = 0; i < shatteredPieces.Length; i++)
        {
            shatteredPositions[i] = shatteredPieces[i].localPosition;
            shatteredRotations[i] = shatteredPieces[i].localRotation;
        }
        shieldBases = new Transform[2];
        shieldBases[0] = transform.GetChild(0);
        shieldBases[1] = transform.GetChild(1);

        yield return base.Start();
    }

    public void Update()
    {
        #if UNITY_EDITOR
        bool leftGrip = Keyboard.current.gKey.wasPressedThisFrame;
        bool rightGrip = Keyboard.current.hKey.wasPressedThisFrame;
        bool leftTrigger = Keyboard.current.vKey.wasPressedThisFrame;
        bool rightTrigger = Keyboard.current.bKey.wasPressedThisFrame;
        #endif

        if (held)
        {
            for (int i = 0; i < shieldBases.Length; i++)
            {
                shieldBases[i].localPosition = baseLocations[rightHand ? 1 : 0];
                shieldBases[i].localEulerAngles = baseRotations[rightHand ? 1 : 0];
            }
        }

        if (!photonView.IsMine)
        {
            grabbableRigidbody.isKinematic = true;
        }
        else if (!held && grabbableRigidbody.isKinematic)
        {
            grabbableRigidbody.isKinematic = false;
        }

        if (held && playerID == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            transform.position = hands[rightHand ? 1 : 0].position;
            transform.rotation = hands[rightHand ? 1 : 0].rotation;
        }
        else if (!held && royalePlayer != null && royalePlayer.alive && PhotonRoyaleLobby.instance.activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            if (InputManager.instance.GetLeftHandHit().collider == shieldCollider || InputManager.instance.GetRightHandHit().collider == shieldCollider)
            {
                #if UNITY_EDITOR
                if (royalePlayer.LeftHandHasSlot(grabbableItem) && InputManager.instance.GetLeftHandHit().collider == shieldCollider && leftGrip)
                #else
                if (royalePlayer.LeftHandHasSlot(grabbableItem) && InputManager.instance.GetLeftHandHit().collider == shieldCollider && InputManager.instance.leftHandGrip.WasPressedThisFrame())
                #endif
                {
                    photonView.RequestOwnership();
                    photonView.RPC("ShieldGrabbed", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, false);
                    royalePlayer.PutItemInSlot(true, grabbableItem, gameObject, transform);
                    for (int i = 0; i < grabbableRenderers.Length; i++)
                    {
                        grabbableRenderers[i].material.color = neutralColor;
                    }
                    rightHand = false;
                }
                #if UNITY_EDITOR
                else if (royalePlayer.RightHandHasSlot(grabbableItem) && InputManager.instance.GetRightHandHit().collider == shieldCollider && rightGrip)
                #else
                else if (royalePlayer.RightHandHasSlot(grabbableItem) && InputManager.instance.GetRightHandHit().collider == shieldCollider && InputManager.instance.rightHandGrip.WasPressedThisFrame())
                #endif
                {
                    photonView.RequestOwnership();
                    photonView.RPC("ShieldGrabbed", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, true);
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
    public void CheckHit(Vector3 position, int damage)
    {
        if (curHealth > 0)
        {
            int last = -1;
            for (int i = textureLevels.Length - 1; i > -1; i--)
            {
                if (curHealth < textureLevels[i])
                {
                    last = i;
                    break;
                }
            }

            curHealth = curHealth - damage;
            for (int i = textureLevels.Length - 1; i > -1; i--)
            {
                if (curHealth < textureLevels[i] && i > last)
                {
                    photonView.RPC("Crunch", RpcTarget.All, i);
                    last = i;
                }
            }
            grabbableRenderers[0].material.mainTexture = textures[last];

            if (curHealth <= 0 && photonView.IsMine)
            {
                photonView.RPC("Shattered", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    public void Crunch(int crunchNum)
    {
        crackSource.pitch = Random.Range(0.9f, 1.1f);
        crackSource.Play();
        grabbableRenderers[0].material.mainTexture = textures[crunchNum];
    }

    [PunRPC]
    public void ResetShield()
    {
        held = false;
        playerID = -1;
        shieldCollider.enabled = true;
        curHealth = maxHealth;

        grabbableRigidbody.isKinematic = !photonView.IsMine;
        activeInHand = false;
        shatteredShield.transform.SetParent(transform);
        shatteredShield.SetActive(false);
        shatteredShield.transform.localPosition = Vector3.zero;
        shatteredShield.transform.localRotation = grabbableRenderers[0].transform.localRotation;
        for (int i = 0; i < shatteredPieces.Length; i++)
        {
            shatteredPieces[i].localPosition = shatteredPositions[i];
            shatteredPieces[i].localRotation = shatteredRotations[i];
        }
    }

    [PunRPC]
    public void ShieldGrabbed(int newID, bool hand)
    {
        rightHand = hand;
        held = true;
        playerID = newID;
        shieldCollider.enabled = false;
        grabbableRigidbody.isKinematic = true;
        gameObject.SetActive(true);
    }

    [PunRPC]
    public void Shattered()
    {
        curHealth = maxHealth;
        held = false;
        playerID = -1;
        shieldCollider.enabled = true;

        shatteredShield.SetActive(true);
        shatteredShield.transform.SetParent(null);
        for (int i = 0; i < shatteredPieces.Length; i++)
        {
            shatteredPieces[i].localPosition = shatteredPositions[i];
            shatteredPieces[i].localRotation = shatteredRotations[i];
        }

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
    public void ShieldDropped(int healthRemaining)
    {
        held = false;
        playerID = -1;
        shieldCollider.enabled = true;
        curHealth = healthRemaining;

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

    [PunRPC]
    public void ActiveInHand()
    {
        activeInHand = true;
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (held && playerID == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            photonView.RPC("ShieldGrabbed", newPlayer, playerID, rightHand);
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        ResetShield();
    }
}
