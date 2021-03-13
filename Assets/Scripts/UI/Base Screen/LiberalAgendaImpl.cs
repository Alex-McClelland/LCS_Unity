using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.UI;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.World;


public class LiberalAgendaImpl : MonoBehaviour, LiberalAgenda {

    public UIControllerImpl uiController;

    public GameObject p_SupremeCourtName;

    public Text t_ExecutiveTitles;
    public Text t_ExecutiveNames;
    public Text t_Congress;
    public Text t_SupremeCourtLabel;
    public GameObject SupremeCourtContainer;
    public List<GameObject> t_SupremeCourtNames;

    public Transform leftColumn;
    public Transform centerColumn;
    public Transform rightColumn;
    public LawAlignmentDisplay p_LawDisplay;

    public Button b_ViewPolls;
    public PollDisplay pollDisplay;
    private bool pollsActive = false;

    private Dictionary<string, LawAlignmentDisplay> laws;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void show()
    {
        uiController.addCurrentScreen(this);

        if (laws == null) laws = new Dictionary<string, LawAlignmentDisplay>();

        fillLaws();
        refresh();

        gameObject.SetActive(true);
    }

    public void refresh()
    {
        MasterController mc = MasterController.GetMC();

        t_ExecutiveTitles.text = "";
        string presidentColor = getAlignmentColor(MasterController.government.president.getComponent<Politician>().alignment);
        t_ExecutiveTitles.text += presidentColor + mc.getTranslation("GOVERNMENT_president") + " (" + MasterController.ordinal(MasterController.government.presidentTerm) + " " + mc.getTranslation("BASE_term") +"):</color>\n";
        string vpColor = getAlignmentColor(MasterController.government.vicePresident.getComponent<Politician>().alignment);
        t_ExecutiveTitles.text += vpColor + mc.getTranslation("GOVERNMENT_vice_president") + ":</color>\n";
        string sosColor = getAlignmentColor(MasterController.government.secretaryOfState.getComponent<Politician>().alignment);
        t_ExecutiveTitles.text += sosColor + mc.getTranslation("GOVERNMENT_secretary_of_state") + ":</color>\n";
        string agColor = getAlignmentColor(MasterController.government.attorneyGeneral.getComponent<Politician>().alignment);
        t_ExecutiveTitles.text += agColor + mc.getTranslation("GOVERNMENT_attorney_general") + ":</color>";

        t_ExecutiveNames.text = "";
        t_ExecutiveNames.text += presidentColor + MasterController.government.president.getComponent<CreatureInfo>().getName() + "</color>\n";
        t_ExecutiveNames.text += vpColor + MasterController.government.vicePresident.getComponent<CreatureInfo>().getName() + "</color>\n";
        t_ExecutiveNames.text += sosColor + MasterController.government.secretaryOfState.getComponent<CreatureInfo>().getName() + "</color>\n";
        t_ExecutiveNames.text += agColor + MasterController.government.attorneyGeneral.getComponent<CreatureInfo>().getName() + "</color>";

        int[] supremeCourtAlignments = new int[5];

        for(int i=0;i<MasterController.government.supremeCourt.Count;i++)
        {
            Entity e = MasterController.government.supremeCourt[i];
            if(t_SupremeCourtNames.Count < i + 1)
            {
                GameObject g = Instantiate(p_SupremeCourtName, SupremeCourtContainer.transform, false);
                t_SupremeCourtNames.Add(g);
            }

            string justiceColor = getAlignmentColor(e.getComponent<Politician>().alignment);
            Text t_SupremeCourtName = t_SupremeCourtNames[i].GetComponent<Text>();
            MouseOverText t_SupremeCourtMouseOver = t_SupremeCourtNames[i].GetComponent<MouseOverText>();
            t_SupremeCourtName.text = justiceColor + e.getComponent<CreatureInfo>().getName(true) + "</color>";
            t_SupremeCourtMouseOver.mouseOverText = "Age: " + e.getComponent<Age>().getAge() + "\n" + "Health: " + e.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].Level;

            switch (e.getComponent<Politician>().alignment)
            {
                case Alignment.ARCHCONSERVATIVE:
                    supremeCourtAlignments[0]++;
                    break;
                case Alignment.CONSERVATIVE:
                    supremeCourtAlignments[1]++;
                    break;
                case Alignment.MODERATE:
                    supremeCourtAlignments[2]++;
                    break;
                case Alignment.LIBERAL:
                    supremeCourtAlignments[3]++;
                    break;
                case Alignment.ELITE_LIBERAL:
                    supremeCourtAlignments[4]++;
                    break;
            }
        }

        Color supremeCourtColor;
        if (supremeCourtAlignments[0] > MasterController.government.supremeCourt.Count / 2) supremeCourtColor = Color.red;
        else if (supremeCourtAlignments[4] > MasterController.government.supremeCourt.Count / 2) supremeCourtColor = Color.green;
        else if (supremeCourtAlignments[0] + supremeCourtAlignments[1] > MasterController.government.supremeCourt.Count / 2) supremeCourtColor = Color.magenta;
        else if (supremeCourtAlignments[4] + supremeCourtAlignments[3] > MasterController.government.supremeCourt.Count / 2) supremeCourtColor = Color.cyan;
        else supremeCourtColor = Color.yellow;

        t_SupremeCourtLabel.color = supremeCourtColor;

        int[] houseAlignments = new int[5];

        foreach (string state in MasterController.government.house.Keys)
        {
            foreach (Alignment a in MasterController.government.house[state])
            {
                switch (a)
                {
                    case Alignment.ARCHCONSERVATIVE:
                        houseAlignments[0]++;
                        break;
                    case Alignment.CONSERVATIVE:
                        houseAlignments[1]++;
                        break;
                    case Alignment.MODERATE:
                        houseAlignments[2]++;
                        break;
                    case Alignment.LIBERAL:
                        houseAlignments[3]++;
                        break;
                    case Alignment.ELITE_LIBERAL:
                        houseAlignments[4]++;
                        break;
                }
            }
        }

        string majorityColor = "";
        int houseCount = MasterController.government.houseNum;

        if (houseAlignments[0] > houseCount / 2) majorityColor = "<color=red>";
        else if (houseAlignments[4] > houseCount / 2) majorityColor = "<color=lime>";
        else if (houseAlignments[0] + houseAlignments[1] > houseCount / 2) majorityColor = "<color=magenta>";
        else if (houseAlignments[4] + houseAlignments[3] > houseCount / 2) majorityColor = "<color=cyan>";
        else majorityColor = "<color=yellow>";

        if (MasterController.government.houseWinCheck()) majorityColor = "<b>" + majorityColor;

        /* Alternate coloring method used in LCS 4.07.3 Release
        int lsum = houseAlignments[3] + houseAlignments[4] - houseAlignments[0] - houseAlignments[1];
        if (lsum <= -houseCount/3) majorityColor = "<color=red>";
        else if (lsum < 0) majorityColor = "<color=magenta>";
        else if (lsum < houseCount/3) majorityColor = "<color=yellow>";
        else if (houseAlignments[4] < houseCount*(2f/3f)) majorityColor = "<color=cyan>";
        else majorityColor = "<color=lime>";*/

        t_Congress.text = "";
        t_Congress.text += majorityColor + mc.getTranslation("GOVERNMENT_house") + ":</color> ";
        if (MasterController.government.houseWinCheck()) t_Congress.text += "</b>";
        t_Congress.text +=" <color=red>" + houseAlignments[0] + "</color> / ";
        t_Congress.text += "<color=magenta>" + houseAlignments[1] + "</color> / ";
        t_Congress.text += "<color=yellow>" + houseAlignments[2] + "</color> / ";
        t_Congress.text += "<color=cyan>" + houseAlignments[3] + "</color> / ";
        t_Congress.text += "<color=lime>" + houseAlignments[4] + "</color>";

        int[] senateAlignments = new int[5];

        foreach (string state in MasterController.government.senate.Keys)
        {
            foreach (Alignment a in MasterController.government.senate[state])
            {
                switch (a)
                {
                    case Alignment.ARCHCONSERVATIVE:
                        senateAlignments[0]++;
                        break;
                    case Alignment.CONSERVATIVE:
                        senateAlignments[1]++;
                        break;
                    case Alignment.MODERATE:
                        senateAlignments[2]++;
                        break;
                    case Alignment.LIBERAL:
                        senateAlignments[3]++;
                        break;
                    case Alignment.ELITE_LIBERAL:
                        senateAlignments[4]++;
                        break;
                }
            }
        }

        majorityColor = "";
        int senateCount = MasterController.government.senateNum;

        if (senateAlignments[0] > senateCount / 2) majorityColor = "<color=red>";
        else if (senateAlignments[4] > senateCount / 2) majorityColor = "<color=lime>";
        else if (senateAlignments[0] + senateAlignments[1] > senateCount / 2) majorityColor = "<color=magenta>";
        else if (senateAlignments[4] + senateAlignments[3] > senateCount / 2) majorityColor = "<color=cyan>";
        else majorityColor = "<color=yellow>";

        if (MasterController.government.senateWinCheck()) majorityColor = "<b>" + majorityColor;
        /* Alternate coloring method used in LCS 4.07.3 Release
        lsum = senateAlignments[3] + senateAlignments[4] - senateAlignments[0] - senateAlignments[1];
        if (lsum <= -senateCount / 3) majorityColor = "<color=red>";
        else if (lsum < 0) majorityColor = "<color=magenta>";
        else if (lsum < senateCount / 3) majorityColor = "<color=yellow>";
        else if (senateAlignments[4] < senateCount * (2f / 3f)) majorityColor = "<color=cyan>";
        else majorityColor = "<color=lime>";*/

        t_Congress.text += "\n";
        t_Congress.text += majorityColor + mc.getTranslation("GOVERNMENT_senate") + ":</color>";
        if (MasterController.government.senateWinCheck()) t_Congress.text += "</b>";
        t_Congress.text += " <color=red>" + senateAlignments[0] + "</color> / ";
        t_Congress.text += "<color=magenta>" + senateAlignments[1] + "</color> / ";
        t_Congress.text += "<color=yellow>" + senateAlignments[2] + "</color> / ";
        t_Congress.text += "<color=cyan>" + senateAlignments[3] + "</color> / ";
        t_Congress.text += "<color=lime>" + senateAlignments[4] + "</color>";

        if (pollsActive) pollDisplay.Activate();

        foreach (string s in MasterController.government.laws.Keys)
        {
            laws[s].setLaw(s, MasterController.government.laws[s].alignment);
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

    public void togglePolls()
    {
        if (pollsActive)
        {
            b_ViewPolls.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("BASE_view_polls");
            pollDisplay.gameObject.SetActive(false);
            pollsActive = false;
        }
        else
        {
            b_ViewPolls.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("BASE_view_laws");
            pollDisplay.Activate();
            pollDisplay.gameObject.SetActive(true);
            pollsActive = true;
        }
    }

    public void viewMap()
    {
        uiController.hideUI();
        uiController.nationMap.showDemographics();
    }

    private void fillLaws()
    {
        int i = 0;

        foreach(string s in MasterController.government.laws.Keys)
        {
            LawAlignmentDisplay lawDisplay;

            if (i < MasterController.government.laws.Count/3)
            {
                if (!laws.ContainsKey(s))
                {
                    lawDisplay = Instantiate(p_LawDisplay);
                    laws.Add(s, lawDisplay);
                    lawDisplay.transform.SetParent(leftColumn, false);
                }
                else
                {
                    lawDisplay = laws[s];
                }
            }
            else if(i < (MasterController.government.laws.Count / 3) * 2)
            {
                if (!laws.ContainsKey(s))
                {
                    lawDisplay = Instantiate(p_LawDisplay);
                    laws.Add(s, lawDisplay);
                    lawDisplay.transform.SetParent(centerColumn, false);
                }
                else
                {
                    lawDisplay = laws[s];
                }
            } else
            {
                if (!laws.ContainsKey(s))
                {
                    lawDisplay = Instantiate(p_LawDisplay);
                    laws.Add(s, lawDisplay);
                    lawDisplay.transform.SetParent(rightColumn, false);
                }
                else
                {
                    lawDisplay = laws[s];
                }
            }

            lawDisplay.setLaw(s, MasterController.government.laws[s].alignment);

            i++;
        }
    }

    private string getAlignmentColor(Alignment alignment)
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

        return "";
    }
}
