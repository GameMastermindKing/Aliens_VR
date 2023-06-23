using UnityEngine;
using System.Collections;
using Oculus.Platform;

public class AppEntitlementCheck : MonoBehaviour
{
    public GameObject stuff;

    private IEnumerator Start()
    {
        yield return null;
        try
        {
            Core.AsyncInitialize().OnComplete(message => 
            {
                if (!message.IsError)
                {
                    AbuseReport.SetReportButtonPressedNotificationCallback(ReportingCallback.instance.OnReportButtonIntentNotif);
                }
                Entitlements.IsUserEntitledToApplication().OnComplete(EntitlementCallback);
            });
        }
        catch (UnityException e)
        {
            Debug.LogError("Platform failed to initialize due to exception.");
            Debug.LogException(e);
            // Immediately quit the application.
            UnityEngine.Application.Quit();
        }
    }

    // Called when the Meta Quest Platform completes the async entitlement check request and a result is available.
    void EntitlementCallback(Message msg)
    {
        if (msg.IsError) // User failed entitlement check
        {
            // Implements a default behavior for an entitlement check failure -- log the failure and exit the app.
            Debug.LogError("You are not entitled to use this app due to not being on oculus platform swap to oculus to make any purchases.");
            UnityEngine.Application.Quit();
            stuff.SetActive(false);
        }
        else // User passed entitlement check
        {
            // Log the succeeded entitlement check for debugging.
            Debug.Log("You are entitled to use this app.");
            stuff.SetActive(true);
            OVRPlugin.systemDisplayFrequency = 90.0f;
        }
    }
}
