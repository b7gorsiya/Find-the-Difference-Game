using CrimsonGames.CBN.Managers;
using System;
using System.Collections.Generic;

[Serializable]
public class InternalGameSettingsData
{
    public string catalogURL;
    public int prePuzzleAdsNumber;
    public int postPuzzleAdsNumber;
    public int rateUsPuzzleNumber;
    public int iapPromoPuzzleNumber;
    public bool firstSessionShowAd;
    public int rewardedHintInGameTime;
    public string gameVersion;
    public int socialMediaPuzzleNumber;

    public int maxLivesCount;
    public int maxHintCount;
}

[Serializable]
public class NotificationClickData
{
    public string title;
    public string category;
}

[Serializable]
public class NotificationClickCustomData
{
    public string notificationCategory;
}

[Serializable]
public class MongoSettings
{
    public string endpoint;
    public string dataSource;
    public string database;
    public string masterDatabase;
    public string database_production;
    public string masterDatabase_production;
    public string eventCollection;
    public string dauMasterCollection;
    public string playerMasterCollection;
    public string playerStatsCollection;
    public string apiKey;
}

[Serializable]
public class ExperimentDataSettings
{
    public List<ExperimentData> experimentData;
}