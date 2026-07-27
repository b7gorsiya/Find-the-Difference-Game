using CrimsonLibrary.SupportLibrary.UIHelperScripts;
using System.Collections.Generic;
using UnityEngine;


public class MyCollectionUI : UIPanel
{
    public Transform collectionParent;
    public GameObject collectionItem_Prefab;

    public List<CollectionItem> collectionItems = new List<CollectionItem>();
    List<ChapterInfo> chapterList;


    public void InitUI()
    {
        chapterList = GameManager.Instance.catalogData.chaptersInfo;
        foreach (var chapter in chapterList)
        {
            var _collectionItem = Instantiate(collectionItem_Prefab, collectionParent).GetComponent<CollectionItem>();
            _collectionItem.Initialized += CheckUIInit;
            _collectionItem.Init(chapter);
            _collectionItem.ChapterLockedUI();
            collectionItems.Add(_collectionItem);
        }
        //Debug.Log("Create Collection UI");
        CatalogDownloadManager.CatalogDownloaded -= InitUI;
    }

    int uiCount = 0;
    void CheckUIInit()
    {
        uiCount++;
        if (uiCount >= GameManager.Instance.catalogData.chaptersInfo.Count)
        {
            UIManager.Instance.collectionReady = true;
            Debug.Log("Colletion Ready");
        }
    }
    public void DrawUI()
    {
        int episodeCount = 0;
        foreach (var chapter in collectionItems)
        {
            if(chapter.info.no_Of_Episodes<=0)
            {
                chapter.shrink_Locked_ChapterStats.text = "Coming Soon";
                continue;
            }
            if (episodeCount < GameManager.Instance.currentLevel)
            {
                chapter.ChapterUnlockedUI();
            }
            episodeCount += chapter.info.no_Of_Episodes;

            if(episodeCount < GameManager.Instance.currentLevel)
            {
                chapter.ChapterCompletedUI();
            }else 
            {
                int completedEpisode = episodeCount - GameManager.Instance.currentLevel;
                if (completedEpisode < chapter.info.no_Of_Episodes)
                {
                    // this is the current level
                    chapter.UpdateStatText(chapter.info.no_Of_Episodes - (completedEpisode+1));
                }
            }

            if (chapter.info.id == GameManager.Instance.currentChapterID)
            {
                chapter.ToggleExpand();
            }
        }
    }

    public override void DeactivatePanel(bool ignoreUnityTimeScale = true)
    {
        base.DeactivatePanel(ignoreUnityTimeScale);
        CollapseAll();
    }

    public void CollapseAll()
    {
        foreach (var chapter in collectionItems)
        {
            if (chapter.isExpanded)
            {
                chapter.ToggleExpand();
            }
        }
    }
}
