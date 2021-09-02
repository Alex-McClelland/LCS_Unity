using System;
using System.Collections.Generic;
using LCS.Engine.UI;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Location;
using LCS.Engine.Events;
using System.Xml;
using LCS.Engine.Scenes;
using LCS.Engine.Components.Item;
using LCS.Engine.Data;

namespace LCS.Engine.Components.Creature
{
    public class CriminalRecord : Component
    {
        public Dictionary<string, int> CrimesWanted { get; set; }
        public Dictionary<string, int> CrimesPunished { get; set; }
        public Dictionary<string, int> CrimesAcquitted { get; set; }

        //in months
        [SimpleSave]
        public int CurrentSentence;
        [SimpleSave]
        public int LifeSentences;
        [SimpleSave]
        public bool deathPenalty;
        [SimpleSave]
        public int TotalTimeServed;
        [SimpleSave]
        public int Heat;
        [SimpleSave]
        public int Confessions;
        [SimpleSave]
        public bool hospitalArrest;
        [SimpleSave]
        public Entity clothing;

        public CriminalRecord()
        {
            CrimesWanted = new Dictionary<string, int>();
            CrimesPunished = new Dictionary<string, int>();
            CrimesAcquitted = new Dictionary<string, int>();
            Heat = 0;
            CurrentSentence = 0;
            LifeSentences = 0;
            TotalTimeServed = 0;
            Confessions = 0;
            deathPenalty = false;
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("CriminalRecord");
                entityNode.AppendChild(saveNode);

                foreach (string s in CrimesWanted.Keys)
                {
                    XmlNode wantedNode = saveNode.OwnerDocument.CreateElement("CrimeWanted");
                    XmlAttribute name = wantedNode.OwnerDocument.CreateAttribute("type");
                    XmlAttribute count = wantedNode.OwnerDocument.CreateAttribute("count");
                    name.Value = s;
                    wantedNode.Attributes.Append(name);
                    wantedNode.Attributes.Append(count);
                    saveNode.AppendChild(wantedNode);
                }

                foreach (string s in CrimesPunished.Keys)
                {
                    XmlNode node = saveNode.OwnerDocument.CreateElement("CrimePunished");
                    XmlAttribute name = node.OwnerDocument.CreateAttribute("type");
                    XmlAttribute count = node.OwnerDocument.CreateAttribute("count");
                    name.Value = s;
                    node.Attributes.Append(name);
                    node.Attributes.Append(count);
                    saveNode.AppendChild(node);
                }

                foreach (string s in CrimesAcquitted.Keys)
                {
                    XmlNode node = saveNode.OwnerDocument.CreateElement("CrimeAcquitted");
                    XmlAttribute name = node.OwnerDocument.CreateAttribute("type");
                    XmlAttribute count = node.OwnerDocument.CreateAttribute("count");
                    name.Value = s;
                    node.Attributes.Append(name);
                    node.Attributes.Append(count);
                    saveNode.AppendChild(node);
                }
            }

            foreach (XmlNode node in saveNode.SelectNodes("CrimeWanted"))
            {
                node.Attributes["count"].Value = CrimesWanted[node.Attributes["type"].Value].ToString();
            }
            foreach (XmlNode node in saveNode.SelectNodes("CrimePunished"))
            {
                node.Attributes["count"].Value = CrimesPunished[node.Attributes["type"].Value].ToString();
            }
            foreach (XmlNode node in saveNode.SelectNodes("CrimeAcquitted"))
            {
                node.Attributes["count"].Value = CrimesAcquitted[node.Attributes["type"].Value].ToString();
            }

            saveSimpleFields();
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);

            foreach(XmlNode node in componentData.SelectNodes("CrimeWanted"))
                CrimesWanted.Add(node.Attributes["type"].Value, int.Parse(node.Attributes["count"].Value));
            foreach (XmlNode node in componentData.SelectNodes("CrimePunished"))
                CrimesPunished.Add(node.Attributes["type"].Value, int.Parse(node.Attributes["count"].Value));
            foreach (XmlNode node in componentData.SelectNodes("CrimeAcquitted"))
                CrimesAcquitted.Add(node.Attributes["type"].Value, int.Parse(node.Attributes["count"].Value));
        }

        public override void subscribe()
        {
            base.subscribe();
            MasterController.GetMC().nextDay += doDaily;
            MasterController.GetMC().nextMonth += doProcess;
        }

        public override void unsubscribe()
        {
            base.unsubscribe();
            MasterController.GetMC().nextDay -= doDaily;
            MasterController.GetMC().nextMonth -= doProcess;
        }

        private void doDaily(object sender, EventArgs args)
        {
            doLoseHeat();
            doRefreshCrimes();
        }

        private void doLoseHeat()
        {
            //Lose heat every day, unless you are being interrogated by police, or awaiting interrogation in the hospital.
            if(hasComponent<Liberal>() &&
                getComponent<Liberal>().status != Liberal.Status.JAIL_POLICE_CUSTODY &&
                !hospitalArrest &&
                Heat > 100)
                Heat -= Heat / 100;
        }

        //Clear out warrants for things that are no longer illegal
        private void doRefreshCrimes()
        {
            List<string> tempcrimes = new List<string>(CrimesWanted.Keys);

            foreach(string crime in tempcrimes)
            {
                if (CrimesWanted[crime] == 0) continue;

                if (getCrimeVariant(crime) == null)
                {
                    CrimesWanted[crime] = 0;
                }
            }
        }

        public bool isCriminal()
        {
            foreach(int i in CrimesWanted.Values)
            {
                if (i > 0) return true;
            }

            return false;
        }

        public bool isMajorCriminal()
        {
            foreach(string crime in CrimesWanted.Keys)
            {
                if (getCrimeVariant(crime).degree == CrimeDef.CrimeDegree.MISDEMEANOR) continue;
                if (CrimesWanted[crime] > 0) return true;
            }

            return false;
        }

        public void arrest()
        {
            getComponent<Liberal>().status = Liberal.Status.JAIL_POLICE_CUSTODY;
            if (getComponent<Liberal>().squad != null) getComponent<Liberal>().squad.Remove(owner);
            if(getComponent<Liberal>().hauledUnit != null)
                Fight.dropHauledUnit(owner);
            getComponent<Liberal>().setActivity("NONE");
            getComponent<CreatureBase>().Location = getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("GOVERNMENT_POLICE_STATION");
            getComponent<Inventory>().destroyWeapon();
            getComponent<Inventory>().destroyAllClips();
            if (getComponent<Inventory>().vehicle != null)
            {
                if (getComponent<Inventory>().vehicle.getComponent<Vehicle>().preferredDriver == owner)
                    getComponent<Inventory>().vehicle.getComponent<Vehicle>().preferredDriver = null;
                getComponent<Inventory>().vehicle = null;
            }

            if(getComponent<Body>().Blood <= 30)
            {
                hospitalArrest = true;
                int time = getComponent<Body>().getClinicTime();

                if (time > 0)
                {
                    getComponent<Liberal>().status = Liberal.Status.HOSPITAL;

                    getComponent<Body>().HospitalTime = time;
                    getComponent<CreatureBase>().Location = getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("HOSPITAL_UNIVERSITY");

                    string message = getComponent<CreatureInfo>().getName() + " has been sent to ";
                    message += getComponent<CreatureBase>().Location.getComponent<SiteBase>().getCurrentName();
                    message += " under guard to recover for " + time + (time > 1 ? " months" : " month") + ", after which " + getComponent<CreatureInfo>().heShe().ToLower() + " will be returned to Police custody.";

                    MasterController.GetMC().addMessage(message, true);
                }
            }

            int totalCrimesWanted = 0;
            foreach (int wanted in CrimesWanted.Values) totalCrimesWanted += wanted;

            //If they've been arrested but have no outstanding charges, charge them with loitering.
            if (totalCrimesWanted == 0) addCrime(Constants.CRIME_LOITERING);

            //If they just escaped from prison, just return them immediately rather than going through a whole other trial. Add on time for the escape attempt.
            if((getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.JUST_ESCAPED) != 0)
            {
                calculateSentence(false);
                imprison();
            }
        }

        public void addCrime(string crime)
        {
            //If the current situation is one where nobody will press charges (i.e. an attack from non-police), ignore any crimes committed.
            if ((MasterController.GetMC().combatModifiers & MasterController.CombatModifiers.NOCHARGES) != 0)
                return;

            if (GameData.getData().crimeList.ContainsKey(crime))
            {
                CrimeDef.CrimeVariant variant = getCrimeVariant(crime);

                //If variant is null, not currently a crime
                if (variant == null)
                    return;

                CrimesWanted[crime] += 1;
                Heat += variant.severity;
            }
            else
            {
                MasterController.GetMC().addErrorMessage("Name not found in crime definitions: " + crime);
            }
        }
        
        public void calculateSentence(bool lenient = false)
        {
            MasterController mc = MasterController.GetMC();

            //DEATH SENTENCE
            if (!lenient &&
                (isDeathPenaltyApplicable() ||
                MasterController.government.laws["DEATH_PENALTY"].alignment == Alignment.ARCHCONSERVATIVE))
            {
                switch (MasterController.government.laws["DEATH_PENALTY"].alignment)
                {
                    case Alignment.ARCHCONSERVATIVE:
                        deathPenalty = true;
                        break;
                    case Alignment.CONSERVATIVE:
                        deathPenalty = mc.LCSRandom(3) != 0;
                        break;
                    case Alignment.MODERATE:
                        deathPenalty = mc.LCSRandom(2) != 0;
                        break;
                    case Alignment.LIBERAL:
                        deathPenalty = mc.LCSRandom(5) == 0;
                        break;
                    case Alignment.ELITE_LIBERAL:
                        deathPenalty = false;
                        break;
                }

                if (deathPenalty) CurrentSentence = 3;
            }

            if (!deathPenalty)
            {
                int tempSentence = 0;
                int tempLifeSentence = 0;

                foreach (string crime in CrimesWanted.Keys)
                {
                    tempSentence += getCrimeSentence(crime);
                    int tempLifeSentence_2 = getCrimeLifeSentences(crime);
                    if (tempLifeSentence_2 > 0) tempLifeSentence += tempLifeSentence_2;
                }

                if (lenient) tempSentence /= 2;
                if(lenient && tempLifeSentence == 1)
                {
                    tempSentence += mc.LCSRandom("240-360");
                    tempLifeSentence = 0;
                }

                if(tempSentence > 1200)
                {
                    tempLifeSentence += tempSentence / 1200;
                }

                LifeSentences += tempLifeSentence;

                //Don't bother adding to monthly sentence if they already have a life sentence.
                if (LifeSentences == 0)
                {
                    if (tempSentence > 36) tempSentence -= tempSentence % 12; 
                    CurrentSentence += tempSentence;
                }
            }
        }

        public void imprison()
        {
            Heat = 0;
            Confessions = 0;
            List<string> crimeList = new List<string>(CrimesWanted.Keys);
            foreach (string crime in crimeList)
            {
                CrimesPunished[crime] += CrimesWanted[crime];
                CrimesWanted[crime] = 0;
            }

            if (CurrentSentence > 0 || LifeSentences > 0)
            {
                if (hasComponent<Liberal>()) getComponent<Liberal>().status = Liberal.Status.JAIL_PRISON;
                getComponent<CreatureBase>().Location = getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("GOVERNMENT_PRISON");
                if (clothing != null) clothing.depersist();
                clothing = null;
            }
            else
            {
                //They aren't really being acquitted but at this point it is functionally identical
                acquit();
            }
        }

        public void acquit()
        {
            List<string> crimeList = new List<string>(CrimesWanted.Keys);
            foreach(string crime in crimeList)
            {
                CrimesAcquitted[crime] += CrimesWanted[crime];
                CrimesWanted[crime] = 0;
            }

            Heat = 0;
            Confessions = 0;
            if (clothing != null)
            {
                getComponent<Inventory>().equipArmor(clothing);
            }

            clothing = null;

            getComponent<Liberal>().status = Liberal.Status.ACTIVE;
            getComponent<Liberal>().goHome();
        }

        public void parole()
        {
            Heat = 0;
            Confessions = 0;
                       
            getComponent<Inventory>().equipArmor(Factories.ItemFactory.create("ARMOR_CLOTHES"));

            if (clothing != null) clothing.depersist();
            clothing = null;

            getComponent<Liberal>().status = Liberal.Status.ACTIVE;
            getComponent<Liberal>().goHome();
        }

        public void escape()
        {
            addCrime(Constants.CRIME_ESCAPE);
            
            if (clothing != null) clothing.depersist();
            clothing = null;

            getComponent<Liberal>().status = Liberal.Status.ACTIVE;
            getComponent<Liberal>().goHome();
        }

        public int getScareFactor()
        {
            int scareFactor = 0;

            foreach (string crime in CrimesWanted.Keys)
            {
                if (CrimesWanted[crime] == 0) continue;
                CrimeDef.CrimeVariant variant = getCrimeVariant(crime);

                if (variant == null) continue;

                scareFactor += CrimesWanted[crime] * variant.severity;
            }

            return scareFactor;
        }

        private CrimeDef.CrimeVariant getCrimeVariant(string crime)
        {
            if (!MasterController.GetMC().testCondition(GameData.getData().crimeList[crime].appearCondition)) return null;

            CrimeDef.CrimeVariant variant = null;

            if (GameData.getData().crimeList[crime].variants.Count > 1)
            {
                foreach (CrimeDef.CrimeVariant testVariant in GameData.getData().crimeList[crime].variants)
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
                variant = GameData.getData().crimeList[crime].variants[0];
            }

            return variant;
        }

        /*Note: This only checks if they have committed a crime for which the sentence is death;
        if death peanalty laws are Archconservative or Elite Liberal this doesn't matter as the sentence will always be death for the former and
        never for the latter.
        */
        private bool isDeathPenaltyApplicable()
        {
            foreach (string crime in CrimesWanted.Keys)
            {
                if (CrimesWanted[crime] == 0) continue;

                CrimeDef.CrimeVariant variant = null;

                if (GameData.getData().crimeList[crime].variants.Count > 1)
                {
                    foreach (CrimeDef.CrimeVariant testVariant in GameData.getData().crimeList[crime].variants)
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
                    variant = GameData.getData().crimeList[crime].variants[0];
                }

                if (variant.deathSentence) return true;
            }

            return false;
        }

        private int getCrimeLifeSentences(string crime)
        {
            if (CrimesWanted[crime] == 0) return 0;

            CrimeDef.CrimeVariant variant = null;

            if (GameData.getData().crimeList[crime].variants.Count > 1)
            {
                foreach (CrimeDef.CrimeVariant testVariant in GameData.getData().crimeList[crime].variants)
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
                variant = GameData.getData().crimeList[crime].variants[0];
            }

            if (variant.lifeSentence == 0) return 0;
            else return -(MasterController.GetMC().LCSRandom(variant.lifeSentence) - CrimesWanted[crime]);
        }

        private int getCrimeSentence(string crime)
        {
            CrimeDef.CrimeVariant variant = getCrimeVariant(crime);

            if (variant != null)
                return MasterController.GetMC().LCSRandom(variant.sentence) * CrimesWanted[crime];
            else
                return 0;
        }

        private void doProcess(object sender, EventArgs args)
        {
            if (!hasComponent<Liberal>()) return;

            MasterController mc = MasterController.GetMC();

            if(getComponent<Liberal>().status == Liberal.Status.JAIL_POLICE_CUSTODY)
            {
                //Since being returned from the hospital and this monthly check happen on the same day, we want to make sure that Liberals don't
                //immediately get interrogated, spending the next month in the police station instead, allowing them to be rescued.
                if (hospitalArrest)
                {
                    hospitalArrest = false;
                    return;
                }

                if((getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.MISSING) != 0)
                {
                   mc.addMessage("Cops have re-polluted " + getComponent<CreatureInfo>().getName() + "'s mind with Conservatism!", true);
                    getComponent<Liberal>().leaveLCS();
                    return;
                }

                if ((getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.ILLEGAL_IMMIGRANT) != 0 && 
                    MasterController.government.laws["IMMIGRATION"].alignment != Alignment.ELITE_LIBERAL)
                {
                    mc.addMessage(getComponent<CreatureInfo>().getName() + " has been shipped out to INS to face " + 
                        ((MasterController.government.laws["IMMIGRATION"].alignment == Alignment.ARCHCONSERVATIVE && 
                        MasterController.government.laws["DEATH_PENALTY"].alignment == Alignment.ARCHCONSERVATIVE) ? 
                        "execution." : "deportation."), true);

                    getComponent<Liberal>().leader.getComponent<CriminalRecord>().addCrime(Constants.CRIME_HIRE_ILLEGAL);

                    getComponent<Liberal>().leaveLCS();
                    return;
                }

                //Try to get Racketeering charge
                int copstrength = 100;
                if (MasterController.government.laws["POLICE"].alignment == Alignment.ARCHCONSERVATIVE)
                    copstrength = 200;
                if (MasterController.government.laws["POLICE"].alignment == Alignment.CONSERVATIVE)
                    copstrength = 150;
                if (MasterController.government.laws["POLICE"].alignment == Alignment.LIBERAL)
                    copstrength = 75;
                if (MasterController.government.laws["POLICE"].alignment == Alignment.ELITE_LIBERAL)
                    copstrength = 50;

                copstrength = (copstrength * Heat) / 4;
                if (copstrength > 200) copstrength = 200;

                int libStrength = getComponent<CreatureBase>().Juice + getComponent<CreatureBase>().BaseAttributes["HEART"].getModifiedValue() * 5
                    - getComponent<CreatureBase>().BaseAttributes["WISDOM"].getModifiedValue() * 5 + getComponent<CreatureBase>().Skills["PSYCHOLOGY"].level * 5;

                //Confession Check
                if(mc.LCSRandom(copstrength) > libStrength && getComponent<Liberal>().leader != null)
                {
                    string ratText = getComponent<CreatureInfo>().getName() + " has broken under the pressure and ratted " + getComponent<Liberal>().leader.getComponent<CreatureInfo>().getName() + " out!\n";
                    if (getComponent<Liberal>().leader.getComponent<Liberal>().status == Liberal.Status.SLEEPER)
                    {
                        ratText += "\n" + getComponent<Liberal>().leader.getComponent<CreatureInfo>().getName() + " has had to flee to the active LCS for protection.";
                        getComponent<Liberal>().leader.getComponent<Liberal>().status = Liberal.Status.ACTIVE;
                        getComponent<Liberal>().leader.getComponent<Liberal>().homeBase = getComponent<Liberal>().leader.getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER");
                        getComponent<Liberal>().leader.getComponent<Liberal>().goHome();
                        getComponent<Liberal>().leader.getComponent<Liberal>().setActivity("NONE");
                    }

                    if (getComponent<Liberal>().leader.getComponent<Liberal>().status != Liberal.Status.JAIL_PRISON)
                    {
                        getComponent<Liberal>().leader.getComponent<CriminalRecord>().addCrime(Constants.CRIME_RACKETEERING);
                        getComponent<Liberal>().leader.getComponent<CriminalRecord>().Confessions++;
                        ratText += "The traitor will testify in court.\n";
                    }
                    else
                    {
                        //This is a bit of a hack but it gets the right number and this person is getting nuked anyway.
                        CrimesWanted["RACKETEERING"] = 1;
                        int sentenceExtension = getCrimeSentence("RACKETEERING");

                        if (!getComponent<Liberal>().leader.getComponent<CriminalRecord>().deathPenalty && 
                            getComponent<Liberal>().leader.getComponent<CriminalRecord>().LifeSentences == 0)
                        {
                            getComponent<Liberal>().leader.getComponent<CriminalRecord>().CurrentSentence += sentenceExtension;
                            if (sentenceExtension > 0)
                            {
                                ratText += getComponent<Liberal>().leader.getComponent<CreatureInfo>().heShe() + " has had " +
                                    getComponent<Liberal>().leader.getComponent<CreatureInfo>().hisHer().ToLower() + " sentence extended by " +
                                    MasterController.NumberToWords(sentenceExtension).ToLower() + (sentenceExtension > 1 ? " months" : " month") + ".\n";
                            }
                        }
                    }

                    if (getComponent<Liberal>().homeBase.hasComponent<SafeHouse>())
                    {
                        ratText += "Safe houses may be compromised.";
                        getComponent<Liberal>().homeBase.getComponent<SafeHouse>().exposeBase();
                    }

                    mc.addMessage(ratText, true);
                    getComponent<Liberal>().leaveLCS();
                }
                else
                {
                    mc.addMessage(getComponent<CreatureInfo>().getName() + " is moved to the courthouse for trial.");
                    getComponent<CreatureBase>().Location = getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("GOVERNMENT_COURTHOUSE");
                    getComponent<Liberal>().status = Liberal.Status.JAIL_COURT;
                    //Clothes will be held in case of acquittal
                    clothing = getComponent<Inventory>().armor;
                    getComponent<Inventory>().equipArmor(Factories.ItemFactory.create("ARMOR_PRISONER"));
                    //Since the Courthouse won't "catch" the clothes dropped by equipArmor, manually persist it here.                    
                    if(clothing != null) clothing.persist();
                }
            }
            else if(getComponent<Liberal>().status == Liberal.Status.JAIL_COURT)
            {
                mc.addMessage(owner.getComponent<CreatureInfo>().getName() + " is standing trial.", true);

                trialActionQueue = mc.createSubQueue(() => 
                {                    
                    mc.uiController.closeUI();
                    preTrial();
                }, "Start pretrial",
                () => 
                {
                    mc.uiController.closeUI();
                    trialActionQueue = null;
                    mc.doNextAction();
                }, "close trial screen->Next action");
            }
            else if(getComponent<Liberal>().status == Liberal.Status.JAIL_PRISON)
            {
                TotalTimeServed++;

                if(!deathPenalty && (CurrentSentence > 1 || LifeSentences > 0))
                {
                    //Random events for people not on death row or about to be paroled
                    prisonScene();
                }
                
                //Commute sentence if death penalty has been outlawed.
                if(deathPenalty && MasterController.government.laws["DEATH_PENALTY"].alignment == Alignment.ELITE_LIBERAL)
                {
                    mc.addMessage(getComponent<CreatureInfo>().getName() + "'s death sentence has been commuted to life due to the abolition of the death penalty.", true);
                    deathPenalty = false;
                    LifeSentences += 1;
                }

                if(LifeSentences == 0 || deathPenalty)
                {
                    CurrentSentence--;

                    //Pause the death penalty countdown if the prison is closed, to allow for opportunities to rescue the Liberal
                    if (deathPenalty && getComponent<CreatureBase>().Location.getComponent<TroubleSpot>().closed > 0)
                    {
                        CurrentSentence++;
                    }

                    if (CurrentSentence == 0)
                    {
                        if (deathPenalty)
                        {
                            string executionMethod = "";

                            if(MasterController.government.laws["DEATH_PENALTY"].alignment == Alignment.ARCHCONSERVATIVE)
                            {
                                executionMethod = GameData.getData().executionTypeList["cruelandunusual"][mc.LCSRandom(GameData.getData().executionTypeList["cruelandunusual"].Count)];
                            }
                            else if(MasterController.government.laws["DEATH_PENALTY"].alignment == Alignment.CONSERVATIVE ||
                                MasterController.government.laws["DEATH_PENALTY"].alignment == Alignment.MODERATE)
                            {
                                executionMethod = GameData.getData().executionTypeList["standard"][mc.LCSRandom(GameData.getData().executionTypeList["standard"].Count)];
                            }
                            else
                            {
                                executionMethod = GameData.getData().executionTypeList["supposedlypainless"][mc.LCSRandom(GameData.getData().executionTypeList["supposedlypainless"].Count)];
                            }

                            mc.addMessage("FOR SHAME:\nToday, the Conservative Machine executed " + getComponent<CreatureInfo>().getName() + " by " + executionMethod + ".", true);
                            if(getComponent<Liberal>().leader != null)
                            {
                                mc.addMessage(getComponent<Liberal>().leader.getComponent<CreatureInfo>().getName() + " has failed the Liberal Crime Squad.\nIf you can't protect your own people, who can you protect?", true);
                                getComponent<Liberal>().leader.getComponent<CreatureBase>().juiceMe(-50, -50);
                            }

                            getComponent<CreatureBase>().doDie(new Die("was executed by " + executionMethod));
                            //If this execution resulted in the end of the LCS, set specific endgame state for execution
                            if (mc.endGameState == MasterController.EndGame.DEAD)
                                mc.endGameState = MasterController.EndGame.EXECUTED;
                        }
                        else
                        {
                            mc.addMessage(getComponent<CreatureInfo>().getName() + " has been released from prison.\nNo doubt there are some mental scars, but the Liberal is back.", true);
                            parole();
                        }
                    }
                    else
                    {
                        //notify of impending events
                        if(CurrentSentence == 1)
                        {
                            string notificationText = getComponent<CreatureInfo>().getName() + " is due to be ";

                            if (deathPenalty) notificationText += "<color=red>executed</color>";
                            else notificationText += "released";

                            notificationText += " next month.";

                            if(deathPenalty && getComponent<CreatureBase>().Location.getComponent<TroubleSpot>().closed > 0)
                            {
                                notificationText += "\nHowever, the closure of " + getComponent<CreatureBase>().Location.getComponent<SiteBase>().getCurrentName() + " has delayed " + getComponent<CreatureInfo>().getName() + "'s execution by an additional month.\nBut they won't wait forever...";
                            }

                            mc.addMessage(notificationText, true);                            
                        }
                        else if (deathPenalty)
                        {
                            if (getComponent<CreatureBase>().Location.getComponent<TroubleSpot>().closed > 0)
                            {
                                mc.addMessage("The closure of " + getComponent<CreatureBase>().Location.getComponent<SiteBase>().getCurrentName() + " has delayed " + getComponent<CreatureInfo>().getName() + "'s execution by a month. But they won't wait forever...");
                            }
                            else
                            {
                                mc.addMessage(getComponent<CreatureInfo>().getName() + " is due to be <color=red>executed</color> in " + MasterController.NumberToWords(CurrentSentence).ToLower() + " months.");
                            }
                        }
                    }
                }
            }
        }

        private void prisonScene()
        {
            MasterController mc = MasterController.GetMC();

            if (mc.LCSRandom(5) > 0) return;

            //Labor Camp
            if(MasterController.government.laws["PRISON"].alignment == Alignment.ARCHCONSERVATIVE)
            {
                string[] labor_camp_experiences =
                {
                    " is forced to operate dangerous machinery in prison.",
                    " is beaten by sadistic prison guards.",
                    " carries heavy burdens back and forth in prison labor camp.",
                    " does back-breaking work all month in prison.",
                    " gets in a brutal fight with another prisoner.",
                    " participates in a quickly-suppressed prison riot."
                };

                int escaped = 0;
                string message = getComponent<CreatureInfo>().getName();

                if(getComponent<Liberal>().leader == null && mc.LCSRandom(3) == 0)
                {
                    escaped = 2;
                    message += " leads the oppressed prisoners and overwhelms the prison guards!";
                }
                else if(getComponent<CreatureBase>().Skills[Constants.SKILL_DISGUISE].check(Difficulty.HEROIC) && mc.LCSRandom(10) == 0)
                {
                    escaped = 1;
                    message += " wears an electrician's outfit and rides away with some contractors.";
                    getComponent<Inventory>().equipArmor(Factories.ItemFactory.create("ARMOR_WORKCLOTHES"));
                }
                else if(getComponent<CreatureBase>().Skills[Constants.SKILL_SECURITY].check(Difficulty.CHALLENGING) &&
                        getComponent<CreatureBase>().Skills[Constants.SKILL_STEALTH].check(Difficulty.HARD) &&
                        mc.LCSRandom(10) == 0)
                {
                    escaped = 1;
                    message += " picks the lock on their leg chains and then sneaks away!";
                }
                else if(getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].check(Difficulty.HARD) &&
                        mc.LCSRandom(10) == 0)
                {
                    escaped = 1;
                    message += " consumes drugs that simulate death, and is thrown out with the trash!";
                }

                if (escaped == 0)
                {
                    message += labor_camp_experiences[mc.LCSRandom(labor_camp_experiences.Length)];
                    if(mc.LCSRandom(4) == 0)
                    {
                        if(getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].getModifiedValue() > 0)
                        {
                            message += "\n" + getComponent<CreatureInfo>().getName() + " is badly hurt in the process.";
                            getComponent<CreatureBase>().juiceMe(-40, 0);
                            getComponent<CreatureBase>().juiceMe(-10, -50);
                        }
                        else
                        {
                            message += "\n" + getComponent<CreatureInfo>().getName() + " is found dead.";
                            getComponent<CreatureBase>().doDie(new Die("was worked to death in a Conservative labor camp"));
                        }
                    }
                    else
                    {
                        message += "\n" + getComponent<CreatureInfo>().getName() + " managed to avoid lasting injury.";
                    }
                }
                else
                {
                    Entity prison = getComponent<CreatureBase>().Location;
                    message += "\n" + getComponent<CreatureInfo>().getName() + " escaped from prison!";
                    getComponent<CreatureBase>().juiceMe(50, 1000);
                    escape();

                    if(escaped == 2)
                    {
                        int num_escaped = 0;

                        foreach(Entity e in MasterController.lcs.getAllMembers())
                        {
                            if(e.getComponent<CreatureBase>().Location == prison && 
                                e.getComponent<Liberal>().status == Liberal.Status.JAIL_PRISON)
                            {
                                num_escaped++;
                                e.getComponent<CriminalRecord>().escape();
                            }
                        }

                        if(num_escaped > 1)
                            message += "\nThe LCS will rise again! Multiple LCS members escape!";
                        else if(num_escaped == 1)
                            message += "\nAnother imprisoned LCS member also gets out!";
                    }
                }

                mc.addMessage(message, true, true);
            }
            //Liberal Rehabilitation
            else if(MasterController.government.laws["PRISON"].alignment == Alignment.ELITE_LIBERAL)
            {
                string[] reeducation_experiences =
                {
                    " is subjected to rehabilitative therapy in prison.",
                    " works on a prison mural about political diversity.",
                    " routinely sees a Liberal therapist in prison.",
                    " participates in a group therapy session in prison.",
                    " sings songs with prisoners of all political persuasions.",
                    " is encouraged to befriend Conservatives in prison.",
                    " puts on an anti-crime performance in prison.",
                    " sees a video in prison by victims of political crime."
                };

                string message = getComponent<CreatureInfo>().getName() + reeducation_experiences[mc.LCSRandom(reeducation_experiences.Length)];

                if (getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].check(Difficulty.FORMIDABLE))
                {
                    if(getComponent<CreatureBase>().Juice > 0 && mc.LCSRandom(2) == 0)
                    {
                        message += "\n" + getComponent<CreatureInfo>().getName() + " feels bad about LCS actions, and loses juice!";
                        getComponent<CreatureBase>().juiceMe(-50, 0);
                    }
                    else if(mc.LCSRandom(15) > getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].getModifiedValue() ||
                        getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].getModifiedValue() <
                        getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].getModifiedValue())
                    {
                        message += "\n" + getComponent<CreatureInfo>().getName() + " silently grows Wiser...";
                        getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level++;
                    }
                    else if(getComponent<Liberal>().recruitType == Liberal.RecruitType.LOVE_SLAVE && mc.LCSRandom(4) != 0)
                    {
                        message += "\n" + getComponent<CreatureInfo>().getName() + " only stays loyal to the LCS for " + getComponent<Liberal>().leader.getComponent<CreatureInfo>().getName() + ".";
                    }
                    else
                    {
                        message += "\n" + getComponent<CreatureInfo>().getName() + " abandons the Liberal Crime Squad!";
                        if(getComponent<Liberal>().leader != null)
                        {
                            getComponent<Liberal>().leader.getComponent<CriminalRecord>().addCrime(Constants.CRIME_RACKETEERING);
                            getComponent<Liberal>().leader.getComponent<CriminalRecord>().Confessions++;
                        }
                        getComponent<Liberal>().leaveLCS();
                    }
                }
                else
                {
                    message += "\n" + getComponent<CreatureInfo>().getName() + " remains strong.";
                }

                mc.addMessage(message, true, true);
            }
            //Regular Prison Life
            else
            {
                string[] good_experiences =
               {
                  " advertises the LCS every day to other inmates.",
                  " organizes a group of inmates to beat up on a serial rapist.",
                  " learns lots of little skills from other inmates.",
                  " gets a prison tattoo with the letters L-C-S.",
                  " thinks up new protest songs while in prison."
               };
                string[] bad_experiences =
                {
                  " gets sick for a few days from nasty prison food.",
                  " spends too much time working out at the prison gym.",
                  " is raped by another prison inmate, repeatedly.",
                  " writes a letter to the warden swearing off political activism.",
                  " rats out one of the other inmates in exchange for benefits."
               };
                string[] general_experiences =
                {
                  " mouths off to a prison guard and ends up in solitary.",
                  " gets high off drugs smuggled into the prison.",
                  " does nothing but read books at the prison library.",
                  " gets into a fight and is punished with latrine duty.",
                  " constantly tries thinking how to escape from prison."
               };

                int escaped = 0;
                string message = getComponent<CreatureInfo>().getName();

                if(getComponent<CreatureBase>().Juice > (getComponent<Liberal>().leader == null ? 200 : 500))
                {
                    if(getComponent<Liberal>().leader == null && mc.LCSRandom(10) == 0)
                    {
                        escaped = 2;
                        message += " leads a riot with dozens of prisoners chanting the LCS slogan!";
                    }
                    else if(getComponent<CreatureBase>().Skills[Constants.SKILL_COMPUTERS].check(Difficulty.HARD) && mc.LCSRandom(5) == 0)
                    {
                        escaped = 2;
                        message += " codes a virus on a smuggled phone that opens all the prison doors!";
                    }
                    else if(getComponent<CreatureBase>().Skills[Constants.SKILL_DISGUISE].check(Difficulty.HARD) && mc.LCSRandom(5) == 0)
                    {
                        escaped = 1;
                        message += " puts on smuggled street clothes and calmly walks out of prison.";
                        getComponent<Inventory>().equipArmor(Factories.ItemFactory.create("ARMOR_CLOTHES"));
                    }
                    else if(getComponent<CreatureBase>().Skills[Constants.SKILL_SECURITY].check(Difficulty.CHALLENGING) &&
                            getComponent<CreatureBase>().Skills[Constants.SKILL_STEALTH].check(Difficulty.CHALLENGING) &&
                            mc.LCSRandom(5) == 0)
                    {
                        escaped = 1;
                        message += " jimmies the cell door and cuts the outer fence in the dead of night!";
                    }
                    else if(getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].check(Difficulty.AVERAGE) &&
                        getComponent<CreatureBase>().Skills[Constants.SKILL_MARTIAL_ARTS].check(Difficulty.EASY) &&
                        mc.LCSRandom(5) == 0)
                    {
                        escaped = 1;
                        message += " intentionally ODs on smuggled drugs, then breaks out of the medical ward!";
                    }
                }

                if(escaped == 0)
                {
                    if (getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].check(Difficulty.HARD))
                    {
                        if(mc.LCSRandom(2) == 0)
                            message += good_experiences[mc.LCSRandom(good_experiences.Length)];
                        else
                            message += general_experiences[mc.LCSRandom(general_experiences.Length)];

                        message += "\n" + getComponent<CreatureInfo>().getName() + " has become a more hardened, Juicier criminal.";
                        getComponent<CreatureBase>().juiceMe(20, 1000);
                    }
                    else if (getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].check(Difficulty.CHALLENGING))
                    {
                        message += general_experiences[mc.LCSRandom(general_experiences.Length)];
                        message += "\n" + getComponent<CreatureInfo>().getName() + " seems to be mostly fine, though.";
                    }
                    else
                    {
                        message += bad_experiences[mc.LCSRandom(bad_experiences.Length)];
                        message += "\n" + getComponent<CreatureInfo>().getName() + " is kinda losing it in here. Juice, that is.";
                        getComponent<CreatureBase>().juiceMe(-20, -30);
                    }
                }
                else
                {
                    Entity prison = getComponent<CreatureBase>().Location;
                    message += "\n" + getComponent<CreatureInfo>().getName() + " escaped from prison!";
                    getComponent<CreatureBase>().juiceMe(50, 1000);
                    escape();

                    if (escaped == 2)
                    {
                        int num_escaped = 0;

                        foreach (Entity e in MasterController.lcs.getAllMembers())
                        {
                            if (e.getComponent<CreatureBase>().Location == prison &&
                                e.getComponent<Liberal>().status == Liberal.Status.JAIL_PRISON)
                            {
                                num_escaped++;
                                e.getComponent<CriminalRecord>().escape();
                            }
                        }

                        if (num_escaped > 1)
                            message += "\nThe LCS will rise again! Multiple LCS members escape!";
                        else if (num_escaped == 1)
                            message += "\nAnother imprisoned LCS member also gets out!";
                    }
                }

                mc.addMessage(message, true, true);
            }
        }

        #region TRIAL
        public Entity sleeperLawyer;
        private Entity sleeperJudge;
        private int defensepower;
        private int jury;
        private TrialActions.TrialSelection selection;
        private ActionQueue trialActionQueue;
        public bool selectionMode;

        public void preTrial()
        {
            MasterController mc = MasterController.GetMC();
            Trial trial = mc.uiController.trial;
            selectionMode = false;            

            trial.show(owner);
            
            trial.printTitle(owner.getComponent<CreatureInfo>().getName() + " is standing trial.");
            trial.clearText();

            sleeperJudge = null;
            sleeperLawyer = null;

            foreach(Entity e in MasterController.lcs.getAllSleepers())
            {
                if ((e.def == "JUDGE_CONSERVATIVE" || e.def == "JUDGE_LIBERAL") && e.getComponent<Liberal>().infiltration > mc.LCSRandom(100))
                    sleeperJudge = e;
                if(e.def == "LAWYER")
                {
                    if (sleeperLawyer == null)
                        sleeperLawyer = e;
                    else if (sleeperLawyer.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].level + sleeperLawyer.getComponent<CreatureBase>().Skills[Constants.SKILL_LAW].level <
                        e.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].level + e.getComponent<CreatureBase>().Skills[Constants.SKILL_LAW].level)
                        sleeperLawyer = e;
                }
            }

            if (sleeperJudge != null)
            {
                trialActionQueue.Add(() => { trial.printText("Sleeper " + sleeperJudge.getComponent<CreatureInfo>().getName() + " reads the charges, trying to hide a smile:\n\n"); }, "print trial text");
            }
            else
            {
                trialActionQueue.Add(() => { trial.printText("The judge reads the charges:\n\n"); }, "print trial text");
            }

            int uniqueCrimes = 0;
            foreach (string crime in CrimesWanted.Keys)
            {
                if (CrimesWanted[crime] > 0) uniqueCrimes++;
            }

            int totalUniqueCrimes = uniqueCrimes;

            foreach (string crime in CrimesWanted.Keys)
            {
                if (CrimesWanted[crime] > 0)
                {
                    string crimeText = totalUniqueCrimes == uniqueCrimes ? "<color=red>The defendant " + getComponent<CreatureInfo>().givenName + " " + owner.getComponent<CreatureInfo>().surname + " is charged with " : "<color=red>";

                    if (CrimesWanted[crime] > 1 && !(crime == "RACKETEERING" || crime == "HIRE_ILLEGAL"))
                    {
                        crimeText += MasterController.NumberToWords(CrimesWanted[crime]).ToLower() + " counts of ";
                    }

                    crimeText += getCrimeName(crime).ToLower();

                    uniqueCrimes--;

                    if (uniqueCrimes > 1) crimeText += ", ";
                    else if (uniqueCrimes == 1) crimeText += " and ";
                    else crimeText += ".";

                    crimeText += "</color>";

                    trialActionQueue.Add(() => { trial.printText(crimeText); }, "print trial text");
                }
            }

            if (sleeperJudge == null && Confessions > 0)
            {
                string witnessText = "<color=yellow>";

                if (Confessions > 1)
                    witnessText += "\n\n" + MasterController.NumberToWords(Confessions) + " former LCS members will testify against ";
                else
                    witnessText += "\n\nA former LCS member will testify against ";

                witnessText += getComponent<CreatureInfo>().givenName + " " + getComponent<CreatureInfo>().surname + ".</color>";

                trialActionQueue.Add(() => { trial.printText(witnessText); }, "print trial text");
            }

            trialActionQueue.Add(() => 
            {
                trial.printText("\n\nHow does " + getComponent<CreatureInfo>().getName() + " conduct " + getComponent<CreatureInfo>().hisHer().ToLower() + " defense?");
                selectionMode = true;                
                trial.generateTrialButtons();
            }, "generate trial buttons");
        }

        public void mainTrial(TrialActions.TrialSelection selection)
        {
            // 0 = Public Defender
            // 1 = Defend Self
            // 2 = Plead Guilty
            // 3 = Ace Attorney
            // 4 = Sleeper Lawyer

            MasterController mc = MasterController.GetMC();
            Trial trial = mc.uiController.trial;
            selectionMode = false;

            trial.clearText();
            bool autoconvict = false;
            this.selection = selection;

            if (selection != TrialActions.TrialSelection.PLEAD_GUILTY)
            {
                trial.printText("The trial proceeds.  Jury selection is first.\n\n");

                jury = mc.LCSRandom(61) - (60 * MasterController.generalPublic.PublicMood) / 100;
                int prosecution = 0;

                if (sleeperJudge != null) jury -= 20;

                string aceAttorneyName = Factories.CreatureFactory.generateGivenName() + " " + Factories.CreatureFactory.generateSurname();

                string juryText = "";

                if (selection == TrialActions.TrialSelection.ACE_ATTORNEY)
                {
                    MasterController.lcs.changeFunds(-5000);

                    if (mc.LCSRandom(10) != 0)
                    {
                        juryText = aceAttorneyName + " ensures the jury is stacked in " + getComponent<CreatureInfo>().getName() + "'s favor!";
                        if (jury > 0) jury = 0;
                        jury -= 30;
                    }
                    else
                    {
                        juryText = "<color=red>" + aceAttorneyName + "'s CONSERVATIVE ARCH-NEMESIS will represent the prosecution!!!</color>";
                        jury = 0;
                        prosecution = 100;
                    }
                }
                else if (jury <= -29)
                {
                    juryText = "<color=lime>";
                    switch (mc.LCSRandom(4))
                    {
                        case 0:
                            juryText += getComponent<CreatureInfo>().getName() + "'s best friend from childhood is a juror.";
                            break;
                        case 1:
                            juryText += "The jury is Flaming Liberal.";
                            break;
                        case 2:
                            juryText += "A few of the jurors are closet Socialists.";
                            break;
                        case 3:
                            juryText += "One of the jurors flashes a SECRET LIBERAL HAND SIGNAL when no one is looking.";
                            break;
                    }

                    juryText += "</color>";
                }
                else if (jury <= -15) juryText = "The jury is fairly Liberal.";
                else if (jury < 15) juryText = "The jury is quite moderate.";
                else if (jury < 29) juryText = "The jury is a bit Conservative.";
                else
                {
                    juryText = "<color=red>";
                    switch (mc.LCSRandom(4))
                    {
                        case 0:
                            juryText += "Such a collection of Conservative jurors has never before been assembled.";
                            break;
                        case 1:
                            juryText += "One of the accepted jurors is a Conservative activist.";
                            break;
                        case 2:
                            juryText += "A few of the jurors are members of the KKK.";
                            break;
                        case 3:
                            juryText += "The jury is frighteningly Conservative.";
                            break;
                    }

                    juryText += "</color>";
                }

                if (mc.DebugMode) juryText += " (Jury=" + jury + ")";
                juryText += "\n\n";
                trialActionQueue.Add(() => { trial.printText(juryText); }, "print trial text");

                prosecution += 40 + mc.LCSRandom(101) + getScareFactor() + (20 * Confessions);

                if (autoconvict)
                {
                    trialActionQueue.Add(() => { trial.printText("There is no question of " + getComponent<CreatureInfo>().getName() + "'s guilt."); }, "print trial text");
                }
                else
                {
                    string prosecutionText = "";

                    if (prosecution <= 50) prosecutionText = "The prosecution's presentation is terrible.";
                    else if (prosecution <= 75) prosecutionText = "The prosecution gives a standard presentation.";
                    else if (prosecution <= 125) prosecutionText = "The prosecution's case is solid.";
                    else if (prosecution <= 175) prosecutionText = "The prosecution makes an airtight case.";
                    else prosecutionText = "The prosecution is incredibly strong.";

                    if (mc.DebugMode) prosecutionText += " (Prosecution=" + prosecution + ")";
                    prosecutionText += "\n\n";

                    trialActionQueue.Add(() => { trial.printText(prosecutionText); }, "print trial text");
                }

                jury += mc.LCSRandom(prosecution / 2 + 1) + prosecution / 2;

                defensepower = 0;
                string defenseText = "";
                string attorneyName = "";
                if (selection == TrialActions.TrialSelection.SLEEPER_ATTORNEY) attorneyName = sleeperLawyer.getComponent<CreatureInfo>().getName();
                else if (selection == TrialActions.TrialSelection.ACE_ATTORNEY) attorneyName = aceAttorneyName;

                if (selection != TrialActions.TrialSelection.DEFEND_SELF)
                {
                    if (autoconvict) defenseText = "The defense makes a noble attempt, but the outcome is inevitable.";
                    else
                    {
                        if (selection == TrialActions.TrialSelection.PUBLIC_DEFENDER) defensepower = mc.LCSRandom(71);    // Court-appointed attorney
                        else if (selection == TrialActions.TrialSelection.ACE_ATTORNEY) defensepower = mc.LCSRandom(71) + 80; // Ace Liberal attorney
                        else if (selection == TrialActions.TrialSelection.SLEEPER_ATTORNEY)
                        {
                            // Sleeper attorney
                            defensepower = mc.LCSRandom(71) + sleeperLawyer.getComponent<CreatureBase>().Skills["LAW"].level * 2
                                                      + sleeperLawyer.getComponent<CreatureBase>().Skills["PERSUASION"].level * 2;
                            sleeperLawyer.getComponent<CreatureBase>().Skills["LAW"].addExperience(prosecution / 4);
                            sleeperLawyer.getComponent<CreatureBase>().Skills["PERSUASION"].addExperience(prosecution / 4);
                        }
                    }

                    if (defensepower <= 5) defenseText = "The defense attorney rarely showed up.";
                    else if (defensepower <= 15) defenseText = "The defense attorney accidentally said \"My client is GUILTY!\" during closing.";
                    else if (defensepower <= 25) defenseText = "The defense is totally lame.";
                    else if (defensepower <= 50) defenseText = "The defense was lackluster.";
                    else if (defensepower <= 75) defenseText = "Defense arguments were pretty good.";
                    else if (defensepower <= 100) defenseText = "The defense was really slick.";
                    else if (defensepower <= 145)
                    {
                        if (prosecution < 100) defenseText = "The defense makes the prosecution look like amateurs.";
                        else defenseText = "The defense is extremely compelling.";
                    }
                    else
                    {
                        if (prosecution < 100)
                        {
                            defenseText = attorneyName + "'s arguments made several of the jurors stand up and shout \"NOT GUILTY!\" before deliberations even began.";
                            if (selection == TrialActions.TrialSelection.SLEEPER_ATTORNEY) sleeperLawyer.getComponent<CreatureBase>().juiceMe(10, 500); // Bow please
                        }
                        else
                        {
                            defenseText = attorneyName + " conducts an incredible defense.";
                        }
                    }
                }
                else
                {
                    defensepower = 5 * (getComponent<CreatureBase>().Skills["PERSUASION"].roll() - 3) +
                        10 * (getComponent<CreatureBase>().Skills["LAW"].roll() - 3);
                    getComponent<CreatureBase>().Skills["PERSUASION"].addExperience(50);
                    getComponent<CreatureBase>().Skills["LAW"].addExperience(50);

                    defenseText = getComponent<CreatureInfo>().getName();
                    if (defensepower <= 0)
                    {
                        switch (mc.LCSRandom(3))
                        {
                            case 0:
                                defenseText += " makes one horrible mistake after another.";
                                break;
                            case 1:
                                defenseText += " forgot where " + getComponent<CreatureInfo>().heShe().ToLower() + " was and shouted \"DEATH TO THE PATRIARCHY\" at the judge.";
                                getComponent<CreatureBase>().juiceMe(1, 50);
                                break;
                            case 2:
                                defenseText += " accidentally flips off the judge and jury multiple times.";
                                break;
                        }

                        getComponent<CreatureBase>().juiceMe(-10, -50); // You should be ashamed
                    }
                    else if (defensepower <= 25) defenseText += "'s case really sucked.";
                    else if (defensepower <= 50) defenseText += " did all right, but made some mistakes.";
                    else if (defensepower <= 75) defenseText += "'s arguments were pretty good.";
                    else if (defensepower <= 100) defenseText += " worked the jury very well.";
                    else if (defensepower <= 150) defenseText += " made a very powerful case.";
                    else
                    {
                        defenseText += " had the jury, judge, and prosecution crying for freedom.";
                        getComponent<CreatureBase>().juiceMe(50, 1000); // That shit is legend
                    }
                }

                if (mc.DebugMode) defenseText += " (defensePower= " + defensepower + ")";
                defenseText += "\n\n";
                trialActionQueue.Add(() => { trial.printText(defenseText); }, "print trial text");

                trialActionQueue.Add(() => { trial.printText("The jury leaves to consider the case.\n\n"); }, "print trial text");

                verdict();
            }
            else
            {
                trial.printText("The court accepts the plea.\n\n");

                if (sleeperJudge != null || mc.LCSRandom(2) == 0) sentence(true);
                else sentence(false);
            }
        }

        public void verdict()
        {
            MasterController mc = MasterController.GetMC();
            Trial trial = mc.uiController.trial;
            trialActionQueue.Add(() =>
            {
                trial.clearText();
                trial.printText("The jury has returned from deliberations.\n\n");
            }, "print trial text");

            //Hung Jury
            if (jury == defensepower)
            {
                trialActionQueue.Add(() => { trial.printText("<color=yellow>But they can't reach a verdict!</color>\n"); }, "print trial text");

                //Re-try
                if (mc.LCSRandom(2) == 0 || getScareFactor() >= 10 || Confessions > 0)
                {
                    trialActionQueue.Add(() => { trial.printText("The case will be re-tried next month."); }, "print trial text");
                    return;
                }
                else
                {
                    trialActionQueue.Add(() => { trial.printText("The prosecution declines to re-try the case.\n\n"); }, "print trial text");
                    if (CurrentSentence == 0)
                    {
                        trialActionQueue.Add(() => { trial.printText("<color=lime>" + getComponent<CreatureInfo>().getName() + " is free!</color>"); }, "print trial text");
                        acquit();
                        return;
                    }
                    else
                    {
                        string prisonText = getComponent<CreatureInfo>().getName() + " will be returned to prison to resume an earlier sentence";

                        if (!deathPenalty && CurrentSentence > 1 &&
                            (mc.LCSRandom(2) == 0 || sleeperJudge != null))
                        {
                            CurrentSentence--;
                            prisonText += ", less a month for time already served.";
                        }
                        else prisonText += ".";

                        if (deathPenalty)
                        {
                            CurrentSentence = 3;
                            prisonText += " The Execution is scheduled to occur three months from now.";
                        }

                        trialActionQueue.Add(() => { trial.printText(prisonText); }, "print trial text");
                    }
                }
            }
            else if (defensepower > jury)
            {
                trialActionQueue.Add(() => { trial.printText("<b><color=lime>NOT GUILTY!</color></b>\n\n"); }, "print trial text");
                if (CurrentSentence == 0)
                {
                    trialActionQueue.Add(() => { trial.printText("<color=lime>" + getComponent<CreatureInfo>().getName() + " is free!</color>"); }, "print trial text");
                    mc.addMessage(getComponent<CreatureInfo>().getName() + " is free!");
                    acquit();
                    return;
                }
                else
                {
                    string prisonText = getComponent<CreatureInfo>().getName() + " will be returned to prison to resume an earlier sentence";

                    if (!deathPenalty && CurrentSentence > 1 &&
                        (mc.LCSRandom(2) == 0 || sleeperJudge != null))
                    {
                        CurrentSentence--;
                        prisonText += ", less a month for time already served.";
                    }
                    else prisonText += ".";

                    if (deathPenalty)
                    {
                        CurrentSentence = 3;
                        prisonText += " The Execution is scheduled to occur three months from now.";
                    }

                    trialActionQueue.Add(() => { trial.printText(prisonText); }, "print trial text");
                }

                if (selection == TrialActions.TrialSelection.DEFEND_SELF) getComponent<CreatureBase>().juiceMe(10, 100);
                else if (selection == TrialActions.TrialSelection.SLEEPER_ATTORNEY) sleeperLawyer.getComponent<CreatureBase>().juiceMe(10, 100);
            }
            else
            {
                trialActionQueue.Add(() => { trial.printText("<b><color=red>GUILTY!</color></b>\n\n"); }, "print trial text");

                if (selection == TrialActions.TrialSelection.SLEEPER_ATTORNEY) sleeperLawyer.getComponent<CreatureBase>().juiceMe(-5, 0);

                getComponent<CreatureBase>().juiceMe(25, 200);

                if (defensepower / 3 >= jury / 4 || sleeperJudge != null) sentence(true);
                else sentence(false);
            }
        }

        private void sentence(bool lenience)
        {
            MasterController mc = MasterController.GetMC();
            Trial trial = mc.uiController.trial;

            bool oldDeathPenalty = deathPenalty;
            int oldSentence = CurrentSentence;
            int oldLifeSentence = LifeSentences;

            calculateSentence(lenience);
            imprison();

            int newSentence = CurrentSentence - oldSentence;
            int newLifeSentence = LifeSentences - oldLifeSentence;

            if (lenience)
            {
                trialActionQueue.Add(() => { trial.printText("During sentencing, the judge grants some leniency.\n\n"); }, "print trial text");
            }

            if (oldDeathPenalty)
            {
                deathPenalty = true;
                CurrentSentence = 3;

                trialActionQueue.Add(() => { trial.printText(getComponent<CreatureInfo>().givenName + " " + getComponent<CreatureInfo>().surname + ", you will be returned to prison to carry out your death sentence.\n"); }, "print trial text");
                trialActionQueue.Add(() => { trial.printText("The execution is scheduled to occur three months from now."); }, "print trial text");
            }
            else if (deathPenalty)
            {
                trialActionQueue.Add(() => { trial.printText("<color=red>" + getComponent<CreatureInfo>().givenName + " " + getComponent<CreatureInfo>().surname + ", you are sentenced to <b>DEATH</b>!</color>\n"); }, "print trial text");
                trialActionQueue.Add(() => { trial.printText("The execution is scheduled to occur three months from now."); }, "print trial text");
                MasterController.GetMC().addMessage(getComponent<CreatureInfo>().getName() + " is sentenced to <color=red>DEATH</color>");
            }
            else if (oldLifeSentence > 0 || (oldSentence > 0 && newSentence == 0))
            {
                trialActionQueue.Add(() => { trial.printText(getComponent<CreatureInfo>().givenName + " " + getComponent<CreatureInfo>().surname + ", the court sees no need to add to your existing sentence.\n"); }, "print trial text");
                string sentenceText = "You will be returned to prison to resume it";
                if (lenience && CurrentSentence > 1)
                {
                    CurrentSentence--;
                    sentenceText += ", less a month for time already served";
                }
                sentenceText += ".";

                trialActionQueue.Add(() => { trial.printText(sentenceText); }, "print trial text");
            }
            else if (CurrentSentence == 0 && LifeSentences == 0)
            {
                trialActionQueue.Add(() => { trial.printText(getComponent<CreatureInfo>().givenName + " " + getComponent<CreatureInfo>().surname + ", consider this a warning.  You are free to go."); }, "print trial text");
                MasterController.GetMC().addMessage(getComponent<CreatureInfo>().getName() + " is let off with a warning.");
            }
            else
            {
                string sentenceText = getComponent<CreatureInfo>().givenName + " " + getComponent<CreatureInfo>().surname + ", you are sentenced to ";

                if (LifeSentences > 0)
                {
                    if (LifeSentences > 1)
                    {
                        sentenceText += LifeSentences + " consecutive life terms in prison";
                        if (oldSentence >= 0)
                        {
                            sentenceText += ".\n\n" + "Have a nice day, " + getComponent<CreatureInfo>().givenName + " " + getComponent<CreatureInfo>().surname;
                        }
                    }
                    else sentenceText += "life in prison";

                    MasterController.GetMC().addMessage(getComponent<CreatureInfo>().getName() + " is sentenced to life in prison.");
                }
                else if (newSentence >= 36)
                {
                    sentenceText += MasterController.NumberToWords(newSentence / 12).ToLower();
                    sentenceText += " years in prison";
                    MasterController.GetMC().addMessage(getComponent<CreatureInfo>().getName() + " is sentenced to " + newSentence / 12 + " years in prison");
                }
                else
                {
                    sentenceText += MasterController.NumberToWords(newSentence).ToLower();
                    sentenceText += " month";
                    if (newSentence > 1) sentenceText += "s";
                    sentenceText += " in prison";
                    MasterController.GetMC().addMessage(getComponent<CreatureInfo>().getName() + " is sentenced to " + newSentence + (newSentence > 1 ? " months" : " month") + " in prison.");
                }

                if ((oldSentence > 0 && newSentence > 0) || (oldLifeSentence > 0 && newLifeSentence > 0))
                {
                    sentenceText += ", ";
                    if (lenience)
                    {
                        if (oldSentence > newSentence)
                        {
                            CurrentSentence = oldSentence;
                        }
                        else
                        {
                            CurrentSentence = newSentence;
                        }

                        sentenceText += "to be served concurrently";
                    }
                    else
                    {
                        sentenceText += "to be served consecutively";
                    }
                }

                sentenceText += ".";

                trialActionQueue.Add(() => { trial.printText(sentenceText); }, "print trial text");

                if (getComponent<Liberal>().leader != null && getComponent<Liberal>().leader.getComponent<CreatureBase>().Juice > 50)
                {
                    int juice = getComponent<CreatureBase>().Juice / 10;
                    if (juice < 5) juice = 5;
                    getComponent<Liberal>().leader.getComponent<CreatureBase>().juiceMe(-juice, 0);
                }
            }
        }

        private string getCrimeName(string crime)
        {
            CrimeDef.CrimeVariant variant = null;

            if (GameData.getData().crimeList[crime].variants.Count > 1)
            {
                foreach (CrimeDef.CrimeVariant testVariant in GameData.getData().crimeList[crime].variants)
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
                variant = GameData.getData().crimeList[crime].variants[0];
            }

            if (variant != null) return variant.courtName;
            else return "Causing Bugs (Bad crime def: " + crime + ")";
        }
        #endregion
    }
}
