using UnityEngine;

namespace Game3.Hunting
{
    public sealed class SmoothCameraFollow : MonoBehaviour
    {
        private Transform target;
        private Vector2 halfWorldSize;
        private Vector3 velocity;

        public void Initialize(Transform followTarget, Vector2 worldSize)
        {
            target = followTarget;
            halfWorldSize = worldSize * 0.5f;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desired = new Vector3(target.position.x, target.position.y, transform.position.z);
            var camera = GetComponent<Camera>();
            if (camera != null)
            {
                var vertical = camera.orthographicSize;
                var horizontal = vertical * camera.aspect;
                desired.x = Mathf.Clamp(desired.x, -halfWorldSize.x + horizontal, halfWorldSize.x - horizontal);
                desired.y = Mathf.Clamp(desired.y, -halfWorldSize.y + vertical, halfWorldSize.y - vertical);
            }

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 0.16f);
        }
    }
}
