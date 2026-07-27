using CrimsonLibrary.SupportLibrary.Utils.Generics;
using System;
using System.Collections.Generic;
using UnityEngine;
using RestSharp;
using Newtonsoft.Json;
using System.Threading.Tasks;
using CrimsonLibrary.SupportLibrary.Utils.InternetReachabilityVerifier;
using System.IO;

namespace CrimsonGames.Analytics
{
    [Serializable]
    public enum WalletRequestType
    {
        GET,
        POST,
    }

    [Serializable]
    public class PlayerWalletUpdateParams
    {
        public string PlayerID;
        public string lastTransactionToken;
        public int valueToAdd;
        public string databaseName;
        public WalletRequestType walletRequestType;
    }

    [Serializable]
    public class FetchInstallDataParams
    {
        public string playerID;
        public string database;
        public string collection;
    }

    [Serializable]
    public class EventDataParams
    {
        public string database;
        public string collection;
        public string document;
    }

    [Serializable]
    public class AdmonEventParams
    {
        public string database;
        public string collection;
        public string document;
    }

    [Serializable]
    public enum FailedRequestType
    {
        Economy,
        DAU,
        AdMon,
        GameEvent,
    }

    [Serializable]
    public class FailedRequestData
    {
        public FailedRequestType failedRequestType;
        public object eventObject;

        public FailedRequestData(FailedRequestType failedRequestType, object eventObject)
        {
            this.failedRequestType = failedRequestType;
            this.eventObject = eventObject;
        }
    }

    public class MongoDBAPIManager : GenericManager<MongoDBAPIManager>
    {
        public const string WALLET_ENDPOINT = "path";
        public const string DAU_ENDPOINT = "path";
        public const string INSTALL_DATA_ENDPOINT = "path";
        public const string ADMON_ENDPOINT = "path";
        public const string GAME_EVENTS_ENDPOINT = "path";

        public Queue<FailedRequestData> failedRequests = new Queue<FailedRequestData>();

        private bool logEvents = false;

        private void Awake()
        {
            base.Awake();
            CheckForFailedEventsOnDisk();
            SettingsManager.Instance.onInternetStatusChange += OnInternetStatusChange;
            SettingsManager.Instance.onApplicationBackground += WriteEventsCacheToDisk;
            logEvents = SettingsManager.Instance.logEvents;
        }

        #region Install Data

        public async Task<CGInstallDataMongo> GetInstallData(FetchInstallDataParams installDataParams)
        {
            if(logEvents)
            {
                Debug.Log("[MongoDBDataAPIManager][GetInstallData] called");
            }
            
            var client = new RestClient(INSTALL_DATA_ENDPOINT);
            var request = new RestRequest();
            request.Timeout = 5000;

            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Access-Control-Request-Headers", "*");

            if(logEvents)
            {
                Debug.Log($"Serialized request body: {JsonConvert.SerializeObject(installDataParams)}");
            }
            
            request.AddStringBody(JsonConvert.SerializeObject(installDataParams), DataFormat.Json);

            RestResponse response = await client.PostAsync(request);

            if(logEvents)
            {
                Debug.Log($"Request sent to db : {installDataParams.database}, collection : {installDataParams.collection}, awaiting response...");
            }

            if (response.IsSuccessful)
            {
                if (logEvents)
                {
                    Debug.Log($"Response successful. Raw response content: {response.Content}");
                }
                    
                //onSuccess.SafeInvoke(response.Content);
                var installData = JsonConvert.DeserializeObject<CGInstallDataMongo>(response.Content);
                return installData;
            }

            return null;
        }

        #endregion

        #region Events and DAU
        public async Task LogDAU(
        MongoDAUBody mongoDAUBodyNew,
        Action<string> onSuccess,
        Action onError = null,
        int retryCount = 0,
        int maxRetries = 5,
        float retryDelay = 1f,
        Action onFirstRetry = null)
        {
            Debug.Log("[MongoDBDataAPIManager][LogDAU] called");
            try
            {
                if (logEvents)
                {
                    Debug.Log("Preparing RestClient and request.");
                }

                var client = new RestClient(DAU_ENDPOINT);
                var request = new RestRequest();
                request.Timeout = 5000;

                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Access-Control-Request-Headers", "*");

                if (logEvents)
                {
                    Debug.Log($"Serialized request body: {JsonConvert.SerializeObject(mongoDAUBodyNew)}");
                }

                request.AddStringBody(JsonConvert.SerializeObject(mongoDAUBodyNew), DataFormat.Json);

                RestResponse response = await client.PostAsync(request);

                if(logEvents)
                {
                    Debug.Log($"Request sent to db : {mongoDAUBodyNew.database}, collection : {mongoDAUBodyNew.collection}, awaiting response...");
                }

                if (response.IsSuccessful)
                {
                    if (logEvents)
                    {
                        Debug.Log($"Response successful. Raw response content: {response.Content}");
                    }

                    onSuccess.SafeInvoke(response.Content);
                    CheckCacheQueue();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception occurred: {ex.Message}");
                Debug.Log($"Stack Trace: {ex.StackTrace}");

                // Retry logic
                if (retryCount < maxRetries)
                {
                    if (retryCount == 0)
                    {
                        Debug.Log("First retry initiated.");
                        onFirstRetry?.Invoke();  // Null-check for onFirstRetry
                        Debug.Log("onFirstRetry invoked.");
                    }

                    Debug.LogWarning($"Retrying... Attempt {retryCount + 1}/{maxRetries}");
                    await Task.Delay(TimeSpan.FromSeconds(retryDelay));  // Adding delay before retrying
                    await LogDAU(mongoDAUBodyNew, onSuccess, onError, retryCount + 1, maxRetries, retryDelay, onFirstRetry);
                }
                else
                {
                    Debug.LogError("Max retries reached. Failed to log DAU");
                    onError?.Invoke(); // Null-check for onError
                    FailedRequestData failedRequestData = new FailedRequestData(FailedRequestType.DAU, mongoDAUBodyNew);
                    failedRequests.Enqueue(failedRequestData);
                    Debug.Log("onError invoked.");
                }
            }

            Debug.Log("[MongoDBDataAPIManager][LogDAU] finished");
        }

        public async Task LogAdRevenue(AdmonEventParams admonEventParams, Action onSuccess = null)
        {
            if (logEvents)
                Debug.Log("[MongoDBDataAPIManager][LogAdRevenue] called");
            try
            {
                if (logEvents)
                    Debug.Log("Preparing RestClient and request.");
                var client = new RestClient(ADMON_ENDPOINT);
                var request = new RestRequest();
                request.Timeout = 5000;

                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Access-Control-Request-Headers", "*");

                if (logEvents)
                    Debug.Log($"Serialized request body: {JsonConvert.SerializeObject(admonEventParams)}");
                request.AddStringBody(JsonConvert.SerializeObject(admonEventParams), DataFormat.Json);

                RestResponse response = await client.PostAsync(request);
                Debug.Log($"Request sent to db : {admonEventParams.database}, collection : {admonEventParams.collection}, awaiting response...");

                if (response.IsSuccessful)
                {
                    if (logEvents)
                        Debug.Log($"Response successful. Raw response content: {response.Content}");
                    onSuccess.SafeInvoke();
                    CheckCacheQueue();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception occurred: {ex.Message}");
                Debug.Log($"Stack Trace: {ex.StackTrace}");
                FailedRequestData failedRequestData = new FailedRequestData(FailedRequestType.AdMon, admonEventParams);
                failedRequests.Enqueue(failedRequestData);
            }
        }

        public async Task LogGameEvent(EventDataParams eventDataParams, Action onSuccess = null)
        {
            if (logEvents)
                Debug.Log("[MongoDBDataAPIManager][LogGameEvent] called");
            try
            {
                if (logEvents)
                    Debug.Log("Preparing RestClient and request.");
                var client = new RestClient(GAME_EVENTS_ENDPOINT);
                var request = new RestRequest();
                request.Timeout = 5000;

                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Access-Control-Request-Headers", "*");

                if (logEvents)
                    Debug.Log($"Serialized request body: {JsonConvert.SerializeObject(eventDataParams)}");
                request.AddStringBody(JsonConvert.SerializeObject(eventDataParams), DataFormat.Json);

                RestResponse response = await client.PostAsync(request);
                if(logEvents)
                    Debug.Log($"Request sent to db : {eventDataParams.database}, collection : {eventDataParams.collection}, awaiting response...");

                if (response.IsSuccessful)
                {
                    if (logEvents)
                        Debug.Log($"Response successful. Raw response content: {response.Content}");
                    onSuccess.SafeInvoke();
                    CheckCacheQueue();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception occurred: {ex.Message}");
                Debug.Log($"Stack Trace: {ex.StackTrace}");

                FailedRequestData failedRequestData = new FailedRequestData(FailedRequestType.GameEvent, eventDataParams);
                failedRequests.Enqueue(failedRequestData);
            }
        }
        #endregion

        #region Data Caching
        private void OnInternetStatusChange(InternetReachabilityVerifier.Status status)
        {
            if (status == InternetReachabilityVerifier.Status.NetVerified)
            {
                CheckCacheQueue();
            }
        }

        private void CheckCacheQueue()
        {
            while (failedRequests.Count > 0)
            {
                FailedRequestData failedRequestData = failedRequests.Dequeue();
                HandleFailedRequest(failedRequestData);
            }
        }

        private void HandleFailedRequest(FailedRequestData failedRequestData)
        {
            switch (failedRequestData.failedRequestType)
            {
                case FailedRequestType.Economy:
                    break;
                case FailedRequestType.DAU:
                    HandleFailedDAURequest(failedRequestData);
                    break;
                case FailedRequestType.AdMon:
                    HandleFailedAdmonRequest(failedRequestData);
                    break;
                case FailedRequestType.GameEvent:
                    HandleFailedGameEvent(failedRequestData);
                    break;
                default:
                    break;
            }
        }

        private async void HandleFailedDAURequest(FailedRequestData failedRequestData)
        {
            MongoDAUBody mongoDAUBodyNew = null;
            try
            {
                mongoDAUBodyNew = (MongoDAUBody)failedRequestData.eventObject;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to cast eventObject to MongoDAUBodyNew: {ex.Message}");
                return; // Exit the method as the cast failed
            }

            await LogDAU(mongoDAUBodyNew, (s) => CrimsonEventsLogger.SaveDAUInPrefs());
        }

        private async void HandleFailedAdmonRequest(FailedRequestData failedRequestData)
        {
            AdmonEventParams admonEventParams = null;
            try
            {
                admonEventParams = (AdmonEventParams)failedRequestData.eventObject;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to cast eventObject to AdmonEventParams: {ex.Message}");
                return; // Exit the method as the cast failed
            }

            await LogAdRevenue(admonEventParams);
        }

        private async void HandleFailedGameEvent(FailedRequestData failedRequestData)
        {
            EventDataParams eventDataParams = null;
            try
            {
                eventDataParams = (EventDataParams)failedRequestData.eventObject;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to cast eventObject to EventDataParams: {ex.Message}");
                return; // Exit the method as the cast failed
            }

            await LogGameEvent(eventDataParams);
        }

        private void WriteEventsCacheToDisk()
        {
            if (failedRequests.Count > 0)
            {
                DataWriter.WriteToDisk("failedevents", failedRequests, eDataSaveType.JSON, "FailedEvents", _formatting: Formatting.None);

                failedRequests.Clear();
            }
        }

        private void CheckForFailedEventsOnDisk()
        {
            string fullFilePath = Path.Combine(Application.persistentDataPath, "FailedEvents", "failedevents.json");
            if (!File.Exists(fullFilePath))
            {
                return;
            }

            var diskQueue = DataReader.ReadFromDisk<Queue<FailedRequestData>>("failedevents", eDataSaveType.JSON, "FailedEvents");
            while (diskQueue.Count > 0)
            {
                var failedReq = diskQueue.Dequeue();
                failedRequests.Enqueue(failedReq);
            }

            File.Delete(fullFilePath);
        }

        #endregion
    }

}
