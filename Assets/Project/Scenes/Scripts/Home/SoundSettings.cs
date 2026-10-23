using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundSettings : MonoBehaviour
{
    [SerializeField] Toggle _toggleAudio;

    public void Setup()
    {
        gameObject.SetActive(true);
        float sfx = PlayerPrefs.GetFloat("SFX_VOLUME", 1);
        _toggleAudio.SetIsOnWithoutNotify(sfx == 1);
    }

    public void OnValueChange(bool value)
    {
        SoundManager.SfxVolume = value ? 1 : 0;
        SoundManager.Save();
    }
    public void Close()
    {
        gameObject.SetActive(false);
    }
}
