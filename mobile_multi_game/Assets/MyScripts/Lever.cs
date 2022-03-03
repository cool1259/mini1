using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Lever : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    private RectTransform lever;
    private RectTransform background;

    private float leverRange = 140f;

    private Vector2 inputDirection;
    private bool isInput = false;

    private void Awake()
    {
        background = GetComponent<RectTransform>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        controlJoyStickLever(eventData);
        isInput = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        controlJoyStickLever(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        lever.anchoredPosition = Vector2.zero;
        isInput = false;
        
    }

    private void controlJoyStickLever(PointerEventData eventData)
    {
        Vector2 inputPos = eventData.position - background.anchoredPosition - background.sizeDelta/2;
        lever.anchoredPosition = inputPos.magnitude < leverRange ? inputPos : inputPos.normalized * leverRange;

        //Vector2 offset = rectTransform.sizeDelta / 2;
        //// Debug.Log("offset = " + offset);

        //Vector2 p = new Vector2(rectTransform.position.x, rectTransform.position.y);

        //var inputPos = eventData.position;

        //Debug.Log("eventData.position = " + eventData.position);
        //Debug.Log("rectTransform.anchoredPosition = " + rectTransform.anchoredPosition);
        //Debug.Log("inputPos = " + inputPos);
        //var inputVector = inputPos.magnitude < leverRange ? inputPos : inputPos.normalized * leverRange;
        //lever.anchoredPosition = inputVector;
        // inputDirection = inputVector / leverRange;
    }
}
