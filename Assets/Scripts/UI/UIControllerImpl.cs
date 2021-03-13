using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.UI;
using LCS.Engine.UI.UIEvents;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class UIControllerImpl : MonoBehaviour, UIController {
    public SquadUIImpl squadUIImpl;
    public Squad squadUI { get; set; }

    public EnemyUIImpl enemyUIImpl;
    public Squad enemyUI { get; set; }

    public TitleController titlePageImpl;
    public TitlePage titlePage { get; set; }

    public BaseController baseModeImpl;
    public BaseMode baseMode { get; set; }

    public InfoScreenController charInfoImpl;
    public CharInfo charInfo { get; set; }

    public LiberalManagementBoard organizationManagementImpl;
    public OrganizationManagement organizationManagement { get; set; }

    public SafeHouseView safeHouseManagementImpl;
    public SafeHouseManagement safeHouseManagement { get; set; }

    public SiteScreenController siteModeImpl;
    public SiteMode siteMode { get; set; }

    public TrialController trialImpl;
    public Trial trial { get; set; }

    public ChaseController chaseImpl;
    public Chase chase { get; set; }

    public MapScreenController nationMapImpl;
    public NationMap nationMap { get; set; }

    public ElectionScreenController electionImpl;
    public Election election { get; set; }

    public LawScreenController lawImpl;
    public Law law { get; set; }

    public MeetingScreenImpl meetingImpl;
    public Meeting meeting { get; set; }

    public ShopScreenImpl shopImpl;
    public ShopUI shop { get; set; }

    public HighScoreImpl highScoreImpl;
    public HighScorePage highScore { get; set; }

    public FounderQuestionsImpl founderQuestionsImpl;
    public FounderQuestions founderQuestions { get; set; }

    public NewsScreenController newsImpl;
    public NewsUI news { get; set; }

    public FastAdvanceController fastAdvanceImpl;
    public FastAdvanceUI fastAdvance { get; set; }

    public MartyrScreenImpl martyrScreenImpl;
    public MartyrScreen martyrScreen { get; set; }

    public MessageLog debugLog;

    public PopupMessage popupMessageBox;
    public PopupGuardian popupGuardian;
    public PortraitBuilder portraitBuilder;

    [SerializeField]
    private Tooltip tooltipInstance;
    public static Tooltip tooltip;
    public Text t_ActionList;
    
    private Action anyKeyBind;
    private List<UIBase> currentScreens { get; set; }

    public Color buttonColorOn;
    public Color buttonColorOff;

    public GameObject popupBlocker;

    public bool DebugMode;
    public bool skillRollDebug;
    public bool forceChase;
    public bool actionStep;
    public bool TurboMode;

    private bool keybindSetThisFrame = false;

	// Use this for initialization
	void Start () {
        MasterController.GetMC().uiController = this;
        MasterController.GetMC().DebugMode = DebugMode;
        MasterController.GetMC().SkillRollDebug = skillRollDebug;
        MasterController.GetMC().forceChase = forceChase;
        MasterController.GetMC().actionStep = actionStep;
        currentScreens = new List<UIBase>();
        anyKeyBind = null;

        squadUI = squadUIImpl;
        titlePage = titlePageImpl;
        baseMode = baseModeImpl;
        charInfo = charInfoImpl;
        organizationManagement = organizationManagementImpl;
        safeHouseManagement = safeHouseManagementImpl;
        siteMode = siteModeImpl;
        trial = trialImpl;
        enemyUI = enemyUIImpl;
        chase = chaseImpl;
        nationMap = nationMapImpl;
        election = electionImpl;
        law = lawImpl;
        meeting = meetingImpl;
        shop = shopImpl;
        highScore = highScoreImpl;
        founderQuestions = founderQuestionsImpl;
        news = newsImpl;
        fastAdvance = fastAdvanceImpl;
        martyrScreen = martyrScreenImpl;

        tooltip = tooltipInstance;

        popupMessageBox.dimmer.SetActive(false);
        MasterController.GetMC().startGame();
    }
	
	// Update is called once per frame
	void Update () {
        if (DebugMode) updateActionList();

        if (Input.GetKeyDown(KeyCode.F2))
        {
            debugLog.gameObject.SetActive(!debugLog.gameObject.activeSelf);
        }
        
        if(actionStep && Input.GetKeyDown(KeyCode.F3))
        {
            MasterController.GetMC().doNextAction(false);
        }
    }

    void LateUpdate()
    {
        if (!(popupMessageBox.popupOpen || popupGuardian.popupOpen))
        {
            if (Input.anyKeyDown && anyKeyBind != null && !keybindSetThisFrame)
            {
                doInput(anyKeyBind);
            }
        }

        keybindSetThisFrame = false;
    }

    void OnApplicationQuit()
    {
        GameData.getData().saveToDisk();
    }

    public void generateTranslations()
    {
        foreach(TranslateableField f in FindObjectsOfTypeAll<TranslateableField>())
        {
            f.setTranslation();
        }

        foreach(MouseOverText m in FindObjectsOfTypeAll<MouseOverText>())
        {
            m.setTranslation();
        }
    }

    /** Gets all objects of the named type, whether active or inactive */
    public static List<T> FindObjectsOfTypeAll<T>()
    {
        List<T> results = new List<T>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.isLoaded)
            {
                var allGameObjects = s.GetRootGameObjects();
                for (int j = 0; j < allGameObjects.Length; j++)
                {
                    var go = allGameObjects[j];
                    results.AddRange(go.GetComponentsInChildren<T>(true));
                }
            }
        }
        return results;
    }

    public void ExitGame(bool save = true)
    {
        if(save) GameData.getData().saveGame();
        Application.Quit();
    }

    public void addCurrentScreen(UIBase ui)
    {
        if (!currentScreens.Contains(ui))
            currentScreens.Add(ui);
    }

    public void removeCurrentScreen(UIBase ui)
    {
        currentScreens.Remove(ui);
    }

    public bool isScreenActive(UIBase ui)
    {
        return currentScreens.Contains(ui);
    }

    public void closeUI()
    {
        List<UIBase> currentScreensTemp = new List<UIBase>(currentScreens);
        foreach (UIBase ui in currentScreensTemp)
        {
            ui.close();
        }
    }

    public void hideUI()
    {
        foreach (UIBase ui in currentScreens)
        {
            ui.hide();
        }
    }

    public void showUI()
    {
        foreach (UIBase ui in currentScreens)
        {
            ui.show();
        }
    }

    public void refreshUI()
    {
        foreach (UIBase ui in currentScreens)
        {
            ui.refresh();
        }
    }

    public void updateDebugLog()
    {
        debugLog.updateMessageLog();
    }

    public void RegisterAnyKeyBind(Action action)
    {
        anyKeyBind = action;
        keybindSetThisFrame = true;
    }

    public void ClearAnyKeyBind()
    {
        anyKeyBind = null;
    }

    public void showPopup(string text, Action clickAction)
    {
        popupMessageBox.doPopup(text, clickAction);
    }

    public void showOptionPopup(string text, List<PopupOption> options)
    {
        popupMessageBox.doOptionPopup(text, options);
    }

    public void showYesNoPopup(string text, List<PopupOption> options)
    {
        popupMessageBox.doYesNoPopup(text, options);
    }

    public void showGuardianPopup(List<Entity> items)
    {
        popupGuardian.doPopup(items);
    }

    public void NewGame()
    {
        MasterController mc = MasterController.GetMC();
        mc.DebugMode = DebugMode;
        mc.SkillRollDebug = skillRollDebug;
    }

    public void doInput(Action action)
    {
        if(action != null) action();
        foreach(UIBase screen in currentScreens)
        {
            screen.refresh();
        }
    }

    public void GameOver()
    {
        safeHouseManagementImpl.selectedBase = null;
    }

    public void toggleTurboMode()
    {
        TurboMode = !TurboMode;
    }

    private void updateActionList()
    {
        t_ActionList.text = "";

        updateActionList(MasterController.GetMC().actionQueue);
    }

    private void updateActionList(ActionQueue action, int depth = 0)
    {
        string text = "";

        for(int i = 0; i < depth; i++)
        {
            text += "  ";
        }
        text += action.description;
        t_ActionList.text += text + "\n";

        if (action.Count == 0) return;
        else
        {
            foreach(ActionQueue subAction in action)
            {
                updateActionList(subAction, depth + 1);
            }
        }
    }

    //Events
    public event EventHandler<Speak> speak;
    public void doSpeak(Speak args)
    {
        if(speak != null)
            speak(this, args);
    }

    // //////////////////////////////////////// RANDOM USEFUL UTILITY FUNCTIONS FOR UI STUFF ///////////////////////////////// //    

    public static string breakLines(string inputText, int lineLength)
    {
        string text = inputText;
        int stringPos = 0;

        while(stringPos + lineLength < text.Length)
        {
            string section = text.Substring(stringPos, lineLength);
            if (section.Contains("\n"))
            {
                stringPos += section.LastIndexOf("\n") + 1;
            }
            else if (!section.Contains(" "))
            {
                if (stringPos > 0 && text[stringPos - 1] != '\n') text = text.Insert(stringPos, "-\n");
                stringPos += lineLength;
            }
            else
            {
                stringPos += section.LastIndexOf(' ');
                text = text.Remove(stringPos, 1).Insert(stringPos, "\n");
            }
        }

        return text;
    }

    public static string dimColors(string input)
    {
        return input.Replace("<color=cyan>", "<color=teal>")
            .Replace("<color=yellow>", "<color=olive>")
            .Replace("<color=red>", "<color=maroon>")
            .Replace("<color=white>", "<color=grey>")
            .Replace("<color=magenta>", "<color=purple>")
            .Replace("<color=lime>", "<color=green>");
    }

    public static string getAlignmentColor(Alignment alignment)
    {
        switch (alignment)
        {
            case Alignment.ARCHCONSERVATIVE:
                return "<color=red>";
            case Alignment.CONSERVATIVE:
                return "<color=magenta>";
            case Alignment.MODERATE:
                return "<color=yellow>";
            case Alignment.LIBERAL:
                return "<color=cyan>";
            case Alignment.ELITE_LIBERAL:
                return "<color=lime>";
        }

        return "<color=white>";
    }

    public static Color colorConversion(ConsoleColor color)
    {
        switch (color)
        {
            case ConsoleColor.White:
                return Color.white;
            case ConsoleColor.Black:
                return Color.black;
            case ConsoleColor.Blue:
                return Color.blue;
            case ConsoleColor.Cyan:
                return Color.cyan;
            case ConsoleColor.Gray:
                return Color.gray;
            case ConsoleColor.Green:
                return Color.green;
            case ConsoleColor.Magenta:
                return Color.magenta;
            case ConsoleColor.Red:
                return Color.red;
            case ConsoleColor.Yellow:
                return Color.yellow;
            default:
                return Color.white;
        }
    }
}
