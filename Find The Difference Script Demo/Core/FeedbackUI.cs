using CrimsonGames.Analytics;
using CrimsonLibrary.SupportLibrary.UIHelperScripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class FeedbackUI : UIPanel
{
    public Button submitButtonActive;
    public TMP_InputField feedbackInputField;
    public string feedbackInputText;
    public GameObject submittedSuccessfullyObject;

    public override void OnVisible()
    {
        Debug.Log("Test");
        submitButtonActive.transform.parent.gameObject.SetActive(true);
        feedbackInputField.gameObject.SetActive(true);
    }
    public void OnTextValueChanged(string newValue)
    {
        if (!string.IsNullOrEmpty(newValue))
        {
            submitButtonActive.gameObject.SetActive(true);
        }
        else
        {
            submitButtonActive.gameObject.SetActive(false);
        }

        feedbackInputText = newValue;
    }

    public void SubmitFeedback()
    {
        AudioManager.Instance.OnButtonTap();
        EventData eventData = CrimsonEventsLogger.GetDefaultEventData();
        eventData.eventName = EPlayerEvent.suggestion_submitted.ToString();
        eventData.phylum = "suggestion";
        eventData.eventClass = null;
        eventData.order = null;
        eventData.family = feedbackInputText;
        eventData.genus = "homescreen";
        CrimsonEventsLogger.LogEvent(eventData);
        submitButtonActive.transform.parent.gameObject.SetActive(false);
        feedbackInputField.gameObject.SetActive(false);
        submittedSuccessfullyObject.SetActive(true);
        StartCoroutine(WaitForSeconds(2f));
    }

    IEnumerator WaitForSeconds(float time = 1f)
    {
        yield return new WaitForSeconds(time);
        if (IsPanelActive)
        {
            DeactivatePanel();
        }
    }

    public override void DeactivatePanel(bool ignoreUnityTimeScale = true)
    {
        UIManager.Instance.PopInNextLevelButton();
        base.DeactivatePanel(ignoreUnityTimeScale);
        submittedSuccessfullyObject.SetActive(false);
        feedbackInputField.text = string.Empty;
    }
}