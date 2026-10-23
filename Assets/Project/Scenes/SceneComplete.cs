using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneComplete : PopupBase
{
    public override void OnClickClose()
    {
        base.OnClickClose();
        if(PopupYesNo.instance != null)
        {
            PopupYesNo.instance.OnClickClose();
        }
    }
}
