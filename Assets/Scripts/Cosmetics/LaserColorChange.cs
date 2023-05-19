using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;

public class LaserColorChange : MonoBehaviourPunCallbacks
{
    public Renderer laserRenderer;

    [ColorUsage(false, true)]
    public List<Color> colors = new List<Color>();

    public int lastColor = 0;

    public bool auto = false;

    public float timePerChange = 5.0f;

    float lastChange;

    public void Start()
    {
        if (photonView.IsMine)
        {
            lastColor = PlayerPrefs.GetInt("LastLaser", 0);
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
                lastColor = (lastColor + 1) % colors.Count;
                AnnounceColor(lastColor);
                lastChange = Time.time + timePerChange;
            }
        }
        else if (photonView.IsMine && hitButton)
        {
            lastColor = (lastColor + 1) % colors.Count;
            AnnounceColor(lastColor);
            photonView.RPC("AnnounceColor", RpcTarget.Others, lastColor);
            PlayerPrefs.SetInt("LastLaser", lastColor);
        }
    }

    [PunRPC]
    public void AnnounceColor(int color)
    {
        laserRenderer.material.SetColor("_EmissionColor", colors[color]);
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
