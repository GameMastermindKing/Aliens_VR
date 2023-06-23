using System.Collections;
using System.Collections.Generic;
using Oculus.Platform;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.VR.Player;
using TMPro;

public class ReportingCallback : MonoBehaviour
{
    [System.Serializable]
    public class ReportPlayerGroup
    {
        public PhotonVRPlayer userLink;
        public TextMeshProUGUI userNameText;
        public Toggle reportToggle;
    }

    public static ReportingCallback instance;
    public ReportPlayerGroup[] reportPlayers;
    public GameObject reportingInterface;
    public TextMeshProUGUI roomText;
    public float distance = 4.0f;

    bool takeLate;
    Vector3 tempVec;

    public void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void Update()
    {
        tempVec = GorillaLocomotion.Player.Instance.headCollider.transform.forward;
        reportingInterface.transform.position = GorillaLocomotion.Player.Instance.headCollider.transform.position + tempVec * distance;
        reportingInterface.transform.LookAt(GorillaLocomotion.Player.Instance.headCollider.transform, GorillaLocomotion.Player.Instance.headCollider.transform.up);
        reportingInterface.transform.Rotate(Vector3.up * 180.0f, Space.Self);
    }

    public void OnReportButtonIntentNotif(Message<string> message)
    {
        if (!message.IsError)
        {
            ActivateReportPopup();
            AbuseReport.ReportRequestHandled(ReportRequestResponse.Handled);
        }
    }

    public void ActivateReportPopup()
    {
        reportingInterface.SetActive(true);
        string message = "Report - " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " - " + 
            PhotonNetwork.CurrentRoom.Name.Replace(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "").Replace("|", "");
        roomText.text = message;

        PhotonVRPlayer[] allPlayers = FindObjectsOfType<PhotonVRPlayer>();
        for (int i = 0; i < allPlayers.Length; i++)
        {
            bool found = false;
            for (int j = 0; j < reportPlayers.Length; j++)
            {
                if (reportPlayers[j].userLink == allPlayers[i])
                {
                    reportPlayers[j].userNameText.text = reportPlayers[j].userLink.NameText.text;
                    found = true;
                    break;
                }
            }
            
            if (!found && !allPlayers[i].photonView.IsMine)
            {
                for (int j = 0; j < reportPlayers.Length; j++)
                {
                    if (reportPlayers[j].userLink == null)
                    {
                        reportPlayers[j].userLink = allPlayers[i];
                        reportPlayers[j].reportToggle.isOn = false;
                        break;
                    }
                }
            }
        }

        for (int i = 0; i < reportPlayers.Length; i++)
        {
            if (reportPlayers[i].userLink != null)
            {
                reportPlayers[i].userNameText.transform.parent.gameObject.SetActive(true);
                reportPlayers[i].userNameText.text = reportPlayers[i].userLink.NameText.text;
            }
            else
            {
                reportPlayers[i].userNameText.transform.parent.gameObject.SetActive(false);
            }
        }
    }

    public void SubmitReport()
    {
        takeLate = true;
    }

    public void LateUpdate()
    {
        if (takeLate)
        {
            takeLate = false;
            reportingInterface.SetActive(false);
        }
    }
}
