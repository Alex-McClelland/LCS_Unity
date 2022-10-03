using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.UI;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.Location;
using LCS.Engine.Events;
using LCS.Engine.Data;
using System;

public class BaseController : MonoBehaviour, BaseMode {

    public UIControllerImpl uiController;
    public SquadUIImpl squadUI;

    public Text t_Money;
    public Button b_SquadButton;
    public Text t_Date;
    public InputField t_Slogan;
    public Button b_Wait;

    public Button b_Travel;

    public ButtonSelectionGroup buttonGroup;
    public Button b_ManageLibs;
    public Button b_ViewAgenda;
    public Button b_ManageBases;

    public LiberalManagementBoard LibManagementView;
    public LiberalAgendaImpl LiberalAgendaView;
    public SafeHouseView BaseManagementView;

    public GameObject i_ScreenDim;
    public Transform LocationMenu;
    public Button b_TravelCancel;
    public Button b_ChangeCity;
    public Button p_MenuButton;
    private List<GameObject> districtButtons;
    private Dictionary<string, List<LocationButton>> locationButtons;

    private BaseModeActions actions;

    public MessageLog messageLog;
    public Color locationClosed;
    public Color locationHighSec;
    public Color locationNormal;

    private bool tabsHidden;
    private SelectedTab selectedTab;

    private float speedTimeCountdown;

    private enum SelectedTab
    {
        NONE,
        BASE_MANAGEMENT,
        AGENDA_VIEW,
        LIBERAL_MANAGEMENT
    }

	// Use this for initialization
	void Start () {
        
    }
	
	// Update is called once per frame
	void Update () {
        if (MasterController.GetMC().phase == MasterController.Phase.BASE) {
            if (!MasterController.lcs.checkForActiveMembers())
            {
                speedTimeCountdown -= Time.deltaTime;
            }

            if (speedTimeCountdown <= 0)
            {
                nextDay();
                speedTimeCountdown = 0.05f;
            }
        }
    }

    public void init(BaseModeActions actions)
    {
        this.actions = actions;
        districtButtons = new List<GameObject>();
        locationButtons = new Dictionary<string, List<LocationButton>>();
    }

    public void show()
    {
        gameObject.SetActive(true);
        uiController.addCurrentScreen(this);

        tabsHidden = messageLog.expanded?true:false;
        if(speedTimeCountdown <= 0) speedTimeCountdown = 0.05f;

        if (selectedTab == SelectedTab.NONE)
        {
            buttonGroup.ButtonSelect(1);
            selectedTab = SelectedTab.AGENDA_VIEW;
        }        

        i_ScreenDim.SetActive(false);
        LocationMenu.gameObject.SetActive(false);

        displaySquad();
        loadLocations();
        messageLog.updateMessageLog();
        setBaseButtonColor();
        selectTab(selectedTab);
        buttonGroup.ButtonSelect((int)selectedTab - 1);

        t_Slogan.text = MasterController.lcs.slogan;
        refreshMoney();
        t_Date.text = MasterController.GetMC().currentDate.ToString("D");    
    }

    public void refresh()
    {
        if (t_Slogan.isFocused) return;
        if (!gameObject.activeSelf) return;

        displaySquad();
        
        messageLog.updateMessageLog();
        setBaseButtonColor();

        t_Slogan.text = MasterController.lcs.slogan;
        refreshMoney();
        t_Date.text = MasterController.GetMC().currentDate.ToString("D");
    }

    public void close()
    {
        hide();
        uiController.removeCurrentScreen(this);
    }

    public void hide()
    {
        gameObject.SetActive(false);

        foreach (GameObject o in districtButtons)
        {
            Destroy(o);
        }

        foreach (string s in locationButtons.Keys)
        {
            foreach (LocationButton o in locationButtons[s])
            {
                Destroy(o.button.gameObject);
            }

            locationButtons[s].Clear();
        }

        districtButtons.Clear();
        locationButtons.Clear();
    }

    public void refreshMoney()
    {
        t_Money.text = "Mone(y): " + MasterController.lcs.Money.ToString("C00");
    }

    public void showHideTabs()
    {
        if (!tabsHidden)
        {
            tabsHidden = true;
            LibManagementView.gameObject.SetActive(false);
            LiberalAgendaView.gameObject.SetActive(false);
            BaseManagementView.gameObject.SetActive(false);
        }
        else
        {
            tabsHidden = false;
            switch (selectedTab)
            {
                case SelectedTab.NONE:
                    break;
                case SelectedTab.LIBERAL_MANAGEMENT:
                    LibManagementView.gameObject.SetActive(true);
                    break;
                case SelectedTab.AGENDA_VIEW:
                    LiberalAgendaView.gameObject.SetActive(true);
                    break;
                case SelectedTab.BASE_MANAGEMENT:
                    BaseManagementView.gameObject.SetActive(true);
                    break;
            }
        }
    }

    public void nextSquad()
    {
        actions.nextSquad();
        refreshLocations();
        displaySquad();
    }

    public void changeSlogan()
    {
        actions.changeSlogan(t_Slogan.text);
    }

    public void showLibManagement()
    {
        selectTab(SelectedTab.LIBERAL_MANAGEMENT);
    }

    public void showAgenda()
    {
        selectTab(SelectedTab.AGENDA_VIEW);
    }

    public void showBaseManagement()
    {
        selectTab(SelectedTab.BASE_MANAGEMENT);
    }

    private void selectTab(SelectedTab tab)
    {
        selectedTab = tab;

        switch (selectedTab)
        {
            case SelectedTab.NONE:
                LibManagementView.close();
                LiberalAgendaView.close();
                BaseManagementView.close();
                break;
            case SelectedTab.LIBERAL_MANAGEMENT:
                LiberalAgendaView.close();
                BaseManagementView.close();
                LibManagementView.show();
                buttonGroup.ButtonSelect(2);
                break;
            case SelectedTab.AGENDA_VIEW:
                LibManagementView.close();                
                BaseManagementView.close();
                LiberalAgendaView.show();
                buttonGroup.ButtonSelect(1);
                break;
            case SelectedTab.BASE_MANAGEMENT:
                LibManagementView.close();
                LiberalAgendaView.close();
                BaseManagementView.show();
                buttonGroup.ButtonSelect(0);
                break;
        }

        if (tabsHidden)
        {
            LibManagementView.gameObject.SetActive(false);
            LiberalAgendaView.gameObject.SetActive(false);
            BaseManagementView.gameObject.SetActive(false);
        }
    }

    public void showLocationMenu()
    {
        refreshLocations();

        i_ScreenDim.SetActive(true);
        LocationMenu.gameObject.SetActive(true);

        foreach(GameObject o in districtButtons)
        {
            string b = o.GetComponentInChildren<Text>().text;
            o.SetActive(true);
        }

        foreach(string s in locationButtons.Keys)
        {
            foreach(LocationButton o in locationButtons[s])
            {
                o.button.gameObject.SetActive(false);
            }
        }

        if (MasterController.nation.cities.Count == 1)
            b_ChangeCity.gameObject.SetActive(false);
        else
            b_ChangeCity.gameObject.SetActive(true);
    }

    public void displayDistrict(string districtName)
    {
        foreach (GameObject o in districtButtons)
        {
            o.SetActive(false);
        }

        foreach (LocationButton o in locationButtons[districtName])
        {
            o.button.gameObject.SetActive(true);
            if (o.location.getComponent<SiteBase>().hidden)
                o.button.gameObject.SetActive(false);
        }

        b_ChangeCity.gameObject.SetActive(false);
    }

    public void setSquadTarget(string city, string locationDef)
    {
        actions.setDestination(city, locationDef);
        displaySquad();

        hideTravelMenu();
    }

    public void cancelTravel()
    {
        actions.setDestination("NONE", "NONE");
        displaySquad();

        hideTravelMenu();
    }

    public void hideTravelMenu()
    {
        i_ScreenDim.SetActive(false);
        LocationMenu.gameObject.SetActive(false);
        UIControllerImpl.tooltip.setText("");
    }

    public void nextDay()
    {
        UIControllerImpl.tooltip.setText("");
        actions.waitADay();
    }

    public void viewMartyrs()
    {
        uiController.hideUI();
        UIControllerImpl.tooltip.setText("");
        uiController.martyrScreen.show();
    }

    public void viewFinances()
    {
        uiController.hideUI();
        UIControllerImpl.tooltip.setText("");
        uiController.finances.show();
    }

    private void refreshLocations()
    {
        if (MasterController.lcs.activeSquad == null) return;

        /*
        foreach (string district in MasterController.lcs.activeSquad[0].getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().locations.Keys)
        {
            if (!locationButtons.ContainsKey(district)) continue;

            foreach (LocationButton lb in locationButtons[district])
            {
                lb.button.GetComponentInChildren<Text>().text = lb.location.getComponent<SiteBase>().getCurrentName();

                if (lb.location.hasComponent<TroubleSpot>())
                {
                    if (lb.location.getComponent<TroubleSpot>().closed > 0)
                    {
                        lb.button.image.color = locationClosed;
                        lb.button.GetComponent<MouseOverText>().mouseOverText = "Closed";
                    }
                    else if (lb.location.getComponent<TroubleSpot>().highSecurity > 0)
                    {
                        lb.button.image.color = locationHighSec;
                        lb.button.GetComponent<MouseOverText>().mouseOverText = "High Security";
                    }
                    else
                    {
                        lb.button.image.color = locationNormal;
                        lb.button.GetComponent<MouseOverText>().mouseOverText = "";
                    }
                }
            }
        }
        */

        foreach (GameObject o in districtButtons)
        {
            Destroy(o);
        }

        foreach (string s in locationButtons.Keys)
        {
            foreach (LocationButton o in locationButtons[s])
            {
                Destroy(o.button.gameObject);
            }

            locationButtons[s].Clear();
        }

        districtButtons.Clear();
        locationButtons.Clear();

        loadLocations();
    }

    private void loadLocations()
    {
        if (MasterController.lcs.activeSquad == null) return;

        City city = MasterController.lcs.activeSquad[0].getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>();

        int i = 1;

        foreach(string district in city.locations.Keys)
        {
            int j = 1;

            if (!locationButtons.ContainsKey(district))
            {
                Button districtButton = Instantiate(p_MenuButton);
                districtButton.transform.SetParent(LocationMenu, false);
                //Remove the content size fitter as it interferes with the layout group
                Destroy(districtButton.GetComponent<ContentSizeFitter>());
                string districtText = district;
                foreach (NationDef.NationDistrict districtDef in GameData.getData().nationList["USA"].cities["DC"].districts)
                {
                    if ((districtDef.flags & NationDef.DistrictFlag.NEED_VEHICLE) == 0)
                        continue;

                    if(districtDef.name == district)
                    {
                        districtText += " (Need Car)";
                        break;
                    }
                }

                if (getKeyCode(i) != KeyCode.Semicolon)
                {
                    districtButton.GetComponent<ButtonHotkey>().key = getKeyCode(i);
                    //TODO: Make this less stupid
                    districtText = "(" + (i==10?0:i) + ") " + districtText;
                }

                districtButton.GetComponentInChildren<Text>().text = districtText;
                districtButton.onClick.AddListener(() => { displayDistrict(district); });

                districtButtons.Add(districtButton.gameObject);
                locationButtons.Add(district, new List<LocationButton>());

                districtButton.gameObject.SetActive(false);
            }
            
            foreach(Entity location in city.locations[district])
            {
                bool containsLocation = false;

                foreach(LocationButton lb in locationButtons[district])
                {
                    if (lb.location == location)
                    {
                        containsLocation = true;

                        //Normally we want to use getCurrentName() for this but because we don't want the city suffix we're grabbing it directly
                        string locationName = location.getComponent<SiteBase>().currentName.name;

                        if (getKeyCode(j) != KeyCode.Semicolon)
                        {
                            locationName = "(" + (j==10?0:j) + ") " + locationName;
                        }

                        lb.button.GetComponentInChildren<Text>().text = locationName;

                        if (lb.location.hasComponent<TroubleSpot>())
                        {
                            if (lb.location.getComponent<TroubleSpot>().closed > 0)
                            {
                                lb.button.image.color = locationClosed;
                                lb.button.GetComponent<MouseOverText>().mouseOverText = "Closed";
                            }
                            else if (lb.location.getComponent<TroubleSpot>().highSecurity > 0)
                            {
                                lb.button.image.color = locationHighSec;
                                lb.button.GetComponent<MouseOverText>().mouseOverText = "High Security";
                            }
                            else
                            {
                                lb.button.image.color = locationNormal;
                                lb.button.GetComponent<MouseOverText>().mouseOverText = "";
                            }
                        }
                        break;
                    }
                }

                if (!containsLocation)
                {
                    LocationButton locationButton = new LocationButton();
                    Button button = Instantiate(p_MenuButton);
                    button.transform.SetParent(LocationMenu, false);
                    //Remove the content size fitter as it interferes with the layout group
                    Destroy(button.GetComponent<ContentSizeFitter>());

                    //Normally we want to use getCurrentName() for this but because we don't want the city suffix we're grabbing it directly
                    string locationName = location.getComponent<SiteBase>().currentName.name;

                    if (getKeyCode(j) != KeyCode.Semicolon)
                    {
                        button.GetComponent<ButtonHotkey>().key = getKeyCode(j);
                        locationName = "(" + (j==10?0:j) + ") " + locationName;
                    }

                    button.GetComponentInChildren<Text>().text = locationName;
                    button.onClick.AddListener(() => { setSquadTarget(location.getComponent<SiteBase>().city.def, location.def); });

                    locationButton.button = button;
                    locationButton.location = location;

                    locationButtons[district].Add(locationButton);

                    if (location.hasComponent<TroubleSpot>())
                    {
                        if (location.getComponent<TroubleSpot>().closed > 0)
                        {
                            button.image.color = locationClosed;
                            button.GetComponent<MouseOverText>().mouseOverText = "Closed";
                        }
                        else if (location.getComponent<TroubleSpot>().highSecurity > 0)
                        {
                            button.image.color = locationHighSec;
                            button.GetComponent<MouseOverText>().mouseOverText = "High Security";
                        }
                        else
                        {
                            button.image.color = locationNormal;
                            button.GetComponent<MouseOverText>().mouseOverText = "";
                        }
                    }

                    button.gameObject.SetActive(false);
                }

                j++;
            }

            i++;
        }

        locationButtons["TRAVEL"] = new List<LocationButton>();

        i = 1;

        foreach(Entity travelCity in MasterController.nation.cities.Values)
        {
            LocationButton locationButton = new LocationButton();
            Button button = Instantiate(p_MenuButton);
            button.transform.SetParent(LocationMenu, false);
            //Remove the content size fitter as it interferes with the layout group
            Destroy(button.GetComponent<ContentSizeFitter>());

            string cityName = travelCity.getComponent<City>().name;
            if (getKeyCode(i) != KeyCode.Semicolon)
            {
                button.GetComponent<ButtonHotkey>().key = getKeyCode(i);
                cityName = "(" + i + ") " + cityName;
            }

            button.GetComponentInChildren<Text>().text = cityName;
            button.onClick.AddListener(() => { setSquadTarget(travelCity.def, "RESIDENTIAL_SHELTER"); });

            locationButton.button = button;
            locationButton.location = travelCity.getComponent<City>().getLocation("RESIDENTIAL_SHELTER");

            locationButtons["TRAVEL"].Add(locationButton);
            i++;
        }

        b_ChangeCity.transform.SetAsLastSibling();
        b_TravelCancel.transform.SetAsLastSibling();
    }

    /**
     * Get the keycode associated with the integer value for a list. Returns Semicolon if there is no match.
     * */
    private KeyCode getKeyCode(int i)
    {
        switch (i)
        {
            case 1: return KeyCode.Alpha1;
            case 2: return KeyCode.Alpha2;
            case 3: return KeyCode.Alpha3;
            case 4: return KeyCode.Alpha4;
            case 5: return KeyCode.Alpha5;
            case 6: return KeyCode.Alpha6;
            case 7: return KeyCode.Alpha7;
            case 8: return KeyCode.Alpha8;
            case 9: return KeyCode.Alpha9;
            case 10: return KeyCode.Alpha0;
            default: return KeyCode.Semicolon;
        }
    }

    private class LocationButton
    {
        public Entity location;
        public Button button;
    }

    private void setBaseButtonColor()
    {
        bool underSiege = false;
        bool underAttack = false;
        bool midheat = false;
        bool highheat = false;

        ButtonSelectionGroupChild button = b_ManageBases.GetComponent<ButtonSelectionGroupChild>();        

        foreach (Entity e in MasterController.nation.getAllBases())
        {
            if (!e.getComponent<SafeHouse>().owned) continue;

            if (e.getComponent<SafeHouse>().heat > e.getComponent<SafeHouse>().getSecrecy()) midheat = true;
            if (e.getComponent<SafeHouse>().heat >= 100) highheat = true;
            if (e.getComponent<SafeHouse>().underSiege) underSiege = true;
            if (e.getComponent<SafeHouse>().underAttack) underAttack = true;
        }

        if (highheat || underSiege)
        {
            button.c_UnselectedColor = BaseManagementView.c_HighHeat;
            button.c_UnselectedTextColor = Color.white;
        }
        else if (midheat)
        {
            button.c_UnselectedColor = BaseManagementView.c_MediumHeat;
            button.c_UnselectedTextColor = Color.black;
        }
        else
        {
            button.c_UnselectedColor = buttonGroup.c_UnselectedDefault;
            button.c_UnselectedTextColor = Color.white;
        }

        if (underAttack)
        {
            b_Wait.interactable = false;
            b_Wait.GetComponent<MouseOverText>().mouseOverText = "Cannot wait while under attack";
        }
        else
        {
            b_Wait.interactable = true;
            b_Wait.GetComponent<MouseOverText>().mouseOverText = "";
        }

        b_ManageBases.GetComponent<ButtonSelectionGroupChild>().refresh();
    }

    private void displaySquad()
    {
        if (MasterController.GetMC().uiController.squadUI.displaySquad(MasterController.lcs.activeSquad))
        {
            b_Travel.interactable = true;
            b_SquadButton.GetComponentInChildren<Text>().text = MasterController.lcs.activeSquad.name;
            b_Travel.GetComponentInChildren<Text>().text = MasterController.lcs.activeSquad.target!=null? "(G) " + MasterController.lcs.activeSquad.target.getComponent<SiteBase>().getCurrentName(true) :"(G)o Forth to Stop EVIL";

            if (MasterController.lcs.activeSquad.homeBase.getComponent<SafeHouse>().underSiege)
            {
                b_Travel.interactable = false;
                b_Travel.GetComponentInChildren<Text>().text = "UNDER SIEGE";
            }
        }
        else
        {
            b_Travel.interactable = false;
            b_SquadButton.GetComponentInChildren<Text>().text = "No Squad Selected";
            b_Travel.GetComponentInChildren<Text>().text = "Go Forth to Stop EVIL";
            b_SquadButton.interactable = MasterController.lcs.squads.Count == 0 ? false : true;
        }
    }
}
