using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;


public class GoldItem : MonoBehaviour
{
    [SerializeField] GameObject Text;


    private void Awake()
    {
        Text.SetActive(false);
    }

    public void InCreaseValue()
    {        
        Text.transform.localPosition = Vector3.zero;
        Text.transform.localScale = Vector3.one;
        Text.transform.parent = null;
        Text.SetActive(true);
        Text.transform.DOMoveY(2f, 1f).OnComplete(() => Destroy(Text));
    }
}
