using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewSpell",
    menuName = "TCG/Cards/Spell"
)]
public class SpellData : CardData
{
    [Header("Spell Effects")]
    [SerializeField]
    private List<CardEffect> spellEffects =
        new List<CardEffect>();

    public IReadOnlyList<CardEffect> SpellEffects =>
        spellEffects;
}