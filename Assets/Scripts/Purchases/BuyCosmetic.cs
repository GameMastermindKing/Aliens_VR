using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayFab;
using PlayFab.ClientModels;

public class BuyCosmetic : MonoBehaviour
{
    public GameObject buttons;
    public GameObject warning;

    public int deleteAmmount;
    public static bool granting = false;
    public static int numTries = 0;
    public static int maxTries = 10;

    public float upDist = 3.0f;

    int newe;

    public string itemName;

    RaycastHit resultHitLeft;
    RaycastHit resultHitRight;
    Collider myCollider;

    public bool rightHandInside = false;
    public bool leftHandInside = false;
    public bool popupOn;
    public string properName;
    Coroutine waitRoutine;
    static BuyCosmetic pointedObject;

    public void Start()
    {
        myCollider = GetComponent<Collider>();
    }

    public void Update()
    {
        resultHitLeft = InputManager.instance.GetLeftHandHit();
        resultHitRight = InputManager.instance.GetRightHandHit();

        if (popupOn && (!CosmeticsNotEnough.instance.popupObject.activeSelf || 
                        Vector3.Distance(CosmeticsNotEnough.instance.popupObject.transform.position, transform.position + upDist * Vector3.up) > 0.2f))
        {
            popupOn = false;
        }

        if ((resultHitRight.collider == myCollider || resultHitLeft.collider == myCollider) && 
            Vector3.Distance(myCollider.transform.position, GorillaLocomotion.Player.Instance.headCollider.transform.position) < 3.0f)
        {
            #if UNITY_EDITOR
            if (!granting && Keyboard.current.tKey.wasPressedThisFrame)
            #else
            if (!granting && ((resultHitRight.collider == myCollider && InputManager.instance.rightHandTrigger.WasPressedThisFrame()) || 
                              (resultHitLeft.collider == myCollider && InputManager.instance.leftHandTrigger.WasPressedThisFrame())))
            #endif
            {
                if(PlayerPrefs.GetInt("seeds", 0) >= deleteAmmount)
                {
                    this.gameObject.SetActive(false);
                    buttons.gameObject.SetActive(true);
                    
                    numTries = 0;
                    granting = true;
                    Grant(itemName, newe, deleteAmmount);
                }
                else
                {
                    popupOn = true;
                    CosmeticsNotEnough.instance.popupObject.SetActive(true);
                    CosmeticsNotEnough.instance.popupObject.transform.position = transform.position + upDist * Vector3.up;
                    CosmeticsNotEnough.instance.popupObject.transform.rotation = transform.rotation;
                    CosmeticsNotEnough.instance.popupObject.transform.LookAt(CosmeticsNotEnough.instance.popupObject.transform.position + new Vector3(transform.forward.x, 0.0f, transform.forward.z));
                    CosmeticsNotEnough.instance.popupObject.transform.Rotate(Vector3.up * 180.0f);
                    CosmeticsNotEnough.instance.Setup(properName);
                }
            }

            warning.transform.position = transform.position + upDist * Vector3.up;
            warning.transform.rotation = transform.rotation;
            warning.transform.LookAt(warning.transform.position + new Vector3(transform.forward.x, 0.0f, transform.forward.z));
            warning.transform.Rotate(Vector3.up * 180.0f);
            warning.gameObject.SetActive(!granting && (!popupOn || !CosmeticsNotEnough.instance.popupObject.activeSelf));
            pointedObject = this;
        }
        else if (pointedObject == null)
        {
            warning.gameObject.SetActive(false);
        }
        else if (pointedObject == this)
        {
            pointedObject = null;
        }
    }
  
    public static void Grant(string itemName, int newe, int deleteAmmount)
    {
        var ItemGrant = new PlayFab.ServerModels.GrantItemsToUserRequest()
        {
            ItemIds = new List<string> { itemName },

            PlayFabId = GameObject.FindObjectOfType<PlayFabManager>().MyPlayFabID
        };
        PlayFabServerAPI.GrantItemsToUser(ItemGrant,
        onSuccess =>
        {
            BuyCosmetic[] buys = FindObjectsOfType<BuyCosmetic>(true);
            for (int i = 0; i < buys.Length; i++)
            {
                if (buys[i].itemName == itemName)
                {
                    buys[i].buttons.SetActive(true);
                }
            }

            newe = PlayerPrefs.GetInt("seeds", 0) - deleteAmmount;
            PlayerPrefs.SetInt("seeds", newe);
            
            PlayFab.ClientModels.StatisticUpdate update = new PlayFab.ClientModels.StatisticUpdate();
            update.StatisticName = "Last Reported Currency";
            update.Value = newe;

            PlayFab.ClientModels.UpdatePlayerStatisticsRequest request = new PlayFab.ClientModels.UpdatePlayerStatisticsRequest();
            request.Statistics = new List<PlayFab.ClientModels.StatisticUpdate>();
            request.Statistics.Add(update);

            PlayFabClientAPI.UpdatePlayerStatistics(request, OnUpdateStatistics, OnUpdateStatisticsError);

            ShopConsole.lastBoughtType = itemName;
            granting = false;
            Debug.Log("Cosmetic Bought");
        },
        onFailed =>
        {
            Debug.Log("Error");
            numTries++;
            if (numTries < maxTries)
            {
                Grant(itemName, newe, deleteAmmount);
            }
            else
            {
                granting = false;
            }
        });
    }

    public static void OnUpdateStatistics(PlayFab.ClientModels.UpdatePlayerStatisticsResult result)
    {

    }

    public static void OnUpdateStatisticsError(PlayFabError error)
    {
        Debug.LogError("Update Statistics failed: " + error.ErrorMessage);
    }
}
