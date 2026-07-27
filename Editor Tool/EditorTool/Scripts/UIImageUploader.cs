using CrimsonGames.CBN.InputHandling;
using SFB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;

public class UIImageUploader : MonoBehaviour
{
    public RawImage imageOne; // Assign a UI RawImage to preview the uploaded image
    public RawImage imageTwo; // Assign a UI RawImage to preview the uploaded image

    public TextMeshProUGUI statusText;      // Optional Text UI to show messages to the user

    int imageIndex = 0;

    public TextMeshProUGUI savePathINP;

    [Header("Level Info")]
    public TMP_InputField levelINP;
    public TMP_InputField levelImageID;
    public TMP_InputField levelEpisodeIndex;
    public TextMeshProUGUI episodeList;


    [Header("Edit UI")]
    public Toggle offlineToggle;
    public bool isOffline => offlineToggle != null && offlineToggle.isOn;
   

    [SerializeField] ImageMarkerWithPanZoom marker;

    public static string filepath1;
    public static string filepath2;

    public GameObject overWritePanel;
    public GameObject episodeOverWritePanel;

    public GameObject uploadingPanel;
    public TextMeshProUGUI totalLevelText;
    public TextMeshProUGUI serverPath;

    public TMP_Dropdown countryDropDown;
    public TMP_Dropdown cityDropDown;

    public GameObject loadingPanel;

    public static UIImageUploader instance;

    public GameObject errorPanel;
    private void Awake()
    {
        instance = this;
        Init_DropDown();
        CatalogForTool.instance.StartEditor();
    }

    public void ShowError(string message)
    {
        errorPanel.SetActive(true);
        errorPanel.GetComponentInChildren<TextMeshProUGUI>().text = message;
    }

    #region Init UI

    public void ClearUI()
    {
        //Init_DropDown();
        marker.ClearUI();
    }
    private void Init_DropDown()
    {
        // Clear existing options
        countryDropDown.ClearOptions();
        List<string> enumNames = new List<string>(Enum.GetNames(typeof(LevelInfoCountry)));
        countryDropDown.AddOptions(enumNames);

        // Clear existing options
        cityDropDown.ClearOptions();
        List<string> enumNames2 = new List<string>(Enum.GetNames(typeof(LevelInfoChapters)));
        cityDropDown.AddOptions(enumNames2);
        cityDropDown.onValueChanged.AddListener(UpdateEpisodeList);
    }
    public void UpdateEpisodeList(int _value)
    {
        var levelData = CatalogForTool.instance.CatalogData.levelsInfo;
        var listOfEpisode = levelData.Where((level) => level.chapterId.Equals(_value + 1));

        episodeList.text = "";
        foreach (var level in listOfEpisode)
        {
            episodeList.text += "\n" + level.episodeId + " : " + level.imageId +"<b>["+level.levelNo+"]</b>";
        }
        levelEpisodeIndex.text = (listOfEpisode.Count() + 1).ToString();
    }

    public void UpdateImageIDName()
    {
        string imageId = Regex.Replace(Path.GetFileName(filepath1), @"_(?!.*_).*", "");
        levelImageID.text = imageId;
    }

    public void UpdateOtherUI(CatalogData _data)
    {
        levelEpisodeIndex.text =(_data.chaptersInfo[(cityDropDown.value)].no_Of_Episodes+1).ToString() ;
        levelINP.text = (_data.levelsInfo.Count + 1).ToString();
        UpdateEpisodeList(cityDropDown.value);
    }
    #endregion

    public void UploadingPanelStat(bool _isLevel)
    {
        if (_isLevel)
        {
            uploadingPanel.transform.GetChild(0).GetComponentInChildren<Image>().color = Color.white;
            uploadingPanel.transform.GetComponentInChildren<TextMeshProUGUI>().text = "Uploading Level";

            UpdateStatus("Level Uploading");
        }
        else
        {
            uploadingPanel.transform.GetChild(0).GetComponentInChildren<Image>().color = Color.green;
            uploadingPanel.transform.GetComponentInChildren<TextMeshProUGUI>().text = "Updating & Uploading Staging Catalog";

            UpdateStatus("Level Updated");
        }
        uploadingPanel.SetActive(true);
    }
    public void UploadImage(int index = 1)
    {
        string path = OpenFileDialog();

        if (string.IsNullOrEmpty(path))
        {
            UpdateStatus("No file selected.");
            return;
        }
        imageIndex = index;
        ValidateAndLoadImage(path);
    }
    public void LoadLevel()
    {
        if (string.IsNullOrWhiteSpace(levelINP.text))
        {
            UpdateStatus("Level number not correct");
            return;
        }
        //if (string.IsNullOrWhiteSpace(savePathINP.text))
        //{
        //    UpdateStatus("Level number not correct");
        //    return;
        //}
        LevelCreation.instance.LoadLevel(savePathINP.text, levelINP.text);
    }
    internal void LoadUI(LevelData _data)
    {
        marker.LoadLevel(_data);
    }
    public void SaveLevel(bool _forceOverWrite)
    {
        marker.SaveLevel(_forceOverWrite);
    }
    internal void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log(message);
    }

    #region Folder Picker
    private string OpenFolderDialog()
    {
        var path = StandaloneFileBrowser.OpenFolderPanel("Select Save Folder", "", false);
        if (path.Length <= 0)
            return null;
        return path[0];
    }

    #endregion

    #region Image Picker
    private string OpenFileDialog()
    {
        var path = StandaloneFileBrowser.OpenFilePanel("Select an Image", "", new[] { new ExtensionFilter("Image Files", "png", "jpg", "jpeg") }, false);
        if (path.Length <= 0)
            return null;
        return path[0];
    }
    #endregion
    private void ValidateAndLoadImage(string path)
    {
        byte[] fileData = File.ReadAllBytes(path);
        Debug.Log("Original PNG size :: " + fileData.Length);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);

        if (!texture.LoadImage(fileData))
        {
            UpdateStatus("Invalid image file.");
            return;
        }
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();

        if (texture.width == 1440 && texture.height == 900)
        {
            ((imageIndex == 1) ? imageOne : imageTwo).texture = texture;
            //((imageIndex == 1) ? imageOne : imageTwo).SetNativeSize();
            UpdateStatus("Image loaded successfully.");
            if (imageIndex == 1)
            {
                filepath1 = path;
                UpdateImageIDName();
            }
            else if (imageIndex == 2)
            {
                filepath2 = path;
            }
        }
        else
        {
            UpdateStatus($"Invalid image size: {texture.width}x{texture.height}. Required size is {1440}x{900}.");
        }
    }
}
