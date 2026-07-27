using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

[RequireComponent(typeof(Gradient2))]
public class ThemeableGradient : MonoBehaviour
{
    public UnityEngine.Gradient baseGradient = new UnityEngine.Gradient() { colorKeys = new GradientColorKey[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.white, 1) } };
    public UnityEngine.Gradient darkGradient = new UnityEngine.Gradient() { colorKeys = new GradientColorKey[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.white, 1) } };
    public UnityEngine.Gradient warmGradient = new UnityEngine.Gradient() { colorKeys = new GradientColorKey[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.white, 1) } };

    public bool applyThemeOnAwake;

    public Gradient2 baseGradientComponent;

    private void Awake()
    {
        if(applyThemeOnAwake) 
        {
            ApplyTheme(ThemeManager.Instance.currentTheme);
        }
    }

    private void OnEnable()
    {
        ThemeManager.Instance.OnThemeChanged += ApplyTheme;
    }

    private void ApplyTheme(ThemeName themeName)
    {
        //throw new NotImplementedException();
        switch (themeName)
        {
            case ThemeName.Base:
                baseGradientComponent.EffectGradient = baseGradient;
                break;
            case ThemeName.Dark:
                baseGradientComponent.EffectGradient = darkGradient;
                break;
            case ThemeName.Warm:
                baseGradientComponent.EffectGradient = warmGradient;
                break;
            default:
                break;
        }
    }

    private void OnDisable()
    {
        ThemeManager.Instance.OnThemeChanged -= ApplyTheme;
    }
}
