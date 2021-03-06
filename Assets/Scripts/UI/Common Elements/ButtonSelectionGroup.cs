using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSelectionGroup : MonoBehaviour {

    public List<Button> buttons;

    public Color c_SelectedDefault;
    public Color c_UnselectedDefault;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void ButtonSelect(int button)
    {
        if (button >= buttons.Count) return;

        foreach(Button b in buttons)
        {
            if (b.GetComponent<ButtonSelectionGroupChild>() != null)
            {
                b.GetComponent<ButtonSelectionGroupChild>().unselect();
            }
            else
            {
                b.image.color = c_UnselectedDefault;
            }
        }

        if (buttons[button].GetComponent<ButtonSelectionGroupChild>() != null)
        {
            buttons[button].GetComponent<ButtonSelectionGroupChild>().select();
        }
        else
        {
            buttons[button].image.color = c_SelectedDefault;
        }
    }

    public void refresh()
    {
        foreach(Button b in buttons)
        {
            if (b.GetComponent<ButtonSelectionGroupChild>() != null)
            {
                b.GetComponent<ButtonSelectionGroupChild>().refresh();
            }
        }
    }
}
