using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;

namespace KinematicCharacterController.Walkthrough.FramePerfectRotation
{
    /// <summary>
    /// 玩家角色输入结构体
    /// 存储移动轴和相机旋转信息
    /// </summary>
    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward;  // 向前移动轴（WS键/左摇杆上下）
        public float MoveAxisRight;    // 向右移动轴（AD键/左摇杆左右）
        public Quaternion CameraRotation; // 相机的世界旋转角度
    }

    /// <summary>
    /// 自定义角色控制器（帧完美旋转示例）
    /// 核心解决：角色旋转与相机/输入不同步导致的“卡顿/延迟”问题，实现无延迟的帧同步旋转
    /// </summary>
    public class MyCharacterController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor; // KCC核心电机组件

        [Header("稳定移动设置")]
        public float MaxStableMoveSpeed = 10f; // 地面最大移动速度
        public float StableMovementSharpness = 15; // 地面移动响应锐度（数值越大加速越快）
        public float OrientationSharpness = 10; // 角色朝向响应锐度

        [Header("空中移动设置")]
        public float MaxAirMoveSpeed = 10f; // 空中最大移动速度
        public float AirAccelerationSpeed = 5f; // 空中加速度
        public float Drag = 0.1f; // 空中拖拽阻力

        [Header("其他设置")]
        public Vector3 Gravity = new Vector3(0, -30f, 0); // 重力加速度
        public Transform MeshRoot; // 角色模型根节点（用于独立控制模型旋转）
        public bool FramePerfectRotation = true; // 是否开启“帧完美旋转”

        private Vector3 _moveInputVector; // 处理后的移动输入向量
        private Vector3 _lookInputVector; // 处理后的朝向输入向量

        private void Start()
        {
            // 将当前控制器赋值给KCC电机（核心关联步骤）
            Motor.CharacterController = this;
        }

        /// <summary>
        /// 由MyPlayer脚本每帧调用，用于向角色传递输入信息
        /// </summary>
        /// <param name="inputs">玩家输入结构体</param>
        public void SetInputs(ref PlayerCharacterInputs inputs)
        {
            // 限制输入向量的长度（避免斜向移动速度过快）
            Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

            // 计算相机在角色平面上的方向和旋转（适配角色当前的“上”方向）
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
            }
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

            // 转换移动/朝向输入为角色本地空间
            _moveInputVector = cameraPlanarRotation * moveInputVector;
            _lookInputVector = cameraPlanarDirection;
        }

        /// <summary>
        /// 输入更新后的回调（自定义方法，由MyPlayer调用）
        /// 核心：开启帧完美旋转时，提前同步模型旋转，避免KCC电机更新带来的延迟
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        /// <param name="cameraForward">相机前方向量</param>
        public void PostInputUpdate(float deltaTime, Vector3 cameraForward)
        {
            if (FramePerfectRotation)
            {
                // 将相机前方向量投影到角色平面（忽略垂直方向）
                _lookInputVector = Vector3.ProjectOnPlane(cameraForward, Motor.CharacterUp);

                // 计算新的旋转角度
                Quaternion newRotation = default;
                HandleRotation(ref newRotation, deltaTime);
                // 直接赋值给模型根节点，实现“帧同步”旋转（无延迟）
                MeshRoot.rotation = newRotation;
            }
        }

        /// <summary>
        /// 处理角色旋转的核心方法
        /// </summary>
        /// <param name="rot">目标旋转（引用传递）</param>
        /// <param name="deltaTime">帧间隔时间</param>
        private void HandleRotation(ref Quaternion rot, float deltaTime)
        {
            if (_lookInputVector != Vector3.zero)
            {
                // 让角色朝向“看向输入向量”的方向，保持角色“上”方向不变
                rot = Quaternion.LookRotation(_lookInputVector, Motor.CharacterUp);
            }
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
        /// 在此方法中设置角色（物理体）的旋转角度
        /// 【KCC约束】这是唯一允许设置角色物理体旋转的地方
        /// </summary>
        /// <param name="currentRotation">当前物理体旋转（引用传递，可修改）</param>
        /// <param name="deltaTime">帧间隔时间</param>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 同步物理体旋转（保证物理移动方向与模型朝向一致）
            HandleRotation(ref currentRotation, deltaTime);
        }

        /// <summary>
        /// （由KinematicCharacterMotor在更新周期中调用）
        /// 在此方法中设置角色当前的速度
        /// 【KCC约束】这是唯一允许设置角色速度的地方
        /// </summary>
        /// <param name="currentVelocity">当前角色速度（引用传递，可修改）</param>
        /// <param name="deltaTime">帧间隔时间</param>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            Vector3 targetMovementVelocity = Vector3.zero;
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                // 在斜坡上重新调整速度方向（贴合斜坡切线）
                currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

                // 计算目标速度（适配地面法线）
                Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                Vector3 reorientedInput = Vector3.Cross(Motor.GroundingStatus.GroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;

                // 平滑地面移动速度（指数缓动，更自然）
                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-StableMovementSharpness * deltaTime));
            }
            else
            {
                // 空中移动输入处理
                if (_moveInputVector.sqrMagnitude > 0f)
                {
                    targetMovementVelocity = _moveInputVector * MaxAirMoveSpeed;

                    // 防止在空中爬上不稳定的斜坡
                    if (Motor.GroundingStatus.FoundAnyGround)
                    {
                        Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                        targetMovementVelocity = Vector3.ProjectOnPlane(targetMovementVelocity, perpenticularObstructionNormal);
                    }

                    // 计算速度差值并叠加（仅在重力垂直平面上）
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
        /// 离散碰撞检测回调（检测到离散碰撞时触发）
        /// </summary>
        /// <param name="hitCollider">碰撞到的碰撞体</param>
        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
}