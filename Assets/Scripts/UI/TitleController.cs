using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.UI;

public class TitleController : MonoBehaviour, TitlePage {

    public UIControllerImpl uiController;

    public Button b_NewGame;
    public Button b_LoadGame;

    public Text t_version;

    private TitlePageActions actions;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void init(TitlePageActions actions)
    {
        this.actions = actions;

        b_NewGame.onClick.AddListener(() => { close(); this.actions.newGame(); });
        b_LoadGame.onClick.AddListener(() => { close(); this.actions.loadGame(); });
        t_version.text = "Version: " + MasterController.CURRENT_VERSION;
    }

    public void show()
    {
        gameObject.SetActive(true);
        uiController.addCurrentScreen(this);

        if (!File.Exists(Application.persistentDataPath + "/Save.sav"))
            b_LoadGame.interactable = false;
        else
            b_LoadGame.interactable = true;
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
    }
}
