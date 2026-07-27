using BunnyCDN.Net.Storage;
using CrimsonGames.Analytics;
using CrimsonLibrary.SupportLibrary.Utils.Generics;
using CrimsonLibrary.SupportLibrary.Utils.InternetReachabilityVerifier;
using Cysharp.Threading.Tasks;
using System;
using System.Buffers.Text;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using System.Buffers.Text;

public class LoadingJsonData : GenericManager<LoadingJsonData>
{
    public static Action<LevelData, Texture, Texture> dataDownloaded;

    internal Texture image1; // UI RawImage for the first texture
    internal Texture image2; // UI RawImage for the second texture
    public Slider progressBar; // UI Slider for progress bar
    public TextMeshProUGUI progressText; // UI Text to show loading percentage

    private bool isLoading = false;

    public LevelData markerData;

    public Action onGameplayStarted;
    BunnyCDNStorage storage;

    Texture2D texture;

    string chapteImagePath = "";
    private void Awake()
    {
        chapteImagePath = Path.Combine(Application.persistentDataPath, "ChapterImages");
        storage = new BunnyCDNStorage("path", "Key", "sg");
    }

    private async Task WaitForInitialFileCopy(Func<bool> condition, int checkIntervalMs = 100)
    {
        while (!condition())
        {
            await Task.Delay(checkIntervalMs);
        }
    }

    public async void StartLoading(int level)
    {
        if (isLoading) return;

        await WaitForInitialFileCopy(() => PlayerPrefs.HasKey("startlevels"));

        UpdateProgressText(0);
        isLoading = true;

        string filename = GetFileNameForCurrentLevel();
        if (filename == null)
        {
            Debug.LogError("Failed to get file name for current level, this should not happen");
            return;
        }

        //string path = Path.Combine(FTDSaveLoadService.persistentDataPath, $"Level_{level:000}.json");
        string path = Path.Combine(FTDSaveLoadService.persistentDataPath, filename);

        UIManager.Instance.loaderPanel.ActivatePanel();
        UIManager.Instance.mainMenuPanel.DeactivatePanel();
        UIManager.Instance.gameUIPanel.DeactivatePanel();
        UIManager.Instance.collectionPanel.DeactivatePanel();
        UIManager.Instance.mainUIPanel.DeactivatePanel();

        print($"Loading level {level}");

        try
        {
            await LoadJsonDataAsync(path);
        }
        catch (Exception ex)
        {
            //if (!UIManager.Instance.noInernetPanel.IsPanelActive)
            //{
            //    UIManager.Instance.commingSoonLevelText.text = "Level " + GameManager.Instance.currentLevel.ToString();
            //    UIManager.Instance.commingSoonPanel.ActivatePanel();
            //}
            Debug.LogError($"Error during loading: {ex.Message}");
        }
        finally
        {
            isLoading = false;
            await UniTask.Delay(1000);
            UIManager.Instance.loaderPanel.DeactivatePanel();
            UIManager.Instance.mainMenuPanel.DeactivatePanel();
            UIManager.Instance.gameOverPanel.DeactivatePanel();
            UIManager.Instance.gameUIPanel.ActivatePanel();
            onGameplayStarted.SafeInvoke();
            //Analytics
            CrimsonEventsLogger.LogEvent(EPlayerEvent.puzzle_viewed, 0, UIManager.Instance.mainMenuPanel.GameLoadSource);
        }
    }

    public string GetFileNameForCurrentLevel()
    {
        int level = GameManager.Instance.currentLevel;
        string filename = null;
        if (level > 0 && level <= GameManager.Instance.catalogData.levelsInfo.Count)
        {
            filename = Path.GetFileName(GameManager.Instance.catalogData.levelsInfo[level - 1].url);
            //clear old level
            if (level > 1)
            {
                string oldLevel = Path.GetFileName(GameManager.Instance.catalogData.levelsInfo[level - 2].url);
                string pathOld = Path.Combine(FTDSaveLoadService.persistentDataPath, oldLevel);
                Debug.Log("Deleting Old level");
                File.Delete(pathOld);
            }
            Debug.Log($"Filename :: {filename}");
        }
        return filename;
    }
    private async UniTask LoadJsonDataAsync(string jsonFilePath)
    {
        // Step 1: Load JSON
        Stopwatch sw = new Stopwatch();
        sw.Start();

        UpdateProgressText(20);
        string jsonData = await LoadFileAsync(jsonFilePath);
        UpdateProgressText(40);

        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogError("Failed to load JSON file.");
            return;
        }

        // Step 2: Parse JSON
        markerData = /*JsonConvert.DeserializeObject<LevelData>(jsonData);//*/ JsonUtility.FromJson<LevelData>(jsonData);
        if (markerData == null)
        {
            Debug.LogError("Failed to parse JSON data.");
            return;
        }

        await UniTask.Yield(); // Allow UI thread to update
        UpdateProgressText(60);

        if (texture != null)
        {
            Destroy(texture);
            texture = null;
        }
        // Step 3: Load Textures
        await UniTask.DelayFrame(60);
        var texture1Task =await LoadTextureFromBase64Async(markerData.image1_Base64);
        await UniTask.DelayFrame(60);
        var texture2Task =await LoadTextureFromBase64Async(markerData.image2_Base64);
        image1 = texture1Task;
        image2 = texture2Task;

        UpdateProgressText(80);

        if (GameManager.Instance.playTutorial)
        { // Ensure UI is completely initialized
            await UniTask.WaitUntil(() => UIManager.Instance.menuReady);
            UpdateProgressText(85);
            await UniTask.WaitUntil(() => UIManager.Instance.collectionReady);
            UpdateProgressText(90);
        }
        // Step 4: Notify Completion
        UpdateProgressText(100);
        dataDownloaded?.Invoke(markerData, image1, image2);
        UIManager.Instance.UpdateLevelNumber();
        GameManager.Instance.state = GameManager.GameState.GamePlay;

        sw.Stop();
        Debug.Log("Time taken for LoadJsonDataAsync: " + sw.ElapsedMilliseconds + " ms");

        CrimsonEventsLogger.LogEvent(EPlayerEvent.puzzle_loaded, (int)(sw.ElapsedMilliseconds / 1000), UIManager.Instance.mainMenuPanel.GameLoadSource);
    }
    internal void UpdateProgressText(float percentage)
    {
        if (GameManager.Instance.playTutorial && percentage / 100 < progressBar.value)
        {
            return;
        }
        progressBar.value = percentage / 100;
        progressText.text = $"Loading...";
    }
    private async UniTask<string> LoadFileAsync(string filePath)
    {
        UIManager.Instance.retryButton.onClick.RemoveAllListeners();

        UIManager.Instance.retryButton.onClick.AddListener(() =>
        {
            UIManager.Instance.noInernetPanel.DeactivatePanel();
            UIManager.Instance.mainMenuPanel.ClickLoadLevel();
        });

        if (File.Exists(filePath))
        {
            return await UniTask.RunOnThreadPool(() => File.ReadAllText(filePath));
        }
        else
        {
            if (InternetReachabilityVerifier.Instance.status != InternetReachabilityVerifier.Status.NetVerified) //Not net verified is the correct check
            {
                UIManager.Instance.noInernetPanel.ActivatePanel();
                return null;
            }
            else if (GameManager.Instance.currentLevel <= GameManager.Instance.catalogData.levelsInfo.Count)
            {
                await FTDSaveLoadService.DownloadLevel(GameManager.Instance.catalogData.levelsInfo[GameManager.Instance.currentLevel - 1]);
                return await UniTask.RunOnThreadPool(() => File.ReadAllText(filePath));
            }
            else
            {
                if (GameManager.Instance.catalogData.levelsInfo.Count <= 0)
                {
                    UIManager.Instance.loaderPanel.ActivatePanel();
                    //fire base is not initilized
                    SettingsManager.Instance.FirebaseStartupCheck();
                    SettingsManager.Instance.onFirebaseInitialized += () => { UIManager.Instance.mainMenuPanel.ClickLoadLevel(); };
                }
                else
                {
                    // for reching out to maximum levels 
                    Debug.LogError($"File not found: {filePath}");
                    UIManager.Instance.commingSoonLevelText.text = "Level " + GameManager.Instance.currentLevel.ToString();
                    UIManager.Instance.commingSoonPanel.ActivatePanel();
                }
                return null;
            }
        }
    }
    public bool HasReachedEndOfLevels()
    {
        if (GameManager.Instance.catalogData.levelsInfo.Count < GameManager.Instance.currentLevel)
        {
            UIManager.Instance.commingSoonLevelText.text = "Level " + GameManager.Instance.currentLevel.ToString();
            UIManager.Instance.commingSoonPanel.ActivatePanel();
            return true;
        }

        return false;
    }

    internal async UniTask<Texture2D> DownloadTexture(string imageFileURL)
    {
        if (string.IsNullOrEmpty(imageFileURL)) return null;

        string fileName = Path.GetFileName(imageFileURL);

        if (!Directory.Exists(chapteImagePath))
        {
            Directory.CreateDirectory(chapteImagePath);
        }
        string localPathURL = Path.Combine(chapteImagePath, fileName);

        if (File.Exists(localPathURL))
        {
            return await LoadTextureFromLocal(localPathURL);
            // Debug.Log("Get the image from Local");
            //  var imageData = await File.ReadAllBytesAsync(localPathURL); // save in local
            // return await GetTexture(imageData);
        }
        else
        {
            if (InternetReachabilityVerifier.Instance.status != InternetReachabilityVerifier.Status.NetVerified)
            {
                return null;
            }

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
    }
    public async Task<Texture2D> LoadTextureFromLocal(string localPathURL)
    {
        using (var request = UnityWebRequestTexture.GetTexture("file://" + localPathURL))
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield(); // wait without blocking

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading texture: " + request.error);
                return null;
            }

            return DownloadHandlerTexture.GetContent(request);
        }
    }
    internal async UniTask<Texture2D> LoadTextureFromBase64Async(string base64)
    {
        if (string.IsNullOrEmpty(base64)) return null;
        try
        {
            // faster and better way to decode base64
            byte[] imageData = FastBase64Decode(base64);// Convert.FromBase64String(base64);
            await UniTask.SwitchToMainThread();
            return await GetTexture(imageData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading texture: {ex.Message}");
            return null;
        }
    }
    public static byte[] FastBase64Decode(string base64)
    {
        ReadOnlySpan<byte> base64Span = Encoding.UTF8.GetBytes(base64);
       
        int decodedLength = (base64Span.Length * 3) / 4;
        byte[] decodedBytes = new byte[decodedLength];

        if (Base64.DecodeFromUtf8(base64Span, decodedBytes, out int consumed, out int written)==System.Buffers.OperationStatus.InvalidData)
        {
            throw new FormatException("Invalid Base64 string.");
        }

        Array.Resize(ref decodedBytes, written);
        return decodedBytes;
    }
    async UniTask<Texture2D> GetTexture(byte[] imageData)
    {
        try
        {
             texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            //texture.LoadImage(imageData);  // Loads the image from byte array 2X remove
            if (texture.LoadImage(imageData))
            {
                // Commented to save memroy usage
                //if (texture.format == TextureFormat.RGBA32 || texture.format == TextureFormat.RGB24)
                //{
                //    texture.Compress(true);  // Compress if supported
                //}
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
}
