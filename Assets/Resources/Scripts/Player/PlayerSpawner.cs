using UnityEngine;
using System.Collections;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using PlayFab.ClientModels;
using PlayFab;
using ExitGames.Client.Photon;

namespace Photon.VR.Player
{
    public class PlayerSpawner : MonoBehaviourPunCallbacks
    {

        [Tooltip("The location of the player prefab")]
        public string PrefabLocation = "PhotonVR/Player";
        [HideInInspector]
        public bool isPlayerSpwaned;
        [HideInInspector]
        public GameObject playerTemp;
        public bool isGameOfTag;

        public string plafabId;
       
        private void Awake() => DontDestroyOnLoad(gameObject);

        public override void OnJoinedRoom()
        {
            isPlayerSpwaned = true;
            playerTemp = PhotonNetwork.Instantiate(PrefabLocation, Vector3.zero, Quaternion.identity);

            StartCoroutine(WaitForPlayFab());

            _myCustomProperties = PhotonNetwork.LocalPlayer.CustomProperties;
        }

        IEnumerator WaitForPlayFab()
        {
            while (!PlayFabManager.instance.PlayFabPlayerLoggedIn())
            {
                yield return new WaitForSeconds(0.5f);
            }
            GetAccountInfoRequest InfoRequest = new GetAccountInfoRequest();
            PlayFabClientAPI.GetAccountInfo(InfoRequest, AccountInfoSuccess, OnError);
        }

        /*public override void OnPlayerEnteredRoom(Realtime.Player newPlayer)
        {
            GetAccountInfoRequest InfoRequest = new GetAccountInfoRequest();
            PlayFabClientAPI.GetAccountInfo(InfoRequest, AccountInfoSuccess, OnError);

            _myCustomProperties = PhotonNetwork.LocalPlayer.CustomProperties;
        }*/

        
        public override void OnLeftRoom()
        {
            isPlayerSpwaned = false;
            PhotonNetwork.Destroy(playerTemp);
        }

        public void AccountInfoSuccess(GetAccountInfoResult result1)
        {
            plafabId = result1.AccountInfo.PlayFabId;
            Debug.Log(plafabId);
            RefreshStats(result1.AccountInfo.PlayFabId, false);
        }

        void OnError(PlayFabError error)
        {

        }
        
        ExitGames.Client.Photon.Hashtable _myCustomProperties;
        public void RefreshStats( string id, bool deepVoice)
        {

            _myCustomProperties["plafabId2"] = id;
            PhotonNetwork.LocalPlayer.SetCustomProperties(_myCustomProperties);

            playerTemp.GetComponent<PhotonVRPlayer>().RefreshPlayerValues();


        }
        
    }
}