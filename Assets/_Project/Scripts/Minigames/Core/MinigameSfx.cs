using System.Collections.Generic;
using UnityEngine;

// 미니게임 공용 효과음. Resources/Sfx/<이름> 클립을 캐시해 두고 전용 AudioSource로 재생한다. 클립이 없으면 조용히 무시
public static class MinigameSfx
{
    private static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private static readonly AudioSource[] pool = new AudioSource[3];
    private static int next;

    public static void Play(string name, float volume = 0.6f)
    {
        if (string.IsNullOrEmpty(name)) return;
        AudioClip clip = GetClip(name);
        if (clip == null) return;
        AudioSource s = GetSource();
        if (s == null) return;
        // 효과음 믹서 그룹에 연결 (SFX 슬라이더가 먹게). AudioManager가 없으면 그대로
        UnityEngine.Audio.AudioMixerGroup group = AudioManager.SfxGroup;
        if (group != null && s.outputAudioMixerGroup != group) s.outputAudioMixerGroup = group;
        // 같은 소리가 단조롭지 않게 매번 음높이를 조금 흔든다 (tick은 폭을 더 넓게)
        // pitch는 AudioSource 전체에 걸리므로 소스를 돌려 쓰며, 이전에 재생 중인 소리는 다른 소스라 영향이 없다
        s.pitch = name == "tick" ? Random.Range(0.92f, 1.12f) : Random.Range(0.95f, 1.06f);
        s.PlayOneShot(clip, volume);
    }

    private static AudioClip GetClip(string name)
    {
        if (clips.TryGetValue(name, out AudioClip cached) && cached != null) return cached;
        AudioClip clip = Resources.Load<AudioClip>("Sfx/" + name);
        if (clip != null) clips[name] = clip;
        return clip;
    }

    // 소스 풀에서 하나를 돌려 준다. 재생 중이 아닌 소스를 우선, 모두 재생 중이면 순서대로 덮어쓴다
    private static AudioSource GetSource()
    {
        if (!Application.isPlaying) return null;
        if (pool[0] == null)
        {
            GameObject go = new GameObject("MinigameSfx");
            Object.DontDestroyOnLoad(go);
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = go.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
                pool[i].spatialBlend = 0f;
            }
            next = 0;
        }
        for (int i = 0; i < pool.Length; i++)
        {
            AudioSource candidate = pool[i];
            if (candidate != null && !candidate.isPlaying) return candidate;
        }
        next = (next + 1) % pool.Length;
        return pool[next];
    }
}
