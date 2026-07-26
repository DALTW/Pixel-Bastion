using System;
using UnityEngine;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    public sealed class SideDefenseMapLayout : MonoBehaviour
    {
        [Header("Scene-editable Map Pieces")]
        [SerializeField] private SpriteRenderer[] mapSegments = Array.Empty<SpriteRenderer>();

        [Header("Base and Spawn Points")]
        [SerializeField] private Transform alliedTower;
        [SerializeField] private SideDefenseSpawnPoint humanSpawnPoint;
        [SerializeField] private SideDefenseSpawnPoint monsterSpawnPoint;
        [SerializeField] private Transform spawnedUnitsRoot;

        [Header("World Helpers")]
        [SerializeField] private HorizontalCameraController cameraController;
        [SerializeField] private BoxCollider2D groundCollider;
        [SerializeField] private float worldLeft;
        [SerializeField] private float worldRight;

        public SpriteRenderer[] MapSegments => mapSegments;
        public Transform AlliedTower => alliedTower;
        public SideDefenseSpawnPoint HumanSpawnPoint => humanSpawnPoint;
        public SideDefenseSpawnPoint MonsterSpawnPoint => monsterSpawnPoint;
        public HorizontalCameraController CameraController => cameraController;
        public float WorldLeft => worldLeft;
        public float WorldRight => worldRight;

        public void Configure(
            SpriteRenderer[] editableMapSegments,
            Transform tower,
            SideDefenseSpawnPoint humanSpawn,
            SideDefenseSpawnPoint monsterSpawn,
            Transform unitsRoot,
            HorizontalCameraController horizontalCamera,
            BoxCollider2D ground)
        {
            mapSegments = editableMapSegments ?? Array.Empty<SpriteRenderer>();
            alliedTower = tower;
            humanSpawnPoint = humanSpawn;
            monsterSpawnPoint = monsterSpawn;
            spawnedUnitsRoot = unitsRoot;
            cameraController = horizontalCamera;
            groundCollider = ground;
            RefreshWorldBounds();
        }

        public GameObject SpawnHuman(GameObject humanPrefab)
        {
            if (humanSpawnPoint == null)
            {
                throw new InvalidOperationException("Human spawn point is not assigned.");
            }

            return humanSpawnPoint.Spawn(humanPrefab, spawnedUnitsRoot);
        }

        public GameObject SpawnMonster(GameObject monsterPrefab)
        {
            if (monsterSpawnPoint == null)
            {
                throw new InvalidOperationException("Monster spawn point is not assigned.");
            }

            return monsterSpawnPoint.Spawn(monsterPrefab, spawnedUnitsRoot);
        }

        [ContextMenu("Align Map Segments Left To Right")]
        public void AlignMapSegmentsLeftToRight()
        {
            if (mapSegments == null || mapSegments.Length == 0)
            {
                return;
            }

            for (int index = 1; index < mapSegments.Length; index++)
            {
                SpriteRenderer previous = mapSegments[index - 1];
                SpriteRenderer current = mapSegments[index];
                if (previous == null || current == null)
                {
                    continue;
                }

                float horizontalShift = previous.bounds.max.x - current.bounds.min.x;
                current.transform.position += Vector3.right * horizontalShift;
            }

            RefreshWorldBounds();
        }

        [ContextMenu("Refresh Camera And Ground Bounds")]
        public void RefreshWorldBounds()
        {
            if (mapSegments == null || mapSegments.Length == 0)
            {
                return;
            }

            worldLeft = float.PositiveInfinity;
            worldRight = float.NegativeInfinity;

            foreach (SpriteRenderer segment in mapSegments)
            {
                if (segment == null)
                {
                    continue;
                }

                worldLeft = Mathf.Min(worldLeft, segment.bounds.min.x);
                worldRight = Mathf.Max(worldRight, segment.bounds.max.x);
            }

            if (float.IsInfinity(worldLeft) || float.IsInfinity(worldRight))
            {
                worldLeft = 0f;
                worldRight = 0f;
                return;
            }

            if (cameraController != null)
            {
                cameraController.SetWorldBounds(worldLeft, worldRight);
            }

            if (groundCollider != null)
            {
                Transform groundTransform = groundCollider.transform;
                Vector3 groundPosition = groundTransform.position;
                groundPosition.x = (worldLeft + worldRight) * 0.5f;
                groundTransform.position = groundPosition;

                Vector2 colliderSize = groundCollider.size;
                colliderSize.x = worldRight - worldLeft;
                groundCollider.size = colliderSize;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.85f);
            Gizmos.DrawLine(
                new Vector3(worldLeft, -2.55f, 0f),
                new Vector3(worldRight, -2.55f, 0f));
        }
    }
}
