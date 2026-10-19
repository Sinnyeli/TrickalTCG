using TMPro;
using UnityEngine;

public class BattleUXResources : ScriptableObject
{
    public TMP_FontAsset menuFont;
    public TextAsset localization;

    // Every slot is optional: leave one empty and that moment stays silent.
    [Header("Music")]
    public AudioClip menuMusic;
    public AudioClip battleMusic;

    [Header("Battle Sounds")]
    public AudioClip turnStartSound;
    public AudioClip turnTimerWarningSound;
    public AudioClip cardPlayedSound;
    public AudioClip attackHitSound;
    public AudioClip damageSound;
    public AudioClip healSound;
    public AudioClip unitDiedSound;
    public AudioClip victorySound;
    public AudioClip defeatSound;

    [Header("Menu Sounds")]
    public AudioClip buttonClickSound;
}
