using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalkBubbleChild : MonoBehaviour {

    public TalkBubble talkBubbleParent;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void DestroySelf()
    {
        talkBubbleParent.destroySelf();
    }
}
