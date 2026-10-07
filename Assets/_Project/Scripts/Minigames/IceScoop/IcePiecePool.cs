using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace BariBarista.Minigames
{
    /// <summary>
    /// 얼음 조각 생성·반납을 한곳에 모은 풀. 나중에 공용 PoolService로 바꿀 때 이 클래스만 고치면 된다.
    /// </summary>
    public class IcePiecePool : MonoBehaviour
    {
        [Tooltip("얼음 조각 원본 (Rigidbody + Collider + IcePiece). 비우면 기본 큐브를 만든다")]
        [SerializeField] private IcePiece template;
        [SerializeField] private int capacity = 60;
        [Tooltip("기본 큐브를 만들 때의 크기")]
        [SerializeField] private float fallbackSize = 0.085f;

        private ObjectPool<IcePiece> pool;
        private List<IcePiece> active;

        public int Capacity => capacity;
        public int ActiveCount => active != null ? active.Count : 0;
        /// <summary>지금 꺼내 쓴 얼음 목록. 읽기만 할 것.</summary>
        public IReadOnlyList<IcePiece> Active { get { EnsureInit(); return active; } }

        public void EnsureInit()
        {
            if (pool != null) return;
            active = new List<IcePiece>(capacity);
            pool = new ObjectPool<IcePiece>(Create, OnGet, OnRelease, OnDestroyPiece,
                collectionCheck: false, defaultCapacity: capacity, maxSize: capacity);

            // 러시 중에 생성하지 않도록 미리 만들어 둔다
            var warm = new List<IcePiece>(capacity);
            for (int i = 0; i < capacity; i++) warm.Add(pool.Get());
            for (int i = 0; i < warm.Count; i++) pool.Release(warm[i]);
            active.Clear();
        }

        /// <summary>얼음 하나를 꺼낸다. 다 쓰고 있으면 null.</summary>
        public IcePiece Spawn(Vector3 position, Quaternion rotation, float sizeFactor = 1f)
        {
            EnsureInit();
            if (active.Count >= capacity) return null;
            IcePiece piece = pool.Get();
            piece.ResetPiece(position, rotation, sizeFactor);
            return piece;
        }

        public void Despawn(IcePiece piece)
        {
            if (piece == null || !active.Contains(piece)) return;
            pool.Release(piece);
        }

        /// <summary>꺼낸 얼음을 전부 반납한다.</summary>
        public void DespawnAll()
        {
            EnsureInit();
            for (int i = active.Count - 1; i >= 0; i--) pool.Release(active[i]);
            active.Clear();
        }

        private IcePiece Create()
        {
            IcePiece piece;
            if (template != null)
            {
                piece = Instantiate(template, transform);
            }
            else
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Ice";
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one * fallbackSize;
                var rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.05f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                piece = go.AddComponent<IcePiece>();
            }
            piece.gameObject.SetActive(false);
            return piece;
        }

        private void OnGet(IcePiece piece)
        {
            piece.gameObject.SetActive(true);
            active.Add(piece);
        }

        private void OnRelease(IcePiece piece)
        {
            if (piece.Body != null && !piece.Body.isKinematic)
            {
                piece.Body.linearVelocity = Vector3.zero;
                piece.Body.angularVelocity = Vector3.zero;
            }
            piece.gameObject.SetActive(false);
            active.Remove(piece);
        }

        private static void OnDestroyPiece(IcePiece piece)
        {
            if (piece != null) Destroy(piece.gameObject);
        }
    }
}
