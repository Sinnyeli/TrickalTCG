#if UNITY_EDITOR
public static class AllCardEffectsInitializer
{
    public static void Build() => CardInitializationBuilder.InitializeAll();
}
#endif
