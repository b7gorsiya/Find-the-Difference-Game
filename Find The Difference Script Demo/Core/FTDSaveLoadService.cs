using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using PlayFab.EconomyModels;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class FTDSaveLoadService
{
    public static string persistentDataPath;
    static LevelData levelData;
    static string jsonData = string.Empty;

    public static void Init()
    {
        persistentDataPath = Application.persistentDataPath;
    }

    public static async UniTask DownloadLevel(LevelInfo _levelInfo)
    {
        string url = _levelInfo.url;
        UnityEngine.Debug.Log($"[DownloadLevel] called with {url}");
        Stopwatch sw = new Stopwatch();
        sw.Start();

        string fileName = Path.GetFileName(url);
        string fullFilePath = Path.Combine(persistentDataPath, fileName);


        if (File.Exists(fullFilePath))
        {
            UnityEngine.Debug.Log($"File already exists, but download was called, do nothing");
            return;
        }

        try
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    UnityEngine.Debug.LogError("Failed to download json: " + response.ReasonPhrase);
                    return;
                }

                byte[] data = await response.Content.ReadAsByteArrayAsync();
                jsonData = Encoding.UTF8.GetString(data);
                //jsonData = await response.Content.ReadAsStringAsync();
                levelData = JsonUtility.FromJson<LevelData>(jsonData);// JsonConvert.DeserializeObject<LevelData>(jsonData);
                // string newJsonData = JsonUtility.ToJson(levelData); //JsonConvert.SerializeObject(levelData); :: Do not needed

                File.WriteAllText(Path.Combine(persistentDataPath, Path.GetFileName(url)), jsonData);// newJsonData);
                UnityEngine.Debug.Log($"File {Path.GetFileName(url)} written to {Path.Combine(persistentDataPath, Path.GetFileName(url))}");
            }
        }
        catch (HttpRequestException httpEx)
        {
            UIManager.Instance.noInernetPanel.ActivatePanel();
            UnityEngine.Debug.LogError($"HTTP request error: {httpEx.Message}");
            return;
        }
        catch (IOException ioEx)
        {
            UIManager.Instance.noInernetPanel.ActivatePanel();
            UnityEngine.Debug.LogError($"File I/O error: {ioEx.Message}");
            return;
        }
        catch (Exception ex)
        {
            UIManager.Instance.noInernetPanel.ActivatePanel();
            UnityEngine.Debug.LogError($"Unexpected error: {ex.Message}");
            return;
        }

        UIManager.Instance.retryButton.onClick.RemoveAllListeners();
        sw.Stop();
        UnityEngine.Debug.Log("Time taken for DownloadLevel: " + sw.ElapsedMilliseconds + " ms");
    }
}
