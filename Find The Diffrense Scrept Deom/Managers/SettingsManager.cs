using CrimsonGames.Analytics;
using CrimsonLibrary.SupportLibrary.Utils.Generics;
using CrimsonLibrary.SupportLibrary.Utils.InternetReachabilityVerifier;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using CrimsonLibrary.SupportLibrary.Extensions;
using Cysharp.Threading.Tasks;

public class SettingsManager : GenericManager<SettingsManager>
{
    private InternalGameSettingsData internalSettingsData;
    private MongoSettings mongoSettingsData;
    private EconomySettings economySettingsData;
    private ExperimentDataSettings experimentData;

    //private List<FiltersTranslationItem> filtersTranslationItems;

    public List<GameObject> debugObjects = new List<GameObject>();

    public InternetReachabilityVerifier.Status internetStatus;

    public Action onApplicationFocusLost;
    public Action onApplicationPause;
    public Action onApplicationResume;
    public Action onApplicationBackground;
    public Action onApplicationForeground;

    public Action onFirebaseInitialized;

    Firebase.DependencyStatus dependencyStatus = Firebase.DependencyStatus.UnavailableOther;
    protected bool isFirebaseInitialized = false;

    private int sessionTimeInSeconds;

    public Action<int> onSessionTimerUpdate;

    public Action onConfigDownloaded;

    public Action<NotificationClickData> onNotificationClicked;

    public Action<InternetReachabilityVerifier.Status> onInternetStatusChange;

    private bool isConfigDownloaded = false;

    private FirebaseApp app;

    private string fcmToken;

    public bool isProductionBuild = false;
    public bool logEvents;
    public bool logAllDebug;
    public bool showAds;
    public bool showRewardedAds;
    public bool debugDateForDailyStreak;
    public bool useFakeStore;
    public float configWaitTimeout;
    public bool debugHideAds;
    public bool useTestConfig;
    public bool logTextureInfo;

    private const string playStoreURL = "playstore URL";

    public InternalGameSettingsData InternalSettingsData
    {
        get
        {
            return internalSettingsData;
        }
    }

    public MongoSettings MongoSettingsData
    {
        get
        {
            return mongoSettingsData;
        }
    }

    public EconomySettings EconomySettingsData
    {
        get
        {
            return economySettingsData;
        }
    }

    public int SessionTimeInSeconds
    {
        get
        {
            return sessionTimeInSeconds;
        }
    }

    public bool IsConfigDownloaded { get => isConfigDownloaded; }
    public string FcmToken { get => fcmToken; }

    public static string PlayStoreURL => playStoreURL;

    public ExperimentDataSettings ExperimentData { get => experimentData; }
    public bool HasSetUserPropertiesFirebase { get => hasSetUserPropertiesFirebase; }

    private int lastLoggedSessionTime = -1;

    private bool hasSetUserPropertiesFirebase = false;

    private const string LastSessionKey = "lastsessionutc";
    private const string LastSessionKeyLocal = "lastsessionlocal";

    private void Awake()
    {
#if !UNITY_EDITOR
        Application.targetFrameRate = 120;

        if(isProductionBuild)
        {
            Debug.unityLogger.logEnabled = false;
        }
        else
        {
            if(logAllDebug)
            {
                Debug.unityLogger.logEnabled = true;
            }
            else
            {
                Debug.unityLogger.logEnabled = false;
            }
        }
#endif
        DataWriter.Init();
        FTDSaveLoadService.Init();
        CrimsonGames.Analytics.CrimsonEventsLogger.Init();
        InternetReachabilityVerifier.Instance.statusChangedDelegate += OnNetworkStatusChange;
        onFirebaseInitialized += () => { FetchDataAsync(); };

        Firebase.Messaging.FirebaseMessaging.TokenRegistrationOnInitEnabled = true;
        //AppStateEventNotifier.AppStateChanged += OnAppStateChanged; //TODO: Implement when Google Ads SDK integrated

        //if (PlayerPrefs.HasKey("language"))
        //{
        //    LocalizationManager.CurrentLanguage = PlayerPrefs.GetString("language");
        //    Debug.Log("Current language set to :: " + LocalizationManager.CurrentLanguage);
        //}
    }

    protected void Start()
    {
        FirebaseStartupCheck();

        StartCoroutine(SessionTimer());

        if (!isProductionBuild)
        {
            foreach (var item in debugObjects)
            {
                item.SetActive(true);
            }
        }
        else
        {
            foreach (var item in debugObjects)
            {
                item.SetActive(false);
            }
        }
    }

    private void OnNetworkStatusChange(InternetReachabilityVerifier.Status newStatus)
    {
        internetStatus = newStatus;
        onInternetStatusChange.SafeInvoke(newStatus);
    }

    public IEnumerator SessionTimer()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(1f);
            sessionTimeInSeconds++;
            onSessionTimerUpdate.SafeInvoke(sessionTimeInSeconds);
        }
    }

    private void SetInternalSettingsData(string settingsJson)
    {
        internalSettingsData = JsonConvert.DeserializeObject<InternalGameSettingsData>(settingsJson);
        DataWriter.WriteToDisk("lastdownloaded_internalsettings", internalSettingsData, eDataSaveType.JSON, "Settings");
    }

    public string GetInternalSettingsDataDefaultJson()
    {
        InternalGameSettingsData data = new InternalGameSettingsData();
        data.gameVersion = Application.version;
        data.catalogURL = "catalogUR:";
        data.prePuzzleAdsNumber = 1;
        data.postPuzzleAdsNumber = 5;
        data.rateUsPuzzleNumber = 3;
        data.iapPromoPuzzleNumber = 2;
        data.socialMediaPuzzleNumber = 5;
        data.firstSessionShowAd = false;
        data.gameVersion = Application.version;
        data.maxLivesCount = 5;
        data.maxHintCount = 3;
        return JsonConvert.SerializeObject(data);
    }

    public string GetMongoSettingsDataDefaultJson()
    {
        MongoSettings mongoSettings = new MongoSettings();
        mongoSettings.endpoint = "Mangodb path";
        mongoSettings.dataSource = "Cluster0";
        mongoSettings.database = "cg_master";
        mongoSettings.masterDatabase = "cg_master";
        mongoSettings.database_production = "cg_master_prod";
        mongoSettings.masterDatabase_production = "cg_master_prod";
        mongoSettings.eventCollection = "4_differences_gods_stories";
        mongoSettings.dauMasterCollection = "dau_master";
        mongoSettings.playerMasterCollection = "player_master";
        mongoSettings.playerStatsCollection = "sudoku_player_stats";
        mongoSettings.apiKey = "api KEy";

        return JsonConvert.SerializeObject(mongoSettings);
    }

    //Firebase Runtime Config

    internal void FirebaseStartupCheck()
    {
        Debug.Log("FirebaseStartupCheck called");
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                FirebaseAnalytics.SetUserProperty(FirebaseAnalytics.UserPropertySignUpMethod, "PlayFab");
                FirebaseAnalytics.SetUserId(PlayFabPlayerManager.Instance.PlayFabId);
                app = Firebase.FirebaseApp.DefaultInstance;
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
    }

    void InitializeFirebase()
    {
        Dictionary<string, object> defaults = new Dictionary<string, object>();

        defaults.Add("GameSettings", GetInternalSettingsDataDefaultJson());

        Debug.Log("Firebase Messaging Initialized");
        Debug.Log("App Name :: " + app.Name);
        Debug.Log("App ProjectId :: " + app.Options.ProjectId);
        Debug.Log("App MessageSenderId :: " + app.Options.MessageSenderId);
        Debug.Log("App ApiKey :: " + app.Options.ApiKey);

        Firebase.Messaging.FirebaseMessaging.MessageReceived += OnMessageReceived;
        Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
        Firebase.Messaging.FirebaseMessaging.SubscribeAsync("PushNotifications").ContinueWithOnMainThread(task =>
        {
            Debug.Log("Sucessfully subscribed to push notifications");
        });

        Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults).ContinueWithOnMainThread(task =>
        {
            onFirebaseInitialized.SafeInvoke();
        });

        SetUserPropertiesFirebase();

        isFirebaseInitialized = true;
    }

    public async void SetUserPropertiesFirebase()
    {
        bool hasUserProperties = PlayerPrefs.HasKey("firebaseuserproperties");

        FirebaseAnalytics.SetUserProperty("build_no", CrimsonEventsLogger.GetKingdom().build_no.ToString());
        Debug.Log($"[SetUserPropertiesFirebase] build_no set: {CrimsonEventsLogger.GetKingdom().build_no}");

        CGInstallDataMongo installDataMongo = await WaitForInstallDataMongo();
        if (installDataMongo == null)
        {
            Debug.LogWarning("[SetUserPropertiesFirebase] Install data is null. Skipping Firebase properties update.");
            return;
        }

        int eventUtc = (int)EventDataUtilities.GetCurrentEpochTime();
        int installUtc = installDataMongo.install.utc;
        int utcDiffDays = DateTimeExtensions.CalculateDiffDays(eventUtc, installUtc);
        int localDiffDays = DateTimeExtensions.CalculateLocalDiffDays(eventUtc, installUtc);

        Debug.Log($"[SetUserPropertiesFirebase] Calculated Days - UTC: {utcDiffDays}, Local: {localDiffDays}");

        FirebaseAnalytics.SetUserProperty("cohortLocal", DateTimeExtensions.GetCohort(localDiffDays));
        FirebaseAnalytics.SetUserProperty("cohortUTC", DateTimeExtensions.GetCohort(utcDiffDays));
        FirebaseAnalytics.SetUserProperty("diffDaysUTC", utcDiffDays.ToString());
        FirebaseAnalytics.SetUserProperty("diffDaysLocal", localDiffDays.ToString());
        FirebaseAnalytics.SetUserProperty("daysSinceLastActiveUTC", TrackSession());
        FirebaseAnalytics.SetUserProperty("daysSinceLastActiveLocal", TrackLocalSession());

        if (!hasUserProperties)
        {
            Debug.Log("[SetUserPropertiesFirebase] First-time properties being set.");

            FirebaseAnalytics.SetUserProperty("cg_campaign", installDataMongo.campaign.name);
            FirebaseAnalytics.SetUserProperty("network", installDataMongo.install.network);
            FirebaseAnalytics.SetUserProperty("creative", installDataMongo.campaign.creativeName);
            FirebaseAnalytics.SetUserProperty("isReengagement", installDataMongo.campaign.isRengagement ? "true" : "false");
            FirebaseAnalytics.SetUserProperty("installDateUTC", DateTimeExtensions.UnixToCustomDateFormat(installDataMongo.install.utc));
            FirebaseAnalytics.SetUserProperty("installDateLocal", DateTimeExtensions.UnixToLocalCustomDateFormat(installDataMongo.install.utc));
            FirebaseAnalytics.SetUserProperty("installBuildNo", installDataMongo.device.appVersion);

            PlayerPrefs.SetInt("firebaseuserproperties", 1);
            PlayerPrefs.Save(); // Ensure it persists immediately

            Debug.Log("[SetUserPropertiesFirebase] PlayerPrefs updated and saved.");
        }

        hasSetUserPropertiesFirebase = true;

        CrimsonEventsLogger.LogEvent(EPlayerEvent.cg_session_start);
    }

    public static string TrackSession()
    {
        string todayString = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        if (PlayerPrefs.HasKey(LastSessionKey))
        {
            string lastSessionString = PlayerPrefs.GetString(LastSessionKey);
            if (DateTime.TryParse(lastSessionString, out DateTime lastDate))
            {
                int daysSinceLast = (DateTime.UtcNow.Date - lastDate.Date).Days;

                // Save current session date
                PlayerPrefs.SetString(LastSessionKey, todayString);
                PlayerPrefs.Save();

                return CategorizeDaysSince(daysSinceLast);
            }
        }

        // First launch or invalid date
        PlayerPrefs.SetString(LastSessionKey, todayString);
        PlayerPrefs.Save();
        return "0";
    }

    public static string TrackLocalSession()
    {
        string todayString = DateTime.Now.Date.ToString("yyyy-MM-dd");

        if (PlayerPrefs.HasKey(LastSessionKeyLocal))
        {
            string lastSessionString = PlayerPrefs.GetString(LastSessionKeyLocal);
            if (DateTime.TryParse(lastSessionString, out DateTime lastDate))
            {
                int daysSinceLast = (DateTime.Now.Date - lastDate.Date).Days;

                // Save current session date
                PlayerPrefs.SetString(LastSessionKeyLocal, todayString);
                PlayerPrefs.Save();

                return CategorizeDaysSince(daysSinceLast);
            }
        }

        // First launch or invalid date
        PlayerPrefs.SetString(LastSessionKeyLocal, todayString);
        PlayerPrefs.Save();
        return "0";
    }

    private static string CategorizeDaysSince(int days)
    {
        if (days <= 6)
            return days.ToString();
        else if (days <= 14)
            return "7-14";
        else if (days <= 30)
            return "15-30";
        else
            return "30+";
    }

    private static async Task<CGInstallDataMongo> WaitForInstallDataMongo()
    {
        CGInstallDataMongo installDataMongo;

        do
        {
            installDataMongo = InstallDataManager.Instance.installDataMongo;

            // Check if PlayerID is valid
            if (!string.IsNullOrEmpty(installDataMongo?.PlayerID))
                break;

            // Wait before rechecking
            await Task.Delay(100);
        } while (true);

        return installDataMongo;
    }

    public virtual void OnMessageReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs e)
    {
        Debug.Log("Received a new message");
        var notification = e.Message.Notification;
        if (notification != null)
        {
            Debug.Log("title: " + notification.Title);
            Debug.Log("body: " + notification.Body);
            var android = notification.Android;
            if (android != null)
            {
                Debug.Log("android channel_id: " + android.ChannelId);
            }
        }
        if (e.Message.From.Length > 0)
            Debug.Log("from: " + e.Message.From);
        if (e.Message.Link != null)
        {
            Debug.Log("link: " + e.Message.Link.ToString());
        }
        if (e.Message.Data.Count > 0)
        {
            NotificationClickData notificationClickData = new NotificationClickData();
            Debug.Log("data:");
            foreach (System.Collections.Generic.KeyValuePair<string, string> iter in
                     e.Message.Data)
            {
                Debug.Log("  " + iter.Key + ": " + iter.Value);
                if (iter.Key == "customData")
                {
                    notificationClickData.category = JsonConvert.DeserializeObject<NotificationClickCustomData>(iter.Value).notificationCategory;
                }

                if (iter.Key == "title")
                {
                    notificationClickData.title = iter.Value;
                }
            }

            if (!string.IsNullOrEmpty(notificationClickData.category) && !string.IsNullOrEmpty(notificationClickData.title))
            {
                onNotificationClicked.SafeInvoke(notificationClickData);
            }
        }
    }

    public virtual void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
    {
        Debug.Log("Received Registration Token: " + token.Token);
        fcmToken = token.Token;
    }

    protected bool LogTaskCompletion(Task task, string operation)
    {
        bool complete = false;
        if (task.IsCanceled)
        {
            Debug.Log(operation + " canceled.");
        }
        else if (task.IsFaulted)
        {
            Debug.Log(operation + " encounted an error.");
            foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
            {
                string errorCode = "";
                Firebase.FirebaseException firebaseEx = exception as Firebase.FirebaseException;
                if (firebaseEx != null)
                {
                    errorCode = String.Format("Error.{0}: ",
                      ((Firebase.Messaging.Error)firebaseEx.ErrorCode).ToString());
                }
                Debug.Log(errorCode + exception.ToString());
            }
        }
        else if (task.IsCompleted)
        {
            Debug.Log(operation + " completed");
            complete = true;
        }
        return complete;
    }

    public Task FetchDataAsync()
    {
        Debug.Log("Firebase FetchDataAsync Called");
        bool isTimeout = false;
        //UIManager.Instance.AppendDebugLogString("Firebase FetchDataAsync Called");
        Task fetchTask = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero);

        StartCoroutine(WaitForSeconds(configWaitTimeout, () =>
        {
            Debug.Log("[FetchDataAsync] Timing out firebase fetch at " + configWaitTimeout + " seconds");
            //UIManager.Instance.AppendDebugLogString("[FetchDataAsync] Timing out firebase fetch at " + configWaitTimeout + " seconds");
            if (!isConfigDownloaded)
            {
                HandleConfigDownloadFailOrTimeout();
                isConfigDownloaded = true;

                onConfigDownloaded.SafeInvoke();
            }
        }));

        //return fetchTask.ContinueWithOnMainThread(FetchComplete);
        return fetchTask.ContinueWithOnMainThread(task =>
        {
            // Check if the timeout has occurred
            if (!isTimeout)
            {
                // Only execute FetchComplete if the timeout hasn't happened
                FetchComplete(task);
            }
            else
            {
                Debug.Log("Skipping FetchComplete because of timeout");
            }
        });
    }

    void FetchComplete(Task fetchTask)
    {
        var info = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.Info;
        switch (info.LastFetchStatus)
        {
            case Firebase.RemoteConfig.LastFetchStatus.Success:
                Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.ActivateAsync()
                .ContinueWithOnMainThread(async task =>
                {
                    Debug.Log("GameSettings config pre fetch");
                    string settingsJson = string.Empty;

                    if (!useTestConfig)
                    {
                        settingsJson = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue("GameSettings").StringValue;
                    }
                    else
                    {
                        settingsJson = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue("GameSettingsTest").StringValue;
                    }

                    SetInternalSettingsData(settingsJson);
                    DataWriter.WriteToDisk("lastdownloaded_internalsettings", internalSettingsData, eDataSaveType.JSON, "Settings");
                    Debug.Log("GameSettings config set");

                    Debug.Log("Mongo config pre fetch");
                    string mongoSettingsJson = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue("Mongo").StringValue;
                    SetMongoSettings(mongoSettingsJson);
                    DataWriter.WriteToDisk("lastdownloaded_mongosettings", mongoSettingsData, eDataSaveType.JSON, "Settings");
                    Debug.Log("Mongo config set");

                    Debug.Log("Level information set");

                    Debug.Log(String.Format("Remote data loaded and ready (last fetch time {0}).",
               info.FetchTime));

                    if (isConfigDownloaded)
                    {
                        return;
                    }

                    isConfigDownloaded = true;
                    onConfigDownloaded.SafeInvoke();
                    Debug.Log("OnConfigDownloaded SafeInvoke()");
                });

                break;
            case Firebase.RemoteConfig.LastFetchStatus.Failure:
                switch (info.LastFetchFailureReason)
                {
                    case Firebase.RemoteConfig.FetchFailureReason.Error:
                        Debug.Log("Fetch failed for unknown reason");
                        //UIManager.Instance.AppendDebugLogString("Fetch failed for unknown reason");
                        break;
                    case Firebase.RemoteConfig.FetchFailureReason.Throttled:
                        Debug.Log("Fetch throttled until " + info.ThrottledEndTime);
                        //UIManager.Instance.AppendDebugLogString("Fetch throttled until " + info.ThrottledEndTime);
                        break;
                }

                HandleConfigDownloadFailOrTimeout();

                if (isConfigDownloaded)
                {
                    return;
                }

                isConfigDownloaded = true;
                onConfigDownloaded.SafeInvoke();
                break;

            case Firebase.RemoteConfig.LastFetchStatus.Pending:

                Debug.Log("Latest Fetch call still pending.");
                //UIManager.Instance.AppendDebugLogString("Latest Fetch call still pending");

                StartCoroutine(WaitForSeconds(configWaitTimeout, () =>
                {
                    Debug.Log("Timing out firebase fetch at " + configWaitTimeout + " seconds");
                    //UIManager.Instance.AppendDebugLogString("Timing out firebase fetch at " + configWaitTimeout + " seconds");
                    if (!isConfigDownloaded)
                    {
                        HandleConfigDownloadFailOrTimeout();

                        if (isConfigDownloaded)
                        {
                            return;
                        }

                        isConfigDownloaded = true;
                        onConfigDownloaded.SafeInvoke();
                    }
                }));

                break;
        }
    }

    void HandleConfigDownloadFailOrTimeout()
    {
        if (internalSettingsData != null)
        {
            return;
        }

        if (HasAllSavedConfig())
        {
            Debug.Log("Cached config found, loading");

            internalSettingsData = DataReader.ReadFromDisk<InternalGameSettingsData>("lastdownloaded_internalsettings", eDataSaveType.JSON, "Settings");
            mongoSettingsData = DataReader.ReadFromDisk<MongoSettings>("lastdownloaded_mongosettings", eDataSaveType.JSON, "Settings");

            Debug.Log("Set config from cache");
        }
        else
        {
            SetInternalSettingsData(GetInternalSettingsDataDefaultJson());
            SetMongoSettings(GetMongoSettingsDataDefaultJson());
            Debug.Log("Set config from defaults because did not find cached config");
        }
    }

    IEnumerator WaitForSeconds(float seconds, Action postWait)
    {
        yield return new WaitForSeconds(seconds);
        postWait.SafeInvoke();
    }

    void SetMongoSettings(string mongoSettingsJson)
    {
        mongoSettingsData = JsonConvert.DeserializeObject<MongoSettings>(mongoSettingsJson);

        if (isProductionBuild)
        {
            mongoSettingsData.database = mongoSettingsData.database_production;
            mongoSettingsData.masterDatabase = mongoSettingsData.masterDatabase_production;
        }
    }

    public bool HasAllSavedConfig()
    {
        Debug.Log("[HasAllSavedConfig] called");
        bool hasSavedConfig = HasSavedSettings("lastdownloaded_internalsettings");
        bool hasSavedMongo = HasSavedSettings("lastdownloaded_mongosettings");
        bool hasSavedLevelInfo = HasSavedSettings("lastdownloaded_levelinfo");
        bool hasSavedChapterInfo = HasSavedSettings("lastdownloaded_chapterInfo");

        return hasSavedConfig && hasSavedMongo && hasSavedLevelInfo && hasSavedChapterInfo;
    }

    public bool HasSavedSettings(string fileName)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        string folderPath = Application.persistentDataPath + "/" + "Settings" + "/";
        string filePath = folderPath + fileName + Properties.ASSET_EXTENSION;

#elif UNITY_ANDROID && !UNITY_EDITOR

        string folderPath = Application.persistentDataPath + "/" + "Settings" + "/";
        string filePath = folderPath + fileName + Properties.ASSET_EXTENSION;
#endif
        if (File.Exists(filePath))
        {
            return true;
        }

        return false;
    }

    private void OnAppStateChanged(/*AppState state*/)
    {
        //if(state == AppState.Background)
        //{
        //    if (sessionTimeInSeconds - lastLoggedSessionTime >= 1)
        //    {
        //        CrimsonEventsLogger.LogSessionTime(sessionTimeInSeconds);
        //        lastLoggedSessionTime = sessionTimeInSeconds;

        //        sessionTimeInSeconds = 0;
        //        lastLoggedSessionTime = 0;

        //        onApplicationBackground.SafeInvoke();
        //    }
        //}
        //else if(state == AppState.Foreground)
        //{
        //    onApplicationForeground.SafeInvoke();
        //}
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            if (sessionTimeInSeconds - lastLoggedSessionTime >= 1)
            {
                //CrimsonEventsLogger.LogEvent(EPlayerEvent.session_ended, family: sessionTimeInSeconds);
                lastLoggedSessionTime = sessionTimeInSeconds;

                sessionTimeInSeconds = 0;
                lastLoggedSessionTime = 0;
            }
            onApplicationPause.SafeInvoke();
        }
    }

    private void OnApplicationFocus(bool focus) // Home button works but hamburger doesn't
    {
        if (!focus)
        {
            if (sessionTimeInSeconds - lastLoggedSessionTime >= 1)
            {
                //CrimsonEventsLogger.LogEvent(EPlayerEvent.session_ended, family: sessionTimeInSeconds);
                lastLoggedSessionTime = sessionTimeInSeconds;

                sessionTimeInSeconds = 0;
                lastLoggedSessionTime = 0;
            }

            onApplicationFocusLost.SafeInvoke();
        }
        else
        {
            onApplicationResume.SafeInvoke();
        }
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();

        //CrimsonEventsLogger.LogEvent(EPlayerEvent.session_ended, family: sessionTimeInSeconds);
        sessionTimeInSeconds = 0;
        lastLoggedSessionTime = 0;

        CrimsonEventsLogger.UnsubscribeActions();
    }
}
