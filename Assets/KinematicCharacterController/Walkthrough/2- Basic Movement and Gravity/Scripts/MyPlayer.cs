// 引入Unity基础集合库
using System.Collections;
// 引入泛型集合库
using System.Collections.Generic;
// Unity核心引擎库
using UnityEngine;
// 运动学角色控制器核心库
using KinematicCharacterController;
// 运动学角色控制器官方示例库
using KinematicCharacterController.Examples;
// LINQ查询库（此处用于批量获取碰撞器）
using System.Linq;

// 命名空间：运动学角色控制器-入门教程-基础移动模块
namespace KinematicCharacterController.Walkthrough.BasicMovement
{
    /// <summary>
    /// 自定义玩家控制核心脚本
    /// 负责：鼠标/键盘输入处理、轨道相机控制、角色控制器输入参数传递
    /// </summary>
    public class MyPlayer : MonoBehaviour
    {
        [Header("相机相关引用")]
        // 轨道相机（第三人称环绕相机）
        public ExampleCharacterCamera OrbitCamera;
        // 相机跟随的目标点（角色身上的空物体，控制相机跟随位置）
        public Transform CameraFollowPoint;

        [Header("角色控制器引用")]
        // 自定义角色控制器（处理角色移动、物理等核心逻辑）
        public MyCharacterController Character;

        // 输入轴名称常量 - 鼠标X轴（左右视角），避免魔法字符串
        private const string MouseXInput = "Mouse X";
        // 输入轴名称常量 - 鼠标Y轴（上下视角）
        private const string MouseYInput = "Mouse Y";
        // 输入轴名称常量 - 鼠标滚轮（相机缩放）
        private const string MouseScrollInput = "Mouse ScrollWheel";
        // 输入轴名称常量 - 水平轴（A/D/左右方向键）
        private const string HorizontalInput = "Horizontal";
        // 输入轴名称常量 - 垂直轴（W/S/前后方向键）
        private const string VerticalInput = "Vertical";

        /// <summary>
        /// Unity生命周期 - 游戏启动时执行一次
        /// 初始化鼠标状态、相机跟随、相机碰撞忽略
        /// </summary>
        private void Start()
        {
            // 锁定鼠标光标到屏幕中心，隐藏光标（第一/第三人称游戏常规操作）
            Cursor.lockState = CursorLockMode.Locked;

            // 设置轨道相机的跟随目标为指定的跟随点
            OrbitCamera.SetFollowTransform(CameraFollowPoint);

            // 清空相机遮挡检测的忽略碰撞器列表（防止默认值干扰）
            OrbitCamera.IgnoredColliders.Clear();
            // 将角色所有子物体的碰撞器加入相机忽略列表，避免相机被角色自身遮挡
            OrbitCamera.IgnoredColliders.AddRange(Character.GetComponentsInChildren<Collider>());
        }

        /// <summary>
        /// Unity生命周期 - 每帧更新（早于LateUpdate）
        /// 处理玩家输入检测（输入检测推荐在Update执行）
        /// </summary>
        private void Update()
        {
            // 鼠标左键按下时，重新锁定光标（防止玩家按ESC解锁后无法恢复）
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            // 处理角色移动相关的输入
            HandleCharacterInput();
        }

        /// <summary>
        /// Unity生命周期 - 每帧延迟更新（晚于所有Update）
        /// 处理相机逻辑（避免相机与角色运动不同步，推荐在LateUpdate执行）
        /// </summary>
        private void LateUpdate()
        {
            // 处理相机视角、缩放相关的输入
            HandleCameraInput();
        }

        /// <summary>
        /// 处理相机的输入逻辑：视角旋转、滚轮缩放、右键切视角
        /// </summary>
        private void HandleCameraInput()
        {
            // 获取鼠标上下/左右的原始输入值，构建相机视角输入向量
            float mouseLookAxisUp = Input.GetAxisRaw(MouseYInput);   // 鼠标Y轴-上下视角
            float mouseLookAxisRight = Input.GetAxisRaw(MouseXInput); // 鼠标X轴-左右视角
            Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

            // 鼠标未锁定时，置零视角输入（防止未锁定光标时误操作相机）
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                lookInputVector = Vector3.zero;
            }

            // 获取鼠标滚轮输入（取反：滚轮默认值与相机缩放方向相反，取反后符合直觉）
            float scrollInput = -Input.GetAxis(MouseScrollInput);
            // WebGL平台禁用滚轮缩放（避免该平台下滚轮输入的兼容问题）
#if UNITY_WEBGL
            scrollInput = 0f;
#endif

            // 将输入参数传递给轨道相机，执行相机的更新逻辑（传入帧时间保证帧率无关）
            OrbitCamera.UpdateWithInput(Time.deltaTime, scrollInput, lookInputVector);

            // 鼠标右键按下时，切换相机视角（0=第一人称/默认距离=第三人称）
            if (Input.GetMouseButtonDown(1))
            {
                OrbitCamera.TargetDistance = (OrbitCamera.TargetDistance == 0f) ? OrbitCamera.DefaultDistance : 0f;
            }
        }

        /// <summary>
        /// 处理角色的移动输入逻辑：获取键盘轴输入，封装为输入结构体传递给角色控制器
        /// </summary>
        private void HandleCharacterInput()
        {
            // 初始化玩家角色输入结构体（存储角色移动所需的所有输入参数）
            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

            // 给输入结构体赋值：垂直轴-前进/后退（W/S）、水平轴-左/右（A/D）
            characterInputs.MoveAxisForward = Input.GetAxisRaw(VerticalInput);
            characterInputs.MoveAxisRight = Input.GetAxisRaw(HorizontalInput);
            // 给输入结构体赋值：相机的旋转信息（角色移动方向将基于相机视角，符合第三人称操作逻辑）
            characterInputs.CameraRotation = OrbitCamera.Transform.rotation;

            // 将输入参数传递给角色控制器（ref：结构体是值类型，传引用避免值拷贝，提升效率）
            Character.SetInputs(ref characterInputs);
        }
    }
}