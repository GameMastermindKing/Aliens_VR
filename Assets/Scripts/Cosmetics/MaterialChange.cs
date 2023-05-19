using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;

public class MaterialChange : MonoBehaviourPunCallbacks
{
    public Renderer mainRenderer;
    public List<Texture2D> textures;
    public string cosmeticName;

    public int lastColor = 0;

    public bool auto = false;

    public float timePerChange = 5.0f;

    float lastChange;

    public override void OnEnable()
    {
        base.OnEnable();
        if (photonView.IsMine)
        {
            lastColor = PlayerPrefs.GetInt(cosmeticName + "_Color", 0);
            AnnounceColor(lastColor);
            photonView.RPC("AnnounceColor", RpcTarget.Others, lastColor);
        }
    }

    public void Update()
    {
        bool hitButton = false;
        #if UNITY_EDITOR
        hitButton = Keyboard.current.tKey.wasPressedThisFrame;
        #else
        hitButton = InputManager.instance.rightThumbstickPress.WasPressedThisFrame();
        #endif

        if (auto)
        {
            if (Time.time > lastChange)
            {
                lastColor = (lastColor + 1) % textures.Count;
                AnnounceColor(lastColor);
                lastChange = Time.time + timePerChange;
            }
        }
        else if (photonView.IsMine && hitButton)
        {
            lastColor = (lastColor + 1) % textures.Count;
            AnnounceColor(lastColor);
            photonView.RPC("AnnounceColor", RpcTarget.Others, lastColor);
            PlayerPrefs.SetInt(cosmeticName + "_Color", lastColor);
        }
    }

    [PunRPC]
    public void AnnounceColor(int color)
    {
        mainRenderer.material.mainTexture = textures[color];
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (photonView.IsMine)
        {
            photonView.RPC("AnnounceColor", newPlayer, lastColor);
        }
    }
}
