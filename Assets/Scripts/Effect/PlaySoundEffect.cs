using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Play Sound")]
public class PlaySoundEffect : CardEffect
{
    [SerializeField]
    private AudioClip clip;
    public AudioClip Clip => clip;

    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (clip == null)
        {
            Debug.LogWarning(name + ": assign the intended sound clip.");
            return;
        }
        GameAudio.PlaySfx(clip);
    }
}
