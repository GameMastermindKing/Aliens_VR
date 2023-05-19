using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class SwitchHand : MonoBehaviourPunCallbacks
{
    public SwitchHand otherObject;
    public string objectName;

    public int hand = 0;

    public void Start()
    {
        if (PlayerPrefs.GetInt(objectName + "_Hand", 0) != hand)
        {
            SwitchToOther();
            photonView.RPC("SwitchToOther", RpcTarget.Others);
        }
    }

    public void Update()
    {
        bool hitButton = false;
        #if UNITY_EDITOR
        hitButton = Keyboard.current.tKey.wasPressedThisFrame;
        #else
        hitButton = InputManager.instance.leftThumbstickPress.WasPressedThisFrame();
        #endif

        if (photonView.IsMine && hitButton)
        {
            SwitchToOther();
            photonView.RPC("SwitchToOther", RpcTarget.Others);
            PlayerPrefs.SetInt(objectName + "_Hand", otherObject.hand);
        }
    }

    [PunRPC]
    public void SwitchToOther()
    {
        otherObject.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }
}
