using CrimsonLibrary.SupportLibrary.UIHelperScripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CompletedCollectionInfoPanel : UIPanel
{
    public RawImage completedChapterImage;
    public TMP_Text completedChapterTitleText;
    public TMP_Text completedChapterInfoText;

    public void Show(Texture2D tex, string title, string infoText)
    {
        completedChapterImage.texture = tex;
        completedChapterTitleText.text = title;
        completedChapterInfoText.text = infoText;
        ActivatePanel();
    }

    public void Hide()
    {
        //Possible cleanup here, TODO : Confirm texture required or not
        DeactivatePanel();
    }
}
