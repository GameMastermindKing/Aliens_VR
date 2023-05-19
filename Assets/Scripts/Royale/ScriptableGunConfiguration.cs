using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GunConfiguration", menuName = "Weapons/GunConfiguration", order = 1)]
public class ScriptableGunConfiguration : ScriptableObject
{
    public float grabDistance = 2.0f;
    public float fireRate = 0.375f;
    public float spread = 0.0f;
    public int numBullets = 1;
    public int ammoConsumed = 10;
    public int ammo = 1;
    public float reloadTime = 2.0f;
}
