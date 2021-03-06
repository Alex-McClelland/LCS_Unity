using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;

public class MessageLog : MonoBehaviour {

    public Button expando;
    public Transform content;
    public MessageLogEntry p_MessageLogEntry;
    public string stream;
    public int ageLimit;

    public bool expanded = false;

    private Vector3 originalPosition;
    private Vector2 originalSize;
    private List<MessageLogEntry> messageList;

    // Use this for initialization
    void Start () {
        
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Expando()
    {
        if (!expanded)
        {
            RectTransform rect = GetComponent<RectTransform>();

            originalPosition = rect.localPosition;
            originalSize = rect.sizeDelta;

            //rect.localPosition = new Vector3(0, 0);
            rect.sizeDelta = new Vector2(originalSize.x, 460);

            expando.GetComponentInChildren<Text>().text = "v";
            expanded = true;
        }
        else
        {
            RectTransform rect = GetComponent<RectTransform>();

            rect.localPosition = originalPosition;
            rect.sizeDelta = originalSize;

            expando.GetComponentInChildren<Text>().text = "^";
            expanded = false;
        }
    }

    public void updateMessageLog()
    {
        List<MasterController.LogMessage> log = null;

        if (stream == "messageLog") log = MasterController.GetMC().messageLog;
        else if (stream == "combatLog") log = MasterController.GetMC().combatLog;
        else if (stream == "debugLog") log = MasterController.GetMC().debugLog;

        if (messageList == null)
        {
            messageList = new List<MessageLogEntry>();
            for(int i = 0; i < MasterController.MAX_LOG_SIZE; i++)
            {
                MessageLogEntry entry = Instantiate(p_MessageLogEntry);
                entry.transform.SetParent(content.transform, false);
                messageList.Add(entry);
            }
        }

        for (int i = 0; i < MasterController.MAX_LOG_SIZE; i++)
        {
            MessageLogEntry entry = messageList[i];
            entry.gameObject.SetActive(false);

            if (i >= log.Count) continue;
            if (log[Math.Max(0, log.Count - MasterController.MAX_LOG_SIZE) + i].age > ageLimit) continue;

            entry.text.text = log[Math.Max(0, log.Count - MasterController.MAX_LOG_SIZE) + i].text;
            entry.age = log[Math.Max(0, log.Count - MasterController.MAX_LOG_SIZE) + i].age;
            if (entry.age == 0)
            {
                if (log[Math.Max(0, log.Count - MasterController.MAX_LOG_SIZE) + i].priority)
                    entry.text.fontStyle = FontStyle.Bold;
                else
                    entry.text.fontStyle = FontStyle.Normal;
                entry.text.color = Color.white;
            }
            if(entry.age > 0)
            {
                entry.text.fontStyle = FontStyle.Normal;
                entry.text.color = Color.gray;
                entry.text.text = UIControllerImpl.dimColors(entry.text.text);
            }

            entry.gameObject.SetActive(true);
        }        
    }
}
