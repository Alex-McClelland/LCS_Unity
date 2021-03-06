using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Location;

public class SafeHouseTabs : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public SafeHouseView safeHouseView;
    public Transform content;
    public Button b_LeftArrow;
    public Button b_RightArrow;

    public Button p_MenuButton;

    public Color c_SelectedBase;
    public Color c_UnselectedBase;

    public ScrollRect scroll;

    private Dictionary<Entity, Button> safeHouseButtons;

    public void updateSafeHouses()
    {
        if (safeHouseButtons == null)
        {
            safeHouseButtons = new Dictionary<Entity, Button>();

            foreach(Entity safeHouse in MasterController.nation.getAllBases())
            {
                Button menuButton = Instantiate(p_MenuButton);
                safeHouseButtons.Add(safeHouse, menuButton);
                menuButton.transform.SetParent(content, false);
                menuButton.onClick.AddListener(() => { selectSafeHouse(safeHouse); });
            }
        }

        foreach(Entity safeHouse in safeHouseButtons.Keys)
        {
            safeHouseButtons[safeHouse].GetComponentInChildren<Text>().text = safeHouse.getComponent<SiteBase>().getCurrentName();
            if (safeHouse.getComponent<SafeHouse>().owned) safeHouseButtons[safeHouse].gameObject.SetActive(true);
            else safeHouseButtons[safeHouse].gameObject.SetActive(false);
        }

        foreach (Entity e in safeHouseButtons.Keys)
        {
            if (e.getComponent<SafeHouse>().heat >= 100 || e.getComponent<SafeHouse>().underSiege)
            {
                safeHouseButtons[e].image.color = safeHouseView.c_HighHeat;
                safeHouseButtons[e].GetComponentInChildren<Text>().color = Color.white;
            }
            else if (e.getComponent<SafeHouse>().heat > e.getComponent<SafeHouse>().getSecrecy())
            {
                safeHouseButtons[e].image.color = safeHouseView.c_MediumHeat;
                safeHouseButtons[e].GetComponentInChildren<Text>().color = Color.black;
            }
            else
            {
                safeHouseButtons[e].image.color = c_UnselectedBase;
                safeHouseButtons[e].GetComponentInChildren<Text>().color = Color.white;

            }
        }

        safeHouseButtons[safeHouseView.selectedBase].image.color = c_SelectedBase;
        safeHouseButtons[safeHouseView.selectedBase].GetComponentInChildren<Text>().color = Color.white;
    }

    public void selectSafeHouse(Entity safeHouse)
    {        
        safeHouseView.selectSafeHouse(safeHouse);
        updateSafeHouses();
    }

    public void scrollLeft()
    {
        scroll.horizontalNormalizedPosition -= 0.1f;
    }

    public void scrollRight()
    {
        scroll.horizontalNormalizedPosition += 0.1f;
    }

    public void clearSafeHouses()
    {
        if(safeHouseButtons != null)
        {
            foreach(Entity e in safeHouseButtons.Keys)
            {
                Destroy(safeHouseButtons[e].gameObject);
            }

            safeHouseButtons.Clear();
            safeHouseButtons = null;
        }
    }
}
