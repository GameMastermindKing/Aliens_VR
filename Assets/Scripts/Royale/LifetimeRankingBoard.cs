using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.VR.Player;
using Photon.Pun;
using PlayFab.ClientModels;
using PlayFab;
using TMPro;
using Photon.Realtime;

public class LifetimeRankingBoard : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public class LeaderboardEntry
    {
        public TextMeshProUGUI playerName;
        public TextMeshProUGUI killTotalText;
        public TextMeshProUGUI winTotalText;
        public TextMeshProUGUI deathTotalText;
        public TextMeshProUGUI gamesTotalText;
        public TextMeshProUGUI scoreTotalText;
        public TextMeshProUGUI standingText;
    }

    [System.Serializable]
    public class PlayerData
    {
        public string playerName;
        public int killTotal = 0;
        public int winTotal = 0;
        public int deathTotal = 0;
        public int gamesTotal = 0;
        public int scoreTotal = 0;
        public int currentStanding = 999999;
        public string playfabID;
    }

    public static LifetimeRankingBoard instance;
    public LeaderboardEntry[] playerEntries;
    public LeaderboardEntry myEntry;
    public PlayerData[] playerDatas = new PlayerData[10];

    public string icingsLeaderboardName;
    public string deathsLeaderboardName;
    public string gamesLeaderboardName;
    public string winsLeaderboardName;
    public string scoresLeaderboardName;

    public bool synced = false;
    public bool gathered = false;
    public float updateTime = 600.0f;
    public float lastUpdate = 0.0f;
    public float maxDistance = 195f;
    public float minDistance = 185f;

    public int winBalance = 2;
    public int lossBalance = 1;
    public int killBalance = 1;
    public int deathBalance = 1;

    public Vector3 startPos;

    PlayerData myData = new PlayerData();

    static int lastKillCount = 0;
    bool safeToStart = false;

    Coroutine cleanupRoutine;

    public IEnumerator Start()
    {
        instance = this;
        startPos = transform.position;
        while (!PlayFabManager.instance.PlayFabPlayerLoggedIn())
        {
            yield return new WaitForSeconds(0.5f);
        }
        SendMyDetails();
        safeToStart = true;

        if (photonView.IsMine)
        {
            GetPlayersOnLeaderboard();
        }
    }

    public void Update()
    {
        transform.LookAt(GorillaLocomotion.Player.Instance.headCollider.transform.position);
        if (Vector3.Distance(GorillaLocomotion.Player.Instance.headCollider.transform.position, startPos) > maxDistance)
        {
            transform.position = GorillaLocomotion.Player.Instance.headCollider.transform.position + (startPos - GorillaLocomotion.Player.Instance.headCollider.transform.position).normalized * maxDistance;
        }
        else if (Vector3.Distance(GorillaLocomotion.Player.Instance.headCollider.transform.position, startPos) < minDistance)
        {
            transform.position = GorillaLocomotion.Player.Instance.headCollider.transform.position + (startPos - GorillaLocomotion.Player.Instance.headCollider.transform.position).normalized * minDistance;
        }
        
        if (photonView.IsMine && safeToStart && (Time.time - lastUpdate > updateTime || (!synced && !gathered)))
        {
            GetPlayersOnLeaderboard();
        }

        #if UNITY_EDITOR
        if (Keyboard.current.periodKey.wasPressedThisFrame)
        {
            GetPlayersOnLeaderboard();
        }
        #endif
    }

    public void GetPlayersOnLeaderboard()
    {
        gathered = true;
        for (int i = 0; i < playerDatas.Length; i++)
        {
            playerDatas[i].gamesTotal = -1;
            playerDatas[i].killTotal = -1;
            playerDatas[i].winTotal = -1;
            playerDatas[i].deathTotal = -1;
            playerDatas[i].scoreTotal = -1;
        }

        lastUpdate = Time.time;
        PlayFab.ClientModels.GetLeaderboardRequest request = new GetLeaderboardRequest();
        request.StatisticName = scoresLeaderboardName;

        PlayFabClientAPI.GetLeaderboard(request, OnGetScoresLeaderboard, OnGetScoresLeaderboardFail);
    }

    public void OnGetScoresLeaderboard(PlayFab.ClientModels.GetLeaderboardResult result)
    {
        for (int i = 0; i < playerDatas.Length; i++)
        {
            if (i < result.Leaderboard.Count)
            {
                playerDatas[i].scoreTotal = result.Leaderboard[i].StatValue;
                playerDatas[i].playerName = result.Leaderboard[i].Profile.DisplayName;
                playerDatas[i].playfabID = result.Leaderboard[i].PlayFabId;

                GetLeaderboardAroundPlayerRequest winsRequest = new GetLeaderboardAroundPlayerRequest();
                winsRequest.StatisticName = winsLeaderboardName;
                winsRequest.PlayFabId = result.Leaderboard[i].PlayFabId;
                winsRequest.MaxResultsCount = 1;
                PlayFabClientAPI.GetLeaderboardAroundPlayer(winsRequest, OnGetWinsLeaderboard, OnGetWinsLeaderboardFail);
            }
        }
    }

    public void OnGetScoresLeaderboardFail(PlayFabError error)
    {
        Debug.Log(error.ErrorMessage);
        GetPlayersOnLeaderboard();
    }

    public void OnGetWinsLeaderboard(PlayFab.ClientModels.GetLeaderboardAroundPlayerResult result)
    {
        for (int i = 0; i < playerDatas.Length; i++)
        {
            if (playerDatas[i].playfabID == result.Leaderboard[0].PlayFabId)
            {
                playerDatas[i].winTotal = result.Leaderboard[0].StatValue;

                GetLeaderboardAroundPlayerRequest killRequest = new GetLeaderboardAroundPlayerRequest();
                killRequest.StatisticName = icingsLeaderboardName;
                killRequest.PlayFabId = result.Leaderboard[0].PlayFabId;
                killRequest.MaxResultsCount = 1;
                PlayFabClientAPI.GetLeaderboardAroundPlayer(killRequest, OnGetIcingsLeaderboard, OnGetIcingsLeaderboardFail);
            }
        }
    }

    public void OnGetWinsLeaderboardFail(PlayFabError error)
    {
        Debug.Log(error.ErrorMessage);
    }

    public void OnGetIcingsLeaderboard(PlayFab.ClientModels.GetLeaderboardAroundPlayerResult result)
    {
        for (int i = 0; i < playerDatas.Length; i++)
        {
            if (playerDatas[i].playfabID == result.Leaderboard[0].PlayFabId)
            {
                playerDatas[i].killTotal = result.Leaderboard[0].StatValue;

                GetLeaderboardAroundPlayerRequest deathRequest = new GetLeaderboardAroundPlayerRequest();
                deathRequest.StatisticName = deathsLeaderboardName;
                deathRequest.PlayFabId = result.Leaderboard[0].PlayFabId;
                deathRequest.MaxResultsCount = 1;
                PlayFabClientAPI.GetLeaderboardAroundPlayer(deathRequest, OnGetDeathsLeaderboard, OnGetDeathsLeaderboardFail);
            }
        }
    }

    public void OnGetIcingsLeaderboardFail(PlayFabError error)
    {
        Debug.Log(error.ErrorMessage);
    }

    public void OnGetDeathsLeaderboard(PlayFab.ClientModels.GetLeaderboardAroundPlayerResult result)
    {
        for (int i = 0; i < playerDatas.Length; i++)
        {
            if (playerDatas[i].playfabID == result.Leaderboard[0].PlayFabId)
            {
                playerDatas[i].deathTotal = result.Leaderboard[0].StatValue;

                GetLeaderboardAroundPlayerRequest gamesRequest = new GetLeaderboardAroundPlayerRequest();
                gamesRequest.StatisticName = gamesLeaderboardName;
                gamesRequest.PlayFabId = result.Leaderboard[0].PlayFabId;
                gamesRequest.MaxResultsCount = 1;
                PlayFabClientAPI.GetLeaderboardAroundPlayer(gamesRequest, OnGetGamesLeaderboard, OnGetGamesLeaderboardFail);
            }
        }
    }

    public void OnGetDeathsLeaderboardFail(PlayFabError error)
    {
        Debug.Log(error.ErrorMessage);
    }

    public void OnGetGamesLeaderboard(PlayFab.ClientModels.GetLeaderboardAroundPlayerResult result)
    {
        for (int i = 0; i < playerDatas.Length; i++)
        {
            if (playerDatas[i].playfabID == result.Leaderboard[0].PlayFabId)
            {
                playerDatas[i].gamesTotal = result.Leaderboard[0].StatValue;
                CheckIfLeaderboardIsReady();
            }
        }
    }

    public void OnGetGamesLeaderboardFail(PlayFabError error)
    {
        Debug.Log(error.ErrorMessage);
    }

    void CalculateStandings()
    {
        List<PlayerData> players = new List<PlayerData>(playerDatas);
        players.Sort((x, y) => y.scoreTotal.CompareTo(x.scoreTotal));

        for (int i = 0; i < playerEntries.Length; i++)
        {
            if (players[i].playfabID != "")
            {
                playerEntries[i].playerName.text = players[i].playerName;
                playerEntries[i].deathTotalText.text = players[i].deathTotal.ToString();
                playerEntries[i].gamesTotalText.text = players[i].gamesTotal.ToString();
                playerEntries[i].killTotalText.text = players[i].killTotal.ToString();
                playerEntries[i].scoreTotalText.text = players[i].scoreTotal.ToString();
                playerEntries[i].winTotalText.text = players[i].winTotal.ToString();
            }
            else
            {
                playerEntries[i].playerName.text = "";
                playerEntries[i].winTotalText.text = "";
                playerEntries[i].deathTotalText.text = "";
                playerEntries[i].gamesTotalText.text = "";
                playerEntries[i].killTotalText.text = "";
                playerEntries[i].scoreTotalText.text = "";
            }
        }
        SendData();
    }

    void CheckIfLeaderboardIsReady()
    {
        bool ready = true;
        for (int i = 0; i < playerDatas.Length; i++)
        {
            if (playerDatas[i].playfabID != "")
            {
                if (playerDatas[i].winTotal == -1)
                {
                    ready = false;
                    GetLeaderboardAroundPlayerRequest winsRequest = new GetLeaderboardAroundPlayerRequest();
                    winsRequest.StatisticName = winsLeaderboardName;
                    winsRequest.PlayFabId = playerDatas[i].playfabID;
                    winsRequest.MaxResultsCount = 1;
                    PlayFabClientAPI.GetLeaderboardAroundPlayer(winsRequest, OnGetWinsLeaderboard, OnGetWinsLeaderboardFail);
                }
                else if (playerDatas[i].killTotal == -1)
                {
                    ready = false;
                    GetLeaderboardAroundPlayerRequest killRequest = new GetLeaderboardAroundPlayerRequest();
                    killRequest.StatisticName = icingsLeaderboardName;
                    killRequest.PlayFabId = playerDatas[i].playfabID;
                    killRequest.MaxResultsCount = 1;
                    PlayFabClientAPI.GetLeaderboardAroundPlayer(killRequest, OnGetIcingsLeaderboard, OnGetIcingsLeaderboardFail);
                }
                else if (playerDatas[i].deathTotal == -1)
                {
                    ready = false;
                    GetLeaderboardAroundPlayerRequest deathRequest = new GetLeaderboardAroundPlayerRequest();
                    deathRequest.StatisticName = deathsLeaderboardName;
                    deathRequest.PlayFabId = playerDatas[i].playfabID;
                    deathRequest.MaxResultsCount = 1;
                    PlayFabClientAPI.GetLeaderboardAroundPlayer(deathRequest, OnGetDeathsLeaderboard, OnGetDeathsLeaderboardFail);
                }
                else if (playerDatas[i].gamesTotal == -1)
                {
                    ready = false;
                    GetLeaderboardAroundPlayerRequest gamesRequest = new GetLeaderboardAroundPlayerRequest();
                    gamesRequest.StatisticName = gamesLeaderboardName;
                    gamesRequest.PlayFabId = playerDatas[i].playfabID;
                    gamesRequest.MaxResultsCount = 1;
                    PlayFabClientAPI.GetLeaderboardAroundPlayer(gamesRequest, OnGetGamesLeaderboard, OnGetGamesLeaderboardFail);
                }
            }
        }

        if (ready)
        {
            CalculateStandings();
        }
    }

    [PunRPC]
    public void ReportStandings(string[] playerNames, string[] killTotals, string[] winTotals, string[] deathTotals, string[] gameTotals, string[] scoreTotals)
    {
        lastUpdate = Time.time;
        synced = true;
        for (int i = 0; i < playerEntries.Length; i++)
        {
            playerEntries[i].playerName.text = playerNames[i];
            playerEntries[i].killTotalText.text = killTotals[i];
            playerEntries[i].winTotalText.text = winTotals[i];
            playerEntries[i].deathTotalText.text = deathTotals[i];
            playerEntries[i].gamesTotalText.text = gameTotals[i];
            playerEntries[i].scoreTotalText.text = scoreTotals[i];
        }
    }

    public void SendData(Player newPlayer)
    {
        string[] playerNames = new string[10];
        string[] killTotals = new string[10];
        string[] winTotals = new string[10];
        string[] deathTotals = new string[10];
        string[] gameTotals = new string[10];
        string[] scoreTotals = new string[10];

        for (int i = 0; i < playerEntries.Length; i++)
        {
            playerNames[i] = playerEntries[i].playerName.text;
            killTotals[i] = playerEntries[i].killTotalText.text;
            winTotals[i] = playerEntries[i].winTotalText.text;
            deathTotals[i] = playerEntries[i].deathTotalText.text;
            gameTotals[i] = playerEntries[i].gamesTotalText.text;
            scoreTotals[i] = playerEntries[i].scoreTotalText.text;
        }
        
        photonView.RPC("ReportStandings", newPlayer, playerNames, killTotals, winTotals, deathTotals, gameTotals, scoreTotals);
    }

    public void SendData()
    {
        string[] playerNames = new string[10];
        string[] killTotals = new string[10];
        string[] winTotals = new string[10];
        string[] deathTotals = new string[10];
        string[] gameTotals = new string[10];
        string[] scoreTotals = new string[10];

        for (int i = 0; i < playerEntries.Length; i++)
        {
            playerNames[i] = playerEntries[i].playerName.text;
            killTotals[i] = playerEntries[i].killTotalText.text;
            winTotals[i] = playerEntries[i].winTotalText.text;
            deathTotals[i] = playerEntries[i].deathTotalText.text;
            gameTotals[i] = playerEntries[i].gamesTotalText.text;
            scoreTotals[i] = playerEntries[i].scoreTotalText.text;
        }
        
        photonView.RPC("ReportStandings", RpcTarget.Others, playerNames, killTotals, winTotals, deathTotals, gameTotals, scoreTotals);
    }

    public static void SendKillData(int count)
    {
        if (!IsValidGameForStats())
        {
            return;
        }
        
        if (LifetimeRankingBoard.instance == null)
        {
            return;
        }
        
        PlayFab.ClientModels.UpdatePlayerStatisticsRequest request = new UpdatePlayerStatisticsRequest();
        List<StatisticUpdate> updates = new List<StatisticUpdate>();
        StatisticUpdate statistic = new StatisticUpdate();
        statistic.StatisticName = LifetimeRankingBoard.instance.icingsLeaderboardName;
        statistic.Value = count;
        updates.Add(statistic);
        request.Statistics = updates;
        lastKillCount = count;

        PlayFabClientAPI.UpdatePlayerStatistics(request, OnUpdateKillData, OnUpdateKillDataFailed);
    }

    public static void OnUpdateKillData(PlayFab.ClientModels.UpdatePlayerStatisticsResult result)
    {
    }

    public static void OnUpdateKillDataFailed(PlayFabError error)
    {
        SendKillData(lastKillCount);
    }

    public static void SendDeathData()
    {
        if (!IsValidGameForStats())
        {
            return;
        }
        
        if (LifetimeRankingBoard.instance == null)
        {
            return;
        }
        
        PlayFab.ClientModels.UpdatePlayerStatisticsRequest request = new UpdatePlayerStatisticsRequest();
        List<StatisticUpdate> updates = new List<StatisticUpdate>();
        StatisticUpdate statistic = new StatisticUpdate();
        statistic.StatisticName = LifetimeRankingBoard.instance.deathsLeaderboardName;
        statistic.Value = 1;
        updates.Add(statistic);
        request.Statistics = updates;

        PlayFabClientAPI.UpdatePlayerStatistics(request, OnUpdateDeathData, OnUpdateDeathDataFailed);
    }

    public static void SendReviveData()
    {
        if (!IsValidGameForStats())
        {
            return;
        }
        
        if (LifetimeRankingBoard.instance == null)
        {
            return;
        }
        
        PlayFab.ClientModels.UpdatePlayerStatisticsRequest request = new UpdatePlayerStatisticsRequest();
        List<StatisticUpdate> updates = new List<StatisticUpdate>();
        StatisticUpdate statistic = new StatisticUpdate();
        statistic.StatisticName = LifetimeRankingBoard.instance.deathsLeaderboardName;
        statistic.Value = -1;
        updates.Add(statistic);
        request.Statistics = updates;

        PlayFabClientAPI.UpdatePlayerStatistics(request, OnUpdateDeathData, OnUpdateReviveDataFailed);
    }

    public static void OnUpdateDeathData(PlayFab.ClientModels.UpdatePlayerStatisticsResult result)
    {
    }

    public static void OnUpdateDeathDataFailed(PlayFabError error)
    {
        SendDeathData();
    }

    public static void OnUpdateReviveDataFailed(PlayFabError error)
    {
        SendReviveData();
    }

    public static void SendGameData()
    {
        if (!IsValidGameForStats())
        {
            return;
        }
        
        if (LifetimeRankingBoard.instance == null)
        {
            return;
        }
        
        PlayFab.ClientModels.UpdatePlayerStatisticsRequest request = new UpdatePlayerStatisticsRequest();
        List<StatisticUpdate> updates = new List<StatisticUpdate>();
        StatisticUpdate statistic = new StatisticUpdate();
        statistic.StatisticName = LifetimeRankingBoard.instance.gamesLeaderboardName;
        statistic.Value = 1;
        updates.Add(statistic);
        request.Statistics = updates;

        PlayFabClientAPI.UpdatePlayerStatistics(request, OnUpdateGamesData, OnUpdateGamesDataFailed);
    }

    public static void OnUpdateGamesData(PlayFab.ClientModels.UpdatePlayerStatisticsResult result)
    {
    }

    public static void OnUpdateGamesDataFailed(PlayFabError error)
    {
        SendGameData();
    }

    public static void SendWinData()
    {
        if (!IsValidGameForStats())
        {
            return;
        }

        if (LifetimeRankingBoard.instance == null)
        {
            return;
        }

        PlayFab.ClientModels.UpdatePlayerStatisticsRequest request = new UpdatePlayerStatisticsRequest();
        List<StatisticUpdate> updates = new List<StatisticUpdate>();
        StatisticUpdate statistic = new StatisticUpdate();
        statistic.StatisticName = LifetimeRankingBoard.instance.winsLeaderboardName;
        statistic.Value = 1;
        updates.Add(statistic);
        request.Statistics = updates;

        PlayFabClientAPI.UpdatePlayerStatistics(request, OnUpdateWinsData, OnUpdateWinsDataFailed);
    }

    public static void OnUpdateWinsData(PlayFab.ClientModels.UpdatePlayerStatisticsResult result)
    {
    }

    public static void OnUpdateWinsDataFailed(PlayFabError error)
    {
        SendWinData();
    }

    public static void SendMyDetails()
    {
        if (LifetimeRankingBoard.instance == null)
        {
            return;
        }

        GetPlayerStatisticsRequest request = new GetPlayerStatisticsRequest();
        request.StatisticNames = new List<string>();
        request.StatisticNames.Add(LifetimeRankingBoard.instance.icingsLeaderboardName);
        request.StatisticNames.Add(LifetimeRankingBoard.instance.deathsLeaderboardName);
        request.StatisticNames.Add(LifetimeRankingBoard.instance.gamesLeaderboardName);
        request.StatisticNames.Add(LifetimeRankingBoard.instance.winsLeaderboardName);
        PlayFabClientAPI.GetPlayerStatistics(request, OnGotData, OnGotDataFailed);
    }

    public static void OnGotData(GetPlayerStatisticsResult result)
    {
        if (LifetimeRankingBoard.instance == null)
        {
            return;
        }

        for (int i = 0; i < result.Statistics.Count; i++)
        {
            if (result.Statistics[i].StatisticName == LifetimeRankingBoard.instance.icingsLeaderboardName)
            {
                LifetimeRankingBoard.instance.myData.killTotal = result.Statistics[i].Value;
            }
            else if (result.Statistics[i].StatisticName == LifetimeRankingBoard.instance.deathsLeaderboardName)
            {
                LifetimeRankingBoard.instance.myData.deathTotal = result.Statistics[i].Value;
            }
            else if (result.Statistics[i].StatisticName == LifetimeRankingBoard.instance.gamesLeaderboardName)
            {
                LifetimeRankingBoard.instance.myData.gamesTotal = result.Statistics[i].Value;
            }
            else if (result.Statistics[i].StatisticName == LifetimeRankingBoard.instance.winsLeaderboardName)
            {
                LifetimeRankingBoard.instance.myData.winTotal = result.Statistics[i].Value;
            }
        }
        
        LifetimeRankingBoard.instance.myData.scoreTotal = LifetimeRankingBoard.instance.myData.killTotal * LifetimeRankingBoard.instance.killBalance + 
            LifetimeRankingBoard.instance.myData.winTotal * LifetimeRankingBoard.instance.winBalance - 
            LifetimeRankingBoard.instance.myData.deathTotal * LifetimeRankingBoard.instance.deathBalance - 
            (LifetimeRankingBoard.instance.myData.gamesTotal - LifetimeRankingBoard.instance.myData.winTotal) * LifetimeRankingBoard.instance.lossBalance;
        
        UpdatePlayerStatisticsRequest request = new UpdatePlayerStatisticsRequest();
        request.Statistics = new List<StatisticUpdate>();

        StatisticUpdate update = new StatisticUpdate();
        update.StatisticName = LifetimeRankingBoard.instance.scoresLeaderboardName;
        update.Value = LifetimeRankingBoard.instance.myData.scoreTotal;
        request.Statistics.Add(update);
        PlayFabClientAPI.UpdatePlayerStatistics(request, OnUpdatedMyScore, OnUpdatedMyScoreFailed);
    }

    public static void OnGotDataFailed(PlayFabError error)
    {
        SendMyDetails();
    }

    public static void OnUpdatedMyScore(UpdatePlayerStatisticsResult result)
    {
        if (LifetimeRankingBoard.instance == null)
        {
            return;
        }

        GetLeaderboardAroundPlayerRequest scoreRequest = new GetLeaderboardAroundPlayerRequest();
        scoreRequest.StatisticName = LifetimeRankingBoard.instance.scoresLeaderboardName;
        scoreRequest.MaxResultsCount = 1;
        PlayFabClientAPI.GetLeaderboardAroundPlayer(scoreRequest, OnGetMyScoreLeaderboard, OnGetMyScoreLeaderboardFail);
    }

    public static void OnUpdatedMyScoreFailed(PlayFabError error)
    {
        if (LifetimeRankingBoard.instance == null)
        {
            return;
        }

        UpdatePlayerStatisticsRequest request = new UpdatePlayerStatisticsRequest();
        request.Statistics = new List<StatisticUpdate>();

        StatisticUpdate update = new StatisticUpdate();
        update.StatisticName = LifetimeRankingBoard.instance.scoresLeaderboardName;
        update.Value = LifetimeRankingBoard.instance.myData.scoreTotal;
        request.Statistics.Add(update);
        PlayFabClientAPI.UpdatePlayerStatistics(request, OnUpdatedMyScore, OnUpdatedMyScoreFailed);
    }

    public static void OnGetMyScoreLeaderboard(GetLeaderboardAroundPlayerResult result)
    {
        if (LifetimeRankingBoard.instance == null)
        {
            return;
        }

        LifetimeRankingBoard.instance.myData.currentStanding = result.Leaderboard[0].Position;
        LifetimeRankingBoard.instance.myData.playerName = result.Leaderboard[0].DisplayName;

        LifetimeRankingBoard.instance.myEntry.playerName.text = LifetimeRankingBoard.instance.myData.playerName;
        LifetimeRankingBoard.instance.myEntry.deathTotalText.text = LifetimeRankingBoard.instance.myData.deathTotal.ToString();
        LifetimeRankingBoard.instance.myEntry.winTotalText.text = LifetimeRankingBoard.instance.myData.winTotal.ToString();
        LifetimeRankingBoard.instance.myEntry.killTotalText.text = LifetimeRankingBoard.instance.myData.killTotal.ToString();
        LifetimeRankingBoard.instance.myEntry.gamesTotalText.text = LifetimeRankingBoard.instance.myData.gamesTotal.ToString();
        LifetimeRankingBoard.instance.myEntry.scoreTotalText.text = LifetimeRankingBoard.instance.myData.scoreTotal.ToString();
        LifetimeRankingBoard.instance. myEntry.standingText.text = LifetimeRankingBoard.instance.myData.currentStanding.ToString();

        if (LifetimeRankingBoard.instance.myData.currentStanding < 10)
        {
            LifetimeRankingBoard.instance.StartCoroutine(GiveBadge(LifetimeRankingBoard.instance.myData.currentStanding));
        }
    }

    public static void OnGetMyScoreLeaderboardFail(PlayFabError error)
    {
        GetLeaderboardAroundPlayerRequest scoreRequest = new GetLeaderboardAroundPlayerRequest();
        scoreRequest.StatisticName = LifetimeRankingBoard.instance.winsLeaderboardName;
        scoreRequest.MaxResultsCount = 1;
        PlayFabClientAPI.GetLeaderboardAroundPlayer(scoreRequest, OnGetMyScoreLeaderboard, OnGetMyScoreLeaderboardFail);
    }

    static IEnumerator GiveBadge(int rank)
    {
        while (PhotonRoyalePlayer.me == null)
        {
            yield return null;
        }
        PhotonRoyalePlayer.me.photonView.RPC("ReportTopTen", RpcTarget.All, rank);
    }

    public static bool IsValidGameForStats()
    {
        return PopUpComputer.instance.inPublicRoom &&
               ((PhotonRoyaleLobby.instance.useTeams && PhotonRoyaleLobby.instance.activePlayersList.Count >= PhotonRoyaleLobby.instance.minPerTeam * 3) ||
               (!PhotonRoyaleLobby.instance.useTeams && PhotonRoyaleLobby.instance.activePlayersList.Count >= 5));
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (photonView.IsMine && (gathered || synced))
        {
            SendData(newPlayer);
        }
    }
}
