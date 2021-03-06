using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using LCS.Engine.Components.Location;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Item;
using LCS.Engine.Data;

namespace LCS.Engine.Components.Creature
{
    public class Dating : Component
    {
        [SimpleSave]
        public Entity partner;
        [SimpleSave]
        public int timeleft;

        private ActionQueue actionRoot { get; set; }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Dating");
                entityNode.AppendChild(saveNode);
            }

            saveSimpleFields();
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);
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
        }

        private void doDaily(object sender, EventArgs arg)
        {
            MasterController mc = MasterController.GetMC();
            if (timeleft > 0)
            {
                timeleft--;
                if(timeleft == 0)
                {
                    actionRoot = mc.createSubQueue(() =>
                    {
                        finishVacation();
                    }, "Finish Vacation",
                    () =>
                    {
                        actionRoot = null;
                        mc.doNextAction();
                    }, "close meeting screen->Next Action");
                }
            }
        }

        public void doStartDate()
        {
            MasterController mc = MasterController.GetMC();

            //If this date has been cleared from the Liberal's schedule (due to selecting the vacation option on a previous date), skip it
            if (!partner.getComponent<Liberal>().plannedDates.Contains(owner))
            {
                mc.doNextAction();
                return;
            }
            //Also skip if they are currently away on a vacation
            if(timeleft > 0)
            {
                mc.doNextAction();
                return;
            }
            
            //If the liberal finds themselves in jail for whatever reason, then the relationship ends
            if(partner.getComponent<Liberal>().status == Liberal.Status.JAIL_COURT ||
                partner.getComponent<Liberal>().status == Liberal.Status.JAIL_POLICE_CUSTODY ||
                partner.getComponent<Liberal>().status == Liberal.Status.JAIL_PRISON)
            {
                breakUp();
                mc.doNextAction();
                return;
            }

            actionRoot = mc.createSubQueue(() =>
            {
                mc.uiController.closeUI();
                startDate();
            }, "Start Date",
            () =>
            {
                mc.uiController.closeUI();
                actionRoot = null;
                mc.doNextAction();
            }, "close meeting screen->Next Action");
        }

        private void startDate()
        {
            MasterController mc = MasterController.GetMC();

            mc.uiController.meeting.showDate(owner);
            string text = partner.getComponent<CreatureInfo>().getName();
            if(partner.getComponent<Liberal>().status == Liberal.Status.HOSPITAL)
            {
                text += " has a \"hot\" date with " + getComponent<CreatureInfo>().getName() + " at " + partner.getComponent<CreatureBase>().Location.getComponent<SiteBase>().getCurrentName() + ".";
            }
            else
            {
                text += " has a hot date with " + getComponent<CreatureInfo>().getName() + ".";
            }
            text += "How should " + partner.getComponent<CreatureInfo>().getName() + " approach the situation?";

            mc.uiController.meeting.printTitle("Seeing " + getComponent<CreatureInfo>().getName() + "\n" + getComponent<CreatureInfo>().type_name + ", " + getComponent<CreatureInfo>().workLocation.getComponent<SiteBase>().getCurrentName());
            mc.uiController.meeting.printText(text);
        }

        public void regularDate(bool spendMoney)
        {
            MasterController mc = MasterController.GetMC();

            int thingsInCommon = 0;
            foreach(string skill in getComponent<CreatureBase>().Skills.Keys)
            {
                if (getComponent<CreatureBase>().Skills[skill].level > 0 &&
                    partner.getComponent<CreatureBase>().Skills[skill].level > 0 &&
                    partner.getComponent<CreatureBase>().Skills[skill].level * 2 >= getComponent<CreatureBase>().Skills[skill].level)
                    thingsInCommon++;
            }

            int aroll = partner.getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].roll();
            aroll += thingsInCommon * 3;
            if (spendMoney)
            {
                MasterController.lcs.changeFunds(-100);
                aroll += mc.LCSRandom(10) + 1;
            }
            int troll = getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].roll();
            switch (getComponent<CreatureInfo>().alignment)
            {
                case Alignment.CONSERVATIVE:
                    troll += troll*(getComponent<CreatureBase>().Juice / 100);
                    break;
                case Alignment.MODERATE:
                    troll += troll * (getComponent<CreatureBase>().Juice / 150);
                    break;
                case Alignment.LIBERAL:
                    troll += troll * (getComponent<CreatureBase>().Juice / 200);
                    break;
            }
            if(getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].level > 0)
            {
                aroll += partner.getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].roll();
                troll += getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].roll();
            }
            if(getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].level > 0)
            {
                aroll += partner.getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].roll();
                troll += getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].roll();
            }
            if(getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].level > 0)
            {
                aroll += partner.getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].roll();
                troll += getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].roll();
            }

            partner.getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].addExperience(mc.LCSRandom(4) + 5);
            foreach (CreatureBase.Skill skill in partner.getComponent<CreatureBase>().Skills.Values)
            {
                if ((skill.getFlags() & SkillDef.SkillFlag.LEARN_FROM_RECRUITMENT) == 0) continue;
                partner.getComponent<CreatureBase>().Skills[skill.type].addExperience(Math.Max(0, getComponent<CreatureBase>().Skills[skill.type].level - partner.getComponent<CreatureBase>().Skills[skill.type].level));
            }

            troll += partner.getComponent<Liberal>().getLoverCount();

            dateResult(aroll, troll);
        }

        public void startVacation()
        {
            foreach(Entity e in partner.getComponent<Liberal>().plannedDates)
            {
                if (e != owner) e.depersist();
            }
            partner.getComponent<Liberal>().plannedDates.Clear();
            foreach(Entity e in partner.getComponent<Liberal>().plannedMeetings)
            {
                e.depersist();
            }            
            partner.getComponent<Liberal>().plannedMeetings.Clear();
            partner.getComponent<Liberal>().plannedDates.Add(owner);
            partner.getComponent<Liberal>().status = Liberal.Status.AWAY;
            partner.getComponent<Liberal>().awayTime = 7;
            partner.getComponent<Liberal>().changeSquad(null);
            timeleft = 7;
            MasterController.GetMC().doNextAction();
        }

        private enum DateResult
        {
            MEET_TOMORROW,
            BREAKUP,
            JOINED,
            ARRESTED
        }
        
        private DateResult dateResult(int aroll, int troll)
        {
            MasterController mc = MasterController.GetMC();

            string text = "";

            if(aroll > troll)
            {
                text += "<color=cyan>" + getComponent<CreatureInfo>().getName() + " is quite taken with " + partner.getComponent<CreatureInfo>().getName() + "'s unique life philosophy...</color>";
                if(mc.LCSRandom((aroll- troll)/2) > getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].getModifiedValue())
                {
                    text += "\n<color=lime>In fact, " + getComponent<CreatureInfo>().getName() + " is " + partner.getComponent<CreatureInfo>().getName() + "'s totally unconditional love-slave!</color>";
                    if (getComponent<CreatureInfo>().workLocation.hasComponent<TroubleSpot>())
                        getComponent<CreatureInfo>().workLocation.getComponent<TroubleSpot>().mapped = true;
                    partner.getComponent<Liberal>().recruit(owner, Liberal.RecruitType.LOVE_SLAVE);
                    partner.getComponent<Liberal>().plannedDates.Remove(owner);
                    List<UI.PopupOption> options = new List<UI.PopupOption>();
                    options.Add(new UI.PopupOption("Sleeper", () =>
                    {
                        getComponent<Liberal>().sleeperize();
                        removeMe();
                        MasterController.GetMC().doNextAction();
                    }));
                    options.Add(new UI.PopupOption("Regular", () =>
                    {
                        removeMe();
                        MasterController.GetMC().doNextAction();
                    }));
                    string sleeperPrompt = "Should " + getComponent<CreatureInfo>().getName() + " stay at " + getComponent<CreatureInfo>().workLocation.getComponent<SiteBase>().getCurrentName() + " as a sleeper agent or join the LCS as a regular member?";

                    actionRoot.Add(() => { MasterController.GetMC().uiController.showOptionPopup(sleeperPrompt, options); }, "Sleeper Prompt");
                    mc.uiController.showPopup(text, mc.doNextAction);
                    return DateResult.JOINED;
                }
                else
                {
                    if(getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level > 3)
                    {
                        text += "\n<color=lime>" + partner.getComponent<CreatureInfo>().getName() + " is slowly warming " + getComponent<CreatureInfo>().getName() + "'s frozen Conservative heart.</color>";                        
                        getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level--;
                        if(getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
                            getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level++;
                    }
                    else if(mc.LCSRandom(getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].getModifiedValue()) == 0)
                    {
                        if (getComponent<CreatureInfo>().workLocation.hasComponent<TroubleSpot>())
                        {
                            text += "\n" + getComponent<CreatureInfo>().getName() + " turns the topic of discussion to the " + getComponent<CreatureInfo>().workLocation.getComponent<SiteBase>().getCurrentName() ;
                            if (!getComponent<CreatureInfo>().workLocation.getComponent<TroubleSpot>().mapped)
                            {
                                text += ". " + partner.getComponent<CreatureInfo>().getName() + " was able to create a map of the site with this information.";
                                getComponent<CreatureInfo>().workLocation.getComponent<TroubleSpot>().mapped = true;
                            }
                            else
                            {
                                text += ", but " + partner.getComponent<CreatureInfo>().getName() + " knows all about that already.";
                            }
                        }
                    }

                    text += "\nThey'll meet again tomorrow.";
                    mc.uiController.showPopup(text, mc.doNextAction);
                    return DateResult.MEET_TOMORROW;
                }
            }
            else if(aroll == troll)
            {
                text += getComponent<CreatureInfo>().getName() + " seemed to have fun, but left early";
                switch (mc.LCSRandom(7))
                {
                    case 0:
                        text += " to wash " + getComponent<CreatureInfo>().hisHer().ToLower() + " hair.";
                        break;
                    case 1:
                        text += " due to an allergy attack.";
                        break;
                    case 2:
                        text += " due to an early meeting tomorrow.";
                        break;
                    case 3:
                        text += " to catch " + getComponent<CreatureInfo>().hisHer().ToLower() + " favorite TV show.";
                        break;
                    case 4:
                        text += " to gake care of " + getComponent<CreatureInfo>().hisHer().ToLower() + " pet ";
                        switch(mc.LCSRandom(3 + (MasterController.government.laws[Constants.LAW_ANIMAL_RESEARCH].alignment == Alignment.ARCHCONSERVATIVE ? 1 : 0)))
                        {
                            case 0:
                                text += " cat.";
                                break;
                            case 1:
                                text += " dog.";
                                break;
                            case 2:
                                text += " fish.";
                                break;
                            case 3:
                                text += " six-legged pig.";
                                break;
                        }
                        break;
                    case 5:
                        text += " to go to a birthday party.";
                        break;
                    case 6:
                        text += " to recharge " + getComponent<CreatureInfo>().hisHer().ToLower() + " cell phone.";
                        break;
                }
                text += "\nThey'll meet again tomorrow.";
                mc.uiController.showPopup(text, mc.doNextAction);
                return DateResult.MEET_TOMORROW;
            }
            else
            {
                if(getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE && aroll < troll / 2)
                {
                    text += "<color=red>Talking with " + getComponent<CreatureInfo>().getName() + " actually pollutes " + partner.getComponent<CreatureInfo>().getName() + " mind with wisdom!!!</color>";
                    partner.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level++;
                    foreach (CreatureBase.Skill skill in partner.getComponent<CreatureBase>().Skills.Values)
                    {
                        if ((skill.getFlags() & SkillDef.SkillFlag.LEARN_FROM_RECRUITMENT) == 0) continue;
                        partner.getComponent<CreatureBase>().Skills[skill.type].addExperience(Math.Max(0, (getComponent<CreatureBase>().Skills[skill.type].level - partner.getComponent<CreatureBase>().Skills[skill.type].level)*20));
                    }
                }

                if(partner.getComponent<CriminalRecord>().isCriminal() &&
                    (mc.LCSRandom(50) == 0 || ((getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.SNITCH) != 0) && mc.LCSRandom(4) == 0))
                {
                    text += "\n<color=red>" + getComponent<CreatureInfo>().getName() + " was leaking information to the police the whole time!";

                    //3/4 chance of being arrested with <50 juice, 1/2 with >=50 juice
                    if((partner.getComponent<CreatureBase>().Juice < 50 && mc.LCSRandom(2) == 0) || mc.LCSRandom(2) == 0)
                    {
                        breakUp();
                        partner.getComponent<CriminalRecord>().arrest();
                        text += "\n<color=magenta>" + partner.getComponent<CreatureInfo>().getName() + " has been arrested.</color>";
                        text = text.TrimStart('\n');
                        mc.uiController.showPopup(text, mc.doNextAction);
                        return DateResult.ARRESTED;
                    }
                    else
                    {
                        text += "\n<color=lime>But " + partner.getComponent<CreatureInfo>().getName() + " manages to escape the police ambush!</color>";
                    }
                }
                else
                {
                    if(partner.getComponent<Liberal>().getLoverCount() > 0 && mc.LCSRandom(2) == 0)
                    {
                        text += "\n<color=magenta>The date starts well, but goes horribly wrong when " + getComponent<CreatureInfo>().getName() + " notices " + partner.getComponent<CreatureInfo>().getName() + "'s ";
                        switch (partner.getComponent<Liberal>().getLoverCount())
                        {
                            case 5: text += "awe-inspiring "; break;
                            case 4: text += "intricate "; break;
                            case 3: text += "complicated "; break;
                            case 2: text += "detailed "; break;
                            case 1: break;
                            default: text += "mind-bending "; break;
                        }
                        text += "schedule for keeping " + getComponent<CreatureInfo>().getName() + " from meeting ";
                        int lsfound = 0;
                        for(int i = 0; i < partner.getComponent<Liberal>().subordinates.Count; i++)
                        {
                            if (partner.getComponent<Liberal>().subordinates[i].getComponent<Liberal>().recruitType != Liberal.RecruitType.LOVE_SLAVE)
                                continue;
                            lsfound++;
                            if (lsfound == 1)
                            {
                                text += partner.getComponent<Liberal>().subordinates[i].getComponent<CreatureInfo>().getName();
                            }
                            else if (lsfound < partner.getComponent<Liberal>().getLoverCount())
                            {
                                text += ", " + partner.getComponent<Liberal>().subordinates[i].getComponent<CreatureInfo>().getName();
                            }
                            else
                            {
                                text += " and " + partner.getComponent<Liberal>().subordinates[i].getComponent<CreatureInfo>().getName();
                            }
                        }
                        text += ".</color>";
                    }
                    else
                    {
                        text += "\n<color=magenta>" + getComponent<CreatureInfo>().getName() + " can sense things just aren't working out.</color>";
                    }

                    text += "\n<color=magenta>This relationship is over.</color>";
                    breakUp();
                    text = text.TrimStart('\n');
                    mc.uiController.showPopup(text, mc.doNextAction);
                }
                return DateResult.BREAKUP;
            }
        }

        public void breakUp()
        {
            partner.getComponent<Liberal>().plannedDates.Remove(owner);
            owner.depersist();
        }

        private void finishVacation()
        {
            MasterController mc = MasterController.GetMC();

            //Temporarily make the entity Conservative so Liberals aren't trivial to seduce.
            Alignment originalAlign = getComponent<CreatureInfo>().alignment;
            getComponent<CreatureInfo>().alignment = Alignment.CONSERVATIVE;

            int aroll = partner.getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].roll() * 2;
            int troll = getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].roll();

            getComponent<CreatureInfo>().alignment = originalAlign;

            int thingsInCommon = 0;
            foreach (string skill in getComponent<CreatureBase>().Skills.Keys)
            {
                if (getComponent<CreatureBase>().Skills[skill].level > 0 &&
                    partner.getComponent<CreatureBase>().Skills[skill].level > 0 &&
                    partner.getComponent<CreatureBase>().Skills[skill].level * 2 >= getComponent<CreatureBase>().Skills[skill].level)
                    thingsInCommon++;
            }

            aroll += thingsInCommon * 3;

            if (getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].level > 0)
            {
                aroll += partner.getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].roll();
                troll += getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].roll();
            }
            if (getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].level > 0)
            {
                aroll += partner.getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].roll();
                troll += getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].roll();
            }
            if (getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].level > 0)
            {
                aroll += partner.getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].roll();
                troll += getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].roll();
            }

            partner.getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].addExperience(mc.LCSRandom(11) + 15);
            foreach (CreatureBase.Skill skill in partner.getComponent<CreatureBase>().Skills.Values)
            {
                if ((skill.getFlags() & SkillDef.SkillFlag.LEARN_FROM_RECRUITMENT) == 0) continue;
                partner.getComponent<CreatureBase>().Skills[skill.type].addExperience(Math.Max(0, getComponent<CreatureBase>().Skills[skill.type].level - partner.getComponent<CreatureBase>().Skills[skill.type].level));
            }

            mc.uiController.showPopup(partner.getComponent<CreatureInfo>().getName() + " returns from vacation.", () => { dateResult(aroll, troll); });
        }

        public void kidnap()
        {
            MasterController mc = MasterController.GetMC();

            string text = "\n\n";
            int bonus = 0;

            if(partner.getComponent<Inventory>().getWeapon().def != "WEAPON_NONE")
            {
                if((partner.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getAttack().flags & AttackDef.AttackFlags.RANGED) != 0)
                {
                    text += "<color=yellow>" + partner.getComponent<CreatureInfo>().getName() + " comes back from the bathroom toting the " + partner.getComponent<Inventory>().getWeapon().getComponent<ItemBase>().getName() + " and threatens to blow the Conservative's brains out!</color>";
                    bonus = 5;
                }
                else
                {
                    text += "<color=yellow>" + partner.getComponent<CreatureInfo>().getName() + " grabs the Conservative from behind, holding the " + partner.getComponent<Inventory>().getWeapon().getComponent<ItemBase>().getName() + " to the corporate slave's throat!</color>";
                    if ((partner.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.TAKE_HOSTAGE) != 0)
                        bonus = 5;
                    else //Using something stupid like a gavel to take a hostage just emboldens them
                        bonus = -1;
                }
            }
            else
            {
                text += "<color=yellow>" + partner.getComponent<CreatureInfo>().getName() + " seizes the Conservative swine from behind and warns it not to " + mc.swearFilter("fuck around", "resist") + ".</color>";
                bonus = partner.getComponent<CreatureBase>().Skills[Constants.SKILL_MARTIAL_ARTS].level - 1;
            }

            mc.uiController.meeting.printText(text);
            string resultText = "\n";

            if (((getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.KIDNAP_RESIST) == 0 && mc.LCSRandom(15) != 0) || mc.LCSRandom(2 + bonus) != 0)
            {
                if(bonus > 0)
                {
                    resultText += "<color=lime>" + getComponent<CreatureInfo>().getName() + " doesn't resist.</color>";
                }
                else
                {
                    resultText += "<color=lime>" + getComponent<CreatureInfo>().getName() + " struggles and yells for help, but nobody comes.</color>";
                }

                resultText += "\n<color=lime>" + partner.getComponent<CreatureInfo>().getName() + " kidnaps the Conservative!</color>";

                actionRoot.Add(() => { mc.uiController.meeting.printText(resultText); }, "Kidnap Result");
                
                partner.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().addNewHostage(owner);
                //Add one day because otherwise it will ready "0 day in captivity"
                getComponent<Hostage>().timeInCaptivity++;

                actionRoot.Add(() => { partner.getComponent<Liberal>().plannedDates.Remove(owner); removeMe(); mc.doNextAction(); }, "Dating cleanup");
            }
            else
            {
                partner.getComponent<CriminalRecord>().addCrime(Constants.CRIME_KIDNAPPING);
                breakUp();
                if (mc.LCSRandom(2) == 0)
                {
                    resultText += "<color=magenta>" + getComponent<CreatureInfo>().getName() + " manages to escape on the way back to the safehouse!\n" + partner.getComponent<CreatureInfo>().getName() +" has failed to kidnap the Conservative.</color>";
                }
                else
                {
                    resultText += "<color=red>" + getComponent<CreatureInfo>().getName() + "'s fist is the last thing " + partner.getComponent<CreatureInfo>().getName() + " remembers seeing!\nThe Liberal wakes up in the " + partner.getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("GOVERNMENT_POLICE_STATION").getComponent<SiteBase>().getCurrentName() + "...</color>";
                    partner.getComponent<CriminalRecord>().arrest();
                }
                actionRoot.Add(() => { mc.uiController.meeting.printText(resultText); }, "Kidnap Result");
            }
        }

        public void initDating(Entity partner)
        {
            this.partner = partner;
            timeleft = 0;

            //People don't go to dates armed and wearing work clothes.
            //TODO: Hold on to weapon/armor so they'll bring them to the LCS if they get recruited?
            getComponent<Inventory>().destroyWeapon();
            if ((getComponent<Inventory>().getArmor().getComponent<Armor>().getFlags() & ItemDef.ArmorFlags.DATE_APPROPRIATE) == 0)
            {
                getComponent<Inventory>().destroyArmor();
                getComponent<Inventory>().equipArmor(Factories.ItemFactory.create("ARMOR_CLOTHES"));
            }
        }
    }
}
