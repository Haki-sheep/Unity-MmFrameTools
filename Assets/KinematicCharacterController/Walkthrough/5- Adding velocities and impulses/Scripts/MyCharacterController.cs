using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;

namespace KinematicCharacterController.Walkthrough.AddingImpulses
{
    // 玩家角色输入结构体：存储角色的移动、视角、跳跃等核心输入信息
    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward; // 向前移动轴（WS键/左摇杆上下，范围-1到1）
        public float MoveAxisRight;   // 向右移动轴（AD键/左摇杆左右，范围-1到1）
        public Quaternion CameraRotation; // 主相机的旋转角度（用于匹配移动/朝向与相机视角）
        public bool JumpDown; // 跳跃按键是否按下（帧级检测，仅按键按下的那一帧为true）
    }

    // 自定义角色控制器（添加冲量版）：实现ICharacterController接口
    // 核心功能：基础移动、跳跃（含二段跳/墙跳）+ 支持外部添加冲量（击退/冲刺等）
    public class MyCharacterController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor; // 运动学角色电机（核心驱动组件，负责物理移动计算）

        [Header("地面稳定移动参数")]
        public float MaxStableMoveSpeed = 10f; // 地面最大移动速度（单位：米/秒）
        public float StableMovementSharpness = 15; // 地面移动响应锐度（值越大加速/减速越快）
        public float OrientationSharpness = 10; // 角色朝向响应锐度（值越大转向越灵敏）

        [Header("空中移动参数")]
        public float MaxAirMoveSpeed = 10f; // 空中最大移动速度（单位：米/秒）
        public float AirAccelerationSpeed = 5f; // 空中加速度（单位：米/秒²）
        public float Drag = 0.1f; // 空中阻力系数（值越大，空中速度衰减越快）

        [Header("跳跃参数")]
        public bool AllowJumpingWhenSliding = false; // 是否允许在斜坡滑动（非稳定接地）时跳跃
        public bool AllowDoubleJump = false; // 是否启用二段跳功能
        public bool AllowWallJump = false; // 是否启用墙跳功能
        public float JumpSpeed = 10f; // 跳跃初速度（单位：米/秒）
        public float JumpPreGroundingGraceTime = 0f; // 跳跃预接地容错时间（提前按跳，落地后仍能触发）
        public float JumpPostGroundingGraceTime = 0f; // 跳跃后接地容错时间（土狼时间，离地短时间内仍可跳）

        [Header("通用参数")]
        public Vector3 Gravity = new Vector3(0, -30f, 0); // 重力加速度（Y轴为负表示向下，默认-30接近真实重力）
        public Transform MeshRoot; // 角色模型根节点（仅用于视觉表现，不影响核心移动逻辑）

        // 私有运行时状态变量（仅控制器内部使用）
        private Vector3 _moveInputVector; // 处理后的移动输入向量（已转换为世界空间，基于相机朝向）
        private Vector3 _lookInputVector; // 角色朝向目标向量（用于平滑转向）
        private bool _jumpRequested = false; // 是否有未处理的跳跃请求
        private bool _jumpConsumed = false; // 基础跳跃是否已被消耗（防止单次接地连续跳）
        private bool _jumpedThisFrame = false; // 本帧是否执行了跳跃（用于状态防重置）
        private float _timeSinceJumpRequested = Mathf.Infinity; // 距离上次跳跃请求的时间（用于容错时间判断）
        private float _timeSinceLastAbleToJump = 0f; // 距离上次可跳跃状态的时间（土狼时间核心变量）
        private bool _doubleJumpConsumed = false; // 二段跳是否已被消耗
        private bool _canWallJump = false; // 当前帧是否满足墙跳条件
        private Vector3 _wallJumpNormal; // 墙跳时的墙面法线（决定墙跳的反弹方向）
        private Vector3 _internalVelocityAdd = Vector3.zero; // 内部冲量缓存（用于外部添加临时速度，如击退/冲刺）

        private void Start()
        {
            // 将当前控制器绑定到角色电机，让电机驱动控制器的逻辑
            Motor.CharacterController = this;
        }

        /// <summary>
        /// 设置角色输入（由玩家输入脚本每帧调用）
        /// 作用：将原始输入转换为角色空间的可用数据，统一处理移动/视角/跳跃输入
        /// </summary>
        /// <param name="inputs">玩家原始输入数据（按帧传递）</param>
        public void SetInputs(ref PlayerCharacterInputs inputs)
        {
            // 限制移动输入的长度为1（防止斜向移动时速度叠加导致超速）
            Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

            // 计算相机在角色平面上的朝向和旋转（消除垂直方向影响，仅保留水平朝向）
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
            // 容错：如果相机正对着天/地，改用相机向上方向计算
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
            }
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

            // 记录移动和朝向输入向量
            _moveInputVector = cameraPlanarRotation * moveInputVector;
            _lookInputVector = cameraPlanarDirection;

            // 处理跳跃输入：按下跳跃键时记录请求，并重置请求计时
            if (inputs.JumpDown)
            {
                _timeSinceJumpRequested = 0f;
                _jumpRequested = true;
            }
        }

        /// <summary>
        /// 角色更新前的预处理（由KinematicCharacterMotor在每帧更新时调用）
        /// 可用于帧前状态重置、数据准备等（当前无逻辑，预留扩展）
        /// </summary>
        /// <param name="deltaTime">帧时间（秒）</param>
        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// 更新角色旋转（由KinematicCharacterMotor调用，这是唯一允许设置角色旋转的地方）
        /// 作用：根据视角输入平滑调整角色朝向，保证转向自然
        /// </summary>
        /// <param name="currentRotation">当前角色旋转（引用传递，直接修改生效）</param>
        /// <param name="deltaTime">帧时间（秒）</param>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 有视角输入且朝向锐度大于0时，执行平滑转向
            if (_lookInputVector != Vector3.zero && OrientationSharpness > 0f)
            {
                // 指数插值平滑转向（比普通Lerp更自然，接近目标时减速）
                Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                // 设置角色最终旋转（由电机自动应用到Transform）
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
            }
        }

        /// <summary>
        /// 更新角色速度（由KinematicCharacterMotor调用，这是唯一允许设置角色速度的地方）
        /// 核心逻辑：区分地面/空中移动规则，处理跳跃（基础/二段/墙跳）、重力、阻力 + 外部冲量
        /// </summary>
        /// <param name="currentVelocity">当前角色速度（引用传递，直接修改生效）</param>
        /// <param name="deltaTime">帧时间（秒）</param>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            Vector3 targetMovementVelocity = Vector3.zero;
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                // 地面稳定移动逻辑：重新调整速度方向以匹配斜坡（防止角色在斜坡上悬空）
                currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

                // 计算地面目标移动速度（适配斜坡方向）
                Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                Vector3 reorientedInput = Vector3.Cross(Motor.GroundingStatus.GroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;

                // 指数插值平滑到目标速度（地面移动更跟手）
                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-StableMovementSharpness * deltaTime));
            }
            else
            {
                // 空中移动逻辑：处理移动输入
                if (_moveInputVector.sqrMagnitude > 0f)
                {
                    targetMovementVelocity = _moveInputVector * MaxAirMoveSpeed;

                    // 防止角色通过空中移动攀爬不稳定斜坡（限制移动方向）
                    if (Motor.GroundingStatus.FoundAnyGround)
                    {
                        Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                        targetMovementVelocity = Vector3.ProjectOnPlane(targetMovementVelocity, perpenticularObstructionNormal);
                    }

                    // 仅在重力垂直平面上应用空中加速度（避免重力方向干扰移动）
                    Vector3 velocityDiff = Vector3.ProjectOnPlane(targetMovementVelocity - currentVelocity, Gravity);
                    currentVelocity += velocityDiff * AirAccelerationSpeed * deltaTime;
                }

                // 应用重力（空中核心物理）
                currentVelocity += Gravity * deltaTime;

                // 应用空中阻力（模拟空气阻力，让空中移动有减速感）
                currentVelocity *= (1f / (1f + (Drag * deltaTime)));
            }

            // ========== 跳跃核心逻辑（基础跳/二段跳/墙跳） ==========
            {
                _jumpedThisFrame = false;
                _timeSinceJumpRequested += deltaTime;
                if (_jumpRequested)
                {
                    // 处理二段跳逻辑
                    if (AllowDoubleJump)
                    {
                        // 二段跳触发条件：基础跳已消耗 + 二段跳未消耗 + 不在地面（或斜坡滑动时不允许跳）
                        if (_jumpConsumed && !_doubleJumpConsumed && (AllowJumpingWhenSliding ? !Motor.GroundingStatus.FoundAnyGround : !Motor.GroundingStatus.IsStableOnGround))
                        {
                            // 强制解除接地（避免二段跳时角色仍吸附地面）
                            Motor.ForceUnground(0.1f);

                            // 应用二段跳速度（抵消当前垂直速度，保证跳跃高度一致）
                            currentVelocity += (Motor.CharacterUp * JumpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                            _jumpRequested = false;
                            _doubleJumpConsumed = true;
                            _jumpedThisFrame = true;
                        }
                    }

                    // 处理基础跳/墙跳逻辑
                    if (_canWallJump ||
                        (!_jumpConsumed && ((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime)))
                    {
                        // 计算跳跃方向：墙跳→斜坡→默认向上
                        Vector3 jumpDirection = Motor.CharacterUp;
                        if (_canWallJump)
                        {
                            jumpDirection = _wallJumpNormal; // 墙跳沿墙面法线方向反弹
                        }
                        else if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                        {
                            jumpDirection = Motor.GroundingStatus.GroundNormal; // 斜坡跳跃沿斜坡法线方向
                        }

                        // 强制解除接地（核心：防止跳跃时角色仍吸附在地面/墙面）
                        Motor.ForceUnground(0.1f);

                        // 应用跳跃速度（抵消当前垂直速度，避免重力影响跳跃高度）
                        currentVelocity += (jumpDirection * JumpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                        _jumpRequested = false;
                        _jumpConsumed = true;
                        _jumpedThisFrame = true;
                    }
                }

                // 重置墙跳状态（每帧重置，避免持续触发墙跳）
                _canWallJump = false;
            }
#region 冲量应用
            // 处理外部添加的冲量（如击退、冲刺、爆炸等效果）
            if (_internalVelocityAdd.sqrMagnitude > 0f)
            {
                currentVelocity += _internalVelocityAdd;
                _internalVelocityAdd = Vector3.zero; // 冲量仅生效一帧，避免持续叠加
            }
#endregion
        }

        /// <summary>
        /// 角色更新后的处理（由KinematicCharacterMotor调用）
        /// 作用：处理跳跃容错时间、重置接地状态下的跳跃标记
        /// </summary>
        /// <param name="deltaTime">帧时间（秒）</param>
        public void AfterCharacterUpdate(float deltaTime)
        {
            // 处理跳跃容错逻辑
            {
                // 超过预接地容错时间，取消跳跃请求（避免无效请求堆积）
                if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                {
                    _jumpRequested = false;
                }

                // 接地状态下重置跳跃消耗标记（允许再次跳跃）
                if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                {
                    // 本帧未跳跃时才重置（避免落地瞬间重置导致多跳）
                    if (!_jumpedThisFrame)
                    {
                        _doubleJumpConsumed = false;
                        _jumpConsumed = false;
                    }
                    _timeSinceLastAbleToJump = 0f;
                }
                else
                {
                    // 空中累计土狼时间（离地后短时间内仍可跳）
                    _timeSinceLastAbleToJump += deltaTime;
                }
            }
        }

        /// <summary>
        /// 检查碰撞体是否有效（由KinematicCharacterMotor调用）
        /// 作用：过滤不需要响应的碰撞体（如触发器、忽略层），当前默认全部有效
        /// </summary>
        /// <param name="coll">待检测的碰撞体</param>
        /// <returns>是否有效（true=响应碰撞，false=忽略）</returns>
        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        /// <summary>
        /// 角色接触地面时的回调（由KinematicCharacterMotor调用）
        /// 可扩展：落地震动、音效、粒子特效等（当前无逻辑，预留扩展）
        /// </summary>
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        /// <summary>
        /// 角色移动碰撞到物体时的回调（由KinematicCharacterMotor调用）
        /// 核心作用：检测墙跳条件，记录墙面法线
        /// </summary>
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            // 墙跳触发条件：开启墙跳 + 不在稳定地面 + 碰撞到非稳定障碍物（墙面）
            if (AllowWallJump && !Motor.GroundingStatus.IsStableOnGround && !hitStabilityReport.IsStable)
            {
                _canWallJump = true;
                _wallJumpNormal = hitNormal;
            }
        }

        /// <summary>
        /// 处理碰撞稳定性报告（由KinematicCharacterMotor调用）
        /// 可扩展：自定义碰撞稳定性判断（当前无逻辑，预留扩展）
        /// </summary>
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        /// <summary>
        /// 角色接地状态更新后的回调（由KinematicCharacterMotor调用）
        /// （当前无逻辑，预留扩展）
        /// </summary>
        /// <param name="deltaTime">帧时间（秒）</param>
        public void PostGroundingUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// 给角色添加额外速度（由外部调用，如击退、冲刺效果）
        /// </summary>
        /// <param name="velocity">要添加的速度向量</param>
        public void AddVelocity(Vector3 velocity)
        {
            _internalVelocityAdd += velocity;
        }

        /// <summary>
        /// 检测到离散碰撞时的回调（由KinematicCharacterMotor调用）
        /// 可扩展：处理快速移动时的碰撞穿透（当前无逻辑，预留扩展）
        /// </summary>
        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
}