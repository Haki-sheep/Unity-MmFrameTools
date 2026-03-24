using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;

namespace KinematicCharacterController.Walkthrough.MultipleMovementStates
{
    /// <summary>
    /// 角色状态枚举
    /// 用于区分角色不同的移动/行为状态（可扩展：奔跑、冲刺、攀爬等）
    /// </summary>
    public enum CharacterState
    {
        Default, // 默认状态
    }

    /// <summary>
    /// 玩家角色输入结构体
    /// 存储角色的所有输入信息（移动、视角、跳跃、蹲伏）
    /// </summary>
    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward; // 向前移动轴（WS键/左摇杆上下）
        public float MoveAxisRight;   // 向右移动轴（AD键/左摇杆左右）
        public Quaternion CameraRotation; // 相机旋转角度
        public bool JumpDown; // 跳跃按键按下（帧）
        public bool CrouchDown; // 蹲伏按键按下（帧）
        public bool CrouchUp; // 蹲伏按键抬起（帧）
    }

    /// <summary>
    /// 自定义角色控制器核心类（多状态版本）
    /// 实现ICharacterController接口，支持基于状态的移动逻辑管理
    /// </summary>
    public class MyCharacterController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor; // KCC核心电机组件

        [Header("稳定移动设置")]
        public float MaxStableMoveSpeed = 10f; // 地面最大移动速度
        public float StableMovementSharpness = 15; // 地面移动响应锐度（数值越大加速越快）
        public float OrientationSharpness = 10; // 角色朝向响应锐度
        public float MaxStableDistanceFromLedge = 5f; // 边缘稳定站立的最大距离（防止角色从边缘滑落）
        [Range(0f, 180f)]
        public float MaxStableDenivelationAngle = 180f; // 稳定地面的最大坡度角（超过则判定为斜坡/不可站立）

        [Header("空中移动设置")]
        public float MaxAirMoveSpeed = 10f; // 空中最大移动速度
        public float AirAccelerationSpeed = 5f; // 空中加速度
        public float Drag = 0.1f; // 空中拖拽阻力

        [Header("跳跃设置")]
        public bool AllowJumpingWhenSliding = false; // 允许在斜坡滑动时跳跃
        public bool AllowDoubleJump = false; // 允许二段跳
        public bool AllowWallJump = false; // 允许墙跳
        public float JumpSpeed = 10f; // 跳跃初速度
        public float JumpPreGroundingGraceTime = 0f; // 跳跃预接地容错时间（落地前按跳也能触发）
        public float JumpPostGroundingGraceTime = 0f; // 跳跃后接地容错时间（落地后短时间仍能跳）

        [Header("其他设置")]
        public List<Collider> IgnoredColliders = new List<Collider>(); // 忽略碰撞的碰撞体列表
        public bool OrientTowardsGravity = false; // 是否朝向重力方向（适配斜面/反重力场景）
        public Vector3 Gravity = new Vector3(0, -30f, 0); // 重力加速度
        public Transform MeshRoot; // 角色模型根节点（用于蹲伏时缩放）

        public CharacterState CurrentCharacterState { get; private set; } // 当前角色状态
        
        private Collider[] _probedColliders = new Collider[8]; // 碰撞检测缓存数组（避免频繁GC）
        private Vector3 _moveInputVector; // 处理后的移动输入向量
        private Vector3 _lookInputVector; // 处理后的朝向输入向量
        private bool _jumpRequested = false; // 跳跃请求标记
        private bool _jumpConsumed = false; // 普通跳跃已使用标记
        private bool _doubleJumpConsumed = false; // 二段跳已使用标记
        private bool _jumpedThisFrame = false; // 本帧是否触发了跳跃
        private bool _canWallJump = false; // 可墙跳标记
        private Vector3 _wallJumpNormal; // 墙跳的墙面法线方向
        private float _timeSinceJumpRequested = Mathf.Infinity; // 距离上次跳跃请求的时间
        private float _timeSinceLastAbleToJump = 0f; // 距离上次可跳跃状态的时间
        private Vector3 _internalVelocityAdd = Vector3.zero; // 额外叠加的速度（如外力推动）
        private bool _shouldBeCrouching = false; // 应该蹲伏的标记（输入驱动）
        private bool _isCrouching = false; // 当前是否处于蹲伏状态

        private void Start()
        {
            // 将当前控制器赋值给KCC电机（核心关联步骤）
            Motor.CharacterController = this;

            // 初始化角色状态
            TransitionToState(CharacterState.Default);
        }

        /// <summary>
        /// 处理角色状态切换，包含状态退出/进入的回调
        /// </summary>
        public void TransitionToState(CharacterState newState)
        {
            CharacterState tmpInitialState = CurrentCharacterState;
            OnStateExit(tmpInitialState, newState); // 退出旧状态
            CurrentCharacterState = newState; // 更新当前状态
            OnStateEnter(newState, tmpInitialState); // 进入新状态
        }

        /// <summary>
        /// 进入状态时的回调事件
        /// </summary>
        public void OnStateEnter(CharacterState state, CharacterState fromState)
        {
            switch (state)
            {
                case CharacterState.Default:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// 退出状态时的回调事件
        /// </summary>
        public void OnStateExit(CharacterState state, CharacterState toState)
        {
            switch (state)
            {
                case CharacterState.Default:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// 由MyPlayer脚本每帧调用，用于向角色传递输入信息
        /// </summary>
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

            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // 转换移动/朝向输入为角色本地空间
                        _moveInputVector = cameraPlanarRotation * moveInputVector;
                        _lookInputVector = cameraPlanarDirection;

                        // 处理跳跃输入
                        if (inputs.JumpDown)
                        {
                            _timeSinceJumpRequested = 0f;
                            _jumpRequested = true;
                        }

                        // 处理蹲伏输入
                        if (inputs.CrouchDown)
                        {
                            _shouldBeCrouching = true;

                            if (!_isCrouching)
                            {
                                _isCrouching = true;
                                // 设置蹲伏时的胶囊碰撞体尺寸（半径、高度、中心偏移）
                                Motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                                // 缩放角色模型
                                MeshRoot.localScale = new Vector3(1f, 0.5f, 1f);
                            }
                        }
                        else if (inputs.CrouchUp)
                        {
                            _shouldBeCrouching = false;
                        }

                        break;
                    }
            }
        }

        /// <summary>
        /// （由KinematicCharacterMotor在更新周期中调用）
        /// 角色开始移动更新前的预处理逻辑
        /// </summary>
        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// （由KinematicCharacterMotor在更新周期中调用）
        /// 在此方法中设置角色当前的旋转角度
        /// 【注意】这是唯一允许设置角色旋转的地方
        /// </summary>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        if (_lookInputVector != Vector3.zero && OrientationSharpness > 0f)
                        {
                            // 平滑插值当前朝向到目标朝向（指数缓动，更自然）
                            Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                            // 设置当前旋转（会被KinematicCharacterMotor使用）
                            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
                        }
                        if (OrientTowardsGravity)
                        {
                            // 旋转角色使其“上”方向朝向重力反方向（适配斜面/反重力场景）
                            currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -Gravity) * currentRotation;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// （由KinematicCharacterMotor在更新周期中调用）
        /// 在此方法中设置角色当前的速度
        /// 【注意】这是唯一允许设置角色速度的地方
        /// </summary>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
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

                            // 平滑地面移动速度（指数缓动）
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

                        // 跳跃逻辑处理
                        {
                            _jumpedThisFrame = false;
                            _timeSinceJumpRequested += deltaTime;
                            if (_jumpRequested)
                            {
                                // 处理二段跳
                                if (AllowDoubleJump)
                                {
                                    if (_jumpConsumed && !_doubleJumpConsumed && (AllowJumpingWhenSliding ? !Motor.GroundingStatus.FoundAnyGround : !Motor.GroundingStatus.IsStableOnGround))
                                    {
                                        // 强制脱离地面（避免跳跃时仍吸附地面）
                                        Motor.ForceUnground(0.1f);

                                        // 叠加跳跃速度（抵消当前垂直速度）
                                        currentVelocity += (Motor.CharacterUp * JumpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                                        _jumpRequested = false;
                                        _doubleJumpConsumed = true;
                                        _jumpedThisFrame = true;
                                    }
                                }

                                // 检查是否满足跳跃条件（墙跳/普通跳/容错期跳）
                                if (_canWallJump ||
                                    (!_jumpConsumed && ((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime)))
                                {
                                    // 计算跳跃方向（墙跳用墙面法线，斜坡用地面法线，普通跳用角色上方向）
                                    Vector3 jumpDirection = Motor.CharacterUp;
                                    if (_canWallJump)
                                    {
                                        jumpDirection = _wallJumpNormal;
                                    }
                                    else if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                                    {
                                        jumpDirection = Motor.GroundingStatus.GroundNormal;
                                    }

                                    // 强制脱离地面（0.1秒内不检测地面，避免跳跃时吸附）
                                    // 【注】注释掉这行后，角色跳跃时会仍吸附地面，可自行测试效果
                                    Motor.ForceUnground(0.1f);

                                    // 叠加跳跃速度（抵消当前垂直速度）
                                    currentVelocity += (jumpDirection * JumpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                                    _jumpRequested = false;
                                    _jumpConsumed = true;
                                    _jumpedThisFrame = true;
                                }
                            }

                            // 重置墙跳标记（每帧重置，避免持续触发）
                            _canWallJump = false;
                        }

                        // 叠加额外速度（如外力推动）
                        if (_internalVelocityAdd.sqrMagnitude > 0f)
                        {
                            currentVelocity += _internalVelocityAdd;
                            _internalVelocityAdd = Vector3.zero;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// （由KinematicCharacterMotor在更新周期中调用）
        /// 角色完成移动更新后的收尾逻辑
        /// </summary>
        public void AfterCharacterUpdate(float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // 跳跃相关状态重置
                        {
                            // 跳跃预接地容错期超时，取消跳跃请求
                            if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                            {
                                _jumpRequested = false;
                            }

                            // 当角色在地面上时，重置跳跃相关标记
                            if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                            {
                                // 本帧未触发跳跃时，重置二段跳和普通跳标记
                                if (!_jumpedThisFrame)
                                {
                                    _doubleJumpConsumed = false;
                                    _jumpConsumed = false;
                                }
                                _timeSinceLastAbleToJump = 0f;
                            }
                            else
                            {
                                // 记录距离上次可跳跃状态的时间（用于后接地容错）
                                _timeSinceLastAbleToJump += deltaTime;
                            }
                        }

                        // 处理取消蹲伏逻辑
                        if (_isCrouching && !_shouldBeCrouching)
                        {
                            // 先恢复站立状态的胶囊碰撞体尺寸，检测是否有障碍物
                            Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                            if (Motor.CharacterOverlap(
                                Motor.TransientPosition,
                                Motor.TransientRotation,
                                _probedColliders,
                                Motor.CollidableLayers,
                                QueryTriggerInteraction.Ignore) > 0)
                            {
                                // 检测到障碍物，保持蹲伏尺寸
                                Motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                            }
                            else
                            {
                                // 无障碍物，恢复站立状态
                                MeshRoot.localScale = new Vector3(1f, 1f, 1f);
                                _isCrouching = false;
                            }
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// 过滤碰撞体（是否参与角色碰撞检测）
        /// </summary>
        public bool IsColliderValidForCollisions(Collider coll)
        {
            if (IgnoredColliders.Contains(coll))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 地面碰撞回调（角色接触地面时触发）
        /// </summary>
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        /// <summary>
        /// 移动碰撞回调（角色移动时碰撞到物体触发）
        /// </summary>
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // 墙跳条件：允许墙跳 + 不在稳定地面 + 碰撞不稳定（撞到墙面）
                        if (AllowWallJump && !Motor.GroundingStatus.IsStableOnGround && !hitStabilityReport.IsStable)
                        {
                            _canWallJump = true;
                            _wallJumpNormal = hitNormal;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// 叠加额外速度（外部调用，如被击退、爆炸推动）
        /// </summary>
        public void AddVelocity(Vector3 velocity)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        _internalVelocityAdd += velocity;
                        break;
                    }
            }
        }

        /// <summary>
        /// 处理碰撞稳定性报告（自定义碰撞稳定性判断）
        /// </summary>
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        /// <summary>
        /// 接地状态更新后的回调
        /// </summary>
        public void PostGroundingUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// 离散碰撞检测回调（检测到离散碰撞时触发）
        /// </summary>
        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
}