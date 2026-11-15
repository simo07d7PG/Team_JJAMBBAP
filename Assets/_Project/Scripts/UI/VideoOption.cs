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
        Screen.SetResolution(resolutions[resolutinNum].width,
            resolutions[resolutinNum].height,
            screenMode);
    }
}