using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EquipGunSkinButton : MonoBehaviour
{
    public EquipGunSkinButton[] connectedButtons;
    public Renderer buttonRenderer;
    public Text buttonText;
    public string gunName;

    RaycastHit resultHitLeft;
    RaycastHit resultHitRight;

    public Collider myCollider;

    public Color offColor;
    public Color onColor;

    public bool cosmeticEnabled = false;

    public string itemName;

    public void Start()
    {
        buttonRenderer.material.color = PlayerPrefs.GetFloat(itemName, 0) == 1 ? onColor : offColor;
        buttonText.text = PlayerPrefs.GetFloat(itemName, 0) == 1 ? "EQUIP" + gunName : "";
    }

    public void Update()
    {
        resultHitLeft = InputManager.instance.GetLeftHandHit();
        resultHitRight = InputManager.instance.GetRightHandHit();

        myCollider.enabled = cosmeticEnabled;
        buttonRenderer.enabled = cosmeticEnabled;
        buttonText.text = cosmeticEnabled ? "EQUIP" + gunName : "";
        
        if (!cosmeticEnabled && buttonRenderer.material.color == onColor)
        {
            buttonText.text = "";
            buttonRenderer.material.color = offColor;
            PlayerPrefs.SetFloat(itemName, 0);
        }

        if (resultHitRight.collider == myCollider || resultHitLeft.collider == myCollider)
        {
            #if UNITY_EDITOR
            if (Keyboard.current.tKey.wasPressedThisFrame)
            #else
            if (((resultHitRight.collider == myCollider && InputManager.instance.rightHandTrigger.WasPressedThisFrame()) || 
                 (resultHitLeft.collider == myCollider && InputManager.instance.leftHandTrigger.WasPressedThisFrame())))
            #endif
            {
                PlayerPrefs.SetFloat(itemName, 1);
                for (int i = 0; i < connectedButtons.Length; i++)
                {
                    PlayerPrefs.SetFloat(connectedButtons[i].itemName, 0);
                    connectedButtons[i].buttonRenderer.material.color = offColor;
                }
                buttonText.text = "EQUIP" + gunName;
                buttonRenderer.material.color = onColor;
            }
        }
    }
}
