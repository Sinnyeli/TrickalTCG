using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardHoverZoom : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private bool hovered;
    private Vector3 baseline, applied;
    private bool enlarged;
    private Canvas hoverCanvas;
    public void OnPointerEnter(PointerEventData e) { hovered = true; }
    public void OnPointerExit(PointerEventData e) { hovered = false; Restore(); }
    private void LateUpdate()
    {
        CardView card = GetComponent<CardView>();
        if (!hovered || (card != null && card.IsDragging)) { Restore(); return; }
        if (!enlarged || transform.localScale != applied) baseline = transform.localScale;
        float multiplier = card != null ? 1.35f : 1.2f;
        applied = baseline * multiplier;
        transform.localScale = applied;
        enlarged = true;
        if (hoverCanvas == null)
        {
            hoverCanvas = GetComponent<Canvas>();
            if (hoverCanvas == null) hoverCanvas = gameObject.AddComponent<Canvas>();
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }
        hoverCanvas.overrideSorting = true;
        hoverCanvas.sortingOrder = 100;
    }
    private void Restore()
    {
        if (enlarged && transform.localScale == applied) transform.localScale = baseline;
        enlarged = false;
        if (hoverCanvas != null) hoverCanvas.overrideSorting = false;
    }
    private void OnDisable() { hovered = false; Restore(); }
}
