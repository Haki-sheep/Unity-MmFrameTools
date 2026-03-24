using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;

// 命名空间：KCC插件的寻路示例 - 玩家/相机/角色控制器搭建模块
namespace KinematicCharacterController.Walkthrough.PlayerCameraCharacterSetup
{
    /// <summary>
    /// 自定义KCC角色控制器
    /// 实现ICharacterController接口，接管KCC运动马达的核心回调，自定义角色的旋转、移动等逻辑
    /// 是KCC运动马达（KinematicCharacterMotor）的核心逻辑载体
    /// </summary>
    public class MyCharacterController : MonoBehaviour, ICharacterController
    {
        // KCC核心运动马达：负责角色的物理移动、碰撞检测、地面探测等底层逻辑，通过接口回调驱动当前控制器
        public KinematicCharacterMotor Motor;

        /// <summary>
        /// 初始化：建立运动马达与当前自定义控制器的关联
        /// </summary>
        private void Start()
        {
            // 将当前实现了ICharacterController的自定义控制器赋值给马达
            // 让KCC运动马达以当前控制器的逻辑来驱动角色运动
            Motor.CharacterController = this;
        }

        /// <summary>
        /// 【KCC接口回调】角色更新前执行
        /// 马达执行所有更新逻辑（旋转、速度、碰撞、地面检测等）之前的回调
        /// </summary>
        /// <param name="deltaTime">帧时间增量</param>
        public void BeforeCharacterUpdate(float deltaTime)
        {
            // 可在此处编写：更新前的准备逻辑（如输入采集、状态重置等）
        }

        /// <summary>
        /// 【KCC接口回调】更新角色旋转
        /// 马达请求获取角色当前目标旋转时的回调，可在此自定义角色旋转逻辑
        /// </summary>
        /// <param name="currentRotation">角色当前的旋转值</param>
        /// <param name="deltaTime">帧时间增量</param>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 可在此处编写：角色旋转逻辑（如跟随相机视角旋转、转向移动方向等）
            // 通过修改ref参数currentRotation来改变角色的最终旋转
        }

        /// <summary>
        /// 【KCC接口回调】更新角色速度
        /// 马达请求获取角色当前目标速度时的回调，可在此自定义角色移动速度逻辑
        /// </summary>
        /// <param name="currentVelocity">角色当前的速度值</param>
        /// <param name="deltaTime">帧时间增量</param>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 可在此处编写：角色移动速度逻辑（如根据输入计算移动速度、重力、跳跃、冲刺等）
            // 通过修改ref参数currentVelocity来改变角色的最终移动速度
        }

        /// <summary>
        /// 【KCC接口回调】角色更新后执行
        /// 马达完成所有更新逻辑（旋转、速度、碰撞、地面检测等）之后的回调
        /// </summary>
        /// <param name="deltaTime">帧时间增量</param>
        public void AfterCharacterUpdate(float deltaTime)
        {
            // 可在此处编写：更新后的收尾逻辑（如状态同步、特效触发、相机适配等）
        }

        /// <summary>
        /// 【KCC接口回调】判断碰撞体是否可参与碰撞
        /// 马达检测到碰撞体时，会调用此方法判断是否需要与该碰撞体产生碰撞
        /// </summary>
        /// <param name="coll">检测到的碰撞体</param>
        /// <returns>true=可碰撞，false=穿模忽略该碰撞体</returns>
        public bool IsColliderValidForCollisions(Collider coll)
        {
            // 此处默认返回true，代表所有碰撞体都参与碰撞
            // 可自定义过滤（如忽略特定层、特定标签的碰撞体）
            return true;
        }

        /// <summary>
        /// 【KCC接口回调】地面探测命中回调
        /// 马达的地面探测逻辑检测到地面时触发的回调
        /// </summary>
        /// <param name="hitCollider">命中的地面碰撞体</param>
        /// <param name="hitNormal">地面的命中法向量</param>
        /// <param name="hitPoint">地面的命中点坐标</param>
        /// <param name="hitStabilityReport">碰撞稳定性报告（可修改）</param>
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            // 可在此处编写：触地后的逻辑（如取消空中状态、播放落地特效、恢复跳跃能力等）
        }

        /// <summary>
        /// 【KCC接口回调】移动过程中碰撞命中回调
        /// 马达在处理角色移动逻辑时，检测到与碰撞体碰撞时触发的回调
        /// </summary>
        /// <param name="hitCollider">命中的碰撞体</param>
        /// <param name="hitNormal">碰撞体的命中法向量</param>
        /// <param name="hitPoint">碰撞体的命中点坐标</param>
        /// <param name="hitStabilityReport">碰撞稳定性报告（可修改）</param>
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            // 可在此处编写：移动碰撞后的逻辑（如撞墙反馈、反弹、触发机关等）
        }

        /// <summary>
        /// 【KCC接口回调】处理碰撞稳定性报告
        /// 马达检测到任意碰撞后，会调用此方法让开发者自定义修改碰撞稳定性报告
        /// 可通过修改报告来控制马达对该碰撞的处理方式（如是否忽略、是否稳定碰撞等）
        /// </summary>
        /// <param name="hitCollider">命中的碰撞体</param>
        /// <param name="hitNormal">命中法向量</param>
        /// <param name="hitPoint">命中点坐标</param>
        /// <param name="atCharacterPosition">碰撞发生时的角色位置</param>
        /// <param name="atCharacterRotation">碰撞发生时的角色旋转</param>
        /// <param name="hitStabilityReport">待处理的碰撞稳定性报告</param>
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
            // 可在此处自定义修改hitStabilityReport的参数，影响马达对碰撞的后续处理
        }

        /// <summary>
        /// 【KCC接口回调】地面检测完成后的更新回调
        /// 马达完成地面探测逻辑后、处理物理移动物体/速度等逻辑之前的回调
        /// 是处理地面相关状态的核心回调（如判断是否浮空、调整重力等）
        /// </summary>
        /// <param name="deltaTime">帧时间增量</param>
        public void PostGroundingUpdate(float deltaTime)
        {
            // 可在此处编写：地面检测后的状态逻辑（如根据是否接地调整重力、处理空中下落等）
        }

        /// <summary>
        /// 【KCC接口回调】检测到离散碰撞的回调
        /// 马达检测到**非移动逻辑导致**的离散碰撞时触发（如被其他刚体碰撞、场景碰撞体动态变化等）
        /// </summary>
        /// <param name="hitCollider">检测到的碰撞体</param>
        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
            // 可在此处编写：离散碰撞的处理逻辑（如被撞后的位移反馈、受击判定等）
        }
    }
}