using CrimsonGames.Analytics;
using CrimsonLibrary.SupportLibrary.Utils.Generics;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TenjinAdMobData
{
    public float publisher_revenue_micro;
    public float publisher_revenue_decimal;
    public float value_micros;
    public string ad_unit_id;
    public string currency_code;
    public string response_id;
    public string precision_type;
    public string format;
    public string network_placement;
    public string network_name;
}

public class TenjinSDKWrapper : GenericManager<TenjinSDKWrapper>
{
    public string tenjinSdkKey = "key";
    private BaseTenjin tenjinInstance;
    public BaseTenjin TenjinInstance { get => tenjinInstance; }

    private void Start()
    {
        PlayFabPlayerManager.Instance.onLoginSuccess += () =>
        {
            TenjinConnect();
        };
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            TenjinConnect();
        }
    }

    public void TenjinConnect()
    {
        StartCoroutine(WaitForPlayfabAndConnect());
    }

    IEnumerator WaitForPlayfabAndConnect()
    {
        yield return new WaitUntil(() => !string.IsNullOrEmpty(PlayFabPlayerManager.Instance.PlayFabId));
        BaseTenjin instance = Tenjin.getInstance(tenjinSdkKey);
        instance.SetAppStoreType(AppStoreType.googleplay);
        instance.SetCustomerUserId(PlayFabPlayerManager.Instance.PlayFabId);
        instance.SetCacheEventSetting(true);
        instance.Connect();
        tenjinInstance = instance;
    }

    public void SendAdRevenue(CGAdRevenueMongo adRevenueData, string networkPlacement, string responseId, string adUnitId)
    {
        float adRevenue = adRevenueData.adEventData.ad_revenue;
        TenjinAdMobData data = new TenjinAdMobData
        {
            publisher_revenue_micro = adRevenue,
            publisher_revenue_decimal = adRevenue,
            value_micros = adRevenue,
            ad_unit_id = adUnitId,
            currency_code = adRevenueData.adEventData.ad_currency,
            response_id = responseId,
            precision_type = "Unknown",
            format = adRevenueData.adEventData.ad_type,
            network_placement = networkPlacement,
            network_name = adRevenueData.adEventData.ad_platform
        };

        tenjinInstance.AdMobImpressionFromJSON(JsonConvert.SerializeObject(data));
    }

    public void CompletedAndroidPurchase(string ProductId, string CurrencyCode, int Quantity, double UnitPrice, string Receipt, string Signature)
    {
        tenjinInstance.Transaction(ProductId, CurrencyCode, Quantity, UnitPrice, null, Receipt, Signature);
    }
}
