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
        float sfx = SoundManager.SfxVolume;
        _toggleAudio.SetIsOnWithoutNotify(sfx > 0f);
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
