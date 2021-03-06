using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AttributePoint : MonoBehaviour {

    private bool maxed = false;

    public RawImage i_Point_BG;
    public RawImage i_Point;

    [Range(0,1)]
    public float progress;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        i_Point.rectTransform.sizeDelta = new Vector2(5,progress*10);

        if(progress == 1 && !maxed)
        {
            i_Point_BG.rectTransform.localPosition = new Vector3(i_Point_BG.rectTransform.localPosition.x - 2, i_Point_BG.rectTransform.localPosition.y + 2, i_Point_BG.rectTransform.localPosition.z);
            i_Point_BG.rectTransform.sizeDelta = new Vector2(9, 14);
            maxed = true;
        }
	}
}
