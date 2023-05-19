using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;
using Photon.VR.Player;
using UnityEngine.XR;

public class Grenade : MonoBehaviourPunCallbacks
{
    public PhotonRoyalePlayer.InventoryItem grenadeItem;
    public Transform[] hands;

    public ScriptableGrenadeConfiguration grenadeConfig;
    public Rigidbody grenadeRigidbody;
    public ParticleSystem explodeSystem;
    public Collider grenadeCollider;
    public Renderer grenadeRenderer;
    public AudioSource grabSound;
    public AudioSource warningSound;
    public GameObject explosion;
    public int playerID = -1;
    public int lastPlayer = -1;
    public bool activated = false;
    public bool rightHand = false;
    public bool held = false;
    public bool thrown = false;
    public List<Vector3> lastFrames = new List<Vector3>();
    public List<Vector3> lastLocalFrames = new List<Vector3>();
    public Color neutralColor;
    public Color highlightColor;

    public float hapticWaitSeconds = 0.05f;
    public float vibrationAmmount = 0.15f;

    float boomStart = 0.0f;
    public int lastSecond = 0;
    public int grenadeID = 0;

    bool inHand = false;

    PhotonRoyalePlayer royalePlayer;
    static bool gotCasts = false;

    public IEnumerator Start()
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

    bool GrenadeIsInMyHand()
    {
        inHand = false;
        for (int i = 0; i < royalePlayer.gunHands[0].stackedItems.Count; i++)
        {
            inHand = inHand || royalePlayer.gunHands[0].stackedItems[i].itemID == grenadeID && 
                royalePlayer.gunHands[0].stackedItems[i].itemType == PhotonRoyalePlayer.ItemType.Grenade;
        }

        for (int i = 0; i < royalePlayer.gunHands[1].stackedItems.Count; i++)
        {
            inHand = inHand || royalePlayer.gunHands[1].stackedItems[i].itemID == grenadeID && 
                royalePlayer.gunHands[1].stackedItems[i].itemType == PhotonRoyalePlayer.ItemType.Grenade;
        }

        for (int i = 0; i < royalePlayer.leftHandItems[0].stackedItems.Count; i++)
        {
            inHand = inHand || royalePlayer.leftHandItems[0].stackedItems[i].itemID == grenadeID && 
                royalePlayer.leftHandItems[0].stackedItems[i].itemType == PhotonRoyalePlayer.ItemType.Grenade;
        }

        for (int i = 0; i < royalePlayer.leftHandItems[1].stackedItems.Count; i++)
        {
            inHand = inHand || royalePlayer.leftHandItems[1].stackedItems[i].itemID == grenadeID && 
                royalePlayer.leftHandItems[1].stackedItems[i].itemType == PhotonRoyalePlayer.ItemType.Grenade;
        }

        for (int i = 0; i < royalePlayer.rightHandItems[0].stackedItems.Count; i++)
        {
            inHand = inHand || royalePlayer.rightHandItems[0].stackedItems[i].itemID == grenadeID && 
                royalePlayer.rightHandItems[0].stackedItems[i].itemType == PhotonRoyalePlayer.ItemType.Grenade;
        }

        for (int i = 0; i < royalePlayer.rightHandItems[1].stackedItems.Count; i++)
        {
            inHand = inHand || royalePlayer.rightHandItems[1].stackedItems[i].itemID == grenadeID && 
                royalePlayer.rightHandItems[1].stackedItems[i].itemType == PhotonRoyalePlayer.ItemType.Grenade;
        }
        return inHand;
    }

    public void Update()
    {
        #if UNITY_EDITOR
        bool leftGrip = Keyboard.current.gKey.wasPressedThisFrame;
        bool rightGrip = Keyboard.current.hKey.wasPressedThisFrame;
        bool leftTrigger = Keyboard.current.vKey.isPressed;
        bool rightTrigger = Keyboard.current.bKey.isPressed;
        #endif

        if (royalePlayer != null)
        {
            GrenadeIsInMyHand();
        }

        bool playerInGame = PhotonRoyaleLobby.instance != null ? PhotonRoyaleLobby.instance.activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber) : false;

        if (gameObject.activeInHierarchy)
        {
            grenadeRigidbody.isKinematic = !photonView.IsMine || (held && !thrown);
            if (playerID == -1)
            {
                held = false;
            }

            if (activated)
            {
                if (Mathf.FloorToInt(Time.time - boomStart) > lastSecond)
                {
                    lastSecond = Mathf.FloorToInt(Time.time - boomStart);
                    grenadeRenderer.materials[2].SetColor("_EmissionColor", grenadeConfig.secondColors[Mathf.Min(grenadeConfig.secondColors.Length - 1, lastSecond)]);
                    warningSound.pitch = 0.7f + 0.1f * lastSecond;
                    warningSound.volume = 0.1f + 0.05f * lastSecond;
                    warningSound.Play();

                    if (lastSecond >= grenadeConfig.fuseTime && photonView.IsMine)
                    {
                        activated = false;
                        photonView.RPC("Explode", RpcTarget.All);
                    }
                }
            }

            if (held && playerID == PhotonNetwork.LocalPlayer.ActorNumber && inHand)
            {
                transform.position = hands[rightHand ? 1 : 0].position;
                transform.rotation = hands[rightHand ? 1 : 0].rotation;
                if (!activated)
                {
                    #if UNITY_EDITOR
                    if (rightHand && rightTrigger)
                    #else
                    if (rightHand && InputManager.instance.rightHandTrigger.WasPressedThisFrame())
                    #endif
                    {
                        photonView.RPC("GrenadeTriggered", RpcTarget.All);
                        lastFrames.Clear();
                        lastFrames.Add(transform.position);
                        
                        GorillaLocomotion.Player.Instance.playerLooker.position = transform.position;
                        lastLocalFrames.Clear();
                        lastLocalFrames.Add(GorillaLocomotion.Player.Instance.playerLooker.localPosition.x * GorillaLocomotion.Player.Instance.playerLooker.right +
                                            GorillaLocomotion.Player.Instance.playerLooker.localPosition.y * GorillaLocomotion.Player.Instance.playerLooker.up +
                                            GorillaLocomotion.Player.Instance.playerLooker.localPosition.z * GorillaLocomotion.Player.Instance.playerLooker.forward);
                    }
                    #if UNITY_EDITOR
                    else if (!rightHand && leftTrigger)
                    #else
                    else if (!rightHand && InputManager.instance.leftHandTrigger.WasPressedThisFrame())
                    #endif
                    {
                        photonView.RPC("GrenadeTriggered", RpcTarget.All);
                        lastFrames.Clear();
                        lastFrames.Add(transform.position);

                        GorillaLocomotion.Player.Instance.playerLooker.position = transform.position;
                        lastLocalFrames.Clear();
                        lastLocalFrames.Add(GorillaLocomotion.Player.Instance.playerLooker.localPosition.x * GorillaLocomotion.Player.Instance.playerLooker.right +
                                            GorillaLocomotion.Player.Instance.playerLooker.localPosition.y * GorillaLocomotion.Player.Instance.playerLooker.up +
                                            GorillaLocomotion.Player.Instance.playerLooker.localPosition.z * GorillaLocomotion.Player.Instance.playerLooker.forward);
                    }
                }
                else
                {
                    GorillaLocomotion.Player.Instance.playerLooker.position = transform.position;
                    lastFrames.Add(transform.position);
                    lastLocalFrames.Add(GorillaLocomotion.Player.Instance.playerLooker.localPosition.x * GorillaLocomotion.Player.Instance.playerLooker.right +
                                        GorillaLocomotion.Player.Instance.playerLooker.localPosition.y * GorillaLocomotion.Player.Instance.playerLooker.up +
                                        GorillaLocomotion.Player.Instance.playerLooker.localPosition.z * GorillaLocomotion.Player.Instance.playerLooker.forward);
                    if (lastFrames.Count > 10)
                    {
                        lastFrames.RemoveAt(0);
                        lastLocalFrames.RemoveAt(0);
                    }

                    if (!thrown)
                    {
                        #if UNITY_EDITOR
                        if (rightHand && !rightTrigger)
                        #else
                        if (rightHand && !InputManager.instance.rightHandTrigger.IsPressed())
                        #endif
                        {
                            thrown = true;
                            grenadeRigidbody.isKinematic = false;
                            photonView.RPC("Thrown", RpcTarget.All, playerID);
                            SetInitialVelocity();
                        }
                        #if UNITY_EDITOR
                        else if (!rightHand && !leftTrigger)
                        #else
                        else if (!rightHand && !InputManager.instance.leftHandTrigger.IsPressed())
                        #endif
                        {
                            thrown = true;
                            grenadeRigidbody.isKinematic = false;
                            photonView.RPC("Thrown", RpcTarget.All, playerID);
                            SetInitialVelocity();
                        }
                    }
                }
            }
            else if (held && photonView.IsMine && !inHand)
            {
                ResolvePermissions();
            }
            else if (!held && !thrown && royalePlayer != null && royalePlayer.alive && playerInGame)
            {
                if (!gotCasts)
                {
                    InputManager.instance.GetHandCasts(3.0f);
                    gotCasts = true;
                }

                if (InputManager.instance.GetLeftHandHit().collider == grenadeCollider || InputManager.instance.GetRightHandHit().collider == grenadeCollider)
                {
                    #if UNITY_EDITOR
                    if (royalePlayer.LeftHandHasSlot(grenadeItem) && InputManager.instance.GetLeftHandHit().collider == grenadeCollider && leftGrip)
                    #else
                    if (royalePlayer.LeftHandHasSlot(grenadeItem) && InputManager.instance.GetLeftHandHit().collider == grenadeCollider && InputManager.instance.leftHandGrip.WasPressedThisFrame())
                    #endif
                    {
                        photonView.RequestOwnership();
                        photonView.RPC("GrenadeGrabbed", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);

                        grenadeItem.itemID = grenadeID;
                        royalePlayer.PutItemInSlot(true, grenadeItem, gameObject, transform);
                        grenadeRenderer.materials[0].color = neutralColor;
                        rightHand = false;
                    }
                    #if UNITY_EDITOR
                    else if (royalePlayer.RightHandHasSlot(grenadeItem) && InputManager.instance.GetRightHandHit().collider == grenadeCollider && rightGrip)
                    #else
                    else if (royalePlayer.RightHandHasSlot(grenadeItem) && InputManager.instance.GetRightHandHit().collider == grenadeCollider && InputManager.instance.rightHandGrip.WasPressedThisFrame())
                    #endif
                    {
                        photonView.RequestOwnership();
                        photonView.RPC("GrenadeGrabbed", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);

                        grenadeItem.itemID = grenadeID;
                        royalePlayer.PutItemInSlot(false, grenadeItem, gameObject, transform);
                        grenadeRenderer.materials[0].color = neutralColor;
                        rightHand = true;
                    }
                    else
                    {
                        grenadeRenderer.materials[0].color = highlightColor;
                    }
                }
                else
                {
                    grenadeRenderer.materials[0].color = neutralColor;
                }
            }
        }
    }

    public void SetInitialVelocity()
    {
        Vector3 totalVec = Vector3.zero;
        Vector3 averageVec = Vector3.zero;
        Vector3 totalLocalVec = Vector3.zero;
        Vector3 averageLocalVec = Vector3.zero;
        int validFrames = 0; 
        int validLocalFrames = 0; 

        for (int i = 1; i < lastFrames.Count; i++)
        {
            if ((lastFrames[i] - lastFrames[i - 1]).magnitude > grenadeConfig.ignoreDistance)
            {
                validFrames++;
                totalVec += lastFrames[i] - lastFrames[i - 1];
            }
        }

        for (int i = 1; i < lastLocalFrames.Count; i++)
        {
            if ((lastLocalFrames[i] - lastLocalFrames[i - 1]).magnitude > grenadeConfig.ignoreDistance)
            {
                validLocalFrames++;
                totalLocalVec += lastLocalFrames[i] - lastLocalFrames[i - 1];
            }
        }

        if (validFrames > 0)
        {
            averageVec = totalVec / validFrames;
        }

        if (validLocalFrames > 0)
        {
            averageLocalVec = (totalLocalVec / validLocalFrames) * grenadeConfig.throwMod;
        }
        grenadeRigidbody.velocity = averageVec + averageLocalVec;
    }

    [PunRPC]
    public void ResetGrenade()
    {
        held = false;
        thrown = false;
        activated = false;
        playerID = -1;
        lastFrames.Clear();
        lastSecond = 0;
        grenadeRenderer.materials[2].SetColor("_EmissionColor", grenadeConfig.secondColors[lastSecond]);
        grenadeRigidbody.isKinematic = false;
    }

    [PunRPC]
    public void Explode()
    {
        bool playerInGame = PhotonRoyaleLobby.instance != null ? PhotonRoyaleLobby.instance.activePlayersList.Contains(PhotonNetwork.LocalPlayer.ActorNumber) : false;
        
        explodeSystem.gameObject.SetActive(true);
        explodeSystem.transform.position = transform.position;
        explodeSystem.transform.SetParent(null);
        explodeSystem.transform.localScale = Vector3.one;
        explodeSystem.Play();

        if (photonView.IsMine)
        {
            PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
            for (int i = 0; i < players.Length; i++)
            {
                float distance = Vector3.Distance(transform.position, players[i].gameObject.GetComponent<PhotonVRPlayer>().Head.position);
                float force = distance < grenadeConfig.minDamageDistance ? grenadeConfig.maxForce : 
                    Mathf.Lerp(grenadeConfig.maxForce, grenadeConfig.minForce, (distance - grenadeConfig.minDamageDistance) / (grenadeConfig.maxDamageDistance - grenadeConfig.minDamageDistance));
                float damage = distance < grenadeConfig.minDamageDistance ? grenadeConfig.maxDamage : 
                    Mathf.Lerp(grenadeConfig.maxDamage, grenadeConfig.minDamage, (distance - grenadeConfig.minDamageDistance) / (grenadeConfig.maxDamageDistance - grenadeConfig.minDamageDistance));
                if (playerInGame && 
                    players[i].alive &&
                    distance < grenadeConfig.maxDamageDistance)
                {
                    players[i].photonView.RPC("ExplosiveKnockback", players[i].photonView.Controller, 
                                              (players[i].gameObject.GetComponent<PhotonVRPlayer>().Head.position - transform.position).normalized, force);
                    players[i].photonView.RPC("CheckHit", players[i].photonView.Controller, players[i].gameObject.GetComponent<PhotonVRPlayer>().Head.position, (int)damage, lastPlayer);
                }
            }
        }
        activated = false;
        held = false;
        gameObject.SetActive(false);
    }

    [PunRPC]
    public void Thrown(int lastID)
    {
        held = false;
        lastPlayer = lastID;
        playerID = -1;
        grenadeCollider.enabled = true;

        if (photonView.IsMine)
        {
            grenadeRigidbody.isKinematic = false;
            royalePlayer.MoveToNextStackItem(gameObject, !rightHand);
        }
    }

    [PunRPC]
    public void GrenadeTriggered()
    {
        activated = true;
        boomStart = Time.time;
        lastSecond = 0;
        grenadeRenderer.materials[2].SetColor("_EmissionColor", grenadeConfig.secondColors[lastSecond]);
        warningSound.pitch = 0.7f + 0.1f * lastSecond;
        warningSound.volume = 0.2f + 0.1f * lastSecond;
        warningSound.Play();
    }

    [PunRPC]
    public void GrenadeGrabbed(int newID)
    {
        held = true;
        thrown = false;
        activated = false;
        playerID = newID;
        grenadeCollider.enabled = false;
        grenadeRigidbody.isKinematic = true;
        grabSound.Play();
        gameObject.SetActive(true);
    }

    [PunRPC]
    public void GrenadeDropped()
    {
        held = false;
        activated = false;
        playerID = -1;
        grenadeCollider.enabled = true;

        if (photonView.IsMine)
        {
            grenadeRigidbody.isKinematic = false;
        }
    }
    
    [PunRPC]
    public void Stacked(bool onOff)
    {
        gameObject.SetActive(onOff);
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
        photonView.RPC("GrenadeDropped", RpcTarget.All);
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (held && playerID == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            photonView.RPC("GrenadeGrabbed", newPlayer, playerID);
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

    public void OnDrawGizmosSelected()
    {
        if (grenadeConfig != null)
        {
            Gizmos.DrawWireSphere(transform.position, grenadeConfig.maxDamageDistance);
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        held = false;
        activated = false;
        playerID = -1;
    }
}
