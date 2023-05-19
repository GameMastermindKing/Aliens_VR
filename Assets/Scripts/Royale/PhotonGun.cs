using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.XR;

public class PhotonGun : MonoBehaviourPunCallbacks
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
    public int playerID;
    public bool held;
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

        if (!photonView.IsMine)
        {
            gunRigidbody.isKinematic = true;
        }
        else if (!held && gunRigidbody.isKinematic)
        {
            gunRigidbody.isKinematic = false;
        }

        if (!reloading)
        {
            if (held && playerID == PhotonNetwork.LocalPlayer.ActorNumber && (royalePlayer.gunHands[1].itemInSlot == gunItem || royalePlayer.gunHands[0].itemInSlot == gunItem))
            {
                transform.position = hands[rightHand ? 1 : 0].position;
                transform.rotation = hands[rightHand ? 1 : 0].rotation;
                if (Time.time > nextFire)
                {
                    #if UNITY_EDITOR
                    if (rightHand && rightTrigger)
                    #else
                    if (rightHand && InputManager.instance.rightHandTrigger.IsPressed())
                    #endif
                    {
                        if (royalePlayer.ammo >= gunData.numBullets)
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
                        else
                        {
                            nextFire = Time.time + gunData.fireRate;
                            photonView.RPC("OutOfAmmo", RpcTarget.All);
                        }
                    }
                    #if UNITY_EDITOR
                    else if (!rightHand && leftTrigger)
                    #else
                    else if (!rightHand && InputManager.instance.leftHandTrigger.IsPressed())
                    #endif
                    {
                        if (royalePlayer.ammo >= gunData.ammoConsumed)
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
                        else
                        {
                            nextFire = Time.time + gunData.fireRate;
                            photonView.RPC("OutOfAmmo", RpcTarget.All);
                        }
                    }
                }
            }
            else if (held && photonView.IsMine && royalePlayer.gunHands[1].itemInSlot != gunItem && royalePlayer.gunHands[0].itemInSlot != gunItem)
            {
                ResolvePermissions();
            }
            else if (!held && royalePlayer != null && royalePlayer.alive && playerInGame)
            {
                if (!gotCasts)
                {
                    InputManager.instance.GetHandCasts(gunData.grabDistance);
                    gotCasts = true;
                }

                if (InputManager.instance.GetLeftHandHit().collider == gunCollider || InputManager.instance.GetRightHandHit().collider == gunCollider)
                {
                    #if UNITY_EDITOR
                    if (royalePlayer.LeftHandHasSlot(gunItem) && InputManager.instance.GetLeftHandHit().collider == gunCollider && leftGrip)
                    #else
                    if (royalePlayer.LeftHandHasSlot(gunItem) && InputManager.instance.GetLeftHandHit().collider == gunCollider && InputManager.instance.leftHandGrip.WasPressedThisFrame())
                    #endif
                    {
                        photonView.RequestOwnership();
                        photonView.RPC("GunGrabbed", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
                        royalePlayer.PutItemInSlot(true, gunItem, gameObject, transform);
                        royalePlayer.MakeHeldItemsSkinned();
                        for (int i = 0; i < gunRenderers.Length; i++)
                        {
                            gunRenderers[i].material.color = neutralColor;
                        }
                        rightHand = false;
                    }
                    #if UNITY_EDITOR
                    else if (royalePlayer.RightHandHasSlot(gunItem) && InputManager.instance.GetRightHandHit().collider == gunCollider && rightGrip)
                    #else
                    else if (royalePlayer.RightHandHasSlot(gunItem) && InputManager.instance.GetRightHandHit().collider == gunCollider && InputManager.instance.rightHandGrip.WasPressedThisFrame())
                    #endif
                    {
                        photonView.RequestOwnership();
                        photonView.RPC("GunGrabbed", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
                        royalePlayer.PutItemInSlot(false, gunItem, gameObject, transform);
                        royalePlayer.MakeHeldItemsSkinned();
                        for (int i = 0; i < gunRenderers.Length; i++)
                        {
                            gunRenderers[i].material.color = neutralColor;
                        }
                        rightHand = true;
                    }
                    else
                    {
                        for (int i = 0; i < gunRenderers.Length; i++)
                        {
                            gunRenderers[i].material.color = highlightColor;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < gunRenderers.Length; i++)
                    {
                        gunRenderers[i].material.color = neutralColor;
                    }
                }
            }
        }
        else
        {
            if (gunData.reloadTime + startReload < Time.time)
            {
                reloading = false;
            }
            
            if (held && playerID == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                transform.position = hands[rightHand ? 1 : 0].position;
                transform.rotation = hands[rightHand ? 1 : 0].rotation;
            }
        }
    }

    public void LateUpdate()
    {
        gotCasts = false;
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
    public void GunGrabbed(int newID)
    {
        if (held == false && playerID == -1)
        {
            held = true;
            playerID = newID;
            gunCollider.enabled = false;
            gunRigidbody.isKinematic = true;
            gameObject.SetActive(true);

            if (reloadSound.enabled && reloadSound.gameObject.activeInHierarchy)
            {
                reloadSound.Play();
            }
        }
    }

    [PunRPC]
    public void GunDropped()
    {
        held = false;
        playerID = -1;
        gunCollider.enabled = true;
        for (int i = 0; i < gunRenderers.Length; i++)
        {
            gunRenderers[i].enabled = true;
        }

        if (photonView.IsMine)
        {
            gunRigidbody.isKinematic = false;
        }
    }

    [PunRPC]
    public void ShotFiredRemote(Vector3 firePos, Vector3[] bulletVelocities, int newPlayerID, bool isExploding, int[] bulletIDs)
    {
        PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].photonView.ControllerActorNr == playerID)
            {
                players[i].ammo -= gunData.ammoConsumed;
                break;
            }
        }

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
        PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].photonView.ControllerActorNr == playerID)
            {
                players[i].ammo -= gunData.ammoConsumed;
                break;
            }
        }

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
            curBullet.playerID = playerID;
            curBullet.gameObject.SetActive(true);

            if (curBullet.GetComponent<PhotonExplodingBullet>() != null)
            {
                bulletIDs.Add(((PhotonExplodingBullet)curBullet).id);
                curBullet.photonView.RequestOwnership();
                curBullet.GetComponent<PhotonView>().RPC("SetPlayerID", RpcTarget.All, playerID);
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
        photonView.RPC("ShotFiredRemote", RpcTarget.Others, firePos, bulletVelocities, playerID, bulletIDs.Count > 0, bulletIDs.ToArray());
    }

    public void Reload()
    {
        startReload = Time.time;
        reloading = true;
        reloadSound.Play();
    }

    public void ResolvePermissions()
    {
        PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].photonView.OwnerActorNr == playerID)
            {
                photonView.TransferOwnership(players[i].photonView.Controller);
                return;
            }
        }
        photonView.RPC("GunDropped", RpcTarget.All);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (held && playerID == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            photonView.RPC("GunGrabbed", newPlayer, playerID);
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        ResetGun();
    }

    [PunRPC]
    public void ResetGun()
    {
        held = false;
        playerID = -1;
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
