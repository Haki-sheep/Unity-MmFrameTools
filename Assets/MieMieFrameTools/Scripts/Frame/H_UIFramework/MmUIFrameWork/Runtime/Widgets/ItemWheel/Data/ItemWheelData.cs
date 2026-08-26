using System;
using UnityEngine;

/// <summary>
/// 轮盘扇区通用展示数据 由 ItemWheelController 动态加入
/// </summary>
[Serializable]
public class ItemWheelData
{
    /// <summary> 扇区图标 </summary>
    public Sprite Icon;

    /// <summary> 中心说明文案 </summary>
    public string Info;
}
