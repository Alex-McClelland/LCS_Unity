using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.Location;
using LCS.Engine.Components.Item;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Creature;
using LCS.Engine.Data;

public class InventoryMenu : MonoBehaviour {

    public UIControllerImpl uiController;

    public Transform gridContent;

    public ItemButton p_ItemButton;
    public Transform baseButtonRoot;
    public GameObject p_MenuButton;

    public Entity currentCharacter;
    public Entity currentBase;

    private Dictionary<string, ItemButton> buttons;
    private List<GameObject> squadButtons;
    private Dictionary<Entity, GameObject> baseButtons;

    private Entity transferBase;

    void Awake()
    {
        buttons = new Dictionary<string, ItemButton>();
        squadButtons = new List<GameObject>();
        baseButtons = new Dictionary<Entity, GameObject>();
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void showBaseInventory(string type)
    {
        if(currentCharacter != null)
        {
            foreach(Entity item in currentCharacter.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().getInventory())
            {
                if (item.getComponent<ItemBase>().targetBase != null) continue;
                if (type == "weapon" && !item.hasComponent<Weapon>()) continue;
                if (type == "armor" && !item.hasComponent<Armor>()) continue;
                if (type == "clip")
                {
                    if(!item.hasComponent<Clip>())
                        continue;
                    if (item.getComponent<Clip>().getAmmoType() != currentCharacter.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getAmmoType())
                        continue;
                }
                if (type == "vehicle" && !item.hasComponent<Vehicle>()) continue;

                string itemName = item.def;
                if (item.hasComponent<Armor>())
                {
                    itemName += item.getComponent<Armor>().quality;
                    if (item.getComponent<Armor>().damaged) itemName += "$D";
                    if (item.getComponent<Armor>().bloody) itemName += "$B";
                }
                //Vehicles shouldn't stack
                if (item.hasComponent<Vehicle>())
                    itemName += item.guid;

                if (!buttons.ContainsKey(itemName))
                {
                    ItemButton button = Instantiate(p_ItemButton);
                    buttons.Add(itemName, button);
                    button.transform.SetParent(gridContent, false);
                    button.setItem(item);

                    Entity targetItem = item;

                    button.button.onClick.AddListener(() => { doBaseButtonPress(type, targetItem); });
                }
                else
                {
                    buttons[itemName].changeCount(1);
                }
            }

            ItemButton unequipButton = Instantiate(p_ItemButton);
            buttons.Add("NONE", unequipButton);
            unequipButton.transform.SetParent(gridContent, false);
            unequipButton.setEmpty("UNEQUIP");
            unequipButton.button.onClick.AddListener(() => { doUnequipButtonPress(type); });
        }
        else if(currentBase != null)
        {
            buildBaseMenu();
            cancelSelect();

            List<Entity> tempBaseInventory = new List<Entity>(sortBaseInventory(currentBase.getComponent<SafeHouse>().getInventory()));

            foreach (Entity item in tempBaseInventory)
            {
                string itemName = item.def + (item.getComponent<ItemBase>().targetBase != null ? item.getComponent<ItemBase>().targetBase.def : "");
                if (item.hasComponent<Vehicle>()) itemName += item.guid;
                if (item.hasComponent<Armor>())
                {
                    itemName += item.getComponent<Armor>().quality;
                    if (item.getComponent<Armor>().damaged) itemName += "$D";
                    if (item.getComponent<Armor>().bloody) itemName += "$B";
                }

                if (!buttons.ContainsKey(itemName))
                {
                    ItemButton button = Instantiate(p_ItemButton);
                    buttons.Add(itemName, button);
                    button.transform.SetParent(gridContent, false);
                    button.setItem(item);
                    button.button.onClick.AddListener(() => { doTransferButtonPress(item); refresh(type); });
                }
                else
                {
                    buttons[itemName].changeCount(1);
                }
            }
        }
    }

    public List<Entity> sortBaseInventory(List<Entity> baseInventory)
    {
        List<Entity> newBaseInventory = new List<Entity>(baseInventory);
        Dictionary<Entity, List<Entity>> transferItems = new Dictionary<Entity, List<Entity>>();

        foreach(Entity e in MasterController.nation.getAllBases())
        {
            transferItems[e] = new List<Entity>();
        }

        foreach(Entity e in newBaseInventory)
        {
            if (e.getComponent<ItemBase>().targetBase != null)
            {
                transferItems[e.getComponent<ItemBase>().targetBase].Add(e);
            }
        }

        newBaseInventory.RemoveAll((Entity e) => { return e.getComponent<ItemBase>().targetBase != null; });

        newBaseInventory.Sort(baseInventoryCompare);

        foreach(Entity b in MasterController.nation.getAllBases())
        {
            transferItems[b].Sort(baseInventoryCompare);
            newBaseInventory.AddRange(transferItems[b]);
        }

        return newBaseInventory;
    }

    private int baseInventoryCompare(Entity e1, Entity e2)
    {
        string item1Name = getBaseItemHash(e1);
        string item2Name = getBaseItemHash(e2);

        return item1Name.CompareTo(item2Name);
    }

    private string getBaseItemHash(Entity e)
    {
        string itemName = e.def;
        if (e.hasComponent<Vehicle>()) itemName += e.guid;
        if (e.hasComponent<Armor>())
        {
            itemName += e.getComponent<Armor>().quality;
            if (e.getComponent<Armor>().damaged) itemName += "$D";
            if (e.getComponent<Armor>().bloody) itemName += "$B";
        }

        return itemName;
    }

    public void showSquadInventory(string type)
    {
        if (currentCharacter != null)
        {
            if (currentCharacter.getComponent<Liberal>().squad == null) return;

            foreach (Entity item in currentCharacter.getComponent<Liberal>().squad.inventory)
            {
                if (type == "weapon" && !item.hasComponent<Weapon>()) continue;
                if (type == "armor" && !item.hasComponent<Armor>()) continue;
                if (type == "clip")
                {
                    if (!item.hasComponent<Clip>())
                        continue;
                    if (item.getComponent<Clip>().getAmmoType() != currentCharacter.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getAmmoType())
                        continue;
                }

                ItemButton button = Instantiate(p_ItemButton);
                squadButtons.Add(button.gameObject);
                button.transform.SetParent(gridContent, false);
                button.setItem(item);

                Entity targetItem = item;

                button.button.onClick.AddListener(() => { doSquadButtonPress(type, targetItem); });
            }

            ItemButton unequipButton = Instantiate(p_ItemButton);
            squadButtons.Add(unequipButton.gameObject);
            unequipButton.transform.SetParent(gridContent, false);
            unequipButton.setEmpty("UNEQUIP");
            unequipButton.button.onClick.AddListener(() => { doUnequipButtonPress(type); });
        }
    }

    public void clear()
    {
        currentCharacter = null;
        currentBase = null;
        transferBase = null;

        clearButtons();
    }

    private void clearButtons()
    {
        foreach (ItemButton o in buttons.Values)
            Destroy(o.gameObject);
        foreach (GameObject o in squadButtons)
            Destroy(o);
        foreach (Entity e in baseButtons.Keys)
            Destroy(baseButtons[e]);

        buttons.Clear();
        squadButtons.Clear();
        baseButtons.Clear();
    }

    private void doBaseButtonPress(string type, Entity item)
    {
        if (type == "weapon")
        {
            currentCharacter.getComponent<Inventory>().equipWeapon(item);
            //Thrown weapons load themselves as ammo (this is maybe a bit unintuitive and could probably be better)
            if((item.getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.THROWN) != 0)
                item.getComponent<Weapon>().clip = item; 
        }
        else if (type == "armor")
            currentCharacter.getComponent<Inventory>().equipArmor(item);
        else if (type == "clip")
        {
            bool result = currentCharacter.getComponent<Inventory>().equipClip(item);
            if (result && currentCharacter.getComponent<Inventory>().getWeapon().getComponent<Weapon>().needsReload())
                currentCharacter.getComponent<Inventory>().reload(false);
        }
        else if(type == "vehicle")
        {
            currentCharacter.getComponent<Inventory>().equipVehicle(item, Input.GetKey(KeyCode.LeftShift));
        }
        else
            return;

        refresh(type);
    }

    private void doSquadButtonPress(string type, Entity item)
    {
        bool result = false;

        if (type == "weapon")
            result = currentCharacter.getComponent<Inventory>().equipWeapon(item);
        else if (type == "armor")
            result = currentCharacter.getComponent<Inventory>().equipArmor(item);
        else if (type == "clip")
            result = currentCharacter.getComponent<Inventory>().equipClip(item);
        else
            return;

        if(result)
            currentCharacter.getComponent<Liberal>().squad.inventory.Remove(item);

        refresh(type);
    }

    private void doUnequipButtonPress(string type)
    {
        if (type == "weapon")
            currentCharacter.getComponent<Inventory>().dropWeapon();
        else if (type == "armor")
            currentCharacter.getComponent<Inventory>().dropArmor();
        else if (type == "clip")
            currentCharacter.getComponent<Inventory>().dropClip();
        else if(type == "vehicle")
        {
            if (currentCharacter.getComponent<Inventory>().vehicle != null &&
                currentCharacter.getComponent<Inventory>().vehicle.getComponent<Vehicle>().preferredDriver == currentCharacter)
                currentCharacter.getComponent<Inventory>().vehicle.getComponent<Vehicle>().preferredDriver = null;
            currentCharacter.getComponent<Inventory>().vehicle = null;
        }
        else
            return;

        refresh(type);
    }

    private void doTransferButtonPress(Entity item)
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            string refName = item.def + (item.getComponent<ItemBase>().targetBase != null ? item.getComponent<ItemBase>().targetBase.def : "");
            if (item.hasComponent<Vehicle>()) refName += item.guid;
            if (item.hasComponent<Armor>())
            {
                refName += item.getComponent<Armor>().quality;
                if (item.getComponent<Armor>().damaged) refName += "$D";
                if (item.getComponent<Armor>().bloody) refName += "$B";
            }

            foreach (Entity e in currentBase.getComponent<SafeHouse>().getInventory())
            {
                string itemName = e.def + (e.getComponent<ItemBase>().targetBase != null ? e.getComponent<ItemBase>().targetBase.def : "");
                if (e.hasComponent<Vehicle>()) itemName += e.guid;
                if (e.hasComponent<Armor>())
                {
                    itemName += e.getComponent<Armor>().quality;
                    if (e.getComponent<Armor>().damaged) itemName += "$D";
                    if (e.getComponent<Armor>().bloody) itemName += "$B";
                }

                if (refName == itemName && transferBase != e.getComponent<ItemBase>().Location)
                    e.getComponent<ItemBase>().targetBase = transferBase;
            }
        }
        else
        {
            if (transferBase != item.getComponent<ItemBase>().Location)
                item.getComponent<ItemBase>().targetBase = transferBase;
        }
    }

    private void refresh(string type)
    {
        Entity tempBase = transferBase;

        if (transform.parent.GetComponent<InfoScreenController>() != null)
            transform.parent.GetComponent<InfoScreenController>().setInventoryButtons();

        clearButtons();

        if (MasterController.GetMC().phase == MasterController.Phase.BASE)
            showBaseInventory(type);
        else
            showSquadInventory(type);

        if(tempBase != null)
            selectBase(tempBase);
    }

    private void buildBaseMenu()
    {
        foreach (Entity safeHouse in MasterController.nation.getAllBases())
        {
            if (!safeHouse.getComponent<SafeHouse>().owned) continue;
            if (safeHouse == currentBase) continue;

            if (!baseButtons.ContainsKey(safeHouse))
            {
                GameObject baseButton = Instantiate(p_MenuButton);
                baseButtons.Add(safeHouse, baseButton);
                baseButton.transform.SetParent(baseButtonRoot, false);
                baseButton.GetComponentInChildren<Text>().text = safeHouse.getComponent<SiteBase>().getCurrentName(true);
                baseButton.GetComponent<Button>().onClick.AddListener(() => { selectBase(safeHouse); });
            }
            else
            {
                baseButtons[safeHouse].GetComponentInChildren<Text>().text = safeHouse.getComponent<SiteBase>().getCurrentName(true);
            }
        }
    }

    public void cancelSelect()
    {
        foreach (GameObject value in baseButtons.Values)
        {
            value.GetComponent<Button>().image.color = uiController.buttonColorOff;
        }

        transferBase = null;
    }

    private void selectBase(Entity safeHouse)
    {
        foreach(GameObject value in baseButtons.Values)
        {
            value.GetComponent<Button>().image.color = uiController.buttonColorOff;
        }

        baseButtons[safeHouse].GetComponent<Button>().image.color = uiController.buttonColorOn;
        transferBase = safeHouse;
    }
}
