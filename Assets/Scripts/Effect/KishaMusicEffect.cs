using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Kisha Music")]
public class KishaMusicEffect : CardEffect
{
    [SerializeField] private AudioClip track;
    public AudioClip Track => track;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (GhostMusicController.Instance == null) { Debug.LogWarning("Kisha needs GhostMusicController in the gameplay scene."); return; }
        GhostMusicController.Instance.PlayTrack(track);
    }
}
