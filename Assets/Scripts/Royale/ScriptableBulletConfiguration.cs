using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BulletConfiguration", menuName = "Weapons/BulletConfiguration", order = 2)]
public class ScriptableBulletConfiguration : ScriptableObject
{
    public int freezeDamage;
    public float bulletSpeed;
    public float lifeTime = 10.0f;
}
