using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class MoveJoyStick : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
{
    [SerializeField]
    private RectTransform lever;
    private RectTransform rectTransform;

    [SerializeField]
    private float multiplier = 2.7f;


    [SerializeField,Range(10,150)]
    private float leverRange;

    private Vector2 inputDirection;
    private bool isInput = false;

    public GameObject MyPlayer;

    public enum JoystickType { Move,Aim};
    public JoystickType joystickType;
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        controlJoyStickLever(eventData);
        isInput = true;
    }

    //드래그해서 마우스 멈추고있는동안은 이벤트안됨 그래서 isInput써서 update 에다 해야됨
    public void OnDrag(PointerEventData eventData)
    {
        controlJoyStickLever(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        lever.anchoredPosition = Vector2.zero;
        isInput = false;

        if (MyPlayer)
        {
            if(joystickType==JoystickType.Move)
               MyPlayer.GetComponent<playerScript>().Move(Vector2.zero);

            else if (joystickType == JoystickType.Aim)
                MyPlayer.GetComponent<playerScript>().aimMove(Vector2.zero);
        }

    }

    private void controlJoyStickLever(PointerEventData eventData)
    {

        var inputPos = eventData.position - rectTransform.anchoredPosition; // /2-new Vector2(100f,100f);
       // Debug.Log(rectTransform.anchoredPosition);
        var inputVector = inputPos.magnitude < leverRange ? inputPos : inputPos.normalized * leverRange;
        lever.anchoredPosition = inputVector/ multiplier; // 여기서 나누기 2를 할거라고!! 알았냐고~!! 레버레인지는 최대값 환경이라고~
        // 인풋값이 아니라고~~~
        inputDirection = inputVector / leverRange;
    }

    private void inputControlVector()
    {
        if (MyPlayer)
        {
            if(joystickType==JoystickType.Move)
            MyPlayer.GetComponent<playerScript>().Move(inputDirection);

            else if (joystickType == JoystickType.Aim)
                MyPlayer.GetComponent<playerScript>().aimMove(inputDirection);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isInput)
            inputControlVector();
    }
}
