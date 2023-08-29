using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using LCS.Engine.Components.Location;
using LCS.Engine.Components.Item;
using LCS.Engine.Components.World;

namespace LCS.Engine.Components.Creature
{
    public class Hostage : Component
    {
        public Dictionary<Entity, float> rapport { get; set; }
        [SimpleSave]
        public int timeInCaptivity;
        [SimpleSave]
        public Tactics tactics;
        [SimpleSave]
        public Entity leadInterrogator;
        [SimpleSave]
        public int drugUse;

        [Flags]
        public enum Tactics
        {
            NONE = 0,
            CONVERT = 1,
            RESTRAIN = 2,
            ASSAULT = 4,
            USE_PROPS = 8,
            USE_DRUGS = 16,
            KILL = 32
        }

        public Hostage()
        {
            timeInCaptivity = 0;
            rapport = new Dictionary<Entity, float>();

            tactics |= Tactics.CONVERT | Tactics.RESTRAIN;
            drugUse = 0;
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Hostage");
                entityNode.AppendChild(saveNode);
            }

            saveSimpleFields();

            if (saveNode.SelectSingleNode("rapport") != null)
                saveNode.RemoveChild(saveNode.SelectSingleNode("rapport"));

            XmlNode rapportNode = saveNode.OwnerDocument.CreateElement("rapport");
            saveNode.AppendChild(rapportNode);

            foreach(Entity e in rapport.Keys)
            {
                XmlNode rapportEntityNode = rapportNode.OwnerDocument.CreateElement("rapportEntity");
                rapportNode.AppendChild(rapportEntityNode);
                XmlAttribute amt = rapportNode.OwnerDocument.CreateAttribute("amt");
                amt.Value = rapport[e].ToString();
                rapportEntityNode.Attributes.Append(amt);
                rapportEntityNode.InnerText = e.guid.ToString();
            }
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);

            foreach(XmlNode node in componentData.SelectSingleNode("rapport").ChildNodes)
            {
                try
                {
                    rapport.Add(entityList[int.Parse(node.InnerText)], float.Parse(node.Attributes["amt"].Value));
                }
                catch (KeyNotFoundException)
                {
                    MasterController.GetMC().addErrorMessage("Entity reference " + int.Parse(node.InnerText) + " not found on object " + owner.def + ":" + componentData.ParentNode.Attributes["guid"].Value + ":" + componentData.Name + ":rapport");
                }
            }
        }

        public override void subscribe()
        {
            base.subscribe();
            MasterController.GetMC().nextDay += doDaily;
        }

        public override void unsubscribe()
        {
            base.unsubscribe();
            MasterController.GetMC().nextDay -= doDaily;
            getComponent<CreatureBase>().die -= doDie;
        }

        public override void selfSubscribe()
        {
            base.selfSubscribe();
            getComponent<CreatureBase>().die += doDie;
        }
        
        public void toggleTactic(Tactics tactic)
        {
            if ((tactics & tactic) != 0)
                tactics &= ~tactic;
            else
                tactics |= tactic;
        }

        private void doDaily(object sender, EventArgs arg)
        {
            if ((getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.KIDNAPPED) == 0)
            {
                if (MasterController.GetMC().LCSRandom(14) + 5 < timeInCaptivity)
                {
                    getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.KIDNAPPED;

                    News.NewsStory story = new News.NewsStory();
                    story.type = "KIDNAPREPORT";
                    story.subject = owner;
                    story.location = getComponent<CreatureInfo>().workLocation;
                    MasterController.news.stories.Add(story);
                }
            }

            if (timeInCaptivity > 0 && getComponent<Body>().Alive)
                doInterrogation();

            timeInCaptivity++;            
        }

        private void doDie(object sender, EventArgs arg)
        {
            foreach(Entity e in rapport.Keys)
            {
                if (e.getComponent<Liberal>().dailyActivity.type == "INTERROGATE" && e.getComponent<Liberal>().dailyActivity.interrogationTarget == owner)
                    e.getComponent<Liberal>().setActivity("NONE");
            }

            removeMe();
        }

        public void refreshInterrogation()
        {
            List<Entity> interrogators = new List<Entity>();

            foreach(Entity e in getComponent<CreatureBase>().Location.getComponent<SafeHouse>().getBasedLiberals())
                if(e.getComponent<Liberal>().dailyActivity.interrogationTarget == owner)
                    interrogators.Add(e);

            if(interrogators.Count == 0)
            {
                leadInterrogator = null;
                return;
            }

            //If the lead interrogator has stopped interrogating, or there isn't one, pick a new one.
            if(leadInterrogator == null || !interrogators.Contains(leadInterrogator))
                leadInterrogator = interrogators[MasterController.GetMC().LCSRandom(interrogators.Count)];

            foreach(Entity e in interrogators)
            {
                if (!rapport.ContainsKey(e))
                    rapport.Add(e, 0);
            }
        }

        private void doInterrogation()
        {
            MasterController mc = MasterController.GetMC();
            List<Entity> interrogators = new List<Entity>();

            foreach (Entity e in getComponent<CreatureBase>().Location.getComponent<SafeHouse>().getBasedLiberals())
                if (e.getComponent<Liberal>().dailyActivity.interrogationTarget == owner &&
                    (e.getComponent<Liberal>().squad == null || e.getComponent<Liberal>().squad.target == null))
                    interrogators.Add(e);

            //Escape attempt?
            if(interrogators.Count == 0 || (tactics & Tactics.RESTRAIN) == 0)
            {
                if((mc.LCSRandom(200) + 25*interrogators.Count < 
                    getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].getModifiedValue() +
                    getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_AGILITY].getModifiedValue() +
                    getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_STRENGTH].getModifiedValue()) &&
                    timeInCaptivity >= 5)
                {
                    mc.addMessage(getComponent<CreatureInfo>().getName() + " has escaped!", true);
                    getComponent<CreatureBase>().Location.getComponent<SafeHouse>().timeUntilLocated = 3;
                    foreach (Entity e in rapport.Keys)
                        e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_KIDNAPPING);

                    foreach (Entity e in getComponent<CreatureBase>().Location.getComponent<SafeHouse>().getBasedLiberals())
                        if (e.getComponent<Liberal>().dailyActivity.interrogationTarget == owner)
                            e.getComponent<Liberal>().setActivity("NONE");
                    
                    owner.depersist();
                    owner.getComponent<CreatureBase>().Location = null;
                    return;
                }
            }

            if (interrogators.Count == 0) return;
            string interrogationText = "The Education of " + getComponent<CreatureInfo>().getName() + ": Day " + timeInCaptivity;
            
            if((tactics & Tactics.KILL) != 0)
            {
                interrogationText = "The <color=red>Final</color> Education of " + getComponent<CreatureInfo>().getName() + ": Day " + timeInCaptivity;
                Entity killer = null;

                foreach (Entity e in interrogators)
                {
                    if (mc.LCSRandom(50) < e.getComponent<CreatureBase>().Juice ||
                        mc.LCSRandom(9) + 1 >= e.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level)
                    {
                        killer = e;
                        break;
                    }
                }

                if(killer != null)
                {
                    interrogationText += "\n<color=magenta>" + killer.getComponent<CreatureInfo>().getName() + " executes " + getComponent<CreatureInfo>().getName() + " by ";
                    switch (mc.LCSRandom(5))
                    {
                        case 0: interrogationText += "strangling it to death."; break;
                        case 1: interrogationText += "beating it to death."; break;
                        case 2: interrogationText += "burning photos of Reagan in front of it."; break;
                        case 3: interrogationText += "telling it that taxes have been increased."; break;
                        case 4: interrogationText += "telling it its parents wanted to abort it."; break;
                    }
                    interrogationText += "</color>";

                    if(mc.LCSRandom(killer.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level) > mc.LCSRandom(3))
                    {
                        killer.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level--;
                        interrogationText += "\n<color=lime>" + killer.getComponent<CreatureInfo>().getName() + " feels sick to " + killer.getComponent<CreatureInfo>().hisHer().ToLower() + " stomach afterward and ";
                        switch (mc.LCSRandom(4))
                        {
                            case 0: interrogationText += "throws up in a trash can."; break;
                            case 1: interrogationText += "gets drunk, eventually falling asleep."; break;
                            case 2: interrogationText += "curls up in a ball, crying softly."; break;
                            case 3: interrogationText += "shoots up and collapses in a heap on the floor."; break;
                        }
                        interrogationText += "</color>";
                    }
                    else if(mc.LCSRandom(3) == 0)
                    {
                        interrogationText += "\n<color=cyan>" + killer.getComponent<CreatureInfo>().getName() + " grows colder.";
                        killer.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level++;
                    }
                    getComponent<CreatureBase>().doDie(new Events.Die("was executed by " + killer.getComponent<CreatureInfo>().getName()));
                }
                else
                {
                    interrogationText += "\n<color=yellow>There is no one able to get up the nerve to execute " + getComponent<CreatureInfo>().getName() + " in cold blood.</color>";
                    interrogationText = fullInterrogation(interrogationText, true);
                }
            }
            else
            {
                interrogationText = fullInterrogation(interrogationText);
            }

            mc.addMessage(interrogationText, true);
        }

        private string fullInterrogation(string interrogationText, bool triedToExecute = false)
        {
            MasterController mc = MasterController.GetMC();
            List<Entity> interrogators = new List<Entity>();
            Entity tempLeadInterrogator = leadInterrogator;

            foreach (Entity e in getComponent<CreatureBase>().Location.getComponent<SafeHouse>().getBasedLiberals())
                if (e.getComponent<Liberal>().dailyActivity.interrogationTarget == owner &&
                    (e.getComponent<Liberal>().squad == null || e.getComponent<Liberal>().squad.target == null))
                    interrogators.Add(e);

            if (!interrogators.Contains(leadInterrogator))
                tempLeadInterrogator = interrogators[mc.LCSRandom(interrogators.Count)];

            int business = 0;
            int religion = 0;
            int science = 0;
            int attack = 0;
            bool turned = false;

            bool boughtDrugs = false;
            bool boughtProps = false;

            if (!triedToExecute)
            {
                if (MasterController.lcs.Money >= 50 && (tactics & Tactics.USE_DRUGS) != 0)
                {
                    boughtDrugs = true;
                    MasterController.lcs.changeFunds(-50);
                }
                if(MasterController.lcs.Money >= 250 && (tactics & Tactics.USE_PROPS) != 0)
                {
                    boughtProps = true;
                    MasterController.lcs.changeFunds(-250);
                }
            }

            Dictionary<Entity, int> indivAttack = new Dictionary<Entity, int>();
            foreach (Entity e in interrogators)
            {
                if (e.getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].level > business)
                    business = e.getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].level;
                if (e.getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].level > religion)
                    religion = e.getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].level;
                if (e.getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].level > science)
                    science = e.getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].level;

                indivAttack[e] = e.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].getModifiedValue() +
                    e.getComponent<CreatureBase>().Skills[Constants.SKILL_PSYCHOLOGY].level * 2 +
                    e.getComponent<Inventory>().getArmor().getComponent<Armor>().getInterrogationBasePower() -
                    e.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].getModifiedValue();

                if (indivAttack[e] < 0) indivAttack[e] = 0;
                if (indivAttack[e] > attack) attack = indivAttack[e];
            }

            attack += interrogators.Count;
            attack += timeInCaptivity;
            attack += business - getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].level;
            attack += religion - getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].level;
            attack += science - getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].level;
            attack += tempLeadInterrogator.getComponent<CreatureBase>().Skills[Constants.SKILL_PSYCHOLOGY].roll() -
                getComponent<CreatureBase>().Skills[Constants.SKILL_PSYCHOLOGY].roll();
            attack += getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].roll();
            attack -= getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].roll() * 2;

            interrogationText += "\nThe Automaton";
            if((tactics & Tactics.RESTRAIN) != 0)
            {
                interrogationText += " is tied hands and feet to a metal chair in the middle of a back room.";
            }
            else
            {
                interrogationText += " is locked in a back room converted into a makeshift cell.";
            }

            //DRUGS
            if(boughtDrugs)
            {
                interrogationText += "\nIt is subjected to dangerous hallucinogens";

                int drugBonus = 10 + tempLeadInterrogator.getComponent<Inventory>().getArmor().getComponent<Armor>().getInterrogationDrugBonus();
                drugUse++;
                if(mc.LCSRandom(50) < drugUse)
                {
                    getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].Level--;
                    interrogationText += "\n<color=yellow>It foams at the mouth and its eyes roll back into its skull!</color>";

                    Entity doctor = tempLeadInterrogator;
                    foreach(Entity e in interrogators)
                    {
                        if (e.getComponent<CreatureBase>().Skills[Constants.SKILL_FIRST_AID].level > doctor.getComponent<CreatureBase>().Skills[Constants.SKILL_FIRST_AID].level)
                            doctor = e;
                    }

                    if(getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].Level <= 0 ||
                        doctor.getComponent<CreatureBase>().Skills[Constants.SKILL_FIRST_AID].level == 0)
                    {
                        if(doctor.getComponent<CreatureBase>().Skills[Constants.SKILL_FIRST_AID].level > 0)
                        {
                            interrogationText += "\n<color=magenta>" + doctor.getComponent<CreatureInfo>().getName() + " uses a defibrillator repeatedly but " + getComponent<CreatureInfo>().getName() + " flatlines.\nIt is a lethal overdose in " + getComponent<CreatureInfo>().getName() + "'s weakened state.</color>";
                        }
                        else
                        {
                            interrogationText += "\n<color=magenta>" + doctor.getComponent<CreatureInfo>().getName() + " has a panic attack and " + mc.swearFilter("shits " + doctor.getComponent<CreatureInfo>().hisHer().ToLower() + " pants", " makes a stinky") + ".\n" + getComponent<CreatureInfo>().getName() + " dies due to " + doctor.getComponent<CreatureInfo>().getName() + "'s incompetence at first aid.</color>";
                        }

                        getComponent<CreatureBase>().doDie(new Events.Die("died of drug overdose"));
                        return interrogationText;
                    }
                    else
                    {
                        if (doctor.getComponent<CreatureBase>().Skills[Constants.SKILL_FIRST_AID].check(Difficulty.CHALLENGING))
                        {
                            doctor.getComponent<CreatureBase>().Skills[Constants.SKILL_FIRST_AID].addExperience(5 * Math.Max(0, 10 - doctor.getComponent<CreatureBase>().Skills[Constants.SKILL_FIRST_AID].level), 10);
                            interrogationText += "\n<color=lime>" + doctor.getComponent<CreatureInfo>().getName() + " deftly rescues it from cardiac arrest with a defibrillator, skillfully saving it from any health damage.</color>";
                            getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].Level++;
                            //Drugs flushed from their system
                            drugUse = 0;
                            drugBonus = 0;
                        }
                        else
                        {
                            doctor.getComponent<CreatureBase>().Skills[Constants.SKILL_FIRST_AID].addExperience(5 * Math.Max(0, 5 - doctor.getComponent<CreatureBase>().Skills[Constants.SKILL_FIRST_AID].level), 5);
                            interrogationText += "\n<color=yellow>" + doctor.getComponent<CreatureInfo>().getName() + " clumsily rescues it from cardiac arrest with a defibrillator.</color>";
                            interrogationText += "\n" + getComponent<CreatureInfo>().getName() + " had a near-death experience and met ";
                            if (getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].level > 0)
                                interrogationText += "God in Heaven.";
                            else
                                interrogationText += "John Lennon";
                            //Hell of a trip, man
                            drugBonus *= 2;
                        }
                        rapport[doctor] += 0.5f;
                    }
                }

                attack += drugBonus;
            }

            //BEATING
            if((tactics & Tactics.ASSAULT) != 0 && !triedToExecute)
            {
                int forceroll = 0;
                bool tortured = false;

                foreach (Entity e in interrogators)
                {
                    forceroll += e.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_STRENGTH].roll() + e.getComponent<Inventory>().getArmor().getComponent<Armor>().getInterrogationAssaultBonus();
                    rapport[e] -= 0.4f;
                }

                if(!tempLeadInterrogator.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].check(Difficulty.EASY) && boughtProps)
                {
                    tortured = true;
                    forceroll *= 5;
                    rapport[tempLeadInterrogator] -= 3;

                    interrogationText += "\n<color=magenta>" + tempLeadInterrogator.getComponent<CreatureInfo>().getName();

                    switch (mc.LCSRandom(6))
                    {
                        case 0: interrogationText += " reenacts scenes from Abu Ghraib"; break;
                        case 1: interrogationText += " whips the Automaton with a steel cable"; break;
                        case 2: interrogationText += " holds the hostage's head under water"; break;
                        case 3: interrogationText += " pushes needles under the Automaton's fingernails"; break;
                        case 4: interrogationText += " beats the hostage with a metal bat"; break;
                        case 5: interrogationText += " beats the hostage with a belt"; break;
                    }
                    interrogationText += ", screaming \"<color=red>";
                    for(int i = 0; i < 2; i++)
                    {
                        switch (mc.LCSRandom(11))
                        {
                            case 0: interrogationText += "I hate you"; break;
                            case 1: interrogationText += "Does it hurt?"; break;
                            case 2: interrogationText += "Nobody loves you"; break;
                            case 3: interrogationText += "God hates you"; break;
                            case 4: interrogationText += "Don't " + mc.swearFilter("fuck", "mess") + " with me"; break;
                            case 5: interrogationText += "This is Liberalism"; break;
                            case 6: interrogationText += "Convert, " + mc.swearFilter("bitch", "jerk"); break;
                            case 7: interrogationText += "I'm going to kill you"; break;
                            case 8: interrogationText += "Do you love me?"; break;
                            case 9: interrogationText += "I am your God"; break;
                            case 10: interrogationText += "Is it safe?"; break;
                        }
                        if (i == 0) interrogationText += "! ";
                    }
                    interrogationText += "!</color>\" in its face.</color>";
                    if (getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level > 1)
                        getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level--;
                    if (getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level > 1)
                        getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level--;
                }
                else
                {
                    if (interrogators.Count == 1)
                        interrogationText += "\n" + interrogators[0].getComponent<CreatureInfo>().getName() + " beats";
                    else if (interrogators.Count == 2)
                        interrogationText += "\n" + interrogators[0].getComponent<CreatureInfo>().getName() + " and " + interrogators[1].getComponent<CreatureInfo>().getName() + " beat";
                    else
                        interrogationText += "\n" + getComponent<CreatureInfo>().getName() + "'s guards beat";
                    interrogationText += " the Automaton";

                    if (boughtProps)
                    {
                        switch (mc.LCSRandom(6))
                        {
                            case 0: interrogationText += " with a giant stuffed elephant"; break;
                            case 1: interrogationText += " while draped in a Confederate flag"; break;
                            case 2: interrogationText += " with a cardboard cutout of Reagan"; break;
                            case 3: interrogationText += " with a King James Bible"; break;
                            case 4: interrogationText += " with fists full of money"; break;
                            case 6: interrogationText += " with Conservative propaganda on the walls"; break;
                        }
                    }

                    interrogationText += ", ";

                    switch (mc.LCSRandom(4))
                    {
                        case 0: interrogationText += "screaming"; break;
                        case 1: interrogationText += "yelling"; break;
                        case 2: interrogationText += "shouting"; break;
                        case 3: interrogationText += "hollering"; break;
                    }

                    interrogationText += ", \"<color=yellow>";

                    for (int i = 0; i < 3; i++)
                    {
                        switch (mc.LCSRandom(20))
                        {
                            case 0: interrogationText += "McDonalds"; break;
                            case 1: interrogationText += "Microsoft"; break;
                            case 2: interrogationText += "Bill Gates"; break;
                            case 3: interrogationText += "Wal-Mart"; break;
                            case 4: interrogationText += "George W. Bush"; break;
                            case 5: interrogationText += "ExxonMobil"; break;
                            case 6: interrogationText += "Trickle-down economics"; break;
                            case 7: interrogationText += "Family values"; break;
                            case 8: interrogationText += "Conservatism"; break;
                            case 9: interrogationText += "War on Drugs"; break;
                            case 10: interrogationText += "War on Terror"; break;
                            case 11: interrogationText += "Ronald Reagan"; break;
                            case 12: interrogationText += "Rush Limbaugh"; break;
                            case 13: interrogationText += "Tax cuts"; break;
                            case 14: interrogationText += "Military spending"; break;
                            case 15: interrogationText += "Ann Coulter"; break;
                            case 16: interrogationText += "Deregulation"; break;
                            case 17: interrogationText += "Police"; break;
                            case 18: interrogationText += "Corporations"; break;
                            case 19: interrogationText += "Wiretapping"; break;

                        }
                        if (i < 2) interrogationText += "! ";
                    }
                    interrogationText += "!</color>\" in its face.";
                }

                getComponent<Body>().Blood -= (5 + mc.LCSRandom(5)) * (boughtProps ? 2 : 1);

                if (!getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].check((Difficulty)forceroll))
                {
                    if (getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].check((Difficulty)forceroll))
                    {
                        interrogationText += "\n" + getComponent<CreatureInfo>().getName();
                        if (!boughtDrugs)
                        {
                            switch (mc.LCSRandom(2))
                            {
                                case 0: interrogationText += " prays..."; break;
                                case 1: interrogationText += " cries out for God."; break;
                            }
                        }
                        else
                        {
                            switch (mc.LCSRandom(2))
                            {
                                case 0: interrogationText += " takes solace in the personal appearance of God."; break;
                                case 1: interrogationText += " appears to be having a religious experience."; break;
                            }
                        }
                    }
                    else if(forceroll > 
                        getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].getModifiedValue()*3 +
                        getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].getModifiedValue()*3 +
                        getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].getModifiedValue()*3)
                    {
                        interrogationText += "\n" + getComponent<CreatureInfo>().getName();
                        switch (mc.LCSRandom(4))
                        {
                            case 0:
                                interrogationText += " screams helplessly for ";
                                if (boughtDrugs) interrogationText += "John Lennon's mercy.";
                                else if (getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].level > 0) interrogationText += "God's mercy.";
                                else interrogationText += "mommy.";
                                break;
                            case 1:
                                if ((tactics & Tactics.RESTRAIN) != 0) interrogationText += " goes limp in the restraints.";
                                else interrogationText += " curls up in the corner and doesn't move.";
                                break;
                            case 2:
                                if (boughtDrugs && mc.LCSRandom(5) == 0) interrogationText += " barks helplessly.";
                                else interrogationText += " cries helplessly.";
                                break;
                            case 3:
                                if (boughtDrugs && mc.LCSRandom(3) == 0) interrogationText += " wonders about apples.";
                                else interrogationText += " wonders about death.";
                                break;
                        }
                        if (getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level > 1)
                            getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level--;

                        if(getComponent<CreatureBase>().Juice > 0 && mc.LCSRandom(2) == 0)
                            getComponent<CreatureBase>().juiceMe(-forceroll, 0);
                        else if(getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level > 1)
                        {
                            getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level -= forceroll / 10;
                            if (getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level < 1)
                                getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level = 1;
                        }

                        if(mc.LCSRandom(5) == 0 &&
                            getComponent<CreatureInfo>().workLocation.hasComponent<TroubleSpot>() && 
                            (!getComponent<CreatureInfo>().workLocation.getComponent<TroubleSpot>().mapped ||
                            getComponent<CreatureInfo>().workLocation.getComponent<SiteBase>().hidden))
                        {
                            interrogationText += "\n" + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " beats information out of the pathetic thing. A detailed map has been created of " + getComponent<CreatureInfo>().workLocation.getComponent<SiteBase>().getCurrentName() + ".";
                            getComponent<CreatureInfo>().workLocation.getComponent<TroubleSpot>().mapped = true;
                            getComponent<CreatureInfo>().workLocation.getComponent<SiteBase>().hidden = false;
                        }

                    }
                    else
                    {
                        interrogationText += "\n" + getComponent<CreatureInfo>().getName() + " seems to be getting the message.";

                        if (getComponent<CreatureBase>().Juice > 0 && mc.LCSRandom(2) == 0)
                            getComponent<CreatureBase>().juiceMe(-forceroll, 0);
                        else if (getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level > 1)
                        {
                            getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level -= (forceroll / 10) + 1;
                            if (getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level < 1)
                                getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level = 1;
                        }
                    }

                    if(getComponent<Body>().Blood <= 0)
                    {
                        interrogationText += "\n<color=red>" + getComponent<CreatureInfo>().getName() + "'s weakened body crumbles under the brutal assault.</color>";
                        getComponent<CreatureBase>().doDie(new Events.Die("was beaten to death"));
                        return interrogationText;
                    }
                    
                    if (!getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].check((Difficulty)(forceroll / 3)))
                    {
                        if(getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].Level > 1)
                        {
                            getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].Level--;
                            interrogationText += "\n<color=magenta>" + getComponent<CreatureInfo>().getName() + " is badly hurt.</color>";
                        }
                        else
                        {
                            getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].Level--;
                            interrogationText += "\n<color=red>" + getComponent<CreatureInfo>().getName() + "'s weakened body crumbles under the brutal assault.</color>";
                            getComponent<CreatureBase>().doDie(new Events.Die("was beaten to death"));
                            return interrogationText;
                        }
                    }
                }
                else
                {
                    interrogationText += "\n" + getComponent<CreatureInfo>().getName() + " takes it well.";
                }

                if (tortured)
                {
                    if (mc.LCSRandom(tempLeadInterrogator.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level) > mc.LCSRandom(3))
                    {
                        tempLeadInterrogator.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level--;
                        interrogationText += "\n<color=lime>" + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " feels sick to " + tempLeadInterrogator.getComponent<CreatureInfo>().hisHer().ToLower() + " stomach afterward and ";
                        switch (mc.LCSRandom(4))
                        {
                            case 0: interrogationText += "throws up in a trash can."; break;
                            case 1: interrogationText += "gets drunk, eventually falling asleep."; break;
                            case 2: interrogationText += "curls up in a ball, crying softly."; break;
                            case 3: interrogationText += "shoots up and collapses in a heap on the floor."; break;
                        }
                        interrogationText += "</color>";
                    }
                    else if (mc.LCSRandom(3) == 0)
                    {
                        interrogationText += "\n<color=cyan>" + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " grows colder.";
                        tempLeadInterrogator.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level++;
                    }
                }
            }

            //Conversion
            if((tactics & Tactics.CONVERT) != 0 && !triedToExecute)
            {
                float rapportTemp = rapport[tempLeadInterrogator];

                if ((tactics & Tactics.RESTRAIN) == 0) attack += 5;
                attack += (int)(rapport[tempLeadInterrogator] * 3);

                interrogationText += "\n" + tempLeadInterrogator.getComponent<CreatureInfo>().getName();

                if (boughtProps)
                {
                    attack += 10;
                    switch (mc.LCSRandom(9))
                    {
                        case 0: interrogationText += " plays violent video games with "; break;
                        case 1: interrogationText += " reads Origin of the Species to "; break;
                        case 2: interrogationText += " burns flags in front of "; break;
                        case 3: interrogationText += " explores an elaborate political fantasy with "; break;
                        case 4: interrogationText += " watches controversial avant-garde films with "; break;
                        case 5: interrogationText += " plays the anime film Bible Black for "; break;
                        case 6: interrogationText += " watches a documentary about Emmett Till with "; break;
                        case 7: interrogationText += " watches Michael Moore films with "; break;
                        case 8: interrogationText += " listens to Liberal radio shows with "; break;
                    }
                }
                else
                {
                    switch (mc.LCSRandom(4))
                    {
                        case 0: interrogationText += " talks about " + GameData.getData().viewList[MasterController.generalPublic.randomissue(true)].name + " with "; break;
                        case 1: interrogationText += " argues about " + GameData.getData().viewList[MasterController.generalPublic.randomissue(true)].name + " with "; break;
                        case 2: interrogationText += " tries to expose the true Liberal side of "; break;
                        case 3: interrogationText += " attempts to recruit "; break;
                    }
                }

                interrogationText += getComponent<CreatureInfo>().getName() + ".";

                if (boughtDrugs)
                {
                    interrogationText += "\n" + getComponent<CreatureInfo>().getName();
                    if (getComponent<CreatureBase>().Skills[Constants.SKILL_PSYCHOLOGY].check(Difficulty.CHALLENGING))
                    {
                        switch (mc.LCSRandom(4))
                        {
                            case 0: interrogationText += " takes the drug-induced hallucinations with stoicism."; break;
                            case 1: interrogationText += " mutters its initials over and over again."; break;
                            case 2: interrogationText += " babbles continuous numerical sequences."; break;
                            case 3: interrogationText += " manages to remain grounded through the hallucinations."; break;
                        }
                    }
                    else if((rapport[tempLeadInterrogator] > 1 && mc.LCSRandom(3) == 0) || mc.LCSRandom(10) == 0)
                    {
                        rapportTemp = 10;
                        switch (mc.LCSRandom(4))
                        {
                            case 0: interrogationText += " hallucinates and sees " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " as an angel."; break;
                            case 1: interrogationText += " realizes with joy that " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " is Ronald Reagan!"; break;
                            case 2: interrogationText += " stammers and " + ((tactics & Tactics.RESTRAIN) == 0 ? "hugs ":"talks about hugging ") + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + "."; break;
                            case 3: interrogationText += " begs " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " to let the colors stay forever."; break;
                        }
                    }
                    else if((rapport[tempLeadInterrogator] < -1 && mc.LCSRandom(3) != 0) || mc.LCSRandom(5) == 0)
                    {
                        attack = 0;
                        switch (mc.LCSRandom(4))
                        {
                            case 0: interrogationText += " screams in horror as " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " turns into an alien."; break;
                            case 1: interrogationText += ((tactics & Tactics.RESTRAIN) == 0 ? " curls up and" : "") + " begs for the nightmare to end."; break;
                            case 2: interrogationText += " watches " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " shift from one demonic form to another."; break;
                            case 3:
                                if(rapport[tempLeadInterrogator] < -3)
                                    interrogationText += " begs Hitler to stay and kill " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + ".";
                                else
                                    interrogationText += " screams for " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " to stop looking like Hitler.";
                                break;
                        }
                    }
                    else
                    {
                        switch (mc.LCSRandom(4))
                        {
                            case 0: interrogationText += " comments on the swirling light " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " is radiating."; break;
                            case 1: interrogationText += " can't stop looking at the moving colors."; break;
                            case 2: interrogationText += " laughs hysterically at " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + "'s altered appearance."; break;
                            case 3: interrogationText += " barks and woofs like a dog."; break;
                        }
                    }
                }

                if(getComponent<CreatureBase>().Skills[Constants.SKILL_PSYCHOLOGY].level > tempLeadInterrogator.getComponent<CreatureBase>().Skills[Constants.SKILL_PSYCHOLOGY].level)
                {
                    interrogationText += "\n<color=magenta>" + getComponent<CreatureInfo>().getName();
                    switch (mc.LCSRandom(4))
                    {
                        case 0: interrogationText += " plays mind games with " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + "."; break;
                        case 1: interrogationText += " knows how this works, and won't budge."; break;
                        case 2: interrogationText += " asks if Liberal mothers would approve of this."; break;
                        case 3: interrogationText += " seems resistant to this form of interrogation."; break;
                    }
                    interrogationText += "</color>";
                }
                else if((tactics & Tactics.ASSAULT) != 0 || rapportTemp < -2)
                {
                    interrogationText += "\n<color=magenta>" + getComponent<CreatureInfo>().getName();
                    switch (mc.LCSRandom(7))
                    {
                        case 0: interrogationText += " babbles mindlessly."; break;
                        case 1: interrogationText += " just whimpers."; break;
                        case 2: interrogationText += " cries helplessly."; break;
                        case 3: interrogationText += " is losing faith in the world."; break;
                        case 4: interrogationText += " only grows more distant."; break;
                        case 5: interrogationText += " is too terrified to even speak to " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + "."; break;
                        case 6: interrogationText += " just hates the LCS even more."; break;
                    }
                    interrogationText += "</color>";
                    if (tempLeadInterrogator.getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].check(Difficulty.CHALLENGING))
                    {
                        interrogationText += "\n<color=lime>" + tempLeadInterrogator.getComponent<CreatureInfo>().getName();
                        switch (mc.LCSRandom(7))
                        {
                            case 0: interrogationText += " consoles the Conservative automaton."; break;
                            case 1: interrogationText += " shares some chocolates."; break;
                            case 2: interrogationText += " provides a shoulder to cry on."; break;
                            case 3: interrogationText += " understands " + getComponent<CreatureInfo>().getName() + "'s pain."; break;
                            case 4: interrogationText += "'s heart opens to the poor Conservative."; break;
                            case 5: interrogationText += " helps the poor thing to come to terms with captivity."; break;
                            case 6: interrogationText += "'s patience and kindness leaves the Conservative confused."; break;
                        }
                        interrogationText += "</color>";
                        rapport[tempLeadInterrogator] += 0.7f;

                        if(rapport[tempLeadInterrogator] > 3)
                        {
                            interrogationText += "\n<color=lime>" + getComponent<CreatureInfo>().getName();
                            switch (mc.LCSRandom(7))
                            {
                                case 0: interrogationText += " emotionally clings to " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + "'s sympathy."; break;
                                case 1: interrogationText += " begs " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " for help."; break;
                                case 2: interrogationText += " promises to be good."; break;
                                case 3: interrogationText += " reveals childhood pains."; break;
                                case 4: interrogationText += " thanks " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " for being merciful."; break;
                                case 5: interrogationText += " cries in " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + "'s arms."; break;
                                case 6: interrogationText += " really likes " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + "."; break;
                            }
                            interrogationText += "</color>";

                            if (rapport[tempLeadInterrogator] > 5) turned = true;
                        }
                    }

                    if (getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level > 1)
                        getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level--;
                }
                else if(!boughtDrugs && (getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].level > 
                    tempLeadInterrogator.getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].level +
                    tempLeadInterrogator.getComponent<CreatureBase>().Skills[Constants.SKILL_PSYCHOLOGY].level))
                {
                    interrogationText += "\n<color=magenta>";
                    switch (mc.LCSRandom(4))
                    {
                        case 0: interrogationText += tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " is unable to shake " + getComponent<CreatureInfo>().getName() + "'s religious convictions."; break;
                        case 1: interrogationText += getComponent<CreatureInfo>().getName() + " will never be broken so long as God grants it strength."; break;
                        case 2: interrogationText += tempLeadInterrogator.getComponent<CreatureInfo>().getName() + "'s efforts to question " + getComponent<CreatureInfo>().getName() + "'s faith seem futile."; break;
                        case 3: interrogationText += getComponent<CreatureInfo>().getName() + " calmly explains the Conservative tenets of its faith."; break;
                    }
                    interrogationText += "</color>";
                    tempLeadInterrogator.getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].addExperience(getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].level*4);
                }
                else if (!boughtDrugs && (getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].level >
                    tempLeadInterrogator.getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].level +
                    tempLeadInterrogator.getComponent<CreatureBase>().Skills[Constants.SKILL_PSYCHOLOGY].level))
                {
                    interrogationText += "\n<color=magenta>";
                    switch (mc.LCSRandom(4))
                    {
                        case 0: interrogationText += tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " will never be moved by " + getComponent<CreatureInfo>().getName() + "'s pathetic economic ideals."; break;
                        case 1: interrogationText += getComponent<CreatureInfo>().getName() + " wishes a big company would just buy the LCS and shut it down."; break;
                        case 2: interrogationText += tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " explains to " + getComponent<CreatureInfo>().getName() + " why communism failed."; break;
                        case 3: interrogationText += getComponent<CreatureInfo>().getName() + " mumbles incoherently about Reaganomics."; break;
                    }
                    interrogationText += "</color>";
                    tempLeadInterrogator.getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].addExperience(getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].level*4);
                }
                else if (!boughtDrugs && (getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].level >
                    tempLeadInterrogator.getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].level +
                    tempLeadInterrogator.getComponent<CreatureBase>().Skills[Constants.SKILL_PSYCHOLOGY].level))
                {
                    interrogationText += "\n<color=magenta>";
                    switch (mc.LCSRandom(4))
                    {
                        case 0: interrogationText += tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " wonders what mental disease has possessed " + getComponent<CreatureInfo>().getName() + "."; break;
                        case 1: interrogationText += getComponent<CreatureInfo>().getName() + " explains why nuclear energy is safe."; break;
                        case 2: interrogationText += tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " makes Albert Einstein faces at " + getComponent<CreatureInfo>().getName() + "."; break;
                        case 3: interrogationText += tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " pities " + getComponent<CreatureInfo>().getName() + " blind ignorance of science."; break;
                    }
                    interrogationText += "</color>";
                    tempLeadInterrogator.getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].addExperience(getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].level * 4);
                }
                else if (!getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].check((Difficulty)(attack / 6)))
                {
                    if (getComponent<CreatureBase>().Juice > 0)
                        getComponent<CreatureBase>().juiceMe(-attack, 0);

                    if (getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level < 10)
                        getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level++;
                    rapport[tempLeadInterrogator] += 1.5f;

                    if (getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].getModifiedValue() > getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].getModifiedValue() + 4)
                        turned = true;
                    if (rapport[tempLeadInterrogator] > 4)
                        turned = true;

                    interrogationText += "\n" + getComponent<CreatureInfo>().getName();
                    switch (mc.LCSRandom(5))
                    {
                        case 0: interrogationText += "'s Conservative beliefs are shaken."; break;
                        case 1: interrogationText += " quietly considers these ideas."; break;
                        case 2: interrogationText += " is beginning to see Liberal reason."; break;
                        case 3: interrogationText += " has a revelation of understanding."; break;
                        case 4: interrogationText += " grudgingly admits sympathy for LCS ideals."; break;
                    }

                    if(mc.LCSRandom(5) == 0 &&
                            getComponent<CreatureInfo>().workLocation.hasComponent<TroubleSpot>() &&
                            (!getComponent<CreatureInfo>().workLocation.getComponent<TroubleSpot>().mapped ||
                            getComponent<CreatureInfo>().workLocation.getComponent<SiteBase>().hidden))
                    {
                        interrogationText += "\n" + getComponent<CreatureInfo>().getName() + " reveals details about the " + getComponent<CreatureInfo>().workLocation.getComponent<SiteBase>().getCurrentName() + ", allowing " + leadInterrogator.getComponent<CreatureInfo>().getName() + " to create a map of the site.";
                        getComponent<CreatureInfo>().workLocation.getComponent<TroubleSpot>().mapped = true;
                        getComponent<CreatureInfo>().workLocation.getComponent<SiteBase>().hidden = false;
                    }
                }
                else if (!getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].check((Difficulty)tempLeadInterrogator.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].getModifiedValue()) ||
                    boughtProps)
                {
                    rapport[tempLeadInterrogator] += 0.2f;

                    interrogationText += "\n" + getComponent<CreatureInfo>().getName() + " holds firm.";
                }
                else
                {
                    rapport[tempLeadInterrogator] += 0.5f;
                    tempLeadInterrogator.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level++;

                    interrogationText += "\n<color=magenta>" + getComponent<CreatureInfo>().getName() + " turns the tables on " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + "! " + tempLeadInterrogator.getComponent<CreatureInfo>().heShe() + " has been tainted with Wisdom!</color>";
                }
            }

            if (!triedToExecute)
            {
                tempLeadInterrogator.getComponent<CreatureBase>().Skills[Constants.SKILL_PSYCHOLOGY].addExperience(attack / 2 + 1);
                foreach (Entity e in interrogators)
                    e.getComponent<CreatureBase>().Skills[Constants.SKILL_PSYCHOLOGY].addExperience(attack / 4 + 1);
            }

            if(!turned && getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level <= 1 && mc.LCSRandom(3) != 0 && timeInCaptivity > 6)
            {
                if(mc.LCSRandom(6) != 0 || (tactics & Tactics.RESTRAIN) != 0)
                {
                    interrogationText += "\n<color=maroon>" + getComponent<CreatureInfo>().getName();
                    switch(mc.LCSRandom(5 - ((tactics & Tactics.RESTRAIN) != 0 ? 1 : 0)))
                    {
                        case 0: interrogationText += " mutters about death."; break;
                        case 1: interrogationText += " broods darkly."; break;
                        case 2: interrogationText += " has lost hope of rescue"; break;
                        case 3: interrogationText += " is making peace with God."; break;
                        case 4:
                            interrogationText += " is bleeding from self-inflicted wounds.";
                            getComponent<Body>().Blood -= mc.LCSRandom(15) + 10;
                            break;
                    }
                    interrogationText += "</color>";
                }
                else
                {
                    interrogationText += "\n<color=red>" + getComponent<CreatureInfo>().getName() + " has committed suicide.</color>";
                    getComponent<CreatureBase>().doDie(new Events.Die("committed suicide"));
                }
            }

            if(getComponent<Body>().Alive && getComponent<Body>().Blood <= 0)
                getComponent<CreatureBase>().doDie(new Events.Die("bled to death in captivity"));

            if (!getComponent<Body>().Alive)
            {
                interrogationText += "\n<color=red>" + getComponent<CreatureInfo>().getName() + " is dead under " + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + "'s interrogation.</color>";

                if (mc.LCSRandom(tempLeadInterrogator.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level) > mc.LCSRandom(3))
                {
                    tempLeadInterrogator.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level--;
                    interrogationText += "\n<color=lime>" + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " feels sick to " + tempLeadInterrogator.getComponent<CreatureInfo>().hisHer().ToLower() + " stomach afterward and ";
                    switch (mc.LCSRandom(4))
                    {
                        case 0: interrogationText += "throws up in a trash can."; break;
                        case 1: interrogationText += "gets drunk, eventually falling asleep."; break;
                        case 2: interrogationText += "curls up in a ball, crying softly."; break;
                        case 3: interrogationText += "shoots up and collapses in a heap on the floor."; break;
                    }
                    interrogationText += "</color>";
                }
                else if (mc.LCSRandom(3) == 0)
                {
                    interrogationText += "\n<color=cyan>" + tempLeadInterrogator.getComponent<CreatureInfo>().getName() + " grows colder.";
                    tempLeadInterrogator.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level++;
                }

                return interrogationText;
            }

            if (turned)
            {
                foreach (Entity e in getComponent<CreatureBase>().Location.getComponent<SafeHouse>().getBasedLiberals())
                    if (e.getComponent<Liberal>().dailyActivity.interrogationTarget == owner)
                        e.getComponent<Liberal>().setActivity("NONE");

                interrogationText += "\n<color=lime>The Automaton has been Enlightened! Your Liberal ranks are swelling!</color>";
                if(getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].getModifiedValue() > 7 &&
                    getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].getModifiedValue() > 2 &&
                    mc.LCSRandom(4) == 0 &&
                    (getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.KIDNAPPED) != 0)
                {
                    interrogationText += "\n<color=lime>The conversion is convincing enough that the police no longer consider it a kidnapping.</color>";
                    getComponent<CreatureInfo>().flags &= ~CreatureInfo.CreatureFlag.KIDNAPPED;
                    getComponent<CreatureInfo>().flags &= ~CreatureInfo.CreatureFlag.MISSING;
                }

                tempLeadInterrogator.getComponent<Liberal>().recruit(owner, Liberal.RecruitType.ENLIGHTENED);
                if (getComponent<CreatureInfo>().workLocation.hasComponent<TroubleSpot>())
                {
                    getComponent<CreatureInfo>().workLocation.getComponent<TroubleSpot>().mapped = true;
                    getComponent<CreatureInfo>().workLocation.getComponent<SiteBase>().hidden = false;
                }

                if((getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.MISSING) != 0 &&
                    (getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.KIDNAPPED) == 0)
                {
                    interrogationText += "\n<color=lime>" + getComponent<CreatureInfo>().getName() + "'s disappearance has not yet been reported.</color>";
                    getComponent<CreatureInfo>().flags &= ~CreatureInfo.CreatureFlag.MISSING;
                }

                if((getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.KIDNAPPED) == 0)
                {
                    getComponent<Liberal>().sleeperize();
                }

                removeMe();
            }

            return interrogationText;
        }
    }
}
