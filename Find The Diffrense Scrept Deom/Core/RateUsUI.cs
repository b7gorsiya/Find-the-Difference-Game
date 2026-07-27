using CrimsonLibrary.SupportLibrary.UIHelperScripts;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using CrimsonGames.Analytics;

public class RateUsUI : UIPanel
{
    public List<Button> starButtons = new List<Button>();
    public List<Image> inactiveOverlays = new List<Image>();
    public Button activeSubmitButton;
    public Color originalInactiveColor;

    public override void ActivatePanel(bool ignoreUnityTimeScale = true)
    {
        base.ActivatePanel(ignoreUnityTimeScale);

        for (int i = 0; i < starButtons.Count; i++)
        {
            var button = starButtons[i];
            button.onClick.RemoveAllListeners();
            int index = i;
            button.onClick.AddListener(() => Rate(index));
        }
    }
   
    public void Rate(int index)
    {
        AudioManager.Instance.PlaySFX(AudioEvent.Tap);

        foreach (var item in inactiveOverlays)
        {
            item.enabled = true;
           // item.color = originalInactiveColor;
        }

        for (int i = 0; i < index + 1; i++)
        {
            inactiveOverlays[i].enabled = false;
           // var inactiveOverlay = inactiveOverlays[i];
           // inactiveOverlay.color = starButtons[i].GetComponent<Image>().color;
            //inactiveOverlay.ChangeAlpha(1f);
        }

        activeSubmitButton.gameObject.SetActive(true);

        activeSubmitButton.onClick.RemoveAllListeners();
        activeSubmitButton.onClick.AddListener(() => DeactivatePanel());
        if (index >= 4)
        {
            activeSubmitButton.onClick.AddListener(() =>
            {
                AudioManager.Instance.OnButtonTap();
                UIManager.Instance.PopInNextLevelButton();
                ShowRateUsNative();
                DeactivatePanel();
            });
            activeSubmitButton.onClick.AddListener(() => CrimsonEventsLogger.LogEvent(EPlayerEvent.inGame_rated, family: 5));
        }
        else
        {
            activeSubmitButton.onClick.AddListener(OpenFeedback);
            activeSubmitButton.onClick.AddListener(() => CrimsonEventsLogger.LogEvent(EPlayerEvent.inGame_rated, family: index + 1));
        }
    }

    public void OpenFeedback()
    {
        UIManager.Instance.feedbackUI.ActivatePanel();
    }

    public void ShowRateUsNative()
    {
        RateApp.Instance.RateAndReview();
    }
}