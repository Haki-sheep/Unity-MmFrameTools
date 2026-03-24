using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;

namespace KinematicCharacterController.Walkthrough.SimpleJumping
{
    // 玩家角色输入结构体：存储角色的移动、视角、跳跃等输入信息
    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward; // 向前移动轴（WS键/左摇杆上下）
        public float MoveAxisRight; // 向右移动轴（AD键/左摇杆左右）
        public Quaternion CameraRotation; // 相机的旋转角度
        public bool JumpDown; // 跳跃按键是否按下（帧级）
    }

    // 自定义角色控制器：实现ICharacterController接口，处理角色的移动、旋转、跳跃等核心逻辑
    public class MyCharacterController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor; // 运动学角色电机（核心驱动组件）

        [Header("地面稳定移动参数")]
        public float MaxStableMoveSpeed = 10f; // 地面最大移动速度
        public float StableMovementSharpness = 15; // 地面移动响应锐度（值越大加速越快）
        public float OrientationSharpness = 10; // 角色朝向响应锐度（值越大转向越快）

        [Header("空中移动参数")]
        public float MaxAirMoveSpeed = 10f; // 空中最大移动速度
        public float AirAccelerationSpeed = 5f; // 空中加速度
        public float Drag = 0.1f; // 空中阻力（值越大减速越快）

        [Header("跳跃参数")]
        public bool AllowJumpingWhenSliding = false; // 是否允许在斜坡滑动时跳跃
        public float JumpSpeed = 10f; // 跳跃初速度
        public float JumpPreGroundingGraceTime = 0f; // 预处理
        public float JumpPostGroundingGraceTime = 0f; // 土狼

        [Header("通用参数")]
        public Vector3 Gravity = new Vector3(0, -30f, 0); // 重力加速度（Y轴为负表示向下）
        public Transform MeshRoot; // 角色模型根节点（用于视觉表现，非核心逻辑）

        // 私有运行时变量
        private Vector3 _moveInputVector; // 处理后的移动输入向量（已转换为世界空间）
        private Vector3 _lookInputVector; // 处理后的视角输入向量（角色朝向目标）
        private bool _jumpRequested = false; // 是否有跳跃请求
        private bool _jumpConsumed = false; // 跳跃请求是否已被消耗（防止连续跳）
        private bool _jumpedThisFrame = false; // 本帧是否执行了跳跃
        private float _timeSinceJumpRequested = Mathf.Infinity; // 距离上次跳跃请求的时间
        private float _timeSinceLastAbleToJump = 0f; // 距离上次可跳跃状态的时间（用于容错）

        private void Start()
        {
            // 将当前控制器绑定到角色电机
            Motor.CharacterController = this;
        }

        /// <summary>
        /// 设置角色输入（由玩家输入脚本每帧调用）
        /// 将原始输入转换为角色空间的可用数据
        /// </summary>
        /// <param name="inputs">玩家原始输入数据</param>
        public void SetInputs(ref PlayerCharacterInputs inputs)
        {
            // 限制移动输入的长度为1（防止斜向移动速度过快）
            Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

            // 计算相机在角色平面上的朝向和旋转
            // 确保移动方向与相机视角匹配
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
            }
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

            // 将移动输入转换为基于相机朝向的世界空间向量
            _moveInputVector = cameraPlanarRotation * moveInputVector;
            // 记录角色需要朝向的目标方向
            _lookInputVector = cameraPlanarDirection;

            // 处理跳跃输入
            if (inputs.JumpDown)
            {
                _timeSinceJumpRequested = 0f;
                _jumpRequested = true;
            }
        }

        /// <summary>
        /// 角色更新前的预处理（由KinematicCharacterMotor调用）
        /// 空方法，暂无需处理
        /// </summary>
        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// 更新角色旋转（由KinematicCharacterMotor调用）
        /// 这是唯一允许设置角色旋转的地方
        /// </summary>
        /// <param name="currentRotation">当前角色旋转（引用传递，直接修改）</param>
        /// <param name="deltaTime">帧时间</param>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 有视角输入且朝向锐度大于0时，平滑转向目标方向
            if (_lookInputVector != Vector3.zero && OrientationSharpness > 0f)
            {
                // 使用指数插值平滑转向（比Lerp更自然）
                Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                // 设置角色最终旋转（由电机应用）
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
            }
        }

        /// <summary>
        /// 更新角色速度（由KinematicCharacterMotor调用）
        /// 这是唯一允许设置角色速度的地方
        /// </summary>
        /// <param name="currentVelocity">当前角色速度（引用传递，直接修改）</param>
        /// <param name="deltaTime">帧时间</param>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            Vector3 targetMovementVelocity = Vector3.zero;

            // ========== 地面移动逻辑 ==========
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                // 重新调整速度方向以匹配斜坡（防止角色在斜坡上悬空）
                currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

                // 计算地面目标移动速度
                Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                Vector3 reorientedInput = Vector3.Cross(Motor.GroundingStatus.GroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;

                // 平滑插值到目标速度（指数插值更自然）
                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-StableMovementSharpness * deltaTime));
            }
            // ========== 空中移动逻辑 ==========
            else
            {
                // 空中移动输入处理
                if (_moveInputVector.sqrMagnitude > 0f)
                {
                    targetMovementVelocity = _moveInputVector * MaxAirMoveSpeed;

                    // 防止角色在不稳定斜坡上通过空中移动攀爬
                    if (Motor.GroundingStatus.FoundAnyGround)
                    {
                        Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                        targetMovementVelocity = Vector3.ProjectOnPlane(targetMovementVelocity, perpenticularObstructionNormal);
                    }

                    // 计算速度差值并仅在重力垂直平面上应用加速度
                    Vector3 velocityDiff = Vector3.ProjectOnPlane(targetMovementVelocity - currentVelocity, Gravity);
                    currentVelocity += velocityDiff * AirAccelerationSpeed * deltaTime;
                }

                // 应用重力
                currentVelocity += Gravity * deltaTime;

                // 应用空中阻力
                currentVelocity *= (1f / (1f + (Drag * deltaTime)));
            }

            // ========== 跳跃逻辑 ==========
            _jumpedThisFrame = false;
            _timeSinceJumpRequested += deltaTime;
            if (_jumpRequested)
            {
                // 检查是否满足跳跃条件：未消耗跳跃请求 + (滑动时抉择 或 在土狼时间内)
                if (!_jumpConsumed 
                    && (
                    (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                    || _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime))
                {
                    
                    // 计算跳跃方向（优先地面法线，其次角色向上）
                    Vector3 jumpDirection = Motor.CharacterUp;
                    if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                    {
                        jumpDirection = Motor.GroundingStatus.GroundNormal;
                    }

                    // 强制解除接地（防止跳跃时仍吸附在地面）
                    Motor.ForceUnground(0.1f);

                    // 应用跳跃速度（抵消当前垂直速度，避免重力影响跳跃高度）
                    currentVelocity += (jumpDirection * JumpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                    // 重置跳跃状态
                    _jumpRequested = false;
                    _jumpConsumed = true;
                    _jumpedThisFrame = true;
                }
            }
        }

        /// <summary>
        /// 角色更新后的处理（由KinematicCharacterMotor调用）
        /// 处理跳跃相关的状态重置和容错时间
        /// </summary>
        public void AfterCharacterUpdate(float deltaTime)
        {
            // 处理跳跃容错逻辑
            {
                // 超过预接地容错时间则取消跳跃请求
                if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                {
                    _jumpRequested = false;
                }

                // 接地状态下重置跳跃消耗标记（允许再次跳跃）
                if (AllowJumpingWhenSliding 
                        ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                {
                    if (!_jumpedThisFrame)
                    {
                        _jumpConsumed = false;
                    }
                    _timeSinceLastAbleToJump = 0f;
                }
                else
                {
                    // 空中时累计距离上次可跳跃的时间（用于后接地容错）
                    _timeSinceLastAbleToJump += deltaTime;
                }
            }
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void PostGroundingUpdate(float deltaTime)
        {
        }

        public void AddVelocity(Vector3 velocity)
        {
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
}