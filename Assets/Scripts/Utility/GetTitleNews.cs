using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab.ClientModels;
using PlayFab;

public class GetTitleNews : MonoBehaviour
{
    public delegate void NewsBroadcast(GetTitleNewsResult result);
    public NewsBroadcast OnNewsResult;

    public IEnumerator Start()
    {
        while (!PlayFabManager.instance.loggedIn)
        {
            yield return new WaitForSeconds(0.5f);
        }

        GetTitleNewsRequest request = new GetTitleNewsRequest();
        PlayFab.PlayFabClientAPI.GetTitleNews(request, OnGetTitleNews, OnGetTitleNewsFailed);
    }

    public void OnGetTitleNews(GetTitleNewsResult result)
    {
        Debug.Log("Sending");
        if (OnNewsResult != null)
        {
            OnNewsResult(result);
        }
    }

    public void OnGetTitleNewsFailed(PlayFabError error)
    {

    }
}
