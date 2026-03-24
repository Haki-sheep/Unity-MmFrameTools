/// <summary>
/// NewTestUI113 Logic层 - 用户编写
/// </summary>

using MieMieFrameWork;
using MieMieFrameWork.UI;
using UnityEngine;
using UnityEngine.UI;

internal class NewTestUI113 : UIWindowBase
{
    internal NewTestUI113Gen View { get; private set; }

    internal protected override void OnAwake()
    {
        base.OnAwake();
        View = UIContent.GetComponent<NewTestUI113Gen>();
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
