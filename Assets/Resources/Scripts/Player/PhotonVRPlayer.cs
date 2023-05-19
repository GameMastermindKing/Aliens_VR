using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using Photon.Voice;
using PlayFab.ClientModels;
using PlayFab;
using ExitGames.Client.Photon;
using System.Collections;
using Photon.Voice.PUN;
using PlayFab.ServerModels;
using System.Threading.Tasks;

namespace Photon.VR.Player
{
    public class PhotonVRPlayer : MonoBehaviourPunCallbacks
    {
        public enum SoundType
        {
            Snow,
            Metal,
            Meat,
            Wood,
            Rock
        }

        public enum CosmeticsMode
        {
            Enabled,
            Disabled
        }

        public static CosmeticsMode cosmetics = CosmeticsMode.Enabled;

        [Header("Objects")]
        public static PhotonVRPlayer instance;
        public PhotonRoyalePlayer royalePlayer;
        public Transform Head;
        public Transform LeftHand;
        public Transform RightHand;
        public Collider[] mechColliders;
        public PhotonView playerPhotonView;
        public GorillaLocomotion.Player player;
        public AudioSource PlayersAudio;
        public Texture2D normPengTexture;
        public Texture2D bluePengTexture;
        public Vector3[] nameScale;

        [Tooltip("The objects that will get the colour of the player applied to them")]
        public List<Renderer> ColourObjects = new List<Renderer>();

        [Header("Other")]
        public TextMeshPro NameText;
        public bool HideLocalPlayer = true;
        public bool localMechOn;
        bool deepVoice;
        public bool jailed;
        public bool yeti;
        public GameObject[] mechs;
        public GameObject YetiObject;
        public GameObject PenguinObject;
        public string playfabId;
        public List<GameObject> AllFrozen;
        public List<GameObject> AllNotFrozen;

        [Header("Sounds")]
        public AudioSource FreezeSound;
        public AudioSource UnFreezeSound;
        public int Players;

        private void Awake()
        {
            playerPhotonView = this.GetComponent<PhotonView>();
            if (photonView.IsMine)
            {
                instance = this;
                PhotonVRManager.Manager.LocalPlayer = this;
                if (HideLocalPlayer)
                {
                    Head.gameObject.SetActive(false);
                    RightHand.gameObject.SetActive(false);
                    LeftHand.gameObject.SetActive(false);
                }

            }

            if (NameText != null)
            {
                if (NameValidation.instance.IsNameValid(photonView.Owner.NickName))
                {
                    NameText.text = photonView.Owner.NickName;
                }
                else
                {
                    NameText.text = NameValidation.instance.GetFixName();
                }
            }

            foreach (Renderer renderer in ColourObjects)
            {
                renderer.material.color = JsonUtility.FromJson<Color>((string)photonView.Owner.CustomProperties["Colour"]);
            }

            PhotonNetwork.LocalPlayer.TagObject = this.gameObject;

            // It will delete automatically when you leave the room
            DontDestroyOnLoad(gameObject);

        }

        IEnumerator Start()
        {
            yield return null;

        }

        private void Update()
        {
            Players = PhotonNetwork.CurrentRoom.PlayerCount;
            if (player == null && Time.time > 3.0f && photonView.IsMine)
            {
                player = FindObjectOfType<GorillaLocomotion.Player>();
                player.playerRigidBody.isKinematic = false;
                player.playerRigidBody.useGravity = true;
            }

            NameText.transform.localScale = localMechOn ? nameScale[1] : nameScale[0];

            if (PhotonRoyalePlayer.SceneIsRoyaleMode())
            {
                PlayerCosmetics cosmeticsScript = GetComponent<PlayerCosmetics>();
                for (int i = 0; i < PlayerCosmetics.instance.objectsToChangeIfMine.Count; i++)
                {
                    cosmeticsScript.objectsToChangeIfMine[i].layer = cosmetics == CosmeticsMode.Enabled && !photonView.IsMine ? cosmeticsScript.normalLayer : cosmeticsScript.layerToUse;
                }

                if (royalePlayer == null)
                {
                    royalePlayer = GetComponent<PhotonRoyalePlayer>();
                }

                if (royalePlayer != null && royalePlayer.teamID > -1 && royalePlayer.teamID < 15)
                {
                    NameText.color = PhotonRoyaleLobby.instance.teamColors[royalePlayer.teamID];
                }
            }

            if (player != null && !player.useObserver)
            {
                if (photonView.IsMine)
                {
                    Head.transform.position = PhotonVRManager.Manager.Head.transform.position;
                    Head.transform.rotation = PhotonVRManager.Manager.Head.transform.rotation;

                    RightHand.transform.position = PhotonVRManager.Manager.RightHand.transform.position;
                    RightHand.transform.rotation = PhotonVRManager.Manager.RightHand.transform.rotation;

                    LeftHand.transform.position = PhotonVRManager.Manager.LeftHand.transform.position;
                    LeftHand.transform.rotation = PhotonVRManager.Manager.LeftHand.transform.rotation;
                }
                if (deepVoice)
                {
                    PlayersAudio.pitch = 0.72f;
                }
                else
                {
                    PlayersAudio.pitch = 1f;
                }
            }
        }

        public void ToggleMechColliders(bool onOff)
        {
            for (int i = 0; i < mechColliders.Length; i++)
            {
                mechColliders[i].enabled = onOff;
            }
        }

        public void RefreshPlayerValues() => photonView.RPC("RPCRefreshPlayerValues", RpcTarget.All);

        [PunRPC]
        private void RPCRefreshPlayerValues()
        {
            playfabId = (string)photonView.Owner.CustomProperties["plafabId2"];
            if (NameText != null)
            {
                if (NameValidation.instance.IsNameValid(photonView.Owner.NickName))
                {
                    NameText.text = photonView.Owner.NickName;
                }
                else
                {
                    NameText.text = NameValidation.instance.GetFixName();
                }
                PopUpComputer.instance.UpdateMutePlayers();
                PopUpComputer.instance.UpdateInvitePlayers();
            }
            foreach (Renderer renderer in ColourObjects)
            {
                renderer.material.color = JsonUtility.FromJson<Color>((string)photonView.Owner.CustomProperties["Colour"]);
            }
        }

        [PunRPC]
        public void color()
        {
            GetComponentInChildren<SkinnedMeshRenderer>().material.mainTexture = bluePengTexture;
            print("Color Blue");
        }

        [PunRPC]
        public void Muted()
        {
            PlayFab.ClientModels.StatisticUpdate update = new PlayFab.ClientModels.StatisticUpdate();
            update.StatisticName = "Times Muted";
            update.Value = 1;

            PlayFab.ClientModels.UpdatePlayerStatisticsRequest request = new PlayFab.ClientModels.UpdatePlayerStatisticsRequest();
            request.Statistics = new List<PlayFab.ClientModels.StatisticUpdate>();
            request.Statistics.Add(update);

            PlayFabClientAPI.UpdatePlayerStatistics(request, OnUpdateStatistics, OnUpdateStatisticsError);
            Debug.Log("Muted");
        }

        [PunRPC]
        public void UnMuted()
        {
            PlayFab.ClientModels.StatisticUpdate update = new PlayFab.ClientModels.StatisticUpdate();
            update.StatisticName = "Times Muted";
            update.Value = -1;

            PlayFab.ClientModels.UpdatePlayerStatisticsRequest request = new PlayFab.ClientModels.UpdatePlayerStatisticsRequest();
            request.Statistics = new List<PlayFab.ClientModels.StatisticUpdate>();
            request.Statistics.Add(update);

            PlayFabClientAPI.UpdatePlayerStatistics(request, OnUpdateStatistics, OnUpdateStatisticsError);
            Debug.Log("Unmuted");
        }

        void OnUpdateStatistics(PlayFab.ClientModels.UpdatePlayerStatisticsResult result)
        {

        }

        void OnUpdateStatisticsError(PlayFabError error)
        {
            Debug.LogError("Update Statistics failed: " + error.ErrorMessage);
        }

        [PunRPC]
        public void ActivateMech(int mechID)
        {
            PenguinObject.SetActive(false);
            YetiObject.SetActive(false);
            localMechOn = true;

            for (int i = 0; i < mechs.Length; i++)
            {
                mechs[i].SetActive(i == mechID);
            }

            if (photonView.IsMine)
            {
                GorillaLocomotion.Player.Instance.transform.localScale = new Vector3(7.5f, 7.5f, 7.5f);
            }
        }

        [PunRPC]
        public void DeactivateMech()
        {
            PenguinObject.SetActive(true);
            for (int i = 0; i < mechs.Length; i++)
            {
                mechs[i].SetActive(false);
            }
            localMechOn = false;

            if (photonView.IsMine)
            {
                GorillaLocomotion.Player.Instance.transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }

        [PunRPC]
        public void PlayFootstep(int footstepID)
        {
            if (!photonView.IsMine)
            {
                GorillaLocomotion.Player player = FindObjectOfType<GorillaLocomotion.Player>();
                player.PlayFootstepSound((SoundType)footstepID, LeftHand.position);
            }
        }

        public override void OnPlayerEnteredRoom(Realtime.Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);
            if (yeti)
            {
                playerPhotonView.RPC("BeYeti", newPlayer);
            }

            if (jailed)
            {
                playerPhotonView.RPC("Jailed", newPlayer);
            }

            for (int i = 0; i < mechs.Length; i++)
            {
                if (mechs[i].activeSelf)
                {
                    photonView.RPC("ActivateMech", newPlayer, i);
                    break;
                }
            }
        }
    }
}