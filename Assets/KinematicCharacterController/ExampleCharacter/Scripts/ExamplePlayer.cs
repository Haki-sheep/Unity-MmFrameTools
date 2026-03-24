using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;

namespace KinematicCharacterController.Examples
{
    /// <summary>
    /// 示例玩家控制器
    /// 负责处理玩家的输入（键盘、鼠标）和相机的控制
    /// 将输入信息传递给角色控制器
    /// </summary>
    public class ExamplePlayer : MonoBehaviour
    {
        /// <summary>
        /// 角色控制器引用，用于向角色传递输入信息
        /// </summary>
        public ExampleCharacterController Character;
        /// <summary>
        /// 角色相机引用，用于控制相机跟随和视角
        /// </summary>
        public ExampleCharacterCamera CharacterCamera;

        // 输入轴名称常量
        private const string MouseXInput = "Mouse X";      // 鼠标水平移动轴
        private const string MouseYInput = "Mouse Y";      // 鼠标垂直移动轴
        private const string MouseScrollInput = "Mouse ScrollWheel";  // 鼠标滚轮轴
        private const string HorizontalInput = "Horizontal";  // 水平移动轴（A/D键）
        private const string VerticalInput = "Vertical";    // 垂直移动轴（W/S键）

        /// <summary>
        /// 初始化方法，在对象创建后第一帧执行
        /// 设置光标锁定状态和相机初始配置
        /// </summary>
        private void Start()
        {
            // 锁定光标到游戏窗口中心，隐藏光标
            Cursor.lockState = CursorLockMode.Locked;

            // 告诉相机跟随角色的相机跟随点
            CharacterCamera.SetFollowTransform(Character.CameraFollowPoint);

            // 清除相机忽略的碰撞体列表
            CharacterCamera.IgnoredColliders.Clear();
            // 将角色及其子对象中的所有碰撞体添加到忽略列表，避免相机检测到自身碰撞
            CharacterCamera.IgnoredColliders.AddRange(Character.GetComponentsInChildren<Collider>());
        }

        /// <summary>
        /// 每帧更新方法，用于处理玩家输入
        /// </summary>
        private void Update()
        {
            // 当玩家点击鼠标左键时，重新锁定光标
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            // 处理角色输入
            HandleCharacterInput();
        }

        /// <summary>
        /// 晚于物理更新的每帧方法，用于处理相机跟随物理移动物体
        /// </summary>
        private void LateUpdate()
        {
            // 如果相机需要跟随物理移动物体，且角色绑定了刚体
            if (CharacterCamera.RotateWithPhysicsMover && Character.Motor.AttachedRigidbody != null)
            {
                // 获取物理移动器的旋转增量，更新相机的水平方向
                CharacterCamera.PlanarDirection = Character.Motor.AttachedRigidbody.GetComponent<PhysicsMover>().RotationDeltaFromInterpolation * CharacterCamera.PlanarDirection;
                // 将方向投影到水平面上，并归一化
                CharacterCamera.PlanarDirection = Vector3.ProjectOnPlane(CharacterCamera.PlanarDirection, Character.Motor.CharacterUp).normalized;
            }

            // 处理相机输入
            HandleCameraInput();
        }

        /// <summary>
        /// 处理相机输入
        /// 包括视角旋转和缩放
        /// </summary>
        private void HandleCameraInput()
        {
            // 获取鼠标输入
            float mouseLookAxisUp = Input.GetAxisRaw(MouseYInput);      // 鼠标垂直移动
            float mouseLookAxisRight = Input.GetAxisRaw(MouseXInput);   // 鼠标水平移动
            // 构建相机视角输入向量
            Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

            // 如果光标没有被锁定，重置视角输入为零
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                lookInputVector = Vector3.zero;
            }

            // 获取滚轮输入（负值使缩放方向更直观：向前滚轮放大）
            float scrollInput = -Input.GetAxis(MouseScrollInput);
#if UNITY_WEBGL
            // WebGL平台下禁用缩放功能，避免问题
            scrollInput = 0f;
#endif

            // 使用输入更新相机
            CharacterCamera.UpdateWithInput(Time.deltaTime, scrollInput, lookInputVector);

            // 处理切换缩放级别（切换近景/远景）
            if (Input.GetMouseButtonDown(1))
            {
                // 如果当前是近景（0），切换到默认距离；否则切换到近景
                CharacterCamera.TargetDistance = (CharacterCamera.TargetDistance == 0f) ? CharacterCamera.DefaultDistance : 0f;
            }
        }

        /// <summary>
        /// 处理角色输入
        /// 收集键盘/鼠标输入并传递给角色控制器
        /// </summary>
        private void HandleCharacterInput()
        {
            // 创建输入数据结构
            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

            // 构建角色输入数据
            characterInputs.MoveAxisForward = Input.GetAxisRaw(VerticalInput);    // 前后移动输入
            characterInputs.MoveAxisRight = Input.GetAxisRaw(HorizontalInput);    // 左右移动输入
            characterInputs.CameraRotation = CharacterCamera.Transform.rotation;  // 当前相机旋转
            characterInputs.JumpDown = Input.GetKeyDown(KeyCode.Space);           // 跳跃键（空格）
            characterInputs.CrouchDown = Input.GetKeyDown(KeyCode.C);            // 下蹲键按下（C键）
            characterInputs.CrouchUp = Input.GetKeyUp(KeyCode.C);                // 下蹲键释放（C键）

            // 将输入数据传递给角色控制器
            Character.SetInputs(ref characterInputs);
        }
    }
}