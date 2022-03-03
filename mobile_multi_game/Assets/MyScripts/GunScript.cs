using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
public class GunScript : MonoBehaviour
{
   
    public Transform transform_icon;
    Vector2 MousePosition;
    Camera camera;
    private void Start()
    {
        camera = GameObject.Find("Main Camera").GetComponent<Camera>();
        transform_icon= GameObject.Find("gun").GetComponent<Transform>();
    }
    //CodeFinder 코드파인더
    //From https://codefinder.janndk.com/ 
    private void Update()
    {
        Vector2 mousePos = Input.mousePosition;
      //  MousePosition = camera.ScreenToWorldPoint(MousePosition);
        transform_icon.position = mousePos;

      
    }
}
