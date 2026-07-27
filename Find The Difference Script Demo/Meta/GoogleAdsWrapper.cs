using CrimsonLibrary.SupportLibrary.Utils.Generics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using System;
using CrimsonLibrary.SupportLibrary.Utils.InternetReachabilityVerifier;
using UnityEngine.Advertisements;
using GoogleMobileAds.Common;
using Newtonsoft.Json;
using System.Linq;
using CrimsonGames.Analytics;

//[Serializable]
//public class CGAdRevenueData
//{
//    public string ad_platform;
//    public string ad_currency;
//    public string ad_type;
//    public float ad_revenue;

//    public void AddRevenue(float revenueToAdd)
//    {
//        ad_revenue = ad_revenue + revenueToAdd;
//    }
//}

public class GoogleAdsWrapper : GenericManager<GoogleAdsWrapper>, IUnityAdsInitializationListener
{
    public enum EAdType
    {
        Banner,
        Interstitial,
        Rewarded,
        Native
    }

    private string _bannerTopUnitId = "id";
    private string _bannerUnitId = "id"; //Our banner
    private string _interstitialUnitId = "id"; //Our interstitial
    private string _rewardedUnitId = "id"; //Our rewarded


    BannerView _bannerView;
    BannerView _bannerViewTop;
    InterstitialAd _interstitialAd;
    RewardedAd _rewardedAd;
    NativeOverlayAd _nativeOverlayAd;

    public bool unityAdsTestMode;
    public string unityGameId;

    private string currentBannerNetworkSourceName;
    private string currentTopBannerNetworkSourceName;
    private string currentInterstitialNetworkSourceName;
    private string currentRewardedNetworkSourceName;

    private bool hideBannerOnLoad = false;

    public bool hasAttemptedFirstPuzzleInSession = false;

    private bool rewardedAdFlag = false;

    protected void Awake()
    {
        base.Awake();

        IAPManager.OnIAPPurchaseSuccess += (prod) =>
        {
            if (prod.definition.id == "no_ads_package")
            {
                SettingsManager.Instance.showAds = false;
                HideBannerAd();
            }
        };

        SettingsManager.Instance.showAds = !IAPManager.Instance.HasPurchasedNoAds();
        //SettingsManager.Instance.showRewardedAds = !IAPManager.Instance.HasPurchasedNoAdsWithHint();
        if (SettingsManager.Instance.showAds)
        {
            hideBannerOnLoad = false;
        }
        else
        {
            hideBannerOnLoad = true;
            HideBannerAd();
        }
        InitializeUnityAds();
        InitializeGoogleAds();

        SettingsManager.Instance.onConfigDownloaded += () =>
        {
            hasAttemptedFirstPuzzleInSession = SettingsManager.Instance.InternalSettingsData.firstSessionShowAd;
        };
        
        InternetReachabilityVerifier.Instance.statusChangedDelegate += OnNetworkStatusChange;
        //AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
        //SettingsManager.Instance.onApplicationResume += ShowInterstitialAd;
    }

    public void InitializeGoogleAds()
    {
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            Debug.Log("Google AdMob SDK successfully initialized");

            Dictionary<string, AdapterStatus> map = initStatus.getAdapterStatusMap();
            foreach (KeyValuePair<string, AdapterStatus> keyValuePair in map)
            {
                string className = keyValuePair.Key;
                AdapterStatus status = keyValuePair.Value;
                switch (status.InitializationState)
                {
                    case AdapterState.NotReady:
                        // The adapter initialization did not complete.
                        Debug.Log("Adapter: " + className + " not ready.");
                        break;
                    case AdapterState.Ready:
                        // The adapter was successfully initialized.
                        Debug.Log("Adapter: " + className + " is initialized.");
                        break;
                }
            }
        });


        StartCoroutine(CheckAndLoadAdsPeriodically());
        //LoadAd(EAdType.Banner);
        //LoadAd(EAdType.Interstitial);
        //LoadAd(EAdType.Rewarded);
    }

    void CheckAndLoadAd(EAdType adType)
    {
        switch (adType)
        {
            case EAdType.Banner:
                if (_bannerView == null)
                {
                    LoadAd(EAdType.Banner);
                }
                break;
            case EAdType.Interstitial:
                if (_interstitialAd == null)
                {
                    LoadAd(EAdType.Interstitial);
                }
                break;
            case EAdType.Rewarded:
                if (_rewardedAd == null)
                {
                    LoadAd(EAdType.Rewarded);
                }
                break;
            case EAdType.Native:
                if (_nativeOverlayAd == null)
                {
                    LoadAd(EAdType.Native);
                }
                break;
            // Add additional ad types if needed
            default:
                //Debug.LogError("Unsupported ad type: " + adType);
                break;
        }
    }

    IEnumerator CheckAndLoadAdsPeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(30f);

            CheckAndLoadAd(EAdType.Banner);
            CheckAndLoadAd(EAdType.Interstitial);
            CheckAndLoadAd(EAdType.Rewarded);
            CheckAndLoadAd(EAdType.Native);
        }
    }

    private void OnNetworkStatusChange(InternetReachabilityVerifier.Status newStatus)
    {
        if (newStatus == InternetReachabilityVerifier.Status.NetVerified)
        {
            CheckAndLoadAd(EAdType.Banner);
            CheckAndLoadAd(EAdType.Interstitial);
            CheckAndLoadAd(EAdType.Rewarded);
            CheckAndLoadAd(EAdType.Native);
        }
    }

    public void OpenAdInspector()
    {
        MobileAds.OpenAdInspector((AdInspectorError error) =>
        {
            Debug.LogError("There was some issue showing ad inspector");
        });
    }

    public void InitializeUnityAds()
    {
        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            Advertisement.Initialize(unityGameId, unityAdsTestMode, this);
        }
    }

    public void CreateBannerView()
    {
        //Debug.Log("Creating banner view");

        // If we already have a banner, destroy the old one.
        if (_bannerView != null)
        {
            DestroyBannerAd();
        }

        if(_bannerViewTop != null)
        {
            DestroyBannerAdTop();
        }

        _bannerView = new BannerView(_bannerUnitId, AdSize.IABBanner, AdPosition.Bottom);
        _bannerViewTop = new BannerView(_bannerTopUnitId, AdSize.Banner, AdPosition.Top);

        ListenToAdEvents();
    }

    public void LoadAd(EAdType adType)
    {
        switch (adType)
        {
            case EAdType.Banner:
                // Load banner ad
                CreateBannerView();
                // create our request used to load the ad.
                var adRequest = new AdRequest();
                adRequest.Keywords.Add("crimsongames_banner"); //Change this

                // send the request to load the ad.
                //Debug.Log("Loading banner ad.");
                _bannerView.LoadAd(adRequest);

                var adRequestTop = new AdRequest();
                adRequest.Keywords.Add("crimsongames_banner_top"); //Change this
                _bannerViewTop.LoadAd(adRequestTop);

                //Auto hide
                if(hideBannerOnLoad)
                {
                    HideBannerAd();
                }

                break;
            case EAdType.Interstitial:
                // Load interstitial ad
                LoadInterstitialAd(_interstitialUnitId);
                // Rest of the interstitial ad loading code
                break;
            case EAdType.Rewarded:
                // Load rewarded ad
                // Implement rewarded ad loading code here
                LoadRewardedAd(_rewardedUnitId);
                break;
            case EAdType.Native:
                // Load native ad
                // Implement native ad loading code here
                //LoadNativeAd(_nativeUnitId);
                break;
            default:
                Debug.LogError("Unsupported ad type: " + adType);
                break;
        }
    }

    public void LoadNativeAd(string _nativeAdUnitId)
    {
        // Clean up the old ad before loading a new one.
        if (_nativeOverlayAd != null)
        {
            DestroyNativeAd();
        }

        Debug.Log("Loading native overlay ad.");

        var adRequest = new AdRequest();

        var options = new NativeAdOptions
        {
            AdChoicesPlacement = AdChoicesPlacement.TopRightCorner,
            MediaAspectRatio = MediaAspectRatio.Portrait,
            VideoOptions = new VideoOptions { ClickToExpandRequested = true, CustomControlsRequested = false, StartMuted = false }
        };

        NativeOverlayAd.Load(_nativeAdUnitId, adRequest, options, (NativeOverlayAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError("Native Overlay ad failed to load an ad " +
                               " with error: " + error);
                return;
            }

            // The ad should always be non-null if the error is null, but
            // double-check to avoid a crash.
            if (ad == null)
            {
                Debug.LogError("Unexpected error: Native Overlay ad load event " +
                               " fired with null ad and null error.");
                return;
            }

            if (ad != null)
            {
                ResponseInfo responseInfo = ad.GetResponseInfo();

                if (responseInfo != null)
                {
                    currentRewardedNetworkSourceName = GetAdNetworkNameFromAdapterClassName(ad.GetResponseInfo().GetMediationAdapterClassName());
                }
            }

            // The operation completed successfully.
            Debug.Log("Native Overlay ad loaded with response : " +
                       ad.GetResponseInfo());
            _nativeOverlayAd = ad;

            // Register to ad events to extend functionality.
            RegisterEventHandlers(ad);
        });
    }

    public void RenderAd()
    {
        if (_nativeOverlayAd != null)
        {
            Debug.Log("Rendering Native Overlay ad.");

            //// Define a native template style with a custom style.
            //var style = new NativeTemplateStyle
            //{
            //    TemplateID = NativeTemplateID.Medium,
            //    MainBackgroundColor = Color.red,
            //    CallToActionText = new NativeTemplateTextStyles
            //    {
            //        BackgroundColor = Color.green,
            //        FontColor = Color.white,
            //        FontSize = 9,
            //        Style = NativeTemplateFontStyle.Bold
            //    }
            //};

            //public NativeTemplateStyle(NativeTemplateStyle templateStyle)
            //{
            //    TemplateId = templateStyle.TemplateId;
            //    MainBackgroundColor = templateStyle.MainBackgroundColor;
            //    PrimaryText = templateStyle.PrimaryText;
            //    SecondaryText = templateStyle.SecondaryText;
            //    TertiaryText = templateStyle.TertiaryText;
            //    CallToActionText = templateStyle.CallToActionText;
            //}

            NativeTemplateStyle style = new NativeTemplateStyle();
            style.TemplateId = NativeTemplateId.Medium;
            style.MainBackgroundColor = Color.red;
            style.CallToActionText = new NativeTemplateTextStyle
            {
                BackgroundColor = Color.green,
                TextColor = Color.white,
                FontSize = 15,
                Style = NativeTemplateFontStyle.Bold
            };


            // Renders a native overlay ad at the default size
            // and anchored to the bottom of the scene.
            _nativeOverlayAd.RenderTemplate(style, AdPosition.Center);
        }
    }

    void DestroyNativeAd()
    {
        if (_nativeOverlayAd != null)
        {
            Debug.Log("Destroying native overlay ad.");
            _nativeOverlayAd.Destroy();
            _nativeOverlayAd = null;
        }
    }

    public void LoadInterstitialAd(string _interstitialUnitId)
    {
        if (string.IsNullOrEmpty(_interstitialUnitId))
        {
            Debug.LogError("Interstitial unit ID is null or empty.");
            return;
        }

        // Clean up the old ad before loading a new one.
        DestroyIntersitialAd();

        //Debug.Log("Loading the interstitial ad.");

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        if (adRequest == null)
        {
            Debug.LogError("Ad request is null.");
            return;
        }

        // send the request to load the ad.
        InterstitialAd.Load(_interstitialUnitId, adRequest,
            (InterstitialAd ad, LoadAdError error) =>
            {
                currentInterstitialNetworkSourceName = null;

                if (ad != null)
                {
                    ResponseInfo responseInfo = ad.GetResponseInfo();

                    if (responseInfo != null)
                    {
                        List<AdapterResponseInfo> adapterResponses = responseInfo.GetAdapterResponses();

                        if (adapterResponses != null && adapterResponses.Count > 0)
                        {
                            currentInterstitialNetworkSourceName = adapterResponses[0].AdSourceName;
                        }

                        currentInterstitialNetworkSourceName = GetAdNetworkNameFromAdapterClassName(ad.GetResponseInfo().GetMediationAdapterClassName());
                    }
                }

                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    //Debug.LogError("Interstitial ad failed to load an ad " +
                    //               "with error: " + error);
                    return;
                }

                //Debug.Log("Interstitial ad loaded with response: " + ad.GetResponseInfo());

                _interstitialAd = ad;
                RegisterEventHandlers(_interstitialAd);
            });
    }

    public void LoadRewardedAd(string _rewardedAdUnitId)
    {
        if (string.IsNullOrEmpty(_rewardedAdUnitId))
        {
            Debug.LogError("Rewarded ad unit ID is null or empty.");
            return;
        }

        DestroyRewardedAd();

        //Debug.Log("Loading the rewarded ad.");

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        if (adRequest == null)
        {
            Debug.LogError("Ad request is null.");
            return;
        }

        // send the request to load the ad.
        RewardedAd.Load(_rewardedAdUnitId, adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
                currentRewardedNetworkSourceName = null;

                if (ad != null)
                {
                    ResponseInfo responseInfo = ad.GetResponseInfo();

                    if (responseInfo != null)
                    {
                        List<AdapterResponseInfo> adapterResponses = responseInfo.GetAdapterResponses();

                        if (adapterResponses != null && adapterResponses.Count > 0)
                        {
                            currentRewardedNetworkSourceName = adapterResponses[0].AdSourceName;
                        }

                        currentRewardedNetworkSourceName = GetAdNetworkNameFromAdapterClassName(ad.GetResponseInfo().GetMediationAdapterClassName());
                    }
                }

                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    //Debug.LogError("Rewarded ad failed to load an ad " +
                    //               "with error: " + error);
                    return;
                }

                //Debug.Log("Rewarded ad loaded with response: " + ad.GetResponseInfo());

                _rewardedAd = ad;
                RegisterEventHandlers(_rewardedAd);
            });
    }

    public void ShowInterstitialAd()
    {
        if (SettingsManager.Instance.debugHideAds)
        {
            return;
        }

        if (!SettingsManager.Instance.showAds)
        {
            return;
        }

        if(rewardedAdFlag)
        {
            rewardedAdFlag = false;
            return;
        }
        //if (!hasAttemptedFirstPuzzleInSession)
        //{
        //    //Debug.Log("First puzzle attempt in session");
        //    hasAttemptedFirstPuzzleInSession = true;
        //    return;
        //}

        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            //Debug.Log("Showing interstitial ad.");
            _interstitialAd.Show();
        }
        else
        {
            //Debug.LogError("Interstitial ad is not ready yet.");
        }
    }

    public void ShowBannerAd()
    {
        //Debug.Log("Called show banner ad, show ads setting :: " + SettingsManager.Instance.showAds);
        if (SettingsManager.Instance.debugHideAds)
        {
            return;
        }

        if (!SettingsManager.Instance.showAds)
        {
            return;
        }

        if (_bannerView != null)
        {
            //Debug.Log("Showing banner view.");
            _bannerView.Show();
        }

        if(_bannerViewTop != null)
        {
            _bannerViewTop.Show();
        }
    }

    public void ShowRewardedAd(Action onSuccess)
    {
        if (SettingsManager.Instance.debugHideAds)
        {
            onSuccess.SafeInvoke();
            return;
        }

        const string rewardMsg =
            "Rewarded ad rewarded the user. Type: {0}, amount: {1}.";

        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            rewardedAdFlag = true;
            _rewardedAd.Show((Reward reward) =>
            {
                //Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
                onSuccess.SafeInvoke();
                rewardedAdFlag = false;
            });
        }
    }

    public void ShowNativeOverlayAd()
    {
        if (SettingsManager.Instance.debugHideAds)
        {
            return;
        }

        if (!SettingsManager.Instance.showAds)
        {
            return;
        }

        if (_nativeOverlayAd != null)
        {
            Debug.Log("Showing Native Overlay ad.");
            _nativeOverlayAd.Show();
        }
    }

    public void HideNativeOverlayAd()
    {
        if (_nativeOverlayAd != null)
        {
            Debug.Log("Hiding Native Overlay ad.");
            _nativeOverlayAd.Hide();
        }
    }

    public bool CanShowRewardedAd()
    {
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            return true;
        }

        return false;
    }

    public void HideBannerAd()
    {
        if (_bannerView != null)
        {
            _bannerView.Hide();
        }

        if(_bannerViewTop != null)
        {
            _bannerViewTop.Hide();
        }
    }

    private void ListenToAdEvents()
    {
        // Raised when an ad is loaded into the banner view.
        _bannerView.OnBannerAdLoaded += () =>
        {
            currentBannerNetworkSourceName = GetAdNetworkNameFromAdapterClassName(_bannerView.GetResponseInfo().GetMediationAdapterClassName());
        };

        _bannerViewTop.OnBannerAdLoaded += () =>
        {
            currentTopBannerNetworkSourceName = GetAdNetworkNameFromAdapterClassName(_bannerViewTop.GetResponseInfo().GetMediationAdapterClassName());
        };
        // Raised when an ad fails to load into the banner view.
        _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
        };
        // Raised when the ad is estimated to have earned money.
        _bannerView.OnAdPaid += (AdValue adValue) =>
        {
            string json = JsonConvert.SerializeObject(InstallDataManager.Instance.installDataMongo);
            CGAdRevenueMongo adRevenueData = JsonConvert.DeserializeObject<CGAdRevenueMongo>(json);
            adRevenueData.adEventData = new CGAdRevenueData();
            adRevenueData.adEventData.ad_revenue = adValue.Value / 1000000f;
            adRevenueData.adEventData.ad_type = "Banner";
            adRevenueData.adEventData.ad_platform = currentBannerNetworkSourceName;
            adRevenueData.adEventData.ad_currency = adValue.CurrencyCode;

            var adMonEventParams = CrimsonEventsLogger.GetAdRevDoc(adRevenueData);
            MongoDBAPIManager.Instance.LogAdRevenue(adMonEventParams);

            FacebookSDKWrapper.Instance.SendRevenue(adRevenueData.adEventData.ad_revenue);
            TenjinSDKWrapper.Instance.SendAdRevenue(adRevenueData, "Banner_Bottom", _bannerView.GetResponseInfo().GetResponseId(), "ca-app-pub-5054423413611183/7479814708");
            CrimsonEventsLogger.LogAdRevEventFirebase(adRevenueData);
        };
        _bannerViewTop.OnAdPaid += (AdValue adValue) =>
        {
            string json = JsonConvert.SerializeObject(InstallDataManager.Instance.installDataMongo);
            CGAdRevenueMongo adRevenueData = JsonConvert.DeserializeObject<CGAdRevenueMongo>(json);
            adRevenueData.adEventData = new CGAdRevenueData();
            adRevenueData.adEventData.ad_revenue = adValue.Value / 1000000f;
            adRevenueData.adEventData.ad_type = "Banner_Top";
            adRevenueData.adEventData.ad_platform = currentTopBannerNetworkSourceName;
            adRevenueData.adEventData.ad_currency = adValue.CurrencyCode;

            var adMonEventParams = CrimsonEventsLogger.GetAdRevDoc(adRevenueData);
            MongoDBAPIManager.Instance.LogAdRevenue(adMonEventParams);

            FacebookSDKWrapper.Instance.SendRevenue(adRevenueData.adEventData.ad_revenue);
            TenjinSDKWrapper.Instance.SendAdRevenue(adRevenueData, "Banner_Top", _bannerViewTop.GetResponseInfo().GetResponseId(), "ca-app-pub-5054423413611183/6563914747");
            CrimsonEventsLogger.LogAdRevEventFirebase(adRevenueData);
        };
        // Raised when an impression is recorded for an ad.
        _bannerView.OnAdImpressionRecorded += () =>
        {
        };
        // Raised when a click is recorded for an ad.
        _bannerView.OnAdClicked += () =>
        {
        };
        // Raised when an ad opened full screen content.
        _bannerView.OnAdFullScreenContentOpened += () =>
        {
        };
        // Raised when the ad closed full screen content.
        _bannerView.OnAdFullScreenContentClosed += () =>
        {
        };
    }

    private void RegisterEventHandlers(InterstitialAd interstitialAd)
    {
        // Raised when the ad is estimated to have earned money.
        interstitialAd.OnAdPaid += (AdValue adValue) =>
        {
            string json = JsonConvert.SerializeObject(InstallDataManager.Instance.installDataMongo);
            CGAdRevenueMongo adRevenueData = JsonConvert.DeserializeObject<CGAdRevenueMongo>(json);
            adRevenueData.adEventData = new CGAdRevenueData();
            adRevenueData.adEventData.ad_revenue = adValue.Value / 1000000f;
            adRevenueData.adEventData.ad_type = "Interstitial";
            adRevenueData.adEventData.ad_platform = currentInterstitialNetworkSourceName;
            adRevenueData.adEventData.ad_currency = adValue.CurrencyCode;

            var adMonEventParams = CrimsonEventsLogger.GetAdRevDoc(adRevenueData);
            MongoDBAPIManager.Instance.LogAdRevenue(adMonEventParams);

            FacebookSDKWrapper.Instance.SendRevenue(adRevenueData.adEventData.ad_revenue);
            TenjinSDKWrapper.Instance.SendAdRevenue(adRevenueData, "Interstitial", interstitialAd.GetResponseInfo().GetResponseId(), "ca-app-pub-5054423413611183/6166733031");
            CrimsonEventsLogger.LogAdRevEventFirebase(adRevenueData);
        };
        // Raised when an impression is recorded for an ad.
        interstitialAd.OnAdImpressionRecorded += () =>
        {
        };
        // Raised when a click is recorded for an ad.
        interstitialAd.OnAdClicked += () =>
        {
        };
        // Raised when an ad opened full screen content.
        interstitialAd.OnAdFullScreenContentOpened += () =>
        {
        };
        // Raised when the ad closed full screen content.
        interstitialAd.OnAdFullScreenContentClosed += () =>
        {
            LoadInterstitialAd(_interstitialUnitId);
        };
        // Raised when the ad failed to open full screen content.
        interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
        };
    }

    private void RegisterEventHandlers(RewardedAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            string json = JsonConvert.SerializeObject(InstallDataManager.Instance.installDataMongo);
            CGAdRevenueMongo adRevenueData = JsonConvert.DeserializeObject<CGAdRevenueMongo>(json);
            adRevenueData.adEventData = new CGAdRevenueData();
            adRevenueData.adEventData.ad_revenue = adValue.Value / 1000000f;
            adRevenueData.adEventData.ad_type = "Rewarded";
            adRevenueData.adEventData.ad_platform = currentRewardedNetworkSourceName;
            adRevenueData.adEventData.ad_currency = adValue.CurrencyCode;

            var adMonEventParams = CrimsonEventsLogger.GetAdRevDoc(adRevenueData);
            MongoDBAPIManager.Instance.LogAdRevenue(adMonEventParams);

            FacebookSDKWrapper.Instance.SendRevenue(adRevenueData.adEventData.ad_revenue);
            TenjinSDKWrapper.Instance.SendAdRevenue(adRevenueData, "Rewarded", ad.GetResponseInfo().GetResponseId(), "ca-app-pub-5054423413611183/8022740188");
            CrimsonEventsLogger.LogAdRevEventFirebase(adRevenueData);
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            LoadRewardedAd(_rewardedUnitId);
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            LoadRewardedAd(_rewardedUnitId);
        };
    }

    public void RegisterEventHandlers(NativeOverlayAd nativeAd)
    {
        // Raised when the ad is estimated to have earned money.
        nativeAd.OnAdPaid += (AdValue adValue) =>
        {
            //Debug.Log(String.Format("Native Overlay ad paid {0} {1}.",
            //    adValue.Value,
            //    adValue.CurrencyCode));

            //CrimsonSingularSDKWrapper.Instance.SendAdRevenue(currentRewardedNetworkSourceName, adValue.Value, adValue.CurrencyCode, "Native");
            //AdsGenus adsGenus = new AdsGenus();
            //adsGenus.adSource = currentRewardedNetworkSourceName;
            //adsGenus.revenue = adValue.Value;
            //adsGenus.currency = adValue.CurrencyCode;
        };
        // Raised when an impression is recorded for an ad.
        nativeAd.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Native Overlay ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        nativeAd.OnAdClicked += () =>
        {
            Debug.Log("Native Overlay ad was clicked.");
        };
        // Raised when the ad opened full screen content.
        nativeAd.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Native Overlay ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        nativeAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Native Overlay ad full screen content closed.");
        };
    }

    public void DestroyBannerAd()
    {
        if (_bannerView != null)
        {
            //Debug.Log("Destroying banner ad.");
            _bannerView.Destroy();
            _bannerView = null;
        }
    }

    public void DestroyBannerAdTop()
    {
        if (_bannerViewTop != null)
        {
            //Debug.Log("Destroying banner ad.");
            _bannerViewTop.Destroy();
            _bannerViewTop = null;
        }
    }

    public void DestroyIntersitialAd()
    {
        if (_interstitialAd != null)
        {
            //Debug.Log("Destroying interstitial ad");
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }
    }

    public void DestroyRewardedAd()
    {
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }
    }

    private void OnAppStateChanged(AppState state)
    {
        Debug.Log("App State changed to :: " + state);

        if (state == AppState.Foreground)
        {
            ShowInterstitialAd();
        }
    }

    public string GetAdNetworkNameFromAdapterClassName(string adapterClassName)
    {
        if (adapterClassName == null)
        {
            return "AdMob Network";
        }

        switch (true)
        {
            case bool _ when adapterClassName.Contains("Unity"):
                return "Unity Ads";
            case bool _ when adapterClassName.Contains("AppLovin"):
                return "AppLovin";
            case bool _ when adapterClassName.Contains("Liftoff") || adapterClassName.Contains("Vungle"):
                return "Liftoff";
            case bool _ when adapterClassName.Contains("Chartboost"):
                return "Chartboost";
            case bool _ when adapterClassName.Contains("InMobi"):
                return "InMobi";
            case bool _ when adapterClassName.Contains("Facebook") || adapterClassName.Contains("Meta"):
                return "Meta";
            case bool _ when adapterClassName.Contains("Mintegral"):
                return "Mintegral";
            case bool _ when adapterClassName.Contains("Pangle"):
                return "Pangle";
            default:
                return "AdMob Network";
        }
    }


    IEnumerator WaitForConfig(Action onSuccess)
    {
        yield return new WaitUntil(() => SettingsManager.Instance.IsConfigDownloaded);
        onSuccess.SafeInvoke();
    }

    void IUnityAdsInitializationListener.OnInitializationComplete()
    {
        Debug.Log("Unity Ads initialization complete.");
    }

    void IUnityAdsInitializationListener.OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
    }


}
