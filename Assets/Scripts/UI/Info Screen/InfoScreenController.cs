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
        b_Base.GetComponent<MouseOverText>().mouseOverText = "Assign a new Safe House to this Liberal";
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
        CreatureInfo info = selectedChar.getComponent<CreatureInfo>();
        t_Name.text = info.givenName + " " + info.surname;
        t_Alias.text = info.alias;
        switch (info.genderLiberal)
        {
            case CreatureInfo.CreatureGender.FEMALE:
                b_Gender.GetComponentInChildren<Text>().text = "Female";
                break;
            case CreatureInfo.CreatureGender.MALE:
            case CreatureInfo.CreatureGender.WHITEMALEPATRIARCH:
                b_Gender.GetComponentInChildren<Text>().text = "Male";
                break;
            case CreatureInfo.CreatureGender.NEUTRAL:
                b_Gender.GetComponentInChildren<Text>().text = "Non-binary";
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
                    line.t_Difficulty.text = "<color=lime>Simple</color>";
                    break;
                case 1:
                    line.t_Difficulty.text = "<color=cyan>Very Easy</color>";
                    break;
                case 2:
                    line.t_Difficulty.text = "<color=teal>Easy</color>";
                    break;
                case 3:
                    line.t_Difficulty.text = "<color=blue>Below Average</color>";
                    break;
                case 4:
                    line.t_Difficulty.text = "Average";
                    break;
                case 5:
                    line.t_Difficulty.text = "<color=grey>Above Average</color>";
                    break;
                case 6:
                    line.t_Difficulty.text = "<color=yellow>Hard</color>";
                    break;
                case 7:
                    line.t_Difficulty.text = "<color=purple>Very Hard</color>";
                    break;
                case 8:
                    line.t_Difficulty.text = "<color=magenta>Extremely Difficult</color>";
                    break;
                case 9:
                    line.t_Difficulty.text = "<color=maroon>Nearly Impossible</color>";
                    break;
                default:
                    line.t_Difficulty.text = "<color=red>Impossible</color>";
                    break;
            }
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
            t_Sentence.text = "Current Sentence: ";
            if (record.deathPenalty) t_Sentence.text += "<color=RED><b>DEATH!!!</b></color> (" + record.CurrentSentence + (record.CurrentSentence > 1 ? " months" : " month") + " until execution)";
            else if (record.LifeSentences > 0) t_Sentence.text += (record.LifeSentences > 1 ? record.LifeSentences + " LIFE SENTENCES" : "LIFE");
            else
            {
                if(record.CurrentSentence < 36)
                    t_Sentence.text += record.CurrentSentence + (record.CurrentSentence > 1 ? " Months" : " Month");
                else
                    t_Sentence.text += record.CurrentSentence/12 + " Years";
            }
        }

        if (record.TotalTimeServed == 0) t_TimeServed.gameObject.SetActive(false);
        else
        {
            t_TimeServed.gameObject.SetActive(true);
            t_TimeServed.text = "Total Time Served: ";
            if (record.TotalTimeServed < 36)
                t_TimeServed.text += record.TotalTimeServed + (record.TotalTimeServed > 1 ? " Months" : " Month");
            else
                t_TimeServed.text += record.TotalTimeServed / 12 + " Years";
        }

        if ((selectedChar.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.MISSING) != 0)
            t_Wanted.text = "WANTED FOR REHABILITATION";
        else if ((selectedChar.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.ILLEGAL_IMMIGRANT) != 0)
            t_Wanted.text = "WANTED FOR DEPORTATION";
        else if (mostWantedCrime != null)
            t_Wanted.text = "WANTED FOR " + mostWantedCrime.name.ToUpper();
        else
            t_Wanted.text = "";

        Age age = selectedChar.getComponent<Age>();

        if (selectedChar.hasComponent<Liberal>())
        {
            t_TypeName.text = cBase.getRankName() + " (" + info.type_name + ")";
            t_Birthday.text = "Born " + age.birthday.ToString("MMMM dd, yyyy") + " (Age " + age.getAge() + ")";
        }
        else
        {
            //Encounter name instead of type name here so that CCS/Professional Thief won't be revealed by inspection
            t_TypeName.text = cBase.getRankName() + " (" + info.encounterName + ")";
            t_Birthday.text = "Age: " + age.getRoughAge();
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
            b_Activity.GetComponentInChildren<Text>().text = "Interrogation Tactics (A)";
            t_DaysCaptive.text = selectedChar.getComponent<Hostage>().timeInCaptivity + (selectedChar.getComponent<Hostage>().timeInCaptivity > 1?" Days":" Day") + " in Captivity";

            refreshInterrogationTactics();

            if (selectedChar.getComponent<Hostage>().leadInterrogator != null)
            {
                t_Interrogator.text = "Lead Interrogator: " + selectedChar.getComponent<Hostage>().leadInterrogator.getComponent<CreatureInfo>().getName();
                if (!selectedChar.getComponent<Hostage>().rapport.ContainsKey(selectedChar.getComponent<Hostage>().leadInterrogator))
                {
                    t_Rapport.text = "The Conservative has not yet developed a relationship with " + selectedChar.getComponent<Hostage>().leadInterrogator.getComponent<CreatureInfo>().getName();
                }
                else
                {
                    if(selectedChar.getComponent<Hostage>().rapport[selectedChar.getComponent<Hostage>().leadInterrogator] > 3)
                        t_Rapport.text = "The Conservative clings helplessly to " + selectedChar.getComponent<Hostage>().leadInterrogator.getComponent<CreatureInfo>().getName() + " as its only friend.";
                    else if(selectedChar.getComponent<Hostage>().rapport[selectedChar.getComponent<Hostage>().leadInterrogator] > 1)
                        t_Rapport.text = "The Conservative likes " + selectedChar.getComponent<Hostage>().leadInterrogator.getComponent<CreatureInfo>().getName() + ".";
                    else if (selectedChar.getComponent<Hostage>().rapport[selectedChar.getComponent<Hostage>().leadInterrogator] > -1)
                        t_Rapport.text = "The Conservative is uncooperative toward " + selectedChar.getComponent<Hostage>().leadInterrogator.getComponent<CreatureInfo>().getName() + ".";
                    else if (selectedChar.getComponent<Hostage>().rapport[selectedChar.getComponent<Hostage>().leadInterrogator] > -4)
                        t_Rapport.text = "The Conservative hates " + selectedChar.getComponent<Hostage>().leadInterrogator.getComponent<CreatureInfo>().getName() + ".";
                    else
                        t_Rapport.text = "The Conservative would like to murder " + selectedChar.getComponent<Hostage>().leadInterrogator.getComponent<CreatureInfo>().getName() + ".";
                }
            }
            else
            {
                t_Interrogator.text = "Not Being Interrogated";
                t_Rapport.text = "The Conservative is sitting alone in the dark.";
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
        Liberal lib = selectedChar.getComponent<Liberal>();
        CreatureBase cBase = selectedChar.getComponent<CreatureBase>();
        Body body = selectedChar.getComponent<Body>();
        t_JoinDate.text = "Joined on " + lib.joinDate.ToString("MMMM dd, yyyy");

        if (lib.status == Liberal.Status.SLEEPER)
        {
            t_SleeperInfiltration.gameObject.SetActive(true);
            t_SleeperInfiltration.text = "Effectiveness: " + lib.infiltration + "%";
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

            FollowerBox.GetComponent<MouseOverText>().mouseOverText = "Followers" + (lib.subordinates.Count > 0 ? ":" : "");

            foreach (Entity e in lib.subordinates)
            {
                if(e.getComponent<Liberal>().recruitType != Liberal.RecruitType.LOVE_SLAVE)
                    FollowerBox.GetComponent<MouseOverText>().mouseOverText += "\n" + e.getComponent<CreatureInfo>().getName();
                if (e.getComponent<Liberal>().recruitType == Liberal.RecruitType.ENLIGHTENED)
                    FollowerBox.GetComponent<MouseOverText>().mouseOverText += " (Enlightened)";
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

            LoverBox.GetComponent<MouseOverText>().mouseOverText = "Lovers:";

            foreach (Entity e in lib.subordinates)
            {
                if (e.getComponent<Liberal>().recruitType == Liberal.RecruitType.LOVE_SLAVE)
                    LoverBox.GetComponent<MouseOverText>().mouseOverText += "\n" + e.getComponent<CreatureInfo>().getName();
            }

            if(lib.recruitType == Liberal.RecruitType.LOVE_SLAVE)
            {
                LoverBox.GetComponent<MouseOverText>().mouseOverText += "\n" + lib.leader.getComponent<CreatureInfo>().getName() + " (Master)";
            }
        }

        b_Squad.GetComponentInChildren<Text>().text = lib.squad != null ? lib.squad.name : "No Squad";
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
                b_LiberalGuardian.GetComponent<MouseOverText>().mouseOverText = "Buy a Printing Press to write for the Liberal Guardian";
            }
            else
            {
                b_LiberalGuardian.interactable = true;
                b_LiberalGuardian.GetComponent<MouseOverText>().mouseOverText = "Write columns and editorials for the Liberal Guardian";
            }
        }

        if (lib.status == Liberal.Status.HOSPITAL)
        {
            b_Base.GetComponentInChildren<Text>().text = cBase.Location.getComponent<SiteBase>().getCurrentName();
            b_Activity.GetComponentInChildren<Text>().text = "Healing (" + body.HospitalTime + (body.HospitalTime > 1 ? " months remaining" : "month remains") + ")";
        }

        if (lib.status == Liberal.Status.JAIL_POLICE_CUSTODY)
        {
            b_Base.GetComponentInChildren<Text>().text = cBase.Location.getComponent<SiteBase>().getCurrentName();
            b_Activity.GetComponentInChildren<Text>().text = "Being Interrogated";
        }

        if (lib.status == Liberal.Status.JAIL_COURT)
        {
            b_Base.GetComponentInChildren<Text>().text = cBase.Location.getComponent<SiteBase>().getCurrentName();
            b_Activity.GetComponentInChildren<Text>().text = "Awaiting Trial";
        }

        if (lib.status == Liberal.Status.JAIL_PRISON)
        {
            b_Base.GetComponentInChildren<Text>().text = cBase.Location.getComponent<SiteBase>().getCurrentName();
            b_Activity.GetComponentInChildren<Text>().text = "Doing Hard Time";
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
                b_Gender.GetComponentInChildren<Text>().text = "Male";
                break;
            case CreatureInfo.CreatureGender.NEUTRAL:
                b_Gender.GetComponentInChildren<Text>().text = "Non-binary";
                break;
            case CreatureInfo.CreatureGender.FEMALE:
                b_Gender.GetComponentInChildren<Text>().text = "Female";
                break;
        }
    }

    public void fireLiberal()
    {
        List<PopupOption> options = new List<PopupOption>();
        options.Add(new PopupOption("Yes", () =>
        {
            actions.fireLiberal(selectedChar);
            back();
        }));

        options.Add(new PopupOption("No", () =>
        {
            //Do Nothing
        }));

        MasterController.GetMC().uiController.showYesNoPopup("Do you want to permanently release this squad member from the LCS? If they have low heart, they may go to the police.", options);
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

        b_Squad.GetComponentInChildren<Text>().text = newSquad != null ? newSquad.name : "No Squad";
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

            b_Squad.GetComponentInChildren<Text>().text = "The Masses";
            if(MasterController.GetMC().phase == MasterController.Phase.TROUBLE)
                b_Base.GetComponentInChildren<Text>().text = selectedChar.getComponent<CreatureBase>().Location.getComponent<SiteBase>().getCurrentName();
            else
                b_Base.GetComponentInChildren<Text>().text = selectedChar.getComponent<CreatureInfo>().workLocation.getComponent<SiteBase>().getCurrentName();
            if (selectedChar.hasComponent<Hostage>())
                b_Activity.GetComponentInChildren<Text>().text = "(A) Interrogation Tactics";
            else
                b_Activity.GetComponentInChildren<Text>().text = "Mindless Consermerism";

            if (selectedChar.getComponent<CreatureInfo>().alignment != Alignment.LIBERAL)
            {
                b_Squad.GetComponent<MouseOverText>().mouseOverText = "You can't give orders to non-Liberals!";
                b_Base.GetComponent<MouseOverText>().mouseOverText = "You can't give orders to non-Liberals!";
                b_Activity.GetComponent<MouseOverText>().mouseOverText = "You can't give orders to non-Liberals!";
            }
            else
            {
                b_Squad.GetComponent<MouseOverText>().mouseOverText = "This Liberal has not yet been motivated to direct action";
                b_Base.GetComponent<MouseOverText>().mouseOverText = "This Liberal has not yet been motivated to direct action";
                b_Activity.GetComponent<MouseOverText>().mouseOverText = "This Liberal has not yet been motivated to direct action";
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
                b_Base.GetComponent<MouseOverText>().mouseOverText = "Moving this hostage would be too risky";
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

            b_Squad.GetComponent<MouseOverText>().mouseOverText = name + " is in no condition to join a squad!";
            b_Base.GetComponent<MouseOverText>().mouseOverText = name + " is in no condition to travel!";
            b_Activity.GetComponent<MouseOverText>().mouseOverText = name + " is in no condition to be activated!";

            return;
        }

        if (mc.phase != MasterController.Phase.BASE || !(lib.status == Liberal.Status.ACTIVE || lib.status == Liberal.Status.SLEEPER))
        {
            b_Squad.interactable = false;
            b_Squad.GetComponent<MouseOverText>().mouseOverText = "Cannot change squads while away from Safe House";
            b_Base.interactable = false;
            b_Base.GetComponent<MouseOverText>().mouseOverText = "Cannot change home while away from Safe House";
            b_Activity.interactable = false;
            b_Activity.GetComponent<MouseOverText>().mouseOverText = "Cannot set activity while away from Safe House";

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
                    b_SleeperRecruit.GetComponent<MouseOverText>().mouseOverText = "";
                }
                else
                {
                    b_SleeperRecruit.interactable = false;
                    if(lib.homeBase.hasComponent<TroubleSpot>())
                        b_SleeperRecruit.GetComponent<MouseOverText>().mouseOverText = "Need more Juice to recruit";
                    else
                        b_SleeperRecruit.GetComponent<MouseOverText>().mouseOverText = "Can't recruit from this location";
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
                b_Base.GetComponent<MouseOverText>().mouseOverText = "Assign a new safe house to this Liberal";
            }
            else
            {
                if (lib.status != Liberal.Status.SLEEPER)
                    b_Base.GetComponent<MouseOverText>().mouseOverText = "Travel to another safe house with your squad to rebase this Liberal";
                else
                    b_Base.GetComponent<MouseOverText>().mouseOverText = "Sleepers need to stay in their designated workplace";
            }

            if (selectedChar.getComponent<Liberal>().status == Liberal.Status.ACTIVE &&
                selectedChar.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().underSiege)
            {
                b_Base.interactable = false;
                b_Base.GetComponent<MouseOverText>().mouseOverText = "Cannot travel while under siege";
            }

            if (selectedChar.getComponent<Body>().BadlyHurt)
            {
                b_Activity.interactable = false;
                b_Activity.GetComponent<MouseOverText>().mouseOverText = selectedChar.getComponent<CreatureInfo>().getName() + " is too badly hurt and needs professional care";
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
                b_Squad.GetComponent<MouseOverText>().mouseOverText = "This Liberal needs a wheelchair before they can go out again";
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
