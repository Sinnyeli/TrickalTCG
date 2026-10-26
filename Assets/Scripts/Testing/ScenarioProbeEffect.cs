#if UNITY_EDITOR
using System.Collections.Generic;

// Test-only effect that records each time it resolves, so scenarios can check that a trigger fired.
public class ScenarioProbeEffect : CardEffect
{
    public int TimesResolved { get; private set; }
    public RuntimeCard LastSource { get; private set; }

    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        TimesResolved++;
        LastSource = source;
    }
}
#endif
