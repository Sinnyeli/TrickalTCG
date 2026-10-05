using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatFailureUI : MonoBehaviour
{
    private static CombatFailureUI instance;
    private CanvasGroup group;
    private TextMeshProUGUI label;
    private string reason;
    private float expires;
    private const float Duration = 2.8f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    public static string Message(string reason)
    {
        var localization = LocalizationManager.Instance;
        bool korean = localization != null && localization.CurrentLanguage == GameLanguage.Korean;
        string fallback;
        switch (reason)
        {
            case "NOT_TURN": fallback = korean ? "내 턴이 아닙니다." : "It is not your turn."; break;
            case "ENEMY_UNIT": fallback = korean ? "내 유닛으로만 공격할 수 있습니다." : "You can only attack with your own units."; break;
            case "NOT_READY": fallback = korean ? "이 유닛은 아직 공격할 수 없습니다." : "This unit cannot attack yet."; break;
            case "FROZEN": fallback = korean ? "빙결된 유닛은 공격할 수 없습니다." : "Frozen units cannot attack."; break;
            case "ALREADY_ATTACKED": fallback = korean ? "이 유닛은 이번 턴에 이미 공격했습니다." : "This unit has already attacked this turn."; break;
            case "BLOCKED": fallback = korean ? "명상의 시간으로 공격이 제한됩니다." : "Attacks are blocked by Meditation Time."; break;
            case "NO_ATTACKER": fallback = korean ? "먼저 공격할 내 유닛을 선택하세요." : "Select one of your units to attack first."; break;
            case "INVALID_UNIT": fallback = korean ? "이 유닛은 더 이상 전장에 없습니다." : "This unit is no longer on the battlefield."; break;
            case "NO_TARGET": fallback = korean ? "공격 대상을 선택하세요." : "Choose a target to attack."; break;
            case "STEALTH": fallback = korean ? "은신 유닛은 공격할 수 없습니다." : "You cannot attack a Stealth unit."; break;
            case "OWN_UNIT": fallback = korean ? "내 유닛은 공격할 수 없습니다." : "You cannot attack your own unit."; break;
            case "OWN_PLAYER": fallback = korean ? "내 플레이어는 공격할 수 없습니다." : "You cannot attack your own player."; break;
            case "TAUNT_UNIT": fallback = korean ? "도발 유닛을 먼저 공격하세요." : "Attack a Taunt unit first."; break;
            case "TAUNT_HERO": fallback = korean ? "상대 플레이어를 공격하려면 도발 유닛을 먼저 공격해야 합니다." : "You must attack a Taunt unit before the opposing player."; break;
            case "RUSH": fallback = korean ? "속공은 이번 턴에 유닛만 공격할 수 있습니다." : "Rush only allows attacks against units this turn."; break;
            default: fallback = korean ? "공격할 수 없습니다." : "Cannot attack."; break;
        }
        if (localization == null) return fallback;
        string key = "COMBAT_FAIL_" + reason;
        string text = localization.Get(key);
        return string.IsNullOrWhiteSpace(text) || text == key ? fallback : text;
    }

    public static void Show(string reason)
    {
        if (instance == null) instance = new GameObject("Combat Failure Notice").AddComponent<CombatFailureUI>();
        instance.reason = reason;
        instance.label.text = Message(reason);
        instance.expires = Time.unscaledTime + Duration;
        instance.group.alpha = 1;
    }

    private void Awake()
    {
        var canvasObject = new GameObject("Notice Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 24000;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        group = canvasObject.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false; group.interactable = false; group.alpha = 0;
        var panel = new GameObject("Notice", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        var rect = (RectTransform)panel.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(0, 15); rect.sizeDelta = new Vector2(850, 76);
        var image = panel.GetComponent<Image>(); image.color = new Color(.12f, .08f, .08f, .92f); image.raycastTarget = false;
        var textObject = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);
        var textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20, 8); textRect.offsetMax = new Vector2(-20, -8);
        label = textObject.GetComponent<TextMeshProUGUI>();
        var resources = Resources.Load<BattleUXResources>("BattleUXResources");
        label.font = resources != null && resources.menuFont != null ? resources.menuFont : TMP_Settings.defaultFontAsset;
        label.fontSize = 28; label.enableAutoSizing = true; label.fontSizeMin = 18; label.fontSizeMax = 28;
        label.alignment = TextAlignmentOptions.Center; label.color = new Color(1, .85f, .7f); label.raycastTarget = false;
        if (LocalizationManager.Instance != null) LocalizationManager.Instance.OnLanguageChanged += RefreshLanguage;
    }

    private void RefreshLanguage() { if (reason != null) label.text = Message(reason); }
    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) { group.alpha = 0; return; }
        group.alpha = EscapeMenu.IsOpen ? 0 : Mathf.Clamp01((expires - Time.unscaledTime) / .3f);
    }
    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null) LocalizationManager.Instance.OnLanguageChanged -= RefreshLanguage;
        if (instance == this) instance = null;
    }
}
