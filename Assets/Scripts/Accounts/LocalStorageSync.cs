using System.Runtime.InteropServices;
public static class LocalStorageSync
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void TrickalFlushLocalStorage();
#endif
    public static void Flush()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        TrickalFlushLocalStorage();
#endif
    }
}
