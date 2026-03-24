using KinematicCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Playables;

namespace KinematicCharacterController.Walkthrough.MovingPlatform
{


    /// <summary>
    /// 自定义移动平台核心类
    /// 实现IMoverController接口，通过时间线(PlayableDirector)驱动平台移动
    /// </summary>
    public class MyMovingPlatform : MonoBehaviour, IMoverController
    {
        public PhysicsMover Mover; // 物理移动器组件（处理平台的物理移动逻辑）
        public PlayableDirector Director; // 时间线导演组件（控制动画/平台轨迹）

        private Transform _transform; // 缓存自身Transform组件（减少GC和性能消耗）

        private void Start()
        {
            _transform = this.transform;

            // 将当前控制器赋值给物理移动器（核心关联步骤）
            Mover.MoverController = this;
        }

        /// <summary>
        /// 由PhysicsMover在每个FixedUpdate调用
        /// 用于告知物理移动器平台需要移动到的目标位置和旋转角度
        /// </summary>
        /// <param name="goalPosition">输出：平台的目标位置</param>
        /// <param name="goalRotation">输出：平台的目标旋转</param>
        /// <param name="deltaTime">帧时间增量（物理帧时间）</param>
        public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
        {
            // 记录平台当前的真实位置和旋转A
            Vector3 _positionBeforeAnim = _transform.position;
            Quaternion _rotationBeforeAnim = _transform.rotation;

            // 让TimeLine计算应该去的地方B
            EvaluateAtTime(Time.time);

            // 把B结果给PhysicsMover脚本
            goalPosition = _transform.position;
            goalRotation = _transform.rotation;

            // 立即恢复 Transform 到 A 点 但 PhysicsMover已经拿到 B 点作为目标了
            // 这样做是为了让物理移动器处理真实的移动逻辑，而非直接由动画驱动（避免物理穿透/卡顿）
            _transform.position = _positionBeforeAnim;
            _transform.rotation = _rotationBeforeAnim;
        }

        /// <summary>
        /// 根据指定时间更新时间线导演的状态
        /// </summary>
        /// <param name="time">要设置的目标时间</param>
        public void EvaluateAtTime(double time)
        {
            // 将时间线时间设置为指定时间对总时长取模（实现循环播放）
            Director.time = time % Director.duration;
            // 强制计算时间线在当前时间的状态（更新平台目标位姿）
            Director.Evaluate();
        }
    }
}