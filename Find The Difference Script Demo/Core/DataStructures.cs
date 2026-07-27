using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum LevelInfoCountry
{
    India,
}
[Serializable]
public enum LevelInfoChapters
{
    Mumbai,
    Delhi,
    Chennai,
    Kolkata,
    Hyderabad,
    Bengaluru
}
[Serializable]
public class CatalogData
{
    public List<ChapterInfo> chaptersInfo;
    public List<LevelInfo> levelsInfo;
}
[Serializable]
public class LevelInfo
{
    public string imageId;
    public string url;
    public int levelNo;
    public int episodeId;
    public int chapterId;
    public LevelInfoCountry country;
}
[Serializable]
public class ChapterInfo
{
    public int id;
    public string title;
    public int no_Of_Episodes;
    public string claimText;
    public string collectionImage=string.Empty;
    public string progressImage = string.Empty;
}
[Serializable]
public class LevelData
{
    [HideInInspector] public string image1_Base64 = string.Empty;
    [HideInInspector] public string image2_Base64 = string.Empty;
    public List<MarkedDifferenceData> points;
    public int chapterId;
    public int episodeIndex;
    //public LevelInfoChapters chapterTitle;
    //public LevelInfoCountry country;
}
[Serializable]
public class MarkedDifferenceData
{
    public Vector2 point;
    public float scale = 1.0f;
}
