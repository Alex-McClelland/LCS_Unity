using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LCS.Engine;
using LCS.Engine.UI;
using System.Xml;

public class HighScoreImpl : MonoBehaviour, HighScorePage {

    public UIControllerImpl uiController;

    public HighScoreEntry p_HighScoreEntry;
    public HighScoreEntry universalStats;
    public Transform highScoreTableContent;

    private List<GameObject> objectList;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void show()
    {
        XmlDocument scores = GameData.getData().loadHighScores();
        objectList = new List<GameObject>();

        int totalRecruits = 0;
        int totalMartyrs = 0;
        int totalKills = 0;
        int totalKidnappings = 0;
        int totalMoneyTaxed = 0;
        int totalMoneySpent = 0;
        int totalFlagsBought = 0;
        int totalFlagsBurned = 0;

        if (scores.DocumentElement != null)
        {
            foreach (XmlNode node in scores.DocumentElement.ChildNodes)
            {
                string slogan = node.SelectSingleNode("slogan").InnerText;
                string fate = node.SelectSingleNode("fate").InnerText;
                int recruits = int.Parse(node.SelectSingleNode("recruits").InnerText);
                int martyrs = int.Parse(node.SelectSingleNode("martyrs").InnerText);
                int kills = int.Parse(node.SelectSingleNode("kills").InnerText);
                int kidnappings = int.Parse(node.SelectSingleNode("kidnappings").InnerText);
                int moneyTaxed = int.Parse(node.SelectSingleNode("moneyTaxed").InnerText);
                int moneySpent = int.Parse(node.SelectSingleNode("moneySpent").InnerText);
                int flagsBought = int.Parse(node.SelectSingleNode("flagsBought").InnerText);
                int flagsBurned = int.Parse(node.SelectSingleNode("flagsBurned").InnerText);

                totalRecruits += recruits;
                totalMartyrs += martyrs;
                totalKills += kills;
                totalKidnappings += kidnappings;
                totalMoneyTaxed += moneyTaxed;
                totalMoneySpent += moneySpent;
                totalFlagsBought += flagsBought;
                totalFlagsBurned += flagsBurned;

                HighScoreEntry newEntry = Instantiate(p_HighScoreEntry);
                newEntry.slogan.text = slogan;
                newEntry.fate.text = fate;
                newEntry.recruitCount.text = recruits + "";
                newEntry.martyrCount.text = martyrs + "";
                newEntry.killCount.text = kills + "";
                newEntry.kidnapCount.text = kidnappings + "";
                newEntry.taxCount.text = moneyTaxed + "";
                newEntry.spendCount.text = moneySpent + "";
                newEntry.flagBuyCount.text = flagsBought + "";
                newEntry.flagBurnCount.text = flagsBurned + "";

                newEntry.transform.SetParent(highScoreTableContent, false);
                objectList.Add(newEntry.gameObject);
            }
        }

        universalStats.recruitCount.text = totalRecruits + "";
        universalStats.martyrCount.text = totalMartyrs + "";
        universalStats.killCount.text = totalKills + "";
        universalStats.kidnapCount.text = totalKidnappings + "";
        universalStats.taxCount.text = totalMoneyTaxed + "";
        universalStats.spendCount.text = totalMoneySpent + "";
        universalStats.flagBuyCount.text = totalFlagsBought + "";
        universalStats.flagBurnCount.text = totalFlagsBurned + "";

        gameObject.SetActive(true);
        uiController.addCurrentScreen(this);
    }

    public void hide()
    {
        gameObject.SetActive(false);
    }

    public void refresh()
    {

    }

    public void close()
    {
        hide();
        foreach(GameObject o in objectList)
        {
            Destroy(o);
        }
        uiController.removeCurrentScreen(this);
    }
}
