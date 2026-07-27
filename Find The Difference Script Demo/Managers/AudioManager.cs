using CrimsonLibrary.SupportLibrary.Utils.Generics;
using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : GenericManager<AudioManager>
{
    [Header("Audio Settings")]
    [SerializeField] private int poolSize = 10;
    [SerializeField] private float musicFadeDuration = 1.0f;

    [Header("Volume Controls")]
    [Range(0, 1)] public float musicVolume = 0.5f;
    [Range(0, 1)] public float sfxVolume = 0.7f;

    [Header("Audio Clips")]
    public AudioClip backgroundMusic;

    [Serializable]
    public struct EventAudioClip
    {
        public AudioEvent audioEvent;
        public AudioClip clip;
    }

    [Header("Event-Based Audio Clips")]
    public EventAudioClip[] eventClips;

    private AudioSource musicSource;
    private Queue<AudioSource> sfxPool;
    private Dictionary<AudioEvent, AudioClip> audioEventClipMap;

    private bool isMusicEnabled = true;
    private bool isSFXEnabled = true;

    public bool IsMusicEnabled
    {
        get => isMusicEnabled;
        set
        {
            isMusicEnabled = value;
            if (!isMusicEnabled)
                StopMusic();
            else if (!musicSource.isPlaying && backgroundMusic != null && GameManager.Instance.state==GameManager.GameState.GamePlay)
                PlayMusic();
        }
    }

    public bool IsSFXEnabled
    {
        get => isSFXEnabled;
        set => isSFXEnabled = value;
    }

    private void Awake()
    {
        InitializeAudioSources();
        InitializeEventClipMap();
    }

    private void InitializeAudioSources()
    {
        // Create a dedicated AudioSource for music
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = musicVolume;

        // Create a pool of AudioSources for SFX
        sfxPool = new Queue<AudioSource>();
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
            sfxPool.Enqueue(sfxSource);
        }
    }

    private void InitializeEventClipMap()
    {
        audioEventClipMap = new Dictionary<AudioEvent, AudioClip>();

        foreach (var eventClip in eventClips)
        {
            if (!audioEventClipMap.ContainsKey(eventClip.audioEvent))
            {
                audioEventClipMap[eventClip.audioEvent] = eventClip.clip;
            }
        }
    }

    private void OnEnable()
    {
        GameManager.LevelStart += () => PlaySFX(AudioEvent.LevelStart); 
       // GameManager.LevelComplete += LevelFinishSFX;
        UIDifferenceMarker.mark += ()=>PlaySFX(AudioEvent.Mark);
        UIDifferenceMarker.wrongMark += () => PlaySFX(AudioEvent.WrongMark);

    }

    private void OnDestroy()
    {
        GameManager.LevelStart -= () => PlaySFX(AudioEvent.LevelStart);
        //GameManager.LevelComplete -= LevelFinishSFX;
        UIDifferenceMarker.mark -= () => PlaySFX(AudioEvent.Mark);
        UIDifferenceMarker.wrongMark -= () => PlaySFX(AudioEvent.WrongMark);
    }

    
    public void PlayMusic()
    {
        if (!isMusicEnabled) return;

        if (backgroundMusic != null)
        {
            StartCoroutine(FadeMusic(backgroundMusic));
        }
        else
        {
            Debug.LogWarning($"Music track not found.");
        }
    }

    private System.Collections.IEnumerator FadeMusic(AudioClip newClip)
    {
        // Fade out current music
        float currentVolume = musicVolume;
        while (currentVolume > 0)
        {
            currentVolume -= Time.deltaTime / musicFadeDuration * musicVolume;
            musicSource.volume = Mathf.Max(currentVolume, 0);
            yield return null;
        }

        musicSource.clip = newClip;
        if (isMusicEnabled) musicSource.Play();

        // Fade in new music
        currentVolume = 0;
        while (currentVolume < musicVolume)
        {
            currentVolume += Time.deltaTime / musicFadeDuration * musicVolume;
            musicSource.volume = Mathf.Min(currentVolume, musicVolume);
            yield return null;
        }
    }

    public void PlaySFX(AudioEvent audioEvent)
    {
        if (!isSFXEnabled) return;

        if (audioEventClipMap.TryGetValue(audioEvent, out AudioClip clip))
        {
            AudioSource sfxSource = GetAvailableSFXSource();
            sfxSource.clip = clip;
            sfxSource.Play();
        }
        else
        {
            Debug.LogWarning($"Audio clip for event '{audioEvent}' not found.");
        }
    }

    private AudioSource GetAvailableSFXSource()
    {
        AudioSource source = sfxPool.Dequeue();
        sfxPool.Enqueue(source);
        return source;
    }

    public void StopMusic()
    {
        if (musicSource.isPlaying)
            musicSource.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        musicSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        foreach (var source in sfxPool)
        {
            source.volume = volume;
        }
    }
    public void LevelFinishSFX(bool _win,bool _playRateUsOutro)
    {
        if (_win)
        {
            PlaySFX((!_playRateUsOutro)?AudioEvent.Outro:AudioEvent.Outro_RateUs);
        }
    }
    public void OnButtonTap() => PlaySFX(AudioEvent.ButtonTap);
    public void OpenPopUp() => PlaySFX(AudioEvent.PopUpOpen);
    public void ClosePopUp() => PlaySFX(AudioEvent.PopUpClose);
}

public enum AudioEvent
{
    GameStart,
    LevelStart,
    LivesOver,
    Mark,
    WrongMark,
    Outro,
    Outro_RateUs,
    NextLevelButton,
    Tap,
    PopUpOpen,
    PopUpClose,
    ButtonTap,
    OutroCardGrant,
    ClaimCard
}
