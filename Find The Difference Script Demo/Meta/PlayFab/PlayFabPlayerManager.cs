using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using PlayFab;
using PlayFab.CloudScriptModels;
using PlayFab.ClientModels;
using CrimsonLibrary.SupportLibrary.Utils.Generics;
using PlayFab.DataModels;
using EntityKey = PlayFab.DataModels.EntityKey;
using PlayFab.ProfilesModels;
using Firebase.Analytics;
using Newtonsoft.Json;
using CrimsonLibrary.SupportLibrary.Extensions;

public class PlayFabPlayerManager : GenericManager<PlayFabPlayerManager>
{
    public Action onLoginSuccess;
    public Action onLoginFail;

    public Action onPlayerDataReady;

    public PlayFabAuthService _AuthService = PlayFabAuthService.Instance;

    private bool isLoggedIn;

    private string entityId;
    private string entityType;
    private string playFabId;
    private string playerCountryCode;

    public string PlayFabId
    {
        get
        {
            return PlayerPrefs.GetString("playfabid");
        }
    }

    public string EntityId { get => entityId; }
    public string PlayerCountryCode { get => playerCountryCode; }

    public bool IsLoggedIn
    {
        get
        {
            return isLoggedIn;
        }
    }

    public PlayFab.CloudScriptModels.EntityKey GetEntityKeyForCloudScript()
    {
        PlayFab.CloudScriptModels.EntityKey key = new PlayFab.CloudScriptModels.EntityKey()
        {
            Id = entityId,
            Type = entityType,
        };
        return key;
    }

    public PlayFab.MultiplayerModels.EntityKey GetEntityKeyForMultiplayer()
    {
        PlayFab.MultiplayerModels.EntityKey key = new PlayFab.MultiplayerModels.EntityKey()
        {
            Id = entityId,
            Type = entityType,
        };
        return key;
    }

    public EntityKey GetEntityKeyForData()
    {
        EntityKey key = new EntityKey()
        {
            Id = entityId,
            Type = entityType,
        };
        return key;
    }

    public void InitiatePlayFab()
    {
        PlayFabSettings.staticSettings.TitleId = Properties.PlayFabProperties.titleId;
        if (Properties.PlayFabProperties.skipCertificateValidation)
        {
            PlayFab.Internal.PlayFabWebRequest.SkipCertificateValidation();
        }

        PlayFabAuthService.OnLoginSuccess += OnLoginSuccess;
        PlayFabAuthService.OnPlayFabError += OnPlayFabError;

        PlayFabAuthService.Instance.RememberMe = true;

        //_AuthService.InfoRequestParams = InfoRequestParams;
    }

    public void Login(string _username, string _email, string _password, Authtypes _authType)
    {
        _AuthService.Username = _username;
        _AuthService.Email = _email;
        _AuthService.Password = _password;

        _AuthService.Authenticate(_authType);
    }

    public void Register(string _username, string _email, string _password)
    {
        _AuthService.Username = _username;
        _AuthService.Email = _email;
        _AuthService.Password = _password;

        _AuthService.Authenticate(Authtypes.RegisterPlayFabAccount);
    }

    private void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        InitiatePlayFab();
        //if (_AuthService.HasUserPreviouslyLoggedIn)
        //{
        //    _AuthService.Authenticate();
        //}

        // recommended for debugging:
        //PlayGamesPlatform.DebugLogEnabled = true;

        // Activate the Google Play Games platform
        //PlayGamesPlatform.Activate();

        _AuthService.Authenticate(Authtypes.Silent);
    }

    void OnLoginSuccess(PlayFab.ClientModels.LoginResult result)
    {
        Debug.Log("PlayFab ID :: " + result.PlayFabId + ", Entity ID :: " + result.EntityToken.Entity.Id + ", Entity Type :: " + result.EntityToken.Entity.Type);

        playFabId = result.PlayFabId;
        entityId = result.EntityToken.Entity.Id;
        entityType = result.EntityToken.Entity.Type;
        isLoggedIn = true;

        if (!PlayerPrefs.HasKey("playfabid"))
        {
            PlayerPrefs.SetString("playfabid", result.PlayFabId);
            FirebaseAnalytics.LogEvent("first_login", "player_id_first_login", result.PlayFabId);
            Debug.Log("Sent first_login event to firebase with PlayerID :: " + result.PlayFabId);
        }

        PlayFabProfilesAPI.GetProfile(new GetEntityProfileRequest(),
        result =>
        {
            Debug.Log("Title id :: " + result.Profile.Lineage.TitlePlayerAccountId);
            Debug.Log("Master id :: " + result.Profile.Lineage.MasterPlayerAccountId);
        },
        error => Debug.LogError(error.GenerateErrorReport()));

        PlayFabClientAPI.GetPlayerProfile(new GetPlayerProfileRequest()
        {
            PlayFabId = Instance.PlayFabId,
            ProfileConstraints = new PlayerProfileViewConstraints() { ShowLocations = true }
        },
        result =>
        {
            playerCountryCode = result.PlayerProfile.Locations[0].CountryCode.ToString();
            onLoginSuccess.SafeInvoke();
        },
        error => Debug.LogError(error.GenerateErrorReport()));

        //Register for push notifications (PlayFab)
        StartCoroutine(WaitForTokenFromFirebase(() =>
        {
            AndroidDevicePushNotificationRegistrationRequest req = new AndroidDevicePushNotificationRegistrationRequest();
            req.DeviceToken = SettingsManager.Instance.FcmToken;

            PlayFabClientAPI.AndroidDevicePushNotificationRegistration(req,
            result =>
            {
                Debug.Log("Successfully registered for push notifications from PlayFab/FCM");
            },
            error =>
            {
                Debug.Log("Error registering for push notifications :: " + error.GenerateErrorReport());
            });
        }));
    }

    void OnPlayFabError(PlayFabError loginResult)
    {
        Debug.Log("PlayFab Error");
        Debug.LogError(loginResult.GenerateErrorReport());

        if (!PlayerPrefs.HasKey("playfabid"))
        {
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            PlayerPrefs.SetString("playfabid", deviceId);
            playFabId = deviceId;
            FirebaseAnalytics.LogEvent("first_login", "player_id_first_login", deviceId);
            Debug.Log("Sent first_login event to firebase with PlayerID :: " + deviceId);
        }
        else
        {
            playFabId = PlayerPrefs.GetString("playfabid");
        }

        onLoginFail.SafeInvoke();
    }

    IEnumerator WaitForTokenFromFirebase(Action onFirebaseTokenSet)
    {
        yield return new WaitUntil(() => !string.IsNullOrEmpty(SettingsManager.Instance.FcmToken));
        onFirebaseTokenSet.SafeInvoke();
    }
    public void GetPlayerCurrencies()
    {

        PlayFabClientAPI.GetUserInventory(new PlayFab.ClientModels.GetUserInventoryRequest(),
            (result) =>
            {
                foreach (var pair in result.VirtualCurrency)
                {
                    Debug.Log(pair.Key + " :: " + pair.Value);
                };
            },
            (error) =>
            {
                Debug.Log("Error getting player currency");
            });
    }

    private static string GenerateGuidEquivalent()
    {
        byte[] buffer = Guid.NewGuid().ToByteArray();
        long longValue = BitConverter.ToInt64(buffer, 0);
        return Math.Abs(longValue).ToString("X16").Substring(9);
    }
}
