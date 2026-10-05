using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class LocalLoginSceneBuilder
{
    [MenuItem("Tools/Accounts/Create Login Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        const string path = "Assets/Scenes/Login.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
        { Debug.Log("Login scene already exists; it has been preserved."); AddBuildScene(path); return; }
        if (TMP_Settings.defaultFontAsset == null)
        { Debug.LogError("Import TMP Essential Resources before creating the Login scene."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        var canvasObject = new GameObject("Login Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        var panel = Box("Login Panel", canvasObject.transform, new Vector2(460, 390), Vector2.zero);
        panel.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.23f);
        Label("Login / Register", panel.transform, new Vector2(400, 48), new Vector2(0, 135), 30);
        var id = Input("ID", panel.transform, 65); id.characterLimit = 32;
        var password = Input("Password", panel.transform, 0); password.characterLimit = 128; password.contentType = TMP_InputField.ContentType.Password;
        var login = MakeButton("Login", panel.transform, -100, -75);
        var register = MakeButton("Register", panel.transform, 100, -75);
        var status = Label("", panel.transform, new Vector2(410, 60), new Vector2(0, -145), 18);
        var ui = panel.AddComponent<LoginUI>(); var serialized = new SerializedObject(ui);
        serialized.FindProperty("idInput").objectReferenceValue = id; serialized.FindProperty("passwordInput").objectReferenceValue = password;
        serialized.FindProperty("loginButton").objectReferenceValue = login; serialized.FindProperty("registerButton").objectReferenceValue = register;
        serialized.FindProperty("statusText").objectReferenceValue = status; serialized.ApplyModifiedPropertiesWithoutUndo();
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        EditorSceneManager.SaveScene(scene, path); AddBuildScene(path); AssetDatabase.SaveAssets();
        Debug.Log("Login scene created with ID/password, Login and Register. It is the first enabled build scene.");
    }
    private static void AddBuildScene(string path)
    {
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(path, true) };
        scenes.AddRange(EditorBuildSettings.scenes.Where(scene => scene.path != path)); EditorBuildSettings.scenes = scenes.ToArray();
    }
    private static GameObject Box(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size; rect.anchoredPosition = position; return go;
    }
    private static TextMeshProUGUI Label(string text, Transform parent, Vector2 size, Vector2 position, int fontSize)
    {
        var go = new GameObject(text.Length == 0 ? "Status" : text, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); rect.sizeDelta = size; rect.anchoredPosition = position;
        var label = go.GetComponent<TextMeshProUGUI>(); label.font = TMP_Settings.defaultFontAsset; label.text = text; label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center; label.color = Color.white; label.raycastTarget = false; return label;
    }
    private static TMP_InputField Input(string title, Transform parent, float y)
    {
        var go = Box(title + " Input", parent, new Vector2(390, 50), new Vector2(0, y));
        go.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f);
        var input = go.AddComponent<TMP_InputField>();
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)); viewport.transform.SetParent(go.transform, false);
        var viewportRect = viewport.GetComponent<RectTransform>(); viewportRect.anchorMin = Vector2.zero; viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(14, 3); viewportRect.offsetMax = new Vector2(-14, -3);
        var text = Label("", viewport.transform, new Vector2(360, 44), Vector2.zero, 22);
        text.color = Color.black; text.alignment = TextAlignmentOptions.MidlineLeft;
        var placeholder = Label(title, viewport.transform, new Vector2(360, 44), Vector2.zero, 22); placeholder.color = Color.gray;
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        input.textViewport = viewportRect; input.textComponent = text; input.placeholder = placeholder; input.targetGraphic = go.GetComponent<Image>();
        return input;
    }
    private static Button MakeButton(string title, Transform parent, float x, float y)
    {
        var go = Box(title + " Button", parent, new Vector2(180, 50), new Vector2(x, y)); go.GetComponent<Image>().color = new Color(0.24f, 0.46f, 0.70f);
        var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>(); Label(title, go.transform, new Vector2(170, 44), Vector2.zero, 22); return button;
    }
}
