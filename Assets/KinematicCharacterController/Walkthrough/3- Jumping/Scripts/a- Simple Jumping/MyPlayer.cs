using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
using System.Linq;

namespace KinematicCharacterController.Walkthrough.SimpleJumping
{
    /// <summary>
    /// 玩家输入控制器
    /// 负责处理鼠标/键盘输入，分发给相机和角色控制器
    /// 核心功能：控制轨道相机视角、收集角色移动/跳跃输入并传递给角色控制器
    /// </summary>
    public class MyPlayer : MonoBehaviour
    {
        /// <summary>轨道相机组件（用于第三人称视角控制）</summary>
        public ExampleCharacterCamera OrbitCamera;
        /// <summary>相机跟随的目标点（角色身上的空物体，用于调整相机跟随位置）</summary>
        public Transform CameraFollowPoint;
        /// <summary>自定义角色控制器（接收输入并处理角色运动）</summary>
        public MyCharacterController Character;

        // 输入轴常量定义（避免硬编码，提高可读性）
        private const string MouseXInput = "Mouse X";       // 鼠标水平移动输入轴
        private const string MouseYInput = "Mouse Y";       // 鼠标垂直移动输入轴
        private const string MouseScrollInput = "Mouse ScrollWheel"; // 鼠标滚轮输入轴
        private const string HorizontalInput = "Horizontal"; // 水平移动输入轴（AD键/左摇杆左右）
        private const string VerticalInput = "Vertical";     // 垂直移动输入轴（WS键/左摇杆上下）

        private void Start()
        {
            // 锁定鼠标到屏幕中心（避免视角控制时鼠标移出窗口）
            Cursor.lockState = CursorLockMode.Locked;

            // 设置相机的跟随目标为指定的跟随点
            OrbitCamera.SetFollowTransform(CameraFollowPoint);

            // 让相机忽略角色的所有碰撞体（防止相机被角色模型遮挡）
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

            // 处理角色的移动/跳跃输入
            HandleCharacterInput();
        }

        private void LateUpdate()
        {
            // 处理相机的视角/缩放输入（LateUpdate确保在角色移动后更新相机，避免抖动）
            HandleCameraInput();
        }

        /// <summary>
        /// 处理相机输入（鼠标移动控制视角、滚轮控制距离、右键切换近距离视角）
        /// </summary>
        private void HandleCameraInput()
        {
            // 获取鼠标视角输入向量（X=水平，Y=垂直）
            float mouseLookAxisUp = Input.GetAxisRaw(MouseYInput);      // 鼠标垂直移动值（上下）
            float mouseLookAxisRight = Input.GetAxisRaw(MouseXInput);   // 鼠标水平移动值（左右）
            Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

            // 如果鼠标未锁定，禁用视角移动（防止误操作）
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                lookInputVector = Vector3.zero;
            }

            // 获取鼠标滚轮输入（用于缩放相机距离，WebGL平台禁用以避免兼容问题）
            float scrollInput = -Input.GetAxis(MouseScrollInput);
#if UNITY_WEBGL
            scrollInput = 0f;
#endif

            // 将输入传递给相机，更新相机视角和距离
            OrbitCamera.UpdateWithInput(Time.deltaTime, scrollInput, lookInputVector);

            // 鼠标右键切换相机距离（近距离视角/默认视角）
            if (Input.GetMouseButtonDown(1))
            {
                OrbitCamera.TargetDistance = (OrbitCamera.TargetDistance == 0f) ? OrbitCamera.DefaultDistance : 0f;
            }
        }

        /// <summary>
        /// 处理角色输入（收集移动/跳跃输入，封装为结构体并传递给角色控制器）
        /// </summary>
        private void HandleCharacterInput()
        {
            // 初始化角色输入结构体（用于传递输入数据）
            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

            // 填充角色输入数据
            characterInputs.MoveAxisForward = Input.GetAxisRaw(VerticalInput);    // 前后移动输入（WS键）
            characterInputs.MoveAxisRight = Input.GetAxisRaw(HorizontalInput);    // 左右移动输入（AD键）
            characterInputs.CameraRotation = OrbitCamera.Transform.rotation;      // 相机当前旋转（用于角色移动方向匹配视角）
            characterInputs.JumpDown = Input.GetKeyDown(KeyCode.Space);            // 跳跃输入（空格键按下瞬间）

            // 将输入数据传递给角色控制器（引用传递避免值拷贝）
            Character.SetInputs(ref characterInputs);
        }
    }
}