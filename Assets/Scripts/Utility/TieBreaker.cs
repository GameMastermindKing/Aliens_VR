using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TieBreaker : MonoBehaviourPunCallbacks
{
    public static List<int> breakers = new List<int>();

    public IEnumerator Start()
    {
        yield return new WaitForSeconds(20.0f);
        if (breakers.Count < 10 && !breakers.Contains(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            breakers.Add(PhotonNetwork.LocalPlayer.ActorNumber);
            photonView.RPC("UpdateBreakerList", RpcTarget.All, new object[] { breakers });
        }
    }

    [PunRPC]
    public void UpdateBreakerList(int[] newBreakers)
    {
        breakers = new List<int>(newBreakers);
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        if (photonView.IsMine)
        {
            PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
            for (int i = breakers.Count - 1; i > -1; i--)
            {
                bool found = false;
                for (int j = 0; j < players.Length; j++)
                {
                    if (players[j].photonView.ControllerActorNr == breakers[i])
                    {
                        found = true;
                    }
                }

                if (!found)
                {
                    breakers.RemoveAt(i);
                }
            }
            photonView.RPC("UpdateBreakerList", RpcTarget.All, new object[] { breakers });
        }
    }
}
