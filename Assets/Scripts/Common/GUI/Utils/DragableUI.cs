using UnityEngine;
using UnityEngine.EventSystems;

public class DragableUI : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    Vector2 offset;

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        offset = ((RectTransform)transform).anchoredPosition - eventData.pressPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        ((RectTransform)transform).anchoredPosition = eventData.position + offset;
    }
}
