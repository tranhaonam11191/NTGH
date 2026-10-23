using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static float DefaultBgmVolume = 1;
    public static float DefaultSfxVolume = 1;
    public static float DefaultVoiceVolume = 1;
    public static float DefaultBgmVolumeScale = 1f;
    public static float DefaultSfxVolumeScale = 1;
    public static float DefaultVoiceVolumeScale = 1;

    static float bgmVolume = -1;
    static float sfxVolume = -1;
    static float voiceVolume = -1;

    static float bgmVolumeScale = 1;

    public static float BgmVolume
    {
        get
        {            
            return bgmVolume;
        }

        set
        {
            bgmVolume = value;            
        }
    }

    public static float SfxVolume
    {
        get
        {            
            return sfxVolume;
        }

        set
        {
            sfxVolume = value;
        }
    }    

    public static void Save()
    {
        PlayerPrefs.SetFloat("BGM_VOLUME", BgmVolume);
        PlayerPrefs.SetFloat("SFX_VOLUME", SfxVolume);        
        PlayerPrefs.Save();
    }

    [SerializeField] string m_BgmPath = "Sounds/Bgm";
    [SerializeField] string m_SfxPath = "Sounds/Sfx";
    [SerializeField] string m_VoicePath = "Sounds/Voice";
    [SerializeField] string m_ButtonTapSfx = "sfx_button_tap";
    [SerializeField] AudioSource m_BgmSource;
    [SerializeField] AudioSource m_SfxSource;    

    Dictionary<string, AudioClip> m_AudioDict = new Dictionary<string, AudioClip>();
    bool m_PauseBgm;
    public AudioSource BGM { get { return m_BgmSource; } }

    public string sfxPath
    {
        get { return m_SfxPath; }
    }

    public string bgmPath
    {
        get { return m_BgmPath; }
    }    

    public void PlayButtonTapSfx()
    {
        PlaySfx(m_ButtonTapSfx);
    }    

    public void PlaySfx(AudioClip audioClip, float volumeScale = 1f)
    {
        m_SfxSource.PlayOneShot(audioClip, SfxVolume * DefaultSfxVolumeScale * volumeScale);
    }

    public void PlaySfx(string audioName, float volumeScale = 1f, bool oneShot = true, bool isLoop = false)
    {
        CacheClip(m_SfxPath, audioName);

        m_SfxSource.volume = SfxVolume * DefaultSfxVolumeScale * volumeScale;
        if (oneShot)
        {
            m_SfxSource.PlayOneShot(m_AudioDict[audioName], m_SfxSource.volume);
        }
        else
        {
            m_SfxSource.loop = isLoop;
            m_SfxSource.clip = m_AudioDict[audioName];
            m_SfxSource.Play();
        }
    }

    public void StopSfx()
    {
        m_SfxSource.Stop();
    }  

    public void PlayBgm(string audioName, float volumeScale = 1f)
    {
        CacheBgm(audioName);

        bgmVolumeScale = volumeScale;
        m_BgmSource.volume = BgmVolume * DefaultBgmVolumeScale * bgmVolumeScale;

        bool currentClipNull = (m_BgmSource.clip == null);
        bool sameCurrentClip = (!currentClipNull && m_BgmSource.clip.name == audioName);

        if (sameCurrentClip)
        {
            if (!m_BgmSource.isPlaying)
            {
                m_BgmSource.Play();
            }
        }
        else
        {
            if (!currentClipNull)
            {
                StopBgm();
            }
            m_BgmSource.clip = m_AudioDict[audioName];
            m_BgmSource.Play();
        }
    }

    public void StopBgm(bool clearClip = false)
    {
        if (m_BgmSource.isPlaying)
        {
            m_BgmSource.Stop();
        }

        if (clearClip && m_BgmSource.clip != null)
        {
            Resources.UnloadAsset(m_BgmSource.clip);
            m_BgmSource.clip = null;
        }
    }            

    public void CacheClip(string path, string audioName)
    {
        if (!m_AudioDict.ContainsKey(audioName))
        {
            AudioClip clip = Resources.Load<AudioClip>(System.IO.Path.Combine(path, audioName));
            if (clip == null) { Debug.LogError("not found " + System.IO.Path.Combine(path, audioName)); }
            m_AudioDict.Add(audioName, clip);
        }
    }

    public void CacheSfx(params string[] array)
    {
        foreach (var item in array)
        {
            CacheSfx(item);
        }
    }

    public void CacheSfx(string audioName)
    {
        CacheClip(m_SfxPath, audioName);
    }

    public void CacheBgm(string audioName)
    {
        CacheClip(m_BgmPath, audioName);
    }    
}