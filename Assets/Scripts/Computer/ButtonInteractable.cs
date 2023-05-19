using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Photon.VR;
using Photon.VR.Player;

public class ButtonInteractable : PopUpInteractable
{
    public enum ButtonType
    {
        Option1,
        Option2,
        Option3,
        Backspace,
        Join_Pub,
        ChangeToUsername,
        ChangeToPrivateRoom,
        Leave,
        Enter,
        ChangeToMutePlayers,
        ToggleMute,
        ExitObserver,
        JoinPublicCode,
        InvitePlayer,
        ChangeToInvitePlayers,
        OpenCosmeticsMenu,
    }

    public ButtonType buttonType;
    public int buttonID = 0;

    public override void OnActivated()
    {
        base.OnActivated();
        switch (buttonType)
        {
            case ButtonType.Option1:
            {
                EnableSmoothTurn();
                break;
            }

            case ButtonType.Option2:
            {
                EnableSnapTurn();
                break;
            }

            case ButtonType.Option3:
            {
                DisableOtherTurnModes();
                break;
            }

            case ButtonType.Backspace:
            {
                master.BackspaceInput();
                break;
            }

            case ButtonType.Join_Pub:
            {
                StartCoroutine(PopUpComputer.instance.JoinPublicRoom());
                break;
            }

            case ButtonType.ChangeToUsername:
            {
                master.ChangeMode(PopUpComputer.CurrentMode.Username);
                break;
            }

            case ButtonType.ChangeToPrivateRoom:
            {
                master.ChangeMode(PopUpComputer.CurrentMode.RoomCode);
                break;
            }

            case ButtonType.ChangeToMutePlayers:
            {
                master.ChangeMode(PopUpComputer.CurrentMode.MutePlayers);
                break;
            }

            case ButtonType.ChangeToInvitePlayers:
            {
                master.ChangeMode(PopUpComputer.CurrentMode.InvitePlayers);
                break;
            }

            case ButtonType.Leave:
            {
                StartCoroutine(PopUpComputer.instance.JoinPublicRoom());
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex != 0)
                {
                    Invoke("LoadLobbyScene", 1.0f);
                }
                break;
            }

            case ButtonType.Enter:
            {
                master.ActivateMode();
                break;
            }

            case ButtonType.ToggleMute:
            {
                master.ToggleMutePlayer(buttonID);
                break;
            }

            case ButtonType.InvitePlayer:
            {
                master.InvitePlayer(buttonID);
                break;
            }

            case ButtonType.ExitObserver:
            {
                GorillaLocomotion.Player.Instance.SwitchToNormal();
                break;
            }

            case ButtonType.JoinPublicCode:
            {
                if (master.roomCodeText.text.Length > 0)
                {
                    if (!NameValidation.instance.IsNameValid(master.roomCodeText.text))
                    {
                        Application.Quit();
                    }
                    StartCoroutine(master.JoinSpecificPublicRoom());
                }
                break;
            }

            case ButtonType.OpenCosmeticsMenu:
            {
                if (PopUpComputer.currentMode != PopUpComputer.CurrentMode.Cosmetics)
                {
                    PopUpComputer.currentMode = PopUpComputer.CurrentMode.Cosmetics;
                    master.cosmeticsScript.Activate();
                }
                break;
            }
        }
    }

    void Start()
    {
        if (buttonType == ButtonType.Option2 && PlayerPrefs.GetInt("SnapTurn", 0) == 1)
        {
            EnableSnapTurn();
        }
    }

    void EnableSmoothTurn()
    {
        //FindObjectOfType<DeviceBasedContinuousTurnProvider>().enabled = true;
        //FindObjectOfType<DeviceBasedSnapTurnProvider>().enabled = false;
        PlayerPrefs.SetInt("NoTurn", 0);
        PlayerPrefs.SetInt("SnapTurn", 0);
        PlayerPrefs.SetInt("SmoothTurn", 1);
    }

    void EnableSnapTurn()
    {
        //FindObjectOfType<DeviceBasedContinuousTurnProvider>().enabled = false;
        //FindObjectOfType<DeviceBasedSnapTurnProvider>().enabled = true;
        PlayerPrefs.SetInt("NoTurn", 0);
        PlayerPrefs.SetInt("SnapTurn", 1);
        PlayerPrefs.SetInt("SmoothTurn", 0);
    }

    void DisableOtherTurnModes()
    {
        //FindObjectOfType<DeviceBasedContinuousTurnProvider>().enabled = false;
        //FindObjectOfType<DeviceBasedSnapTurnProvider>().enabled = false;
        PlayerPrefs.SetInt("NoTurn", 1);
        PlayerPrefs.SetInt("SnapTurn", 0);
        PlayerPrefs.SetInt("SmoothTurn", 0);
    }

    void LoadLobbyScene()
    {
        Photon.Pun.PhotonNetwork.Disconnect();
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}
