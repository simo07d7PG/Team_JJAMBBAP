using UnityEngine;
using UnityEngine.UI;

// 카페 분위기 증기. 에스프레소 머신·우유 냉장고 윗면에서 흰 반투명 점이 천천히 올라가며 커지고 사라진다.
// CafeView가 붙이고, 화면 위 UI 오버레이로 그린다(씬 변경 없음). 카페 화면일 때만 보인다
public class CafeAmbience : MonoBehaviour
{
    private const int PuffsPerSource = 6;
    private const float MaxAlpha = 0.18f;
    private const float RiseMeters = 0.4f;
    private const float MinSize = 18f;
    private const float MaxSize = 70f;
    private const int TextureSize = 64;

    private static readonly string[] SourceIds = { "espresso", "milk" };

    private class Puff
    {
        public RectTransform rect;
        public Image image;
        public Station source;
        public float age;
        public float life;
        public float drift;
        public float seed;
    }

    private CafeView view;
    private CafeLoop loop;
    private Camera cam;
    private Canvas rootCanvas;
    private RectTransform container;
    private Sprite sprite;
    private Texture2D texture;
    private Puff[] puffs;

    // CafeView에서 넘겨 준다
    public void Init(CafeView owner, Camera camera)
    {
        view = owner;
        cam = camera;
    }

    private void Start()
    {
        if (view == null) view = GetComponent<CafeView>();
        if (cam == null) cam = GetComponent<Camera>();
        loop = FindFirstObjectByType<CafeLoop>();
        if (loop == null || loop.dayText == null || loop.stations == null) { enabled = false; return; }
        rootCanvas = loop.dayText.canvas.rootCanvas;
        if (rootCanvas == null) { enabled = false; return; }

        System.Collections.Generic.List<Station> sources = new System.Collections.Generic.List<Station>();
        for (int i = 0; i < loop.stations.Length; i++)
        {
            Station s = loop.stations[i];
            if (s == null) continue;
            for (int k = 0; k < SourceIds.Length; k++)
            {
                if (s.stationId == SourceIds[k]) { sources.Add(s); break; }
            }
        }
        if (sources.Count == 0) { enabled = false; return; }

        BuildSprite();
        GameObject root = new GameObject("CafeAmbience", typeof(RectTransform));
        container = root.GetComponent<RectTransform>();
        container.SetParent(rootCanvas.transform, false);
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.offsetMin = Vector2.zero;
        container.offsetMax = Vector2.zero;
        // HUD 뒤에 깔리게 맨 아래로
        container.SetAsFirstSibling();

        puffs = new Puff[sources.Count * PuffsPerSource];
        for (int i = 0; i < puffs.Length; i++)
        {
            GameObject go = new GameObject("Puff", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(container, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, 0f);
            Puff p = new Puff();
            p.rect = rt;
            p.image = img;
            p.source = sources[i / PuffsPerSource];
            p.life = Random.Range(2.8f, 4.0f);
            // 처음에는 나이를 어긋나게 해서 한꺼번에 나오지 않게
            p.age = p.life * (i % PuffsPerSource) / PuffsPerSource;
            p.drift = Random.Range(-0.04f, 0.04f);
            p.seed = Random.Range(0f, 6.28f);
            puffs[i] = p;
        }
    }

    // 가장자리가 부드러운 흰 원
    private void BuildSprite()
    {
        texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        float c = (TextureSize - 1) * 0.5f;
        Color32[] px = new Color32[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01((1f - d) / 0.45f);
                px[y * TextureSize + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
            }
        }
        texture.SetPixels32(px);
        texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), 100f);
    }

    private void OnDestroy()
    {
        if (container != null) Destroy(container.gameObject);
        if (sprite != null) Destroy(sprite);
        if (texture != null) Destroy(texture);
    }

    private void LateUpdate()
    {
        if (puffs == null) return;
        bool show = view != null && view.viewEnabled && Time.timeScale > 0f
            && cam != null && cam.enabled && cam.gameObject.activeInHierarchy;
        if (container.gameObject.activeSelf != show) container.gameObject.SetActive(show);
        if (!show) return;

        float dt = Time.deltaTime;
        float scale = rootCanvas.scaleFactor;
        if (scale <= 0f) scale = 1f;
        for (int i = 0; i < puffs.Length; i++)
        {
            Puff p = puffs[i];
            if (p.source == null || !p.source.gameObject.activeInHierarchy)
            {
                p.image.color = new Color(1f, 1f, 1f, 0f);
                continue;
            }
            p.age += dt;
            if (p.age >= p.life)
            {
                p.age -= p.life;
                p.life = Random.Range(2.8f, 4.0f);
                p.drift = Random.Range(-0.04f, 0.04f);
                p.seed = Random.Range(0f, 6.28f);
            }
            float u = p.age / p.life;
            Vector3 world = SourceTop(p.source);
            world.y += RiseMeters * u;
            world += cam.transform.right * (p.drift * u + Mathf.Sin(p.seed + u * 3f) * 0.015f);
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0.05f || sp.x < -100f || sp.y < -100f || sp.x > Screen.width + 100f || sp.y > Screen.height + 100f)
            {
                p.image.color = new Color(1f, 1f, 1f, 0f);
                continue;
            }
            // 가까울수록 크게 (거리 2m 기준)
            float size = Mathf.Lerp(MinSize, MaxSize, u) * Mathf.Clamp(2f / sp.z, 0.5f, 2f);
            p.rect.sizeDelta = new Vector2(size, size);
            p.rect.anchoredPosition = new Vector2(sp.x, sp.y) / scale;
            // 나타났다 사라지는 종 모양 알파
            float a = MaxAlpha * Mathf.Sin(u * Mathf.PI);
            p.image.color = new Color(1f, 1f, 1f, a);
        }
    }

    // 기계 윗면 가운데 (보이는 렌더러 → 콜라이더 순)
    private static Vector3 SourceTop(Station s)
    {
        Bounds b = new Bounds();
        bool has = false;
        if (s.targets != null)
        {
            for (int i = 0; i < s.targets.Length; i++)
            {
                Renderer r = s.targets[i];
                if (r == null) continue;
                if (!has) b = r.bounds;
                else b.Encapsulate(r.bounds);
                has = true;
            }
        }
        if (!has)
        {
            Collider c = s.GetComponentInChildren<Collider>();
            if (c == null) return s.transform.position;
            b = c.bounds;
        }
        return new Vector3(b.center.x, b.max.y, b.center.z);
    }
}
