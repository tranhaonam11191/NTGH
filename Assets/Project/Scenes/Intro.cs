using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS.UI;

public class Intro : MonoBehaviour
{
    public void EndAnimation()
    {
        SS.UI.Core.Load<MainController>("Main");
    }
}
