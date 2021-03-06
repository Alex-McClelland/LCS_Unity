using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.UI;
using LCS.Engine.Scenes;
using LCS.Engine.Data;

public class ChaseController : MonoBehaviour, Chase {

    public UIControllerImpl uiController;
    public SquadUIImpl squadUI;
    public EnemyUIImpl enemyUI;

    public Button p_MenuButton;
    public Transform eventOptionsBox;
    public MessageLog messageLog;
    public GameObject o_EventText;
    public Image i_EventTextBorder;
    public Text t_EventText;
    public Text t_EventTitle;
    public GameObject screenBlocker;

    private ChaseActions actions;
    private List<GameObject> generatedObjects;
    private List<GameObject> generatedButtons;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void show()
    {
        gameObject.SetActive(true);
        uiController.addCurrentScreen(this);

        generatedButtons = new List<GameObject>();
        generatedObjects = new List<GameObject>();

        messageLog.updateMessageLog();
        generateButtons();
        enableInput();
    }

    public void refresh()
    {
        messageLog.updateMessageLog();
    }

    public void hide()
    {
        gameObject.SetActive(false);

        uiController.ClearAnyKeyBind();
        disposeObjects();
    }

    public void close()
    {
        hide();
        uiController.removeCurrentScreen(this);
    }

    public void init(ChaseActions actions)
    {
        this.actions = actions;
    }

    private void disposeObjects()
    {
        foreach (GameObject o in generatedObjects)
        {
            Destroy(o);
        }

        foreach (GameObject o in generatedButtons)
        {
            Destroy(o);
        }

        generatedObjects.Clear();
        generatedButtons.Clear();
    }

    private void clearButtons()
    {
        foreach (GameObject o in generatedButtons)
        {
            Destroy(o);
        }

        generatedButtons.Clear();
        uiController.ClearAnyKeyBind();
    }

    public void disableInput()
    {
        foreach (GameObject o in generatedButtons)
        {
            Button b = o.GetComponent<Button>();

            if (b != null) b.interactable = false;
        }

        screenBlocker.SetActive(true);
        uiController.RegisterAnyKeyBind(actions.advance);
    }

    public void enableInput()
    {
        screenBlocker.SetActive(false);

        clearButtons();
        generateButtons();
        foreach (GameObject o in generatedButtons)
        {
            Button b = o.GetComponent<Button>();

            if (b != null) b.interactable = true;
        }
    }

    private void run()
    {
        disableInput();
        actions.run();
    }

    private void driveEscape()
    {
        disableInput();
        actions.driveEscape();
    }

    private void bail()
    {
        disableInput();
        actions.bail();
    }

    private void driveObstacleRisky()
    {
        disableInput();
        actions.driveObstacleRisky();
    }

    private void driveObstacleSafe()
    {
        disableInput();
        actions.driveObstacleSafe();
    }

    private void fight()
    {
        disableInput();
        actions.fight();
    }

    private void surrender()
    {
        disableInput();
        actions.surrender();
    }

    private void generateButtons()
    {
        MasterController mc = MasterController.GetMC();

        if (mc.currentChaseScene.chaseType == ChaseScene.ChaseType.FOOT ||
            mc.currentChaseScene.chaseType == ChaseScene.ChaseType.SIEGE)
        {
            if (mc.currentChaseScene.chasePhase == ChaseScene.ChasePhase.SELECTION)
            {
                Button MenuButton = Instantiate(p_MenuButton);
                generatedButtons.Add(MenuButton.gameObject);
                Destroy(MenuButton.GetComponent<ContentSizeFitter>());
                MenuButton.transform.SetParent(eventOptionsBox, false);
                MenuButton.GetComponentInChildren<Text>().text = "(R)un For It!";
                MenuButton.onClick.AddListener(run);
                ButtonHotkey hotkey = MenuButton.GetComponent<ButtonHotkey>();
                hotkey.key = KeyCode.R;
                hotkey.blockers.Add(uiController.popupBlocker);

                MenuButton = Instantiate(p_MenuButton);
                generatedButtons.Add(MenuButton.gameObject);
                Destroy(MenuButton.GetComponent<ContentSizeFitter>());
                MenuButton.transform.SetParent(eventOptionsBox, false);
                MenuButton.GetComponentInChildren<Text>().text = "(F)ight!";
                MenuButton.onClick.AddListener(fight);
                hotkey = MenuButton.GetComponent<ButtonHotkey>();
                hotkey.key = KeyCode.F;
                hotkey.blockers.Add(uiController.popupBlocker);

                if (mc.currentChaseScene.chaserType == LocationDef.EnemyType.POLICE)
                {
                    MenuButton = Instantiate(p_MenuButton);
                    generatedButtons.Add(MenuButton.gameObject);
                    Destroy(MenuButton.GetComponent<ContentSizeFitter>());
                    MenuButton.transform.SetParent(eventOptionsBox, false);
                    MenuButton.GetComponentInChildren<Text>().text = "(G)ive Up";
                    MenuButton.onClick.AddListener(surrender);
                    hotkey = MenuButton.GetComponent<ButtonHotkey>();
                    hotkey.key = KeyCode.G;
                    hotkey.blockers.Add(uiController.popupBlocker);
                }
            }
            else if (mc.currentChaseScene.chasePhase == ChaseScene.ChasePhase.COMPLETE)
            {
                Button MenuButton = Instantiate(p_MenuButton);
                generatedButtons.Add(MenuButton.gameObject);
                Destroy(MenuButton.GetComponent<ContentSizeFitter>());
                MenuButton.transform.SetParent(eventOptionsBox, false);
                MenuButton.GetComponentInChildren<Text>().text = "Continue";
                MenuButton.onClick.AddListener(mc.doNextAction);
                ButtonHotkey hotkey = MenuButton.GetComponent<ButtonHotkey>();
                hotkey.key = KeyCode.Return;
                hotkey.blockers.Add(uiController.popupBlocker);
            }
        }
        else if(mc.currentChaseScene.chaseType == ChaseScene.ChaseType.CAR)
        {
            if (mc.currentChaseScene.chasePhase == ChaseScene.ChasePhase.SELECTION)
            {
                Button MenuButton;
                ButtonHotkey hotkey;

                if (mc.currentChaseScene.obstacle == ChaseScene.ObstacleType.NONE)
                {
                    MenuButton = Instantiate(p_MenuButton);
                    generatedButtons.Add(MenuButton.gameObject);
                    Destroy(MenuButton.GetComponent<ContentSizeFitter>());
                    MenuButton.transform.SetParent(eventOptionsBox, false);
                    MenuButton.GetComponentInChildren<Text>().text = "T(R)y to Lose Them!";
                    MenuButton.onClick.AddListener(driveEscape);
                    hotkey = MenuButton.GetComponent<ButtonHotkey>();
                    hotkey.key = KeyCode.R;
                    hotkey.blockers.Add(uiController.popupBlocker);

                    MenuButton = Instantiate(p_MenuButton);
                    generatedButtons.Add(MenuButton.gameObject);
                    Destroy(MenuButton.GetComponent<ContentSizeFitter>());
                    MenuButton.transform.SetParent(eventOptionsBox, false);
                    MenuButton.GetComponentInChildren<Text>().text = "(F)ight!";
                    MenuButton.onClick.AddListener(fight);
                    hotkey = MenuButton.GetComponent<ButtonHotkey>();
                    hotkey.key = KeyCode.F;
                    hotkey.blockers.Add(uiController.popupBlocker);
                }
                else
                {
                    string obstacleTextRisky = "";
                    string obstacleTextSafe = "";

                    switch (mc.currentChaseScene.obstacle)
                    {
                        case ChaseScene.ObstacleType.FRUITSTAND:
                            obstacleTextRisky = "(R) Swerve to avoid it!";
                            obstacleTextSafe = "(F) Plow through it!";
                            break;
                        case ChaseScene.ObstacleType.TRUCK:
                            obstacleTextRisky = "(R) Speed around it!";
                            obstacleTextSafe = "(F) Slow Down!";
                            break;
                        case ChaseScene.ObstacleType.REDLIGHT:
                            obstacleTextRisky = "(R)un the light!";
                            obstacleTextSafe = "(F) Slow down and turn!";
                            break;
                        case ChaseScene.ObstacleType.CHILD:
                            obstacleTextRisky = "(R) Swere around them!";
                            obstacleTextSafe = "(F) Slam the brakes!";
                            break;
                    }

                    MenuButton = Instantiate(p_MenuButton);
                    generatedButtons.Add(MenuButton.gameObject);
                    Destroy(MenuButton.GetComponent<ContentSizeFitter>());
                    MenuButton.transform.SetParent(eventOptionsBox, false);
                    MenuButton.GetComponentInChildren<Text>().text = obstacleTextRisky;
                    MenuButton.onClick.AddListener(driveObstacleRisky);
                    hotkey = MenuButton.GetComponent<ButtonHotkey>();
                    hotkey.key = KeyCode.R;
                    hotkey.blockers.Add(uiController.popupBlocker);

                    MenuButton = Instantiate(p_MenuButton);
                    generatedButtons.Add(MenuButton.gameObject);
                    Destroy(MenuButton.GetComponent<ContentSizeFitter>());
                    MenuButton.transform.SetParent(eventOptionsBox, false);
                    MenuButton.GetComponentInChildren<Text>().text = obstacleTextSafe;
                    MenuButton.onClick.AddListener(driveObstacleSafe);
                    hotkey = MenuButton.GetComponent<ButtonHotkey>();
                    hotkey.key = KeyCode.F;
                    hotkey.blockers.Add(uiController.popupBlocker);
                }

                MenuButton = Instantiate(p_MenuButton);
                generatedButtons.Add(MenuButton.gameObject);
                Destroy(MenuButton.GetComponent<ContentSizeFitter>());
                MenuButton.transform.SetParent(eventOptionsBox, false);
                MenuButton.GetComponentInChildren<Text>().text = "(B)ail out and run!";
                MenuButton.onClick.AddListener(bail);
                hotkey = MenuButton.GetComponent<ButtonHotkey>();
                hotkey.key = KeyCode.B;
                hotkey.blockers.Add(uiController.popupBlocker);

                if (mc.currentChaseScene.chaserType == LocationDef.EnemyType.POLICE)
                {
                    MenuButton = Instantiate(p_MenuButton);
                    generatedButtons.Add(MenuButton.gameObject);
                    Destroy(MenuButton.GetComponent<ContentSizeFitter>());
                    MenuButton.transform.SetParent(eventOptionsBox, false);
                    MenuButton.GetComponentInChildren<Text>().text = "(G)ive Up";
                    MenuButton.onClick.AddListener(surrender);
                    hotkey = MenuButton.GetComponent<ButtonHotkey>();
                    hotkey.key = KeyCode.G;
                    hotkey.blockers.Add(uiController.popupBlocker);
                }
            }
            else if (mc.currentChaseScene.chasePhase == ChaseScene.ChasePhase.COMPLETE)
            {
                Button MenuButton = Instantiate(p_MenuButton);
                generatedButtons.Add(MenuButton.gameObject);
                Destroy(MenuButton.GetComponent<ContentSizeFitter>());
                MenuButton.transform.SetParent(eventOptionsBox, false);
                MenuButton.GetComponentInChildren<Text>().text = "Continue";
                MenuButton.onClick.AddListener(mc.doNextAction);
            }
        }
    }
}
