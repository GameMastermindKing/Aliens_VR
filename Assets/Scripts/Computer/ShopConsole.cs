using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopConsole : MonoBehaviour
{
    public enum CurrentMode
    {
        Attract,
        Categories,
        Inside_Category
    }

    [System.Serializable]
    public class CategoryButtonDetails
    {
        public SelectCosmeticButton[] selectButtons;
        public TextMeshProUGUI[] selectButtonTexts;
        public Image[] selectButtonImages;
    }

    public NewShop shopDetailsScript;

    public NewShop.CosmeticPosition currentCategory;
    public CurrentMode storeMode;
    public GameObject[] categories;
    public float timeToAttract = 30.0f;

    [Header("Special Rules")]
    public Transform snowmobileStartPos;
    public Transform spaceshipStartPos;
    public Transform mechStartPos;

    [Header("Selected Cosmetic")]
    public CategoryButtonDetails[] cosmeticButtons;
    public ShopBuyCosmeticButton buyButton;
    public ShopEquipButton equipButton;
    public ShopEquipButton unequipButton;
    public RectTransform selectedGroup;
    public TextMeshProUGUI selectedDescriptionText;
    public TextMeshProUGUI selectedPriceText;
    public TextMeshProUGUI selectedNameText;
    public TextMeshProUGUI totalCubesText;
    public NewShop.CosmeticType selectedType;
    public Image selectedImage;
    public Sprite bkgSprite;
    public Vector3 selectedGroupOn;
    public Vector3 selectedGroupOff;

    [Header("Category Buttons")]
    public ShopCategoryButton[] categoryButtons;
    public NewIAPButton[] iapButtons;
    public Vector3[] categoryButtonsOffSpots;
    public Vector3[] categoryButtonsOnSpots;
    public float categoryMaxMove = 50.0f;

    public float lastInteraction = 0.0f;
    Vector3 lastPos;

    public static string lastBoughtType;

    public IEnumerator Start()
    {
        for (int i = 0; i < iapButtons.Length; i++)
        {
            iapButtons[i].rect.anchoredPosition3D = iapButtons[i].offVec;
        }

        for (int i = 0; i < categoryButtons.Length; i++)
        {
            categoryButtonsOnSpots[i] = categoryButtons[i].rect.anchoredPosition3D;
            categoryButtonsOffSpots[i] = categoryButtons[i].rect.anchoredPosition3D.x > 0 ? categoryButtons[i].rect.anchoredPosition3D + Vector3.right * Random.Range(80.0f, 120.0f) : 
                categoryButtons[i].rect.anchoredPosition3D - Vector3.right * Random.Range(80.0f, 120.0f);
            categoryButtons[i].rect.anchoredPosition3D = categoryButtonsOffSpots[i];
        }

        while (!NewShop.dataGathered)
        {
            yield return null;
        }

        for (int i = 0; i < cosmeticButtons.Length; i++)
        {
            categories[i].SetActive(true);
            for (int j = 0; j < cosmeticButtons[i].selectButtons.Length; j++)
            {
                cosmeticButtons[i].selectButtons[j].gameObject.SetActive(true);
                cosmeticButtons[i].selectButtons[j].rect = cosmeticButtons[i].selectButtons[j].GetComponent<RectTransform>();
                cosmeticButtons[i].selectButtons[j].SetRects();
                cosmeticButtons[i].selectButtons[j].uiRenderer = cosmeticButtons[i].selectButtons[j].uiRenderer.transform.GetChild(0).GetComponent<Image>();
                cosmeticButtons[i].selectButtons[j].neutralColor = cosmeticButtons[i].selectButtons[j].uiRenderer.color;
                cosmeticButtons[i].selectButtons[j].gameObject.SetActive(j < NewShop.sortedCosmeticsData[i].Count);
                cosmeticButtons[i].selectButtonImages[j].sprite = j < NewShop.sortedCosmeticsData[i].Count ? NewShop.sortedCosmeticsData[i][j].cosmeticSprite : null;
                cosmeticButtons[i].selectButtonTexts[j].text = j < NewShop.sortedCosmeticsData[i].Count ? NewShop.sortedCosmeticsData[i][j].cosmeticCleanName.ToUpper() : null;
                cosmeticButtons[i].selectButtons[j].type = j < NewShop.sortedCosmeticsData[i].Count ? NewShop.sortedCosmeticsData[i][j].cosmeticType : NewShop.CosmeticType.Tophat;
            }
            categories[i].SetActive(false);
        }
    }

    public void Update()
    {
        totalCubesText.text = "Cubes Left: " + CurrencyManager.currency.ToString("N0");
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            lastPos = categoryButtons[i].rect.anchoredPosition3D;
            categoryButtons[i].rect.anchoredPosition3D = Vector3.Lerp(categoryButtons[i].rect.anchoredPosition3D, storeMode == CurrentMode.Categories ? 
                                                                      categoryButtonsOnSpots[i] : categoryButtonsOffSpots[i], Time.deltaTime * 2.0f);
            
            categoryButtons[i].myCollider.enabled = storeMode == CurrentMode.Categories;
            if (Vector3.Distance(lastPos, categoryButtons[i].rect.anchoredPosition3D) > categoryMaxMove)
            {
                categoryButtons[i].rect.anchoredPosition3D = (categoryButtons[i].rect.anchoredPosition3D - lastPos).normalized * categoryMaxMove + lastPos;
            }
        }

        for (int i = 0; i < iapButtons.Length; i++)
        {
            lastPos = iapButtons[i].rect.anchoredPosition3D;
            iapButtons[i].rect.anchoredPosition3D = Vector3.Lerp(iapButtons[i].rect.anchoredPosition3D, storeMode == CurrentMode.Categories ? 
                                                                 iapButtons[i].onVec : iapButtons[i].offVec, Time.deltaTime * 2.0f);
            
            iapButtons[i].myCollider.enabled = storeMode == CurrentMode.Categories;
            if (Vector3.Distance(lastPos, iapButtons[i].rect.anchoredPosition3D) > categoryMaxMove)
            {
                iapButtons[i].rect.anchoredPosition3D = (iapButtons[i].rect.anchoredPosition3D - lastPos).normalized * categoryMaxMove + lastPos;
            }
        }

        for (int i = 0; i < cosmeticButtons.Length; i++)
        {
            if (cosmeticButtons[i].selectButtons[0].rect == null)
            {
                break;
            }

            for (int j = 0; j < cosmeticButtons[i].selectButtons.Length; j++)
            {
                if (cosmeticButtons[i].selectButtons[j].rect == null)
                {
                    break;
                }

                cosmeticButtons[i].selectButtons[j].myCollider.enabled = storeMode == CurrentMode.Inside_Category && categories[i].activeInHierarchy;
                cosmeticButtons[i].selectButtons[j].rect.anchoredPosition3D = storeMode == CurrentMode.Inside_Category && categories[i].activeInHierarchy ? 
                    Vector3.Lerp(cosmeticButtons[i].selectButtons[j].rect.anchoredPosition3D, cosmeticButtons[i].selectButtons[j].onPos, Time.deltaTime * 2.0f) :
                    Vector3.Lerp(cosmeticButtons[i].selectButtons[j].rect.anchoredPosition3D, cosmeticButtons[i].selectButtons[j].offPos, Time.deltaTime * 2.0f);
            }
        }

        selectedGroup.anchoredPosition3D = 
            Vector3.Lerp(selectedGroup.anchoredPosition3D, storeMode == CurrentMode.Inside_Category ? selectedGroupOn : selectedGroupOff, Time.deltaTime * 4.0f);

        if (Time.time - lastInteraction > timeToAttract)
        {
            storeMode = CurrentMode.Attract;
        }
    }

    public void CategoryActivated(NewShop.CosmeticPosition category)
    {
        lastInteraction = Time.time;
        for (int i = 0; i < categories.Length; i++)
        {
            categories[i].SetActive(i == (int)category);
        }
        currentCategory = category;
        storeMode = CurrentMode.Inside_Category;

        buyButton.gameObject.SetActive(false);
        equipButton.gameObject.SetActive(false);
        unequipButton.gameObject.SetActive(false);
        selectedDescriptionText.text = "";
        selectedPriceText.text = "";
        selectedNameText.text = "";
        selectedImage.sprite = bkgSprite;
    }

    public void CosmeticSelected(NewShop.CosmeticType type)
    {
        lastInteraction = Time.time;
        selectedType = type;
        for (int i = 0; i < NewShop.lastCosmeticsData.Count; i++)
        {
            if (NewShop.lastCosmeticsData[i].cosmeticType == type)
            {
                shopDetailsScript.LookAtItem(NewShop.lastCosmeticsData[i]);
                buyButton.gameObject.SetActive(!NewShop.lastCosmeticsData[i].limited && !NewShop.lastCosmeticsData[i].purchased && 
                    PlayerPrefs.GetInt("seeds", 0) >= NewShop.lastCosmeticsData[i].cosmeticPrice);
                equipButton.gameObject.SetActive(!buyButton.gameObject.activeSelf && !IsSled(NewShop.lastCosmeticsData[i].cosmeticType) &&
                                      !PlayerCosmetics.instance.CosmeticEquipped(NewShop.lastCosmeticsData[i].cosmeticName) && 
                                       NewShop.lastCosmeticsData[i].purchased);
                unequipButton.gameObject.SetActive(!buyButton.gameObject.activeSelf && !IsSled(NewShop.lastCosmeticsData[i].cosmeticType) &&
                                         PlayerCosmetics.instance.CosmeticEquipped(NewShop.lastCosmeticsData[i].cosmeticName) && 
                                         NewShop.lastCosmeticsData[i].purchased);
                selectedDescriptionText.text = NewShop.lastCosmeticsData[i].cosmeticDescription;
                selectedPriceText.text = NewShop.lastCosmeticsData[i].limited ? "Not Available" : NewShop.lastCosmeticsData[i].cosmeticPrice.ToString("N0") + " Cubes";
                selectedNameText.text = NewShop.lastCosmeticsData[i].cosmeticCleanName;
                selectedImage.sprite = NewShop.lastCosmeticsData[i].cosmeticSprite;
                break;
            }
        }
    }

    public void AttemptPurchase()
    {
        lastInteraction = Time.time;
        buyButton.myCollider.enabled = false;
        buyButton.gameObject.SetActive(false);
        if (!BuyCosmetic.granting)
        {
            BuyCosmetic.granting = true;
            for (int i = 0; i < NewShop.lastCosmeticsData.Count; i++)
            {
                if (NewShop.lastCosmeticsData[i].cosmeticType == selectedType && 
                    PlayerPrefs.GetInt("seeds", 0) >= NewShop.lastCosmeticsData[i].cosmeticPrice && 
                    !NewShop.lastCosmeticsData[i].purchased)
                {
                    BuyCosmetic.Grant(NewShop.lastCosmeticsData[i].cosmeticName, 0, NewShop.lastCosmeticsData[i].cosmeticPrice);
                    break;
                }
            }
        }
        StartCoroutine(WaitForPurchase());
    }

    IEnumerator WaitForPurchase()
    {
        while (BuyCosmetic.granting)
        {
            yield return null;
        }

        if (lastBoughtType != "")
        {
            for (int i = 0; i < NewShop.lastCosmeticsData.Count; i++)
            {
                if (NewShop.lastCosmeticsData[i].cosmeticName == lastBoughtType)
                {
                    NewShop.lastCosmeticsData[i].purchased = true;
                    buyButton.gameObject.SetActive(false);
                    equipButton.gameObject.SetActive(true);

                    yield return new WaitForSeconds(0.05f);
                    lastBoughtType = "";
                }
            }
        }
        else
        {
            buyButton.gameObject.SetActive(true);
        }
        buyButton.myCollider.enabled = true;
        PopUpComputer.instance.cosmeticsScript.UpdateEquipped();
    }

    public static void EquipCosmetic(ShopConsole consoleScript, NewShop.CosmeticType selectedType, NewShop.CosmeticPosition currentCategory)
    {
        if (consoleScript != null)
        {
            consoleScript.lastInteraction = Time.time;
        }

        UnEquipAllCosmeticsOfType(currentCategory, selectedType, consoleScript);
        for (int i = 0; i < NewShop.lastCosmeticsData.Count; i++)
        {
            if (NewShop.lastCosmeticsData[i].cosmeticType == selectedType)
            {
                PlayerCosmetics.instance.photonView.RPC("EnableCosmetic", Photon.Pun.RpcTarget.All, NewShop.lastCosmeticsData[i].cosmeticName);
                HandleSpecialRules(NewShop.lastCosmeticsData[i].cosmeticType, true, consoleScript);
                PlayerPrefs.SetFloat(NewShop.lastCosmeticsData[i].cosmeticName, 1f);
            }
        }

        PopUpComputer.instance.cosmeticsScript.UpdateEquipped();
        if (consoleScript != null)
        {
            for (int i = 0; i < NewShop.lastCosmeticsData.Count; i++)
            {
                if (NewShop.lastCosmeticsData[i].cosmeticType == selectedType)
                {
                    consoleScript.shopDetailsScript.LookAtItem(NewShop.lastCosmeticsData[i]);
                }
            }
        }
    }

    public static void UnEquipCosmetic(ShopConsole consoleScript, NewShop.CosmeticType selectedType)
    {
        if (consoleScript != null)
        {
            consoleScript.lastInteraction = Time.time;
        }

        for (int i = 0; i < NewShop.lastCosmeticsData.Count; i++)
        {
            if (NewShop.lastCosmeticsData[i].cosmeticType == selectedType)
            {
                PlayerCosmetics.instance.photonView.RPC("DisableCosmetic", Photon.Pun.RpcTarget.All, NewShop.lastCosmeticsData[i].cosmeticName);
                HandleSpecialRules(NewShop.lastCosmeticsData[i].cosmeticType, false, consoleScript);
                PlayerPrefs.SetFloat(NewShop.lastCosmeticsData[i].cosmeticName, 0);
            }
        }
        PopUpComputer.instance.cosmeticsScript.UpdateEquipped();
    }

    public static void UnEquipAllCosmeticsOfType(NewShop.CosmeticPosition type, NewShop.CosmeticType specificType, ShopConsole consoleScript)
    {
        for (int i = 0; i < NewShop.lastCosmeticsData.Count; i++)
        {
            if (type != NewShop.CosmeticPosition.Mode)
            {
                if (NewShop.lastCosmeticsData[i].cosmeticPosition == type && PlayerCosmetics.instance.CosmeticEquipped(NewShop.lastCosmeticsData[i].cosmeticName) && 
                    NewShop.lastCosmeticsData[i].cosmeticType != specificType)
                {
                    PlayerCosmetics.instance.photonView.RPC("DisableCosmetic", Photon.Pun.RpcTarget.All, NewShop.lastCosmeticsData[i].cosmeticName);
                    HandleSpecialRules(NewShop.lastCosmeticsData[i].cosmeticType, false, consoleScript);
                    PlayerPrefs.SetFloat(NewShop.lastCosmeticsData[i].cosmeticName, 0);
                }
            }
            else
            {
                if (IsSled(specificType))
                {
                }
                else if (IsGun(specificType))
                {
                }
            }
        }
    }

    public static bool IsSled(NewShop.CosmeticType specificType)
    {
        return specificType == NewShop.CosmeticType.WoodSled || specificType == NewShop.CosmeticType.SciFiSled || specificType == NewShop.CosmeticType.FishSled ||
               specificType == NewShop.CosmeticType.IceSled || specificType == NewShop.CosmeticType.FoamSled || specificType == NewShop.CosmeticType.TubeSled;
    }

    public static bool IsGun(NewShop.CosmeticType specificType)
    {
        return specificType == NewShop.CosmeticType.PaintGuns || specificType == NewShop.CosmeticType.GoldGuns || specificType == NewShop.CosmeticType.CyberGuns;
    }

    public static void HandleSpecialRules(NewShop.CosmeticType type, bool equipped, ShopConsole consoleScript)
    {
        switch (type)
        {
            default:
            {
                break;
            }
        }
    }
}
