using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.VR;
using Photon.Pun;
using Photon.VR.Player;

public class PhotonRoyaleObserver : MonoBehaviourPunCallbacks
{
    public enum ObserverMode
    {
        Thumbsticks,
        Look_and_Grip,
    }

    public static PhotonRoyaleObserver instance;
    public static ObserverMode observerMode;
    public GorillaLocomotion.Player player;
    public GameObject ghostBase;
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;
    public Vector3 lastPosition;

    public bool observing = false;

    public void Start()
    {
        if (photonView.IsMine)
        {
            instance = this;
        }
    }

    public void Update()
    {
        if (player == null)
        {
            player = FindObjectOfType<GorillaLocomotion.Player>();
        }

        if (photonView.IsMine)
        {
            transform.parent.gameObject.GetComponent<PhotonVRPlayer>().PlayersAudio.transform.position = 
                (observing ? head.position : transform.parent.gameObject.GetComponent<PhotonVRPlayer>().Head.position);
        }

        if (photonView.IsMine && player.useObserver)
        {
            head.transform.position = PhotonVRManager.Manager.Head.transform.position;
            head.transform.rotation = PhotonVRManager.Manager.Head.transform.rotation;

            rightHand.transform.position = PhotonVRManager.Manager.RightHand.transform.position;
            rightHand.transform.rotation = PhotonVRManager.Manager.RightHand.transform.rotation;

            leftHand.transform.position = PhotonVRManager.Manager.LeftHand.transform.position;
            leftHand.transform.rotation = PhotonVRManager.Manager.LeftHand.transform.rotation;
        }
    }

    [PunRPC]
    public void ActivateObserver()
    {
        observing = true;
        ghostBase.SetActive(true);
        if (photonView.IsMine)
        {
            head.transform.position = PhotonVRManager.Manager.Head.transform.position;
            head.transform.rotation = PhotonVRManager.Manager.Head.transform.rotation;

            rightHand.transform.position = PhotonVRManager.Manager.RightHand.transform.position;
            rightHand.transform.rotation = PhotonVRManager.Manager.RightHand.transform.rotation;

            leftHand.transform.position = PhotonVRManager.Manager.LeftHand.transform.position;
            leftHand.transform.rotation = PhotonVRManager.Manager.LeftHand.transform.rotation;
        }
    }

    [PunRPC]
    public void DisableObserver()
    {
        observing = false;
        ghostBase.SetActive(false);
    }
}
