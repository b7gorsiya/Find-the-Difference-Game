using CrimsonGames.Analytics;
using CrimsonLibrary.SupportLibrary.Utils.Generics;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager : GenericManager<GameManager>
{

    public static Action LevelStart;
    public static Action<bool, bool> LevelComplete;

    public enum GameState
    {
        Loading,
        MainMenu,
        GamePlay,
        GameOver,
        Pause
    }

    public GameState state;
    public int currentLevel;
    public int currentChapterID;
    public bool playTutorial = false;
    public bool stopZoomAndPan = false;

    private float gameOverDelay = 1;

    [SerializeField] internal int maxLives;
    public int maxHintCount;
    public int hintCount;

    public Vector2 tutorialPoint;

    [SerializeField] bool testLevel = false;
    [SerializeField] int testLevelNo = 0;

    [SerializeField] LoadingJsonData loadingJsonData;

    private Coroutine gameTimerCoroutine;
    private int gameTimer = 0;
    public Action<int> onGameTimerUpdate;

    //In game analytics properties
    private int hintsUsed = 0;

    public CatalogDownloadManager catalogDownloadManager;
    public CatalogData catalogData;

    private async void Awake()
    {
        if (PlayerPrefs.GetInt("Music", 1) == 1) AudioManager.Instance.PlayMusic(); // incase back from game play

        state = GameState.Loading;
        currentLevel = PlayerPrefs.GetInt("Level", 1);
        currentChapterID= PlayerPrefs.GetInt("Chapter", 1);
        if (testLevel)
        {
            currentLevel = testLevelNo;
        }
        playTutorial = !PlayerPrefs.HasKey("tutorial");

        SettingsManager.Instance.onConfigDownloaded += GetSettingData;
        //SettingsManager.Instance.onConfigDownloaded += DownloadLevel;
        SettingsManager.Instance.onConfigDownloaded += PseudoStart;
        loadingJsonData.onGameplayStarted += OnGameplayStart;
    }

    private async void PseudoStart()
    {
        CatalogDownloadManager.CatalogDownloaded += UIManager.Instance.mainMenuPanel.InitMainMenuChaptersUI;
        CatalogDownloadManager.CatalogDownloaded += UIManager.Instance.collectionPanel.InitUI;

        // Play tutorial if not played else take to home page
        if (playTutorial)
        {
            await WaitForInitialFileCopy(() => PlayerPrefs.HasKey("startlevels"));
            await catalogDownloadManager.DownloadAndProcessCatalog();
            PlayTutorialLevel();
            return;
        }
        else
        {
            CatalogDownloadManager.CatalogDownloaded += DownloadLevel;
            await LoadMetaData();
        }
    }

    private async void DownloadLevel()
    {
        await WaitForInitialFileCopy(() => PlayerPrefs.HasKey("startlevels"));

        if (currentLevel <= catalogData.levelsInfo.Count)
        {
            UIManager.Instance.retryButton.onClick.AddListener(() =>
            {
                UIManager.Instance.noInernetPanel.DeactivatePanel();
                DownloadLevel();
            });
            await FTDSaveLoadService.DownloadLevel(catalogData.levelsInfo[currentLevel - 1]);
        }
    }

    private async Task WaitForInitialFileCopy(Func<bool> condition, int checkIntervalMs = 100)
    {
        while (!condition())
        {
            await Task.Delay(checkIntervalMs);
        }
    }

    private void GetSettingData()
    {
        var internalSettingsData = SettingsManager.Instance.InternalSettingsData;
        maxHintCount = internalSettingsData.maxHintCount;
        maxLives = internalSettingsData.maxLivesCount;

        hintCount = PlayerPrefs.GetInt("Hint", maxHintCount);
    }
    internal void GameOverHandle(float animationDelay, bool _isWin = true)
    {
        //diable controlls
        UIManager.Instance.hintButton.interactable = false;
        UIManager.Instance.backButton.interactable = false;
        UIManager.Instance.FillOutOutroInfo();
        stopZoomAndPan = true;

        state = GameState.Pause;

        DOVirtual.DelayedCall(animationDelay, () =>
        {
            // if (_isWin) AudioManager.Instance.PlaySFX(AudioEvent.Outro);
            if (!_isWin)
            {
                UIManager.Instance.LivesOver();
                CrimsonEventsLogger.LogEvent(EPlayerEvent.puzzle_lost, genus: UIManager.Instance.mainMenuPanel.GameLoadSource);
                return;
            }
            CrimsonEventsLogger.LogEvent(EPlayerEvent.puzzle_complete, genus: UIManager.Instance.mainMenuPanel.GameLoadSource);
            state = GameState.GameOver;
            PlayerPrefs.SetInt("Level", ++currentLevel);
            UIManager.Instance.OnGameOver();
            Clean();
        });
    }

    // Simulating for now
    private async UniTask LoadMetaData()
    {
        CatalogDownloadManager.CatalogDownloaded += OpenMainMenu;
        await catalogDownloadManager.DownloadAndProcessCatalog();// StartLoadingAsync();
    }
    private void OpenMainMenu()
    {
        loadingJsonData.UpdateProgressText(100); // Final update for 100%
        UIManager.Instance.loaderPanel.DeactivatePanel();
        UIManager.Instance.mainMenuPanel.ActivatePanel();
        CatalogDownloadManager.CatalogDownloaded -= OpenMainMenu;
    }
    private void PlayTutorialLevel()
    {
        hintCount += 1;// make it four for FTUE
        UIManager.Instance.mainMenuPanel.ClickLoadLevel();
        //PlayerPrefs.SetInt("tutorial", 1);
    }
    public void PauseGame(bool _Pause)
    {
        if (_Pause)
        {
            state = GameState.Pause;
            stopZoomAndPan = true;
        }
        else
        {
            DOVirtual.DelayedCall(0.2f, () => state = GameState.GamePlay);
            stopZoomAndPan = false;
        }
    }

    internal void SetDebugLevel(int level)
    {
        currentLevel = testLevelNo;
    }

    private void OnGameplayStart()
    {
        gameTimerCoroutine = StartCoroutine(Timer());
    }

    private IEnumerator Timer()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(1f);
            gameTimer++;
            onGameTimerUpdate.SafeInvoke(gameTimer);
        }
    }

    public void Clean()
    {
        if (gameTimerCoroutine != null)
        {
            StopCoroutine(gameTimerCoroutine);
            gameTimerCoroutine = null;
            gameTimer = 0;
        }

        hintsUsed = 0;
    }

    #region Analytics Specific

    public Order GetGameOrder()
    {
        Order order = new Order();
        order.puzzle_mode = "Regular";
        order.time_elapsed_sec = gameTimer;
        order.hints_used = hintsUsed;
        order.lives_remaining = UIDifferenceMarker.Instance.CurrentLives;
        order.differences_remaining = UIDifferenceMarker.Instance.RemainingDifferencePoints;
        return order;
    }

    public void IncrementHintUsed()
    {
        hintsUsed++;
    }

    #endregion
}
