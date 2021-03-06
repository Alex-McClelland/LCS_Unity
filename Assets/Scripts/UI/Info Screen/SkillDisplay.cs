using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.Creature;

public class SkillDisplay : MonoBehaviour {

    public GameObject point;

    public Text t_Skill;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void SetSkill(CreatureBase.Skill skill)
    {
        t_Skill.text = GameData.getData().skillList[skill.type].name;
        if(skill.level == 0)
        {
            t_Skill.color = Color.gray;
        }

        if (skill.level >= skill.associatedAttribute.getModifiedValue())
        {
            t_Skill.color = Color.cyan;
        }

        for (int i = 0; i < skill.level && i < 20; i++)
        {
            GameObject newPoint = Instantiate(point);
            newPoint.transform.SetParent(transform, false);
            newPoint.GetComponent<RectTransform>().localPosition = new Vector3(100 + i * 12 + ((i / 10) * 5), -3, 0);
            newPoint.GetComponent<AttributePoint>().progress = 1;  
        }

        for(int i = skill.level; i < skill.associatedAttribute.getModifiedValue() && i < 20; i++)
        {
            GameObject newPoint = Instantiate(point);
            newPoint.transform.SetParent(transform, false);
            newPoint.GetComponent<RectTransform>().localPosition = new Vector3(100 + i * 12 + ((i / 10) * 5), -3, 0);

            if (i == skill.level)
            {
                newPoint.GetComponent<AttributePoint>().progress = ((float)skill.experience) / (100 + 10*skill.level);
            }
            else
            {
                newPoint.GetComponent<AttributePoint>().progress = 0;
            }
        }
    }
}
