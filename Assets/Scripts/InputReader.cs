using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Match3
{
    [RequireComponent(typeof(PlayerInput))]
    public class InputReader : MonoBehaviour
    {
        private PlayerInput playerInput;
        private InputAction fireAction;

        public event Action<Vector2> FireAt;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            fireAction = playerInput.actions["Fire"];
        }

        private void OnEnable()
        {
            playerInput.actions.Enable();
            fireAction.performed += OnFire;
        }

        private void OnDisable()
        {
            fireAction.performed -= OnFire;
        }

        private void OnFire(InputAction.CallbackContext ctx)
        {
            Vector2 screenPos = Vector2.zero;

            if (ctx.control?.device is Mouse mouse)
                screenPos = mouse.position.ReadValue();
            else if (ctx.control?.device is Touchscreen touch)
                screenPos = touch.primaryTouch.position.ReadValue();

            FireAt?.Invoke(screenPos);
        }
    }
}