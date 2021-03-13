using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine.UI;
using LCS.Engine;

public class PopupMessage : MonoBehaviour {

    public UIControllerImpl uiController;

    public Text text;
    public Button option1;
    private Action action1;
    public Button option2;
    private Action action2;
    public Button option3;
    private Action action3;
    public GameObject dimmer;

    public bool popupOpen;
    private bool optionPopup;
    private bool yesNoPopup;

    private Action clickAction;
    private List<string> fullText;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (popupOpen)
        {
            if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
            {
                uiController.doInput(closeWindow);
            }

            if (fullText.Count == 0)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) && action1 != null)
                {
                    uiController.doInput(() =>
                    {
                        optionPopup = false;
                        closeWindow();
                        action1();
                    });
                }

                if (Input.GetKeyDown(KeyCode.Alpha2) && action2 != null)
                {
                    uiController.doInput(() =>
                    {
                        optionPopup = false;
                        closeWindow();
                        action2();
                    });
                }

                if (Input.GetKeyDown(KeyCode.Alpha3) && action3 != null)
                {
                    uiController.doInput(() =>
                    {
                        optionPopup = false;
                        closeWindow();
                        action3();
                    });
                }

                if (yesNoPopup)
                {
                    if (Input.GetKeyDown(KeyCode.Y) && action1 != null)
                    {
                        uiController.doInput(() =>
                        {                            
                            optionPopup = false;
                            yesNoPopup = false;
                            closeWindow();
                            action1();
                        });
                    }

                    if (Input.GetKeyDown(KeyCode.N) && action2 != null)
                    {
                        uiController.doInput(() =>
                        {
                            optionPopup = false;
                            yesNoPopup = false;
                            closeWindow();
                            action2();
                        });
                    }
                }
            }
        }
	}

    public void doPopup(string text, Action clickAction = null)
    {
        dimmer.SetActive(true);
        popupOpen = true;
        optionPopup = false;
        gameObject.SetActive(true);

        option1.gameObject.SetActive(false);
        option2.gameObject.SetActive(false);
        option3.gameObject.SetActive(false);

        fullText = new List<string>(text.Split('\n'));

        this.text.text = fullText[0];
        fullText.RemoveAt(0);
        this.clickAction = clickAction;
    }

    public void doYesNoPopup(string text, List<PopupOption> options)
    {
        dimmer.SetActive(true);
        popupOpen = true;
        optionPopup = true;
        yesNoPopup = true;
        gameObject.SetActive(true);

        action1 = null;
        action2 = null;
        action3 = null;

        if (options.Count > 0)
            action1 = options[0].action;
        if (options.Count > 1)
            action2 = options[1].action;

        option1.onClick.RemoveAllListeners();
        option2.onClick.RemoveAllListeners();
        option3.onClick.RemoveAllListeners();
        option1.gameObject.SetActive(false);
        option2.gameObject.SetActive(false);
        option3.gameObject.SetActive(false);

        if (action1 != null)
        {
            option1.gameObject.SetActive(true);
            option1.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("OPTION_yes_hotkey");
            option1.onClick.AddListener(() => { uiController.doInput(() => {  optionPopup = false; yesNoPopup = false; closeWindow(); action1(); }); });
        }
        if (action2 != null)
        {
            option2.gameObject.SetActive(true);
            option2.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("OPTION_no_hotkey");
            option2.onClick.AddListener(() => { uiController.doInput(() => { optionPopup = false; yesNoPopup = false; closeWindow(); action2(); }); });
        }

        fullText = new List<string>(text.Split('\n'));

        this.text.text = fullText[0];
        fullText.RemoveAt(0);
        this.clickAction = null;

        if (fullText.Count > 0)
            hideButtons();
    }

    public void doOptionPopup(string text, List<PopupOption> options)
    {
        dimmer.SetActive(true);
        popupOpen = true;
        optionPopup = true;
        gameObject.SetActive(true);

        action1 = null;
        action2 = null;
        action3 = null;

        if (options.Count > 0)
            action1 = options[0].action;
        if(options.Count > 1)
            action2 = options[1].action;
        if(options.Count > 2)
            action3 = options[2].action;

        option1.onClick.RemoveAllListeners();
        option2.onClick.RemoveAllListeners();
        option3.onClick.RemoveAllListeners();
        option1.gameObject.SetActive(false);
        option2.gameObject.SetActive(false);
        option3.gameObject.SetActive(false);

        if (action1 != null)
        {
            option1.gameObject.SetActive(true);
            option1.GetComponentInChildren<Text>().text = options[0].text;
            option1.onClick.AddListener(() => { uiController.doInput(() => { optionPopup = false; closeWindow(); action1(); }); });
        }
        if (action2 != null)
        {
            option2.gameObject.SetActive(true);
            option2.GetComponentInChildren<Text>().text = options[1].text;
            option2.onClick.AddListener(() => { uiController.doInput(() => {  optionPopup = false; closeWindow(); action2(); }); });
        }
        if (action3 != null)
        {
            option3.gameObject.SetActive(true);
            option3.GetComponentInChildren<Text>().text = options[2].text;
            option3.onClick.AddListener(() => { uiController.doInput(() => { optionPopup = false; closeWindow(); action3(); }); });
        }

        fullText = new List<string>(text.Split('\n'));

        this.text.text = fullText[0];
        fullText.RemoveAt(0);
        this.clickAction = null;

        if (fullText.Count > 0)
            hideButtons();
    }

    public void closeWindowButton()
    {
        uiController.doInput(closeWindow);
    }

    private void closeWindow()
    {
        if (fullText.Count > 0)
        {
            text.text += "\n" + fullText[0];
            fullText.RemoveAt(0);

            if (optionPopup && fullText.Count == 0)
                showButtons();
        }
        else
        {
            if (!optionPopup)
            {
                popupOpen = false;
                gameObject.SetActive(false);
                dimmer.SetActive(false);                

                if (clickAction != null)
                {
                    //This MUST always be the last thing in this method.
                    clickAction();
                }
            }
        }
    }

    private void hideButtons()
    {
        option1.gameObject.SetActive(false);
        option2.gameObject.SetActive(false);
        option3.gameObject.SetActive(false);
    }

    private void showButtons()
    {
        if (action1 != null)
        {
            option1.gameObject.SetActive(true);
        }
        if (action2 != null)
        {
            option2.gameObject.SetActive(true);
        }
        if (action3 != null)
        {
            option3.gameObject.SetActive(true);
        }
    }
}
