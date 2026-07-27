using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ThemeableToggle : Toggle
{
    [Header("Themeable Toggle")]
    public Image themeImage;
    public RectTransform toggleButtonRectTransform;
    public float xOff;
    public float xOn;

    protected override void Awake()
    {
        base.Awake();

        Refresh();
        ThemeManager.Instance.OnThemeChanged += (themeName) => Refresh();
    }

    public void Refresh()
    {
        if (isOn)
        {
            ToggleOn();
        }
        else
        {
            ToggleOff();
        }
    }

    public void ToggleOn()
    {
        themeImage.color = 
            ThemeManager.Instance.catalogItems.Find(x => x.itemType == ThemeableItemType.SettingsToggle && x.themeName == ThemeManager.Instance.currentTheme).color;
        toggleButtonRectTransform.DOAnchorPosX(xOn, 0.2f);
    }

    public void ToggleOff()
    {
        themeImage.color = 
            ThemeManager.Instance.catalogItems.Find(x => x.itemType == ThemeableItemType.GameScreenProgressBarBG && x.themeName == ThemeManager.Instance.currentTheme).color;
        toggleButtonRectTransform.DOAnchorPosX(xOff, 0.2f);
    }
}
