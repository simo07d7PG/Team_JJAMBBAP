using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상점 물건 칸 하나. CafeShop이 원본을 복제해 채운다. 버튼 OnClick → OnBuy
// NEW 띠·맥동·어둡게·구매 번쩍임은 모두 코드로 런타임에 만든다 (씬 수정 없이)
public class ShopCard : MonoBehaviour
{
    [HideInInspector] public CafeShop shop;
    [HideInInspector] public int index;

    public Image background;
    public Image icon;
    public GameObject lockIcon;
    public TMP_Text kindText;
    public TMP_Text titleText;
    public TMP_Text descText;
    public TMP_Text priceText;
    public Button button;
    public TMP_Text buttonLabel;

    // 맥동: 배경 알파가 0.85↔1 사이를 오간다
    private const float PulseMinAlpha = 0.85f;
    private const float PulsePeriod = 2.4f;
    // 구매 튕김: 1.0→1.15→1.0
    private const float BounceScale = 1.15f;
    private const float BounceSeconds = 0.25f;
    // 구매 번쩍임: 금색 알파 0.5→0
    private const float FlashStartAlpha = 0.5f;
    private static readonly Color FlashColor = new Color(1f, 0.82f, 0.2f);
    private static readonly Color BadgeColor = new Color(0.86f, 0.15f, 0.15f);
    private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.35f);

    private Color baseColor = Color.white;
    private bool pulsing;
    private Image badge;
    private Image dim;
    private Image flash;
    private float bounceStart = -1f;
    private Vector3 bounceBaseScale = Vector3.one;

    // 상태에 맞춰 배경색·NEW 띠·맥동·어둡게를 정한다 (CafeShop.FillCard가 부른다)
    public void SetVisualState(Color color, bool isNew, bool canPulse, bool isDimmed)
    {
        baseColor = color;
        pulsing = canPulse;
        if (!canPulse) background.color = color;
        if (isNew) EnsureBadge();
        if (badge != null) badge.gameObject.SetActive(isNew);
        if (isDimmed) EnsureDim();
        if (dim != null) dim.gameObject.SetActive(isDimmed);
        // 어둡게 덮는 판이 NEW 띠 위로 올라오지 않게 띠를 맨 위로
        if (badge != null && isNew) badge.transform.SetAsLastSibling();
    }

    // 구매 성공 순간: 튕기고 금색으로 번쩍인다
    public void PlayBuyEffect()
    {
        EnsureFlash();
        RectTransform rect = (RectTransform)transform;
        if (bounceStart < 0f) bounceBaseScale = rect.localScale;
        bounceStart = Time.unscaledTime;
        flash.transform.SetAsLastSibling();
        flash.gameObject.SetActive(true);
        SetAlpha(flash, FlashStartAlpha);
    }

    private void Update()
    {
        if (pulsing && background != null)
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (2f * Mathf.PI / PulsePeriod));
            Color c = baseColor;
            c.a = baseColor.a * Mathf.Lerp(PulseMinAlpha, 1f, wave);
            background.color = c;
        }

        if (bounceStart >= 0f)
        {
            float t = (Time.unscaledTime - bounceStart) / BounceSeconds;
            RectTransform rect = (RectTransform)transform;
            if (t >= 1f)
            {
                rect.localScale = bounceBaseScale;
                bounceStart = -1f;
                if (flash != null) flash.gameObject.SetActive(false);
            }
            else
            {
                rect.localScale = bounceBaseScale * (1f + (BounceScale - 1f) * Mathf.Sin(t * Mathf.PI));
                if (flash != null) SetAlpha(flash, FlashStartAlpha * (1f - t));
            }
        }
    }

    private void OnDisable()
    {
        // 상점이 닫히며 꺼지면 튕김·번쩍임을 마무리한다
        if (bounceStart >= 0f)
        {
            ((RectTransform)transform).localScale = bounceBaseScale;
            bounceStart = -1f;
        }
        if (flash != null) flash.gameObject.SetActive(false);
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    // 카드 전체를 덮는 장식 Image (클릭 막지 않고, 레이아웃에서 제외)
    private Image CreateOverlay(string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(transform, false);
        go.GetComponent<LayoutElement>().ignoreLayout = true;
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void EnsureDim()
    {
        if (dim != null) return;
        dim = CreateOverlay("DimOverlay", DimColor);
    }

    private void EnsureFlash()
    {
        if (flash != null) return;
        flash = CreateOverlay("BuyFlash", new Color(FlashColor.r, FlashColor.g, FlashColor.b, 0f));
        flash.gameObject.SetActive(false);
    }

    // 우상단 'NEW' 붉은 띠 (살짝 기울임)
    private void EnsureBadge()
    {
        if (badge != null) return;
        GameObject go = new GameObject("NewBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(transform, false);
        go.GetComponent<LayoutElement>().ignoreLayout = true;
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(96f, 36f);
        rect.anchoredPosition = new Vector2(8f, 8f);
        rect.localRotation = Quaternion.Euler(0f, 0f, -10f);
        badge = go.GetComponent<Image>();
        badge.color = BadgeColor;
        badge.raycastTarget = false;

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        TextMeshProUGUI label = textGo.GetComponent<TextMeshProUGUI>();
        if (titleText != null) label.font = titleText.font;
        label.text = "NEW";
        label.fontSize = 24f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    public void OnBuy()
    {
        shop.Buy(index);
    }
}
