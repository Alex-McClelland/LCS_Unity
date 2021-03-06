using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Location;
using LCS.Engine.Components.Creature;

public class PaperStyle : MonoBehaviour {

    public Text t_Headline;
    public Text t_MainStory;
    public List<Text> t_Columns;
    public Image i_Image;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void show(News.NewsStory story, int pageNumber)
    {
        t_Headline.text = story.headline;
        t_MainStory.text = story.text;
        fillColumns(pageNumber, t_Columns);
    }

    private void fillColumns(int pageNumber, List<Text> columns)
    {
        if (columns.Count == 0) return;

        foreach(Text text in columns)
        {
            if(text != t_MainStory) text.text = "";
        }

        MasterController mc = MasterController.GetMC();
        List<string> adList = new List<string>();
        List<string> fillerArticles = new List<string>();
        System.Random rand = new System.Random(mc.currentDate.Year + (pageNumber * 10000) + (mc.currentDate.Day * 1000000) + (mc.currentDate.Month * 100000000));

        adList.Add("No Fee Consignment Program.\nCall for Details.");
        adList.Add("Fine Leather Chairs\nSpecial Purchase\n\nNow $" + (rand.Next(201) + 400));
        adList.Add("Paris Flea Market Sale!\n50% Off");
        adList.Add("Quality Pre-Owned Vehicles!\n" + (mc.currentDate.Year - rand.Next(15)) + " Lexus");
        adList.Add("Spa\nHealth, Beauty, and Fitness\n7 Days a Week");
        adList.Add("Need Credit? No References? Call Now!\n" + (rand.Next(900) + 100) + "-" + (rand.Next(9000) + 1000));
        adList.Add("SWF seeks SBM for PGDDHBM");
        adList.Add("BUY BUY BUY\nSALE SALE SALE\nMARRY AND REPRODUCE");
        adList.Add("Had an Accident?\bNeed a Lawyer?\nCall Now!\n" + (rand.Next(900) + 100) + "-" + (rand.Next(9000) + 1000));
        adList.Add("Plumbing Repairs\nDone Cheap");

        fillerArticles.Add("<b>Local Typesetter Gets Lazy</b>\n\nLorem ipsum dolor sit amet, consectetur adipiscing elit. Proin vitae pharetra magna. Nunc sed hendrerit nisi. Integer at ligula vitae ipsum porta placerat vitae ac ipsum. Aenean aliquet ante ipsum, quis venenatis lacus pharetra sed. Sed id massa condimentum dolor porttitor rutrum. Suspendisse et odio urna. Suspendisse fringilla purus sed interdum finibus.");
        //The following are Onion articles which might be a copyright issue, so if you can come up with something funny feel free to replace them.
        string name = LCS.Engine.Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.MALE, rand.Next()) + " " + LCS.Engine.Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL, rand.Next());
        fillerArticles.Add("<b>Eviction Notice All Business</b>\n\nShocked that the personal correspondence would take such a cold and impersonal tone, delinquent tenant " + name + ", " + (rand.Next(17) + 18) + " found the eviction notice posted on the door of his apartment to be disturbingly all business. \"I thought they would at least give me a ‘good morning’ before getting down to brass tacks, but ‘to whom it may concern’ makes it sound like I may not even care about getting kicked out of my own place\"");
        name = LCS.Engine.Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.MALE, rand.Next()) + " " + LCS.Engine.Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL, rand.Next());
        fillerArticles.Add("<b>Curiosity Rover Finds 5 Bucks On Mars</b>\n\nStunned by both the sheer good fortune of their discovery and its implications for future exploration, scientists at NASA confirmed Friday that the Curiosity Rover had found five bucks in the red dust of Mars’ Gale Crater. \"This is unbelievable—five whole American dollars!\" said program director " + name + ".");
        fillerArticles.Add("<b>Genealogists Find 99% Of People Not Related To Anyone Cool</b>\n\nIn a breakthrough finding that could reshape the understanding of human ancestry, genealogists from the Federation of Genealogical Societies published a study Friday revealing that 99 percent of people are not related to anyone cool. \"According to data compiled from hundreds of research institutions worldwide, only about 1 percent of humans ever to live have been related to anyone remotely fun or interesting\" a report published in the journal FORUM read in part, adding that advancements in DNA testing enabled genealogists to gain the most comprehensive picture of how few people are in any way related to an actually cool person.");
        name = LCS.Engine.Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.MALE, rand.Next()) + " " + LCS.Engine.Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL, rand.Next());
        fillerArticles.Add("<b>Himalayan Goat Dies Following Failed Everest Climb</b>\n\nConfirming the worst fears of friends and family, authorities announced Thursday that a Himalayan goat that went missing 10 days ago on the treacherous southwest face of Mount Everest has died following a failed ascent. \"At approximately 8:30 a.m. this morning, a group of Canadian climbers discovered the body of a 7-year-old male Himalayan tahr; we can now confirm that it is indeed Ngodap Goat,\" said Everest Search and Rescue team leader " + name + ", noting that unseasonably cold temperatures, coupled with the goat’s refusal to carry supplemental oxygen, may have contributed to a tragic outcome that authorities are attributing to caprine error. \"We can take some solace in knowing that the young buck died doing what he loved.\"");
        name = LCS.Engine.Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.MALE, rand.Next()) + " " + LCS.Engine.Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL, rand.Next());
        string name2 = LCS.Engine.Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.FEMALE, rand.Next());
        fillerArticles.Add("<b>‘Cooking Together Is So Fun,’ Says Man Correcting Girlfriend’s Every Knife Cut</b>\n\nStepping back to appreciate the moment during a relaxing \"couple’s night in,\" local boyfriend " + name + ", " + (rand.Next(17) + 18) + ", exclaimed Tuesday to his girlfriend, " + name2 + ", \"Cooking together is so fun!\" before resuming his practice of meticulously correcting every single one of her knife cuts. \"I mean, how nice is this? Just you, me, and some…unevenly julienned carrots, c’mon, " + name2 + ", you really have to square off that carrot first\"");
        name = LCS.Engine.Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.MALE, rand.Next()) + " " + LCS.Engine.Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL, rand.Next());
        fillerArticles.Add("<b>Legendary Reclusive Author Has Never Published Single Piece Of Writing</b>\n\nHailing his totally nonexistent body of work as an \"act of pure genius\" literary experts at Indiana University on Monday praised legendary author " + name + ", a recluse who has never published a single piece of writing.");

        int adCount = 0;

        //The deeper into the paper we are, the more ads there will be.
        if (pageNumber > 1) adCount = 2;
        if (pageNumber > 10) adCount = 5;
        if (pageNumber > 20) adCount = 8;

        for(int i = 0; i < adCount; i++)
        {
            int selection = rand.Next(adList.Count);
            fillerArticles.Add(adList[selection]);
            adList.RemoveAt(selection);
        }

        bool storyDividerAdded = false;

        while(fillerArticles.Count > 0)
        {
            int selection = rand.Next(fillerArticles.Count);
            if(columns[fillerArticles.Count % columns.Count] == t_MainStory && !storyDividerAdded)
            {
                columns[fillerArticles.Count % columns.Count].text += "\n_________________\n\n";
                storyDividerAdded = true;
            }
            columns[fillerArticles.Count % columns.Count].text += fillerArticles[selection] + "\n_________________\n\n";
            fillerArticles.RemoveAt(selection);
        }
    }
}
