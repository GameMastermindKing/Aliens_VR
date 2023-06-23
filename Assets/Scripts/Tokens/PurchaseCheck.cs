using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Platform;
using Oculus.Platform.Models;

public class PurchaseCheck : MonoBehaviour
{
    public float checkFrequency = 60f;
    float lastCheck = 0.0f;

    public Dictionary<string, int> purchaseDict = new Dictionary<string, int>();

    public static bool shouldCheckPurchase;
    public static PurchaseCheck instance;

    public IEnumerator Start()
    {
        if (instance == null)
        {
            DontDestroyOnLoad(gameObject);
            purchaseDict["1000-ICE"] = 1000;
            purchaseDict["5000-ICE"] = 5000;
            purchaseDict["13000-ICE"] = 13000;
            purchaseDict["1000-NewIcecubes"] = 1000;
            purchaseDict["5000-NewIceCubes"] = 5000;
            purchaseDict["13000-NewIceCubes"] = 13000;
            purchaseDict["1000-IceCubesCurrencie1"] = 1000;
            purchaseDict["5000-IceCubesCurrencie1"] = 5000;
            purchaseDict["13000-IceCubesCurrency"] = 13000;
            purchaseDict["40000IceCubes"] = 40000;
            instance = this;

            yield return new WaitForSeconds(5);
            CheckPurchase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Update()
    {
        if (Time.realtimeSinceStartup - lastCheck > checkFrequency)
        {
            lastCheck = Time.realtimeSinceStartup;
            if (shouldCheckPurchase)
            {
                CheckPurchase();
            }
        }
    }

    public void CheckPurchase()
    {
        IAP.GetViewerPurchases().OnComplete(GetPurchasesCallback);
    }

    private void GetPurchasesCallback(Message<PurchaseList> msg)
    {
        if (msg.IsError) 
        {
            Debug.LogError("Get Purchases has failed because: " + msg.GetError().Message);
            return;
        }
        Debug.LogError("Get Purchases has succeeded");

        foreach (var purch in msg.GetPurchaseList())
        {
            Debug.LogError("Get Purchases has: " + purch.Sku);
            if (purchaseDict.ContainsKey(purch.Sku))
            {
                int seedCount = PlayerPrefs.GetInt("seeds", 0) + purchaseDict[purch.Sku];
                PlayerPrefs.SetInt("seeds", seedCount);
                IAP.ConsumePurchase(purch.Sku);
                shouldCheckPurchase = false;
            }
        }
    }
}
