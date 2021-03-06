using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.Item;
using LCS.Engine.Components.Location;
using LCS.Engine.Data;

public class SquadMemberUI : MonoBehaviour {

    public Entity character { get; set; }

    public SquadUIImpl squadUI;

    public Text t_Name;
    public Text t_Health;
    public Text t_Weapon;
    public Text t_Armor;
    public PortraitImage i_Portrait;
    public PortraitImage i_HauledUnit;
    public Image i_SelectionBorder;

    public Transform bubbleRoot;
    public TalkBubble talkBubble;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void displayCharacter(Entity e)
    {
        if (e == null)
        {
            t_Name.gameObject.SetActive(false);
            t_Health.gameObject.SetActive(false);
            t_Weapon.gameObject.SetActive(false);
            t_Armor.gameObject.SetActive(false);
            i_Portrait.gameObject.SetActive(false);
            i_HauledUnit.gameObject.SetActive(false);
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

        if(e.getComponent<Liberal>().hauledUnit != null)
        {
            i_HauledUnit.buildPortrait(e.getComponent<Liberal>().hauledUnit);
            i_HauledUnit.gameObject.SetActive(true);
            string name;
            if (e.getComponent<Liberal>().hauledUnit.hasComponent<Liberal>())
                name = e.getComponent<Liberal>().hauledUnit.getComponent<CreatureInfo>().getName();
            else
                name = e.getComponent<Liberal>().hauledUnit.getComponent<CreatureInfo>().encounterName;

            i_HauledUnit.GetComponent<MouseOverText>().mouseOverText = name;
        }
        else
        {
            i_HauledUnit.gameObject.SetActive(false);
        }

        t_Name.text = character.getComponent<CreatureInfo>().getName();

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

        t_Weapon.text = inventory.getWeapon().getComponent<ItemBase>().getName(true);
        if (inventory.getWeapon().getComponent<Weapon>().clip != null) t_Weapon.text += "(" + inventory.getWeapon().getComponent<Weapon>().clip.getComponent<Clip>().ammo + ")";

        t_Armor.text = inventory.getArmor().getComponent<ItemBase>().getName(true);
        if(inventory.getArmor().getComponent<Armor>().quality > 1)
            t_Armor.text += "[" + inventory.getArmor().getComponent<Armor>().quality + "]";
        if (inventory.getArmor().getComponent<Armor>().damaged)
            t_Armor.text += "[D]";
        if (inventory.getArmor().getComponent<Armor>().bloody)
            t_Armor.text += "[B]";

        if(MasterController.GetMC().phase == MasterController.Phase.TROUBLE && MasterController.GetMC().currentSiteModeScene != null)
        {
            int disguiseLevel = inventory.getDisguiseLevel();
            Position squadPosition = MasterController.GetMC().currentSiteModeScene.squadPosition;

            if(disguiseLevel == -1)
            {
                t_Armor.color = Color.red;
            }
            else if(e.getComponent<CreatureBase>().Location.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x, squadPosition.y].getComponent<TileBase>().restricted ||
                (e.getComponent<CreatureBase>().Location.getComponent<TroubleSpot>().getFlags() & LocationDef.TroubleSpotFlag.RESTRICTED) != 0)
            {
                if (disguiseLevel == 0)
                    t_Armor.color = Color.red;
                else if (disguiseLevel == 1)
                    t_Armor.color = Color.yellow;
                else if (disguiseLevel == 2)
                    t_Armor.color = Color.green;
            }
            else
            {
                t_Armor.color = Color.white;
            }

            int weaponLevel = inventory.checkWeaponDisguise();

            if(weaponLevel < 2)
            {
                if (weaponLevel == 1 && disguiseLevel > 0)
                {
                    t_Weapon.color = Color.green;
                }
                else
                {
                    t_Weapon.color = Color.red;
                }
            }
            else
            {
                t_Weapon.color = Color.white;
            }
        }
        else
        {
            t_Armor.color = Color.white;
            t_Weapon.color = Color.white;
        }
    }

    public void refresh()
    {
        displayCharacter(character);
    }

    public void select()
    {
        if(character != null)
            squadUI.selectSquadMember(character);
    }

    public void selectHauledUnit()
    {
        squadUI.selectSquadMember(character.getComponent<Liberal>().hauledUnit);
    }
}
