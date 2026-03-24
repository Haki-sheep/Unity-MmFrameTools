using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;

namespace KinematicCharacterController.Walkthrough.ClimbingLadders
{
    /// <summary>
    /// 角色整体状态枚举
    /// </summary>
    public enum CharacterState
    {
        Default,   // 默认状态（地面/空中移动）
        Climbing,  // 攀爬状态（爬梯子）
    }

    /// <summary>
    /// 梯子攀爬细分状态枚举
    /// </summary>
    public enum ClimbingState
    {
        Anchoring,    // 锚定状态（角色吸附到梯子的过渡阶段）
        Climbing,     // 攀爬状态（正常爬梯子）
        DeAnchoring   // 解锚状态（角色脱离梯子的过渡阶段）
    }

    /// <summary>
    /// 玩家角色输入结构体
    /// 存储所有输入信息（移动、相机、交互、动作）
    /// </summary>
    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward;    // 向前移动轴（WS键/左摇杆上下）
        public float MoveAxisRight;      // 向右移动轴（AD键/左摇杆左右）
        public Quaternion CameraRotation;// 相机世界旋转角度
        public bool JumpDown;            // 跳跃按键按下
        public bool CrouchDown;          // 蹲伏按键按下
        public bool CrouchUp;            // 蹲伏按键抬起
        public bool ClimbLadder;         // 攀爬梯子交互按键（比如E键）
    }

    /// <summary>
    /// 自定义角色控制器（梯子攀爬示例）
    /// 核心功能：默认移动（地面/空中）、梯子攀爬（吸附/移动/脱离）、蹲伏、跳跃/二段跳/墙跳
    /// </summary>
    public class MyCharacterController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor; // KCC核心电机组件

        [Header("稳定移动设置（地面）")]
        public float MaxStableMoveSpeed = 10f; // 地面最大移动速度
        public float StableMovementSharpness = 15; // 地面移动响应锐度
        public float OrientationSharpness = 10; // 角色朝向响应锐度
        public float MaxStableDistanceFromLedge = 5f; // 边缘稳定移动最大距离
        [Range(0f, 180f)]
        public float MaxStableDenivelationAngle = 180f; // 稳定移动最大坡度角

        [Header("空中移动设置")]
        public float MaxAirMoveSpeed = 10f; // 空中最大移动速度
        public float AirAccelerationSpeed = 5f; // 空中加速度
        public float Drag = 0.1f; // 空中拖拽阻力

        [Header("跳跃设置")]
        public bool AllowJumpingWhenSliding = false; // 滑行时是否允许跳跃
        public bool AllowDoubleJump = false; // 是否允许二段跳
        public bool AllowWallJump = false; // 是否允许墙跳
        public float JumpSpeed = 10f; // 跳跃初速度
        public float JumpPreGroundingGraceTime = 0f; // 跳跃预接地容错时间（落地前按跳也生效）
        public float JumpPostGroundingGraceTime = 0f; // 跳跃后接地容错时间（落地后短时间仍能跳）

        [Header("梯子攀爬设置")]
        public float ClimbingSpeed = 4f; // 梯子攀爬速度
        public float AnchoringDuration = 0.25f; // 锚定/解锚过渡动画时长（秒）
        public LayerMask InteractionLayer; // 交互层（梯子所在层）

        [Header("其他设置")]
        public List<Collider> IgnoredColliders = new List<Collider>(); // 忽略的碰撞体列表
        public bool OrientTowardsGravity = false; // 是否朝向重力方向
        public Vector3 Gravity = new Vector3(0, -30f, 0); // 重力加速度
        public Transform MeshRoot; // 角色模型根节点

        public CharacterState CurrentCharacterState { get; private set; } // 当前角色状态

        // 私有变量
        private Collider[] _probedColliders = new Collider[8]; // 碰撞检测结果缓存（最多检测8个碰撞体）
        private Vector3 _moveInputVector; // 处理后的移动输入向量
        private Vector3 _lookInputVector; // 处理后的朝向输入向量
        private bool _jumpRequested = false; // 跳跃请求标记
        private bool _jumpConsumed = false; // 普通跳跃已使用标记
        private bool _doubleJumpConsumed = false; // 二段跳已使用标记
        private bool _jumpedThisFrame = false; // 本帧是否跳跃
        private bool _canWallJump = false; // 可墙跳标记
        private Vector3 _wallJumpNormal; // 墙跳的墙面法线
        private float _timeSinceJumpRequested = Mathf.Infinity; // 距离上次请求跳跃的时间
        private float _timeSinceLastAbleToJump = 0f; // 距离上次能跳跃的时间（容错用）
        private Vector3 _internalVelocityAdd = Vector3.zero; // 额外叠加的速度（如被击退）
        private bool _shouldBeCrouching = false; // 应该蹲伏标记
        private bool _isCrouching = false; // 当前是否蹲伏

        // 梯子相关私有变量
        private float _ladderUpDownInput; // 梯子上下攀爬输入（对应MoveAxisForward）
        private MyLadder _activeLadder { get; set; } // 当前激活的梯子
        private ClimbingState _internalClimbingState; // 内部攀爬状态
        private ClimbingState _climbingState
        {
            get
            {
                return _internalClimbingState;
            }
            set
            {
                _internalClimbingState = value;
                _anchoringTimer = 0f; // 重置锚定计时器
                _anchoringStartPosition = Motor.TransientPosition; // 记录锚定起始位置
                _anchoringStartRotation = Motor.TransientRotation; // 记录锚定起始旋转
            }
        }
        private Vector3 _ladderTargetPosition; // 梯子攀爬目标位置
        private Quaternion _ladderTargetRotation; // 梯子攀爬目标旋转
        private float _onLadderSegmentState = 0; // 角色在梯子段上的位置状态（参考MyLadder的计算结果）
        private float _anchoringTimer = 0f; // 锚定/解锚计时器
        private Vector3 _anchoringStartPosition = Vector3.zero; // 锚定起始位置
        private Quaternion _anchoringStartRotation = Quaternion.identity; // 锚定起始旋转
        private Quaternion _rotationBeforeClimbing = Quaternion.identity; // 攀爬前的角色旋转（脱离后恢复）

        private void Start()
        {
            // 将当前控制器赋值给KCC电机
            Motor.CharacterController = this;

            // 初始化角色状态为默认
            TransitionToState(CharacterState.Default);
        }

        /// <summary>
        /// 处理角色状态切换，包含状态退出/进入回调
        /// </summary>
        /// <param name="newState">目标状态</param>
        public void TransitionToState(CharacterState newState)
        {
            CharacterState tmpInitialState = CurrentCharacterState;
            OnStateExit(tmpInitialState, newState); // 退出原状态
            CurrentCharacterState = newState; // 切换状态
            OnStateEnter(newState, tmpInitialState); // 进入新状态
        }

        /// <summary>
        /// 进入状态时的回调
        /// </summary>
        /// <param name="state">进入的新状态</param>
        /// <param name="fromState">离开的原状态</param>
        public void OnStateEnter(CharacterState state, CharacterState fromState)
        {
            switch (state)
            {
                case CharacterState.Default:
                    {
                        break;
                    }
                case CharacterState.Climbing:
                    {
                        // 记录攀爬前的角色旋转（脱离梯子后恢复）
                        _rotationBeforeClimbing = Motor.TransientRotation;

                        // 关闭KCC的碰撞求解和地面求解（攀爬时不需要物理地面）
                        Motor.SetMovementCollisionsSolvingActivation(false);
                        Motor.SetGroundSolvingActivation(false);
                        // 初始化为锚定状态（吸附到梯子）
                        _climbingState = ClimbingState.Anchoring;

                        // 计算并存储要吸附到梯子的目标位置和旋转
                        _ladderTargetPosition = _activeLadder.ClosestPointOnLadderSegment(Motor.TransientPosition, out _onLadderSegmentState);
                        _ladderTargetRotation = _activeLadder.transform.rotation;
                        break;
                    }
            }
        }

        /// <summary>
        /// 退出状态时的回调
        /// </summary>
        /// <param name="state">离开的原状态</param>
        /// <param name="toState">进入的新状态</param>
        public void OnStateExit(CharacterState state, CharacterState toState)
        {
            switch (state)
            {
                case CharacterState.Default:
                    {
                        break;
                    }
                case CharacterState.Climbing:
                    {
                        // 恢复KCC的碰撞求解和地面求解（回到正常移动）
                        Motor.SetMovementCollisionsSolvingActivation(true);
                        Motor.SetGroundSolvingActivation(true);
                        break;
                    }
            }
        }

        /// <summary>
        /// 由MyPlayer脚本每帧调用，向角色传递输入信息
        /// </summary>
        /// <param name="inputs">玩家输入结构体</param>
        public void SetInputs(ref PlayerCharacterInputs inputs)
        {
            // 处理梯子攀爬输入
            _ladderUpDownInput = inputs.MoveAxisForward;
            if (inputs.ClimbLadder) // 按下攀爬梯子交互键
            {
                // 检测角色周围的交互层碰撞体（找梯子）
                if (Motor.CharacterOverlap(Motor.TransientPosition, Motor.TransientRotation, _probedColliders, InteractionLayer, QueryTriggerInteraction.Collide) > 0)
                {
                    if (_probedColliders[0] != null)
                    {
                        // 获取梯子组件
                        MyLadder ladder = _probedColliders[0].gameObject.GetComponent<MyLadder>();
                        if (ladder)
                        {
                            // 状态切换：默认状态 → 攀爬状态（开始爬梯子）
                            if (CurrentCharacterState == CharacterState.Default)
                            {
                                _activeLadder = ladder;
                                TransitionToState(CharacterState.Climbing);
                            }
                            // 状态切换：攀爬状态 → 解锚状态（停止爬梯子）
                            else if (CurrentCharacterState == CharacterState.Climbing)
                            {
                                _climbingState = ClimbingState.DeAnchoring;
                                _ladderTargetPosition = Motor.TransientPosition;
                                _ladderTargetRotation = _rotationBeforeClimbing;
                            }
                        }
                    }
                }
            }

            // 限制输入向量长度（避免斜向移动速度过快）
            Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

            // 计算相机在角色平面上的方向和旋转（适配角色Up方向）
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
            }
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

            // 根据角色状态处理输入
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // 处理移动/朝向输入
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
                                // 修改胶囊体尺寸（蹲伏）
                                Motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                                // 修改模型缩放（视觉上蹲伏）
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
        /// <param name="deltaTime">帧间隔时间</param>
        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// （由KinematicCharacterMotor在更新周期中调用）
        /// 在此方法中设置角色当前的旋转角度
        /// 【KCC约束】这是唯一允许设置角色旋转的地方
        /// </summary>
        /// <param name="currentRotation">当前角色旋转（引用传递，可修改）</param>
        /// <param name="deltaTime">帧间隔时间</param>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // 平滑更新角色朝向（朝向输入方向）
                        if (_lookInputVector != Vector3.zero && OrientationSharpness > 0f)
                        {
                            Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;
                            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
                        }
                        // 朝向重力方向（如反重力场景）
                        if (OrientTowardsGravity)
                        {
                            currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -Gravity) * currentRotation;
                        }
                        break;
                    }
                case CharacterState.Climbing:
                    {
                        // 根据梯子攀爬细分状态更新旋转
                        switch (_climbingState)
                        {
                            case ClimbingState.Climbing:
                                // 正常攀爬：角色旋转与梯子一致
                                currentRotation = _activeLadder.transform.rotation;
                                break;
                            case ClimbingState.Anchoring:
                            case ClimbingState.DeAnchoring:
                                // 锚定/解锚：平滑插值到目标旋转（过渡动画）
                                currentRotation = Quaternion.Slerp(_anchoringStartRotation, _ladderTargetRotation, (_anchoringTimer / AnchoringDuration));
                                break;
                        }
                        break;
                    }
            }
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
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        Vector3 targetMovementVelocity = Vector3.zero;
                        // 地面稳定移动
                        if (Motor.GroundingStatus.IsStableOnGround)
                        {
                            // 在斜坡上重新调整速度方向（贴合斜坡切线）
                            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

                            // 计算目标速度（适配地面法线）
                            Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                            Vector3 reorientedInput = Vector3.Cross(Motor.GroundingStatus.GroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                            targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;

                            // 平滑地面移动速度
                            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-StableMovementSharpness * deltaTime));
                        }
                        // 空中移动
                        else
                        {
                            // 叠加空中移动输入
                            if (_moveInputVector.sqrMagnitude > 0f)
                            {
                                targetMovementVelocity = _moveInputVector * MaxAirMoveSpeed;

                                // 防止在空中爬上不稳定的斜坡
                                if (Motor.GroundingStatus.FoundAnyGround)
                                {
                                    Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                                    targetMovementVelocity = Vector3.ProjectOnPlane(targetMovementVelocity, perpenticularObstructionNormal);
                                }

                                Vector3 velocityDiff = Vector3.ProjectOnPlane(targetMovementVelocity - currentVelocity, Gravity);
                                currentVelocity += velocityDiff * AirAccelerationSpeed * deltaTime;
                            }

                            // 叠加重力
                            currentVelocity += Gravity * deltaTime;

                            // 应用空中拖拽阻力
                            currentVelocity *= (1f / (1f + (Drag * deltaTime)));
                        }

                        // 处理跳跃逻辑
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
                                        Motor.ForceUnground(0.1f); // 强制脱离地面

                                        // 叠加跳跃速度（抵消原有垂直速度）
                                        currentVelocity += (Motor.CharacterUp * JumpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                                        _jumpRequested = false;
                                        _doubleJumpConsumed = true;
                                        _jumpedThisFrame = true;
                                    }
                                }

                                // 判断是否满足跳跃条件（普通跳/墙跳/容错跳）
                                if (_canWallJump ||
                                    (!_jumpConsumed && ((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime)))
                                {
                                    // 计算跳跃方向
                                    Vector3 jumpDirection = Motor.CharacterUp;
                                    if (_canWallJump)
                                    {
                                        jumpDirection = _wallJumpNormal; // 墙跳方向为墙面法线
                                    }
                                    else if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                                    {
                                        jumpDirection = Motor.GroundingStatus.GroundNormal; // 斜坡跳跃方向为地面法线
                                    }

                                    // 强制脱离地面（避免跳跃时仍吸附地面）
                                    Motor.ForceUnground(0.1f);

                                    // 叠加跳跃速度
                                    currentVelocity += (jumpDirection * JumpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                                    _jumpRequested = false;
                                    _jumpConsumed = true;
                                    _jumpedThisFrame = true;
                                }
                            }

                            // 重置墙跳标记
                            _canWallJump = false;
                        }

                        // 叠加额外速度（如被击退）
                        if (_internalVelocityAdd.sqrMagnitude > 0f)
                        {
                            currentVelocity += _internalVelocityAdd;
                            _internalVelocityAdd = Vector3.zero;
                        }
                        break;
                    }
                case CharacterState.Climbing:
                    {
                        // 攀爬状态先重置速度
                        currentVelocity = Vector3.zero;

                        // 根据攀爬细分状态更新速度
                        switch (_climbingState)
                        {
                            case ClimbingState.Climbing:
                                // 正常攀爬：沿梯子Up方向移动
                                currentVelocity = (_ladderUpDownInput * _activeLadder.transform.up).normalized * ClimbingSpeed;
                                break;
                            case ClimbingState.Anchoring:
                            case ClimbingState.DeAnchoring:
                                // 锚定/解锚：平滑移动到目标位置
                                Vector3 tmpPosition = Vector3.Lerp(_anchoringStartPosition, _ladderTargetPosition, (_anchoringTimer / AnchoringDuration));
                                currentVelocity = Motor.GetVelocityForMovePosition(Motor.TransientPosition, tmpPosition, deltaTime);
                                break;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// （由KinematicCharacterMotor在更新周期中调用）
        /// 角色完成移动更新后的收尾逻辑
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        public void AfterCharacterUpdate(float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // 处理跳跃相关容错逻辑
                        {
                            // 超过预接地容错时间，取消跳跃请求
                            if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                            {
                                _jumpRequested = false;
                            }

                            // 回到地面，重置跳跃标记
                            if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                            {
                                if (!_jumpedThisFrame)
                                {
                                    _doubleJumpConsumed = false;
                                    _jumpConsumed = false;
                                }
                                _timeSinceLastAbleToJump = 0f;
                            }
                            else
                            {
                                // 累计离地面时间（用于后接地容错）
                                _timeSinceLastAbleToJump += deltaTime;
                            }
                        }

                        // 处理取消蹲伏
                        if (_isCrouching && !_shouldBeCrouching)
                        {
                            // 先恢复胶囊体到站立尺寸，检测是否有障碍物
                            Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                            if (Motor.CharacterOverlap(
                                Motor.TransientPosition,
                                Motor.TransientRotation,
                                _probedColliders,
                                Motor.CollidableLayers,
                                QueryTriggerInteraction.Ignore) > 0)
                            {
                                // 有障碍物，保持蹲伏尺寸
                                Motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                            }
                            else
                            {
                                // 无障碍物，恢复站立
                                MeshRoot.localScale = new Vector3(1f, 1f, 1f);
                                _isCrouching = false;
                            }
                        }
                        break;
                    }
                case CharacterState.Climbing:
                    {
                        switch (_climbingState)
                        {
                            case ClimbingState.Climbing:
                                // 检测是否超出梯子范围（需要脱离梯子）
                                _activeLadder.ClosestPointOnLadderSegment(Motor.TransientPosition, out _onLadderSegmentState);
                                if (Mathf.Abs(_onLadderSegmentState) > 0.05f)
                                {
                                    _climbingState = ClimbingState.DeAnchoring;

                                    // 超出梯子顶部 → 移动到顶部脱离点
                                    if (_onLadderSegmentState > 0)
                                    {
                                        _ladderTargetPosition = _activeLadder.TopReleasePoint.position;
                                        _ladderTargetRotation = _activeLadder.TopReleasePoint.rotation;
                                    }
                                    // 超出梯子底部 → 移动到底部脱离点
                                    else if (_onLadderSegmentState < 0)
                                    {
                                        _ladderTargetPosition = _activeLadder.BottomReleasePoint.position;
                                        _ladderTargetRotation = _activeLadder.BottomReleasePoint.rotation;
                                    }
                                }
                                break;
                            case ClimbingState.Anchoring:
                            case ClimbingState.DeAnchoring:
                                // 锚定/解锚计时结束，切换状态
                                if (_anchoringTimer >= AnchoringDuration)
                                {
                                    if (_climbingState == ClimbingState.Anchoring)
                                    {
                                        _climbingState = ClimbingState.Climbing; // 锚定完成 → 正常攀爬
                                    }
                                    else if (_climbingState == ClimbingState.DeAnchoring)
                                    {
                                        TransitionToState(CharacterState.Default); // 解锚完成 → 回到默认状态
                                    }
                                }

                                // 累计锚定/解锚时间
                                _anchoringTimer += deltaTime;
                                break;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// 过滤碰撞体（是否参与角色碰撞检测）
        /// </summary>
        /// <param name="coll">待检测的碰撞体</param>
        /// <returns>是否有效（true=参与碰撞，false=忽略）</returns>
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
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // 墙跳条件：允许墙跳 + 不在稳定地面 + 碰撞不稳定（撞到墙面）
                        if (AllowWallJump && !Motor.GroundingStatus.IsStableOnGround && !hitStabilityReport.IsStable)
                        {
                            _canWallJump = true;
                            _wallJumpNormal = hitNormal; // 记录墙跳的墙面法线
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// 叠加额外速度（外部调用，如被击退、爆炸推动）
        /// </summary>
        /// <param name="velocity">要叠加的速度</param>
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

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void PostGroundingUpdate(float deltaTime)
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
}