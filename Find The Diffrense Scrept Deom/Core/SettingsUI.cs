using CrimsonLibrary.SupportLibrary.UIHelperScripts;
using DG.Tweening;
using System.Collections;
using System.Threading;
using TMPro;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : UIPanel
{
    [Header("Settings UI")]
    [SerializeField] private Button soundToggleBtn;
    private GameObject soundButtonHandle;

    [SerializeField] private Button vibrationToggleBtn;
    private GameObject vibrationButtonHandle;

    [SerializeField] private Button musicToggleBtn;
    private GameObject musicButtonHandle;

    public TMP_Text versionInfoText;
    public TMP_Text playerIdText;

    Color ogColor;

    private void Awake()
    {
        ogColor = soundToggleBtn.GetComponent<Image>().color;

        soundButtonHandle = soundToggleBtn.transform.GetChild(1).gameObject;
        vibrationButtonHandle = vibrationToggleBtn.transform.GetChild(1).gameObject;
        musicButtonHandle = musicToggleBtn.transform.GetChild(1).gameObject;

        if (soundButtonHandle == null || vibrationButtonHandle == null || musicButtonHandle == null)
        {
            Debug.LogError("Button handles could not be found. Please ensure the hierarchy is correct.");
        }

        soundToggleBtn.onClick.AddListener(SoundToggle);
        vibrationToggleBtn.onClick.AddListener(VibrationToggle);
        musicToggleBtn.onClick.AddListener(MusicToggle);

        SetApplicationVersion();

        StartCoroutine(WaitForPlayerID());
    }

    private void OnEnable()
    {
        Init();
        // Additional logic when the panel is enabled (if needed).
    }

    private void OnDestroy()
    {
        // Remove listeners to avoid memory leaks.
        soundToggleBtn.onClick.RemoveListener(SoundToggle);
        vibrationToggleBtn.onClick.RemoveListener(VibrationToggle);
        musicToggleBtn.onClick.RemoveListener(MusicToggle);
    }
    public void GetSettings()
    {
        int sound = PlayerPrefs.GetInt("Sound", 1);
        AudioManager.Instance.IsSFXEnabled = sound == 1;

        int music = PlayerPrefs.GetInt("Music", 1);
        AudioManager.Instance.IsMusicEnabled = music == 1;
    }
    public void Init()
    {
        // Initialize sound toggle
        int sound = PlayerPrefs.GetInt("Sound", 1);
        SetToggleState(soundToggleBtn, soundButtonHandle, sound == 1, soundToggleBtn.transform.GetChild(1).GetComponent<Image>());
        AudioManager.Instance.IsSFXEnabled = sound == 1;

        // Initialize vibration toggle
        int vibration = PlayerPrefs.GetInt("Vibration", 1);
        SetToggleState(vibrationToggleBtn, vibrationButtonHandle, vibration == 1, vibrationToggleBtn.transform.GetChild(1).GetComponent<Image>());

        // Initialize music toggle
        int music = PlayerPrefs.GetInt("Music", 1);
        SetToggleState(musicToggleBtn, musicButtonHandle, music == 1, musicToggleBtn.transform.GetChild(1).GetComponent<Image>());
        AudioManager.Instance.IsMusicEnabled = music == 1;
    }
    private void SoundToggle()
    {
        int sound = PlayerPrefs.GetInt("Sound", 1);
        float currentX = soundButtonHandle.transform.localPosition.x;

        if (sound == 1)
        {
            soundButtonHandle.transform.DOLocalMoveX(currentX - 40, 0.2f);
            soundToggleBtn.transform.GetChild(0).GetComponent<Image>().DOFade(0, 0.2f);
            soundToggleBtn.GetComponent<Image>().DOFade(1, 0.2f).OnComplete(() => {
                PlayerPrefs.SetInt("Sound", 0);
                PlayerPrefs.Save();
            });
            sound = 0;
        }
        else
        {
            soundButtonHandle.transform.DOLocalMoveX(currentX + 40, 0.2f);
            soundToggleBtn.GetComponent<Image>().DOFade(0, 0.2f);
            soundToggleBtn.transform.GetChild(0).GetComponent<Image>().DOFade(1, 0.2f).OnComplete(() => {
                PlayerPrefs.SetInt("Sound", 1);
                PlayerPrefs.Save();
            });
            sound = 1;
        }
        AudioManager.Instance.IsSFXEnabled=sound == 1;
        AudioManager.Instance.OnButtonTap();
    }

    private void VibrationToggle()
    {
        int vibration = PlayerPrefs.GetInt("Vibration", 1);
        float currentX = vibrationButtonHandle.transform.localPosition.x;

        if (vibration == 1)
        {
            vibrationButtonHandle.transform.DOLocalMoveX(currentX - 40, 0.2f);
            vibrationToggleBtn.transform.GetChild(0).GetComponent<Image>().DOFade(0, 0.2f);
            vibrationToggleBtn.GetComponent<Image>().DOFade(1, 0.2f).OnComplete(() => {
                PlayerPrefs.SetInt("Vibration", 0);
                PlayerPrefs.Save();
            });
            vibration = 0;
        }
        else
        {
            vibrationButtonHandle.transform.DOLocalMoveX(currentX + 40, 0.2f);
            vibrationToggleBtn.GetComponent<Image>().DOFade(0, 0.2f);
            vibrationToggleBtn.transform.GetChild(0).GetComponent<Image>().DOFade(1, 0.2f).OnComplete(() => {
                PlayerPrefs.SetInt("Vibration", 1);
                PlayerPrefs.Save();
            });
            vibration = 1;
        }
        AudioManager.Instance.OnButtonTap();
    }

    private void MusicToggle()
    {
        int music = PlayerPrefs.GetInt("Music", 1);
        float currentX = musicButtonHandle.transform.localPosition.x;

        if (music == 1)
        {
            musicButtonHandle.transform.DOLocalMoveX(currentX - 40, 0.2f);
            musicToggleBtn.transform.GetChild(0).GetComponent<Image>().DOFade(0, 0.2f);
            musicToggleBtn.GetComponent<Image>().DOFade(1, 0.2f).OnComplete(() => {
                PlayerPrefs.SetInt("Music", 0);
                PlayerPrefs.Save();
            });
            music = 0;
        }
        else
        {
            musicButtonHandle.transform.DOLocalMoveX(currentX + 40, 0.2f);
            musicToggleBtn.GetComponent<Image>().DOFade(0, 0.2f);
            musicToggleBtn.transform.GetChild(0).GetComponent<Image>().DOFade(1, 0.2f).OnComplete(() => {
                PlayerPrefs.SetInt("Music", 1);
                PlayerPrefs.Save();
            });
            music = 1;
        }
        AudioManager.Instance.IsMusicEnabled = music == 1;
        AudioManager.Instance.PlayMusic();
        AudioManager.Instance.OnButtonTap();
    }

    private void SetToggleState(Button toggleButton, GameObject handle, bool isEnabled, Image activeImage)
    {
        float currentX = handle.transform.localPosition.x;

        float targetX = isEnabled ? 0 : -40;
        handle.transform.DOLocalMoveX(currentX + targetX, 0.2f);

        if (isEnabled)
        {
            toggleButton.GetComponent<Image>().DOFade(0, 0.2f);// = false;
            toggleButton.transform.GetChild(0).GetComponent<Image>().DOFade(1,0.2f);//.enabled = true;
        }
        else
        {
            toggleButton.GetComponent<Image>().DOFade(1, 0.2f);//.enabled = true;
            toggleButton.transform.GetChild(0).GetComponent<Image>().DOFade(0, 0.2f);//.enabled = false;
        }
        //Color targetColor = isEnabled ? activeColor : Color.grey;
        //toggleButton.GetComponent<Image>().color = targetColor;
    }

    public void SetApplicationVersion()
    {
        versionInfoText.text = "Version " + Application.version;
    }

    IEnumerator WaitForPlayerID()
    {
        yield return new WaitUntil(() => !string.IsNullOrEmpty(PlayFabPlayerManager.Instance.PlayFabId));
        playerIdText.text = PlayFabPlayerManager.Instance.PlayFabId;
    }
}
