using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer Instance { get; private set; }

    [Range(0f, 1f)] public float volume = 0.5f;

    AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("MusicPlayer");
        go.AddComponent<MusicPlayer>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        var clip = Resources.Load<AudioClip>("level-music");
        if (clip == null)
        {
            Debug.LogWarning("[MusicPlayer] background-music.wav not found in Assets/Resources/");
            return;
        }

        source             = gameObject.AddComponent<AudioSource>();
        source.clip        = clip;
        source.loop        = true;
        source.playOnAwake = false;
        source.volume      = volume;
        source.Play();
    }
}
