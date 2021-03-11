using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.UI;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Location;
using LCS.Engine.Data;

public class SafeHouseView : MonoBehaviour, SafeHouseManagement {

    public UIControllerImpl uiController;

    public SafeHouseTabs tabs;
    public Text t_Heat;
    public Text t_Secrecy;
    public Text t_Food;
    public Button b_Bodies;
    public Button b_Hostages;
    public Button b_Flag;
    public Button b_Upgrade;
    public Button b_Food;
    public BaseStorageView storageView;
    public SafeHouseUpgrades upgradeView;
    public InventoryMenu inventoryView;
    public GameObject siegeBox;

    public GameObject i_Business;
    public GameObject i_Camera;
    public GameObject i_Flag;
    public GameObject i_Fortified;
    public GameObject i_Generator;
    public GameObject i_PrintingPress;
    public GameObject i_TankTraps;

    public Color c_MediumHeat;
    public Color c_HighHeat;

    public Entity selectedBase;

    private SafeHouseManagementActions actions;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void init(SafeHouseManagementActions actions)
    {
        this.actions = actions;
    }

    public void show()
    {
        uiController.addCurrentScreen(this);

        if (selectedBase == null) tabs.selectSafeHouse(MasterController.nation.cities["DC"].getComponent<City>().getLocation("RESIDENTIAL_SHELTER"));
        tabs.updateSafeHouses();

        displaySafeHouse();
        storageView.refresh();

        gameObject.SetActive(true);
    }

    public void refresh()
    {
        tabs.updateSafeHouses();
        displaySafeHouse();
        storageView.refresh();
    }

    public void close()
    {
        hide();
        uiController.removeCurrentScreen(this);
    }

    public void hide()
    {
        gameObject.SetActive(false);
        tabs.clearSafeHouses();
        storageView.hideAll();

        upgradeView.hideButtons();
    }

    public void selectSafeHouse(Entity safeHouse)
    {
        selectedBase = safeHouse;
        displaySafeHouse();
        storageView.hideAll();
    }

    public void showCorpseList()
    {
        storageView.showCorpseList(selectedBase);
    }

    public void showHostageList()
    {
        storageView.showHostageList(selectedBase);
    }

    public void buyFood()
    {
        actions.buyRations(selectedBase);
        refreshFood();
    }

    private void refreshFood()
    {
        int foodAmt = selectedBase.getComponent<SafeHouse>().food;
        int libAmt = selectedBase.getComponent<SafeHouse>().getBasedLiberals().Count;
        t_Food.text = GameData.getData().translationList["BASE_food_string"].Replace("$FOODAMT", foodAmt.ToString()).Replace("$LIBAMT", libAmt.ToString());
        if (selectedBase.getComponent<SafeHouse>().getBasedLiberals().Count > 0)
        {
            int daysLeft = selectedBase.getComponent<SafeHouse>().food / selectedBase.getComponent<SafeHouse>().getBasedLiberals().Count;
            t_Food.text += " " + GameData.getData().translationList["BASE_food_remaining"].Replace("$DAYSLEFT", daysLeft.ToString());
        }
    }

    public void flagButton()
    {
        if ((selectedBase.getComponent<SafeHouse>().investments & SafeHouse.Investments.FLAG) != 0)
        {
            actions.burnFlag(selectedBase);
        }
        else
        {
            actions.buyFlag(selectedBase);
        }

        setFlagButton();
        refreshSecrecy();
    }

    public void refreshSecrecy()
    {
        t_Secrecy.text = GameData.getData().translationList["BASE_secrecy"] + ": " + selectedBase.getComponent<SafeHouse>().getSecrecy() + "%";
    }

    public void showInventory()
    {
        inventoryView.gameObject.SetActive(true);
        inventoryView.clear();
        inventoryView.currentBase = selectedBase;
        inventoryView.showBaseInventory("");   
    }

    public void hideInventory()
    {
        inventoryView.gameObject.SetActive(false);       
    }

    public void applyUpgrade(string upgrade)
    {
        actions.upgrade(selectedBase, upgrade);
        refreshSecrecy();
    }

    public void select(Entity e)
    {
        actions.selectChar(e);
    }

    public void giveUpSiege()
    {
        actions.giveUpSiege(selectedBase);
    }

    public void escapeEngageSiege()
    {
        actions.escapeEngageSiege(selectedBase);
    }

    private void setFlagButton()
    {
        if((selectedBase.getComponent<SafeHouse>().investments & SafeHouse.Investments.FLAG) != 0)
        {
            b_Flag.GetComponentInChildren<Text>().text = GameData.getData().translationList["BASE_burn_flag_button"];
        }
        else
        {
            b_Flag.GetComponentInChildren<Text>().text = GameData.getData().translationList["BASE_buy_flag_button"];
            if (selectedBase.getComponent<SafeHouse>().underSiege)
            {
                b_Flag.interactable = false;
            }
            else
            {
                b_Flag.interactable = true;
            }
        }
    }

    private void displaySafeHouse()
    {
        string heatColor = "<color=grey>";

        if (selectedBase.getComponent<SafeHouse>().heat > 0) heatColor = "<color=white>";
        if (selectedBase.getComponent<SafeHouse>().heat > selectedBase.getComponent<SafeHouse>().getSecrecy()) heatColor = "<color=yellow>";
        if (selectedBase.getComponent<SafeHouse>().heat >= 100) heatColor = "<color=red>";

        t_Heat.text = GameData.getData().translationList["BASE_heat"] + ": " + heatColor + selectedBase.getComponent<SafeHouse>().heat + "%</color>";
        refreshSecrecy();
        refreshFood();

        if (selectedBase.getComponent<SafeHouse>().getBodies().Count == 0) b_Bodies.interactable = false;
        else b_Bodies.interactable = true;

        if (selectedBase.getComponent<SafeHouse>().getHostages().Count == 0) b_Hostages.interactable = false;
        else b_Hostages.interactable = true;

        i_Business.SetActive((selectedBase.getComponent<SafeHouse>().investments & SafeHouse.Investments.BUSINESS_FRONT) != 0);
        i_Camera.SetActive((selectedBase.getComponent<SafeHouse>().investments & SafeHouse.Investments.CAMERAS) != 0);
        i_Flag.SetActive((selectedBase.getComponent<SafeHouse>().investments & SafeHouse.Investments.FLAG) != 0);
        i_Fortified.SetActive((selectedBase.getComponent<SafeHouse>().investments & SafeHouse.Investments.FORTIFIED) != 0);
        i_Generator.SetActive((selectedBase.getComponent<SafeHouse>().investments & SafeHouse.Investments.GENERATOR) != 0);
        i_PrintingPress.SetActive((selectedBase.getComponent<SafeHouse>().investments & SafeHouse.Investments.PRINTING_PRESS) != 0);
        i_TankTraps.SetActive((selectedBase.getComponent<SafeHouse>().investments & SafeHouse.Investments.TANK_TRAPS) != 0);

        if ((selectedBase.getComponent<SafeHouse>().getFlags() & LocationDef.BaseFlag.UPGRADABLE) == 0)
        {
            b_Upgrade.interactable = false;
        }
        else
        {
            b_Upgrade.interactable = true;
        }

        if (!selectedBase.getComponent<SafeHouse>().underSiege)
        {
            b_Food.interactable = true;
            siegeBox.SetActive(false);
        }
        else
        {
            b_Food.interactable = false;
            siegeBox.SetActive(true);
        }

        setFlagButton();
        hideInventory();
    }
}
