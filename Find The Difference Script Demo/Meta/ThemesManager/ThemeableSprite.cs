using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ThemeableSprite : MonoBehaviour, IThemeable
{
    Image baseImage;

    public Sprite baseSprite;
    public Sprite darkSprite;
    public Sprite warmSprite;

    public bool applyThemeOnAwake;

    void Awake()
    {
        baseImage = GetComponent<Image>();

        if (applyThemeOnAwake)
        {
            ApplyTheme(ThemeManager.Instance.currentTheme);
        }
    }

    void OnEnable()
    {
        ThemeManager.Instance.OnThemeChanged += ApplyTheme;

        if (applyThemeOnAwake)
        {
            ApplyTheme(ThemeManager.Instance.currentTheme);
        }
    }

    public void ApplyTheme(ThemeName themeName)
    {
        switch (themeName)
        {
            case ThemeName.Base:
                baseImage.sprite = baseSprite;
                break;
            case ThemeName.Dark:
                baseImage.sprite = darkSprite;
                break;
            case ThemeName.Warm:
                baseImage.sprite = warmSprite;
                break;
            default:
                break;
        }
    }

    void OnDisable()
    {
        ThemeManager.Instance.OnThemeChanged -= ApplyTheme;
    }
}
