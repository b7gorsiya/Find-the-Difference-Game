using CrimsonGames.Analytics;
using CrimsonLibrary.SupportLibrary.UIHelperScripts;
using DG.Tweening;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : UIPanel
{
    [Space]
    [Header("MainMenu")]
    public Button nextLevelButton;
    TextMeshProUGUI buttonText;

    // Debug Properties
    public TMP_InputField testLevelinp;
    public TextMeshProUGUI testLevelTXT;

    private string gameLoadSource = "home";

    public string GameLoadSource { get => gameLoadSource; }

    [Header("Chapter Progress")]
    public GameObject menuChapterProgressPrefab;
    public List<MainMenuChapterUI> menuChapterProgressItems = new List<MainMenuChapterUI>();
    List<ChapterInfo> chapterList;
    public Transform progressUIContent;

    private void Awake()
    {
        buttonText = nextLevelButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    public void DrawMainMenu()
    {
        GameManager.Instance.state = GameManager.GameState.MainMenu;
        buttonText = nextLevelButton.GetComponentInChildren<TextMeshProUGUI>();
        buttonText.text = "Level " + GameManager.Instance.currentLevel;// "Level " + PlayerPrefs.GetInt("Level", 1);
        //Analytics
        CrimsonEventsLogger.LogEvent(EPlayerEvent.menu_loaded, family: 1);
        UIManager.Instance.mainUIPanel.ActivatePanel();
        DrawProgressUI();
    }
    public void InitMainMenuChaptersUI()
    {
        chapterList = GameManager.Instance.catalogData.chaptersInfo;

        foreach (var chapter in chapterList)
        {
            var _chapterUI = Instantiate(menuChapterProgressPrefab, progressUIContent).GetComponent<MainMenuChapterUI>();
            _chapterUI.Initialized += CheckUIInit;
            _chapterUI.Init(chapter);
            if (menuChapterProgressItems != null && menuChapterProgressItems.Count > 0)
            {
                menuChapterProgressItems.Last().nextChapter = _chapterUI.gameObject;
            }
            menuChapterProgressItems.Add(_chapterUI);
            _chapterUI.gameObject.SetActive(false);
            _chapterUI.gameObject.GetComponent<RectTransform>().DOAnchorPos(new Vector2(1000, 0), 0.01f);
        }
        CatalogDownloadManager.CatalogDownloaded -= InitMainMenuChaptersUI;
    }
    int uiCount = 0;
    void CheckUIInit()
    {
        uiCount++;
        if (uiCount >= GameManager.Instance.catalogData.chaptersInfo.Count)
        {
            UIManager.Instance.menuReady = true;
            Debug.Log("Menu Ready");
        }
    }
    public void DrawProgressUI()
    {
        int episodeCount = 0;

        foreach (var menuChapterUI in menuChapterProgressItems)
        {
            // in case no episode added in Chapter
            if(menuChapterUI.info.no_Of_Episodes<=0)
            {
                if (menuChapterUI.info.id == GameManager.Instance.currentChapterID)
                {
                    menuChapterUI.gameObject.SetActive(true);
                    menuChapterUI.gameObject.GetComponent<RectTransform>().DOAnchorPos(new Vector2(0, 0), 0.01f);
                }
                continue;
            }
            Debug.Log("Checking chapter progress "+menuChapterUI.info.title);

            episodeCount += menuChapterUI.info.no_Of_Episodes;

            if (episodeCount < GameManager.Instance.currentLevel)
            {
                menuChapterUI.gameObject.SetActive(false);
            }
            int completedEpisode = episodeCount - GameManager.Instance.currentLevel;
            if (completedEpisode < menuChapterUI.info.no_Of_Episodes && GameManager.Instance.currentChapterID==menuChapterUI.info.id)
            {
                // this is the current level
                menuChapterUI.gameObject.SetActive(true);
                menuChapterUI.gameObject.GetComponent<RectTransform>().DOAnchorPos(new Vector2(0, 0), 0.01f);
                DOVirtual.DelayedCall(0.3f, () => menuChapterUI.UpdateProgress((menuChapterUI.info.no_Of_Episodes - completedEpisode) - 1));
                break;
            }
        }
    }
    public void ClickLoadLevel()
    {
        AudioManager.Instance.OnButtonTap();

        //Do check for coming soon
        if (LoadingJsonData.Instance.HasReachedEndOfLevels())
        {
            return;
        }

        LoadingJsonData.Instance.StartLoading(GameManager.Instance.currentLevel/*PlayerPrefs.GetInt("Level", 1)*/);
        UIManager.Instance.ClearPopUps();
        //Analytics
        CrimsonEventsLogger.LogEvent(EPlayerEvent.puzzle_tapped, genus: gameLoadSource);
    }

    public void OnTestLevelChanges()
    {
        testLevelTXT.text = "Test Level " + testLevelinp.text;
    }
    public void TestLevel()
    {
        GameManager.Instance.currentLevel = int.Parse(testLevelinp.text);
        LoadingJsonData.Instance.StartLoading(int.Parse(testLevelinp.text));
    }

    public void CheckVersion()
    {
        Debug.Log("[CheckVersion] Called");

        string clientVersion = Application.version;
        string latestVersionFromFirebase = SettingsManager.Instance.InternalSettingsData.gameVersion;

        string[] clientVersionComponents = clientVersion.Split('.');
        string[] latestVersionComponents = latestVersionFromFirebase.Split('.');

        int clientMajorVersion = int.Parse(clientVersionComponents[0]);
        int clientMinorVersion = int.Parse(clientVersionComponents[1]);

        int latestMajorVersion = int.Parse(latestVersionComponents[0]);
        int latestMinorVersion = int.Parse(latestVersionComponents[1]);

        Debug.Log("Client version :: " + clientMajorVersion + "." + clientMinorVersion);
        Debug.Log("Firebase version :: " + latestMajorVersion + "." + latestMinorVersion);

        int comparisonResult = string.Compare(clientVersion, latestVersionFromFirebase);

        if (comparisonResult < 0)
        {
            // Client version is older
            Debug.Log("Show update popup");
#if !UNITY_EDITOR
            UIManager.Instance.updatePopup.ActivatePanel(); //BG edit
#endif
            //CrimsonEventsLogger.LogUpdatePopupViewed();
            CrimsonEventsLogger.LogEvent(EPlayerEvent.updatePopUp_viewed);
        }
        else if (comparisonResult == 0)
        {
            // Client version is up to date
            Debug.Log("Client version is up to date");
        }
        else
        {
            // Client version is newer (unlikely in a standard update scenario)
            Debug.Log("Somehow client version is newer");
        }
    }

    public void LogUpdateCTA()
    {
        Application.OpenURL(SettingsManager.PlayStoreURL);
        CrimsonEventsLogger.LogEvent(EPlayerEvent.updatePopUp_updateSelected);
    }

    public void SetGameLoadSource(string source)
    {
        gameLoadSource = source;
    }
}
