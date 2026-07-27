using BunnyCDN.Net.Storage;
using BunnyCDN.Net.Storage.Models;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class CatalogForTool : MonoBehaviour
{
    [SerializeField] internal string stagingCatalogURL = "https://crimsongamescbnasia.b-cdn.net/FTDAssets/Catalog/FTD_catalog_staging.json";
    [SerializeField] string productionCatalogURL = "https://crimsongamescbnasia.b-cdn.net/FTDAssets/Catalog/FTD_catalog.json";

    private const int MaxRetries = 3;
    private const int RetryDelay = 2; // seconds

    public CatalogData CatalogData;

    public static CatalogForTool instance;

    public bool isTesting = false;
    public string testingURL = "https://crimsongamescbnasia.b-cdn.net/FTDAssets/TestLevels/Catalog/FTD_catalog_test.json";

    [SerializeField] string saveFilePath;
    List<StorageObject> savedLevelsList = new();
    BunnyCDNStorage storage;

    private void Awake()
    {
        instance = this;
        saveFilePath = UnityEngine.Application.temporaryCachePath + "/Levels/";
        if (!Directory.Exists(saveFilePath))
            Directory.CreateDirectory(saveFilePath);

        storage = new BunnyCDNStorage("crimsongamescbnasia", "76a0ec76-7729-41f6-91cc4a38272e-a731-4e06", "sg");
    }
    public async void StartEditor()
    {
        UIImageUploader.instance.loadingPanel.SetActive(true);
        await DownloadAndProcessCatalog();
        UIImageUploader.instance.loadingPanel.SetActive(false);
        UIImageUploader.instance.UpdateOtherUI(CatalogData);
    }
    internal async UniTask<Texture2D> DownloadTexture(string imageFileURL)
    {
        if (string.IsNullOrEmpty(imageFileURL)) return null;

        string fileName = Path.GetFileName(imageFileURL);

        string localPath = saveFilePath + $"{fileName}.png";

        if (!Directory.Exists(saveFilePath))
        {
            Directory.CreateDirectory(saveFilePath);
        }
        string localPathURL = Path.Combine(saveFilePath, fileName);

        var stream = await storage.DownloadObjectAsStreamAsync(imageFileURL);
        await UniTask.SwitchToMainThread();

        // Read the stream into a byte array
        using (MemoryStream memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            byte[] imageData = memoryStream.ToArray();
            await File.WriteAllBytesAsync(localPathURL, imageData); // save in local
            return await GetTexture(imageData);
        }
    }
    async UniTask<Texture2D> GetTexture(byte[] imageData)
    {
        try
        {
            //Texture2D texture = new Texture2D(2, 2);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(imageData);  // Loads the image from byte array

            if (texture.LoadImage(imageData))
            {
                if (texture.format == TextureFormat.RGBA32 || texture.format == TextureFormat.RGB24)
                {
                    texture.Compress(true);  // Compress if supported
                }
                // texture.Compress(true);
                texture.Apply();
                return texture;
            }
            else
            {
                Debug.LogError("Failed to load texture from Base64 string.");
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading texture: {ex.Message}");
            return null;
        }
    }
    public async UniTask DownloadAndProcessCatalog()
    {
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
            CatalogData = await ParseJsonInBackground(json);
            Debug.Log("Staging Catalog Downloaded");
            return;
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
        string filePath = (isTesting) ? testingURL : stagingCatalogURL;

        UnityWebRequest request = UnityWebRequest.Get(filePath);
        var asyncOp = request.SendWebRequest();

        while (!asyncOp.isDone)
        {
            //uiManager.UpdateProgressTimer(Time.deltaTime);
            await UniTask.Yield();
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            throw new Exception($"Error downloading JSON: {request.error}");
        }

        return request.downloadHandler.text;
    }

    internal async void UploadImage(string _localPath, string _serverpath)
    {
        await storage.UploadAsync(_localPath, _serverpath);
    }
}
