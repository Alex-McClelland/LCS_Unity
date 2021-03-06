using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.UI;
using LCS.Engine.Components.World;
using LCS.Engine.Data;

public class ElectionScreenController : MonoBehaviour, Election {

    private enum CongressPhase
    {
        PRE_HOUSE_ELECTION,
        HOUSE_ELECTION_STARTED,
        HOUSE_ELECTION_FINISHED,
        PRE_SENATE_ELECTION,
        SENATE_ELECTION_STARTED,
        SENATE_ELECTION_FINISHED
    }

    public UIControllerImpl uiController;

    public Image p_CongressIcon;

    public Transform houseDisplay;
    public Transform senateGroup1;
    public Transform senateGroup2;
    public Transform senateGroup3;
    public Text t_Title;
    public Button b_HouseButton;

    public Sprite s_CC;
    public Sprite s_C;
    public Sprite s_M;
    public Sprite s_L;
    public Sprite s_LL;

    public Text t_CCDelta;
    public Text t_CDelta;
    public Text t_MDelta;
    public Text t_LDelta;
    public Text t_LLDelta;

    public float timeDelay;

    private List<Image> houseIcons;
    private List<Image> senateIcons;
    private CongressPhase congressPhase;
    private int[] seatDeltas = { 0, 0, 0, 0, 0 };
    private List<Alignment> congressResults;

    private float timer;
    private int currentSeat;
    private ElectionActions actions;

    void Awake()
    {
        
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (congressPhase == CongressPhase.HOUSE_ELECTION_STARTED || congressPhase == CongressPhase.SENATE_ELECTION_STARTED)
        {
            timer -= Time.deltaTime;

            if(timer <= 0)
            {
                if (congressPhase == CongressPhase.HOUSE_ELECTION_STARTED)
                    showHouseSeatChange();
                else if (congressPhase == CongressPhase.SENATE_ELECTION_STARTED)
                    showSenateSeatChange();
                timer = timeDelay;
            }
        }
	}

    public void init(ElectionActions actions)
    {
        this.actions = actions;

        int houseNum = 0;
        int senateNum = 0;

        if(houseIcons != null)
        {
            foreach(Image i in houseIcons)
            {
                Destroy(i.gameObject);
            }
            houseIcons.Clear();
        }

        if(senateIcons != null)
        {
            foreach(Image i in senateIcons)
            {
                Destroy(i.gameObject);
            }
            senateIcons.Clear();
        }

        houseIcons = new List<Image>();
        senateIcons = new List<Image>();

        foreach (NationDef.StateDef state in GameData.getData().nationList["USA"].states)
        {
            if ((state.flags & NationDef.stateFlags.NONSTATE) != 0) continue;

            houseNum += state.congress;
            senateNum += 2;
        }

        for (int i = 0; i < houseNum; i++)
        {
            Image icon = Instantiate(p_CongressIcon);
            icon.transform.SetParent(houseDisplay, false);
            houseIcons.Add(icon);
        }

        for (int i = 0; i < senateNum; i++)
        {
            Image icon = Instantiate(p_CongressIcon);
            if (i % 3 == 0) icon.transform.SetParent(senateGroup1, false);
            else if (i % 3 == 1) icon.transform.SetParent(senateGroup2, false);
            else icon.transform.SetParent(senateGroup3, false);
            senateIcons.Add(icon);
        }
    }

    public void show()
    {
        gameObject.SetActive(true);
        uiController.addCurrentScreen(this);

        Government gov = MasterController.government;
        congressPhase = CongressPhase.PRE_HOUSE_ELECTION;
            
        t_Title.text = "House Elections " + MasterController.GetMC().currentDate.Year;
        b_HouseButton.interactable = true;
        b_HouseButton.GetComponentInChildren<Text>().text = "WATCH ELECTION";

        houseDisplay.gameObject.SetActive(true);
        senateGroup1.parent.gameObject.SetActive(false);

        for (int j = 0; j < seatDeltas.Length; j++) seatDeltas[j] = 0;

        int i = 0;

        foreach(string state in gov.house.Keys)
        {
            foreach(Alignment align in gov.house[state])
            {
                houseIcons[i].sprite = getAlignIcon(align);
                houseIcons[i].color = Color.white;
                i++;
            }
        }
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

    }

    public void electionButton()
    {
        switch (congressPhase)
        {
            case CongressPhase.PRE_HOUSE_ELECTION:
                startHouseElection();
                break;
            case CongressPhase.HOUSE_ELECTION_FINISHED:
                prepareSenateElection();
                break;
            case CongressPhase.PRE_SENATE_ELECTION:
                startSenateElection();
                break;
            case CongressPhase.SENATE_ELECTION_FINISHED:
                MasterController.GetMC().doNextAction();
                break;
        }
    }

    private void startSenateElection()
    {
        congressResults = new List<Alignment>();
        timer = timeDelay;
        currentSeat = 0;

        congressPhase = CongressPhase.SENATE_ELECTION_STARTED;
        actions.senateElection();

        foreach (string state in MasterController.government.senate.Keys)
        {
            foreach (Alignment align in MasterController.government.senate[state])
            {
                congressResults.Add(align);
            }
        }

        b_HouseButton.interactable = false;
    }

    private void prepareSenateElection()
    {
        houseDisplay.gameObject.SetActive(false);
        senateGroup1.parent.gameObject.SetActive(true);
        for (int j = 0; j < seatDeltas.Length; j++) seatDeltas[j] = 0;
        t_CCDelta.text = seatDeltas[0].ToString("+#;-#;0");
        t_CDelta.text = seatDeltas[1].ToString("+#;-#;0");
        t_MDelta.text = seatDeltas[2].ToString("+#;-#;0");
        t_LDelta.text = seatDeltas[3].ToString("+#;-#;0");
        t_LLDelta.text = seatDeltas[4].ToString("+#;-#;0");

        int i = 0;

        Government gov = MasterController.government;

        int senateMod = (MasterController.GetMC().currentDate.Year % 6) / 2;

        foreach (string state in gov.senate.Keys)
        {
            foreach (Alignment align in gov.senate[state])
            {
                senateIcons[i].sprite = getAlignIcon(align);
                if (i % 3 == senateMod)
                    senateIcons[i].color = Color.white;
                else
                    senateIcons[i].color = Color.grey;
                i++;
            }
        }

        t_Title.text = "Senate Elections " + MasterController.GetMC().currentDate.Year;
        b_HouseButton.GetComponentInChildren<Text>().text = "WATCH ELECTION";

        congressPhase = CongressPhase.PRE_SENATE_ELECTION;
    }

    private void startHouseElection()
    {
        congressResults = new List<Alignment>();
        timer = timeDelay;
        currentSeat = 0;

        congressPhase = CongressPhase.HOUSE_ELECTION_STARTED;
        actions.houseElection();

        foreach (string state in MasterController.government.house.Keys)
        {
            foreach (Alignment align in MasterController.government.house[state])
            {
                congressResults.Add(align);
            }
        }

        b_HouseButton.interactable = false;
    }

    private void showSenateSeatChange()
    {
        if (senateIcons[currentSeat].sprite == s_CC) seatDeltas[0]--;
        else if (senateIcons[currentSeat].sprite == s_C) seatDeltas[1]--;
        else if (senateIcons[currentSeat].sprite == s_M) seatDeltas[2]--;
        else if (senateIcons[currentSeat].sprite == s_L) seatDeltas[3]--;
        else if (senateIcons[currentSeat].sprite == s_LL) seatDeltas[4]--;

        senateIcons[currentSeat].sprite = getAlignIcon(congressResults[currentSeat]);
        senateIcons[currentSeat].color = Color.grey;

        if (congressResults[currentSeat] == Alignment.ARCHCONSERVATIVE) seatDeltas[0]++;
        else if (congressResults[currentSeat] == Alignment.CONSERVATIVE) seatDeltas[1]++;
        else if (congressResults[currentSeat] == Alignment.MODERATE) seatDeltas[2]++;
        else if (congressResults[currentSeat] == Alignment.LIBERAL) seatDeltas[3]++;
        else if (congressResults[currentSeat] == Alignment.ELITE_LIBERAL) seatDeltas[4]++;

        t_CCDelta.text = seatDeltas[0].ToString("+#;-#;0");
        t_CDelta.text = seatDeltas[1].ToString("+#;-#;0");
        t_MDelta.text = seatDeltas[2].ToString("+#;-#;0");
        t_LDelta.text = seatDeltas[3].ToString("+#;-#;0");
        t_LLDelta.text = seatDeltas[4].ToString("+#;-#;0");

        currentSeat++;

        if (currentSeat >= congressResults.Count)
        {
            congressPhase = CongressPhase.SENATE_ELECTION_FINISHED;
            b_HouseButton.interactable = true;
            b_HouseButton.GetComponentInChildren<Text>().text = "Continue...";
        }
    }

    private void showHouseSeatChange()
    {
        if (houseIcons[currentSeat].sprite == s_CC) seatDeltas[0]--;
        else if (houseIcons[currentSeat].sprite == s_C) seatDeltas[1]--;
        else if (houseIcons[currentSeat].sprite == s_M) seatDeltas[2]--;
        else if (houseIcons[currentSeat].sprite == s_L) seatDeltas[3]--;
        else if (houseIcons[currentSeat].sprite == s_LL) seatDeltas[4]--;

        houseIcons[currentSeat].sprite = getAlignIcon(congressResults[currentSeat]);
        houseIcons[currentSeat].color = Color.grey;

        if (congressResults[currentSeat] == Alignment.ARCHCONSERVATIVE) seatDeltas[0]++;
        else if (congressResults[currentSeat] == Alignment.CONSERVATIVE) seatDeltas[1]++;
        else if (congressResults[currentSeat] == Alignment.MODERATE) seatDeltas[2]++;
        else if (congressResults[currentSeat] == Alignment.LIBERAL) seatDeltas[3]++;
        else if (congressResults[currentSeat] == Alignment.ELITE_LIBERAL) seatDeltas[4]++;

        t_CCDelta.text = seatDeltas[0].ToString("+#;-#;0");
        t_CDelta.text = seatDeltas[1].ToString("+#;-#;0");
        t_MDelta.text = seatDeltas[2].ToString("+#;-#;0");
        t_LDelta.text = seatDeltas[3].ToString("+#;-#;0");
        t_LLDelta.text = seatDeltas[4].ToString("+#;-#;0");

        currentSeat++;

        if(currentSeat >= congressResults.Count)
        {
            congressPhase = CongressPhase.HOUSE_ELECTION_FINISHED;
            b_HouseButton.interactable = true;
            b_HouseButton.GetComponentInChildren<Text>().text = "Continue...";
        }
    }

    private Sprite getAlignIcon(Alignment align)
    {
        switch (align)
        {
            case Alignment.ARCHCONSERVATIVE:
                return s_CC;
            case Alignment.CONSERVATIVE:
                return s_C;
            case Alignment.MODERATE:
                return s_M;
            case Alignment.LIBERAL:
                return s_L;
            case Alignment.ELITE_LIBERAL:
                return s_LL;
        }

        return null;
    }
}
