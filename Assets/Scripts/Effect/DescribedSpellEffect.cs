using System.Collections.Generic;
using UnityEngine;

public enum DescribedSpellKind { Endure, SleeveIn, Morale, Surprise, Meditation, Picnic, Feat, Soda, Membership, AllEndure, Sprint, MasterCombat, MarkEnemy, AttackBuff, OpenRun, EarlyLeave, Weapon, Potion }
public class DescribedSpellEffect : CardEffect
{
    [SerializeField] private DescribedSpellKind kind;
    [SerializeField] private int amount;
    [SerializeField] private List<SpellData> randomSpells = new List<SpellData>();
    [SerializeField] private List<CardData> potionPool = new List<CardData>();
    public bool CanPlay(RuntimeCard source)
    {
        if (source == null || GameManager.Instance == null) return false;
        var game = GameManager.Instance;
        var hand = new List<RuntimeCard>(game.GetHandManager(source.Owner).Hand);
        if (kind == DescribedSpellKind.Membership) return hand.Exists(c => c != source && c.Zone == CardZone.Hand);
        if (kind == DescribedSpellKind.SleeveIn)
            return hand.Exists(c => c.Data is ArtifactData && c.Zone == CardZone.Hand) &&
                new List<RuntimeCard>(game.GetBattlefield(source.Owner).Minions).Exists(c => c.Data is ApostleData && c.EquippedArtifacts.Count < c.GetMaxEquipment());
        return true;
    }
    public override bool CanTarget(RuntimeCard source, RuntimeCard target) =>
        source != null && target != null && target.Zone == CardZone.Field && target.CurrentHealth > 0 &&
        (TargetType != EffectTargetType.FriendlyUnit || target.Owner == source.Owner) &&
        (TargetType != EffectTargetType.EnemyUnit || target.Owner != source.Owner) && base.CanTarget(source, target);
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        var game = GameManager.Instance; if (source == null || game == null || game.IsGameOver) return;
        var friendly = game.GetBattlefield(source.Owner);
        var enemy = game.GetBattlefield(source.Owner == PlayerSide.Player ? PlayerSide.Opponent : PlayerSide.Player);
        switch (kind)
        {
            case DescribedSpellKind.Potion:
                var potions = potionPool.FindAll(c => c != null);
                if (potions.Count > 0) game.GetHandManager(source.Owner).AddGeneratedCard(potions[Random.Range(0, potions.Count)], source.Owner);
                return;
            case DescribedSpellKind.SleeveIn:
                var artifacts = new List<RuntimeCard>(game.GetHandManager(source.Owner).Hand).FindAll(c => c.Data is ArtifactData && c.Zone == CardZone.Hand);
                SpellChoiceUI.Choose(source.Owner, artifacts, artifact =>
                {
                    var apostles = new List<RuntimeCard>(friendly.Minions).FindAll(c => c.Data is ApostleData && c.EquippedArtifacts.Count < c.GetMaxEquipment());
                    SpellChoiceUI.Choose(source.Owner, apostles, unit =>
                    {
                        if (artifact.Zone != CardZone.Hand || unit.Zone != CardZone.Field || unit.EquippedArtifacts.Count >= unit.GetMaxEquipment()) return;
                        var hand = game.GetHandManager(source.Owner);
                        if (!hand.RemoveCardFromHand(artifact)) return;
                        if (!unit.EquipArtifact(artifact)) { artifact.ChangeZone(CardZone.Hand); hand.AddCard(artifact); }
                        friendly.RefreshMinionView(unit);
                    });
                }); return;
            case DescribedSpellKind.Membership:
                SpellChoiceUI.Choose(source.Owner, new List<RuntimeCard>(game.GetHandManager(source.Owner).Hand), card =>
                { if (card.Zone == CardZone.Hand) { card.ModifyManaCost(-2); game.GetHandManager(source.Owner).RefreshAllCardViews(); } }); return;
            case DescribedSpellKind.Meditation: DescribedSpellState.BlockAttacksUntil(source.Owner); return;
            case DescribedSpellKind.Morale:
                foreach (var unit in new List<RuntimeCard>(friendly.Minions))
                {
                    if (game.IsGameOver) break;
                    var candidates = new List<RuntimeCard>(enemy.Minions).FindAll(c => c.CurrentHealth > 0 && !c.IsStealthed);
                    if (candidates.Exists(c => c.HasKeyword(CardKeyword.Taunt))) candidates.RemoveAll(c => !c.HasKeyword(CardKeyword.Taunt));
                    if (candidates.Count > 0) game.CombatManager.ForceUnitAttack(unit, candidates[Random.Range(0, candidates.Count)]);
                } return;
            case DescribedSpellKind.Surprise: CastRandomSpells(source); return;
            case DescribedSpellKind.Picnic:
                foreach (var unit in new List<RuntimeCard>(friendly.Minions))
                    if (friendly.BounceCard(unit)) { unit.ModifyManaCost(-1); game.GetHandManager(source.Owner).RefreshCardView(unit); }
                return;
            case DescribedSpellKind.AllEndure:
                foreach (var unit in new List<RuntimeCard>(friendly.Minions)) unit.GrantRuntimeKeyword(CardKeyword.Endure, 0);
                foreach (var unit in friendly.Minions) friendly.RefreshMinionView(unit); return;
            case DescribedSpellKind.OpenRun: game.AddTemporaryNextCardDiscount(source.Owner, 2); return;
        }
        if (targets == null) return;
        foreach (var unit in targets)
        {
            if (!CanTarget(source, unit)) continue;
            switch (kind)
            {
                case DescribedSpellKind.Endure: unit.GrantRuntimeKeyword(CardKeyword.Endure, 0); break;
                case DescribedSpellKind.Soda: unit.AddTimedModifier(new RuntimeModifier(0, 3, true, source), 1); break;
                case DescribedSpellKind.AttackBuff: unit.AddTimedModifier(new RuntimeModifier(amount, 0, true, source), 1); break;
                case DescribedSpellKind.Sprint:
                    unit.AddTimedModifier(new RuntimeModifier(4, 4, true, source), 1);
                    unit.GrantRuntimeKeyword(CardKeyword.Rush, 1); unit.ScheduleDeathAtTurnEnd(source); break;
                case DescribedSpellKind.Weapon: DescribedSpellState.GrantWeapon(source, unit); break;
                case DescribedSpellKind.MasterCombat: DescribedSpellState.RefreshOnKill(unit); break;
                case DescribedSpellKind.MarkEnemy: DescribedSpellState.MarkTarget(source, unit); break;
                case DescribedSpellKind.EarlyLeave:
                    if (friendly.BounceCard(unit)) { unit.ModifyManaCost(-1); game.GetHandManager(source.Owner).RefreshCardView(unit); } break;
                case DescribedSpellKind.Feat:
                    var candidates = new List<RuntimeCard>(enemy.Minions).FindAll(c => c.CurrentHealth > 0 && !c.IsStealthed);
                    if (candidates.Count > 0)
                    {
                        var target = candidates[Random.Range(0, candidates.Count)];
                        bool damage = target.TakeDamage(unit.CurrentHealth, source);
                        if (damage && target.CurrentHealth > 0) game.EffectManager.ResolveOnDamageTaken(target);
                        game.CombatManager.CheckDeath(target); enemy.RefreshMinionView(target);
                    } break;
            }
            if (unit.Zone == CardZone.Field) game.GetBattlefield(unit.Owner).RefreshMinionView(unit);
        }
    }
    private bool CanAutoCast(PlayerSide owner, SpellData spell)
    {
        var game = GameManager.Instance;
        var cast = new RuntimeCard(spell); cast.SetOwner(owner); cast.ChangeZone(CardZone.Hand);
        foreach (var effect in spell.SpellEffects)
        {
            if (effect == null) continue;
            if (effect is DescribedSpellEffect described && !described.CanPlay(cast)) return false;
            var type = effect.TargetType;
            bool manual = type == EffectTargetType.FriendlyUnit || type == EffectTargetType.EnemyUnit ||
                type == EffectTargetType.AnyUnit || type == EffectTargetType.AnyTarget || type == EffectTargetType.EnemyTarget;
            if (!manual) continue;
            bool valid = (effect is DamageEffect || effect is HealEffect) &&
                (type == EffectTargetType.AnyTarget || type == EffectTargetType.EnemyTarget);
            foreach (PlayerSide side in new[] { PlayerSide.Player, PlayerSide.Opponent })
                foreach (var unit in game.GetBattlefield(side).Minions)
                    if (!unit.IsStealthed && unit.CurrentHealth > 0 && effect.CanTarget(cast, unit) &&
                        (type != EffectTargetType.FriendlyUnit || side == owner) &&
                        (type != EffectTargetType.EnemyUnit && type != EffectTargetType.EnemyTarget || side != owner)) valid = true;
            if (!valid) return false;
        }
        return true;
    }
    private void CastRandomSpells(RuntimeCard source)
    {
        var game = GameManager.Instance;
        var pool = randomSpells.FindAll(s => s != null && s != source.Data && s.SpellEffects.Count > 0);
        for (int i = 0; i < 5 && !game.IsGameOver && pool.Count > 0; i++)
        {
            var eligible = pool.FindAll(spellData => CanAutoCast(source.Owner, spellData));
            if (eligible.Count == 0) break;
            var spell = eligible[Random.Range(0, eligible.Count)];
            var cast = new RuntimeCard(spell); cast.SetOwner(source.Owner); cast.ChangeZone(CardZone.Hand);
            SpellChoiceUI.AutomaticDepth++;
            try
            {
                foreach (var effect in spell.SpellEffects)
                {
                    if (effect == null || game.IsGameOver || (effect is DescribedSpellEffect described && !described.CanPlay(cast))) continue;
                    var units = new List<RuntimeCard>();
                    foreach (PlayerSide side in new[] { PlayerSide.Player, PlayerSide.Opponent })
                        foreach (var unit in game.GetBattlefield(side).Minions)
                            if (!unit.IsStealthed && effect.CanTarget(cast, unit) &&
                                (effect.TargetType != EffectTargetType.FriendlyUnit || unit.Owner == cast.Owner) &&
                                (effect.TargetType != EffectTargetType.EnemyUnit && effect.TargetType != EffectTargetType.EnemyTarget || unit.Owner != cast.Owner)) units.Add(unit);
                    bool manual = effect.TargetType == EffectTargetType.FriendlyUnit || effect.TargetType == EffectTargetType.EnemyUnit || effect.TargetType == EffectTargetType.AnyUnit || effect.TargetType == EffectTargetType.AnyTarget || effect.TargetType == EffectTargetType.EnemyTarget;
                    if (manual)
                    {
                        var heroes = new List<PlayerView>();
                        if ((effect is DamageEffect || effect is HealEffect) &&
                            (effect.TargetType == EffectTargetType.AnyTarget || effect.TargetType == EffectTargetType.EnemyTarget))
                            foreach (PlayerSide side in new[] { PlayerSide.Player, PlayerSide.Opponent })
                                if (effect.TargetType == EffectTargetType.AnyTarget || side != cast.Owner)
                                { var hero = game.GetPlayerView(side); if (hero != null && hero.CurrentHealth > 0) heroes.Add(hero); }
                        int total = units.Count + heroes.Count;
                        if (total > 0)
                        {
                            int choice = Random.Range(0, total);
                            if (choice < units.Count) effect.Resolve(cast, new List<RuntimeCard> { units[choice] });
                            else if (effect is DamageEffect damage) damage.ResolveHero(cast, heroes[choice - units.Count]);
                            else if (effect is HealEffect heal) heal.ResolveHero(cast, heroes[choice - units.Count]);
                        }
                    }
                    else game.EffectManager.ResolveCardEffect(cast, effect);
                }
                game.RecordLastCastSpell(cast);
                RuntimeCard.PublishTrigger(CardTriggerType.SpellCast, cast, cast);
                game.EffectManager.TriggerResonance(cast.Owner);
            }
            finally { SpellChoiceUI.AutomaticDepth--; }
        }
    }
}
