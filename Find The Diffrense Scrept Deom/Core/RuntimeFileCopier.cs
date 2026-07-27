using System.Collections;
using System.IO;
using UnityEngine;

public class RuntimeFileCopier : MonoBehaviour
{
    public float checkVersion = 0.03f;
    private void Awake()
    {
        CheckForDataUpdate();

        if (!PlayerPrefs.HasKey("startlevels"))
        {
            StartCoroutine(CopyLevelFilesAtRuntime());
        }
    }
    private void CheckForDataUpdate()
    {
        Debug.Log("Checking for the Data Update");
        float currentVersion = float.Parse(Application.version);
        if (currentVersion >= checkVersion && PlayerPrefs.GetInt("DataUpdated",0)==0) 
        {
            Debug.Log("need to clear data");
            PlayerPrefs.DeleteAll();
            string path = Application.persistentDataPath;
            if (Directory.Exists(path))
            {
                DirectoryInfo info= new DirectoryInfo(path);
                info.Delete(true);
               // Directory.Delete(path, true);
                Directory.CreateDirectory(path); // Recreate the directory to prevent issues
            }

            PlayerPrefs.SetInt("DataUpdated", 1);
        }

    }

    public IEnumerator CopyLevelFilesAtRuntime()
    {
        string sourcePath = Application.streamingAssetsPath;
        string destinationPath = Application.persistentDataPath;

        if (!Directory.Exists(destinationPath))
        {
            Directory.CreateDirectory(destinationPath);
        }

        string[] fileNames = { "Level_001.json", "Level_002.json", "Level_003.json" };

        foreach (string fileName in fileNames)
        {
            string sourceFilePath = Path.Combine(sourcePath, fileName);
            string destinationFilePath = Path.Combine(destinationPath, fileName);

#if UNITY_ANDROID && !UNITY_EDITOR
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(sourceFilePath))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    File.WriteAllBytes(destinationFilePath, request.downloadHandler.data);
                    Debug.Log($"Copied {fileName} to {destinationPath}");
                }
                else
                {
                    Debug.LogError($"Failed to copy {fileName}: {request.error}");
                }
            }
#else
            if (File.Exists(sourceFilePath))
            {
                File.Copy(sourceFilePath, destinationFilePath, overwrite: true);
                Debug.Log($"Copied {fileName} to {destinationPath}");
            }
            else
            {
                Debug.LogError($"File not found: {sourceFilePath}");
            }

            yield return null;
#endif
        }

        StartCoroutine(CopyCatalogFilesAtRuntime());
        yield break;
    }

    public IEnumerator CopyCatalogFilesAtRuntime()
    {
        string sourcePath = Application.streamingAssetsPath;
        string destinationPath = Path.Combine(Application.persistentDataPath, "Settings");

        if (!Directory.Exists(destinationPath))
        {
            Directory.CreateDirectory(destinationPath);
        }


        string fileName = "OfflineData.json";

        string sourceFilePath = Path.Combine(sourcePath, fileName);
        string destinationFilePath = Path.Combine(destinationPath, fileName);

#if UNITY_ANDROID && !UNITY_EDITOR
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(sourceFilePath))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    File.WriteAllBytes(destinationFilePath, request.downloadHandler.data);
                    Debug.Log($"Copied {fileName} to {destinationPath}");
                }
                else
                {
                    Debug.LogError($"Failed to copy {fileName}: {request.error}");
                }
            }
#else
        if (File.Exists(sourceFilePath))
        {
            File.Copy(sourceFilePath, destinationFilePath, overwrite: true);
            Debug.Log($"Copied {fileName} to {destinationPath}");
        }
        else
        {
            Debug.LogError($"File not found: {sourceFilePath}");
        }

        yield return null;
#endif

        Debug.Log("File copying completed.");
        PlayerPrefs.SetInt("startlevels", 1);
        yield break;
    }
}
