using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PatchNotes : MonoBehaviour
{
    public GetTitleNews getter;
    public TextMeshPro notesText;
    public GameObject patchNotesObject;

    public void Start()
    {
        getter.OnNewsResult += ReceiveResult;
    }

    public void ReceiveResult(PlayFab.ClientModels.GetTitleNewsResult result)
    {
        Debug.Log(Application.version);
        for (int i = 0; i < result.News.Count; i++)
        {
            if (result.News[i].Title.Contains("Patch Notes") && result.News[i].Title.Contains(Application.version))
            {
                notesText.text = result.News[i].Body;
                patchNotesObject.SetActive(true);
            }
        }
    }
}
