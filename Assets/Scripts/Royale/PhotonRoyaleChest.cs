using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;

public class PhotonRoyaleChest : MonoBehaviourPunCallbacks
{
    public static bool gotCasts = false;

    public Renderer[] chestRenderers;
    public Renderer[] chestGlowRenderers;
    public Rigidbody chestRigidbody;
    public Transform joint;
    public Collider openCollider;
    public AudioSource openSource;
    public bool open = false;
    public int minGuns = 1;
    public int maxGuns = 2;
    public int ammoInside = 30;
    public float forwardDist = 1.0f;
    public float randDist = 0.5f;
    public bool landed = false;

    float openPercentage = 0.0f;

    public Color[] normalColors;
    public Color[] highlightColors;

    [ColorUsageAttribute(false, true)]
    public Color[] glowColors;

    [ColorUsageAttribute(false, true)]
    public Color[] noGlowColors;

    public Vector3 openVec;
    public Vector3 closedVec;

    [PunRPC]
    public void ChestOpened(int[] gunIDs, int ammoNum, int itemNum)
    {
        if (!open)
        {
            open = true;
            openSource.Play();
            chestRigidbody.isKinematic = true;
            for (int i = 0; i < gunIDs.Length; i++)
            {
                GunSpawner.instance.guns[gunIDs[i]].gameObject.SetActive(true);
            }

            GunSpawner.instance.ammo[ammoNum].gameObject.SetActive(true);
            if (itemNum != -1)
            {
                GunSpawner.instance.items[itemNum].gameObject.SetActive(true);
            }
        }
    }

    [PunRPC]
    public void SyncOpened(bool opened)
    {
        open = opened;
        chestRigidbody.isKinematic = open || !photonView.IsMine;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        if (chestRigidbody == null)
        {
            chestRigidbody = GetComponent<Rigidbody>();
        }

        if (openSource == null)
        {
            openSource = GetComponent<AudioSource>();
        }
        ResetChest();
    }

    public void ResetChest()
    {
        open = false;
        chestRigidbody.isKinematic = !photonView.IsMine;
        chestRenderers[0].materials[0].color = normalColors[0];
        chestRenderers[1].materials[0].color = normalColors[1];
        chestGlowRenderers[0].materials[0].SetColor("_EmissionColor", glowColors[0]);
        chestGlowRenderers[1].materials[0].SetColor("_EmissionColor", glowColors[1]);
        landed = false;
    }

    public void Update()
    {
        #if UNITY_EDITOR
        bool leftGrip = Keyboard.current.gKey.wasPressedThisFrame;
        bool rightGrip = Keyboard.current.hKey.wasPressedThisFrame;
        bool leftTrigger = Keyboard.current.vKey.isPressed;
        bool rightTrigger = Keyboard.current.bKey.isPressed;
        #endif

        if (photonView.IsMine && chestRigidbody.isKinematic == true && !landed)
        {
            chestRigidbody.isKinematic = false;
        }
        else if ((!photonView.IsMine && !chestRigidbody.isKinematic) || landed)
        {
            chestRigidbody.isKinematic = true;
        }

        if (!open && PhotonRoyaleLobby.instance.activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber))
        {
            if (!gotCasts)
            {
                InputManager.instance.GetHandCasts(3.0f);
                gotCasts = true;
            }

            if ((InputManager.instance.GetLeftHandHit().collider == openCollider || InputManager.instance.GetRightHandHit().collider == openCollider) && PhotonRoyalePlayer.me != null && PhotonRoyalePlayer.me.alive)
            {
                #if UNITY_EDITOR
                if (InputManager.instance.GetLeftHandHit().collider == openCollider && leftGrip)
                #else
                if (InputManager.instance.GetLeftHandHit().collider == openCollider && InputManager.instance.leftHandGrip.WasPressedThisFrame())
                #endif
                {
                    OpenChestLocal();
                }
                #if UNITY_EDITOR
                else if (InputManager.instance.GetRightHandHit().collider == openCollider && rightGrip)
                #else
                else if (InputManager.instance.GetRightHandHit().collider == openCollider && InputManager.instance.rightHandGrip.WasPressedThisFrame())
                #endif
                {
                    OpenChestLocal();
                }
                else
                {
                    chestRenderers[0].materials[0].color = highlightColors[0];
                    chestRenderers[1].materials[0].color = highlightColors[1];
                }
            }
            else
            {
                chestRenderers[0].materials[0].color = normalColors[0];
                chestRenderers[1].materials[0].color = normalColors[1];
            }
        }
        else if (open)
        {
            chestGlowRenderers[0].materials[0].SetColor
                ("_EmissionColor", Color.Lerp(chestGlowRenderers[0].materials[0].GetColor("_EmissionColor"), noGlowColors[0], Time.deltaTime * 0.5f));
            chestGlowRenderers[1].materials[0].SetColor
                ("_EmissionColor", Color.Lerp(chestGlowRenderers[1].materials[0].GetColor("_EmissionColor"), noGlowColors[1], Time.deltaTime * 0.5f));
        }

        openPercentage = Mathf.Lerp(openPercentage, open ? 1.0f : 0.0f, Time.deltaTime);
        joint.localEulerAngles = Vector3.Lerp(closedVec, openVec, openPercentage);
    }

    public void OpenChestLocal()
    {
        int[] gunIDs = new int[Random.Range(minGuns, maxGuns + 1)];
        for (int i = 0; i < gunIDs.Length; i++)
        {
            gunIDs[i] = GunSpawner.instance.GetOpenGun();
        }
        int ammoNum = GunSpawner.instance.GetOpenAmmo();
        int item = GunSpawner.instance.GetOpenItem();

        photonView.RequestOwnership();
        photonView.RPC("ChestOpened", RpcTarget.All, gunIDs, ammoNum, item);
        chestRenderers[0].materials[0].color = normalColors[0];
        chestRenderers[1].materials[0].color = normalColors[1];

        for (int i = 0; i < gunIDs.Length; i++)
        {
            GunSpawner.instance.guns[gunIDs[i]].gameObject.SetActive(true);
            GunSpawner.instance.guns[gunIDs[i]].gameObject.GetComponent<PhotonGun>().photonView.RequestOwnership();
            GunSpawner.instance.guns[gunIDs[i]].gameObject.GetComponent<PhotonGun>().ResetGun();
            GunSpawner.instance.guns[gunIDs[i]].gameObject.GetComponent<PhotonGun>().photonView.RPC("ResetGun", RpcTarget.Others);
            GunSpawner.instance.guns[gunIDs[i]].transform.position = transform.position + transform.forward * forwardDist + 
                new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(0.3f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized * Random.Range(0.0f, randDist);
        }

        GunSpawner.instance.ammo[ammoNum].gameObject.SetActive(true);
        GunSpawner.instance.ammo[ammoNum].gameObject.GetComponent<AmmoPickup>().photonView.RequestOwnership();
        GunSpawner.instance.ammo[ammoNum].transform.position = transform.position + transform.forward * forwardDist + 
            new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(0.3f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized * Random.Range(0.0f, randDist);

        if (item != -1)
        {
            Debug.Log(GunSpawner.instance.items[item].name);
            GunSpawner.instance.items[item].gameObject.SetActive(true);
            GunSpawner.instance.items[item].gameObject.GetComponent<PhotonView>().RequestOwnership();
            if (GunSpawner.instance.items[item].gameObject.GetComponent<Grenade>() != null)
            {
                GunSpawner.instance.items[item].gameObject.GetComponent<Grenade>().photonView.RPC("ResetGrenade", RpcTarget.Others);
                GunSpawner.instance.items[item].gameObject.GetComponent<Grenade>().ResetGrenade();
                GunSpawner.instance.items[item].transform.position = transform.position + transform.forward * forwardDist + 
                    new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(0.3f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized * Random.Range(0.0f, randDist);
            }
            else if (GunSpawner.instance.items[item].gameObject.GetComponent<PhotonTorch>() != null)
            {
                GunSpawner.instance.items[item].gameObject.GetComponent<PhotonTorch>().photonView.RPC("ResetTorch", RpcTarget.Others);
                GunSpawner.instance.items[item].gameObject.GetComponent<PhotonTorch>().ResetTorch();
                GunSpawner.instance.items[item].transform.position = transform.position + transform.forward * forwardDist + 
                    new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(0.3f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized * Random.Range(0.0f, randDist);
            }
            else if (GunSpawner.instance.items[item].gameObject.GetComponent<PhotonIceShield>() != null)
            {
                GunSpawner.instance.items[item].gameObject.GetComponent<PhotonIceShield>().photonView.RPC("ResetShield", RpcTarget.Others);
                GunSpawner.instance.items[item].gameObject.GetComponent<PhotonIceShield>().ResetShield();
                GunSpawner.instance.items[item].transform.position = transform.position + transform.forward * forwardDist + 
                    new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(0.8f, 1.5f), Random.Range(-1.0f, 1.0f)).normalized * Random.Range(0.0f, randDist);
            }
            else
            {
                GunSpawner.instance.items[item].transform.position = transform.position + transform.forward * forwardDist + 
                    new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(0.3f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized * Random.Range(0.0f, randDist);
            }
        }
    }
    
    public void LateUpdate()
    {
        gotCasts = false;
    }

    public void OnCollisionEnter(Collision hit)
    {
        if (!landed && (hit.transform.tag == "WOOD" || hit.transform.tag == "SNOW" || hit.transform.tag == "ROCK" || hit.transform.tag == "METAL"))
        {
            photonView.RPC("Landed", RpcTarget.All);
        }
    }

    [PunRPC]
    public void Landed()
    {
        landed = true;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        StartCoroutine(SyncWait(newPlayer));
    }

    public IEnumerator SyncWait(Player newPlayer)
    {
        yield return new WaitForSeconds(3.0f);
        if (photonView.IsMine)
        {
            photonView.RPC("SyncOpened", newPlayer, open);
        }
    }
}
