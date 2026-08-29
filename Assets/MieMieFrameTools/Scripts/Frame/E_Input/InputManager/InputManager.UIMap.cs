using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MieMieFrameWork.M_InputSystem
{
    /// <summary>
    /// InputManager UI 命令映射 绑定来自 InputSystemAction.inputactions 的 UICommand Map
    /// 事件命名 On/操作/对象/手势 Started按下瞬间 Held持续按住 Canceled松开瞬间
    /// </summary>
    public partial class InputManager
    {
        /// <summary> UI 命令映射 全局常开 独立于 Player/UI Map 切换 </summary>
        private InputActionMap uiCommandMap;

        /// <summary> 打开背包 </summary>
        private InputAction openBagAction;

        /// <summary> 打开设置 </summary>
        private InputAction openSettingAction;

        /// <summary> 物品轮盘 </summary>
        private InputAction itemWheelAction;

        /// <summary> 打开制作 </summary>
        private InputAction openCraftAction;

        /// <summary> 打开背包按下瞬间 </summary>
        public Action OnOpenBagStarted;

        /// <summary> 打开设置按下瞬间 </summary>
        public Action OnOpenSettingStarted;

        /// <summary> 物品轮盘按下瞬间 </summary>
        public Action OnItemWheelStarted;

        /// <summary> 物品轮盘松开瞬间 </summary>
        public Action OnItemWheelCanceled;

        /// <summary> 打开制作按下瞬间 </summary>
        public Action OnOpenCraftStarted;

        /// <summary> 物品轮盘按住中 </summary>
        public bool IsItemWheelHeld => itemWheelAction != null && itemWheelAction.IsPressed();

        /// <summary>
        /// 订阅并启用 UICommand Map
        /// </summary>
        private void SetupUiCommandMap()
        {
            if (inputActions == null || inputActions.asset == null)
                return;

            uiCommandMap = inputActions.asset.FindActionMap("UICommand");
            if (uiCommandMap == null)
            {
                Debug.LogError("InputSystemAction 缺少 UICommand Map");
                return;
            }

            openBagAction = uiCommandMap.FindAction("OpenBag");
            openSettingAction = uiCommandMap.FindAction("OpenSetting");
            itemWheelAction = uiCommandMap.FindAction("ItemWheel");
            openCraftAction = uiCommandMap.FindAction("OpenCraft");

            if (openBagAction != null)
                openBagAction.started += OnOpenBagStartedCallback;
            if (openSettingAction != null)
                openSettingAction.started += OnOpenSettingStartedCallback;
            if (itemWheelAction != null)
            {
                itemWheelAction.started += OnItemWheelStartedCallback;
                itemWheelAction.canceled += OnItemWheelCanceledCallback;
            }
            if (openCraftAction != null)
                openCraftAction.started += OnOpenCraftStartedCallback;

            uiCommandMap.Enable();
        }

        /// <summary>
        /// 取消订阅并关闭 UICommand Map
        /// </summary>
        private void TeardownUiCommandMap()
        {
            if (uiCommandMap == null)
                return;

            if (openBagAction != null)
                openBagAction.started -= OnOpenBagStartedCallback;
            if (openSettingAction != null)
                openSettingAction.started -= OnOpenSettingStartedCallback;
            if (itemWheelAction != null)
            {
                itemWheelAction.started -= OnItemWheelStartedCallback;
                itemWheelAction.canceled -= OnItemWheelCanceledCallback;
            }
            if (openCraftAction != null)
                openCraftAction.started -= OnOpenCraftStartedCallback;

            uiCommandMap.Disable();
            uiCommandMap = null;
            openBagAction = null;
            openSettingAction = null;
            itemWheelAction = null;
            openCraftAction = null;
        }

        /// <summary>
        /// 打开背包按下
        /// </summary>
        private void OnOpenBagStartedCallback(InputAction.CallbackContext context)
        {
            OnOpenBagStarted?.Invoke();
        }

        /// <summary>
        /// 打开设置按下
        /// </summary>
        private void OnOpenSettingStartedCallback(InputAction.CallbackContext context)
        {
            OnOpenSettingStarted?.Invoke();
        }

        /// <summary>
        /// 物品轮盘按下
        /// </summary>
        private void OnItemWheelStartedCallback(InputAction.CallbackContext context)
        {
            OnItemWheelStarted?.Invoke();
        }

        /// <summary>
        /// 物品轮盘松开
        /// </summary>
        private void OnItemWheelCanceledCallback(InputAction.CallbackContext context)
        {
            OnItemWheelCanceled?.Invoke();
        }

        /// <summary>
        /// 打开制作按下
        /// </summary>
        private void OnOpenCraftStartedCallback(InputAction.CallbackContext context)
        {
            OnOpenCraftStarted?.Invoke();
        }
    }
}
