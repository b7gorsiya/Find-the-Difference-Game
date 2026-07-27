using CrimsonLibrary.SupportLibrary.Utils.InternetReachabilityVerifier;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class CatalogDownloadManager : MonoBehaviour
{
    private const int MaxRetries = 3;
    private const int RetryDelay = 2; // seconds

    public static Action CatalogDownloaded;

    private string offlineCatalogName = "OfflineData.json";
    private string remoteCatalogName = "FTD_Catalog";

    private async UniTask WaitForInitialFileCopy(Func<bool> condition, int checkIntervalMs = 100)
    {
        while (!condition())
        {
            await UniTask.Delay(checkIntervalMs);
        }
    }
    public async UniTask DownloadAndProcessCatalog()
    {
        LoadingJsonData.Instance.UpdateProgressText(10f); // Update progress bar

        await WaitForInitialFileCopy(() => PlayerPrefs.HasKey("startlevels"));

        try
        {
            //uiManager.ResetProgressTimer();
            float startTime = Time.time;

            // Download JSON asynchronously
            string json = await DownloadJsonWithRetriesAsync();
            if (json == null)
            {
                Debug.LogError("Failed to download JSON after retries.");
                return;
            }
            // Parse JSON in a background thread
            GameManager.Instance.catalogData = await ParseJsonInBackground(json);
            // Update progress and create UI
            await UniTask.SwitchToMainThread();
           CatalogDownloaded?.Invoke();
            float elapsedTime = Time.time - startTime;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in DownloadAndProcessCatalog: {ex.Message}");
        }
    }
    private async UniTask<CatalogData> ParseJsonInBackground(string json)
    {
        return await UniTask.RunOnThreadPool(() =>
        {
            try
            {
                var catalogData = JsonConvert.DeserializeObject<CatalogData>(json);

                return (catalogData != null) ? catalogData : new();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing JSON: {ex.Message}");
                return new();
            }
        });
    }
    private async UniTask<string> DownloadJsonWithRetriesAsync()
    {
        int retries = 0;
        while (retries < MaxRetries)
        {
            try
            {
                return await DownloadJsonAsyncWithProgress();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Retry {retries + 1}/{MaxRetries} failed: {ex.Message}");
                retries++;
                if (retries < MaxRetries)
                    await UniTask.Delay(TimeSpan.FromSeconds(RetryDelay));
            }
        }
        return null;
    }
    private async UniTask<string> DownloadJsonAsyncWithProgress()
    {
        //string filePath = jsonUrl;// Path.Combine(Application.persistentDataPath, "Settings/" + offlineCatalogName);// jsonUrl;
        string filePath = SettingsManager.Instance.InternalSettingsData.catalogURL;

        //in case not internet
        if (InternetReachabilityVerifier.Instance.status != InternetReachabilityVerifier.Status.NetVerified)
        {
            string offlineCatalog_Path = Path.Combine(Application.persistentDataPath, "Settings/" + offlineCatalogName);
            string remoteCatalog_Path = Path.Combine(Application.persistentDataPath, "Settings/" + remoteCatalogName+".json");

            if (File.Exists(remoteCatalog_Path))
            {
                filePath = remoteCatalog_Path;
            }
            else if (File.Exists(offlineCatalog_Path))
            {
                filePath = offlineCatalog_Path;
            }
        }
        // Ensure the file path is properly formatted for Android
        if (filePath.StartsWith(Application.persistentDataPath))
        {
            filePath = "file://" + filePath;
        }

        UnityWebRequest request = UnityWebRequest.Get(filePath);
        var asyncOp = request.SendWebRequest();

        while (!asyncOp.isDone)
        {
            await UniTask.SwitchToMainThread();  // Ensure UI updates on the main thread
            LoadingJsonData.Instance.UpdateProgressText(asyncOp.progress * 40f);
            await UniTask.Yield();
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            throw new Exception($"Error downloading JSON: {request.error}");
        }

        // update catalog 
        if (filePath.Contains("http"))
        {
            DataWriter.WriteToDisk(remoteCatalogName, JsonConvert.DeserializeObject<CatalogData>(request.downloadHandler.text), eDataSaveType.JSON, "Settings");
        }
        LoadingJsonData.Instance.UpdateProgressText(50f);
        return request.downloadHandler.text;
    }
}
