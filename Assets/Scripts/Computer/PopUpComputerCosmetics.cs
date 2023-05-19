using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopUpComputerCosmetics : MonoBehaviour
{
    public NewShopCosmeticsConfiguration configuration;

    public GameObject selectCosmeticMenu;
    public GameObject buyButton;
    public GameObject equipButton;
    public GameObject unequipButton;
    public GameObject mirror;
    public PopUpComputerCosmeticButton[] buttons;
    public PopUpComputerSelectCosmeticButton[] selectCosmeticButtons;
    public Sprite emptySprite;
    public Image equippedImage;
    public TextMeshProUGUI equippedDescription;
    public TextMeshProUGUI equippedName;
    public TextMeshProUGUI equippedPrice;

    public AudioSource buySource;
    public AudioSource equipSource;
    public AudioSource unequipSource;

    public NewShop.CosmeticPosition openCategory;
    bool foundOne = false;
    public NewShop.CosmeticEntry lastType;

    public void Activate()
    {
        if (NewShop.sortedCosmeticsData.Count > 0)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                foundOne = false;
                for (int j = 0; j < NewShop.sortedCosmeticsData[(int)buttons[i].cosmeticType].Count; j++)
                {
                    if (PlayerCosmetics.instance.CosmeticEquipped(NewShop.sortedCosmeticsData[(int)buttons[i].cosmeticType][j].cosmeticName))
                    {
                        if (!foundOne)
                        {
                            buttons[i].cosmeticImage.sprite = NewShop.sortedCosmeticsData[(int)buttons[i].cosmeticType][j].cosmeticSprite;
                        }
                        else if (buttons[i].cosmeticType != NewShop.CosmeticPosition.Mode)
                        {
                            ShopConsole.UnEquipCosmetic(null, NewShop.sortedCosmeticsData[(int)buttons[i].cosmeticType][j].cosmeticType);
                        }
                        foundOne = true;
                    }
                }

                if (!foundOne)
                {
                    buttons[i].cosmeticImage.sprite = emptySprite;
                }
            }
        }
        mirror.SetActive(true);
    }

    public void UpdateEquipped()
    {
        if (NewShop.sortedCosmeticsData.Count > 0)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                foundOne = false;
                for (int j = 0; j < NewShop.sortedCosmeticsData[(int)buttons[i].cosmeticType].Count; j++)
                {
                    if (PlayerCosmetics.instance.CosmeticEquipped(NewShop.sortedCosmeticsData[(int)buttons[i].cosmeticType][j].cosmeticName))
                    {
                        if (!foundOne)
                        {
                            buttons[i].cosmeticImage.sprite = NewShop.sortedCosmeticsData[(int)buttons[i].cosmeticType][j].cosmeticSprite;
                        }
                        foundOne = true;
                    }
                }

                if (!foundOne)
                {
                    buttons[i].cosmeticImage.sprite = emptySprite;
                }
            }
        }
    }

    public void HandleList(NewShop.CosmeticPosition category)
    {
        bool selected = false;
        selectCosmeticMenu.SetActive(true);
        openCategory = category;

        for (int i = 0; i < selectCosmeticButtons.Length; i++)
        {
            selectCosmeticButtons[i].gameObject.SetActive(i < NewShop.sortedCosmeticsData[(int)category].Count);
            selectCosmeticButtons[i].type = i < NewShop.sortedCosmeticsData[(int)category].Count ? NewShop.sortedCosmeticsData[(int)category][i] : null;
            selectCosmeticButtons[i].owned = selectCosmeticButtons[i].type != null && selectCosmeticButtons[i].type.purchased;
            selectCosmeticButtons[i].selected = selectCosmeticButtons[i].type != null && PlayerCosmetics.instance.CosmeticEquipped(selectCosmeticButtons[i].type.cosmeticName);
            selectCosmeticButtons[i].Init();
            
            if (selectCosmeticButtons[i].selected)
            {
                selected = true;
                equippedDescription.text = selectCosmeticButtons[i].type.cosmeticDescription;
                equippedImage.sprite = selectCosmeticButtons[i].type.cosmeticSprite;
                equippedName.text = selectCosmeticButtons[i].type.cosmeticCleanName;
                equippedPrice.text = selectCosmeticButtons[i].type.purchased ? "OWNED" : selectCosmeticButtons[i].type.limited ? 
                    "NOT AVAILABLE" : selectCosmeticButtons[i].type.cosmeticPrice.ToString("N0") + " CUBES";

                buyButton.gameObject.SetActive(!selectCosmeticButtons[i].type.limited && !selectCosmeticButtons[i].type.purchased);
                equipButton.gameObject.SetActive(!buyButton.gameObject.activeSelf && !ShopConsole.IsSled(selectCosmeticButtons[i].type.cosmeticType) &&
                                                 !PlayerCosmetics.instance.CosmeticEquipped(selectCosmeticButtons[i].type.cosmeticName) && 
                                                  selectCosmeticButtons[i].type.purchased && 
                                                  (selectCosmeticButtons[i].type.allowedInCompetitionModes || !PlayerCosmetics.instance.SceneIsCompetitive()));
                unequipButton.gameObject.SetActive(!buyButton.gameObject.activeSelf && !ShopConsole.IsSled(selectCosmeticButtons[i].type.cosmeticType) &&
                                                    PlayerCosmetics.instance.CosmeticEquipped(selectCosmeticButtons[i].type.cosmeticName) && 
                                                    selectCosmeticButtons[i].type.purchased);
                lastType = selectCosmeticButtons[i].type;
            }
        }

        if (!selected)
        {
            equippedDescription.text = "";
            equippedImage.sprite = emptySprite;
            equippedName.text = "";
            equippedPrice.text = "";
        }
        mirror.SetActive(false);
    }

    public void SelectCosmetic(NewShop.CosmeticEntry type)
    {
        for (int i = 0; i < selectCosmeticButtons.Length; i++)
        {
            if (selectCosmeticButtons[i].type != type)
            {
                selectCosmeticButtons[i].selected = false;
                selectCosmeticButtons[i].myCollider.enabled = true;
            }
            else
            {
                selectCosmeticButtons[i].myCollider.enabled = false;
            }
        }

        equippedDescription.text = type.cosmeticDescription;
        equippedImage.sprite = type.cosmeticSprite;
        equippedName.text = type.cosmeticCleanName;
        equippedPrice.text = type.purchased ? "OWNED" : type.limited ? "NOT AVAILABLE" : type.cosmeticPrice.ToString("N0") + " CUBES";

        buyButton.gameObject.SetActive(!type.limited && !type.purchased && PlayerPrefs.GetInt("seeds", 0) >= type.cosmeticPrice);
        equipButton.gameObject.SetActive(!buyButton.gameObject.activeSelf && !ShopConsole.IsSled(type.cosmeticType) &&
                                         !PlayerCosmetics.instance.CosmeticEquipped(type.cosmeticName) && 
                                          type.purchased && 
                                         (type.allowedInCompetitionModes || !PlayerCosmetics.instance.SceneIsCompetitive()));
        unequipButton.gameObject.SetActive(!buyButton.gameObject.activeSelf && !ShopConsole.IsSled(type.cosmeticType) &&
                                            PlayerCosmetics.instance.CosmeticEquipped(type.cosmeticName) && 
                                            type.purchased);
        lastType = type;
    }

    public void CloseListMenu()
    {
        selectCosmeticMenu.SetActive(false);
        mirror.SetActive(true);
    }
    
    public void AttemptPurchase(PopUpComputerBuyEquipButton button)
    {
        buySource.Play();
        button.myCollider.enabled = false;
        button.gameObject.SetActive(false);
        if (!BuyCosmetic.granting)
        {
            BuyCosmetic.granting = true;
            if (PlayerPrefs.GetInt("seeds", 0) >= lastType.cosmeticPrice && 
                !lastType.purchased)
            {
                BuyCosmetic.Grant(lastType.cosmeticName, 0, lastType.cosmeticPrice);
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

        if (ShopConsole.lastBoughtType != "")
        {
            for (int i = 0; i < NewShop.lastCosmeticsData.Count; i++)
            {
                if (NewShop.lastCosmeticsData[i].cosmeticName == ShopConsole.lastBoughtType)
                {
                    NewShop.lastCosmeticsData[i].purchased = true;
                    buyButton.gameObject.SetActive(false);
                    equipButton.gameObject.SetActive(!buyButton.gameObject.activeSelf && !ShopConsole.IsSled(NewShop.lastCosmeticsData[i].cosmeticType) &&
                                                     !PlayerCosmetics.instance.CosmeticEquipped(NewShop.lastCosmeticsData[i].cosmeticName) && 
                                                      NewShop.lastCosmeticsData[i].purchased && 
                                                     (NewShop.lastCosmeticsData[i].allowedInCompetitionModes || !PlayerCosmetics.instance.SceneIsCompetitive()));

                    yield return new WaitForSeconds(0.05f);
                    ShopConsole.lastBoughtType = "";
                }
            }
        }
        else
        {
            buyButton.gameObject.SetActive(true);
        }
        buyButton.GetComponent<PopUpComputerBuyEquipButton>().myCollider.enabled = true;
        UpdateEquipped();
    }
}
