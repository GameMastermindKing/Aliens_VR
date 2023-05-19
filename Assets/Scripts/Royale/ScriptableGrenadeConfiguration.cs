using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GrenadeConfiguration", menuName = "Weapons/GrenadeConfiguration", order = 2)]
public class ScriptableGrenadeConfiguration : ScriptableObject
{
    public float throwMod = 1.5f;
    public float minDamage = 20.0f;
    public float maxDamage = 100.0f;
    public float minForce = 20.0f;
    public float maxForce = 100.0f;
    public float minDamageDistance = 1.0f;
    public float maxDamageDistance = 10.0f;
    public int fuseTime = 5;
    public float ignoreDistance = 0.01f;

    [ColorUsageAttribute(false, true)]
    public Color[] secondColors;
}
