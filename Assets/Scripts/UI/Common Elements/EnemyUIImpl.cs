using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LCS.Engine;
using LCS.Engine.UI;
using LCS.Engine.UI.UIEvents;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.Item;
using LCS.Engine.Data;

public class EnemyUIImpl : MonoBehaviour, Squad {

    public enum SelectionMode
    {
        VIEW_CHARINFO,
        CHOOSE_TALK_TARGET,
        CHOOSE_KIDNAP_TARGET
    }

    private enum DisplayMode
    {
        NORMAL,
        DRIVE
    }

    public Transform layout;
    public UIControllerImpl uiController;
    public TalkBubble p_TalkBubble;

    public EnemyInfo p_EnemyInfo;
    public VehicleBoxUI p_VehicleBox;
    public List<EnemyInfo> enemyList;
    public List<VehicleBoxUI> vehicleList;

    public Entity selectedChar;

    private DisplayMode displayMode;
    private SelectionMode selectionMode;
    private SquadActions actions;
    private List<Entity> squad;
    private List<Entity> vehicles;

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
        if (e == null) return;

        switch (selectionMode)
        {
            case SelectionMode.VIEW_CHARINFO:
                actions.selectChar(e);
                break;
            case SelectionMode.CHOOSE_TALK_TARGET:
                if ((e.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.NO_BLUFF) != 0) break;
                clearSelection();
                enemyList[squad.IndexOf(e)].i_SelectionBorder.gameObject.SetActive(true);
                selectedChar = e;
                break;
            case SelectionMode.CHOOSE_KIDNAP_TARGET:
                if (((e.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.THREATENING) != 0 &&
                    e.getComponent<Body>().Blood > 20) ||
                    e.getComponent<CreatureInfo>().alignment != Alignment.CONSERVATIVE)
                    break;
                clearSelection();
                enemyList[squad.IndexOf(e)].i_SelectionBorder.gameObject.SetActive(true);
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
        displayMode = DisplayMode.NORMAL;
        if (enemyList == null) enemyList = new List<EnemyInfo>();
        this.squad = newSquad;

        show();

        if (squad != null)
        {
            for (int i = 0; i < squad.Count; i++)
            {
                if (i >= enemyList.Count)
                {
                    EnemyInfo enemy = Instantiate(p_EnemyInfo);
                    enemy.enemyUI = this;
                    enemy.transform.SetParent(layout, false);
                    enemyList.Add(enemy);
                }
                enemyList[i].displayCharacter(squad[i]);
                enemyList[i].gameObject.SetActive(true);
            }

            for (int i = squad.Count; i < enemyList.Count; i++)
            {
                enemyList[i].gameObject.SetActive(false);
            }

            return true;
        }
        else
        {
            for (int i = 0; i < enemyList.Count; i++)
            {
                enemyList[i].gameObject.SetActive(false);
            }

            return false;
        }
    }

    public bool displayDriving(List<Entity> newVehicles)
    {
        displayMode = DisplayMode.DRIVE;
        if (enemyList == null) enemyList = new List<EnemyInfo>();
        this.vehicles = newVehicles;
        squad = new List<Entity>();

        show();

        if (vehicles != null)
        {
            int i = 0;
            for (int j = 0; j < vehicles.Count;j++)
            {
                if(j >= vehicleList.Count)
                {
                    VehicleBoxUI vBox = Instantiate(p_VehicleBox);
                    vBox.transform.SetParent(layout, false);
                    vehicleList.Add(vBox);
                }
                for (int k = 0; k < vehicles[j].getComponent<Vehicle>().passengers.Count; k++)
                {
                    if (i >= enemyList.Count)
                    {
                        EnemyInfo enemy = Instantiate(p_EnemyInfo);
                        enemy.enemyUI = this;
                        enemy.transform.SetParent(vehicleList[j].transform, false);
                        enemyList.Add(enemy);
                    }
                    squad.Add(vehicles[j].getComponent<Vehicle>().passengers[k]);
                    enemyList[i].displayCharacter(vehicles[j].getComponent<Vehicle>().passengers[k]);
                    enemyList[i].gameObject.SetActive(true);

                    i++;
                }

                vehicleList[j].gameObject.SetActive(true);
                vehicleList[j].t_VehicleName.text = vehicles[j].getComponent<ItemBase>().getName();                

                while (i < enemyList.Count)
                {
                    enemyList[i].gameObject.SetActive(false);
                    i++;
                }
            }

            for(int j = vehicles.Count; j < vehicleList.Count; j++)
            {
                vehicleList[j].gameObject.SetActive(false);
            }

            return true;
        }
        else
        {
            for (int i = 0; i < vehicleList.Count; i++)
            {
                vehicleList[i].gameObject.SetActive(false);
            }

            return false;
        }
    }

    public void close()
    {
        foreach(EnemyInfo o in enemyList)
        {
            Destroy(o.gameObject);
        }
        enemyList.Clear();
        foreach(VehicleBoxUI o in vehicleList)
        {
            Destroy(o.gameObject);
        }
        vehicleList.Clear();
        hide();
        uiController.removeCurrentScreen(this);
    }

    public void hide()
    {
        gameObject.SetActive(false);
    }

    public void refresh()
    {
        if (displayMode == DisplayMode.NORMAL)
            displaySquad(squad);
        else if (displayMode == DisplayMode.DRIVE)
            displayDriving(vehicles);
    }

    public void clearSelection()
    {
        selectedChar = null;
        foreach (EnemyInfo enemy in enemyList)
        {
            enemy.i_SelectionBorder.gameObject.SetActive(false);
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
                foreach(EnemyInfo info in enemyList)
                {
                    if((info.character.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.NO_BLUFF) != 0)
                        info.GetComponent<MouseOverText>().mouseOverText = "This person no longer wants to talk to you.";
                }
                break;
            case SelectionMode.CHOOSE_KIDNAP_TARGET:
                foreach(EnemyInfo info in enemyList)
                {
                    if (((info.character.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.THREATENING) != 0 &&
                    info.character.getComponent<Body>().Blood > 20) &&
                    info.character.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
                        info.GetComponent<MouseOverText>().mouseOverText = "This person is too dangerous.";
                }
                break;
            case SelectionMode.VIEW_CHARINFO:
                foreach (EnemyInfo info in enemyList)
                {
                    info.GetComponent<MouseOverText>().mouseOverText = "";
                }
                break;
        }
    }

    public bool noTalkers()
    {
        if (squad == null) return true;
        foreach(Entity e in squad)
        {
            if (e == null) continue;
            if ((e.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.NO_BLUFF) == 0)
                return false;
        }

        return true;
    }

    public bool noKidnap()
    {
        if (squad == null) return true;
        foreach (Entity e in squad)
        {
            if (e == null) continue;
            if (((e.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.THREATENING) == 0 ||
                    e.getComponent<Body>().Blood <= 20) &&
                    e.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
                return false;
        }

        return true;
    }

    public bool noBluff()
    {
        if (squad == null) return true;
        foreach (Entity e in squad)
        {
            if (e == null) continue;
            if (e.getComponent<CreatureInfo>().alignment != Alignment.CONSERVATIVE) continue;
            if ((e.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.NO_BLUFF) == 0)
                return false;
        }

        return true;
    }

    private void speak(object sender, Speak args)
    {
        if (!squad.Contains(args.speaker)) return;

        int i = 0;
        foreach (EnemyInfo enemy in enemyList)
        {
            if (enemy.character == args.speaker)
                break;
            i++;
        }

        if (i >= enemyList.Count) return;

        TalkBubble talkBubble = Instantiate(p_TalkBubble);
        talkBubble.transform.SetParent(transform.GetComponentInParent<Canvas>().transform, false);
        talkBubble.showText(args.text, enemyList[i].bubbleRoot.position, Direction.RIGHT, args.duration);
    }
}
