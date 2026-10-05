using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class SpellChoiceUI : MonoBehaviour
{
    public static int AutomaticDepth;
    private PlayerSide owner;
    private List<RuntimeCard> choices;
    private Action<RuntimeCard> callback;
    private int page;
    private GameObject canvas;
    private static readonly Queue<Action> pending = new Queue<Action>();
    private static bool choosing;
    private static SpellChoiceUI current;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { pending.Clear(); choosing = false; AutomaticDepth = 0; current = null; }
    public static void Choose(PlayerSide owner, List<RuntimeCard> choices, Action<RuntimeCard> callback)
    {
        choices.RemoveAll(c => c == null);
        if (choices.Count == 0) return;
        if (AutomaticDepth > 0)
        { callback(choices[UnityEngine.Random.Range(0, choices.Count)]); return; }
        string choiceID = null;
        if (!UnityRemoteMatch.IsGuest)
        {
            choiceID = BattleChoiceRequests.Register(owner, choices, callback);
            if (owner == PlayerSide.Opponent) return;
            callback = card => BattleActions.Submit(owner, BattleActionKind.ChooseCard, target: card, choice: choiceID);
        }
        Action show = () =>
        {
            choosing = true;
            var ui = new GameObject("Spell Choice").AddComponent<SpellChoiceUI>();
            current = ui; ui.owner = owner; ui.choices = choices; ui.callback = callback; ui.Build();
        };
        if (choosing) pending.Enqueue(show); else show();
    }
    private void Update()
    {
        var game = GameManager.Instance;
        if (game == null || game.IsGameOver || (!game.TurnManager.IsMyTurn(owner) && !BattleChoiceRequests.IsPendingOwner(owner) && !(UnityRemoteMatch.IsGuest && UnityRemoteMatch.Instance.HasRemoteChoice))) { pending.Clear(); Destroy(gameObject); }
    }
    private void OnDestroy() { if (current == this) { current = null; choosing = false; pending.Clear(); } }
    private void Build()
    {
        if (canvas != null) Destroy(canvas);
        canvas = new GameObject("Choice Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().sortingOrder = 25000;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
        var shade = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));shade.transform.SetParent(canvas.transform,false);
        var rect = (RectTransform)shade.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;shade.GetComponent<Image>().color=new Color(0,0,0,.8f);
        var resources = Resources.Load<BattleUXResources>("BattleUXResources");
        for (int i = page*7; i < Mathf.Min(choices.Count, page*7+7); i++)
        {
            RuntimeCard selected = choices[i];
            string name = LocalizationManager.Instance == null ? selected.Data.cardName : LocalizationManager.Instance.GetCardName(selected.Data);
            Button(name, 200-(i-page*7)*60, () =>
            {
                var action = callback; choosing = false; current = null;
                gameObject.SetActive(false); Destroy(gameObject);
                action(selected);
                if (!choosing && pending.Count>0) pending.Dequeue()();
            }, resources);
        }
        if(page>0) Button("Previous / 이전",-250,()=>{page--;Build();},resources);
        if((page+1)*7<choices.Count) Button("Next / 다음",-310,()=>{page++;Build();},resources);
    }
    private void Button(string title,float y,UnityEngine.Events.UnityAction action,BattleUXResources resources)
    {
        var go=new GameObject("Choice",typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(canvas.transform,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(520,50);rect.anchoredPosition=new Vector2(0,y);
        go.GetComponent<Image>().color=new Color(.25f,.3f,.4f);go.GetComponent<Button>().onClick.AddListener(action);
        var label=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(go.transform,false);
        var lr=(RectTransform)label.transform;lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=lr.offsetMax=Vector2.zero;
        var text=label.GetComponent<TextMeshProUGUI>();text.text=title;text.font=resources==null?TMP_Settings.defaultFontAsset:resources.menuFont;text.fontSize=24;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
    }
}
