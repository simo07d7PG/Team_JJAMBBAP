using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// "물 끓여라!" — 화면에 나온 키(Q/W/E/R)를 순서대로 눌러 물을 데운다. 목표 온도에 닿으면 성공.
// 맞으면 온도가 오르고 다음 키로, 틀리면 온도가 조금 내려가고 잠깐 입력이 잠긴다(같은 키가 그대로 남음).
public class KettleBoilMicrogame : Minigame
{
    [Header("참조")]
    [Tooltip("온도에 따라 색이 바뀌는 주전자 렌더러들 (몸통 구 + 주둥이)")]
    [SerializeField] private Renderer[] kettleRenderers;
    [Tooltip("뚜껑. 높은 온도에서 떨린다")]
    [SerializeField] private Transform lid;
    [Tooltip("버너 불꽃(원뿔). 온도가 오를수록 커진다")]
    [SerializeField] private Transform flame;
    [SerializeField] private KettleBoilHud hud;
    [Tooltip("키 입력 4개, 순서대로 Q / W / E / R (InputSystem_Actions > UI/KeyQ·KeyW·KeyE·KeyR)")]
    [SerializeField] private InputActionReference[] keyActions = new InputActionReference[KeyCount];

    [Header("수치")]
    [Tooltip("시작 온도(℃). 틀려도 이 밑으로는 안 내려간다")]
    [SerializeField] private float startTemp = 20f;
    [Tooltip("이 온도 이상이면 즉시 성공(℃)")]
    [SerializeField] private float targetTemp = 92f;
    [Tooltip("맞는 키 한 번에 오르는 온도(℃)")]
    [SerializeField] private float correctGain = 9f;
    [Tooltip("틀린 키 한 번에 내려가는 온도(℃)")]
    [SerializeField] private float wrongPenalty = 5f;
    [Tooltip("틀린 뒤 입력이 잠기는 시간(초)")]
    [SerializeField] private float wrongLockSeconds = 0.25f;
    [Tooltip("시간 초과 때 이 온도 이상이면 \"덜 끓었어!\"(℃)")]
    [SerializeField] private float almostTemp = 60f;
    [Tooltip("오른쪽에 미리 보여 줄 다음 키 개수 (HUD 미리보기 칸 수와 맞춘다)")]
    [SerializeField] private int previewCount = 3;

    [Header("등급 (틀린 횟수별)")]
    [SerializeField] private float qualityNoMiss = 1f;
    [SerializeField] private float qualityOneMiss = 0.5f;
    [SerializeField] private float qualityManyMiss = 0.2f;

    [Header("연출")]
    [Tooltip("주전자 색: 시작 온도(푸른색) → 목표 온도(붉은색)")]
    [SerializeField] private Color coldColor = new Color(0.35f, 0.55f, 0.85f);
    [SerializeField] private Color hotColor = new Color(0.85f, 0.2f, 0.15f);
    [Tooltip("이 온도부터 뚜껑이 떨린다(℃)")]
    [SerializeField] private float lidShakeFromTemp = 80f;
    [Tooltip("목표 온도에서 뚜껑이 들썩이는 높이(m)와 기울기(도)")]
    [SerializeField] private float lidShakeHeight = 0.01f;
    [SerializeField] private float lidShakeDegrees = 6f;
    [Tooltip("뚜껑 떨림 빠르기")]
    [SerializeField] private float lidShakeSpeed = 30f;
    [Tooltip("불꽃 크기 배율: 시작 온도 / 목표 온도")]
    [SerializeField] private float flameMinScale = 0.4f;
    [SerializeField] private float flameMaxScale = 1.3f;
    [Tooltip("불꽃 일렁임 폭(배율)과 빠르기")]
    [SerializeField] private float flameFlicker = 0.08f;
    [SerializeField] private float flameFlickerSpeed = 12f;

    // 키 번호: 0 = Q, 1 = W, 2 = E, 3 = R
    private const int KeyCount = 4;
    private static readonly string[] KeyLabels = { "Q", "W", "E", "R" };

    // 일시정지 판정: timeScale이 이 값 이하면 입력을 받지 않는다
    private const float PausedTimeScale = 0f;

    // 결과 문구
    private const string TextSuccess = "보글보글!";
    private const string TextAlmost = "덜 끓었어!";
    private const string TextTimeOver = "시간 초과!";

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    // 눌러야 할 키 순서. [0]이 지금 키, 그 뒤가 미리보기
    private List<int> sequence = new List<int>();
    private float temp;
    private int missCount;
    private float lockTimer;
    private float nextPourTime;
    private float noiseSeed;

    private MaterialPropertyBlock block;
    private Vector3 lidRestLocal;
    private Quaternion lidRestRot;
    private Vector3 flameRestScale;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        lidRestLocal = lid.localPosition;
        lidRestRot = lid.localRotation;
        flameRestScale = flame.localScale;
    }

    protected override void ResetGame()
    {
        for (int i = 0; i < keyActions.Length; i++)
        {
            keyActions[i].action.Enable();
        }

        temp = startTemp;
        missCount = 0;
        lockTimer = 0f;
        noiseSeed = Random.value * 100f;

        sequence.Clear();
        FillSequence();

        hud.Configure(startTemp, targetTemp, previewCount);
        hud.SetTemperature(temp);
        ShowKeys();
        UpdateVisuals();
    }

    private void Update()
    {
        if (!isPlaying) return;

        UpdateVisuals();

        // 일시정지 중에는 키 입력을 반영하지 않는다
        if (Time.timeScale <= PausedTimeScale) return;

        if (lockTimer > 0f)
        {
            lockTimer -= Time.deltaTime;
            return;
        }

        int pressedKey;
        int pressedCount = CountPressedKeys(out pressedKey);
        if (pressedCount == 0) return;

        // 한 프레임에 한 번만 판정한다. 두 개 이상 동시에 눌렸으면 틀림 한 번(여러 키 연타로 맞히는 것 방지)
        if (pressedCount == 1 && pressedKey == sequence[0]) Correct();
        else Wrong();
    }

    // 이번 프레임에 눌린 키 수. 하나라도 눌렸으면 마지막으로 찾은 키 번호를 pressedKey에 넣는다
    private int CountPressedKeys(out int pressedKey)
    {
        pressedKey = -1;
        int count = 0;
        for (int i = 0; i < keyActions.Length; i++)
        {
            if (keyActions[i].action.WasPressedThisFrame())
            {
                count++;
                pressedKey = i;
            }
        }
        return count;
    }

    private void Correct()
    {
        temp += correctGain;
        sequence.RemoveAt(0);
        FillSequence();
        ShowKeys();
        hud.PlayCorrect();
        // 연타해도 0.35초 간격으로만 물 소리
        if (Time.time >= nextPourTime)
        {
            nextPourTime = Time.time + 0.35f;
            MinigameSfx.Play("sfx_pour", 0.35f);
        }
        hud.SetTemperature(temp);

        if (temp >= targetTemp) EndGame(true, TextSuccess);
    }

    // 틀리면 온도가 내려가고(시작 온도 밑으로는 안 내려감) 잠깐 잠긴다. 키는 그대로
    private void Wrong()
    {
        missCount++;
        temp = Mathf.Max(startTemp, temp - wrongPenalty);
        lockTimer = wrongLockSeconds;
        hud.PlayWrong(wrongLockSeconds);
        hud.SetTemperature(temp);
    }

    // 지금 키 + 미리보기 칸 수만큼 순서를 채운다. 같은 키가 연속으로 나오지 않는다
    private void FillSequence()
    {
        while (sequence.Count < previewCount + 1)
        {
            int key = Random.Range(0, KeyCount);
            if (sequence.Count > 0 && key == sequence[sequence.Count - 1])
            {
                // 앞 키와 같으면 나머지 3개 중 하나로 민다
                key = (key + Random.Range(1, KeyCount)) % KeyCount;
            }
            sequence.Add(key);
        }
    }

    private void ShowKeys()
    {
        hud.SetCurrentKey(KeyLabels[sequence[0]]);
        for (int i = 0; i < previewCount; i++)
        {
            hud.SetPreviewKey(i, KeyLabels[sequence[i + 1]]);
        }
    }

    // 시간 초과. 목표 온도 근처까지 올렸으면 "덜 끓었어!"
    public override void TimeOver()
    {
        if (temp >= almostTemp) EndGame(false, TextAlmost);
        else EndGame(false, TextTimeOver);
    }

    private void EndGame(bool success, string text)
    {
        if (!isPlaying) return;
        if (success) quality = QualityByMisses();
        Finish(success, text);
        lid.localPosition = lidRestLocal;
        lid.localRotation = lidRestRot;
    }

    // 틀린 횟수 0 → 완벽, 1 → 좋아요, 2 이상 → 아슬아슬
    private float QualityByMisses()
    {
        if (missCount == 0) return qualityNoMiss;
        if (missCount == 1) return qualityOneMiss;
        return qualityManyMiss;
    }

    // 주전자 색·뚜껑 떨림·불꽃 크기를 온도에 맞춘다
    private void UpdateVisuals()
    {
        float heat = Mathf.Clamp01((temp - startTemp) / Mathf.Max(1f, targetTemp - startTemp));
        SetKettleColor(Color.Lerp(coldColor, hotColor, heat));
        UpdateLid();

        float flicker = 1f + flameFlicker * (Mathf.PerlinNoise(noiseSeed, Time.time * flameFlickerSpeed) * 2f - 1f);
        flame.localScale = flameRestScale * Mathf.Lerp(flameMinScale, flameMaxScale, heat) * flicker;
    }

    // 떨림 시작 온도부터 목표 온도까지 점점 세게 들썩인다
    private void UpdateLid()
    {
        float shake = Mathf.Clamp01((temp - lidShakeFromTemp) / Mathf.Max(1f, targetTemp - lidShakeFromTemp));
        if (temp < lidShakeFromTemp) shake = 0f;
        float t = Time.time * lidShakeSpeed;
        float up = Mathf.PerlinNoise(t, noiseSeed);
        float tilt = Mathf.PerlinNoise(noiseSeed, t) * 2f - 1f;
        lid.localPosition = lidRestLocal + Vector3.up * lidShakeHeight * shake * up;
        lid.localRotation = lidRestRot * Quaternion.Euler(0f, 0f, lidShakeDegrees * shake * tilt);
    }

    private void SetKettleColor(Color c)
    {
        for (int i = 0; i < kettleRenderers.Length; i++)
        {
            kettleRenderers[i].GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(ColorId, c);
            kettleRenderers[i].SetPropertyBlock(block);
        }
    }
}
