using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine.Components.Creature;
using LCS.Engine;

public class AttributeDisplay : MonoBehaviour {

    public GameObject point;

    public Text t_Attribute;
    public int points;

    public Color point_base;
    public Color point_BG;

    public Color point_extra_base;
    public Color point_extra_BG;

    public Color point_low_base;
    public Color point_low_BG;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void SetAttribute(string name, int level, int baseLevel)
    {
        t_Attribute.text = name;
        points = level;

        if(level >= baseLevel)
        {
            for (int i = 0; i < baseLevel; i++)
            {
                GameObject newPoint = Instantiate(point);
                newPoint.transform.SetParent(transform, false);
                newPoint.GetComponent<RectTransform>().localPosition = new Vector3(100 + i * 12 + ((i / 10) * 5), -3, 0);
                newPoint.GetComponent<AttributePoint>().progress = 1;
                newPoint.GetComponent<AttributePoint>().i_Point.color = point_base;
                newPoint.GetComponent<AttributePoint>().i_Point_BG.color = point_BG;
            }

            for(int i = baseLevel; i < level; i++)
            {
                GameObject newPoint = Instantiate(point);
                newPoint.transform.SetParent(transform, false);
                newPoint.GetComponent<RectTransform>().localPosition = new Vector3(100 + i * 12 + ((i / 10) * 5), -3, 0);
                newPoint.GetComponent<AttributePoint>().progress = 1;
                newPoint.GetComponent<AttributePoint>().i_Point.color = point_extra_base;
                newPoint.GetComponent<AttributePoint>().i_Point_BG.color = point_extra_BG;
            }
        }
        else
        {
            for (int i = 0; i < level; i++)
            {
                GameObject newPoint = Instantiate(point);
                newPoint.transform.SetParent(transform, false);
                newPoint.GetComponent<RectTransform>().localPosition = new Vector3(100 + i * 12 + ((i / 10) * 5), -3, 0);
                newPoint.GetComponent<AttributePoint>().progress = 1;
                newPoint.GetComponent<AttributePoint>().i_Point.color = point_base;
                newPoint.GetComponent<AttributePoint>().i_Point_BG.color = point_BG;
            }

            for (int i = level; i < baseLevel; i++)
            {
                GameObject newPoint = Instantiate(point);
                newPoint.transform.SetParent(transform, false);
                newPoint.GetComponent<RectTransform>().localPosition = new Vector3(100 + i * 12 + ((i / 10) * 5), -3, 0);
                newPoint.GetComponent<AttributePoint>().progress = 1;
                newPoint.GetComponent<AttributePoint>().i_Point.color = point_low_base;
                newPoint.GetComponent<AttributePoint>().i_Point_BG.color = point_low_BG;
            }
        }
    }
}
