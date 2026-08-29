using UnityEngine;

/// <summary>
/// 指针解析器
/// </summary>
public class ItemWheelPointerResolver
{
    private readonly RectTransform wheelRoot;
    private readonly RingLayoutData[] sectorLayoutDataList;

    /// <summary> 判定内圆半径 </summary>
    public float ActivationInnerRadius { get; set; }

    /// <summary> 判定外圆半径 </summary>
    public float ActivationOuterRadius { get; set; }

    public ItemWheelPointerResolver(RectTransform wheelRoot, RingLayoutData[] sectorLayoutDataList)
    {
        this.wheelRoot = wheelRoot;
        this.sectorLayoutDataList = sectorLayoutDataList;
    }

    /// <summary>
    /// 获取鼠标当前落在哪个扇环
    /// </summary>
    public int GetMouseOnWitchSectorIndex(Vector2 mousePos)
    {
        // 将屏幕坐标转换为局部坐标 转换后轴心点为0,0点
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                wheelRoot, mousePos, null, out Vector2 localPos))
            return -1;

        // 判断是否在激活范围内
        float r = localPos.magnitude;
        if (r < ActivationInnerRadius || r > ActivationOuterRadius)
            return -1;

        // 计算角度
        float deg = (Mathf.Atan2(localPos.y, localPos.x) * Mathf.Rad2Deg + 360f) % 360f;

        // 遍历所有扇环 判断角度是否在范围内
        foreach (var data in sectorLayoutDataList)
        {
            if (deg >= data.startAngle && deg < data.startAngle + data.sweepAngle)
                return data.index;
        }

        return -1;
    }
}
