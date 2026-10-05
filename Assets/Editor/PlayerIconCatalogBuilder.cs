using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PlayerIconCatalogBuilder
{
    static PlayerIconCatalogBuilder() { EditorApplication.delayCall += Refresh; }
    [MenuItem("Tools/Accounts/Refresh Player Icons")]
    public static void Refresh()
    {
        const string path = "Assets/Resources/PlayerIconCatalog.asset";
        if (!AssetDatabase.IsValidFolder("Assets/Img/PlayerIcon")) return;
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        var icons = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Img/PlayerIcon" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p)
            .Select(AssetDatabase.LoadAssetAtPath<Texture2D>).Where(t => t != null).ToArray();
        var catalog = AssetDatabase.LoadAssetAtPath<PlayerIconCatalog>(path);
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<PlayerIconCatalog>(); AssetDatabase.CreateAsset(catalog, path); }
        if (catalog.icons.SequenceEqual(icons)) return;
        catalog.icons = icons; EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
    }
}
public class PlayerIconCatalogPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(p => p.StartsWith("Assets/Img/PlayerIcon/")))
            EditorApplication.delayCall += PlayerIconCatalogBuilder.Refresh;
    }
}
