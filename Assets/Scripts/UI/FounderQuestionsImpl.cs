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
        MasterController mc = MasterController.GetMC();

        switch (founder.getComponent<CreatureInfo>().genderConservative)
        {
            case CreatureInfo.CreatureGender.FEMALE:
                b_sex.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_sex_female");
                break;
            case CreatureInfo.CreatureGender.MALE:
                b_sex.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_sex_male");
                break;
            case CreatureInfo.CreatureGender.NEUTRAL:
                b_sex.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_sex_neutral");
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
        MasterController mc = MasterController.GetMC();

        questionNum++;
        switch (questionNum)
        {
            case 0:
                t_questionText.text = mc.getTranslation("QUESTIONS_q1_text");
                b_answerA.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q1_a");
                b_answerB.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q1_b");
                b_answerC.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q1_c");
                b_answerD.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q1_d");
                b_answerE.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q1_e");
                break;
            case 1:
                t_questionText.text = mc.getTranslation("QUESTIONS_q2_text");
                b_answerA.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q2_a");
                b_answerB.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q2_b");
                b_answerC.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q2_c");
                b_answerD.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q2_d");
                b_answerE.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q2_e");
                break;
            case 2:
                t_questionText.text = mc.getTranslation("QUESTIONS_q3_text");
                b_answerA.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q3_a");
                b_answerB.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q3_b");
                b_answerC.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q3_c");
                b_answerD.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q3_d");
                b_answerE.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q3_e");
                break;
            case 3:
                t_questionText.text = mc.getTranslation("QUESTIONS_q4_text");
                b_answerA.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q4_a");
                b_answerB.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q4_b");
                b_answerC.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q4_c");
                b_answerD.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q4_d");
                b_answerE.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q4_e");
                break;
            case 4:
                t_questionText.text = mc.getTranslation("QUESTIONS_q5_text");
                b_answerA.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q5_a");
                b_answerB.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q5_b");
                b_answerC.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q5_c");
                b_answerD.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q5_d");
                b_answerE.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q5_e");
                break;
            case 5:
                t_questionText.text = mc.getTranslation("QUESTIONS_q6_text");
                b_answerA.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q6_a");
                b_answerB.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q6_b");
                b_answerC.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q6_c");
                b_answerD.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q6_d");
                b_answerE.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q6_e");
                break;
            case 6:
                t_questionText.text = mc.getTranslation("QUESTIONS_q7_text");
                b_answerA.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q7_a");
                b_answerB.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q7_b");
                b_answerC.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q7_c");
                b_answerD.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q7_d");
                b_answerE.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q7_e");
                break;
            case 7:
                t_questionText.text = mc.getTranslation("QUESTIONS_q8_text");
                b_answerA.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q8_a");
                b_answerB.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q8_b");
                b_answerC.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q8_c");
                b_answerD.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q8_d");
                b_answerE.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q8_e");
                break;
            case 8:
                t_questionText.text = mc.getTranslation("QUESTIONS_q9_text");
                b_answerA.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q9_a");
                b_answerB.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q9_b");
                b_answerC.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q9_c");
                b_answerD.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q9_d");
                b_answerE.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q9_e");
                break;
            case 9:
                t_questionText.text = mc.getTranslation("QUESTIONS_q10_text");
                b_answerA.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q10_a");
                b_answerB.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q10_b");
                b_answerC.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q10_c");
                b_answerD.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q10_d");
                b_answerE.GetComponentInChildren<Text>().text = mc.getTranslation("QUESTIONS_q10_e");
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
