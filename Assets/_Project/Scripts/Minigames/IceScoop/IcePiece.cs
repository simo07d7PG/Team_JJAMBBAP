using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>얼음 조각 하나. 풀에서 꺼내 쓰고 반납한다.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class IcePiece : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        /// <summary>바닥에 떨어진 것으로 이미 보고했는지.</summary>
        public bool Dropped { get; set; }

        private void Awake() => Body = GetComponent<Rigidbody>();

        public void ResetPiece(Vector3 position, Quaternion rotation)
        {
            if (Body == null) Body = GetComponent<Rigidbody>();
            Dropped = false;
            transform.SetPositionAndRotation(position, rotation);
            Body.position = position;
            Body.rotation = rotation;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
        }
    }
}
