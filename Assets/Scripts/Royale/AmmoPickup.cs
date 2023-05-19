using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class AmmoPickup : MonoBehaviourPunCallbacks
{
    public GameObject pickupObject;
    public Renderer[] ammoRenderers;
    public Collider ammoCollider;
    public int ammoInside = 50;
    public float grabDistance = 2.0f;
    public Color neutralColor;
    public Color highlightColor;
    PhotonRoyalePlayer royalePlayer;

    public void Start()
    {
        StartCoroutine(GetPlayer());
    }

    IEnumerator GetPlayer()
    {
        while (royalePlayer == null)
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
            yield return new WaitForSeconds(1.0f);
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(GetPlayer());
    }

    public void Update()
    {
        #if UNITY_EDITOR
        bool leftGrip = Keyboard.current.gKey.wasPressedThisFrame;
        bool rightGrip = Keyboard.current.hKey.wasPressedThisFrame;
        bool leftTrigger = Keyboard.current.vKey.isPressed;
        bool rightTrigger = Keyboard.current.bKey.isPressed;
        #endif

        if ((InputManager.instance.GetLeftHandHit().collider == ammoCollider || InputManager.instance.GetRightHandHit().collider == ammoCollider) && royalePlayer != null && 
            royalePlayer.alive && PhotonRoyaleLobby.instance.activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            #if UNITY_EDITOR
            if (InputManager.instance.GetLeftHandHit().collider == ammoCollider && leftGrip)
            #else
            if (InputManager.instance.GetLeftHandHit().collider == ammoCollider && InputManager.instance.leftHandGrip.WasPressedThisFrame())
            #endif
            {
                photonView.RPC("PickUpAmmo", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
                for (int i = 0; i < ammoRenderers.Length; i++)
                {
                    ammoRenderers[i].material.color = neutralColor;
                }
            }
            #if UNITY_EDITOR
            else if (InputManager.instance.GetRightHandHit().collider == ammoCollider && rightGrip)
            #else
            else if (InputManager.instance.GetRightHandHit().collider == ammoCollider && InputManager.instance.rightHandGrip.WasPressedThisFrame())
            #endif
            {
                photonView.RPC("PickUpAmmo", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
                for (int i = 0; i < ammoRenderers.Length; i++)
                {
                    ammoRenderers[i].material.color = neutralColor;
                }
            }
            else
            {
                for (int i = 0; i < ammoRenderers.Length; i++)
                {
                    ammoRenderers[i].material.color = highlightColor;
                }
            }
        }
        else
        {
            for (int i = 0; i < ammoRenderers.Length; i++)
            {
                ammoRenderers[i].material.color = neutralColor;
            }
        }
    }

    [PunRPC]
    public void PickUpAmmo(int playerID)
    {
        PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].photonView.ControllerActorNr == playerID)
            {
                players[i].ammo += ammoInside;
                break;
            }
        }
        pickupObject.SetActive(false);
    }
}
