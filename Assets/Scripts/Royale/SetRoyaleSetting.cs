using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.VR.Player;

public class SetRoyaleSetting : MonoBehaviour
{
    public Renderer[] healthRenderers;
    public Renderer[] ammoRenderers;
    public Renderer[] itemRenderers;
    public Renderer[] observerRenderers;
    public Renderer[] deathRenderers;
    public Renderer[] cosmeticsRenderers;

    public Color offButtonColor;
    public Color onButtonColor;

    public void Start()
    {
        #if UNITY_EDITOR
        SetHealthMode(0);
        SetAmmoMode(0);
        SetItemMode(0);
        SetObserverMode(0);
        SetDeathMode(0);
        SetCosmeticsMode(0);
        #else
        SetHealthMode(PlayerPrefs.GetInt("HealthMode", 0));
        SetAmmoMode(PlayerPrefs.GetInt("AmmoMode", 0));
        SetItemMode(PlayerPrefs.GetInt("ItemMode", 0));
        SetObserverMode(PlayerPrefs.GetInt("ObserverMode", 0));
        SetDeathMode(PlayerPrefs.GetInt("DeathMode", 0));
        SetCosmeticsMode(PlayerPrefs.GetInt("CosmeticsMode", 0));
        #endif
    }

    public void SetHealthMode(int newMode)
    {
        PhotonRoyalePlayer.healthMode = (PhotonRoyalePlayer.HealthMode)newMode;
        for (int i = 0; i < healthRenderers.Length; i++)
        {
            healthRenderers[i].material.color = newMode == i ? onButtonColor : offButtonColor;
        }
        PlayerPrefs.SetInt("HealthMode", newMode);
    }

    public void SetAmmoMode(int newMode)
    {
        PhotonRoyalePlayer.ammoMode = (PhotonRoyalePlayer.AmmoMode)newMode;
        for (int i = 0; i < ammoRenderers.Length; i++)
        {
            ammoRenderers[i].material.color = newMode == i ? onButtonColor : offButtonColor;
        }
        PlayerPrefs.SetInt("AmmoMode", newMode);
    }

    public void SetItemMode(int newMode)
    {
        PhotonRoyalePlayer.itemMode = (PhotonRoyalePlayer.ItemMode)newMode;
        for (int i = 0; i < itemRenderers.Length; i++)
        {
            itemRenderers[i].material.color = newMode == i ? onButtonColor : offButtonColor;
        }
        PlayerPrefs.SetInt("ItemMode", newMode);
    }

    public void SetObserverMode(int newMode)
    {
        PhotonRoyaleObserver.observerMode = (PhotonRoyaleObserver.ObserverMode)newMode;
        for (int i = 0; i < observerRenderers.Length; i++)
        {
            observerRenderers[i].material.color = newMode == i ? onButtonColor : offButtonColor;
        }
        PlayerPrefs.SetInt("ObserverMode", newMode);
    }

    public void SetDeathMode(int newMode)
    {
        PhotonRoyaleLobby.deathMode = (PhotonRoyaleLobby.DeathMode)newMode;
        for (int i = 0; i < deathRenderers.Length; i++)
        {
            deathRenderers[i].material.color = newMode == i ? onButtonColor : offButtonColor;
        }
        PlayerPrefs.SetInt("DeathMode", newMode);
    }

    public void SetCosmeticsMode(int newMode)
    {
        PhotonVRPlayer.cosmetics = (PhotonVRPlayer.CosmeticsMode)newMode;
        for (int i = 0; i < cosmeticsRenderers.Length; i++)
        {
            cosmeticsRenderers[i].material.color = newMode == i ? onButtonColor : offButtonColor;
        }
        PlayerPrefs.SetInt("CosmeticsMode", newMode);
    }
}
