using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class RoyaleNonStackedGrabbable : MonoBehaviourPunCallbacks
{
    public PhotonRoyalePlayer.InventoryItem grabbableItem;
    public Rigidbody grabbableRigidbody;
    public Renderer[] grabbableRenderers;
    public PhotonRoyalePlayer royalePlayer;
    public Transform[] hands;

    public int playerID;
    public bool held;
    public bool rightHand;
    public bool activeInHand = false;

    public Color neutralColor;
    public Color highlightColor;

    public virtual IEnumerator Start()
    {
        GorillaLocomotion.Player player = FindObjectOfType<GorillaLocomotion.Player>();
        hands = new Transform[2];
        hands[0] = player.leftHandTransform;
        hands[1] = player.rightHandTransform;

        yield return StartCoroutine(GetPlayer());
    }

    IEnumerator GetPlayer()
    {
        while (royalePlayer == null)
        {
            PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].photonView.IsMine)
                {
                    royalePlayer = players[i];
                    break;
                }
            }
            yield return new WaitForSeconds(1.0f);
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine(GetPlayer());
    }
}
