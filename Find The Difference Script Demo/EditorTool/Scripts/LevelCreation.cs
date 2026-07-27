using BunnyCDN.Net.Storage;
using BunnyCDN.Net.Storage.Models;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;


public class LevelCreation : MonoBehaviour
{
    public static LevelCreation instance;

    string storageName= "/crimsongamescbnasia";
    string storageURL= "https://crimsongamescbnasia.b-cdn.net";

    [SerializeField] int level;
    [SerializeField] string saveFilePath;
    [SerializeField] UIImageUploader imageUploader;
    BunnyCDNStorage storage;

    string serverStorageNameStaging = "/crimsongamescbnasia/FTDAssets";
    string serverStorageNameTest = "/crimsongamescbnasia/FTDAssets/TestLevels";
    string serverStorageName
    {
        get
        {
            if (CatalogForTool.instance.isTesting)
            {
                return serverStorageNameTest; 
            }
            else
            {
                return serverStorageNameStaging;
            }
        }
    }

    string stagingCatalogStaging = "/crimsongamescbnasia/FTDAssets/Catalog/FTD_catalog_staging.json";
    string stagingCatalogTest = "/crimsongamescbnasia/FTDAssets/TestLevels/Catalog/FTD_catalog_test.json";
    string stagingCatalog
    {
        get
        {
            if (CatalogForTool.instance.isTesting)
            {
                return stagingCatalogTest;
            }
            else
            {
                return stagingCatalogStaging;
            }
        }
    }

    List<StorageObject> savedLevelsList = new();
    public LevelInfo currentLevelIfo;
    private async void Awake()
    {
        instance = this;
        saveFilePath = UnityEngine.Application.temporaryCachePath + "/Levels/";
        storage = new BunnyCDNStorage("crimsongamescbnasia", "76a0ec76-7729-41f6-91cc4a38272e-a731-4e06", "sg");
        ServerStatus();
    }
    private async UniTask WaitForCatalogInstance()
    {
        while (CatalogForTool.instance == null)
        {
            await UniTask.Yield(); // or Task.Delay(10) for a slight delay
        }
    }
    public async void ServerStatus()
    {
        await WaitForCatalogInstance();

        savedLevelsList = new();
        var objList = await storage.GetStorageObjectsAsync(serverStorageName);
        foreach (var item in objList)
        {
            if (!item.IsDirectory)
            {
                savedLevelsList.Add(item);
            }
        }
        imageUploader.serverPath.text = "Storage Path :" + savedLevelsList[0].Path;
        imageUploader.totalLevelText.text = "Total Level :" + (savedLevelsList.Count).ToString();
    }
    internal async void LoadLevel(string folderLocation, string levelNo)
    {
        level = int.Parse(levelNo);
        string levelPath = savedLevelsList[level - 1].FullPath;
        string localPath = saveFilePath + $"Level_{level:000}.json";
        //  string jsonFilePath = Path.Combine(folderLocation, $"Level_{level:000}.json");
        await storage.DownloadObjectAsync(levelPath, localPath);
        if (!File.Exists(localPath))
        {
            Debug.LogError("JSON file not found: " + localPath);
            return;
        }
        Debug.Log("downloaded level from :" + levelPath);


        // Read and parse JSON
        string jsonData = File.ReadAllText(localPath);
        Debug.Log("level from :" + jsonData);

        LevelData markerData = JsonConvert.DeserializeObject<LevelData>(jsonData);

        imageUploader.LoadUI(markerData);
        imageUploader.UpdateStatus("Level Loaded For View Only");

    }
    public async void SaveMarkersToJSON(List<MarkedDifferenceData> markedPoints, bool _forceOverWrite = false)
    {
        if (CheckForNullData(markedPoints))
        {
            return;
        }
        int episodeIndex = int.Parse(UIImageUploader.instance.levelEpisodeIndex.text);
        int chapterIndex = UIImageUploader.instance.cityDropDown.value + 1;

        string levelURL = Path.Combine(serverStorageName, $"Level_{level:000}.json");
       
        int levelIfoIndex = -1;
        int duplicateEpisodeIndex = -1;

        CatalogData cat_data = CatalogForTool.instance.CatalogData;

        if (CheckForOverWrite(episodeIndex, chapterIndex, ref levelIfoIndex, ref duplicateEpisodeIndex) && !_forceOverWrite)
        {
            if (duplicateEpisodeIndex > -1)
            {
                UIImageUploader.instance.episodeOverWritePanel.SetActive(true);
                UIImageUploader.instance.episodeOverWritePanel.GetComponentInChildren<UnityEngine.UI.Button>().onClick.RemoveAllListeners();
                UIImageUploader.instance.episodeOverWritePanel.GetComponentInChildren<UnityEngine.UI.Button>().
                    onClick.AddListener(() =>
                    {
                        if (levelIfoIndex > -1)
                        {
                            UIImageUploader.instance.overWritePanel.SetActive(true);
                        }
                        else
                        {
                            //remove episode for conflict
                            imageUploader.SaveLevel(true);
                        }

                    });
                return;
            }
            else
            {
                UIImageUploader.instance.overWritePanel.SetActive(true);
            }
            return;
        }
        imageUploader.UploadingPanelStat(true);

        if (!UIImageUploader.instance.isOffline)
        {
            await SaveLevelFile(markedPoints, levelURL);
        }

        imageUploader.UploadingPanelStat(false);
        //Catalog Update and Upload
        currentLevelIfo = new LevelInfo
        {
            levelNo = level,
            episodeId = episodeIndex,
            chapterId = chapterIndex,
            country = (LevelInfoCountry)UIImageUploader.instance.countryDropDown.value,
            imageId = UIImageUploader.instance.levelImageID.text,
            url = levelURL.Replace(storageName, storageURL),
        };
        Debug.Log("Level URL :" + currentLevelIfo.url);

        //Add Level ifo at proper index for OverWrite as well
        if (levelIfoIndex > -1)
        {
            if (cat_data.levelsInfo[levelIfoIndex].episodeId != episodeIndex)
            {
                UIImageUploader.instance.uploadingPanel.SetActive(false);
                Debug.Log("Can not Upload here");
                UIImageUploader.instance.ShowError("Level Episode does not match");
                return;
            }
            cat_data.levelsInfo[levelIfoIndex] = currentLevelIfo;
        }
        else if (duplicateEpisodeIndex > -1)
        {
            cat_data.levelsInfo[duplicateEpisodeIndex] = currentLevelIfo;
        }
        else
        {
            cat_data.levelsInfo.Add(currentLevelIfo);
            //Update episode Count
            cat_data.chaptersInfo[chapterIndex - 1].no_Of_Episodes += 1;
        }
        SortTheCatalog();

        if (!UIImageUploader.instance.isOffline)
        {
            await UpdateServerCatalog();
        }
        UIImageUploader.instance.uploadingPanel.SetActive(false);
        UIImageUploader.instance.UpdateOtherUI(cat_data);
    }
    private bool CheckForOverWrite(int episode, int chapter, ref int _levelInfoIndex, ref int _duplicateEpisode)
    {
        for (int i = 0; i < CatalogForTool.instance.CatalogData.levelsInfo.Count; i++)
        {
            if (CatalogForTool.instance.CatalogData.levelsInfo[i].chapterId == chapter && CatalogForTool.instance.CatalogData.levelsInfo[i].episodeId == episode)
            {
                _duplicateEpisode = i;
            }
            if (CatalogForTool.instance.CatalogData.levelsInfo[i].levelNo == level)
            {
                _levelInfoIndex = i;
                Debug.Log("Level is there in level info");
                return true;
            }
        }
        if (_duplicateEpisode > -1) return true;
        return false;
    }
    private async UniTask SaveLevelFile(List<MarkedDifferenceData> markedPoints, string levelURL)
    {
        imageUploader.UploadingPanelStat(true);

        // Convert textures to Base64 strings
        byte[] originalBytes1 = File.ReadAllBytes(UIImageUploader.filepath1);
        string tex1Base64 = Convert.ToBase64String(originalBytes1);

        byte[] originalBytes2 = File.ReadAllBytes(UIImageUploader.filepath2);
        string tex2Base64 = Convert.ToBase64String(originalBytes2);

        // Create the data object
        LevelData data = new LevelData
        {
            image1_Base64 = tex1Base64,
            image2_Base64 = tex2Base64,
            points = markedPoints,
            chapterId = imageUploader.cityDropDown.value+1,
            episodeIndex = int.Parse(imageUploader.levelEpisodeIndex.text),// need input for this 
        };

        // incase no directory
        if (!Directory.Exists(saveFilePath))
        {
            Directory.CreateDirectory(saveFilePath);
        }

        // Convert to JSON
        string json = JsonUtility.ToJson(data);

        // Save to file
        string localFilePath = Path.Combine(saveFilePath, $"Level_{level:000}.json");

        await File.WriteAllTextAsync(localFilePath, json);

        await storage.UploadAsync(localFilePath, levelURL);

        Debug.Log("Level Uploaded at :" + levelURL);
        imageUploader.UploadingPanelStat(false);
        ServerStatus();

        return;
    }
    internal void SortTheCatalog()
    {
        CatalogData data = CatalogForTool.instance.CatalogData;
        CatalogForTool.instance.CatalogData.levelsInfo = data.levelsInfo.OrderBy(l => l.chapterId) // Sort by chapterId (ascending)
                     .ThenBy(l => l.episodeId) // Sort by episodeId (ascending)
                     .ToList();

        CatalogForTool.instance.CatalogData.chaptersInfo = data.chaptersInfo.OrderBy(l => l.id) // Sort by chapterId (ascending)
                    .ToList();
    }
    internal async UniTask UpdateServerCatalog()
    {
        // Convert to JSON
        string catalogJson = JsonConvert.SerializeObject(CatalogForTool.instance.CatalogData, Formatting.None);

        // Save to file
        string filePath = Path.Combine(UnityEngine.Application.temporaryCachePath, "Catalog.json");

        await File.WriteAllTextAsync(filePath, catalogJson);

        await storage.UploadAsync(filePath, stagingCatalog);
    }
    public async void DeleteLevel()
    {
        level = int.Parse(imageUploader.levelINP.text);
        int episodeIndex = int.Parse(UIImageUploader.instance.levelEpisodeIndex.text);
        int chapterIndex = UIImageUploader.instance.cityDropDown.value + 1;


        if (level <= 0 || episodeIndex <= 0 || chapterIndex <= 0)
        {
            UIImageUploader.instance.ShowError("Check and validate chapter , episode and level ");
            return;
        }

        try
        {
            int deleteIndex = CatalogForTool.instance.CatalogData.levelsInfo.IndexOf(CatalogForTool.instance.CatalogData.levelsInfo.Where((lvl) => lvl.chapterId == chapterIndex && lvl.episodeId == episodeIndex && lvl.levelNo == level).ToArray()[0]);
            CatalogForTool.instance.CatalogData.levelsInfo.RemoveAt(deleteIndex);
            CatalogForTool.instance.CatalogData.chaptersInfo[chapterIndex - 1].no_Of_Episodes--;

            SortTheCatalog();
            /// 
            await UpdateServerCatalog();
            // 
            Debug.Log("Deleted level :" + level);
            UIImageUploader.instance.UpdateOtherUI(CatalogForTool.instance.CatalogData);

        }
        catch
        {
            UIImageUploader.instance.ShowError("Can not find level to delete");
        }
    }
    bool CheckForNullData(List<MarkedDifferenceData> markedPoints)
    {
        if (markedPoints == null || markedPoints.Count <= 0)
        {
            imageUploader.UpdateStatus("Please add diffrence point");
            return true;
        }
        if (imageUploader.imageOne.texture == null)
        {
            imageUploader.UpdateStatus("Please add image 1");
            return true;
        }
        if (imageUploader.imageTwo.texture == null)
        {
            imageUploader.UpdateStatus("Please add image 2");
            return true;
        }
        if (string.IsNullOrEmpty(imageUploader.levelEpisodeIndex.text))
        {
            imageUploader.UpdateStatus("Please add levelEpisodeIndex");
            return true;
        }
        if (string.IsNullOrEmpty(imageUploader.levelImageID.text))
        {
            imageUploader.UpdateStatus("Please add levelImageID");
            return true;
        }
        if (string.IsNullOrEmpty(imageUploader.levelINP.text))
        {
            imageUploader.UpdateStatus("Please add Level no");
            return true;
        }
        level = int.Parse(imageUploader.levelINP.text);
        //saveFilePath=imageUploader.savePathINP.text;
        return false;
    }
    private string ConvertTextureToBase64(Texture texture)
    {
        // Convert Texture to Texture2D
        //Texture2D texture2D = ConvertToTexture2D(texture);
        Texture2D texture2D = texture as Texture2D;

        // Encode the Texture2D to PNG or JPG and convert to Base64
        //byte[] textureBytes = texture2D.EncodeToPNG(); // Use EncodeToJPG() if preferred
        byte[] textureBytes = ImageConversion.EncodeArrayToPNG(texture2D.GetRawTextureData(), texture2D.graphicsFormat, (uint)texture2D.width, (uint)texture2D.height);
        Debug.Log($"PNG Byte Size: {textureBytes.Length}");
        string base64 = Convert.ToBase64String(textureBytes);
        Debug.Log($"Base64 String Length: {base64.Length}");
        return base64;
    }
    private Texture2D ConvertToTexture2D(Texture texture)
    {
        // Create a new Texture2D with the same dimensions as the input texture
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

        // Copy the Texture to the RenderTexture
        Graphics.Blit(texture, renderTexture);

        // Create a new Texture2D to store the RenderTexture's contents
        Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);

        // Read pixels from the RenderTexture into the Texture2D
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        texture2D.Apply();

        // Release the RenderTexture
        RenderTexture.ReleaseTemporary(renderTexture);
        RenderTexture.active = null;

        return texture2D;
    }
}
