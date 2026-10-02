using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuild
{
    private const string OutputDirectory = "Builds/WebGL";

    [MenuItem("TCG/Build WebGL")]
    public static void Build()
    {
        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
            throw new BuildFailedException("Enable at least one scene in Build Settings.");

        Directory.CreateDirectory(OutputDirectory);

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputDirectory,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("WebGL build failed. Check the Editor log for details.");

        Debug.Log($"WebGL build ready at {Path.GetFullPath(OutputDirectory)}");
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenes.Add(scene.path);
        }

        return scenes.ToArray();
    }
}
