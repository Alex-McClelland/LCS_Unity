using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.UI;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.World;

public class TrialController : MonoBehaviour, Trial {

    public UIControllerImpl uiController;

    public Button p_MenuButton;
    public Transform eventOptionsBox;
    public MessageLog messageLog;
    public Text t_Name;
    public Text t_EventText;
    public Text t_SideText;
    public Text t_EventTitle;
    public PortraitImage i_Portrait;

    public Entity character;

    private TrialActions actions;
    private List<GameObject> generatedObjects;
    private bool showTrialButtons;
    private bool allowCharSelection;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        
	}

    public void init(TrialActions actions)
    {
        this.actions = actions;
    }

    public void show(Entity defendant)
    {
        showTrialButtons = false;
        allowCharSelection = false;
        character = defendant;
        show();
    }

    public void show()
    {
        uiController.addCurrentScreen(this);

        gameObject.SetActive(true);
        generatedObjects = new List<GameObject>();
        
        messageLog.updateMessageLog();

        t_Name.text = character.getComponent<CreatureInfo>().givenName + " " + (character.getComponent<CreatureInfo>().alias != ""?"\"" + character.getComponent<CreatureInfo>().alias + "\" ":"") + character.getComponent<CreatureInfo>().surname;
        i_Portrait.buildPortrait(character);

        if(showTrialButtons)
            generateTrialButtons();

        uiController.RegisterAnyKeyBind(() => actions.advance(character));
    }

    public void refresh()
    {

    }

    public void close()
    {
        hide();
        uiController.removeCurrentScreen(this);
    }

    public void hide()
    {
        gameObject.SetActive(false);
        disposeButtons();
        uiController.ClearAnyKeyBind();
    }

    public void printTitle(string text)
    {
        t_EventTitle.text = text;
    }

    public void printText(string text)
    {
        t_EventText.text += text;
    }

    public void clearText()
    {
        t_EventText.text = "";
    }

    public void selectCharacter()
    {
        if (allowCharSelection)
        {
            hide();
            uiController.charInfo.show(character);
        }
    }

    private void disposeButtons()
    {
        foreach (GameObject o in generatedObjects)
        {
            Destroy(o);
        }
    }

    private void disableButtons()
    {
        foreach (GameObject o in generatedObjects)
        {
            Button b = o.GetComponent<Button>();

            if (b != null) b.interactable = false;
        }
    }

    private void enableButtons()
    {
        foreach (GameObject o in generatedObjects)
        {
            Button b = o.GetComponent<Button>();

            if (b != null) b.interactable = true;
        }
    }
    
    public void generateTrialButtons()
    {
        showTrialButtons = true;
        allowCharSelection = true;
        Button MenuButton = Instantiate(p_MenuButton);
        generatedObjects.Add(MenuButton.gameObject);
        Destroy(MenuButton.GetComponent<ContentSizeFitter>());
        MenuButton.transform.SetParent(eventOptionsBox, false);
        MenuButton.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("TRIAL_defense_court_appointed");
        MenuButton.onClick.AddListener(() => { actions.selection(character, TrialActions.TrialSelection.PUBLIC_DEFENDER); extraButtonAction(); });
        ButtonHotkey hotkey = MenuButton.GetComponent<ButtonHotkey>();
        hotkey.key = KeyCode.C;
        hotkey.blockers.Add(uiController.popupBlocker);

        MenuButton = Instantiate(p_MenuButton);
        generatedObjects.Add(MenuButton.gameObject);
        Destroy(MenuButton.GetComponent<ContentSizeFitter>());
        MenuButton.transform.SetParent(eventOptionsBox, false);
        MenuButton.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("TRIAL_defense_self");
        MenuButton.onClick.AddListener(() => { actions.selection(character, TrialActions.TrialSelection.DEFEND_SELF); extraButtonAction(); });
        hotkey = MenuButton.GetComponent<ButtonHotkey>();
        hotkey.key = KeyCode.D;
        hotkey.blockers.Add(uiController.popupBlocker);

        MenuButton = Instantiate(p_MenuButton);
        generatedObjects.Add(MenuButton.gameObject);
        Destroy(MenuButton.GetComponent<ContentSizeFitter>());
        MenuButton.transform.SetParent(eventOptionsBox, false);
        MenuButton.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("TRIAL_defense_plead_guilty");
        MenuButton.onClick.AddListener(() => { actions.selection(character, TrialActions.TrialSelection.PLEAD_GUILTY); extraButtonAction(); });
        hotkey = MenuButton.GetComponent<ButtonHotkey>();
        hotkey.key = KeyCode.G;
        hotkey.blockers.Add(uiController.popupBlocker);

        MenuButton = Instantiate(p_MenuButton);
        generatedObjects.Add(MenuButton.gameObject);
        Destroy(MenuButton.GetComponent<ContentSizeFitter>());
        MenuButton.transform.SetParent(eventOptionsBox, false);
        MenuButton.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("TRIAL_defense_ace");
        MenuButton.onClick.AddListener(() => { actions.selection(character, TrialActions.TrialSelection.ACE_ATTORNEY); extraButtonAction(); });
        hotkey = MenuButton.GetComponent<ButtonHotkey>();
        hotkey.key = KeyCode.A;
        hotkey.blockers.Add(uiController.popupBlocker);
        if (MasterController.lcs.Money < 5000)
            MenuButton.interactable = false;

        if(character.getComponent<CriminalRecord>().sleeperLawyer != null)
        {
            MenuButton = Instantiate(p_MenuButton);
            generatedObjects.Add(MenuButton.gameObject);
            Destroy(MenuButton.GetComponent<ContentSizeFitter>());
            MenuButton.transform.SetParent(eventOptionsBox, false);
            MenuButton.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("TRIAL_defense_sleeper").Replace("$SLEEPERNAME", character.getComponent<CriminalRecord>().sleeperLawyer.getComponent<CreatureInfo>().getName());
            MenuButton.onClick.AddListener(() => { actions.selection(character, TrialActions.TrialSelection.SLEEPER_ATTORNEY); extraButtonAction(); });
            hotkey = MenuButton.GetComponent<ButtonHotkey>();
            hotkey.key = KeyCode.S;
            hotkey.blockers.Add(uiController.popupBlocker);
        }
    }

    private void extraButtonAction()
    {
        disableButtons();
        allowCharSelection = false;
    }
}
