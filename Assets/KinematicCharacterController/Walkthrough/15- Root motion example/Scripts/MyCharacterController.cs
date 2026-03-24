using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;

namespace KinematicCharacterController.Walkthrough.RootMotionExample
{
    /// <summary>
    /// 玩家角色输入结构体
    /// 存储角色的移动输入轴信息
    /// </summary>
    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward;  // 向前移动轴（WS键/左摇杆上下，范围-1~1）
        public float MoveAxisRight;    // 向右移动轴（AD键/左摇杆左右，范围-1~1）
    }

    /// <summary>
    /// 自定义角色控制器（根运动示例）
    /// 核心逻辑：将动画根运动（Root Motion）与KCC物理移动结合，实现动画驱动的角色移动
    /// </summary>
    public class MyCharacterController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor; // KCC核心电机组件（处理物理移动、碰撞等）

        [Header("稳定移动设置")]
        public float MaxStableMoveSpeed = 10f; // 地面最大移动速度
        public float StableMovementSharpness = 15; // 地面移动响应锐度（数值越大加速越快）
        public float OrientationSharpness = 10; // 角色朝向响应锐度

        [Header("空中移动设置")]
        public float MaxAirMoveSpeed = 10f; // 空中最大移动速度
        public float AirAccelerationSpeed = 5f; // 空中加速度
        public float Drag = 0.1f; // 空中拖拽阻力

        [Header("动画参数设置")]
        public Animator CharacterAnimator; // 角色动画器组件
        public float ForwardAxisSharpness = 10; // 向前动画轴的平滑锐度
        public float TurnAxisSharpness = 5; // 转向动画轴的平滑锐度

        [Header("其他设置")]
        public Vector3 Gravity = new Vector3(0, -30f, 0); // 重力加速度
        public Transform MeshRoot; // 角色模型根节点

        // 私有变量
        private Vector3 _moveInputVector; // 处理后的移动输入向量
        private Vector3 _lookInputVector; // 处理后的朝向输入向量
        private float _forwardAxis; // 平滑后的向前动画轴
        private float _rightAxis; // 平滑后的向右/转向动画轴
        private float _targetForwardAxis; // 目标向前动画轴（原始输入）
        private float _targetRightAxis; // 目标向右/转向动画轴（原始输入）
        private Vector3 _rootMotionPositionDelta; // 根运动位置增量（每帧动画的位移）
        private Quaternion _rootMotionRotationDelta; // 根运动旋转增量（每帧动画的旋转）

        /// <summary>
        /// 由MyPlayer脚本每帧调用，用于向角色传递输入信息
        /// </summary>
        /// <param name="inputs">玩家输入结构体</param>
        public void SetInputs(ref PlayerCharacterInputs inputs)
        {
            // 记录目标动画轴（原始输入值）
            _targetForwardAxis = inputs.MoveAxisForward;
            _targetRightAxis = inputs.MoveAxisRight;
        }

        private void Start()
        {
            // 初始化根运动增量
            _rootMotionPositionDelta = Vector3.zero;
            _rootMotionRotationDelta = Quaternion.identity;

            // 将当前控制器赋值给KCC电机（核心关联步骤）
            Motor.CharacterController = this;
        }

        private void Update()
        {
            // 处理动画轴的平滑插值（指数缓动，避免动画突变）
            _forwardAxis = Mathf.Lerp(_forwardAxis, _targetForwardAxis, 1f - Mathf.Exp(-ForwardAxisSharpness * Time.deltaTime));
            _rightAxis = Mathf.Lerp(_rightAxis, _targetRightAxis, 1f - Mathf.Exp(-TurnAxisSharpness * Time.deltaTime));
            
            // 向动画器传递参数，驱动动画播放
            CharacterAnimator.SetFloat("Forward", _forwardAxis); // 向前动画参数
            CharacterAnimator.SetFloat("Turn", _rightAxis);     // 转向动画参数
            CharacterAnimator.SetBool("OnGround", Motor.GroundingStatus.IsStableOnGround); // 地面状态参数
        }

        /// <summary>
        /// （由KinematicCharacterMotor在更新周期中调用）
        /// 角色开始移动更新前的预处理逻辑
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// （由KinematicCharacterMotor在更新周期中调用）
        /// 在此方法中设置角色当前的旋转角度
        /// 【注意】这是唯一允许设置角色旋转的地方
        /// </summary>
        /// <param name="currentRotation">当前角色旋转（引用传递，可修改）</param>
        /// <param name="deltaTime">帧间隔时间</param>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 将动画根运动的旋转增量应用到角色当前旋转
            currentRotation = _rootMotionRotationDelta * currentRotation;
        }

        /// <summary>
        /// （由KinematicCharacterMotor在更新周期中调用）
        /// 在此方法中设置角色当前的速度
        /// 【注意】这是唯一允许设置角色速度的地方
        /// </summary>
        /// <param name="currentVelocity">当前角色速度（引用传递，可修改）</param>
        /// <param name="deltaTime">帧间隔时间</param>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                if (deltaTime > 0)
                {
                    // 地面状态：角色速度 = 动画根运动的位移增量 / 帧时间
                    currentVelocity = _rootMotionPositionDelta / deltaTime;
                    // 将速度重新定向到地面切线方向（适配斜坡）
                    currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
                }
                else
                {
                    // 防止除以0（极端情况）
                    currentVelocity = Vector3.zero;
                }
            }
            else
            {
                // 空中状态：基于输入轴添加移动加速度
                if (_forwardAxis > 0f)
                {
                    // 计算目标空中移动速度
                    Vector3 targetMovementVelocity = Motor.CharacterForward * _forwardAxis * MaxAirMoveSpeed;
                    // 仅在重力垂直平面上叠加速度差值（避免影响下落速度）
                    Vector3 velocityDiff = Vector3.ProjectOnPlane(targetMovementVelocity - currentVelocity, Gravity);
                    currentVelocity += velocityDiff * AirAccelerationSpeed * deltaTime;
                }

                // 叠加重力
                currentVelocity += Gravity * deltaTime;

                // 应用空中拖拽阻力
                currentVelocity *= (1f / (1f + (Drag * deltaTime)));
            }
        }

        /// <summary>
        /// （由KinematicCharacterMotor在更新周期中调用）
        /// 角色完成移动更新后的收尾逻辑
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        public void AfterCharacterUpdate(float deltaTime)
        {
            // 重置根运动增量（每帧更新后清空，避免累计）
            _rootMotionPositionDelta = Vector3.zero;
            _rootMotionRotationDelta = Quaternion.identity;
        }

        /// <summary>
        /// 过滤碰撞体（是否参与角色碰撞检测）
        /// </summary>
        /// <param name="coll">待检测的碰撞体</param>
        /// <returns>是否有效（true=参与碰撞，false=忽略）</returns>
        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true; // 所有碰撞体都参与检测
        }

        /// <summary>
        /// 地面碰撞回调（角色接触地面时触发）
        /// </summary>
        /// <param name="hitCollider">碰撞到的地面碰撞体</param>
        /// <param name="hitNormal">碰撞法线</param>
        /// <param name="hitPoint">碰撞点</param>
        /// <param name="hitStabilityReport">碰撞稳定性报告</param>
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        /// <summary>
        /// 移动碰撞回调（角色移动时碰撞到物体触发）
        /// </summary>
        /// <param name="hitCollider">碰撞到的物体碰撞体</param>
        /// <param name="hitNormal">碰撞法线</param>
        /// <param name="hitPoint">碰撞点</param>
        /// <param name="hitStabilityReport">碰撞稳定性报告</param>
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        /// <summary>
        /// 接地状态更新后的回调
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        public void PostGroundingUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// 叠加额外速度（外部调用，如被击退、爆炸推动）
        /// </summary>
        /// <param name="velocity">要叠加的速度</param>
        public void AddVelocity(Vector3 velocity)
        {
        }

        /// <summary>
        /// 处理碰撞稳定性报告（自定义碰撞稳定性判断）
        /// </summary>
        /// <param name="hitCollider">碰撞体</param>
        /// <param name="hitNormal">碰撞法线</param>
        /// <param name="hitPoint">碰撞点</param>
        /// <param name="atCharacterPosition">角色当前位置</param>
        /// <param name="atCharacterRotation">角色当前旋转</param>
        /// <param name="hitStabilityReport">碰撞稳定性报告（引用传递，可修改）</param>
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        /// <summary>
        /// 动画根运动回调（由Unity动画系统自动调用）
        /// 累计每帧动画的根运动位移和旋转增量
        /// </summary>
        private void OnAnimatorMove()
        {
            // 累加根运动位移增量（动画每帧的实际位移）
            _rootMotionPositionDelta += CharacterAnimator.deltaPosition;
            // 累加根运动旋转增量（动画每帧的实际旋转）
            _rootMotionRotationDelta = CharacterAnimator.deltaRotation * _rootMotionRotationDelta;
        }

        /// <summary>
        /// 离散碰撞检测回调（检测到离散碰撞时触发）
        /// </summary>
        /// <param name="hitCollider">碰撞到的碰撞体</param>
        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
}