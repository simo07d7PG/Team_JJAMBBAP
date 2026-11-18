using System.Collections.Generic;
using UnityEngine;

// 미니게임 공용 효과음. Resources/Sfx/<이름> 클립을 캐시해 두고 전용 AudioSource로 재생한다. 클립이 없으면 조용히 무시
public static class MinigameSfx
{
    private static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private static AudioSource source;

    public static void Play(string name, float volume = 0.6f)
    {
        if (string.IsNullOrEmpty(name)) return;
        AudioClip clip = GetClip(name);
        if (clip == null) return;
        AudioSource s = GetSource();
        if (s != null) s.PlayOneShot(clip, volume);
    }

    private static AudioClip GetClip(string name)
    {
        if (clips.TryGetValue(name, out AudioClip cached) && cached != null) return cached;
        AudioClip clip = Resources.Load<AudioClip>("Sfx/" + name);
        if (clip != null) clips[name] = clip;
        return clip;
    }

    private static AudioSource GetSource()
    {
        if (source != null) return source;
        if (!Application.isPlaying) return null;
        GameObject go = new GameObject("MinigameSfx");
        Object.DontDestroyOnLoad(go);
        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }
}
