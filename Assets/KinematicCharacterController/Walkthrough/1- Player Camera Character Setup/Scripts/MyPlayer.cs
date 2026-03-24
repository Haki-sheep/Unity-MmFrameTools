using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
using System.Linq;

// 命名空间：KCC插件的寻路示例 - 玩家/相机/角色控制器搭建模块
namespace KinematicCharacterController.Walkthrough.PlayerCameraCharacterSetup
{
    /// <summary>
    /// KCC插件自定义玩家脚本
    /// 核心作用：管控轨道相机的视角输入、相机跟随初始化，为角色控制器提供相机视角基础数据
    /// </summary>
    public class MyPlayer : MonoBehaviour
    {
        [Header("相机与角色组件引用")]
        public ExampleCharacterCamera OrbitCamera; // 轨道相机核心组件（KCC示例相机）
        public Transform CameraFollowPoint;       // 相机跟随的目标点（角色身上的专用点位，避免模型旋转导致相机抖动）
        public MyCharacterController Character;   // 自定义的KCC角色控制器

        private Vector3 _lookInputVector = Vector3.zero; // 相机视角的鼠标输入向量（X=左右拖动，Y=上下拖动）

        /// <summary>
        /// 初始化：鼠标状态、相机跟随、相机碰撞忽略设置
        /// </summary>
        private void Start()
        {
            // 锁定鼠标光标到游戏窗口中心，隐藏光标，保证视角操作正常
            Cursor.lockState = CursorLockMode.Locked;

            // 给轨道相机设置跟随的目标变换（绑定到角色的相机跟随点）
            OrbitCamera.SetFollowTransform(CameraFollowPoint);

            // 让相机在检测遮挡时，忽略角色自身的所有碰撞器（解决相机穿模角色模型的问题）
            OrbitCamera.IgnoredColliders = Character.GetComponentsInChildren<Collider>().ToList();
        }

        /// <summary>
        /// 每帧更新：处理鼠标左键重新锁定光标的逻辑
        /// </summary>
        private void Update()
        {
            // 当鼠标左键按下时，重新锁定光标（解决ESC解锁光标后，点击游戏窗口恢复操作的需求）
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        /// <summary>
        /// 延迟更新：在所有物体Update执行后处理相机输入（Unity相机控制最佳实践，避免视角抖动）
        /// </summary>
        private void LateUpdate()
        {
            // 调用相机输入处理的核心方法
            HandleCameraInput();
        }

        /// <summary>
        /// 相机输入处理核心方法
        /// 功能：采集鼠标视角拖动、滚轮变焦、右键视角切换输入，并传递给轨道相机执行更新
        /// </summary>
        private void HandleCameraInput()
        {
            // 采集鼠标原始输入轴：Y轴=上下拖动视角，X轴=左右拖动视角（GetAxisRaw无平滑，响应更灵敏）
            float mouseLookAxisUp = Input.GetAxisRaw("Mouse Y");
            float mouseLookAxisRight = Input.GetAxisRaw("Mouse X");
            // 构建相机视角的输入向量，传递给相机做旋转计算
            _lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

            // 光标未锁定时，置零视角输入（防止玩家解锁光标后，误触鼠标导致视角乱飘）
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                _lookInputVector = Vector3.zero;
            }

            // 采集鼠标滚轮输入，用于相机变焦（负号是为了让滚轮上滑拉近、下滑拉远，符合常规操作习惯）
            float scrollInput = -Input.GetAxis("Mouse ScrollWheel");
#if UNITY_WEBGL
            // WebGL平台禁用滚轮变焦（避免平台兼容问题，导致相机异常）
            scrollInput = 0f;
#endif

            // 将时间增量、滚轮变焦输入、鼠标视角输入传递给相机，执行相机的旋转/变焦更新
            OrbitCamera.UpdateWithInput(Time.deltaTime, scrollInput, _lookInputVector);

            // 鼠标右键按下时，切换相机视角：贴脸视角（距离0）↔ 默认远距视角
            if (Input.GetMouseButtonDown(1))
            {
                OrbitCamera.TargetDistance = (OrbitCamera.TargetDistance == 0f) ? OrbitCamera.DefaultDistance : 0f;
            }
        }
    }
}