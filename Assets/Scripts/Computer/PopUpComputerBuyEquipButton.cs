using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopUpComputerBuyEquipButton : LongRangeButton
{
    public enum BuyEquipType
    {
        Buy,
        Equip,
        Unequip
    }
    
    public PopUpComputerCosmetics shopScript;
    public BuyEquipType type;

    public override void ActivateButton(bool onOff)
    {
        if (PopUpComputer.instance.computerGroup.blocksRaycasts == true)
        {
            switch (type)
            {
                case BuyEquipType.Buy:
                {
                    shopScript.AttemptPurchase(this);
                    break;
                }

                case BuyEquipType.Equip:
                {
                    shopScript.equipSource.Play();
                    ShopConsole.EquipCosmetic(null, shopScript.lastType.cosmeticType, shopScript.lastType.cosmeticPosition);
                    shopScript.equipButton.SetActive(false);
                    shopScript.unequipButton.SetActive(true);

                    if (shopScript.lastType.cosmeticPosition == NewShop.CosmeticPosition.Mode)
                    {
                        if (ShopConsole.IsGun(shopScript.lastType.cosmeticType))
                        {
                            PhotonRoyalePlayer.me.AdjustSkinEligibility();
                        }
                    }
                    break;
                }
                
                case BuyEquipType.Unequip:
                {
                    shopScript.unequipSource.Play();
                    ShopConsole.UnEquipCosmetic(null, shopScript.lastType.cosmeticType);
                    shopScript.equipButton.SetActive(true);
                    shopScript.unequipButton.SetActive(false);

                    if (shopScript.lastType.cosmeticPosition == NewShop.CosmeticPosition.Mode)
                    {
                        if (ShopConsole.IsGun(shopScript.lastType.cosmeticType))
                        {
                            PhotonRoyalePlayer.me.AdjustSkinEligibility();
                        }
                    }
                    break;
                }
            }
        }
    }
}
