using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;

// 运动学角色控制器-入门示例：基础移动模块命名空间
namespace KinematicCharacterController.Walkthrough.BasicMovement
{
    /// <summary>
    /// 玩家角色输入数据结构体
    /// 存储角色移动和相机相关的输入信息
    /// </summary>
    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward; // 前后移动轴输入（WS/上下方向键）
        public float MoveAxisRight;   // 左右移动轴输入（AD/左右方向键）
        public Quaternion CameraRotation; // 主相机的旋转信息
    }

    /// <summary>
    /// 自定义角色控制器
    /// 实现ICharacterController接口，基于KinematicCharacterController插件实现基础的地面/空中移动逻辑
    /// </summary>
    public class MyCharacterController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor; // 运动学角色马达（核心驱动组件，插件核心）

        [Header("地面稳定移动参数")]
        public float MaxStableMoveSpeed = 10f; // 地面稳定移动的最大速度
        public float StableMovementSharpness = 15; // 地面移动的平滑过渡系数（值越大过渡越快）
        public float OrientationSharpness = 10; // 角色朝向的平滑过渡系数（值越大转向越灵敏）

        [Header("空中移动参数")]
        public float MaxAirMoveSpeed = 10f; // 空中最大移动速度
        public float AirAccelerationSpeed = 5f; // 空中移动的加速度
        public float Drag = 0.1f; // 空中移动的阻力（减缓空中速度）

        [Header("其他参数")]
        public bool RotationObstruction; // 角色旋转是否被阻挡
        public Vector3 Gravity = new Vector3(0, -30f, 0); // 重力向量（自定义重力，替代Unity物理重力）
        public Transform MeshRoot; // 角色网格根节点（用于视觉表现旋转，不影响物理）

        private Vector3 _moveInputVector; // 处理后的世界空间移动输入向量
        private Vector3 _lookInputVector; // 处理后的世界空间朝向输入向量（基于相机）
        
        private void Start()
        {
            // 初始化：将当前角色控制器赋值给马达，建立驱动关联
            Motor.CharacterController = this;
        }

        /// <summary>
        /// 设置角色输入信息
        /// 由玩家输入脚本（MyPlayer）每帧调用，传递最新的输入数据
        /// </summary>
        /// <param name="inputs">玩家输入数据结构体（引用传递，减少拷贝）</param>
        public void SetInputs(ref PlayerCharacterInputs inputs)
        {
            // 限制输入向量的长度为1，防止斜向移动时的速度叠加（斜向加速问题）
            Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);
            // 将相机的朝向投到平面上(Motor.CharacterUp为角色所在平面上的法向量)并归一化
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
            // 容错处理：如果相机正对着角色上下方向，改用相机向上方向计算平面朝向
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
            }
            // 计算相机在角色平面的旋转四元数（用于将本地输入转换为世界空间）
            Quaternion cameraPlanarRotation =   Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);
            // Debug.Log("_moveInputVector: " + _moveInputVector);
            // 将本地移动输入转换为世界空间（基于相机朝向），赋值给私有变量供后续使用
            _moveInputVector = cameraPlanarRotation * moveInputVector;

            // 保存相机平面的朝向方向，用于角色旋转
            _lookInputVector = cameraPlanarDirection;
        }

        /// <summary>
        /// 角色移动更新前的回调
        /// 由KinematicCharacterMotor在其更新周期中调用，执行移动前的预处理逻辑
        /// </summary>
        /// <param name="deltaTime">帧时间（时间步，保证移动帧率无关）</param>
        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// 更新角色旋转
        /// 由KinematicCharacterMotor在其更新周期中调用，**这是设置角色旋转的唯一有效位置**
        /// </summary>
        /// <param name="currentRotation">角色当前的旋转四元数（引用传递，直接修改）</param>
        /// <param name="deltaTime">帧时间</param>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 存在朝向输入且朝向平滑系数大于0时，执行平滑旋转
            if (_lookInputVector != Vector3.zero && OrientationSharpness > 0f)
            {
                // 指数插值平滑过渡角色朝向（相比Lerp更自然，帧率无关）
                Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                // 设置角色当前旋转（会被KinematicCharacterMotor使用，更新物理和视觉旋转）
                currentRotation =  Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
            }
        }

        /// <summary>
        /// 更新角色速度
        /// 由KinematicCharacterMotor在其更新周期中调用，**这是设置角色速度的唯一有效位置**
        /// </summary>
        /// <param name="currentVelocity">角色当前的速度向量（引用传递，直接修改）</param>
        /// <param name="deltaTime">帧时间</param>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 初始化目标移动速度
            Vector3 targetMovementVelocity = Vector3.zero;
            // 判断角色是否稳定站在地面上（插件的地面检测状态）
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                // 将当前速度重新映射到地面斜坡上，避免斜坡变化时的速度损失（平滑移动的容错）
                currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
                // 计算地面移动的目标速度
                Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp); // 移动输入的右侧方向(但是左手坐标系)
                // Debug.Log("_moveInputVector: " + _moveInputVector);
                // Debug.Log("inputRight: " + inputRight);
                // 意图重定向 将移动输入重新调整为贴合地面法线的方向，保证斜坡上的正确移动
                Vector3 reorientedInput = Vector3.Cross(Motor.GroundingStatus.GroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;

                // 指数插值平滑地面移动速度（相比Lerp更丝滑，无明显卡顿）
                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-StableMovementSharpness * deltaTime));
            }
            else
            {
                // 空中状态：处理空中移动输入
                if (_moveInputVector.sqrMagnitude > 0f)
                {
                    // 计算空中移动的目标速度
                    targetMovementVelocity = _moveInputVector * MaxAirMoveSpeed;

                    // 角色在空中但是没站稳 检测到地面 时，防止空中移动沿不稳定斜坡攀爬,避免从斜坡上下落的时候直接掉下去
                    if (Motor.GroundingStatus.FoundAnyGround)
                    {
                        // Debug.Log( Motor.GroundingStatus.GroundNormal);
                        // Debug.Log(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal));
                        // 用于消除空中时还想上坡的分量
                        Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;

                        // Debug.Log("perpenticularObstructionNormal: " + perpenticularObstructionNormal);
                        // 将目标速度投影到阻挡平面上，取消斜坡方向的移动 
                        // targetMovementVelocity向前  perpenticularObstructionNormal向后 直接变0
                        targetMovementVelocity = Vector3.ProjectOnPlane(targetMovementVelocity, perpenticularObstructionNormal);
                        // Debug.Log("targetMovementVelocity: " + targetMovementVelocity);

                    }

                    // 计算速度差值并投影到重力垂直平面（避免重力影响空中加速度）
                    Vector3 velocityDiff = Vector3.ProjectOnPlane(targetMovementVelocity - currentVelocity, Gravity);
                    // Debug.Log("velocityDiff: " + velocityDiff);
                    // 叠加空中加速度，更新当前速度
                    currentVelocity += velocityDiff * AirAccelerationSpeed * deltaTime;
                }

                // 空中状态：添加自定义重力
                currentVelocity += Gravity * deltaTime;

                // 空中状态：应用空气阻力，减缓空中速度
                currentVelocity *= 1f / (1f + (Drag * deltaTime));
            }
        }

        /// <summary>
        /// 角色移动更新后的回调
        /// 由KinematicCharacterMotor在其更新周期中调用，执行移动后的后续逻辑
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        public void AfterCharacterUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// 判断碰撞体是否可参与角色碰撞检测
        /// 插件调用，用于过滤不需要碰撞的物体（如触发体、友方单位等）
        /// </summary>
        /// <param name="coll">检测到的碰撞体</param>
        /// <returns>是否参与碰撞：true=参与，false=忽略</returns>
        public bool IsColliderValidForCollisions(Collider coll)
        {
            // 默认识别所有碰撞体，可根据需求过滤（如忽略标签为IgnorePlayer的物体）
            return true;
        }

        /// <summary>
        /// 地面碰撞回调
        /// 角色与地面发生碰撞时由插件调用
        /// </summary>
        /// <param name="hitCollider">碰撞的地面碰撞体</param>
        /// <param name="hitNormal">碰撞点的法线方向</param>
        /// <param name="hitPoint">世界空间的碰撞点</param>
        /// <param name="hitStabilityReport">碰撞稳定性报告（引用传递，可修改碰撞状态）</param>
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        /// <summary>
        /// 移动碰撞回调
        /// 角色移动过程中与物体发生碰撞时由插件调用
        /// </summary>
        /// <param name="hitCollider">碰撞的物体碰撞体</param>
        /// <param name="hitNormal">碰撞点的法线方向</param>
        /// <param name="hitPoint">世界空间的碰撞点</param>
        /// <param name="hitStabilityReport">碰撞稳定性报告（引用传递，可修改碰撞状态）</param>
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        /// <summary>
        /// 地面检测后的更新回调
        /// 插件完成地面检测后调用，可处理地面相关的后续逻辑（如落地检测、斜坡适配等）
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        public void PostGroundingUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// 给角色添加额外速度
        /// 用于外部触发的速度叠加（如跳跃、被击退、冲刺等）
        /// </summary>
        /// <param name="velocity">要添加的速度向量</param>
        public void AddVelocity(Vector3 velocity)
        {
        }

        /// <summary>
        /// 处理碰撞稳定性报告
        /// 插件调用，用于自定义碰撞的稳定性判断（如是否判定为稳定地面）
        /// </summary>
        /// <param name="hitCollider">碰撞的碰撞体</param>
        /// <param name="hitNormal">碰撞点法线</param>
        /// <param name="hitPoint">碰撞点</param>
        /// <param name="atCharacterPosition">碰撞时的角色位置</param>
        /// <param name="atCharacterRotation">碰撞时的角色旋转</param>
        /// <param name="hitStabilityReport">碰撞稳定性报告（引用传递，可修改）</param>
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        /// <summary>
        /// 离散碰撞检测回调
        /// 角色检测到离散碰撞（非连续碰撞）时由插件调用
        /// </summary>
        /// <param name="hitCollider">碰撞的碰撞体</param>
        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器可视化：显示地面检测和法线
        /// </summary>
        private void OnDrawGizmos()
        {
            if (Motor == null) return;

            Vector3 characterPos = Motor.Transform.position;

            // 绘制角色胶囊体底部（黄色）
            Gizmos.color = Color.yellow;
            Vector3 capsuleBottom = characterPos + Motor.CharacterTransformToCapsuleBottom;
            Gizmos.DrawWireSphere(capsuleBottom, 0.1f);

            // 绘制 FoundAnyGround 检测范围（白色球）- 在胶囊体下方
            if (Motor.GroundingStatus.FoundAnyGround)
            {
                Gizmos.color = Color.green;
                float probeDistance = Motor.Capsule != null ? Motor.Capsule.radius : 0.5f;
                Gizmos.DrawWireSphere(capsuleBottom - Motor.CharacterUp * probeDistance, probeDistance);    
            }

            // 绘制 GroundNormal（红色线）- 从胶囊体底部向上
            if (Motor.GroundingStatus.FoundAnyGround)
            {
                Gizmos.color = Color.red;
                Vector3 normalStart = capsuleBottom;
                Vector3 normalEnd = capsuleBottom + Motor.GroundingStatus.GroundNormal * 2f;
                Gizmos.DrawLine(normalStart, normalEnd);

                // 在法线末端显示文字
                UnityEditor.Handles.Label(normalEnd + Vector3.up * 0.2f,
                    $"GroundNormal:\n{Motor.GroundingStatus.GroundNormal}\nFoundAnyGround: {Motor.GroundingStatus.FoundAnyGround}\nStable: {Motor.GroundingStatus.IsStableOnGround}");
            }

            // 绘制阻挡法线（蓝色线）- 只在空中且检测到地面时
            if (!Motor.GroundingStatus.IsStableOnGround && Motor.GroundingStatus.FoundAnyGround)
            {
                Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(capsuleBottom, capsuleBottom + perpenticularObstructionNormal * 2f);

                UnityEditor.Handles.Label(capsuleBottom + perpenticularObstructionNormal * 2f + Vector3.up * 0.2f,
                    $"ObstructionNormal:\n{perpenticularObstructionNormal}");
            }

            // 绘制当前速度（黄色线）
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(characterPos, characterPos + Motor.Velocity * 0.1f);
            }
        }
#endif
    }
}