using System.Collections.Generic;
using UnityEngine;

namespace PhysicsSandbox.Visualization
{
    /// <summary>
    /// Reusable arrow object pool to avoid instantiate/destroy churn while field vectors update.
    /// </summary>
    public sealed class FieldArrowPool : MonoBehaviour
    {
        [SerializeField] private Transform arrowPrefab;
        [SerializeField, Min(0)] private int prewarmCount = 64;

        private readonly Queue<Transform> pool = new();

        private void Awake()
        {
            Prewarm();
        }

        private void Prewarm()
        {
            for (int i = 0; i < prewarmCount; i++)
            {
                Transform arrow = Instantiate(arrowPrefab, transform);
                arrow.gameObject.SetActive(false);
                pool.Enqueue(arrow);
            }
        }

        public Transform Rent()
        {
            Transform arrow = pool.Count > 0 ? pool.Dequeue() : Instantiate(arrowPrefab, transform);
            arrow.gameObject.SetActive(true);
            return arrow;
        }

        public void Return(Transform arrow)
        {
            arrow.gameObject.SetActive(false);
            arrow.SetParent(transform, false);
            pool.Enqueue(arrow);
        }
    }
}
