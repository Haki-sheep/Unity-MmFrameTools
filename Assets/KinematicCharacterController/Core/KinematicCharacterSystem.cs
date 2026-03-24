using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace KinematicCharacterController
{
    /// <summary>
    /// KCC 核心系统管理器
    /// 
    /// 职责：
    /// 1. 管理所有 KinematicCharacterMotor（角色电机）的注册和模拟
    /// 2. 管理所有 PhysicsMover（物理移动器）的注册和模拟
    /// 3. 处理模拟循环（FixedUpdate）和插值（LateUpdate）
    /// 4. 确保多个角色和移动器之间的正确交互顺序
    /// 
    /// Kcc: 这是一个单例系统，在场景中自动创建，通常命名为"KinematicCharacterSystem"
    /// Kcc: 执行顺序为 -100，确保在其他角色的 Update 之前运行
    /// </summary>
    [DefaultExecutionOrder(-100)]  // Kcc: 确保在所有其他脚本之前运行
    public class KinematicCharacterSystem : MonoBehaviour
    {
        // ============== 静态单例和列表 ==============
        
        /// <summary>
        /// 系统单例实例
        /// Kcc: 用于全局访问系统功能
        /// </summary>
        private static KinematicCharacterSystem _instance;

        /// <summary>
        /// 所有已注册的角色电机列表
        /// Kcc: 在 Simulate 中遍历此列表更新所有角色
        /// </summary>
        public static List<KinematicCharacterMotor> CharacterMotors = new List<KinematicCharacterMotor>();

        /// <summary>
        /// 所有已注册的物理移动器列表
        /// Kcc: 移动平台需要先更新速度，再与角色交互
        /// </summary>
        public static List<PhysicsMover> PhysicsMovers = new List<PhysicsMover>();

        // 插值相关变量
        private static float _lastCustomInterpolationStartTime = -1f;
        private static float _lastCustomInterpolationDeltaTime = -1f;

        /// <summary>
        /// KCC 全局设置
        /// Kcc: 可以在 Unity 编辑器中配置（通过创建 KCCSettings 资源）
        /// </summary>
        public static KCCSettings Settings;

        // ============== 初始化方法 ==============

        /// <summary>
        /// 确保系统已创建，如果没有则创建单例实例
        /// 
        /// Kcc: 这是系统的主要入口点
        /// Kcc: 通常由 KinematicCharacterMotor 或 PhysicsMover 在 Awake 中调用
        /// </summary>
        public static void EnsureCreation()
        {
            if (_instance == null)
            {
                // 创建系统游戏对象
                GameObject systemGameObject = new GameObject("KinematicCharacterSystem");
                _instance = systemGameObject.AddComponent<KinematicCharacterSystem>();

                // 隐藏系统对象，防止用户在编辑器中误操作
                systemGameObject.hideFlags = HideFlags.NotEditable;
                _instance.hideFlags = HideFlags.NotEditable;

                // 创建设置实例
                Settings = ScriptableObject.CreateInstance<KCCSettings>();

                // 跨场景保持
                GameObject.DontDestroyOnLoad(systemGameObject);
            }
        }

        /// <summary>
        /// 获取系统单例实例
        /// </summary>
        /// <returns>系统实例，如果不存在返回 null</returns>
        public static KinematicCharacterSystem GetInstance()
        {
            return _instance;
        }

        // ============== 角色电机注册方法 ==============

        /// <summary>
        /// 设置角色电机列表的最大容量
        /// Kcc: 预先设置容量可以避免动态扩容时的内存分配
        /// </summary>
        /// <param name="capacity">目标容量</param>
        public static void SetCharacterMotorsCapacity(int capacity)
        {
            if (capacity < CharacterMotors.Count)
            {
                capacity = CharacterMotors.Count;
            }
            CharacterMotors.Capacity = capacity;
        }

        /// <summary>
        /// 注册角色电机到系统
        /// Kcc: 由 KinematicCharacterMotor.Awake 自动调用
        /// Kcc: 注册后电机将在 Simulate 中被更新
        /// </summary>
        /// <param name="motor">要注册的角色电机</param>
        public static void RegisterCharacterMotor(KinematicCharacterMotor motor)
        {
            CharacterMotors.Add(motor);
        }

        /// <summary>
        /// 从系统注销角色电机
        /// Kcc: 由 KinematicCharacterMotor.OnDestroy 自动调用
        /// </summary>
        /// <param name="motor">要注销的角色电机</param>
        public static void UnregisterCharacterMotor(KinematicCharacterMotor motor)
        {
            CharacterMotors.Remove(motor);
        }

        // ============== 物理移动器注册方法 ==============

        /// <summary>
        /// 设置物理移动器列表的最大容量
        /// Kcc: 预先设置容量可以避免动态扩容时的内存分配
        /// </summary>
        /// <param name="capacity">目标容量</param>
        public static void SetPhysicsMoversCapacity(int capacity)
        {
            if (capacity < PhysicsMovers.Count)
            {
                capacity = PhysicsMovers.Count;
            }
            PhysicsMovers.Capacity = capacity;
        }

        /// <summary>
        /// 注册物理移动器到系统
        /// Kcc: 由 PhysicsMover.Awake 自动调用
        /// Kcc: 注册后移动器将在 Simulate 中被更新
        /// 
        /// Kcc: 关键步骤：将移动器的刚体插值设置为 None
        /// Kcc: 这是因为系统自己处理插值，不需要 Unity 的默认插值
        /// </summary>
        /// <param name="mover">要注册的物理移动器</param>
        public static void RegisterPhysicsMover(PhysicsMover mover)
        {
            PhysicsMovers.Add(mover);

            // Kcc: 禁用 Unity 的刚体插值，由系统接管
            mover.Rigidbody.interpolation = RigidbodyInterpolation.None;
        }

        /// <summary>
        /// 从系统注销物理移动器
        /// Kcc: 由 PhysicsMover.OnDestroy 自动调用
        /// </summary>
        /// <param name="mover">要注销的物理移动器</param>
        public static void UnregisterPhysicsMover(PhysicsMover mover)
        {
            PhysicsMovers.Remove(mover);
        }

        // ============== Unity 生命周期方法 ==============

        /// <summary>
        /// 防止脚本重新编译时重复创建单例
        /// Kcc: 当脚本被重新编译时，OnDisable 会销毁系统对象
        /// </summary>
        private void OnDisable()
        {
            Destroy(this.gameObject);
        }

        /// <summary>
        /// 唤醒方法
        /// Kcc: 初始化单例引用
        /// </summary>
        private void Awake()
        {
            _instance = this;
        }

        /// <summary>
        /// 固定更新 - 核心模拟循环
        /// 
        /// Kcc: 使用 FixedUpdate 而不是 Update，确保与物理系统同步
        /// Kcc: 执行顺序：
        ///     1. PreSimulationInterpolationUpdate - 记录初始位置
        ///     2. Simulate - 执行角色和移动器的模拟
        ///     3. PostSimulationInterpolationUpdate - 准备插值
        /// 
        /// Kcc: 仅当 AutoSimulation 为 true 时执行
        /// </summary>
        private void FixedUpdate()
        {
            if (Settings.AutoSimulation)
            {
                float deltaTime = Time.deltaTime;

                // 插值预处理：记录初始位置
                if (Settings.Interpolate)
                {
                    PreSimulationInterpolationUpdate(deltaTime);
                }

                // 核心模拟：更新所有角色和移动器
                Simulate(deltaTime, CharacterMotors, PhysicsMovers);

                // 插值后处理：设置目标位置
                if (Settings.Interpolate)
                {
                    PostSimulationInterpolationUpdate(deltaTime);
                }
            }
        }

        /// <summary>
        /// 延迟更新 - 处理帧间插值
        /// 
        /// Kcc: 在所有 FixedUpdate 完成后执行
        /// Kcc: 执行从上一帧位置到当前位置的平滑插值
        /// </summary>
        private void LateUpdate()
        {
            if (Settings.Interpolate)
            {
                CustomInterpolationUpdate();
            }
        }

        // ============== 插值预处理方法 ==============

        /// <summary>
        /// 模拟前插值更新
        /// 
        /// Kcc: 记录每个角色和移动器的瞬时位置/旋转
        /// Kcc: 这些值将成为插值的起点
        /// Kcc: 同时将 Transform 设置为瞬时位置，准备开始模拟
        /// </summary>
        /// <param name="deltaTime">帧时间间隔</param>
        public static void PreSimulationInterpolationUpdate(float deltaTime)
        {
            // Kcc: 处理所有角色电机的初始位置记录
            for (int i = 0; i < CharacterMotors.Count; i++)
            {
                KinematicCharacterMotor motor = CharacterMotors[i];

                // 记录初始位置和旋转（用于插值）
                motor.InitialTickPosition = motor.TransientPosition;
                motor.InitialTickRotation = motor.TransientRotation;

                // Kcc: 将 Transform 设置为瞬时位置，开始模拟
                motor.Transform.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation);
            }

            // Kcc: 处理所有物理移动器的初始位置记录
            for (int i = 0; i < PhysicsMovers.Count; i++)
            {
                PhysicsMover mover = PhysicsMovers[i];

                // 记录初始位置和旋转
                mover.InitialTickPosition = mover.TransientPosition;
                mover.InitialTickRotation = mover.TransientRotation;

                // 设置 Transform 和 Rigidbody
                mover.Transform.SetPositionAndRotation(mover.TransientPosition, mover.TransientRotation);
                mover.Rigidbody.position = mover.TransientPosition;
                mover.Rigidbody.rotation = mover.TransientRotation;
            }
        }

        // ============== 核心模拟方法 ==============

        /// <summary>
        /// 执行模拟 - KCC 的核心逻辑
        /// 
        /// Kcc: 重要执行顺序（确保正确的交互）：
        ///     1. PhysicsMover 速度更新（获取移动器的新速度）
        ///     2. 角色 Phase 1 更新（地面探测、预处理）
        ///     3. PhysicsMover 移动（应用移动器位移）
        ///     4. 角色 Phase 2 更新（旋转、速度求解、移动）
        /// 
        /// Kcc: 这个顺序确保：
        ///     - 移动器先更新速度，角色可以检测到移动的地面
        ///     - 角色在移动器移动后才计算最终位置
        ///     - 避免角色穿模或抖动
        /// </summary>
        /// <param name="deltaTime">帧时间间隔</param>
        /// <param name="motors">要模拟的角色电机列表</param>
        /// <param name="movers">要模拟的物理移动器列表</param>
        public static void Simulate(float deltaTime, List<KinematicCharacterMotor> motors, List<PhysicsMover> movers)
        {
            int characterMotorsCount = motors.Count;
            int physicsMoversCount = movers.Count;

#pragma warning disable 0162
            // ========== 第一步：更新物理移动器速度 ==========
            // Kcc: 先更新所有移动器的速度，使它们的位置变为最新
            for (int i = 0; i < physicsMoversCount; i++)
            {
                movers[i].VelocityUpdate(deltaTime);
            }

            // ========== 第二步：角色 Phase 1 更新 ==========
            // Kcc: 在移动器移动之前，处理角色的地面检测
            // Kcc: 此时移动器的位置还是旧的，角色会检测到稳定的地面
            for (int i = 0; i < characterMotorsCount; i++)
            {
                motors[i].UpdatePhase1(deltaTime);
            }

            // ========== 第三步：移动器位移应用 ==========
            // Kcc: 现在移动器移动到新位置
            for (int i = 0; i < physicsMoversCount; i++)
            {
                PhysicsMover mover = movers[i];

                // 更新 Transform 和 Rigidbody
                mover.Transform.SetPositionAndRotation(mover.TransientPosition, mover.TransientRotation);
                mover.Rigidbody.position = mover.TransientPosition;
                mover.Rigidbody.rotation = mover.TransientRotation;
            }

            // ========== 第四步：角色 Phase 2 更新和移动 ==========
            // Kcc: 移动器已经移动，现在角色可以计算最终位置
            // Kcc: Phase 2 包含：UpdateRotation、UpdateVelocity、碰撞求解
            for (int i = 0; i < characterMotorsCount; i++)
            {
                KinematicCharacterMotor motor = motors[i];

                motor.UpdatePhase2(deltaTime);

                // Kcc: 更新 Transform 到新的瞬时位置
                motor.Transform.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation);
            }
#pragma warning restore 0162
        }

        // ============== 插值后处理方法 ==============

        /// <summary>
        /// 模拟后插值更新
        /// 
        /// Kcc: 在模拟完成后调用
        /// Kcc: 将角色和移动器恢复到初始位置，为插值做准备
        /// Kcc: 同时通过 MovePosition/MoveRotation 设置物理系统的目标位置
        /// </summary>
        /// <param name="deltaTime">帧时间间隔</param>
        public static void PostSimulationInterpolationUpdate(float deltaTime)
        {
            // 记录插值开始时间和持续时间
            _lastCustomInterpolationStartTime = Time.time;
            _lastCustomInterpolationDeltaTime = deltaTime;

            // Kcc: 将角色 Transform 恢复到初始位置（插值起点）
            for (int i = 0; i < CharacterMotors.Count; i++)
            {
                KinematicCharacterMotor motor = CharacterMotors[i];

                motor.Transform.SetPositionAndRotation(motor.InitialTickPosition, motor.InitialTickRotation);
            }

            // Kcc: 处理移动器的位置设置
            for (int i = 0; i < PhysicsMovers.Count; i++)
            {
                PhysicsMover mover = PhysicsMovers[i];

                if (mover.MoveWithPhysics)
                {
                    // Kcc: 使用 MovePosition/MoveRotation 设置物理目标
                    mover.Rigidbody.position = mover.InitialTickPosition;
                    mover.Rigidbody.rotation = mover.InitialTickRotation;

                    mover.Rigidbody.MovePosition(mover.TransientPosition);
                    mover.Rigidbody.MoveRotation(mover.TransientRotation);
                }
                else
                {
                    // 直接设置位置（不使用物理引擎）
                    mover.Rigidbody.position = (mover.TransientPosition);
                    mover.Rigidbody.rotation = (mover.TransientRotation);
                }
            }
        }

        // ============== 自定义插值方法 ==============

        /// <summary>
        /// 自定义插值更新
        /// 
        /// Kcc: 在 LateUpdate 中调用
        /// Kcc: 计算插值因子，在 InitialTickPosition 和 TransientPosition 之间平滑过渡
        /// Kcc: 实现平滑的角色移动，避免物理更新带来的抖动
        /// 
        /// Kcc: 插值因子计算：
        ///     factor = (当前时间 - 插值开始时间) / 上一帧的物理时间
        ///     当 factor = 0 时在初始位置，factor = 1 时在目标位置
        /// </summary>
        private static void CustomInterpolationUpdate()
        {
            // 计算插值因子（0 到 1）
            float interpolationFactor = Mathf.Clamp01((Time.time - _lastCustomInterpolationStartTime) / _lastCustomInterpolationDeltaTime);

            // ========== 角色插值 ==========
            for (int i = 0; i < CharacterMotors.Count; i++)
            {
                KinematicCharacterMotor motor = CharacterMotors[i];

                // Kcc: 在初始位置和目标位置之间线性插值
                motor.Transform.SetPositionAndRotation(
                    Vector3.Lerp(motor.InitialTickPosition, motor.TransientPosition, interpolationFactor),
                    Quaternion.Slerp(motor.InitialTickRotation, motor.TransientRotation, interpolationFactor));
            }

            // ========== 移动器插值 ==========
            for (int i = 0; i < PhysicsMovers.Count; i++)
            {
                PhysicsMover mover = PhysicsMovers[i];
                
                // 计算新的位置和旋转
                mover.Transform.SetPositionAndRotation(
                    Vector3.Lerp(mover.InitialTickPosition, mover.TransientPosition, interpolationFactor),
                    Quaternion.Slerp(mover.InitialTickRotation, mover.TransientRotation, interpolationFactor));

                // Kcc: 计算插值产生的位移差
                // Kcc: 用于让角色跟随移动器时计算正确的相对速度
                Vector3 newPos = mover.Transform.position;
                Quaternion newRot = mover.Transform.rotation;
                mover.PositionDeltaFromInterpolation = newPos - mover.LatestInterpolationPosition;
                mover.RotationDeltaFromInterpolation = Quaternion.Inverse(mover.LatestInterpolationRotation) * newRot;
                mover.LatestInterpolationPosition = newPos;
                mover.LatestInterpolationRotation = newRot;
            }
        }
    }
}
