using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.Item;

public class EnemyInfo : MonoBehaviour {

    public Entity character { get; set; }

    public EnemyUIImpl enemyUI;

    public Text t_Name;
    public Text t_Health;
    public Text t_Weapon;
    public Text t_Armor;
    public PortraitImage i_Portrait;
    public Transform bubbleRoot;
    public TalkBubble talkBubble;
    public Image i_SelectionBorder;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void displayCharacter(Entity e)
    {
        if(e == null)
        {
            t_Name.gameObject.SetActive(false);
            t_Health.gameObject.SetActive(false);
            t_Weapon.gameObject.SetActive(false);
            t_Armor.gameObject.SetActive(false);
            i_Portrait.gameObject.SetActive(false);
            return;
        }
        else
        {
            t_Name.gameObject.SetActive(true);
            t_Health.gameObject.SetActive(true);
            t_Weapon.gameObject.SetActive(true);
            t_Armor.gameObject.SetActive(true);
            i_Portrait.gameObject.SetActive(true);
        }

        character = e;

        i_Portrait.buildPortrait(e);

        string nameColor = "<color=white>";
        if (character.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE) nameColor = "<color=red>";
        if (character.getComponent<CreatureInfo>().alignment == Alignment.LIBERAL) nameColor = "<color=lime>";
        t_Name.text = nameColor + character.getComponent<CreatureInfo>().encounterName + "</color>";

        Body body = character.getComponent<Body>();

        t_Health.text = body.getHealthStatusText(true);

        if (body.isBleeding())
        {
            t_Health.color = Color.red;
        }
        else
        {
            t_Health.color = Color.white;
        }

        Inventory inventory = character.getComponent<Inventory>();

        if (character.getComponent<CreatureInfo>().inCombat ||
            inventory.getWeapon().getComponent<Weapon>().getSize() > inventory.getArmor().getComponent<Armor>().getConcealmentSize())
        {
            t_Weapon.text = inventory.getWeapon().getComponent<ItemBase>().getName(true);
        }
        else
        {
            t_Weapon.text = inventory.naturalWeapon.getComponent<ItemBase>().getName(true);
        }

        t_Armor.text = inventory.getArmor().getComponent<ItemBase>().getName(true);
    }

    public void refresh()
    {
        displayCharacter(character);
    }

    public void select()
    {
        if(character != null)
            enemyUI.selectSquadMember(character);
    }
}
