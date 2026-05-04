using System.Collections.Generic;
using UnityEngine;

public class SfxPlayer : MonoBehaviour
{
    public static SfxPlayer Instance { get; private set; }

    [Range(0f, 1f)] public float volume = 0.8f;

    AudioSource source;
    Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("SfxPlayer");
        go.AddComponent<SfxPlayer>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    public static void Play(string clipName, float volumeScale = 1f)
    {
        if (Instance == null) return;
        Instance.PlayInternal(clipName, volumeScale);
    }

    void PlayInternal(string clipName, float volumeScale)
    {
        if (!cache.TryGetValue(clipName, out var clip))
        {
            clip = Resources.Load<AudioClip>(clipName);
            if (clip == null)
            {
                Debug.LogWarning($"[SfxPlayer] Clip '{clipName}' not found in Assets/Resources/");
                return;
            }
            cache[clipName] = clip;
        }
        source.PlayOneShot(clip, volume * volumeScale);
    }
}
