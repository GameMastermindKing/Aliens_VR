using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class ColorPicker : MonoBehaviour
{
    public Transform plane;
    public Texture2D circleTex;
    public Renderer circleRenderer;
    public Collider circleCollider;
    public Collider lowerCollider;
    public Collider upperCollider;
    public Renderer[] renderersToTint;

    public float brightness = 1.0f;
    public float brightnessTick = 0.1f;

    public bool useX = false;
    public bool posNeg = false;

    public AudioSource beepSource;

    RaycastHit leftHit;
    RaycastHit rightHit;

    public void Start()
    {
        string[] color = PlayerPrefs.GetString("HairColor", "").Split(',');
        if (color.Length == 3)
        {
            for (int i = 0; i < renderersToTint.Length; i++)
            {
                renderersToTint[i].material.color = new Color(float.Parse(color[0]), float.Parse(color[1]), float.Parse(color[2]), 1.0f);
            }
        }
    }

    public void Update()
    {
        leftHit = InputManager.instance.GetLeftHandHit();
        rightHit = InputManager.instance.GetRightHandHit();

        #if UNITY_EDITOR
        if (leftHit.collider == upperCollider && Keyboard.current.tKey.wasPressedThisFrame ||
            rightHit.collider == upperCollider && Keyboard.current.tKey.wasPressedThisFrame)
        #else
        if (leftHit.collider == upperCollider && InputManager.instance.leftHandTrigger.WasPressedThisFrame() ||
            rightHit.collider == upperCollider && InputManager.instance.rightHandTrigger.WasPressedThisFrame())
        #endif
        {
            RaiseBrightness();
            beepSource.pitch = Random.Range(0.8f, 1.0f);
            beepSource.Play();
        }
        #if UNITY_EDITOR
        else if (leftHit.collider == lowerCollider && Keyboard.current.tKey.wasPressedThisFrame ||
                 rightHit.collider == lowerCollider && Keyboard.current.tKey.wasPressedThisFrame)
        #else
        else if (leftHit.collider == lowerCollider && InputManager.instance.leftHandTrigger.WasPressedThisFrame() ||
                 rightHit.collider == lowerCollider && InputManager.instance.rightHandTrigger.WasPressedThisFrame())
        #endif
        {
            LowerBrightness();
            beepSource.pitch = Random.Range(0.8f, 1.0f);
            beepSource.Play();
        }
        #if UNITY_EDITOR
        else if (leftHit.collider == circleCollider && Keyboard.current.tKey.wasPressedThisFrame ||
                 rightHit.collider == circleCollider && Keyboard.current.tKey.wasPressedThisFrame)
        #else
        else if (leftHit.collider == circleCollider && InputManager.instance.leftHandTrigger.WasPressedThisFrame() ||
                 rightHit.collider == circleCollider && InputManager.instance.rightHandTrigger.WasPressedThisFrame())
        #endif
        {
            #if UNITY_EDITOR
            Color result = GetColorFromPos(leftHit.collider == circleCollider && Keyboard.current.tKey.wasPressedThisFrame ? leftHit.point : rightHit.point);
            #else
            Color result = GetColorFromPos(leftHit.collider == circleCollider && InputManager.instance.leftHandTrigger.WasPressedThisFrame() ? leftHit.point : rightHit.point);
            #endif

            #if UNITY_EDITOR
            if (leftHit.collider == circleCollider && Keyboard.current.tKey.wasPressedThisFrame ||
                rightHit.collider == circleCollider && Keyboard.current.tKey.wasPressedThisFrame)
            #else
            if (leftHit.collider == circleCollider && InputManager.instance.leftHandTrigger.WasPressedThisFrame() ||
                rightHit.collider == circleCollider && InputManager.instance.rightHandTrigger.WasPressedThisFrame())
            #endif
            {
                beepSource.pitch = Random.Range(0.8f, 1.0f);
                beepSource.Play();
            }

            for (int i = 0; i < renderersToTint.Length; i++)
            {
                renderersToTint[i].material.color = result;
            }
        }
    }
    
    public void LowerBrightness()
    {
        brightness = Mathf.Clamp(brightness - brightnessTick, 0.0f, 1.0f);
        circleRenderer.material.color = Color.Lerp(Color.black, Color.white, brightness);
        circleRenderer.material.SetColor("_EmissionColor", Color.Lerp(Color.black, Color.white, brightness));
    }

    public void RaiseBrightness()
    {
        brightness = Mathf.Clamp(brightness + brightnessTick, 0.0f, 1.0f);
        circleRenderer.material.color = Color.Lerp(Color.black, Color.white, brightness);
        circleRenderer.material.SetColor("_EmissionColor", Color.Lerp(Color.black, Color.white, brightness));
    }

    public Color GetColorFromPos(Vector3 pos)
    {
        float x;
        if (useX)
        {
            x = !posNeg ? ((pos.x - circleCollider.bounds.min.x) / circleCollider.bounds.size.x) : 1 - ((pos.x - circleCollider.bounds.min.x) / circleCollider.bounds.size.x);
        }
        else
        {
            x = !posNeg ? ((pos.z - circleCollider.bounds.min.z) / circleCollider.bounds.size.z) : 1 - ((pos.z - circleCollider.bounds.min.z) / circleCollider.bounds.size.z);
        }

        float y = ((pos.y - circleCollider.bounds.min.y) / circleCollider.bounds.size.y);
        Color result = circleTex.GetPixel((int)(x * circleTex.width), (int)(y * circleTex.height));
        result.r *= brightness;
        result.g *= brightness;
        result.b *= brightness;
        
        if (result.a > 0.5f)
        {
            ColorSwitcher.instance.photonView.RPC("SetColor", RpcTarget.All, result.r, result.g, result.b);
        }
        else
        {
            ColorSwitcher.instance.photonView.RPC("SetColor", RpcTarget.All, 1f, 1f, 1f);
        }
        return result.a > 0.5f ? result : Color.white;
    }
}
