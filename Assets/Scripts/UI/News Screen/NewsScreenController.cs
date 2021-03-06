using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.Creature;
using LCS.Engine.UI;

public class NewsScreenController : MonoBehaviour, NewsUI {

    public UIControllerImpl uiController;
    public Text t_Date;
    public Text t_PageNumber;
    public Text t_Title;

    public Font neutralFont;
    public Font guardianFont;

    public List<PaperStyle> pageStyles;

    public Sprite i_taxes_positive;
    public Sprite i_taxes_negative;

    private NewsActions actions;
    private int currentStory;
    private bool guardian;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void init(NewsActions newsActions)
    {
        actions = newsActions;
    }

    public void show()
    {
        guardian = false;

        foreach (Entity lib in MasterController.lcs.getAllMembers())
        {
            if (lib.getComponent<Liberal>().dailyActivity.type == "WRITE_GUARDIAN")
            {
                guardian = true;
                break;
            }
        }

        if (MasterController.news.stories.Exists(s => s.type == "MAJOREVENT"))
            guardian = false;

        uiController.addCurrentScreen(this);
        gameObject.SetActive(true);
        currentStory = 0;
        showStory(0);
        t_Date.text = MasterController.GetMC().currentDate.ToString("D");
        if (guardian)
        {
            t_Title.text = "The Liberal Guardian";
            t_Title.font = guardianFont;
            t_Title.fontSize = 30;
        }
        else
        {
            t_Title.text = "The Moderate Informer";
            t_Title.font = neutralFont;
            t_Title.fontSize = 40;
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

    public void nextStory()
    {
        if(MasterController.news.stories.Count > currentStory + 1)
        {
            currentStory++;
            showStory(currentStory);
        }
    }

    public void prevStory()
    {
        if (0 < currentStory)
        {
            currentStory--;
            showStory(currentStory);
        }
    }

    private void showStory(int story)
    {
        foreach (PaperStyle p in pageStyles)
        {
            p.gameObject.SetActive(false);
        }

        if(MasterController.news.stories[story].type == "MAJOREVENT")
        {
            pageStyles[2].show(MasterController.news.stories[story], MasterController.news.stories[story].page);
            pageStyles[2].gameObject.SetActive(true);
            switch (MasterController.news.stories[story].majorstorytype)
            {
                case Constants.VIEW_TAXES:
                    if (MasterController.news.stories[story].positive)
                        pageStyles[2].i_Image.sprite = i_taxes_positive;
                    else
                        pageStyles[2].i_Image.sprite = i_taxes_negative;

                    pageStyles[2].i_Image.gameObject.SetActive(true);
                    break;
                default:
                    pageStyles[2].i_Image.sprite = null;
                    pageStyles[2].i_Image.gameObject.SetActive(false);
                    break;
            }
        }
        else if (MasterController.news.stories[story].page == 1)
        {
            pageStyles[0].show(MasterController.news.stories[story], MasterController.news.stories[story].page);
            pageStyles[0].gameObject.SetActive(true);
        }
        else
        {
            pageStyles[1].show(MasterController.news.stories[story], MasterController.news.stories[story].page);
            pageStyles[1].gameObject.SetActive(true);
        }

        t_PageNumber.text = "pg. " + MasterController.news.stories[story].page;
    }

    public void refresh()
    {

    }

    public void nextScreen()
    {
        close();
        actions.nextScreen();
    }
}
