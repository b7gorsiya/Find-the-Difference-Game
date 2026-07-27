using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EconomySettings
{
    public int joiningBonus;
    public int dailyStreakBonusBase;
    public int puzzleCompleteReward;
    public int puzzleAttemptSink;
    public int rewardedAdBonus;
    public List<int> streakDaysRewards = new List<int>();
}

public class EconomyManager : GenericManager<EconomyManager>
{
    public int currentWalletBalance;
    public bool hasUsedRewardedAd = false;

    public Action<int> onWalletBalanceUpdated;
    public Action<int> onCooldownUpdated;

    int unlockTime = 0;
    int currTime = 0;

    private Coroutine cooldownTimerCoroutine = null;

    private bool hadIssuesFetching = false;

    private void Awake()
    {
        base.Awake();
        
        //PlayFabPlayerManager.Instance.onLoginSuccess += () =>
        //{
        //    StartCoroutine(WaitForConfigCoroutine(() =>
        //    {
        //        CheckForBalance(CheckForExistingCooldown);
        //    }));
        //};
    }

    private void Start()
    {
        if (PlayerPrefs.HasKey("walletbalance"))
        {
            var balance = PlayerPrefs.GetInt("walletbalance");
            //UIManager.Instance.mainMenuUI.coinsBalanceText.text = balance.ToString();
        }
    }

    public void CheckForExistingCooldown()
    {
        if (!PlayerPrefs.HasKey("isoncooldown"))
        {
            return;
        }
        else
        {
            unlockTime = PlayerPrefs.GetInt("isoncooldown");
        }

        if(unlockTime <= 0)
        {
            return;
        }

        //currTime = (int)EventDataUtilities.GetCurrentEpochTime();
        //if (currTime < unlockTime)
        //{
        //    cooldownTimerCoroutine = StartCoroutine(StartOrResumeCooldown());
        //    LocalNotificationsManager.Instance.SendRefillNotification();
        //}
        //else
        //{
        //    if (PlayerPrefs.HasKey("isoncooldown"))
        //    {
        //        PlayerPrefs.SetInt("isoncooldown", 0);
        //        AddToWallet(SettingsManager.Instance.EconomySettingsData.puzzleAttemptSink, "[EconomyManager][CheckForExistingCooldown]");
        //    }
        //}
    }

    public void CheckAndStartCooldown(/*PlayableElement playableElement*/)
    {
        //if (!PlayerPrefs.HasKey("isoncooldown"))
        //{
        //    unlockTime = (int)EventDataUtilities.GetCurrentEpochTime() + SettingsManager.Instance.InternalSettingsData.coinsWaitTimerInSeconds;
        //    PlayerPrefs.SetInt("isoncooldown", unlockTime);
        //}
        //else
        //{
        //    unlockTime = PlayerPrefs.GetInt("isoncooldown");
        //}

        //if(unlockTime <= 0)
        //{
        //    unlockTime = (int)EventDataUtilities.GetCurrentEpochTime() + SettingsManager.Instance.InternalSettingsData.coinsWaitTimerInSeconds;
        //    PlayerPrefs.SetInt("isoncooldown", unlockTime);
        //}

        //currTime = (int)EventDataUtilities.GetCurrentEpochTime();
        //if (currTime < unlockTime)
        //{
        //    if(cooldownTimerCoroutine == null)
        //    {
        //        cooldownTimerCoroutine = StartCoroutine(StartOrResumeCooldown());
        //        LocalNotificationsManager.Instance.SendRefillNotification();
        //    }
            
        //    int diffTime = unlockTime - currTime;
        //    UIManager.Instance.mainMenuUI.notEnoughCoinsUI.Show(diffTime, playableElement);
        //}
        //else
        //{
        //    //Unlock
        //    if(PlayerPrefs.HasKey("isoncooldown"))
        //    {
        //        PlayerPrefs.SetInt("isoncooldown", 0);
        //        AddToWallet(SettingsManager.Instance.EconomySettingsData.puzzleAttemptSink, "[EconomyManager][CheckAndStartCooldown]");
        //        StopCoroutine(cooldownTimerCoroutine);
        //        cooldownTimerCoroutine = null;
        //        LocalNotificationsManager.Instance.CancelRefillNotification();
        //    }
        //}
    }

    public void StopCooldown()
    {
        if(cooldownTimerCoroutine != null)
        {
            PlayerPrefs.DeleteKey("isoncooldown");
            StopCoroutine(cooldownTimerCoroutine);
            cooldownTimerCoroutine = null;
        }
    }

    public IEnumerator StartOrResumeCooldown()
    {
        return null;
    //    while (currTime < unlockTime)
    //    {
    //        yield return new WaitForSecondsRealtime(1f);
    //        currTime = (int)EventDataUtilities.GetCurrentEpochTime();
    //        int diffTime = unlockTime - currTime;
    //        onCooldownUpdated.SafeInvoke(diffTime);
    //    }

    //    if (PlayerPrefs.HasKey("isoncooldown"))
    //    {
    //        PlayerPrefs.SetInt("isoncooldown", 0);
    //        AddToWallet(SettingsManager.Instance.EconomySettingsData.puzzleAttemptSink, "[EconomyManager][StartOrResumeCooldown]");
    //        StopCoroutine(cooldownTimerCoroutine);
    //        cooldownTimerCoroutine = null;
    //        LocalNotificationsManager.Instance.CancelRefillNotification();
    //    }
    }

    public bool CanPlayGame()
    {
        if(currentWalletBalance < SettingsManager.Instance.EconomySettingsData.puzzleAttemptSink)
        {
            return false;
        }
        return true;
    }

    IEnumerator WaitForConfigCoroutine(Action onSuccess)
    {
        yield return new WaitUntil(() => SettingsManager.Instance.IsConfigDownloaded);

        onSuccess.SafeInvoke();
    }

    public async void CheckForBalance(Action onSuccess = null)
    {
        Debug.Log("[CheckForBalance] called");

        //await MongoDBAPIManager.Instance.FetchWalletBalance(
        //    (balance) =>
        //    {
        //        Debug.Log($"FetchWalletBalance successful. Current balance: {balance}");
        //        SetCurrentWalletBalance(balance);

        //        hadIssuesFetching = false;
        //        Debug.Log("Issues fetching flag set to false.");

        //        PlayerPrefs.DeleteKey("cachedwalletbalance");
        //        Debug.Log("Deleted cached wallet balance from PlayerPrefs.");

        //        onSuccess?.Invoke();  // Null-checked for safety
        //        Debug.Log("onSuccess invoked after successful balance fetch.");
        //    },
        //    () =>
        //    {
        //        Debug.LogError("Wallet record not found, showing welcome bonus UI.");
        //        //AddToWallet(2500); // Uncomment if you want to add a specific amount to the wallet

        //        StartCoroutine(WaitForLoaderDeactivate(() =>
        //        {
        //            Debug.Log("Welcome bonus UI shown after wallet record not found.");
        //            UIManager.Instance.mainMenuUI.welcomeBonusUI.Show(SettingsManager.Instance.EconomySettingsData.joiningBonus);
        //        }));
        //    },
        //    onFirstRetry: () =>
        //    {
        //        Debug.LogWarning("First retry initiated due to an issue fetching wallet balance.");
        //        HandleFetchingIssues(() =>
        //        {
        //            Debug.Log("Handling fetching issues during the first retry.");
        //            onSuccess?.Invoke();  // Null-checked for safety
        //            Debug.Log("onSuccess invoked after handling fetching issues.");
        //        });
        //    },
        //    onError: () =>
        //    {
        //        Debug.LogError("Error occurred during FetchWalletBalance, loading balance from cache.");
        //        hadIssuesFetching = false;
        //        Debug.Log("Issues fetching flag set to false.");

        //        LoadBalanceFromCache();
        //        Debug.Log("Loaded wallet balance from cache.");

        //        onSuccess?.Invoke();  // Null-checked for safety
        //        Debug.Log("onSuccess invoked after loading balance from cache.");
        //    }
        //);

        Debug.Log("CheckForBalance finished.");
    }


    private void HandleFetchingIssues(Action fakeOnSuccess = null)
    {
        Debug.Log("[HandleFetchingIssues] Called");
        if(hadIssuesFetching)
        {
            return;
        }

        hadIssuesFetching = true;
        LoadBalanceFromCache();
        fakeOnSuccess.SafeInvoke();
    }

    private void LoadBalanceFromCache()
    {
        Debug.Log("[LoadBalanceFromCache] Called");
        currentWalletBalance = PlayerPrefs.GetInt("walletbalance", SettingsManager.Instance.EconomySettingsData.joiningBonus);
        Debug.Log("Cached wallet balance :: " +  currentWalletBalance);
        PlayerPrefs.SetInt("cachedwalletbalance", 1);
        onWalletBalanceUpdated.SafeInvoke(currentWalletBalance);
    }

    IEnumerator WaitForLoaderDeactivate(Action onSuccess)
    {
        return null;
        //yield return new WaitUntil(() => !UIManager.Instance.loaderUI.IsPanelActive);
        //onSuccess.SafeInvoke();
    }

    void SetCurrentWalletBalance(int walletBalance)
    {
        currentWalletBalance = walletBalance;
        PlayerPrefs.SetInt("walletbalance", currentWalletBalance);
        onWalletBalanceUpdated.SafeInvoke(currentWalletBalance);
    }

    public async void AddToWallet(int value, string callingSourceForDebug, Action onSuccess = null)
    {
        //await MongoDBAPIManager.Instance.UpdateWallet(value, () =>
        //{
        //    Debug.Log("Wallet update successfull, called from :: " + callingSourceForDebug);
        //    SetCurrentWalletBalance(currentWalletBalance + value);
        //    onSuccess.SafeInvoke();
        //});
    }

    public async void SubtractFromWallet(int value)
    {
        //await MongoDBAPIManager.Instance.UpdateWallet(-value, () =>
        //{
        //    SetCurrentWalletBalance(currentWalletBalance - value);
        //});
    }
}
