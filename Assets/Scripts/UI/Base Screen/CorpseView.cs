using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.Creature;

public class CorpseView : MonoBehaviour {

    public SafeHouseView safehouseController;

    public Text t_Health;
    public Text t_Name;

    public PortraitImage i_Portrait;

    private Entity character { get; set; }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void displayCharacter(Entity e)
    {
        character = e;

        i_Portrait.buildPortrait(e);       

        t_Name.text = character.getComponent<CreatureInfo>().getName();

        Body body = character.getComponent<Body>();

        t_Health.text = body.getHealthStatusText(true);
    }

    public void select()
    {
        safehouseController.select(character);
    }
}
