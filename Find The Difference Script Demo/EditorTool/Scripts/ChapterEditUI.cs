using SFB;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterEditUI : MonoBehaviour
{
    public Transform chapterContainer;
    public Transform episodeContainer;

    [Header("Chapter Details")]
    public RawImage chapterImage;
    public RawImage chapterIcon;
    public TMP_InputField chapterClaimText;

    [Header("Episode Details")]
    public GameObject episodeEditUI;
    public TextMeshProUGUI episodeTitle;
    public TMP_InputField episodeID;

    public GameObject episodePrefab;
    public GameObject chapterPrefab;


    internal string chapterImagePath = null;
    internal string chapterIconPath = null;

    internal int currentEpisode = -1;
    internal int currentChapter = -1;
    internal int currentLevel = -1;
    public void InitializeChapterEdit()
    {
        foreach (var i in CatalogForTool.instance.CatalogData.chaptersInfo)
        {
            var _obj = Instantiate(chapterPrefab, chapterContainer);
            var _script = _obj.GetComponent<ToolChapter>();
            _script.Init(i, this);
        }
        episodeEditUI.SetActive(false);
    }

    #region Save/Delete

    //Episode
    public async void DeleteEpisode()
    {
        if (currentLevel <= 0 || currentEpisode <= 0 || currentChapter <= 0)
        {
            UIImageUploader.instance.ShowError("Check and validate chapter , episode and level ");
            return;
        }

        int episodeIndex = currentEpisode;
        int chapterIndex = currentChapter;

        try
        {
            int deleteIndex = CatalogForTool.instance.CatalogData.levelsInfo.IndexOf(CatalogForTool.instance.CatalogData.levelsInfo.Where((lvl) => lvl.chapterId == chapterIndex && lvl.episodeId == episodeIndex && lvl.levelNo == currentLevel).ToArray()[0]);
            CatalogForTool.instance.CatalogData.levelsInfo.RemoveAt(deleteIndex);
            CatalogForTool.instance.CatalogData.chaptersInfo[chapterIndex - 1].no_Of_Episodes--;

            LevelCreation.instance.SortTheCatalog();
            /// 
            await LevelCreation.instance.UpdateServerCatalog();
            // 
            Debug.Log("Deleted Epiosde :" + currentEpisode);
            UIImageUploader.instance.UpdateOtherUI(CatalogForTool.instance.CatalogData);

        }
        catch
        {
            UIImageUploader.instance.ShowError("Can not find level to delete");
        }
    }
    //Episode
    public async void UpdateEpisode()
    {
        if (currentLevel <= 0 || currentEpisode <= 0 || currentChapter <= 0)
        {
            UIImageUploader.instance.ShowError("Check and validate chapter , episode and level ");
            return;
        }

        int updatedEpisodeIndex = int.Parse(episodeID.text);
        int chapterIndex = currentChapter;

        try
        {
            int updateIndex = CatalogForTool.instance.CatalogData.levelsInfo.IndexOf(CatalogForTool.instance.CatalogData.levelsInfo.Where((lvl) => lvl.chapterId == chapterIndex && lvl.episodeId == currentEpisode && lvl.levelNo == currentLevel).ToArray()[0]);
            CatalogForTool.instance.CatalogData.levelsInfo[updateIndex].episodeId = updatedEpisodeIndex;
            /// 
            await LevelCreation.instance.UpdateServerCatalog();
            // 
            Debug.Log("Update Epiosde :" + currentEpisode + " TO :" + updatedEpisodeIndex);
            UIImageUploader.instance.UpdateOtherUI(CatalogForTool.instance.CatalogData);

        }
        catch
        {
            UIImageUploader.instance.ShowError("Can not find level to delete");
        }
    }
    public async void DeleteChapter()
    {
        if (currentChapter <= 0)
        {
            UIImageUploader.instance.ShowError("Check and validate chapter  ");
            return;
        }

        int chapterIndex = currentChapter;

        try
        {
            CatalogForTool.instance.CatalogData.chaptersInfo.RemoveAt(currentChapter - 1);

            LevelCreation.instance.SortTheCatalog();
            /// 
            await LevelCreation.instance.UpdateServerCatalog();
            // 
            Debug.Log("Deleted Chapter :" + currentChapter);
            UIImageUploader.instance.UpdateOtherUI(CatalogForTool.instance.CatalogData);

        }
        catch
        {
            UIImageUploader.instance.ShowError("Can not find level to delete");
        }
    }
    public async void UpdateChapter()
    {
        if (currentChapter <= 0)
        {
            UIImageUploader.instance.ShowError("Check and validate chapter  ");
            return;
        }

        int chapterIndex = currentChapter;

        try
        {
            ChapterInfo _info = CatalogForTool.instance.CatalogData.chaptersInfo[currentChapter - 1];

            _info.claimText = chapterClaimText.text;
            if (File.Exists(chapterImagePath)) // upload image 
            {
                CatalogForTool.instance.UploadImage(chapterImagePath, _info.collectionImage);
            }
            if (File.Exists(chapterIconPath))
            {
                CatalogForTool.instance.UploadImage(chapterIconPath, _info.progressImage);
            }
            // _info.progressImage = chapterIconPath;
            // _info.collectionImage = chapterImagePath;

            CatalogForTool.instance.CatalogData.chaptersInfo[currentChapter - 1] = _info;
            /// 
            await LevelCreation.instance.UpdateServerCatalog();
            // 
            Debug.Log("Update Chapter :" + currentChapter);
            UIImageUploader.instance.UpdateOtherUI(CatalogForTool.instance.CatalogData);

        }
        catch
        {
            UIImageUploader.instance.ShowError("Can not find level to delete");
        }
    }
    #endregion

    #region Image Picker
    public void PickChapterImage()
    {
        string _imagePath = OpenFileDialog();
        chapterImage.texture = GetTexture(_imagePath);
        chapterImagePath = _imagePath;
    }
    public void PickChapterIcon()
    {
        string _imagePath = OpenFileDialog();
        Debug.Log("Pick path :" + _imagePath);
        chapterIcon.texture = GetTexture(_imagePath);
        chapterIconPath = _imagePath;

    }
    private string OpenFileDialog()
    {
        var path = StandaloneFileBrowser.OpenFilePanel("Select an Image", "", new[] { new ExtensionFilter("Image Files", "png", "jpg", "jpeg") }, false);
        if (path.Length <= 0)
            return null;
        return path[0];
    }
    #endregion

    private Texture GetTexture(string _path)
    {
        byte[] fileData = File.ReadAllBytes(_path);
        Debug.Log("Original PNG size :: " + fileData.Length);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);

        if (!texture.LoadImage(fileData))
        {
            return null;
        }
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();

        return texture;
    }

}
