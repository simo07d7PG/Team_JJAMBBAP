using System.Collections.Generic;
using UnityEngine;

// 얼음 조각을 미리 만들어 두고 꺼내 쓰고 반납하는 풀
public class IcePiecePool : MonoBehaviour
{
    [Tooltip("얼음 조각 원본 (Rigidbody + Collider + IcePiece)")]
    [SerializeField] private IcePiece template;
    [SerializeField] private int capacity = 60;

    // 지금 꺼내 쓴 얼음 목록. 읽기만 할 것
    [HideInInspector] public List<IcePiece> active = new List<IcePiece>();

    private List<IcePiece> all = new List<IcePiece>();

    private void Awake()
    {
        // 플레이 중에 생성하지 않도록 미리 만들어 둔다
        for (int i = 0; i < capacity; i++)
        {
            IcePiece piece = Instantiate(template, transform);
            piece.gameObject.SetActive(false);
            all.Add(piece);
        }
    }

    // 얼음 하나를 꺼낸다. 다 쓰고 있으면 null
    public IcePiece Spawn(Vector3 position, Quaternion rotation, float sizeFactor)
    {
        for (int i = 0; i < all.Count; i++)
        {
            IcePiece piece = all[i];
            if (piece.gameObject.activeSelf) continue;
            piece.gameObject.SetActive(true);
            piece.ResetPiece(position, rotation, sizeFactor);
            active.Add(piece);
            return piece;
        }
        return null;
    }

    // 꺼낸 얼음을 전부 반납한다
    public void DespawnAll()
    {
        for (int i = 0; i < active.Count; i++)
        {
            active[i].gameObject.SetActive(false);
        }
        active.Clear();
    }
}
