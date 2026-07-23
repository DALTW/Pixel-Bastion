using UnityEngine;
using UnityEngine.InputSystem;

namespace Game3.Hunting
{
    public sealed class HuntingInput : MonoBehaviour
    {
        private InputActionMap map;
        private InputAction move;
        private InputAction aim;
        private InputAction fire;
        private InputAction reload;
        private InputAction interact;
        private InputAction cancel;

        public Vector2 Move => move?.ReadValue<Vector2>() ?? Vector2.zero;
        public Vector2 PointerScreen => aim?.ReadValue<Vector2>() ?? Vector2.zero;
        public bool FireHeld => fire?.IsPressed() == true;
        public bool FirePressed => fire?.WasPressedThisFrame() == true;
        public bool ReloadPressed => reload?.WasPressedThisFrame() == true;
        public bool InteractHeld => interact?.IsPressed() == true;
        public bool InteractPressed => interact?.WasPressedThisFrame() == true;
        public bool CancelPressed => cancel?.WasPressedThisFrame() == true;

        private void Awake()
        {
            map = new InputActionMap("Hunting");
            move = map.AddAction("Move", InputActionType.Value);
            var movement = move.AddCompositeBinding("2DVector");
            movement.With("Up", "<Keyboard>/w");
            movement.With("Down", "<Keyboard>/s");
            movement.With("Left", "<Keyboard>/a");
            movement.With("Right", "<Keyboard>/d");
            var arrows = move.AddCompositeBinding("2DVector");
            arrows.With("Up", "<Keyboard>/upArrow");
            arrows.With("Down", "<Keyboard>/downArrow");
            arrows.With("Left", "<Keyboard>/leftArrow");
            arrows.With("Right", "<Keyboard>/rightArrow");

            aim = map.AddAction("Aim", InputActionType.PassThrough, "<Pointer>/position");
            fire = map.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            reload = map.AddAction("Reload", InputActionType.Button, "<Keyboard>/r");
            interact = map.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
            cancel = map.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
        }

        private void OnEnable() => map?.Enable();
        private void OnDisable() => map?.Disable();

        public bool WeaponSlotPressed(int oneBasedSlot)
        {
            if (Keyboard.current == null)
            {
                return false;
            }

            return oneBasedSlot switch
            {
                1 => Keyboard.current.digit1Key.wasPressedThisFrame,
                2 => Keyboard.current.digit2Key.wasPressedThisFrame,
                3 => Keyboard.current.digit3Key.wasPressedThisFrame,
                4 => Keyboard.current.digit4Key.wasPressedThisFrame,
                _ => false
            };
        }
    }
}
