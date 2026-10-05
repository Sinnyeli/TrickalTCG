using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CombatPresentation : MonoBehaviour
{
    private static CombatPresentation instance;
    private RectTransform root;
    private AttackAimGraphic aim;
    private int activeAnimations;
    public static bool IsAnimating => instance != null && instance.activeAnimations > 0;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }
    public static CombatPresentation Get()
    {
        if (instance != null) return instance;
        GameObject go = new GameObject("Combat Presentation", typeof(RectTransform), typeof(Canvas));
        instance = go.AddComponent<CombatPresentation>();
        instance.root = (RectTransform)go.transform;
        Canvas canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200;
        return instance;
    }
    public static MinionViewBase FindView(RuntimeCard card)
    {
        foreach (MinionViewBase view in FindObjectsByType<MinionViewBase>(FindObjectsSortMode.None))
            if (view.isActiveAndEnabled && view.RuntimeCard == card) return view;
        return null;
    }
    public static void Attack(RuntimeCard attacker, Transform target)
    {
        MinionViewBase view = FindView(attacker);
        if (view != null && target != null) Get().StartCoroutine(Get().Lunge(view, target.position));
    }
    private IEnumerator Lunge(MinionViewBase view, Vector3 destination)
    {
        activeAnimations++;
        GameObject copy = CopyVisuals(view.transform, root);
        RectTransform visual = copy.GetComponent<RectTransform>();
        Canvas sourceCanvas = view.GetComponentInParent<Canvas>();
        Camera camera = sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? sourceCanvas.worldCamera : null;
        Vector2 screenStart = RectTransformUtility.WorldToScreenPoint(camera, view.transform.position);
        Vector2 screenEnd = RectTransformUtility.WorldToScreenPoint(camera, destination);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenStart, null, out Vector2 from);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenEnd, null, out Vector2 target);
        visual.anchorMin = visual.anchorMax = visual.pivot = new Vector2(.5f, .5f);
        visual.anchoredPosition = from;
        visual.localScale = view.transform.lossyScale;
        CanvasGroup group = view.GetComponent<CanvasGroup>();
        if (group == null) group = view.gameObject.AddComponent<CanvasGroup>();
        float originalAlpha = group.alpha; group.alpha = 0;
        try
        {
            Vector2 impact = Vector2.Lerp(from, target, .82f);
            float elapsed = 0;
            while (elapsed < .14f)
            {
                elapsed += Time.unscaledDeltaTime;
                visual.anchoredPosition = Vector2.Lerp(from, impact, Mathf.SmoothStep(0, 1, elapsed / .14f));
                yield return null;
            }
            elapsed = 0;
            while (elapsed < .20f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / .20f);
                float back = t - 1f;
                float bounce = 1f + 2.7f * back * back * back + 1.7f * back * back;
                visual.anchoredPosition = Vector2.LerpUnclamped(impact, from, bounce);
                yield return null;
            }
        }
        finally
        {
            if (group != null) group.alpha = originalAlpha;
            if (copy != null) Destroy(copy);
            activeAnimations--;
        }
    }
    // Build fresh graphics instead of instantiating card scripts, hover Canvases
    // and Raycasters. Missing scripts on a source prefab are never cloned.
    private static GameObject CopyVisuals(Transform source, Transform parent)
    {
        GameObject copy = new GameObject(source.name + " Visual", typeof(RectTransform));
        RectTransform rect = (RectTransform)copy.transform;
        rect.SetParent(parent, false);
        RectTransform original = source as RectTransform;
        if (original != null)
        {
            rect.anchorMin = original.anchorMin;
            rect.anchorMax = original.anchorMax;
            rect.pivot = original.pivot;
            rect.sizeDelta = original.sizeDelta;
            rect.anchoredPosition3D = original.anchoredPosition3D;
        }
        rect.localRotation = source.localRotation;
        rect.localScale = source.localScale;

        Image image = source.GetComponent<Image>();
        if (image != null)
        {
            Image visual = copy.AddComponent<Image>();
            visual.sprite = image.sprite; visual.color = image.color;
            visual.material = image.material; visual.type = image.type;
            visual.preserveAspect = image.preserveAspect;
            visual.fillCenter = image.fillCenter; visual.fillMethod = image.fillMethod;
            visual.fillAmount = image.fillAmount; visual.fillClockwise = image.fillClockwise;
            visual.fillOrigin = image.fillOrigin; visual.pixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier;
            visual.raycastTarget = false;
        }
        RawImage raw = source.GetComponent<RawImage>();
        if (raw != null)
        {
            RawImage visual = copy.AddComponent<RawImage>();
            visual.texture = raw.texture; visual.uvRect = raw.uvRect;
            visual.color = raw.color; visual.raycastTarget = false;
        }
        TMP_Text text = source.GetComponent<TMP_Text>();
        if (text != null)
        {
            TextMeshProUGUI visual = copy.AddComponent<TextMeshProUGUI>();
            visual.font = text.font; visual.fontSharedMaterial = text.fontSharedMaterial;
            visual.text = text.text; visual.color = text.color; visual.fontSize = text.fontSize;
            visual.fontStyle = text.fontStyle; visual.alignment = text.alignment;
            visual.enableAutoSizing = text.enableAutoSizing;
            visual.fontSizeMin = text.fontSizeMin; visual.fontSizeMax = text.fontSizeMax;
            visual.margin = text.margin; visual.richText = text.richText;
            visual.overflowMode = text.overflowMode; visual.raycastTarget = false;
        }
        Mask mask = source.GetComponent<Mask>();
        if (mask != null && image != null) copy.AddComponent<Mask>().showMaskGraphic = mask.showMaskGraphic;
        if (source.GetComponent<RectMask2D>() != null) copy.AddComponent<RectMask2D>();
        foreach (Transform child in source)
        {
            // TMP regenerates its own submeshes from the copied text/font.
            if (child.GetComponent<TMP_SubMeshUI>() == null) CopyVisuals(child, rect);
        }
        copy.SetActive(source.gameObject.activeSelf);
        return copy;
    }

    private void LateUpdate()
    {
        GameManager gm = GameManager.Instance;
        RuntimeCard selected = gm == null || gm.CombatManager == null ? null : gm.CombatManager.SelectedAttacker;
        bool visible = gm != null && !gm.IsGameOver && selected != null && selected.Owner == PlayerSide.Player &&
            selected.CanAttack && selected.Zone == CardZone.Field && gm.TurnManager.IsMyTurn(PlayerSide.Player) &&
            !EscapeMenu.IsOpen && (EffectTargetManager.Instance == null || !EffectTargetManager.Instance.IsSelectingTarget);
        if (!visible) { if (aim != null) aim.gameObject.SetActive(false); return; }
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return;
        Vector2 pointer = Mouse.current.position.ReadValue();
        bool cancel = Mouse.current.rightButton.wasPressedThisFrame;
#else
        Vector2 pointer = Input.mousePosition;
        bool cancel = Input.GetMouseButtonDown(1);
#endif
        if (cancel) { gm.CombatManager.ClearSelection(); return; }
        MinionViewBase view = FindView(selected); if (view == null) return;
        if (aim == null)
        {
            GameObject go = new GameObject("Attack Aim", typeof(RectTransform), typeof(AttackAimGraphic));
            go.transform.SetParent(root, false);
            aim = go.GetComponent<AttackAimGraphic>(); aim.raycastTarget = false; aim.color = new Color(1f, .65f, .15f);
        }
        aim.gameObject.SetActive(true);
        Canvas canvas = view.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, RectTransformUtility.WorldToScreenPoint(camera, view.transform.position), null, out Vector2 from);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, pointer, null, out Vector2 to);
        aim.SetEndpoints(from, to);
    }
}
