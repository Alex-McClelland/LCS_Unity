using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;

public class TalkBubble : MonoBehaviour {

    public Text t_text;    
    public Image i_bubble;
    public Animator animator;

    public float textTimeDelay;
    public bool isShowing;
    public int lineLength;

    public Sprite i_LeftTail;
    public Sprite i_RightTail;

    private int textIterator = int.MaxValue;
    private float timer;
    private float displayTime;
    private bool useDisplayTimer;
    private string fullText = "";
    private Vector2 targetPosition;
    private Direction side;

	// Use this for initialization
	void Start () {
        isShowing = false;
	}
	
	// Update is called once per frame
	void Update () {
		if(textIterator < fullText.Length)
        {
            timer -= Time.deltaTime;
            setPosition();

            if (timer < 0)
            {
                textIterator++;
                timer = textTimeDelay;
                t_text.text = fullText.Substring(0, textIterator);
            }

            if (textIterator == fullText.Length) isShowing = true;
        }

        if(isShowing && useDisplayTimer && displayTime > 0)
        {
            displayTime -= Time.deltaTime;
        }

        if(useDisplayTimer && displayTime < 0)
        {
            hideText();
        }
	}

    public void showText(string text, Vector3 position, Direction side, float displayTime = -1)
    {
        this.side = side;

        if (side == Direction.RIGHT)
        {
            i_bubble.sprite = i_RightTail;
            RectTransform rectTransform = (RectTransform) i_bubble.transform;
            rectTransform.pivot = new Vector2(1, 0);
        }
        else i_bubble.sprite = i_LeftTail;

        text = UIControllerImpl.breakLines(text, lineLength);

        fullText = text;
        this.displayTime = displayTime;
        if (displayTime == -1) useDisplayTimer = false;
        else useDisplayTimer = true;

        targetPosition = position;
        setPosition();

        textIterator = 0;
        timer = textTimeDelay;
        animator.SetBool("show", true);
    }

    public void hideText()
    {
        animator.SetBool("show", false);
        isShowing = false;
    }

    public void destroySelf()
    {
        Destroy(gameObject);
    }

    private void setPosition()
    {
        transform.position = targetPosition;

        float maxWidth;
        float minWidth;

        if(side == Direction.RIGHT)
        {
            maxWidth = (transform.parent.GetComponent<RectTransform>().rect.width / 2);
            minWidth = (transform.parent.GetComponent<RectTransform>().rect.width / 2) + (transform.GetChild(0).GetComponent<RectTransform>().rect.width);
        }
        else
        {
            maxWidth = (transform.parent.GetComponent<RectTransform>().rect.width / 2) - (transform.GetChild(0).GetComponent<RectTransform>().rect.width);
            minWidth = (transform.parent.GetComponent<RectTransform>().rect.width / 2);
        }
            
        float maxHeight = (transform.parent.GetComponent<RectTransform>().rect.height / 2) - (transform.GetChild(0).GetComponent<RectTransform>().rect.height);
        float minHeight = (transform.parent.GetComponent<RectTransform>().rect.height / 2);

        if (transform.localPosition.x > maxWidth) transform.localPosition = new Vector3(maxWidth, transform.localPosition.y);
        if (transform.localPosition.x < -minWidth) transform.localPosition = new Vector3(-minWidth, transform.localPosition.y);
        if (transform.localPosition.y > maxHeight) transform.localPosition = new Vector3(transform.localPosition.x, maxHeight);
        if (transform.localPosition.y < -minHeight) transform.localPosition = new Vector3(transform.localPosition.x, -minHeight);
    }
}
