using UnityEngine;
using UnityEngine.EventSystems;

/* The idea is to be able to change the size of an UI element by dragging
 a rect area in the right-up corner (dragableRect).
     */
public class ResizableUI : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private Vector2 startingPoint;
    private Vector2 startingSize;
    private Vector2 minSize;
    public RectTransform resizableRect;

    void Start()
    {
        minSize = resizableRect.rect.size;
        resizableRect.anchoredPosition -= Vector2.Scale(resizableRect.rect.size, resizableRect.pivot);
        resizableRect.pivot = Vector2.zero;
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        startingPoint = eventData.pressPosition;
        startingSize = resizableRect.sizeDelta;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 newSize = eventData.position - startingPoint + startingSize;
        resizableRect.sizeDelta = new Vector2(Mathf.Max(newSize.x, minSize.x), Mathf.Max(newSize.y, minSize.y));
    }
}
