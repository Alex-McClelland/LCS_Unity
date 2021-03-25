using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.UI;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Creature;
using LCS.Engine.Containers;

public class LawScreenController : MonoBehaviour, Law {

    public enum Mode
    {
        PROP,
        SUPREME_COURT,
        CONGRESS
    }

    public UIControllerImpl uiController;

    public Button b_TurboMode;

    public Text t_Title;
    public Transform lawContainer;
    public Button b_ShowResults;

    private int currentItem;

    public Proposition p_Prop;
    private List<int> propResults;    
    private int propYesVotes;
    private int propNoVotes;

    public Bill p_Bill;
    private List<CongressBillResult> billResults;
    private int houseYesVotes;
    private int houseNoVotes;
    private int senateYesVotes;
    private int senateNoVotes;
    private bool billPassedCongress;

    public Mode mode;

    public float shortDelay;
    public float midDelay;
    public float longDelay;

    private List<GameObject> laws;
    private Dictionary<bool, int> propWeights;
    private Dictionary<bool, int> houseWeights;
    private Dictionary<bool, int> senateWeights;
    private float timer;
    private bool voteStarted;
    private bool voteFinished;
    private bool presidentStarted;
    private bool presidentFinished;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (voteStarted)
        {
            timer -= Time.deltaTime;

            if(timer <= 0)
            {
                timer = shortDelay;

                switch (mode)
                {
                    case Mode.PROP:
                        doPropUpdate();
                        break;
                    case Mode.CONGRESS:
                        doCongressUpdate();
                        break;
                    case Mode.SUPREME_COURT:
                        doSupremeCourtUpdate();
                        timer = midDelay;
                        break;
                }
            }
        }

        if (presidentStarted)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                timer = longDelay;
                doPresidentSignature();
            }
        }
	}

    private void doSupremeCourtUpdate()
    {
        if (!uiController.TurboMode)
        {
            if (MasterController.GetMC().WeightedRandom(propWeights))
            {
                propYesVotes++;
                propWeights[true]--;
            }
            else
            {
                propNoVotes++;
                propWeights[false]--;
            }
        }
        else
        {
            propYesVotes += propWeights[true];
            propNoVotes += propWeights[false];
        }

        string yesColor = "<color=grey>";
        string noColor = "<color=grey>";

        if (propYesVotes > propNoVotes) yesColor = "<color=white>";
        else noColor = "<color=white>";

        laws[currentItem].GetComponent<Proposition>().t_Support.text = yesColor + MasterController.GetMC().getTranslation("LAW_supreme_court_change").Replace("$COUNT", propYesVotes.ToString()) + "</color>\n" 
            + noColor + MasterController.GetMC().getTranslation("LAW_supreme_court_status_quo").Replace("$COUNT", propNoVotes.ToString()) + "</color>";

        if (propYesVotes + propNoVotes == MasterController.government.supremeCourt.Count)
        {            
            loadNextCaseResult();
        }
    }

    private void doCongressUpdate()
    {
        if (!uiController.TurboMode)
        {
            //House
            if (houseYesVotes + houseNoVotes < MasterController.government.houseNum)
            {
                if (MasterController.GetMC().WeightedRandom(houseWeights))
                {
                    houseYesVotes++;
                    houseWeights[true]--;
                }
                else
                {
                    houseNoVotes++;
                    houseWeights[false]--;
                }
            }

            //Senate
            if (MasterController.GetMC().LCSRandom(4) == 0 && senateYesVotes + senateNoVotes < MasterController.government.senateNum)
            {
                if (MasterController.GetMC().WeightedRandom(senateWeights))
                {
                    senateYesVotes++;
                    senateWeights[true]--;
                }
                else
                {
                    senateNoVotes++;
                    senateWeights[false]--;
                }
            }
        }
        else
        {
            houseYesVotes += houseWeights[true];
            houseNoVotes += houseWeights[false];

            senateYesVotes += senateWeights[true];
            senateNoVotes += senateWeights[false];
        }

        string yesColor = "<color=grey>";
        string noColor = "<color=grey>";
        if (houseYesVotes > houseNoVotes) yesColor = "<color=white>";
        else noColor = "<color=white>";
        laws[currentItem].GetComponent<Bill>().t_House.text = MasterController.GetMC().getTranslation("GOVERNMENT_yea") + ": " + yesColor + houseYesVotes + "</color>\n" + MasterController.GetMC().getTranslation("GOVERNMENT_nay") + ": " + noColor + houseNoVotes + "</color>";

        yesColor = "<color=grey>";
        noColor = "<color=grey>";
        if (senateYesVotes > senateNoVotes) yesColor = "<color=white>";
        else noColor = "<color=white>";
        laws[currentItem].GetComponent<Bill>().t_Senate.text = MasterController.GetMC().getTranslation("GOVERNMENT_yea") + ": " + yesColor + senateYesVotes + "</color>\n" + MasterController.GetMC().getTranslation("GOVERNMENT_nay") + ": " + noColor + senateNoVotes + "</color>";

        if (senateYesVotes + senateNoVotes == MasterController.government.senateNum &&
            houseYesVotes + houseNoVotes == MasterController.government.houseNum)
        {
            if(senateYesVotes == senateNoVotes)
            {
                if(billResults[currentItem].vpVote)
                    laws[currentItem].GetComponent<Bill>().t_Senate.text = "(" + MasterController.GetMC().getTranslation("GOVERNMENT_vice_president_short") + ") " + MasterController.GetMC().getTranslation("GOVERNMENT_yea") + ": " + "<color=white>" + senateYesVotes + "</color>\n" + MasterController.GetMC().getTranslation("GOVERNMENT_nay") + ": " + "<color=grey>" + senateNoVotes + "</color>";
                else
                    laws[currentItem].GetComponent<Bill>().t_Senate.text = MasterController.GetMC().getTranslation("GOVERNMENT_yea") + ": " + "<color=grey>" + senateYesVotes + "</color>\n(" + MasterController.GetMC().getTranslation("GOVERNMENT_vice_president_short") + ") " + MasterController.GetMC().getTranslation("GOVERNMENT_nay") + ": " + "<color=white>" + senateNoVotes + "</color>";
            }

            if (billResults[currentItem].houseYesVotes > MasterController.government.houseNum / 2 &&
                (billResults[currentItem].senateYesVotes > MasterController.government.senateNum / 2 ||
                (billResults[currentItem].senateYesVotes == MasterController.government.senateNum / 2 &&
                billResults[currentItem].vpVote)))
            {
                billPassedCongress = true;
            }
            else
            {
                laws[currentItem].GetComponent<Bill>().t_President.text = "<color=grey>" + MasterController.GetMC().getTranslation("LAW_dead_in_congress") + "</color>";
            }

            loadNextBillResult();
        }
    }

    private void doPropUpdate()
    {
        if (!uiController.TurboMode)
        {
            for (int i = 0; i < 4; i++)
            {
                if (MasterController.GetMC().WeightedRandom(propWeights))
                {
                    propYesVotes++;
                    propWeights[true]--;
                }
                else
                {
                    propNoVotes++;
                    propWeights[false]--;
                }
            }
        }
        else
        {
            propYesVotes += propWeights[true];
            propNoVotes += propWeights[false];
        }

        string yesColor = "<color=grey>";
        string noColor = "<color=grey>";

        if (propYesVotes > propNoVotes) yesColor = "<color=white>";
        else noColor = "<color=white>";

        laws[currentItem].GetComponent<Proposition>().t_Support.text = MasterController.GetMC().getTranslation("OPTION_yes") + ": " + yesColor + (propYesVotes / 1000f).ToString("P1") + "</color>\n" + MasterController.GetMC().getTranslation("OPTION_no") + ":  " + noColor + (propNoVotes / 1000f).ToString("P1") + "</color>";

        if (propYesVotes + propNoVotes == 1000)
        {
            if (propYesVotes == 500)
            {
                laws[currentItem].GetComponent<Proposition>().t_Support.text = MasterController.GetMC().getTranslation("OPTION_yes") + ": " + yesColor + (propYesVotes / 1000f).ToString("P1") + "</color>\n(" + MasterController.GetMC().getTranslation("LAW_prop_tie") + ") " + MasterController.GetMC().getTranslation("OPTION_no") + ":  " + noColor + (propNoVotes / 1000f).ToString("P1") + "</color>";
            }
            loadNextPropResult();
        }
    }

    public void show(List<PropositionResult> result)
    {
        show();
        mode = Mode.PROP;
        propResults = new List<int>();

        MasterController mc = MasterController.GetMC();

        t_Title.text = mc.getTranslation("LAW_prop_title").Replace("$YEAR", mc.currentDate.Year.ToString());

        for (int i = 0; i < result.Count; i++)
        {
            Proposition prop = Instantiate(p_Prop);
            prop.transform.SetParent(lawContainer, false);
            laws.Add(prop.gameObject);

            prop.t_Name.text = mc.getTranslation("LAW_prop_name").Replace("$NAME", result[i].propNum.ToString()) + ":";
            prop.t_Description.text = "<color=" + (result[i].lawDir == Alignment.LIBERAL ? "lime" : "red") + ">";
            prop.t_Description.text += mc.getTranslation("LAW_law_desc").Replace("$DESC", GameData.getData().lawList[result[i].lawDef].electionText[result[i].lawDir]) + "</color>";
            prop.t_Support.text = mc.getTranslation("OPTION_yes") + ": <color=grey>0.0%</color>\n" + mc.getTranslation("OPTION_no") + ":  <color=grey>0.0%</color>";

            propResults.Add(result[i].yesVotes);
        }
    }

    public void show(List<CongressBillResult> result)
    {
        show();
        mode = Mode.CONGRESS;
        billResults = new List<CongressBillResult>();

        MasterController mc = MasterController.GetMC();

        t_Title.text = mc.getTranslation("LAW_congress_title").Replace("$YEAR", mc.currentDate.Year.ToString());

        for(int i=  0; i < result.Count; i++)
        {
            Bill bill = Instantiate(p_Bill);
            bill.transform.SetParent(lawContainer, false);
            laws.Add(bill.gameObject);

            bill.t_Name.text = mc.getTranslation("LAW_congress_name").Replace("$NAME", mc.currentDate.Year + "-" + (i + 1)) + ":";
            bill.t_Description.text = "<color=" + (result[i].lawDir == Alignment.LIBERAL ? "lime" : "red") + ">";
            bill.t_Description.text += mc.getTranslation("LAW_law_desc").Replace("$DESC", GameData.getData().lawList[result[i].lawDef].electionText[result[i].lawDir]) + "</color>";
            bill.t_President.text = "";
            bill.t_House.text = mc.getTranslation("GOVERNMENT_yea") + ": <color=grey>0</color>\n" + mc.getTranslation("GOVERNMENT_nay") +": <color=grey>0</color>";
            bill.t_Senate.text = mc.getTranslation("GOVERNMENT_yea") + ": <color=grey>0</color>\n" + mc.getTranslation("GOVERNMENT_nay") + ": <color=grey>0</color>";

            billResults.Add(result[i]);
        }

    }

    public void show(List<SupremeCourtResult> result)
    {
        show();
        mode = Mode.SUPREME_COURT;
        propResults = new List<int>();
        MasterController mc = MasterController.GetMC();

        t_Title.text = mc.getTranslation("LAW_supreme_court_title").Replace("$YEAR", mc.currentDate.Year.ToString());

        for(int i = 0; i < result.Count; i++)
        {
            //Not really a proposition but the display structure is the same so we'll just use that
            Proposition ccase = Instantiate(p_Prop);
            ccase.transform.SetParent(lawContainer, false);
            laws.Add(ccase.gameObject);

            ccase.t_Name.text = result[i].caseName;
            ccase.t_Description.text = "<color=" + (result[i].lawDir == Alignment.LIBERAL ? "lime" : "red") + ">";
            ccase.t_Description.text += mc.getTranslation("LAW_supreme_court_desc").Replace("$DESC", GameData.getData().lawList[result[i].lawDef].electionText[result[i].lawDir]) + "</color>";
            ccase.t_Support.text = "<color=grey>" + mc.getTranslation("LAW_supreme_court_change").Replace("$COUNT", "0") + "</color>\n<color=grey>" + mc.getTranslation("LAW_supreme_court_status_quo").Replace("$COUNT", "0") +"</color>";

            propResults.Add(result[i].yesVotes);
        }
    }

    public void toggleTurboMode()
    {
        uiController.toggleTurboMode();
        if (uiController.TurboMode)
        {
            b_TurboMode.image.color = uiController.buttonColorOn;
        }
        else
        {
            b_TurboMode.image.color = uiController.buttonColorOff;
        }
    }

    public void show()
    {
        gameObject.SetActive(true);
        uiController.addCurrentScreen(this);

        laws = new List<GameObject>();
        voteStarted = false;
        voteFinished = false;
        presidentStarted = false;
        presidentFinished = false;
        billPassedCongress = false;

        if (uiController.TurboMode)
        {
            b_TurboMode.image.color = uiController.buttonColorOn;
        }
        else
        {
            b_TurboMode.image.color = uiController.buttonColorOff;
        }

        b_ShowResults.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("LAW_show_results_button");
    }

    public void hide()
    {
        gameObject.SetActive(false);
        foreach(GameObject o in laws)
        {
            Destroy(o);
        }

        laws.Clear();
    }

    public void close()
    {
        hide();
        uiController.removeCurrentScreen(this);
    }

    public void refresh()
    {

    }

    public void showResultsButton()
    {
        if (!voteFinished)
        {
            timer = shortDelay;
            voteStarted = true;
            currentItem = -1;

            switch (mode)
            {
                case Mode.PROP:
                    propWeights = new Dictionary<bool, int>();
                    loadNextPropResult();                    
                    break;
                case Mode.CONGRESS:
                    houseWeights = new Dictionary<bool, int>();
                    senateWeights = new Dictionary<bool, int>();
                    loadNextBillResult();
                    break;
                case Mode.SUPREME_COURT:
                    propWeights = new Dictionary<bool, int>();
                    loadNextCaseResult();
                    timer = midDelay;
                    break;
            }
            b_ShowResults.interactable = false;
        }
        else if(mode == Mode.CONGRESS && !presidentFinished)
        {
            timer = longDelay;
            currentItem = -1;
            presidentStarted = true;
            doPresidentSignature();
            b_ShowResults.interactable = false;
        }
        else
        {
            MasterController.GetMC().doNextAction();
        }
    }

    private void doPresidentSignature()
    {
        currentItem++;
        if (currentItem >= laws.Count)
        {
            presidentFinished = true;
            b_ShowResults.interactable = true;
            b_ShowResults.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("LAW_continue");
            return;
        }

        while (!(billResults[currentItem].houseYesVotes > MasterController.government.houseNum / 2 &&
                (billResults[currentItem].senateYesVotes > MasterController.government.senateNum / 2 ||
                (billResults[currentItem].senateYesVotes == MasterController.government.senateNum / 2 &&
                billResults[currentItem].vpVote))))
        {
            currentItem++;
            if (currentItem >= laws.Count)
            {
                presidentFinished = true;
                b_ShowResults.interactable = true;
                b_ShowResults.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("LAW_continue");
                return;
            }
        }

        if (!billResults[currentItem].presidentVeto)
        {
            laws[currentItem].GetComponent<Bill>().t_President.text = MasterController.government.president.getComponent<CreatureInfo>().getName().ToUpper();
        }
        else
        {
            if (billResults[currentItem].houseYesVotes >= MasterController.government.houseNum * (2 / 3f) &&
                billResults[currentItem].senateYesVotes >= MasterController.government.senateNum * (2 / 3f))
                laws[currentItem].GetComponent<Bill>().t_President.text = MasterController.GetMC().getTranslation("LAW_forced_by_congress");
            else
            {
                string color = "<color=" + (billResults[currentItem].lawDir == Alignment.LIBERAL ? "red" : "lime") + ">";
                laws[currentItem].GetComponent<Bill>().t_President.text = color + MasterController.GetMC().getTranslation("LAW_veto") + "</color>";
            }
        }
    }

    private void loadNextCaseResult()
    {
        currentItem++;

        if (currentItem >= laws.Count)
        {
            voteStarted = false;
            voteFinished = true;
            b_ShowResults.interactable = true;
            b_ShowResults.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("LAW_continue");
            //40% chance justice will retire
            if (MasterController.GetMC().LCSRandom(10) >= 6)
                MasterController.government.appointNewJustice(true);
            return;
        }

        propYesVotes = 0;
        propNoVotes = 0;

        int totalYesVotes = propResults[currentItem];
        int totalNoVotes = MasterController.government.supremeCourt.Count - totalYesVotes;

        propWeights[true] = totalYesVotes;
        propWeights[false] = totalNoVotes;
    }

    private void loadNextBillResult()
    {
        currentItem++;

        if (currentItem >= laws.Count)
        {
            voteStarted = false;
            voteFinished = true;
            b_ShowResults.interactable = true;
            if (billPassedCongress)
                b_ShowResults.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("LAW_watch_president");
            else
            {
                b_ShowResults.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("LAW_continue");
                presidentFinished = true;
            }
            return;
        }

        senateYesVotes = 0;
        senateNoVotes = 0;
        houseYesVotes = 0;
        houseNoVotes = 0;

        int totalSenateYesVotes = billResults[currentItem].senateYesVotes;
        int totalHouseYesVotes = billResults[currentItem].houseYesVotes;
        int totalSenateNoVotes = MasterController.government.senateNum - totalSenateYesVotes;
        int totalHouseNoVotes = MasterController.government.houseNum - totalHouseYesVotes;

        senateWeights[true] = totalSenateYesVotes;
        senateWeights[false] = totalSenateNoVotes;
        houseWeights[true] = totalHouseYesVotes;
        houseWeights[false] = totalHouseNoVotes;
    }

    private void loadNextPropResult()
    {
        currentItem++;

        if (currentItem >= laws.Count)
        {
            voteStarted = false;
            voteFinished = true;
            b_ShowResults.interactable = true;
            b_ShowResults.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("LAW_continue");
            return;
        }

        propYesVotes = 0;
        propNoVotes = 0;

        int totalYesVotes = propResults[currentItem];
        int totalNoVotes = 1000 - totalYesVotes;

        propWeights[true] = totalYesVotes;
        propWeights[false] = totalNoVotes;
    }
}
