using UnityEngine;

[CreateAssetMenu(
    fileName = "NewSpell",
    menuName = "TCG/Cards/Spell"
)]
public class SpellData : CardData
{
    [Header("Spell Effect")]
    [SerializeField] private CardEffect spellEffect;

    public CardEffect SpellEffect => spellEffect;
}