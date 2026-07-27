using CrimsonGames.Analytics;
using CrimsonLibrary.SupportLibrary.UIHelperScripts;
using CrimsonLibrary.SupportLibrary.Utils.Generics;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameManager;

public class UIManager : GenericManager<UIManager>
{
    public TextMeshProUGUI levelNumber;

    public MainMenuUI mainMenuPanel;
    public MyCollectionUI collectionPanel;
    public UIPanel gameUIPanel;
    public UIPanel loaderPanel;
    public Button backButton;
    public UIPanel backPanel;
    public UIPanel mainUIPanel;
    public CompletedCollectionInfoPanel completedCollectionInfoPanel;

    [Header("GameOver data")]
    public UIPanel gameOverPanel;
    public RawImage mainImage;
    public GameObject winUI;
    public GameObject loseUI;
    public TextMeshProUGUI gameoverMSG;
    public Button nextLevelBtn;
    public TextMeshProUGUI outroChapterTitleText;
    public RawImage outroChapterImage;
    public TextMeshProUGUI claimOutroChapterTitleText;
    public TextMeshProUGUI claimOutroText;

    public RawImage claimOutroImage;
    [Space]
    public TextMeshProUGUI livesText;
    //public TextMeshProUGUI markerCountsText;

    [Header("Lives Renew")]
    public UIPanel livesOverPanel;
    public Button closeBtn;
    public Button rewardBtn;
    public Button restartButton;

    [Header("Hint UI")]
    public Button hintButton;
    public GameObject hintRenewPanel;
    public Button hint_closeBtn;
    public Button hint_rewardBtn;
    public TextMeshProUGUI hintTxt;
    public GameObject hintOverAd_icon;

    [Header("CommingSoon")]
    public UIPanel commingSoonPanel;
    public TextMeshProUGUI commingSoonLevelText;

    [Header("Error Popups")]
    public UIPanel noAdsPanel;
    public Button cancelButton;
    public UIPanel noInernetPanel;
    public Button retryButton;

    [Header("General Popups")]
    public RateUsUI rateUsUI;
    public UIPanel updatePopup;
    public GlobalToaster toaster;
    public TMP_Text settingsPlayerIdText;
    public FeedbackUI feedbackUI;
    public UIPanel iapPanel;

    public bool menuReady=false;
    public bool collectionReady = false;

    private void Awake()
    {
        PlayFabPlayerManager.Instance.onLoginSuccess += () =>
        {
            settingsPlayerIdText.text = "ID : " + PlayFabPlayerManager.Instance.PlayFabId;
        };

        PlayFabPlayerManager.Instance.onLoginFail += () =>
        {
            settingsPlayerIdText.text = "ID : " + PlayFabPlayerManager.Instance.PlayFabId;
        };

        IAPManager.OnIAPPurchaseSuccess += (product) =>
        {
            if (iapPanel.IsPanelActive)
            {
                iapPanel.DeactivatePanel();
            }
        };
    }
    public void ClearPopUps()
    {
        iapPanel.DeactivatePanel();
        rateUsUI.DeactivatePanel();
        feedbackUI.DeactivatePanel();
    }
    public void UpdateLevelNumber()
    {
        levelNumber.text = "Level " + GameManager.Instance.currentLevel;
    }
    public void UpdateMarkersCount(int total, int marked)
    {
        //markerCountsText.text = marked + "/" + total;
    }

    public void UpdateLive(int _lives)
    {
        livesText.text = _lives.ToString("0");
    }

    public void OnGameOver(bool _isWin = true)
    {
        if (GameManager.Instance.currentLevel - 1 >= SettingsManager.Instance.InternalSettingsData.postPuzzleAdsNumber)
        {
            GoogleAdsWrapper.Instance.ShowInterstitialAd();
        }

        if (_isWin)
        {
            nextLevelBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Level " + (GameManager.Instance.currentLevel - 1); // for animation keep it current level
            gameoverMSG.text = "You have completed level " + (GameManager.Instance.currentLevel - 1);
        }
        else
            gameoverMSG.text = "Lose Lose";

        nextLevelBtn.transform.parent.gameObject.SetActive(false);

        winUI.SetActive(false);
        winUI.SetActive(_isWin);
        if (_isWin)
        {
            winUI.GetComponent<UIPanel>().ActivatePanel();
        }
        loseUI.SetActive(!_isWin);

        gameOverPanel.ActivatePanel();

        GameManager.Instance.state = GameState.GameOver;

        //Rate us popup check
        if ((GameManager.Instance.currentLevel - 1) == SettingsManager.Instance.InternalSettingsData.rateUsPuzzleNumber)
        {
            winUI.GetComponent<Animator>().Play("GameWin 0"); // outro animation
            //rateUsUI.ActivatePanel();
        }
        else
        {
            winUI.GetComponent<Animator>().Play("GameWin");
        }
        //LevelComplete?.Invoke(_isWin, GameManager.Instance.currentLevel - 1 == SettingsManager.Instance.InternalSettingsData.rateUsPuzzleNumber);
    }
    public async void FillOutOutroInfo()
    {
        winUI.GetComponent<Animator>().enabled = true;
        outroChapterImage.transform.parent.parent.gameObject.SetActive(true);

        ChapterInfo info = collectionPanel.collectionItems[GameManager.Instance.currentChapterID - 1].info;

        int totalEpisode = info.no_Of_Episodes;
        int currentEpisode = GameManager.Instance.catalogData.levelsInfo[GameManager.Instance.currentLevel - 1].episodeId;

        winUI.GetComponent<OutroAnimation>().chapterStatSlider.value = (1f / (float)totalEpisode) * ((float)currentEpisode-1f);

        claimOutroChapterTitleText.text = outroChapterTitleText.text = info.title;
        claimOutroText.text = info.claimText;
        var tex = LoadingJsonData.Instance.DownloadTexture(info.collectionImage);
        Destroy(claimOutroImage.texture);
        claimOutroImage.texture = await tex;

        outroChapterImage.texture = claimOutroImage.texture;
    }
    public void ShowRatePopUp()
    {
        rateUsUI.ActivatePanel();
    }
    public void PopInNextLevelButton()
    {
        if (GameManager.Instance.state == GameManager.GameState.GameOver&&! nextLevelBtn.transform.parent.gameObject.activeSelf)
        {
            AudioManager.Instance.PlaySFX(AudioEvent.NextLevelButton);
            DOVirtual.DelayedCall(1f, () => nextLevelBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Level " + GameManager.Instance.currentLevel);
            nextLevelBtn.transform.parent.gameObject.SetActive(true);
        }
    }
    public void LivesOver()
    {
        livesOverPanel.ActivatePanel();
        // closeBtn.onClick.AddListener(() => { livesOverPanel.SetActive(false); UIManager.Instance.OnGameOver(false); });
    }
    public void LivesRewarded()
    {
        UIDifferenceMarker.Instance.RefillLives();
        livesOverPanel.DeactivatePanel();
        DOVirtual.DelayedCall(0.1f, () => GameManager.Instance.state = GameState.GamePlay);
    }

    public bool showingHint = false;
    public void ShowHint()
    {
        AudioManager.Instance.OnButtonTap();
        if (showingHint || GameManager.Instance.state == GameManager.GameState.Pause)
        {
            return;
        }

        showingHint = true;

        if (GameManager.Instance.hintCount > 0)
        {
            GameManager.Instance.hintCount--;
            UIDifferenceMarker.Instance.ShowHint();
            UpdateHintText();
        }
        else
        {
            //hint_closeBtn.onClick.RemoveAllListeners();
            //hint_rewardBtn.onClick.RemoveAllListeners();

            //GameManager.Instance.hintCount = 0;
            //GameManager.Instance.state = GameState.Pause;
            //hintRenewPanel.SetActive(true);
            //hint_closeBtn.onClick.AddListener(() =>
            //{
            //    hintRenewPanel.SetActive(false);
            //    GameManager.Instance.state = GameState.GamePlay;
            //});
            //hint_rewardBtn.onClick.AddListener(() =>
            //{
            if (GoogleAdsWrapper.Instance.CanShowRewardedAd())
            {
                GoogleAdsWrapper.Instance.ShowRewardedAd(() =>
                {
                    hintOverAd_icon.SetActive(false);
                    hintTxt.transform.parent.gameObject.SetActive(true);
                    GameManager.Instance.hintCount++;
                    //GameManager.Instance.hintCount = GameManager.Instance.maxHintCount;
                    PlayerPrefs.SetInt("Hint", GameManager.Instance.hintCount);
                    hintRenewPanel.SetActive(false);
                    UpdateHintText();
                    showingHint = false;
                    DOVirtual.DelayedCall(0.1f, () =>
                    {
                        GameManager.Instance.state = GameState.GamePlay;
                        ShowHint();
                    });
                });
            }
            else
            {
                DOTween.Clear();
                noAdsPanel.ActivatePanel();
                cancelButton.onClick.AddListener(() => DOVirtual.DelayedCall(0.1f, () => showingHint = false));
            }
            //});
        }
        PlayerPrefs.SetInt("Hint", GameManager.Instance.hintCount);
    }
    public void BackButtonClick()
    {
        if (showingHint)
        {
            return;
        }
        AudioManager.Instance.OnButtonTap();

        backPanel.ActivatePanel();
        GameManager.Instance.PauseGame(true);

        CrimsonEventsLogger.LogEvent(EPlayerEvent.backButton_tapped, family: 0, genus: mainMenuPanel.GameLoadSource);

        GameManager.Instance.Clean();
    }
    public void Home()
    {
        Debug.Log("home");
        backPanel.DeactivatePanel();
        gameOverPanel.DeactivatePanel();
        gameUIPanel.DeactivatePanel();
        //collectionPanel.DeactivatePanel();

        mainMenuPanel.ActivatePanel();
    }
    public void UpdateHintText()
    {
        hintTxt.text = GameManager.Instance.hintCount.ToString();
        if (GameManager.Instance.hintCount <= 0)
        {
            hintOverAd_icon.SetActive(true);
            hintTxt.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            hintOverAd_icon.SetActive(false);
            hintTxt.transform.parent.gameObject.SetActive(true);
        }
    }

    public void AddHint_Debug()
    {
        GameManager.Instance.hintCount = GameManager.Instance.maxHintCount;
        PlayerPrefs.SetInt("Hint", GameManager.Instance.hintCount);
        UpdateHintText();
        showingHint = false;
        //DOVirtual.DelayedCall(0.1f, () => GameManager.Instance.state = GameState.GamePlay);
    }

    public void ContactUs()
    {
        AudioManager.Instance.OnButtonTap();
        Application.OpenURL("https://forms.gle/atN1gQaD1EkgPFXcA");
    }

    public void CopyPlayerID()
    {
        AudioManager.Instance.OnButtonTap();
        GUIUtility.systemCopyBuffer = PlayFabPlayerManager.Instance.PlayFabId;
        toaster.ShowToaster("Player ID copied!");
    }

    public void OpenPrivacyPolicy()
    {
        AudioManager.Instance.OnButtonTap();
        Application.OpenURL("https://crimsongames.net/privacypolicy");
    }

    public void OpenTandC()
    {
        AudioManager.Instance.OnButtonTap();
        Application.OpenURL("https://crimsongames.net/termsandconditions/");
    }

    public void LogNoAdsTapped(string source)
    {
        CrimsonEventsLogger.LogEvent(EPlayerEvent.noAds_buttonTapped, genus: source);
    }
}
