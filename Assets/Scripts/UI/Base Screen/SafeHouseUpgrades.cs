using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.Location;

public class SafeHouseUpgrades : MonoBehaviour {

    public UIControllerImpl uiController;
    public SafeHouseView safeHouseManagement;

    public Button b_Fortify;
    public Button b_Cameras;
    public Button b_Traps;
    public Button b_TankTraps;
    public Button b_Generator;
    public Button b_AAGun;
    public Button b_PrintingPress;
    public Button b_BusinessFront;
    
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void showButtons()
    {
        Entity safehouse = safeHouseManagement.selectedBase;
        MasterController mc = MasterController.GetMC();

        transform.parent.gameObject.SetActive(true);

        if((safehouse.getComponent<SafeHouse>().investments & SafeHouse.Investments.FORTIFIED) != 0)
        {
            b_Fortify.interactable = false;
            b_Fortify.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_fortify_installed");
        }
        else
        {
            b_Fortify.interactable = true;
            b_Fortify.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_fortify");
        }

        if ((safehouse.getComponent<SafeHouse>().investments & SafeHouse.Investments.CAMERAS) != 0)
        {
            b_Cameras.interactable = false;
            b_Cameras.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_cameras_installed");
        }
        else
        {
            b_Cameras.interactable = true;
            b_Cameras.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_cameras");
        }

        if ((safehouse.getComponent<SafeHouse>().investments & SafeHouse.Investments.TRAPS) != 0)
        {
            b_Traps.interactable = false;
            b_Traps.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_traps_installed");
        }
        else
        {
            b_Traps.interactable = true;
            b_Traps.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_traps");
        }

        if ((safehouse.getComponent<SafeHouse>().investments & SafeHouse.Investments.TANK_TRAPS) != 0)
        {
            b_TankTraps.interactable = false;
            b_TankTraps.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_tank_traps_installed");
        }
        else
        {
            b_TankTraps.interactable = true;
            b_TankTraps.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_tank_traps");
        }

        if ((safehouse.getComponent<SafeHouse>().investments & SafeHouse.Investments.GENERATOR) != 0)
        {
            b_Generator.interactable = false;
            b_Generator.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_generator_installed");
        }
        else
        {
            b_Generator.interactable = true;
            b_Generator.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_generator");
        }

        if ((safehouse.getComponent<SafeHouse>().investments & SafeHouse.Investments.AAGUN) != 0)
        {
            b_AAGun.interactable = false;
            b_AAGun.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_aagun_installed");
        }
        else
        {
            b_AAGun.interactable = true;
            if (MasterController.GetMC().testCondition("LAW:GUN_CONTROL:=:-2"))
            {
                b_AAGun.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_aagun_legal");
            }
            else
            {
                b_AAGun.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_aagun");
            }
        }

        if ((safehouse.getComponent<SafeHouse>().investments & SafeHouse.Investments.PRINTING_PRESS) != 0)
        {
            b_PrintingPress.interactable = false;
            b_PrintingPress.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_printing_press_installed");
        }
        else
        {
            b_PrintingPress.interactable = true;
            b_PrintingPress.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_printing_press");
        }

        if ((safehouse.getComponent<SafeHouse>().investments & SafeHouse.Investments.BUSINESS_FRONT) != 0)
        {
            b_BusinessFront.interactable = false;
            b_BusinessFront.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_front_installed");
        }
        else
        {
            b_BusinessFront.interactable = true;
            b_BusinessFront.GetComponentInChildren<Text>().text = mc.getTranslation("BASE_upgrade_front");
        }

        if (safehouse.getComponent<SafeHouse>().underSiege)
        {
            b_Fortify.interactable = false;
            b_Cameras.interactable = false;
            b_Traps.interactable = false;
            b_TankTraps.interactable = false;
            b_Generator.interactable = false;
            b_AAGun.interactable = false;
            b_PrintingPress.interactable = false;
            b_BusinessFront.interactable = false;
        }

    }

    public void hideButtons()
    {
        transform.parent.gameObject.SetActive(false);
    }

    public void applyUpgrade(string upgrade)
    {
        safeHouseManagement.applyUpgrade(upgrade);
        showButtons();
    }
}
