using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Intro : MonoBehaviour
{
    public void EndAnimation()
    {
        GameManager.instance.LoadScene("Main");
    }
}
