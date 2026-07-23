using UnityEngine;
using UnityEngine.InputSystem;

namespace Game3.Hunting
{
    public sealed class HuntingInput : MonoBehaviour
    {
        private InputActionMap map;
        private InputAction move;
        private InputAction attack;
        private InputAction interact;
        private InputAction cancel;

        public Vector2 Move => move?.ReadValue<Vector2>() ?? Vector2.zero;
        public bool AttackPressed => attack?.WasPressedThisFrame() == true;
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

            attack = map.AddAction("Attack", InputActionType.Button, "<Keyboard>/space");
            interact = map.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
            cancel = map.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
        }

        private void OnEnable() => map?.Enable();
        private void OnDisable() => map?.Disable();
    }
}
