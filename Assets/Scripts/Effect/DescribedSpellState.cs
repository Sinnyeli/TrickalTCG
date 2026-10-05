using System.Collections.Generic;
using UnityEngine;
public class DescribedSpellState : MonoBehaviour
{
    private static DescribedSpellState instance;
    private readonly HashSet<PlayerSide> blockedUntil = new HashSet<PlayerSide>();
    private readonly HashSet<RuntimeCard> killRefresh = new HashSet<RuntimeCard>();
    private readonly Dictionary<RuntimeCard, RuntimeCard> marked = new Dictionary<RuntimeCard, RuntimeCard>();
    private readonly Dictionary<RuntimeCard, List<RuntimeCard>> weapons = new Dictionary<RuntimeCard, List<RuntimeCard>>();
    public static bool AttacksBlocked => instance != null && instance.blockedUntil.Count > 0;
    private static DescribedSpellState Get()
    {
        if (instance == null) instance = new GameObject("Temporary Spell Rules").AddComponent<DescribedSpellState>();
        return instance;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { instance = null; }
    public static void BlockAttacksUntil(PlayerSide owner) { Get().blockedUntil.Add(owner); }
    public static void RefreshOnKill(RuntimeCard unit) { Get().killRefresh.Add(unit); }
    public static void MarkTarget(RuntimeCard source, RuntimeCard target) { Get().marked[target] = source; }
    public static void GrantWeapon(RuntimeCard source, RuntimeCard unit)
    {
        var state = Get();
        if (!state.weapons.TryGetValue(unit, out var sources)) state.weapons[unit] = sources = new List<RuntimeCard>();
        sources.Add(source);
    }
    public static void ClearUnit(RuntimeCard unit)
    {
        if (instance == null) return;
        instance.weapons.Remove(unit); instance.killRefresh.Remove(unit); instance.marked.Remove(unit);
    }
    public static void StartTurn(PlayerSide side)
    {
        if (instance == null) return;
        instance.blockedUntil.Remove(side);
        instance.killRefresh.Clear(); instance.marked.Clear();
    }
    public static void BeforeAttack(RuntimeCard attacker, RuntimeCard defender)
    {
        if (instance == null || attacker == null) return;
        if (instance.weapons.TryGetValue(attacker, out var weaponSources))
            foreach (var weaponSource in weaponSources) attacker.AddModifier(new RuntimeModifier(1, 1, true, weaponSource));
        if (defender != null && instance.marked.TryGetValue(defender, out RuntimeCard source) && attacker.Owner == source.Owner)
            attacker.AddTimedModifier(new RuntimeModifier(2, 0, true, source), 1);
    }
    public static void AfterAttack(RuntimeCard attacker, RuntimeCard defender)
    {
        if (instance != null && attacker != null && defender != null && defender.CurrentHealth <= 0 &&
            attacker.CurrentHealth > 0 && instance.killRefresh.Contains(attacker)) attacker.EnableAttack();
    }
}
