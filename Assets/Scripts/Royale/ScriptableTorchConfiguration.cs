using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TorchConfiguration", menuName = "Weapons/TorchConfiguration", order = 3)]
public class ScriptableTorchConfiguration : ScriptableObject
{
    public float grabDistance = 2.0f;
    public int healPerTick = 5;
    public int maxHeal = 100;
    public float timePerTick = 1f;
}
