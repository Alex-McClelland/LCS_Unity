using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.Item;
using LCS.Engine.Components.Location;
using LCS.Engine.Data;

public class ItemButton : MonoBehaviour {

    public Entity item { get; set; }

    public Sprite no_icon;
    public Button button;

    public Image i_Icon;
    public Image i_VehicleIcon;
    public Text t_Name;
    public Text t_Count;

    public GameObject icon_Transfer;
    public GameObject icon_Bloody;
    public GameObject icon_Damaged;

    public Color black;
    public Color blue;
    public Color green;
    public Color red;
    public Color tan;
    public Color yellow;

    private int count;
    bool showCount;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void setItem(Entity item, int count = 1, bool showCount = false)
    {
        this.item = item;
        this.showCount = showCount;
        this.count = count;

        if (count > 1 || showCount)
            t_Count.text = count + "";
        else
            t_Count.text = "";

        t_Name.text = item.getComponent<ItemBase>().getName(true);

        if (item.hasComponent<Clip>())
            t_Name.text += "(" + item.getComponent<Clip>().ammo + ")";

        if (item.hasComponent<Armor>() && item.getComponent<Armor>().quality > 1)
            t_Name.text += "[" + item.getComponent<Armor>().quality + "]";

        if (item.hasComponent<Vehicle>())
        {
            i_VehicleIcon.gameObject.SetActive(true);
            if (((ItemDef.VehicleDef)GameData.getData().itemList[item.def].components["vehicle"]).vehicleIcon != null)
                i_VehicleIcon.sprite = ((ItemDef.VehicleDef)GameData.getData().itemList[item.def].components["vehicle"]).vehicleIcon;
            else
                i_VehicleIcon.sprite = null;

            if (i_VehicleIcon.sprite != null)
            {
                switch (item.getComponent<Vehicle>().color)
                {
                    case ItemDef.VehicleColor.BLACK:
                        i_VehicleIcon.color = black;
                        break;
                    case ItemDef.VehicleColor.BLUE:
                        i_VehicleIcon.color = blue;
                        break;
                    case ItemDef.VehicleColor.GREEN:
                        i_VehicleIcon.color = green;
                        break;
                    case ItemDef.VehicleColor.POLICE:
                        //This will just show the base icon colours since white doesn't blend
                        i_VehicleIcon.color = Color.white;
                        break;
                    case ItemDef.VehicleColor.RED:
                        i_VehicleIcon.color = red;
                        break;
                    case ItemDef.VehicleColor.TAN:
                        i_VehicleIcon.color = tan;
                        break;
                    case ItemDef.VehicleColor.WHITE:
                        i_VehicleIcon.color = Color.white;
                        break;
                    case ItemDef.VehicleColor.YELLOW:
                        i_VehicleIcon.color = yellow;
                        break;
                }
            }
            else
            {
                i_VehicleIcon.gameObject.SetActive(false);
            }
        }
        else
        {
            i_VehicleIcon.gameObject.SetActive(false);
        }

        i_Icon.gameObject.SetActive(true);
        if (!MasterController.GetMC().isFuture())
        {
            if (GameData.getData().itemList[item.def].icon != null)
                i_Icon.sprite = GameData.getData().itemList[item.def].icon;
            else
                i_Icon.sprite = no_icon;
        }
        else
        {
            if (GameData.getData().itemList[item.def].futureicon != null)
                i_Icon.sprite = GameData.getData().itemList[item.def].futureicon;
            else if(GameData.getData().itemList[item.def].icon != null)
                i_Icon.sprite = GameData.getData().itemList[item.def].icon;
            else
                i_Icon.sprite = no_icon;
        }

        string description = GameData.getData().itemList[item.def].description;
        if (item.hasComponent<Weapon>() && item.def != "WEAPON_NONE")
        {
            ItemDef.WeaponDef weaponData = (ItemDef.WeaponDef) GameData.getData().itemList[item.def].components["weapon"];

            string damageAmount = "NONE";
            if (weaponData.attack[0].fixed_damage + weaponData.attack[0].random_damage/2 > 100) damageAmount = "LETHAL";
            else if (weaponData.attack[0].fixed_damage + weaponData.attack[0].random_damage/2 > 75) damageAmount = "HIGH";
            else if (weaponData.attack[0].fixed_damage + weaponData.attack[0].random_damage/2 > 30) damageAmount = "MODERATE";
            else if (weaponData.attack[0].fixed_damage + weaponData.attack[0].random_damage/2 > 0) damageAmount = "LOW";

            string accuracy = "";
            if (weaponData.attack[0].accuracy_bonus > 0) accuracy = "GOOD";
            else if (weaponData.attack[0].accuracy_bonus < 0) accuracy = "BAD";

            string penetration = "NONE";
            if (weaponData.attack[0].armorpiercing > 5) penetration = "STRONG";
            else if (weaponData.attack[0].armorpiercing > 0) penetration = "MILD";

            description += "\nDamage: " + damageAmount;
            if(weaponData.attack[0].number_attacks > 1) description += "\nAttacks per turn: " + weaponData.attack[0].number_attacks;
            if (accuracy != "") description += "\nAccuracy: " + accuracy;
            description += "\nPenetration: " + penetration;
        }
        if (item.hasComponent<Armor>() && item.def != "ARMOR_NONE")
        {
            ItemDef.ArmorDef armorData = (ItemDef.ArmorDef)GameData.getData().itemList[item.def].components["armor"];

            foreach(string s in armorData.armor.Keys)
            {
                string armorAmt = "LOW";

                if (armorData.armor[s] > 6) armorAmt = "HIGH";
                else if (armorData.armor[s] > 3) armorAmt = "MODERATE";

                description += "\n" + GameData.getData().bodyPartList[s].name + " Armor: " + armorAmt;
            }

            if (armorData.stealth_value > 2) description += "\nVERY SNEAKY";
            else if (armorData.stealth_value > 1) description += "\nSNEAKY";

            string interrogationString = "";

            if (armorData.basePower > 5) interrogationString += "INSPIRING";
            else if (armorData.basePower > 2) interrogationString += "INTIMIDATING";
            else if (armorData.basePower > 0) interrogationString += "IMPOSING";

            if (armorData.assaultBonus > 5) interrogationString += ", TERRIFYING";
            else if (armorData.assaultBonus > 2) interrogationString += ", FRIGHTENING";
            else if (armorData.assaultBonus > 0) interrogationString += ", SCARY";

            if (armorData.drugBonus > 5) interrogationString += ", PSYCHEDELIC";
            else if (armorData.drugBonus > 2) interrogationString += ", UNSETTLING";
            else if (armorData.drugBonus > 0) interrogationString += ", ODD";

            interrogationString = interrogationString.TrimStart(new char[]{ ' ', ','});

            if (interrogationString != "") description += "\n" + interrogationString;
        }
        if (item.hasComponent<Vehicle>())
        {
            ItemDef.VehicleDef vehicleData = (ItemDef.VehicleDef)GameData.getData().itemList[item.def].components["vehicle"];

            string topSpeed = "SLOW";
            if (vehicleData.driveHardCap > 15) topSpeed = "VERY FAST";
            else if (vehicleData.driveHardCap > 12) topSpeed = "FAST";
            else if (vehicleData.driveHardCap > 8) topSpeed = "MODERATE";

            description += "\nTOP SPEED: " + topSpeed;

            string acceleration = "SLOW";
            if (vehicleData.driveBase + (vehicleData.driveSkill * vehicleData.driveSoftCap) > 20) acceleration = "VERY FAST";
            else if (vehicleData.driveBase + (vehicleData.driveSkill * vehicleData.driveSoftCap) > 12) acceleration = "FAST";
            else if (vehicleData.driveBase + (vehicleData.driveSkill * vehicleData.driveSoftCap) > 8) acceleration = "MODERATE";

            description += "\nACCELERATION: " + acceleration;

            string handling = "POOR";
            if (vehicleData.dodgeBase + (vehicleData.dodgeSkill * vehicleData.dodgeSoftCap) + vehicleData.dodgeHardCap > 20) handling = "VERY GOOD";
            else if (vehicleData.dodgeBase + (vehicleData.dodgeSkill * vehicleData.dodgeSoftCap) + vehicleData.dodgeHardCap > 12) handling = "GOOD";
            else if (vehicleData.dodgeBase + (vehicleData.dodgeSkill * vehicleData.dodgeSoftCap) + vehicleData.dodgeHardCap > 8) handling = "MODERATE";

            description += "\nHANDLING: " + handling;
        }

        while (Regex.Match(description, "\\$SWEARFILTER{.*?}").Success)
        {
            Match match = Regex.Match(description, "\\$SWEARFILTER{.*?}");
            string content = match.Value.Substring(match.Value.IndexOf('{')).Trim(new char[] { '{', '}' });
            string[] sections = content.Split(':');

            description = description.Replace(match.Value, MasterController.GetMC().swearFilter(sections[0], sections[1]));
        }

        if(item.getComponent<ItemBase>().targetBase != null)
        {
            description += "\nSending to " + item.getComponent<ItemBase>().targetBase.getComponent<SiteBase>().getCurrentName();
        }

        GetComponent<MouseOverText>().mouseOverText = description.TrimStart('\n');

        refresh();
    }

    public void changeCount(int amt)
    {
        count += amt;

        if (count > 1 || showCount)
            t_Count.text = count + "";
        else
            t_Count.text = "";
    }

    public void setEmpty(string text = "")
    {
        item = null;
        t_Count.text = "";
        t_Name.text = text;
        i_Icon.gameObject.SetActive(false);
        i_VehicleIcon.gameObject.SetActive(false);
    }

    public void refresh()
    {
        if (item.getComponent<ItemBase>().targetBase != null) icon_Transfer.SetActive(true);
        else icon_Transfer.SetActive(false);

        if (item.hasComponent<Armor>())
        {
            if (item.getComponent<Armor>().bloody) icon_Bloody.SetActive(true);
            else icon_Bloody.SetActive(false);

            if (item.getComponent<Armor>().damaged) icon_Damaged.SetActive(true);
            else icon_Damaged.SetActive(false);
        }
        else
        {
            icon_Bloody.SetActive(false);
            icon_Damaged.SetActive(false);
        }
    }
}
