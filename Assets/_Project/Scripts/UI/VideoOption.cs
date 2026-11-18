using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VideoOption : MonoBehaviour
{
    FullScreenMode screenMode;
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenBtn;
    List<UnityEngine.Resolution> resolutions = new List<UnityEngine.Resolution>();
    int resolutinNum;

    void Start()
    {
        InitUI();
    }

    void InitUI()
    {
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

        resolutionDropdown.options.Clear();

        int optionNum = 0;
        foreach (UnityEngine.Resolution item in resolutions)
        {
            TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData();
            float refreshRate = (float)item.refreshRateRatio.value;
            option.text = item.width + " x " + item.height + " " + Mathf.Round(refreshRate) + "Hz";
            resolutionDropdown.options.Add(option);

            if (item.width == Screen.width && item.height == Screen.height)
                resolutionDropdown.value = optionNum;

            optionNum++;
        }
        resolutionDropdown.RefreshShownValue();

        fullscreenBtn.isOn = Screen.fullScreenMode.Equals(FullScreenMode.FullScreenWindow) ? true : false;
    }

    public void DropboxOptionChange(int x)
    {
        resolutinNum = x;
    }

    public void FullScreenBtn(bool isFull)
    {
        screenMode = isFull ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
    }

    public void OkBtnClick()
    {
        if (resolutions == null || resolutions.Count == 0 || resolutinNum < 0 || resolutinNum >= resolutions.Count) return;
        Screen.SetResolution(resolutions[resolutinNum].width,
            resolutions[resolutinNum].height,
            screenMode);
    }
}