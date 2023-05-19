using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PopUpInteractable : MonoBehaviour
{
    public Image interactableImage;
    public GameObject tooltip;

    public Color neutralColor;
    public Color highlightColor;
    public Color pressedColor;

    protected PopUpComputer master;
    bool leftTrigger = false;
    bool rightTrigger = false;
    bool leftTriggerHeld = false;
    bool rightTriggerHeld = false;
    Coroutine payAttentionRoutine;

    public virtual void OnHighlight(PopUpComputer instance)
    {
        master = instance;
        if (payAttentionRoutine == null)
        {
            payAttentionRoutine = StartCoroutine(PayAttentionToHands());
        }
    }

    public virtual IEnumerator PayAttentionToHands()
    {
        if (tooltip != null)
        {
            tooltip.SetActive(true);
        }

        while ((master.leftInteractable == this || master.rightInteractable == this) && master.computerGroup.alpha > 0.0f)
        {
            #if UNITY_EDITOR
            leftTrigger = Keyboard.current.tKey.wasPressedThisFrame;
            rightTrigger = Keyboard.current.tKey.wasPressedThisFrame;
            leftTriggerHeld = Keyboard.current.tKey.isPressed;
            rightTriggerHeld = Keyboard.current.tKey.isPressed;
            #else
            leftTrigger = InputManager.instance.leftHandTrigger.WasPressedThisFrame();
            rightTrigger = InputManager.instance.rightHandTrigger.WasPressedThisFrame();
            leftTriggerHeld = InputManager.instance.leftHandTrigger.IsPressed();
            rightTriggerHeld = InputManager.instance.rightHandTrigger.IsPressed();
            #endif
            
            if ((master.leftInteractable == this && leftTrigger) ||
                (master.rightInteractable == this && rightTrigger))
            {
                if (this.GetType() == typeof(ButtonInteractable))
                {
                    master.StartVibration(master.leftInteractable == this, 0.7f, 0.15f);
                }
                interactableImage.color = pressedColor;
                OnActivated();
            }
            else if ((master.leftInteractable == this && leftTriggerHeld) || 
                     (master.rightInteractable == this && rightTriggerHeld))
            {
                interactableImage.color = pressedColor;
            }
            else if (master.leftInteractable == this || master.rightInteractable == this)
            {
                interactableImage.color = highlightColor;
            }
            else
            {
                interactableImage.color = neutralColor;
            }
            yield return null;
        }

        if ((master.leftInteractable != this && master.rightInteractable != this) || master.computerGroup.alpha <= 0.0f)
        {
            if (tooltip != null)
            {
                tooltip.SetActive(false);
            }
            interactableImage.color = neutralColor;
        }
        payAttentionRoutine = null;
    }

    public virtual void OnActivated()
    {
        master.activateSource.pitch = Random.Range(0.8f, 1.0f);
        master.activateSource.Play();
    }

    public void OnDisable()
    {
        interactableImage.color = neutralColor;
        payAttentionRoutine = null;
    }
}
