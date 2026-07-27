using UnityEngine;
using System;
using Newtonsoft.Json;
using CrimsonGames.Utilities;
using System.Text.RegularExpressions;
using CrimsonLibrary.SupportLibrary.Extensions;

namespace CrimsonGames.Analytics
{
    public static class MongoDBDataStructures
    {
        public static long GetCurrentEpochTime()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long epochTime = now.ToUnixTimeSeconds();
            return epochTime;
        }
    }

    [Serializable]
    public class MongoDAUBody
    {
        public string dataSource;
        public string database;
        public string collection;
        public MongoDAUDocument document;
    }

    [Serializable]
    public class MongoDAUDocument
    {
        public string PlayerID;
        public MongoDateContainer dauDateUTC;
        public MongoDateContainer installDateUTC;
        public MongoDateContainer dauDateLocal;
        public MongoDateContainer installDateLocal;
        public string country;
        public string gameName;
        public int gameId;
        public string currentBuild;
        public string installBuild;
        public string platform;
        public string cohortUTC;
        public string cohortLocal;
        public string cohortRelHrs;
        public string network;
        public string campaign;
        public string creative;
        public bool isRengagement;
        public string payerStatus;
        public bool notifStatus;
        public string idfaStatus;
        public float os;
        public int clientInsertTS;

        public MongoDAUDocument()
        {
            dauDateUTC = new MongoDateContainer();
            installDateUTC = new MongoDateContainer();
            dauDateLocal = new MongoDateContainer();
            installDateLocal = new MongoDateContainer();
        }

        public static MongoDAUDocument GetDefault()
        {
            CGInstallDataMongo installDataMongo = InstallDataManager.Instance.installDataMongo;

            var doc = new MongoDAUDocument();
            doc.PlayerID = PlayFabPlayerManager.Instance.PlayFabId;
            DateTime utcNow = DateTime.UtcNow;
            DateTime dateOnly = utcNow.Date;
            var dauDateUTC = (long)(dateOnly - new DateTime(1970, 1, 1)).TotalSeconds;
            var dauTimeUTC = (long)(utcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            var clientDate = DateTime.Today.ToString("dd-MM-yyyy");
            doc.dauDateUTC.date = DateTimeExtensions.UnixToCustomDateFormat((int)dauDateUTC);
            doc.installDateUTC.date = DateTimeExtensions.UnixToCustomDateFormat(installDataMongo.install.utc);
            doc.dauDateLocal.date = DateTimeExtensions.UnixToLocalCustomDateFormat((int)dauDateUTC);
            doc.installDateLocal.date = DateTimeExtensions.UnixToLocalCustomDateFormat(installDataMongo.install.utc);
            doc.country = CountryDetector.Instance.countryCode;
            doc.gameName = Application.productName;
            doc.gameId = 4;

            doc.currentBuild = Application.version; //float.Parse(Application.version);
            doc.installBuild = installDataMongo.device.appVersion; //float.Parse(installDataMongo.device.appVersion);
            doc.platform = installDataMongo.device.platform;

            doc.network = installDataMongo.install.network;
            doc.campaign = installDataMongo.campaign.source.name;
            doc.creative = installDataMongo.campaign.creativeName;
            doc.isRengagement = installDataMongo.campaign.isRengagement;


            string payerStatus = "nonPayer";
            //if (IAPManager.Instance.HasPurchasedNoAds())
            //{
            //    payerStatus = "payer";
            //}
            //if (IAPManager.Instance.HasPurchasedNoAdsWithHint())
            //{
            //    payerStatus = "payer";
            //}

            doc.payerStatus = payerStatus;
            doc.notifStatus = false; // TODO: Change to actual
            doc.idfaStatus = "NA";
            string osVersion = SystemInfo.operatingSystem;
            string platform = null;
            int osVersionInt = 0;

            Match match = Regex.Match(osVersion, @"(\D+)\s+OS\s+(\d+)");

            if (match.Success)
            {
                string osName = match.Groups[1].Value; // Get the Android OS name
                int versionNumber = int.Parse(match.Groups[2].Value); // Convert OS version to an integer

                platform = osName;
                osVersionInt = versionNumber;
            }

            doc.os = osVersionInt;
            doc.clientInsertTS = (int)EventDataUtilities.GetCurrentEpochTime();
            return doc;
        }
    }

    [Serializable]
    public class MongoDateContainer
    {
        [JsonProperty("$date")]
        public string date;
    }

    [Serializable]
    public class CGInstallDataMongo
    {
        public string PlayerID;
        public App app;
        public Device device;
        public Install install;
        public Campaign campaign;
        public string rawReferralString;

        public void SetLocationData()
        {
            app.eventData.country = CountryDetector.Instance.countryInfo.country;
            app.eventData.city = CountryDetector.Instance.countryInfo.city;
            app.eventData.ip = CountryDetector.Instance.countryInfo.ip;
            app.eventData.state = CountryDetector.Instance.countryInfo.region;
            campaign.touch = new Touch();
            campaign.touch.ip = CountryDetector.Instance.countryInfo.ip;
            campaign.touch.country = CountryDetector.Instance.countryInfo.country;
            campaign.touch.state = CountryDetector.Instance.countryInfo.region;
            campaign.touch.city = CountryDetector.Instance.countryInfo.city;
            campaign.attrib = new Attrib();
            campaign.attrib.ip = CountryDetector.Instance.countryInfo.ip;
            campaign.attrib.country = CountryDetector.Instance.countryInfo.country;
            campaign.attrib.state = CountryDetector.Instance.countryInfo.region;
            campaign.attrib.city = CountryDetector.Instance.countryInfo.city;
            campaign.installEventData = new InstallEventData();
            campaign.installEventData.ip = CountryDetector.Instance.countryInfo.ip;
            campaign.installEventData.country = CountryDetector.Instance.countryInfo.country;
            campaign.installEventData.state = CountryDetector.Instance.countryInfo.region;
            campaign.installEventData.city = CountryDetector.Instance.countryInfo.city;
        }
    }

    [Serializable]
    public class App
    {
        public string name { get; set; }
        public string bundleID { get; set; }
        public object publicID { get; set; }

        [JsonProperty("event")]
        public InstallEventData eventData { get; set; }

        public int gameId;

        public static App GetDefault()
        {
            App app = new App();
            app.name = Application.productName;
            //app.bundleID = "apps.free.happy.paint.colouring.book.colour.number.god.hindu.images"; TODO: Put android bundle id
            app.eventData = new InstallEventData();
            app.eventData.name = "sngLogin";
            app.eventData.utc = (int)MongoDBDataStructures.GetCurrentEpochTime();
            app.eventData.isFirst = true;
            app.gameId = 2;

            return app;
        }
    }

    [Serializable]
    public class Device
    {
        public string osVersion { get; set; }
        public string appVersion { get; set; }
        public object idfa { get; set; }
        public object idfv { get; set; }
        public string aifa { get; set; }
        public string platform { get; set; }
        public string model { get; set; }
        public string brand { get; set; }
        public Att att { get; set; }

        public static Device GetDefault()
        {
            Device device = new Device();
            device.appVersion = Application.version;

            string osVersion = SystemInfo.operatingSystem;
            Match match = Regex.Match(osVersion, @"(\D+)\s+OS\s+(\d+)");
            if (match.Success)
            {
                string osName = match.Groups[1].Value; // Get the Android OS name
                device.platform = osName;
                device.osVersion = match.Groups[2].Value;
            }

            device.model = SystemInfo.deviceModel;
            device.att = new Att();
            device.att.latFlag = "0";
            return device;
        }
    }

    [Serializable]
    public class InstallEventData
    {
        public string name { get; set; }
        public int utc { get; set; }
        public bool isFirst { get; set; }
        public string ip { get; set; }
        public string country { get; set; }
        public string state { get; set; }
        public string city { get; set; }
    }

    [Serializable]
    public class Att
    {
        public object attAuthStatus { get; set; }
        public object attAuthName { get; set; }
        public string latFlag { get; set; }
    }

    [Serializable]
    public class Install
    {
        public object matchType { get; set; }
        public object attribWindow { get; set; }
        public object source { get; set; }
        public string partnerSite { get; set; }
        public string network { get; set; }
        public string appStoreFlag { get; set; }
        public int utc { get; set; }
        public bool isOrganic { get; set; }
        public Fraud fraud { get; set; }

        public Install()
        {
            fraud = new Fraud();
        }
    }

    [Serializable]
    public class Fraud
    {
        public object fraudStatus { get; set; }
        public object fraudReason { get; set; }
    }

    [Serializable]
    public class Campaign
    {
        public string name { get; set; }
        public Click click { get; set; }
        public string creativeID { get; set; }
        public string creativeName { get; set; }
        public Source source { get; set; }
        public bool isRengagement { get; set; }
        public bool isViewThrough { get; set; }
        public Touch touch { get; set; }
        public Attrib attrib { get; set; }

        [JsonProperty("event")]
        public InstallEventData installEventData { get; set; }
    }

    [Serializable]
    public class Source
    {
        public string _id { get; set; }
        public string name { get; set; }
    }

    [Serializable]
    public class Touch
    {
        public object ip { get; set; }
        public object country { get; set; }
        public object state { get; set; }
        public object city { get; set; }
    }

    [Serializable]
    public class Click
    {
        public string _id { get; set; }
        public int utc { get; set; }
    }

    [Serializable]
    public class Attrib
    {
        public string ip { get; set; }
        public string country { get; set; }
        public string state { get; set; }
        public string city { get; set; }

    }

    [Serializable]
    public class MMPInstallDocumentRoot
    {
        public MMPInstallDocument document;
    }

    [Serializable]
    public class MMPInstallDocument
    {
        public string _id { get; set; }
        public string PlayerID { get; set; }
        public App app { get; set; }
        public Device device { get; set; }
        public Install install { get; set; }
        public Campaign campaign { get; set; }
        public string rawReferralString { get; set; }
    }

    [Serializable]
    public class CGAdRevenueMongo : CGInstallDataMongo
    {
        public CGAdRevenueData adEventData;
        public MongoCohort cohort;
        public MongoDiffDays diffDays;
        public MongoEventDateData eventDate;
        public MongoInstallDateData installDate;

        public CGAdRevenueMongo()
        {
            cohort = new MongoCohort();
            diffDays = new MongoDiffDays();
            eventDate = new MongoEventDateData();
            installDate = new MongoInstallDateData();
        }
    }

    [Serializable]
    public class CGAdRevenueMongoNew
    {
        public string PlayerID;
        public CGAdRevenueData adEventData;
        public CGAdApp app;
        public CGAdCampaign campaign;
        public CGAdDevice device;
        public CGAdInstall install;
        public string cohortUTC;
        public int diffDaysUTC;
        public MongoDateContainer eventDateUTC;
        public MongoDateContainer installDateUTC;
        public int clientInsertTS;
        public string country;
        public double installBuild;
        public bool notifStatus;

        public CGAdRevenueMongoNew()
        {
            adEventData = new CGAdRevenueData();
            app = new CGAdApp();
            campaign = new CGAdCampaign();
            device = new CGAdDevice();
            install = new CGAdInstall();
            eventDateUTC = new MongoDateContainer();
            installDateUTC = new MongoDateContainer();
        }

        public static CGAdRevenueMongoNew Construct(CGAdRevenueMongo baseData)
        {
            CGAdRevenueMongoNew adRevenueMongoNew = new CGAdRevenueMongoNew();

            adRevenueMongoNew.PlayerID = baseData.PlayerID;
            adRevenueMongoNew.adEventData = baseData.adEventData;

            adRevenueMongoNew.app.name = baseData.app.name;
            adRevenueMongoNew.app.bundleId = baseData.app.bundleID;
            adRevenueMongoNew.app.eventUTC = baseData.app.eventData.utc;
            adRevenueMongoNew.app.gameId = baseData.app.gameId;

            adRevenueMongoNew.campaign.name = baseData.campaign.name;
            adRevenueMongoNew.campaign.creativeName = baseData.campaign.creativeName;
            adRevenueMongoNew.campaign.isRengagement = baseData.campaign.isRengagement;

            adRevenueMongoNew.cohortUTC = baseData.cohort.utc;
            adRevenueMongoNew.diffDaysUTC = baseData.diffDays.utc;
            adRevenueMongoNew.eventDateUTC = baseData.eventDate.utc;
            adRevenueMongoNew.installDateUTC = baseData.installDate.utc;
            adRevenueMongoNew.country = baseData.app.eventData.country;

            adRevenueMongoNew.device.osVersion = float.Parse(baseData.device.osVersion);
            adRevenueMongoNew.device.appVersion = float.Parse(baseData.device.appVersion);
            adRevenueMongoNew.device.platform = baseData.device.platform;
            adRevenueMongoNew.device.latFlag = "0";

            adRevenueMongoNew.install.partnerSite = baseData.install.partnerSite;
            adRevenueMongoNew.install.network = baseData.install.network;

            return adRevenueMongoNew;
        }
    }

    [Serializable]
    public class CGAdApp
    {
        public string name;
        public string bundleId;
        public int eventUTC;
        public int gameId;
    }

    [Serializable]
    public class CGAdDevice
    {
        public float osVersion;
        public double appVersion;
        public string platform;
        public string latFlag;
    }

    [Serializable]
    public class CGAdInstall
    {
        public string partnerSite;
        public string network;
    }

    [Serializable]
    public class CGAdCampaign
    {
        public string name;
        public string creativeName;
        public bool isRengagement;
    }

    [Serializable]
    public class MongoCohort
    {
        public string utc;
    }

    [Serializable]
    public class MongoDiffDays
    {
        public int utc;
    }

    [Serializable]
    public class MongoEventDateData
    {
        public MongoDateContainer utc;

        public MongoEventDateData()
        {
            utc = new MongoDateContainer();
        }
    }

    [Serializable]
    public class MongoInstallDateData
    {
        public MongoDateContainer utc;

        public MongoInstallDateData()
        {
            utc = new MongoDateContainer();
        }
    }

    [Serializable]
    public class CGAdRevenueData
    {
        public string ad_platform;
        public string ad_currency;
        public string ad_type;
        public float ad_revenue;

        public void AddRevenue(float revenueToAdd)
        {
            ad_revenue = ad_revenue + revenueToAdd;
        }
    }
}