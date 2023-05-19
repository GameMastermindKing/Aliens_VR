using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopUpComputerCosmeticButton : LongRangeButton
{
    public PopUpComputerCosmetics cosmeticsMenu;
    public NewShop.CosmeticPosition cosmeticType;
    public Image cosmeticImage;

    public override void ActivateButton(bool onOff)
    {
        if (PopUpComputer.instance.computerGroup.blocksRaycasts == true)
        {
            base.ActivateButton(onOff);
            cosmeticsMenu.HandleList(cosmeticType);
        }
    }
}
