using UnityEngine;
using UnityEngine.SceneManagement;

// One music source and one sound-effect source for the whole game, both following the
// Settings volumes. Clips come from Resources/BattleUXResources; empty slots are silent.
public class GameAudio : MonoBehaviour
{
    private static GameAudio instance;
    private static BattleUXResources clips;
    private AudioSource music;
    private AudioSource effects;

    public static BattleUXResources Clips =>
        clips != null ? clips : clips = Resources.Load<BattleUXResources>("BattleUXResources");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        clips = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() => Get();

    private static GameAudio Get()
    {
        if (instance != null || !Application.isPlaying)
            return instance;
        // Awake registers the instance, so anything it calls back into Get() finds it
        // instead of creating another GameAudio (which recursed until Unity crashed).
        new GameObject("Game Audio").AddComponent<GameAudio>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        music = CreateSource(loop: true);
        effects = CreateSource(loop: false);
        ApplyVolumes();
        GameSettings.Changed += ApplyVolumes;
        SceneManager.sceneLoaded += SceneLoaded;
        PlaySceneMusic(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;
        GameSettings.Changed -= ApplyVolumes;
        SceneManager.sceneLoaded -= SceneLoaded;
    }

    public static void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
            return;
        GameAudio audio = Get();
        if (audio != null)
            audio.effects.PlayOneShot(clip, volumeScale);
    }

    // Keeps the current track playing when it is asked for again (for example, between menu scenes).
    public static void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null)
            return;
        GameAudio audio = Get();
        if (audio == null || (audio.music.clip == clip && audio.music.isPlaying))
            return;
        audio.music.clip = clip;
        audio.music.loop = loop;
        audio.music.Play();
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode) => PlaySceneMusic(scene);

    private static void PlaySceneMusic(Scene scene)
    {
        if (Clips == null)
            return;
        PlayMusic(scene.name == "Battlefield" ? Clips.battleMusic : Clips.menuMusic);
    }

    private void ApplyVolumes()
    {
        music.volume = GameSettings.MusicVolume;
        effects.volume = GameSettings.SfxVolume;
    }

    private AudioSource CreateSource(bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0;
        source.loop = loop;
        return source;
    }
}
