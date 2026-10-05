using UnityEngine;

public class GhostMusicController : MonoBehaviour
{
    public static GhostMusicController Instance { get; private set; }
    [SerializeField] private AudioSource musicSource;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Debug.LogWarning("Multiple GhostMusicController instances."); return; }
        Instance = this;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public void PlayTrack(AudioClip track)
    {
        if (musicSource == null || track == null) { Debug.LogWarning("Kisha needs a music AudioSource and an assigned track."); return; }
        musicSource.clip = track; musicSource.loop = false; musicSource.Play();
    }
}
