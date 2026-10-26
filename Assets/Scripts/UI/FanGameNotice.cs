using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A small "unofficial fan game" line along the bottom of the login and title screens.
public class FanGameNotice : MonoBehaviour
{
    private static FanGameNotice instance;
    private GameObject overlay;
    private TextMeshProUGUI label;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null)
            return;
        instance = new GameObject("Fan Game Notice").AddComponent<FanGameNotice>();
        DontDestroyOnLoad(instance.gameObject);
    }

    private void Awake()
    {
        if (LocalizationManager.Instance == null)
            new GameObject("Localization Manager").AddComponent<LocalizationManager>();
        Build();
        SceneManager.sceneLoaded += SceneLoaded;
        LocalizationManager.Instance.OnLanguageChanged += LanguageChanged;
        Refresh(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= LanguageChanged;
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode) => Refresh(scene);

    private void LanguageChanged() => Refresh(SceneManager.GetActiveScene());

    private void Refresh(Scene scene)
    {
        bool show = scene.name == "Login" || scene.name == "TitleScreen";
        overlay.SetActive(show);
        if (!show)
            return;
        bool korean =
            LocalizationManager.Instance != null
            && LocalizationManager.Instance.CurrentLanguage == GameLanguage.Korean;
        label.text = korean
            ? "비공식 팬 게임입니다. 트릭컬 RE:VIVE의 권리는 에피드게임즈에 있습니다."
            : "Unofficial fan game. Trickal RE:VIVE belongs to Epid Games.";
    }

    private void Build()
    {
        overlay = new GameObject(
            "Fan Notice Overlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );
        overlay.transform.SetParent(transform, false);
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 29000;
        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;

        RectTransform rect = new GameObject(
            "Label",
            typeof(RectTransform)
        ).GetComponent<RectTransform>();
        rect.SetParent(overlay.transform, false);
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(.5f, 0);
        rect.anchoredPosition = new Vector2(0, 12);
        rect.sizeDelta = new Vector2(-40, 32);

        label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        var resources = Resources.Load<BattleUXResources>("BattleUXResources");
        label.font = resources == null ? TMP_Settings.defaultFontAsset : resources.menuFont;
        label.fontSize = 18;
        label.color = new Color(1, 1, 1, .7f);
        label.alignment = TextAlignmentOptions.Bottom;
        label.raycastTarget = false;
    }
}
