using CrimsonLibrary.SupportLibrary.UIHelperScripts;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OutroAnimation : MonoBehaviour
{
    public Slider chapterStatSlider;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI statText;

    public UIPanel claimPanel;

    Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayOutroSFX()
    {
        AudioManager.Instance.PlaySFX(AudioEvent.Outro);
    }
    public void PlayRateUsOutroSFX()
    {
        AudioManager.Instance.PlaySFX(AudioEvent.Outro_RateUs);
    }
    public void OnCompleteOutro()
    {
        UIManager.Instance.ShowRatePopUp();
    }
    public void PopUpNextLevelButton()
    {
        UIManager.Instance.PopInNextLevelButton();
    }

    public void ChapterStatAnimation()
    {
        ChapterInfo info = UIManager.Instance.collectionPanel.collectionItems[GameManager.Instance.currentChapterID - 1].info;

        int totalEpisode = info.no_Of_Episodes;
        int completedEpisode = GameManager.Instance.catalogData.levelsInfo[GameManager.Instance.currentLevel - 2].episodeId;

        statText.text = completedEpisode + "/" + totalEpisode;

        if (completedEpisode == totalEpisode)
        {
            // chapter complete UI
            animator.enabled = false;
            DOVirtual.DelayedCall(0.2f, () => chapterStatSlider.DOValue((1f / (float)totalEpisode) * (float)completedEpisode, 0.5f).OnComplete(() =>
            {
                DOVirtual.DelayedCall(0.2f, () =>
                {
                    Debug.Log("Chapter Complete");
                    AudioManager.Instance.PlaySFX(AudioEvent.OutroCardGrant);
                    UIManager.Instance.outroChapterImage.transform.parent.parent.gameObject.SetActive(false);
                    gameObject.GetComponent<UIPanel>().DeactivatePanel();//.DOFade(0, 0.2f);
                    claimPanel.ActivatePanel();
                });
            }));
        }
        else
        {
            chapterStatSlider.DOValue((1f / (float)totalEpisode) * (float)completedEpisode, 0.5f);
        }
    }
}
