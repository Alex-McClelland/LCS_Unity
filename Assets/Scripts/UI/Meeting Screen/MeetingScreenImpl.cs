using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine.UI;
using LCS.Engine;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.World;

public class MeetingScreenImpl : MonoBehaviour, Meeting {

    public UIControllerImpl uiController;

    public Text t_Name;
    public Text t_Title;
    public Text t_Text;
    public PortraitImage i_Portrait;

    public GameObject recruitmentButtons;
    public Button b_useProps;
    public Button b_discuss;
    public Button b_invite;
    public Button b_dismiss;

    public GameObject datingButtons;
    public Button b_spendCash;
    public Button b_cheapDate;
    public Button b_vacation;
    public Button b_breakup;
    public Button b_kidnap;

    public Entity character;

    private MeetingActions actions;
    private bool allowCharSelection;

    private enum ScreenMode
    {
        RECRUIT,
        DATE
    }

    private ScreenMode screenMode;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void init(MeetingActions actions)
    {
        this.actions = actions;
    }

    public void showMeeting(Entity e)
    {
        character = e;
        t_Text.text = "";
        t_Title.text = "Adventures in Liberal Recruitment";
        screenMode = ScreenMode.RECRUIT;
        recruitmentButtons.SetActive(true);
        datingButtons.SetActive(false);
        show();
    }

    public void showDate(Entity e)
    {
        character = e;
        t_Text.text = "";
        screenMode = ScreenMode.DATE;
        recruitmentButtons.SetActive(false);
        datingButtons.SetActive(true);
        b_kidnap.GetComponentInChildren<Text>().text = character.getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.FEMALE ? "E - Just kidnap the Conservative bitch" : "E - Just kidnap the Conservative prick";
        show();
    }

    public void show()
    {
        gameObject.SetActive(true);
        uiController.addCurrentScreen(this);

        t_Name.text = character.getComponent<CreatureInfo>().givenName + " " + (character.getComponent<CreatureInfo>().alias != "" ? "\"" + character.getComponent<CreatureInfo>().alias + "\" " : "") + character.getComponent<CreatureInfo>().surname;
        i_Portrait.buildPortrait(character);
        setButtonInteractivity();
    }

    public void hide()
    {
        gameObject.SetActive(false);
        uiController.ClearAnyKeyBind();
    }

    public void close()
    {
        hide();
        uiController.removeCurrentScreen(this);
    }

    public void refresh()
    {

    }

    public void printTitle(string text)
    {
        t_Title.text = text;
    }

    public void printText(string text)
    {
        t_Text.text += text;
    }

    public void discussion(bool useProps)
    {
        disableButtons();
        uiController.RegisterAnyKeyBind(MasterController.GetMC().doNextAction);
        allowCharSelection = false;
        actions.discussion(character, useProps);
    }

    public void invite()
    {
        disableButtons();
        uiController.RegisterAnyKeyBind(MasterController.GetMC().doNextAction);
        allowCharSelection = false;
        actions.joinLCS(character);
    }

    public void endMeetings()
    {
        disableButtons();
        uiController.RegisterAnyKeyBind(MasterController.GetMC().doNextAction);
        allowCharSelection = false;
        actions.endMeetings(character);
    }

    public void date(bool spendMoney)
    {
        disableButtons();
        uiController.RegisterAnyKeyBind(MasterController.GetMC().doNextAction);
        allowCharSelection = false;
        actions.normalDate(character, spendMoney);
    }

    public void vacation()
    {
        disableButtons();
        uiController.RegisterAnyKeyBind(MasterController.GetMC().doNextAction);
        allowCharSelection = false;
        actions.vacation(character);
    }

    public void breakUp()
    {
        disableButtons();
        uiController.RegisterAnyKeyBind(MasterController.GetMC().doNextAction);
        allowCharSelection = false;
        actions.breakUp(character);
    }

    public void kidnap()
    {
        disableButtons();
        uiController.RegisterAnyKeyBind(MasterController.GetMC().doNextAction);
        allowCharSelection = false;
        actions.kidnap(character);
    }

    public void selectCharacter()
    {
        if (allowCharSelection)
        {
            hide();
            uiController.charInfo.show(character);
        }
    }

    private void disableButtons()
    {
        uiController.ClearAnyKeyBind();
        b_useProps.interactable = false;
        b_discuss.interactable = false;
        b_invite.interactable = false;
        b_dismiss.interactable = false;
        b_spendCash.interactable = false;
        b_cheapDate.interactable = false;
        b_vacation.interactable = false;
        b_breakup.interactable = false;
        b_kidnap.interactable = false;
    }

    private void setButtonInteractivity()
    {
        allowCharSelection = true;

        if (screenMode == ScreenMode.RECRUIT)
        {
            if (MasterController.lcs.Money < 50)
            {
                b_useProps.interactable = false;
                b_useProps.GetComponent<MouseOverText>().mouseOverText = "Not enough money";
            }
            else
            {
                b_useProps.interactable = true;
                b_useProps.GetComponent<MouseOverText>().mouseOverText = "";
            }

            b_discuss.interactable = true;

            if (character.getComponent<Recruit>().eagerness < 4)
            {
                b_invite.interactable = false;
                b_invite.GetComponent<MouseOverText>().mouseOverText = character.getComponent<CreatureInfo>().getName() + " isn't ready to join the LCS";
            }
            else if (!character.getComponent<Recruit>().recruiter.getComponent<Liberal>().canRecruit())
            {
                b_invite.interactable = false;
                b_invite.GetComponent<MouseOverText>().mouseOverText = character.getComponent<Recruit>().recruiter.getComponent<CreatureInfo>().getName() + " needs more Juice to recruit";
            }
            else
            {
                b_invite.interactable = true;
                b_invite.GetComponent<MouseOverText>().mouseOverText = "";
            }

            b_dismiss.interactable = true;
        }
        else if (screenMode == ScreenMode.DATE)
        {
            if (MasterController.lcs.Money < 100)
            {
                b_spendCash.interactable = false;
                b_spendCash.GetComponent<MouseOverText>().mouseOverText = "Not enough money";
            }
            else
            {
                b_spendCash.interactable = true;
                b_spendCash.GetComponent<MouseOverText>().mouseOverText = "";
            }

            b_cheapDate.interactable = true;

            if (character.getComponent<Dating>().partner.getComponent<Body>().Blood < 100 || 
                character.getComponent<Dating>().partner.getComponent<Liberal>().status == Liberal.Status.HOSPITAL)
            {
                b_vacation.interactable = false;
                b_vacation.GetComponent<MouseOverText>().mouseOverText = "Too injured to travel";
            }
            else
            {
                b_vacation.interactable = true;
                b_vacation.GetComponent<MouseOverText>().mouseOverText = "";
            }

            b_breakup.interactable = true;

            if (character.getComponent<CreatureInfo>().alignment != Alignment.CONSERVATIVE)
            {
                b_kidnap.gameObject.SetActive(false);
            }
            else
            {
                b_breakup.gameObject.SetActive(true);
                if (character.getComponent<Dating>().partner.getComponent<Liberal>().status != Liberal.Status.HOSPITAL)
                {
                    b_kidnap.interactable = true;
                    b_kidnap.GetComponent<MouseOverText>().mouseOverText = "";
                }
                else
                {
                    b_kidnap.interactable = false;
                    b_kidnap.GetComponent<MouseOverText>().mouseOverText = "Too injured for that";
                }
            }
        }
    }
}
