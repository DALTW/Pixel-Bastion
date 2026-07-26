using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class HorizontalCameraController : MonoBehaviour
    {
        [Header("Horizontal Scrolling")]
        [SerializeField, Min(0.1f)] private float keyboardPanSpeed = 12f;
        [SerializeField, Min(0.01f)] private float dragSensitivity = 1f;
        [SerializeField] private bool allowKeyboardPan = true;
        [SerializeField] private bool allowMouseDrag = true;
        [SerializeField] private bool allowMiddleMouseDrag = true;
        [SerializeField] private bool ignoreDragWhenPointerOverUi = true;

        [Header("Editable World Bounds")]
        [SerializeField] private float worldLeft;
        [SerializeField] private float worldRight = 20f;

        private Camera controlledCamera;
        private float targetX;
        private Vector2 previousPointerPosition;
        private bool pointerDragging;
        private int activeDragButton = -1;

        public float WorldLeft => worldLeft;
        public float WorldRight => worldRight;

        public void SetWorldBounds(float left, float right)
        {
            worldLeft = Mathf.Min(left, right);
            worldRight = Mathf.Max(left, right);
            targetX = ClampCenterX(targetX);
            ApplyTargetPosition();
        }

        public void MoveToLeftEdge()
        {
            targetX = MinimumCenterX();
            ApplyTargetPosition();
        }

        public void MoveToRightEdge()
        {
            targetX = MaximumCenterX();
            ApplyTargetPosition();
        }

        public void FocusWorldX(float worldX)
        {
            targetX = ClampCenterX(worldX);
            ApplyTargetPosition();
        }

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
            targetX = transform.position.x;
        }

        private void OnEnable()
        {
            if (controlledCamera == null)
            {
                controlledCamera = GetComponent<Camera>();
            }

            targetX = ClampCenterX(transform.position.x);
            ApplyTargetPosition();
        }

        private void Update()
        {
            float keyboardDirection = ReadKeyboardDirection();
            targetX += keyboardDirection * keyboardPanSpeed * Time.unscaledDeltaTime;
            ReadPointerDrag();
            targetX = ClampCenterX(targetX);
            ApplyTargetPosition();
        }

        private void OnDisable()
        {
            pointerDragging = false;
            activeDragButton = -1;
        }

        private float ReadKeyboardDirection()
        {
            if (!allowKeyboardPan)
            {
                return 0f;
            }

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return 0f;
            }

            float direction = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                direction -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                direction += 1f;
            }

            return direction;
#else
            return Input.GetAxisRaw("Horizontal");
#endif
        }

        private void ReadPointerDrag()
        {
            if (!allowMouseDrag)
            {
                pointerDragging = false;
                activeDragButton = -1;
                return;
            }

#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                pointerDragging = false;
                activeDragButton = -1;
                return;
            }

            Vector2 pointerPosition = mouse.position.ReadValue();
            bool leftPressed = mouse.leftButton.wasPressedThisFrame;
            bool middlePressed =
                allowMiddleMouseDrag && mouse.middleButton.wasPressedThisFrame;

            if ((leftPressed || middlePressed) && CanBeginPointerDrag())
            {
                pointerDragging = true;
                activeDragButton = middlePressed ? 2 : 0;
                previousPointerPosition = pointerPosition;
            }

            bool activeButtonHeld =
                activeDragButton == 0
                    ? mouse.leftButton.isPressed
                    : activeDragButton == 2 && mouse.middleButton.isPressed;

            if (pointerDragging && activeButtonHeld)
            {
                ApplyPointerDelta(pointerPosition - previousPointerPosition);
                previousPointerPosition = pointerPosition;
            }

            if (pointerDragging && !activeButtonHeld)
            {
                pointerDragging = false;
                activeDragButton = -1;
            }
#else
            Vector2 pointerPosition = Input.mousePosition;
            bool leftPressed = Input.GetMouseButtonDown(0);
            bool middlePressed =
                allowMiddleMouseDrag && Input.GetMouseButtonDown(2);

            if ((leftPressed || middlePressed) && CanBeginPointerDrag())
            {
                pointerDragging = true;
                activeDragButton = middlePressed ? 2 : 0;
                previousPointerPosition = pointerPosition;
            }

            bool activeButtonHeld =
                activeDragButton == 0
                    ? Input.GetMouseButton(0)
                    : activeDragButton == 2 && Input.GetMouseButton(2);

            if (pointerDragging && activeButtonHeld)
            {
                ApplyPointerDelta(pointerPosition - previousPointerPosition);
                previousPointerPosition = pointerPosition;
            }

            if (pointerDragging && !activeButtonHeld)
            {
                pointerDragging = false;
                activeDragButton = -1;
            }
#endif
        }

        private bool CanBeginPointerDrag()
        {
            return !ignoreDragWhenPointerOverUi ||
                   EventSystem.current == null ||
                   !EventSystem.current.IsPointerOverGameObject();
        }

        private void ApplyPointerDelta(Vector2 pixelDelta)
        {
            float screenHeight = Mathf.Max(1f, Screen.height);
            float worldUnitsPerPixel = controlledCamera.orthographicSize * 2f / screenHeight;
            targetX -= pixelDelta.x * worldUnitsPerPixel * dragSensitivity;
        }

        private float MinimumCenterX()
        {
            float halfWidth = CameraHalfWidth();
            float minimum = worldLeft + halfWidth;
            float maximum = worldRight - halfWidth;
            return minimum <= maximum ? minimum : (worldLeft + worldRight) * 0.5f;
        }

        private float MaximumCenterX()
        {
            float halfWidth = CameraHalfWidth();
            float minimum = worldLeft + halfWidth;
            float maximum = worldRight - halfWidth;
            return minimum <= maximum ? maximum : (worldLeft + worldRight) * 0.5f;
        }

        private float ClampCenterX(float worldX)
        {
            return Mathf.Clamp(worldX, MinimumCenterX(), MaximumCenterX());
        }

        private float CameraHalfWidth()
        {
            if (controlledCamera == null)
            {
                controlledCamera = GetComponent<Camera>();
            }

            return controlledCamera.orthographicSize * controlledCamera.aspect;
        }

        private void ApplyTargetPosition()
        {
            Vector3 position = transform.position;
            position.x = targetX;
            transform.position = position;
        }
    }
}
