using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;

    public InputAction leftHandTrigger;
    public InputAction rightHandTrigger;
    public InputAction leftHandGrip;
    public InputAction rightHandGrip;
    public InputAction leftHandX;
    public InputAction leftHandY;
    public InputAction rightHandA;
    public InputAction rightHandB;
    public InputAction rightThumbstick;
    public InputAction leftThumbstick;
    public InputAction rightThumbstickPress;
    public InputAction leftThumbstickPress;
    
    public Transform[] playerHands;
    public LayerMask handMask;

    RaycastHit resultHitLeft;
    RaycastHit resultHitRight;

    public void Start()
    {
        if (instance == null)
        {
            DontDestroyOnLoad(gameObject);
            leftHandTrigger.Enable();
            rightHandTrigger.Enable();
            leftHandGrip.Enable();
            rightHandGrip.Enable();
            leftHandX.Enable();
            leftHandY.Enable();
            rightHandA.Enable();
            rightHandB.Enable();
            rightThumbstick.Enable();
            leftThumbstick.Enable();
            rightThumbstickPress.Enable();
            leftThumbstickPress.Enable();
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Update()
    {
        GetHandCasts(100.0f);
    }

    public void GetHandCasts(float distance)
    {
        if (playerHands[0] == null)
        {
            playerHands[0] = FindObjectOfType<GorillaLocomotion.Player>().leftHandTransform;
            playerHands[1] = FindObjectOfType<GorillaLocomotion.Player>().rightHandTransform;
        }
        Physics.Raycast(playerHands[0].position, playerHands[0].forward, out resultHitLeft, distance, handMask);
        Physics.Raycast(playerHands[1].position, playerHands[1].forward, out resultHitRight, distance, handMask);
    }

    public RaycastHit GetLeftHandHit()
    {
        return resultHitLeft;
    }

    public RaycastHit GetRightHandHit()
    {
        return resultHitRight;
    }

    public bool IsTriggerDown(bool left)
    {
        return left ? leftHandTrigger.WasPressedThisFrame() : rightHandTrigger.WasPressedThisFrame();
    }

    public Vector2 GetRightThumbstick()
    {
        #if UNITY_EDITOR
        Vector2 result = Vector2.zero;
        if (Mouse.current.scroll.ReadValue().magnitude > 0.0f)
        {
            result.y = -Mouse.current.scroll.ReadValue().y;
        }
        else
        {
            result.x = (Keyboard.current.leftArrowKey.IsPressed() ? -1.0f : Keyboard.current.rightArrowKey.IsPressed() ? 1.0f : 0.0f);
            result.y = (Keyboard.current.downArrowKey.IsPressed() ? -1.0f : Keyboard.current.upArrowKey.IsPressed() ? 1.0f : 0.0f);
        }
        return result;
        #else
        return rightThumbstick.ReadValue<Vector2>();
        #endif
    }

    public Vector2 GetLeftThumbstick()
    {
        #if UNITY_EDITOR
        Vector2 result = Vector2.zero;
        result.x = (Keyboard.current.aKey.IsPressed() ? -1.0f : Keyboard.current.dKey.IsPressed() ? 1.0f : 0.0f);
        result.y = (Keyboard.current.sKey.IsPressed() ? -1.0f : Keyboard.current.wKey.IsPressed() ? 1.0f : 0.0f);
        return result;
        #else
        return leftThumbstick.ReadValue<Vector2>();
        #endif
    }
}
