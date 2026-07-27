using CrimsonLibrary.SupportLibrary.Utils.Generics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;
using System.Threading;

public class FacebookSDKWrapper : GenericManager<FacebookSDKWrapper>
{
    void Awake()
    {
        base.Awake();
        if (!FB.IsInitialized)
        {
            // Initialize the Facebook SDK
            FB.Init(InitCallback, OnHideUnity);
        }
        else
        {
            // Already initialized, signal an app activation App Event
            FB.ActivateApp();
        }
        PlayFabPlayerManager.Instance.onLoginSuccess += SendLoginEvent;
    }

    private void InitCallback()
    {
        if (FB.IsInitialized)
        {
            Debug.Log("Initialized the Facebook SDK successfully");
            // Signal an app activation App Event
            FB.ActivateApp();
            // Continue with Facebook SDK
            // ...
            Debug.Log("Facebook SDK ActivateApp called");
        }
        else
        {
            Debug.Log("Failed to Initialize the Facebook SDK");
        }
    }

    private void OnHideUnity(bool isGameShown)
    {

    }

    private void SendLoginEvent()
    {
        if (FB.IsInitialized)
        {
            Dictionary<string, object> loginParams = new Dictionary<string, object>
            {
                { "PlayerID", PlayFabPlayerManager.Instance.PlayFabId }
            };
            FB.LogAppEvent("CGLogin", parameters: loginParams);
            Debug.Log("Sending CGLogin event to facebook");
        }
        else
        {
            Debug.Log("Failed to send CGLogin event to facebook because Facebook SDK wasn't initialized in time");
        }
    }

    public void SendRevenue(float revenueAmount)
    {
        FB.LogPurchase(revenueAmount, "USD");
    }
}
