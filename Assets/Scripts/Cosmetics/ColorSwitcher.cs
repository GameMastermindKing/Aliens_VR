using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class ColorSwitcher : MonoBehaviourPunCallbacks
{
    public static ColorSwitcher instance;
    public PhotonRoyalePlayer playerScript;
    public Color currentColor = Color.gray;
    public Renderer[] renderersToTint;

    public IEnumerator Start()
    {
        while (playerScript.photonView == null)
        {
            yield return null;
        }
        
        if (playerScript.photonView.IsMine)
        {
            instance = this;

            string[] color = PlayerPrefs.GetString("HairColor", "").Split(',');
            if (color.Length == 3)
            {
                photonView.RPC("SetColor", RpcTarget.All, float.Parse(color[0]), float.Parse(color[1]), float.Parse(color[2]));
            }
            else
            {
                photonView.RPC("SetColor", RpcTarget.All, currentColor.r, currentColor.g, currentColor.b);
            }
        }
    }

    [PunRPC]
    public void SetColor(float colorR, float colorG, float colorB)
    {
        currentColor = new Color(colorR, colorG, colorB, 1.0f);
        for (int i = 0; i < renderersToTint.Length; i++)
        {
            renderersToTint[i].material.color = currentColor;
        }
        
        if (photonView.IsMine)
        {
            PlayerPrefs.SetString("HairColor", currentColor.r.ToString() + "," + currentColor.g.ToString() + "," + currentColor.b.ToString());
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (photonView.IsMine)
        {
            photonView.RPC("SetColor", newPlayer, currentColor.r, currentColor.g, currentColor.b);
        }
    }
}
