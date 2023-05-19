using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class PhotonRoyaleLobby : MonoBehaviourPunCallbacks
{
    public enum DeathMode
    {
        Sky,
        UI,
    }

    [System.Serializable]
    public class DeathAnnouncement
    {
        public string freezeID;
        public string frozeID;
        public string weaponUsed;
    }
    
    public static PhotonRoyaleLobby instance;
    public static DeathMode deathMode;
    public AudioSource dunSource;
    public GameObject celebrationEffect;
    public AudioClip dunClip;
    public AudioClip goClip;
    public GameObject[] observerButtons;
    public GameObject[] lobbyBlockades;
    public Transform[] returnPoints;
    public Collider[] lobbyColliders;
    public Renderer[] queueButtons;
    public List<int> playersQueueList;
    public List<int> activePlayersList;
    public TextMeshProUGUI countdownText;
    public Bounds lobbyBounds = new Bounds();
    public Vector3 lastDeath = Vector3.zero;

    List<int> playersIFroze = new List<int>();

    public float deathFadeInTime = 0.5f;
    public float deathWaitTime = 2.0f;
    public float deathFadeOutTime = 0.5f;
    public float launchTime = 0.0f;

    [Header("Teams")]
    public int minPerTeam = 2;
    public int maxPerTeam = 2;
    public bool useTeams = false;
    public Color[] teamColors;

    Coroutine animateAnnouncementsRoutine;
    List<DeathAnnouncement> announcementsWaiting = new List<DeathAnnouncement>();

    public float waitTimeUntilStart = 15.0f;
    public float dunPitchIncrease = 0.01f;
    public int numPlayersToStart = 2;

    #if UNITY_EDITOR
    public bool queueEditor;
    #endif

    public bool gameStarted = false;
    public bool waitToEnd = false;
    public bool countingDown = false;
    public bool celebrating = false;
    public bool synced;
    GorillaLocomotion.Player player;
    int lastDun = -1;
    float startTime = 0.0f;

    public Coroutine checkRoutine = null;
    public Coroutine celebrateRoutine = null;

    public static List<PhotonRoyalePlayer> playersInGame = new List<PhotonRoyalePlayer>();
    public static int playersLeft = 0;

    public void Start()
    {
        instance = this;
        GorillaLocomotion.Player player = FindObjectOfType<GorillaLocomotion.Player>();
    }

    public bool AmIOutOfBounds(Vector3 position)
    {
        return !lobbyBounds.Contains(position);
    }

    [PunRPC]
    public void CelebrateWinners()
    {
        celebrating = true;
        celebrationEffect.transform.position = lastDeath;
        celebrationEffect.SetActive(true);

        if (photonView.IsMine)
        {
            celebrateRoutine = StartCoroutine(WaitForLeavers());
        }
    }

    IEnumerator WaitForLeavers()
    {
        if (!waitToEnd)
        {
            waitToEnd = true;
            yield return new WaitForSeconds(Random.Range(5.5f, 10.5f));
        
            PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
            List<PhotonRoyalePlayer> winners = new List<PhotonRoyalePlayer>();

            if (PhotonRoyaleLobby.instance.useTeams)
            {
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i].alive && PhotonRoyaleLobby.instance.activePlayersList.Contains(players[i].photonView.ControllerActorNr))
                    {
                        winners.Add(players[i]);
                    }
                }

                bool oneTeam = true;
                for (int i = 1; i < winners.Count; i++)
                {
                    if (!winners[0].playersInTeam.Contains(winners[i]))
                    {
                        oneTeam = false;
                    }
                }
                
                if (oneTeam)
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (PhotonRoyaleLobby.instance.activePlayersList.Contains(players[i].photonView.ControllerActorNr))
                        {
                            if (!players[i].alive)
                            {
                                players[i].photonView.RPC("Revive", RpcTarget.All);
                            }
                            players[i].photonView.RPC("ResetToAirship", players[i].photonView.Controller);
                        }
                    }
                    PhotonRoyaleLobby.instance.photonView.RPC("GameOver", RpcTarget.All);
                }
            }
            else
            {
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i].alive && PhotonRoyaleLobby.instance.activePlayersList.Contains(players[i].photonView.ControllerActorNr))
                    {
                        winners.Add(players[i]);
                    }
                }
                
                if (winners.Count <= 1)
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (PhotonRoyaleLobby.instance.activePlayersList.Contains(players[i].photonView.ControllerActorNr))
                        {
                            if (!players[i].alive)
                            {
                                players[i].photonView.RPC("Revive", RpcTarget.All);
                            }
                            players[i].photonView.RPC("ResetToAirship", players[i].photonView.Controller);
                        }
                    }
                    PhotonRoyaleLobby.instance.photonView.RPC("GameOver", RpcTarget.All);
                }
            }
        }
        waitToEnd = false;
        celebrateRoutine = null;
    }

    [PunRPC]
    public void SyncState(int[] playersInQueue, int[] activePlayers, bool isGameStarted, float timeBeforeStart, bool countDownActive, bool celebrating)
    {
        synced = true;
        playersQueueList = new List<int>(playersInQueue);
        activePlayersList = new List<int>(activePlayers);
        gameStarted = isGameStarted;
        countingDown = countDownActive;
        this.celebrating = celebrating;

        if (timeBeforeStart > 0.0f)
        {
            startTime = Time.time + timeBeforeStart;
        }

        if (countingDown)
        {
            lastDun = Mathf.CeilToInt(timeBeforeStart);
            dunSource.pitch = 0.5f + (15 - lastDun) * 0.1f;
        }
    }

    [PunRPC]
    public void AddPlayer(int[] ids)
    {
        if (!useTeams || (useTeams && minPerTeam <= ids.Length))
        {
            for (int i = 0; i < ids.Length; i++)
            {
                if (!playersQueueList.Contains(ids[i]))
                {
                    playersQueueList.Add(ids[i]);
                }
            }
        }
        
        if (photonView.IsMine && !countingDown && playersQueueList.Count >= numPlayersToStart && !gameStarted)
        {
            countingDown = true;
            photonView.RPC("StartCountdown", RpcTarget.All);
        }
    }

    [PunRPC]
    public void StartCountdown()
    {
        waitToEnd = false;
        countingDown = true;
        startTime = Time.time + waitTimeUntilStart;
        dunSource.clip = dunClip;
        dunSource.pitch = 0.5f;
        dunSource.Play();
        lastDun = 15;
    }

    [PunRPC]
    public void InterruptCountdown()
    {
        countingDown = false;
    }

    [PunRPC]
    public void StartRound(int[] players)
    {
        if (photonView.IsMine)
        {
            GunSpawner.instance.photonView.RequestOwnership();
        }
        
        if (LifetimeRankingBoard.instance != null && activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            LifetimeRankingBoard.SendGameData();
        }
        playersIFroze.Clear();

        GunSpawner.instance.SetupGame();
        playersInGame.Clear();
        dunSource.Stop();
        dunSource.pitch = 1.0f;
        dunSource.clip = goClip;
        dunSource.Play();

        countingDown = false;
        gameStarted = true;
        playersQueueList.Clear();
        activePlayersList = new List<int>(players);

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                StartCoroutine(LobbyOff());
                PhotonRoyalePlayer.me.health = PhotonRoyalePlayer.me.startingHealth;

                PopUpComputer.instance.blocked = true;
                if (PopUpComputer.instance.computerGroup.alpha > 0.0f)
                {
                    PopUpComputer.instance.ToggleComputer();
                }
                PopUpComputer.instance.ChangeMode(PopUpComputer.CurrentMode.MutePlayers);

                for (int j = 0; j < lobbyBlockades.Length; j++)
                {
                    lobbyBlockades[j].SetActive(false);
                }
                break;
            }
        }
    }

    [PunRPC]
    public void GameOver()
    {
        celebrating = false;
        celebrationEffect.SetActive(false);
        playersInGame.Clear();
        if (PhotonStorm.instance.photonView.IsMine)
        {
            PhotonStorm.instance.photonView.RPC("ResetStormToStart", RpcTarget.All);
        }

        if (LifetimeRankingBoard.instance != null && activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            LifetimeRankingBoard.SendKillData(playersIFroze.Count);
            LifetimeRankingBoard.SendMyDetails();
            playersIFroze.Clear();
        }

        if (player == null)
        {
            player = FindObjectOfType<GorillaLocomotion.Player>();
        }

        if (player.useObserver && !activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            for (int i = 0; i < observerButtons.Length; i++)
            {
                observerButtons[i].SetActive(false);
            }
            player.SwitchToNormal();
        }

        gameStarted = false;
        activePlayersList.Clear();

        ClearAnnouncements();

        for (int i = 0; i < lobbyBlockades.Length; i++)
        {
            lobbyBlockades[i].SetActive(true);
        }

        for (int i = 0; i < lobbyColliders.Length; i++)
        {
            lobbyColliders[i].enabled = true;
        }
    }

    [PunRPC]
    public void PlayerFrozen(int playerFroze, int playerFreezing)
    {
        Photon.VR.Player.PhotonVRPlayer[] players = FindObjectsOfType<Photon.VR.Player.PhotonVRPlayer>();
        string frozeName = "PENGI";
        string freezeName = "THE_STORM";
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].photonView.ControllerActorNr == playerFroze)
            {
                frozeName = players[i].NameText.text;
            }
            else if (players[i].photonView.ControllerActorNr == playerFreezing)
            {
                freezeName = players[i].NameText.text;
                if (players[i].photonView.IsMine)
                {
                    photonView.RequestOwnership();
                    if (!playersIFroze.Contains(playerFroze) && (!PhotonRoyaleLobby.instance.useTeams || NotSameTeam(playerFroze)))
                    {
                        playersIFroze.Add(playerFroze);
                    }
                }
            }
        }

        announcementsWaiting.Add(new DeathAnnouncement());
        announcementsWaiting[announcementsWaiting.Count - 1].freezeID = freezeName;
        announcementsWaiting[announcementsWaiting.Count - 1].frozeID = frozeName;

        if (animateAnnouncementsRoutine == null)
        {
            animateAnnouncementsRoutine = StartCoroutine(AnimateFreezing());
        }

        if (gameStarted)
        {
            if ((playersLeft > 5 && !useTeams) || (playersLeft > maxPerTeam * 2 && useTeams))
            {
                PhotonNetwork.CurrentRoom.IsVisible = false;
            }
            else
            {
                PhotonNetwork.CurrentRoom.IsVisible = true;
            }
        }
        else
        {
            PhotonNetwork.CurrentRoom.IsVisible = true;
        }
    }

    IEnumerator AnimateFreezing()
    {
        while (announcementsWaiting.Count > 0)
        {
            switch (deathMode)
            {
                case DeathMode.Sky:
                {
                    PhotonRoyalePlayer.me.skyDeathFreezeNameText.text = announcementsWaiting[0].freezeID;
                    PhotonRoyalePlayer.me.skyDeathFrozeNameText.text = announcementsWaiting[0].frozeID;

                    announcementsWaiting.RemoveAt(0);
                    PhotonRoyalePlayer.me.deathAnnouncementSource.Play();

                    while (PhotonRoyalePlayer.me.skyDeathGroup.alpha < 1.0f)
                    {
                        PhotonRoyalePlayer.me.skyDeathGroup.alpha += Time.deltaTime / deathFadeInTime;
                        yield return null;
                    }

                    yield return new WaitForSeconds(deathWaitTime);
                    while (PhotonRoyalePlayer.me.skyDeathGroup.alpha > 0.0f)
                    {
                        PhotonRoyalePlayer.me.skyDeathGroup.alpha -= Time.deltaTime / deathFadeOutTime;
                        yield return null;
                    }
                    break;
                }

                case DeathMode.UI:
                {
                    PhotonRoyalePlayer.me.uiDeathFreezeNameText.text = announcementsWaiting[0].freezeID;
                    PhotonRoyalePlayer.me.uiDeathFrozeNameText.text = announcementsWaiting[0].frozeID;
                    announcementsWaiting.RemoveAt(0);
                    PhotonRoyalePlayer.me.uiDeathAnnouncementSource.Play();

                    while (PhotonRoyalePlayer.me.uiDeathGroup.alpha < 1.0f)
                    {
                        PhotonRoyalePlayer.me.uiDeathGroup.alpha += Time.deltaTime / deathFadeInTime;
                        yield return null;
                    }

                    yield return new WaitForSeconds(deathWaitTime);
                    while (PhotonRoyalePlayer.me.uiDeathGroup.alpha > 0.0f)
                    {
                        PhotonRoyalePlayer.me.uiDeathGroup.alpha -= Time.deltaTime / deathFadeOutTime;
                        yield return null;
                    }
                    break;
                }
            }
        }
        animateAnnouncementsRoutine = null;
    }

    bool NotSameTeam(int otherID)
    {
        bool foundFriend = false;
        for (int i = 0; i < PhotonRoyalePlayer.me.playersInTeam.Count; i++)
        {
            if (PhotonRoyalePlayer.me.playersInTeam[i].photonView.ControllerActorNr == otherID)
            {
                foundFriend = true;
                break;
            }
        }
        return !foundFriend;
    }

    void ClearAnnouncements()
    {
        announcementsWaiting.Clear();
        if (PhotonRoyalePlayer.me != null)
        {
            PhotonRoyalePlayer.me.skyDeathGroup.alpha = 0.0f;
            PhotonRoyalePlayer.me.uiDeathGroup.alpha = 0.0f;
        }
    }

    public void ToggleObserver()
    {
        if (player == null)
        {
            player = FindObjectOfType<GorillaLocomotion.Player>();
        }

        if (!player.useObserver)
        {
            player.SwitchToObserver();
        }
        else
        {
            player.SwitchToNormal();
        }
    }

    public IEnumerator LobbyOff()
    {
        yield return new WaitForSeconds(5.0f);
        for (int i = 0; i < lobbyColliders.Length; i++)
        {
            lobbyColliders[i].enabled = false;
        }

        yield return new WaitForSeconds(5.0f);
        for (int i = 0; i < lobbyColliders.Length; i++)
        {
            lobbyColliders[i].enabled = true;
        }
    }

    public void Update()
    {
        #if UNITY_EDITOR
        if (queueEditor)
        {
            queueEditor = false;
            AddMeToQueue();
        }

        if (UnityEngine.InputSystem.Keyboard.current.periodKey.wasPressedThisFrame)
        {
            photonView.RequestOwnership();
        }
        #endif

        if (photonView.Controller == null && PhotonNetwork.CurrentRoom != null)
        {
            photonView.RequestOwnership();
        }
        else if (photonView.Controller == PhotonNetwork.LocalPlayer)
        {
            if (!GunSpawner.instance.photonView.IsMine)
            {
                GunSpawner.instance.photonView.RequestOwnership();
            }
        }

        if (gameStarted && playersInGame.Count == 0)
        {
            PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
            for (int i = 0; i < players.Length; i++)
            {
                if (PhotonRoyaleLobby.instance.activePlayersList.Contains(players[i].photonView.ControllerActorNr))
                {
                    playersInGame.Add(players[i]);
                }
            }
        }
        else if (gameStarted && playersInGame.Count > 0)
        {
            playersLeft = 0;
            for (int i = playersInGame.Count - 1; i > -1; i--)
            {
                if (playersInGame[i] == null)
                {
                    playersInGame.RemoveAt(i);
                }
                else if (playersInGame[i].alive)
                {
                    playersLeft++; 
                }
            }
            PhotonRoyalePlayer.me.skyPlayersRemainingText.text = playersLeft.ToString();
            PhotonRoyalePlayer.me.uiPlayersRemainingText.text = playersLeft.ToString();
        }
        else if (gameStarted && activePlayersList.Count <= 0 && photonView.IsMine)
        {
            gameStarted = false;
        }

        if (!gameStarted && PhotonRoyalePlayer.me != null)
        {
            PhotonRoyalePlayer.me.skyPlayersRemainingText.text = "0";
        }

        for (int i = 0; i < observerButtons.Length; i++)
        {
            observerButtons[i].SetActive(!activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber) && PhotonRoyaleLobby.instance.gameStarted);
        }

        countdownText.gameObject.SetActive(countingDown);
        if (countingDown)
        {
            if (startTime > Time.time)
            {
                countdownText.text = Mathf.CeilToInt(startTime - Time.time).ToString("0");
                if (Mathf.CeilToInt((startTime - Time.time) / 1f) < lastDun)
                {
                    lastDun--;
                    dunSource.pitch += dunPitchIncrease;
                    dunSource.Play();
                    launchTime = Time.time;
                }
            }
            else
            {
                launchTime = Time.time;
                if (photonView.IsMine)
                {
                    if (!PhotonStorm.instance.photonView.IsMine)
                    {
                        PhotonStorm.instance.photonView.RequestOwnership();
                    }
                    PhotonStorm.instance.MoveStorm(true);
                    photonView.RPC("StartRound", RpcTarget.All, playersQueueList.ToArray());
                }
                countdownText.text = "0";
            }
        }

        if (activePlayersList.Count > 0 && gameStarted && !celebrating)
        {
            if (photonView.IsMine && checkRoutine == null)
            {       
                checkRoutine = StartCoroutine(PhotonRoyalePlayer.me.CheckIfGameIsOver());
            }
        }
        else if (activePlayersList.Count > 0 && gameStarted && celebrating)
        {
            if (photonView.IsMine && celebrateRoutine == null)
            {
                celebrateRoutine = StartCoroutine(WaitForLeavers());
            }
        }

        if (PhotonRoyalePlayer.me != null)
        {
            for (int i = 0; i < queueButtons.Length; i++)
            {
                if (useTeams)
                {
                    for (int j = PhotonRoyalePlayer.me.playersInTeam.Count - 1; j > -1; j--)
                    {
                        if (PhotonRoyalePlayer.me.playersInTeam[j] == null)
                        {
                            PhotonRoyalePlayer.me.playersInTeam.RemoveAt(j);
                        }
                    }
                    queueButtons[i].gameObject.SetActive(PhotonRoyalePlayer.me.playersInTeam.Count >= minPerTeam);
                }
                queueButtons[i].material.color = (playersQueueList.Contains(PhotonNetwork.LocalPlayer.ActorNumber) || 
                    activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber)) ? Color.green : Color.red;
            }

            switch (deathMode)
            {
                case DeathMode.Sky:
                {
                    PhotonRoyalePlayer.me.uiDeathGroup.transform.parent.gameObject.SetActive(false);
                    PhotonRoyalePlayer.me.skyDeathGroup.transform.parent.gameObject.SetActive(true);
                    break;
                }

                case DeathMode.UI:
                {
                    PhotonRoyalePlayer.me.uiDeathGroup.transform.parent.gameObject.SetActive(true);
                    PhotonRoyalePlayer.me.skyDeathGroup.transform.parent.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }

    public void AddMeToQueue()
    {
        for (int i = PhotonRoyalePlayer.me.playersInTeam.Count - 1; i > -1; i--)
        {
            if (PhotonRoyalePlayer.me.playersInTeam[i] == null)
            {
                PhotonRoyalePlayer.me.playersInTeam.RemoveAt(i);
            }
        }

        if (!playersQueueList.Contains(PhotonNetwork.LocalPlayer.ActorNumber) && (!useTeams || (useTeams && PhotonRoyalePlayer.me.playersInTeam.Count >= minPerTeam)))
        {
            if (!useTeams)
            {
                photonView.RPC("AddPlayer", RpcTarget.All, new int[] { PhotonNetwork.LocalPlayer.ActorNumber });
            }
            else
            {
                List<int> playerIDs = new List<int>();
                for (int i = 0; i < PhotonRoyalePlayer.me.playersInTeam.Count; i++)
                {
                    playerIDs.Add(PhotonRoyalePlayer.me.playersInTeam[i].photonView.ControllerActorNr);
                }
                photonView.RPC("AddPlayer", RpcTarget.All, playerIDs.ToArray());
            }
        }
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        synced = true;
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        activePlayersList.Clear();
        playersInGame.Clear();
        playersQueueList.Clear();
        gameStarted = false;
        countingDown = false;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (photonView.IsMine)
        {
            photonView.RPC("SyncState", newPlayer, playersQueueList.ToArray(), activePlayersList.ToArray(), gameStarted, startTime - Time.time, countingDown, celebrating);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        if (photonView.IsMine)
        {
            if (playersQueueList.Contains(otherPlayer.ActorNumber))
            {
                playersQueueList.Remove(otherPlayer.ActorNumber);
                if (playersQueueList.Count < numPlayersToStart)
                {
                    photonView.RPC("InterruptCountdown", RpcTarget.All);
                }
            }

            PhotonRoyalePlayer[] allPlayers = FindObjectsOfType<PhotonRoyalePlayer>();
            for (int i = 0; i < allPlayers.Length; i++)
            {
                for (int j = 0; j < allPlayers[i].playersInTeam.Count; j++)
                {
                    if (allPlayers[i].playersInTeam[j] == null || allPlayers[i].playersInTeam[j].photonView.Controller == otherPlayer)
                    {
                        allPlayers[i].playersInTeam.RemoveAt(j);
                        break;
                    }
                }
            }
        }
    }

    public IEnumerator SyncWait(Player newPlayer)
    {
        yield return new WaitForSeconds(3.0f);
        if (photonView.IsMine)
        {
            photonView.RPC("SyncState", newPlayer, playersQueueList.ToArray(), activePlayersList.ToArray(), gameStarted, startTime - Time.time, countingDown, celebrating);
        }
    }

    public void OnDrawGizmos()
    {
        for (int i = 0; i < returnPoints.Length; i++)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(returnPoints[i].position, 1.0f);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(lobbyBounds.center, lobbyBounds.size);
    }
}
