using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Match3
{
    /// <summary>
    /// 输入读取器。
    /// 负责处理玩家的输入操作（鼠标点击或屏幕触摸），并将其统一转换为屏幕坐标事件。
    /// 使用观察者模式解耦输入检测与游戏逻辑。
    /// </summary>
    [RequireComponent(typeof(PlayerInput))] // 确保挂载 PlayerInput 组件，用于管理输入动作
    public class InputReader : MonoBehaviour
    {
        // PlayerInput 组件引用，用于访问输入系统配置
        private PlayerInput playerInput;
        
        // "Fire" 动作引用，对应鼠标左键或屏幕触摸操作
        private InputAction fireAction;

        /// <summary>
        /// 点击事件。
        /// 当检测到点击操作时触发，传递屏幕坐标 (Vector2)。
        /// 其他脚本（如 Board）可以订阅此事件来获取点击位置。
        /// </summary>
        public event Action<Vector2> FireAt;

        private void Awake()
        {
            // 获取 PlayerInput 组件
            playerInput = GetComponent<PlayerInput>();
            // 获取名为 "Fire" 的输入动作（需在 Input Actions 资源中定义）
            fireAction = playerInput.actions["Fire"];
        }

        // 脚本启用时：开启输入监听并绑定事件
        private void OnEnable()
        {
            playerInput.actions.Enable(); // 启用所有输入动作
            fireAction.performed += OnFire; // 当 "Fire" 动作完成时，调用 OnFire 方法
        }

        // 脚本禁用时：解绑事件，防止内存泄漏
        private void OnDisable()
        {
            fireAction.performed -= OnFire;
        }

        /// <summary>
        /// 处理点击操作。
        /// 区分鼠标和触摸屏设备，获取准确的点击坐标。
        /// </summary>
        private void OnFire(InputAction.CallbackContext ctx)
        {
            Vector2 screenPos = Vector2.zero;

            // 如果是鼠标设备，获取鼠标位置
            if (ctx.control?.device is Mouse mouse)
                screenPos = mouse.position.ReadValue();
            // 如果是触摸屏设备，获取主触摸点位置
            else if (ctx.control?.device is Touchscreen touch)
                screenPos = touch.primaryTouch.position.ReadValue();

            // 触发点击事件，通知订阅者（如棋盘逻辑）
            FireAt?.Invoke(screenPos);
        }
    }
}