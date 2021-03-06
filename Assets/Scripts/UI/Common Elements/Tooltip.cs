using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour {

    public Text t_tooltip;

    public int lineLength;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        setPosition();
    }

    public void setPosition()
    {
        Vector2 localPosition = new Vector2();
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform.parent.transform, Input.mousePosition, Camera.main, out localPosition);

        this.transform.localPosition = localPosition;

        float maxWidth = (transform.parent.GetComponent<RectTransform>().rect.width / 2) - (GetComponent<RectTransform>().rect.width);
        float maxHeight = (transform.parent.GetComponent<RectTransform>().rect.height / 2) - (GetComponent<RectTransform>().rect.height);
        float minWidth = (transform.parent.GetComponent<RectTransform>().rect.width / 2);
        float minHeight = (transform.parent.GetComponent<RectTransform>().rect.height / 2);

        if (transform.localPosition.x > maxWidth) transform.localPosition = new Vector3(maxWidth, transform.localPosition.y);
        if (transform.localPosition.x < -minWidth) transform.localPosition = new Vector3(-minWidth, transform.localPosition.y);
        if (transform.localPosition.y > maxHeight) transform.localPosition = new Vector3(transform.localPosition.x, maxHeight);
        if (transform.localPosition.y < -minHeight) transform.localPosition = new Vector3(transform.localPosition.x, -minHeight);
    }

    public void setText(string text)
    {
        text = UIControllerImpl.breakLines(text, lineLength);

        t_tooltip.text = text;

        if (text != "")
        {
            setPosition();
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
