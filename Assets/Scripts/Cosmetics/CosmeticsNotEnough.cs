using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CosmeticsNotEnough : MonoBehaviour
{
    public static CosmeticsNotEnough instance;
    public GameObject popupObject;
    public Transform inAppPurchasePos;
    public Transform looker;

    public float autoOffDistance = 10.0f;

    public TextMeshProUGUI questionText;
    
    public void Start()
    {
        instance = this;
        popupObject.SetActive(false);

    }

    public void Update()
    {
        if (Vector3.Distance(GorillaLocomotion.Player.Instance.transform.position, popupObject.transform.position) > autoOffDistance)
        {
            popupObject.SetActive(false);
        }

        #if UNITY_EDITOR
        if (Keyboard.current.vKey.wasPressedThisFrame)
        {
            YesButton();
        }
        #endif
    }

    public void Setup(string itemName)
    {
        questionText.text = "You do not have enough ice" + "\n" + "cubes to buy the " + itemName + "." + "\n" + "\n" + "Would you like to buy more" + "\n" + "ice cubes?";
    }
    
    public void YesButton()
    {
        GorillaLocomotion.Player.Instance.transform.position = inAppPurchasePos.position;
        GorillaLocomotion.Player.Instance.InitializeValues();
        popupObject.SetActive(false);
    }

    public void NoButton()
    {
        popupObject.SetActive(false);
    }
}
