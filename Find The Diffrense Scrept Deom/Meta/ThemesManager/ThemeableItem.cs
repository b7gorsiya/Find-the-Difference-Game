using CrimsonLibrary.SupportLibrary.Extensions;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class ThemeableItem : MonoBehaviour, IThemeable
{
    public ThemeType themeType;
    public ThemeableItemType itemType;

    Graphic baseGraphic;

    public bool applyThemeOnAwake;

    void Awake()
    {
        baseGraphic = GetComponent<Graphic>();
        ThemeManager.Instance.OnThemeChanged += ApplyTheme;

        if (applyThemeOnAwake)
        {
            ApplyTheme(ThemeManager.Instance.currentTheme);
        }
    }

    void OnEnable()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (!gameObject.activeSelf)
        {
            return;
        }

        ApplyTheme(ThemeManager.Instance.currentTheme);
    }

    public void ApplyTheme(ThemeName themeName)
    {
        if(baseGraphic == null)
        {
            return;
        }

        if(!gameObject.activeSelf)
        {
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        switch (themeName)
        {
            case ThemeName.Base:
                StartCoroutine(ApplyBaseThemeProxy());
                break;
            case ThemeName.Dark:
                StartCoroutine(ApplyDarkThemeProxy());
                break;
            case ThemeName.Warm:
                StartCoroutine(ApplyWarmThemeProxy());
                break;
            default:
                break;
        }
    }

    IEnumerator ApplyBaseThemeProxy()
    {
        yield return new WaitUntil(() => ThemeManager.Instance.CatalogLoaded == true);
        ApplyBaseTheme();
    }

    IEnumerator ApplyDarkThemeProxy()
    {
        yield return new WaitUntil(() => ThemeManager.Instance.CatalogLoaded == true);
        ApplyDarkTheme();
    }

    IEnumerator ApplyWarmThemeProxy()
    {
        yield return new WaitUntil(() => ThemeManager.Instance.CatalogLoaded == true);
        ApplyWarmTheme();
    }

    void ApplyBaseTheme()
    {
        var colorToApply = ThemeManager.Instance.catalogItems.Find(x => x.itemType == itemType && x.themeName == ThemeName.Base).color;
        if(colorToApply != null) 
        {
            baseGraphic.color = colorToApply;
        }
        else
        {
            Debug.LogError("Error finding color for themeable item, check catalog for any issues");
        }
    }

    void ApplyDarkTheme()
    {
        var colorToApply = ThemeManager.Instance.catalogItems.Find(x => x.itemType == itemType && x.themeName == ThemeName.Dark).color;
        if(colorToApply != null)
        {
            baseGraphic.color = colorToApply;
        }
        else
        {
            Debug.LogError("Error finding color for themeable item, check catalog for any issues");
        }
    }

    void ApplyWarmTheme()
    {
        var colorToApply = ThemeManager.Instance.catalogItems.Find(x => x.itemType == itemType && x.themeName == ThemeName.Warm).color;
        if (colorToApply != null)
        {
            baseGraphic.color = colorToApply;
        }
        else
        {
            Debug.LogError("Error finding color for themeable item, check catalog for any issues");
        }
    }
}
