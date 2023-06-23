using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlayFab;
using System;

public class TokensHandler : MonoBehaviour
{
    public static TokensHandler instance;
    // Start is called before the first frame update
    public static int seeds;
    public static int HowMuchADay = 100;

    public bool usePlayFab = false;
    public static bool staticUsePlayfab;
    
    static int amountToIncrease;

    public void Start()
    {
        if (instance == null)
        {
            DontDestroyOnLoad(gameObject);
            instance = this;
            if (!PlayerPrefs.HasKey("seeds"))
            {
                PlayerPrefs.SetInt("seeds", 500);
                PlayerPrefs.SetInt("seed", 1);
            }

            staticUsePlayfab = usePlayFab;
            StartCoroutine(WaitForPlayFab());
            InvokeRepeating("eEE", 1, 5);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public static void time()
    {
        if (PlayerPrefs.GetString("a", "") == "" || DateTime.FromBinary(long.Parse(PlayerPrefs.GetString("a"))).Date < DateTime.Today.Date)
        {
            PlayerPrefs.SetString("a", DateTime.Today.ToBinary().ToString());
            PlayerPrefs.SetInt("seeds", seeds + HowMuchADay);
            
            print("Currency = " + seeds);
        }
        print(DateTime.FromBinary(long.Parse(PlayerPrefs.GetString("a"))).Date);
    }

    public IEnumerator WaitForPlayFab()
    {
        while (!PlayFabManager.instance.PlayFabPlayerLoggedIn())
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        PlayFab.ClientModels.GetPlayerStatisticsRequest getRequest = new PlayFab.ClientModels.GetPlayerStatisticsRequest();
        getRequest.StatisticNames = new List<string>();
        getRequest.StatisticNames.Add("Last Reported Currency");
        getRequest.StatisticNames.Add("Marking");
        PlayFabClientAPI.GetPlayerStatistics(getRequest, OnGetCash, OnGetCashError);
    }

    public void eEE()
    {
        seeds = PlayerPrefs.GetInt("seeds", 0);
    }

    public static void OnGetCash(PlayFab.ClientModels.GetPlayerStatisticsResult result)
    {
        bool found = false;
        int stat = -1;
        for (int i = 0; i < result.Statistics.Count; i++)
        {
            if (result.Statistics[i].StatisticName == "Last Reported Currency")
            {
                found = true;
                stat = result.Statistics[i].Value;
                break;
            }
        }

        seeds = PlayerPrefs.GetInt("seeds", 0);
        if (found && stat > seeds)
        {
            seeds = stat;
            PlayerPrefs.SetInt("seeds", seeds);
        }
        time();

        PlayFab.ClientModels.StatisticUpdate update = new PlayFab.ClientModels.StatisticUpdate();
        update.StatisticName = "Last Reported Currency";
        update.Value = seeds;

        PlayFab.ClientModels.UpdatePlayerStatisticsRequest request = new PlayFab.ClientModels.UpdatePlayerStatisticsRequest();
        request.Statistics = new List<PlayFab.ClientModels.StatisticUpdate>();
        request.Statistics.Add(update);

        PlayFabClientAPI.UpdatePlayerStatistics(request, OnUpdateStatistics, OnUpdateStatisticsError);

        if (staticUsePlayfab)
        {
            PlayFab.ClientModels.GetPlayerStatisticsRequest getRequest = new PlayFab.ClientModels.GetPlayerStatisticsRequest();
            getRequest.StatisticNames = new List<string>();
            getRequest.StatisticNames.Add("MigratedCurrency");
            PlayFabClientAPI.GetPlayerStatistics(getRequest, OnGetStatistics, OnGetStatisticsError);
        }
    }

    public static void OnGetCashError(PlayFabError error)
    {
        Debug.LogError("Update Statistics failed: " + error.ErrorMessage);
    }

    public static void OnGetStatistics(PlayFab.ClientModels.GetPlayerStatisticsResult result)
    {
        bool found = false;
        int stat = -1;
        for (int i = 0; i < result.Statistics.Count; i++)
        {
            if (result.Statistics[i].StatisticName == "MigratedCurrency")
            {
                found = true;
                stat = result.Statistics[i].Value;
                break;
            }
        }

        if (!found || stat < 1)
        {
            PlayFab.ClientModels.GetUserInventoryRequest getRequest = new PlayFab.ClientModels.GetUserInventoryRequest();
            PlayFabClientAPI.GetUserInventory(getRequest, OnGetInventory, OnGetInventoryError);
        }
    }

    public static void OnGetStatisticsError(PlayFabError error)
    {
        Debug.LogError("Update Statistics failed: " + error.ErrorMessage);
    }

    public static void OnUpdateStatistics(PlayFab.ClientModels.UpdatePlayerStatisticsResult result)
    {

    }

    public static void OnUpdateStatisticsError(PlayFabError error)
    {
        Debug.LogError("Update Statistics failed: " + error.ErrorMessage);
    }

    public static void OnGetInventory(PlayFab.ClientModels.GetUserInventoryResult result)
    {
        amountToIncrease = 0;
        if (result.VirtualCurrency.ContainsKey("IC"))
        {
            amountToIncrease = PlayerPrefs.GetInt("seeds", 0) - result.VirtualCurrency["IC"];
        }
        else
        {
            amountToIncrease = PlayerPrefs.GetInt("seeds", 0);
        }

        PlayFab.ClientModels.AddUserVirtualCurrencyRequest request = new PlayFab.ClientModels.AddUserVirtualCurrencyRequest();
        request.Amount = amountToIncrease;
        request.VirtualCurrency = "IC";
        PlayFabClientAPI.AddUserVirtualCurrency(request, OnAddCurrencySuccess, OnAddCurrencyError);
    }

    public static void OnGetInventoryError(PlayFabError error)
    {
        Debug.LogError("Get Inventory failed: " + error.ErrorMessage);
    }

    public static void OnAddCurrencySuccess(PlayFab.ClientModels.ModifyUserVirtualCurrencyResult result)
    {
        PlayFab.ClientModels.StatisticUpdate update = new PlayFab.ClientModels.StatisticUpdate();
        update.StatisticName = "MigratedCurrency";
        update.Value = 1;

        PlayFab.ClientModels.UpdatePlayerStatisticsRequest request = new PlayFab.ClientModels.UpdatePlayerStatisticsRequest();
        request.Statistics = new List<PlayFab.ClientModels.StatisticUpdate>();
        request.Statistics.Add(update);

        PlayFabClientAPI.UpdatePlayerStatistics(request, OnFinalStatistics, OnFinalStatisticsError);
    }

    public static void OnAddCurrencyError(PlayFabError error)
    {
        PlayFab.ClientModels.AddUserVirtualCurrencyRequest request = new PlayFab.ClientModels.AddUserVirtualCurrencyRequest();
        request.Amount = amountToIncrease;
        request.VirtualCurrency = "IC";
        PlayFabClientAPI.AddUserVirtualCurrency(request, OnAddCurrencySuccess, OnAddCurrencyError);

        Debug.LogError("Update Add Currency failed: " + error.ErrorMessage);
    }

    public static void OnFinalStatistics(PlayFab.ClientModels.UpdatePlayerStatisticsResult result)
    {

    }

    public static void OnFinalStatisticsError(PlayFabError error)
    {
        PlayFab.ClientModels.StatisticUpdate update = new PlayFab.ClientModels.StatisticUpdate();
        update.StatisticName = "MigratedCurrency";
        update.Value = 1;

        PlayFab.ClientModels.UpdatePlayerStatisticsRequest request = new PlayFab.ClientModels.UpdatePlayerStatisticsRequest();
        request.Statistics = new List<PlayFab.ClientModels.StatisticUpdate>();
        request.Statistics.Add(update);
        PlayFabClientAPI.UpdatePlayerStatistics(request, OnFinalStatistics, OnFinalStatisticsError);
        
        Debug.LogError("Update Statistics failed: " + error.ErrorMessage);
    }
}