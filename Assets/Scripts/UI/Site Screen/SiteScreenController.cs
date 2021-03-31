using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.UI;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.Location;
using LCS.Engine.Components.World;
using LCS.Engine.Data;

public class SiteScreenController : MonoBehaviour, SiteMode
{
    private Entity location;

    public UIControllerImpl uiController;

    public GameObject mapObject;
    public GameObject encounterMapObject;

    public GameObject mainView;
    public GameObject encounterView;

    public WorldSpaceMap worldSpaceMap;
    public MessageLog messageLog;
    public MessageLog encounterMessageLog;
    public Text t_siteStatus;
    public GameObject screenBlocker;
    public GameObject talkButtonHolder;
    public GameObject combatTalkButtonHolder;
    public GameObject kidnapButtonHolder;
    public GameObject robBankButtonHolder;
    public Button b_EncounterWarning;
    public Button b_Use;
    public Button b_Get;
    public Button b_Release;
    public Button b_Rob;
    public Button b_TalkButton;
    public Button b_TalkCancel;
    public Button b_TalkIssues;
    public Button b_TalkDating;
    public Button b_TalkRentRoom;
    public Button b_TalkBuyWeapons;
    public Button b_Fight;
    public Button b_Kidnap;
    public Button b_KidnapCommit;
    public Button b_Surrender;
    public Button b_ThreatenHostage;

    public List<Button> buttons;
    public List<Button> talkButtons;

    private SiteModeActions actions;
    private bool selectionMode;
    private bool encounterWarning = false;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        //Talk mode shortcuts
        if (selectionMode)
        {
            int selectionNum = -1;

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                selectionNum = 0;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                selectionNum = 1;
            }
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                selectionNum = 2;
            }
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            {
                selectionNum = 3;
            }
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
            {
                selectionNum = 4;
            }
            if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
            {
                selectionNum = 5;
            }

            if(selectionNum != -1) selectByNumber(selectionNum);

            //Back one step
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                if (uiController.enemyUIImpl.hasSelection())
                {
                    uiController.enemyUIImpl.clearSelection();
                }
                else if (uiController.squadUIImpl.hasSelection())
                {
                    uiController.squadUIImpl.clearSelection();
                }
            }
        }

        if (MasterController.GetMC().currentSiteModeScene != null)
        {
            Position p = MasterController.GetMC().currentSiteModeScene.squadPosition;

            if ((location.getComponent<TroubleSpot>().map[p.z][p.x, p.y].hasComponent<TileSpecial>() &&
                !location.getComponent<TroubleSpot>().map[p.z][p.x, p.y].getComponent<TileSpecial>().used &&
                location.getComponent<TroubleSpot>().map[p.z][p.x, p.y].getComponent<TileSpecial>().isUsable()) ||
                location.getComponent<TroubleSpot>().map[p.z][p.x, p.y].getComponent<TileFloor>().type == TileFloor.Type.STAIRS_DOWN ||
                location.getComponent<TroubleSpot>().map[p.z][p.x, p.y].getComponent<TileFloor>().type == TileFloor.Type.STAIRS_UP)
                b_Use.interactable = true;
            else
                b_Use.interactable = false;

            bool getInteractive = true;

            if (location.getComponent<TroubleSpot>().map[p.z][p.x, p.y].getComponent<TileBase>().loot.Count > 0 ||
                location.getComponent<TroubleSpot>().map[p.z][p.x, p.y].getComponent<TileBase>().cash > 0)
            {
                getInteractive = true;
                b_Get.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("SITE_get");
            }
            else
                getInteractive = false;

            if (!getInteractive)
            {
                if (MasterController.GetMC().currentSiteModeScene.canGraffiti())
                {
                    getInteractive = true;
                    b_Get.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("SITE_graffiti");
                }
                else
                {
                    b_Get.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("SITE_get");
                }
            }

            b_Get.interactable = getInteractive;

            if (MasterController.GetMC().currentSiteModeScene.encounterEntities.Exists(e => e != null && e.def == "LANDLORD"))
            {
                b_TalkRentRoom.gameObject.SetActive(true);
            }
            else
            {
                b_TalkRentRoom.gameObject.SetActive(false);
            }

            if (MasterController.GetMC().currentSiteModeScene.encounterEntities.Exists(e => e != null && e.def == "GANGMEMBER") &&
                !(MasterController.GetMC().currentSiteModeScene.location.def == "GOVERNMENT_POLICE_STATION" ||
                MasterController.GetMC().currentSiteModeScene.location.def == "GOVERNMENT_COURTHOUSE" ||
                MasterController.GetMC().currentSiteModeScene.location.def == "GOVERNMENT_PRISON"))
            {
                b_TalkBuyWeapons.gameObject.SetActive(true);
            }
            else
            {
                b_TalkBuyWeapons.gameObject.SetActive(false);
            }

            if (MasterController.GetMC().currentSiteModeScene.encounterEntities.Exists(e => e != null && e.def == "BANK_TELLER") &&
                MasterController.GetMC().currentSiteModeScene.location.def == "BUSINESS_BANK" &&
                !MasterController.GetMC().currentSiteModeScene.bankRobbed)
            {
                b_Rob.gameObject.SetActive(true);
            }
            else
            {
                b_Rob.gameObject.SetActive(false);
            }

            if (MasterController.GetMC().currentSiteModeScene.inEncounter &&
                MasterController.GetMC().currentSiteModeScene.encounterEntities.Exists(e=> e != null && ((e.getComponent<CreatureInfo>().encounterName == "Prisoner" && e.getComponent<CreatureInfo>().alignment == Alignment.LIBERAL) || (e.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.FREEABLE) != 0)))
            {
                b_Release.gameObject.SetActive(true);
            }
            else
            {
                b_Release.gameObject.SetActive(false);
            }

            if (MasterController.GetMC().currentSiteModeScene.encounterHasPolice())
            {
                b_Surrender.gameObject.SetActive(true);
            }
            else
            {
                b_Surrender.gameObject.SetActive(false);
            }

            if (MasterController.GetMC().currentSiteModeScene.haveHostage())
            {
                b_ThreatenHostage.gameObject.SetActive(true);
            }
            else
            {
                b_ThreatenHostage.gameObject.SetActive(false);
            }
        }
        //Talk mode
		if (uiController.squadUIImpl.selectedChar == null || uiController.enemyUIImpl.selectedChar == null)
        {
            foreach(Button b in talkButtons)
            {
                b.interactable = false;
                b.GetComponent<MouseOverText>().mouseOverText = MasterController.GetMC().getTranslation("SITE_talk_mouseover");
            }

            b_KidnapCommit.interactable = false;
            b_KidnapCommit.GetComponent<MouseOverText>().mouseOverText = MasterController.GetMC().getTranslation("SITE_talk_select_hostage_mouseover");
        }
        else
        {
            foreach (Button b in talkButtons)
            {
                b.interactable = true;
                b.GetComponent<MouseOverText>().mouseOverText = "";
            }
            if(uiController.enemyUIImpl.selectedChar.getComponent<Age>().isYoung() ||
                uiController.squadUIImpl.selectedChar.getComponent<Age>().isYoung())
            {
                b_TalkDating.interactable = false;
                b_TalkDating.GetComponent<MouseOverText>().mouseOverText = MasterController.GetMC().getTranslation("TALK_dating_too_young");
            }

            b_KidnapCommit.interactable = true;
            b_KidnapCommit.GetComponent<MouseOverText>().mouseOverText = "";
        }        
	}

    public void init(SiteModeActions actions)
    {
        this.actions = actions;
    }

    public void show()
    {
        uiController.addCurrentScreen(this);
        gameObject.SetActive(true);

        screenBlocker.SetActive(false);

        messageLog.updateMessageLog();
        encounterMessageLog.updateMessageLog();

        setStatusText();

        MasterController.GetMC().currentSiteModeScene.encounterWarnings = encounterWarning;

        if (encounterWarning)
        {
            b_EncounterWarning.image.color = uiController.buttonColorOn;
        }
        else
        {
            b_EncounterWarning.image.color = uiController.buttonColorOff;
        }
    }

    public void hide()
    {
        uiController.ClearAnyKeyBind();
        gameObject.SetActive(false);
    }

    public void close()
    {
        hide();
        worldSpaceMap.cleanMap();
        uiController.removeCurrentScreen(this);
    }

    public void move(string direction)
    {        
        deactivateButtons();
        worldSpaceMap.fullMapView = false;
        uiController.RegisterAnyKeyBind(advance);
        actions.move(direction);
    }

    public void fight()
    {
        deactivateButtons();
        uiController.RegisterAnyKeyBind(advance);
        actions.fight();
    }

    public void wait()
    {
        deactivateButtons();
        uiController.RegisterAnyKeyBind(advance);
        actions.wait();
    }

    public void use()
    {
        deactivateButtons();
        uiController.RegisterAnyKeyBind(advance);
        actions.use();
    }

    public void talk()
    {
        selectionMode = true;
        uiController.squadUIImpl.changeSelectionMode(SquadUIImpl.SelectionMode.CHOOSE_TALK_TARGET);
        uiController.enemyUIImpl.changeSelectionMode(EnemyUIImpl.SelectionMode.CHOOSE_TALK_TARGET);
        deactivateButtons(false);

        if (MasterController.GetMC().currentSiteModeScene.encounterHasEnemies() &&
            MasterController.GetMC().currentSiteModeScene.alarmTriggered)
        {
            combatTalkButtonHolder.SetActive(true);
        }
        else
        {
            talkButtonHolder.SetActive(true);
        }
    }

    public void robBank()
    {
        selectionMode = true;
        deactivateButtons(false);
        robBankButtonHolder.SetActive(true);
    }

    public void cancelRobBank()
    {
        screenBlocker.SetActive(false);
        activateButtons();
        robBankButtonHolder.SetActive(false);
        selectionMode = false;
    }

    public void robBankNote()
    {
        actions.robBankNote();
        cancelRobBank();
        deactivateButtons();
        uiController.RegisterAnyKeyBind(advance);
        selectionMode = false;
    }

    public void robBankThreaten()
    {
        actions.robBankThreaten();
        cancelRobBank();
        deactivateButtons();
        uiController.RegisterAnyKeyBind(advance);
        selectionMode = false;
    }

    private void selectByNumber(int i)
    {
        if (uiController.squadUIImpl.hasSelection())
            uiController.enemyUIImpl.selectSquadMember(i);
        else
            uiController.squadUIImpl.selectSquadMember(i);
    }

    public void kidnap()
    {
        selectionMode = true;
        uiController.squadUIImpl.changeSelectionMode(SquadUIImpl.SelectionMode.CHOOSE_KIDNAP_TARGET);
        uiController.enemyUIImpl.changeSelectionMode(EnemyUIImpl.SelectionMode.CHOOSE_KIDNAP_TARGET);
        deactivateButtons(false);
        kidnapButtonHolder.SetActive(true);
    }

    public void loot()
    {
        deactivateButtons();
        uiController.RegisterAnyKeyBind(advance);
        actions.loot();
    }

    public void release()
    {
        deactivateButtons();
        uiController.RegisterAnyKeyBind(advance);
        actions.releaseOppressed();
    }

    public void talkIssues()
    {
        actions.talkIssues(uiController.squadUIImpl.selectedChar, uiController.enemyUIImpl.selectedChar);
        cancelTalk();
        deactivateButtons();
        uiController.RegisterAnyKeyBind(advance);
        selectionMode = false;
    }

    public void talkDating()
    {
        actions.talkDating(uiController.squadUIImpl.selectedChar, uiController.enemyUIImpl.selectedChar);
        cancelTalk();
        deactivateButtons();
        uiController.RegisterAnyKeyBind(advance);
        selectionMode = false;
    }
    
    public void talkRentRoom()
    {
        actions.talkRentRoom();
        cancelTalk();
        deactivateButtons();
        uiController.RegisterAnyKeyBind(advance);
        selectionMode = false;
    }

    public void talkBuyWeapons()
    {
        actions.talkBuyWeapons();
        cancelTalk();
        deactivateButtons();
        uiController.RegisterAnyKeyBind(advance);
        selectionMode = false;
    }

    public void talkIntimidate()
    {
        actions.talkIntimidate();
        cancelTalk();
        deactivateButtons();
        uiController.RegisterAnyKeyBind(advance);
        selectionMode = false;
    }

    public void talkThreatenHostage()
    {
        actions.talkThreatenHostage();
        cancelTalk();
        deactivateButtons();
        uiController.RegisterAnyKeyBind(advance);
        selectionMode = false;
    }

    public void talkBluff()
    {
        actions.talkBluff();
        cancelTalk();
        deactivateButtons();
        uiController.RegisterAnyKeyBind(advance);
        selectionMode = false;
    }

    public void talkSurrender()
    {
        actions.surrender();
        cancelTalk();
        deactivateButtons();
        uiController.RegisterAnyKeyBind(advance);
        selectionMode = false;
    }

    public void kidnapTarget()
    {
        actions.kidnap(uiController.squadUIImpl.selectedChar, uiController.enemyUIImpl.selectedChar);
        cancelKidnap();
        deactivateButtons();
        uiController.RegisterAnyKeyBind(advance);
        selectionMode = false;
    }

    public void cancelTalk()
    {
        uiController.squadUIImpl.changeSelectionMode(SquadUIImpl.SelectionMode.VIEW_CHARINFO);
        uiController.enemyUIImpl.changeSelectionMode(EnemyUIImpl.SelectionMode.VIEW_CHARINFO);
        screenBlocker.SetActive(false);
        activateButtons();
        talkButtonHolder.SetActive(false);
        combatTalkButtonHolder.SetActive(false);
        uiController.squadUIImpl.clearSelection();
        uiController.enemyUIImpl.clearSelection();
        selectionMode = false;
    }

    public void cancelKidnap()
    {
        uiController.squadUIImpl.changeSelectionMode(SquadUIImpl.SelectionMode.VIEW_CHARINFO);
        uiController.enemyUIImpl.changeSelectionMode(EnemyUIImpl.SelectionMode.VIEW_CHARINFO);
        screenBlocker.SetActive(false);
        activateButtons();
        kidnapButtonHolder.SetActive(false);
        uiController.squadUIImpl.clearSelection();
        uiController.enemyUIImpl.clearSelection();
        selectionMode = false;
    }

    private void checkForControl()
    {
        if (!MasterController.GetMC().currentSiteModeScene.processingRound && !selectionMode)
        {
            uiController.ClearAnyKeyBind();
            activateButtons();
            screenBlocker.SetActive(false);
        }
    }

    public void advance()
    {
        if (MasterController.GetMC().currentSiteModeScene == null) return;

        if (MasterController.GetMC().currentSiteModeScene.processingRound == true)
        {
            actions.advanceRound();
            if(MasterController.GetMC().currentSiteModeScene != null && 
                MasterController.GetMC().currentSiteModeScene.processingRound == false)
            {
                uiController.ClearAnyKeyBind();
                activateButtons();
                screenBlocker.SetActive(false);                
            }
        }
        else
        {
            uiController.ClearAnyKeyBind();
            activateButtons();
            screenBlocker.SetActive(false);
        }
    }

    public void buildMap(Entity location, int z)
    {
        Position squadPosition = MasterController.GetMC().currentSiteModeScene.squadPosition;
        this.location = location;
        activateButtons();

        worldSpaceMap.cleanMap();
        worldSpaceMap.buildMap(location.getComponent<TroubleSpot>().map[z], z);
        worldSpaceMap.setPosition(squadPosition.x, squadPosition.y);

        refresh();
    }

    public void refresh()
    {
        if (MasterController.GetMC().currentSiteModeScene == null) return;
        if (!gameObject.activeSelf) return;

        Position squadPosition = MasterController.GetMC().currentSiteModeScene.squadPosition;

        worldSpaceMap.refreshMap();
        worldSpaceMap.setPosition(squadPosition.x, squadPosition.y);
        messageLog.updateMessageLog();
        encounterMessageLog.updateMessageLog();
        setStatusText();
        checkForControl();
    }

    public void startEncounter()
    {
        mapObject.SetActive(false);
        encounterMapObject.SetActive(true);
        mainView.SetActive(false);
        encounterView.SetActive(true);
    }

    public void leaveEncounter()
    {
        mapObject.SetActive(true);
        encounterMapObject.SetActive(false);
        mainView.SetActive(true);
        encounterView.SetActive(false);
    }
    
    public void toggleEncounterWarning()
    {
        if (encounterWarning)
        {
            encounterWarning = false;
            actions.setEncounterWarnings(false);
            b_EncounterWarning.image.color = uiController.buttonColorOff;
        }
        else
        {
            encounterWarning = true;
            actions.setEncounterWarnings(true);
            b_EncounterWarning.image.color = uiController.buttonColorOn;
        }
    }

    public void toggleMapZoom()
    {
        worldSpaceMap.fullMapView = !worldSpaceMap.fullMapView;
    }

    private void activateButtons()
    {
        foreach(Button b in buttons)
        {
            b.interactable = true;
        }

        if (uiController.enemyUIImpl.noTalkers())
        {
            b_TalkButton.interactable = false;
            b_TalkButton.GetComponent<MouseOverText>().mouseOverText = MasterController.GetMC().getTranslation("SITE_talk_no_talkers_mouseover");
        }
        else
        {
            b_TalkButton.GetComponent<MouseOverText>().mouseOverText = "";
        }

        if(uiController.enemyUIImpl.noBluff() && 
            MasterController.GetMC().currentSiteModeScene.alarmTriggered &&
            MasterController.GetMC().currentSiteModeScene.encounterHasEnemies())
        {
            b_TalkButton.interactable = false;
            b_TalkButton.GetComponent<MouseOverText>().mouseOverText = MasterController.GetMC().getTranslation("SITE_talk_no_bluff_mouseover");
        }
        else
        {
            b_TalkButton.GetComponent<MouseOverText>().mouseOverText = "";
        }

        if(!MasterController.GetMC().currentSiteModeScene.encounterHasEnemies())
        {
            b_Fight.interactable = false;
            b_Kidnap.interactable = false;
            b_Kidnap.GetComponent<MouseOverText>().mouseOverText = "";
        }
        else
        {
            b_Fight.interactable = true;
            if (uiController.enemyUIImpl.noKidnap())
            {
                b_Kidnap.interactable = false;
                b_Kidnap.GetComponent<MouseOverText>().mouseOverText = MasterController.GetMC().getTranslation("SITE_kidnap_too_dangerous_mouseover");
            }
            else
            {
                b_Kidnap.interactable = true;
                b_Kidnap.GetComponent<MouseOverText>().mouseOverText = "";
            }
        }
    }

    private void deactivateButtons(bool useScreenBlocker = true)
    {
        screenBlocker.SetActive(useScreenBlocker);

        foreach (Button b in buttons)
        {
            b.interactable = false;
        }
    }

    private void setStatusText()
    {
        string text = location.getComponent<SiteBase>().getCurrentName();
        if(location.getComponent<TroubleSpot>().map.Count > 1)
        {
            text += " " + MasterController.GetMC().getTranslation("SITE_status_floor").Replace("$FLOOR", MasterController.shortOrdinal((MasterController.GetMC().currentSiteModeScene.squadPosition.z + 1)));
        }

        if (MasterController.GetMC().currentSiteModeScene.location.hasComponent<SafeHouse>() &&
            MasterController.GetMC().currentSiteModeScene.location.getComponent<SafeHouse>().underSiege)
        {
            text += " -- <color=red>" + MasterController.GetMC().getTranslation("SITE_status_under_siege") + "</color>";
        }
        else
        {
            if (MasterController.GetMC().currentSiteModeScene.alarmTriggered)
            {
                if (MasterController.GetMC().currentSiteModeScene.alarmTimer > 80)
                {
                    switch (location.getComponent<TroubleSpot>().getResponseType())
                    {
                        case LocationDef.EnemyType.ARMY:
                            text += " -- <b><color=red>" + MasterController.GetMC().getTranslation("SITE_status_military") + "</color></b>";
                            break;
                        case LocationDef.EnemyType.AGENT:
                            text += " -- <b><color=red>" + MasterController.GetMC().getTranslation("SITE_status_agents") + "</color></b>";
                            break;
                        case LocationDef.EnemyType.MERC:
                            text += " -- <b><color=red>" + MasterController.GetMC().getTranslation("SITE_status_mercenaries") + "</color></b>";
                            break;
                        case LocationDef.EnemyType.REDNECK:
                            text += " -- <b><color=red>" + MasterController.GetMC().getTranslation("SITE_status_redneck") + "</color></b>";
                            break;
                        case LocationDef.EnemyType.GANG:
                            text += " -- <b><color=red>" + MasterController.GetMC().getTranslation("SITE_status_gang") + "</color></b>";
                            break;
                        case LocationDef.EnemyType.CCS:
                            text += " -- <b><color=red>" + MasterController.GetMC().getTranslation("SITE_status_ccs") + "</color></b>";
                            break;
                        default:
                            if (MasterController.government.laws["POLICE"].alignment == Alignment.ARCHCONSERVATIVE &&
                                MasterController.government.laws["DEATH_PENALTY"].alignment == Alignment.ARCHCONSERVATIVE)
                                text += " -- <b><color=red>" + MasterController.GetMC().getTranslation("SITE_status_death_squad") + "</color></b>";
                            else
                                text += " -- <b><color=red>" + MasterController.GetMC().getTranslation("SITE_status_police") + "</color></b>";
                            break;
                    }
                }
                else if (MasterController.GetMC().currentSiteModeScene.alarmTimer > 60)
                {
                    text += " -- <b><color=red>" + MasterController.GetMC().getTranslation("SITE_status_reinforcements") + "</color></b>";
                }
                else
                {
                    if (MasterController.GetMC().currentSiteModeScene.siteAlienate == 2)
                    {
                        text += " -- <color=red>" + MasterController.GetMC().getTranslation("SITE_status_alienated_all") + "</color>";
                    }
                    else if (MasterController.GetMC().currentSiteModeScene.siteAlienate == 1)
                    {
                        text += " -- <color=red>" + MasterController.GetMC().getTranslation("SITE_status_alienated_masses") + "</color>";
                    }
                    else
                    {
                        text += " -- <color=red>" + MasterController.GetMC().getTranslation("SITE_status_conservatives_alarmed") + "</color>";
                    }
                }
            }
            else if (MasterController.GetMC().currentSiteModeScene.suspicionTimer == 0)
            {
                text += " -- <color=yellow>" + MasterController.GetMC().getTranslation("SITE_status_conservatives_suspicious") + "</color>";
            }
        }

        t_siteStatus.text = text;
    }
}
