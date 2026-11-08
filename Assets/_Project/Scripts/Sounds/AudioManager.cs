using UnityEngine;
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
    }

    private void Start()
    {
        // 시작할 때 사운드 재생
        PlayBackground("background");
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