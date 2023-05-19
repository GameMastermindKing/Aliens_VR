using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerCosmetics : MonoBehaviourPunCallbacks
{
    #if UNITY_EDITOR
    public List<GameObject> testCosmetics;
    #endif

    public static PlayerCosmetics instance;

    public List<GameObject> Cosmetics;
    public List<GameObject> competitiveCosmetics;
    public List<GameObject> specialCosmetics;
    public List<int> competitiveScenes;

    public List<GameObject> objectsToChangeIfMine;
    public List<GameObject> observerObjectsToChangeIfMine;
    public int layerToUse;
    public int normalLayer;

    private void Awake()
    {
        if (photonView.IsMine)
        {
            instance = this;
            foreach (var item in GetComponentsInChildren<Collider>())
            {
                item.enabled = false;
            }

            List<string> foundNames = new List<string>();
            for (int i = 0; i < Cosmetics.Count; i++)
            {
                if (!foundNames.Contains(Cosmetics[i].name) && PlayerPrefs.GetFloat(Cosmetics[i].name, 0) == 1 && 
                    (!competitiveCosmetics.Contains(Cosmetics[i]) || !competitiveScenes.Contains(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex)) &&
                    !specialCosmetics.Contains(Cosmetics[i]))
                {
                    foundNames.Add(Cosmetics[i].name);
                }
            }
            
            if (foundNames.Count > 0)
            {
                photonView.RPC("EnableAllCosmetics", RpcTarget.All, new object[] { foundNames.ToArray() });
            }

            for (int i = 0; i < objectsToChangeIfMine.Count; i++)
            {
                objectsToChangeIfMine[i].layer = layerToUse;
            }

            for (int i = 0; i < observerObjectsToChangeIfMine.Count; i++)
            {
                observerObjectsToChangeIfMine[i].layer = layerToUse;
            }

            #if UNITY_EDITOR
            for (int i = 0; i < testCosmetics.Count; i++)
            {
                testCosmetics[i].SetActive(true);
            }
            #endif
        }
    }

    public bool SceneIsCompetitive()
    {
        return competitiveScenes.Contains(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public bool CosmeticEquipped(string cosmeticName)
    {
        for (int i = 0; i < Cosmetics.Count; i++)
        {
            if (Cosmetics[i].name == cosmeticName)
            {
                return Cosmetics[i].activeSelf;
            }
        }
        return false;
    }

    [PunRPC]
    void EnableCosmetic(string cosmeticName)
    {
        for (int i = 0; i < Cosmetics.Count; i++)
        {
            if (Cosmetics[i].name == cosmeticName && 
                (!competitiveCosmetics.Contains(Cosmetics[i]) || !competitiveScenes.Contains(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex)))
            {
                Cosmetics[i].SetActive(true);
            }
        }
    }

    [PunRPC]
    void EnableAllCosmetics(string[] cosmeticNames)
    {
        for (int i = 0; i < cosmeticNames.Length; i++)
        {
            for (int j = 0; j < Cosmetics.Count; j++)
            {
                if (Cosmetics[j].name == cosmeticNames[i] && 
                    (!competitiveCosmetics.Contains(Cosmetics[j]) || !competitiveScenes.Contains(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex)))
                {
                    Cosmetics[j].SetActive(true);
                }
            }
        }
    }

    [PunRPC]
    void DisableCosmetic(string cosmeticName)
    {
        for (int i = 0; i < Cosmetics.Count; i++)
        {
            if (Cosmetics[i].name == cosmeticName)
            {
                Cosmetics[i].SetActive(false);
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (photonView.IsMine)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < Cosmetics.Count; i++)
            {
                if (Cosmetics[i].activeSelf && !names.Contains(Cosmetics[i].name))
                {
                    names.Add(Cosmetics[i].name);
                }
            }
            StartCoroutine(AnnounceCosmetics(names, newPlayer));
        }
    }

    IEnumerator AnnounceCosmetics(List<string> names, Player newPlayer)
    {
        yield return new WaitForSeconds(5.0f);   
        photonView.RPC("EnableAllCosmetics", newPlayer,  new object[] { names.ToArray() });
    }
}
