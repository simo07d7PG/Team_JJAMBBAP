using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("---------- Audio Source ----------")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;


    [System.Serializable]
    public class Sound
    {
        public string name;      // 사운드 이름
        public AudioClip clip;   // 오디오 클립
    }

    [Header("---------- Sound List ----------")]
    public List<Sound> soundList; // 추가할 리스트

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplySavedVolume();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void Start()
    {
        // 게임 씬에서 시작한 게 아니라면 로비 BGM 재생
        if (SceneManager.GetActiveScene().name != GameSceneName) PlayBackground("background");
    }

    private const string GameSceneName = "GameScene";
    private const string MainSceneName = "MainScene";

    // 게임 씬에서는 로비 BGM을 멈추고(카페 BGM은 CafeEffects가 따로 재생), 메인 씬으로 돌아오면 다시 튼다
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (musicSource == null) return;
        if (scene.name == GameSceneName)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }
        else if (scene.name == MainSceneName)
        {
            PlayBackground("background");
        }
    }

    // 믹서 그룹: 다른 씬의 AudioSource를 같은 믹서에 연결할 때 쓴다 (AudioManager가 없으면 null)
    public static AudioMixerGroup MusicGroup =>
        instance != null && instance.musicSource != null ? instance.musicSource.outputAudioMixerGroup : null;

    public static AudioMixerGroup SfxGroup =>
        instance != null && instance.SFXSource != null ? instance.SFXSource.outputAudioMixerGroup : null;

    // 저장된 음량을 믹서에 적용한다 (로비 설정 화면을 거치지 않고 시작해도 반영되게)
    private void ApplySavedVolume()
    {
        AudioMixer mixer = null;
        if (musicSource != null && musicSource.outputAudioMixerGroup != null) mixer = musicSource.outputAudioMixerGroup.audioMixer;
        if (mixer == null && SFXSource != null && SFXSource.outputAudioMixerGroup != null) mixer = SFXSource.outputAudioMixerGroup.audioMixer;
        if (mixer == null) return;
        if (PlayerPrefs.HasKey("musicVolume"))
            mixer.SetFloat("music", Mathf.Log10(Mathf.Max(PlayerPrefs.GetFloat("musicVolume"), 0.0001f)) * 20f);
        if (PlayerPrefs.HasKey("SFXVolume"))
            mixer.SetFloat("SFX", Mathf.Log10(Mathf.Max(PlayerPrefs.GetFloat("SFXVolume"), 0.0001f)) * 20f);
    }

    // 이름으로 배경음악 재생
    public void PlayBackground(string soundName)
    {
        Sound s = soundList.Find(x => x.name == soundName);
        if (s == null)
        {
            Debug.LogWarning($"[AudioManager] 배경음악을 찾을 수 없습니다: {soundName}");
            return;
        }

        if (musicSource.clip != s.clip)
        {
            musicSource.clip = s.clip;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    // 이름으로 효과음(SFX) 재생
    public void PlaySFX(string soundName)
    {
        Sound s = soundList.Find(x => x.name == soundName);
        if (s == null)
        {
            Debug.LogWarning($"[AudioManager] 효과음을 찾을 수 없습니다: {soundName}");
            return;
        }

        SFXSource.PlayOneShot(s.clip);
    }

    // 재생 기능 호환되도록 유지
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            SFXSource.PlayOneShot(clip);
        }
    }

    public void ChangeBackgroundMusic(string newSoundName)
    {
        PlayBackground(newSoundName);
    }
}