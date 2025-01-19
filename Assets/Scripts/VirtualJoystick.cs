using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    // source: http://www.theappguruz.com/blog/beginners-guide-learn-to-make-simple-virtual-joystick-in-unity

    private Image joystickContainer;
    private Image joystick;

    public Vector3 InputDirection;


    // Start is called before the first frame update
    void Start()
    {
        joystickContainer = GetComponent<Image>();
        joystick = transform.GetChild(0).GetComponent<Image>();
        InputDirection = Vector3.zero;

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position = Vector2.zero;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickContainer.rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out position);

        position.x = (position.x / joystickContainer.rectTransform.sizeDelta.x);
        position.y = (position.y / joystickContainer.rectTransform.sizeDelta.y);

        float x = (joystickContainer.rectTransform.pivot.x == 1f) ? position.x * 2 + 1 : position.x * 2 - 1;
        float y = (joystickContainer.rectTransform.pivot.y == 1f) ? position.y * 2 + 1 : position.y * 2 - 1;

        InputDirection = new Vector3(x, y, 0);
        InputDirection = (InputDirection.magnitude > 1) ? InputDirection.normalized : InputDirection;

        joystick.rectTransform.anchoredPosition = new Vector3(InputDirection.x * (joystickContainer.rectTransform.sizeDelta.x / 3),
            InputDirection.y * (joystickContainer.rectTransform.sizeDelta.y / 3));

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        InputDirection = Vector3.zero;
        joystick.rectTransform.anchoredPosition = Vector3.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }
}
