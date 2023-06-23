using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorldCanvasUIButton : MonoBehaviour
{
    public Collider myCollider;
    public Button myButton;
    public Image buttonImage;

    public Color neutralColor = Color.white;
    public Color highlightColor = Color.yellow;

    public void Update()
    {
        if (InputManager.instance.GetRightHandHit().collider == myCollider || InputManager.instance.GetLeftHandHit().collider == myCollider)
        {
            buttonImage.color = highlightColor;
            if (InputManager.instance.GetRightHandHit().collider == myCollider && InputManager.instance.rightHandTrigger.WasPressedThisFrame() || 
                InputManager.instance.GetLeftHandHit().collider == myCollider && InputManager.instance.leftHandTrigger.WasPressedThisFrame())
            {
                myButton.onClick.Invoke();
            }
        }
        else
        {
            buttonImage.color = neutralColor;
        }
    }
}
