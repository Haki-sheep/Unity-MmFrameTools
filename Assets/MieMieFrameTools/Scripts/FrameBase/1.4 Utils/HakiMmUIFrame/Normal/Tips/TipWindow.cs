using System;
using System.Collections.Generic;
using MieMieFrameWork;
using MieMieFrameWork.UI;
using UnityEngine;
using UnityEngine.UI;

public class TipWindow : UIWindowBase
{
    // View层引用
    public TipWindowLogic tipWindowLogic;

    internal protected override void OnAwake()
    {
        base.OnAwake();
        tipWindowLogic = UIContent.GetComponent<TipWindowLogic>();
        // tipWindowLogic.Init(GetUIComp<Text>("t"),GetUIComp<Image>("Bk"));
        tipWindowLogic.ChangeTipText("");
        // 获取View组件
    }

    internal protected override void OnShow()
    {
        base.OnShow();
    }


    internal protected override void OnHide()
    {
        base.OnHide();
    }

    internal protected override void OnDestroy()
    {
        base.OnDestroy();
    }

}
