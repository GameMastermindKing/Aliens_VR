using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class LongRangeButton : MonoBehaviour
{
    public Renderer buttonRenderer;
    public Image uiRenderer;
    public AudioSource activateSound;

    RaycastHit resultHitLeft;
    RaycastHit resultHitRight;

    public Collider myCollider;

    public Color neutralColor;
    public Color highlightColor;

    public bool activate = true;

    public void Update()
    {
        resultHitLeft = InputManager.instance.GetLeftHandHit();
        resultHitRight = InputManager.instance.GetRightHandHit();

        if (resultHitLeft.collider == myCollider || resultHitRight.collider == myCollider)
        {
            if (buttonRenderer != null)
            {
                buttonRenderer.material.color = highlightColor;
            }
            else if (uiRenderer != null)
            {
                uiRenderer.color = highlightColor;
            }

            #if UNITY_EDITOR
            if (Keyboard.current.tKey.wasPressedThisFrame)
            #else
            if ((resultHitLeft.collider == myCollider && InputManager.instance.leftHandTrigger.WasPressedThisFrame()) ||
                (resultHitRight.collider == myCollider && InputManager.instance.rightHandTrigger.WasPressedThisFrame()))
            #endif
            {
                if (activate)
                {
                    ActivateButton(true);
                }
                else
                {
                    ActivateButton(false);
                }
            }
        }
        else
        {
            if (buttonRenderer != null)
            {
                buttonRenderer.material.color = GetNeutralColor();
            }
            else if (uiRenderer != null)
            {
                uiRenderer.color = GetNeutralColor();
            }
        }
    }

    public virtual Color GetNeutralColor()
    {
        return neutralColor;
    }

    public virtual void ActivateButton(bool onOff)
    {
        activateSound.pitch = Random.Range(0.9f, 1.1f);
        activateSound.Play();
    }
}
