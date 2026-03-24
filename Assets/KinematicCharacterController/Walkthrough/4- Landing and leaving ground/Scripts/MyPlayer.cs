using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;

namespace KinematicCharacterController.Walkthrough.LandingLeavingGround
{
    // 玩家输入管理器：负责收集玩家的键鼠/手柄输入，分发给角色控制器和相机系统
    // 核心功能：1. 处理相机的视角控制（旋转/缩放） 2. 收集移动/跳跃输入并传递给角色控制器
    public class MyPlayer : MonoBehaviour
    {
        public ExampleCharacterCamera OrbitCamera; // 轨道相机组件（负责第三人称视角旋转/缩放）
        public Transform CameraFollowPoint; // 相机跟随的目标点（角色身上的相机锚点）
        public MyCharacterController Character; // 角色控制器（用于传递玩家输入）

        // 输入轴常量（统一管理输入轴名称，方便后续修改）
        private const string MouseXInput = "Mouse X"; // 鼠标X轴输入（左右移动）
        private const string MouseYInput = "Mouse Y"; // 鼠标Y轴输入（上下移动）
        private const string MouseScrollInput = "Mouse ScrollWheel"; // 鼠标滚轮输入（相机缩放）
        private const string HorizontalInput = "Horizontal"; // 水平移动轴（AD键/左摇杆左右）
        private const string VerticalInput = "Vertical"; // 垂直移动轴（WS键/左摇杆上下）

        private void Start()
        {
            // 锁定鼠标到屏幕中心（避免视角控制时鼠标移出窗口）
            Cursor.lockState = CursorLockMode.Locked;

            // 告诉轨道相机要跟随的目标变换（角色的相机锚点）
            OrbitCamera.SetFollowTransform(CameraFollowPoint);

            // 让相机忽略角色自身的碰撞体（防止相机被角色模型遮挡）
            OrbitCamera.IgnoredColliders.Clear();
            OrbitCamera.IgnoredColliders.AddRange(Character.GetComponentsInChildren<Collider>());
        }

        private void Update()
        {
            // 点击鼠标左键时重新锁定鼠标（防止解锁后无法控制视角）
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            // 每帧处理角色的移动/跳跃输入
            HandleCharacterInput();
        }

        private void LateUpdate()
        {
            // 延迟更新相机（确保角色移动后再更新相机，避免视角抖动）
            HandleCameraInput();
        }

        /// <summary>
        /// 处理相机输入（鼠标旋转、滚轮缩放、右键切换近距离视角）
        /// </summary>
        private void HandleCameraInput()
        {
            // 构建相机视角输入向量（鼠标X=左右旋转，鼠标Y=上下旋转）
            float mouseLookAxisUp = Input.GetAxisRaw(MouseYInput);
            float mouseLookAxisRight = Input.GetAxisRaw(MouseXInput);
            Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

            // 鼠标未锁定时，禁用视角控制（避免误操作）
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                lookInputVector = Vector3.zero;
            }

            // 鼠标滚轮输入（用于相机缩放，WebGL平台禁用防止兼容问题）
            float scrollInput = -Input.GetAxis(MouseScrollInput);
#if UNITY_WEBGL
            scrollInput = 0f;
#endif

            // 将输入传递给轨道相机，更新相机位置和旋转
            OrbitCamera.UpdateWithInput(Time.deltaTime, scrollInput, lookInputVector);

            // 右键点击切换相机距离（近距离视角/默认视角）
            if (Input.GetMouseButtonDown(1))
            {
                OrbitCamera.TargetDistance = (OrbitCamera.TargetDistance == 0f) ? OrbitCamera.DefaultDistance : 0f;
            }
        }

        /// <summary>
        /// 处理角色输入（移动、跳跃），并传递给角色控制器
        /// 核心：将Unity输入轴转换为角色控制器需要的PlayerCharacterInputs结构体
        /// </summary>
        private void HandleCharacterInput()
        {
            // 创建角色输入结构体（用于传递输入数据）
            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

            // 填充移动输入（原始输入轴，无平滑处理）
            characterInputs.MoveAxisForward = Input.GetAxisRaw(VerticalInput);
            characterInputs.MoveAxisRight = Input.GetAxisRaw(HorizontalInput);
            // 传递相机旋转（让角色移动方向匹配相机视角）
            characterInputs.CameraRotation = OrbitCamera.Transform.rotation;
            // 跳跃输入（仅检测空格键按下的那一帧）
            characterInputs.JumpDown = Input.GetKeyDown(KeyCode.Space);

            // 将输入数据传递给角色控制器（引用传递，减少内存拷贝）
            Character.SetInputs(ref characterInputs);
        }
    }
}