using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KinematicCharacterController
{
    /// <summary>
    /// 表示物理移动器(PhysicsMover)中与模拟相关的完整状态
    /// 可用于保存状态或恢复到历史状态
    /// </summary>
    [System.Serializable]
    public struct PhysicsMoverState
    {
        public Vector3 Position; // 位置
        public Quaternion Rotation; // 旋转
        public Vector3 Velocity; // 移动速度
        public Vector3 AngularVelocity; // 角速度
    }

    /// <summary>
    /// PhysicsMover是 KCC 框架中管理移动平台的核心组件，通过运动学刚体实现平台移动，并保证与角色的物理交互正确性；
    /// 用于管理运动学刚体移动的组件
    /// 确保移动平台与角色之间的正确物理交互
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsMover : MonoBehaviour
    {
        /// <summary>
        /// 移动器关联的刚体组件
        /// </summary>
        [ReadOnly]
        public Rigidbody Rigidbody;

        /// <summary>
        /// 移动方式开关：
        /// true = 使用rigidbody.MovePosition（物理驱动移动，带碰撞检测）
        /// false = 使用rigidbody.position（直接赋值位置，无物理检测）
        /// </summary>
        public bool MoveWithPhysics = true;

        /// <summary>
        /// 移动器控制器（由外部实现IMoverController的脚本赋值）
        /// </summary>
        [NonSerialized]
        public IMoverController MoverController;
        /// <summary>
        /// 插值过程中记录的最新位置
        /// </summary>
        [NonSerialized]
        public Vector3 LatestInterpolationPosition;
        /// <summary>
        /// 插值过程中记录的最新旋转
        /// </summary>
        [NonSerialized]
        public Quaternion LatestInterpolationRotation;
        /// <summary>
        /// 插值产生的最新位置增量
        /// </summary>
        [NonSerialized]
        public Vector3 PositionDeltaFromInterpolation;
        /// <summary>
        /// 插值产生的最新旋转增量
        /// </summary>
        [NonSerialized]
        public Quaternion RotationDeltaFromInterpolation;

        /// <summary>
        /// 该移动器在KinematicCharacterSystem数组中的索引
        /// </summary>
        public int IndexInCharacterSystem { get; set; }
        /// <summary>
        /// 移动器的移动速度（每帧更新）
        /// </summary>
        public Vector3 Velocity { get; protected set; }
        /// <summary>
        /// 移动器的角速度（每帧更新）
        /// </summary>
        public Vector3 AngularVelocity { get; protected set; }
        /// <summary>
        /// 记录物理帧开始前的初始位置
        /// </summary>
        public Vector3 InitialTickPosition { get; set; }
        /// <summary>
        /// 记录物理帧开始前的初始旋转
        /// </summary>
        public Quaternion InitialTickRotation { get; set; }

        /// <summary>
        /// 移动器的Transform组件（缓存）
        /// </summary>
        public Transform Transform { get; private set; }
        /// <summary>
        /// 移动计算开始前的初始位置
        /// </summary>
        public Vector3 InitialSimulationPosition { get; private set; }
        /// <summary>
        /// 移动计算开始前的初始旋转
        /// </summary>
        public Quaternion InitialSimulationRotation { get; private set; }

        private Vector3 _internalTransientPosition;

        /// <summary>
        /// 移动器的瞬态位置（角色更新阶段始终保持最新）
        /// </summary>
        public Vector3 TransientPosition
        {
            get
            {
                return _internalTransientPosition;
            }
            private set
            {
                _internalTransientPosition = value;
            }
        }

        private Quaternion _internalTransientRotation;
        /// <summary>
        /// 移动器的瞬态旋转（角色更新阶段始终保持最新）
        /// </summary>
        public Quaternion TransientRotation
        {
            get
            {
                return _internalTransientRotation;
            }
            private set
            {
                _internalTransientRotation = value;
            }
        }


        private void Reset()
        {
            ValidateData();
        }

        private void OnValidate()
        {
            ValidateData();
        }

        /// <summary>
        /// 验证并初始化所有必要的参数
        /// </summary>
        public void ValidateData()
        {
            Rigidbody = gameObject.GetComponent<Rigidbody>();

            Rigidbody.centerOfMass = Vector3.zero; // 重置质心到原点
            Rigidbody.maxAngularVelocity = Mathf.Infinity; // 取消角速度上限
            Rigidbody.maxDepenetrationVelocity = Mathf.Infinity; // 取消解穿透速度上限
            Rigidbody.isKinematic = true; // 设置为运动学刚体（不受物理力影响）
            Rigidbody.interpolation = RigidbodyInterpolation.None; // 关闭刚体插值（由系统自行处理）
        }

        private void OnEnable()
        {
            KinematicCharacterSystem.EnsureCreation(); // 确保角色系统已初始化
            KinematicCharacterSystem.RegisterPhysicsMover(this); // 注册到角色系统
        }

        private void OnDisable()
        {
            KinematicCharacterSystem.UnregisterPhysicsMover(this); // 从角色系统注销
        }

        private void Awake()
        {
            Transform = this.transform;
            ValidateData();

            TransientPosition = Rigidbody.position;
            TransientRotation = Rigidbody.rotation;
            InitialSimulationPosition = Rigidbody.position;
            InitialSimulationRotation = Rigidbody.rotation;
            LatestInterpolationPosition = Transform.position;
            LatestInterpolationRotation = Transform.rotation;
        }

        /// <summary>
        /// 直接设置移动器的位置
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            Transform.position = position;
            Rigidbody.position = position;
            InitialSimulationPosition = position;
            TransientPosition = position;
        }

        /// <summary>
        /// 直接设置移动器的旋转
        /// </summary>
        public void SetRotation(Quaternion rotation)
        {
            Transform.rotation = rotation;
            Rigidbody.rotation = rotation;
            InitialSimulationRotation = rotation;
            TransientRotation = rotation;
        }

        /// <summary>
        /// 直接设置移动器的位置和旋转
        /// </summary>
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            Transform.SetPositionAndRotation(position, rotation);
            Rigidbody.position = position;
            Rigidbody.rotation = rotation;
            InitialSimulationPosition = position;
            InitialSimulationRotation = rotation;
            TransientPosition = position;
            TransientRotation = rotation;
        }

        /// <summary>
        /// 获取移动器当前的完整模拟状态
        /// </summary>
        public PhysicsMoverState GetState()
        {
            PhysicsMoverState state = new PhysicsMoverState();

            state.Position = TransientPosition;
            state.Rotation = TransientRotation;
            state.Velocity = Velocity;
            state.AngularVelocity = AngularVelocity;

            return state;
        }

        /// <summary>
        /// 立即应用指定的移动器状态
        /// </summary>
        public void ApplyState(PhysicsMoverState state)
        {
            SetPositionAndRotation(state.Position, state.Rotation);
            Velocity = state.Velocity;
            AngularVelocity = state.AngularVelocity;
        }

        /// <summary>
        /// 根据帧时间和目标位姿计算并缓存速度/角速度
        /// </summary>
        public void VelocityUpdate(float deltaTime)
        {
            //记录动画计算前的初始位置
            InitialSimulationPosition = TransientPosition;
            InitialSimulationRotation = TransientRotation;

            // 通过控制器获取目标位置和旋转
            MoverController.UpdateMovement(out _internalTransientPosition, out _internalTransientRotation, deltaTime);
            // ↑ 这里 MyMovingPlatform 返回 B 点（虽然 Transform 实际在 A）
            if (deltaTime > 0f)
            {
                // 计算移动速度 (B-A)/deltaTime
                Velocity = (TransientPosition - InitialSimulationPosition) / deltaTime;
                                
                // 计算角速度
                Quaternion rotationFromCurrentToGoal = TransientRotation * 
                                                (Quaternion.Inverse(InitialSimulationRotation));

                AngularVelocity = (Mathf.Deg2Rad * rotationFromCurrentToGoal.eulerAngles) / deltaTime;
            }
        }
    }
}