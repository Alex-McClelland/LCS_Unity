using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseOverText : MonoBehaviour {

    public string mouseOverText;

    void Awake()
    {
        EventTrigger trigger = GetComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((data) => { showMouseOverText(); });
        trigger.triggers.Add(entry);
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerExit;
        entry.callback.AddListener((data) => { clearMouseOverText(); });
        trigger.triggers.Add(entry);
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void showMouseOverText()
    {
        UIControllerImpl.tooltip.setText(mouseOverText);
    }

    public void clearMouseOverText()
    {
        UIControllerImpl.tooltip.setText("");
    }
}
