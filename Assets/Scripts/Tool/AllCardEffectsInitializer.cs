using UnityEditor;
using UnityEngine;

public static class AllCardEffectsInitializer
{
    [MenuItem("Tools/Cards/Initialize All Available Card Effects")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        CardInitializationBuilder.Build();
        TriggerEffectInitializer.Build();
        Debug.Log("Initialization tools finished. Check the Console for errors and the initialization report for skipped cards. Unsupported abilities remain pending.");
    }
}
