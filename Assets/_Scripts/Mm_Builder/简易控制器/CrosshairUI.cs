using UnityEngine;
using UnityEngine.UI;



/// <summary>
/// 中心焦点UI组件
/// 显示准星
/// </summary>
public class CrosshairUI : MonoBehaviour
{
    [Header("UI元素引用")]
    [SerializeField] private Image crosshairImage; // 准星图片

    [Header("颜色设置")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color validColor = Color.green;
    [SerializeField] private Color invalidColor = Color.red;

    // 状态
    private bool isTargetValid = false;

    private void Start()
    {
    }

    private void Update()
    {
    }

 

    /// <summary>
    /// 设置目标是否有效
    /// </summary>
    public void SetTargetValid(bool valid)
    {
        isTargetValid = valid;
    }


    /// <summary>
    /// 设置选中的方块名称（保留接口，但不再使用）
    /// </summary>
    public void SetSelectedBlockName(string name)
    {
        // 保留接口，但UI不再显示
    }

    /// <summary>
    /// 保留接口以兼容，但不再使用
    /// </summary>
    public void SetBreakProgress(float progress)
    {
        // 不再需要
    }

    /// <summary>
    /// 显示/隐藏UI
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
