using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CosmeticsConfiguration", menuName = "Cosmetics/CosmeticsConfiguration", order = 2)]
public class NewShopCosmeticsConfiguration : ScriptableObject
{
    public NewShopCosmeticsConfiguration()
    {
        for (int i = 0; i < (int)NewShop.CosmeticType.COUNT; i++)
        {
            activeCosmetics.Add(new NewShop.CosmeticEntry());
            activeCosmetics[activeCosmetics.Count - 1].cosmeticType = (NewShop.CosmeticType)i;
            activeCosmetics[activeCosmetics.Count - 1].cosmeticName = activeCosmetics[activeCosmetics.Count - 1].cosmeticType.ToString();
        }
    }

    public List<NewShop.CosmeticEntry> activeCosmetics = new List<NewShop.CosmeticEntry>();

    public List<NewShop.CosmeticEntry> GetEntries()
    {
        List<NewShop.CosmeticEntry> entries = new List<NewShop.CosmeticEntry>();
        for (int i = 0; i < activeCosmetics.Count; i++)
        {
            entries.Add(new NewShop.CosmeticEntry());
            activeCosmetics[i].CopyTo(entries[i]);
        }
        return entries;
    }
}
