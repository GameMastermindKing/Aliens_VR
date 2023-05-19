using System.Collections;
using System.Collections.Generic;
using Oculus.Platform;
using UnityEngine;
using UnityEngine.UI;

public class NewIAPButton : MonoBehaviour
{
    public Image iapImage;
    public Button button;
    public RectTransform rect;
    public Vector3 onVec;
    public Vector3 offVec;

    RaycastHit resultHitLeft;
    RaycastHit resultHitRight;

    public Collider myCollider;

    public Color neutralColor;
    public Color highlightColor;

    // Update is called once per frame
    void Update()
    {
        if (Core.IsInitialized())
        {
            resultHitLeft = InputManager.instance.GetLeftHandHit();
            resultHitRight = InputManager.instance.GetRightHandHit();

            if (resultHitLeft.collider == myCollider || resultHitRight.collider == myCollider)
            {
                iapImage.color = highlightColor;
                if (resultHitLeft.collider == myCollider && InputManager.instance.leftHandTrigger.WasPressedThisFrame())
                {
                    button.onClick.Invoke();
                }
                else if (resultHitRight.collider == myCollider && InputManager.instance.rightHandTrigger.WasPressedThisFrame())
                {
                    button.onClick.Invoke();
                }
            }
            else
            {
                iapImage.color = neutralColor;
            }
        }
    }
}
