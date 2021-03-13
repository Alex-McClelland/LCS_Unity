using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Location;
using LCS.Engine.Components.Item;
using LCS.Engine.UI;
using LCS.Engine.Data;

public class InfoScreenController : MonoBehaviour, CharInfo {

    private List<GameObject> generatedObjects;
    private List<GameObject> squadButtons;
    private List<GameObject> baseButtons;
    private List<GameObject> interrogationButtons;
    private List<GameObject> vehicleButtons;

    public UIControllerImpl uiController;

    public Entity selectedChar { get; set; }
    
    public Button p_MenuButton;

    public Text t_Name;
    public Text t_Juice;    
    public InputField t_Alias;
    public Button b_Gender;
    public Text t_Birthday;
    public Text t_TypeName;
    public Text t_JoinDate;  
    public Text t_Wanted;
    public PortraitImage i_Portrait;
    public Text t_SleeperInfiltration;

    public GameObject i_Wheelchair;

    public GameObject HumanHealth;
    public Image i_Head_Human;
    public Image i_Torso_Human;
    public Image i_ArmLeft_Human;
    public Image i_ArmRight_Human;
    public Image i_LegLeft_Human;
    public Image i_LegRight_Human;

    public GameObject DogHealth;
    public Image i_Head_Dog;
    public Image i_Torso_Dog;
    public Image i_ArmLeft_Dog;
    public Image i_ArmRight_Dog;
    public Image i_LegLeft_Dog;
    public Image i_LegRight_Dog;

    public Text t_HealthStatus;
    public Color c_Fine;
    public Color c_Scarred;
    public Color c_Injured;
    public Color c_InjuredVital;
    public Color c_SeveredNasty;
    public Color c_Severed;

    public InventoryMenu inventoryMenu;
    public Button b_Reload;
    public ItemButton b_Weapon;
    public ItemButton b_Armor;
    public ItemButton b_Clip;
    public ItemButton b_Vehicle;

    public Button b_Base;
    public Button b_Activity;
    public Button b_Squad;
    public InputField t_NewSquad;
    public GameObject FollowerBox;
    public GameObject EnlightenedBox;
    public GameObject LoverBox;

    public Transform AttributeList;
    public GameObject attribute;

    public Transform SkillList;
    public GameObject skill;

    public GameObject SkillsBlackout;
    public Transform SkillColumn1;
    public Transform SkillColumn2;
    public Transform SkillColumn3;
    public SkillDesc p_SkillDesc;

    public GameObject CriminalRecordBlackout;
    public Transform TreasonColumn;
    public Transform FelonyColumn;
    public Transform MisdemeanorColumn;
    public Text t_Sentence;
    public Text t_TimeServed;

    public GameObject MakeClothingBlackout;
    public MakeClothingLine p_MakeClothingLine;
    public Transform MakeClothingContent;

    public GameObject debugBox;
    public Text t_Heat;
    public Text t_Confessions;
    public Text t_Scariness;

    public GameObject arrowBox;
    public Button b_up;
    public Button b_down;
    public Button b_right;
    public Button b_left;

    public GameObject ScreenDim;
    public GameObject ActivityMenu;
    public GameObject ActivismMenu;
    public GameObject FundraisingMenu;
    public GameObject AcquisitionMenu;
    public GameObject TeachingMenu;
    public GameObject HealingMenu;
    public GameObject LearningMenu;
    public GameObject InterrogationMenu;
    public GameObject SquadMenu;
    public GameObject BaseMenu;
    public GameObject SleeperMenu;
    public GameObject SleeperAdvocacyMenu;
    public GameObject SleeperEspionageMenu;
    public GameObject InterrogationActionMenu;
    public GameObject StealCarMenu;

    public Text t_Interrogator;
    public Text t_Rapport;
    public Text t_DaysCaptive;

    public Button b_Prostitution;
    public Button b_Interrogate;
    public Button b_DumpBodies;
    public Button b_LiberalGuardian;
    public Button b_SleeperRecruit;
    public Button b_Wheelchair;
    public Button b_FireLiberal;

    private CharInfoActions actions;

	// Use this for initialization
	void Start () {
        
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void init(CharInfoActions actions)
    {
        this.actions = actions;
    }

    public void show(Entity selectedChar)
    {
        gameObject.SetActive(true);
        generatedObjects = new List<GameObject>();

        this.selectedChar = selectedChar;

        hideAllMenus();
        ScreenDim.SetActive(false);
        inventoryMenu.gameObject.SetActive(false);
        clearMouseOverText();
        i_Portrait.buildPortrait(selectedChar);

        b_Squad.GetComponent<MouseOverText>().mouseOverText = "";
        b_Base.GetComponent<MouseOverText>().mouseOverText = MasterController.GetMC().getTranslation("INFO_mouseover_base");
        b_Activity.GetComponent<MouseOverText>().mouseOverText = "";

        populateInfo();
        setButtonInteractivity();

        squadButtons = new List<GameObject>();
        baseButtons = new List<GameObject>();
        interrogationButtons = new List<GameObject>();
        vehicleButtons = new List<GameObject>();
        if (selectedChar.hasComponent<Liberal>())
        {
            populateLiberalInfo();
            buildSquadMenu();
            buildBaseMenu();
            buildInterrogationMenu();
            buildVehicleMenu();
            arrowBox.SetActive(true);
            if (selectedChar.getComponent<Liberal>().leader == null)
                b_up.interactable = false;
            else
                b_up.interactable = true;

            if (selectedChar.getComponent<Liberal>().subordinates.Count == 0)
                b_down.interactable = false;
            else
                b_down.interactable = true;

            if(MasterController.GetMC().phase != MasterController.Phase.BASE)
            {
                b_up.interactable = false;
                b_down.interactable = false;
            }
        }
        else
        {
            FollowerBox.SetActive(false);
            LoverBox.SetActive(false);
            arrowBox.SetActive(false);
            t_JoinDate.text = "";
        }
    }

    public void hide()
    {
        foreach(GameObject a in generatedObjects)
        {
            Destroy(a);
        }

        foreach(GameObject a in squadButtons)
        {
            Destroy(a);
        }

        foreach (GameObject a in baseButtons)
        {
            Destroy(a);
        }

        foreach(GameObject a in interrogationButtons)
        {
            Destroy(a);
        }

        foreach(GameObject a in vehicleButtons)
        {
            Destroy(a);
        }

        gameObject.SetActive(false);
    }

    private void populateInfo()
    {
        MasterController mc = MasterController.GetMC();

        CreatureInfo info = selectedChar.getComponent<CreatureInfo>();
        t_Name.text = info.givenName + " " + info.surname;
        t_Alias.text = info.alias;
        switch (info.genderLiberal)
        {
            case CreatureInfo.CreatureGender.FEMALE:
                b_Gender.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("GENDER_female");
                break;
            case CreatureInfo.CreatureGender.MALE:
            case CreatureInfo.CreatureGender.WHITEMALEPATRIARCH:
                b_Gender.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("GENDER_male");
                break;
            case CreatureInfo.CreatureGender.NEUTRAL:
                b_Gender.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("GENDER_neutral");
                break;
        }

        CreatureBase cBase = selectedChar.getComponent<CreatureBase>();

        List<CreatureBase.CreatureAttribute> attributes = new List<CreatureBase.CreatureAttribute>(cBase.BaseAttributes.Values);
        List<CreatureBase.Skill> skills = new List<CreatureBase.Skill>(cBase.Skills.Values);
        skills.Sort();

        for (int i = 0; i < cBase.BaseAttributes.Count; i++)
        {
            GameObject newAttribute = Instantiate(attribute);
            generatedObjects.Add(newAttribute);
            newAttribute.transform.SetParent(AttributeList, false);
            newAttribute.transform.localPosition = new Vector3(0, i * -20, 0);
            newAttribute.GetComponent<AttributeDisplay>().SetAttribute(GameData.getData().attributeList[attributes[i].Type].name, attributes[i].getModifiedValue(), attributes[i].Level);
        }

        for (int i = 0; i < 6; i++)
        {
            GameObject newSkill = Instantiate(skill);
            generatedObjects.Add(newSkill);
            newSkill.transform.SetParent(SkillList, false);
            newSkill.transform.localPosition = new Vector3(0, i * -20, 0);
            newSkill.GetComponent<SkillDisplay>().SetSkill(skills[i]);
        }

        foreach(ItemDef item in GameData.getData().itemList.Values)
        {
            if (!item.components.ContainsKey("armor")) continue;
            Government gov = MasterController.government;

            ItemDef.ArmorDef def = (ItemDef.ArmorDef) item.components["armor"];
            //Can't craft Deathsquad uniforms if Deathsquad doesn't currently exist.
            if ((def.flags & ItemDef.ArmorFlags.DEATHSQUAD) != 0 &&
                (gov.laws[Constants.LAW_POLICE].alignment != Alignment.ARCHCONSERVATIVE ||
                gov.laws[Constants.LAW_DEATH_PENALTY].alignment != Alignment.ARCHCONSERVATIVE))
                continue;

            //make_difficulty of 0 means item can't be made by Liberals
            if (def.make_difficulty == 0) continue;

            MakeClothingLine line = Instantiate(p_MakeClothingLine);
            generatedObjects.Add(line.gameObject);
            line.transform.SetParent(MakeClothingContent, false);
            line.b_Name.GetComponentInChildren<Text>().text = MasterController.GetMC().isFuture()?item.nameFuture:item.name;
            line.b_Name.onClick.AddListener(() => { selectActivity("MAKE_CLOTHING " + item.type); hideMakeClothing(); });
            line.t_Price.text = "$" + def.make_price;

            int difficulty = 3 + def.make_difficulty - selectedChar.getComponent<CreatureBase>().Skills[Constants.SKILL_TAILORING].level;
            if (difficulty < 0) difficulty = 0;

            switch (difficulty)
            {
                case 0:
                    line.t_Difficulty.text = "<color=lime>" + mc.getTranslation("INFO_make_simple");
                    break;
                case 1:
                    line.t_Difficulty.text = "<color=cyan>" + mc.getTranslation("INFO_make_very_easy");
                    break;
                case 2:
                    line.t_Difficulty.text = "<color=teal>" + mc.getTranslation("INFO_make_easy");
                    break;
                case 3:
                    line.t_Difficulty.text = "<color=blue>" + mc.getTranslation("INFO_make_below_average");
                    break;
                case 4:
                    line.t_Difficulty.text = "<color=white>" + mc.getTranslation("INFO_make_average");
                    break;
                case 5:
                    line.t_Difficulty.text = "<color=grey>" + mc.getTranslation("INFO_make_above_average");
                    break;
                case 6:
                    line.t_Difficulty.text = "<color=yellow>" + mc.getTranslation("INFO_make_hard");
                    break;
                case 7:
                    line.t_Difficulty.text = "<color=purple>" + mc.getTranslation("INFO_make_very_hard");
                    break;
                case 8:
                    line.t_Difficulty.text = "<color=magenta>" + mc.getTranslation("INFO_extremely_difficult");
                    break;
                case 9:
                    line.t_Difficulty.text = "<color=maroon>" + mc.getTranslation("INFO_make_nearly_impossible");
                    break;
                default:
                    line.t_Difficulty.text = "<color=red>" + mc.getTranslation("INFO_make_impossible");
                    break;
            }

            line.t_Difficulty.text += "</color>";
        }

        skills.Sort(delegate (CreatureBase.Skill a, CreatureBase.Skill b)
        {
            return a.type.CompareTo(b.type);
        });

        for (int i = 0; i < skills.Count; i++)
        {
            SkillDesc skillDesc = Instantiate(p_SkillDesc);
            generatedObjects.Add(skillDesc.gameObject);
            skillDesc.skillName.text = GameData.getData().skillList[skills[i].type].name;
            int skillPercent = (int)((skills[i].experience / (100f + 10 * skills[i].level)) * 100f);
            skillDesc.skillValue.text = skills[i].level + (skillPercent < 10 ? ".0" : ".") + skillPercent + "  /  " + skills[i].associatedAttribute.getModifiedValue() + ".00";

            if (skills[i].level < 1)
            {
                skillDesc.skillName.color = Color.gray;
                skillDesc.skillValue.color = Color.gray;
            }
            else if (skills[i].level >= skills[i].associatedAttribute.getModifiedValue())
            {
                skillDesc.skillName.color = Color.cyan;
                skillDesc.skillValue.color = Color.cyan;
            }

            if (GameData.getData().skillList[skills[i].type].category == "activism")
            {
                skillDesc.transform.SetParent(SkillColumn1, false);
            }
            else if (GameData.getData().skillList[skills[i].type].category == "infiltration")
            {
                skillDesc.transform.SetParent(SkillColumn2, false);
            }
            else if (GameData.getData().skillList[skills[i].type].category == "combat")
            {
                skillDesc.transform.SetParent(SkillColumn3, false);
            }
        }

        CriminalRecord record = selectedChar.getComponent<CriminalRecord>();
        List<string> crimeList = new List<string>(record.CrimesWanted.Keys);

        if (!uiController.DebugMode) debugBox.SetActive(false);
        else debugBox.SetActive(true);

        t_Heat.text = "HEAT: " + record.Heat;
        t_Confessions.text = "CONFESSIONS: " + record.Confessions;
        t_Scariness.text = "SCARINESS: " + record.getScareFactor();

        crimeList.Sort();

        int mostWantedSeverity = -1;
        string mostWantedCrimeDef = "";
        CrimeDef.CrimeVariant mostWantedCrime = null;

        for (int i = 0; i < crimeList.Count; i++)
        {
            //If the appearcondition is currently false, don't display this crime
            if (!MasterController.GetMC().testCondition(GameData.getData().crimeList[crimeList[i]].appearCondition))
                continue;

            CrimeDef.CrimeVariant variant = null;

            if (GameData.getData().crimeList[crimeList[i]].variants.Count > 1)
            {
                foreach (CrimeDef.CrimeVariant testVariant in GameData.getData().crimeList[crimeList[i]].variants)
                {
                    if (MasterController.GetMC().testCondition(testVariant.condition))
                    {
                        variant = testVariant;
                        break;
                    }
                }
            }
            else
            {
                variant = GameData.getData().crimeList[crimeList[i]].variants[0];
            }

            //This shouldn't happen but if it does, I guess don't display in this case either.
            if (variant == null)
            {
                MasterController.GetMC().addErrorMessage("No valid variants for crime " + crimeList[i]);
                continue;
            }            
            
            if (record.CrimesWanted[crimeList[i]] > 0)
            {
                if (variant.severity * record.CrimesWanted[crimeList[i]] > mostWantedSeverity)
                {
                    mostWantedCrimeDef = crimeList[i];
                    mostWantedCrime = variant;
                    mostWantedSeverity = variant.severity * record.CrimesWanted[crimeList[i]];
                }
                else if (variant.severity == mostWantedCrime.severity && record.CrimesWanted[crimeList[i]] > record.CrimesWanted[mostWantedCrimeDef])
                {
                    mostWantedCrimeDef = crimeList[i];
                    mostWantedCrime = variant;
                }
            }

            SkillDesc crimeDesc = Instantiate(p_SkillDesc);
            generatedObjects.Add(crimeDesc.gameObject);
            crimeDesc.skillName.text = (record.CrimesWanted[crimeList[i]] > 0 ? "<color=yellow>" : "<color=white>") + variant.name + "</color>";
            crimeDesc.skillValue.text = (record.CrimesWanted[crimeList[i]] > 0 ? "<color=yellow>" : "<color=white>") + record.CrimesWanted[crimeList[i]] + "</color> / <color=red>" + record.CrimesPunished[crimeList[i]] + "</color> / <color=lime>" + record.CrimesAcquitted[crimeList[i]] + "</color>";

            if(variant.degree == CrimeDef.CrimeDegree.TREASON)
            {
                crimeDesc.transform.SetParent(TreasonColumn, false);
            }
            else if (variant.degree == CrimeDef.CrimeDegree.FELONY)
            {
                crimeDesc.transform.SetParent(FelonyColumn, false);
            }
            else if (variant.degree == CrimeDef.CrimeDegree.MISDEMEANOR)
            {
                crimeDesc.transform.SetParent(MisdemeanorColumn, false);
            }
        }

        if (record.CurrentSentence == 0 && record.LifeSentences == 0) t_Sentence.gameObject.SetActive(false);
        else
        {
            t_Sentence.gameObject.SetActive(true);
            t_Sentence.text = mc.getTranslation("INFO_crime_current_sentence");
            if (record.deathPenalty) t_Sentence.text += mc.getTranslation("INFO_crime_death_sentence").Replace("$TIMELEFT", record.CurrentSentence.ToString()).Replace("$MONTH", record.CurrentSentence > 1 ? mc.getTranslation("TIME_month_plural") : mc.getTranslation("TIME_month"));
            else if (record.LifeSentences > 0)
                t_Sentence.text += (record.LifeSentences > 1 ? mc.getTranslation("INFO_crime_life_sentence_multiple").Replace("$COUNT", record.LifeSentences.ToString()) : mc.getTranslation("INFO_crime_life_sentence"));
            else
            {
                if (record.CurrentSentence < 36)
                    t_Sentence.text += mc.getTranslation("INFO_crime_standard_sentence").Replace("$LENGTH", record.CurrentSentence.ToString()).Replace("$MONTHSYEARS", record.CurrentSentence > 1 ? mc.getTranslation("TIME_month_plural") : mc.getTranslation("TIME_month"));
                else
                    t_Sentence.text += mc.getTranslation("INFO_crime_standard_sentence").Replace("$LENGTH", (record.CurrentSentence/12).ToString()).Replace("$MONTHSYEARS", mc.getTranslation("TIME_year_plural"));
            }
        }

        if (record.TotalTimeServed == 0) t_TimeServed.gameObject.SetActive(false);
        else
        {
            t_TimeServed.gameObject.SetActive(true);
            if (record.TotalTimeServed < 36)
                t_TimeServed.text = mc.getTranslation("INFO_crime_time_served").Replace("$LENGTH", record.TotalTimeServed.ToString()).Replace("$MONTHSYEARS", record.TotalTimeServed > 1 ? mc.getTranslation("TIME_month_plural") : mc.getTranslation("TIME_month"));
            else
                t_TimeServed.text = mc.getTranslation("INFO_crime_time_served").Replace("$LENGTH", (record.TotalTimeServed / 12).ToString()).Replace("$MONTHSYEARS", mc.getTranslation("TIME_year_plural"));
        }

        if ((selectedChar.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.MISSING) != 0)
            t_Wanted.text = mc.getTranslation("INFO_crime_wanted_for").Replace("$CRIME", mc.getTranslation("INFO_crime_rehabilitation"));
        else if ((selectedChar.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.ILLEGAL_IMMIGRANT) != 0)
            t_Wanted.text = mc.getTranslation("INFO_crime_wanted_for").Replace("$CRIME", mc.getTranslation("INFO_crime_deportation"));
        else if (mostWantedCrime != null)
            t_Wanted.text = mc.getTranslation("INFO_crime_wanted_for").Replace("$CRIME", mostWantedCrime.name.ToUpper());
        else
            t_Wanted.text = "";

        Age age = selectedChar.getComponent<Age>();

        if (selectedChar.hasComponent<Liberal>())
        {
            t_TypeName.text = cBase.getRankName() + " (" + info.type_name + ")";
            t_Birthday.text = mc.getTranslation("INFO_full_age").Replace("$DATE", age.birthday.ToString("MMMM dd, yyyy")).Replace("$AGE", age.getAge().ToString());
        }
        else
        {
            //Encounter name instead of type name here so that CCS/Professional Thief won't be revealed by inspection
            t_TypeName.text = cBase.getRankName() + " (" + info.encounterName + ")";
            t_Birthday.text = mc.getTranslation("INFO_rough_age").Replace("$ROUGHAGE", age.getRoughAge());
        }

        int juiceBreakPoint = 0;

        if (cBase.Juice == -50) juiceBreakPoint = -49;
        else if (cBase.Juice < -10) juiceBreakPoint = -10;
        else if (cBase.Juice < 0) juiceBreakPoint = 0;
        else if (cBase.Juice < 10) juiceBreakPoint = 10;
        else if (cBase.Juice < 50) juiceBreakPoint = 50;
        else if (cBase.Juice < 100) juiceBreakPoint = 100;
        else if (cBase.Juice < 200) juiceBreakPoint = 200;
        else if (cBase.Juice < 500) juiceBreakPoint = 500;
        else juiceBreakPoint = 1000;

        t_Juice.text = cBase.Juice + "/" + juiceBreakPoint;

        Body body = selectedChar.getComponent<Body>();

        t_HealthStatus.text = body.getHealthStatusText();
        if((info.flags & CreatureInfo.CreatureFlag.WHEELCHAIR) != 0)
        {
            i_Wheelchair.SetActive(true);
        }
        else
        {
            i_Wheelchair.SetActive(false);
        }

        if (body.isBleeding())
        {
            t_HealthStatus.color = Color.red;
        }
        else
        {
            t_HealthStatus.color = Color.white;
        }

        if (body.getSpecies().type == "HUMAN")
        {
            HumanHealth.gameObject.SetActive(true);
            DogHealth.gameObject.SetActive(false);
            setPartStatus("Head", i_Head_Human);
            setPartStatus("Torso", i_Torso_Human);
            setPartStatus("Left Arm", i_ArmLeft_Human);
            setPartStatus("Right Arm", i_ArmRight_Human);
            setPartStatus("Left Leg", i_LegLeft_Human);
            setPartStatus("Right Leg", i_LegRight_Human);
        }
        else if(body.getSpecies().type == "DOG")
        {
            HumanHealth.gameObject.SetActive(false);
            DogHealth.gameObject.SetActive(true);
            setPartStatus("Head", i_Head_Dog);
            setPartStatus("Torso", i_Torso_Dog);
            setPartStatus("Left Front Leg", i_ArmLeft_Dog);
            setPartStatus("Right Front Leg", i_ArmRight_Dog);
            setPartStatus("Left Hind Leg", i_LegLeft_Dog);
            setPartStatus("Right Hind Leg", i_LegRight_Dog);
        }

        if (age.isYoung() || body.getSpecies().type != "HUMAN")
            b_Prostitution.interactable = false;
        else
            b_Prostitution.interactable = true;

        if (!selectedChar.hasComponent<Liberal>() || selectedChar.getComponent<Liberal>().leader == null)
            b_FireLiberal.interactable = false;
        else
            b_FireLiberal.interactable = true;

        setInventoryButtons();

        if (selectedChar.hasComponent<Hostage>())
        {
            t_Rapport.gameObject.SetActive(true);
            t_Interrogator.gameObject.SetActive(true);
            t_DaysCaptive.gameObject.SetActive(true);
            b_Activity.GetComponentInChildren<Text>().text = mc.getTranslation("INFO_interrogate_tactics");
            t_DaysCaptive.text = mc.getTranslation("INFO_time_in_captivity").Replace("$TIME", selectedChar.getComponent<Hostage>().timeInCaptivity.ToString()).Replace("$DAYS", selectedChar.getComponent<Hostage>().timeInCaptivity > 1 ? mc.getTranslation("TIME_day_plural") : mc.getTranslation("TIME_day"));

            refreshInterrogationTactics();

            if (selectedChar.getComponent<Hostage>().leadInterrogator != null)
            {
                string leadInterrogatorName = selectedChar.getComponent<Hostage>().leadInterrogator.getComponent<CreatureInfo>().getName();

                t_Interrogator.text = mc.getTranslation("INFO_lead_interrogator").Replace("$INTERROGATOR", leadInterrogatorName);
                if (!selectedChar.getComponent<Hostage>().rapport.ContainsKey(selectedChar.getComponent<Hostage>().leadInterrogator))
                {
                    t_Rapport.text = mc.getTranslation("INFO_rapport_none").Replace("$INTERROGATOR", leadInterrogatorName);
                }
                else
                {
                    if(selectedChar.getComponent<Hostage>().rapport[selectedChar.getComponent<Hostage>().leadInterrogator] > 3)
                        t_Rapport.text = mc.getTranslation("INFO_rapport_devoted").Replace("$INTERROGATOR", leadInterrogatorName);
                    else if(selectedChar.getComponent<Hostage>().rapport[selectedChar.getComponent<Hostage>().leadInterrogator] > 1)
                        t_Rapport.text = mc.getTranslation("INFO_rapport_likes").Replace("$INTERROGATOR", leadInterrogatorName);
                    else if (selectedChar.getComponent<Hostage>().rapport[selectedChar.getComponent<Hostage>().leadInterrogator] > -1)
                        t_Rapport.text = mc.getTranslation("INFO_rapport_neutral").Replace("$INTERROGATOR", leadInterrogatorName);
                    else if (selectedChar.getComponent<Hostage>().rapport[selectedChar.getComponent<Hostage>().leadInterrogator] > -4)
                        t_Rapport.text = mc.getTranslation("INFO_rapport_hates").Replace("$INTERROGATOR", leadInterrogatorName);
                    else
                        t_Rapport.text = mc.getTranslation("INFO_rapport_murder").Replace("$INTERROGATOR", leadInterrogatorName); ;
                }
            }
            else
            {
                t_Interrogator.text = mc.getTranslation("INFO_no_interrogator");
                t_Rapport.text = mc.getTranslation("INFO_rapport_no_interrogator");
            }
        }
        else
        {
            t_Rapport.gameObject.SetActive(false);
            t_Interrogator.gameObject.SetActive(false);
            t_DaysCaptive.gameObject.SetActive(false);
        }
    }

    public void toggleInterrogationTactic(string tactic)
    {
        actions.toggleInterrogationTactic(selectedChar, (Hostage.Tactics) Enum.Parse(typeof(Hostage.Tactics), tactic));
        refreshInterrogationTactics();
    }

    private void refreshInterrogationTactics()
    {
        if ((selectedChar.getComponent<Hostage>().tactics & Hostage.Tactics.CONVERT) != 0)
            InterrogationActionMenu.GetComponent<ButtonSelectionGroup>().buttons[0].GetComponent<ButtonSelectionGroupChild>().select();
        else
            InterrogationActionMenu.GetComponent<ButtonSelectionGroup>().buttons[0].GetComponent<ButtonSelectionGroupChild>().unselect();
        if ((selectedChar.getComponent<Hostage>().tactics & Hostage.Tactics.RESTRAIN) != 0)
            InterrogationActionMenu.GetComponent<ButtonSelectionGroup>().buttons[1].GetComponent<ButtonSelectionGroupChild>().select();
        else
            InterrogationActionMenu.GetComponent<ButtonSelectionGroup>().buttons[1].GetComponent<ButtonSelectionGroupChild>().unselect();
        if ((selectedChar.getComponent<Hostage>().tactics & Hostage.Tactics.ASSAULT) != 0)
            InterrogationActionMenu.GetComponent<ButtonSelectionGroup>().buttons[2].GetComponent<ButtonSelectionGroupChild>().select();
        else
            InterrogationActionMenu.GetComponent<ButtonSelectionGroup>().buttons[2].GetComponent<ButtonSelectionGroupChild>().unselect();
        if ((selectedChar.getComponent<Hostage>().tactics & Hostage.Tactics.USE_PROPS) != 0)
            InterrogationActionMenu.GetComponent<ButtonSelectionGroup>().buttons[3].GetComponent<ButtonSelectionGroupChild>().select();
        else
            InterrogationActionMenu.GetComponent<ButtonSelectionGroup>().buttons[3].GetComponent<ButtonSelectionGroupChild>().unselect();
        if ((selectedChar.getComponent<Hostage>().tactics & Hostage.Tactics.USE_DRUGS) != 0)
            InterrogationActionMenu.GetComponent<ButtonSelectionGroup>().buttons[4].GetComponent<ButtonSelectionGroupChild>().select();
        else
            InterrogationActionMenu.GetComponent<ButtonSelectionGroup>().buttons[4].GetComponent<ButtonSelectionGroupChild>().unselect();
        if ((selectedChar.getComponent<Hostage>().tactics & Hostage.Tactics.KILL) != 0)
            InterrogationActionMenu.GetComponent<ButtonSelectionGroup>().buttons[5].GetComponent<ButtonSelectionGroupChild>().select();
        else
            InterrogationActionMenu.GetComponent<ButtonSelectionGroup>().buttons[5].GetComponent<ButtonSelectionGroupChild>().unselect();
    }

    public void setInventoryButtons()
    {
        Inventory inventory = selectedChar.getComponent<Inventory>();

        b_Weapon.button.interactable = true;
        b_Armor.button.interactable = true;
        b_Clip.button.interactable = true;
        b_Vehicle.button.interactable = true;
        b_Reload.interactable = true;

        b_Weapon.GetComponent<MouseOverText>().mouseOverText = "";
        b_Armor.GetComponent<MouseOverText>().mouseOverText = "";
        b_Clip.GetComponent<MouseOverText>().mouseOverText = "";
        b_Vehicle.GetComponent<MouseOverText>().mouseOverText = "";

        Entity weapon;

        if (selectedChar.getComponent<CreatureInfo>().inCombat ||
            inventory.getWeapon().getComponent<Weapon>().getSize() > inventory.getArmor().getComponent<Armor>().getConcealmentSize() ||
            selectedChar.hasComponent<Liberal>())
            weapon = inventory.getWeapon();
        else
            weapon = inventory.naturalWeapon;

        if (weapon.getComponent<Weapon>().getAmmoType() == "NONE")
            b_Weapon.setItem(weapon);
        else if (weapon.getComponent<Weapon>().clip != null)
            b_Weapon.setItem(weapon, inventory.getWeapon().getComponent<Weapon>().clip.getComponent<Clip>().ammo, true);
        else
            b_Weapon.setItem(weapon, 0, true);

        b_Armor.setItem(inventory.getArmor());

        if (selectedChar.hasComponent<Liberal>())
        {
            if (inventory.clips.Count > 0)
                b_Clip.setItem(inventory.clips.Peek(), inventory.clips.Count, true);
            else
                b_Clip.setEmpty();
        }
        else
        {
            if(weapon.getComponent<Weapon>().getAmmoType() != "NONE")
            {
                if (inventory.clips.Count > 0)
                    b_Clip.setItem(inventory.clips.Peek(), inventory.clips.Count, true);
                else
                    b_Clip.setEmpty();
            }
            else
            {
                b_Clip.setEmpty();
            }
        }

        if (inventory.getWeapon().getComponent<Weapon>().getAmmoType() == "NONE")
        {
            b_Clip.button.interactable = false;
            b_Reload.interactable = false;

            if(inventory.clips.Count > 0 && inventory.clips.Peek().hasComponent<Weapon>() && (inventory.clips.Peek().getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.THROWN) != 0)
            {
                b_Clip.button.interactable = true;
                b_Reload.interactable = true;
            }
        }
        else
        {
            b_Clip.button.interactable = true;
            b_Reload.interactable = true;
        }

        if (inventory.vehicle != null)
        {
            b_Vehicle.setItem(inventory.vehicle);
            if (inventory.vehicle.getComponent<Vehicle>().preferredDriver == selectedChar)
                b_Vehicle.t_Name.text = b_Vehicle.t_Name.text + "(D)";
        }
        else
            b_Vehicle.setEmpty();

        setButtonInteractivity();
    }

    private void setPartStatus(string partName, Image partImage)
    {
        KeyValuePair<string, string> partStatus = selectedChar.getComponent<Body>().getBodyPartStatus(partName);
        partImage.GetComponent<MouseOverText>().mouseOverText = partStatus.Key;
        switch (partStatus.Value)
        {
            case "FINE":
                partImage.color = c_Fine;
                break;
            case "SCARRED":
                partImage.color = c_Scarred;
                break;
            case "INJURED":
                partImage.color = c_Injured;
                break;
            case "INJURED_VITAL":
                partImage.color = c_InjuredVital;
                break;
            case "SEVERED_NASTY":
                partImage.color = c_SeveredNasty;
                break;
            case "SEVERED":
                partImage.color = c_Severed;
                break;
        }
    }

    private void populateLiberalInfo()
    {
        MasterController mc = MasterController.GetMC();
        Liberal lib = selectedChar.getComponent<Liberal>();
        CreatureBase cBase = selectedChar.getComponent<CreatureBase>();
        Body body = selectedChar.getComponent<Body>();
        t_JoinDate.text = mc.getTranslation("INFO_joined_on").Replace("$JOINDATE", lib.joinDate.ToString("MMMM dd, yyyy"));

        if (lib.status == Liberal.Status.SLEEPER)
        {
            t_SleeperInfiltration.gameObject.SetActive(true);
            t_SleeperInfiltration.text = mc.getTranslation("INFO_sleeper_infiltration").Replace("$INFILTRATION", lib.infiltration.ToString());
        }
        else
        {
            t_SleeperInfiltration.gameObject.SetActive(false);
        }

        if (lib.recruitType == Liberal.RecruitType.ENLIGHTENED)
        {
            EnlightenedBox.SetActive(true);
            FollowerBox.SetActive(false);
        }
        else
        {
            FollowerBox.SetActive(true);
            EnlightenedBox.SetActive(false);
            FollowerBox.GetComponentInChildren<Text>().text = lib.getNormalSubordinateCount() + "/" + lib.getSubordinateLimit();

            FollowerBox.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_mouseover_followers") + (lib.subordinates.Count > 0 ? ":" : "");

            foreach (Entity e in lib.subordinates)
            {
                if(e.getComponent<Liberal>().recruitType != Liberal.RecruitType.LOVE_SLAVE)
                    FollowerBox.GetComponent<MouseOverText>().mouseOverText += "\n" + e.getComponent<CreatureInfo>().getName();
                if (e.getComponent<Liberal>().recruitType == Liberal.RecruitType.ENLIGHTENED)
                    FollowerBox.GetComponent<MouseOverText>().mouseOverText += " " + mc.getTranslation("INFO_follower_enlightened");
            }
        }

        if (lib.getLoverCount() == 0)
        {
            LoverBox.SetActive(false);
        }
        else
        {
            LoverBox.SetActive(true);
            LoverBox.GetComponentInChildren<Text>().text = "" + lib.getLoverCount();

            LoverBox.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_mouseover_lovers") + ":";

            foreach (Entity e in lib.subordinates)
            {
                if (e.getComponent<Liberal>().recruitType == Liberal.RecruitType.LOVE_SLAVE)
                    LoverBox.GetComponent<MouseOverText>().mouseOverText += "\n" + e.getComponent<CreatureInfo>().getName();
            }

            if(lib.recruitType == Liberal.RecruitType.LOVE_SLAVE)
            {
                LoverBox.GetComponent<MouseOverText>().mouseOverText += "\n" + lib.leader.getComponent<CreatureInfo>().getName() + " " + mc.getTranslation("INFO_lover_master");
            }
        }

        b_Squad.GetComponentInChildren<Text>().text = lib.squad != null ? lib.squad.name : mc.getTranslation("INFO_no_squad_button");
        b_Activity.GetComponentInChildren<Text>().text = lib.getActivityName() + " (A)";
        setBaseButtonText();

        if (lib.status != Liberal.Status.SLEEPER)
        {
            if (lib.homeBase.getComponent<SafeHouse>().getBodies().Count > 0)
                b_DumpBodies.interactable = true;
            else
                b_DumpBodies.interactable = false;

            if (lib.homeBase.getComponent<SafeHouse>().getHostages().Count > 0)
                b_Interrogate.interactable = true;
            else
                b_Interrogate.interactable = false;

            if ((lib.homeBase.getComponent<SafeHouse>().investments & SafeHouse.Investments.PRINTING_PRESS) == 0)
            {
                b_LiberalGuardian.interactable = false;
                b_LiberalGuardian.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_activism_write_guardian_no_press_mouseover");
            }
            else
            {
                b_LiberalGuardian.interactable = true;
                b_LiberalGuardian.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_activism_write_guardian_mouseover");
            }
        }

        if (lib.status == Liberal.Status.HOSPITAL)
        {
            b_Base.GetComponentInChildren<Text>().text = cBase.Location.getComponent<SiteBase>().getCurrentName();
            b_Activity.GetComponentInChildren<Text>().text = mc.getTranslation("INFO_healing_time_left").Replace("$TIME",body.HospitalTime.ToString()).Replace("$MONTHS", body.HospitalTime > 1 ? mc.getTranslation("TIME_month_plural") : mc.getTranslation("TIME_month")) + ")";
        }

        if (lib.status == Liberal.Status.JAIL_POLICE_CUSTODY)
        {
            b_Base.GetComponentInChildren<Text>().text = cBase.Location.getComponent<SiteBase>().getCurrentName();
            b_Activity.GetComponentInChildren<Text>().text = mc.getTranslation("INFO_in_police_custody");
        }

        if (lib.status == Liberal.Status.JAIL_COURT)
        {
            b_Base.GetComponentInChildren<Text>().text = cBase.Location.getComponent<SiteBase>().getCurrentName();
            b_Activity.GetComponentInChildren<Text>().text = mc.getTranslation("INFO_awaiting_trial");
        }

        if (lib.status == Liberal.Status.JAIL_PRISON)
        {
            b_Base.GetComponentInChildren<Text>().text = cBase.Location.getComponent<SiteBase>().getCurrentName();
            b_Activity.GetComponentInChildren<Text>().text = mc.getTranslation("INFO_in_prison");
        }
    }

    public void changeAlias()
    {
        CreatureInfo info = selectedChar.getComponent<CreatureInfo>();

        info.alias = t_Alias.text;
    }

    public void changeGender()
    {
        actions.changeGender(selectedChar);

        switch (selectedChar.getComponent<CreatureInfo>().genderLiberal)
        {
            case CreatureInfo.CreatureGender.MALE:
            case CreatureInfo.CreatureGender.WHITEMALEPATRIARCH:
                b_Gender.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("GENDER_male");
                break;
            case CreatureInfo.CreatureGender.NEUTRAL:
                b_Gender.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("GENDER_neutral");
                break;
            case CreatureInfo.CreatureGender.FEMALE:
                b_Gender.GetComponentInChildren<Text>().text = MasterController.GetMC().getTranslation("GENDER_female");
                break;
        }
    }

    public void fireLiberal()
    {
        List<PopupOption> options = new List<PopupOption>();
        options.Add(new PopupOption(MasterController.GetMC().getTranslation("OPTION_yes"), () =>
        {
            actions.fireLiberal(selectedChar);
            back();
        }));

        options.Add(new PopupOption(MasterController.GetMC().getTranslation("OPTION_no"), () =>
        {
            //Do Nothing
        }));

        MasterController.GetMC().uiController.showYesNoPopup(MasterController.GetMC().getTranslation("INFO_release_liberal_confirmation"), options);
    }

    public void selectActivity(string activity)
    {
        actions.setActivity(selectedChar, activity);
        
        hideAllMenus();

        b_Activity.GetComponentInChildren<Text>().text = selectedChar.getComponent<Liberal>().getActivityName() + " (A)";
        clearMouseOverText();
    }

    public void showFullSkills()
    {
        SkillsBlackout.SetActive(true);
    }

    public void hideFullSkill()
    {
        hideAllMenus();
        SkillsBlackout.SetActive(false);
    }

    public void showCriminalRecord()
    {
        CriminalRecordBlackout.SetActive(true);
    }

    public void hideCriminalRecord()
    {
        hideAllMenus();
        CriminalRecordBlackout.SetActive(false);
    }

    public void showMakeClothing()
    {
        hideAllMenus();     
        MakeClothingBlackout.SetActive(true);
    }

    public void hideMakeClothing()
    {
        hideAllMenus();
        MakeClothingBlackout.SetActive(false);
    }

    public void showInventoryMenu(string category)
    {
        if (!selectedChar.hasComponent<Liberal>()) return;

        inventoryMenu.gameObject.SetActive(true);
        inventoryMenu.clear();
        inventoryMenu.currentCharacter = selectedChar;
        if (MasterController.GetMC().phase == MasterController.Phase.BASE)
            inventoryMenu.showBaseInventory(category);
        else
            inventoryMenu.showSquadInventory(category);
    }

    public void hideInventoryMenu()
    {
        hideAllMenus();
        inventoryMenu.gameObject.SetActive(false);
        inventoryMenu.clear();
    }

    public void newSquad()
    {
        string squadName = t_NewSquad.text;

        LiberalCrimeSquad.Squad newSquad = actions.newSquad(selectedChar, squadName);
        changeSquad(newSquad);
    }

    public void noSquad()
    {
        changeSquad(null);
    }

    public void changeSquad(LiberalCrimeSquad.Squad newSquad)
    {
        actions.setSquad(selectedChar, newSquad);
        hideAllMenus();

        b_Squad.GetComponentInChildren<Text>().text = newSquad != null ? newSquad.name : MasterController.GetMC().getTranslation("INFO_no_squad_button");
        setBaseButtonText();
        buildSquadMenu();
        setButtonInteractivity();
    }

    public void cancelMove()
    {
        actions.moveBase(selectedChar, null);
        setBaseButtonText();
        hideAllMenus();
    }

    public void showMenu(string menuName)
    {
        switch (menuName)
        {
            case "activity":
                hideAllMenus();
                if (selectedChar.hasComponent<Hostage>())
                {
                    InterrogationActionMenu.SetActive(true);
                }
                else if (selectedChar.getComponent<Liberal>().status != Liberal.Status.SLEEPER)
                {
                    ActivityMenu.SetActive(true);
                }
                else
                {
                    SleeperMenu.SetActive(true);
                }
                break;
            case "sleeper_advocacy":
                hideAllMenus();
                SleeperMenu.SetActive(true);
                SleeperAdvocacyMenu.SetActive(true);
                break;
            case "sleeper_espionage":
                hideAllMenus();
                SleeperMenu.SetActive(true);
                SleeperEspionageMenu.SetActive(true);
                break;
            case "activism":
                hideAllMenus();
                ActivityMenu.SetActive(true);
                ActivismMenu.SetActive(true);
                break;
            case "fundraising":
                hideAllMenus();
                ActivityMenu.SetActive(true);
                FundraisingMenu.SetActive(true); 
                break;
            case "recruitment":
                hideAllMenus();
                ActivityMenu.SetActive(true);
                AcquisitionMenu.SetActive(true);
                break;
            case "teaching":
                hideAllMenus();
                ActivityMenu.SetActive(true);
                TeachingMenu.SetActive(true);
                break;
            case "healing":
                hideAllMenus();
                ActivityMenu.SetActive(true);
                HealingMenu.SetActive(true);
                break;
            case "learning":
                hideAllMenus();
                ActivityMenu.SetActive(true);
                LearningMenu.SetActive(true);
                break;
            case "interrogation":
                hideAllMenus();
                ActivityMenu.SetActive(true);
                InterrogationMenu.SetActive(true);
                break;
            case "steal_vehicle":
                hideAllMenus();
                ActivityMenu.SetActive(true);
                AcquisitionMenu.SetActive(true);
                StealCarMenu.SetActive(true);
                break;
            case "squad":
                hideAllMenus();
                SquadMenu.SetActive(true);
                break;
            case "base":
                hideAllMenus();
                BaseMenu.SetActive(true);
                break;
            default:
                hideAllMenus();
                break;
        }

        ScreenDim.SetActive(true);
    }

    public void hideAllMenus()
    {
        ScreenDim.gameObject.SetActive(false);
        ActivityMenu.SetActive(false);
        ActivismMenu.SetActive(false);
        FundraisingMenu.SetActive(false);
        TeachingMenu.SetActive(false);
        AcquisitionMenu.SetActive(false);
        HealingMenu.SetActive(false);
        LearningMenu.SetActive(false);
        InterrogationMenu.SetActive(false);
        SquadMenu.SetActive(false);
        BaseMenu.SetActive(false);
        SleeperMenu.SetActive(false);
        SleeperAdvocacyMenu.SetActive(false);
        SleeperEspionageMenu.SetActive(false);
        InterrogationActionMenu.SetActive(false);
        StealCarMenu.SetActive(false);
    }

    public void back()
    {
        hide();
        actions.back();
    }

    public void reload()
    {
        actions.reload(selectedChar);
        setInventoryButtons();
    }

    private void clearMouseOverText()
    {
        //TODO: Needed?
        UIControllerImpl.tooltip.setText("");
    }

    public void navigation(string d)
    {
        if (!selectedChar.hasComponent<Liberal>()) return;

        Liberal lib = selectedChar.getComponent<Liberal>();

        if (MasterController.GetMC().phase == MasterController.Phase.BASE)
        {
            switch (d)
            {
                case "UP":
                    if (lib.leader != null)
                    {
                        hide();
                        show(lib.leader);
                    }
                    break;
                case "DOWN":
                    if (lib.subordinates.Count > 0)
                    {
                        hide();
                        show(lib.subordinates[0]);
                    }
                    break;
                case "LEFT":
                    if (lib.leader != null && lib.leader.getComponent<Liberal>().subordinates.Count > 1)
                    {
                        int currentSubordinate = lib.leader.getComponent<Liberal>().subordinates.IndexOf(selectedChar);
                        int subordinateCount = lib.leader.getComponent<Liberal>().subordinates.Count;
                        hide();
                        show(lib.leader.getComponent<Liberal>().subordinates[((currentSubordinate - 1) % subordinateCount) >= 0 ? ((currentSubordinate - 1) % subordinateCount) : subordinateCount - 1]);
                    }
                    break;
                case "RIGHT":
                    if (lib.leader != null && lib.leader.getComponent<Liberal>().subordinates.Count > 1)
                    {
                        int currentSubordinate = lib.leader.getComponent<Liberal>().subordinates.IndexOf(selectedChar);
                        int subordinateCount = lib.leader.getComponent<Liberal>().subordinates.Count;                        
                        hide();
                        show(lib.leader.getComponent<Liberal>().subordinates[(currentSubordinate + 1) % subordinateCount]);
                    }
                    break;
            }
        }
        else
        {
            if (lib.squad != null && lib.squad.Count > 1)
            {
                int currentSquadMember = lib.squad.IndexOf(selectedChar);
                int subordinateCount = lib.squad.Count;

                switch (d)
                {
                    case "LEFT":
                        hide();
                        show(lib.squad[((currentSquadMember - 1) % subordinateCount) >= 0 ? ((currentSquadMember - 1) % subordinateCount) : subordinateCount - 1]);
                        break;
                    case "RIGHT":
                        hide();
                        show(lib.squad[(currentSquadMember + 1) % subordinateCount]);
                        break;
                }
            }
        }
    }

    private void setButtonInteractivity()
    {
        Liberal lib = selectedChar.getComponent<Liberal>();
        MasterController mc = MasterController.GetMC();

        b_Squad.gameObject.SetActive(true);
        b_Weapon.gameObject.SetActive(true);
        b_Armor.gameObject.SetActive(true);
        b_Clip.gameObject.SetActive(true);
        b_Vehicle.gameObject.SetActive(true);
        b_Reload.gameObject.SetActive(true);
        t_Wanted.gameObject.SetActive(true);
        SkillList.gameObject.SetActive(true);
        b_Wheelchair.gameObject.SetActive(false);

        if (mc.phase != MasterController.Phase.BASE)
            b_Gender.interactable = false;
        else
            b_Gender.interactable = true;

        if (!selectedChar.hasComponent<Liberal>())
        {
            b_Squad.interactable = false;            
            b_Base.interactable = false;
            b_Activity.interactable = false;
            b_Weapon.button.interactable = false;
            b_Armor.button.interactable = false;
            b_Clip.button.interactable = false;
            b_Vehicle.button.interactable = false;
            b_Reload.interactable = false;
            t_SleeperInfiltration.gameObject.SetActive(false);

            b_Squad.GetComponentInChildren<Text>().text = mc.getTranslation("INFO_nonlib_squad_button");
            if(MasterController.GetMC().phase == MasterController.Phase.TROUBLE)
                b_Base.GetComponentInChildren<Text>().text = selectedChar.getComponent<CreatureBase>().Location.getComponent<SiteBase>().getCurrentName();
            else
                b_Base.GetComponentInChildren<Text>().text = selectedChar.getComponent<CreatureInfo>().workLocation.getComponent<SiteBase>().getCurrentName();
            if (selectedChar.hasComponent<Hostage>())
                b_Activity.GetComponentInChildren<Text>().text = mc.getTranslation("INFO_interrogate_tactics");
            else
                b_Activity.GetComponentInChildren<Text>().text = mc.getTranslation("INFO_nonlib_activity");

            if (selectedChar.getComponent<CreatureInfo>().alignment != Alignment.LIBERAL)
            {
                b_Squad.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_nonlib_other_mouseover");
                b_Base.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_nonlib_other_mouseover");
                b_Activity.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_nonlib_other_mouseover");
            }
            else
            {
                b_Squad.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_nonlib_liberal_mouseover");
                b_Base.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_nonlib_liberal_mouseover"); ;
                b_Activity.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_nonlib_liberal_mouseover"); ;
            }

            if (selectedChar.hasComponent<Hostage>())
            {
                b_Squad.gameObject.SetActive(false);
                b_Weapon.gameObject.SetActive(false);
                b_Armor.gameObject.SetActive(false);
                b_Clip.gameObject.SetActive(false);
                b_Vehicle.gameObject.SetActive(false);
                b_Reload.gameObject.SetActive(false);
                SkillList.gameObject.SetActive(false);
                t_Wanted.gameObject.SetActive(false);
                b_Base.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_nonlib_hostage_move");
                b_Activity.GetComponent<MouseOverText>().mouseOverText = "";

                b_Activity.interactable = true;
            }

            return;
        }

        if (!selectedChar.getComponent<Body>().Alive)
        {
            b_Squad.interactable = false;
            b_Base.interactable = false;
            b_Activity.interactable = false;
            b_Weapon.button.interactable = false;
            b_Armor.button.interactable = false;
            b_Clip.button.interactable = false;
            b_Vehicle.button.interactable = false;
            b_Reload.interactable = false;

            string name = selectedChar.getComponent<CreatureInfo>().alias == "" ? selectedChar.getComponent<CreatureInfo>().givenName : selectedChar.getComponent<CreatureInfo>().alias;

            b_Squad.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_dead_lib_squad_mouseover").Replace("$NAME",name);
            b_Base.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_dead_lib_base_mouseover").Replace("$NAME", name);
            b_Activity.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_dead_lib_activity_mouseover").Replace("$NAME", name);

            return;
        }

        if (mc.phase != MasterController.Phase.BASE || !(lib.status == Liberal.Status.ACTIVE || lib.status == Liberal.Status.SLEEPER))
        {
            b_Squad.interactable = false;
            b_Squad.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_away_squad_mouseover");
            b_Base.interactable = false;
            b_Base.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_away_base_mouseover");
            b_Activity.interactable = false;
            b_Activity.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_away_activity_mouseover");

            b_Vehicle.button.interactable = false;
            if (mc.phase != MasterController.Phase.TROUBLE)
            {
                b_Reload.interactable = false;
                b_Weapon.button.interactable = false;
                b_Armor.button.interactable = false;
                b_Clip.button.interactable = false;
            }
            else
            {
                if (selectedChar.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getAmmoType() == "NONE")
                {
                    b_Clip.button.interactable = false;
                    b_Reload.interactable = false;
                    if (selectedChar.getComponent<Inventory>().clips.Count > 0 && 
                        selectedChar.getComponent<Inventory>().clips.Peek().hasComponent<Weapon>() && 
                        (selectedChar.getComponent<Inventory>().clips.Peek().getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.THROWN) != 0)
                    {
                        b_Clip.button.interactable = true;
                        b_Reload.interactable = true;
                    }
                }
                else
                {
                    b_Clip.button.interactable = true;
                    b_Reload.interactable = true;
                }
                b_Armor.button.interactable = true;
                b_Weapon.button.interactable = true;
            }
        }
        else
        {
            if (lib.status == Liberal.Status.SLEEPER)
            {
                b_Squad.gameObject.SetActive(false);
                b_Weapon.button.interactable = false;
                b_Armor.button.interactable = false;
                b_Clip.button.interactable = false;
                b_Vehicle.button.interactable = false;
                b_Reload.interactable = false;
                if (lib.getNormalSubordinateCount() < lib.getSubordinateLimit() && lib.homeBase.hasComponent<TroubleSpot>())
                {
                    b_SleeperRecruit.interactable = true;
                    b_SleeperRecruit.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_sleeper_expand_network_mouseover");
                }
                else
                {
                    b_SleeperRecruit.interactable = false;
                    if(lib.homeBase.hasComponent<TroubleSpot>())
                        b_SleeperRecruit.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_sleeper_expand_network_mouseover_no_juice");
                    else
                        b_SleeperRecruit.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_sleeper_expand_network_mouseover_invalid_location");
                }
            }
            else
            {
                b_Squad.interactable = true;
            }
            b_Base.interactable = lib.squad != null ? false : true;
            if (lib.status == Liberal.Status.SLEEPER) b_Base.interactable = false;
            if (b_Base.interactable)
            {
                b_Base.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_mouseover_base");
            }
            else
            {
                if (lib.status != Liberal.Status.SLEEPER)
                    b_Base.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_mouseover_base_squad");
                else
                    b_Base.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_mouseover_base_sleeper");
            }

            if (selectedChar.getComponent<Liberal>().status == Liberal.Status.ACTIVE &&
                selectedChar.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().underSiege)
            {
                b_Base.interactable = false;
                b_Base.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_mouseover_base_siege");
            }

            if (selectedChar.getComponent<Body>().BadlyHurt)
            {
                b_Activity.interactable = false;
                b_Activity.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_injured_activity_mouseover").Replace("$NAME", selectedChar.getComponent<CreatureInfo>().getName());
            }
            else
            {
                b_Activity.interactable = true;
            }

            if(!selectedChar.getComponent<Body>().canWalk() &&
                (selectedChar.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.WHEELCHAIR) == 0)
            {
                b_Wheelchair.gameObject.SetActive(true);
                b_Squad.interactable = false;
                b_Squad.GetComponent<MouseOverText>().mouseOverText = mc.getTranslation("INFO_wheelchair_squad_mouseover");
            }
        }
    }

    private void buildSquadMenu()
    {
        foreach(GameObject g in squadButtons)
        {
            Destroy(g);
        }
        t_NewSquad.text = "";

        foreach (LiberalCrimeSquad.Squad squad in MasterController.lcs.squads)
        {
            if (selectedChar.getComponent<Liberal>().squad == squad) continue;
            if (selectedChar.getComponent<Liberal>().homeBase != squad.homeBase) continue;

            Button squadButton = Instantiate(p_MenuButton);
            squadButtons.Add(squadButton.gameObject);
            squadButton.GetComponentInChildren<Text>().text = squad.name;
            if (squad.Count == 6) squadButton.GetComponent<Button>().interactable = false;

            squadButton.transform.SetParent(SquadMenu.GetComponent<ScrollRect>().content, false);
            squadButton.transform.SetAsFirstSibling();
            squadButton.onClick.AddListener(() => changeSquad(squad));
        }
    }

    private void buildBaseMenu()
    {
        foreach (GameObject g in baseButtons)
        {
            Destroy(g);
        }

        foreach (Entity lcsBase in MasterController.nation.getAllBases())
        {
            if (!lcsBase.getComponent<SafeHouse>().owned) continue;
            if (selectedChar.getComponent<Liberal>().homeBase == lcsBase) continue;

            Button baseButton = Instantiate(p_MenuButton);
            baseButtons.Add(baseButton.gameObject);
            baseButton.GetComponentInChildren<Text>().text = lcsBase.getComponent<SiteBase>().getCurrentName(true);

            baseButton.transform.SetParent(BaseMenu.GetComponent<ScrollRect>().content, false);
            baseButton.transform.SetAsFirstSibling();
            baseButton.onClick.AddListener(() => 
            {
                actions.moveBase(selectedChar, lcsBase);
                setBaseButtonText();
                hideAllMenus();
            });
        }        
    }

    private void buildInterrogationMenu()
    {
        foreach (GameObject g in interrogationButtons)
        {
            Destroy(g);
        }

        if (!selectedChar.getComponent<Liberal>().homeBase.hasComponent<SafeHouse>()) return;

        foreach(Entity hostage in selectedChar.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().getHostages())
        {
            Button hostageButton = Instantiate(p_MenuButton);
            interrogationButtons.Add(hostageButton.gameObject);
            hostageButton.GetComponentInChildren<Text>().text = hostage.getComponent<CreatureInfo>().getName();

            hostageButton.transform.SetParent(InterrogationMenu.GetComponent<ScrollRect>().content, false);
            hostageButton.onClick.AddListener(() =>
            {
                actions.setActivityInterrogate(selectedChar, hostage);
                hideAllMenus();
                b_Activity.GetComponentInChildren<Text>().text = selectedChar.getComponent<Liberal>().getActivityName() + " (A)";
            });
        }
    }

    private void buildVehicleMenu()
    {
        foreach(GameObject g in vehicleButtons)
        {
            Destroy(g);
        }

        foreach(ItemDef item in GameData.getData().itemList.Values)
        {
            if (item.components.ContainsKey("vehicle"))
            {
                if(((ItemDef.VehicleDef)item.components["vehicle"]).stealDifficulty < 10)
                {
                    Button vehicleButton = Instantiate(p_MenuButton);
                    vehicleButtons.Add(vehicleButton.gameObject);
                    vehicleButton.GetComponentInChildren<Text>().text = item.name;

                    vehicleButton.transform.SetParent(StealCarMenu.GetComponent<ScrollRect>().content, false);
                    vehicleButton.onClick.AddListener(() =>
                    {
                        actions.setActivity(selectedChar, "STEAL_VEHICLE " + item.type);
                        hideAllMenus();
                        b_Activity.GetComponentInChildren<Text>().text = selectedChar.getComponent<Liberal>().getActivityName() + " (A)";
                    });
                }
            }
        }
    }

    private void setBaseButtonText()
    {
        if (selectedChar.getComponent<Liberal>().targetBase == null)
        {
            b_Base.GetComponentInChildren<Text>().text = selectedChar.getComponent<Liberal>().homeBase.getComponent<SiteBase>().getCurrentName();
        }
        else
        {
            b_Base.GetComponentInChildren<Text>().text = selectedChar.getComponent<Liberal>().homeBase.getComponent<SiteBase>().getCurrentName(true) + "->" + selectedChar.getComponent<Liberal>().targetBase.getComponent<SiteBase>().getCurrentName(true);
        }
    }
}
