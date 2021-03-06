using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickAndDrag : MonoBehaviour {

    private Vector2 clickPosition;
    private Vector3 mousePosition;
    private bool maybeDragging = false;
    private bool stopDragNextFrame = false;

    public bool dragging = false;

    // Use this for initialization
    void Start () {
       
    }
	
	// Update is called once per frame
	void Update () {
        if ((mousePosition != Input.mousePosition) && maybeDragging)
        {
            dragging = true;
        }

        if (dragging)
        {
            Vector2 localPosition = new Vector2();
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform.parent.transform, Input.mousePosition, Camera.main, out localPosition);

            transform.localPosition = localPosition - clickPosition;

            float maxWidth = (transform.parent.GetComponent<RectTransform>().rect.width/2) - (GetComponent<RectTransform>().rect.width/2);
            float maxHeight = (transform.parent.GetComponent<RectTransform>().rect.height/2) - (GetComponent<RectTransform>().rect.height/2);

            if (transform.localPosition.x > maxWidth) transform.localPosition = new Vector3(maxWidth, transform.localPosition.y);
            if (transform.localPosition.x < -maxWidth) transform.localPosition = new Vector3(-maxWidth, transform.localPosition.y);
            if (transform.localPosition.y > maxHeight) transform.localPosition = new Vector3(transform.localPosition.x, maxHeight);
            if (transform.localPosition.y < -maxHeight) transform.localPosition = new Vector3(transform.localPosition.x, -maxHeight);

            if (stopDragNextFrame)
            {
                dragging = false;
                stopDragNextFrame = false;
            }
        }
    }

    public void mouseDown()
    {
        if (Input.GetMouseButton(0))
        {
            maybeDragging = true;
            mousePosition = Input.mousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, Input.mousePosition, Camera.main, out clickPosition);
        }
    }

    public void mouseUp()
    {
        maybeDragging = false;
        stopDragNextFrame = true;
    }
}
