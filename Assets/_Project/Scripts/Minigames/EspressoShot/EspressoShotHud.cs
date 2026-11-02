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

    // 치직 글씨 종류: 없음 / 치지직(떨림) / 푸슉(튀기만)
    private enum AlertMode
    {
        None,
        Sputter,
        Burst
    }

    // 숫자 구간: 목표 전 / 목표 안 / 넘김. 아직 정해지지 않은 상태는 ZoneUnset
    private const int ZoneBefore = 0;
    private const int ZoneInBand = 1;
    private const int ZoneOver = 2;
    private const int ZoneUnset = -1;

    // 숫자 표시 형식
    private const string MlFormat = "{0} ml";

    // 폭이 아주 좁은 구간도 보이게 하는 최소 띠 두께(0~1), "여기서 떼!" 글씨 반높이(px)
    private const float MinBandWidth01 = 0.02f;
    private const float BandLabelHalfHeight = 24f;

    // 띠 튀는 효과가 가라앉는 속도
    private const float PulseDecaySpeed = 4f;

    // 글씨 팝 크기: 시작 / 정점 배율
    private const float PopStartScale = 0.5f;
    private const float PopPeakScale = 1.15f;

    // 치지직 떨림(기울기·위아래 주파수와 진폭), 푸슉 상승 속도(px/초)
    private const float SputterTiltFrequency = 25f;
    private const float SputterTiltDegrees = 4f;
    private const float SputterShakeFrequency = 18f;
    private const float SputterShakeAmplitude = 4f;
    private const float BurstRiseSpeed = 60f;

    private bool saved;
    private Vector3 bandRestScale;
    private Color normalColor;
    private float pulse;
    private int zone;
    private AlertMode alertMode;
    private float alertElapsed;
    private float alertSeconds;
    private Vector2 alertBasePos;
    private Color alertColor;

    // 치직 글씨를 쓸 수 있는지(글씨 오브젝트가 연결돼 있는지)
    private bool HasAlert
    {
        get { return alertText != null; }
    }

    // 처음 쓸 때 원래 크기·색을 기억한다 (Configure가 Awake보다 먼저 불릴 수 있다)
    private void SaveRest()
    {
        if (saved) return;
        saved = true;
        bandRestScale = band.localScale;
        normalColor = valueText.color;
        if (HasAlert)
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
        float width = Mathf.Max(max01 - min01, MinBandWidth01);
        float mid = (min01 + max01) * 0.5f;
        SetAnchorY(band, mid - width * 0.5f, mid + width * 0.5f);
        band.offsetMin = new Vector2(band.offsetMin.x, 0f);
        band.offsetMax = new Vector2(band.offsetMax.x, 0f);
        SetAnchorY(bandLabel, mid, mid);
        bandLabel.offsetMin = new Vector2(0f, -BandLabelHalfHeight);
        bandLabel.offsetMax = new Vector2(0f, BandLabelHalfHeight);
        capacityText.SetText(MlFormat, Mathf.RoundToInt(capacityMl));

        pulse = 0f;
        band.localScale = bandRestScale;
        zone = ZoneUnset;
        SetZone(ZoneBefore);
        HideAlert();
        SetAmount(0f, capacityMl);
    }

    // 앵커의 x는 그대로 두고 y 범위만 바꾼다
    private static void SetAnchorY(RectTransform rt, float minY, float maxY)
    {
        rt.anchorMin = new Vector2(rt.anchorMin.x, minY);
        rt.anchorMax = new Vector2(rt.anchorMax.x, maxY);
    }

    public void SetAmount(float ml, float capacityMl)
    {
        float v = Mathf.Clamp01(ml / capacityMl);
        fill.anchorMax = new Vector2(fill.anchorMax.x, v);
        fill.gameObject.SetActive(v > 0f);
        valueText.SetText(MlFormat, Mathf.RoundToInt(ml));
    }

    // 0 = 목표 전, 1 = 목표 안, 2 = 넘김
    public void SetZone(int z)
    {
        SaveRest();
        if (zone == z) return;
        zone = z;
        valueText.color = ZoneColor(z);
    }

    // 구간별 숫자 색
    private Color ZoneColor(int z)
    {
        if (z == ZoneInBand) return inBandColor;
        if (z == ZoneOver) return overColor;
        return normalColor;
    }

    // 한가운데를 지나는 순간 띠가 튄다
    public void Flash()
    {
        pulse = 1f;
    }

    // 치지직: 노즐 옆(화면 위치)에 튀어나와 떨다가 흐려진다
    public void ShowSputter(Vector3 nozzleWorld, Camera cam, float seconds)
    {
        if (!HasAlert) return;
        SaveRest();
        Vector2 local;
        if (TryGetAlertBasePos(nozzleWorld, cam, out local))
        {
            alertBasePos = local + alertOffset;
        }
        StartAlert(AlertMode.Sputter, sputterWord, seconds);
    }

    // 노즐의 월드 좌표를 글씨가 속한 UI의 로컬 좌표로 바꾼다. 못 바꾸면 false
    private bool TryGetAlertBasePos(Vector3 nozzleWorld, Camera cam, out Vector2 pos)
    {
        RectTransform parent = alertText.rectTransform.parent as RectTransform;
        Canvas canvas = alertText.canvas;
        Camera uiCam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) uiCam = canvas.worldCamera;
        pos = Vector2.zero;
        return cam != null && parent != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, cam.WorldToScreenPoint(nozzleWorld), uiCam, out pos);
    }

    // 푸슉: 멈춤이 끝나고 쏟아질 때 같은 자리에서 한 번 튄다
    public void ShowBurst(float seconds)
    {
        if (!HasAlert) return;
        StartAlert(AlertMode.Burst, burstWord, seconds);
    }

    public void HideAlert()
    {
        alertMode = AlertMode.None;
        if (!HasAlert) return;
        alertText.gameObject.SetActive(false);
        alertText.rectTransform.localRotation = Quaternion.identity;
    }

    private void StartAlert(AlertMode mode, string word, float seconds)
    {
        alertMode = mode;
        alertElapsed = 0f;
        alertSeconds = Mathf.Max(seconds, alertPopSeconds * 2f);
        alertText.text = word;
        alertText.gameObject.SetActive(true);
        UpdateAlert();
    }

    // 글씨 크기 곡선: 작게 → 살짝 크게 → 제자리
    private float PopScale(float elapsed)
    {
        if (elapsed < alertPopSeconds) return Mathf.Lerp(PopStartScale, PopPeakScale, elapsed / alertPopSeconds);
        return Mathf.Lerp(PopPeakScale, 1f, Mathf.Clamp01((elapsed - alertPopSeconds) / alertPopSeconds));
    }

    private void UpdateAlert()
    {
        RectTransform rt = alertText.rectTransform;
        rt.localScale = Vector3.one * alertScale * PopScale(alertElapsed);

        // 치지직은 살짝 기울며 위아래로 떨고, 푸슉은 위로 조금 떠오른다
        Vector2 pos = alertBasePos;
        float tilt = 0f;
        if (alertMode == AlertMode.Sputter)
        {
            tilt = Mathf.Sin(alertElapsed * SputterTiltFrequency) * SputterTiltDegrees;
            pos.y += Mathf.Sin(alertElapsed * SputterShakeFrequency) * SputterShakeAmplitude;
        }
        else
        {
            pos.y += alertElapsed * BurstRiseSpeed;
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
            pulse = Mathf.Max(0f, pulse - Time.deltaTime * PulseDecaySpeed);
            band.localScale = bandRestScale * (1f + flashScale * pulse);
        }
        if (alertMode != AlertMode.None && HasAlert)
        {
            alertElapsed += Time.deltaTime;
            if (alertElapsed >= alertSeconds) HideAlert();
            else UpdateAlert();
        }
    }
}
