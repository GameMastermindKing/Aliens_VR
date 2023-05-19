using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopUpComputerSelectCosmeticButton : LongRangeButton
{
    public PopUpComputerCosmetics menuScript;
    public NewShop.CosmeticEntry type;

    public Color selectedColor;
    public Image cosmeticImage;

    public Color ownedColor = Color.white;
    public Color notOwnedColor = new Color(0.3f, 0.3f, 0.3f);

    public bool selected = false;
    public bool owned = false;

    public void Init()
    {
        cosmeticImage.sprite = type != null ? type.cosmeticSprite : menuScript.emptySprite;
    }

    public override void ActivateButton(bool onOff)
    {
        if (PopUpComputer.instance.computerGroup.blocksRaycasts == true)
        {
            base.ActivateButton(onOff);
            selected = true;
            menuScript.SelectCosmetic(type);
        }
    }

    public override Color GetNeutralColor()
    {
        return selected ? selectedColor : owned ? ownedColor : notOwnedColor;
    }
}
