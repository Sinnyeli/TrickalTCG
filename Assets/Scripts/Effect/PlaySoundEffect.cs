using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Play Sound")]
public class PlaySoundEffect : CardEffect
{
    [SerializeField] private AudioClip clip;
    public AudioClip Clip => clip;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (clip == null) { Debug.LogWarning(name + ": assign the intended sound clip."); return; }
        var sound = new GameObject("Card sound: " + clip.name);
        var audio = sound.AddComponent<AudioSource>(); audio.playOnAwake = false;
        audio.spatialBlend = 0; audio.loop = false; audio.clip = clip; audio.Play();
        Object.Destroy(sound, clip.length + 0.1f);
    }
}
