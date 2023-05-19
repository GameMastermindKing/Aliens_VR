using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.VR.Player;

public class IceCubeButton : MonoBehaviour
{
    public Renderer buttonRenderer;

    [ColorUsage(false, true)]
    public Color onColor;

    [ColorUsage(false, true)]
    public Color offColor;

    public string cubeName;
    public bool cubeOn = false;

    public IEnumerator Start()
    {
        while (PlayerCosmetics.instance == null)
        {
            yield return null;
        }
        cubeOn = PlayerCosmetics.instance.CosmeticEquipped(cubeName);

        buttonRenderer.material.color = cubeOn ? onColor : offColor;
        buttonRenderer.material.SetColor("_EmissionColor", cubeOn ? onColor : offColor);
    }

    public void OnTriggerEnter(Collider hit)
    {
        if (hit.gameObject.GetComponentInParent<GorillaLocomotion.Player>() != null)
        {
            cubeOn = !cubeOn;
            buttonRenderer.material.color = cubeOn ? onColor : offColor;
            buttonRenderer.material.SetColor("_EmissionColor", cubeOn ? onColor : offColor);

            if (cubeOn)
            {
                PlayerCosmetics.instance.photonView.RPC("EnableCosmetic", Photon.Pun.RpcTarget.All, cubeName);
            }
            else
            {
                PlayerCosmetics.instance.photonView.RPC("DisableCosmetic", Photon.Pun.RpcTarget.All, cubeName);
            }
            PlayerPrefs.SetFloat(cubeName, cubeOn ? 1 : 0);
        }
    }
}
