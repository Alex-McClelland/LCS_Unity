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

        transform.parent.gameObject.SetActive(true);

        if((safehouse.getComponent<SafeHouse>().investments & SafeHouse.Investments.FORTIFIED) != 0)
        {
            b_Fortify.interactable = false;
            b_Fortify.GetComponentInChildren<Text>().text = "Compound Fortified";
        }
        else
        {
            b_Fortify.interactable = true;
            b_Fortify.GetComponentInChildren<Text>().text = "Fortify for (S)iege ($2000)";
        }

        if ((safehouse.getComponent<SafeHouse>().investments & SafeHouse.Investments.CAMERAS) != 0)
        {
            b_Cameras.interactable = false;
            b_Cameras.GetComponentInChildren<Text>().text = "Cameras Installed";
        }
        else
        {
            b_Cameras.interactable = true;
            b_Cameras.GetComponentInChildren<Text>().text = "Install (C)ameras ($2000)";
        }

        if ((safehouse.getComponent<SafeHouse>().investments & SafeHouse.Investments.TRAPS) != 0)
        {
            b_Traps.interactable = false;
            b_Traps.GetComponentInChildren<Text>().text = "Compound Trapped";
        }
        else
        {
            b_Traps.interactable = true;
            b_Traps.GetComponentInChildren<Text>().text = "Set (B)ooby Traps ($3000)";
        }

        if ((safehouse.getComponent<SafeHouse>().investments & SafeHouse.Investments.TANK_TRAPS) != 0)
        {
            b_TankTraps.interactable = false;
            b_TankTraps.GetComponentInChildren<Text>().text = "Tank Traps Placed";
        }
        else
        {
            b_TankTraps.interactable = true;
            b_TankTraps.GetComponentInChildren<Text>().text = "Lay (T)ank Traps ($3000)";
        }

        if ((safehouse.getComponent<SafeHouse>().investments & SafeHouse.Investments.GENERATOR) != 0)
        {
            b_Generator.interactable = false;
            b_Generator.GetComponentInChildren<Text>().text = "Generator Installed";
        }
        else
        {
            b_Generator.interactable = true;
            b_Generator.GetComponentInChildren<Text>().text = "Install (G)enerator ($3000)";
        }

        if ((safehouse.getComponent<SafeHouse>().investments & SafeHouse.Investments.AAGUN) != 0)
        {
            b_AAGun.interactable = false;
            b_AAGun.GetComponentInChildren<Text>().text = "Anti-Aircraft Gun Installed";
        }
        else
        {
            b_AAGun.interactable = true;
            if (MasterController.GetMC().testCondition("LAW:GUN_CONTROL:=:-2"))
            {
                b_AAGun.GetComponentInChildren<Text>().text = "(I)nstall Perfectly Legal AA-Gun ($35,000)";
            }
            else
            {
                b_AAGun.GetComponentInChildren<Text>().text = "(I)nstall Concealed AA-Gun ($200,000)";
            }
        }

        if ((safehouse.getComponent<SafeHouse>().investments & SafeHouse.Investments.PRINTING_PRESS) != 0)
        {
            b_PrintingPress.interactable = false;
            b_PrintingPress.GetComponentInChildren<Text>().text = "Printing Press Active";
        }
        else
        {
            b_PrintingPress.interactable = true;
            b_PrintingPress.GetComponentInChildren<Text>().text = "Buy Printing (P)ress ($3000)";
        }

        if ((safehouse.getComponent<SafeHouse>().investments & SafeHouse.Investments.BUSINESS_FRONT) != 0)
        {
            b_BusinessFront.interactable = false;
            b_BusinessFront.GetComponentInChildren<Text>().text = "Business Front Operating";
        }
        else
        {
            b_BusinessFront.interactable = true;
            b_BusinessFront.GetComponentInChildren<Text>().text = "(E)stablish Business Front ($3000)";
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
