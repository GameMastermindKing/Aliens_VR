using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Photon.VR;
using Photon.VR.Player;
using PlayFab;
using PlayFab.ClientModels;

public class PopUpComputer : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public class MutePlayerGroup
    {
        public PhotonVRPlayer userLink;
        public TextMeshProUGUI userNameText;
        public TextMeshProUGUI muteText;
        public bool userMuted = false;
    }

    [System.Serializable]
    public class InvitePlayerGroup
    {
        public PhotonRoyalePlayer userLink;
        public TextMeshProUGUI userNameText;
        public TextMeshProUGUI inviteText;
        public bool onTeam = false;
        public bool asking = false;
        public bool asked = false;
    }

    public static PopUpComputer instance;
    public static GameObject enableOnPrivateObject;
    public CanvasGroup computerGroup;
    public Transform[] fingerLasers;
    public static CurrentMode currentMode;
    public PopUpComputerCosmetics cosmeticsScript;
    public MutePlayerGroup[] muteGroups;
    public InvitePlayerGroup[] inviteGroups;
    public GameObject privateRoomButton;
    public GameObject usernameButton;
    public GameObject muteButton;
    public GameObject joinPublicButton;
    public GameObject joinPublicCodeButton;
    public GameObject exitObserverButton;
    public GameObject submitButton;
    public GameObject invitePlayersButton;
    public GameObject cosmeticsButton;
    public GameObject mirrorCam;
    public Transform[] playerHands;
    public LayerMask handMask;
    public RectTransform muteGroup;
    public RectTransform usernameGroup;
    public RectTransform usernameNameGroup;
    public RectTransform teamNamesGroup;
    public RectTransform roomCodeGroup;
    public RectTransform invitePlayersGroup;
    public RectTransform cosmeticsGroup;
    public PopUpInteractable leftInteractable;
    public PopUpInteractable rightInteractable;
    public TextMeshProUGUI computerHeaderText;
    public TextMeshProUGUI currentRoomText;
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI roomCodeText;
    public TextMeshProUGUI submitText;
    public TextMeshProUGUI teamText;

    public AudioSource overSource;
    public AudioSource activateSource;
    public AudioSource inviteSource;

    public Vector3 computerPosOn;
    public Vector3 computerPosOff;
    public Vector3 mutePosOn;
    public Vector3 mutePosOff;
    public Vector3 textBoxPosOn;
    public Vector3 textBoxPosOff;
    public Vector3 cosmeticsPosOn;
    public Vector3 cosmeticsPosOff;
    public float forwardDistance = 2.0f;
    public bool blocked = false;
    public bool inPublicRoom = true;

    float hapticWaitSeconds = 0.05f;
    RaycastHit resultHitLeft;
    RaycastHit resultHitRight;

    Collider lastLeftCollider;
    Collider lastRightCollider;

    string results = "";
    bool imAlive;
    bool theyAreDead;
    bool imInGame;
    bool theyAreInGame;
    bool publicServer;

    public enum CurrentMode
    {
        Username,
        RoomCode,
        MutePlayers,
        InvitePlayers,
        Cosmetics,
    }

    public void Start()
    {
        instance = this;
        Vector3 oldPos = transform.position;
        transform.SetParent(null, true);
        transform.position = oldPos;
        if (PlayerPrefs.HasKey("username"))
        {
            usernameText.text = PlayerPrefs.GetString("username", "Fred");
            if (!NameValidation.instance.IsNameValid(PlayerPrefs.GetString("username", "Fred")))
            {
                usernameText.text = NameValidation.instance.GetFixName();
                PlayerPrefs.SetString("username", usernameText.text);
                Application.Quit();
            }
        }
        else
        {
            usernameText.text = "Penguin" + UnityEngine.Random.Range(0, 10000000).ToString("0000000");
            PhotonVRManager.SetUsername(usernameText.text);
            PlayerPrefs.SetString("username", usernameText.text);
        }

        Invoke("UpdateMutePlayers", 10.0f);
        Invoke("UpdateInvitePlayers", 10.0f);

        if (currentMode == CurrentMode.Cosmetics)
        {
            cosmeticsScript.Invoke("Activate", 5f);
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        UpdateInvitePlayers();
        SwitchToSafeMode();
    }

    public void Update()
    {
        #if UNITY_EDITOR
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            //ToggleMutePlayer(0);
        }

        if (Keyboard.current.equalsKey.wasPressedThisFrame)
        {
            StartCoroutine(JoinPrivateRoom());
        }

        if (Keyboard.current.minusKey.wasPressedThisFrame)
        {
            Debug.Log("Public Room Test");
            MakePrivateRoomPublic();
        }

        if (Keyboard.current.slashKey.wasPressedThisFrame)
        {
            ComputerJoinPublicRoom();
        }

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            PrintProperties();
        }

        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            StartCoroutine(JoinSpecificPublicRoom());
        }

        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            if (PlayerPrefs.GetString("username", "") != "" && PlayerPrefs.GetString("username", "") != usernameText.text)
            {
                PlayerPrefs.SetString("username", usernameText.text);
                PhotonVRManager.SetUsername(usernameText.text);
                var request = new UpdateUserTitleDisplayNameRequest();
                request.DisplayName = PlayerPrefs.GetString("username");
                PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnPlayerNameResult, OnPlayFabError);
            }
        }

        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            for (int i = 0; i < inviteGroups.Length; i++)
            {
                if (inviteGroups[i].userLink != null && inviteGroups[i].asking)
                {
                    InvitePlayer(i);
                    break;
                }
            }
        }
        #endif

        SwitchToSafeMode();
        if (PhotonNetwork.CurrentRoom != null)
        {
            currentRoomText.text = "Current Room: " + PhotonNetwork.CurrentRoom.Name.Replace(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "").Replace("|", "");
        }
        currentRoomText.gameObject.SetActive(currentMode != CurrentMode.Cosmetics);

        if (InputManager.instance.rightHandB.WasPressedThisFrame() && !blocked)
        {
            ToggleComputer();
        }

        if (PhotonRoyalePlayer.SceneIsRoyaleMode() && PhotonRoyalePlayer.me != null)
        {
            for (int i = PhotonRoyalePlayer.me.playersInTeam.Count - 1; i > -1; i--)
            {
                if (PhotonRoyalePlayer.me.playersInTeam[i] == null)
                {
                    PhotonRoyalePlayer.me.playersInTeam.RemoveAt(i);
                }
            }

            for (int i = 0; i < muteGroups.Length; i++)
            {
                if (muteGroups[i] != null && muteGroups[i].userLink != null)
                {
                    imAlive = PhotonRoyalePlayer.me.alive;
                    theyAreDead = !muteGroups[i].userLink.GetComponent<PhotonRoyalePlayer>().alive;
                    imInGame = PhotonRoyaleLobby.instance.activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber) && PhotonRoyaleLobby.instance.gameStarted;
                    theyAreInGame = PhotonRoyaleLobby.instance.activePlayersList.Contains(muteGroups[i].userLink.photonView.ControllerActorNr) && PhotonRoyaleLobby.instance.gameStarted;
                    #if UNITY_EDITOR
                    muteGroups[i].userLink.PlayersAudio.volume = (muteGroups[i].userMuted || (imAlive && theyAreDead && imInGame && theyAreInGame) || 
                        (imInGame && !theyAreInGame)) || GorillaLocomotion.Player.editorMute ? 0.0f : 1.0f;
                    #else
                    muteGroups[i].userLink.PlayersAudio.volume = (muteGroups[i].userMuted || (imAlive && theyAreDead && imInGame && theyAreInGame) || (imInGame && !theyAreInGame)) ? 0.0f : 1.0f;
                    #endif
                }
            }
            
            for (int i = 0; i < inviteGroups.Length; i++)
            {
                inviteGroups[i].inviteText.transform.parent.gameObject.SetActive(!PhotonRoyalePlayer.me.playersInTeam.Contains(inviteGroups[i].userLink) &&
                    PhotonRoyalePlayer.me.playersInTeam.Count < PhotonRoyaleLobby.instance.maxPerTeam && 
                    (inviteGroups[i].userLink != null && inviteGroups[i].userLink.playersInTeam.Count + PhotonRoyalePlayer.me.playersInTeam.Count <= PhotonRoyaleLobby.instance.maxPerTeam));

                Color teamColor = inviteGroups[i].userLink != null && inviteGroups[i].userLink.teamID > -1 && inviteGroups[i].userLink.teamID < 15 ? 
                    PhotonRoyaleLobby.instance.teamColors[inviteGroups[i].userLink.teamID] : Color.white;
                inviteGroups[i].userNameText.color = teamColor;
                teamText.color = teamColor;
            }

            string teams = "Team:" + "\n";
            for (int i = 0; i < PhotonRoyaleLobby.instance.maxPerTeam; i++)
            {
                teams += PhotonRoyalePlayer.me.playersInTeam.Count > i ? PhotonRoyalePlayer.me.playersInTeam[i].GetComponent<PhotonVRPlayer>().NameText.text : "Empty Slot";
                teams += i < PhotonRoyaleLobby.instance.maxPerTeam - 1 ? "\n" : "";
            }

            Color myColor = PhotonRoyalePlayer.me != null && PhotonRoyalePlayer.me.teamID > -1 && PhotonRoyalePlayer.me.teamID < 15 ? 
                PhotonRoyaleLobby.instance.teamColors[PhotonRoyalePlayer.me.teamID] : Color.white;
            teamText.text = teams;
            teamText.color = myColor;
        }

        Physics.Raycast(playerHands[0].position, playerHands[0].forward, out resultHitLeft, 100.0f, handMask);
        Physics.Raycast(playerHands[1].position, playerHands[1].forward, out resultHitRight, 100.0f, handMask);
        if (resultHitLeft.collider != null)
        {
            if (computerGroup.alpha > 0.0f)
            {
                if (resultHitLeft.collider != lastLeftCollider)
                {
                    lastLeftCollider = resultHitLeft.collider;
                    overSource.pitch = Random.Range(0.9f, 1.1f);
                    overSource.Play();
                }

                leftInteractable = resultHitLeft.collider.gameObject.GetComponent<PopUpInteractable>();
                if (leftInteractable != null)
                {
                    leftInteractable.OnHighlight(this);
                }
            }

            fingerLasers[0].localScale = new Vector3(fingerLasers[0].localScale.x, fingerLasers[0].localScale.y, 
                Vector3.Distance(fingerLasers[0].position, resultHitLeft.point) / GorillaLocomotion.Player.Instance.transform.localScale.x);
            fingerLasers[0].gameObject.SetActive(true);
        }
        else
        {
            leftInteractable = null;
            fingerLasers[0].gameObject.SetActive(false);
        }

        if (resultHitRight.collider != null)
        {
            if (computerGroup.alpha > 0.0f)
            {
                if (resultHitRight.collider != lastRightCollider)
                {
                    lastRightCollider = resultHitRight.collider;
                    overSource.pitch = Random.Range(0.9f, 1.1f);
                    overSource.Play();
                }

                rightInteractable = resultHitRight.collider.gameObject.GetComponent<PopUpInteractable>();
                if (rightInteractable != null)
                {
                    rightInteractable.OnHighlight(this);
                }
            }

            fingerLasers[1].localScale = new Vector3(fingerLasers[1].localScale.x, fingerLasers[1].localScale.y, 
                Vector3.Distance(fingerLasers[1].position, resultHitRight.point) / GorillaLocomotion.Player.Instance.transform.localScale.x);
            fingerLasers[1].gameObject.SetActive(true);
        }
        else
        {
            rightInteractable = null;
            fingerLasers[1].gameObject.SetActive(false);
        }

        roomCodeGroup.anchoredPosition3D = 
            Vector3.Lerp(roomCodeGroup.anchoredPosition3D, currentMode == CurrentMode.RoomCode ? textBoxPosOn : textBoxPosOff, Time.deltaTime * 2.0f);
        usernameNameGroup.anchoredPosition3D = 
            Vector3.Lerp(usernameNameGroup.anchoredPosition3D, currentMode == CurrentMode.Username ? textBoxPosOn : textBoxPosOff, Time.deltaTime * 2.0f);
        teamNamesGroup.anchoredPosition3D = 
            Vector3.Lerp(teamNamesGroup.anchoredPosition3D, currentMode == CurrentMode.InvitePlayers ? textBoxPosOn : textBoxPosOff, Time.deltaTime * 2.0f);

        muteGroup.anchoredPosition3D = 
            Vector3.Lerp(muteGroup.anchoredPosition3D, currentMode == CurrentMode.MutePlayers ? mutePosOn : mutePosOff, Time.deltaTime * 2.0f);
        usernameGroup.anchoredPosition3D = 
            Vector3.Lerp(usernameGroup.anchoredPosition3D, (currentMode == CurrentMode.Username || currentMode == CurrentMode.RoomCode) ? computerPosOn : computerPosOff, Time.deltaTime * 2.0f);
        invitePlayersGroup.anchoredPosition3D = 
            Vector3.Lerp(invitePlayersGroup.anchoredPosition3D, currentMode == CurrentMode.InvitePlayers ? mutePosOn : mutePosOff, Time.deltaTime * 2.0f);
        cosmeticsGroup.anchoredPosition3D = 
            Vector3.Lerp(cosmeticsGroup.anchoredPosition3D, currentMode == CurrentMode.Cosmetics ? cosmeticsPosOn : cosmeticsPosOff, Time.deltaTime * 2.0f);

        mirrorCam.SetActive(cosmeticsGroup.anchoredPosition3D.y < cosmeticsPosOff.y * 0.6f);

        submitButton.SetActive(currentMode == CurrentMode.Username && usernameText.text.Length > 2 ||
                               currentMode == CurrentMode.RoomCode && roomCodeText.text.Length > 2);
        submitText.text = currentMode == CurrentMode.Username ? "Submit Username" : currentMode == CurrentMode.RoomCode ? "Join Private Code" : "";

        usernameButton.SetActive(!GorillaLocomotion.Player.Instance.useObserver && !(InRoyaleGame()));
        privateRoomButton.SetActive(!GorillaLocomotion.Player.Instance.useObserver && !(InRoyaleGame()));
        joinPublicButton.SetActive(!GorillaLocomotion.Player.Instance.useObserver && !(InRoyaleGame()));
        exitObserverButton.SetActive((PhotonRoyalePlayer.SceneIsRoyaleMode()) && (GorillaLocomotion.Player.Instance.useObserver &&
                                     (!InRoyaleGame())));
        joinPublicCodeButton.SetActive(currentMode == CurrentMode.RoomCode && roomCodeText.text.Length > 2 && !GorillaLocomotion.Player.Instance.useObserver && 
                                       !(InRoyaleGame()));
        invitePlayersButton.SetActive(PhotonRoyalePlayer.SceneIsRoyaleMode() && PhotonRoyaleLobby.instance.useTeams);
        cosmeticsButton.SetActive(NewShop.dataGathered);
        muteButton.SetActive(true);
    }

    public bool InRoyaleGame()
    {
        if (PhotonRoyaleLobby.instance == null)
        {
            return false;
        }

        bool inActive = PhotonRoyaleLobby.instance.activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber);
        bool inQueue = PhotonRoyaleLobby.instance.playersQueueList.Contains(PhotonNetwork.LocalPlayer.ActorNumber);
        return (PhotonRoyalePlayer.SceneIsRoyaleMode() && PhotonRoyaleLobby.instance != null && 
                (PhotonRoyaleLobby.instance.gameStarted && (inActive || (inQueue && PhotonRoyaleLobby.instance.countingDown))));
    }

    public void SwitchToSafeMode()
    {
        if (currentMode == CurrentMode.InvitePlayers && (!PhotonRoyalePlayer.SceneIsRoyaleMode() || !PhotonRoyaleLobby.instance.useTeams))
        {
            ChangeMode(CurrentMode.RoomCode);
        }
    }

    public void ToggleComputer()
    {
        if (computerGroup.alpha > 0.0f)
        {
            computerGroup.alpha = 0.0f;
            computerGroup.blocksRaycasts = false;
        }
        else
        {
            GorillaLocomotion.Player player = FindObjectOfType<GorillaLocomotion.Player>();
            computerGroup.transform.position = player.headCollider.transform.position + 
                player.headCollider.transform.forward * forwardDistance;
            computerGroup.transform.LookAt(player.headCollider.transform.position, -Physics.gravity);
            computerGroup.transform.Rotate(Vector3.up * 180.0f);
            computerGroup.transform.position += Vector3.up * 0.2f;
            computerGroup.alpha = 1.0f;
            computerGroup.blocksRaycasts = true;
        }
    }

    public void AcceptInput(string text)
    {
        if (currentMode == CurrentMode.Username && usernameText.text.Length < 15)
        {
            usernameText.text += text;
        }
        else if (currentMode == CurrentMode.RoomCode && roomCodeText.text.Length < 15)
        {
            roomCodeText.text += text;
        }
    }

    public void BackspaceInput()
    {
        if (currentMode == CurrentMode.Username && usernameText.text.Length > 0)
        {
            usernameText.text = usernameText.text.Substring(0, usernameText.text.Length - 1);
        }
        else if (currentMode == CurrentMode.RoomCode && roomCodeText.text.Length > 0)
        {
            roomCodeText.text = roomCodeText.text.Substring(0, roomCodeText.text.Length - 1);
        }
    }

    public void ActivateMode()
    {
        switch (currentMode)
        {
            case CurrentMode.Username:
            {
                if (usernameText.text.Length > 2)
                {
                    if (!NameValidation.instance.IsNameValid(usernameText.text))
                    {
                        usernameText.text = NameValidation.instance.GetFixName();
                        PlayerPrefs.SetString("username", usernameText.text);
                        PhotonVRManager.SetUsername(usernameText.text);
                        Application.Quit();
                    }
                    else if (PlayerPrefs.GetString("username", "") != "" && PlayerPrefs.GetString("username", "") != usernameText.text)
                    {
                        PlayerPrefs.SetString("username", usernameText.text);
                        PhotonVRManager.SetUsername(usernameText.text);
                        var request = new UpdateUserTitleDisplayNameRequest();
                        request.DisplayName = PlayerPrefs.GetString("username");
                        PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnPlayerNameResult, OnPlayFabError);
                    }
                }
                break;
            }

            case CurrentMode.RoomCode:
            {
                if (roomCodeText.text.Length > 2)
                {
                    if (!NameValidation.instance.IsNameValid(roomCodeText.text))
                    {
                        Application.Quit();
                    }
                    StartCoroutine(JoinPrivateRoom());
                }
                break;
            }
        }
    }

    public void ChangeMode(CurrentMode mode)
    {
        currentMode = mode;
        switch (currentMode)
        {
            case CurrentMode.Username:
            {
                computerHeaderText.text = "Change Username";
                break;
            }

            case CurrentMode.RoomCode:
            {
                computerHeaderText.text = "Enter Room Code";
                break;
            }

            case CurrentMode.MutePlayers:
            {
                break;
            }
        }
    }

    public void UpdateMutePlayers()
    {
        PhotonVRPlayer[] allPlayers = FindObjectsOfType<PhotonVRPlayer>();
        for (int i = 0; i < allPlayers.Length; i++)
        {
            bool found = false;
            for (int j = 0; j < muteGroups.Length; j++)
            {
                if (muteGroups[j].userLink == allPlayers[i])
                {
                    muteGroups[j].userNameText.text = muteGroups[j].userLink.NameText.text;
                    found = true;
                    break;
                }
            }
            
            if (!found && !allPlayers[i].photonView.IsMine)
            {
                for (int j = 0; j < muteGroups.Length; j++)
                {
                    if (muteGroups[j].userLink == null)
                    {
                        muteGroups[j].userLink = allPlayers[i];
                        muteGroups[j].userMuted = false;
                        muteGroups[j].muteText.text = "Mute";
                        break;
                    }
                }
            }
        }

        for (int i = 0; i < muteGroups.Length; i++)
        {
            if (muteGroups[i].userLink != null)
            {
                muteGroups[i].userNameText.transform.parent.gameObject.SetActive(true);
                muteGroups[i].userNameText.text = muteGroups[i].userLink.NameText.text;
            }
            else
            {
                muteGroups[i].userNameText.transform.parent.gameObject.SetActive(false);
            }
        }
    }

    public void UpdateInvitePlayers()
    {
        PhotonRoyalePlayer[] allPlayers = FindObjectsOfType<PhotonRoyalePlayer>();
        for (int i = 0; i < allPlayers.Length; i++)
        {
            bool found = false;
            for (int j = 0; j < inviteGroups.Length; j++)
            {
                if (inviteGroups[j].userLink == allPlayers[i])
                {
                    inviteGroups[j].userNameText.text = inviteGroups[j].userLink.GetComponent<PhotonVRPlayer>().NameText.text;
                    found = true;
                    break;
                }
            }
            
            if (!found && !allPlayers[i].photonView.IsMine)
            {
                for (int j = 0; j < inviteGroups.Length; j++)
                {
                    if (inviteGroups[j].userLink == null)
                    {
                        inviteGroups[j].userLink = allPlayers[i];
                        inviteGroups[j].asked = false;
                        inviteGroups[j].asking = false;
                        inviteGroups[j].inviteText.text = "Invite";
                        break;
                    }
                }
            }
        }

        for (int i = 0; i < inviteGroups.Length; i++)
        {
            if (inviteGroups[i].userLink != null)
            {
                inviteGroups[i].userNameText.transform.parent.gameObject.SetActive(true);
                inviteGroups[i].userNameText.text = inviteGroups[i].userLink.GetComponent<PhotonVRPlayer>().NameText.text;
                for (int j = inviteGroups[i].userLink.playersInTeam.Count - 1; j > -1; j--)
                {
                    if (inviteGroups[i].userLink.playersInTeam[j] == null)
                    {
                        inviteGroups[i].userLink.playersInTeam.RemoveAt(j);
                    }
                }
            }
            else
            {
                inviteGroups[i].userNameText.transform.parent.gameObject.SetActive(false);
            }
        }
    }

    public void ToggleMutePlayer(int group)
    {
        if (!muteGroups[group].userMuted)
        {
            muteGroups[group].userMuted = true;
            muteGroups[group].userLink.PlayersAudio.volume = 0.0f;
            muteGroups[group].userLink.photonView.RPC("Muted", muteGroups[group].userLink.photonView.Owner);
            muteGroups[group].userLink.ToggleMechColliders(false);
            muteGroups[group].muteText.text = "Unmute";
        }
        else
        {
            muteGroups[group].userMuted = false;
            muteGroups[group].userLink.PlayersAudio.volume = 1.0f;
            muteGroups[group].userLink.photonView.RPC("UnMuted", muteGroups[group].userLink.photonView.Owner);
            muteGroups[group].userLink.ToggleMechColliders(true);
            muteGroups[group].muteText.text = "Mute";
        }
    }

    public void InvitePlayer(int group)
    {
        if (inviteGroups[group].asking)
        {
            List<int> playerInts = new List<int>();
            for (int i = PhotonRoyalePlayer.me.playersInTeam.Count - 1; i > -1; i--)
            {
                if (PhotonRoyalePlayer.me.playersInTeam[i] != null)
                {
                    playerInts.Add(PhotonRoyalePlayer.me.playersInTeam[i].photonView.ControllerActorNr);
                }
                else
                {
                    PhotonRoyalePlayer.me.playersInTeam.RemoveAt(i);
                }
            }

            for (int i = inviteGroups[group].userLink.playersInTeam.Count - 1; i > -1; i--)
            {
                if (inviteGroups[group].userLink.playersInTeam[i] != null)
                {
                    playerInts.Add(inviteGroups[group].userLink.playersInTeam[i].photonView.ControllerActorNr);
                }
                else
                {
                    inviteGroups[group].userLink.playersInTeam.RemoveAt(i);
                }
            }

            int teamID = PhotonRoyalePlayer.me.teamID != -1 ? PhotonRoyalePlayer.me.teamID : inviteGroups[group].userLink.teamID != -1 ? inviteGroups[group].userLink.teamID : -1;
            PhotonRoyalePlayer[] allPlayers = FindObjectsOfType<PhotonRoyalePlayer>();
            for (int i = 0; i < allPlayers.Length; i++)
            {
                if (teamID != -1 && allPlayers[i].teamID == teamID && !playerInts.Contains(allPlayers[i].photonView.ControllerActorNr))
                {
                    playerInts.Add(allPlayers[i].photonView.ControllerActorNr);
                }
            }

            if (PhotonRoyalePlayer.me.teamID != -1)
            {
                PhotonRoyalePlayer.me.photonView.RPC("TeamFormed", RpcTarget.All, playerInts.ToArray(), PhotonRoyalePlayer.me.teamID);
            }
            else if (inviteGroups[group].userLink.teamID != -1)
            {
                PhotonRoyalePlayer.me.photonView.RPC("TeamFormed", RpcTarget.All, playerInts.ToArray(), inviteGroups[group].userLink.teamID);
            }
            else
            {
                List<int> foundNums = new List<int>();
                PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
                for (int i = 0; i < players.Length; i++)
                {
                    if (!foundNums.Contains(players[i].teamID))
                    {
                        foundNums.Add(players[i].teamID);
                    }
                }

                for (int i = 0; i < 15; i++)
                {
                    if (!foundNums.Contains(i))
                    {
                        PhotonRoyalePlayer.me.photonView.RPC("TeamFormed", RpcTarget.All, playerInts.ToArray(), i);
                        break;
                    }
                }
            }
        }
        else if (!inviteGroups[group].asking && 
                  inviteGroups[group].userLink.playersInTeam.Count + PhotonRoyalePlayer.me.playersInTeam.Count <= PhotonRoyaleLobby.instance.maxPerTeam)
        {
            inviteGroups[group].userLink.photonView.RPC("Invited", inviteGroups[group].userLink.photonView.Owner, PhotonNetwork.LocalPlayer.ActorNumber);
            inviteGroups[group].inviteText.text = "Pending";
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("Room Name: " + PhotonNetwork.CurrentRoom.Name);
        if (!NameValidation.instance.IsNameValid(PhotonNetwork.CurrentRoom.Name.Replace(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "").Replace("|", ""), true))
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
        }
        Invoke("UpdateMutePlayers", 10.0f);
        Invoke("UpdateInvitePlayers", 10.0f);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        Invoke("UpdateMutePlayers", 1.0f);
        Invoke("UpdateInvitePlayers", 1.0f);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        Invoke("UpdateMutePlayers", 1.0f);
        Invoke("UpdateInvitePlayers", 1.0f);
    }

    void OnPlayFabError(PlayFabError obj)
    {
        Debug.Log("Error: " + obj.Error);
    }

    void OnPlayerNameResult(UpdateUserTitleDisplayNameResult obj)
    {
        Debug.Log("Username Was Changed To: " + obj.DisplayName);
    }
    
    public void MakePrivateRoomPublic()
    {
        ExitGames.Client.Photon.Hashtable hastable = new ExitGames.Client.Photon.Hashtable();
        hastable.Add("queue", PhotonVRManager.Manager.DefaultQueue);
        hastable.Add("version", Application.version);

        PhotonNetwork.CurrentRoom.MaxPlayers = (byte)PhotonVRManager.Manager.DefaultRoomLimit;
        PhotonNetwork.CurrentRoom.IsVisible = true;
        PhotonNetwork.CurrentRoom.IsOpen = true;

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)PhotonVRManager.Manager.DefaultRoomLimit;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "queue", "version" };
        PhotonVRManager.Manager.options = roomOptions;
        PhotonNetwork.CurrentRoom.SetCustomProperties(hastable);
        PhotonNetwork.CurrentRoom.SetPropertiesListedInLobby(new string[] { "queue", "version" });
    }

    #if UNITY_EDITOR
    public void PrintProperties()
    {
        results = "";
        foreach(DictionaryEntry entry in PhotonNetwork.CurrentRoom.CustomProperties)
        {
            results += entry.Key.ToString() + ", " + entry.Value.ToString() + "\n";
        }
        results += "MaxPlayers, " + PhotonNetwork.CurrentRoom.MaxPlayers.ToString() + "\n";
        results += "IsVisible, " + PhotonNetwork.CurrentRoom.IsVisible.ToString() + "\n";
        results += "IsOpen, " + PhotonNetwork.CurrentRoom.IsOpen.ToString() + "\n";

        foreach(string entry in PhotonNetwork.CurrentRoom.PropertiesListedInLobby)
        {
            results += entry + "\n";
        }

        Debug.Log(results);
    }

    public void ComputerJoinPublicRoom()
    {
        StartCoroutine(JoinPublicRoom());
    }
    #endif

    public IEnumerator JoinPublicRoom()
    {
        inPublicRoom = true;
        if (PhotonNetwork.InRoom)
        {
            PhotonVRManager.Manager.State = ConnectionState.Connected;
            PhotonNetwork.LeaveRoom();
        }
        yield return new WaitForSeconds(0.5f);
        PhotonVRManager.Manager.State = ConnectionState.JoiningRoom;
        PhotonVRManager.JoinOrCreateRandomRoom(PhotonVRManager.Manager.DefaultQueue, PhotonVRManager.Manager.DefaultRoomLimit);
        publicServer = false;
    }

    public IEnumerator JoinPrivateRoom()
    {
        inPublicRoom = false;
        if (PhotonNetwork.InRoom)
        {
            PhotonVRManager.Manager.State = ConnectionState.Connected;
            PhotonNetwork.LeaveRoom();
            yield return new WaitForSeconds(0.85f);
        }
        
        PhotonVRManager.Manager.State = ConnectionState.JoiningRoom;
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)PhotonVRManager.Manager.DefaultRoomLimit;
        roomOptions.IsVisible = false;
        roomOptions.IsOpen = true;

        PhotonVRManager.Manager.options = roomOptions;
        PhotonNetwork.JoinOrCreateRoom(roomCodeText.text + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, roomOptions, TypedLobby.Default);
        PlayerPrefs.SetString("YetiCode", roomCodeText.text);
        if (PopUpComputer.enableOnPrivateObject != null)
        {
            PopUpComputer.enableOnPrivateObject.SetActive(true);
        }
        publicServer = false;
    }

    public IEnumerator JoinSpecificPublicRoom()
    {
        inPublicRoom = true;
        if (PhotonNetwork.InRoom)
        {
            PhotonVRManager.Manager.State = ConnectionState.Connected;
            PhotonNetwork.LeaveRoom();
            yield return new WaitForSeconds(0.85f);
        }
        
        ExitGames.Client.Photon.Hashtable hastable = new ExitGames.Client.Photon.Hashtable();
        hastable.Add("queue", PhotonVRManager.Manager.DefaultQueue);
        hastable.Add("version", Application.version);
        
        PhotonVRManager.Manager.State = ConnectionState.JoiningRoom;
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)PhotonVRManager.Manager.DefaultRoomLimit;
        roomOptions.IsVisible = false;
        roomOptions.IsOpen = true;
        roomOptions.CustomRoomProperties = hastable;
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "queue", "version" };

        PhotonVRManager.Manager.options = roomOptions;
        PhotonNetwork.JoinOrCreateRoom(roomCodeText.text + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "|", roomOptions, TypedLobby.Default);
        PlayerPrefs.SetString("YetiCode", roomCodeText.text);
        if (PopUpComputer.enableOnPrivateObject != null)
        {
            PopUpComputer.enableOnPrivateObject.SetActive(true);
        }

        publicServer = true;
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        if (publicServer)
        {
            MakePrivateRoomPublic();
            publicServer = false;
        }
    }

    public void StartVibration(bool forLeftController, float amplitude, float duration)
    {
        base.StartCoroutine(this.HapticPulses(forLeftController, amplitude, duration));
    }

    private IEnumerator HapticPulses(bool forLeftController, float amplitude, float duration)
    {
        float startTime = Time.time;
        uint channel = 0U;
        UnityEngine.XR.InputDevice device;
        if (forLeftController)
        {
            device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        }
        else
        {
            device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        }
        while (Time.time < startTime + duration)
        {
            device.SendHapticImpulse(channel, amplitude, this.hapticWaitSeconds);
            yield return new WaitForSeconds(this.hapticWaitSeconds * 0.9f);
        }
        yield break;
    }
}
