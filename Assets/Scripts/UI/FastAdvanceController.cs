using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine.UI;
using LCS.Engine;
using LCS.Engine.Components.Creature;

public class FastAdvanceController : MonoBehaviour, FastAdvanceUI {

    public UIControllerImpl uiController;
    public LiberalAgendaImpl agenda;
    public Text t_Date;
    public Text t_PublicMood;
    public MessageLog log;
    public Button b_Reactivate;
    public Button b_Exit;

    private FastAdvanceActions actions;
    private bool advancing;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (advancing && MasterController.GetMC().phase == MasterController.Phase.BASE)
        {
            MasterController.GetMC().nextPhase();

            if (MasterController.GetMC().canSeeThings)
            {
                advancing = false;
            }

            if (MasterController.GetMC().currentDate.Day == 1)
            {
                advancing = false;
                refresh();
            }
        }

        if (advancing)
        {
            b_Reactivate.interactable = false;
            b_Exit.interactable = false;
        }
        else
        {
            b_Reactivate.interactable = true;
            b_Exit.interactable = true;
        }
	}

    public void init(FastAdvanceActions actions)
    {
        this.actions = actions;
    }

    public void show()
    {
        gameObject.SetActive(true);
        uiController.addCurrentScreen(this);
        agenda.show();
        refresh();

        if (MasterController.GetMC().currentDate.Day != 1) advancing = true;
    }

    public void hide()
    {
        gameObject.SetActive(false);
    }

    public void close()
    {
        hide();
        uiController.removeCurrentScreen(this);
    }

    public void refresh()
    {
        agenda.refresh();
        t_Date.text = MasterController.GetMC().currentDate.ToString("D");
        string moodColor = "<color=white>";
        if (MasterController.generalPublic.PublicMood <= 20)
            moodColor = "<color=red>";
        else if (MasterController.generalPublic.PublicMood <= 40)
            moodColor = "<color=magenta>";
        else if (MasterController.generalPublic.PublicMood <= 60)
            moodColor = "<color=yellow>";
        else if (MasterController.generalPublic.PublicMood <= 80)
            moodColor = "<color=cyan>";
        else
            moodColor = "<color=lime>";
        t_PublicMood.text = GameData.getData().translationList["DISBAND_public_mood"] + "\n" + moodColor + MasterController.generalPublic.PublicMood + "</color>%";
        log.updateMessageLog();
    }

    public void advanceMonth()
    {
        advancing = true;
    }

    public void reactivate()
    {
        advancing = false;
        actions.reform();
    }
}
