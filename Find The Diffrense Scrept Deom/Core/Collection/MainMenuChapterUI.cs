using CrimsonLibrary.SupportLibrary.UIHelperScripts;
using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuChapterUI : UIPanel
{
    public Image progressImage;
    public RawImage chapterImage;
    public Image glowImage;
    public TextMeshProUGUI chapterTittle;

    public ChapterInfo info;

    public GameObject nextChapter;
    public bool isCompleted = false;

    Texture2D iconTexture;

    public Action Initialized;
    public async void Init(ChapterInfo _info)
    {
        info = _info;
        chapterTittle.text = _info.title;
        if (!string.IsNullOrEmpty(info.progressImage))
        {
            if (iconTexture != null)
            {
                Destroy(iconTexture);
            }
            var tex = LoadingJsonData.Instance.DownloadTexture(info.progressImage);
            iconTexture = await tex;
            if (iconTexture != null)
            {

                chapterImage.texture = iconTexture;
            }
            Debug.Log($"load tex {_info.id}");
        }
        Initialized?.Invoke();
        // UpdateProgress(0);
    }

    public void UpdateProgress(int _episodeNo)
    {
        float fillAmount = (1f / (float)info.no_Of_Episodes) * (float)_episodeNo;
        Debug.Log("Fill amount :" + fillAmount);
        if (_episodeNo < info.no_Of_Episodes)
        {
            progressImage.DOFillAmount(fillAmount, 0.5f);
            //do normal transition
        }
        else
        {
            UIManager.Instance.nextLevelBtn.gameObject.SetActive(false);
            // perform chapter complete
            progressImage.DOFillAmount(fillAmount, 0.5f).OnComplete(() =>
            {
                isCompleted = true; // just for flag
                glowImage.DOFade(1, 0.5f).OnComplete(() =>
                {
                    DOVirtual.DelayedCall(0.3f, () =>
                    {
                        this.gameObject.GetComponent<RectTransform>().DOAnchorPosX(-1000, 1);
                        PlayerPrefs.SetInt("Chapter", ++GameManager.Instance.currentChapterID);
                        Debug.Log("ChapterUpdated **");
                        if (nextChapter != null)
                        {
                            nextChapter.SetActive(true);
                            nextChapter.GetComponent<RectTransform>().DOAnchorPosX(0, 1).OnComplete(() => UIManager.Instance.nextLevelBtn.gameObject.SetActive(true));
                        }
                    });
                });
            });


        }
    }
}
