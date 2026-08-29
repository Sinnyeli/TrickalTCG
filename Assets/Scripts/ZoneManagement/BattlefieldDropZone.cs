using UnityEngine;
using UnityEngine.EventSystems;

public class BattlefieldDropZone : MonoBehaviour
{
    public bool IsPointerInside(PointerEventData eventData)
    {
        RectTransform rectTransform =
            GetComponent<RectTransform>();

        return RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera
        );
    }
}