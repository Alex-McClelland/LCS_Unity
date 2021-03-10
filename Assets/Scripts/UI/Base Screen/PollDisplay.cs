﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Creature;
using LCS.Engine.Data;

public class PollDisplay : MonoBehaviour {

    public PollIssue p_PollIssue;

    public Text t_PresidentialApproval;
    public Text t_PublicTopIssue;

    public Transform issueContainer;

    public Dictionary<string, PollIssue> issues;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Activate()
    {
        if (issues == null) issues = new Dictionary<string, PollIssue>();
        GameData g = GameData.getData();

        foreach (Public.PollData data in MasterController.generalPublic.pollData.Values)
        {
            PollIssue issue;

            string issueText = "";
            foreach (ConditionalName text in GameData.getData().viewList[data.def].issueText)
            {
                if (MasterController.GetMC().testCondition(text.condition))
                {
                    issueText = text.name;
                    break;
                }
            }

            if (!issues.ContainsKey(data.def))
            {
                issue = Instantiate(p_PollIssue);
                issue.transform.SetParent(issueContainer, false);
                issue.t_Issue.text = issueText;
                issues.Add(data.def, issue);               
            }
            else
            {
                issue = issues[data.def];
                issue.t_Issue.text = issueText;
            }

            Color viewColor;

            if(data.age > 20)
            {
                viewColor = Color.gray;
                
                issue.t_PublicInterest.text = g.translationList["BASE_poll_interest_unknown"];
                issue.t_Percent.text = "??%";
                issue.t_Error.text = g.translationList["BASE_poll_error_na"];
                issue.t_Age.text = g.translationList["BASE_poll_age_old"];
            }
            else
            {
                switch (data.publicInterest)
                {
                    case Public.PollData.PublicInterest.VERY_HIGH:
                        issue.t_PublicInterest.text = g.translationList["BASE_poll_interest_very_high"];
                        break;
                    case Public.PollData.PublicInterest.HIGH:
                        issue.t_PublicInterest.text = g.translationList["BASE_poll_interest_high"];
                        break;
                    case Public.PollData.PublicInterest.MODERATE:
                        issue.t_PublicInterest.text = g.translationList["BASE_poll_interest_moderate"];
                        break;
                    case Public.PollData.PublicInterest.LOW:
                        issue.t_PublicInterest.text = g.translationList["BASE_poll_interest_low"];
                        break;
                    case Public.PollData.PublicInterest.NONE:
                        issue.t_PublicInterest.text = g.translationList["BASE_poll_interest_none"];
                        break;
                    default:
                        issue.t_PublicInterest.text = g.translationList["BASE_poll_interest_unknown"];
                        break;
                }

                if (data.percent < 10) viewColor = Color.red;
                else if (data.percent < 30) viewColor = Color.magenta;
                else if (data.percent < 70) viewColor = Color.yellow;
                else if (data.percent < 90) viewColor = Color.cyan;
                else viewColor = Color.green;

                issue.t_Percent.text = data.percent + "%";
                issue.t_Error.text = "+/- " + data.noise;
                issue.t_Age.text = data.age + (data.age>1?" days":" day");
            }

            issue.t_Issue.color = viewColor;
            issue.t_PublicInterest.color = viewColor;
            issue.t_Percent.color = viewColor;
            issue.t_Error.color = viewColor;
            issue.t_Age.color = viewColor;
        }

        t_PresidentialApproval.text = MasterController.generalPublic.PresidentApprovalRating/10 + "% " + g.translationList["BASE_presidential_approval"] +  " ";

        string colorString = "<color=white>";

        switch (MasterController.government.president.getComponent<Politician>().alignment)
        {
            case Alignment.ARCHCONSERVATIVE:
                colorString = "<color=red>";
                break;
            case Alignment.CONSERVATIVE:
                colorString = "<color=magenta>";
                break;
            case Alignment.MODERATE:
                colorString = "<color=yellow>";
                break;
            case Alignment.LIBERAL:
                colorString = "<color=cyan>";
                break;
            case Alignment.ELITE_LIBERAL:
                colorString = "<color=lime>";
                break;
        }

        t_PresidentialApproval.text += colorString + g.translationList["GOVERNMENT_president"] + " " + MasterController.government.president.getComponent<CreatureInfo>().getName() + "</color>.";

        string topIssue = "";

        foreach(string s in MasterController.generalPublic.PublicInterest.Keys)
        {
            if (s == "LIBERALCRIMESQUAD" || s == "LIBERALCRIMESQUADPOS") continue;

            if(MasterController.generalPublic.PublicInterest[s] > 0)
            {
                if (topIssue == "") topIssue = s;
                else if (MasterController.generalPublic.PublicInterest[s] >
                    MasterController.generalPublic.PublicInterest[topIssue]) topIssue = s;
            }
        }

        if (topIssue == "") t_PublicTopIssue.text = g.translationList["BASE_no_issues"];
        else
        {
            t_PublicTopIssue.text = g.translationList["BASE_top_issue"] + " ";
            if (MasterController.generalPublic.PublicOpinion[topIssue] > 50)
            {
                foreach (ConditionalName text in GameData.getData().viewList[topIssue].liberalText)
                {
                    if (MasterController.GetMC().testCondition(text.condition))
                    {
                        t_PublicTopIssue.text += text.name;
                        break;
                    }
                }
            }
            else
            {
                foreach (ConditionalName text in GameData.getData().viewList[topIssue].conservativeText)
                {
                    if (MasterController.GetMC().testCondition(text.condition))
                    {
                        t_PublicTopIssue.text += text.name;
                        break;
                    }
                }
            }
        }
    }
}
