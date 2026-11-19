using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VideoOption : MonoBehaviour
{
    FullScreenMode screenMode = FullScreenMode.Windowed;
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenBtn;
    List<UnityEngine.Resolution> resolutions = new List<UnityEngine.Resolution>();
    int resolutinNum;

    // 마지막으로 적용한 값. 같은 값이면 SetResolution을 다시 부르지 않는다
    int lastW = -1, lastH = -1;
    FullScreenMode lastMode = FullScreenMode.Windowed;

    void Start()
    {
        InitUI();

        // 씬에 걸린 인스펙터 이벤트(토글 -> OkBtnClick, 드롭다운 -> DropboxOptionChange)가 남아 있어도
        // 결과가 같도록, 코드에서 토글·드롭다운 변경 시 항상 같은 Apply를 거치게 한다
        if (fullscreenBtn != null)
        {
            fullscreenBtn.onValueChanged.RemoveListener(OnToggleChanged);
            fullscreenBtn.onValueChanged.AddListener(OnToggleChanged);
        }
        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            resolutionDropdown.onValueChanged.AddListener(OnDropdownChanged);
        }
    }

    void OnDestroy()
    {
        if (fullscreenBtn != null) fullscreenBtn.onValueChanged.RemoveListener(OnToggleChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }

    // 현재 화면 설정(빌드에서는 ScreenBootstrap이 적용한 값, 에디터에서는 Screen 값)
    void GetCurrentState(out int w, out int h, out FullScreenMode mode)
    {
        if (ScreenBootstrap.HasApplied)
        {
            w = ScreenBootstrap.AppliedWidth;
            h = ScreenBootstrap.AppliedHeight;
            mode = ScreenBootstrap.AppliedMode;
        }
        else
        {
            w = Screen.width;
            h = Screen.height;
            mode = Screen.fullScreenMode;
        }
    }

    void InitUI()
    {
        if (resolutionDropdown == null || fullscreenBtn == null) return;

        resolutions.Clear();

        for (int i = 0; i < Screen.resolutions.Length; i++)
        {
            if (Screen.resolutions[i].refreshRate == 144)
                resolutions.Add(Screen.resolutions[i]);
        }

        // 144Hz 항목이 없으면 전체 해상도에서 (너비, 높이) 중복을 뺀 목록으로 대체
        if (resolutions.Count == 0)
        {
            for (int i = 0; i < Screen.resolutions.Length; i++)
            {
                UnityEngine.Resolution r = Screen.resolutions[i];
                bool dup = false;
                for (int j = 0; j < resolutions.Count; j++)
                {
                    if (resolutions[j].width == r.width && resolutions[j].height == r.height) { dup = true; break; }
                }
                if (!dup) resolutions.Add(r);
            }
        }

        GetCurrentState(out int curW, out int curH, out FullScreenMode curMode);

        // 현재 해상도가 목록에 없으면 추가한다(없으면 맨 앞 항목이 "현재"로 보이는 문제 방지)
        int currentIndex = -1;
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].width == curW && resolutions[i].height == curH) { currentIndex = i; break; }
        }
        if (currentIndex < 0 && curW > 0 && curH > 0)
        {
            UnityEngine.Resolution cur = new UnityEngine.Resolution();
            cur.width = curW;
            cur.height = curH;
            cur.refreshRateRatio = Screen.currentResolution.refreshRateRatio;
            resolutions.Add(cur);
            currentIndex = resolutions.Count - 1;
        }
        if (currentIndex < 0) currentIndex = 0;

        resolutionDropdown.options.Clear();
        foreach (UnityEngine.Resolution item in resolutions)
        {
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData();
            float refreshRate = (float)item.refreshRateRatio.value;
            option.text = item.width + " x " + item.height + " " + Mathf.Round(refreshRate) + "Hz";
            resolutionDropdown.options.Add(option);
        }

        resolutinNum = currentIndex;
        // 이벤트를 부르지 않고 값만 맞춘다(시작하자마자 적용되는 것 방지)
        resolutionDropdown.SetValueWithoutNotify(currentIndex);
        resolutionDropdown.RefreshShownValue();

        bool isFull = curMode == FullScreenMode.FullScreenWindow || curMode == FullScreenMode.ExclusiveFullScreen;
        screenMode = isFull ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        fullscreenBtn.SetIsOnWithoutNotify(isFull);

        lastW = curW;
        lastH = curH;
        lastMode = isFull ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
    }

    void OnToggleChanged(bool isFull)
    {
        FullScreenBtn(isFull);
        Apply();
    }

    void OnDropdownChanged(int x)
    {
        DropboxOptionChange(x);
        Apply();
    }

    public void DropboxOptionChange(int x)
    {
        resolutinNum = x;
    }

    public void FullScreenBtn(bool isFull)
    {
        screenMode = isFull ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
    }

    // 인스펙터에 연결된 기존 이벤트(토글 -> OkBtnClick)가 불려도 토글의 현재 값 기준으로 적용한다
    public void OkBtnClick()
    {
        Apply();
    }

    void Apply()
    {
        if (resolutions == null || resolutions.Count == 0 || resolutinNum < 0 || resolutinNum >= resolutions.Count) return;

        // 토글 상태가 진실의 원천이다(순서에 상관없이 같은 결과)
        if (fullscreenBtn != null)
            screenMode = fullscreenBtn.isOn ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

        // 창 모드로 돌아갈 때 선택한 해상도가 모니터 크기 이상이면 화면에 들어오는 가장 큰 항목으로 바꾼다
        if (screenMode == FullScreenMode.Windowed)
            FitWindowedSelection();

        int w = resolutions[resolutinNum].width;
        int h = resolutions[resolutinNum].height;
        if (w == lastW && h == lastH && screenMode == lastMode) return;

        Screen.SetResolution(w, h, screenMode);
        lastW = w;
        lastH = h;
        lastMode = screenMode;
        ScreenBootstrap.MarkApplied(w, h, screenMode);
    }

    void FitWindowedSelection()
    {
        UnityEngine.Resolution mon = Screen.currentResolution;
        if (mon.width <= 0 || mon.height <= 0) return;

        float maxW = mon.width * 0.95f;
        float maxH = mon.height * 0.95f;
        UnityEngine.Resolution sel = resolutions[resolutinNum];
        if (sel.width <= maxW && sel.height <= maxH) return;

        int best = -1;
        long bestArea = -1;
        for (int i = 0; i < resolutions.Count; i++)
        {
            UnityEngine.Resolution r = resolutions[i];
            if (r.width > mon.width * 0.9f || r.height > mon.height * 0.9f) continue;
            long area = (long)r.width * r.height;
            if (area > bestArea) { bestArea = area; best = i; }
        }
        if (best < 0) return;

        resolutinNum = best;
        if (resolutionDropdown != null)
        {
            resolutionDropdown.SetValueWithoutNotify(best);
            resolutionDropdown.RefreshShownValue();
        }
    }
}
