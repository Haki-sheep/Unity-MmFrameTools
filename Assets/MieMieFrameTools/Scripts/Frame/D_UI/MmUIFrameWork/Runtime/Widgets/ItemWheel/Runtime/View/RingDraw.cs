using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 程序化扇环的绘制脚本
/// </summary>
public class RingDraw : MaskableGraphic
{
    [SerializeField] private float startAngle = 0f;
    [SerializeField] private float sweepAngle = 90f;
    [SerializeField] private float innerRadius = 40f;
    [SerializeField] private float outerRadius = 80f;
    [SerializeField] private int outerArcPart = 8;
    [SerializeField] private int innerArcPart = 8;
    [SerializeField] private float borderWidth = 0f;
    [SerializeField] private Color borderColor = Color.black;

    public float BorderWidth => borderWidth;
    public float StartAngle => startAngle;
    public float SweepAngle => sweepAngle;
    public float InnerRadius => innerRadius;
    public float OuterRadius => outerRadius;

    /// <summary>
    /// 设置扇环描边宽度
    /// </summary>
    public void SetBorderWidth(float width)
    {
        borderWidth = Mathf.Max(0f, width);
        SetVerticesDirty();
    }

    /// <summary>
    /// 初始化扇环参数
    /// </summary>
    public void InitSector(float startAngleDeg, float sweepAngleDeg, float sectorInner, float sectorOuter,
        int outerArcParts, int innerArcParts, float lineWidth = 0f, Color? lineColor = null)
    {
        startAngle = startAngleDeg;
        sweepAngle = sweepAngleDeg;
        innerRadius = sectorInner;
        outerRadius = sectorOuter;
        outerArcPart = Mathf.Max(1, outerArcParts);
        innerArcPart = Mathf.Max(1, innerArcParts);
        borderWidth = Mathf.Max(0f, lineWidth);
        borderColor = lineColor ?? Color.black;
        SetVerticesDirty();
    }

    /// <summary>
    /// 填充扇环的网格
    /// </summary>
    /// <param name="vh"></param>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        SectorBuilder.PopulateSector(vh, startAngle, sweepAngle, innerRadius, outerRadius,
            outerArcPart, innerArcPart, color, borderWidth, borderColor);
    }

    /// <summary>
    /// 启用时刷新网格
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        SetVerticesDirty();
    }
}

/// <summary>
/// 扇环网格构建
/// </summary>
public static class SectorBuilder
{
    /// <summary>
    /// 按分段数在弧线上采样一点 u01 为弧长归一化参数
    /// </summary>
    public static Vector2 SampleArc(float startDeg, float sweepDeg, float radius, float u01, int arcParts)
    {
        int parts = Mathf.Max(1, arcParts);
        float f = Mathf.Clamp01(u01) * parts;
        int seg = Mathf.Min(Mathf.FloorToInt(f), parts - 1);
        float t = f - seg;
        float a0 = startDeg + sweepDeg * seg / parts;
        float a1 = startDeg + sweepDeg * (seg + 1) / parts;
        float angle = Mathf.Lerp(a0, a1, t) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    /// <summary>
    /// 填充扇环 mesh 外弧描边为独立一层贴在最外缘
    /// </summary>
    public static int PopulateSector(VertexHelper vh, float startDeg, float sweepDeg,
        float sectorInner, float sectorOuter, int outerArcParts, int innerArcParts,
        Color fillColor, float lineWidth, Color lineColor)
    {
        int outerPart = Mathf.Max(1, outerArcParts);
        int innerPart = Mathf.Max(1, innerArcParts);
        int stripCount = Mathf.Max(outerPart, innerPart);

        float lineInner = sectorOuter - Mathf.Max(0f, lineWidth);
        bool hasBorder = lineWidth > 0.001f && lineInner > sectorInner + 0.001f;
        float fillOuter = hasBorder ? lineInner : sectorOuter;

        int added = 0;
        UIVertex vert = UIVertex.simpleVert;
        int vertCount = stripCount + 1;

        List<Vector2> outerPts = new List<Vector2>(vertCount);
        List<Vector2> innerPts = new List<Vector2>(vertCount);
        for (int i = 0; i <= stripCount; i++)
        {
            float u = i / (float)stripCount;
            outerPts.Add(SampleArc(startDeg, sweepDeg, fillOuter, u, outerPart));
            innerPts.Add(SampleArc(startDeg, sweepDeg, sectorInner, u, innerPart));
        }

        vert.color = fillColor;
        for (int i = 0; i < vertCount; i++)
        {
            vert.position = outerPts[i];
            vh.AddVert(vert);
            added++;
        }

        for (int i = 0; i < vertCount; i++)
        {
            vert.position = innerPts[i];
            vh.AddVert(vert);
            added++;
        }

        for (int i = 0; i < stripCount; i++)
        {
            int baseIdx = vh.currentVertCount - added;
            int a = baseIdx + i;
            int b = baseIdx + i + 1;
            int c = baseIdx + vertCount + (i + 1);
            int d = baseIdx + vertCount + i;
            vh.AddTriangle(a, b, c);
            vh.AddTriangle(a, c, d);
        }

        if (hasBorder)
        {
            List<Vector2> rimOuter = new List<Vector2>(vertCount);
            List<Vector2> rimInner = new List<Vector2>(vertCount);
            for (int i = 0; i <= stripCount; i++)
            {
                float u = i / (float)stripCount;
                rimOuter.Add(SampleArc(startDeg, sweepDeg, sectorOuter, u, outerPart));
                rimInner.Add(SampleArc(startDeg, sweepDeg, lineInner, u, outerPart));
            }

            vert.color = lineColor;
            int rimStart = vh.currentVertCount;
            foreach (var p in rimOuter)
            {
                vert.position = p;
                vh.AddVert(vert);
                added++;
            }

            foreach (var p in rimInner)
            {
                vert.position = p;
                vh.AddVert(vert);
                added++;
            }

            for (int i = 0; i < stripCount; i++)
            {
                int a = rimStart + i;
                int b = rimStart + i + 1;
                int c = rimStart + vertCount + (i + 1);
                int d = rimStart + vertCount + i;
                vh.AddTriangle(a, b, c);
                vh.AddTriangle(a, c, d);
            }
        }

        return added;
    }
}
