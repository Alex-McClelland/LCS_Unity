using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine.UI;
using LCS.Engine.Components.Creature;
using LCS.Engine;

public class FounderQuestionsImpl : MonoBehaviour, FounderQuestions {

    public UIControllerImpl uiController;

    public PortraitImage i_portrait;

    public InputField if_alias;

    public Button b_sex;
    public Button b_firstName;
    public Button b_lastName;

    public Text t_questionText;
    public Button b_answerA;
    public Button b_answerB;
    public Button b_answerC;
    public Button b_answerD;
    public Button b_answerE;

    public Button b_nightmare;
    public Button b_strongCCS;
    public Button b_noCCS;

    private int questionNum;
    private Entity founder;

    private bool nightmare = false;
    private bool strongCCS = false;
    private bool noCCS = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void show()
    {
        founder = MasterController.lcs.founder;

        gameObject.SetActive(true);
        uiController.addCurrentScreen(this);

        setSexText();
        setNameText();
        i_portrait.buildPortrait(founder);

        //Start on -1 so the nextQuestion method rolls it up to 0
        questionNum = -1;
        nextQuestion();
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

    public void answerQuestion(int response)
    {
        MasterController.GetMC().startQuestion(questionNum, response);
        nextQuestion();
    }

    public void changeSex()
    {
        switch (founder.getComponent<CreatureInfo>().genderConservative)
        {
            case CreatureInfo.CreatureGender.MALE:
                founder.getComponent<CreatureInfo>().genderConservative = CreatureInfo.CreatureGender.FEMALE;
                founder.getComponent<CreatureInfo>().genderLiberal = CreatureInfo.CreatureGender.FEMALE;
                break;
            case CreatureInfo.CreatureGender.FEMALE:
                founder.getComponent<CreatureInfo>().genderConservative = CreatureInfo.CreatureGender.NEUTRAL;
                founder.getComponent<CreatureInfo>().genderLiberal = CreatureInfo.CreatureGender.NEUTRAL;
                break;
            case CreatureInfo.CreatureGender.NEUTRAL:
                founder.getComponent<CreatureInfo>().genderConservative = CreatureInfo.CreatureGender.MALE;
                founder.getComponent<CreatureInfo>().genderLiberal = CreatureInfo.CreatureGender.MALE;
                break;
        }

        setSexText();
        changePortrait();
    }

    private void setSexText()
    {
        switch (founder.getComponent<CreatureInfo>().genderConservative)
        {
            case CreatureInfo.CreatureGender.FEMALE:
                b_sex.GetComponentInChildren<Text>().text = "a girl";
                break;
            case CreatureInfo.CreatureGender.MALE:
                b_sex.GetComponentInChildren<Text>().text = "a boy";
                break;
            case CreatureInfo.CreatureGender.NEUTRAL:
                b_sex.GetComponentInChildren<Text>().text = "intersex";
                break;
        }
        changeFirstName();
    }

    private void setNameText()
    {
        b_firstName.GetComponentInChildren<Text>().text = founder.getComponent<CreatureInfo>().givenName;
        b_lastName.GetComponentInChildren<Text>().text = founder.getComponent<CreatureInfo>().surname;
    }

    public void changeFirstName()
    {
        founder.getComponent<CreatureInfo>().givenName = LCS.Engine.Factories.CreatureFactory.generateGivenName(founder.getComponent<CreatureInfo>().genderConservative);
        setNameText();
    }

    public void changeLastName()
    {
        founder.getComponent<CreatureInfo>().surname = LCS.Engine.Factories.CreatureFactory.generateSurname(founder.getComponent<CreatureInfo>().genderConservative);
        setNameText();
    }

    public void changeAlias()
    {
        founder.getComponent<CreatureInfo>().alias = if_alias.text;
    }

    public void skipRemainingQuestions()
    {
        while (questionNum < 10)
        {
            answerQuestion(MasterController.GetMC().LCSRandom(5));
        }
    }

    public void changePortrait()
    {
        founder.getComponent<Portrait>().makeMyFace();
        founder.getComponent<Portrait>().forceRegen = true;

        i_portrait.buildPortrait(founder);
    }

    /*
     * 0 = Nightmare Mode
    1 = Strong CCS
    2 = No CCs
    */
    public void toggleOption(int option)
    {
        switch (option)
        {
            case 0:
                if (nightmare)
                {
                    nightmare = false;
                    b_nightmare.image.color = uiController.buttonColorOff;
                }
                else
                {
                    nightmare = true;
                    b_nightmare.image.color = uiController.buttonColorOn;
                }
                break;
            case 1:
                if (strongCCS)
                {
                    strongCCS = false;
                    b_strongCCS.image.color = uiController.buttonColorOff;
                }
                else
                {
                    strongCCS = true;
                    noCCS = false;
                    b_strongCCS.image.color = uiController.buttonColorOn;
                    b_noCCS.image.color = uiController.buttonColorOff;
                }
                break;
            case 2:
                if (noCCS)
                {
                    noCCS = false;
                    b_noCCS.image.color = uiController.buttonColorOff;
                }
                else
                {
                    noCCS = true;
                    strongCCS = false;
                    b_noCCS.image.color = uiController.buttonColorOn;
                    b_strongCCS.image.color = uiController.buttonColorOff;
                }
                break;
        }
    }

    public void nextQuestion()
    {
        questionNum++;
        switch (questionNum)
        {
            case 0:
                t_questionText.text = "The day I was born in 1984...";
                b_answerA.GetComponentInChildren<Text>().text = "(A) the Polish priest Popieluszko was kidnapped by government agents";
                b_answerB.GetComponentInChildren<Text>().text = "(B) was the 3rd anniversary of the assassination attempt on Ronald Reagan";
                b_answerC.GetComponentInChildren<Text>().text = "(C) the Macintosh was introduced";
                b_answerD.GetComponentInChildren<Text>().text = "(D) the Nobel Peace Prize went to Desmond Tutu for opposition to apartheid";
                b_answerE.GetComponentInChildren<Text>().text = "(E) the Sandanista Front won the elections in Nicaragua";
                break;
            case 1:
                t_questionText.text = "When I was bad...";
                b_answerA.GetComponentInChildren<Text>().text = "(A) my parents grounded me and hid my toys, but I knew where they put them";
                b_answerB.GetComponentInChildren<Text>().text = "(B) my father beat me. I learned to take a punch earlier than most";
                b_answerC.GetComponentInChildren<Text>().text = "(C) I was sent to my room, where I studied quietly by myself, alone";
                b_answerD.GetComponentInChildren<Text>().text = "(D) my parents argued with each other about me, but I was never punished";
                b_answerE.GetComponentInChildren<Text>().text = "(E) my father lectured me endlessly, trying to make me think like him";
                break;
            case 2:
                t_questionText.text = "In elementary school...";
                b_answerA.GetComponentInChildren<Text>().text = "(A) I was mischievous, and always up to something";
                b_answerB.GetComponentInChildren<Text>().text = "(B) I had a lot of repressed anger. I hurt animals";
                b_answerC.GetComponentInChildren<Text>().text = "(C) I was at the head of the class, and I worked very hard";
                b_answerD.GetComponentInChildren<Text>().text = "(D) I was unruly and often fought with the other children";
                b_answerE.GetComponentInChildren<Text>().text = "(E) I was the class clown. I even had some friends";
                break;
            case 3:
                t_questionText.text = "When I turned 10...";
                b_answerA.GetComponentInChildren<Text>().text = "(A) my parents divorced. Whenever I talked, they argued, so I stayed quiet";
                b_answerB.GetComponentInChildren<Text>().text = "(B) my parents divorced. Violently";
                b_answerC.GetComponentInChildren<Text>().text = "(C) my parents divorced. Acrimoniously.  I once tripped over the paperwork!";
                b_answerD.GetComponentInChildren<Text>().text = "(D) my parents divorced. Mom slept with the divorce lawyer";
                b_answerE.GetComponentInChildren<Text>().text = "(E) my parents divorced. It still hurts to read my old diary";
                break;
            case 4:
                t_questionText.text = "In junior high school...";
                b_answerA.GetComponentInChildren<Text>().text = "(A) I was into chemistry. I wanted to know what made the world tick";
                b_answerB.GetComponentInChildren<Text>().text = "(B) I played guitar in a grunge band.  We sucked, but so did life";
                b_answerC.GetComponentInChildren<Text>().text = "(C) I drew things, a lot. I was drawing a world better than this";
                b_answerD.GetComponentInChildren<Text>().text = "(D) I played violent video games at home. I was a total outcast";
                b_answerE.GetComponentInChildren<Text>().text = "(E) I was obsessed with swords, and started lifting weights";
                break;
            case 5:
                t_questionText.text = "Things were getting really bad...";
                b_answerA.GetComponentInChildren<Text>().text = "(A) when I stole my first car. I got a few blocks before I totaled it";
                b_answerB.GetComponentInChildren<Text>().text = "(B) and I went to live with my dad. He had been in Nam and he still drank";
                b_answerC.GetComponentInChildren<Text>().text = "(C) and I went completely goth. I had no friends and made costumes by myself";
                b_answerD.GetComponentInChildren<Text>().text = "(D) when I was sent to religious counseling, just stressing me out more";
                b_answerE.GetComponentInChildren<Text>().text = "(E) and I tried being a teacher's assistant. It just made me a target";
                break;
            case 6:
                t_questionText.text = "I knew it had reached a crescendo when...";
                b_answerA.GetComponentInChildren<Text>().text = "(A) I stole a cop car when I was only 14. I went to juvie for 6 months";
                b_answerB.GetComponentInChildren<Text>().text = "(B) my step mom shot her ex-husband, my dad, with a shotgun. She got off";
                b_answerC.GetComponentInChildren<Text>().text = "(C) I tried wrestling for a quarter, desperate to fit in";
                b_answerD.GetComponentInChildren<Text>().text = "(D) I got caught making out, and now I needed to be 'cured' of homosexuality";
                b_answerE.GetComponentInChildren<Text>().text = "(E) I resorted to controlling people. Had my own clique of outcasts";
                break;
            case 7:
                t_questionText.text = "I was only 15 when I ran away, and...";
                b_answerA.GetComponentInChildren<Text>().text = "(A) I started robbing houses: rich people only. I was fed up with their crap";
                b_answerB.GetComponentInChildren<Text>().text = "(B) I hung out with thugs and beat the shit out of people";
                b_answerC.GetComponentInChildren<Text>().text = "(C) I got a horrible job working fast food, smiling as people fed the man";
                b_answerD.GetComponentInChildren<Text>().text = "(D) I let people pay me for sex. I needed the money to survive";
                b_answerE.GetComponentInChildren<Text>().text = "(E) I volunteered for a left-wing candidate. It wasn't *real*, though, you know?";
                break;
            case 8:
                t_questionText.text = "Life went on. On my 18th birthday...";
                b_answerA.GetComponentInChildren<Text>().text = "(A) I got my hands on a sports car. The owner must have been pissed";
                b_answerB.GetComponentInChildren<Text>().text = "(B) I bought myself an assault rifle";
                b_answerC.GetComponentInChildren<Text>().text = "(C) I celebrated. I'd saved a thousand bucks!";
                b_answerD.GetComponentInChildren<Text>().text = "(D) I went to a party and met a cool law student. We've been dating since";
                b_answerE.GetComponentInChildren<Text>().text = "(E) I managed to acquire secret maps of several major buildings downtown";
                break;
            case 9:
                t_questionText.text = "For the past decade, I've been...";
                b_answerA.GetComponentInChildren<Text>().text = "(A) stealing from Corporations. I know they're still keeping more secrets";
                b_answerB.GetComponentInChildren<Text>().text = "(B) a violent criminal. Nothing can change me, or stand in my way";
                b_answerC.GetComponentInChildren<Text>().text = "(C) taking college courses. I can see how much the country needs help";
                b_answerD.GetComponentInChildren<Text>().text = "(D) surviving alone, just like anyone. But we can't go on like this";
                b_answerE.GetComponentInChildren<Text>().text = "(E) writing my manifesto and refining my image. I'm ready to lead";
                break;
            case 10:
                close();
                if (nightmare) MasterController.GetMC().gameFlags |= MasterController.GameFlags.NIGHTMARE;
                if (strongCCS) MasterController.GetMC().gameFlags |= MasterController.GameFlags.ACTIVE_CCS;
                if (noCCS) MasterController.GetMC().gameFlags |= MasterController.GameFlags.NO_CCS;
                MasterController.GetMC().finishQuestions();
                break;
        }
    }
}
