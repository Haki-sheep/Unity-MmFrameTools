using Sirenix.OdinInspector;
using UnityEngine;

public class VirtualGrid : MonoBehaviour
{
    [Header("基础设置")]
    [LabelText("显示开关")]  public bool showGrid = true;
    [LabelText("单位虚拟网格大小")] public int gridUnitSize;

    [LabelText("虚拟网格X/Z最小坐标")] public int minGridXZ;
    [LabelText("虚拟网格X/Z最大坐标")] public int maxGridXZ = 1;
    [LabelText("虚拟网格Y最小坐标")] public int minGridY = 1;
    [LabelText("虚拟网格Y最大坐标")] public int maxGridY;

    [Header("可视化配置")]
    [LabelText("虚拟网格颜色")] public Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    [LabelText("Y轴竖线颜色")] public Color yAxisColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);


    /// <summary>
    /// 世界转网格坐标
    /// 示例：世界坐标(5.2, 0.8, 5.9) → 网格坐标(5, 0, 5)
    /// </summary>
    /// <param name="worldPos">Unity世界坐标</param>
    /// <param name="clampToBounds">是否限制在地图边界内（推荐true）</param>
    /// <returns>网格坐标</returns>
    public Vector3Int WorldToGrid(Vector3 worldPos, bool clampToBounds = true)
    {
        // 按网格单元大小缩放后取整
        int gridX = Mathf.FloorToInt(worldPos.x / gridUnitSize);
        int gridY = Mathf.FloorToInt(worldPos.y / gridUnitSize);
        int gridZ = Mathf.FloorToInt(worldPos.z / gridUnitSize);

        // 限制在地图边界内（避免超出网格世界）
        if (clampToBounds)
        {
            gridX = Mathf.Clamp(gridX, minGridXZ, maxGridXZ);
            gridY = Mathf.Clamp(gridY, minGridY, maxGridY);
            gridZ = Mathf.Clamp(gridZ, minGridXZ, maxGridXZ);
        }

        return new Vector3Int(gridX, gridY, gridZ);
    }

    /// <summary>
    /// 网格转世界坐标
    /// 示例：网格坐标(5, 0, 5) → 世界坐标(5.5, 0.5, 5.5)
    /// </summary>
    /// <param name="gridPos">网格坐标</param>
    /// <returns>网格中心的世界坐标</returns>
    public Vector3 GridToWorldCenter(Vector3Int gridPos)
    {
        // 网格左下角世界坐标 + 半个网格单元（居中）
        float worldX = gridPos.x * gridUnitSize + gridUnitSize / 2f;
        float worldY = gridPos.y * gridUnitSize + gridUnitSize / 2f;
        float worldZ = gridPos.z * gridUnitSize + gridUnitSize / 2f;

        return new Vector3(worldX, worldY, worldZ);
    }

    //验证边界可行性
    public bool ValidBoundary(Vector3Int gridPos)
    {
        if (gridPos.x < minGridXZ || gridPos.y < minGridY || gridPos.z < minGridXZ)
        {
            return false;
        }
        if (gridPos.x >= maxGridXZ || gridPos.y >= maxGridY || gridPos.z >= maxGridXZ)
        {
            return false;
        }
        return true;
    }

    #region 编辑器可视化（Scene视图画网格线）
    private void OnDrawGizmos()
    {
        if (!showGrid) return;
        // 配置不合法时不绘制
        if (gridUnitSize <= 0 || maxGridXZ < minGridXZ || maxGridY < minGridY)
        {
            return;
        }

        // 1. 绘制X-Z平面网格（每层Y都画）
        Gizmos.color = gridColor;
        for (int y = minGridY; y <= maxGridY; y++)
        {
            // 绘制X轴方向的水平线
            for (int z = minGridXZ; z <= maxGridXZ; z++)
            {
                Vector3 start = new Vector3(minGridXZ * gridUnitSize, y * gridUnitSize, z * gridUnitSize);
                Vector3 end = new Vector3(maxGridXZ * gridUnitSize, y * gridUnitSize, z * gridUnitSize);
                Gizmos.DrawLine(start, end);
            }

            // 绘制Z轴方向的竖直线
            for (int x = minGridXZ; x <= maxGridXZ; x++)
            {
                Vector3 start = new Vector3(x * gridUnitSize, y * gridUnitSize, minGridXZ * gridUnitSize);
                Vector3 end = new Vector3(x * gridUnitSize, y * gridUnitSize, maxGridXZ * gridUnitSize);
                Gizmos.DrawLine(start, end);
            }
        }

        // 2. 绘制Y轴竖线（区分高度）
        Gizmos.color = yAxisColor;
        for (int x = minGridXZ; x <= maxGridXZ; x++)
        {
            for (int z = minGridXZ; z <= maxGridXZ; z++)
            {
                Vector3 start = new Vector3(x * gridUnitSize, minGridY * gridUnitSize, z * gridUnitSize);
                Vector3 end = new Vector3(x * gridUnitSize, maxGridY * gridUnitSize, z * gridUnitSize);
                Gizmos.DrawLine(start, end);
            }
        }
    }
    #endregion

    #region 私有工具方法
    /// <summary>
    /// 校验配置合法性，避免错误配置导致逻辑异常
    /// </summary>
    private void ValidateConfig()
    {
        if (gridUnitSize <= 0)
        {
            Debug.LogError("[GridWorld] 网格单元大小不能≤0，已重置为1f", this);
            gridUnitSize = 1;
        }

        if (maxGridXZ < minGridXZ)
        {
            Debug.LogError("[GridWorld] X/Z最大边界不能小于最小边界，已交换值", this);
            (minGridXZ, maxGridXZ) = (maxGridXZ, minGridXZ);
        }

        if (maxGridY < minGridY)
        {
            Debug.LogError("[GridWorld] Y最大边界不能小于最小边界，已交换值", this);
            (minGridY, maxGridY) = (maxGridY, minGridY);
        }
    }
    #endregion
}
