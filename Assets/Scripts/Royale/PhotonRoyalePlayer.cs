using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Photon.VR.Player;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class PhotonRoyalePlayer : MonoBehaviourPunCallbacks
{
    public enum HealthMode
    {
        Body,
        UI,
    }

    public enum AmmoMode
    {
        Body,
        UI,
        Gun
    }

    public enum ItemMode
    {
        Body,
        UI,
        Belt
    }

    public enum ItemType
    {
        HealingTorch,
        Grenade,
        Sniper,
        Shotgun,
        Assault,
        Shield,
        GrenadeLauncher,
        IceCube,
        EMPTY = 999,
    }

    [System.Serializable]
    public class InventoryItem
    {
        public string itemName = "";
        public ItemType itemType;
        public Sprite itemSprite;
        public bool canStack;
        public bool canBePutInPocket = true;
        public int maxStacked = 4;
        public int itemID;
    }

    [System.Serializable]
    public class StackedInventoryItem : InventoryItem
    {
        public GameObject objectBase;
        public Transform objectJoint;

        public StackedInventoryItem(InventoryItem item)
        {
            itemName = item.itemName;
            itemType = item.itemType;
            itemSprite = item.itemSprite;
            canStack = item.canStack;
            canBePutInPocket = item.canBePutInPocket;
            maxStacked = item.maxStacked;
            itemID = item.itemID;
        }
    }

    [System.Serializable]
    public class GunSkin
    {
        public string itemName;
        public string baseName;
        public ItemType itemType;
        public GameObject skinActiveObject;
        public GameObject skinBaseObject;
        public bool rightHand;
        public bool canRender = false;
    }

    [System.Serializable]
    public class InventorySlot
    {
        public GameObject objectBase;
        public Transform objectJoint;
        public InventoryItem itemInSlot;
        public List<StackedInventoryItem> stackedItems;

        public void DropAllFromSlot(Vector3 dropCenter)
        {
            int tries = 8;
            while (ItemInSlot() && tries > 0)
            {
                tries--;
                DropFromSlot(dropCenter);
            }
        }

        public void CleanSlot(InventoryItem emptyItem)
        {
            itemInSlot = emptyItem;
            stackedItems.Clear();
        }

        public void DropFromSlot(Vector3 dropCenter)
        {
            if (itemInSlot != null && itemInSlot.itemType != ItemType.EMPTY)
            {
                switch (itemInSlot.itemType)
                {
                    case ItemType.Assault:
                    case ItemType.Shotgun:
                    case ItemType.Sniper:
                    case ItemType.GrenadeLauncher:
                    {
                        if (!objectBase.activeSelf)
                        {
                            objectBase.GetComponent<PhotonView>().RPC("Stacked", RpcTarget.All, true);
                            objectBase.SetActive(true);
                            objectJoint.position = dropCenter + new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f)).normalized * Random.Range(0.5f, 1.0f);
                        }

                        objectBase.GetComponent<PhotonGun>().photonView.RPC("GunDropped", RpcTarget.All);
                        itemInSlot = new InventoryItem();
                        itemInSlot.itemType = ItemType.EMPTY;
                        objectBase = null;
                        objectJoint = null;
                        break;   
                    }

                    case ItemType.HealingTorch:
                    {
                        if (!objectBase.activeSelf)
                        {
                            objectBase.GetComponent<PhotonView>().RPC("Stacked", RpcTarget.All, true);
                            objectBase.SetActive(true);
                            objectJoint.position = dropCenter + new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f)).normalized * Random.Range(0.5f, 1.0f);
                        }

                        objectBase.GetComponent<PhotonView>().RPC("TorchDropped", RpcTarget.All, objectBase.GetComponent<PhotonTorch>().curHeal);
                        itemInSlot = new InventoryItem();
                        itemInSlot.itemType = ItemType.EMPTY;
                        objectBase = null;
                        objectJoint = null;
                        break;
                    }

                    case ItemType.Shield:
                    {
                        if (!objectBase.activeSelf)
                        {
                            objectBase.GetComponent<PhotonView>().RPC("Stacked", RpcTarget.All, true);
                            objectBase.SetActive(true);
                            objectJoint.position = dropCenter + new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f)).normalized * Random.Range(0.5f, 1.0f);
                        }

                        objectBase.GetComponent<PhotonView>().RPC("ShieldDropped", RpcTarget.All, objectBase.GetComponent<PhotonIceShield>().curHealth);
                        itemInSlot = new InventoryItem();
                        itemInSlot.itemType = ItemType.EMPTY;
                        objectBase = null;
                        objectJoint = null;
                        break;
                    }

                    case ItemType.IceCube:
                    {
                        if (!objectBase.activeSelf)
                        {
                            objectBase.GetComponent<PhotonView>().RPC("Stacked", RpcTarget.All, true);
                            objectBase.SetActive(true);
                            objectJoint.position = dropCenter + new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f)).normalized * Random.Range(0.5f, 1.0f);
                        }

                        objectBase.GetComponent<PhotonView>().RPC("CubeDropped", RpcTarget.All);
                        itemInSlot = new InventoryItem();
                        itemInSlot.itemType = ItemType.EMPTY;
                        objectBase = null;
                        objectJoint = null;
                        break;
                    }
                }
                stackedItems.Clear();
            }
            else if (stackedItems.Count > 0)
            {
                if (stackedItems[0] == null)
                {
                    stackedItems.Clear();
                }

                switch (stackedItems[0].itemType)
                {
                    case ItemType.Grenade:
                    {
                        for (int i = 0; i < stackedItems.Count; i++)
                        {
                            if (!stackedItems[i].objectBase.activeSelf)
                            {
                                stackedItems[i].objectBase.GetComponent<PhotonView>().RPC("Stacked", RpcTarget.All, true);
                                stackedItems[i].objectBase.SetActive(true);
                                stackedItems[i].objectJoint.position = dropCenter + new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f)).normalized * Random.Range(0.5f, 1.0f);
                            }
                            stackedItems[i].objectBase.GetComponent<PhotonView>().RPC("GrenadeDropped", RpcTarget.All);
                        }
                        stackedItems.Clear();
                        break;
                    }

                    default:
                    {
                        stackedItems.Clear();
                        break;
                    }
                }
            }
        }

        public bool ItemInSlot()
        {
            return itemInSlot.itemType != ItemType.EMPTY || stackedItems.Count > 0;
        }

        public bool RoomInSlot(InventoryItem itemToCheck)
        {
            return itemInSlot.itemType == ItemType.EMPTY && 
                (stackedItems.Count == 0 || 
                (stackedItems[0].itemType == itemToCheck.itemType && itemToCheck.maxStacked > stackedItems.Count && itemToCheck.canStack));
        }

        public void DeactivateItem()
        {
            if (stackedItems.Count > 0)
            {
                for (int i = 0; i < stackedItems.Count; i++)
                {
                    if (stackedItems[i].objectBase.activeSelf)
                    {
                        stackedItems[i].objectBase.GetComponent<PhotonView>().RPC("Stacked", RpcTarget.All, false);
                        stackedItems[i].objectBase.SetActive(false);
                    }
                }
            }
            else if (itemInSlot.itemType != ItemType.EMPTY)
            {
                objectBase.GetComponent<PhotonView>().RPC("Stacked", RpcTarget.All, false);
                objectBase.SetActive(false);
            }
        }

        public void ActivateItem()
        {
            if (stackedItems.Count > 0)
            {
                stackedItems[0].objectBase.GetComponent<PhotonView>().RPC("Stacked", RpcTarget.All, true);
                stackedItems[0].objectBase.SetActive(true);

                if (stackedItems[0].objectBase.GetComponent<Grenade>() != null)
                {
                    stackedItems[0].objectBase.GetComponent<Grenade>().photonView.RPC("GrenadeGrabbed", RpcTarget.Others, PhotonNetwork.LocalPlayer.ActorNumber);
                }
            }
            else if (itemInSlot.itemType != ItemType.EMPTY)
            {
                objectBase.GetComponent<PhotonView>().RPC("Stacked", RpcTarget.All, true);
                objectBase.SetActive(true);

                if (objectBase.GetComponent<PhotonIceShield>() != null)
                {
                    objectBase.GetComponent<PhotonIceShield>().photonView.RPC("ShieldGrabbed", RpcTarget.Others, PhotonNetwork.LocalPlayer.ActorNumber, 
                                                                              objectBase.GetComponent<PhotonIceShield>().rightHand);
                }
            }
        }
    }

    public static PhotonRoyalePlayer me;
    public GorillaLocomotion.Player player;
    public static HealthMode healthMode = HealthMode.Body;
    public static AmmoMode ammoMode = AmmoMode.Body;
    public static ItemMode itemMode = ItemMode.Body;

    public Renderer[] renderersToTint;
    public InventorySlot[] gunHands;
    public GunSkin[] gunSkins;
    public InventoryItem emptyItem;
    public GameObject goldCrown;
    public GameObject friendlyIndicator;
    public GameObject longRangeFriendlyIndicator;
    public float longDistance = 100.0f;
    public float health = 100;
    public float maxHealth = 200;
    public float startingHealth = 100;
    public int leaderboardPos = -1;
    public bool alive = true;
    public bool synced = false;

    [Header("Teams")]
    public List<PhotonRoyalePlayer> playersInTeam = new List<PhotonRoyalePlayer>();
    public int teamID = -1;

    [Header("Inventory")]
    public InventorySlot[] leftHandItems;
    public InventorySlot[] rightHandItems;
    public int numSlots = 2;
    public int activeLeftItem = 0;
    public int activeRightItem = 0;
    int slotAimedAtLeft = -1;
    int slotAimedAtRight = -1;

    [Header("Inventory - Body")]
    public GameObject leftBodyInventoryBase;
    public GameObject rightBodyInventoryBase;
    public Image[] inventoryBodyLeftSlots;
    public Image[] inventoryBodyLeftSlotHighlights;
    public Image[] inventoryBodyRightSlots;
    public Image[] inventoryBodyRightSlotHighlights;
    public TextMeshProUGUI[] inventoryBodyLeftSlotCount;
    public TextMeshProUGUI[] inventoryBodyRightSlotCount;

    [Header("Inventory - UI")]
    public GameObject uiInventoryBase;
    public Image[] inventoryUILeftSlots;
    public Image[] inventoryUILeftSlotHighlights;
    public Image[] inventoryUIRightSlots;
    public Image[] inventoryUIRightSlotHighlights;
    public TextMeshProUGUI[] inventoryUILeftSlotCount;
    public TextMeshProUGUI[] inventoryUIRightSlotCount;

    [Header("Sky Death")]
    public AudioSource deathAnnouncementSource;
    public CanvasGroup skyDeathGroup;
    public TextMeshProUGUI skyDeathFreezeNameText;
    public TextMeshProUGUI skyDeathFrozeNameText;
    public TextMeshProUGUI skyPlayersRemainingText;

    [Header("UI Death")]
    public AudioSource uiDeathAnnouncementSource;
    public CanvasGroup uiDeathGroup;
    public TextMeshProUGUI uiDeathFreezeNameText;
    public TextMeshProUGUI uiDeathFrozeNameText;
    public TextMeshProUGUI uiPlayersRemainingText;

    [Header("Ammo")]
    public int ammo = 50;
    public int startAmmo = 50;

    [Header("Did I Hit")]
    public List<Vector3> positions = new List<Vector3>();
    public float sampleTime = 0.05f;
    public int maxSamples = 10;
    public float hitRadius = 0.3f;
    float lastSampleTime = 0.0f;

    [Header("Health Mode - Body")]
    public GameObject bodyHealthBase;
    public TextMeshProUGUI bodyHealthText;
    public RectTransform bodyFreezeBar;
    public Vector3 bodyBarMaxSize;
    public Vector3 bodyBarMinSize;

    public Color normalColor;
    public Color frozenColor;

    [Header("Health Mode - UI")]
    public GameObject uiHealthBase;
    public CanvasGroup freezeUI;
    public TextMeshProUGUI uiHealthText;
    public RectTransform uiFreezeBar;
    public Vector3 uiBarMaxSize;
    public Vector3 uiBarMinSize;

    [Header("Ammo Mode - Body")]
    public GameObject bodyAmmoBase;
    public TextMeshProUGUI bodyAmmoText;
    public RectTransform bodyAmmoBar;
    public Vector3 bodyAmmoBarMaxSize;
    public Vector3 bodyAmmoBarMinSize;

    [Header("Ammo Mode - UI")]
    public GameObject uiAmmoBase;
    public TextMeshProUGUI uiAmmoText;
    public RectTransform uiAmmoBar;
    public Vector3 uiAmmoBarMaxSize;
    public Vector3 uiAmmoBarMinSize;

    [Header("Effects")]
    public ParticleSystem deathEffect;
    public AudioSource deathSource;
    public AudioSource ouchSource;
    public AudioSource healSource;
    public float minHealPitch = 0.8f;
    public float maxHealPitch = 1.5f;

    [Header("Badges")]
    public GameObject[] badgeObjects;
    
    #if UNITY_EDITOR
    public Renderer[] renderersToDisable;
    #endif

    public void Start()
    {
        playersInTeam.Clear();
        playersInTeam.Add(this);
        if (photonView.IsMine)
        {
            me = this;
            AdjustSkinEligibility();
            
            #if UNITY_EDITOR
            for (int i = 0; i < renderersToDisable.Length; i++)
            {
                renderersToDisable[i].enabled = false;
            }
            #endif
        }

        if ((!SceneIsRoyaleMode()) || !photonView.IsMine)
        {
            bodyHealthBase.SetActive(false);
            uiHealthBase.SetActive(false);
            bodyAmmoBase.SetActive(false);
            uiAmmoBase.SetActive(false);
            uiInventoryBase.SetActive(false);
            leftBodyInventoryBase.SetActive(false);
            rightBodyInventoryBase.SetActive(false);
            skyDeathGroup.transform.parent.gameObject.SetActive(false);
            uiDeathGroup.transform.parent.gameObject.SetActive(false);

            if (!SceneIsRoyaleMode())
            {
                enabled = false;
                return;
            }
        }

        gunHands[0].itemInSlot = emptyItem;
        gunHands[0].stackedItems = new List<StackedInventoryItem>();
        gunHands[1].itemInSlot = emptyItem;
        gunHands[1].stackedItems = new List<StackedInventoryItem>();

        for (int i = 0; i < numSlots; i++)
        {
            leftHandItems[i].itemInSlot = emptyItem;
            leftHandItems[i].stackedItems = new List<StackedInventoryItem>();
        }

        for (int i = 0; i < numSlots; i++)
        {
            rightHandItems[i].itemInSlot = emptyItem;
            rightHandItems[i].stackedItems = new List<StackedInventoryItem>();
        }

        if (photonView.IsMine)
        {
            player = FindObjectOfType<GorillaLocomotion.Player>();
        }

        for (int i = 0; i < renderersToTint.Length; i++)
        {
            renderersToTint[i].material.EnableKeyword("_EMISSION");
        }
    }

    public void AdjustSkinEligibility()
    {
        for (int i = 0; i < gunSkins.Length; i++)
        {
            gunSkins[i].skinActiveObject.SetActive(PlayerPrefs.GetInt(CleanItemName(gunSkins[i].itemName), 0) == 1);
            gunSkins[i].skinBaseObject.SetActive(false);
            gunSkins[i].canRender = PlayerPrefs.GetFloat(CleanItemName(gunSkins[i].itemName), 0) == 1 && PlayerPrefs.GetFloat(gunSkins[i].baseName, 0) == 1;
        }
    }

    string CleanItemName(string name)
    {
        string result = name.Replace("Right", "");
        result = result.Replace("Left", "");
        return result;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        if (photonView.IsMine)
        {
            alive = true;
            StartCoroutine(HealthPing());
        }
    }

    public void Update()
    {
        friendlyIndicator.SetActive(PhotonRoyalePlayer.me != this && PhotonRoyalePlayer.me.playersInTeam.Contains(this));
        longRangeFriendlyIndicator.SetActive(friendlyIndicator.activeSelf && 
            Vector3.Distance(renderersToTint[0].transform.position, PhotonRoyalePlayer.me.renderersToTint[0].transform.position) > longDistance);

        if (photonView.IsMine)
        {
            freezeUI.alpha = freezeUI.alpha * (1.0f - 2.0f * Time.deltaTime);
            if (Time.time > lastSampleTime && photonView.IsMine)
            {
                lastSampleTime = Time.time + sampleTime;
                positions.Add(player.bodyCollider.transform.position);
                if (positions.Count > maxSamples)
                {
                    positions.RemoveAt(0);
                }
            }
        }

        for (int i = 0; i < renderersToTint.Length; i++)
        {
            renderersToTint[i].material.SetColor("_EmissionColor", Color.Lerp(frozenColor, normalColor, alive ? Mathf.Clamp(health / startingHealth, 0.0f, 1.0f) : 0.0f));
        }

        if (!photonView.IsMine || !(SceneIsRoyaleMode()))
        {
            bodyHealthBase.SetActive(false);
            uiHealthBase.SetActive(false);
            bodyAmmoBase.SetActive(false);
            uiAmmoBase.SetActive(false);
            uiInventoryBase.SetActive(false);
            leftBodyInventoryBase.SetActive(false);
            rightBodyInventoryBase.SetActive(false);
        }
        else
        {
            if (InputManager.instance.rightHandB.WasPressedThisFrame())
            {
                photonView.RPC("DisableHandSkins", RpcTarget.All, true);
                gunHands[1].DropAllFromSlot(player.headCollider.transform.position);
                gunHands[1].itemInSlot = emptyItem;
            }
            
            if (InputManager.instance.leftHandY.WasPressedThisFrame())
            {
                photonView.RPC("DisableHandSkins", RpcTarget.All, false);
                gunHands[0].DropAllFromSlot(player.headCollider.transform.position);
                gunHands[0].itemInSlot = emptyItem;
            }

            switch (healthMode)
            {
                case HealthMode.Body:
                {
                    bodyHealthBase.SetActive(true);
                    uiHealthBase.SetActive(false);
                    bodyFreezeBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Lerp(bodyBarMinSize.x, bodyBarMaxSize.x, health / startingHealth));
                    bodyFreezeBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(bodyBarMinSize.y, bodyBarMaxSize.y, health / startingHealth));
                    bodyHealthText.text = health.ToString("0");
                    break;
                }

                case HealthMode.UI:
                {
                    bodyHealthBase.SetActive(false);
                    uiHealthBase.SetActive(true);
                    uiFreezeBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Lerp(uiBarMinSize.x, uiBarMaxSize.x, health / startingHealth));
                    uiFreezeBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(uiBarMinSize.y, uiBarMaxSize.y, health / startingHealth));
                    uiHealthText.text = health.ToString("0");
                    break;
                }
            }

            switch (ammoMode)
            {
                case AmmoMode.Body:
                {
                    bodyAmmoBase.SetActive(true);
                    uiAmmoBase.SetActive(false);
                    bodyAmmoBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Lerp(bodyAmmoBarMinSize.x, bodyAmmoBarMaxSize.x, ammo / 200));
                    bodyAmmoBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(bodyAmmoBarMinSize.y, bodyAmmoBarMaxSize.y, ammo / 200));
                    bodyAmmoText.text = ammo.ToString("0");
                    break;
                }

                case AmmoMode.UI:
                {
                    bodyAmmoBase.SetActive(false);
                    uiAmmoBase.SetActive(true);
                    uiAmmoBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Lerp(uiAmmoBarMinSize.x, uiAmmoBarMaxSize.x, ammo / 200));
                    uiAmmoBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(uiAmmoBarMinSize.y, uiAmmoBarMaxSize.y, ammo / 200));
                    uiAmmoText.text = ammo.ToString("0");
                    break;
                }
            }

            slotAimedAtLeft = -1;
            slotAimedAtRight = -1;

            #if UNITY_EDITOR
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                SwitchSlotWithHand(true, 0);
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                SwitchSlotWithHand(true, 1);
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                SwitchSlotWithHand(false, 0);
            }
            else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                SwitchSlotWithHand(false, 1);
            }

            if (Keyboard.current.backspaceKey.wasPressedThisFrame)
            {
                DropEverything();
            }
            #else
            if (Mathf.Abs(InputManager.instance.GetLeftThumbstick().x) > 0.5f)
            {
                slotAimedAtLeft = InputManager.instance.GetLeftThumbstick().x < 0.0f ? 0 : 1;
                if (InputManager.instance.leftHandGrip.WasPressedThisFrame())
                {
                    SwitchSlotWithHand(true, slotAimedAtLeft);
                }
            }

            if (Mathf.Abs(InputManager.instance.GetRightThumbstick().x) > 0.5f)
            {
                slotAimedAtRight = InputManager.instance.GetRightThumbstick().x < 0.0f ? 0 : 1;
                if (InputManager.instance.rightHandGrip.WasPressedThisFrame())
                {
                    SwitchSlotWithHand(false, slotAimedAtRight);
                }
            }
            #endif

            switch (itemMode)
            {
                case ItemMode.Body:
                {
                    leftBodyInventoryBase.SetActive(true);
                    rightBodyInventoryBase.SetActive(true);
                    uiInventoryBase.SetActive(false);

                    for (int i = 0; i < leftHandItems.Length; i++)
                    {
                        inventoryBodyLeftSlotCount[i].text = !leftHandItems[i].ItemInSlot() ? "" : 
                            leftHandItems[i].stackedItems.Count > 0 ? leftHandItems[i].stackedItems.Count.ToString() : "";
                        inventoryBodyLeftSlots[i].color = !leftHandItems[i].ItemInSlot() ? new Color(1f, 1f, 1f, 0f) : Color.white;
                        inventoryBodyLeftSlots[i].sprite = !leftHandItems[i].ItemInSlot() ? null : 
                            leftHandItems[i].stackedItems.Count > 0 ? leftHandItems[i].stackedItems[0].itemSprite : leftHandItems[i].itemInSlot.itemSprite;
                        inventoryBodyLeftSlotHighlights[i].gameObject.SetActive(slotAimedAtLeft == i);
                    }

                    for (int i = 0; i < rightHandItems.Length; i++)
                    {
                        inventoryBodyRightSlotCount[i].text = !rightHandItems[i].ItemInSlot() ? "" : 
                            rightHandItems[i].stackedItems.Count > 0 ? rightHandItems[i].stackedItems.Count.ToString() : "";
                        inventoryBodyRightSlots[i].color = !rightHandItems[i].ItemInSlot() ? new Color(1f, 1f, 1f, 0f) : Color.white;
                        inventoryBodyRightSlots[i].sprite = !rightHandItems[i].ItemInSlot() ? null : 
                            rightHandItems[i].stackedItems.Count > 0 ? rightHandItems[i].stackedItems[0].itemSprite : rightHandItems[i].itemInSlot.itemSprite;
                        inventoryBodyRightSlotHighlights[i].gameObject.SetActive(slotAimedAtRight == i);
                    }
                    break;
                }

                case ItemMode.UI:
                {
                    leftBodyInventoryBase.SetActive(false);
                    rightBodyInventoryBase.SetActive(false);
                    uiInventoryBase.SetActive(true);

                    for (int i = 0; i < leftHandItems.Length; i++)
                    {
                        inventoryUILeftSlotCount[i].text = !leftHandItems[i].ItemInSlot() ? "" : 
                            leftHandItems[i].stackedItems.Count > 0 ? leftHandItems[i].stackedItems.Count.ToString() : "";
                        inventoryUILeftSlots[i].color = !leftHandItems[i].ItemInSlot() ? new Color(1f, 1f, 1f, 0f) : Color.white;
                        inventoryUILeftSlots[i].sprite = !leftHandItems[i].ItemInSlot() ? null : 
                            leftHandItems[i].stackedItems.Count > 0 ? leftHandItems[i].stackedItems[0].itemSprite : leftHandItems[i].itemInSlot.itemSprite;
                        inventoryUILeftSlotHighlights[i].gameObject.SetActive(slotAimedAtLeft == i);
                    }

                    for (int i = 0; i < rightHandItems.Length; i++)
                    {
                        inventoryUIRightSlotCount[i].text = !rightHandItems[i].ItemInSlot() ? "" : 
                            rightHandItems[i].stackedItems.Count > 0 ? rightHandItems[i].stackedItems.Count.ToString() : "";
                        inventoryUIRightSlots[i].color = !rightHandItems[i].ItemInSlot() ? new Color(1f, 1f, 1f, 0f) : Color.white;
                        inventoryUIRightSlots[i].sprite = !rightHandItems[i].ItemInSlot() ? null : 
                            rightHandItems[i].stackedItems.Count > 0 ? rightHandItems[i].stackedItems[0].itemSprite : rightHandItems[i].itemInSlot.itemSprite;
                        inventoryUIRightSlotHighlights[i].gameObject.SetActive(slotAimedAtRight == i);
                    }
                    break;
                }
                
                case ItemMode.Belt:
                {
                    break;
                }
            }
        }
    }

    [PunRPC]
    public void ExplosiveKnockback(Vector3 direction, float force)
    {
        if (photonView.IsMine)
        {
            player.ignoreHandCollision = true;
            player.playerRigidBody.velocity += direction * force;
            StartCoroutine(ReturnHands());
        }
    }

    [PunRPC]
    void DisableHandSkins(bool hand)
    {
        for (int i = 0; i < gunSkins.Length; i++)
        {
            if (gunSkins[i].rightHand == hand)
            {
                gunSkins[i].skinBaseObject.SetActive(false);
            }
        }
    }

    [PunRPC]
    public void SwitchForSkin(bool hand, int skin)
    {
        DisableHandSkins(hand);
        gunSkins[skin].skinBaseObject.transform.parent.gameObject.SetActive(true);
        gunSkins[skin].skinBaseObject.SetActive(true);
        gunSkins[skin].skinActiveObject.SetActive(true);

        if (!photonView.IsMine)
        {
        }
        else
        {
            PhotonGun gun = gunHands[hand ? 1 : 0].objectBase.GetComponent<PhotonGun>();
            if (gun != null)
            {
                gun.photonView.RPC("SetGunVisibility", RpcTarget.All, false);
            }
        }
    }

    public int SkinActiveForItem(InventorySlot slot, bool handID)
    {
        for (int i = 0; i < gunSkins.Length; i++)
        {
            if (gunSkins[i].itemType == slot.itemInSlot.itemType && handID == gunSkins[i].rightHand && 
                gunSkins[i].canRender)
            {
                return i;
            }
        }
        return -1;
    }

    public void MakeHeldItemsSkinned()
    {
        int leftSkin = SkinActiveForItem(gunHands[0], false);
        int rightSkin = SkinActiveForItem(gunHands[1], true);
        if (leftSkin != -1)
        {
            photonView.RPC("SwitchForSkin", RpcTarget.All, false, leftSkin);
        }

        if (rightSkin != -1)
        {
            photonView.RPC("SwitchForSkin", RpcTarget.All, true, rightSkin);
        }
    }

    public IEnumerator ReturnHands()
    {
        yield return new WaitForSeconds(0.2f);
        player.ignoreHandCollision = false;
    }

    public void SwitchSlotWithHand(bool leftHand, int slot)
    {
        InventorySlot handSlot = leftHand ? gunHands[0] : gunHands[1];
        if (!handSlot.itemInSlot.canBePutInPocket)
        {
            return;
        }

        if (handSlot.itemInSlot.itemType != ItemType.EMPTY || handSlot.stackedItems.Count > 0)
        {
            handSlot.DeactivateItem();
        }
        photonView.RPC("DisableHandSkins", RpcTarget.All, !leftHand);
        GameObject slotObject = handSlot.objectBase;
        Transform slotJoint = handSlot.objectJoint;
        List<StackedInventoryItem> stacks = handSlot.stackedItems;
        InventoryItem handItem = handSlot.itemInSlot;

        InventorySlot beltSlot = leftHand ? leftHandItems[slot] : rightHandItems[slot];
        GameObject beltObject = beltSlot.objectBase;
        Transform beltJoint = beltSlot.objectJoint;

        handSlot.itemInSlot = beltSlot.itemInSlot;
        handSlot.stackedItems = beltSlot.stackedItems;
        handSlot.objectBase = beltObject;
        handSlot.objectJoint = beltJoint;

        if (handSlot.itemInSlot.itemType != ItemType.EMPTY || handSlot.stackedItems.Count > 0)
        {
            handSlot.ActivateItem();
        }
        beltSlot.stackedItems = stacks;
        beltSlot.itemInSlot = handItem;
        beltSlot.objectBase = slotObject;
        beltSlot.objectJoint = slotJoint;

        MakeHeldItemsSkinned();
    }

    public void MoveToNextStackItem(GameObject item, bool leftHand)
    {
        InventorySlot handSlot = leftHand ? gunHands[0] : gunHands[1];
        InventorySlot[] otherSlots = leftHand ? leftHandItems : rightHandItems;

        for (int i = 0; i < handSlot.stackedItems.Count; i++)
        {
            if (handSlot.stackedItems[i].objectBase == item)
            {
                handSlot.stackedItems.RemoveAt(i);
            }
        }

        if (handSlot.stackedItems.Count > 0)
        {
            handSlot.stackedItems[0].objectBase.GetComponent<PhotonView>().RPC("Stacked", RpcTarget.Others, true);
            handSlot.stackedItems[0].objectBase.SetActive(true);
        }
        else
        {
            if (otherSlots[0].itemInSlot.itemType != ItemType.EMPTY || otherSlots[0].stackedItems.Count > 0)
            {
                SwitchSlotWithHand(leftHand, 0);
            }
            else if (otherSlots[1].itemInSlot.itemType != ItemType.EMPTY || otherSlots[1].stackedItems.Count > 0)
            {
                SwitchSlotWithHand(leftHand, 1);
            }
        }
    }

    public void EmptySlot(bool leftHand)
    {
        InventorySlot handSlot = leftHand ? gunHands[0] : gunHands[1];
        handSlot.CleanSlot(emptyItem);
    }

    public void PutItemInSlot(bool leftHand, InventoryItem newItem, GameObject objectBase, Transform objectJoint)
    {
        if (!gunHands[leftHand ? 0 : 1].ItemInSlot())
        {
            if (!newItem.canStack)
            {
                Debug.Log("Put item in gun hand " + (leftHand ? "Left" : "Right"));
                gunHands[leftHand ? 0 : 1].itemInSlot = newItem;
                gunHands[leftHand ? 0 : 1].objectBase = objectBase;
                gunHands[leftHand ? 0 : 1].objectJoint = objectJoint;
                gunHands[leftHand ? 0 : 1].objectBase.GetComponent<PhotonView>().RPC("Stacked", RpcTarget.All, true);
            }
            else
            {
                Debug.Log("Put stacked item in gun hand " + (leftHand ? "Left" : "Right"));
                gunHands[leftHand ? 0 : 1].stackedItems.Add(new StackedInventoryItem(newItem));
                gunHands[leftHand ? 0 : 1].stackedItems[gunHands[leftHand ? 0 : 1].stackedItems.Count - 1].objectBase = objectBase;
                gunHands[leftHand ? 0 : 1].stackedItems[gunHands[leftHand ? 0 : 1].stackedItems.Count - 1].objectJoint = objectJoint;
            }
        }
        else if (gunHands[leftHand ? 0 : 1].stackedItems.Count > 0 && 
                 gunHands[leftHand ? 0 : 1].stackedItems[0].itemType == newItem.itemType && 
                 newItem.canStack && 
                 gunHands[leftHand ? 0 : 1].stackedItems.Count < newItem.maxStacked)
        {
            Debug.Log("Added stacked item in gun hand " + (leftHand ? "Left" : "Right"));
            gunHands[leftHand ? 0 : 1].stackedItems.Add(new StackedInventoryItem(newItem));
            gunHands[leftHand ? 0 : 1].stackedItems[gunHands[leftHand ? 0 : 1].stackedItems.Count - 1].objectBase = objectBase;
            gunHands[leftHand ? 0 : 1].stackedItems[gunHands[leftHand ? 0 : 1].stackedItems.Count - 1].objectJoint = objectJoint;
            objectBase.GetComponent<PhotonView>().RPC("Stacked", RpcTarget.Others, false);
            objectBase.SetActive(false);
        }
        else
        {
            if (newItem.canBePutInPocket)
            {
                objectBase.GetComponent<PhotonView>().RPC("Stacked", RpcTarget.Others, false);
                objectBase.SetActive(false);
                for (int i = 0; i < numSlots; i++)
                {
                    if (leftHand ? !leftHandItems[i].ItemInSlot() : !rightHandItems[i].ItemInSlot())
                    {
                        if (leftHand)
                        {
                            if (!newItem.canStack)
                            {
                                Debug.Log("Put item in slot " + (leftHand ? "Left" : "Right"));
                                leftHandItems[i].itemInSlot = newItem;
                                leftHandItems[i].objectBase = objectBase;
                                leftHandItems[i].objectJoint = objectJoint;
                            }
                            else
                            {
                                Debug.Log("Put stacked item in slot " + (leftHand ? "Left" : "Right"));
                                leftHandItems[i].stackedItems.Add(new StackedInventoryItem(newItem));
                                leftHandItems[i].stackedItems[leftHandItems[i].stackedItems.Count - 1].objectBase = objectBase;
                                leftHandItems[i].stackedItems[leftHandItems[i].stackedItems.Count - 1].objectJoint = objectJoint;
                            }
                            break;
                        }
                        else
                        {
                            if (!newItem.canStack)
                            {
                                Debug.Log("Put item in slot " + (leftHand ? "Left" : "Right"));
                                rightHandItems[i].itemInSlot = newItem;
                                rightHandItems[i].objectBase = objectBase;
                                rightHandItems[i].objectJoint = objectJoint;
                            }
                            else
                            {
                                Debug.Log("Put stacked item in slot " + (leftHand ? "Left" : "Right"));
                                rightHandItems[i].stackedItems.Add(new StackedInventoryItem(newItem));
                                rightHandItems[i].stackedItems[rightHandItems[i].stackedItems.Count - 1].objectBase = objectBase;
                                rightHandItems[i].stackedItems[rightHandItems[i].stackedItems.Count - 1].objectJoint = objectJoint;
                            }
                            break;
                        }
                    }
                    else if (leftHand && leftHandItems[i].stackedItems.Count > 0 && 
                            leftHandItems[i].stackedItems[0].itemType == newItem.itemType && 
                            newItem.canStack && 
                            leftHandItems[i].stackedItems.Count < newItem.maxStacked)
                    {
                        Debug.Log("Added stacked item in slot " + (leftHand ? "Left" : "Right"));
                        leftHandItems[i].stackedItems.Add(new StackedInventoryItem(newItem));
                        leftHandItems[i].stackedItems[leftHandItems[i].stackedItems.Count - 1].objectBase = objectBase;
                        leftHandItems[i].stackedItems[leftHandItems[i].stackedItems.Count - 1].objectJoint = objectJoint;
                        break;
                    }
                    else if (!leftHand && rightHandItems[i].stackedItems.Count > 0 && 
                            rightHandItems[i].stackedItems[0].itemType == newItem.itemType && 
                            newItem.canStack && 
                            rightHandItems[i].stackedItems.Count < newItem.maxStacked)
                    {
                        Debug.Log("Added stacked item in slot " + (leftHand ? "Left" : "Right"));
                        rightHandItems[i].stackedItems.Add(new StackedInventoryItem(newItem));
                        rightHandItems[i].stackedItems[rightHandItems[i].stackedItems.Count - 1].objectBase = objectBase;
                        rightHandItems[i].stackedItems[rightHandItems[i].stackedItems.Count - 1].objectJoint = objectJoint;
                        break;
                    }
                }
            }
            else
            {
                Debug.Log("Switching");
                for (int i = 0; i < numSlots; i++)
                {
                    if (leftHand ? !leftHandItems[i].ItemInSlot() : !rightHandItems[i].ItemInSlot())
                    {
                        Debug.Log("Switched with " + i);
                        SwitchSlotWithHand(leftHand, i);
                        PutItemInSlot(leftHand, newItem, objectBase, objectJoint);
                        break;
                    }
                }
            }
        }
    }

    public bool LeftHandHasSlot(InventoryItem item)
    {
        if (!gunHands[0].itemInSlot.canBePutInPocket)
        {
            return false;
        }

        bool slotOpen = false;
        for (int i = 0; i < leftHandItems.Length; i++)
        {
            if (leftHandItems[i].RoomInSlot(item))
            {
                slotOpen = true;
            }
        }
        return (gunHands[0].RoomInSlot(item) || slotOpen);
    }

    public bool RightHandHasSlot(InventoryItem item)
    {
        if (!gunHands[1].itemInSlot.canBePutInPocket)
        {
            return false;
        }

        bool slotOpen = false;
        for (int i = 0; i < rightHandItems.Length; i++)
        {
            if (rightHandItems[i].RoomInSlot(item))
            {
                slotOpen = true;
            }
        }
        return (gunHands[1].RoomInSlot(item) || slotOpen);
    }

    public bool ShouldTakeDamage()
    {
        if (PhotonRoyaleLobby.instance != null && PhotonRoyaleLobby.instance.activePlayersList.Contains(photonView.ControllerActorNr) && 
            !PhotonRoyaleLobby.instance.celebrating)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void TakeDamage(int damage)
    {
        if (alive && ShouldTakeDamage())
        {
            health -= damage;
            if (photonView.IsMine && health <= 0)
            {
                photonView.RPC("Dead", RpcTarget.All);
                if (photonView.IsMine)
                {
                    if (LifetimeRankingBoard.instance != null && !alive)
                    {
                        LifetimeRankingBoard.SendDeathData();
                    }
                }
            }
        }
    }

    public static bool SceneIsRoyaleMode()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return sceneName == "PenguinRoyale" || sceneName == "PenguinRoyaleFFA" || sceneName == "PenguinRoyaleDuos" || sceneName == "PenguinRoyaleSquads" || 
                            sceneName == "PenguinRoyaleChaos";
    }

    public IEnumerator CanIBeFree()
    {
        while (!alive)
        {
            yield return new WaitForSeconds(10.0f);
            StartCoroutine(CheckIfGameIsOver());
        }
    }

    [PunRPC]
    public void Invited(int invitation)
    {
        for (int i = 0; i < PopUpComputer.instance.inviteGroups.Length; i++)
        {
            if (PopUpComputer.instance.inviteGroups[i].userLink != null && PopUpComputer.instance.inviteGroups[i].userLink.photonView.ControllerActorNr == invitation)
            {
                PopUpComputer.instance.inviteSource.Play();
                PopUpComputer.instance.inviteGroups[i].inviteText.text = "Accept";
                PopUpComputer.instance.inviteGroups[i].asking = true;
                break;
            }
        }
    }

    [PunRPC]
    public void TeamFormed(int[] players, int newTeamID)
    {
        string debug = "Team Formed: " + newTeamID.ToString() + "\n";
        bool teamInQueue = false;
        for (int i = 0; i < players.Length; i++)
        {
            debug += "num " + players[i].ToString() + "\n";
            if (PhotonRoyaleLobby.instance != null)
            {
                if (PhotonRoyaleLobby.instance.playersQueueList.Contains(players[i]))
                {
                    teamInQueue = true;
                }
            }
        }

        List<PhotonRoyalePlayer> teamPlayers = new List<PhotonRoyalePlayer>();
        PhotonRoyalePlayer[] allPlayers = FindObjectsOfType<PhotonRoyalePlayer>();
        for (int i = 0; i < allPlayers.Length; i++)
        {
            if (players.Contains(allPlayers[i].photonView.ControllerActorNr))
            {
                debug += "Name " + allPlayers[i].GetComponent<PhotonVRPlayer>().NameText.text + "\n";
                allPlayers[i].teamID = newTeamID;
                allPlayers[i].playersInTeam.Clear();
                for (int j = 0; j < allPlayers.Length; j++)
                {
                    if (players.Contains(allPlayers[j].photonView.ControllerActorNr))
                    {
                        debug += "Name num " + allPlayers[j].GetComponent<PhotonVRPlayer>().NameText.text + "\n";
                        allPlayers[i].playersInTeam.Add(allPlayers[j]);
                    }
                }
                teamPlayers = allPlayers[i].playersInTeam;
            }
        }

        if (PhotonRoyaleLobby.instance != null && PhotonRoyaleLobby.instance.countingDown && teamInQueue)
        {
            for (int i = 0; i < teamPlayers.Count; i++)
            {
                if (!PhotonRoyaleLobby.instance.playersQueueList.Contains(teamPlayers[i].photonView.ControllerActorNr))
                {
                    PhotonRoyaleLobby.instance.playersQueueList.Add(teamPlayers[i].photonView.ControllerActorNr);
                }
            }
        }
        Debug.Log(debug);
    }

    [PunRPC]
    public void SyncState(float curHealth, bool amAlive, bool crowned, int curAmmo, int team, int[] idsInTeam)
    {
        if (!synced)
        {
            ammo = curAmmo;
            health = curHealth;
            alive = amAlive;
            teamID = team;

            playersInTeam.Clear();
            PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
            for (int j = 0; j < players.Length; j++)
            {
                if (idsInTeam.Contains(players[j].photonView.ControllerActorNr))
                {
                    playersInTeam.Add(players[j]);
                }
            }

            if (!alive)
            {
                deathEffect.Play();
            }
            goldCrown.SetActive(crowned);
            synced = true;
        }
    }

    [PunRPC]
    public void TookHit(float newHealth)
    {
        if (newHealth < health && health > 0 && ShouldTakeDamage())
        {
            health = newHealth;
            if (photonView.IsMine && health <= 0)
            {
                photonView.RPC("Dead", RpcTarget.All);
                if (photonView.IsMine)
                {
                    if (LifetimeRankingBoard.instance != null && !alive)
                    {
                        LifetimeRankingBoard.SendDeathData();
                    }
                }
            }
            else if (health > 0)
            {
                for (int i = 0; i < renderersToTint.Length; i++)
                {
                    renderersToTint[i].material.SetColor("_EmissionColor", Color.Lerp(frozenColor, normalColor, alive ? Mathf.Clamp(health / startingHealth, 0.0f, 1.0f) : 0.0f));
                }
                ouchSource.Stop();
                ouchSource.pitch = Random.Range(0.9f, 1.1f);
                ouchSource.Play();
            }
        }
    }

    [PunRPC]
    public void TookStormHit(float newHealth)
    {
        if (newHealth < health && health > 0 && ShouldTakeDamage())
        {
            health = newHealth;
            if (photonView.IsMine && health <= 0)
            {
                if (PhotonRoyaleLobby.instance != null)
                {
                    PhotonRoyaleLobby.instance.photonView.RPC("PlayerFrozen", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, -1);
                }

                if (photonView.IsMine)
                {
                    if (LifetimeRankingBoard.instance != null && !alive)
                    {
                        LifetimeRankingBoard.SendDeathData();
                    }
                }
                photonView.RPC("Dead", RpcTarget.All);
            }
            else if (health > 0)
            {
                for (int i = 0; i < renderersToTint.Length; i++)
                {
                    renderersToTint[i].material.SetColor("_EmissionColor", Color.Lerp(frozenColor, normalColor, alive ? Mathf.Clamp(health / startingHealth, 0.0f, 1.0f) : 0.0f));
                }
                ouchSource.Stop();
                ouchSource.pitch = Random.Range(0.9f, 1.1f);
                ouchSource.Play();
            }
        }
    }

    [PunRPC]
    public void CheckHit(Vector3 position, int damage, int playerID)
    {
        if (photonView.IsMine && health > 0)
        {
            string results = "";
            for (int i = 0; i < positions.Count; i++)
            {
                if (Vector3.Distance(positions[i], position) < hitRadius)
                {
                    if (health - damage <= 0 && PhotonRoyaleLobby.instance != null)
                    {
                        PhotonRoyaleLobby.instance.photonView.RPC("PlayerFrozen", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, playerID);
                    }
                    photonView.RPC("TookHit", RpcTarget.All, health - damage);
                    return;
                }
                else
                {
                    results += Vector3.Distance(positions[i], position).ToString() + "\n";
                }
            }
        }
    }

    [PunRPC]
    public void Dead()
    {
        for (int i = 0; i < renderersToTint.Length; i++)
        {
            renderersToTint[i].material.SetColor("_EmissionColor", frozenColor);
        }

        alive = false;
        if (ammo > 0 && photonView.IsMine)
        {
            if (PhotonRoyaleLobby.instance != null)
            {
                for (int i = 0; i < GunSpawner.instance.deathAmmo.Count; i++)
                {
                    if (!GunSpawner.instance.deathAmmo[i].gameObject.activeSelf)
                    {
                        PhotonVRPlayer vrPlayer = GetComponent<PhotonVRPlayer>();
                        GunSpawner.instance.photonView.RPC("MoveDeathAmmo", RpcTarget.All, 
                            vrPlayer.Head.position + new Vector3(vrPlayer.Head.forward.x, 0.0f, vrPlayer.Head.forward.z).normalized * 1.0f, i, ammo);
                        break;
                    }
                }
            }
        }

        ammo = 0;
        if (photonView.IsMine)
        {
            player.DisableActiveColliders();
            player.playerRigidBody.useGravity = false;
            player.SwitchToObserver();
            PopUpComputer.instance.blocked = false;

            StartCoroutine(CheckIfGameIsOver());
            StartCoroutine(CanIBeFree());
        }

        if (!photonView.IsMine)
        {
            deathEffect.Play();
        }
        else
        {
            DropEverything();
        }

        if (PhotonRoyaleLobby.instance != null)
        {
            PhotonRoyaleLobby.instance.lastDeath = renderersToTint[0].transform.position;
        }
        DisableHandSkins(true);
        DisableHandSkins(false);
        deathSource.pitch = Random.Range(0.9f, 1.1f);
        deathSource.Play();
    }

    public void DropEverything()
    {
        gunHands[0].DropAllFromSlot(player.headCollider.transform.position);
        gunHands[0].itemInSlot = emptyItem;
        gunHands[1].DropAllFromSlot(player.headCollider.transform.position);
        gunHands[1].itemInSlot = emptyItem;

        leftHandItems[0].DropAllFromSlot(player.headCollider.transform.position);
        leftHandItems[0].itemInSlot = emptyItem;
        leftHandItems[1].DropAllFromSlot(player.headCollider.transform.position);
        leftHandItems[1].itemInSlot = emptyItem;
        rightHandItems[0].DropAllFromSlot(player.headCollider.transform.position);
        rightHandItems[0].itemInSlot = emptyItem;
        rightHandItems[1].DropAllFromSlot(player.headCollider.transform.position);
        rightHandItems[1].itemInSlot = emptyItem;
    }

    [PunRPC]
    public void ReportTopTen(int rank)
    {
        for (int i = 0; i < badgeObjects.Length; i++)
        {
            badgeObjects[i].SetActive(false);
        }
        badgeObjects[rank].SetActive(true);
        leaderboardPos = rank;
    }

    [PunRPC]
    public void Heal(int healVal)
    {
        if (health < maxHealth)
        {
            if (photonView.IsMine)
            {
                healSource.pitch = Mathf.Lerp(minHealPitch, maxHealPitch, (health / maxHealth));
                healSource.Play();
            }
            
            health += healVal;
            if (health > maxHealth)
            {
                health = maxHealth;
            }

            for (int i = 0; i < renderersToTint.Length; i++)
            {
                renderersToTint[i].material.SetColor("_EmissionColor", Color.Lerp(frozenColor, normalColor, alive ? Mathf.Clamp(health / startingHealth, 0.0f, 1.0f) : 0.0f));
            }

            if (PhotonRoyaleLobby.instance != null && PhotonRoyaleLobby.instance.useTeams && !alive && health > 0)
            {
                alive = true;
                deathEffect.Stop();
                ammo = startAmmo;

                for (int i = 0; i < renderersToTint.Length; i++)
                {
                    renderersToTint[i].material.SetColor("_EmissionColor", Color.Lerp(frozenColor, normalColor, 1.0f));
                }

                if (photonView.IsMine)
                {
                    player.EnableActiveColliders();
                    player.playerRigidBody.isKinematic = false;
                    player.enabled = true;
                    player.ignoreHandCollision = false;
                    StartCoroutine(HealthPing());
                    player.SwitchToNormal();
                    LifetimeRankingBoard.SendReviveData();
                }
            }
        }
    }

    [PunRPC]
    public void Revive()
    {
        health = startingHealth;
        alive = true;
        deathEffect.Stop();
        goldCrown.SetActive(false);
        ammo = startAmmo;

        for (int i = 0; i < renderersToTint.Length; i++)
        {
            renderersToTint[i].material.SetColor("_EmissionColor", Color.Lerp(frozenColor, normalColor, 1.0f));
        }

        if (photonView.IsMine)
        {
            player.EnableActiveColliders();
            player.playerRigidBody.isKinematic = false;
            player.enabled = true;
            player.ignoreHandCollision = false;
            StartCoroutine(HealthPing());
            player.SwitchToNormal();
        }
    }

    [PunRPC]
    public void ResetToAirship()
    {
        ammo = startAmmo;
        health = startingHealth;
        if (photonView.IsMine)
        {
            photonView.RPC("DisableHandSkins", RpcTarget.All, true);
            photonView.RPC("DisableHandSkins", RpcTarget.All, false);
            PopUpComputer.instance.blocked = false;
            DropEverything();
            player.SwitchToNormal();

            Transform randReturn = PhotonRoyaleLobby.instance != null ? PhotonRoyaleLobby.instance.returnPoints[Random.Range(0, PhotonRoyaleLobby.instance.returnPoints.Length)] : transform;
            player.DisableActiveColliders();
            player.wasLeftHandTouching = false;
            player.wasRightHandTouching = false;

            Vector3 landingVec = randReturn.position + new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f)).normalized * Random.Range(0.0f, 1.0f);
            player.playerRigidBody.isKinematic = true;
            player.transform.position = landingVec + new Vector3(player.transform.position.x - player.headCollider.transform.position.x, 0.0f, player.transform.position.z - player.headCollider.transform.position.z);
            player.InitializeValues();
            player.EnableActiveColliders();
            StartCoroutine(Freeze());
        }
    }

    public IEnumerator Freeze()
    {
        yield return new WaitForSeconds(0.05f);
        player.playerRigidBody.isKinematic = false;
    }

    [PunRPC]
    public void Winner()
    {
        StartCoroutine(DelayCrown());
        alive = true;
        health = startingHealth;
        deathEffect.Stop();
        if (LifetimeRankingBoard.instance != null && photonView.IsMine)
        {
            LifetimeRankingBoard.SendWinData();
        }
    }

    public IEnumerator DelayCrown()
    {
        yield return new WaitForSeconds(1.0f);
        goldCrown.SetActive(true);
    }

    public IEnumerator HealthPing()
    {
        float lastHealth = health;
        while (alive)
        {
            yield return new WaitForSeconds(0.05f);
            if (health < lastHealth)
            {
                lastHealth = health;
                photonView.RPC("TookHit", RpcTarget.Others, health);
            }
        }
    }

    public IEnumerator CheckIfGameIsOver()
    {
        if (PhotonRoyaleLobby.instance != null)
        {
            if (!PhotonRoyaleLobby.instance.celebrating)
            {
                PhotonRoyalePlayer[] players = FindObjectsOfType<PhotonRoyalePlayer>();
                List<PhotonRoyalePlayer> winners = new List<PhotonRoyalePlayer>();

                if (PhotonRoyaleLobby.instance.useTeams)
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (players[i].alive && PhotonRoyaleLobby.instance.activePlayersList.Contains(players[i].photonView.ControllerActorNr))
                        {
                            winners.Add(players[i]);
                        }
                    }

                    bool oneTeam = true;
                    for (int i = 1; i < winners.Count; i++)
                    {
                        if (!winners[0].playersInTeam.Contains(winners[i]))
                        {
                            oneTeam = false;
                        }
                    }
                    
                    if (oneTeam)
                    {
                        if (winners.Count > 0)
                        {
                            for (int i = 0; i < players.Length; i++)
                            {
                                if (PhotonRoyaleLobby.instance.activePlayersList.Contains(players[i].photonView.ControllerActorNr))
                                {
                                    if (winners.Count > 0 && (winners.Contains(players[i]) || winners[0].playersInTeam.Contains(players[i])))
                                    {
                                        players[i].photonView.RPC("Winner", RpcTarget.All);
                                    }
                                }
                            }
                            PhotonRoyaleLobby.instance.photonView.RPC("CelebrateWinners", RpcTarget.All);
                            yield return new WaitForSeconds(5.0f);
                        }

                        for (int i = 0; i < players.Length; i++)
                        {
                            if (PhotonRoyaleLobby.instance.activePlayersList.Contains(players[i].photonView.ControllerActorNr))
                            {
                                if (!players[i].alive)
                                {
                                    players[i].photonView.RPC("Revive", RpcTarget.All);
                                }
                                players[i].photonView.RPC("ResetToAirship", players[i].photonView.Controller);
                            }
                        }
                        PhotonRoyaleLobby.instance.photonView.RPC("GameOver", RpcTarget.All);
                    }
                }
                else
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (players[i].alive && PhotonRoyaleLobby.instance.activePlayersList.Contains(players[i].photonView.ControllerActorNr))
                        {
                            winners.Add(players[i]);
                        }
                    }

                    if (winners.Count == 1)
                    {
                        winners[0].photonView.RPC("Winner", RpcTarget.All);
                        PhotonRoyaleLobby.instance.photonView.RPC("CelebrateWinners", RpcTarget.All);
                        yield return new WaitForSeconds(5.0f);
                    }
                    
                    if (winners.Count <= 1)
                    {
                        for (int i = 0; i < players.Length; i++)
                        {
                            if (PhotonRoyaleLobby.instance.activePlayersList.Contains(players[i].photonView.ControllerActorNr))
                            {
                                if (!players[i].alive)
                                {
                                    players[i].photonView.RPC("Revive", RpcTarget.All);
                                }
                                players[i].photonView.RPC("ResetToAirship", players[i].photonView.Controller);
                            }
                        }
                        PhotonRoyaleLobby.instance.photonView.RPC("GameOver", RpcTarget.All);
                    }
                }
            }
            PhotonRoyaleLobby.instance.checkRoutine = null;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (SceneIsRoyaleMode())
        {
            List<int> playerInts = new List<int>();
            for (int i = 0; i < playersInTeam.Count; i++)
            {
                playerInts.Add(playersInTeam[i].photonView.ControllerActorNr);
            }
            photonView.RPC("SyncState", newPlayer, health, alive, goldCrown.activeSelf, ammo, teamID, playerInts.ToArray());

            if (leaderboardPos != -1 && leaderboardPos < 10)
            {
                photonView.RPC("ReportTopTen", newPlayer, leaderboardPos);
            }
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        if (SceneIsRoyaleMode() && PhotonRoyaleLobby.instance.photonView.IsMine && PhotonRoyaleLobby.instance.gameStarted)
        {
            StartCoroutine(CheckIfGameIsOver());
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        playersInTeam.Clear();
        playersInTeam.Add(this);
        teamID = -1;
    }
}
