using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSelectionGroupChild : MonoBehaviour {

    public Color c_UnselectedColor;
    public Color c_SelectedColor;

    public Color c_UnselectedTextColor;
    public Color c_SelectedTextColor;

    public bool selected;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void select()
    {
        Button button = GetComponent<Button>();
        selected = true;

        button.image.color = c_SelectedColor;
        button.GetComponentInChildren<Text>().color = c_SelectedTextColor;
    }

    public void unselect()
    {
        Button button = GetComponent<Button>();
        selected = false;

        button.image.color = c_UnselectedColor;
        button.GetComponentInChildren<Text>().color = c_UnselectedTextColor;
    }

    public void refresh()
    {
        if (selected)
        {
            Button button = GetComponent<Button>();
            button.image.color = c_SelectedColor;
            button.GetComponentInChildren<Text>().color = c_SelectedTextColor;
        }
        else
        {
            Button button = GetComponent<Button>();
            button.image.color = c_UnselectedColor;
            button.GetComponentInChildren<Text>().color = c_UnselectedTextColor;
        }
    }
}
