using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;

public class ButtonHotkey : MonoBehaviour {

    public KeyCode key = KeyCode.None;
    public KeyCode altKey = KeyCode.None;
    public List<InputField> inputFields;
    public List<GameObject> blockers;
    public bool repeats = false;
    public float repeatTime;
    public float initialWaitTime = 1;

    private float timer;

    void OnEnable()
    {
        timer = initialWaitTime;
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (GetComponent<Button>() != null)
        {
            if (Input.GetKeyDown(key) || Input.GetKeyDown(altKey))
            {
                if (!blockerTest() && GetComponent<Button>().interactable)
                    MasterController.GetMC().uiController.doInput(GetComponent<Button>().onClick.Invoke);
            }
            else if (repeats && (Input.GetKey(key) || Input.GetKey(altKey)))
            {
                if (!blockerTest() && GetComponent<Button>().interactable)
                {
                    timer -= Time.deltaTime;

                    if (timer <= 0)
                    {
                        MasterController.GetMC().uiController.doInput(GetComponent<Button>().onClick.Invoke);
                        timer = repeatTime;
                    }
                }
            }
        }

        if (repeats && (Input.GetKeyUp(key) || Input.GetKeyUp(altKey)))
            timer = initialWaitTime;
    }

    private bool blockerTest()
    {
        foreach (InputField i in inputFields)
        {
            if (i.isFocused)
            {
                return true;
            }
        }

        foreach (GameObject g in blockers)
        {
            if (g.activeInHierarchy)
            {
                return true;
            }
        }

        return false;
    }
}
