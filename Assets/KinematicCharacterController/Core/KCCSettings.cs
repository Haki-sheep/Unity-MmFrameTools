using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KinematicCharacterController
{
    [CreateAssetMenu]
    // 运动学角色控制器（KCC）全局设置配置文件，继承ScriptableObject可在编辑器创建配置实例
    public class KCCSettings : ScriptableObject
    {
        /// <summary>
        /// 决定系统是否自动执行运动学模拟
        /// Determines if the system simulates automatically.
        /// 如果设为true，模拟逻辑将在FixedUpdate生命周期中执行
        /// If true, the simulation is done on FixedUpdate
        /// </summary>
        [Tooltip("是否让系统自动执行模拟，设为true时模拟将在FixedUpdate中执行")]
        public bool AutoSimulation = true;

        /// <summary>
        /// 是否开启角色和物理移动物体（PhysicsMovers）的插值处理
        /// Should interpolation of characters and PhysicsMovers be handled
        /// 开启后可让物体移动更平滑，避免帧率波动导致的抖动
        /// </summary>
        [Tooltip("是否处理角色与PhysicsMovers的移动插值（开启更平滑）")]
        public bool Interpolate = true;

        /// <summary>
        /// 系统中运动器（Motors）列表的初始容量
        /// Initial capacity of the system's list of Motors (will resize automatically if needed, but setting a high initial capacity can help preventing GC allocs)
        /// 列表会根据需要自动扩容，设置合适的初始容量可减少内存重新分配，避免GC开销
        /// </summary>
        [Tooltip("运动器(Motors)列表初始容量，合理设置可避免GC分配")]
        public int MotorsListInitialCapacity = 100;

        /// <summary>
        /// 系统中移动物体（Movers）列表的初始容量
        /// Initial capacity of the system's list of Movers (will resize automatically if needed, but setting a high initial capacity can help preventing GC allocs)
        /// 列表会根据需要自动扩容，设置合适的初始容量可减少内存重新分配，避免GC开销
        /// </summary>
        [Tooltip("移动物体(Movers)列表初始容量，合理设置可避免GC分配")]
        public int MoversListInitialCapacity = 100;
    }
}