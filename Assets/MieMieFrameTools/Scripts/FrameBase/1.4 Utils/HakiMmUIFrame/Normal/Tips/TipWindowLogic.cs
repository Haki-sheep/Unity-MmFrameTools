using System;
using MieMieFrameWork;
using MieMieFrameWork.UI;
using UnityEngine;
using UnityEngine.UI;

public class TipWindowLogic : MonoBehaviour
{
    // 这里放置UI组件的引用
    private Text txt;
    private Image Bk;

    public void Init(Text txt,Image Bk){
        this.txt = txt;
        this.Bk = Bk;
    }
    public void ChangeTipText(string str)
    {
        txt.text = str;
        
        // 使用TextMeshPro的preferredWidth来获取准确的文本宽度
        float textWidth = txt.preferredWidth;
        
        float padding = 40f; // 左右各20像素的内边距
        float finalWidth = Mathf.Max(textWidth + padding, 200f); // 最小宽度200像素
        
        // 同步调整文本和背景的尺寸
        this.txt.rectTransform.sizeDelta = new Vector2(finalWidth, 100f);
        this.Bk.rectTransform.sizeDelta = new Vector2(finalWidth, 100f);
    }


    /// <summary>
    /// 显示Tips面板 并自动隐藏 
    /// </summary>
    /// <param name="str"></param>
    /// <param name="time"></param>
    /// <param name="useAnimation"></param>
    public void ShowTips(Action action,float time,bool useAnimation =true)
    {
        action?.Invoke();
        ModuleHub.Instance.GetManager<Mm_UniTimerManager>().StartTimer(time, () =>
        {
            ModuleHub.Instance.GetManager<UICoreMgr>().HideWindow<TipWindow>(useAnimation);
        });
    }

}
