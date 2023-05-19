using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopCategoryButton : LongRangeButton
{
    public ShopConsole[] consoles;
    public NewShop.CosmeticPosition categoryType;
    public RectTransform rect;

    public void Start()
    {
        rect = GetComponent<RectTransform>();
        consoles = FindObjectsOfType<ShopConsole>();
        neutralColor = uiRenderer.color;
    }

    public override void ActivateButton(bool onOff)
    {
        base.ActivateButton(onOff);
        for (int i = 0; i < consoles.Length; i++)
        {
            consoles[i].CategoryActivated(categoryType);
        }
    }
}
