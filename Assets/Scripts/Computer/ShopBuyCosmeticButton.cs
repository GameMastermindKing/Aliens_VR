using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopBuyCosmeticButton : LongRangeButton
{
    public ShopConsole[] consoles;

    public void Start()
    {
        consoles = FindObjectsOfType<ShopConsole>();
    }

    public override void ActivateButton(bool onOff)
    {
        base.ActivateButton(onOff);
        for (int i = 0; i < consoles.Length; i++)
        {
            consoles[i].AttemptPurchase();
        }
    }
}
