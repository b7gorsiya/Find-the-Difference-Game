using CrimsonLibrary.SupportLibrary.Extensions;
using CrimsonLibrary.SupportLibrary.Utils.Generics;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using CrimsonGames.Utilities.CustomJSONConverters;
using System.IO;

public enum ThemeName
{
    Base,
    Dark,
    Warm,
}

public enum ThemeType
{
    Color,
    Font, //Maybe in the future
}

public enum ThemeableItemType //Add any custom types here, example - panel background, popup background, tile color, highlight color, etc
{
    //Common
    PanelBackground,
    ContainerBackground,
    PopupBackground,
    MainTextFontColor,
    SubTextFontColor,
    MainMenuSettingsButtonColor,

    //Game Specific
    NavbarBackground,
    NavbarIconUnselectedColor,
    NavbarIconSelectedColor,

    //Tiles
    DefaultNumberColor,
    CorrectNumberColor,
    WrongNumberColor,
    SelectedNumberColor,
    TileBaseColor,
    NumberHighlightedTileColor,
    RowColHighlightedTileColor,
    TileSelectedColor,
    IncorrectNumberHighlightedColor,
    RowCol3x3CompletedAnimationHighlightColor,

    //Difficulty Selection
    DifficultySelectionPanelBackgroundColor,
    DifficultySelectionButtonBackgroundColor,
    DifficultySelectionButtonTextColor,
    DifficultySelectionButtonIconColor,

    //Daily Challenge
    DailyChallengeCalendarBackground,
    DailyChallengeDayTextColor,
    DailyChallengeDateSelectedTextColor,

    //Stats
    StatsGameCategoryBaseText,
    StatsGameCategoryActiveText,
    StatsDifficultyButtonBgSelected,
    StatsDifficultyButtonTextSelected,
    StatsDifficultyButtonBgUnselected,
    StatsDifficultyButtonTextUnselected,
    StatsContainer,

    TopBarButton,

    //Settings
    SettingsContainer,
    SettingsText,
    SettingsIcon,
    SettingsSubText,
    SettingsLinksText,
    SettingsToggle,

    //Buttons
    Button,
    ButtonText,
    ButtonNewGameResume,
    ButtonNewGameResumeText,

    PopupTextMain,
    LanguageSelectButton,

    TileMainBackground,

    //Sudoku UI
    GameScreenTopBar,
    GameScreenTopBarButton,
    GameScreenInfoLabelText,
    GameScreenInfoActualText,
    GameScreenActionButton,
    GameScreenActionButtonText,
    GameScreenInputButtonBackground,
    GameScreenInputButtonNumberText,
    GameScreenInputButtonInstancesText,
    GameScreenInputButtonDisabledBackgroundColor,
    GameScreenInputButtonDisabledTextColor,
    GameScreenProgressBarBG,
    GameScreenProgressBarFill,
    GameScreenBoardFrame,
    TileAnimationOverlay,
    TileGameWonOverlay,
    TileDefaultPreAnimationColorNoAlpha,

    //DailyChallenge Date Element
    DCDateElementDateSelectedTextColor,
    DCDateElementDefaultTextColor,
    DCDateElementFillBGColorDefault,
    DCDateElementFillActualColorDefault,
    DCDateElementFillBGColorSelected,
    DCDateElementFillActualColorSelected,
    DCDateElementTextGreyedOutColor,

    //Game Pause
    GamePauseContainerBG,
    PauseTextAreaBG,
    PauseTipText,

    //Post Game
    PostGameXPHolder,
    PostGameXPText,
    PostGameRemoveAdButtonBG,
    PostGameRemoveButtonText,
    PostGameSmallButtons,
    PostGameGameStatsBG,
    PostGameButton,
    PostGameButtonText,
    PostGameStatsLabelText,
    PostGameStatsActualText,
    PostGameMainContainerBG,

    //Language Select
    LanguageSelectBG,
    LanguageSelectText,

    //Notes Text
    NotesText,

    MainMenuXPProgressBarBG,
}

[Serializable]
public class ThemeCatalogItem
{
    public ThemeableItemType itemType;
    public ThemeName themeName;

    [JsonConverter(typeof(ColorConverter))] // Apply the custom converter here
    public Color color;
}

public class ThemeManager : GenericManager<ThemeManager>
{
    public ThemeName currentTheme;
    public Action<ThemeName> OnThemeChanged;

    [Header("Catalog")]
    public List<ThemeCatalogItem> catalogItems;

    private bool catalogLoaded = false;

    public bool CatalogLoaded { get => catalogLoaded; }

    private void Awake()
    {
        LoadCatalog();

        if (PlayerPrefs.HasKey("theme"))
        {
            var currTheme = PlayerPrefs.GetString("theme").ToEnum<ThemeName>();
            ChangeTheme(currTheme);
        }
        else
        {
            currentTheme = ThemeName.Base;
            PlayerPrefs.SetString("theme", ThemeName.Base.ToString());
        }
    }

    public void ChangeTheme(ThemeName theme)
    {
        currentTheme = theme;
        PlayerPrefs.SetString("theme", theme.ToString());
        OnThemeChanged.SafeInvoke(currentTheme);
        Debug.Log("Current theme set to :: " + currentTheme.ToString());
    }

    //Test function
    public void SwitchThemeTest()
    {
        if(currentTheme == ThemeName.Base) 
        {
            ChangeTheme(ThemeName.Dark);
        }
        else if(currentTheme == ThemeName.Dark)
        {
            ChangeTheme(ThemeName.Warm);
        }
        else
        {
            ChangeTheme(ThemeName.Base);
        }

        //DebugLogCatalogJSON();
    }

    public void SetBaseTheme()
    {
        if(currentTheme != ThemeName.Base)
        {
            ChangeTheme(ThemeName.Base);
        }
    }

    public void SetDarkTheme()
    {
        if(currentTheme != ThemeName.Dark)
        {
            ChangeTheme(ThemeName.Dark);
        }
    }

    public void SetWarmTheme()
    {
        if(currentTheme != ThemeName.Warm)
        {
            ChangeTheme(ThemeName.Warm);
        }
    }

    public void DebugLogCatalogJSON()
    {
        Debug.Log("Catalog JSON :: \n" + JsonConvert.SerializeObject(catalogItems));
    }

    public void LoadCatalog()
    {
        string fileName = "colorCatalog";
        string fullFilePath = "";

        fullFilePath = Path.Combine(Application.streamingAssetsPath, fileName + ".json");
        string jsonData;

#if UNITY_EDITOR || UNITY_IOS || UNITY_STANDALONE
        if (!File.Exists(fullFilePath))
        {
            return;
        }

        jsonData = File.ReadAllText(fullFilePath);
#elif UNITY_ANDROID

            UnityWebRequest www = UnityWebRequest.Get(fullFilePath);
            www.SendWebRequest();
            while (!www.isDone) ;
            jsonData = www.downloadHandler.text;
#endif
        catalogItems = JsonConvert.DeserializeObject<List<ThemeCatalogItem>>(jsonData);
        catalogLoaded = true;
    }
}
