using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 주전자 끓이기 화면. 가운데 큰 키 글자 + 오른쪽 다음 키 미리보기, 세로 온도계(목표선) + "74℃". 표시만 한다
public class KettleBoilHud : MonoBehaviour
{
    [Header("키")]
    [Tooltip("가운데 큰 글자 (지금 눌러야 할 키)")]
    [SerializeField] private TMP_Text currentKeyText;
    [Tooltip("오른쪽 미리보기 글자들 (가까운 순서). 칸 수 = 본체의 미리보기 개수")]
    [SerializeField] private TMP_Text[] previewTexts;

    [Header("온도계")]
    [Tooltip("채움. anchorMax.y = 온도 비율")]
    [SerializeField] private RectTransform fill;
    [Tooltip("채움 색을 바꿀 그래픽 (비우면 색 고정)")]
    [SerializeField] private Graphic fillGraphic;
    [Tooltip("목표선. anchorMin.y/anchorMax.y = 목표 온도 비율")]
    [SerializeField] private RectTransform targetLine;
    [SerializeField] private TMP_Text tempText;
    [Tooltip("온도 글자 형식. 폰트에 ℃가 없으면 \"{0}°C\"로 바꾼다")]
    [SerializeField] private string tempFormat = "{0}℃";
    [Tooltip("온도계 바닥·꼭대기 온도(℃). 바닥은 시작 온도보다 조금 낮게 두면 처음에도 채움이 보인다")]
    [SerializeField] private float gaugeMinTemp = 0f;
    [SerializeField] private float gaugeMaxTemp = 100f;
    [Tooltip("채움 색: 시작 온도 / 목표 온도")]
    [SerializeField] private Color coldFillColor = new Color(0.35f, 0.55f, 0.85f);
    [SerializeField] private Color hotFillColor = new Color(0.85f, 0.2f, 0.15f);

    [Header("연출")]
    [Tooltip("맞았을 때 글자 색 (초록)")]
    [SerializeField] private Color correctColor = new Color(0.3f, 0.75f, 0.3f);
    [Tooltip("틀렸을 때 글자 색 (붉은색)")]
    [SerializeField] private Color wrongColor = new Color(0.85f, 0.2f, 0.2f);
    [Tooltip("맞았을 때 글자가 튀는 시간(초)과 최대 배율")]
    [SerializeField] private float bounceSeconds = 0.18f;
    [SerializeField] private float bounceScale = 1.35f;
    [Tooltip("맞았을 때 초록이 원래 색으로 돌아오는 시간(초)")]
    [SerializeField] private float correctColorSeconds = 0.25f;
    [Tooltip("틀렸을 때 좌우 흔들림 폭(px)과 빠르기")]
    [SerializeField] private float shakeAmplitude = 14f;
    [SerializeField] private float shakeFrequency = 60f;


    // 연출 종류: 없음 / 맞음(튀기 + 초록) / 틀림(흔들림 + 붉게)
    private const int EffectNone = 0;
    private const int EffectCorrect = 1;
    private const int EffectWrong = 2;

    // 연출 시간이 0이어도 나눗셈이 깨지지 않게 하는 최소 시간(초)
    private const float MinEffectSeconds = 0.01f;

    private bool saved;
    private Color normalKeyColor;
    private Vector2 keyRestPos;
    private Vector3 keyRestScale;
    private float startTemp;
    private float targetTemp;

    private int effect;
    private float effectElapsed;
    private float effectSeconds;

    // 처음 쓸 때 원래 위치·크기·색을 기억한다 (Configure가 Awake보다 먼저 불릴 수 있다)
    private void SaveRest()
    {
        if (saved) return;
        saved = true;
        normalKeyColor = currentKeyText.color;
        keyRestPos = currentKeyText.rectTransform.anchoredPosition;
        keyRestScale = currentKeyText.rectTransform.localScale;
    }

    // 판 시작: 목표선을 맞추고 연출을 지운다. 쓰지 않는 미리보기 칸은 끈다
    public void Configure(float startTempValue, float targetTempValue, int previewCount)
    {
        SaveRest();
        startTemp = startTempValue;
        targetTemp = targetTempValue;

        float target01 = TempTo01(targetTemp);
        targetLine.anchorMin = new Vector2(targetLine.anchorMin.x, target01);
        targetLine.anchorMax = new Vector2(targetLine.anchorMax.x, target01);

        for (int i = 0; i < previewTexts.Length; i++)
        {
            previewTexts[i].gameObject.SetActive(i < previewCount);
        }

        StopEffect();
    }

    public void SetCurrentKey(string label)
    {
        currentKeyText.text = label;
    }

    // 미리보기 칸 수보다 큰 번호는 무시한다
    public void SetPreviewKey(int index, string label)
    {
        if (index >= previewTexts.Length) return;
        previewTexts[index].text = label;
    }

    public void SetTemperature(float temp)
    {
        float v = TempTo01(temp);
        fill.anchorMax = new Vector2(fill.anchorMax.x, v);
        if (fillGraphic != null)
        {
            float heat = Mathf.Clamp01((temp - startTemp) / Mathf.Max(1f, targetTemp - startTemp));
            fillGraphic.color = Color.Lerp(coldFillColor, hotFillColor, heat);
        }
        tempText.SetText(tempFormat, Mathf.RoundToInt(temp));
    }

    private float TempTo01(float temp)
    {
        return Mathf.Clamp01((temp - gaugeMinTemp) / Mathf.Max(1f, gaugeMaxTemp - gaugeMinTemp));
    }

    // 맞음: 새 키 글자가 튀어나오며 초록에서 원래 색으로
    public void PlayCorrect()
    {
        StartEffect(EffectCorrect, Mathf.Max(bounceSeconds, correctColorSeconds));
    }

    // 틀림: 잠긴 동안 글자가 붉게 좌우로 흔들린다
    public void PlayWrong(float seconds)
    {
        StartEffect(EffectWrong, seconds);
    }

    private void StartEffect(int kind, float seconds)
    {
        SaveRest();
        effect = kind;
        effectElapsed = 0f;
        effectSeconds = Mathf.Max(MinEffectSeconds, seconds);
        UpdateEffect();
    }

    private void StopEffect()
    {
        effect = EffectNone;
        RectTransform rt = currentKeyText.rectTransform;
        rt.anchoredPosition = keyRestPos;
        rt.localScale = keyRestScale;
        currentKeyText.color = normalKeyColor;
    }

    private void UpdateEffect()
    {
        RectTransform rt = currentKeyText.rectTransform;
        if (effect == EffectCorrect)
        {
            // 크기: 크게 시작해 제자리로
            float b = Mathf.Clamp01(effectElapsed / Mathf.Max(MinEffectSeconds, bounceSeconds));
            rt.localScale = keyRestScale * Mathf.Lerp(bounceScale, 1f, b);
            rt.anchoredPosition = keyRestPos;
            currentKeyText.color = Color.Lerp(correctColor, normalKeyColor, Mathf.Clamp01(effectElapsed / Mathf.Max(MinEffectSeconds, correctColorSeconds)));
        }
        else if (effect == EffectWrong)
        {
            // 흔들림은 끝으로 갈수록 잦아든다
            float fade = 1f - Mathf.Clamp01(effectElapsed / effectSeconds);
            float x = Mathf.Sin(effectElapsed * shakeFrequency) * shakeAmplitude * fade;
            rt.anchoredPosition = keyRestPos + new Vector2(x, 0f);
            rt.localScale = keyRestScale;
            currentKeyText.color = wrongColor;
        }
    }

    // 일시정지(timeScale 0) 중에는 연출도 멈춘다
    private void Update()
    {
        if (effect == EffectNone) return;
        effectElapsed += Time.deltaTime;
        if (effectElapsed >= effectSeconds) StopEffect();
        else UpdateEffect();
    }
}
