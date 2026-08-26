#if UNITY_EDITOR
using MieMieUIFrameWork.Runtime;
using UnityEditor;
using UnityEngine;

/// <summary>
/// DOTweenType 编辑器展示信息
/// </summary>
internal static class DOTweenTypeEditorUtil
{
    /// <summary>
    /// 调色板分组
    /// </summary>
    public readonly struct TypePaletteItem
    {
        public readonly DOTweenType Type;
        public readonly string Label;
        public readonly string Icon;

        public TypePaletteItem(DOTweenType type, string label, string icon)
        {
            Type = type;
            Label = label;
            Icon = icon;
        }
    }

    /// <summary>
    /// 图标板条目 按常用度排列
    /// </summary>
    public static readonly TypePaletteItem[] PaletteItemList =
    {
        new TypePaletteItem(DOTweenType.DOAnchorPos, "锚点", "RectTransform Icon"),
        new TypePaletteItem(DOTweenType.DOAnchorPosX, "锚点X", "RectTransform Icon"),
        new TypePaletteItem(DOTweenType.DOAnchorPosY, "锚点Y", "RectTransform Icon"),
        new TypePaletteItem(DOTweenType.DOScale, "缩放", "ScaleTool"),
        new TypePaletteItem(DOTweenType.DOScaleX, "缩放X", "ScaleTool"),
        new TypePaletteItem(DOTweenType.DOScaleY, "缩放Y", "ScaleTool"),
        new TypePaletteItem(DOTweenType.DOFade, "透明", "ViewToolOrbit"),
        new TypePaletteItem(DOTweenType.DOCanvasGroupFade, "CG透明", "CanvasGroup Icon"),
        new TypePaletteItem(DOTweenType.DOColor, "颜色", "ColorPicker.CycleSlider"),
        new TypePaletteItem(DOTweenType.DOFillAmount, "填充", "Image Icon"),
        new TypePaletteItem(DOTweenType.DOLocalMove, "本地移", "MoveTool"),
        new TypePaletteItem(DOTweenType.DOMove, "世界移", "MoveTool"),
        new TypePaletteItem(DOTweenType.DOLocalRotate, "本地转", "RotateTool"),
        new TypePaletteItem(DOTweenType.DORotate, "旋转", "RotateTool"),
        new TypePaletteItem(DOTweenType.DOSizeDelta, "尺寸", "RectTransform Icon"),
        new TypePaletteItem(DOTweenType.DOValue, "滑条值", "Slider Icon"),
    };

    /// <summary>
    /// 获取类型短名
    /// </summary>
    public static string GetLabel(DOTweenType eType)
    {
        switch (eType)
        {
            case DOTweenType.DOMove: return "世界移动";
            case DOTweenType.DOMoveX: return "移动X";
            case DOTweenType.DOMoveY: return "移动Y";
            case DOTweenType.DOMoveZ: return "移动Z";
            case DOTweenType.DOLocalMove: return "本地移动";
            case DOTweenType.DOLocalMoveX: return "本地X";
            case DOTweenType.DOLocalMoveY: return "本地Y";
            case DOTweenType.DOLocalMoveZ: return "本地Z";
            case DOTweenType.DOScale: return "缩放";
            case DOTweenType.DOScaleX: return "缩放X";
            case DOTweenType.DOScaleY: return "缩放Y";
            case DOTweenType.DOScaleZ: return "缩放Z";
            case DOTweenType.DORotate: return "旋转";
            case DOTweenType.DOLocalRotate: return "本地旋转";
            case DOTweenType.DOAnchorPos: return "锚点移动";
            case DOTweenType.DOAnchorPosX: return "锚点X";
            case DOTweenType.DOAnchorPosY: return "锚点Y";
            case DOTweenType.DOAnchorPosZ: return "锚点Z";
            case DOTweenType.DOAnchorPos3D: return "锚点3D";
            case DOTweenType.DOColor: return "颜色";
            case DOTweenType.DOFade: return "透明度";
            case DOTweenType.DOCanvasGroupFade: return "CG透明";
            case DOTweenType.DOFillAmount: return "填充";
            case DOTweenType.DOFlexibleSize: return "弹性尺寸";
            case DOTweenType.DOMinSize: return "最小尺寸";
            case DOTweenType.DOPreferredSize: return "首选尺寸";
            case DOTweenType.DOSizeDelta: return "尺寸";
            case DOTweenType.DOValue: return "滑条值";
            default: return eType.ToString();
        }
    }

    /// <summary>
    /// 获取类型图标
    /// </summary>
    public static GUIContent GetIconContent(DOTweenType eType)
    {
        string iconName = "Animation.Play";
        switch (eType)
        {
            case DOTweenType.DOMove:
            case DOTweenType.DOMoveX:
            case DOTweenType.DOMoveY:
            case DOTweenType.DOMoveZ:
            case DOTweenType.DOLocalMove:
            case DOTweenType.DOLocalMoveX:
            case DOTweenType.DOLocalMoveY:
            case DOTweenType.DOLocalMoveZ:
                iconName = "MoveTool";
                break;
            case DOTweenType.DOScale:
            case DOTweenType.DOScaleX:
            case DOTweenType.DOScaleY:
            case DOTweenType.DOScaleZ:
                iconName = "ScaleTool";
                break;
            case DOTweenType.DORotate:
            case DOTweenType.DOLocalRotate:
                iconName = "RotateTool";
                break;
            case DOTweenType.DOAnchorPos:
            case DOTweenType.DOAnchorPosX:
            case DOTweenType.DOAnchorPosY:
            case DOTweenType.DOAnchorPosZ:
            case DOTweenType.DOAnchorPos3D:
            case DOTweenType.DOSizeDelta:
                iconName = "RectTransform Icon";
                break;
            case DOTweenType.DOColor:
                iconName = "ColorPicker.CycleSlider";
                break;
            case DOTweenType.DOFade:
            case DOTweenType.DOCanvasGroupFade:
                iconName = "ViewToolOrbit";
                break;
            case DOTweenType.DOFillAmount:
                iconName = "Image Icon";
                break;
            case DOTweenType.DOValue:
                iconName = "Slider Icon";
                break;
            case DOTweenType.DOFlexibleSize:
            case DOTweenType.DOMinSize:
            case DOTweenType.DOPreferredSize:
                iconName = "LayoutElement Icon";
                break;
        }

        var content = EditorGUIUtility.IconContent(iconName);
        if (content == null || content.image == null)
            content = EditorGUIUtility.IconContent("Animation.Play");
        content.tooltip = GetLabel(eType);
        return content;
    }

    /// <summary>
    /// 追加并行标记
    /// </summary>
    public static string GetAddTypeMark(AddType eAddType)
    {
        return eAddType == AddType.Join ? "∥" : "→";
    }

    /// <summary>
    /// 新增条目时的合理目标值
    /// </summary>
    public static Vector4 GetDefaultToValue(DOTweenType eType)
    {
        switch (eType)
        {
            case DOTweenType.DOScale:
                return new Vector4(1.2f, 1.2f, 1.2f, 0f);
            case DOTweenType.DOScaleX:
            case DOTweenType.DOScaleY:
            case DOTweenType.DOScaleZ:
                return new Vector4(1.2f, 0f, 0f, 0f);
            case DOTweenType.DOFade:
            case DOTweenType.DOCanvasGroupFade:
            case DOTweenType.DOFillAmount:
                return new Vector4(1f, 0f, 0f, 0f);
            case DOTweenType.DOColor:
                return new Vector4(1f, 1f, 1f, 1f);
            default:
                return Vector4.zero;
        }
    }
}
#endif
