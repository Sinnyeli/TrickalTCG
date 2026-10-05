using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchmakingNotice : MonoBehaviour
{
    private TMP_Text text;
    private Button cancel;
    public static MatchmakingNotice Show(string message, UnityEngine.Events.UnityAction cancelAction)
    {
        var go = new GameObject("Matchmaking", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var notice = go.AddComponent<MatchmakingNotice>();
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; go.GetComponent<Canvas>().sortingOrder = 26000;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080);
        var panel = new GameObject("Panel",typeof(RectTransform),typeof(Image)); panel.transform.SetParent(go.transform,false);
        var rect = (RectTransform)panel.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0,0,0,.8f);
        var label = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI)); label.transform.SetParent(panel.transform,false);
        var lr = (RectTransform)label.transform; lr.anchorMin = lr.anchorMax = new Vector2(.5f,.5f); lr.sizeDelta = new Vector2(900,180); lr.anchoredPosition = new Vector2(0,50);
        notice.text = label.GetComponent<TextMeshProUGUI>();
        var resources = Resources.Load<BattleUXResources>("BattleUXResources");
        notice.text.font = resources != null && resources.menuFont != null ? resources.menuFont : TMP_Settings.defaultFontAsset;
        notice.text.fontSize = 30; notice.text.alignment = TextAlignmentOptions.Center; notice.text.text = message;
        var button = new GameObject("Cancel",typeof(RectTransform),typeof(Image),typeof(Button)); button.transform.SetParent(panel.transform,false);
        var br = (RectTransform)button.transform; br.anchorMin = br.anchorMax = new Vector2(.5f,.5f); br.sizeDelta = new Vector2(300,65); br.anchoredPosition = new Vector2(0,-100);
        notice.cancel = button.GetComponent<Button>(); notice.cancel.onClick.AddListener(cancelAction);
        var caption = new GameObject("Label", typeof(RectTransform),typeof(TextMeshProUGUI)); caption.transform.SetParent(button.transform,false);
        var cr = (RectTransform)caption.transform; cr.anchorMin=Vector2.zero;cr.anchorMax=Vector2.one;cr.offsetMin=cr.offsetMax=Vector2.zero;
        var ct = caption.GetComponent<TextMeshProUGUI>();ct.font=notice.text.font;ct.fontSize=26;ct.color=Color.black;ct.text="Cancel / 취소";ct.alignment=TextAlignmentOptions.Center;
        return notice;
    }
    public void ShowError(string message)
    {
        text.text = message; cancel.onClick.RemoveAllListeners(); cancel.onClick.AddListener(() => Destroy(gameObject));
        var caption = cancel.GetComponentInChildren<TextMeshProUGUI>(); if (caption != null) caption.text="Close / 닫기";
    }
}
