using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonStorm : MonoBehaviourPunCallbacks
{
    public static PhotonStorm instance;
    public Transform stormAnchor;
    public Transform stormObject;
    public Renderer stormRenderer;
    public GorillaLocomotion.Player player;
    public PhotonRoyalePlayer photonPlayer;
    public AudioSource source;
    public ParticleSystem stormParticles;
    public Vector3 startPos;
    public int startDamage = 2;
    public float maxCenterDist;
    public float minMove = 10.0f;
    public float maxMove = 80.0f;
    public float startSize;
    public float sizeModPerShift; 
    public float timeToMove = 30.0f;
    public float stormFury = 1.0f;
    public float timeBetweenMoves = 60.0f;
    public Vector2 stormTexOffset = new Vector2(1.0f, 0.0f);

    int curHealthDamage = 2;
    int damageIncrease = 4;
    float nextMove = 0.0f;
    float curSize = 200.0f;
    float lastSize = 200.0f;
    float nextSize = 200.0f;
    float lastTick = 0.0f;
    float tickFrequency = 0.5f;
    float normalVolume;
    float percentageMoved;
    Vector3 center;
    Vector3 yNeutralCenter;
    Vector3 yNeutralPlayer;
    Vector3 lastCenter;
    Vector3 targetCenter;
    Vector3 temp;

    public void Start()
    {
        instance = this;
        normalVolume = source.volume;
        curSize = startSize;
        lastSize = startSize;
        nextSize = startSize;
        lastCenter = startPos;
        center = startPos;
        targetCenter = startPos;
    }

    public void Update()
    {
        stormRenderer.material.SetTextureOffset("_BaseMap", stormRenderer.material.GetTextureOffset("_BaseMap") + stormTexOffset * stormFury * Time.deltaTime);

        if (player == null)
        {
            player = FindObjectOfType<GorillaLocomotion.Player>();
        }

        if (photonPlayer == null)
        {
            PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].photonView.IsMine)
                {
                    photonPlayer = players[i];
                    break;
                }
            }
        }
        else
        {
            temp = (player.headCollider.transform.position - center);
            temp.y = 0.0f;
            temp = temp.normalized;
            temp = center + temp * curSize;
            temp.y = player.headCollider.transform.position.y;
            if (Vector3.Distance(temp, center) < Vector3.Distance(player.headCollider.transform.position, center))
            {
                temp = player.headCollider.transform.position;
            }

            source.transform.position = temp;
            source.volume = (photonPlayer.alive && PhotonRoyaleLobby.instance.activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber) ? normalVolume : 0.0f);
            if (PhotonRoyaleLobby.instance.gameStarted)
            {
                if (!PhotonRoyaleLobby.instance.activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber) && photonView.IsMine)
                {
                    PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (PhotonRoyaleLobby.instance.activePlayersList.Contains(players[i].photonView.ControllerActorNr))
                        {
                            photonView.TransferOwnership(players[i].photonView.Controller);
                            break;
                        }
                    }
                }
                else
                {
                    if (Time.time > nextMove)
                    {
                        percentageMoved = Time.time - nextMove > timeToMove ? 1.0f : (Time.time - nextMove) / timeToMove;
                        center = Vector3.Lerp(lastCenter, targetCenter, percentageMoved);
                        curSize = Mathf.Lerp(lastSize, nextSize, percentageMoved);

                        stormAnchor.position = center;
                        stormObject.localScale = new Vector3(curSize, stormObject.localScale.y, curSize);

                        if (Time.time > nextMove + timeToMove && photonView.IsMine)
                        {
                            MoveStorm(false);
                        }
                    }

                    if (PhotonRoyaleLobby.instance.activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber) && photonPlayer.alive)
                    {
                        yNeutralCenter = center;
                        yNeutralCenter.y = 0f;

                        yNeutralPlayer = player.headCollider.transform.position;
                        yNeutralPlayer.y = 0f;

                        if (Vector3.Distance(yNeutralCenter, yNeutralPlayer) > curSize)
                        {
                            if (Time.time - lastTick > tickFrequency)
                            {
                                photonPlayer.freezeUI.alpha = 1.0f;
                                lastTick = Time.time;
                                photonPlayer.photonView.RPC("TookStormHit", RpcTarget.All, photonPlayer.health - curHealthDamage);
                            }
                        }
                    }
                }
            }
        }
    }

    public void MoveStorm(bool first)
    {
        curHealthDamage = first ? startDamage : curHealthDamage + damageIncrease;
        nextMove = Time.time + timeBetweenMoves;
        lastCenter = center;
        lastSize = curSize;

        targetCenter = lastCenter + new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f)).normalized * Random.Range(minMove, maxMove);
        if (Vector3.Distance(targetCenter, startPos) > maxCenterDist)
        {
            targetCenter = startPos + (targetCenter - startPos).normalized * maxCenterDist;
        }

        nextSize = curHealthDamage < 25 ? lastSize * sizeModPerShift : lastSize * 0.1f;
        photonView.RPC("AnnounceMove", RpcTarget.Others, lastCenter, lastSize, nextSize, targetCenter, curHealthDamage);
    }

    [PunRPC]
    public void AnnounceMove(Vector3 lastCenterData, float lastSizeData, float nextSizeData, Vector3 nextCenterData, int newDamage)
    {
        curHealthDamage = newDamage;
        nextMove = Time.time + timeBetweenMoves;
        lastCenter = lastCenterData;
        lastSize = lastSizeData;
        nextSize = nextSizeData;
        targetCenter = nextCenterData;
    }

    [PunRPC]
    public void ResetStormToStart()
    {
        curHealthDamage = startDamage;
        curSize = startSize;
        lastSize = startSize;
        nextSize = startSize;
        lastCenter = startPos;
        center = startPos;
        targetCenter = startPos;
        percentageMoved = 0.0f;

        stormAnchor.position = center;
        stormObject.localScale = new Vector3(curSize, stormObject.localScale.y, curSize);
    }

    [PunRPC]
    public void SyncStorm(Vector3 lastCenterData, float lastSizeData, float nextSizeData, Vector3 targetCenterData, float nextTimeData, int damage)
    {
        lastCenter = lastCenterData;
        lastSize = lastSizeData;
        nextSize = nextSizeData;
        targetCenter = targetCenterData;
        nextMove = Time.time + nextTimeData;
        curHealthDamage = damage;

        if (Time.time > nextMove)
        {
            percentageMoved = Time.time - nextMove > timeToMove ? 1.0f : (Time.time - nextMove) / timeToMove;
            center = Vector3.Lerp(lastCenter, targetCenter, percentageMoved);
            curSize = Mathf.Lerp(lastSize, nextSize, percentageMoved);

            stormAnchor.position = center;
            stormObject.localScale = new Vector3(curSize, stormObject.localScale.y, curSize);
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        ResetStormToStart();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (photonView.IsMine)
        {
            photonView.RPC("SyncStorm", newPlayer, lastCenter, lastSize, nextSize, targetCenter, nextMove - Time.time, curHealthDamage);
        }
    }
}
