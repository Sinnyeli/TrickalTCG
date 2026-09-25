using UnityEngine;
using UnityEngine.EventSystems;

public class DeckEditorDropZone :
    MonoBehaviour,
    IDropHandler
{
    [SerializeField]
    private DeckEditorDropType dropType;

    public DeckEditorDropType DropType =>
        dropType;

    public void OnDrop(
        PointerEventData eventData)
    {
        // Drop handling happens through
        // DeckEditorCardView.OnEndDrag.
    }
}