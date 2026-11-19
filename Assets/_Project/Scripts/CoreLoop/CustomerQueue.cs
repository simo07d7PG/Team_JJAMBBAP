using UnityEngine;

// 줄 서기. 현재 손님 뒤에 다음 손님 최대 2명이 겉모습만 서서 기다린다 (클릭·말풍선 없음).
// 손님 모델(Renderer가 붙은 자식)을 한 번만 복제해 2개를 재사용한다.
public class CustomerQueue : MonoBehaviour
{
    private const int MaxWaiting = 2;
    private const float Spacing = 0.55f;
    private const float MoveSeconds = 0.5f;
    private const float IdleAmp = 0.01f;
    private const float IdlePeriod = 2.2f;
    private const float IdleTilt = 1.2f;
    // Customer.enterOffset 기본값 방향(카운터 자리 기준). 줄은 손님이 들어오는 쪽으로 선다
    private static readonly Vector3 QueueDir = new Vector3(-2.2f, 0f, -0.6f).normalized;
    private static readonly Color[] Tints =
    {
        new Color(0.70f, 0.50f, 0.80f), new Color(0.45f, 0.75f, 0.72f), new Color(0.92f, 0.75f, 0.35f),
        new Color(0.85f, 0.45f, 0.40f), new Color(0.40f, 0.65f, 0.85f),
    };

    private CafeLoop loop;
    private Transform guestParent;
    private Transform[] roots;
    // 줄 순서: order[0]이 맨 앞
    private int[] order = { 0, 1 };
    private float[] slot = new float[MaxWaiting];
    private Renderer[][] renderers;
    private MaterialPropertyBlock block;
    private Vector3 restPos;
    private Quaternion restRot = Quaternion.identity;
    private Vector3 restScale = Vector3.one;
    private bool built;
    private int lastToday = -1;
    private int tintShift;

    public void Init(CafeLoop cafeLoop)
    {
        loop = cafeLoop;
    }

    // 손님이 꺼져 있을 때의 원래 자리·회전·크기를 읽는다 (Hide가 원래 값으로 되돌려 둔다)
    private void CaptureRest(Transform c)
    {
        restPos = c.localPosition;
        restRot = c.localRotation;
        restScale = c.localScale;
    }

    private void Build(Customer c)
    {
        built = true;
        guestParent = c.transform.parent;
        CaptureRest(c.transform);
        roots = new Transform[MaxWaiting];
        renderers = new Renderer[MaxWaiting][];
        block = new MaterialPropertyBlock();
        Transform src = c.transform;
        for (int i = 0; i < MaxWaiting; i++)
        {
            GameObject root = new GameObject("QueueGuest" + i);
            Transform rt = root.transform;
            rt.SetParent(guestParent, false);
            for (int k = 0; k < src.childCount; k++)
            {
                Transform child = src.GetChild(k);
                if (c.bubble != null && (child == c.bubble || c.bubble.IsChildOf(child))) continue;
                if (child.GetComponentInChildren<Renderer>(true) == null) continue;
                GameObject copy = Instantiate(child.gameObject, rt, false);
                copy.name = child.name;
                copy.transform.localPosition = child.localPosition;
                copy.transform.localRotation = child.localRotation;
                copy.transform.localScale = child.localScale;
                copy.SetActive(true);
                Strip(copy);
            }
            renderers[i] = root.GetComponentsInChildren<Renderer>(true);
            roots[i] = rt;
            root.SetActive(false);
            slot[i] = i + 1;
        }
    }

    // 클릭·말풍선·스크립트가 없는 겉모습만 남긴다
    private static void Strip(GameObject go)
    {
        foreach (Collider col in go.GetComponentsInChildren<Collider>(true)) Destroy(col);
        foreach (Canvas cv in go.GetComponentsInChildren<Canvas>(true)) Destroy(cv.gameObject);
        foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null) Destroy(mb);
        }
    }

    private void Update()
    {
        if (loop == null || loop.customer == null) return;
        Customer c = loop.customer;
        if (!built) Build(c);
        if (guestParent == null) return;
        if (!c.gameObject.activeSelf) CaptureRest(c.transform);

        int today = loop.CustomersToday;
        if (lastToday < 0 || today < lastToday) Snap();
        else if (today > lastToday) Advance();
        lastToday = today;

        int waiting = Mathf.Clamp(loop.CustomersPerDayCount - today, 0, MaxWaiting);
        bool visible = !loop.DayOver && waiting > 0
            && !(loop.receiptPanel != null && loop.receiptPanel.activeSelf)
            && !(loop.shop != null && loop.shop.panel != null && loop.shop.panel.activeSelf);

        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;
        Vector3 dir = restRot * QueueDir;
        for (int idx = 0; idx < MaxWaiting; idx++)
        {
            int g = order[idx];
            Transform rt = roots[g];
            bool show = visible && idx < waiting;
            if (rt.gameObject.activeSelf != show) rt.gameObject.SetActive(show);
            if (!show)
            {
                slot[g] = idx + 1;
                continue;
            }
            slot[g] = Mathf.MoveTowards(slot[g], idx + 1, dt / MoveSeconds * Mathf.Max(1f, Mathf.Abs(slot[g] - (idx + 1))));
            float ph = t * Mathf.PI * 2f / IdlePeriod + g * 1.7f;
            Vector3 pos = restPos + QueueDir * 0f + (restRot * (QueueDir * (Spacing * slot[g])));
            pos.y += Mathf.Sin(ph) * IdleAmp;
            rt.localPosition = pos;
            rt.localRotation = restRot * Quaternion.Euler(0f, 0f, Mathf.Sin(ph * 0.5f) * IdleTilt);
            rt.localScale = restScale;
        }
        // 색은 켜질 때마다 바뀌지 않게 슬롯 번호로 고정
        ApplyTints();
    }

    private bool tintApplied;

    private void ApplyTints()
    {
        if (tintApplied) return;
        tintApplied = true;
        for (int i = 0; i < MaxWaiting; i++)
        {
            Color col = Tints[(i + tintShift) % Tints.Length];
            Color mixed = Color.Lerp(Color.white, col, 0.4f);
            block.Clear();
            block.SetColor("_BaseColor", mixed);
            block.SetColor("_Color", mixed);
            Renderer[] rs = renderers[i];
            for (int r = 0; r < rs.Length; r++)
            {
                if (rs[r] != null) rs[r].SetPropertyBlock(block);
            }
        }
    }

    private void Snap()
    {
        order[0] = 0;
        order[1] = 1;
        for (int i = 0; i < MaxWaiting; i++) slot[i] = i + 1;
    }

    // 맨 앞 대기 손님은 사라지고(실제 손님이 등장) 나머지는 한 칸 앞으로, 사라진 모델은 맨 뒤로 재사용
    private void Advance()
    {
        int front = order[0];
        for (int i = 0; i < MaxWaiting - 1; i++) order[i] = order[i + 1];
        order[MaxWaiting - 1] = front;
        slot[front] = MaxWaiting + 1;
    }
}
