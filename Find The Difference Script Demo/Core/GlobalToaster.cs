using CrimsonLibrary.SupportLibrary.UIHelperScripts;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GlobalToaster : UIPanel
{
    public TMP_Text toasterText;
    public float deactivateTime = 2f;

    public void Awake()
    {
        OnActivate.AddListener(() =>
        {
            StartCoroutine(Hide());
        });
    }

    public void ShowToaster(string text)
    {
        toasterText.text = text;
        ActivatePanel();
    }

    IEnumerator Hide()
    {
        yield return new WaitForSecondsRealtime(deactivateTime);
        DeactivatePanel();
    }
}
