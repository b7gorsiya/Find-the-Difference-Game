using BunnyCDN.Net.Storage;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToolChapter : MonoBehaviour
{
    public TextMeshProUGUI chapterText;
    public ChapterInfo info;
    public List<LevelInfo> levelInfos;

    ChapterEditUI uiScript;
    Button btn;
    BunnyCDNStorage storage;

    private void Awake()
    {
        btn = GetComponent<Button>();
        storage = new BunnyCDNStorage("crimsongamescbnasia", "76a0ec76-7729-41f6-91cc4a38272e-a731-4e06", "sg");
    }
    public void Init(ChapterInfo _info, ChapterEditUI _uiScript)
    {
        info = _info;
        chapterText.text = info.title;
        levelInfos = CatalogForTool.instance.CatalogData.levelsInfo.Where((level) => level.chapterId == info.id).ToList();
        uiScript = _uiScript;
        btn.onClick.AddListener(ShowChapterInfo);
    }

    public async void ShowChapterInfo()
    {
        uiScript.currentChapter = info.id;

        foreach (var _levelUI in uiScript.episodeContainer.GetComponentsInChildren<ToolEpisode>())
        {
            Destroy(_levelUI.gameObject);
        }

        foreach (var _level in levelInfos)
        {
            var _obj = Instantiate(uiScript.episodePrefab, uiScript.episodeContainer);
            var _script = _obj.GetComponent<ToolEpisode>();
            _script.Init(_level,uiScript);
        }
        uiScript.chapterClaimText.text = info.claimText;
        var tex = CatalogForTool.instance.DownloadTexture(info.collectionImage);
        var tex2 = CatalogForTool.instance.DownloadTexture(info.progressImage);

        uiScript.episodeEditUI.SetActive(false);

        Destroy(uiScript.chapterImage.texture);
        uiScript.chapterImage.texture = await tex;
        Destroy(uiScript.chapterIcon.texture);
        uiScript.chapterIcon.texture = await tex2;
    }
    internal async UniTask<Texture2D> DownloadTexture(string imageFileURL)
    {

        var stream = await storage.DownloadObjectAsStreamAsync(imageFileURL);
        await UniTask.SwitchToMainThread();

        // Read the stream into a byte array
        using (MemoryStream memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            byte[] imageData = memoryStream.ToArray();
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
}
