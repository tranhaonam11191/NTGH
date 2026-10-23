using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponScripts : MonoBehaviour
{
    public bool rotate;

    [SerializeField] ParticleSystem m_effEnd, m_effStart;

    [SerializeField] GameObject m_imgWeapon;

    [SerializeField] Vector3 posStart, localPosStart;
    
    [SerializeField] string _audioText;

    private void Start()
    {        
        posStart = transform.position;
        localPosStart = m_imgWeapon.transform.localPosition;
        m_imgWeapon.SetActive(false);
    }
    bool isMove;
    // Start is called before the first frame update
    public void Move(Vector3 target)
    {
        if(!isMove)
        {
            SoundManager.instance.PlaySfx(_audioText);
            isMove = true;
            transform.position = posStart; Debug.Log("transform.position: " + transform.position);
            m_effStart.Play();
            m_imgWeapon.transform.localPosition = localPosStart;
            m_imgWeapon.transform.localEulerAngles = Vector3.zero;
            m_imgWeapon.transform.localScale = Vector3.zero;
            m_imgWeapon.SetActive(true);
            m_imgWeapon.transform.DOScale(Vector3.one, .75f);
            rotate = true;
            transform.DOMove(target, .75f).SetEase(Ease.Flash).OnComplete(() =>
            {
                rotate = false;
                m_effEnd.Play();
                GameManager.instance.PlayAudio("Hit");
                //m_imgWeapon.SetActive(false);
                StartCoroutine(coEndMove());
            });
        }        
    }

    IEnumerator coEndMove()
    {
        yield return new WaitForSeconds(.3f);
        isMove = false;
        m_imgWeapon.SetActive(false);
    }

    [SerializeField] float valueRotation;
    // Update is called once per frame
    void Update()
    {
        if (rotate && m_imgWeapon != null)
        {
            m_imgWeapon.transform.localEulerAngles -= new Vector3(0, 0, valueRotation * Time.deltaTime);
        }
    }


}
