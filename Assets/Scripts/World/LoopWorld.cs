using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoopWorld : MonoBehaviour
{
    public float xMax;
    public float zMax;
    public float yMin;
    public float yTop;
    public Vector3 center;
    public Vector3 size;
    public float yReset;

    public void Update()
    {
        if (OutOfBounds(GorillaLocomotion.Player.Instance.transform.position))
        {
            WarpToSafety();
        }
    }

    public bool OutOfBounds(Vector3 position)
    {
        if (Mathf.Abs(position.x) > xMax)
        {
            return true;
        }
        else if (Mathf.Abs(position.z) > zMax)
        {
            return true;
        }
        else if (position.y < yMin || position.y > yTop)
        {
            return true;
        }
        return false;
    }

    public void WarpToSafety()
    {
        Vector3 newVec = GorillaLocomotion.Player.Instance.transform.position;
        newVec.x = newVec.x > xMax ? -xMax : newVec.x < -xMax ? xMax : newVec.x;
        newVec.z = newVec.z > zMax ? -zMax : newVec.z < -zMax ? zMax : newVec.z;
        newVec.y = newVec.y > yTop || newVec.y < yMin ? yReset : newVec.y;
        GorillaLocomotion.Player.Instance.ForceMovePlayerToPosition(newVec);
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        center = new Vector3(0f, (yTop + yMin) / 2.0f, 0f);
        size = new Vector3(xMax * 2, (yTop - yMin), zMax * 2);
        Gizmos.DrawWireCube(center, size);
    }
}
