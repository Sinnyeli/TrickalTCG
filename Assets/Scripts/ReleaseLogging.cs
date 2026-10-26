using UnityEngine;

// Release builds keep warnings and errors but drop the Debug.Log chatter from the player log
// and the browser console. Development builds and the Editor log everything.
public static class ReleaseLogging
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Configure()
    {
        if (!Application.isEditor && !Debug.isDebugBuild)
            Debug.unityLogger.filterLogType = LogType.Warning;
    }
}
