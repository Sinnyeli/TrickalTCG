#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class CardTestSceneBuilder : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.enabled && scene.path == ScenePath)
                throw new BuildFailedException("CardTest is an Editor-only sandbox. Remove it from Build Settings before making a player build.");
    }
    private const string ScenePath = "Assets/Scenes/CardTest.unity";
    [MenuItem("Tools/Cards/Open Card Test Scene")]
    public static void Open()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) Create();
        else EditorSceneManager.OpenScene(ScenePath);
    }
    [MenuItem("Tools/Cards/Rebuild Card Test From Battlefield")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("Rebuild Card Test", "Replace the sandbox scene with a fresh copy of Battlefield? Battlefield itself is unchanged.", "Rebuild", "Cancel")) return;
        var source = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Battlefield.unity");
        if (source == null) throw new InvalidOperationException("Battlefield scene is missing.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) AssetDatabase.DeleteAsset(ScenePath);
        if (!AssetDatabase.CopyAsset("Assets/Scenes/Battlefield.unity", ScenePath)) throw new InvalidOperationException("Could not copy Battlefield.");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var sandbox = new GameObject("Card Test Sandbox").AddComponent<CardTestSandbox>();
        sandbox.database = AssetDatabase.LoadAssetAtPath<CardDatabase>("Assets/Cards/Database/CardDatabase.asset");
        if (sandbox.database == null) throw new InvalidOperationException("CardDatabase is missing.");
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("CardTest ready. Press Play. No login, saved deck or matchmaking required.");
    }
}
#endif
