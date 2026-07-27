using CrimsonGames.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CrimsonGames.Analytics
{
    [Serializable]
    public class EventData
    {
        [JsonProperty(Order = -1)]
        public string eventName;
        [JsonProperty(Order = 1)]
        public string playerId;
        [JsonProperty(Order = 2)]
        public Kingdom kingdom;
        [JsonProperty(Order = 3)]
        public string phylum;
        [JsonProperty(Order = 4)]
        public EventClass eventClass;
        [JsonProperty(Order = 5)]
        public Order order;
        [JsonProperty(Order = 6)]
        public object family;
        [JsonProperty(Order = 7)]
        public object genus;
        [JsonProperty(Order = 8)]
        public string species;
        [JsonProperty(Order = 9)]
        public string actionTimeStamp;
        [JsonProperty(Order = 10)]
        public string logTimeStamp;
        [JsonProperty(Order = 11)]
        public string actionTimeStampUTC;
        [JsonProperty(Order = 12)]
        public string logTimeStampUTC;
        [JsonProperty(Order = 13)]
        public EventDataInstallDataObject installData;
        [JsonProperty(Order = 14)]
        public EventDataCohort cohort;
        [JsonProperty(Order = 15)]
        public EventDataDateObject dauDate;
        [JsonProperty(Order = 16)]
        public EventDataInstallDateObject installDate;
        [JsonProperty(Order = 17)]
        public int clientInsertTS;

        public EventData()
        {
            kingdom = new Kingdom();
            eventClass = new EventClass();
            order = new Order();
            cohort = new EventDataCohort();
            dauDate = new EventDataDateObject();
            installDate = new EventDataInstallDateObject();
            installData = new EventDataInstallDataObject();
        }
    }

    [Serializable]
    public class EventDataCohort
    {
        public string utc;
        public string local;
        public string relHrs;
    }

    [Serializable]
    public class EventDataDateObject
    {
        public MongoDateContainer utc;
        public MongoDateContainer local;
        public int unixTS;

        public EventDataDateObject()
        {
            utc = new MongoDateContainer();
            local = new MongoDateContainer();
        }
    }

    [Serializable]
    public class EventDataInstallDateObject
    {
        public MongoDateContainer utc;
        public MongoDateContainer local;

        public EventDataInstallDateObject()
        {
            utc = new MongoDateContainer();
            local = new MongoDateContainer();
        }
    }

    [Serializable]
    public class EventDataInstallDataObject
    {
        public string network;
        public string campaign;
        public string country;
    }


    //[Serializable]
    //public class FirebasePuzzleEventData
    //{
    //    [JsonProperty(Order = 0)]
    //    public string image_id;
    //    [JsonProperty(Order = 1)]
    //    public int puzzle_num;
    //    [JsonProperty(Order = 2)]
    //    public int hints_used;
    //    [JsonProperty(Order = 3)]
    //    public int time_elapsed_sec;
    //    [JsonProperty(Order = 4)]
    //    public int differences_remaining;
    //    [JsonProperty(Order = 5)]
    //    public int lives_remaining;
    //}

    //[Serializable]
    //public class FirebaseMetaEventData
    //{
    //    [JsonProperty(Order = 0)]
    //    public string image_id;
    //    [JsonProperty(Order = 1)]
    //    public int puzzle_num;
    //    [JsonProperty(Order = 2)]
    //    public string source;
    //}

    //[Serializable]
    //public class FirebaseEventData
    //{
    //    public string phylum;
    //    public string image_id;
    //    public int puzzle_num;
    //    public string puzzle_mode;
    //    public int hints_used;
    //    public int time_elapsed_sec;
    //    public int differences_remaining;
    //    public int lives_remaining;
    //    public object family;
    //    public object genus;
    //    public string species;
    //    public string cohortUTC;
    //    public string cohortLocal;
    //    public int diffDaysUTC;
    //    public int diffDaysLocal;

    //    public FirebaseEventData(EventData eventData, int diffDaysUTC, int diffDaysLocal)
    //    {
    //        if (eventData == null)
    //            throw new ArgumentNullException(nameof(eventData));

    //        this.phylum = eventData.phylum;
    //        this.image_id = eventData.eventClass?.image_id ?? string.Empty;
    //        this.puzzle_num = eventData.eventClass?.puzzle_num ?? 0;
    //        this.puzzle_mode = eventData.order?.puzzle_mode ?? string.Empty;
    //        this.hints_used = eventData.order?.hints_used ?? 0;
    //        this.time_elapsed_sec = eventData.order?.time_elapsed_sec ?? 0;
    //        this.differences_remaining = eventData.order?.differences_remaining ?? 0;
    //        this.lives_remaining = eventData.order?.lives_remaining ?? 0;
    //        this.family = eventData.family ?? new object();
    //        this.genus = eventData.genus ?? new object();
    //        this.species = eventData.species ?? string.Empty;
    //        this.cohortUTC = eventData.cohort?.utc ?? string.Empty;
    //        this.cohortLocal = eventData.cohort?.local ?? string.Empty;
    //        this.diffDaysUTC = diffDaysUTC;
    //        this.diffDaysLocal = diffDaysLocal;
    //    }
    //}

    [Serializable]
    public class EventDataOtherOrderInt : EventData
    {
        [JsonProperty(Order = 5)]
        public new OrderPaddedInt order;
    }

    [Serializable]
    public class OrderPaddedInt
    {
        public int wallet_balance;
    }

    [Serializable]
    public class EventDataWalletOrder : EventData
    {
        [JsonProperty(Order = 5)]
        public new WalletOrder order;
    }

    [Serializable]
    public class WalletOrder
    {
        public int coins_credited;
        public int coins_debited;
        public int wallet_balance;
        public string currency;
    }

    [Serializable]
    public class EventDataOther : EventData
    {
        [JsonProperty(Order = 4)]
        public new string eventClass;
    }

    [Serializable]
    public class EventDataOtherOrder : EventData
    {
        [JsonProperty(Order = 5)]
        public new string order;
    }

    [Serializable]
    public class EventDataOtherOrderAndEvent : EventData
    {
        [JsonProperty(Order = 4)]
        public new string eventClass;
        [JsonProperty(Order = 5)]
        public new string order;
    }

    [Serializable]
    public class EventDataStories : EventData
    {
        [JsonProperty(Order = 4)]
        public new EventDataStoriesContainer eventClass;
        [JsonProperty(Order = 5)]
        public new EventDataStoriesOrderContainer order;
    }

    [Serializable]
    public class EventDataStoriesContainer
    {
        public string image_id;
        public string story_id;
    }

    [Serializable]
    public class EventDataStoriesOrderContainer
    {
        public int total_story_images;
        public int completed_story_images;
    }

    [Serializable]
    public class Kingdom
    {
        public string country_id;
        public string device_id;
        public string platform;
        public int os_version_no;
        public double build_no;
        public string gameName;

        public Kingdom()
        {
            gameName = Application.productName;
        }
    }

    [Serializable]
    public class EventClass
    {
        public string image_id;
        public int puzzle_num;
    }

    [Serializable]
    public class Order
    {
        public string puzzle_mode; //regular/daily
        public int hints_used;
        public int time_elapsed_sec;
        public int differences_remaining;
        public int lives_remaining;

        public Order()
        {
            puzzle_mode = "Regular";
            hints_used = 0;
            time_elapsed_sec = 0;
            differences_remaining = 0;
            lives_remaining = 0;
        }
    }

    [Serializable]
    public class AdsGenus
    {
        public long revenue;
        public string currency;
        public string adSource;
    }

    [Serializable]
    public class AdsGenusFailed
    {
        public string error;
        public string adSource;
    }

    [Serializable]
    public class EventInstallData
    {
        public long installDateUnix;
        public string partnerSite;
        public string network;
        public Source source;
        public EventCampaignData campaign;
        public string appVersion;
        public string country;
        public string deviceBrand;
        public string deviceModel;

        public EventInstallData()
        {
            source = new Source();
            campaign = new EventCampaignData();
        }
    }

    [Serializable]
    public class EventCampaignData
    {
        public string name;
        public string creativeName;
        public bool isRengagement;
    }

    [Serializable]
    public class MongoEventBody
    {
        public string dataSource;
        public string database;
        public string collection;
        public EventData document;
    }

    public static class EventDataUtilities
    {
        public static long GetCurrentEpochTime()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long epochTime = now.ToUnixTimeSeconds();
            return epochTime;
        }
    }
}