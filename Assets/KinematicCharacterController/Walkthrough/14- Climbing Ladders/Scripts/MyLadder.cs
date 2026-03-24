using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KinematicCharacterController.Walkthrough.ClimbingLadders
{
    /// <summary>
    /// 梯子核心逻辑脚本
    /// 定义梯子的物理段（上下锚点）、计算任意点到梯子段的最近点，
    /// 并提供梯子端点的“离开位置”（角色爬完梯子后脱离的位置）
    /// </summary>
    public class MyLadder : MonoBehaviour
    {
        // 梯子段配置
        public Vector3 LadderSegmentBottom; // 梯子段底部在本地坐标系的偏移（相对于梯子Transform）
        public float LadderSegmentLength;   // 梯子段的长度（沿梯子up方向）

        // 角色爬到梯子两端后，脱离梯子时要移动到的目标点
        public Transform BottomReleasePoint; // 梯子底部脱离点（爬到底部后离开的位置）
        public Transform TopReleasePoint;    // 梯子顶部脱离点（爬到顶部后离开的位置）

        // 获取梯子段底部锚点的世界坐标（只读属性）
        public Vector3 BottomAnchorPoint
        {
            get
            {
                // 本地偏移转世界坐标：梯子位置 + 本地底部偏移的世界方向
                return transform.position + transform.TransformVector(LadderSegmentBottom);
            }
        }

        // 获取梯子段顶部锚点的世界坐标（只读属性）
        public Vector3 TopAnchorPoint
        {
            get
            {
                // 底部锚点 + 梯子up方向 * 梯子长度 = 顶部锚点
                return transform.position + transform.TransformVector(LadderSegmentBottom) + (transform.up * LadderSegmentLength);
            }
        }

        /// <summary>
        /// 计算“目标点”到梯子段的最近点，并返回该点在梯子段上的位置状态
        /// </summary>
        /// <param name="fromPoint">目标点（通常是角色位置）</param>
        /// <param name="onSegmentState">输出：点在梯子段上的状态值
        /// - 0 = 点在梯子段范围内（底部~顶部之间）
        /// - 正数 = 点超出梯子顶部的距离
        /// - 负数 = 点低于梯子底部的距离
        /// </param>
        /// <returns>目标点到梯子段的最近点世界坐标</returns>
        public Vector3 ClosestPointOnLadderSegment(Vector3 fromPoint, out float onSegmentState)
        {
            // 梯子段的向量（顶部锚点 - 底部锚点）
            Vector3 segment = TopAnchorPoint - BottomAnchorPoint;
            // 目标点到梯子底部锚点的向量
            Vector3 segmentPoint1ToPoint = fromPoint - BottomAnchorPoint;
            // 计算目标点在梯子段向量上的投影长度（沿梯子方向的距离）
            float pointProjectionLength = Vector3.Dot(segmentPoint1ToPoint, segment.normalized);

            // 情况1：投影长度>0 → 目标点高于梯子底部锚点
            if (pointProjectionLength > 0)
            {
                // 子情况1.1：投影长度 ≤ 梯子段总长度 → 目标点在梯子段范围内
                if (pointProjectionLength <= segment.magnitude)
                {
                    onSegmentState = 0; // 标记：在梯子段内
                    // 最近点 = 底部锚点 + 梯子方向 * 投影长度
                    return BottomAnchorPoint + (segment.normalized * pointProjectionLength);
                }
                // 子情况1.2：投影长度 > 梯子段总长度 → 目标点高于梯子顶部
                else
                {
                    // 标记：超出顶部的距离 = 投影长度 - 梯子总长度
                    onSegmentState = pointProjectionLength - segment.magnitude;
                    return TopAnchorPoint; // 最近点 = 梯子顶部锚点
                }
            }
            // 情况2：投影长度≤0 → 目标点低于梯子底部锚点
            else
            {
                onSegmentState = pointProjectionLength; // 标记：低于底部的距离（负数）
                return BottomAnchorPoint; // 最近点 = 梯子底部锚点
            }
        }

        /// <summary>
        /// 场景视图绘制梯子段的Gizmos（便于调试）
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan; // 青色
            Gizmos.DrawLine(BottomAnchorPoint, TopAnchorPoint); // 绘制梯子段的线段
        }
    }
}