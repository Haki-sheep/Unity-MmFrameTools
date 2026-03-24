using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;  // Kcc: 引入运动学角色控制器核心命名空间
using System;

namespace KinematicCharacterController.Examples
{
    /// <summary>
    /// 角色状态枚举
    /// 定义角色可以处于的各种状态
    /// </summary>
    public enum CharacterState
    {
        Default,  // 默认状态
    }

    /// <summary>
    /// 朝向方法枚举
    /// 决定角色面朝方向的计算方式
    /// </summary>
    public enum OrientationMethod
    {
        TowardsCamera,    // 朝向相机方向
        TowardsMovement,  // 朝向移动方向
    }

    /// <summary>
    /// 玩家角色输入数据结构
    /// 用于从玩家输入传递给角色控制器
    /// </summary>
    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward;     // 前后移动输入 (-1 到 1)
        public float MoveAxisRight;       // 左右移动输入 (-1 到 1)
        public Quaternion CameraRotation; // 当前相机旋转
        public bool JumpDown;             // 跳跃键是否按下
        public bool CrouchDown;           // 下蹲键是否按下
        public bool CrouchUp;             // 下蹲键是否释放
    }

    /// <summary>
    /// AI角色输入数据结构
    /// 用于从AI传递给角色控制器
    /// </summary>
    public struct AICharacterInputs
    {
        public Vector3 MoveVector;  // 移动向量
        public Vector3 LookVector;  // 注视向量
    }

    /// <summary>
    /// 额外朝向方法枚举
    /// 定义额外的朝向控制方式
    /// </summary>
    public enum BonusOrientationMethod
    {
        None,                       // 无额外朝向
        TowardsGravity,             // 朝向重力反方向
        TowardsGroundSlopeAndGravity,  // 朝向地面坡度和重力方向
    }

    /// <summary>
    /// 示例角色控制器
    /// 继承自MonoBehaviour并实现ICharacterController接口
    /// 处理角色的移动、跳跃、下蹲等核心逻辑
    /// 
    /// Kcc: 这是实现ICharacterController接口的主类，所有KCC相关的回调方法都在此类中实现
    /// </summary>
    public class ExampleCharacterController : MonoBehaviour, ICharacterController
    {
        // Kcc: Motor是运动学角色电机的核心引用，所有物理和运动学计算都通过它进行
        public KinematicCharacterMotor Motor;

        [Header("稳定移动设置")]
        public float MaxStableMoveSpeed = 10f;           // 地面最大移动速度
        public float StableMovementSharpness = 15f;      // 地面移动平滑度
        public float OrientationSharpness = 10f;         // 旋转平滑度
        public OrientationMethod OrientationMethod = OrientationMethod.TowardsCamera;  // 朝向方法

        [Header("空中移动设置")]
        public float MaxAirMoveSpeed = 15f;              // 空中最大移动速度
        public float AirAccelerationSpeed = 15f;         // 空中加速度
        public float Drag = 0.1f;                        // 空气阻力

        [Header("跳跃设置")]
        public bool AllowJumpingWhenSliding = false;     // 允许滑动时跳跃
        public float JumpUpSpeed = 10f;                  // 跳跃向上速度
        public float JumpScalableForwardSpeed = 10f;     // 跳跃时可变的前向速度
        public float JumpPreGroundingGraceTime = 0f;     // 落地前跳跃预判时间
        public float JumpPostGroundingGraceTime = 0f;    // 落地后跳跃判定时间

        [Header("杂项设置")]
        public List<Collider> IgnoredColliders = new List<Collider>();  // 忽略的碰撞体列表
        public BonusOrientationMethod BonusOrientationMethod = BonusOrientationMethod.None;  // 额外朝向方法
        public float BonusOrientationSharpness = 10f;   // 额外朝向平滑度
        public Vector3 Gravity = new Vector3(0, -30f, 0);  // 重力向量
        public Transform MeshRoot;                      // 角色模型根变换
        public Transform CameraFollowPoint;             // 相机跟随点
        public float CrouchedCapsuleHeight = 1f;       // 下蹲时的胶囊体高度

        /// <summary>
        /// 当前角色状态（只读）
        /// </summary>
        public CharacterState CurrentCharacterState { get; private set; }

        // 内部状态变量
        private Collider[] _probedColliders = new Collider[8];    // 用于碰撞检测的临时数组
        private RaycastHit[] _probedHits = new RaycastHit[8];     // 用于射线检测的临时数组
        private Vector3 _moveInputVector;                          // 处理后的移动输入向量
        private Vector3 _lookInputVector;                          // 处理后的朝向输入向量
        private bool _jumpRequested = false;                       // 是否请求跳跃
        private bool _jumpConsumed = false;                        // 跳跃是否已消耗
        private bool _jumpedThisFrame = false;                     // 是否在本帧跳跃
        private float _timeSinceJumpRequested = Mathf.Infinity;   // 请求跳跃后的时间
        private float _timeSinceLastAbleToJump = 0f;              // 上次能跳跃后的时间
        private Vector3 _internalVelocityAdd = Vector3.zero;      // 内部添加的速度
        private bool _shouldBeCrouching = false;                  // 是否应该下蹲
        private bool _isCrouching = false;                        // 是否正在下蹲

        // 记录上一帧的地面法线（用于内部计算）
        private Vector3 lastInnerNormal = Vector3.zero;
        private Vector3 lastOuterNormal = Vector3.zero;

        /// <summary>
        /// 唤醒方法，在对象创建时调用
        /// 初始化状态并将角色控制器注册到电机
        /// </summary>
        private void Awake()
        {
            // 初始化状态
            TransitionToState(CharacterState.Default);

            // Kcc: 将当前角色控制器实例赋值给电机，这是建立连接的关键步骤
            // 没有这一步，KCC电机无法调用我们的回调方法
            Motor.CharacterController = this;
        }

        /// <summary>
        /// 处理移动状态转换和进入/退出回调
        /// </summary>
        /// <param name="newState">要转换到的新状态</param>
        public void TransitionToState(CharacterState newState)
        {
            CharacterState tmpInitialState = CurrentCharacterState;
            // 触发当前状态的退出回调
            OnStateExit(tmpInitialState, newState);
            // 更新当前状态
            CurrentCharacterState = newState;
            // 触发新状态的进入回调
            OnStateEnter(newState, tmpInitialState);
        }

        /// <summary>
        /// 状态进入时的回调
        /// 可在此处添加进入状态时的初始化逻辑
        /// </summary>
        /// <param name="state">进入的状态</param>
        /// <param name="fromState">从哪个状态进入</param>
        public void OnStateEnter(CharacterState state, CharacterState fromState)
        {
            switch (state)
            {
                case CharacterState.Default:
                    {
                        // 默认状态无需特殊处理
                        break;
                    }
            }
        }

        /// <summary>
        /// 状态退出时的回调
        /// 可在此处添加退出状态时的清理逻辑
        /// </summary>
        /// <param name="state">退出的状态</param>
        /// <param name="toState">要转换到哪个状态</param>
        public void OnStateExit(CharacterState state, CharacterState toState)
        {
            switch (state)
            {
                case CharacterState.Default:
                    {
                        // 默认状态无需特殊处理
                        break;
                    }
            }
        }

        /// <summary>
        /// 由ExamplePlayer每帧调用，用于告诉角色当前的输入是什么
        /// 处理输入数据并转换为内部使用的格式
        /// </summary>
        /// <param name="inputs">玩家输入数据</param>
        public void SetInputs(ref PlayerCharacterInputs inputs)
        {
            // 限制输入向量长度，最大为1
            Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

            // Kcc: 计算相机在水平面上的方向和旋转
            // 使用Motor.CharacterUp获取当前的角色向上方向（可能是重力方向）
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                // 如果相机朝下，使用向上的方向重新计算
                cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
            }
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // 移动输入：根据相机方向转换输入向量
                        _moveInputVector = cameraPlanarRotation * moveInputVector;

                        // 根据朝向方法计算朝向输入
                        switch (OrientationMethod)
                        {
                            case OrientationMethod.TowardsCamera:
                                // 朝向相机方向
                                _lookInputVector = cameraPlanarDirection;
                                break;
                            case OrientationMethod.TowardsMovement:
                                // 朝向移动方向
                                _lookInputVector = _moveInputVector.normalized;
                                break;
                        }

                        // 跳跃输入处理
                        if (inputs.JumpDown)
                        {
                            _timeSinceJumpRequested = 0f;
                            _jumpRequested = true;
                        }

                        // 下蹲输入处理
                        if (inputs.CrouchDown)
                        {
                            _shouldBeCrouching = true;

                            if (!_isCrouching)
                            {
                                _isCrouching = true;
                                // Kcc: 设置胶囊体维度以实现下蹲效果
                                Motor.SetCapsuleDimensions(0.5f, CrouchedCapsuleHeight, CrouchedCapsuleHeight * 0.5f);
                                // 缩小模型高度
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
        /// 由AI脚本每帧调用，用于告诉角色当前的输入是什么
        /// 处理AI输入数据
        /// </summary>
        /// <param name="inputs">AI输入数据</param>
        public void SetInputs(ref AICharacterInputs inputs)
        {
            _moveInputVector = inputs.MoveVector;
            _lookInputVector = inputs.LookVector;
        }

        private Quaternion _tmpTransientRot;

        /// <summary>
        /// Kcc回调方法：由KinematicCharacterMotor在更新周期中调用
        /// 在角色开始移动更新之前执行
        /// 可用于预处理计算
        /// </summary>
        /// <param name="deltaTime">帧时间间隔</param>
        public void BeforeCharacterUpdate(float deltaTime)
        {
            // 此示例中无需预处理
        }

        /// <summary>
        /// Kcc回调方法：由KinematicCharacterMotor在更新周期中调用
        /// 这里是设置角色旋转的唯一位置
        /// 
        /// Kcc: 这是ICharacterController接口的核心方法之一
        /// Kcc: Motor.CharacterForward 和 Motor.CharacterUp 是KCC提供的便捷属性
        /// Kcc: currentRotation参数是传入的当前旋转，需要修改后传回
        /// </summary>
        /// <param name="currentRotation">当前的旋转，会被修改</param>
        /// <param name="deltaTime">帧时间间隔</param>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // 如果有朝向输入且平滑度大于0，计算平滑的朝向
                        if (_lookInputVector.sqrMagnitude > 0f && OrientationSharpness > 0f)
                        {
                            // 使用指数衰减实现平滑插值
                            Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                            // Kcc: 设置旋转，这个值会被KinematicCharacterMotor使用
                            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
                        }

                        // Kcc: 计算当前的向上方向
                        Vector3 currentUp = (currentRotation * Vector3.up);

                        // 根据额外朝向方法调整旋转
                        if (BonusOrientationMethod == BonusOrientationMethod.TowardsGravity)
                        {
                            // Kcc: 旋转以匹配重力方向（用于倒立行走的特殊情况）
                            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                        }
                        else if (BonusOrientationMethod == BonusOrientationMethod.TowardsGroundSlopeAndGravity)
                        {
                            // Kcc: 使用KCC的地面检测功能
                            if (Motor.GroundingStatus.IsStableOnGround)
                            {
                                // Kcc: Motor.TransientPosition是KCC提供的临时位置，用于中间计算
                                Vector3 initialCharacterBottomHemiCenter = Motor.TransientPosition + (currentUp * Motor.Capsule.radius);

                                // Kcc: Motor.GroundingStatus 包含当前地面状态信息
                                // Kcc: Motor.GroundingStatus.GroundNormal 是检测到的地面法线
                                Vector3 smoothedGroundNormal = Vector3.Slerp(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGroundNormal) * currentRotation;

                                // Kcc: 设置临时位置，使旋转围绕底部半球中心而非枢轴
                                // Kcc: Motor.SetTransientPosition 是KCC提供的方法，用于设置临时位置
                                Motor.SetTransientPosition(initialCharacterBottomHemiCenter + (currentRotation * Vector3.down * Motor.Capsule.radius));
                            }
                            else
                            {
                                // 空中时朝向重力方向
                                Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                            }
                        }
                        else
                        {
                            // 正常情况下平滑回到向上
                            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Kcc回调方法：由KinematicCharacterMotor在更新周期中调用
        /// 这里是设置角色速度的唯一位置
        /// 
        /// Kcc: 这是ICharacterController接口的核心方法之一
        /// Kcc: Motor.GroundingStatus 提供地面检测信息
        /// Kcc: currentVelocity参数是传入的当前速度，需要修改后传回
        /// Kcc: Motor.GetDirectionTangentToSurface 用于计算沿表面的方向
        /// </summary>
        /// <param name="currentVelocity">当前速度向量，会被修改</param>
        /// <param name="deltaTime">帧时间间隔</param>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // 地面移动逻辑
                        if (Motor.GroundingStatus.IsStableOnGround)
                        {
                            float currentVelocityMagnitude = currentVelocity.magnitude;

                            // Kcc: 获取有效的地面法线
                            Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;

                            // Kcc: 重新计算速度方向以沿表面移动
                            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

                            // 计算目标移动速度
                            Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                            Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                            Vector3 targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;

                            // 平滑移动速度
                            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime));
                        }
                        // 空中移动逻辑
                        else
                        {
                            // 如果有移动输入，添加加速度
                            if (_moveInputVector.sqrMagnitude > 0f)
                            {
                                Vector3 addedVelocity = _moveInputVector * AirAccelerationSpeed * deltaTime;

                                // Kcc: 将当前速度投影到水平面上
                                Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                                // Kcc: 限制空中速度不超过最大值
                                if (currentVelocityOnInputsPlane.magnitude < MaxAirMoveSpeed)
                                {
                                    // 限制添加的速度，使总速度不超过最大值
                                    Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, MaxAirMoveSpeed);
                                    addedVelocity = newTotal - currentVelocityOnInputsPlane;
                                }
                                else
                                {
                                    // 确保添加的速度不会超过当前速度的方向
                                    if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                                    {
                                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                                    }
                                }

                                // 防止攀爬斜坡墙壁
                                if (Motor.GroundingStatus.FoundAnyGround)
                                {
                                    if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                                    {
                                        // Kcc: 计算垂直于墙壁的法线方向
                                        Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
                                    }
                                }

                                // 应用添加的速度
                                currentVelocity += addedVelocity;
                            }

                            // 应用重力
                            currentVelocity += Gravity * deltaTime;

                            // 应用空气阻力
                            currentVelocity *= (1f / (1f + (Drag * deltaTime)));
                        }

                        // 处理跳跃
                        _jumpedThisFrame = false;
                        _timeSinceJumpRequested += deltaTime;
                        if (_jumpRequested)
                        {
                            // Kcc: 检查是否允许跳跃
                            // Kcc: Motor.GroundingStatus.IsStableOnGround 表示是否稳定在地面上
                            // Kcc: Motor.GroundingStatus.FoundAnyGround 表示是否检测到任何地面
                            if (!_jumpConsumed && ((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime))
                            {
                                // Kcc: 计算跳跃方向
                                Vector3 jumpDirection = Motor.CharacterUp;
                                if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                                {
                                    // 如果在不稳定的地面上（如斜坡），使用地面法线作为跳跃方向
                                    jumpDirection = Motor.GroundingStatus.GroundNormal;
                                }

                                // Kcc: 强制取消地面附着状态
                                // 如果没有这行代码，角色在跳跃时仍会吸附在地面上
                                Motor.ForceUnground();

                                // 添加跳跃速度并重置跳跃状态
                                currentVelocity += (jumpDirection * JumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                                currentVelocity += (_moveInputVector * JumpScalableForwardSpeed);
                                _jumpRequested = false;
                                _jumpConsumed = true;
                                _jumpedThisFrame = true;
                            }
                        }

                        // 处理附加速度
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
        /// Kcc回调方法：由KinematicCharacterMotor在更新周期中调用
        /// 在角色完成移动更新后执行
        /// 
        /// Kcc: 可在此处理跳跃冷却、下蹲检测等后处理逻辑
        /// Kcc: Motor.LastGroundingStatus 包含上一帧的地面状态
        /// </summary>
        /// <param name="deltaTime">帧时间间隔</param>
        public void AfterCharacterUpdate(float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // 处理跳跃相关数值
                        {
                            // 处理落地前的跳跃预判时间
                            if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                            {
                                _jumpRequested = false;
                            }

                            // Kcc: 检查当前是否在稳定地面上
                            if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                            {
                                // 如果在地面上，重置跳跃相关状态
                                if (!_jumpedThisFrame)
                                {
                                    _jumpConsumed = false;
                                }
                                _timeSinceLastAbleToJump = 0f;
                            }
                            else
                            {
                                // 记录上次能跳跃后的时间（用于落地后的宽限期）
                                _timeSinceLastAbleToJump += deltaTime;
                            }
                        }

                        // 处理取消下蹲
                        if (_isCrouching && !_shouldBeCrouching)
                        {
                            // Kcc: 设置回站立时的胶囊体尺寸
                            Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                            
                            // Kcc: 使用CharacterOverlap进行重叠检测，检查站立空间是否有障碍物
                            // Kcc: Motor.TransientPosition 是当前临时位置
                            // Kcc: Motor.CollidableLayers 是可碰撞的层
                            if (Motor.CharacterOverlap(
                                Motor.TransientPosition,
                                Motor.TransientRotation,
                                _probedColliders,
                                Motor.CollidableLayers,
                                QueryTriggerInteraction.Ignore) > 0)
                            {
                                // 如果有障碍物，保持下蹲状态
                                Motor.SetCapsuleDimensions(0.5f, CrouchedCapsuleHeight, CrouchedCapsuleHeight * 0.5f);
                            }
                            else
                            {
                                // 如果没有障碍物，恢复站立
                                MeshRoot.localScale = new Vector3(1f, 1f, 1f);
                                _isCrouching = false;
                            }
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Kcc回调方法：在地面检测更新后调用
        /// 处理着陆和离开地面的事件
        /// 
        /// Kcc: Motor.GroundingStatus.IsStableOnGround vs Motor.LastGroundingStatus.IsStableOnGround
        /// Kcc: 用于检测这一帧是否刚刚着陆或刚刚离开地面
        /// </summary>
        /// <param name="deltaTime">帧时间间隔</param>
        public void PostGroundingUpdate(float deltaTime)
        {
            // 处理着陆和离开地面事件
            // Kcc: 比较当前和上一帧的地面状态
            if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
            {
                // 刚刚着陆
                OnLanded();
            }
            else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
            {
                // 刚刚离开地面
                OnLeaveStableGround();
            }
        }

        /// <summary>
        /// 检查碰撞体是否有效的回调
        /// 用于过滤不需要检测碰撞的物体
        /// </summary>
        /// <param name="coll">要检查的碰撞体</param>
        /// <returns>是否有效</returns>
        public bool IsColliderValidForCollisions(Collider coll)
        {
            // 如果忽略列表为空，所有碰撞体都有效
            if (IgnoredColliders.Count == 0)
            {
                return true;
            }

            // 检查碰撞体是否在忽略列表中
            if (IgnoredColliders.Contains(coll))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Kcc回调方法：当角色在地面碰撞时调用
        /// 
        /// Kcc: 可以在此处理地面碰撞效果，如播放音效、粒子效果等
        /// Kcc: hitStabilityReport 包含碰撞的稳定性信息
        /// </summary>
        /// <param name="hitCollider">碰撞的碰撞体</param>
        /// <param name="hitNormal">碰撞法线</param>
        /// <param name="hitPoint">碰撞点</param>
        /// <param name="hitStabilityReport">碰撞稳定性报告</param>
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            // 示例中无需处理
        }

        /// <summary>
        /// Kcc回调方法：当角色在移动中碰撞时调用
        /// 
        /// Kcc: 可以在此处理墙壁碰撞效果
        /// Kcc: 区分 OnGroundHit（地面碰撞）和 OnMovementHit（墙壁/障碍物碰撞）
        /// </summary>
        /// <param name="hitCollider">碰撞的碰撞体</param>
        /// <param name="hitNormal">碰撞法线</param>
        /// <param name="hitPoint">碰撞点</param>
        /// <param name="hitStabilityReport">碰撞稳定性报告</param>
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            // 示例中无需处理
        }

        /// <summary>
        /// 添加额外速度的公共方法
        /// 可被外部脚本调用（如受到击退、爆炸等效果）
        /// </summary>
        /// <param name="velocity">要添加的速度向量</param>
        public void AddVelocity(Vector3 velocity)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // 累加到内部速度，将在UpdateVelocity中应用
                        _internalVelocityAdd += velocity;
                        break;
                    }
            }
        }

        /// <summary>
        /// Kcc回调方法：处理碰撞稳定性报告
        /// 
        /// Kcc: hitStabilityReport 包含碰撞的详细信息，如是否在斜坡上、是否稳定等
        /// Kcc: 可在此根据碰撞稳定性调整角色状态
        /// </summary>
        /// <param name="hitCollider">碰撞的碰撞体</param>
        /// <param name="hitNormal">碰撞法线</param>
        /// <param name="hitPoint">碰撞点</param>
        /// <param name="atCharacterPosition">碰撞时角色位置</param>
        /// <param name="atCharacterRotation">碰撞时角色旋转</param>
        /// <param name="hitStabilityReport">碰撞稳定性报告（可修改）</param>
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
            // 示例中无需处理
        }

        /// <summary>
        /// 着陆事件方法
        /// 可在子类中重写以实现着陆逻辑（如播放音效、触发事件等）
        /// </summary>
        protected virtual void OnLanded()
        {
            // 可在子类中重写
        }

        /// <summary>
        /// 离开稳定地面事件方法
        /// 可在子类中重写以实现离开地面逻辑（如切换动画状态等）
        /// </summary>
        protected virtual void OnLeaveStableGround()
        {
            // 可在子类中重写
        }

        /// <summary>
        /// Kcc回调方法：检测到离散碰撞时调用
        /// 
        /// Kcc: 离散碰撞通常发生在角色与刚体对象的碰撞
        /// Kcc: 区别于连续的 OnGroundHit 和 OnMovementHit
        /// </summary>
        /// <param name="hitCollider">碰撞的碰撞体</param>
        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
            // 示例中无需处理
        }
    }
}