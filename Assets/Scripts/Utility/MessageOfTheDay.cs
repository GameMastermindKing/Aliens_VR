using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MessageOfTheDay : MonoBehaviour
{
    public GetTitleNews getter;
    public TextMeshPro newsText;

    public void Start()
    {
        getter.OnNewsResult += ReceiveResult;
    }

    public void ReceiveResult(PlayFab.ClientModels.GetTitleNewsResult result)
    {
        Debug.Log("Received");
        for (int i = 0; i < result.News.Count; i++)
        {
            if (result.News[i].Title == "MessageOfTheDay")
            {
                newsText.text = result.News[i].Body;
                break;
            }
        }
    }
}
