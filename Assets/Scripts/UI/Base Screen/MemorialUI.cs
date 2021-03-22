using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine.Components.World;
using LCS.Engine;

public class MemorialUI : MonoBehaviour {

    public PortraitImage i_Portrait;
    public Text t_Epitaph;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void init(UIControllerImpl uiController, LiberalCrimeSquad.Memorial memorial)
    {
        Dictionary<string, string> damagedOrgans = new Dictionary<string, string>();
        foreach(string s in memorial.damagedOrgans)
        {
            damagedOrgans.Add(s, "DESTROYED");
        }
        i_Portrait.buildPortrait(memorial.portrait, memorial.old, damagedOrgans);
        t_Epitaph.text = MasterController.GetMC().getTranslation("BASE_martyr_epitaph").Replace("$CAUSE", memorial.causeOfDeath.Replace("$NAME", memorial.name)).Replace("DATE", memorial.timeOfDeath.ToString("D"));
    }
}
