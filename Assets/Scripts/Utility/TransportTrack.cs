using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TransportTrack : MonoBehaviourPunCallbacks
{
    public Transform trackGuide;
    public Transform[] trackWaypoints;
    public Transform[] liftSeats;

    float guidePercentage = 0.0f;
    public float percentagePerSecond = 0.03f;
    public float guidePercentagePerSecond = 0.5f;
    public float[] percentageForSeats;

    float[] percentageForWaypoints;
    public bool drawGizmos = true;

    Vector3 tempLine;

    public void Start()
    {
        float totalDistance = 0.0f;
        for (int i = 0; i < trackWaypoints.Length - 1; i++)
        {
            totalDistance += Vector3.Distance(trackWaypoints[i].position, trackWaypoints[i + 1].position);
        }

        float curTotal = 0.0f;
        percentageForWaypoints = new float[trackWaypoints.Length];
        percentageForWaypoints[0] = 0.0f;
        percentageForWaypoints[percentageForWaypoints.Length - 1] = 1.0f;
        for (int i = 0; i < trackWaypoints.Length - 1; i++)
        {
            curTotal += Vector3.Distance(trackWaypoints[i].position, trackWaypoints[i + 1].position);
            percentageForWaypoints[i + 1] = curTotal / totalDistance;
        }
    }

    public void Update()
    {
        guidePercentage = (guidePercentage + guidePercentagePerSecond * Time.deltaTime) % 1.0f;
        trackGuide.position = GetPositionForPercentage(guidePercentage);

        for (int i = 0; i < percentageForSeats.Length; i++)
        {
            percentageForSeats[i] = (percentageForSeats[i] + percentagePerSecond * Time.deltaTime) % 1.0f;
            liftSeats[i].position = GetPositionForPercentage(percentageForSeats[i]);
        }
    }

    public void OnDrawGizmos()
    {
        if (drawGizmos)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < trackWaypoints.Length - 1; i++)
            {
                Gizmos.DrawLine(trackWaypoints[i].position, trackWaypoints[i + 1].position);
            }

            Gizmos.color = Color.blue;
            for (int i = 0; i < trackWaypoints.Length; i++)
            {
                tempLine = trackWaypoints[i].position;
                tempLine.y = -53.4f;
                Gizmos.DrawWireSphere(trackWaypoints[i].position, 2.0f);
                Gizmos.DrawLine(trackWaypoints[i].position - Vector3.up * 2.0f, tempLine);
            }
        }
    }

    public Vector3 GetPositionForPercentage(float percentage)
    {
        for (int i = 0; i < percentageForWaypoints.Length; i++)
        {
            if (percentageForWaypoints[i] > percentage)
            {
                return Vector3.Lerp(trackWaypoints[i - 1].position, trackWaypoints[i].position, 
                    (percentage - percentageForWaypoints[i - 1]) / (percentageForWaypoints[i] - percentageForWaypoints[i - 1]));
            }
        }
        return trackWaypoints[trackWaypoints.Length - 1].position;
    }

    [PunRPC]
    public void SyncState(float[] percentages)
    {
        percentageForSeats = percentages;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (photonView.IsMine)
        {
            photonView.RPC("SyncState", RpcTarget.Others, percentageForSeats);
        }
    }
}
