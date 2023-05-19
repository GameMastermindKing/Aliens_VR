using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectCosmeticButton : LongRangeButton
{
    public ShopCategoryButton myCategory;
    public ShopConsole[] consoles;
    public NewShop.CosmeticType type;
    public RectTransform rect;

    public Vector3 onPos;
    public Vector3 offPos;
    
    public void Start()
    {
        consoles = FindObjectsOfType<ShopConsole>();
    }

    public void SetRects()
    {
        onPos = rect.anchoredPosition3D;
        offPos = rect.anchoredPosition3D.x > 0 ? rect.anchoredPosition3D + Vector3.right * Random.Range(80.0f, 120.0f) : 
            rect.anchoredPosition3D - Vector3.right * Random.Range(80.0f, 120.0f);
    }

    public override void ActivateButton(bool onOff)
    {
        base.ActivateButton(onOff);
        for (int i = 0; i < consoles.Length; i++)
        {
            consoles[i].CosmeticSelected(type);
        }
    }
}
