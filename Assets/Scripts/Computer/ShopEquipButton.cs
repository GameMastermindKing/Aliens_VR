using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopEquipButton : LongRangeButton
{
    public ShopConsole[] consoles;
    public bool onOff;

    public void Start()
    {
        consoles = FindObjectsOfType<ShopConsole>();
    }

    public override void ActivateButton(bool onOff)
    {
        base.ActivateButton(onOff);
        if (this.onOff)
        {
            ShopConsole.EquipCosmetic(consoles[0], consoles[0].selectedType, consoles[0].currentCategory);
            for (int i = 0; i < consoles.Length; i++)
            {
                consoles[i].equipButton.gameObject.SetActive(false);
                consoles[i].unequipButton.gameObject.SetActive(true);
            }
        }
        else
        {
            ShopConsole.UnEquipCosmetic(consoles[0], consoles[0].selectedType);
            for (int i = 0; i < consoles.Length; i++)
            {
                consoles[i].equipButton.gameObject.SetActive(true);
                consoles[i].unequipButton.gameObject.SetActive(false);
            }
        }
    }
}
