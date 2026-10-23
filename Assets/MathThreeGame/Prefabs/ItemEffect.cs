using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemEffect : MonoBehaviour
{
    [SerializeField] ParticleSystem m_eff;

    public void PlayEff()
    {
        m_eff.transform.parent = null;
        m_eff.Play();
    }
}
