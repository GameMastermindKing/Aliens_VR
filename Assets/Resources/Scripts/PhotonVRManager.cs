using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

using Photon.VR.Player;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice;

namespace Photon.VR
{
    public class PhotonVRManager : MonoBehaviourPunCallbacks
    {
        public static PhotonVRManager Manager;

        [Header("Photon")]
        public string AppId;
        public string VoiceAppId;
        [Tooltip("Please read https://doc.photonengine.com/en-us/pun/current/connection-and-authentication/regions for more information")]
        public string Region = "eu";

        [Header("Player")]
        public Transform Head;
        public Transform LeftHand;
        public Transform RightHand;
        public Color Colour;

        [Header("Networking")]
        public string DefaultQueue = "Default";
        public int DefaultRoomLimit = 16;

        [Header("Other")]
        // This shuold always be true
        [Tooltip("If the user shall connect when this object has awoken")]
        public bool ConnectOnAwake = true;
        [Tooltip("If the user shall join a room when they connect")]
        public bool JoinRoomOnConnect = true;

        #if UNITY_EDITOR
        public static string versionOverride = "";
        #endif

        [NonSerialized]
        public PhotonVRPlayer LocalPlayer;

        public RoomOptions options;

        public ConnectionState State = ConnectionState.Disconnected;

        private void Start()
        {
            /*PhotonNetwork.SendRate = 23;
            PhotonNetwork.SerializationRate = 30;*/
            if (Manager == null)
                Manager = this;
            else
            {
                Debug.LogError("There can't be multiple PhotonVRManagers in a scene");
                Application.Quit();
            }

            DontDestroyOnLoad(gameObject);

            if (ConnectOnAwake)
                Connect();

            if (!string.IsNullOrEmpty(PlayerPrefs.GetString("Colour")))
                Colour = JsonUtility.FromJson<Color>(PlayerPrefs.GetString("Colour"));
        }

        /// <summary>
        /// Connects to Photon using the specified AppId and VoiceAppId
        /// </summary>
        public static void Connect()
        {
            Manager.State = ConnectionState.Connecting;
            PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime = Manager.AppId;
            PhotonNetwork.PhotonServerSettings.AppSettings.AppIdVoice = Manager.VoiceAppId;
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = Manager.Region;
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log($"Connecting - AppId: {PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime} VoiceAppId: {PhotonNetwork.PhotonServerSettings.AppSettings.AppIdVoice}");
        }

        /// <summary>
        /// Disconnects from the Photon servers
        /// </summary>
        public void Disconnect()
        {
            PhotonNetwork.Disconnect();
        }

        /// <summary>
        /// Changes Photon servers
        /// </summary>
        /// <param name=Id">The new AppId</param>
        /// <param name="VoiceId">The new VoiceAppId</param>
        public static void ChangeServers(string Id, string VoiceId)
        {
            PhotonNetwork.Disconnect();
            Manager.AppId = Id;
            Manager.VoiceAppId = VoiceId;
            Connect();
        }

        /// <summary>
        /// Sets the Photon nickname to something
        /// </summary>
        /// <param name="Name">The string you want the Photon nickname to be</param>
        public static void SetUsername(string Name)
        {
            PhotonNetwork.LocalPlayer.NickName = Name;
            PlayerPrefs.SetString("Username", Name);

            if (PhotonNetwork.InRoom)
                if (Manager.LocalPlayer != null)
                    Manager.LocalPlayer.RefreshPlayerValues();
        }

        /// <summary>
        /// Sets the colour
        /// </summary>
        /// <param name="PlayerColour">The colour you want the player to be</param>
        public static void SetColour(Color PlayerColour)
        {
            Manager.Colour = PlayerColour;
            PhotonNetwork.LocalPlayer.CustomProperties["Colour"] = JsonUtility.ToJson(PlayerColour);
            PlayerPrefs.SetString("Colour", JsonUtility.ToJson(PlayerColour));

            if (PhotonNetwork.InRoom)
                if (Manager.LocalPlayer != null)
                    Manager.LocalPlayer.RefreshPlayerValues();
        }

        public override void OnConnectedToMaster()
        {
            State = ConnectionState.Connected;
            Debug.Log("Connected");

            PhotonNetwork.LocalPlayer.NickName = PlayerPrefs.GetString("Username");
            PhotonNetwork.LocalPlayer.CustomProperties["Colour"] = JsonUtility.ToJson(Colour);

            if (JoinRoomOnConnect && !PhotonNetwork.InRoom)
            {
                JoinRandomRoom(DefaultQueue, DefaultRoomLimit);
                JoinRoomOnConnect = false;
            }
            else if(SceneManager.GetActiveScene().name.ToString() == "Yeti")
            {
                PhotonNetwork.JoinOrCreateRoom(PlayerPrefs.GetString("YetiCode") +"Yeti" + SceneManager.GetActiveScene().name.ToString(), options, TypedLobby.Default);
            }

        }

        
        /// <summary>
        /// Gets the connection state
        /// </summary>
        /// <returns>The current connection state</returns>
        public static ConnectionState GetConnectionState()
        {
            return Manager.State;
        }

        /// <summary>
        /// Switches scenes and joins a new room
        /// </summary>
        /// <param name="SceneIndex"></param>
        /// <param name="MaxPlayers"></param>
        public static void SwitchScenes(int SceneIndex, int MaxPlayers)
        {
            SceneManager.LoadScene(SceneIndex);
            JoinRandomRoom(SceneIndex.ToString(), MaxPlayers);
        }

        /// <summary>
        /// Switches scenes and joins a new room
        /// </summary>
        /// <param name="SceneIndex"></param>
        public static void SwitchScenes(int SceneIndex)
        {
            SceneManager.LoadScene(SceneIndex);
            JoinRandomRoom(SceneIndex.ToString(), Manager.DefaultRoomLimit);
        }

        /// <summary>
        /// Joins a room
        /// </summary>
        public static void JoinRandomRoom(string Queue, int MaxPlayers) => _JoinRandomRoom(Queue, MaxPlayers);

        /// <summary>
        /// Joins a room
        /// </summary>
        public static void JoinRandomRoom(string Queue) => _JoinRandomRoom(Queue, Manager.DefaultRoomLimit);

        private static void _JoinRandomRoom(string Queue, int MaxPlayers)
        {
            Manager.State = ConnectionState.JoiningRoom;
            ExitGames.Client.Photon.Hashtable hastable = new ExitGames.Client.Photon.Hashtable();
            hastable.Add("queue", Queue);
            hastable.Add("version", 
                #if UNITY_EDITOR 
                Application.isEditor ? versionOverride != "" ? versionOverride : Application.version : 
                #endif 
                Application.version);

            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = (byte)MaxPlayers;
            roomOptions.IsVisible = true;
            roomOptions.IsOpen = true;
            roomOptions.CustomRoomProperties = hastable;
            roomOptions.CustomRoomPropertiesForLobby = new string[] { "queue", "version" };
            Manager.options = roomOptions;

            PhotonNetwork.JoinRandomRoom(hastable, (byte)roomOptions.MaxPlayers, MatchmakingMode.RandomMatching, null, null, null);
            Debug.Log($"Joining random with type {hastable["queue"]}");
        }
        public static void JoinOrCreateRandomRoom(string Queue, int MaxPlayers) => _JoinOrCreateRandomRoom(Queue, MaxPlayers);

        /// <summary>
        /// Joins a room
        /// </summary>
        public static void JoinOrCreateRandomRoom(string Queue) => _JoinOrCreateRandomRoom(Queue, Manager.DefaultRoomLimit);

        private static void _JoinOrCreateRandomRoom(string Queue, int MaxPlayers)
        {
            Manager.State = ConnectionState.JoiningRoom;
            ExitGames.Client.Photon.Hashtable hastable = new ExitGames.Client.Photon.Hashtable();
            hastable.Add("queue", Queue);
            hastable.Add("version", Application.version);

            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = (byte)MaxPlayers;
            roomOptions.IsVisible = true;
            roomOptions.IsOpen = true;
            //roomOptions.CustomRoomProperties = hastable;
            roomOptions.CustomRoomPropertiesForLobby = new string[] { "queue", "version" };
            Manager.options = roomOptions;

            PhotonNetwork.JoinRandomOrCreateRoom(hastable, roomOptions.MaxPlayers, MatchmakingMode.RandomMatching, null, null, new System.Random().Next(99999).ToString());
            Debug.Log($"Joining random with type {hastable["queue"]}");
        }

        /// <summary>
        /// Joins a private room
        /// </summary>
        /// <param name="RoomId">The room code</param>
        /// <param name="MaxPlayers">The maximum amount of players that can be in the room</param>
        public static void JoinPrivateRoom(string RoomId, int MaxPlayers) => _JoinPrivateRoom(RoomId, MaxPlayers);

        /// <summary>
        /// Joins a private room
        /// </summary>
        /// <param name="RoomId">The room code</param>
        /// <param name="MaxPlayers">The maximum amount of players that can be in the room</param>
        public static void JoinPrivateRoom(string RoomId) => _JoinPrivateRoom(RoomId, Manager.DefaultRoomLimit);

        public static void _JoinPrivateRoom(string RoomId, int MaxPlayers)
        {
            PhotonNetwork.JoinOrCreateRoom(RoomId, new RoomOptions()
            {
                IsVisible = true,
                IsOpen = true,
                MaxPlayers = (byte)MaxPlayers
            }, null, null);
            Debug.Log($"Joining a private room: {RoomId}");
        }


        public override void OnJoinedRoom()
        {
            Debug.Log("Joined a room");
            State = ConnectionState.InRoom;
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);
            State = ConnectionState.Disconnected;
            Debug.Log("Disconnected from server");
        }

        public override void OnJoinRandomFailed(short returnCode, string message) => HandleJoinError(returnCode, message);

        private void HandleJoinError(short returnCode, string message)
        {
            Debug.Log("Failed to join room - creating a new one" + " | " + message + " | " + returnCode);

            string roomCode = CreateRoomCode();
            Debug.Log($"Joining {roomCode}");
            PhotonNetwork.CreateRoom(roomCode, options, null, null);
        }


        /// <summary>
        /// Generates a random room code
        /// </summary>
        /// <returns>A room code</returns>
        public string CreateRoomCode()
        {
            return new System.Random().Next(99999999).ToString();
        }
        
    }
    
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        JoiningRoom,
        InRoom
    }
}