using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LCS.Engine;
using LCS.Engine.UI;
using LCS.Engine.UI.UIEvents;
using LCS.Engine.Components.Creature;

public class SquadUIImpl : MonoBehaviour, Squad {

    public enum SelectionMode
    {
        VIEW_CHARINFO,
        CHOOSE_TALK_TARGET,
        CHOOSE_KIDNAP_TARGET,
        CHOOSE_BUYER
    }

    public Transform layout;
    public UIControllerImpl uiController;
    public TalkBubble p_TalkBubble;

    public SquadMemberUI p_SquadMemberUI;
    public List<SquadMemberUI> SquadMembers;

    public Entity selectedChar { get; set; }

    private SelectionMode selectionMode;
    private List<Entity> squad;
    private SquadActions actions;

    // Use this for initialization
    void Start () {
        uiController.speak += speak;
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void init(SquadActions actions)
    {
        this.actions = actions;
    }

    public void show()
    {
        gameObject.SetActive(true);
        uiController.addCurrentScreen(this);        
    }

    public void selectSquadMember(Entity e)
    {
        switch (selectionMode) {
            case SelectionMode.VIEW_CHARINFO:
                actions.selectChar(e);
                break;
            case SelectionMode.CHOOSE_TALK_TARGET:                
                clearSelection();
                SquadMembers[squad.IndexOf(e)].i_SelectionBorder.gameObject.SetActive(true);
                selectedChar = e;
                break;
            case SelectionMode.CHOOSE_KIDNAP_TARGET:
                if (e.getComponent<Liberal>().hauledUnit != null) break;
                clearSelection();
                SquadMembers[squad.IndexOf(e)].i_SelectionBorder.gameObject.SetActive(true);
                selectedChar = e;
                break;
        }
    }

    public void selectSquadMember(int i)
    {
        if (i >= squad.Count) return;

        selectSquadMember(squad[i]);
    }

    public bool displaySquad(List<Entity> newSquad)
    {
        if (SquadMembers == null) SquadMembers = new List<SquadMemberUI>();
        this.squad = newSquad;

        show();

        if (squad != null)
        {            
            for (int i = 0; i < squad.Count; i++)
            {
                if (i >= SquadMembers.Count)
                {
                    SquadMemberUI squadMember = Instantiate(p_SquadMemberUI);
                    squadMember.squadUI = this;
                    squadMember.transform.SetParent(layout, false);
                    SquadMembers.Add(squadMember);
                }
                SquadMembers[i].displayCharacter(squad[i]);
                SquadMembers[i].gameObject.SetActive(true);                
            }

            for (int i = squad.Count; i < SquadMembers.Count; i++)
            {
                SquadMembers[i].gameObject.SetActive(false);
            }

            return true;
        }
        else
        {
            for (int i = 0; i < SquadMembers.Count; i++)
            {
                SquadMembers[i].gameObject.SetActive(false);
            }

            return false;
        }
    }

    public bool displayDriving(List<Entity> vehicles)
    {
        //NOT IMPLEMENTED
        return false;
    }

    public void close()
    {
        hide();
        uiController.removeCurrentScreen(this);
    }

    public void hide()
    {
        gameObject.SetActive(false);
    }

    public void refresh()
    {
        if (!gameObject.activeSelf) return;

        displaySquad(squad);
    }

    public void clearSelection()
    {
        selectedChar = null;
        foreach (SquadMemberUI sui in SquadMembers)
        {
            sui.i_SelectionBorder.gameObject.SetActive(false);
        }
    }

    public bool hasSelection()
    {
        return selectedChar != null;
    }

    public void changeSelectionMode(SelectionMode mode)
    {
        selectionMode = mode;
        switch (mode)
        {
            case SelectionMode.CHOOSE_TALK_TARGET:
                foreach(SquadMemberUI info in SquadMembers)
                {
                    int issues = info.character.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_CHARISMA].getModifiedValue() / 2 +
                        info.character.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].level;
                    int dating = info.character.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_CHARISMA].getModifiedValue() / 2 +
                        info.character.getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].level;
                    info.GetComponent<MouseOverText>().mouseOverText = "Issues: " + issues + "\nDating: " + dating;
                }
                break;
            case SelectionMode.CHOOSE_KIDNAP_TARGET:
                foreach (SquadMemberUI info in SquadMembers)
                {
                    if(info.character.getComponent<Liberal>().hauledUnit != null)
                        info.GetComponent<MouseOverText>().mouseOverText = "They are busy hauling someone already.";
                    else
                        info.GetComponent<MouseOverText>().mouseOverText = "";
                }
                break;
            case SelectionMode.VIEW_CHARINFO:
                foreach (SquadMemberUI info in SquadMembers)
                {
                    info.GetComponent<MouseOverText>().mouseOverText = "";
                }
                break;
        }
    }

    private void speak(object sender, Speak args)
    {
        if (!squad.Contains(args.speaker)) return;

        int i = 0;
        foreach(SquadMemberUI member in SquadMembers)
        {
            if (member.character == args.speaker)
                break;
            i++;
        }

        if (i >= SquadMembers.Count) return;

        TalkBubble talkBubble = Instantiate(p_TalkBubble);
        talkBubble.transform.SetParent(transform.GetComponentInParent<Canvas>().transform, false);
        talkBubble.showText(args.text, SquadMembers[i].bubbleRoot.position, Direction.LEFT, args.duration);
    }
}
