using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CollectionItem : ExpandCard
{
    [Header("Other Properties")]
    public GameObject expandImage;
    public GameObject shrinkImage;
    public Button shrinkLayoutButton;
    public Button expandedLayoutButton;

    [Space]

    [Header("Expand View Properties")]
    public GameObject expand_UnCompleteUI;
    public GameObject expand_CompletedUI;
    public TextMeshProUGUI expand_UnCompleted_ChapterTitle;
    public TextMeshProUGUI expand_UnCompleted_ChapterStats;
    public TextMeshProUGUI expand_Completed_ChapterTitle;
    public TextMeshProUGUI expand_Completed_ChapterStats;
    public RawImage episodeImage_GrayOut;
    public RawImage episodeImage_Completed;
    public Button playBtn;

    [Space]
    [Header("Shrink View Properties")]
    public GameObject shrink_CompletedUI;
    public GameObject lockedUI;
    public GameObject unlockedUI;
    public TextMeshProUGUI shrink_Completed_ChapterTitle;
    public TextMeshProUGUI shrink_Completed_ChapterStats;
    public TextMeshProUGUI shrink_UnLocked_ChapterTitle;
    public TextMeshProUGUI shrink_UnLocked_ChapterStats;
    public TextMeshProUGUI shrink_Locked_ChapterTitle;
    public TextMeshProUGUI shrink_Locked_ChapterStats;

    public bool isUnlocked = true;
    public bool isCompleted = false;


    public GameObject expandBtn;

    public ChapterInfo info;

    public Action Initialized;
    private void OnEnable()
    {
        ChapterLockedUI();
        ExpandCard.Expand += ExpandFun;
        playBtn.onClick.AddListener(() => UIManager.Instance.mainMenuPanel.ClickLoadLevel());
    }
    private void OnDisable()
    {
        ExpandCard.Expand -= ExpandFun;
        playBtn.onClick.RemoveAllListeners();
    }
    public void ExpandFun()
    {
        AudioManager.Instance.PlaySFX(AudioEvent.ButtonTap);

        if (isExpanded)
        {
            expandImage.SetActive(isExpanded);
            shrinkImage.SetActive(!isExpanded);
            episodeImage_GrayOut.DOFade(0.3f, animationTime);
            episodeImage_Completed.DOFade(1, animationTime);
            expand_UnCompleted_ChapterTitle.gameObject.GetComponent<RectTransform>().DOAnchorPosY(0, animationTime);

            shrinkImage.GetComponent<Image>().DOFade(0, animationTime);
        }
        else
        {
            episodeImage_GrayOut.DOFade(0, animationTime);
            episodeImage_Completed.DOFade(0, animationTime);
            expand_UnCompleted_ChapterTitle.gameObject.GetComponent<RectTransform>().DOAnchorPosY(142, animationTime);

            shrinkImage.GetComponent<Image>().DOFade(1, animationTime);

            DOVirtual.DelayedCall(animationTime, () =>
            {
                expandImage.SetActive(isExpanded);
                shrinkImage.SetActive(!isExpanded);
            });
        }
    }

    public async void Init(ChapterInfo _info)
    {
        info = _info;
        shrink_Locked_ChapterTitle.text = expand_Completed_ChapterTitle.text = expand_UnCompleted_ChapterTitle.text = shrink_Completed_ChapterTitle.text = shrink_UnLocked_ChapterTitle.text = _info.title;
        UpdateStatText(0);
        if(string.IsNullOrEmpty(info.collectionImage))
        {
            Initialized?.Invoke();
            // return incase no image added 
            return;
        }
        var tex = LoadingJsonData.Instance.DownloadTexture(info.collectionImage);
        if (episodeImage_Completed.texture != null)
        {
            Destroy(episodeImage_Completed.texture);
        }
        episodeImage_Completed.texture = await tex;
        episodeImage_GrayOut.texture = episodeImage_Completed.texture;
        Initialized?.Invoke();
    }

    public void ChapterUnlockedUI()
    {
        expandImage.SetActive(isExpanded);
        expand_UnCompleteUI.SetActive(true);
        expand_CompletedUI.SetActive(false);
        playBtn.gameObject.SetActive(true);
        shrink_CompletedUI.SetActive(false);

        lockedUI.SetActive(false);
        unlockedUI.SetActive(true);
        expandBtn.SetActive(true);

        expandedLayoutButton.onClick.RemoveAllListeners();
        expandedLayoutButton.onClick.AddListener(() =>
        {
            UIManager.Instance.mainMenuPanel.ClickLoadLevel();
            AudioManager.Instance.PlaySFX(AudioEvent.ButtonTap);
        });
    }
    public void ChapterLockedUI()
    {
        expandImage.SetActive(false);
        lockedUI.SetActive(true);
        unlockedUI.SetActive(false);
        expandBtn.SetActive(false);
    }
    public void ChapterCompletedUI()
    {
        expandImage.SetActive(isExpanded);
        expand_UnCompleteUI.SetActive(false);
        expand_CompletedUI.SetActive(true);
        playBtn.gameObject.SetActive(false);
        shrink_CompletedUI.SetActive(true);

        lockedUI.SetActive(false);
        unlockedUI.SetActive(true);
        expandBtn.SetActive(true);

        UpdateStatText(info.no_Of_Episodes);

        expandedLayoutButton.onClick.RemoveAllListeners();
        expandedLayoutButton.onClick.AddListener(() =>
        {
            UIManager.Instance.completedCollectionInfoPanel.Show(episodeImage_Completed.texture as Texture2D, info.title, info.claimText);
            AudioManager.Instance.PlaySFX(AudioEvent.ButtonTap);
        });
    }

    internal void UpdateStatText(int completedEpisode)
    {
        shrink_Locked_ChapterStats.text = shrink_UnLocked_ChapterStats.text = expand_Completed_ChapterStats.text = shrink_Completed_ChapterStats.text = expand_UnCompleted_ChapterStats.text = completedEpisode + "/" + info.no_Of_Episodes;
    }
}
