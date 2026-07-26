using System;
using UnityEngine;

namespace Game3.SideDefense
{
    public enum SideDefenseFaction
    {
        Human,
        Monster
    }

    [DisallowMultipleComponent]
    public sealed class SideDefenseSpawnPoint : MonoBehaviour
    {
        [SerializeField] private SideDefenseFaction faction;
        [SerializeField, Min(0)] private int minimumSortingOrder = 20;
        [SerializeField] private Color gizmoColor = Color.white;
        [SerializeField, Min(0.05f)] private float gizmoRadius = 0.35f;

        public SideDefenseFaction Faction => faction;

        public void Configure(
            SideDefenseFaction spawnFaction,
            int spawnedUnitSortingOrder,
            Color markerColor)
        {
            faction = spawnFaction;
            minimumSortingOrder = Mathf.Max(0, spawnedUnitSortingOrder);
            gizmoColor = markerColor;
        }

        public GameObject Spawn(GameObject prefab, Transform parent = null)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            GameObject instance = Instantiate(
                prefab,
                transform.position,
                transform.rotation,
                parent);

            foreach (SpriteRenderer spriteRenderer in instance.GetComponentsInChildren<SpriteRenderer>(true))
            {
                spriteRenderer.sortingOrder = Mathf.Max(
                    spriteRenderer.sortingOrder,
                    minimumSortingOrder);
            }

            return instance;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);

            float direction = faction == SideDefenseFaction.Human ? 1f : -1f;
            Vector3 arrowStart = transform.position;
            Vector3 arrowEnd = arrowStart + Vector3.right * (direction * gizmoRadius * 2.5f);
            Gizmos.DrawLine(arrowStart, arrowEnd);
            Gizmos.DrawLine(
                arrowEnd,
                arrowEnd + new Vector3(-direction, 0.5f, 0f) * gizmoRadius);
            Gizmos.DrawLine(
                arrowEnd,
                arrowEnd + new Vector3(-direction, -0.5f, 0f) * gizmoRadius);
        }
    }
}
