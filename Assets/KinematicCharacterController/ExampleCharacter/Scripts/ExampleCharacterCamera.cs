using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace KinematicCharacterController.Examples
{
    /// <summary>
    /// 示例角色相机控制器
    /// 实现第三人称轨道相机系统，支持平滑跟随、视角旋转、距离缩放和遮挡检测
    /// 
    /// 功能特性：
    /// - 轨道式相机围绕目标旋转
    /// - 支持鼠标控制视角（水平/垂直旋转）
    /// - 支持滚轮缩放相机距离
    /// - 遮挡检测：检测到遮挡时自动拉近相机
    /// - 忽略列表：可设置忽略特定碰撞体（如角色自身）
    /// </summary>
    [TypeInfoBox("第三人称轨道相机控制器：实现平滑视角跟随、旋转控制、距离缩放和遮挡检测")]
    public class ExampleCharacterCamera : MonoBehaviour
    {
        // 画面设置
        [LabelText("Unity相机组件")]
        [Tooltip("场景中的Unity相机组件引用")]
        public Camera Camera;

        [LabelText("画面偏移 X")]
        [Tooltip("相机在画面中的水平偏移量，用于调整构图")]
        public Vector2 FollowPointFraming = new Vector2(0f, 0f);

        [LabelText("跟随平滑度")]
        [Tooltip("相机跟随目标的平滑程度，数值越大跟随越快")]
        [Range(1f, 10000f)]
        public float FollowingSharpness = 10000f;

        // 距离设置
        [LabelText("默认距离")]
        [Tooltip("相机的默认起始距离")]
        public float DefaultDistance = 6f;

        [LabelText("最小距离")]
        [Tooltip("相机允许的最近距离（防止穿模）")]
        public float MinDistance = 0f;

        [LabelText("最大距离")]
        [Tooltip("相机允许的最远距离")]
        public float MaxDistance = 10f;

        [LabelText("距离调整速度")]
        [Tooltip("使用滚轮调整距离时的响应速度")]
        public float DistanceMovementSpeed = 5f;

        [LabelText("距离平滑度")]
        [Tooltip("距离变化时的平滑插值程度")]
        public float DistanceMovementSharpness = 10f;

        // 旋转设置
        [LabelText("反转X轴")]
        [Tooltip("是否反转水平旋转方向")]
        public bool InvertX = false;

        [LabelText("反转Y轴")]
        [Tooltip("是否反转垂直旋转方向")]
        public bool InvertY = false;

        [LabelText("默认垂直角度")]
        [Tooltip("相机的默认垂直俯仰角")]
        [Range(-90f, 90f)]
        public float DefaultVerticalAngle = 20f;

        [LabelText("最小垂直角度")]
        [Tooltip("相机垂直旋转的下限（低头）")]
        [Range(-90f, 90f)]
        public float MinVerticalAngle = -90f;

        [LabelText("最大垂直角度")]
        [Tooltip("相机垂直旋转的上限（抬头）")]
        [Range(-90f, 90f)]
        public float MaxVerticalAngle = 90f;

        [LabelText("旋转速度")]
        [Tooltip("鼠标控制视角旋转的灵敏度")]
        public float RotationSpeed = 1f;

        [LabelText("旋转平滑度")]
        [Tooltip("视角旋转的平滑插值程度，数值越大越平滑")]
        public float RotationSharpness = 10000f;

        [LabelText("随物理移动旋转")]
        [Tooltip("相机是否随PhysicsMover物体旋转（如移动平台）")]
        public bool RotateWithPhysicsMover = false;

        // 遮挡检测设置
        [LabelText("检测半径")]
        [Tooltip("用于遮挡检测的球体半径")]
        public float ObstructionCheckRadius = 0.2f;

        [LabelText("检测层")]
        [Tooltip("参与遮挡检测的层")]
        public LayerMask ObstructionLayers = -1;

        [LabelText("处理平滑度")]
        [Tooltip("检测到遮挡时的平滑过渡程度")]
        public float ObstructionSharpness = 10000f;

        [LabelText("忽略碰撞体列表")]
        [Tooltip("参与遮挡检测时忽略的碰撞体列表（如角色自身的碰撞器）")]
        public List<Collider> IgnoredColliders = new List<Collider>();

        // 公共属性（只读）
        // 相机变换组件引用
        public Transform Transform { get; private set; }

        // 跟随目标变换组件引用
        public Transform FollowTransform { get; private set; }

        // 水平方向向量
        // 用于计算相机的水平朝向（忽略Y轴）
        public Vector3 PlanarDirection { get; set; }

        // 目标距离
        // 相机缩放的目标值，会被平滑过渡到实际距离
        public float TargetDistance { get; set; }

        // 内部状态变量
        // 距离是否被遮挡
        private bool _distanceIsObstructed;

        // 当前实际距离
        // 平滑过渡后的实际相机距离
        private float _currentDistance;

        // 目标垂直角度
        // 用于平滑过渡的垂直角度值
        private float _targetVerticalAngle;

        // 遮挡检测结果
        // 最近一次遮挡检测的命中信息
        private RaycastHit _obstructionHit;

        // 检测到的遮挡物数量
        private int _obstructionCount;

        // 遮挡物数组
        // 用于存储SphereCastNonAlloc的检测结果
        private RaycastHit[] _obstructions = new RaycastHit[MaxObstructions];

        // 遮挡开始时间
        // 用于记录遮挡发生的时间点
        private float _obstructionTime;

        // 当前跟随位置
        // 平滑过渡后的跟随位置
        private Vector3 _currentFollowPosition;

        // 最大遮挡物数量
        // SphereCastNonAlloc的数组缓冲区大小
        private const int MaxObstructions = 32;

        // 在编辑器中验证参数
        // 当在Inspector中修改参数时自动调用，确保参数在有效范围内
        void OnValidate()
        {
            DefaultDistance = Mathf.Clamp(DefaultDistance, MinDistance, MaxDistance);
            DefaultVerticalAngle = Mathf.Clamp(DefaultVerticalAngle, MinVerticalAngle, MaxVerticalAngle);
        }

        // 唤醒方法
        // 在对象创建时（场景加载或实例化）调用，用于初始化相机状态
        // 
        // 初始化内容：
        // - 获取并缓存Transform组件引用
        // - 设置默认距离和目标距离
        // - 初始化垂直角度
        // - 设置初始水平方向
        void Awake()
        {
            // 获取自身变换组件
            Transform = this.transform;

            // 初始化距离
            _currentDistance = DefaultDistance;
            TargetDistance = _currentDistance;

            // 初始化垂直角度
            _targetVerticalAngle = 0f;

            // 初始化水平方向（默认朝向前方）
            PlanarDirection = Vector3.forward;
        }

        // 设置相机跟随目标
        // 建立相机与角色之间的关联，使相机能够围绕目标旋转和跟随
        // 
        // 使用时机：
        // 在场景初始化时（如MyPlayer.Start()中）调用一次
        // 
        // 初始化内容：
        // - 设置FollowTransform为目标的Transform
        // - 使用目标的forward方向初始化水平方向
        // - 使用目标位置初始化跟随位置
        // 
        // 参数说明：
        // t - 跟随的目标Transform（通常是角色身上的相机跟随点）
        public void SetFollowTransform(Transform t)
        {
            FollowTransform = t;
            // 使用目标的朝向初始化水平方向
            PlanarDirection = FollowTransform.forward;
            // 初始化跟随位置
            _currentFollowPosition = FollowTransform.position;
        }

        // 使用输入更新相机状态
        // 核心方法：处理旋转、缩放和位置计算
        // 
        // 处理流程：
        // 1. 应用输入反转设置（InvertX/InvertY）
        // 2. 处理水平旋转输入，更新水平方向向量
        // 3. 处理垂直角度输入，限制角度范围
        // 4. 计算目标旋转四元数
        // 5. 处理距离缩放输入
        // 6. 执行遮挡检测
        // 7. 计算并应用相机最终位置
        // 
        // 调用时机：
        // 每帧调用（通常在Player的LateUpdate中调用）
        // 
        // 参数说明：
        // deltaTime - 帧时间间隔（秒）
        // zoomInput - 缩放输入值（鼠标滚轮，负值拉近/正值拉远）
        // rotationInput - 旋转输入向量（x=水平，y=垂直）
        public void UpdateWithInput(float deltaTime, float zoomInput, Vector3 rotationInput)
        {
            // 确保已设置跟随目标
            if (FollowTransform)
            {
                // 输入反转处理
                if (InvertX)
                {
                    rotationInput.x *= -1f;
                }
                if (InvertY)
                {
                    rotationInput.y *= -1f;
                }

                // 水平旋转处理
                // 根据鼠标X轴输入计算水平旋转
                Quaternion rotationFromInput = Quaternion.Euler(FollowTransform.up * (rotationInput.x * RotationSpeed));
                // 更新水平方向向量
                PlanarDirection = rotationFromInput * PlanarDirection;
                // 保持方向在水平面上
                PlanarDirection = Vector3.Cross(FollowTransform.up, Vector3.Cross(PlanarDirection, FollowTransform.up)).normalized;
                // 计算水平旋转四元数
                Quaternion planarRot = Quaternion.LookRotation(PlanarDirection, FollowTransform.up);

                // 垂直角度处理
                // 处理垂直角度输入
                _targetVerticalAngle -= (rotationInput.y * RotationSpeed);
                // 限制垂直角度范围
                _targetVerticalAngle = Mathf.Clamp(_targetVerticalAngle, MinVerticalAngle, MaxVerticalAngle);
                // 计算垂直旋转
                Quaternion verticalRot = Quaternion.Euler(_targetVerticalAngle, 0, 0);
                // 组合目标旋转（水平+垂直），应用平滑过渡
                Quaternion targetRotation = Quaternion.Slerp(Transform.rotation, planarRot * verticalRot, 1f - Mathf.Exp(-RotationSharpness * deltaTime));

                // 应用旋转
                Transform.rotation = targetRotation;

                // 距离缩放处理
                // 如果被遮挡且有缩放输入，重置目标距离到当前距离
                if (_distanceIsObstructed && Mathf.Abs(zoomInput) > 0f)
                {
                    TargetDistance = _currentDistance;
                }
                // 更新目标距离
                TargetDistance += zoomInput * DistanceMovementSpeed;
                // 限制距离范围
                TargetDistance = Mathf.Clamp(TargetDistance, MinDistance, MaxDistance);

                // 跟随位置计算
                // 计算平滑的跟随位置
                _currentFollowPosition = Vector3.Lerp(_currentFollowPosition, FollowTransform.position, 1f - Mathf.Exp(-FollowingSharpness * deltaTime));

                // 遮挡检测处理
                {
                    // 初始化最近遮挡点
                    RaycastHit closestHit = new RaycastHit();
                    closestHit.distance = Mathf.Infinity;

                    // 使用球体射线检测所有遮挡物
                    _obstructionCount = Physics.SphereCastNonAlloc(
                        _currentFollowPosition,
                        ObstructionCheckRadius,
                        -Transform.forward,
                        _obstructions,
                        TargetDistance,
                        ObstructionLayers,
                        QueryTriggerInteraction.Ignore);

                    // 遍历所有检测到的遮挡物
                    for (int i = 0; i < _obstructionCount; i++)
                    {
                        // 检查是否在忽略列表中
                        bool isIgnored = false;
                        for (int j = 0; j < IgnoredColliders.Count; j++)
                        {
                            if (IgnoredColliders[j] == _obstructions[i].collider)
                            {
                                isIgnored = true;
                                break;
                            }
                        }
                        // 再次检查（代码重复，可优化）
                        for (int j = 0; j < IgnoredColliders.Count; j++)
                        {
                            if (IgnoredColliders[j] == _obstructions[i].collider)
                            {
                                isIgnored = true;
                                break;
                            }
                        }

                        // 找到最近的非忽略遮挡物
                        if (!isIgnored && _obstructions[i].distance < closestHit.distance && _obstructions[i].distance > 0)
                        {
                            closestHit = _obstructions[i];
                        }
                    }

                    // 处理遮挡结果
                    // 如果检测到遮挡物
                    if (closestHit.distance < Mathf.Infinity)
                    {
                        _distanceIsObstructed = true;
                        // 平滑调整当前距离到遮挡物位置
                        _currentDistance = Mathf.Lerp(_currentDistance, closestHit.distance, 1f - Mathf.Exp(-ObstructionSharpness * deltaTime));
                    }
                    // 如果没有遮挡物
                    else
                    {
                        _distanceIsObstructed = false;
                        // 平滑调整到目标距离
                        _currentDistance = Mathf.Lerp(_currentDistance, TargetDistance, 1f - Mathf.Exp(-DistanceMovementSharpness * deltaTime));
                    }
                }

                // 相机位置计算
                // 计算相机轨道位置
                Vector3 targetPosition = _currentFollowPosition - (targetRotation * Vector3.forward * _currentDistance);

                // 应用画面偏移
                targetPosition += Transform.right * FollowPointFraming.x;  // 水平偏移
                targetPosition += Transform.up * FollowPointFraming.y;      // 垂直偏移

                // 应用最终位置
                Transform.position = targetPosition;
            }
        }
    }
}
