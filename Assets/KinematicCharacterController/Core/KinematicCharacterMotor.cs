using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace KinematicCharacterController
{
    public enum RigidbodyInteractionType
    {
        None,
        Kinematic,
        SimulatedDynamic
    }

    public enum StepHandlingMethod
    {
        None,
        Standard,
        Extra
    }

    public enum MovementSweepState
    {
        Initial,
        AfterFirstHit,
        FoundBlockingCrease,
        FoundBlockingCorner,
    }

    /// <summary>
    /// Represents the entire state of a character motor that is pertinent for simulation.
    /// Use this to save state or revert to past state
    /// </summary>
    [System.Serializable]
    public struct KinematicCharacterMotorState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 BaseVelocity;

        public bool MustUnground;
        public float MustUngroundTime;
        public bool LastMovementIterationFoundAnyGround;
        public CharacterTransientGroundingReport GroundingStatus;

        public Rigidbody AttachedRigidbody;
        public Vector3 AttachedRigidbodyVelocity;
    }

    /// <summary>
    /// Describes an overlap between the character capsule and another collider
    /// </summary>
    public struct OverlapResult
    {
        public Vector3 Normal;
        public Collider Collider;

        public OverlapResult(Vector3 normal, Collider collider)
        {
            Normal = normal;
            Collider = collider;
        }
    }

    /// <summary>
    /// Contains all the information for the motor's grounding status
    /// </summary>
    public struct CharacterGroundingReport
    {
        public bool FoundAnyGround;
        public bool IsStableOnGround;
        public bool SnappingPrevented;
        public Vector3 GroundNormal;
        public Vector3 InnerGroundNormal;
        public Vector3 OuterGroundNormal;

        public Collider GroundCollider;
        public Vector3 GroundPoint;

        public void CopyFrom(CharacterTransientGroundingReport transientGroundingReport)
        {
            FoundAnyGround = transientGroundingReport.FoundAnyGround;
            IsStableOnGround = transientGroundingReport.IsStableOnGround;
            SnappingPrevented = transientGroundingReport.SnappingPrevented;
            GroundNormal = transientGroundingReport.GroundNormal;
            InnerGroundNormal = transientGroundingReport.InnerGroundNormal;
            OuterGroundNormal = transientGroundingReport.OuterGroundNormal;

            GroundCollider = null;
            GroundPoint = Vector3.zero;
        }
    }

    /// <summary>
    /// Contains the simulation-relevant information for the motor's grounding status
    /// </summary>
    public struct CharacterTransientGroundingReport
    {
        public bool FoundAnyGround;
        public bool IsStableOnGround;
        public bool SnappingPrevented;
        public Vector3 GroundNormal;
        public Vector3 InnerGroundNormal;
        public Vector3 OuterGroundNormal;

        public void CopyFrom(CharacterGroundingReport groundingReport)
        {
            FoundAnyGround = groundingReport.FoundAnyGround;
            IsStableOnGround = groundingReport.IsStableOnGround;
            SnappingPrevented = groundingReport.SnappingPrevented;
            GroundNormal = groundingReport.GroundNormal;
            InnerGroundNormal = groundingReport.InnerGroundNormal;
            OuterGroundNormal = groundingReport.OuterGroundNormal;
        }
    }

    /// <summary>
    /// Contains all the information from a hit stability evaluation
    /// </summary>
    public struct HitStabilityReport
    {
        public bool IsStable;

        public bool FoundInnerNormal;
        public Vector3 InnerNormal;
        public bool FoundOuterNormal;
        public Vector3 OuterNormal;

        public bool ValidStepDetected;
        public Collider SteppedCollider;

        public bool LedgeDetected;
        public bool IsOnEmptySideOfLedge;
        public float DistanceFromLedge;
        public bool IsMovingTowardsEmptySideOfLedge;
        public Vector3 LedgeGroundNormal;
        public Vector3 LedgeRightDirection;
        public Vector3 LedgeFacingDirection;
    }

    /// <summary>
    /// Contains the information of hit rigidbodies during the movement phase, so they can be processed afterwards
    /// </summary>
    public struct RigidbodyProjectionHit
    {
        public Rigidbody Rigidbody;
        public Vector3 HitPoint;
        public Vector3 EffectiveHitNormal;
        public Vector3 HitVelocity;
        public bool StableOnHit;
    }

    /// <summary>
    /// 角色运动马达核心组件
    /// 负责角色碰撞检测、移动解算和物理交互
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    [TypeInfoBox("KCC 角色运动马达：管理角色碰撞和运动解算的核心组件")]
    public class KinematicCharacterMotor : MonoBehaviour
    {
#pragma warning disable 0414
        [HorizontalGroup("基础组件", Width = 0.3f)]
        [BoxGroup("基础组件/胶囊体", CenterLabel = true)]
        [LabelText("胶囊体组件")]
        [Tooltip("角色的胶囊体碰撞器引用")]
        [ReadOnly]
        public CapsuleCollider Capsule;

        [BoxGroup("基础组件/胶囊体")]
        [LabelText("胶囊体半径", Icon = SdfIconType.Circle)]
        [Tooltip("角色胶囊体的半径大小")]
        [Range(0.1f, 2f)]
        [SerializeField]
        private float CapsuleRadius = 0.5f;
        [BoxGroup("基础组件/胶囊体")]
        [LabelText("胶囊体高度")]
        [Tooltip("角色胶囊体的高度大小")]
        [Range(0.5f, 5f)]
        [SerializeField]
        private float CapsuleHeight = 2f;
        /// <summary>
        /// Local y position of the character's capsule center
        /// </summary>
        [BoxGroup("基础组件/胶囊体")]
        [LabelText("胶囊体Y轴偏移")]
        [Tooltip("胶囊体中心相对于角色Transform的Y轴偏移量")]
        [Range(0f, 3f)]
        [SerializeField]
        private float CapsuleYOffset = 1f;
        /// <summary>
        /// Physics material of the character's capsule
        /// </summary>
        [BoxGroup("基础组件/胶囊体")]
        [LabelText("物理材质")]
        [Tooltip("胶囊体的物理材质（仅影响与角色碰撞的物体，不影响角色自身移动）")]
        [SerializeField]
#pragma warning disable 0649
        private PhysicsMaterial CapsulePhysicsMaterial;
#pragma warning restore 0649


        [VerticalGroup("地面检测设置")]
        [BoxGroup("地面检测设置/地面检测")]
        [LabelText("额外检测距离")]
        [Tooltip("增加地面检测范围，使角色在高速下落时仍能吸附到地面")]
        public float GroundDetectionExtraDistance = 0f;
        /// <summary>
        /// Maximum slope angle on which the character can be stable
        /// </summary>    
        [BoxGroup("地面检测设置/地面检测")]
        [LabelText("最大稳定斜坡角度")]
        [Tooltip("角色能够稳定站立的最大斜坡角度")]
        [Range(0f, 89f)]
        public float MaxStableSlopeAngle = 60f;
        /// <summary>
        /// Which layers can the character be considered stable on
        /// </summary>    
        [BoxGroup("地面检测设置/地面检测")]
        [LabelText("稳定地面层")]
        [Tooltip("角色可以稳定站立的地板层")]
        public LayerMask StableGroundLayers = -1;
        /// <summary>
        /// Notifies the Character Controller when discrete collisions are detected
        /// </summary>    
        [BoxGroup("地面检测设置/碰撞事件")]
        [LabelText("离散碰撞事件")]
        [Tooltip("当检测到离散碰撞时通知角色控制器")]
        public bool DiscreteCollisionEvents = false;


        [VerticalGroup("台阶设置")]
        [BoxGroup("台阶设置/台阶检测")]
        [LabelText("台阶处理方式")]
        [Tooltip("处理台阶检测的方式。None=不检测，Standard=标准检测，Extra=额外检测（支持更小台阶）")]
        public StepHandlingMethod StepHandling = StepHandlingMethod.Standard;
        /// <summary>
        /// Maximum height of a step which the character can climb
        /// </summary>    
        [BoxGroup("台阶设置/台阶检测")]
        [LabelText("最大台阶高度")]
        [Tooltip("角色能够攀爬的最大台阶高度")]
        public float MaxStepHeight = 0.5f;
        /// <summary>
        /// Can the character step up obstacles even if it is not currently stable?
        /// </summary>    
        [BoxGroup("台阶设置/台阶检测")]
        [LabelText("无稳定接地时允许攀爬")]
        [Tooltip("即使角色当前未稳定站立在地面，是否仍允许攀爬台阶障碍物")]
        public bool AllowSteppingWithoutStableGrounding = false;
        /// <summary>
        /// Minimum length of a step that the character can step on (used in Extra stepping method. Use this to let the character step on steps that are smaller that its radius
        /// </summary>    
        [BoxGroup("台阶设置/台阶检测")]
        [LabelText("最小台阶深度")]
        [Tooltip("使用Extra台阶检测方式时，角色可以踩踏的最小台阶深度。用于让角色能够踩踏比自身半径更小的台阶")]
        public float MinRequiredStepDepth = 0.1f;


        [VerticalGroup("悬崖设置")]
        [BoxGroup("悬崖设置/悬崖检测")]
        [LabelText("悬崖与高度变化处理")]
        [Tooltip("是否启用悬崖和高度变化检测功能（影响性能）")]
        public bool LedgeAndDenivelationHandling = true;
        /// <summary>
        /// The distance from the capsule central axis at which the character can stand on a ledge and still be stable
        /// </summary>    
        [BoxGroup("悬崖设置/悬崖检测")]
        [LabelText("悬崖最大稳定距离")]
        [Tooltip("角色胶囊体中心轴到悬崖边缘的距离，在此范围内角色可以稳定站立在悬崖上")]
        public float MaxStableDistanceFromLedge = 0.5f;
        /// <summary>
        /// Prevents snapping to ground on ledges beyond a certain velocity
        /// </summary>    
        [BoxGroup("悬崖设置/悬崖检测")]
        [LabelText("悬崖吸附最大速度")]
        [Tooltip("超过此速度时，角色在悬崖边缘将不会吸附到地面")]
        public float MaxVelocityForLedgeSnap = 0f;
        /// <summary>
        /// The maximun downward slope angle change that the character can be subjected to and still be snapping to the ground
        /// </summary>    
        [BoxGroup("悬崖设置/悬崖检测")]
        [LabelText("最大稳定坡度变化角")]
        [Tooltip("角色能够保持地面吸附的最大下降坡度变化角度")]
        [Range(1f, 180f)]
        public float MaxStableDenivelationAngle = 180f;


        [VerticalGroup("刚体交互设置")]
        [BoxGroup("刚体交互设置/交互设置")]
        [LabelText("刚体交互处理")]
        [Tooltip("是否启用与PhysicsMover或动态刚体的交互处理（推动和被推动）")]
        public bool InteractiveRigidbodyHandling = true;
        /// <summary>
        /// How the character interacts with non-kinematic rigidbodies. "Kinematic" mode means the character pushes the rigidbodies with infinite force (as a kinematic body would). "SimulatedDynamic" pushes the rigidbodies with a simulated mass value.
        /// </summary>
        [BoxGroup("刚体交互设置/交互设置")]
        [LabelText("刚体交互类型")]
        [Tooltip("角色如何与动态刚体交互：None=不交互，Kinematic=以运动学方式无限力推动，SimulatedDynamic=以模拟质量值推动")]
        public RigidbodyInteractionType RigidbodyInteractionType;
        [BoxGroup("刚体交互设置/交互设置")]
        [LabelText("模拟角色质量")]
        [Tooltip("用于推动刚体时的模拟质量值")]
        public float SimulatedCharacterMass = 1f;
        /// <summary>
        /// Determines if the character preserves moving platform velocities when de-grounding from them
        /// </summary>
        [BoxGroup("刚体交互设置/交互设置")]
        [LabelText("保留移动平台动量")]
        [Tooltip("角色离开移动平台时是否保留平台的移动速度")]
        public bool PreserveAttachedRigidbodyMomentum = true;


        [VerticalGroup("约束设置")]
        [BoxGroup("约束设置/平面约束")]
        [LabelText("启用平面约束")]
        [Tooltip("是否限制角色在指定平面内移动")]
        public bool HasPlanarConstraint = false;
        /// <summary>
        /// Defines the plane that the character's movement is constrained on, if HasMovementConstraintPlane is active
        /// </summary>
        [BoxGroup("约束设置/平面约束")]
        [LabelText("平面约束轴")]
        [Tooltip("角色移动被限制所在的平面法线方向")]
        public Vector3 PlanarConstraintAxis = Vector3.forward;

        [VerticalGroup("其他设置")]
        [BoxGroup("其他设置/迭代设置")]
        [LabelText("最大移动迭代次数")]
        [Tooltip("每次更新中进行移动检测的最大迭代次数")]
        public int MaxMovementIterations = 5;
        /// <summary>
        /// How many times can we check for decollision per update
        /// </summary>
        [BoxGroup("其他设置/迭代设置")]
        [LabelText("最大解碰撞迭代次数")]
        [Tooltip("每次更新中进行解碰撞检测的最大迭代次数")]
        public int MaxDecollisionIterations = 1;
        /// <summary>
        /// Checks for overlaps before casting movement, making sure all collisions are detected even when already intersecting geometry (has a performance cost, but provides safety against tunneling through colliders)
        /// </summary>
        [BoxGroup("其他设置/碰撞检测")]
        [LabelText("检测初始重叠")]
        [Tooltip("在投射移动前检测重叠，确保即使已与几何体重叠也能检测到所有碰撞（影响性能，但能防止穿模）")]
        public bool CheckMovementInitialOverlaps = true;
        /// <summary>
        /// Sets the velocity to zero if exceed max movement iterations
        /// </summary>
        [BoxGroup("其他设置/安全设置")]
        [LabelText("超迭代时清除速度")]
        [Tooltip("当超过最大移动迭代次数时，是否将速度置零")]
        public bool KillVelocityWhenExceedMaxMovementIterations = true;
        /// <summary>
        /// Sets the remaining movement to zero if exceed max movement iterations
        /// </summary>
        [BoxGroup("其他设置/安全设置")]
        [LabelText("超迭代时清除剩余移动")]
        [Tooltip("当超过最大移动迭代次数时，是否将剩余移动距离置零")]
        public bool KillRemainingMovementWhenExceedMaxMovementIterations = true;

        /// <summary>
        /// Contains the current grounding information
        /// </summary>
        [System.NonSerialized]
        public CharacterGroundingReport GroundingStatus = new CharacterGroundingReport();
        /// <summary>
        /// Contains the previous grounding information
        /// </summary>
        [System.NonSerialized]
        public CharacterTransientGroundingReport LastGroundingStatus = new CharacterTransientGroundingReport();
        /// <summary>
        /// 指定角色移动算法能够检测碰撞的层掩码。默认情况下，这会使用刚体所在层的碰撞矩阵。
        /// </summary>
        [System.NonSerialized]
        public LayerMask CollidableLayers = -1;

        /// <summary>
        /// The Transform of the character motor
        /// </summary>
        public Transform Transform { get { return _transform; } }
        private Transform _transform;
        /// <summary>
        /// The character's goal position in its movement calculations (always up-to-date during the character update phase)
        /// </summary>
        public Vector3 TransientPosition { get { return _transientPosition; } }
        private Vector3 _transientPosition;
        /// <summary>
        /// The character's up direction (always up-to-date during the character update phase)
        /// </summary>
        public Vector3 CharacterUp { get { return _characterUp; } }
        private Vector3 _characterUp;
        /// <summary>
        /// The character's forward direction (always up-to-date during the character update phase)
        /// </summary>
        public Vector3 CharacterForward { get { return _characterForward; } }
        private Vector3 _characterForward;
        /// <summary>
        /// The character's right direction (always up-to-date during the character update phase)
        /// </summary>
        public Vector3 CharacterRight { get { return _characterRight; } }
        private Vector3 _characterRight;
        /// <summary>
        /// The character's position before the movement calculations began
        /// </summary>
        public Vector3 InitialSimulationPosition { get { return _initialSimulationPosition; } }
        private Vector3 _initialSimulationPosition;
        /// <summary>
        /// The character's rotation before the movement calculations began
        /// </summary>
        public Quaternion InitialSimulationRotation { get { return _initialSimulationRotation; } }
        private Quaternion _initialSimulationRotation;
        /// <summary>
        /// Represents the Rigidbody to stay attached to
        /// </summary>
        public Rigidbody AttachedRigidbody { get { return _attachedRigidbody; } }
        private Rigidbody _attachedRigidbody;//角色当前正在使用的 AttachedRigidbody 引用
        /// <summary>
        /// 胶囊体中心点相对于角色Transform的偏移向量
        /// 计算公式：Capsule.center
        /// 用途：获取胶囊体在世界空间中的中心位置
        /// </summary>
        public Vector3 CharacterTransformToCapsuleCenter { get { return _characterTransformToCapsuleCenter; } }
        private Vector3 _characterTransformToCapsuleCenter;
        /// <summary>
        /// 胶囊体底部端点相对于角色Transform的偏移向量
        /// 计算公式：Capsule.center + (-WorldUp * Capsule.height * 0.5f)
        /// 用途：地面检测、移动下边界计算
        /// </summary>
        public Vector3 CharacterTransformToCapsuleBottom { get { return _characterTransformToCapsuleBottom; } }
        private Vector3 _characterTransformToCapsuleBottom;
        /// <summary>
        /// 胶囊体顶部端点相对于角色Transform的偏移向量
        /// 计算公式：Capsule.center + (WorldUp * Capsule.height * 0.5f)
        /// 用途：移动上边界计算
        /// </summary>
        public Vector3 CharacterTransformToCapsuleTop { get { return _characterTransformToCapsuleTop; } }
        private Vector3 _characterTransformToCapsuleTop;
        /// <summary>
        /// 胶囊体底部半球中心点相对于角色Transform的偏移向量
        /// 计算公式：Capsule.center + (-WorldUp * Capsule.height * 0.5f) + (WorldUp * Capsule.radius)
        /// 用途：重叠检测(Overlap)、地面探测(Probe)时确定胶囊体底部的精确位置
        /// 注意：这是胶囊体实际的最底部点（考虑到底部半球也有高度）
        /// </summary>
        public Vector3 CharacterTransformToCapsuleBottomHemi { get { return _characterTransformToCapsuleBottomHemi; } }
        private Vector3 _characterTransformToCapsuleBottomHemi;
        /// <summary>
        /// 胶囊体顶部半球中心点相对于角色Transform的偏移向量
        /// 计算公式：Capsule.center + (WorldUp * Capsule.height * 0.5f) + (-WorldUp * Capsule.radius)
        /// 用途：重叠检测(Overlap)时确定胶囊体顶部的精确位置
        /// 注意：这是胶囊体实际的最顶部点（考虑到顶部半球也有高度）
        /// </summary>
        public Vector3 CharacterTransformToCapsuleTopHemi { get { return _characterTransformToCapsuleTopHemi; } }
        private Vector3 _characterTransformToCapsuleTopHemi;
        /// <summary>
        /// The character's velocity resulting from standing on rigidbodies or PhysicsMover
        /// </summary>
        public Vector3 AttachedRigidbodyVelocity { get { return _attachedRigidbodyVelocity; } }
        private Vector3 _attachedRigidbodyVelocity;
        /// <summary>
        /// The number of overlaps detected so far during character update (is reset at the beginning of the update)
        /// </summary>
        public int OverlapsCount { get { return _overlapsCount; } }
        private int _overlapsCount;
        /// <summary>
        /// The overlaps detected so far during character update
        /// </summary>
        public OverlapResult[] Overlaps { get { return _overlaps; } }
        private OverlapResult[] _overlaps = new OverlapResult[MaxRigidbodyOverlapsCount];

        /// <summary>
        /// The motor's assigned controller
        /// </summary>
        [NonSerialized]
        public ICharacterController CharacterController;
        /// <summary>
        /// Did the motor's last swept collision detection find a ground?
        /// </summary>
        [NonSerialized]
        public bool LastMovementIterationFoundAnyGround;
        /// <summary>
        /// Index of this motor in KinematicCharacterSystem arrays
        /// </summary>
        [NonSerialized]
        public int IndexInCharacterSystem;
        /// <summary>
        /// Remembers initial position before all simulation are done
        /// </summary>
        [NonSerialized]
        public Vector3 InitialTickPosition;
        /// <summary>
        /// Remembers initial rotation before all simulation are done
        /// </summary>
        [NonSerialized]
        public Quaternion InitialTickRotation;
        /// <summary>
        /// Specifies a Rigidbody to stay attached to
        /// </summary>
        [NonSerialized]
        public Rigidbody AttachedRigidbodyOverride;
        /// <summary>
        /// The character's velocity resulting from direct movement
        /// </summary>
        [NonSerialized]
        public Vector3 BaseVelocity;

        // Private
        private RaycastHit[] _internalCharacterHits = new RaycastHit[MaxHitsBudget];
        private Collider[] _internalProbedColliders = new Collider[MaxCollisionBudget];
        private List<Rigidbody> _rigidbodiesPushedThisMove = new List<Rigidbody>(16);
        private RigidbodyProjectionHit[] _internalRigidbodyProjectionHits = new RigidbodyProjectionHit[MaxRigidbodyOverlapsCount];
        private Rigidbody _lastAttachedRigidbody;
        private bool _solveMovementCollisions = true;
        private bool _solveGrounding = true;//是否启用地面检测
        private bool _movePositionDirty = false;
        private Vector3 _movePositionTarget = Vector3.zero;
        private bool _moveRotationDirty = false;
        private Quaternion _moveRotationTarget = Quaternion.identity;
        private bool _lastSolvedOverlapNormalDirty = false;
        private Vector3 _lastSolvedOverlapNormal = Vector3.forward;
        private int _rigidbodyProjectionHitCount = 0;
        private bool _isMovingFromAttachedRigidbody = false;//角色当前是否正在使用 AttachedRigidbody 来移动
        private bool _mustUnground = false;
        private float _mustUngroundTimeCounter = 0f;
        private Vector3 _cachedWorldUp = Vector3.up;
        private Vector3 _cachedWorldForward = Vector3.forward;
        private Vector3 _cachedWorldRight = Vector3.right;
        private Vector3 _cachedZeroVector = Vector3.zero;

        private Quaternion _transientRotation;
        /// <summary>
        /// The character's goal rotation in its movement calculations (always up-to-date during the character update phase)
        /// </summary>
        public Quaternion TransientRotation
        {
            get
            {
                return _transientRotation;
            }
            private set
            {
                _transientRotation = value;
                _characterUp = _transientRotation * _cachedWorldUp;
                _characterForward = _transientRotation * _cachedWorldForward;
                _characterRight = _transientRotation * _cachedWorldRight;
            }
        }

        /// <summary>
        /// The character's total velocity, including velocity from standing on rigidbodies or PhysicsMover
        /// </summary>
        public Vector3 Velocity
        {
            get
            {
                return BaseVelocity + _attachedRigidbodyVelocity;
            }
        }

        // Warning: Don't touch these constants unless you know exactly what you're doing!
        public const int MaxHitsBudget = 16;
        public const int MaxCollisionBudget = 16;
        public const int MaxGroundingSweepIterations = 2;
        public const int MaxSteppingSweepIterations = 3;
        public const int MaxRigidbodyOverlapsCount = 16;
        public const float CollisionOffset = 0.01f;
        public const float GroundProbeReboundDistance = 0.02f;
        public const float MinimumGroundProbingDistance = 0.005f;
        public const float GroundProbingBackstepDistance = 0.1f;
        public const float SweepProbingBackstepDistance = 0.002f;
        public const float SecondaryProbesVertical = 0.02f;
        public const float SecondaryProbesHorizontal = 0.001f;
        public const float MinVelocityMagnitude = 0.01f;
        public const float SteppingForwardDistance = 0.03f;
        public const float MinDistanceForLedge = 0.05f;
        public const float CorrelationForVerticalObstruction = 0.01f;
        public const float ExtraSteppingForwardDistance = 0.01f;
        public const float ExtraStepHeightPadding = 0.01f;
#pragma warning restore 0414 

        private void OnEnable()
        {
            KinematicCharacterSystem.EnsureCreation();
            KinematicCharacterSystem.RegisterCharacterMotor(this);
        }

        private void OnDisable()
        {
            KinematicCharacterSystem.UnregisterCharacterMotor(this);
        }

        private void Reset()
        {
            ValidateData();
        }

        private void OnValidate()
        {
            ValidateData();
        }

        [ContextMenu("Remove Component")]
        private void HandleRemoveComponent()
        {
            CapsuleCollider tmpCapsule = gameObject.GetComponent<CapsuleCollider>();
            DestroyImmediate(this);
            DestroyImmediate(tmpCapsule);
        }

        /// <summary>
        /// Handle validating all required values
        /// </summary>
        public void ValidateData()
        {
            Capsule = GetComponent<CapsuleCollider>();
            CapsuleRadius = Mathf.Clamp(CapsuleRadius, 0f, CapsuleHeight * 0.5f);
            Capsule.direction = 1;
            Capsule.sharedMaterial = CapsulePhysicsMaterial;
            SetCapsuleDimensions(CapsuleRadius, CapsuleHeight, CapsuleYOffset);

            MaxStepHeight = Mathf.Clamp(MaxStepHeight, 0f, Mathf.Infinity);
            MinRequiredStepDepth = Mathf.Clamp(MinRequiredStepDepth, 0f, CapsuleRadius);
            MaxStableDistanceFromLedge = Mathf.Clamp(MaxStableDistanceFromLedge, 0f, CapsuleRadius);

            transform.localScale = Vector3.one;

#if UNITY_EDITOR
            Capsule.hideFlags = HideFlags.NotEditable;
            if (!Mathf.Approximately(transform.lossyScale.x, 1f) || !Mathf.Approximately(transform.lossyScale.y, 1f) || !Mathf.Approximately(transform.lossyScale.z, 1f))
            {
                Debug.LogError("Character's lossy scale is not (1,1,1). This is not allowed. Make sure the character's transform and all of its parents have a (1,1,1) scale.", this.gameObject);
            }
#endif
        }

        /// <summary>
        /// 设置胶囊碰撞器是否能够检测碰撞的情况
        /// </summary>
        public void SetCapsuleCollisionsActivation(bool collisionsActive)
        {
            Capsule.isTrigger = !collisionsActive;
        }

        /// <summary>
        ///设定电机在移动时（或移动至某处时）是否会对碰撞进行处理。
        /// </summary>
        public void SetMovementCollisionsSolvingActivation(bool movementCollisionsSolvingActive)
        {
            _solveMovementCollisions = movementCollisionsSolvingActive;
        }

        /// <summary>
        /// 设置是否对所有命中情况进行接地评估
        /// </summary>
        public void SetGroundSolvingActivation(bool stabilitySolvingActive)
        {
            _solveGrounding = stabilitySolvingActive;
        }

        /// <summary>
        /// Sets the character's position directly
        /// </summary>
        public void SetPosition(Vector3 position, bool bypassInterpolation = true)
        {
            _transform.position = position;
            _initialSimulationPosition = position;
            _transientPosition = position;

            if (bypassInterpolation)
            {
                InitialTickPosition = position;
            }
        }

        /// <summary>
        /// Sets the character's rotation directly
        /// </summary>
        public void SetRotation(Quaternion rotation, bool bypassInterpolation = true)
        {
            _transform.rotation = rotation;
            _initialSimulationRotation = rotation;
            TransientRotation = rotation;

            if (bypassInterpolation)
            {
                InitialTickRotation = rotation;
            }
        }

        /// <summary>
        /// Sets the character's position and rotation directly
        /// </summary>
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation, bool bypassInterpolation = true)
        {
            _transform.SetPositionAndRotation(position, rotation);
            _initialSimulationPosition = position;
            _initialSimulationRotation = rotation;
            _transientPosition = position;
            TransientRotation = rotation;

            if (bypassInterpolation)
            {
                InitialTickPosition = position;
                InitialTickRotation = rotation;
            }
        }

        /// <summary>
        /// Moves the character position, taking all movement collision solving int account. The actual move is done the next time the motor updates are called
        /// </summary>
        public void MoveCharacter(Vector3 toPosition)
        {
            _movePositionDirty = true;
            _movePositionTarget = toPosition;
        }

        /// <summary>
        /// Moves the character rotation. The actual move is done the next time the motor updates are called
        /// </summary>
        public void RotateCharacter(Quaternion toRotation)
        {
            _moveRotationDirty = true;
            _moveRotationTarget = toRotation;
        }

        /// <summary>
        /// Returns all the state information of the motor that is pertinent for simulation
        /// </summary>
        public KinematicCharacterMotorState GetState()
        {
            KinematicCharacterMotorState state = new KinematicCharacterMotorState();

            state.Position = _transientPosition;
            state.Rotation = _transientRotation;

            state.BaseVelocity = BaseVelocity;
            state.AttachedRigidbodyVelocity = _attachedRigidbodyVelocity;

            state.MustUnground = _mustUnground;
            state.MustUngroundTime = _mustUngroundTimeCounter;
            state.LastMovementIterationFoundAnyGround = LastMovementIterationFoundAnyGround;
            state.GroundingStatus.CopyFrom(GroundingStatus);
            state.AttachedRigidbody = _attachedRigidbody;

            return state;
        }

        /// <summary>
        /// Applies a motor state instantly
        /// </summary>
        public void ApplyState(KinematicCharacterMotorState state, bool bypassInterpolation = true)
        {
            SetPositionAndRotation(state.Position, state.Rotation, bypassInterpolation);

            BaseVelocity = state.BaseVelocity;
            _attachedRigidbodyVelocity = state.AttachedRigidbodyVelocity;

            _mustUnground = state.MustUnground;
            _mustUngroundTimeCounter = state.MustUngroundTime;
            LastMovementIterationFoundAnyGround = state.LastMovementIterationFoundAnyGround;
            GroundingStatus.CopyFrom(state.GroundingStatus);
            _attachedRigidbody = state.AttachedRigidbody;
        }

        /// <summary>
        /// 设置胶囊体尺寸。同时缓存重要的胶囊体数据，用于后续的碰撞检测和移动计算
        /// 这是KCC调整角色碰撞体的核心方法
        /// </summary>
        /// <param name="radius">胶囊体半径</param>
        /// <param name="height">胶囊体高度</param>
        /// <param name="yOffset">胶囊体中心相对于角色Transform的Y轴偏移</param>
        public void SetCapsuleDimensions(float radius, float height, float yOffset)
        {
            // 安全检查：确保高度至少比直径大0.01，防止无效的胶囊体几何形状
            height = Mathf.Max(height, (radius * 2f) + 0.01f);

            // 1. 更新私有字段（缓存数据）
            CapsuleRadius = radius;
            CapsuleHeight = height;
            CapsuleYOffset = yOffset;

            // 2. 直接修改Unity胶囊体碰撞器组件
            Capsule.radius = CapsuleRadius;
            // 确保Unity的height属性符合胶囊体几何要求（高度必须大于等于直径）
            Capsule.height = Mathf.Clamp(CapsuleHeight, CapsuleRadius * 2f, CapsuleHeight);
            Capsule.center = new Vector3(0f, CapsuleYOffset, 0f);

            // 3. 缓存胶囊体各关键点的相对位置（用于碰撞检测和移动计算）
            // 胶囊体中心点相对于角色Transform的位置
            _characterTransformToCapsuleCenter = Capsule.center;
            // 胶囊体底部端点
            _characterTransformToCapsuleBottom = 
                    Capsule.center + (-_cachedWorldUp * (Capsule.height * 0.5f));
            // 胶囊体顶部端点
            _characterTransformToCapsuleTop = 
                    Capsule.center + (_cachedWorldUp * (Capsule.height * 0.5f));
            // 胶囊体底部半球中心点（用于处理地面检测等）
            _characterTransformToCapsuleBottomHemi =
                     Capsule.center + (-_cachedWorldUp * (Capsule.height * 0.5f)) + (_cachedWorldUp * Capsule.radius);
            // 胶囊体顶部半球中心点
            _characterTransformToCapsuleTopHemi =
                     Capsule.center + (_cachedWorldUp * (Capsule.height * 0.5f)) + (-_cachedWorldUp * Capsule.radius);
        }

        private void Awake()
        {
            _transform = this.transform;
            ValidateData();

            _transientPosition = _transform.position;
            TransientRotation = _transform.rotation;

            // Build CollidableLayers mask
            CollidableLayers = 0;
            for (int i = 0; i < 32; i++)
            {
                if (!Physics.GetIgnoreLayerCollision(this.gameObject.layer, i))
                {
                    CollidableLayers |= (1 << i);
                }
            }

            SetCapsuleDimensions(CapsuleRadius, CapsuleHeight, CapsuleYOffset);
        }

        /// <summary>
        /// 更新阶段 1 应在物理移动器计算出其速度之后，但在模拟其目标位置/旋转之前调用。其职责包括：
        /// - 初始化所有更新值
        /// - 处理 MovePosition 调用
        /// - 解决初始碰撞重叠
        /// - 探测地面
        /// - 处理检测潜在可交互刚体
        /// </summary>
        public void UpdatePhase1(float deltaTime)
        {
            // NaN propagation safety stop
            if (float.IsNaN(BaseVelocity.x) || float.IsNaN(BaseVelocity.y) || float.IsNaN(BaseVelocity.z))
            {
                BaseVelocity = Vector3.zero;
            }
            if (float.IsNaN(_attachedRigidbodyVelocity.x) || float.IsNaN(_attachedRigidbodyVelocity.y) || float.IsNaN(_attachedRigidbodyVelocity.z))
            {
                _attachedRigidbodyVelocity = Vector3.zero;
            }

#if UNITY_EDITOR
            if (!Mathf.Approximately(_transform.lossyScale.x, 1f) || !Mathf.Approximately(_transform.lossyScale.y, 1f) || !Mathf.Approximately(_transform.lossyScale.z, 1f))
            {
                Debug.LogError("Character's lossy scale is not (1,1,1). This is not allowed. Make sure the character's transform and all of its parents have a (1,1,1) scale.", this.gameObject);
            }
#endif

            _rigidbodiesPushedThisMove.Clear();

            // Before update
            CharacterController.BeforeCharacterUpdate(deltaTime);

            _transientPosition = _transform.position;
            TransientRotation = _transform.rotation;
            _initialSimulationPosition = _transientPosition;
            _initialSimulationRotation = _transientRotation;
            _rigidbodyProjectionHitCount = 0;
            _overlapsCount = 0;
            _lastSolvedOverlapNormalDirty = false;

            #region Handle Move Position
            if (_movePositionDirty)
            {
                if (_solveMovementCollisions)
                {
                    Vector3 tmpVelocity = GetVelocityFromMovement(_movePositionTarget - _transientPosition, deltaTime);
                    if (InternalCharacterMove(ref tmpVelocity, deltaTime))
                    {
                        if (InteractiveRigidbodyHandling)
                        {
                            ProcessVelocityForRigidbodyHits(ref tmpVelocity, deltaTime);
                        }
                    }
                }
                else
                {
                    _transientPosition = _movePositionTarget;
                }

                _movePositionDirty = false;
            }
            #endregion
            //保存上一帧状态
            LastGroundingStatus.CopyFrom(GroundingStatus);
            //重置当前帧状态
            GroundingStatus = new CharacterGroundingReport();
            //更新地面法线
            GroundingStatus.GroundNormal = _characterUp;

            if (_solveMovementCollisions)
            {
                #region Resolve initial overlaps
                Vector3 resolutionDirection = _cachedWorldUp;
                float resolutionDistance = 0f;
                int iterationsMade = 0;
                bool overlapSolved = false;
                while (iterationsMade < MaxDecollisionIterations && !overlapSolved)
                {
                    int nbOverlaps = CharacterCollisionsOverlap(_transientPosition, _transientRotation, _internalProbedColliders);

                    if (nbOverlaps > 0)
                    {
                        // Solve overlaps that aren't against dynamic rigidbodies or physics movers
                        for (int i = 0; i < nbOverlaps; i++)
                        {
                            if (GetInteractiveRigidbody(_internalProbedColliders[i]) == null)
                            {
                                // Process overlap
                                Transform overlappedTransform = _internalProbedColliders[i].GetComponent<Transform>();
                                if (Physics.ComputePenetration(
                                        Capsule,
                                        _transientPosition,
                                        _transientRotation,
                                        _internalProbedColliders[i],
                                        overlappedTransform.position,
                                        overlappedTransform.rotation,
                                        out resolutionDirection,
                                        out resolutionDistance))
                                {
                                    // Resolve along obstruction direction
                                    HitStabilityReport mockReport = new HitStabilityReport();
                                    mockReport.IsStable = IsStableOnNormal(resolutionDirection);
                                    resolutionDirection = GetObstructionNormal(resolutionDirection, mockReport.IsStable);

                                    // Solve overlap
                                    Vector3 resolutionMovement = resolutionDirection * (resolutionDistance + CollisionOffset);
                                    _transientPosition += resolutionMovement;

                                    // Remember overlaps
                                    if (_overlapsCount < _overlaps.Length)
                                    {
                                        _overlaps[_overlapsCount] = new OverlapResult(resolutionDirection, _internalProbedColliders[i]);
                                        _overlapsCount++;
                                    }

                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        overlapSolved = true;
                    }

                    iterationsMade++;
                }
                #endregion
            }

            #region Ground Probing and Snapping
            // Handle ungrounding
            if (_solveGrounding)
            {
                // 将角色位置向上偏移，使其超出地面探测范围
                if (MustUnground())
                {
                    _transientPosition += _characterUp * (MinimumGroundProbingDistance * 1.5f);
                }
                else
                {
                    // Choose the appropriate ground probing distance
                    float selectedGroundProbingDistance = MinimumGroundProbingDistance;
                    if (!LastGroundingStatus.SnappingPrevented && (LastGroundingStatus.IsStableOnGround || LastMovementIterationFoundAnyGround))
                    {
                        if (StepHandling != StepHandlingMethod.None)
                        {
                            selectedGroundProbingDistance = Mathf.Max(CapsuleRadius, MaxStepHeight);
                        }
                        else
                        {
                            selectedGroundProbingDistance = CapsuleRadius;
                        }

                        selectedGroundProbingDistance += GroundDetectionExtraDistance;
                    }

                    ProbeGround(ref _transientPosition, _transientRotation, selectedGroundProbingDistance, ref GroundingStatus);

                    if (!LastGroundingStatus.IsStableOnGround && GroundingStatus.IsStableOnGround)
                    {
                        // Handle stable landing
                        BaseVelocity = Vector3.ProjectOnPlane(BaseVelocity, CharacterUp);
                        BaseVelocity = GetDirectionTangentToSurface(BaseVelocity, GroundingStatus.GroundNormal) * BaseVelocity.magnitude;
                    }
                }
            }

            LastMovementIterationFoundAnyGround = false;

            if (_mustUngroundTimeCounter > 0f)
            {
                _mustUngroundTimeCounter -= deltaTime;
            }
            _mustUnground = false;
            #endregion

            if (_solveGrounding)    
            {
                CharacterController.PostGroundingUpdate(deltaTime);
            }

            if (InteractiveRigidbodyHandling)
            {
                #region Interactive Rigidbody Handling 
                _lastAttachedRigidbody = _attachedRigidbody;
                if (AttachedRigidbodyOverride)
                {
                    _attachedRigidbody = AttachedRigidbodyOverride;
                }
                else
                {
                    // Detect interactive rigidbodies from grounding
                    if (GroundingStatus.IsStableOnGround && GroundingStatus.GroundCollider.attachedRigidbody)
                    {
                        Rigidbody interactiveRigidbody = GetInteractiveRigidbody(GroundingStatus.GroundCollider);
                        if (interactiveRigidbody)
                        {
                            _attachedRigidbody = interactiveRigidbody;
                        }
                    }
                    else
                    {
                        _attachedRigidbody = null;
                    }
                }

                Vector3 tmpVelocityFromCurrentAttachedRigidbody = Vector3.zero;
                Vector3 tmpAngularVelocityFromCurrentAttachedRigidbody = Vector3.zero;
                if (_attachedRigidbody)
                {
                    GetVelocityFromRigidbodyMovement(_attachedRigidbody, _transientPosition, deltaTime, out tmpVelocityFromCurrentAttachedRigidbody, out tmpAngularVelocityFromCurrentAttachedRigidbody);
                }

                // Conserve momentum when de-stabilized from an attached rigidbody
                if (PreserveAttachedRigidbodyMomentum && _lastAttachedRigidbody != null && _attachedRigidbody != _lastAttachedRigidbody)
                {
                    BaseVelocity += _attachedRigidbodyVelocity;
                    BaseVelocity -= tmpVelocityFromCurrentAttachedRigidbody;
                }

                // Process additionnal Velocity from attached rigidbody
                _attachedRigidbodyVelocity = _cachedZeroVector;
                if (_attachedRigidbody)
                {
                    _attachedRigidbodyVelocity = tmpVelocityFromCurrentAttachedRigidbody;

                    // Rotation from attached rigidbody
                    Vector3 newForward = Vector3.ProjectOnPlane(Quaternion.Euler(Mathf.Rad2Deg * tmpAngularVelocityFromCurrentAttachedRigidbody * deltaTime) * _characterForward, _characterUp).normalized;
                    TransientRotation = Quaternion.LookRotation(newForward, _characterUp);
                }

                // Cancel out horizontal velocity upon landing on an attached rigidbody
                if (GroundingStatus.GroundCollider &&
                    GroundingStatus.GroundCollider.attachedRigidbody &&
                    GroundingStatus.GroundCollider.attachedRigidbody == _attachedRigidbody &&
                    _attachedRigidbody != null &&
                    _lastAttachedRigidbody == null)
                {
                    BaseVelocity -= Vector3.ProjectOnPlane(_attachedRigidbodyVelocity, _characterUp);
                }

                // Movement from Attached Rigidbody
                if (_attachedRigidbodyVelocity.sqrMagnitude > 0f)
                {
                    _isMovingFromAttachedRigidbody = true;

                    if (_solveMovementCollisions)
                    {
                        // Perform the move from rgdbdy velocity
                        InternalCharacterMove(ref _attachedRigidbodyVelocity, deltaTime);
                    }
                    else
                    {
                        _transientPosition += _attachedRigidbodyVelocity * deltaTime;
                    }

                    _isMovingFromAttachedRigidbody = false;
                }
                #endregion
            }
        }

        /// <summary>
        /// Update phase 2 is meant to be called after physics movers have simulated their goal positions/rotations. 
        /// At the end of this, the TransientPosition/Rotation values will be up-to-date with where the motor should be at the end of its move. 
        /// It is responsible for:
        /// - Solving Rotation
        /// - Handle MoveRotation calls
        /// - Solving potential attached rigidbody overlaps
        /// - Solving Velocity
        /// - Applying planar constraint
        /// </summary>
        public void UpdatePhase2(float deltaTime)
        {
            // Handle rotation
            CharacterController.UpdateRotation(ref _transientRotation, deltaTime);
            TransientRotation = _transientRotation;

            // Handle move rotation
            if (_moveRotationDirty)
            {
                TransientRotation = _moveRotationTarget;
                _moveRotationDirty = false;
            }

            if (_solveMovementCollisions && InteractiveRigidbodyHandling)
            {
                if (InteractiveRigidbodyHandling)
                {
                    #region Solve potential attached rigidbody overlap
                    if (_attachedRigidbody)
                    {
                        float upwardsOffset = Capsule.radius;

                        RaycastHit closestHit;
                        if (CharacterGroundSweep(
                            _transientPosition + (_characterUp * upwardsOffset),
                            _transientRotation,
                            -_characterUp,
                            upwardsOffset,
                            out closestHit))
                        {
                            if (closestHit.collider.attachedRigidbody == _attachedRigidbody && IsStableOnNormal(closestHit.normal))
                            {
                                float distanceMovedUp = (upwardsOffset - closestHit.distance);
                                _transientPosition = _transientPosition + (_characterUp * distanceMovedUp) + (_characterUp * CollisionOffset);
                            }
                        }
                    }
                    #endregion
                }

                if (InteractiveRigidbodyHandling)
                {
                    #region Resolve overlaps that could've been caused by rotation or physics movers simulation pushing the character
                    Vector3 resolutionDirection = _cachedWorldUp;
                    float resolutionDistance = 0f;
                    int iterationsMade = 0;
                    bool overlapSolved = false;
                    while (iterationsMade < MaxDecollisionIterations && !overlapSolved)
                    {
                        int nbOverlaps = CharacterCollisionsOverlap(_transientPosition, _transientRotation, _internalProbedColliders);
                        if (nbOverlaps > 0)
                        {
                            for (int i = 0; i < nbOverlaps; i++)
                            {
                                // Process overlap
                                Transform overlappedTransform = _internalProbedColliders[i].GetComponent<Transform>();
                                if (Physics.ComputePenetration(
                                        Capsule,
                                        _transientPosition,
                                        _transientRotation,
                                        _internalProbedColliders[i],
                                        overlappedTransform.position,
                                        overlappedTransform.rotation,
                                        out resolutionDirection,
                                        out resolutionDistance))
                                {
                                    // Resolve along obstruction direction
                                    HitStabilityReport mockReport = new HitStabilityReport();
                                    mockReport.IsStable = IsStableOnNormal(resolutionDirection);
                                    resolutionDirection = GetObstructionNormal(resolutionDirection, mockReport.IsStable);

                                    // Solve overlap
                                    Vector3 resolutionMovement = resolutionDirection * (resolutionDistance + CollisionOffset);
                                    _transientPosition += resolutionMovement;

                                    // If interactiveRigidbody, register as rigidbody hit for velocity
                                    if (InteractiveRigidbodyHandling)
                                    {
                                        Rigidbody probedRigidbody = GetInteractiveRigidbody(_internalProbedColliders[i]);
                                        if (probedRigidbody != null)
                                        {
                                            HitStabilityReport tmpReport = new HitStabilityReport();
                                            tmpReport.IsStable = IsStableOnNormal(resolutionDirection);
                                            if (tmpReport.IsStable)
                                            {
                                                LastMovementIterationFoundAnyGround = tmpReport.IsStable;
                                            }
                                            if (probedRigidbody != _attachedRigidbody)
                                            {
                                                Vector3 characterCenter = _transientPosition + (_transientRotation * _characterTransformToCapsuleCenter);
                                                Vector3 estimatedCollisionPoint = _transientPosition;


                                                StoreRigidbodyHit(
                                                    probedRigidbody,
                                                    Velocity,
                                                    estimatedCollisionPoint,
                                                    resolutionDirection,
                                                    tmpReport);
                                            }
                                        }
                                    }

                                    // Remember overlaps
                                    if (_overlapsCount < _overlaps.Length)
                                    {
                                        _overlaps[_overlapsCount] = new OverlapResult(resolutionDirection, _internalProbedColliders[i]);
                                        _overlapsCount++;
                                    }

                                    break;
                                }
                            }
                        }
                        else
                        {
                            overlapSolved = true;
                        }

                        iterationsMade++;
                    }
                    #endregion
                }
            }

            // Handle velocity
            CharacterController.UpdateVelocity(ref BaseVelocity, deltaTime);

            //this.CharacterController.UpdateVelocity(ref BaseVelocity, deltaTime);
            if (BaseVelocity.magnitude < MinVelocityMagnitude)
            {
                BaseVelocity = Vector3.zero;
            }

            #region Calculate Character movement from base velocity   
            // Perform the move from base velocity
            if (BaseVelocity.sqrMagnitude > 0f)
            {
                if (_solveMovementCollisions)
                {
                    InternalCharacterMove(ref BaseVelocity, deltaTime);
                }
                else
                {
                    _transientPosition += BaseVelocity * deltaTime;
                }
            }

            // Process rigidbody hits/overlaps to affect velocity
            if (InteractiveRigidbodyHandling)
            {
                ProcessVelocityForRigidbodyHits(ref BaseVelocity, deltaTime);
            }
            #endregion

            // Handle planar constraint
            if (HasPlanarConstraint)
            {
                _transientPosition = _initialSimulationPosition + Vector3.ProjectOnPlane(_transientPosition - _initialSimulationPosition, PlanarConstraintAxis.normalized);
            }

            // Discrete collision detection
            if (DiscreteCollisionEvents)
            {
                int nbOverlaps = CharacterCollisionsOverlap(_transientPosition, _transientRotation, _internalProbedColliders, CollisionOffset * 2f);
                for (int i = 0; i < nbOverlaps; i++)
                {
                    CharacterController.OnDiscreteCollisionDetected(_internalProbedColliders[i]);
                }
            }

            CharacterController.AfterCharacterUpdate(deltaTime);
        }

        /// <summary>
        /// Determines if motor can be considered stable on given slope normal
        /// </summary>
        private bool IsStableOnNormal(Vector3 normal)
        {
            return Vector3.Angle(_characterUp, normal) <= MaxStableSlopeAngle;
        }

        /// <summary>
        /// Determines if motor can be considered stable on given slope normal
        /// </summary>
        private bool IsStableWithSpecialCases(ref HitStabilityReport stabilityReport, Vector3 velocity)
        {
            if (LedgeAndDenivelationHandling)
            {
                if (stabilityReport.LedgeDetected)
                {
                    if (stabilityReport.IsMovingTowardsEmptySideOfLedge)
                    {
                        // Max snap vel
                        Vector3 velocityOnLedgeNormal = Vector3.Project(velocity, stabilityReport.LedgeFacingDirection);
                        if (velocityOnLedgeNormal.magnitude >= MaxVelocityForLedgeSnap)
                        {
                            return false;
                        }
                    }

                    // Distance from ledge
                    if (stabilityReport.IsOnEmptySideOfLedge && stabilityReport.DistanceFromLedge > MaxStableDistanceFromLedge)
                    {
                        return false;
                    }
                }

                // "Launching" off of slopes of a certain denivelation angle
                if (LastGroundingStatus.FoundAnyGround && stabilityReport.InnerNormal.sqrMagnitude != 0f && stabilityReport.OuterNormal.sqrMagnitude != 0f)
                {
                    float denivelationAngle = Vector3.Angle(stabilityReport.InnerNormal, stabilityReport.OuterNormal);
                    if (denivelationAngle > MaxStableDenivelationAngle)
                    {
                        return false;
                    }
                    else
                    {
                        denivelationAngle = Vector3.Angle(LastGroundingStatus.InnerGroundNormal, stabilityReport.OuterNormal);
                        if (denivelationAngle > MaxStableDenivelationAngle)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Probes for valid ground and midifies the input transientPosition if ground snapping occurs
        /// </summary>
        public void ProbeGround(ref Vector3 probingPosition, Quaternion atRotation, float probingDistance, ref CharacterGroundingReport groundingReport)
        {
            if (probingDistance < MinimumGroundProbingDistance)
            {
                probingDistance = MinimumGroundProbingDistance;
            }

            int groundSweepsMade = 0;
            RaycastHit groundSweepHit = new RaycastHit();
            bool groundSweepingIsOver = false;
            Vector3 groundSweepPosition = probingPosition;
            Vector3 groundSweepDirection = (atRotation * -_cachedWorldUp);
            float groundProbeDistanceRemaining = probingDistance;
            while (groundProbeDistanceRemaining > 0 && (groundSweepsMade <= MaxGroundingSweepIterations) && !groundSweepingIsOver)
            {
                // Sweep for ground detection
                if (CharacterGroundSweep(
                        groundSweepPosition, // position
                        atRotation, // rotation
                        groundSweepDirection, // direction
                        groundProbeDistanceRemaining, // distance
                        out groundSweepHit)) // hit
                {
                    Vector3 targetPosition = groundSweepPosition + (groundSweepDirection * groundSweepHit.distance);
                    HitStabilityReport groundHitStabilityReport = new HitStabilityReport();
                    EvaluateHitStability(groundSweepHit.collider, groundSweepHit.normal, groundSweepHit.point, targetPosition, _transientRotation, BaseVelocity, ref groundHitStabilityReport);

                    groundingReport.FoundAnyGround = true;
                    groundingReport.GroundNormal = groundSweepHit.normal;
                    groundingReport.InnerGroundNormal = groundHitStabilityReport.InnerNormal;
                    groundingReport.OuterGroundNormal = groundHitStabilityReport.OuterNormal;
                    groundingReport.GroundCollider = groundSweepHit.collider;
                    groundingReport.GroundPoint = groundSweepHit.point;
                    groundingReport.SnappingPrevented = false;

                    // Found stable ground
                    if (groundHitStabilityReport.IsStable)
                    {
                        // Find all scenarios where ground snapping should be canceled
                        groundingReport.SnappingPrevented = !IsStableWithSpecialCases(ref groundHitStabilityReport, BaseVelocity);

                        groundingReport.IsStableOnGround = true;

                        // Ground snapping
                        if (!groundingReport.SnappingPrevented)
                        {
                            probingPosition = groundSweepPosition + (groundSweepDirection * (groundSweepHit.distance - CollisionOffset));
                        }

                        CharacterController.OnGroundHit(groundSweepHit.collider, groundSweepHit.normal, groundSweepHit.point, ref groundHitStabilityReport);
                        groundSweepingIsOver = true;
                    }
                    else
                    {
                        // Calculate movement from this iteration and advance position
                        Vector3 sweepMovement = (groundSweepDirection * groundSweepHit.distance) + ((atRotation * _cachedWorldUp) * Mathf.Max(CollisionOffset, groundSweepHit.distance));
                        groundSweepPosition = groundSweepPosition + sweepMovement;

                        // Set remaining distance
                        groundProbeDistanceRemaining = Mathf.Min(GroundProbeReboundDistance, Mathf.Max(groundProbeDistanceRemaining - sweepMovement.magnitude, 0f));

                        // Reorient direction
                        groundSweepDirection = Vector3.ProjectOnPlane(groundSweepDirection, groundSweepHit.normal).normalized;
                    }
                }
                else
                {
                    groundSweepingIsOver = true;
                }

                groundSweepsMade++;
            }
        }

        /// <summary>
        /// Forces the character to unground itself on its next grounding update
        /// </summary>
        public void ForceUnground(float time = 0.1f)
        {
            _mustUnground = true;
            _mustUngroundTimeCounter = time;
        }

        public bool MustUnground()
        {
            return _mustUnground || _mustUngroundTimeCounter > 0f;
        }

        /// <summary>
        /// Returns the direction adjusted to be tangent to a specified surface normal relatively to the character's up direction.
        /// Useful for reorienting a direction on a slope without any lateral deviation in trajectory
        /// </summary>
        public Vector3 GetDirectionTangentToSurface(Vector3 direction, Vector3 surfaceNormal)
        {
            Vector3 directionRight = Vector3.Cross(direction, _characterUp);
            return Vector3.Cross(surfaceNormal, directionRight).normalized;
        }

        /// <summary>
        /// Moves the character's position by given movement while taking into account all physics simulation, step-handling and 
        /// velocity projection rules that affect the character motor
        /// </summary>
        /// <returns> Returns false if movement could not be solved until the end </returns>
        private bool InternalCharacterMove(ref Vector3 transientVelocity, float deltaTime)
        {
            if (deltaTime <= 0f)
                return false;

            // Planar constraint
            if (HasPlanarConstraint)
            {
                transientVelocity = Vector3.ProjectOnPlane(transientVelocity, PlanarConstraintAxis.normalized);
            }

            bool wasCompleted = true;
            Vector3 remainingMovementDirection = transientVelocity.normalized;
            float remainingMovementMagnitude = transientVelocity.magnitude * deltaTime;
            Vector3 originalVelocityDirection = remainingMovementDirection;
            int sweepsMade = 0;
            bool hitSomethingThisSweepIteration = true;
            Vector3 tmpMovedPosition = _transientPosition;
            bool previousHitIsStable = false;
            Vector3 previousVelocity = _cachedZeroVector;
            Vector3 previousObstructionNormal = _cachedZeroVector;
            MovementSweepState sweepState = MovementSweepState.Initial;

            // Project movement against current overlaps before doing the sweeps
            for (int i = 0; i < _overlapsCount; i++)
            {
                Vector3 overlapNormal = _overlaps[i].Normal;
                if (Vector3.Dot(remainingMovementDirection, overlapNormal) < 0f)
                {
                    bool stableOnHit = IsStableOnNormal(overlapNormal) && !MustUnground();
                    Vector3 velocityBeforeProjection = transientVelocity;
                    Vector3 obstructionNormal = GetObstructionNormal(overlapNormal, stableOnHit);

                    InternalHandleVelocityProjection(
                        stableOnHit,
                        overlapNormal,
                        obstructionNormal,
                        originalVelocityDirection,
                        ref sweepState,
                        previousHitIsStable,
                        previousVelocity,
                        previousObstructionNormal,
                        ref transientVelocity,
                        ref remainingMovementMagnitude,
                        ref remainingMovementDirection);

                    previousHitIsStable = stableOnHit;
                    previousVelocity = velocityBeforeProjection;
                    previousObstructionNormal = obstructionNormal;
                }
            }

            // Sweep the desired movement to detect collisions
            while (remainingMovementMagnitude > 0f &&
                (sweepsMade <= MaxMovementIterations) &&
                hitSomethingThisSweepIteration)
            {
                bool foundClosestHit = false;
                Vector3 closestSweepHitPoint = default;
                Vector3 closestSweepHitNormal = default;
                float closestSweepHitDistance = 0f;
                Collider closestSweepHitCollider = null;

                if (CheckMovementInitialOverlaps)
                {
                    int numOverlaps = CharacterCollisionsOverlap(
                                        tmpMovedPosition,
                                        _transientRotation,
                                        _internalProbedColliders,
                                        0f,
                                        false);
                    if (numOverlaps > 0)
                    {
                        closestSweepHitDistance = 0f;

                        float mostObstructingOverlapNormalDotProduct = 2f;

                        for (int i = 0; i < numOverlaps; i++)
                        {
                            Collider tmpCollider = _internalProbedColliders[i];

                            if (Physics.ComputePenetration(
                                Capsule,
                                tmpMovedPosition,
                                _transientRotation,
                                tmpCollider,
                                tmpCollider.transform.position,
                                tmpCollider.transform.rotation,
                                out Vector3 resolutionDirection,
                                out float resolutionDistance))
                            {
                                float dotProduct = Vector3.Dot(remainingMovementDirection, resolutionDirection);
                                if (dotProduct < 0f && dotProduct < mostObstructingOverlapNormalDotProduct)
                                {
                                    mostObstructingOverlapNormalDotProduct = dotProduct;

                                    closestSweepHitNormal = resolutionDirection;
                                    closestSweepHitCollider = tmpCollider;
                                    closestSweepHitPoint = tmpMovedPosition + (_transientRotation * CharacterTransformToCapsuleCenter) + (resolutionDirection * resolutionDistance);

                                    foundClosestHit = true;
                                }
                            }
                        }
                    }
                }

                if (!foundClosestHit && CharacterCollisionsSweep(
                        tmpMovedPosition, // position
                        _transientRotation, // rotation
                        remainingMovementDirection, // direction
                        remainingMovementMagnitude + CollisionOffset, // distance
                        out RaycastHit closestSweepHit, // closest hit
                        _internalCharacterHits) // all hits
                    > 0)
                {
                    closestSweepHitNormal = closestSweepHit.normal;
                    closestSweepHitDistance = closestSweepHit.distance;
                    closestSweepHitCollider = closestSweepHit.collider;
                    closestSweepHitPoint = closestSweepHit.point;

                    foundClosestHit = true;
                }

                if (foundClosestHit)
                {
                    // Calculate movement from this iteration
                    Vector3 sweepMovement = (remainingMovementDirection * (Mathf.Max(0f, closestSweepHitDistance - CollisionOffset)));
                    tmpMovedPosition += sweepMovement;
                    remainingMovementMagnitude -= sweepMovement.magnitude;

                    // Evaluate if hit is stable
                    HitStabilityReport moveHitStabilityReport = new HitStabilityReport();
                    EvaluateHitStability(closestSweepHitCollider, closestSweepHitNormal, closestSweepHitPoint, tmpMovedPosition, _transientRotation, transientVelocity, ref moveHitStabilityReport);

                    // Handle stepping up steps points higher than bottom capsule radius
                    bool foundValidStepHit = false;
                    if (_solveGrounding && StepHandling != StepHandlingMethod.None && moveHitStabilityReport.ValidStepDetected)
                    {
                        float obstructionCorrelation = Mathf.Abs(Vector3.Dot(closestSweepHitNormal, _characterUp));
                        if (obstructionCorrelation <= CorrelationForVerticalObstruction)
                        {
                            Vector3 stepForwardDirection = Vector3.ProjectOnPlane(-closestSweepHitNormal, _characterUp).normalized;
                            Vector3 stepCastStartPoint = (tmpMovedPosition + (stepForwardDirection * SteppingForwardDistance)) +
                                (_characterUp * MaxStepHeight);

                            // Cast downward from the top of the stepping height
                            int nbStepHits = CharacterCollisionsSweep(
                                                stepCastStartPoint, // position
                                                _transientRotation, // rotation
                                                -_characterUp, // direction
                                                MaxStepHeight, // distance
                                                out RaycastHit closestStepHit, // closest hit
                                                _internalCharacterHits,
                                                0f,
                                                true); // all hits 

                            // Check for hit corresponding to stepped collider
                            for (int i = 0; i < nbStepHits; i++)
                            {
                                if (_internalCharacterHits[i].collider == moveHitStabilityReport.SteppedCollider)
                                {
                                    Vector3 endStepPosition = stepCastStartPoint + (-_characterUp * (_internalCharacterHits[i].distance - CollisionOffset));
                                    tmpMovedPosition = endStepPosition;
                                    foundValidStepHit = true;

                                    // Project velocity on ground normal at step
                                    transientVelocity = Vector3.ProjectOnPlane(transientVelocity, CharacterUp);
                                    remainingMovementDirection = transientVelocity.normalized;

                                    break;
                                }
                            }
                        }
                    }

                    // Handle movement solving
                    if (!foundValidStepHit)
                    {
                        Vector3 obstructionNormal = GetObstructionNormal(closestSweepHitNormal, moveHitStabilityReport.IsStable);

                        // Movement hit callback
                        CharacterController.OnMovementHit(closestSweepHitCollider, closestSweepHitNormal, closestSweepHitPoint, ref moveHitStabilityReport);

                        // Handle remembering rigidbody hits
                        if (InteractiveRigidbodyHandling && closestSweepHitCollider.attachedRigidbody)
                        {
                            StoreRigidbodyHit(
                                closestSweepHitCollider.attachedRigidbody,
                                transientVelocity,
                                closestSweepHitPoint,
                                obstructionNormal,
                                moveHitStabilityReport);
                        }

                        bool stableOnHit = moveHitStabilityReport.IsStable && !MustUnground();
                        Vector3 velocityBeforeProj = transientVelocity;

                        // Project velocity for next iteration
                        InternalHandleVelocityProjection(
                            stableOnHit,
                            closestSweepHitNormal,
                            obstructionNormal,
                            originalVelocityDirection,
                            ref sweepState,
                            previousHitIsStable,
                            previousVelocity,
                            previousObstructionNormal,
                            ref transientVelocity,
                            ref remainingMovementMagnitude,
                            ref remainingMovementDirection);

                        previousHitIsStable = stableOnHit;
                        previousVelocity = velocityBeforeProj;
                        previousObstructionNormal = obstructionNormal;
                    }
                }
                // If we hit nothing...
                else
                {
                    hitSomethingThisSweepIteration = false;
                }

                // Safety for exceeding max sweeps allowed
                sweepsMade++;
                if (sweepsMade > MaxMovementIterations)
                {
                    if (KillRemainingMovementWhenExceedMaxMovementIterations)
                    {
                        remainingMovementMagnitude = 0f;
                    }

                    if (KillVelocityWhenExceedMaxMovementIterations)
                    {
                        transientVelocity = Vector3.zero;
                    }
                    wasCompleted = false;
                }
            }

            // Move position for the remainder of the movement
            tmpMovedPosition += (remainingMovementDirection * remainingMovementMagnitude);
            _transientPosition = tmpMovedPosition;

            return wasCompleted;
        }

        /// <summary>
        /// Gets the effective normal for movement obstruction depending on current grounding status
        /// </summary>
        private Vector3 GetObstructionNormal(Vector3 hitNormal, bool stableOnHit)
        {
            // Find hit/obstruction/offset normal
            Vector3 obstructionNormal = hitNormal;
            if (GroundingStatus.IsStableOnGround && !MustUnground() && !stableOnHit)
            {
                Vector3 obstructionLeftAlongGround = Vector3.Cross(GroundingStatus.GroundNormal, obstructionNormal).normalized;
                obstructionNormal = Vector3.Cross(obstructionLeftAlongGround, _characterUp).normalized;
            }

            // Catch cases where cross product between parallel normals returned 0
            if (obstructionNormal.sqrMagnitude == 0f)
            {
                obstructionNormal = hitNormal;
            }

            return obstructionNormal;
        }

        /// <summary>
        /// Remembers a rigidbody hit for processing later
        /// </summary>
        private void StoreRigidbodyHit(Rigidbody hitRigidbody, Vector3 hitVelocity, Vector3 hitPoint, Vector3 obstructionNormal, HitStabilityReport hitStabilityReport)
        {
            if (_rigidbodyProjectionHitCount < _internalRigidbodyProjectionHits.Length)
            {
                if (!hitRigidbody.GetComponent<KinematicCharacterMotor>())
                {
                    RigidbodyProjectionHit rph = new RigidbodyProjectionHit();
                    rph.Rigidbody = hitRigidbody;
                    rph.HitPoint = hitPoint;
                    rph.EffectiveHitNormal = obstructionNormal;
                    rph.HitVelocity = hitVelocity;
                    rph.StableOnHit = hitStabilityReport.IsStable;

                    _internalRigidbodyProjectionHits[_rigidbodyProjectionHitCount] = rph;
                    _rigidbodyProjectionHitCount++;
                }
            }
        }

        public void SetTransientPosition(Vector3 newPos)
        {
            _transientPosition = newPos;
        }

        /// <summary>
        /// Processes movement projection upon detecting a hit
        /// </summary>
        private void InternalHandleVelocityProjection(bool stableOnHit, Vector3 hitNormal, Vector3 obstructionNormal, Vector3 originalDirection,
            ref MovementSweepState sweepState, bool previousHitIsStable, Vector3 previousVelocity, Vector3 previousObstructionNormal,
            ref Vector3 transientVelocity, ref float remainingMovementMagnitude, ref Vector3 remainingMovementDirection)
        {
            if (transientVelocity.sqrMagnitude <= 0f)
            {
                return;
            }

            Vector3 velocityBeforeProjection = transientVelocity;

            if (stableOnHit)
            {
                LastMovementIterationFoundAnyGround = true;
                HandleVelocityProjection(ref transientVelocity, obstructionNormal, stableOnHit);
            }
            else
            {
                // Handle projection
                if (sweepState == MovementSweepState.Initial)
                {
                    HandleVelocityProjection(ref transientVelocity, obstructionNormal, stableOnHit);
                    sweepState = MovementSweepState.AfterFirstHit;
                }
                // Blocking crease handling
                else if (sweepState == MovementSweepState.AfterFirstHit)
                {
                    EvaluateCrease(
                        transientVelocity,
                        previousVelocity,
                        obstructionNormal,
                        previousObstructionNormal,
                        stableOnHit,
                        previousHitIsStable,
                        GroundingStatus.IsStableOnGround && !MustUnground(),
                        out bool foundCrease,
                        out Vector3 creaseDirection);

                    if (foundCrease)
                    {
                        if (GroundingStatus.IsStableOnGround && !MustUnground())
                        {
                            transientVelocity = Vector3.zero;
                            sweepState = MovementSweepState.FoundBlockingCorner;
                        }
                        else
                        {
                            transientVelocity = Vector3.Project(transientVelocity, creaseDirection);
                            sweepState = MovementSweepState.FoundBlockingCrease;
                        }
                    }
                    else
                    {
                        HandleVelocityProjection(ref transientVelocity, obstructionNormal, stableOnHit);
                    }
                }
                // Blocking corner handling
                else if (sweepState == MovementSweepState.FoundBlockingCrease)
                {
                    transientVelocity = Vector3.zero;
                    sweepState = MovementSweepState.FoundBlockingCorner;
                }
            }

            if (HasPlanarConstraint)
            {
                transientVelocity = Vector3.ProjectOnPlane(transientVelocity, PlanarConstraintAxis.normalized);
            }

            float newVelocityFactor = transientVelocity.magnitude / velocityBeforeProjection.magnitude;
            remainingMovementMagnitude *= newVelocityFactor;
            remainingMovementDirection = transientVelocity.normalized;
        }

        private void EvaluateCrease(
            Vector3 currentCharacterVelocity,
            Vector3 previousCharacterVelocity,
            Vector3 currentHitNormal,
            Vector3 previousHitNormal,
            bool currentHitIsStable,
            bool previousHitIsStable,
            bool characterIsStable,
            out bool isValidCrease,
            out Vector3 creaseDirection)
        {
            isValidCrease = false;
            creaseDirection = default;

            if (!characterIsStable || !currentHitIsStable || !previousHitIsStable)
            {
                Vector3 tmpBlockingCreaseDirection = Vector3.Cross(currentHitNormal, previousHitNormal).normalized;
                float dotPlanes = Vector3.Dot(currentHitNormal, previousHitNormal);
                bool isVelocityConstrainedByCrease = false;

                // Avoid calculations if the two planes are the same
                if (dotPlanes < 0.999f)
                {
                    // TODO: can this whole part be made simpler? (with 2d projections, etc)
                    Vector3 normalAOnCreasePlane = Vector3.ProjectOnPlane(currentHitNormal, tmpBlockingCreaseDirection).normalized;
                    Vector3 normalBOnCreasePlane = Vector3.ProjectOnPlane(previousHitNormal, tmpBlockingCreaseDirection).normalized;
                    float dotPlanesOnCreasePlane = Vector3.Dot(normalAOnCreasePlane, normalBOnCreasePlane);

                    Vector3 enteringVelocityDirectionOnCreasePlane = Vector3.ProjectOnPlane(previousCharacterVelocity, tmpBlockingCreaseDirection).normalized;

                    if (dotPlanesOnCreasePlane <= (Vector3.Dot(-enteringVelocityDirectionOnCreasePlane, normalAOnCreasePlane) + 0.001f) &&
                        dotPlanesOnCreasePlane <= (Vector3.Dot(-enteringVelocityDirectionOnCreasePlane, normalBOnCreasePlane) + 0.001f))
                    {
                        isVelocityConstrainedByCrease = true;
                    }
                }

                if (isVelocityConstrainedByCrease)
                {
                    // Flip crease direction to make it representative of the real direction our velocity would be projected to
                    if (Vector3.Dot(tmpBlockingCreaseDirection, currentCharacterVelocity) < 0f)
                    {
                        tmpBlockingCreaseDirection = -tmpBlockingCreaseDirection;
                    }

                    isValidCrease = true;
                    creaseDirection = tmpBlockingCreaseDirection;
                }
            }
        }

        /// <summary>
        /// Allows you to override the way velocity is projected on an obstruction
        /// </summary>
        public virtual void HandleVelocityProjection(ref Vector3 velocity, Vector3 obstructionNormal, bool stableOnHit)
        {
            if (GroundingStatus.IsStableOnGround && !MustUnground())
            {
                // On stable slopes, simply reorient the movement without any loss
                if (stableOnHit)
                {
                    velocity = GetDirectionTangentToSurface(velocity, obstructionNormal) * velocity.magnitude;
                }
                // On blocking hits, project the movement on the obstruction while following the grounding plane
                else
                {
                    Vector3 obstructionRightAlongGround = Vector3.Cross(obstructionNormal, GroundingStatus.GroundNormal).normalized;
                    Vector3 obstructionUpAlongGround = Vector3.Cross(obstructionRightAlongGround, obstructionNormal).normalized;
                    velocity = GetDirectionTangentToSurface(velocity, obstructionUpAlongGround) * velocity.magnitude;
                    velocity = Vector3.ProjectOnPlane(velocity, obstructionNormal);
                }
            }
            else
            {
                if (stableOnHit)
                {
                    // Handle stable landing
                    velocity = Vector3.ProjectOnPlane(velocity, CharacterUp);
                    velocity = GetDirectionTangentToSurface(velocity, obstructionNormal) * velocity.magnitude;
                }
                // Handle generic obstruction
                else
                {
                    velocity = Vector3.ProjectOnPlane(velocity, obstructionNormal);
                }
            }
        }

        /// <summary>
        /// Allows you to override the way hit rigidbodies are pushed / interacted with. 
        /// ProcessedVelocity is what must be modified if this interaction affects the character's velocity.
        /// </summary>
        public virtual void HandleSimulatedRigidbodyInteraction(ref Vector3 processedVelocity, RigidbodyProjectionHit hit, float deltaTime)
        {
        }

        /// <summary>
        /// Takes into account rigidbody hits for adding to the velocity
        /// </summary>
        private void ProcessVelocityForRigidbodyHits(ref Vector3 processedVelocity, float deltaTime)
        {
            for (int i = 0; i < _rigidbodyProjectionHitCount; i++)
            {
                RigidbodyProjectionHit bodyHit = _internalRigidbodyProjectionHits[i];

                if (bodyHit.Rigidbody && !_rigidbodiesPushedThisMove.Contains(bodyHit.Rigidbody))
                {
                    if (_internalRigidbodyProjectionHits[i].Rigidbody != _attachedRigidbody)
                    {
                        // Remember we hit this rigidbody
                        _rigidbodiesPushedThisMove.Add(bodyHit.Rigidbody);

                        float characterMass = SimulatedCharacterMass;
                        Vector3 characterVelocity = bodyHit.HitVelocity;

                        KinematicCharacterMotor hitCharacterMotor = bodyHit.Rigidbody.GetComponent<KinematicCharacterMotor>();
                        bool hitBodyIsCharacter = hitCharacterMotor != null;
                        bool hitBodyIsDynamic = !bodyHit.Rigidbody.isKinematic;
                        float hitBodyMass = bodyHit.Rigidbody.mass;
                        float hitBodyMassAtPoint = bodyHit.Rigidbody.mass; // todo
                        Vector3 hitBodyVelocity = bodyHit.Rigidbody.linearVelocity;
                        if (hitBodyIsCharacter)
                        {
                            hitBodyMass = hitCharacterMotor.SimulatedCharacterMass;
                            hitBodyMassAtPoint = hitCharacterMotor.SimulatedCharacterMass; // todo
                            hitBodyVelocity = hitCharacterMotor.BaseVelocity;
                        }
                        else if (!hitBodyIsDynamic)
                        {
                            PhysicsMover physicsMover = bodyHit.Rigidbody.GetComponent<PhysicsMover>();
                            if (physicsMover)
                            {
                                hitBodyVelocity = physicsMover.Velocity;
                            }
                        }

                        // Calculate the ratio of the total mass that the character mass represents
                        float characterToBodyMassRatio = 1f;
                        {
                            if (characterMass + hitBodyMassAtPoint > 0f)
                            {
                                characterToBodyMassRatio = characterMass / (characterMass + hitBodyMassAtPoint);
                            }
                            else
                            {
                                characterToBodyMassRatio = 0.5f;
                            }

                            // Hitting a non-dynamic body
                            if (!hitBodyIsDynamic)
                            {
                                characterToBodyMassRatio = 0f;
                            }
                            // Emulate kinematic body interaction
                            else if (RigidbodyInteractionType == RigidbodyInteractionType.Kinematic && !hitBodyIsCharacter)
                            {
                                characterToBodyMassRatio = 1f;
                            }
                        }

                        ComputeCollisionResolutionForHitBody(
                            bodyHit.EffectiveHitNormal,
                            characterVelocity,
                            hitBodyVelocity,
                            characterToBodyMassRatio,
                            out Vector3 velocityChangeOnCharacter,
                            out Vector3 velocityChangeOnBody);

                        processedVelocity += velocityChangeOnCharacter;

                        if (hitBodyIsCharacter)
                        {
                            hitCharacterMotor.BaseVelocity += velocityChangeOnCharacter;
                        }
                        else if (hitBodyIsDynamic)
                        {
                            bodyHit.Rigidbody.AddForceAtPosition(velocityChangeOnBody, bodyHit.HitPoint, ForceMode.VelocityChange);
                        }

                        if (RigidbodyInteractionType == RigidbodyInteractionType.SimulatedDynamic)
                        {
                            HandleSimulatedRigidbodyInteraction(ref processedVelocity, bodyHit, deltaTime);
                        }
                    }
                }
            }

        }

        public void ComputeCollisionResolutionForHitBody(
            Vector3 hitNormal,
            Vector3 characterVelocity,
            Vector3 bodyVelocity,
            float characterToBodyMassRatio,
            out Vector3 velocityChangeOnCharacter,
            out Vector3 velocityChangeOnBody)
        {
            velocityChangeOnCharacter = default;
            velocityChangeOnBody = default;

            float bodyToCharacterMassRatio = 1f - characterToBodyMassRatio;
            float characterVelocityMagnitudeOnHitNormal = Vector3.Dot(characterVelocity, hitNormal);
            float bodyVelocityMagnitudeOnHitNormal = Vector3.Dot(bodyVelocity, hitNormal);

            // if character velocity was going against the obstruction, restore the portion of the velocity that got projected during the movement phase
            if (characterVelocityMagnitudeOnHitNormal < 0f)
            {
                Vector3 restoredCharacterVelocity = hitNormal * characterVelocityMagnitudeOnHitNormal;
                velocityChangeOnCharacter += restoredCharacterVelocity;
            }

            // solve impulse velocities on both bodies, but only if the body velocity would be giving resistance to the character in any way
            if (bodyVelocityMagnitudeOnHitNormal > characterVelocityMagnitudeOnHitNormal)
            {
                Vector3 relativeImpactVelocity = hitNormal * (bodyVelocityMagnitudeOnHitNormal - characterVelocityMagnitudeOnHitNormal);
                velocityChangeOnCharacter += relativeImpactVelocity * bodyToCharacterMassRatio;
                velocityChangeOnBody += -relativeImpactVelocity * characterToBodyMassRatio;
            }
        }

        /// <summary>
        /// 确定输入的碰撞器是否适用于碰撞处理
        /// </summary>
        /// <returns> Returns true if the collider is valid </returns>
        private bool CheckIfColliderValidForCollisions(Collider coll)
        {
            // Ignore self
            if (coll == Capsule)
            {
                return false;
            }

            if (!InternalIsColliderValidForCollisions(coll))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断输入的碰撞体是否有效，是否应该参与碰撞处理
        /// 【用途】
        ///     - 在碰撞检测过程中，过滤掉不应该参与碰撞的物体
        ///     - 被 CharacterCollisionsOverlap 和其他碰撞相关方法调用
        /// 【过滤规则】
        ///     1. 如果碰撞体有 Rigidbody 且设置了忽略来自 AttachedRigidbody 的移动，则跳过
        ///     2. 如果角色 RigidbodyInteractionType 为 Kinematic，不与动态 Rigidbody 碰撞
        ///     3. 调用 CharacterController.IsColliderValidForCollisions 进行自定义检查
        /// </summary>
        /// <param name="coll">需要检查的碰撞体</param>
        /// <returns>true=碰撞体有效，参与碰撞处理；false=碰撞体无效，忽略</returns>
        private bool InternalIsColliderValidForCollisions(Collider coll)
        {
            // 获取碰撞体附着的 Rigidbody
            Rigidbody colliderAttachedRigidbody = coll.attachedRigidbody;

            // 如果碰撞体附着了 Rigidbody
            if (colliderAttachedRigidbody)
            {
                // 判断该 Rigidbody 是否是运动学刚体
                bool isRigidbodyKinematic = colliderAttachedRigidbody.isKinematic;

                // 规则1：如果移动来源于 AttachedRigidbody，忽略该 Rigidbody
                // 条件：正在从 AttachedRigidbody 移动 AND (传入碰撞体非运动学刚体 OR 就是 AttachedRigidbody 本身)
                if (_isMovingFromAttachedRigidbody &&
                         (!isRigidbodyKinematic || colliderAttachedRigidbody == _attachedRigidbody))
                {
                    return false; // 无效，跳过
                }

                // 规则2：如果角色的交互类型是 Kinematic，不与动态刚体（Non-Kinematic）碰撞
                if (RigidbodyInteractionType == RigidbodyInteractionType.Kinematic && !isRigidbodyKinematic)
                {
                    // 唤醒动态刚体（让物理引擎处理它）
                    if (coll.attachedRigidbody)
                    {
                        coll.attachedRigidbody.WakeUp();
                    }

                    return false; // 无效，跳过（Kinematic角色不与动态刚体碰撞）
                }
            }

            // 规则3：自定义检查（由 CharacterController 子类实现）
            bool colliderValid = CharacterController.IsColliderValidForCollisions(coll);
            if (!colliderValid)
            {
                return false; // 无效，跳过
            }

            return true; // 碰撞体有效
        }

        /// <summary>
        /// Determines if the motor is considered stable on a given hit
        /// </summary>
        public void EvaluateHitStability(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, Vector3 withCharacterVelocity, ref HitStabilityReport stabilityReport)
        {
            if (!_solveGrounding)
            {
                stabilityReport.IsStable = false;
                return;
            }

            Vector3 atCharacterUp = atCharacterRotation * _cachedWorldUp;
            Vector3 innerHitDirection = Vector3.ProjectOnPlane(hitNormal, atCharacterUp).normalized;

            stabilityReport.IsStable = this.IsStableOnNormal(hitNormal);

            stabilityReport.FoundInnerNormal = false;
            stabilityReport.FoundOuterNormal = false;
            stabilityReport.InnerNormal = hitNormal;
            stabilityReport.OuterNormal = hitNormal;

            // Ledge handling
            if (LedgeAndDenivelationHandling)
            {
                float ledgeCheckHeight = MinDistanceForLedge;
                if (StepHandling != StepHandlingMethod.None)
                {
                    ledgeCheckHeight = MaxStepHeight;
                }

                bool isStableLedgeInner = false;
                bool isStableLedgeOuter = false;

                if (CharacterCollisionsRaycast(
                        hitPoint + (atCharacterUp * SecondaryProbesVertical) + (innerHitDirection * SecondaryProbesHorizontal),
                        -atCharacterUp,
                        ledgeCheckHeight + SecondaryProbesVertical,
                        out RaycastHit innerLedgeHit,
                        _internalCharacterHits) > 0)
                {
                    Vector3 innerLedgeNormal = innerLedgeHit.normal;
                    stabilityReport.InnerNormal = innerLedgeNormal;
                    stabilityReport.FoundInnerNormal = true;
                    isStableLedgeInner = IsStableOnNormal(innerLedgeNormal);
                }

                if (CharacterCollisionsRaycast(
                        hitPoint + (atCharacterUp * SecondaryProbesVertical) + (-innerHitDirection * SecondaryProbesHorizontal),
                        -atCharacterUp,
                        ledgeCheckHeight + SecondaryProbesVertical,
                        out RaycastHit outerLedgeHit,
                        _internalCharacterHits) > 0)
                {
                    Vector3 outerLedgeNormal = outerLedgeHit.normal;
                    stabilityReport.OuterNormal = outerLedgeNormal;
                    stabilityReport.FoundOuterNormal = true;
                    isStableLedgeOuter = IsStableOnNormal(outerLedgeNormal);
                }

                stabilityReport.LedgeDetected = (isStableLedgeInner != isStableLedgeOuter);
                if (stabilityReport.LedgeDetected)
                {
                    stabilityReport.IsOnEmptySideOfLedge = isStableLedgeOuter && !isStableLedgeInner;
                    stabilityReport.LedgeGroundNormal = isStableLedgeOuter ? stabilityReport.OuterNormal : stabilityReport.InnerNormal;
                    stabilityReport.LedgeRightDirection = Vector3.Cross(hitNormal, stabilityReport.LedgeGroundNormal).normalized;
                    stabilityReport.LedgeFacingDirection = Vector3.ProjectOnPlane(Vector3.Cross(stabilityReport.LedgeGroundNormal, stabilityReport.LedgeRightDirection), CharacterUp).normalized;
                    stabilityReport.DistanceFromLedge = Vector3.ProjectOnPlane((hitPoint - (atCharacterPosition + (atCharacterRotation * _characterTransformToCapsuleBottom))), atCharacterUp).magnitude;
                    stabilityReport.IsMovingTowardsEmptySideOfLedge = Vector3.Dot(withCharacterVelocity.normalized, stabilityReport.LedgeFacingDirection) > 0f;
                }

                if (stabilityReport.IsStable)
                {
                    stabilityReport.IsStable = IsStableWithSpecialCases(ref stabilityReport, withCharacterVelocity);
                }
            }

            // Step handling
            if (StepHandling != StepHandlingMethod.None && !stabilityReport.IsStable)
            {
                // Stepping not supported on dynamic rigidbodies
                Rigidbody hitRigidbody = hitCollider.attachedRigidbody;
                if (!(hitRigidbody && !hitRigidbody.isKinematic))
                {
                    DetectSteps(atCharacterPosition, atCharacterRotation, hitPoint, innerHitDirection, ref stabilityReport);

                    if (stabilityReport.ValidStepDetected)
                    {
                        stabilityReport.IsStable = true;
                    }
                }
            }

            CharacterController.ProcessHitStabilityReport(hitCollider, hitNormal, hitPoint, atCharacterPosition, atCharacterRotation, ref stabilityReport);
        }

        private void DetectSteps(Vector3 characterPosition, Quaternion characterRotation, Vector3 hitPoint, Vector3 innerHitDirection, ref HitStabilityReport stabilityReport)
        {
            int nbStepHits = 0;
            Collider tmpCollider;
            RaycastHit outerStepHit;
            Vector3 characterUp = characterRotation * _cachedWorldUp;
            Vector3 verticalCharToHit = Vector3.Project((hitPoint - characterPosition), characterUp);
            Vector3 horizontalCharToHitDirection = Vector3.ProjectOnPlane((hitPoint - characterPosition), characterUp).normalized;
            Vector3 stepCheckStartPos = (hitPoint - verticalCharToHit) + (characterUp * MaxStepHeight) + (horizontalCharToHitDirection * CollisionOffset * 3f);

            // Do outer step check with capsule cast on hit point
            nbStepHits = CharacterCollisionsSweep(
                            stepCheckStartPos,
                            characterRotation,
                            -characterUp,
                            MaxStepHeight + CollisionOffset,
                            out outerStepHit,
                            _internalCharacterHits,
                            0f,
                            true);

            // Check for overlaps and obstructions at the hit position
            if (CheckStepValidity(nbStepHits, characterPosition, characterRotation, innerHitDirection, stepCheckStartPos, out tmpCollider))
            {
                stabilityReport.ValidStepDetected = true;
                stabilityReport.SteppedCollider = tmpCollider;
            }

            if (StepHandling == StepHandlingMethod.Extra && !stabilityReport.ValidStepDetected)
            {
                // Do min reach step check with capsule cast on hit point
                stepCheckStartPos = characterPosition + (characterUp * MaxStepHeight) + (-innerHitDirection * MinRequiredStepDepth);
                nbStepHits = CharacterCollisionsSweep(
                                stepCheckStartPos,
                                characterRotation,
                                -characterUp,
                                MaxStepHeight - CollisionOffset,
                                out outerStepHit,
                                _internalCharacterHits,
                                0f,
                                true);

                // Check for overlaps and obstructions at the hit position
                if (CheckStepValidity(nbStepHits, characterPosition, characterRotation, innerHitDirection, stepCheckStartPos, out tmpCollider))
                {
                    stabilityReport.ValidStepDetected = true;
                    stabilityReport.SteppedCollider = tmpCollider;
                }
            }
        }

        private bool CheckStepValidity(int nbStepHits, Vector3 characterPosition, Quaternion characterRotation, Vector3 innerHitDirection, Vector3 stepCheckStartPos, out Collider hitCollider)
        {
            hitCollider = null;
            Vector3 characterUp = characterRotation * Vector3.up;

            // Find the farthest valid hit for stepping
            bool foundValidStepPosition = false;

            while (nbStepHits > 0 && !foundValidStepPosition)
            {
                // Get farthest hit among the remaining hits
                RaycastHit farthestHit = new RaycastHit();
                float farthestDistance = 0f;
                int farthestIndex = 0;
                for (int i = 0; i < nbStepHits; i++)
                {
                    float hitDistance = _internalCharacterHits[i].distance;
                    if (hitDistance > farthestDistance)
                    {
                        farthestDistance = hitDistance;
                        farthestHit = _internalCharacterHits[i];
                        farthestIndex = i;
                    }
                }

                Vector3 characterPositionAtHit = stepCheckStartPos + (-characterUp * (farthestHit.distance - CollisionOffset));

                int atStepOverlaps = CharacterCollisionsOverlap(characterPositionAtHit, characterRotation, _internalProbedColliders);
                if (atStepOverlaps <= 0)
                {
                    // Check for outer hit slope normal stability at the step position
                    if (CharacterCollisionsRaycast(
                            farthestHit.point + (characterUp * SecondaryProbesVertical) + (-innerHitDirection * SecondaryProbesHorizontal),
                            -characterUp,
                            MaxStepHeight + SecondaryProbesVertical,
                            out RaycastHit outerSlopeHit,
                            _internalCharacterHits,
                            true) > 0)
                    {
                        if (IsStableOnNormal(outerSlopeHit.normal))
                        {
                            // Cast upward to detect any obstructions to moving there
                            if (CharacterCollisionsSweep(
                                                characterPosition, // position
                                                characterRotation, // rotation
                                                characterUp, // direction
                                                MaxStepHeight - farthestHit.distance, // distance
                                                out RaycastHit tmpUpObstructionHit, // closest hit
                                                _internalCharacterHits) // all hits
                                    <= 0)
                            {
                                // Do inner step check...
                                bool innerStepValid = false;
                                RaycastHit innerStepHit;

                                if (AllowSteppingWithoutStableGrounding)
                                {
                                    innerStepValid = true;
                                }
                                else
                                {
                                    // At the capsule center at the step height
                                    if (CharacterCollisionsRaycast(
                                            characterPosition + Vector3.Project((characterPositionAtHit - characterPosition), characterUp),
                                            -characterUp,
                                            MaxStepHeight,
                                            out innerStepHit,
                                            _internalCharacterHits,
                                            true) > 0)
                                    {
                                        if (IsStableOnNormal(innerStepHit.normal))
                                        {
                                            innerStepValid = true;
                                        }
                                    }
                                }

                                if (!innerStepValid)
                                {
                                    // At inner step of the step point
                                    if (CharacterCollisionsRaycast(
                                            farthestHit.point + (innerHitDirection * SecondaryProbesHorizontal),
                                            -characterUp,
                                            MaxStepHeight,
                                            out innerStepHit,
                                            _internalCharacterHits,
                                            true) > 0)
                                    {
                                        if (IsStableOnNormal(innerStepHit.normal))
                                        {
                                            innerStepValid = true;
                                        }
                                    }
                                }

                                // Final validation of step
                                if (innerStepValid)
                                {
                                    hitCollider = farthestHit.collider;
                                    foundValidStepPosition = true;
                                    return true;
                                }
                            }
                        }
                    }
                }

                // Discard hit if not valid step
                if (!foundValidStepPosition)
                {
                    nbStepHits--;
                    if (farthestIndex < nbStepHits)
                    {
                        _internalCharacterHits[farthestIndex] = _internalCharacterHits[nbStepHits];
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get true linear velocity (taking into account rotational velocity) on a given point of a rigidbody
        /// </summary>
        public void GetVelocityFromRigidbodyMovement(Rigidbody interactiveRigidbody, Vector3 atPoint, float deltaTime, out Vector3 linearVelocity, out Vector3 angularVelocity)
        {
            if (deltaTime > 0f)
            {
                linearVelocity = interactiveRigidbody.linearVelocity;
                angularVelocity = interactiveRigidbody.angularVelocity;
                if (interactiveRigidbody.isKinematic)
                {
                    PhysicsMover physicsMover = interactiveRigidbody.GetComponent<PhysicsMover>();
                    if (physicsMover)
                    {
                        linearVelocity = physicsMover.Velocity;
                        angularVelocity = physicsMover.AngularVelocity;
                    }
                }

                if (angularVelocity != Vector3.zero)
                {
                    Vector3 centerOfRotation = interactiveRigidbody.transform.TransformPoint(interactiveRigidbody.centerOfMass);

                    Vector3 centerOfRotationToPoint = atPoint - centerOfRotation;
                    Quaternion rotationFromInteractiveRigidbody = Quaternion.Euler(Mathf.Rad2Deg * angularVelocity * deltaTime);
                    Vector3 finalPointPosition = centerOfRotation + (rotationFromInteractiveRigidbody * centerOfRotationToPoint);
                    linearVelocity += (finalPointPosition - atPoint) / deltaTime;
                }
            }
            else
            {
                linearVelocity = default;
                angularVelocity = default;
                return;
            }
        }

        /// <summary>
        /// Determines if a collider has an attached interactive rigidbody
        /// </summary>
        private Rigidbody GetInteractiveRigidbody(Collider onCollider)
        {
            Rigidbody colliderAttachedRigidbody = onCollider.attachedRigidbody;
            if (colliderAttachedRigidbody)
            {
                if (colliderAttachedRigidbody.gameObject.GetComponent<PhysicsMover>())
                {
                    return colliderAttachedRigidbody;
                }

                if (!colliderAttachedRigidbody.isKinematic)
                {
                    return colliderAttachedRigidbody;
                }
            }
            return null;
        }

        /// <summary>
        /// Calculates the velocity required to move the character to the target position over a specific deltaTime.
        /// Useful for when you wish to work with positions rather than velocities in the UpdateVelocity callback 
        /// </summary>
        public Vector3 GetVelocityForMovePosition(Vector3 fromPosition, Vector3 toPosition, float deltaTime)
        {
            return GetVelocityFromMovement(toPosition - fromPosition, deltaTime);
        }

        public Vector3 GetVelocityFromMovement(Vector3 movement, float deltaTime)
        {
            if (deltaTime <= 0f)
                return Vector3.zero;

            return movement / deltaTime;
        }

        /// <summary>
        /// Trims a vector to make it restricted against a plane 
        /// </summary>
        private void RestrictVectorToPlane(ref Vector3 vector, Vector3 toPlane)
        {
            if (vector.x > 0 != toPlane.x > 0)
            {
                vector.x = 0;
            }
            if (vector.y > 0 != toPlane.y > 0)
            {
                vector.y = 0;
            }
            if (vector.z > 0 != toPlane.z > 0)
            {
                vector.z = 0;
            }
        }

        /// <summary>
        /// 检测角色胶囊体是否与可碰撞物体发生重叠
        /// 【用途】
        ///     - 检测胶囊体形状的空间内是否有其他碰撞体
        ///     - 不产生物理响应（只检测，不推开物体）
        ///     - 常用于：解重叠处理、阻挡检测、下蹲/起身判断等
        /// 【原理】
        ///     - 使用 Physics.OverlapCapsuleNonAlloc 进行重叠检测
        ///     - 内部使用 BottomHemi 和 TopHemi 作为胶囊体端点
        ///     - 自动过滤无效的碰撞体（如 Trigger 或不同层的物体）
        /// </summary>
        /// <param name="position">世界坐标系中的角色位置</param>
        /// <param name="rotation">世界坐标系中的角色旋转</param>
        /// <param name="overlappedColliders">用于存储检测到的重叠碰撞体的数组（预先分配）</param>
        /// <param name="inflate">可选参数：膨胀检测范围（用于更宽松的检测）</param>
        /// <param name="acceptOnlyStableGroundLayer">可选参数：是否只检测"稳定地面层"的物体</param>
        /// <returns>返回检测到的有效重叠物体数量</returns>
        public int CharacterCollisionsOverlap(Vector3 position, Quaternion rotation, 
            Collider[] overlappedColliders, float inflate = 0f, bool acceptOnlyStableGroundLayer = false)
        {
            // 确定检测层：根据参数决定是检测所有可碰撞层，还是只检测稳定地面层
            int queryLayers = CollidableLayers;
            if (acceptOnlyStableGroundLayer)
            {
                // 稳定地面层 = 可碰撞层 & 稳定地面层
                queryLayers = CollidableLayers & StableGroundLayers;
            }

            // 计算胶囊体端点的世界坐标
            // 使用 BottomHemi 和 TopHemi（半球与圆柱交界处）作为胶囊体端点
            Vector3 bottom = position + (rotation * _characterTransformToCapsuleBottomHemi);
            Vector3 top = position + (rotation * _characterTransformToCapsuleTopHemi);

            // 如果指定了膨胀值，扩大胶囊体检测范围
            if (inflate != 0f)
            {
                // 底部向下膨胀，顶部向上膨胀
                bottom += (rotation * Vector3.down * inflate);
                top += (rotation * Vector3.up * inflate);
            }

            // 执行重叠检测（使用 NonAlloc 版本避免 GC）
            // 参数说明：
            //   - bottom/top: 胶囊体的两个端点
            //   - Capsule.radius + inflate: 胶囊体半径（可额外膨胀）
            //   - overlappedColliders: 输出数组，存储检测到的碰撞体
            //   - queryLayers: 检测哪些层的物体
            //   - QueryTriggerInteraction.Ignore: 忽略 Trigger 类型的碰撞体
            int nbHits = 0;
            int nbUnfilteredHits = Physics.OverlapCapsuleNonAlloc(
                        bottom,
                        top,
                        Capsule.radius + inflate,
                        overlappedColliders,
                        queryLayers,
                        QueryTriggerInteraction.Ignore);

            // 过滤无效的碰撞体
            // 无效碰撞体包括：已被销毁的、不在可碰撞层中的、Trigger 等
            nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                // 检查碰撞体是否有效
                if (!CheckIfColliderValidForCollisions(overlappedColliders[i]))
                {
                    nbHits--;
                    // 将有效的碰撞体前移，覆盖无效的位置
                    if (i < nbHits)
                    {
                        overlappedColliders[i] = overlappedColliders[nbHits];
                    }
                }
            }

            // 返回有效重叠物体的数量
            return nbHits;
        }

        /// <summary>
        /// Detect if the character capsule is overlapping with anything
        /// </summary>
        /// <returns> Returns number of overlaps </returns>
        public int CharacterOverlap(Vector3 position, Quaternion rotation, Collider[] overlappedColliders, LayerMask layers, QueryTriggerInteraction triggerInteraction, float inflate = 0f)
        {
            Vector3 bottom = position + (rotation * _characterTransformToCapsuleBottomHemi);
            Vector3 top = position + (rotation * _characterTransformToCapsuleTopHemi);
            if (inflate != 0f)
            {
                bottom += (rotation * Vector3.down * inflate);
                top += (rotation * Vector3.up * inflate);
            }

            int nbHits = 0;
            int nbUnfilteredHits = Physics.OverlapCapsuleNonAlloc(
                        bottom,
                        top,
                        Capsule.radius + inflate,
                        overlappedColliders,
                        layers,
                        triggerInteraction);

            // Filter out the character capsule itself
            nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                if (overlappedColliders[i] == Capsule)
                {
                    nbHits--;
                    if (i < nbHits)
                    {
                        overlappedColliders[i] = overlappedColliders[nbHits];
                    }
                }
            }

            return nbHits;
        }

        /// <summary>
        /// Sweeps the capsule's volume to detect collision hits
        /// </summary>
        /// <returns> Returns the number of hits </returns>
        public int CharacterCollisionsSweep(Vector3 position, Quaternion rotation, Vector3 direction, float distance, out RaycastHit closestHit, RaycastHit[] hits, float inflate = 0f, bool acceptOnlyStableGroundLayer = false)
        {
            int queryLayers = CollidableLayers;
            if (acceptOnlyStableGroundLayer)
            {
                queryLayers = CollidableLayers & StableGroundLayers;
            }

            Vector3 bottom = position + (rotation * _characterTransformToCapsuleBottomHemi) - (direction * SweepProbingBackstepDistance);
            Vector3 top = position + (rotation * _characterTransformToCapsuleTopHemi) - (direction * SweepProbingBackstepDistance);
            if (inflate != 0f)
            {
                bottom += (rotation * Vector3.down * inflate);
                top += (rotation * Vector3.up * inflate);
            }

            // Capsule cast
            int nbHits = 0;
            int nbUnfilteredHits = Physics.CapsuleCastNonAlloc(
                    bottom,
                    top,
                    Capsule.radius + inflate,
                    direction,
                    hits,
                    distance + SweepProbingBackstepDistance,
                    queryLayers,
                    QueryTriggerInteraction.Ignore);

            // Hits filter
            closestHit = new RaycastHit();
            float closestDistance = Mathf.Infinity;
            nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                hits[i].distance -= SweepProbingBackstepDistance;

                RaycastHit hit = hits[i];
                float hitDistance = hit.distance;

                // Filter out the invalid hits
                if (hitDistance <= 0f || !CheckIfColliderValidForCollisions(hit.collider))
                {
                    nbHits--;
                    if (i < nbHits)
                    {
                        hits[i] = hits[nbHits];
                    }
                }
                else
                {
                    // Remember closest valid hit
                    if (hitDistance < closestDistance)
                    {
                        closestHit = hit;
                        closestDistance = hitDistance;
                    }
                }
            }

            return nbHits;
        }

        /// <summary>
        /// Sweeps the capsule's volume to detect hits
        /// </summary>
        /// <returns> Returns the number of hits </returns>
        public int CharacterSweep(Vector3 position, Quaternion rotation, Vector3 direction, float distance, out RaycastHit closestHit, RaycastHit[] hits, LayerMask layers, QueryTriggerInteraction triggerInteraction, float inflate = 0f)
        {
            closestHit = new RaycastHit();

            Vector3 bottom = position + (rotation * _characterTransformToCapsuleBottomHemi);
            Vector3 top = position + (rotation * _characterTransformToCapsuleTopHemi);
            if (inflate != 0f)
            {
                bottom += (rotation * Vector3.down * inflate);
                top += (rotation * Vector3.up * inflate);
            }

            // Capsule cast
            int nbHits = 0;
            int nbUnfilteredHits = Physics.CapsuleCastNonAlloc(
                bottom,
                top,
                Capsule.radius + inflate,
                direction,
                hits,
                distance,
                layers,
                triggerInteraction);

            // Hits filter
            float closestDistance = Mathf.Infinity;
            nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                RaycastHit hit = hits[i];

                // Filter out the character capsule
                if (hit.distance <= 0f || hit.collider == Capsule)
                {
                    nbHits--;
                    if (i < nbHits)
                    {
                        hits[i] = hits[nbHits];
                    }
                }
                else
                {
                    // Remember closest valid hit
                    float hitDistance = hit.distance;
                    if (hitDistance < closestDistance)
                    {
                        closestHit = hit;
                        closestDistance = hitDistance;
                    }
                }
            }

            return nbHits;
        }

        /// <summary>
        /// Casts the character volume in the character's downward direction to detect ground
        /// </summary>
        /// <returns> Returns the number of hits </returns>
        private bool CharacterGroundSweep(Vector3 position, Quaternion rotation, Vector3 direction, float distance, out RaycastHit closestHit)
        {
            closestHit = new RaycastHit();

            // Capsule cast
            int nbUnfilteredHits = Physics.CapsuleCastNonAlloc(
                position + (rotation * _characterTransformToCapsuleBottomHemi) - (direction * GroundProbingBackstepDistance),
                position + (rotation * _characterTransformToCapsuleTopHemi) - (direction * GroundProbingBackstepDistance),
                Capsule.radius,
                direction,
                _internalCharacterHits,
                distance + GroundProbingBackstepDistance,
                CollidableLayers & StableGroundLayers,
                QueryTriggerInteraction.Ignore);

            // Hits filter
            bool foundValidHit = false;
            float closestDistance = Mathf.Infinity;
            for (int i = 0; i < nbUnfilteredHits; i++)
            {
                RaycastHit hit = _internalCharacterHits[i];
                float hitDistance = hit.distance;

                // Find the closest valid hit
                if (hitDistance > 0f && CheckIfColliderValidForCollisions(hit.collider))
                {
                    if (hitDistance < closestDistance)
                    {
                        closestHit = hit;
                        closestHit.distance -= GroundProbingBackstepDistance;
                        closestDistance = hitDistance;

                        foundValidHit = true;
                    }
                }
            }

            return foundValidHit;
        }

        /// <summary>
        /// Raycasts to detect collision hits
        /// </summary>
        /// <returns> Returns the number of hits </returns>
        public int CharacterCollisionsRaycast(Vector3 position, Vector3 direction, float distance, out RaycastHit closestHit, RaycastHit[] hits, bool acceptOnlyStableGroundLayer = false)
        {
            int queryLayers = CollidableLayers;
            if (acceptOnlyStableGroundLayer)
            {
                queryLayers = CollidableLayers & StableGroundLayers;
            }

            // Raycast
            int nbHits = 0;
            int nbUnfilteredHits = Physics.RaycastNonAlloc(
                position,
                direction,
                hits,
                distance,
                queryLayers,
                QueryTriggerInteraction.Ignore);

            // Hits filter
            closestHit = new RaycastHit();
            float closestDistance = Mathf.Infinity;
            nbHits = nbUnfilteredHits;
            for (int i = nbUnfilteredHits - 1; i >= 0; i--)
            {
                RaycastHit hit = hits[i];
                float hitDistance = hit.distance;

                // Filter out the invalid hits
                if (hitDistance <= 0f ||
                    !CheckIfColliderValidForCollisions(hit.collider))
                {
                    nbHits--;
                    if (i < nbHits)
                    {
                        hits[i] = hits[nbHits];
                    }
                }
                else
                {
                    // Remember closest valid hit
                    if (hitDistance < closestDistance)
                    {
                        closestHit = hit;
                        closestDistance = hitDistance;
                    }
                }
            }

            return nbHits;
        }
    }
}