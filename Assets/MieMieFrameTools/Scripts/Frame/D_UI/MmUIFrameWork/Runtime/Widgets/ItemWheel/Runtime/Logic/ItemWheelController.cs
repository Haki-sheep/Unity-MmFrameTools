using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 物品轮盘主控 扇区命中 动态数据与中心说明同步
/// </summary>
public class ItemWheelController : MonoBehaviour
{
    #region 常量
    private const string ItemInfoPath = "CenterCircle/ItemInfo";
    #endregion

    #region 序列化字段
    [SerializeField]
    private RectTransform wheelRoot;

    [SerializeField]
    private TextMeshProUGUI infoText;

    [SerializeField]
    private float expandedInnerRadius = 0f;

    [SerializeField]
    private float expandedOuterRadius = 0f;

    /// <summary> 未选中时显示的文案 </summary>
    [SerializeField]
    private string idleInfo = string.Empty;
    #endregion

    #region 运行时状态
    /// <summary> 运行时扇区数据 由 SetItems / BindItemData 注入 </summary>
    private ItemWheelData[] itemDataList = System.Array.Empty<ItemWheelData>();

    private RingLayoutData[] sectorLayoutDataList;
    private IRingBehaviour[] ringBehaviourList;
    private RingController[] ringControllerList;
    private int currentItemIndex = -1;

    private ItemWheelPointerResolver pointerResolver;
    #endregion

    #region 属性
    /// <summary> 当前选中扇区索引 无选中为 -1 </summary>
    public int CurrentItemIndex => currentItemIndex;

    /// <summary> 当前扇区数据只读视图 </summary>
    public ItemWheelData[] ItemDataList => itemDataList;
    #endregion

    #region 生命周期
    private void Awake()
    {
        if (wheelRoot == null)
            wheelRoot = GetComponent<RectTransform>();

        if (infoText == null)
        {
            Transform infoTf = wheelRoot.Find(ItemInfoPath);
            if (infoTf != null)
                infoText = infoTf.GetComponent<TextMeshProUGUI>();
        }

        sectorLayoutDataList = BuildLayoutFromSectors();
        ringBehaviourList = BuildRingBehaviourList();
        ringControllerList = BuildRingControllerList();

        pointerResolver = new ItemWheelPointerResolver(wheelRoot, sectorLayoutDataList);
        if (sectorLayoutDataList.Length > 0)
        {
            pointerResolver.ActivationInnerRadius = sectorLayoutDataList[0].innerRadius - expandedInnerRadius;
            pointerResolver.ActivationOuterRadius = sectorLayoutDataList[0].outerRadius + expandedOuterRadius;
        }

        RefreshInfoText(-1);
    }

    private void Update()
    {
        if (pointerResolver == null || Mouse.current == null)
            return;

        int index = pointerResolver.GetMouseOnWitchSectorIndex(Mouse.current.position.ReadValue());
        if (index == currentItemIndex)
            return;

        if (currentItemIndex >= 0 && currentItemIndex < ringBehaviourList.Length)
            ringBehaviourList[currentItemIndex]?.OnExit();

        currentItemIndex = index;

        if (currentItemIndex >= 0 && currentItemIndex < ringBehaviourList.Length)
            ringBehaviourList[currentItemIndex]?.OnEnter();

        RefreshInfoText(currentItemIndex);
    }
    #endregion

    #region 数据注入
    /// <summary>
    /// 整表注入扇区数据并刷图标与说明
    /// </summary>
    public void SetItems(ItemWheelData[] dataList)
    {
        int sectorCount = ringControllerList != null ? ringControllerList.Length : 0;
        itemDataList = new ItemWheelData[sectorCount];

        if (dataList != null)
        {
            int copyCount = Mathf.Min(sectorCount, dataList.Length);
            for (int i = 0; i < copyCount; i++)
                itemDataList[i] = dataList[i];
        }

        ApplyIconsToRings();
        RefreshInfoText(currentItemIndex);
    }

    /// <summary>
    /// 按索引注入单扇区数据
    /// </summary>
    public void BindItemData(int sectorIndex, ItemWheelData data)
    {
        EnsureItemDataListSize();
        if (sectorIndex < 0 || sectorIndex >= itemDataList.Length)
            return;

        itemDataList[sectorIndex] = data;
        if (sectorIndex < ringControllerList.Length)
            ringControllerList[sectorIndex]?.SetItemIcon(data != null ? data.Icon : null);

        if (currentItemIndex == sectorIndex)
            RefreshInfoText(sectorIndex);
    }
    #endregion

    #region 视图同步
    /// <summary>
    /// 同步中心 TMP
    /// </summary>
    private void RefreshInfoText(int sectorIndex)
    {
        if (infoText == null)
            return;

        if (sectorIndex < 0 || sectorIndex >= itemDataList.Length || itemDataList[sectorIndex] == null)
        {
            infoText.text = idleInfo ?? string.Empty;
            return;
        }

        infoText.text = itemDataList[sectorIndex].Info ?? string.Empty;
    }

    /// <summary>
    /// 把数据里的图标刷到各扇环
    /// </summary>
    private void ApplyIconsToRings()
    {
        if (ringControllerList == null)
            return;

        for (int i = 0; i < ringControllerList.Length; i++)
        {
            Sprite icon = i < itemDataList.Length && itemDataList[i] != null ? itemDataList[i].Icon : null;
            ringControllerList[i]?.SetItemIcon(icon);
        }
    }

    /// <summary>
    /// 保证数据表长度对齐扇区数
    /// </summary>
    private void EnsureItemDataListSize()
    {
        int sectorCount = ringControllerList != null ? ringControllerList.Length : 0;
        if (itemDataList != null && itemDataList.Length == sectorCount)
            return;

        ItemWheelData[] nextList = new ItemWheelData[sectorCount];
        if (itemDataList != null)
        {
            int copyCount = Mathf.Min(sectorCount, itemDataList.Length);
            for (int i = 0; i < copyCount; i++)
                nextList[i] = itemDataList[i];
        }

        itemDataList = nextList;
    }
    #endregion

    #region 布局收集
    /// <summary>
    /// 从 SectorRingDraw 构建布局数据
    /// </summary>
    private RingLayoutData[] BuildLayoutFromSectors()
    {
        RingDraw[] ringList = wheelRoot.GetComponentsInChildren<RingDraw>();
        RingLayoutData[] layoutList = new RingLayoutData[ringList.Length];

        for (int i = 0; i < ringList.Length; i++)
        {
            RingDraw ring = ringList[i];
            layoutList[i] = new RingLayoutData
            {
                index = ParseSectorIndex(ring.transform, i),
                startAngle = ring.StartAngle,
                sweepAngle = ring.SweepAngle,
                innerRadius = ring.InnerRadius,
                outerRadius = ring.OuterRadius
            };
        }

        System.Array.Sort(layoutList, (a, b) => a.index.CompareTo(b.index));
        return layoutList;
    }

    /// <summary>
    /// 收集每个扇环的交互行为
    /// </summary>
    private IRingBehaviour[] BuildRingBehaviourList()
    {
        IRingBehaviour[] behaviourList = new IRingBehaviour[sectorLayoutDataList.Length];

        foreach (var layout in sectorLayoutDataList)
        {
            Transform sector = wheelRoot.Find($"Sector_{layout.index}");
            if (sector != null)
                behaviourList[layout.index] = sector.GetComponent<IRingBehaviour>();
        }

        return behaviourList;
    }

    /// <summary>
    /// 收集 RingController
    /// </summary>
    private RingController[] BuildRingControllerList()
    {
        RingController[] controllerList = new RingController[sectorLayoutDataList.Length];

        foreach (var layout in sectorLayoutDataList)
        {
            Transform sector = wheelRoot.Find($"Sector_{layout.index}");
            if (sector != null)
                controllerList[layout.index] = sector.GetComponent<RingController>();
        }

        return controllerList;
    }

    /// <summary>
    /// 扇环索引填充
    /// </summary>
    private static int ParseSectorIndex(Transform sector, int fallback)
    {
        string name = sector.name;
        if (name.StartsWith("Sector_") && int.TryParse(name.Substring(7), out int index))
            return index;

        return fallback;
    }
    #endregion
}
