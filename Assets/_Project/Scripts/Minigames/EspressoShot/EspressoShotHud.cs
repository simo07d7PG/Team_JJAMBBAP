using TMPro;
using UnityEngine;

// 에스프레소 샷 화면. 오른쪽 세로 "샷 잔" 게이지(목표 띠 + "여기서 떼!") + 숫자 ml. 표시만 한다
public class EspressoShotHud : MonoBehaviour
{
    [Header("게이지")]
    [Tooltip("채움. anchorMax.y = 값")]
    [SerializeField] private RectTransform fill;
    [Tooltip("목표 띠. anchorMin.y/anchorMax.y = 구간")]
    [SerializeField] private RectTransform band;
    [Tooltip("\"여기서 떼!\" 글씨. 띠 가운데 높이로 옮긴다")]
    [SerializeField] private RectTransform bandLabel;
    [SerializeField] private TMP_Text capacityText;
    [SerializeField] private TMP_Text valueText;

    [Header("연출")]
    [Tooltip("거품 치직 때 뜨는 글씨 (없으면 생략)")]
    [SerializeField] private TMP_Text alertText;
    [Tooltip("숫자 색: 목표 안 / 넘김")]
    [SerializeField] private Color inBandColor = new Color(1f, 0.8f, 0.3f);
    [SerializeField] private Color overColor = new Color(1f, 0.35f, 0.3f);
    [Tooltip("한가운데를 지날 때 띠가 커지는 정도")]
    [SerializeField] private float flashScale = 0.3f;

    [Header("치직 글씨")]
    [SerializeField] private string sputterWord = "치지직…";
    [SerializeField] private string burstWord = "푸슉!";
    [Tooltip("글씨 크기 배율")]
    [SerializeField] private float alertScale = 0.7f;
    [Tooltip("노즐 기준 글씨 위치(px)")]
    [SerializeField] private Vector2 alertOffset = new Vector2(260f, 70f);
    [Tooltip("튀어나오는 시간·끝날 때 흐려지는 시간(초)")]
    [SerializeField] private float alertPopSeconds = 0.12f;
    [SerializeField] private float alertFadeSeconds = 0.15f;

    private bool saved;
    private Vector3 bandRestScale;
    private Color normalColor;
    private float pulse;
    private int zone;
    // 치직 글씨: 0 = 없음, 1 = 치지직(떨림), 2 = 푸슉(튀기만)
    private int alertMode;
    private float alertElapsed;
    private float alertSeconds;
    private Vector2 alertBasePos;
    private Color alertColor;

    // 처음 쓸 때 원래 크기·색을 기억한다 (Configure가 Awake보다 먼저 불릴 수 있다)
    private void SaveRest()
    {
        if (saved) return;
        saved = true;
        bandRestScale = band.localScale;
        normalColor = valueText.color;
        if (alertText != null)
        {
            alertColor = alertText.color;
            alertBasePos = alertText.rectTransform.anchoredPosition;
        }
    }

    // 목표·용량을 맞추고 게이지를 비운다
    public void Configure(float targetMinMl, float targetMaxMl, float capacityMl)
    {
        SaveRest();
        float min01 = Mathf.Clamp01(targetMinMl / capacityMl);
        float max01 = Mathf.Clamp01(targetMaxMl / capacityMl);
        // 폭이 아주 좁은 구간도 보이게 최소 두께를 준다
        float width = Mathf.Max(max01 - min01, 0.02f);
        float mid = (min01 + max01) * 0.5f;
        band.anchorMin = new Vector2(band.anchorMin.x, mid - width * 0.5f);
        band.anchorMax = new Vector2(band.anchorMax.x, mid + width * 0.5f);
        band.offsetMin = new Vector2(band.offsetMin.x, 0f);
        band.offsetMax = new Vector2(band.offsetMax.x, 0f);
        bandLabel.anchorMin = new Vector2(bandLabel.anchorMin.x, mid);
        bandLabel.anchorMax = new Vector2(bandLabel.anchorMax.x, mid);
        bandLabel.offsetMin = new Vector2(0f, -24f);
        bandLabel.offsetMax = new Vector2(0f, 24f);
        capacityText.SetText("{0} ml", Mathf.RoundToInt(capacityMl));

        pulse = 0f;
        band.localScale = bandRestScale;
        zone = -1;
        SetZone(0);
        HideAlert();
        SetAmount(0f, capacityMl);
    }

    public void SetAmount(float ml, float capacityMl)
    {
        float v = Mathf.Clamp01(ml / capacityMl);
        fill.anchorMax = new Vector2(fill.anchorMax.x, v);
        fill.gameObject.SetActive(v > 0f);
        valueText.SetText("{0} ml", Mathf.RoundToInt(ml));
    }

    // 0 = 목표 전, 1 = 목표 안, 2 = 넘김
    public void SetZone(int z)
    {
        SaveRest();
        if (zone == z) return;
        zone = z;
        if (z == 1) valueText.color = inBandColor;
        else if (z == 2) valueText.color = overColor;
        else valueText.color = normalColor;
    }

    // 한가운데를 지나는 순간 띠가 튄다
    public void Flash()
    {
        pulse = 1f;
    }

    // 치지직: 노즐 옆(화면 위치)에 튀어나와 떨다가 흐려진다
    public void ShowSputter(Vector3 nozzleWorld, Camera cam, float seconds)
    {
        if (alertText == null) return;
        SaveRest();
        RectTransform parent = alertText.rectTransform.parent as RectTransform;
        Canvas canvas = alertText.canvas;
        Camera uiCam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) uiCam = canvas.worldCamera;
        Vector2 local;
        if (cam != null && parent != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, cam.WorldToScreenPoint(nozzleWorld), uiCam, out local))
        {
            alertBasePos = local + alertOffset;
        }
        StartAlert(1, sputterWord, seconds);
    }

    // 푸슉: 멈춤이 끝나고 쏟아질 때 같은 자리에서 한 번 튄다
    public void ShowBurst(float seconds)
    {
        if (alertText == null) return;
        StartAlert(2, burstWord, seconds);
    }

    public void HideAlert()
    {
        alertMode = 0;
        if (alertText == null) return;
        alertText.gameObject.SetActive(false);
        alertText.rectTransform.localRotation = Quaternion.identity;
    }

    private void StartAlert(int mode, string word, float seconds)
    {
        alertMode = mode;
        alertElapsed = 0f;
        alertSeconds = Mathf.Max(seconds, alertPopSeconds * 2f);
        alertText.text = word;
        alertText.gameObject.SetActive(true);
        UpdateAlert();
    }

    private void UpdateAlert()
    {
        RectTransform rt = alertText.rectTransform;
        // 크기: 작게 → 살짝 크게 → 제자리
        float pop;
        if (alertElapsed < alertPopSeconds) pop = Mathf.Lerp(0.5f, 1.15f, alertElapsed / alertPopSeconds);
        else pop = Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((alertElapsed - alertPopSeconds) / alertPopSeconds));
        rt.localScale = Vector3.one * alertScale * pop;

        // 치지직은 살짝 기울며 위아래로 떨고, 푸슉은 위로 조금 떠오른다
        Vector2 pos = alertBasePos;
        float tilt = 0f;
        if (alertMode == 1)
        {
            tilt = Mathf.Sin(alertElapsed * 25f) * 4f;
            pos.y += Mathf.Sin(alertElapsed * 18f) * 4f;
        }
        else
        {
            pos.y += alertElapsed * 60f;
        }
        rt.anchoredPosition = pos;
        rt.localRotation = Quaternion.Euler(0f, 0f, tilt);

        // 끝날 때 흐려진다
        Color c = alertColor;
        c.a = alertColor.a * Mathf.Clamp01((alertSeconds - alertElapsed) / alertFadeSeconds);
        alertText.color = c;
    }

    private void Update()
    {
        if (!saved) return;
        if (pulse > 0f)
        {
            pulse = Mathf.Max(0f, pulse - Time.deltaTime * 4f);
            band.localScale = bandRestScale * (1f + flashScale * pulse);
        }
        if (alertMode != 0 && alertText != null)
        {
            alertElapsed += Time.deltaTime;
            if (alertElapsed >= alertSeconds) HideAlert();
            else UpdateAlert();
        }
    }
}
