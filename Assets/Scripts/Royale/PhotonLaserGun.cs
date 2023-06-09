using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.XR;

public class PhotonLaserGun : MonoBehaviourPunCallbacks
{
    public PhotonRoyalePlayer.InventoryItem gunItem;
    public Transform[] hands;
    public Transform barrel;
    public Transform bulletNeutral;
    public Transform looker;
    public Renderer[] gunRenderers;
    public Rigidbody gunRigidbody;
    public Collider gunCollider;
    public List<PhotonBullet> bulletPool;
    public GameObject bullet;
    public AudioSource shotSound;
    public AudioSource reloadSound;
    public AudioSource clickSound;
    public Color neutralColor;
    public Color highlightColor;

    public ScriptableGunConfiguration gunData;
    public float hapticWaitSeconds = 0.05f;
    public float vibrationAmmount = 0.15f;
    public float startReload = 0.0f;
    public int curBullets = 1;
    public int gunID = 0;
    float nextFire = 0.0f;
    
    public static bool gotCasts = false;
    public bool rightHand;
    public bool reloading = false;

    PhotonBullet curBullet;
    PhotonRoyalePlayer royalePlayer;

    public IEnumerator Start()
    {
        GorillaLocomotion.Player player = FindObjectOfType<GorillaLocomotion.Player>();
        hands = new Transform[2];
        hands[0] = player.leftHandTransform;
        hands[1] = player.rightHandTransform;
        curBullets = gunData.ammo;

        if (clickSound == null)
        {
            clickSound = reloadSound.transform.parent.GetChild(2).GetComponent<AudioSource>();
        }

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

    public void Update()
    {
        #if UNITY_EDITOR
        bool leftGrip = Keyboard.current.gKey.wasPressedThisFrame;
        bool rightGrip = Keyboard.current.hKey.wasPressedThisFrame;
        bool leftTrigger = Keyboard.current.vKey.isPressed;
        bool rightTrigger = Keyboard.current.bKey.isPressed;
        #endif

        bool playerInGame = PhotonRoyaleLobby.instance != null ? PhotonRoyaleLobby.instance.activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber) : false;
        gunRigidbody.isKinematic = true;

        if (!reloading)
        {
            if (Time.time > nextFire)
            {
                #if UNITY_EDITOR
                if (rightHand && rightTrigger)
                #else
                if (rightHand && InputManager.instance.rightHandTrigger.IsPressed())
                #endif
                {
                    nextFire = Time.time + gunData.fireRate;
                    Vector3 firePos = barrel.position;
                    Vector3[] directions = new Vector3[gunData.numBullets];
                    for (int i = 0; i < gunData.numBullets; i++)
                    {
                        looker.position = firePos;
                        looker.forward = barrel.forward;
                        looker.Rotate(new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized * Random.Range(0.0f, gunData.spread), Space.Self);
                        directions[i] = looker.forward;
                    }
                    StartVibration(false, vibrationAmmount, 0.15f);
                    ShotFired(firePos, directions);
                }
                #if UNITY_EDITOR
                else if (!rightHand && leftTrigger)
                #else
                else if (!rightHand && InputManager.instance.leftHandTrigger.IsPressed())
                #endif
                {
                    nextFire = Time.time + gunData.fireRate;
                    Vector3 firePos = barrel.position;
                    Vector3[] directions = new Vector3[gunData.numBullets];
                    for (int i = 0; i < gunData.numBullets; i++)
                    {
                        looker.position = firePos;
                        looker.forward = barrel.forward;
                        looker.Rotate(new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized * Random.Range(0.0f, gunData.spread), Space.Self);
                        directions[i] = looker.forward;
                    }
                    StartVibration(false, vibrationAmmount, 0.15f);
                    ShotFired(firePos, directions);
                }
            }
        }
        else
        {
            if (gunData.reloadTime + startReload < Time.time)
            {
                reloading = false;
            }
        }
        transform.position = hands[rightHand ? 1 : 0].position;
        transform.rotation = hands[rightHand ? 1 : 0].rotation;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        if (reloading)
        {
            Reload();
        }
        StartCoroutine(GetPlayer());
    }

    [PunRPC]
    public void SetGunVisibility(bool visible)
    {
        for (int i = 0; i < gunRenderers.Length; i++)
        {
            gunRenderers[i].enabled = visible;
        }
    }

    [PunRPC]
    public void Stacked(bool onOff)
    {
        gameObject.SetActive(onOff);
    }

    [PunRPC]
    public void OutOfAmmo()
    {
        clickSound.Play();
    }

    [PunRPC]
    public void ShotFiredRemote(Vector3 firePos, Vector3[] bulletVelocities, int newPlayerID, bool isExploding, int[] bulletIDs)
    {
        if (isExploding)
        {
            int shotCount = bulletVelocities.Length - 1;
            for (int i = 0; i < bulletIDs.Length; i++)
            {
                curBullet = PhotonExplodingBullet.bullets[bulletIDs[i]];
                curBullet.transform.SetParent(null);
                curBullet.transform.position = firePos;
                curBullet.transform.LookAt(curBullet.transform.position + bulletVelocities[shotCount]);
                curBullet.playerID = newPlayerID;
                curBullet.gameObject.SetActive(true);
                shotCount--;
            }
        }
        else
        {
            while (bulletPool.Count < bulletVelocities.Length)
            {
                PhotonBullet newBullet = Instantiate(bullet, bulletNeutral).GetComponent<PhotonBullet>();
                newBullet.gameObject.SetActive(false);
                bulletPool.Add(newBullet);
            }

            int shotCount = bulletVelocities.Length - 1;
            while (shotCount > -1)
            {
                curBullet = bulletPool[0];
                bulletPool.RemoveAt(0);
                curBullet.transform.SetParent(null);
                curBullet.transform.position = firePos;
                curBullet.transform.LookAt(curBullet.transform.position + bulletVelocities[shotCount]);
                curBullet.playerID = newPlayerID;
                curBullet.gameObject.SetActive(true);
                shotCount--;
            }
        }
        curBullets--;
        shotSound.Play();

        if (curBullets <= 0)
        {
            curBullets = gunData.ammo;
            Reload();
        }
    }

    public void ShotFired(Vector3 firePos, Vector3[] bulletVelocities)
    {
        while (bulletPool.Count < bulletVelocities.Length)
        {
            PhotonBullet newBullet = Instantiate(bullet, bulletNeutral).GetComponent<PhotonBullet>();
            newBullet.gameObject.SetActive(false);
            bulletPool.Add(newBullet);
        }

        List<int> bulletIDs = new List<int>();

        int shotCount = bulletVelocities.Length - 1;
        while (shotCount > -1)
        {
            curBullet = bulletPool[0];
            bulletPool.RemoveAt(0);
            curBullet.transform.SetParent(null);
            curBullet.transform.position = firePos;
            curBullet.transform.LookAt(curBullet.transform.position + bulletVelocities[shotCount]);
            curBullet.playerID = PhotonNetwork.LocalPlayer.ActorNumber;
            curBullet.gameObject.SetActive(true);

            if (curBullet.GetComponent<PhotonExplodingBullet>() != null)
            {
                bulletIDs.Add(((PhotonExplodingBullet)curBullet).id);
                curBullet.photonView.RequestOwnership();
                curBullet.GetComponent<PhotonView>().RPC("SetPlayerID", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
            }
            shotCount--;
        }
        curBullets--;
        shotSound.Play();

        if (curBullets <= 0)
        {
            curBullets = gunData.ammo;
            Reload();
        }
        photonView.RPC("ShotFiredRemote", RpcTarget.Others, firePos, bulletVelocities, PhotonNetwork.LocalPlayer.ActorNumber, bulletIDs.Count > 0, bulletIDs.ToArray());
    }

    public void Reload()
    {
        startReload = Time.time;
        reloading = true;
        reloadSound.Play();
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        ResetGun();
    }

    [PunRPC]
    public void ResetGun()
    {
        reloading = false;
        curBullets = gunData.ammo; 
        gunCollider.enabled = true;

        for (int i = 0; i < gunRenderers.Length; i++)
        {
            gunRenderers[i].enabled = true;
        }  
    }

    public void ResetToSafety()
    {
        if (photonView.IsMine)
        {
            gunRigidbody.velocity = Vector3.zero;
            transform.position = GunSpawner.instance.GetStartSpot();
        }
    }

    public void StartVibration(bool forLeftController, float amplitude, float duration)
    {
        base.StartCoroutine(this.HapticPulses(forLeftController, amplitude, duration));
    }

    // Token: 0x06000315 RID: 789 RVA: 0x00016512 File Offset: 0x00014712
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
