using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.UI;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Creature;

public class LiberalManagementBoard : MonoBehaviour, OrganizationManagement {

    public UIControllerImpl uiController;

    public GameObject expandedToggle;

    public OrgPortrait p_OrgPortrait;
    public GameObject p_String;
    public GameObject p_Pin;
    public GameObject p_Note;

    public Transform portraitHolder;
    public Transform noteHolder;
    public Transform stringHolder;
    public Transform pinHolder;
    public Transform content;

    public Button expando;
    public GameObject innerBorders;

    public Button b_QuickActivityMenu;
    public GameObject quickActivityMenu;
    public bool activityMenuActive = false;
    public string selectMode = "NORMAL";
    public Button b_Activism;
    public Button b_LegalFundraising;
    public Button b_IllegalFundraising;
    public Button b_CheckPolls;
    public Button b_StealCars;
    public Button b_CommunityService;
    public Button b_LayLow;

    public Color buttonSelected;
    public Color buttonUnselected;
    public Color buttonIllegalSelected;
    public Color buttonIllegalUnselected;

    public bool expanded = false;
    public GameObject squadActivityButtons;

    private OrganizationManagementActions actions;

    private Vector3 originalPosition;
    private Vector2 originalSize;

    private Dictionary<Entity, OrgPortrait> lcsMembers;

    // Use this for initialization
    void Start () {

    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void init(OrganizationManagementActions actions)
    {
        this.actions = actions;
    }

    public void show()
    {
        if (lcsMembers == null) lcsMembers = new Dictionary<Entity, OrgPortrait>();

        uiController.addCurrentScreen(this);

        cleanList();

        quickActivityMenu.SetActive(false);
        b_QuickActivityMenu.GetComponent<Image>().color = buttonUnselected;
        activityMenuActive = false;
        setSelectMode("NORMAL");

        populateBoard();
        foreach(OrgPortrait op in lcsMembers.Values)
        {
            op.i_Portrait.buildPortrait(op.character);
            op.setActivityIcon();
        }

        gameObject.SetActive(true);
    }

    public void refresh()
    {
        cleanList();
        populateBoard();
        foreach (OrgPortrait op in lcsMembers.Values)
        {
            op.i_Portrait.buildPortrait(op.character);
            op.setActivityIcon();
        }
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

    public void Expando()
    {
        if (!expanded)
        {
            RectTransform rect = GetComponent<RectTransform>();

            originalPosition = rect.localPosition;
            originalSize = rect.sizeDelta;

            rect.localPosition = new Vector3(0, 0);
            rect.sizeDelta = new Vector2(10, 10);

            innerBorders.SetActive(false);
            squadActivityButtons.SetActive(false);
            expando.GetComponentInChildren<Text>().text = "-";
            expanded = true;
            expandedToggle.SetActive(true);
        }
        else
        {
            RectTransform rect = GetComponent<RectTransform>();

            rect.localPosition = originalPosition;
            rect.sizeDelta = originalSize;

            innerBorders.SetActive(true);
            squadActivityButtons.SetActive(true);
            expando.GetComponentInChildren<Text>().text = "+";
            expanded = false;
            expandedToggle.SetActive(false);
        }
    }

    public void select(Entity character)
    {
        if (selectMode == "NORMAL")
        {
            actions.selectChar(character);
        }
        else
        {
            actions.quickActivity(character, selectMode);
        }
    }

    public void setSelectMode(string mode)
    {
        if (mode == selectMode) selectMode = "NORMAL";
        else selectMode = mode;

        switch (selectMode)
        {
            case "ACTIVISM":
                unselectButtons();
                b_Activism.GetComponent<Image>().color = buttonSelected;
                break;
            case "LEGAL_FUNDRAISING":
                unselectButtons();
                b_LegalFundraising.GetComponent<Image>().color = buttonSelected;
                break;
            case "ILLEGAL_FUNDRAISING":
                unselectButtons();
                b_IllegalFundraising.GetComponent<Image>().color = buttonIllegalSelected;
                break;
            case "CHECK_POLLS":
                unselectButtons();
                b_CheckPolls.GetComponent<Image>().color = buttonSelected;
                break;
            case "STEAL_CARS":
                unselectButtons();
                b_StealCars.GetComponent<Image>().color = buttonIllegalSelected;
                break;
            case "COMMUNITY_SERVICE":
                unselectButtons();
                b_CommunityService.GetComponent<Image>().color = buttonSelected;
                break;
            case "NONE":
                unselectButtons();
                b_LayLow.GetComponent<Image>().color = buttonSelected;
                break;
            default:
                unselectButtons();
                break;
        }
    }

    private void unselectButtons()
    {
        b_Activism.GetComponent<Image>().color = buttonUnselected;
        b_LegalFundraising.GetComponent<Image>().color = buttonUnselected;
        b_IllegalFundraising.GetComponent<Image>().color = buttonIllegalUnselected;
        b_CheckPolls.GetComponent<Image>().color = buttonUnselected;
        b_StealCars.GetComponent<Image>().color = buttonIllegalUnselected;
        b_CommunityService.GetComponent<Image>().color = buttonUnselected;
        b_LayLow.GetComponent<Image>().color = buttonUnselected;
    }

    public void toggleActivityMenu()
    {
        if (activityMenuActive)
        {
            b_QuickActivityMenu.GetComponent<Image>().color = buttonUnselected;
            setSelectMode("NORMAL");
            quickActivityMenu.SetActive(false);
            activityMenuActive = false;
        }
        else
        {
            b_QuickActivityMenu.GetComponent<Image>().color = buttonSelected;
            quickActivityMenu.SetActive(true);
            activityMenuActive = true;
        }
    }

    private void populateBoard()
    {
        parseMembers(MasterController.lcs.founder, 0);

        foreach (Entity e in lcsMembers.Keys)
        {
            lcsMembers[e].t_Name.text = getLiberalName(e);
        }
    }

    private void parseMembers(Entity e, int childNum)
    {
        if (!lcsMembers.ContainsKey(e))
        {
            OrgPortrait op = Instantiate(p_OrgPortrait);
            GameObject pin = Instantiate(p_Pin);
            pin.transform.SetParent(pinHolder, false);
            op.liberalManagementBoard = this;
            op.transform.SetParent(portraitHolder, false);
            op.character = e;

            lcsMembers.Add(e, op);

            if (e.getComponent<Liberal>().leader != null)
            {
                GameObject line = Instantiate(p_String);
                line.transform.SetParent(stringHolder, false);

                op.addParent(lcsMembers[e.getComponent<Liberal>().leader], line.GetComponent<RectTransform>(), pin.GetComponent<RectTransform>());

                if (e.getComponent<Liberal>().managerPosX == 0 && e.getComponent<Liberal>().managerPosY == 0)
                {
                    e.getComponent<Liberal>().managerPosX = op.parent.transform.localPosition.x + childNum * 120;
                    e.getComponent<Liberal>().managerPosY = op.parent.transform.localPosition.y - 140;
                }
                op.transform.localPosition = new Vector3(e.getComponent<Liberal>().managerPosX, e.getComponent<Liberal>().managerPosY);

                float maxWidth = (op.transform.parent.GetComponent<RectTransform>().rect.width / 2) - (op.GetComponent<RectTransform>().rect.width / 2);
                float maxHeight = (op.transform.parent.GetComponent<RectTransform>().rect.height / 2) - (op.GetComponent<RectTransform>().rect.height / 2);

                if (op.transform.localPosition.x > maxWidth) op.transform.localPosition = new Vector3(maxWidth, op.transform.localPosition.y);
                if (op.transform.localPosition.x < -maxWidth) op.transform.localPosition = new Vector3(-maxWidth, op.transform.localPosition.y);
                if (op.transform.localPosition.y > maxHeight) op.transform.localPosition = new Vector3(op.transform.localPosition.x, maxHeight);
                if (op.transform.localPosition.y < -maxHeight) op.transform.localPosition = new Vector3(op.transform.localPosition.x, -maxHeight);

                op.transform.localScale = new Vector3(1, 1, 1);
            }
            else
            {
                op.pin = pin.GetComponent<RectTransform>();
                if (e.getComponent<Liberal>().managerPosX == 0f && e.getComponent<Liberal>().managerPosY == 0f)
                {
                    op.transform.localPosition = new Vector3(0, 122);
                    e.getComponent<Liberal>().managerPosX = 0;
                    e.getComponent<Liberal>().managerPosY = 122;
                }

                op.transform.localPosition = new Vector3(e.getComponent<Liberal>().managerPosX, e.getComponent<Liberal>().managerPosY);
                op.transform.localScale = new Vector3(1.2f, 1.2f, 1);
            }
        }
        else
        {
            OrgPortrait op = lcsMembers[e];

            if (e.getComponent<Liberal>().leader != null)
            {
                GameObject line = op.line.gameObject;

                op.setParent(lcsMembers[e.getComponent<Liberal>().leader]);

                op.transform.localScale = new Vector3(1, 1, 1);
            }
            else
            {
                //If they have just become the leader they should lose their string - they can't become subordinate again anyway
                if (op.line != null)
                {
                    Destroy(op.line.gameObject);
                    op.line = null;
                    op.parent = null;
                }

                op.transform.localScale = new Vector3(1.2f, 1.2f, 1);
            }
        }

        int i = 0 - (e.getComponent<Liberal>().subordinates.Count / 2);
        foreach (Entity lib in e.getComponent<Liberal>().subordinates)
        {
            parseMembers(lib, i);
            i++;
        }
    }

    private void cleanList()
    {
        List<Entity> fullList = new List<Entity>(lcsMembers.Keys);

        foreach (Entity e in fullList)
        {
            if (!MasterController.lcs.getAllMembers().Contains(e))
            {
                if(lcsMembers[e].line != null)
                    Destroy(lcsMembers[e].line.gameObject);
                Destroy(lcsMembers[e].pin.gameObject);
                Destroy(lcsMembers[e].gameObject);
                lcsMembers.Remove(e);
            }
        }
    }

    private string getLiberalName(Entity e)
    {
        string name;

        if (e.getComponent<CreatureInfo>().alias != "")
        {
            name = "\"" + e.getComponent<CreatureInfo>().alias + "\"";
        }
        else
        {
            name = e.getComponent<CreatureInfo>().givenName + " " + e.getComponent<CreatureInfo>().surname;
        }

        return name;
    }
}
