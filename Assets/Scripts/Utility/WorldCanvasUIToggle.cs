using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorldCanvasUIToggle : MonoBehaviour
{
    public Collider myCollider;
    public Toggle myToggle;

    public Color neutralColor = Color.white;
    public Color highlightColor = Color.yellow;

    public void Start()
    {
        if (myToggle == null)
        {
            myToggle = GetComponent<Toggle>();
        }

        if (myCollider == null)
        {
            myCollider = GetComponent<Collider>();
        }
    }

    public void Update()
    {
        if (InputManager.instance.GetRightHandHit().collider == myCollider || InputManager.instance.GetLeftHandHit().collider == myCollider)
        {
            if (InputManager.instance.GetRightHandHit().collider == myCollider && InputManager.instance.rightHandTrigger.WasPressedThisFrame() || 
                InputManager.instance.GetLeftHandHit().collider == myCollider && InputManager.instance.leftHandTrigger.WasPressedThisFrame())
            {
                myToggle.isOn = !myToggle.isOn;
            }
        }
    }
}
