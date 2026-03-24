using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;

namespace KinematicCharacterController.Walkthrough.Crouching
{
    // 玩家角色输入结构体：存储角色的移动、视角、跳跃、下蹲等核心输入信息
    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward; // 向前移动轴（WS键/左摇杆上下，范围-1到1）
        public float MoveAxisRight;   // 向右移动轴（AD键/左摇杆左右，范围-1到1）
        public Quaternion CameraRotation; // 主相机的旋转角度（用于匹配移动/朝向与相机视角）
        public bool JumpDown; // 跳跃按键是否按下（帧级检测，仅按键按下的那一帧为true）
        public bool CrouchDown; // 下蹲按键按下（帧级检测）
        public bool CrouchUp; // 下蹲按键抬起（帧级检测）
    }

    // 自定义角色控制器（下蹲功能版）：实现ICharacterController接口
    // 核心功能：基础移动、跳跃（含二段跳/墙跳）+ 冲量添加 + 动态下蹲/起身（带障碍物检测）
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
        public Transform MeshRoot; // 角色模型根节点（用于下蹲/起身时缩放视觉表现）

        // 私有运行时状态变量（仅控制器内部使用）
        private Collider[] _probedColliders = new Collider[8]; // 碰撞检测缓存数组（用于下蹲起身时检测头顶障碍物）
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
        private bool _shouldBeCrouching = false; // 标记是否「应该」下蹲（输入驱动）
        private bool _isCrouching = false; // 标记角色「当前」是否处于下蹲状态（状态驱动）

        private void Start()
        {
            // 将当前控制器绑定到角色电机，让电机驱动控制器的逻辑
            Motor.CharacterController = this;
        }

        /// <summary>
        /// 设置角色输入（由玩家输入脚本每帧调用）
        /// 作用：将原始输入转换为角色空间的可用数据，统一处理移动/视角/跳跃/下蹲输入
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

            // 处理下蹲输入
            if (inputs.CrouchDown)
            {
                // 标记「应该下蹲」
                _shouldBeCrouching = true;

                // 如果当前未下蹲，则执行下蹲逻辑
                if (!_isCrouching)
                {
                    _isCrouching = true;
                    // 修改胶囊体尺寸（半径0.5，高度1，中心偏移0.5）—— 下蹲状态
                    Motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                    // 缩放模型根节点（视觉上表现为下蹲）
                    MeshRoot.localScale = new Vector3(1f, 0.5f, 1f);
                }
            }
            else if (inputs.CrouchUp)
            {
                // 标记「应该取消下蹲」
                _shouldBeCrouching = false;
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

            // 处理外部添加的冲量（如击退、冲刺、爆炸等效果）
            if (_internalVelocityAdd.sqrMagnitude > 0f)
            {
                currentVelocity += _internalVelocityAdd;
                _internalVelocityAdd = Vector3.zero; // 冲量仅生效一帧，避免持续叠加
            }
        }

        /// <summary>
        /// 角色更新后的处理（由KinematicCharacterMotor调用）
        /// 作用：处理跳跃容错时间、重置接地状态下的跳跃标记 + 处理取消下蹲的逻辑（带障碍物检测）
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

            // 处理取消下蹲的逻辑（起身检测）
            if (_isCrouching && !_shouldBeCrouching)
            {
                // 临时将胶囊体恢复为站立尺寸（半径0.5，高度2，中心偏移1），检测头顶是否有障碍物
                Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                // 执行重叠检测：判断站立尺寸下是否会碰撞到其他物体
                if (Motor.CharacterCollisionsOverlap(
                        Motor.TransientPosition,
                        Motor.TransientRotation,
                        _probedColliders) > 0)
                {
                    // 检测到障碍物 → 保持下蹲尺寸，无法起身
                    Motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                }
                else
                {
                    // 无障碍物 → 恢复站立状态：重置模型缩放 + 标记为非下蹲
                    MeshRoot.localScale = new Vector3(1f, 1f, 1f);
                    _isCrouching = false;
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

        #region Gizmos可视化
        /// <summary>
        /// 绘制胶囊体关键点和CharacterCollisionsOverlap检测范围Gizmos（仅当物体被选中时显示）
        /// 用于可视化KCC的胶囊体几何参数和重叠检测范围
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (Motor == null) return;

            // 获取胶囊体关键点的世界坐标
            Vector3 capsuleCenterWorld = Motor.TransientPosition + (Motor.TransientRotation * Motor.CharacterTransformToCapsuleCenter);
            Vector3 capsuleBottomWorld = Motor.TransientPosition + (Motor.TransientRotation * Motor.CharacterTransformToCapsuleBottom);
            Vector3 capsuleTopWorld = Motor.TransientPosition + (Motor.TransientRotation * Motor.CharacterTransformToCapsuleTop);
            Vector3 capsuleBottomHemiWorld = Motor.TransientPosition + (Motor.TransientRotation * Motor.CharacterTransformToCapsuleBottomHemi);
            Vector3 capsuleTopHemiWorld = Motor.TransientPosition + (Motor.TransientRotation * Motor.CharacterTransformToCapsuleTopHemi);

            // 获取胶囊体半径
            float radius = Motor.Capsule.radius;
            float height = Motor.Capsule.height;

            // ========================================
            // 1. 绘制胶囊体整体轮廓（白色线框）
            // ========================================
            Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
            DrawCapsuleGizmos(capsuleBottomWorld, capsuleTopWorld, radius);

            // ========================================
            // 2. 绘制各关键点（用不同颜色的球体表示）
            // ========================================

            // Center - 胶囊体几何中心（黄色）
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(capsuleCenterWorld, radius * 0.15f);
            GizmosExtensions.DrawLabel(capsuleCenterWorld + Vector3.up * 0.3f, "Center\n(胶囊体中心)", Color.yellow);

            // Bottom - 胶囊体最低点 = Transform位置（红色）
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(capsuleBottomWorld, radius * 0.12f);
            GizmosExtensions.DrawLabel(capsuleBottomWorld + Vector3.right * 0.3f, "Bottom\n(胶囊体最低点)", Color.red);

            // Top - 胶囊体最高点（绿色）
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(capsuleTopWorld, radius * 0.12f);
            GizmosExtensions.DrawLabel(capsuleTopWorld + Vector3.right * 0.3f, "Top\n(胶囊体最高点)", Color.green);

            // BottomHemi - 下半球与圆柱体交界处，用于Overlap检测（橙色）
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireSphere(capsuleBottomHemiWorld, radius * 0.1f);
            GizmosExtensions.DrawLabel(capsuleBottomHemiWorld + Vector3.right * 0.25f, "BottomHemi\n(Overlap检测底部)", new Color(1f, 0.5f, 0f));

            // TopHemi - 上半球与圆柱体交界处，用于Overlap检测（青色）
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(capsuleTopHemiWorld, radius * 0.1f);
            GizmosExtensions.DrawLabel(capsuleTopHemiWorld + Vector3.right * 0.25f, "TopHemi\n(Overlap检测顶部)", Color.cyan);

            // ========================================
            // 3. CharacterCollisionsOverlap 检测范围可视化
            // ========================================

            // 绘制 Overlap 检测用的胶囊体扫描范围（用虚线表示）
            Gizmos.color = new Color(1f, 0f, 1f, 0.6f); // 紫色
            DrawCapsuleGizmos(capsuleBottomHemiWorld, capsuleTopHemiWorld, radius);
            
            // 绘制 BottomHemi 到 TopHemi 的中心轴线（紫色）
            Gizmos.color = new Color(1f, 0f, 1f, 0.8f);
            Gizmos.DrawLine(capsuleBottomHemiWorld, capsuleTopHemiWorld);
            
            // 标注 Overlap 检测范围
            Vector3 overlapLabelPos = capsuleCenterWorld + Vector3.left * (radius + 0.4f);
            GizmosExtensions.DrawLabel(overlapLabelPos, "Overlap检测范围\n(BottomHemi ↔ TopHemi)", new Color(1f, 0f, 1f));

            // ========================================
            // 4. 绘制各点之间的连接线
            // ========================================

            // 圆柱中心轴（黄色）
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            Gizmos.DrawLine(capsuleBottomWorld, capsuleTopWorld);

            // Bottom ↔ BottomHemi 偏移线（橙色）
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawLine(capsuleBottomWorld, capsuleBottomHemiWorld);
            GizmosExtensions.DrawLabel(
                (capsuleBottomWorld + capsuleBottomHemiWorld) * 0.5f + Vector3.back * 0.15f,
                $"+{radius:F2}",
                new Color(1f, 0.5f, 0f, 0.8f)
            );

            // Top ↔ TopHemi 偏移线（青色）
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawLine(capsuleTopWorld, capsuleTopHemiWorld);
            GizmosExtensions.DrawLabel(
                (capsuleTopWorld + capsuleTopHemiWorld) * 0.5f + Vector3.back * 0.15f,
                $"-{radius:F2}",
                new Color(0f, 1f, 1f, 0.8f)
            );

            // ========================================
            // 5. 绘制当前检测到的重叠物体（如果有）
            // ========================================
            DrawOverlappedCollidersGizmos();
        }

        /// <summary>
        /// 绘制当前检测到的重叠物体（辅助方法）
        /// 用于可视化 CharacterCollisionsOverlap 的检测结果
        /// </summary>
        private void DrawOverlappedCollidersGizmos()
        {
            if (_probedColliders == null || _probedColliders.Length == 0) return;

            int overlapCount = 0;
            for (int i = 0; i < _probedColliders.Length; i++)
            {
                if (_probedColliders[i] != null)
                {
                    overlapCount++;

                    // 获取重叠物体的碰撞点（使用包围盒中心作为近似）
                    Collider collider = _probedColliders[i];
                    Bounds colliderBounds = collider.bounds;
                    Vector3 closestPoint = colliderBounds.ClosestPoint(Motor.TransientPosition);

                    // 绘制连线到重叠物体
                    Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f); // 红色
                    Gizmos.DrawLine(Motor.TransientPosition, closestPoint);

                    // 在重叠物体位置绘制标记
                    Gizmos.color = new Color(1f, 0.5f, 0f, 1f); // 橙色
                    Gizmos.DrawWireCube(closestPoint, Vector3.one * 0.15f);

                    // 标注重叠物体
                    GizmosExtensions.DrawLabel(
                        closestPoint + Vector3.up * 0.2f,
                        $"Overlap #{overlapCount}\n{collider.name}",
                        new Color(1f, 0.3f, 0.3f)
                    );
                }
            }

            // 标注重叠检测结果
            if (overlapCount > 0)
            {
                Vector3 resultLabelPos = Motor.TransientPosition + Vector3.up * (Motor.Capsule.height + 0.5f);
                GizmosExtensions.DrawLabel(
                    resultLabelPos,
                    $"CharacterCollisionsOverlap\n检测到 {overlapCount} 个重叠物体",
                    Color.red
                );
            }
        }

        /// <summary>
        /// 绘制胶囊体线框（辅助方法）
        /// </summary>
        private void DrawCapsuleGizmos(Vector3 bottom, Vector3 top, float radius)
        {
            // 绘制顶部和底部的圆形
            Gizmos.DrawWireSphere(bottom, radius);
            Gizmos.DrawWireSphere(top, radius);

            // 绘制连接线
            Vector3 up = (top - bottom).normalized;
            Vector3 right = Vector3.Cross(up, Vector3.forward).normalized;
            if (right.sqrMagnitude < 0.01f)
            {
                right = Vector3.Cross(up, Vector3.right).normalized;
            }
            Vector3 forward = Vector3.Cross(right, up).normalized;

            // 绘制4条连接线
            Gizmos.DrawLine(bottom + right * radius, top + right * radius);
            Gizmos.DrawLine(bottom - right * radius, top - right * radius);
            Gizmos.DrawLine(bottom + forward * radius, top + forward * radius);
            Gizmos.DrawLine(bottom - forward * radius, top - forward * radius);
        }
        #endregion
    }
}

// Gizmos扩展类：用于在Scene视图绘制文字标签
public static class GizmosExtensions
{
    /// <summary>
    /// 在Scene视图绘制文字标签
    /// </summary>
    public static void DrawLabel(Vector3 position, string text, Color color)
    {
#if UNITY_EDITOR
        GUIStyle style = new GUIStyle();
        style.normal.textColor = color;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        
        UnityEditor.Handles.Label(position, text, style);
#endif
    }
}