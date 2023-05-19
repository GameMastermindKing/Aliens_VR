using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using PlayFab;
using PlayFab.ClientModels;

public class NewShop : MonoBehaviourPunCallbacks
{
    public enum CosmeticType
    {
        Tophat,
        Fish,
        Snow_Board,
        Ice_Crown,
        Pebble,
        Glasses,
        Beanie,
        Skis,
        Fire_Skis,
        Red_Goggles,
        Fire_Beanie,
        Chain,
        Shoes,
        RentalSkis,
        Jetpack,
        Brush,
        Torch,
        Jump_Gloves,
        Polar_Bear_Costume,
        Turkey_Bundle,
        Blue_Coat,
        Red_Coat,
        Glow_Penguin,
        RainbowBrush,
        Tree_Cosmetic,
        Santa_Suit,
        Elf,
        Rudolph,
        CyberPenguin,
        CandyCane,
        White_Skis,
        Sweater,
        Present,
        HotChocolate,
        Gingerbread,
        NewYearsTopHat,
        Pink_Glasses,
        RocketScooterPaint,
        RocketScooterPurple,
        RocketScooterBumbleBee,
        PaintHelmet,
        Helmet,
        RocketScooterSciFi,
        JetpackSciFi,
        RedShovel,
        GreenShovel,
        BlueShovel,
        WoodSled,
        SciFiSled,
        FoamSled,
        FishSled,
        UFO,
        PenguinPet,
        BearPet,
        ChickPet,
        RedUFO,
        BlackUFO,
        ToastPan,
        TubeSled,
        IceSled,
        LeprechaunHat,
        LeprechaunPet,
        CyberGlider,
        GoldenGlider,
        PenguinGlider,
        CrownGlider,
        GasMask,
        SnowCamo,
        PaintGuns,
        CyberGuns,
        GoldGuns,
        PaintGlider,
        Snowmobile,
        LavaSnowmobile,
        CyberSnowmobile,
        HairBowl,
        HairMohawk,
        HairFlathead,
        GorillaMech,
        BunnyMech,
        BunnyCostume,
        TeleporterDisc,
        RocketBroomstickStraw,
        RocketBroomstickPenguin,
        RocketBroomstickSciFi,
        BuckleShoe,
        PenguinNecklace,
        IceCubeNecklace,
        DiamondNecklace,
        LaserSaber,
        RainCoat,
        Umbrella,
        COUNT
    }

    public enum CosmeticPosition
    {
        Hand,
        Body,
        Foot,
        Neck,
        Eyes,
        Hair,
        Hat,
        Vehicle,
        Mode,
        Pet,
        Glider,
        COUNT
    }

    [System.Serializable]
    public class CosmeticEntry
    {
        public string cosmeticName;
        public string cosmeticCleanName;
        public string cosmeticDescription;
        public CosmeticType cosmeticType;
        public CosmeticPosition cosmeticPosition;
        public Sprite cosmeticSprite;
        public Vector3 mannequinPosition;
        public Vector3 mannequinRotation;
        public int cosmeticPrice;
        public bool purchased = false;
        public bool limited = false;
        public bool allowedInCompetitionModes = true;

        public void CopyTo(CosmeticEntry otherEntry)
        {
            otherEntry.cosmeticName = cosmeticName;
            otherEntry.cosmeticCleanName = cosmeticCleanName;
            otherEntry.cosmeticDescription = cosmeticDescription;
            otherEntry.cosmeticType = cosmeticType;
            otherEntry.cosmeticPosition = cosmeticPosition;
            otherEntry.cosmeticSprite = cosmeticSprite;
            otherEntry.mannequinPosition = mannequinPosition;
            otherEntry.mannequinRotation = mannequinRotation;
            otherEntry.cosmeticPrice = cosmeticPrice;
            otherEntry.purchased = purchased;
            otherEntry.limited = limited;
            otherEntry.allowedInCompetitionModes = allowedInCompetitionModes;
        }
    }

    [System.Serializable]
    public class VisibleCosmetic
    {
        public CosmeticType cosmeticType;
        public GameObject cosmeticObject;
    }

    public static List<CosmeticEntry> lastCosmeticsData;
    public static List<List<CosmeticEntry>> sortedCosmeticsData = new List<List<CosmeticEntry>>();
    public static bool dataGathered = false;

    public ShopConsole[] consoles;
    public VisibleCosmetic[] visibleCosmetics;
    public Transform mannequin;

    public EquipGunSkinButton[] cyberButtons;
    public EquipGunSkinButton[] goldButtons;
    public EquipGunSkinButton[] paintButtons;

    public NewShopCosmeticsConfiguration configuration;

    public void Start()
    {
        if (lastCosmeticsData == null)
        {
            lastCosmeticsData = configuration.GetEntries();
            for (int i = 0; i < lastCosmeticsData.Count; i++)
            {
                if (lastCosmeticsData[i] == null)
                {
                    Debug.Log(i);
                }
            }
            StartCoroutine(GetOwnedData());
        }
        else
        {
            StartCoroutine(GetOwnedPurchasedData());
        }
    }

    public void LookAtItem(CosmeticEntry item)
    {
        mannequin.localPosition = item.mannequinPosition;
        mannequin.localEulerAngles = item.mannequinRotation;

        paintButtons[0].transform.parent.gameObject.SetActive(item.cosmeticType == CosmeticType.PaintGuns && PlayerCosmetics.instance.CosmeticEquipped(item.cosmeticName));
        goldButtons[0].transform.parent.gameObject.SetActive(item.cosmeticType == CosmeticType.GoldGuns && PlayerCosmetics.instance.CosmeticEquipped(item.cosmeticName));
        cyberButtons[0].transform.parent.gameObject.SetActive(item.cosmeticType == CosmeticType.CyberGuns && PlayerCosmetics.instance.CosmeticEquipped(item.cosmeticName));

        for (int i = 0; i < visibleCosmetics.Length; i++)
        {
            visibleCosmetics[i].cosmeticObject.SetActive(false);
        }

        for (int i = 0; i < visibleCosmetics.Length; i++)
        {
            if (visibleCosmetics[i].cosmeticType == item.cosmeticType)
            {
                visibleCosmetics[i].cosmeticObject.SetActive(true);
            }
        }
    }

    public static IEnumerator GetOwnedData()
    {
        dataGathered = false;
        while (PlayFabManager.instance == null || !PlayFabManager.instance.loggedIn)
        {
            yield return new WaitForSeconds(0.5f);
        }

        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), OnGetUserInventory, OnGetUserInventoryFailed);
    }

    public static IEnumerator GetOwnedPurchasedData()
    {
        while (PlayFabManager.instance == null || !PlayFabManager.instance.loggedIn)
        {
            yield return new WaitForSeconds(0.5f);
        }

        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), OnGetUserInventoryChest, OnGetUserInventoryFailedChest);
    }

    public static void OnGetUserInventory(GetUserInventoryResult result)
    {
        for (int i = 0; i < result.Inventory.Count; i++)
        {
            for (int j = 0; j < lastCosmeticsData.Count; j++)
            {
                if (result.Inventory[i].ItemId == lastCosmeticsData[j].cosmeticName)
                {
                    lastCosmeticsData[j].purchased = true;
                    break;                    
                }
            }
        }
        
        for (int i = 0; i < (int)CosmeticPosition.COUNT; i++)
        {
            sortedCosmeticsData.Add(new List<CosmeticEntry>());
            for (int j = 0; j < lastCosmeticsData.Count; j++)
            {
                if ((int)lastCosmeticsData[j].cosmeticPosition == i)
                {
                    sortedCosmeticsData[i].Add(lastCosmeticsData[j]);
                }
            }
        }
        dataGathered = true;
        GorillaLocomotion.Player.Instance.StartCoroutine(WaitForPlayer());
    }

    public static IEnumerator WaitForPlayer()
    {
        while (PlayerCosmetics.instance == null)
        {
            yield return null;
        }
        PopUpComputer.instance.cosmeticsScript.UpdateEquipped();
    }

    public static void OnGetUserInventoryFailed(PlayFabError error)
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), OnGetUserInventory, OnGetUserInventoryFailed);
    }

    public static void OnGetUserInventoryChest(GetUserInventoryResult result)
    {
        for (int i = 0; i < result.Inventory.Count; i++)
        {
            for (int j = 0; j < lastCosmeticsData.Count; j++)
            {
                if (result.Inventory[i].ItemId == lastCosmeticsData[j].cosmeticName)
                {
                    lastCosmeticsData[j].purchased = true;
                    break;                    
                }
            }
        }
    }

    public static void OnGetUserInventoryFailedChest(PlayFabError error)
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), OnGetUserInventoryChest, OnGetUserInventoryFailedChest);
    }
}
