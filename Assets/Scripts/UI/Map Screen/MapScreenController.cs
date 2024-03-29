﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.UI;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Creature;
using LCS.Engine.Containers;
using LCS.Engine.Components.Location;
using LCS.Engine.Data;

public class MapScreenController : MonoBehaviour, NationMap {

    public enum MapMode
    {
        HOUSE_VIEW,
        SENATE_VIEW,
        ALIGNMENT_VIEW,
        PRESIDENTIAL_ELECTION,
        AMENDMENT_VOTE
    }

    public UIControllerImpl uiController;

    public float veryShortDelay;
    public float shortDelay;
    public float longDelay;

    public List<Image> stateImages;
    public MapMode mapMode;

    public Color c_Archconservative;
    public Color c_Conservative;
    public Color c_Moderate;
    public Color c_Liberal;
    public Color c_EliteLiberal;
    public Color c_AwaitingResults;
    public ButtonSelectionGroup buttons;
    public Button b_Back;
    public Button b_Disband;

    public Text t_LiberalCandidateName;
    public Text t_ConservativeCandidateName;
    public Text t_LiberalVotes;
    public Text t_ConservativeVotes;
    public Text t_LiberalPopVote;
    public Text t_ConservativePopVote;
    public Button b_BeginElection;
    public Text t_ElectionTicker;
    public PortraitImage i_LibCandidate;
    public PortraitImage i_ConCandidate;

    private NationMapActions actions;

    private PresidentialElectionResult electionResult;
    private AmendmentResult amendmentResult;
    private bool amendmentCongressNeeded;
    private bool amendmentCongressStarted;
    private bool amendmentCongressFinished;
    private bool amendmentStatesStarted;
    private bool amendmentStatesFinished;
    private Dictionary<bool, int> houseWeights;
    private int houseYeaTotal;
    private int houseNayTotal;
    private Dictionary<bool, int> senateWeights;
    private int senateYeaTotal;
    private int senateNayTotal;
    private List<NationDef.StateDef> stateList;

    private Dictionary<string, Image> stateImageDict;
    private float nextResultTimer;
    private bool electionStarted;    
    private int electionTick;
    private int electionTickMax;
    private int popTotal;
    private int electoralVotesTotal;
    private int libVoteTotal;
    private int conVoteTotal;
    private float libPopTotal;
    private float conPopTotal;
    private bool recounts;
    private Dictionary<string, bool> reportedStates;
    private string recountText;

    private bool tiebreaker;
    private bool tiebreakerStarted;
    private int libTiebreakerVotes;
    private int conTiebreakerVotes;

    void Awake()
    {
        stateImageDict = new Dictionary<string, Image>();

        foreach(Image i in stateImages)
        {
            stateImageDict.Add(i.gameObject.name, i);
        }
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if(mapMode == MapMode.PRESIDENTIAL_ELECTION && (electionStarted || tiebreakerStarted))
        {
            nextResultTimer -= Time.deltaTime;

            if (nextResultTimer <= 0)
            {
                if (electionStarted)
                {
                    nextResultTimer = shortDelay;
                    doElectionTick(electionTick);

                    if (electionTick > electionTickMax)
                    {
                        if (!recounts)
                        {
                            electionStarted = false;
                        }
                        finishElection();
                    }
                }
                else if (tiebreakerStarted)
                {
                    nextResultTimer = shortDelay;
                    doTieBreaker();

                    if (!tiebreakerStarted)
                    {
                        finishTiebreaker();
                    }
                }
            }
        }

        if(mapMode == MapMode.AMENDMENT_VOTE && (amendmentCongressStarted || amendmentStatesStarted))
        {
            nextResultTimer -= Time.deltaTime;

            if(nextResultTimer <= 0)
            {
                doAmendmentTick();
                if (amendmentCongressStarted)
                    nextResultTimer = veryShortDelay;
                else if (amendmentStatesStarted)
                    nextResultTimer = shortDelay;
            }
        }

        bool underSiege = false;

        foreach (Entity e in MasterController.nation.getAllBases())
        {
            if (!e.getComponent<SafeHouse>().owned) continue;
            if (e.getComponent<SafeHouse>().underSiege) underSiege = true;
        }

        if (underSiege)
            b_Disband.interactable = false;
        else
            b_Disband.interactable = true;
    }

    public void init(NationMapActions actions)
    {
        this.actions = actions;
    }

    public void show()
    {
        gameObject.SetActive(true);
        uiController.addCurrentScreen(this);

        if (mapMode == MapMode.PRESIDENTIAL_ELECTION)
        {
            clearStateMouseoverText();

            buttons.gameObject.SetActive(false);
            b_Back.gameObject.SetActive(false);
            b_Disband.gameObject.SetActive(false);

            t_LiberalCandidateName.text = UIControllerImpl.getAlignmentColor(electionResult.liberalCandidate.getComponent<Politician>().alignment) + electionResult.liberalCandidateRunningName + "</color>";
            t_LiberalCandidateName.fontStyle = FontStyle.Normal;
            t_ConservativeCandidateName.text = UIControllerImpl.getAlignmentColor(electionResult.conservativeCandidate.getComponent<Politician>().alignment) + electionResult.conservativeCandidateRunningName + "</color>";
            t_ConservativeCandidateName.fontStyle = FontStyle.Normal;
            t_LiberalPopVote.fontStyle = FontStyle.Normal;
            t_ConservativePopVote.fontStyle = FontStyle.Normal;

            i_LibCandidate.buildPortrait(electionResult.liberalCandidate);
            i_ConCandidate.buildPortrait(electionResult.conservativeCandidate);

            t_LiberalVotes.text = "<color=grey>0</color>";
            t_ConservativeVotes.text = "<color=grey>0</color>";
            t_LiberalPopVote.text = "0.00 %";
            t_ConservativePopVote.text = "0.00 %";

            electionTick = 0;
            b_BeginElection.GetComponentInChildren<Text>().text = "WATCH ELECTION";

            t_ElectionTicker.text = "";

            foreach (Image i in stateImages)
            {
                i.gameObject.SetActive(true);
                i.color = c_AwaitingResults;
            }

            t_LiberalCandidateName.gameObject.SetActive(true);
            t_ConservativeCandidateName.gameObject.SetActive(true);
            t_LiberalVotes.gameObject.SetActive(true);
            t_ConservativeVotes.gameObject.SetActive(true);
            t_LiberalPopVote.gameObject.SetActive(true);
            t_ConservativePopVote.gameObject.SetActive(true);
            b_BeginElection.gameObject.SetActive(true);
            t_ElectionTicker.gameObject.SetActive(true);
            i_LibCandidate.gameObject.SetActive(true);
            i_ConCandidate.gameObject.SetActive(true);

            electionStarted = false;
            tiebreaker = false;
            tiebreakerStarted = false;
        }
        else if(mapMode == MapMode.AMENDMENT_VOTE)
        {
            clearStateMouseoverText();

            buttons.gameObject.SetActive(false);
            b_Back.gameObject.SetActive(false);
            b_Disband.gameObject.SetActive(false);
            b_BeginElection.GetComponentInChildren<Text>().text = "WATCH RATIFICATION";
            b_BeginElection.gameObject.SetActive(true);
            t_ElectionTicker.gameObject.SetActive(false);
            i_LibCandidate.gameObject.SetActive(false);
            i_ConCandidate.gameObject.SetActive(false);

            if (!amendmentCongressNeeded)
            {
                amendmentCongressStarted = true;
                amendmentStatesFinished = true;
            }
            else
            {
                amendmentCongressStarted = false;
                amendmentCongressFinished = false;
            }
            amendmentStatesStarted = false;
            amendmentStatesFinished = false;
            stateList = new List<NationDef.StateDef>();

            NationDef nation = GameData.getData().nationList["USA"];
            foreach (NationDef.StateDef state in nation.states)
            {
                if ((state.flags & NationDef.stateFlags.NONSTATE) != 0) continue;
                stateList.Add(state);
            }

            foreach (Image i in stateImages)
            {
                i.gameObject.SetActive(true);
                i.color = c_AwaitingResults;
            }

            if (amendmentCongressNeeded)
            {
                houseWeights = new Dictionary<bool, int>();
                senateWeights = new Dictionary<bool, int>();
                houseWeights[true] = amendmentResult.houseYesVotes;
                houseWeights[false] = MasterController.government.houseNum - amendmentResult.houseYesVotes;
                senateWeights[true] = amendmentResult.senateYesVotes;
                senateWeights[false] = MasterController.government.senateNum - amendmentResult.senateYesVotes;

                //Repurpose the lib/con votes for house/senate votes since they're in the same spot anyway
                t_LiberalCandidateName.gameObject.SetActive(true);
                t_LiberalCandidateName.fontStyle = FontStyle.Normal;
                t_ConservativeCandidateName.gameObject.SetActive(true);
                t_ConservativeCandidateName.fontStyle = FontStyle.Normal;
                t_LiberalPopVote.gameObject.SetActive(true);
                t_LiberalPopVote.fontStyle = FontStyle.Normal;
                t_ConservativePopVote.gameObject.SetActive(true);
                t_ConservativePopVote.fontStyle = FontStyle.Normal;
                t_LiberalVotes.gameObject.SetActive(true);
                t_ConservativeVotes.gameObject.SetActive(true);

                t_LiberalVotes.text = "HOUSE";
                t_LiberalCandidateName.text = "House Yea 0";
                houseYeaTotal = 0;
                t_LiberalPopVote.text = "House Nay 0";
                houseNayTotal = 0;
                t_ConservativeVotes.text = "SENATE";
                t_ConservativeCandidateName.text = "Senate Yea 0";
                senateYeaTotal = 0;
                t_ConservativePopVote.text = "Senate Nay 0";
                senateNayTotal = 0;
            }
            else
            {
                t_LiberalCandidateName.gameObject.SetActive(false);
                t_ConservativeCandidateName.gameObject.SetActive(false);
                t_LiberalPopVote.gameObject.SetActive(false);
                t_ConservativePopVote.gameObject.SetActive(false);
                t_LiberalVotes.gameObject.SetActive(false);
                t_ConservativeVotes.gameObject.SetActive(false);
            }
        }
        else
        {
            buttons.gameObject.SetActive(true);
            b_Back.gameObject.SetActive(true);
            b_Disband.gameObject.SetActive(true);
            buttons.ButtonSelect(0);
            setHouseView();
            setStateMouseoverText();

            t_LiberalCandidateName.gameObject.SetActive(false);
            t_ConservativeCandidateName.gameObject.SetActive(false);
            t_LiberalVotes.gameObject.SetActive(false);
            t_ConservativeVotes.gameObject.SetActive(false);
            t_LiberalPopVote.gameObject.SetActive(false);
            t_ConservativePopVote.gameObject.SetActive(false);
            b_BeginElection.gameObject.SetActive(false);
            t_ElectionTicker.gameObject.SetActive(false);
            i_LibCandidate.gameObject.SetActive(false);
            i_ConCandidate.gameObject.SetActive(false);
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

    public void showPresidentialElection(PresidentialElectionResult result)
    {
        electionResult = result;
        mapMode = MapMode.PRESIDENTIAL_ELECTION;

        string primaryText = "After a long primary campaign, the people have rallied around two leaders...\n";
        string candidateColor = UIControllerImpl.getAlignmentColor(result.liberalCandidate.getComponent<Politician>().alignment);
        primaryText += candidateColor + result.liberalCandidateRunningName + "</color> vs. ";
        candidateColor = UIControllerImpl.getAlignmentColor(result.conservativeCandidate.getComponent<Politician>().alignment);
        primaryText += candidateColor + result.conservativeCandidateRunningName + "</color>";

        uiController.showPopup(primaryText, ()=> { uiController.closeUI(); show(); });
    }

    public void showAmendmentVote(AmendmentResult result, string title, string description, bool congressNeeded)
    {
        amendmentResult = result;
        mapMode = MapMode.AMENDMENT_VOTE;
        amendmentCongressNeeded = congressNeeded;        

        uiController.showPopup(title + "\n" + description, () => { uiController.closeUI(); show(); });
    }

    public void showDemographics()
    {
        mapMode = MapMode.HOUSE_VIEW;
        show();
    }

    public void back()
    {
        close();
        uiController.showUI();
    }

    public void electionButton()
    {
        if (mapMode == MapMode.PRESIDENTIAL_ELECTION)
        {
            if (electionTick == 0)
            {
                watchElection();
            }
            else if (!tiebreaker)
            {
                MasterController.GetMC().doNextAction();
            }
            else
            {
                startTieBreaker();
            }
        }
        else if(mapMode == MapMode.AMENDMENT_VOTE)
        {
            if (!amendmentCongressFinished)
            {
                watchAmendmentCongress();
            }
            else if (!amendmentStatesFinished)
            {
                watchAmendmentStates();
            }
            else
            {
                MasterController.GetMC().doNextAction();
            }
        }
    }

    private void watchAmendmentCongress()
    {
        amendmentCongressStarted = true;
        b_BeginElection.interactable = false;
        nextResultTimer = shortDelay;

        houseYeaTotal = 0;
        houseNayTotal = 0;
        senateYeaTotal = 0;
        houseNayTotal = 0;
    }

    private void watchAmendmentStates()
    {
        amendmentStatesStarted = true;
        b_BeginElection.interactable = false;
        nextResultTimer = shortDelay;
    }

    private void watchElection()
    {
        reportedStates = new Dictionary<string, bool>();

        nextResultTimer = longDelay;
        electionStarted = true;
        b_BeginElection.interactable = false;
        b_BeginElection.GetComponentInChildren<Text>().text = "AWAITING RESULTS...";

        libVoteTotal = 0;
        conVoteTotal = 0;
        libPopTotal = 0;
        conPopTotal = 0;

        popTotal = 0;
        electoralVotesTotal = 0;
        electionTickMax = 0;
        recountText = "";

        recounts = false;

        foreach(NationDef.StateDef state in GameData.getData().nationList["USA"].states)
        {
            popTotal += state.population;
            electoralVotesTotal += state.electoralVotes;
            reportedStates.Add(state.name, false);
            if (state.voteOrder > electionTickMax) electionTickMax = state.voteOrder;
        }
    }

    private void finishTiebreaker()
    {
        if(libTiebreakerVotes > conTiebreakerVotes)
        {
            t_ElectionTicker.text = UIControllerImpl.getAlignmentColor(electionResult.liberalCandidate.getComponent<Politician>().alignment) + electionResult.liberalCandidateRunningName + " declares victory!</color>";
            t_ElectionTicker.text += "\n" + UIControllerImpl.getAlignmentColor(electionResult.VPwinnerAlignment) + electionResult.VPwinnerName + "</color> has been selected by the Senate as their Vice President.";
            b_BeginElection.interactable = true;
            b_BeginElection.GetComponentInChildren<Text>().text = "Reflect on these changes";
        }
        else if(conTiebreakerVotes > libTiebreakerVotes)
        {
            t_ElectionTicker.text = UIControllerImpl.getAlignmentColor(electionResult.conservativeCandidate.getComponent<Politician>().alignment) + electionResult.conservativeCandidateRunningName + " declares victory!</color>";
            t_ElectionTicker.text += "\n" + UIControllerImpl.getAlignmentColor(electionResult.VPwinnerAlignment) + electionResult.VPwinnerName + "</color> has been selected by the Senate as their Vice President.";
            b_BeginElection.interactable = true;
            b_BeginElection.GetComponentInChildren<Text>().text = "Reflect on these changes";
        }
        else
        {
            string winnerColor = "";
            if (electionResult.liberalCandidateRunningName == electionResult.winnerName) winnerColor = UIControllerImpl.getAlignmentColor(electionResult.liberalCandidate.getComponent<Politician>().alignment);
            else winnerColor = UIControllerImpl.getAlignmentColor(electionResult.conservativeCandidate.getComponent<Politician>().alignment);

            MasterController.GetMC().addMessage("After hours of deliberation and a few fist fights, congress has finally selected " + winnerColor + electionResult.winnerName + "</color> as the next President.\n" + UIControllerImpl.getAlignmentColor(electionResult.VPwinnerAlignment) + electionResult.VPwinnerName + "</color> has been selected by the Senate as their Vice President.", true);
            b_BeginElection.interactable = true;
            b_BeginElection.GetComponentInChildren<Text>().text = MasterController.GetMC().swearFilter("ARE YOU FUCKING KIDDING ME", "What a wonderful example of the democratic process");
        }

        tiebreaker = false;
    }

    private void startTieBreaker()
    {
        tiebreakerStarted = true;
        b_BeginElection.interactable = false;
        reportedStates.Clear();
        nextResultTimer = longDelay;

        libTiebreakerVotes = 0;
        conTiebreakerVotes = 0;

        foreach (NationDef.StateDef state in GameData.getData().nationList["USA"].states)
        {
            if((state.flags & NationDef.stateFlags.NONSTATE) != 0)
            {
                stateImageDict[state.shortname].gameObject.SetActive(false);
                continue;
            }

            if((state.flags & NationDef.stateFlags.PROPORTIONAL_ELECTORAL_VOTES) != 0)
            {
                deactivateSubstateImages(state.shortname);
            }

            reportedStates.Add(state.name, false);
            stateImageDict[state.shortname].color = c_AwaitingResults;
        }
    }

    private void doTieBreaker()
    {
        Color conCandidateColor = getCandidateColor(electionResult.conservativeCandidate.getComponent<Politician>().alignment);
        Color libCandidateColor = getCandidateColor(electionResult.liberalCandidate.getComponent<Politician>().alignment);

        bool tiebreakerFinished = true;

        foreach (NationDef.StateDef state in GameData.getData().nationList["USA"].states)
        {
            if ((state.flags & NationDef.stateFlags.NONSTATE) != 0 || reportedStates[state.name])
            {
                continue;
            }

            tiebreakerFinished = false;

            reportedStates[state.name] = true;

            if (electionResult.stateTiebreakerResults[state.name] == Alignment.LIBERAL)
            {
                stateImageDict[state.shortname].color = libCandidateColor;
                libTiebreakerVotes++;
                break;
            }
            else
            {
                stateImageDict[state.shortname].color = conCandidateColor;
                conTiebreakerVotes++;
                break;
            }
        }
        
        t_LiberalVotes.text = "" + libTiebreakerVotes;
        t_LiberalVotes.fontStyle = FontStyle.Bold;
        t_ConservativeVotes.text = "" + conTiebreakerVotes;
        t_ConservativeVotes.fontStyle = FontStyle.Bold;

        if (tiebreakerFinished)
        {
            tiebreakerStarted = false;
        }
    }

    private void finishElection()
    {
        //If no recounts, or if they wouldn't make a difference, just end the election.
        if (!recounts || libVoteTotal > (electoralVotesTotal / 2) + 1 || conVoteTotal > (electoralVotesTotal / 2) + 1)
        {
            if(libVoteTotal > conVoteTotal)
            {
                t_ElectionTicker.text = UIControllerImpl.getAlignmentColor(electionResult.liberalCandidate.getComponent<Politician>().alignment) + electionResult.liberalCandidateRunningName + " declares victory!</color>";
            }
            else if(conVoteTotal > libVoteTotal)
            {
                t_ElectionTicker.text = UIControllerImpl.getAlignmentColor(electionResult.conservativeCandidate.getComponent<Politician>().alignment) + electionResult.conservativeCandidateRunningName + " declares victory!</color>";
            }
            else
            {
                t_ElectionTicker.text = "With the electoral college deadlocked, it falls to Congress to select a new leader...\n";
                tiebreaker = true;
            }

            if (!tiebreaker)
            {
                b_BeginElection.GetComponentInChildren<Text>().text = "Reflect on these changes";
            }
            else
            {
                b_BeginElection.GetComponentInChildren<Text>().text = "WATCH TIEBREAKER";
            }

            recounts = false;
            b_BeginElection.interactable = true;
        }
        else
        {
            recounts = false;

            Color conCandidateColor = getCandidateColor(electionResult.conservativeCandidate.getComponent<Politician>().alignment);
            Color libCandidateColor = getCandidateColor(electionResult.liberalCandidate.getComponent<Politician>().alignment);

            t_ElectionTicker.text = "With the election still undecided, it comes down to the recounts...\n";
            recountText = "";

            foreach (NationDef.StateDef state in GameData.getData().nationList["USA"].states)
            {
                //Report one recount at a time until someone wins or we run out of states.
                if (electionResult.stateRecounts[state.name])
                {
                    recounts = true;
                    nextResultTimer = longDelay;
                    electionResult.stateRecounts[state.name] = false;
                    t_ElectionTicker.text += state.name + " recount reporting in!";
                    if (electionResult.stateResults[state.name] == Alignment.LIBERAL)
                    {
                        stateImageDict[state.shortname].color = libCandidateColor;
                        libVoteTotal += state.electoralVotes;
                    }
                    else
                    {
                        stateImageDict[state.shortname].color = conCandidateColor;
                        conVoteTotal += state.electoralVotes;
                    }

                    libPopTotal += ((electionResult.statePopularResults[state.name][0] / 200f) * state.population) / popTotal;
                    conPopTotal += ((electionResult.statePopularResults[state.name][1] / 200f) * state.population) / popTotal;

                    if (libVoteTotal < (electoralVotesTotal / 2) + 1)
                    {
                        t_LiberalVotes.text = "<color=grey>" + libVoteTotal + "</color>";
                    }
                    else
                    {
                        t_LiberalVotes.text = "<color=white>" + libVoteTotal + "</color>";
                    }

                    if (conVoteTotal < (electoralVotesTotal / 2) + 1)
                    {
                        t_ConservativeVotes.text = "<color=grey>" + conVoteTotal + "</color>";
                    }
                    else
                    {
                        t_ConservativeVotes.text = "<color=white>" + conVoteTotal + "</color>";
                    }

                    t_LiberalPopVote.text = libPopTotal.ToString("P2");
                    t_ConservativePopVote.text = conPopTotal.ToString("P2");

                    break;
                }
            }
        }
    }

    private void doAmendmentTick()
    {
        if(amendmentCongressStarted)
        {
            if(houseYeaTotal + houseNayTotal < MasterController.government.houseNum)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (houseYeaTotal + houseNayTotal >= MasterController.government.houseNum) break;

                    if (MasterController.GetMC().WeightedRandom(houseWeights))
                    {
                        houseYeaTotal++;
                        houseWeights[true]--;
                    }
                    else
                    {
                        houseNayTotal++;
                        houseWeights[false]--;
                    }
                }
            }
            else if(senateYeaTotal + senateNayTotal < MasterController.government.senateNum)
            {
                if (MasterController.GetMC().WeightedRandom(senateWeights))
                {
                    senateYeaTotal++;
                    senateWeights[true]--;
                }
                else
                {
                    senateNayTotal++;
                    senateWeights[false]--;
                }
            }
            else
            {
                amendmentCongressFinished = true;
                amendmentCongressStarted = false;
                if (!amendmentResult.congressRatified)
                {
                    amendmentStatesFinished = true;
                    b_BeginElection.GetComponentInChildren<Text>().text = "REFLECT ON THIS";
                }
                else
                {
                    b_BeginElection.GetComponentInChildren<Text>().text = "WATCH STATES";
                }

                b_BeginElection.interactable = true;
            }
            if (houseYeaTotal >= (MasterController.government.houseNum * 2) / 3)
                t_LiberalCandidateName.fontStyle = FontStyle.Bold;
            t_LiberalCandidateName.text = "House Yea " + houseYeaTotal;
            if (houseNayTotal > (MasterController.government.houseNum) / 3)
                t_LiberalPopVote.fontStyle = FontStyle.Bold;
            t_LiberalPopVote.text = "House Nay " + houseNayTotal;
            if (senateYeaTotal >= (MasterController.government.senateNum * 2) / 3)
                t_ConservativeCandidateName.fontStyle = FontStyle.Bold;
            t_ConservativeCandidateName.text = "Senate Yea " + senateYeaTotal;
            if (senateNayTotal > (MasterController.government.senateNum) / 3)
                t_ConservativePopVote.fontStyle = FontStyle.Bold;
            t_ConservativePopVote.text = "Senate Nay " + senateNayTotal;
        }
        else if (amendmentStatesStarted)
        {
            if(stateList.Count > 0)
            {
                NationDef.StateDef state = stateList[MasterController.GetMC().LCSRandom(stateList.Count)];
                stateList.Remove(state);
                //TODO: Color depends on whether this is a "good" or "bad" amendment

                if ((state.flags & NationDef.stateFlags.PROPORTIONAL_ELECTORAL_VOTES) == 0)
                {
                    if (amendmentResult.stateVotes[state.name])
                    {
                        stateImageDict[state.shortname].color = c_EliteLiberal;
                    }
                    else
                    {
                        stateImageDict[state.shortname].color = c_Archconservative;
                    }
                }
                else
                {
                    activateSubstateImages(state.shortname);                    

                    for (int i = 0; i < state.electoralVotes; i++)
                    {
                        if (amendmentResult.stateVotes[state.name])
                        {
                            stateImageDict[state.shortname + "_" + i].color = c_EliteLiberal;
                        }
                        else
                            stateImageDict[state.shortname + "_" + i].color = c_Archconservative;
                    }
                }
            }
            else
            {
                amendmentStatesStarted = false;
                amendmentStatesFinished = true;

                b_BeginElection.GetComponentInChildren<Text>().text = "REFLECT ON THIS";
                b_BeginElection.interactable = true;
            }
        }
    }

    private void doElectionTick(int tick)
    {
        NationDef nation = GameData.getData().nationList["USA"];

        Color conCandidateColor = getCandidateColor(electionResult.conservativeCandidate.getComponent<Politician>().alignment);
        Color libCandidateColor = getCandidateColor(electionResult.liberalCandidate.getComponent<Politician>().alignment);

        bool allStatesReported = true;

        if (tick == 0)
            t_ElectionTicker.text = "The first districts are reporting in...\n" + recountText;
        else
            t_ElectionTicker.text = "" + recountText;

        foreach(NationDef.StateDef state in nation.states)
        {
            if(state.voteOrder == tick)
            {
                if (reportedStates[state.name]) continue;

                allStatesReported = false;

                if (electionResult.stateRecounts[state.name])
                {
                    recountText += "Recount needed in " + state.name + "!\n";
                    recounts = true;
                    reportedStates[state.name] = true;
                    break;
                }
                else
                {
                    if ((state.flags & NationDef.stateFlags.PROPORTIONAL_ELECTORAL_VOTES) == 0)
                    {
                        if (electionResult.stateResults[state.name] == Alignment.LIBERAL)
                        {
                            stateImageDict[state.shortname].color = libCandidateColor;
                            libVoteTotal += state.electoralVotes;
                        }
                        else
                        {
                            stateImageDict[state.shortname].color = conCandidateColor;
                            conVoteTotal += state.electoralVotes;
                        }
                    }
                    else
                    {
                        activateSubstateImages(state.shortname);

                        int libVote = electionResult.stateSpecificResults[state.name][0];

                        for (int i = 0; i < state.electoralVotes; i++)
                        {
                            if (libVote > 0)
                            {
                                stateImageDict[state.shortname + "_" + i].color = libCandidateColor;
                                libVote--;
                            }
                            else
                                stateImageDict[state.shortname + "_" + i].color = conCandidateColor;
                        }

                        libVoteTotal += electionResult.stateSpecificResults[state.name][0];
                        conVoteTotal += electionResult.stateSpecificResults[state.name][1];
                    }

                    libPopTotal += ((electionResult.statePopularResults[state.name][0]/200f)*state.population) / popTotal;
                    conPopTotal += ((electionResult.statePopularResults[state.name][1]/200f)*state.population) / popTotal;

                    float independentVotes = 200 - electionResult.statePopularResults[state.name][0] - electionResult.statePopularResults[state.name][1];

                    stateImageDict[state.shortname].GetComponent<MouseOverText>().mouseOverText = state.name + "\nLiberal Party: " + (electionResult.statePopularResults[state.name][0] / 200f).ToString("P1") + "\nConservative Party: " + (electionResult.statePopularResults[state.name][1] / 200f).ToString("P1") + (independentVotes > 0?"\nThird Parties: " + (independentVotes/200f).ToString("P1"):"");
                    reportedStates[state.name] = true;
                    break;
                }
            }
        }

        if (allStatesReported)
        {
            electionTick++;
            nextResultTimer = longDelay;
        }

        if (libVoteTotal < (electoralVotesTotal / 2) + 1)
        {
            t_LiberalVotes.text = "<color=grey>" + libVoteTotal + "</color>";
            t_LiberalCandidateName.fontStyle = FontStyle.Normal;
        }
        else
        {
            t_LiberalVotes.text = "<color=white>" + libVoteTotal + "</color>";
            t_LiberalCandidateName.fontStyle = FontStyle.Bold;
        }

        if (conVoteTotal < (electoralVotesTotal / 2) + 1)
        {
            t_ConservativeVotes.text = "<color=grey>" + conVoteTotal + "</color>";
            t_ConservativeCandidateName.fontStyle = FontStyle.Normal;
        }
        else
        {
            t_ConservativeVotes.text = "<color=white>" + conVoteTotal + "</color>";
            t_ConservativeCandidateName.fontStyle = FontStyle.Bold;
        }

        t_LiberalPopVote.text = libPopTotal.ToString("P2");
        t_ConservativePopVote.text = conPopTotal.ToString("P2");
    }

    public void setStateMouseoverText()
    {
        NationDef nation = GameData.getData().nationList["USA"];
        Government gov = MasterController.government;

        foreach(NationDef.StateDef state in nation.states)
        {
            string tooltipText = state.name + "\n";

            if ((state.flags & NationDef.stateFlags.NONSTATE) == 0)
            {

                int CC = 0;
                int C = 0;
                int M = 0;
                int L = 0;
                int LL = 0;

                foreach (Alignment a in gov.house[state.name])
                {
                    switch (a)
                    {
                        case Alignment.ARCHCONSERVATIVE:
                            CC++;
                            break;
                        case Alignment.CONSERVATIVE:
                            C++;
                            break;
                        case Alignment.MODERATE:
                            M++;
                            break;
                        case Alignment.LIBERAL:
                            L++;
                            break;
                        case Alignment.ELITE_LIBERAL:
                            LL++;
                            break;
                    }
                }

                tooltipText += "House: " + LL + " L+ / " + L + " L / " + M + " M / " + C + " C / " + CC + " C+\n";

                CC = 0;
                C = 0;
                M = 0;
                L = 0;
                LL = 0;

                foreach (Alignment a in gov.senate[state.name])
                {
                    switch (a)
                    {
                        case Alignment.ARCHCONSERVATIVE:
                            CC++;
                            break;
                        case Alignment.CONSERVATIVE:
                            C++;
                            break;
                        case Alignment.MODERATE:
                            M++;
                            break;
                        case Alignment.LIBERAL:
                            L++;
                            break;
                        case Alignment.ELITE_LIBERAL:
                            LL++;
                            break;
                    }
                }

                tooltipText += "Senate: " + LL + " L+ / " + L + " L / " + M + " M / " + C + " C / " + CC + " C+\n";
            }

            tooltipText += "Alignment: ";

            if (state.alignment >= 4) tooltipText += "Very Liberal";
            else if (state.alignment >= 2) tooltipText += "Moderately Liberal";
            else if (state.alignment >= 1) tooltipText += "Leans Liberal";
            else if (state.alignment >= 0) tooltipText += "Swing State";
            else if (state.alignment >= -1) tooltipText += "Leans Conservative";
            else if (state.alignment >= -3) tooltipText += "Moderately Conservative";
            else tooltipText += "Very Conservative";

            tooltipText += "\nEC Votes: " + state.electoralVotes;
            if ((state.flags & NationDef.stateFlags.PROPORTIONAL_ELECTORAL_VOTES) != 0) tooltipText += " (Distributed proportionally)";

            stateImageDict[state.shortname].GetComponent<MouseOverText>().mouseOverText = tooltipText;
        }
    }

    public void clearStateMouseoverText()
    {
        foreach(Image i in stateImages)
        {
            if(i.GetComponent<MouseOverText>() != null)
                i.GetComponent<MouseOverText>().mouseOverText = "";
        }
    }

    public void setHouseView()
    {
        NationDef nation = GameData.getData().nationList["USA"];
        Government gov = MasterController.government;

        foreach (NationDef.StateDef state in nation.states)
        {
            if ((state.flags & NationDef.stateFlags.NONSTATE) != 0)
            {
                stateImageDict[state.shortname].gameObject.SetActive(false);
                continue;
            }

            if ((state.flags & NationDef.stateFlags.PROPORTIONAL_ELECTORAL_VOTES) != 0)
                deactivateSubstateImages(state.shortname);

            int CC = 0;
            int C = 0;
            int M = 0;
            int L = 0;
            int LL = 0;

            foreach (Alignment a in gov.house[state.name])
            {
                switch (a)
                {
                    case Alignment.ARCHCONSERVATIVE:
                        CC++;
                        break;
                    case Alignment.CONSERVATIVE:
                        C++;
                        break;
                    case Alignment.MODERATE:
                        M++;
                        break;
                    case Alignment.LIBERAL:
                        L++;
                        break;
                    case Alignment.ELITE_LIBERAL:
                        LL++;
                        break;
                }
            }

            if (CC > state.congress / 2) stateImageDict[state.shortname].color = c_Archconservative;
            else if (LL > state.congress / 2) stateImageDict[state.shortname].color = c_EliteLiberal;
            else if (CC + C > state.congress / 2) stateImageDict[state.shortname].color = c_Conservative;
            else if (LL + L > state.congress / 2) stateImageDict[state.shortname].color = c_Liberal;
            else stateImageDict[state.shortname].color = c_Moderate;
        }
    }

    public void setSenateView()
    {
        NationDef nation = GameData.getData().nationList["USA"];
        Government gov = MasterController.government;

        foreach (NationDef.StateDef state in nation.states)
        {
            if ((state.flags & NationDef.stateFlags.NONSTATE) != 0)
            {
                stateImageDict[state.shortname].gameObject.SetActive(false);
                continue;
            }

            if ((state.flags & NationDef.stateFlags.PROPORTIONAL_ELECTORAL_VOTES) != 0)
                deactivateSubstateImages(state.shortname);

            int CC = 0;
            int C = 0;
            int M = 0;
            int L = 0;
            int LL = 0;

            foreach (Alignment a in gov.senate[state.name])
            {
                switch (a)
                {
                    case Alignment.ARCHCONSERVATIVE:
                        CC++;
                        break;
                    case Alignment.CONSERVATIVE:
                        C++;
                        break;
                    case Alignment.MODERATE:
                        M++;
                        break;
                    case Alignment.LIBERAL:
                        L++;
                        break;
                    case Alignment.ELITE_LIBERAL:
                        LL++;
                        break;
                }
            }

            if (CC > 1) stateImageDict[state.shortname].color = c_Archconservative;
            else if (LL > 1) stateImageDict[state.shortname].color = c_EliteLiberal;
            else if (CC + C > 1) stateImageDict[state.shortname].color = c_Conservative;
            else if (LL + L > 1) stateImageDict[state.shortname].color = c_Liberal;
            else stateImageDict[state.shortname].color = c_Moderate;
            
        }
    }

    public void setAlignmentView()
    {
        NationDef nation = GameData.getData().nationList["USA"];

        foreach (NationDef.StateDef state in nation.states)
        {
            if ((state.flags & NationDef.stateFlags.NONSTATE) != 0)
            {
                stateImageDict[state.shortname].gameObject.SetActive(true);
            }

            if ((state.flags & NationDef.stateFlags.PROPORTIONAL_ELECTORAL_VOTES) != 0)
                deactivateSubstateImages(state.shortname);

            if (state.alignment <= -4)
                stateImageDict[state.shortname].color = c_Archconservative;
            else if (state.alignment <= -2)
                stateImageDict[state.shortname].color = c_Conservative;
            else if (state.alignment < 2)
                stateImageDict[state.shortname].color = c_Moderate;
            else if(state.alignment < 4)
                stateImageDict[state.shortname].color = c_Liberal;
            else
                stateImageDict[state.shortname].color = c_EliteLiberal;
        }
    }

    public void disbandButton()
    {
        PopupOption yes = new PopupOption("Yes", () =>
        {
            actions.disband();
        });

        PopupOption no = new PopupOption("No", () => { });

        List<PopupOption> options = new List<PopupOption>();

        options.Add(yes);
        options.Add(no);

        uiController.showYesNoPopup("Disbanding scatters the Liberal Crime Squad, sending all of its members into hiding, free to pursue their own lives. You will be able to observe the political situation in brief, and wait until a resolution is reached.\nIf at any time you determine that the Liberal Crime Squad will be needed again, you may return to the homeless shelter to restart the campaign.\nDo not make this decision lightly. If you do need to return to action, only the most devoted of your former members will return. <b>Disband the LCS?</b>", options);
    }

    private void deactivateSubstateImages(string state)
    {
        foreach(Image i in stateImageDict[state].GetComponentsInChildren<Image>())
        {
            i.gameObject.SetActive(false);
        }

        stateImageDict[state].gameObject.SetActive(true);
    }

    private void activateSubstateImages(string state)
    {
        foreach (Image i in stateImageDict[state].GetComponentsInChildren<Image>())
        {
            i.gameObject.SetActive(true);
        }
    }

    private Color getCandidateColor(Alignment align)
    {
        switch (align)
        {
            case Alignment.ARCHCONSERVATIVE:
                return c_Archconservative;
            case Alignment.CONSERVATIVE:
                return c_Conservative;
            case Alignment.MODERATE:
                return c_Moderate;
            case Alignment.LIBERAL:
                return c_Liberal;
            case Alignment.ELITE_LIBERAL:
                return c_EliteLiberal;
        }

        return Color.white;
    }
}
