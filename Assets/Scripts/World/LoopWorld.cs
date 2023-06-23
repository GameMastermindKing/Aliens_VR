using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoopWorld : MonoBehaviour
{
    public float xMax;
    public float xMin;
    public float zMax;
    public float zMin;
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
        if (position.x > xMax || position.x < xMin)
        {
            return true;
        }
        else if (position.z > zMax || position.z < zMin)
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
        newVec.x = newVec.x > xMax ? xMin : newVec.x < xMin ? xMax : newVec.x;
        newVec.z = newVec.z > zMax ? zMin : newVec.z < zMin ? zMax : newVec.z;
        newVec.y = newVec.y > yTop || newVec.y < yMin ? yReset : newVec.y;
        GorillaLocomotion.Player.Instance.ForceMovePlayerToPosition(newVec);
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        center = new Vector3((xMax + xMin) / 2.0f, (yTop + yMin) / 2.0f, (zMax + zMin) / 2.0f);
        size = new Vector3((xMax - xMin), (yTop - yMin), (zMax - zMin));
        Gizmos.DrawWireCube(center, size);
    }
}
