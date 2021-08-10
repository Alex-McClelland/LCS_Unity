using System;
using System.Collections.Generic;
using LCS.Engine.Scenes;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Location;
using LCS.Engine.Components.Item;
using LCS.Engine.Data;
using System.Xml;

namespace LCS.Engine.Components.Creature
{
    public class Liberal : Component
    {
        public enum RecruitType
        {
            NORMAL,
            LOVE_SLAVE,
            ENLIGHTENED
        }

        public enum Status
        {
            ACTIVE,
            SLEEPER,
            HOSPITAL,
            JAIL_POLICE_CUSTODY,
            JAIL_COURT,
            JAIL_PRISON,
            AWAY,
            DEAD
        }

        public Activity dailyActivity { get; set; }
        public List<Entity> subordinates { get; set; }
        public List<Entity> plannedMeetings { get; set; }
        public List<Entity> plannedDates { get; set; }
        public LiberalCrimeSquad.Squad squad { get; set; }
        [SimpleSave]
        public Entity leader;
        [SimpleSave]
        public RecruitType recruitType;
        [SimpleSave]
        public Entity homeBase;
        [SimpleSave]
        public Entity targetBase;        
        [SimpleSave]
        public Status status;
        [SimpleSave]
        public int awayTime;
        [SimpleSave]
        public int infiltration;
        [SimpleSave]
        public float managerPosX;
        [SimpleSave]
        public float managerPosY;
        [SimpleSave]
        public bool disbanded;
        public DateTime joinDate;

        public Entity hauledUnit;

        public Liberal()
        {
            subordinates = new List<Entity>();
            plannedDates = new List<Entity>();
            plannedMeetings = new List<Entity>();
            awayTime = 0;

            dailyActivity = new Activity("NONE", null, null);
            disbanded = false;
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Liberal");
                entityNode.AppendChild(saveNode);
                XmlNode joinNode = saveNode.OwnerDocument.CreateElement("joinDate");
                saveNode.AppendChild(joinNode);
            }

            saveSimpleFields();
            dailyActivity.save(saveNode);
            saveNode.SelectSingleNode("joinDate").InnerText = joinDate.ToString("d");

            if (saveNode.SelectSingleNode("subordinates") != null)
                saveNode.RemoveChild(saveNode.SelectSingleNode("subordinates"));
            if (saveNode.SelectSingleNode("meetings") != null)
                saveNode.RemoveChild(saveNode.SelectSingleNode("meetings"));
            if (saveNode.SelectSingleNode("dates") != null)
                saveNode.RemoveChild(saveNode.SelectSingleNode("dates"));

            XmlNode subordinatesNode = saveNode.OwnerDocument.CreateElement("subordinates");
            saveNode.AppendChild(subordinatesNode);
            XmlNode meetingsNode = saveNode.OwnerDocument.CreateElement("meetings");
            saveNode.AppendChild(meetingsNode);
            XmlNode datesNode = saveNode.OwnerDocument.CreateElement("dates");
            saveNode.AppendChild(datesNode);

            foreach (Entity e in subordinates)
            {
                XmlNode subNode = saveNode.OwnerDocument.CreateElement("subordinate");
                subNode.InnerText = e.guid.ToString();
                subordinatesNode.AppendChild(subNode);
            }
            foreach (Entity e in plannedMeetings)
            {
                XmlNode meetingNode = saveNode.OwnerDocument.CreateElement("plannedMeeting");
                meetingNode.InnerText = e.guid.ToString();
                meetingsNode.AppendChild(meetingNode);
            }
            foreach (Entity e in plannedDates)
            {
                XmlNode dateNode = saveNode.OwnerDocument.CreateElement("plannedDate");
                dateNode.InnerText = e.guid.ToString();
                datesNode.AppendChild(dateNode);
            }
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);
            XmlNode activityNode = componentData.SelectSingleNode("Activity");
            setActivity(activityNode.Attributes["type"].Value, activityNode.Attributes["subType"].Value, activityNode.Attributes["interrogationTarget"].Value != "null" ? entityList[int.Parse(activityNode.Attributes["interrogationTarget"].Value)] : null);

            if (componentData.SelectSingleNode("joinDate") != null)
                joinDate = DateTime.Parse(componentData.SelectSingleNode("joinDate").InnerText);
            else
            {
                joinDate = MasterController.GetMC().currentDate;
                MasterController.GetMC().addDebugMessage("Liberal " + getComponent<CreatureInfo>().getName() + " missing join date, set to current date");
            }

            foreach (XmlNode node in componentData.SelectSingleNode("subordinates").ChildNodes)
                subordinates.Add(entityList[int.Parse(node.InnerText)]);
            foreach (XmlNode node in componentData.SelectSingleNode("meetings").ChildNodes)
                plannedMeetings.Add(entityList[int.Parse(node.InnerText)]);
            foreach (XmlNode node in componentData.SelectSingleNode("dates").ChildNodes)
                plannedDates.Add(entityList[int.Parse(node.InnerText)]);
        }

        public override void subscribe()
        {
            base.subscribe();
            MasterController.GetMC().nextDay += doDaily;
            MasterController.GetMC().nextMonth += doMonthly;
        }

        public override void selfSubscribe()
        {
            base.selfSubscribe();
            getComponent<CreatureBase>().die += doDie;
        }

        public override void unsubscribe()
        {
            base.unsubscribe();
            getComponent<CreatureBase>().die -= doDie;
            MasterController.GetMC().nextDay -= doDaily;
            MasterController.GetMC().nextMonth -= doMonthly;
        }

        public void liberalize(Entity leader, RecruitType recruitType)
        {
            this.recruitType = recruitType;
            leader.getComponent<Liberal>().subordinates.Add(owner);
            this.leader = leader;
            homeBase = leader.getComponent<Liberal>().homeBase;
            if(MasterController.GetMC().phase != MasterController.Phase.TROUBLE) goHome();
            joinDate = MasterController.GetMC().currentDate;

            //Infiltration won't matter if they aren't a sleeper, but it should be calculated before they have their alignment changed
            if (getComponent<CreatureInfo>().alignment == Alignment.LIBERAL)
                infiltration = 10 + MasterController.GetMC().LCSRandom(10);
            else if (getComponent<CreatureInfo>().alignment == Alignment.MODERATE)
                infiltration = 20 + MasterController.GetMC().LCSRandom(10);
            else {
                infiltration = MasterController.GetMC().LCSRandom(GameData.getData().creatureDefList[owner.def].infiltration);
                infiltration += (int)(35 * ((100 - infiltration) / 100d) + (MasterController.GetMC().LCSRandom(10) - 5));
            }

            //Recently converted libs lose half their infiltration
            if ((getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.CONVERTED) != 0) infiltration /= 2;

            if (infiltration > 100) infiltration = 100;
            if (infiltration < 0) infiltration = 0;

            //Whatever cash they had on hand they'll donate to the cause when they join
            MasterController.lcs.changeFunds(MasterController.GetMC().LCSRandom(GameData.getData().creatureDefList[owner.def].money));

            getComponent<CreatureInfo>().alignment = Alignment.LIBERAL;
            //Thinking of being able to (VERY RARELY) encounter politician entities in-game. If they get converted by any means, they will flip their political stance too.
            if (hasComponent<Politician>()) getComponent<Politician>().alignment = Alignment.ELITE_LIBERAL;

            //If this is a CCS member, reveal their home base
            if (getComponent<CreatureInfo>().workLocation.hasComponent<SafeHouse>() &&
                (getComponent<CreatureInfo>().workLocation.getComponent<SafeHouse>().getFlags() & LocationDef.BaseFlag.CCS_BASE) != 0 &&
                !getComponent<CreatureInfo>().workLocation.getComponent<SafeHouse>().owned)
                getComponent<CreatureInfo>().workLocation.getComponent<SiteBase>().hidden = false;

            owner.persist();
        }

        public void sleeperize()
        {
            status = Status.SLEEPER;
            homeBase = getComponent<CreatureInfo>().workLocation;
            goHome();
            
            setActivity("SLEEPER_NONE");

            if (hasComponent<Politician>())
            {
                getComponent<Politician>().alignment = Alignment.ELITE_LIBERAL;
            }
        }

        public void fireLiberal()
        {
            if (owner.getComponent<CreatureBase>().getAttributeValue(Constants.ATTRIBUTE_HEART) <
                    owner.getComponent<CreatureBase>().getAttributeValue(Constants.ATTRIBUTE_WISDOM) +
                    MasterController.GetMC().LCSRandom(5) &&
                    leader.getComponent<CriminalRecord>().isMajorCriminal())
            {
                leader.getComponent<CriminalRecord>().addCrime(Constants.CRIME_RACKETEERING);
                leader.getComponent<CriminalRecord>().Confessions++;
                if (leader.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().heat > 20)
                {
                    leader.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().timeUntilLocated = 3;
                }
                else
                {
                    leader.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().heat += 20;
                    leader.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().floatingHeat += 20;
                }

                string text = "A Liberal friend tips you off on " + owner.getComponent<CreatureInfo>().getName() +
                    "'s whereabouts. The Conservative traitor has ratted you out to the police, and sworn to testify against " +
                    leader.getComponent<CreatureInfo>().getName() + " in court.";

                MasterController.GetMC().addMessage(text, true);
                MasterController.GetMC().doNextAction();
            }

            leaveLCS();
        }

        private void doDie(object sender, Events.Die args)
        {
            string nameString = getComponent<CreatureInfo>().givenName;
            if (getComponent<CreatureInfo>().alias != "") nameString += " \"" + getComponent<CreatureInfo>().alias + "\"";
            nameString += " " + getComponent<CreatureInfo>().surname;
            List<string> damagedOrgans = new List<string>();

            foreach (Body.BodyPart part in getComponent<Body>().BodyParts)
            {
                foreach (Body.Organ o in part.Organs)
                {
                    if (o.Health != Body.Organ.Damage.FINE)
                        damagedOrgans.Add(o.Name);
                }
            }
            MasterController.lcs.liberalMartyrs.Add(new LiberalCrimeSquad.Memorial(getComponent<Portrait>().copy(), getComponent<Age>().getAge() >= (getComponent<Body>().getSpecies().oldage - getComponent<Body>().getSpecies().oldage/6), nameString, args.cause, MasterController.GetMC().currentDate, damagedOrgans));
            MasterController.GetMC().addMessage(getComponent<CreatureInfo>().getName() + " " + args.cause);
            //If the Founder dies (probably due to old age) after the LCS has disbanded, the game ends as there is no contact between members even if they would have rejoined
            if (!MasterController.GetMC().canSeeThings && leader == null)
                MasterController.GetMC().endGameState = MasterController.EndGame.DISBANDLOSS;
            /*
            else
                leaveLCS();
            */

            setActivity("NONE");
            status = Status.DEAD;
        }

        private void doDaily(object sender, EventArgs args)
        {
            MasterController mc = MasterController.GetMC();
            //Queue up dates
            if (plannedDates.Count > 1)
            {
                string text = getComponent<CreatureInfo>().getName() + " has dates to manage with ";
                for(int i = 0; i < plannedDates.Count; i++)
                {
                    text += plannedDates[i].getComponent<CreatureInfo>().getName();
                    if (i <= plannedDates.Count - 3)
                        text += ", ";
                    else if (i == plannedDates.Count - 2)
                        text += " and ";
                    else
                    {
                        if (status == Status.HOSPITAL)
                            text += " at " + getComponent<CreatureBase>().Location.getComponent<SiteBase>().getCurrentName();
                        text += ".";
                    }
                }
                if(status == Status.JAIL_COURT ||
                status == Status.JAIL_POLICE_CUSTODY ||
                status == Status.JAIL_PRISON)
                {
                    text += "\n<color=magenta>...But " + getComponent<CreatureInfo>().heShe().ToLower() + " can't go because " + getComponent<CreatureInfo>().heShe().ToLower() + "'s in prison!</color>";
                }
                else if(mc.LCSRandom(plannedDates.Count>2?4:6) == 0)
                {
                    switch (mc.LCSRandom(3))
                    {
                        case 0:
                            text += "\nUnfortunately, they" + (plannedDates.Count>2?" all ":" ") + "know each other and had been discussing " + getComponent<CreatureInfo>().getName() + ". An ambush was set for the lying dog...";
                            break;
                        case 1:
                            text += "\nUnfortunately, they" + (plannedDates.Count > 2 ? " all " : " ") + "turn up at the same time.";
                            break;
                        case 2:
                            if (plannedDates.Count > 2)
                                text += "\n" + getComponent<CreatureInfo>().getName() + " realizes that " + getComponent<CreatureInfo>().heShe().ToLower() + " has committed to eating " + MasterController.NumberToWords(plannedDates.Count).ToLower() + " meals at once.";
                            else
                                text += "\n" + getComponent<CreatureInfo>().getName() + " mixes up the names of " + plannedDates[0].getComponent<CreatureInfo>().getName() + " and " + plannedDates[1].getComponent<CreatureInfo>().getName() + ".";
                            text += "\nThings go downhill fast.";
                            break;
                    }

                    string[] date_fail =
                    {
                        " is publicly humiliated.",
                        " runs away.",
                        " escapes through the bathroom window.",
                        " spends the night getting drunk alone.",
                        " gets chased out by an angry mob.",
                        " gets stuck washing dishes all night.",
                        " is rescued by a passing Elite Liberal.",
                        " makes like a tree and leaves."
                    };

                    getComponent<CreatureBase>().juiceMe(-5, -50);
                    text += "\n" + getComponent<CreatureInfo>().getName() + date_fail[mc.LCSRandom(date_fail.Length)];
                    foreach(Entity e in plannedDates)
                    {
                        e.depersist();
                    }
                    plannedDates.Clear();
                }

                mc.addAction(() =>
                {
                    mc.uiController.showPopup(text, mc.doNextAction);
                }, "Show Date Popup");
            }
            foreach(Entity e in plannedDates)
            {
                e.getComponent<Dating>().doStartDate();
            }

            //Count down timer if currently away
            if(status == Status.AWAY && awayTime >= 0)
            {
                awayTime--;                
                //If their current home base is under siege, head to the homeless shelter. If THAT'S under siege, find another safehouse. If they are ALL under siege (what are you DOING?) lay low for a bit longer.
                if (homeBase.getComponent<SafeHouse>().underSiege)
                {
                    if (!getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER").getComponent<SafeHouse>().underSiege)
                    {
                        homeBase = getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER");
                    }
                    else
                    {
                        bool foundSafeHouse = false;

                        foreach(Entity e in getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getAllBases(true))
                        {
                            if (!e.getComponent<SafeHouse>().underSiege)
                            {
                                foundSafeHouse = true;
                                homeBase = e;
                                break;
                            }
                        }

                        if (!foundSafeHouse)
                            awayTime++;
                    }
                }

                if (awayTime == 0)
                {
                    status = Status.ACTIVE;
                    goHome();
                    //(the dating vacation message is handled in the Dating component of their partner, rather than here)
                    if (plannedDates.Count == 0)
                    {
                        mc.addMessage(getComponent<CreatureInfo>().getName() + " returns to the LCS.");
                    }
                }
            }

            //If they've been killed or disbanded, remove them from their squad if they still have one
            if (disbanded && squad != null) squad.Remove(owner);
        }

        public void doMonthly(object sender, EventArgs args)
        {
            //Train seduction skill if they have a lover
            if (recruitType == RecruitType.LOVE_SLAVE)
                getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].addExperience(5);
            if(getLoverCount() > 0)
                getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].addExperience(5*getLoverCount());

            //Sleeper Infiltration
            bool infiltrate = true;

            //Train a little bit of the skills the sleeper uses in their day job
            if(status == Status.SLEEPER)
            {
                foreach (SkillDef skill in GameData.getData().creatureDefList[owner.def].sleeper.bonusSkills)
                {
                    getComponent<CreatureBase>().Skills[skill.type].addExperience(10);
                }
            }

            switch (dailyActivity.type)
            {
                case "SLEEPER_ADVOCATE":
                    //The effect of liberal advocacy is calculated as part of the monthly update in the Public class
                    infiltration -= 2;
                    break;
                case "SLEEPER_STEAL":
                    activitySleeperSteal();
                    infiltrate = false;
                    break;
                case "SLEEPER_RECRUIT":
                    activitySleeperRecruit();
                    break;
                case "SLEEPER_SNOOP":
                    activitySleeperSnoop();
                    break;
                case "SLEEPER_EMBEZZLE":
                    activitySleeperEmbezzle();
                    break;
                case "SLEEPER_JOIN":
                    activitySleeperJoin();
                    break;
            }

            if (infiltrate) infiltration += MasterController.GetMC().LCSRandom(8) - 2;
            if (infiltration > 100)
                infiltration = 100;
            if (infiltration < 0)
                infiltration = 0;
        }

        public void recruit(Entity e, RecruitType recruitType = RecruitType.NORMAL)
        {
            Liberal lib = new Liberal();
            e.setComponent(lib);
            lib.liberalize(owner, recruitType);
            MasterController.highscore.recruits++;
        }

        public bool canRecruit()
        {
            if (getNormalSubordinateCount() < getSubordinateLimit()) return true;
            else return false;
        }

        public void goHome()
        {
            //If the squad no longer owns their home base, go to the homeless shelter instead.
            if (status != Status.SLEEPER &&
                homeBase.hasComponent<SafeHouse>() && 
                !homeBase.getComponent<SafeHouse>().owned)
                homeBase = getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER");

            getComponent<CreatureBase>().Location = homeBase;
            targetBase = null;

            if(hauledUnit != null)
            {
                if (!hauledUnit.getComponent<Body>().Alive)
                {
                    hauledUnit.getComponent<CreatureBase>().Location = homeBase;
                }
                else if (!hauledUnit.hasComponent<Liberal>())
                {
                    homeBase.getComponent<SafeHouse>().addNewHostage(hauledUnit);
                }
                else
                {
                    //Rescued libs should rebase to where they have been hauled
                    hauledUnit.getComponent<Liberal>().homeBase = homeBase;
                    hauledUnit.getComponent<Liberal>().goHome();
                }

                hauledUnit.persist();

                hauledUnit = null;
            }

            //If the LCS is disbanded, then this member should disband themselves as well
            if (!MasterController.GetMC().canSeeThings)
            {
                status = Status.AWAY;
                awayTime = -1;
            }
        }

        public void changeSquad(LiberalCrimeSquad.Squad newSquad)
        {
            if (squad != null) squad.Remove(owner);

            if (newSquad != null)
            {
                newSquad.Add(owner);
                targetBase = null;
            }
            else squad = null;
        }

        public void promote()
        {
            if (leader.getComponent<Liberal>().leader == null) return;

            if (leader.getComponent<Liberal>().leader.getComponent<Liberal>().getSubordinateLimit() -
                leader.getComponent<Liberal>().leader.getComponent<Liberal>().getNormalSubordinateCount() == 0) return;

            leader.getComponent<Liberal>().subordinates.Remove(owner);

            leader = leader.getComponent<Liberal>().leader;
            leader.getComponent<Liberal>().subordinates.Add(owner);            
        }

        public void leaveLCS(bool loseSubordinateContact = false)
        {
            disbanded = true;
            //Dates and meetings no longer have a point of contact; clear them from the save
            foreach(Entity e in plannedMeetings)
                e.depersist();
            foreach (Entity e in plannedDates)
                e.depersist();

            plannedMeetings.Clear();
            plannedDates.Clear();

            if (squad != null)
                squad.Remove(owner);

            List<Entity> sortedSubordinates = new List<Entity>();

            if(leader != null) leader.getComponent<Liberal>().subordinates.Remove(owner);

            //If the founder dies, does anyone else have the courage to lead?
            if(leader == null)
            {
                Entity juiciestLiberal = null;

                foreach (Entity e in MasterController.lcs.getAllMembers())
                {
                    /*Inelligible to take over for the founder: 
                     -The recently deceased founder him/her/xem-self (duh),
                     -Brainwashed conservatives (their minds are too broken to lead anyone)
                     -Sleepers (no contact with the rest of the LCS)
                     -Liberals serving life sentences (same as sleepers)
                     -Liberals with less than 100 juice (not dedicated enough to the cause)*/
                    if (e == owner || 
                        e.getComponent<Liberal>().recruitType == RecruitType.ENLIGHTENED ||
                        e.getComponent<Liberal>().status == Status.SLEEPER ||
                        (e.getComponent<Liberal>().status == Status.JAIL_PRISON && e.getComponent<CriminalRecord>().LifeSentences > 0)) continue;

                    if(e.getComponent<CreatureBase>().Juice >= 100)
                    {
                        if (juiciestLiberal == null) juiciestLiberal = e;
                        else if (e.getComponent<CreatureBase>().Juice > juiciestLiberal.getComponent<CreatureBase>().Juice) juiciestLiberal = e;
                    }
                }

                //If we found a replacement, elevate them to the top, then pretend the founder was a subordinate of theirs and promote liberals up as appropriate.
                if(juiciestLiberal != null)
                {
                    MasterController.lcs.founder = juiciestLiberal;
                    if (juiciestLiberal.getComponent<Liberal>().recruitType == RecruitType.LOVE_SLAVE)
                        juiciestLiberal.getComponent<Liberal>().recruitType = RecruitType.NORMAL;
                    juiciestLiberal.getComponent<Liberal>().leader.getComponent<Liberal>().subordinates.Remove(juiciestLiberal);
                    juiciestLiberal.getComponent<Liberal>().leader = null;
                    leader = juiciestLiberal;
                    MasterController.GetMC().addMessage("With the death of " + getComponent<CreatureInfo>().getName() + ", " + juiciestLiberal.getComponent<CreatureInfo>().getName() + " has stepped up to lead in these dark times.", true);
                }
                else
                {
                    //THE END OF THE LIBERAL CRIME SQUAD!!!
                    MasterController.GetMC().addMessage("With the death of " + getComponent<CreatureInfo>().getName() + ", there are none left with the courage and convinction to lead...", true);

                    if (getComponent<CreatureBase>().Location.hasComponent<SafeHouse>() &&
                        getComponent<CreatureBase>().Location.getComponent<SafeHouse>().underSiege)
                    {
                        switch (getComponent<CreatureBase>().Location.getComponent<SafeHouse>().siegeType)
                        {
                            case LocationDef.EnemyType.POLICE:
                                MasterController.GetMC().endGameState = MasterController.EndGame.POLICE;
                                break;
                            case LocationDef.EnemyType.AGENT:
                                MasterController.GetMC().endGameState = MasterController.EndGame.CIA;
                                break;
                            case LocationDef.EnemyType.REDNECK:
                                MasterController.GetMC().endGameState = MasterController.EndGame.HICKS;
                                break;
                            case LocationDef.EnemyType.MERC:
                                MasterController.GetMC().endGameState = MasterController.EndGame.CORP;
                                break;
                            case LocationDef.EnemyType.CCS:
                                MasterController.GetMC().endGameState = MasterController.EndGame.CCS;
                                break;
                            case LocationDef.EnemyType.FIREMEN:
                                MasterController.GetMC().endGameState = MasterController.EndGame.FIREMEN;
                                break;
                        }
                    }
                    else {
                        MasterController.GetMC().endGameState = MasterController.EndGame.DEAD;
                    }
                }
            }

            if (leader != null)
            {
                //Promote up any subordinates, unless the chain is totally broken
                if (!leader.getComponent<Liberal>().disbanded && !loseSubordinateContact)
                {
                    //Sort subordinates by most juice so if they can't all promote, the best ones stay.
                    while (sortedSubordinates.Count < subordinates.Count)
                    {
                        Entity topJuice = null;

                        foreach (Entity e in subordinates)
                        {
                            if (!sortedSubordinates.Contains(e))
                            {
                                if (topJuice == null) topJuice = e;
                                else if (e.getComponent<CreatureBase>().Juice > topJuice.getComponent<CreatureBase>().Juice) topJuice = e;
                            }
                        }

                        sortedSubordinates.Add(topJuice);
                    }

                    foreach (Entity e in sortedSubordinates)
                    {
                        if (leader.getComponent<Liberal>().getSubordinateLimit() - leader.getComponent<Liberal>().getNormalSubordinateCount() > 0)
                        {
                            //A liberal needs at least a little bit of juice to continue on with the LCS if they lose their leader
                            if (e.getComponent<Liberal>().recruitType != RecruitType.LOVE_SLAVE && e.getComponent<CreatureBase>().Juice > 0)
                                e.getComponent<Liberal>().promote();
                            //Love slaves will normally only follow their lovers, but if they are juiced enough then losing their lover will lead them to embrace the cause.
                            else if (e.getComponent<CreatureBase>().Juice >= 100)
                            {
                                e.getComponent<Liberal>().recruitType = RecruitType.NORMAL;
                                e.getComponent<Liberal>().promote();
                            }
                            else
                            {
                                e.getComponent<Liberal>().leaveLCS();
                            }
                        }
                        else
                        {
                            e.getComponent<Liberal>().leaveLCS();
                        }
                    }
                }
                else
                {
                    if(!leader.getComponent<Liberal>().disbanded)
                        MasterController.GetMC().addMessage(getComponent<CreatureInfo>().getName() + " has abandoned the LCS.");
                    else
                        MasterController.GetMC().addMessage(getComponent<CreatureInfo>().getName() + " has lost contact with the LCS.");
                    foreach (Entity e in new List<Entity>(subordinates))
                    {
                        e.getComponent<Liberal>().leaveLCS();
                    }
                }
            }
            //Depersisting the entity on death is handled in the Body component - this is for Liberals that get flipped or abandon the LCS
            if (getComponent<Body>().Alive)
            {
                owner.depersist();
            }
        }

        public int getSubordinateLimit()
        {
            if (recruitType == RecruitType.ENLIGHTENED) return 0;

            int subordinateLimit = 0;
            int juice = getComponent<CreatureBase>().Juice;

            if (juice >= 500) subordinateLimit = 6;
            else if (juice >= 200) subordinateLimit = 5;
            else if (juice >= 100) subordinateLimit = 3;
            else if (juice >= 50) subordinateLimit = 1;

            //Founder gets extra subordinates
            if (leader == null)
            {
                subordinateLimit += 6;
            }

            return subordinateLimit;
        }

        public int getNormalSubordinateCount()
        {
            int subordinateCount = subordinates.Count;

            foreach(Entity e in subordinates)
            {
                if(e.getComponent<Liberal>().recruitType != RecruitType.NORMAL)
                {
                    subordinateCount--;
                }
            }

            return subordinateCount;
        }

        public int getLoverCount()
        {
            int loverCount = 0;

            if (recruitType == RecruitType.LOVE_SLAVE) loverCount++;

            foreach (Entity e in subordinates)
            {
                if (e.getComponent<Liberal>().recruitType == RecruitType.LOVE_SLAVE)
                {
                    loverCount++;
                }
            }

            return loverCount;
        }

        public void setActivity(string activity, string subType = null, Entity target = null)
        {
            Entity oldTarget = null;
            if (subType == "null") subType = null;
            if(dailyActivity.type == "INTERROGATE")
            {
                oldTarget = dailyActivity.interrogationTarget;
            }

            dailyActivity = new Activity(activity, subType, target);
            if (oldTarget != null)
                oldTarget.getComponent<Hostage>().refreshInterrogation();

            //HACK: Find better fix for load error than just checking if target has hostage component
            if(activity == "INTERROGATE" && target.hasComponent<Hostage>())
            {
                target.getComponent<Hostage>().refreshInterrogation();
            }
        }

        public void quickSetActivity(string type)
        {
            //Quickset is only for active liberals
            if (status != Status.ACTIVE) return;
            //No, you NEED to go to the hospital
            if (getComponent<Body>().BadlyHurt) return;
            
            switch (type)
            {
                case "ACTIVISM":
                    if (getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].getModifiedValue() > 7 || getComponent<CreatureBase>().Juice < 0)
                        setActivity("COMMUNITY_SERVICE");
                    else if(getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].getModifiedValue() > 4)
                        setActivity("LIBERAL_DISOBEDIENCE");
                    else if (getComponent<CreatureBase>().Skills[Constants.SKILL_COMPUTERS].level > 2)
                        setActivity("HACKING");
                    else if (getComponent<CreatureBase>().Skills[Constants.SKILL_ART].level > 1)
                        setActivity("GRAFFITI");
                    else
                        setActivity("LIBERAL_DISOBEDIENCE");
                    break;
                case "LEGAL_FUNDRAISING":
                    List<string> skills = new List<string>(new string[] { Constants.SKILL_MUSIC, Constants.SKILL_ART, Constants.SKILL_TAILORING, Constants.SKILL_PERSUASION });
                    if (getComponent<Inventory>().hasInstrument())
                        setActivity("SELL_MUSIC");
                    else
                    {
                        CreatureBase.Skill bestSkill = getComponent<CreatureBase>().getBestSkill(skills);
                        if (bestSkill.level == 0)
                            setActivity("DONATIONS");
                        else if (bestSkill.type == Constants.SKILL_MUSIC)
                            setActivity("SELL_MUSIC");
                        else if (bestSkill.type == Constants.SKILL_ART)
                            setActivity("SELL_ART");
                        else if (bestSkill.type == Constants.SKILL_TAILORING)
                            setActivity("SELL_SHIRTS");
                        else
                            setActivity("DONATIONS");
                    }
                    break;
                case "ILLEGAL_FUNDRAISING":
                    if (getComponent<CreatureBase>().Skills[Constants.SKILL_COMPUTERS].level > 1)
                        setActivity("CCFRAUD");
                    if (getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].level > 1 &&
                        getComponent<Age>().getAge() >= 18 &&
                        getComponent<Body>().getSpecies().type == "HUMAN")
                        setActivity("PROSTITUTION");
                    else
                        setActivity("SELL_DRUGS");
                    break;
                case "CHECK_POLLS":
                    setActivity("OPINION_POLLS");
                    break;
                case "STEAL_CARS":
                    setActivity("STEAL_VEHICLE", "RANDOM");
                    break;
                case "COMMUNITY_SERVICE":
                    setActivity("COMMUNITY_SERVICE");
                    break;
                case "NONE":
                    setActivity("NONE");
                    break;
            }
        }

        public string getActivityName()
        {
            switch (dailyActivity.type)
            {
                case "NONE":
                case "SLEEPER_NONE":
                    return "Laying Low";
                case "COMMUNITY_SERVICE":
                    return "Performing Community Service";
                case "LIBERAL_DISOBEDIENCE":
                    return "Causing Trouble";
                case "GRAFFITI":
                    return "Spraying Graffiti";
                case "OPINION_POLLS":
                    return "Searching Opinion Polls";
                case "HACKING":
                    return "Hacking";
                case "WRITE_NEWSPAPER":
                    return "Writing to Newspapers";
                case "WRITE_GUARDIAN":
                    return "Writing for the Liberal Guardian";
                case "DONATIONS":
                    return "Soliciting Donations";
                case "SELL_SHIRTS":
                    if (getComponent<CreatureBase>().Skills["TAILORING"].level >= 8)
                        return "Selling Liberal T-Shirts";
                    else if (getComponent<CreatureBase>().Skills["TAILORING"].level >= 4)
                        return "Selling Embroidered Shirts";
                    else
                        return "Selling Tie-Dyed T-Shirts";
                case "SELL_ART":
                    if (getComponent<CreatureBase>().Skills["ART"].level >= 8)
                        return "Selling Liberal Art";
                    else if (getComponent<CreatureBase>().Skills["ART"].level >= 8)
                        return "Selling Paitings";
                    else
                        return "Selling Portrait Sketches";
                case "SELL_MUSIC":
                    return "Busking for Tips";
                case "SELL_DRUGS":
                    return "Selling Brownies";
                case "PROSTITUTION":
                    return "Prostituting";
                case "CCFRAUD":
                    return "Stealing Credit Cards";
                case "TEACH_ACTIVISM":
                    return "Teaching Political Activism";
                case "TEACH_INFILTRATION":
                    return "Teaching Infiltration";
                case "TEACH_WARFARE":
                    return "Teaching Urban Warfare";
                case "LEARN":
                    return "Learning " + GameData.getData().skillList[dailyActivity.subType].name;
                case "MEND_CLOTHING":
                    return "Repairing Clothing/Armor";
                case "FIRST_AID":
                    return "Tending to Liberals";
                case "MOVE_CLINIC":
                    return "Moving to Free Clinic";
                case "INTERROGATE":
                    return "Interrogating " + dailyActivity.interrogationTarget.getComponent<CreatureInfo>().getName();
                case "DISPOSE_BODIES":
                    return "Dumping Bodies";
                case "GET_WHEELCHAIR":
                    return "Acquiring Wheelchair";
                case "MAKE_CLOTHING":
                    return "Making " + (MasterController.GetMC().isFuture() ? GameData.getData().itemList[dailyActivity.subType].nameFuture : GameData.getData().itemList[dailyActivity.subType].name);
                case "STEAL_VEHICLE":
                    return "Stealing A Car";
                case "SLEEPER_ADVOCATE":
                    return "Promoting Liberalism";
                case "SLEEPER_RECRUIT":
                    return "Expanding Sleeper Network";
                case "SLEEPER_SNOOP":
                    return "Uncovering Secrets";
                case "SLEEPER_EMBEZZLE":
                    return "Embezzling Funds";
                case "SLEEPER_STEAL":
                    return "Stealing Equipment";
                case "SLEEPER_JOIN":
                    return "Join Active LCS";
                default:
                    return "BAD ACTIVITY ID";
            }
        }

        public void doActivity()
        {
            //Did they act with a squad earlier? Skip activity unless it is moving to the clinic to heal
            if (squad != null && squad.target != null && dailyActivity.type != "MOVE_CLINIC")
            {
                if(dailyActivity.type != "NONE")
                    MasterController.GetMC().addMessage(getComponent<CreatureInfo>().getName() + " acted with " + getComponent<CreatureInfo>().hisHer().ToLower() + " squad instead of " + getActivityName().ToLower());
                return;
            }
            //If they are away, remember what their activity is, but don't actually do it until they get back.
            if(status == Status.AWAY)
            {
                return;
            }

            //If their interrogation target is dead they should probably stop trying to interrogate them
            if (dailyActivity.type == "INTERROGATE" && 
                (!dailyActivity.interrogationTarget.getComponent<Body>().Alive || 
                !homeBase.getComponent<SafeHouse>().getHostages().Contains(dailyActivity.interrogationTarget)))
                setActivity("NONE");

            //Sleeper activites aren't valid on non-sleepers
            if (dailyActivity.type.StartsWith("SLEEPER") && status != Status.SLEEPER)
                setActivity("NONE");

            //Disable activities if the siege prevents them from getting it done
            if(status == Status.ACTIVE && homeBase.getComponent<SafeHouse>().underSiege)
            {
                //These activities can still be done without leaving the safehouse
                if(!(dailyActivity.type == "MEND_CLOTHING" || 
                    dailyActivity.type == "FIRST_AID" || 
                    dailyActivity.type == "INTERROGATE" ||
                    dailyActivity.type == "TEACH_ACTIVISM" ||
                    dailyActivity.type == "TEACH_INFILTRATION" ||
                    dailyActivity.type == "TEACH_WARFARE" ||
                    dailyActivity.type == "MAKE_CLOTHING"))
                {
                    //These activites can be done in the safehouse, but need electricity
                    if(!(dailyActivity.type == "HACKING" || 
                        dailyActivity.type == "OPINION_POLLS" || 
                        dailyActivity.type == "CCFRAUD") || 
                        homeBase.getComponent<SafeHouse>().lightsOff)
                    setActivity("NONE");
                }
            }

            switch (dailyActivity.type)
            {
                case "NONE":
                    if(status == Status.ACTIVE)
                        activityRepair(true);
                    break;
                case "COMMUNITY_SERVICE":
                    activityCommunityService();
                    break;
                case "LIBERAL_DISOBEDIENCE":
                    activityLiberalDisobedience();
                    break;
                case "GRAFFITI":
                    activityGraffiti();
                    break;
                case "OPINION_POLLS":
                    activityOpinionPolls();
                    break;
                case "HACKING":
                    activityHacking();
                    break;
                case "WRITE_NEWSPAPER":
                    activityWriteNewspaper();
                    break;
                case "WRITE_GUARDIAN":
                    activityWriteGuardian();
                    break;
                case "DONATIONS":
                    activityDonations();
                    break;
                case "SELL_SHIRTS":
                    activitySellShirts();
                    break;
                case "SELL_ART":
                    activitySellArt();
                    break;
                case "SELL_MUSIC":
                    activitySellMusic();
                    break;
                case "SELL_DRUGS":
                    activitySellDrugs();
                    break;
                case "PROSTITUTION":
                    activityProstitution();
                    break;
                case "CCFRAUD":
                    activityCCFraud();
                    break;
                case "TEACH_ACTIVISM":
                case "TEACH_INFILTRATION":
                case "TEACH_WARFARE":
                    activityTeach();
                    break;
                case "LEARN":
                    activityLearn();
                    break;
                case "MEND_CLOTHING":
                    activityRepair();
                    break;
                case "FIRST_AID":
                    //This is handled as part of the daily event in the Body class, with the rest of the healing logic
                    break;
                case "MOVE_CLINIC":
                    activityClinic();
                    break;
                case "DISPOSE_BODIES":
                    activityDisposeBodies();
                    break;
                case "GET_WHEELCHAIR":
                    activityGetWheelchair();
                    break;
                case "MAKE_CLOTHING":
                    activityMakeClothing();
                    break;
                case "STEAL_VEHICLE":
                    activityStealVehicle();
                    break;
                default:
                    //Sleepers have enough freedom to get their clothing dry-cleaned/repaired, if needed
                    if(status == Status.SLEEPER)
                        activityRepair(true);
                    break;
            }
        }

        public class Activity
        {
            public string type { get; set; }
            public string subType { get; set; }
            public Entity interrogationTarget { get; set; }

            public Activity(string type, string subType, Entity interrogationTarget)
            {
                this.type = type;
                this.subType = subType;
                this.interrogationTarget = interrogationTarget;
            }

            public void save(XmlNode node)
            {
                if(node.SelectSingleNode("Activity") != null)
                    node.RemoveChild(node.SelectSingleNode("Activity"));

                XmlNode newNode = node.OwnerDocument.CreateElement("Activity");
                XmlAttribute typeAtt = newNode.OwnerDocument.CreateAttribute("type");
                XmlAttribute subTypeAtt = newNode.OwnerDocument.CreateAttribute("subType");
                XmlAttribute interrogationTargetNode = newNode.OwnerDocument.CreateAttribute("interrogationTarget");

                typeAtt.Value = type;
                if (subType != null) subTypeAtt.Value = subType;
                else subTypeAtt.Value = "null";
                if (interrogationTarget != null) interrogationTargetNode.Value = interrogationTarget.guid.ToString();
                else interrogationTargetNode.Value = "null";

                newNode.Attributes.Append(typeAtt);
                newNode.Attributes.Append(subTypeAtt);
                newNode.Attributes.Append(interrogationTargetNode);
                node.AppendChild(newNode);
            }
        }

        private bool checkForArrest()
        {
            bool attemptArrest = false;
            string arrestMessage = "";
            string storyType = "";

            if(getComponent<Inventory>().armor == null && MasterController.GetMC().LCSRandom(2) == 0)
            {
                getComponent<CriminalRecord>().addCrime(Constants.CRIME_NUDITY);

                attemptArrest = true;
                arrestMessage = getComponent<CreatureInfo>().getName() + " is accosted by police while naked in public.";
                storyType = "NUDITYARREST";
            }
            else if(getComponent<CriminalRecord>().Heat > getComponent<CreatureBase>().Skills["STREET_SENSE"].level * 10 && 
                MasterController.GetMC().LCSRandom(50) == 0)
            {
                attemptArrest = true;
                arrestMessage = getComponent<CreatureInfo>().getName() + " is accosted by police while " + getActivityName().ToLower() + ".";
                storyType = "WANTEDARREST";
            }
            if (attemptArrest)
            {
                MasterController.GetMC().addAction(() =>
                {
                    MasterController.GetMC().addMessage(arrestMessage);
                    ChaseScene scene = new ChaseScene();
                    scene.startActivityFootChase(5, LocationDef.EnemyType.POLICE, storyType, owner, arrestMessage);
                    MasterController.GetMC().doNextAction();
                }, "start new chase: activity arrest");
            }
            return attemptArrest;
        }

        private void activityCommunityService()
        {
            getComponent<CreatureBase>().juiceMe(1, 0);
            if (getComponent<CriminalRecord>().Heat > 0 && MasterController.GetMC().LCSRandom(3) != 0)
                getComponent<CriminalRecord>().Heat--;
            MasterController.generalPublic.changePublicOpinion("LIBERALCRIMESQUADPOS", 1, 0, 80);
        }

        private void activityLiberalDisobedience()
        {
            MasterController.lcs.protestingLiberals.Add(owner);
        }

        private void activityGraffiti()
        {
            MasterController mc = MasterController.GetMC();

            if (!getComponent<Inventory>().canGraffiti())
            {
                bool foundOne = false;

                foreach(Entity e in homeBase.getComponent<SafeHouse>().getInventory())
                {
                    if (!e.hasComponent<Weapon>()) continue;

                    if ((((ItemDef.WeaponDef)GameData.getData().itemList[e.def].components["weapon"]).flags & ItemDef.WeaponFlags.GRAFFITI) != 0)
                    {
                        foundOne = true;

                        string weaponName = mc.isFuture() ? GameData.getData().itemList[e.def].nameFuture : GameData.getData().itemList[e.def].name;

                        mc.addMessage(getComponent<CreatureInfo>().getName() + " grabbed a " + weaponName + " from " + homeBase.getComponent<SiteBase>().getCurrentName() + ".");

                        getComponent<Inventory>().equipWeapon(e);
                        break;
                    }
                }

                if(!foundOne && MasterController.lcs.Money >= 20)
                {
                    MasterController.lcs.changeFunds(-20);
                    mc.addMessage(getComponent<CreatureInfo>().getName() + " bought spraypaint for graffiti.");

                    getComponent<Inventory>().equipWeapon(Factories.ItemFactory.create("WEAPON_SPRAYCAN"));
                }
                else if (!foundOne)
                {
                    mc.addMessage(getComponent<CreatureInfo>().getName() + " needs a spraycan for graffiti.");
                    setActivity("NONE");
                    return;
                }
            }

            string issue = "LIBERALCRIMESQUAD";
            int power = 1;

            if (mc.LCSRandom(10) == 0 && !getComponent<CreatureBase>().Skills["STREET_SENSE"].check(Difficulty.AVERAGE))
            {
                string arrestMessage = getComponent<CreatureInfo>().getName() + " is spotted by police while " + (dailyActivity.subType == null ? "spraying an LCS tag!" : "working on the mural!");
                MasterController.GetMC().addAction(() =>
                {
                    MasterController.GetMC().addMessage(arrestMessage);
                    ChaseScene scene = new ChaseScene();
                    scene.startActivityFootChase(5, LocationDef.EnemyType.POLICE, "GRAFFITIARREST", owner, arrestMessage);
                    MasterController.GetMC().doNextAction();
                }, "start new chase: activity arrest");
                getComponent<CriminalRecord>().addCrime(Constants.CRIME_VANDALISM);

                getComponent<CreatureBase>().Skills["STREET_SENSE"].addExperience(20);
            }
            else if(dailyActivity.subType != null && dailyActivity.subType != "")
            {
                power = 0;
                if(mc.LCSRandom(3) == 0)
                {
                    issue = dailyActivity.subType;
                    power = getComponent<CreatureBase>().Skills["ART"].roll() / 3;

                    mc.addMessage(getComponent<CreatureInfo>().getName() + " has completed a" + (power > 3 ? " beautiful" : "") + " mural about " + GameData.getData().viewList[issue].name + ".");

                    dailyActivity.subType = null;
                    getComponent<CreatureBase>().juiceMe(power, power * 20);
                    MasterController.generalPublic.changePublicOpinion(issue, power);
                    getComponent<CreatureBase>().Skills["ART"].addExperience(Math.Max(10 - getComponent<CreatureBase>().Skills["ART"].level/2, 1));
                }
                else
                {
                    mc.addMessage(getComponent<CreatureInfo>().getName() + " works through the night on a large mural.");
                    getComponent<CreatureBase>().Skills["ART"].addExperience(Math.Max(10 - getComponent<CreatureBase>().Skills["ART"].level / 2, 1));
                }

            }
            else if(mc.LCSRandom(Math.Max(30 - getComponent<CreatureBase>().Skills["ART"].level*2,5)) == 0)
            {
                power = 0;
                issue = MasterController.generalPublic.randomissue(true);
                mc.addMessage(getComponent<CreatureInfo>().getName() + " has begun work on a large mural about " + GameData.getData().viewList[issue].name + ".");
                dailyActivity.subType = issue;
                getComponent<CreatureBase>().Skills["ART"].addExperience(Math.Max(10 - getComponent<CreatureBase>().Skills["ART"].level / 2, 1));
            }

            getComponent<CreatureBase>().Skills["ART"].addExperience(Math.Max(4 - getComponent<CreatureBase>().Skills["ART"].level, 0));

            if(issue == "LIBERALCRIMESQUAD")
            {
                MasterController.generalPublic.changePublicOpinion("LIBERALCRIMESQUAD", mc.LCSRandom(2),0,65);
                MasterController.generalPublic.changePublicOpinion("LIBERALCRIMESQUADPOS", mc.LCSRandom(8)==0?1:0, 0, 65);
                MasterController.generalPublic.PublicInterest[issue] += power;
            }
            else
            {
                MasterController.generalPublic.changePublicOpinion("LIBERALCRIMESQUAD", mc.LCSRandom(2)+1, 0, 85);
                MasterController.generalPublic.changePublicOpinion("LIBERALCRIMESQUADPOS", mc.LCSRandom(4) == 0?1:0, 0, 65);
                MasterController.generalPublic.PublicInterest[issue] += power;
                MasterController.generalPublic.BackgroundLiberalInfluence[issue] += power;
            }
        }

        private void activityOpinionPolls()
        {
            MasterController mc = MasterController.GetMC();

            int skillRoll = getComponent<CreatureBase>().Skills["COMPUTERS"].roll();
            int missChance = 30 - skillRoll;
            int noise;

            if (missChance < 5) missChance = 5;
            if (skillRoll < 1) noise = 18 + mc.LCSRandom(3);
            else if (skillRoll < 2) noise = 16 + mc.LCSRandom(2);
            else if (skillRoll < 3) noise = 14 + mc.LCSRandom(2);
            else if (skillRoll < 4) noise = 12 + mc.LCSRandom(2);
            else if (skillRoll < 5) noise = 10 + mc.LCSRandom(2);
            else if (skillRoll < 6) noise = 8 + mc.LCSRandom(2);
            else if (skillRoll < 7) noise = 7;
            else if (skillRoll < 9) noise = 6;
            else if (skillRoll < 11) noise = 5;
            else if (skillRoll < 14) noise = 4;
            else if (skillRoll < 18) noise = 3;
            else noise = 2;

            foreach(string def in GameData.getData().viewList.Keys)
            {
                //If CCS hasn't been activated yet, or has been defeated, ignore the CCS issue since it's not relevant
                if (def == Constants.VIEW_CONSERVATIVECRIMESQUAD && MasterController.ccs.status == ConservativeCrimeSquad.Status.INACTIVE)
                {
                    continue;
                }
                if (mc.LCSRandom(MasterController.generalPublic.PublicInterest[def] + 100) < missChance) continue;

                if (!MasterController.generalPublic.pollData.ContainsKey(def))
                {
                    Public.PollData newData = new Public.PollData();
                    newData.age = 50;
                    newData.def = def;
                    MasterController.generalPublic.pollData.Add(def, newData);
                }
                Public.PollData pollData = MasterController.generalPublic.pollData[def];

                if (pollData.age > 0 || pollData.noise > noise)
                {
                    pollData.noise = noise;

                    pollData.percent = MasterController.generalPublic.PublicOpinion[def] + mc.LCSRandom(noise * 2 + 1) - noise;
                    if (pollData.percent > 100) pollData.percent = 100;
                    else if (pollData.percent < 0) pollData.percent = 0;

                    if (noise >= 7) pollData.publicInterest = Public.PollData.PublicInterest.UNKNOWN;
                    else if (noise >= 4)
                    {
                        if (MasterController.generalPublic.PublicInterest[def] > 50) pollData.publicInterest = Public.PollData.PublicInterest.HIGH;
                        else pollData.publicInterest = Public.PollData.PublicInterest.LOW;
                    }
                    else
                    {
                        if (MasterController.generalPublic.PublicInterest[def] > 100) pollData.publicInterest = Public.PollData.PublicInterest.VERY_HIGH;
                        else if (MasterController.generalPublic.PublicInterest[def] > 50) pollData.publicInterest = Public.PollData.PublicInterest.HIGH;
                        else if (MasterController.generalPublic.PublicInterest[def] > 10) pollData.publicInterest = Public.PollData.PublicInterest.MODERATE;
                        else if (MasterController.generalPublic.PublicInterest[def] > 0) pollData.publicInterest = Public.PollData.PublicInterest.LOW;
                        else pollData.publicInterest = Public.PollData.PublicInterest.NONE;
                    }
                }

                pollData.age = 0;
            }
        }

        private void activityHacking()
        {
            MasterController.lcs.hackingLiberals.Add(owner);
        }

        private void activityWriteNewspaper()
        {
            Public pub = MasterController.generalPublic;

            if (getComponent<CreatureBase>().Skills["WRITING"].check(Difficulty.EASY))
                pub.BackgroundLiberalInfluence[pub.randomissue()] += 5;

            getComponent<CreatureBase>().Skills["WRITING"].addExperience(1 + MasterController.GetMC().LCSRandom(5));
        }

        private void activityWriteGuardian()
        {
            //Can't write for the Guardian if you've lost your printing press
            if((homeBase.getComponent<SafeHouse>().investments & SafeHouse.Investments.PRINTING_PRESS) == 0)
            {
                activityWriteNewspaper();
                dailyActivity.type = "WRITE_NEWSPAPER";
            }

            Public pub = MasterController.generalPublic;

            if (getComponent<CreatureBase>().Skills["WRITING"].check(Difficulty.EASY))
                pub.BackgroundLiberalInfluence[pub.randomissue()] += 15;
            else
                pub.BackgroundLiberalInfluence[pub.randomissue()] -= 15;

            getComponent<CreatureBase>().Skills["WRITING"].addExperience(1 + MasterController.GetMC().LCSRandom(5));
        }

        private void activityDonations()
        {
            if (!checkForArrest())
            {
                int income = getComponent<CreatureBase>().Skills["PERSUASION"].roll() *
                           getComponent<Inventory>().getProfessionalism() + 1;

                Public pub = MasterController.generalPublic;
                // Country's alignment dramatically affects effectiveness
                // The more conservative the country, the more effective
                if (pub.PublicMood > 90) income /= 2;
                if (pub.PublicMood > 65) income /= 2;
                if (pub.PublicMood > 35) income /= 2;
                if (pub.PublicMood > 10) income /= 2;

                MasterController.lcs.changeFunds(income);

                getComponent<CreatureBase>().Skills["PERSUASION"].addExperience(Math.Max(5 - getComponent<CreatureBase>().Skills["PERSUASION"].level, 2));
            }
        }

        private void activitySellShirts()
        {
            if (!checkForArrest())
            {
                int income = (getComponent<CreatureBase>().Skills["TAILORING"].roll() +
                              getComponent<CreatureBase>().Skills["BUSINESS"].roll()) / 2;

                Public pub = MasterController.generalPublic;

                // Country's alignment affects effectiveness
                // In a Liberal country, there are many competing vendors
                if (pub.PublicMood > 65) income /= 2;
                if (pub.PublicMood > 35) income /= 2;

                //If you're selling epic shirts enough they'll have some political impact
                if (getComponent<CreatureBase>().Skills["TAILORING"].check(Difficulty.FORMIDABLE))
                    pub.BackgroundLiberalInfluence[pub.randomissue()] += 5;

                MasterController.lcs.changeFunds(income);

                getComponent<CreatureBase>().Skills["TAILORING"].addExperience(Math.Max(7 - getComponent<CreatureBase>().Skills["TAILORING"].level, 2));
                getComponent<CreatureBase>().Skills["BUSINESS"].addExperience(Math.Max(7 - getComponent<CreatureBase>().Skills["BUSINESS"].level, 2));
            }
        }

        private void activitySellArt()
        {
            if (!checkForArrest())
            {
                int income = getComponent<CreatureBase>().Skills["ART"].roll();

                Public pub = MasterController.generalPublic;

                // Country's alignment affects effectiveness
                // In a Liberal country, there are many competing vendors
                if (pub.PublicMood > 65) income /= 2;
                if (pub.PublicMood > 35) income /= 2;

                //Epic Liberal art may have positive political effect
                if (getComponent<CreatureBase>().Skills["ART"].check(Difficulty.FORMIDABLE))
                    pub.BackgroundLiberalInfluence[pub.randomissue()] += 5;

                MasterController.lcs.changeFunds(income);

                getComponent<CreatureBase>().Skills["ART"].addExperience(Math.Max(7 - getComponent<CreatureBase>().Skills["ART"].level, 4));
            }
        }

        private void activitySellMusic()
        {
            if (!checkForArrest())
            {
                int income = getComponent<CreatureBase>().Skills["MUSIC"].roll() / 2;
                bool has_instrument = getComponent<Inventory>().hasInstrument();

                if (has_instrument) income *= 4;

                Public pub = MasterController.generalPublic;

                // Country's alignment affects effectiveness
                // In a Liberal country, there are many competing vendors
                if (pub.PublicMood > 65) income /= 2;
                if (pub.PublicMood > 35) income /= 2;

                //Epic Liberal protest songs
                if (getComponent<CreatureBase>().Skills["MUSIC"].check(Difficulty.FORMIDABLE))
                    pub.BackgroundLiberalInfluence[pub.randomissue()] += has_instrument ? 10 : 5;

                MasterController.lcs.changeFunds(income);

                if (has_instrument) getComponent<CreatureBase>().Skills["MUSIC"].addExperience(Math.Max(7 - getComponent<CreatureBase>().Skills["MUSIC"].level, 4));
                else getComponent<CreatureBase>().Skills["MUSIC"].addExperience(Math.Max(5 - getComponent<CreatureBase>().Skills["MUSIC"].level, 2));
            }
        }

        private void activitySellDrugs()
        {
            MasterController mc = MasterController.GetMC();
            Alignment drugLaw = MasterController.government.laws["DRUGS"].alignment;

            //Check for police search
            bool dodgelawroll = mc.LCSRandom(30 * ((int)drugLaw + 3)) != 0;

            //Saved by street sense?
            if (!dodgelawroll)
                dodgelawroll = getComponent<CreatureBase>().Skills["STREET_SENSE"].check(Difficulty.AVERAGE);

            if (!dodgelawroll && (int)drugLaw <= 0) // Busted!
            {
                string arrestMessage = getComponent<CreatureInfo>().getName() + " is accosted by police while selling brownies.";
                MasterController.GetMC().addAction(() =>
                {
                    MasterController.GetMC().addMessage(arrestMessage);
                    ChaseScene scene = new ChaseScene();
                    scene.startActivityFootChase(5, LocationDef.EnemyType.POLICE, "DRUGARREST", owner, arrestMessage);
                    MasterController.GetMC().doNextAction();
                }, "start new chase: activity arrest");
                getComponent<CriminalRecord>().addCrime(Constants.CRIME_BROWNIES);
            }

            int income = getComponent<CreatureBase>().Skills["PERSUASION"].roll() +
                         getComponent<CreatureBase>().Skills["BUSINESS"].roll() +
                         getComponent<CreatureBase>().Skills["STREET_SENSE"].roll();

            // more money when more illegal
            if (drugLaw == Alignment.ARCHCONSERVATIVE) income *= 4;
            if (drugLaw == Alignment.CONSERVATIVE) income *= 2;
            if (drugLaw == Alignment.LIBERAL) income /= 4;
            if (drugLaw == Alignment.ELITE_LIBERAL) income /= 8;

            MasterController.lcs.changeFunds(income);

            // Make the sale
            getComponent<CreatureBase>().Skills["PERSUASION"].addExperience(Math.Max(4 - getComponent<CreatureBase>().Skills["PERSUASION"].level, 1));
            // Know the streets
            getComponent<CreatureBase>().Skills["STREET_SENSE"].addExperience(Math.Max(7 - getComponent<CreatureBase>().Skills["STREET_SENSE"].level, 3));
            // Manage your money
            getComponent<CreatureBase>().Skills["BUSINESS"].addExperience(Math.Max(10 - getComponent<CreatureBase>().Skills["BUSINESS"].level, 3));
        }

        private void activityProstitution()
        {
            MasterController mc = MasterController.GetMC();

            //Do business avg once every 3 days
            if (mc.LCSRandom(3) != 0) return;

            int money = 0;
            bool caught = false;

            int performance = getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].roll();
            if (performance > (int)Difficulty.HEROIC)
                money = mc.LCSRandom(201) + 200;
            else
                money = mc.LCSRandom(10 * performance) + 10 * performance;

            //Street sense check to avoid dealing with slimy people that lower juice

            if(mc.LCSRandom(3) == 0 && !getComponent<CreatureBase>().Skills[Constants.SKILL_STREET_SENSE].check(Difficulty.AVERAGE))
                getComponent<CreatureBase>().juiceMe(mc.LCSRandom(3) == 0 ? -1 : 0, -20);

            getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].addExperience(Math.Max(10 - getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].level, 0));
            getComponent<CreatureBase>().Skills[Constants.SKILL_STREET_SENSE].addExperience(Math.Max(10 - getComponent<CreatureBase>().Skills[Constants.SKILL_STREET_SENSE].level, 0));

            //Police sting
            if(mc.LCSRandom(50) == 0)
            {
                if (!getComponent<CreatureBase>().Skills[Constants.SKILL_STREET_SENSE].check(Difficulty.AVERAGE))
                {
                    caught = true;
                    mc.addMessage(getComponent<CreatureInfo>().getName() + " has been arrested in a prostitution sting.", true);
                    getComponent<CreatureBase>().juiceMe(-7, -30);
                    getComponent<CriminalRecord>().addCrime(Constants.CRIME_PROSTITUTION);
                    getComponent<CriminalRecord>().arrest();
                }
                else
                {
                    mc.addMessage(getComponent<CreatureInfo>().getName() + " was nearly caught in a prostitution sting.", true);
                    getComponent<CreatureBase>().juiceMe(5, 0);
                }
            }

            if (!caught)
            {
                getComponent<CreatureBase>().Skills[Constants.SKILL_STREET_SENSE].addExperience(Math.Max(5 - getComponent<CreatureBase>().Skills[Constants.SKILL_STREET_SENSE].level, 0));
                MasterController.lcs.changeFunds(money);
            }
        }

        private void activityCCFraud()
        {
            MasterController mc = MasterController.GetMC();

            int hackSkill = getComponent<CreatureBase>().Skills[Constants.SKILL_COMPUTERS].roll();
            getComponent<CreatureBase>().Skills[Constants.SKILL_COMPUTERS].addExperience(2);
            Difficulty diff = Difficulty.CHALLENGING;

            if((int)diff <= hackSkill)
            {
                int fundgain = mc.LCSRandom(100) + 1;
                while((int)diff < hackSkill)
                {
                    fundgain += mc.LCSRandom(50) + 1;
                    diff += 2;
                }                

                MasterController.lcs.changeFunds(fundgain);

                if (fundgain / 25 > mc.LCSRandom(hackSkill + 1))
                    getComponent<CriminalRecord>().addCrime(Constants.CRIME_CCFRAUD);
            }
        }

        private void activityLearn()
        {
            MasterController mc = MasterController.GetMC();

            if (getComponent<CreatureBase>().Skills[dailyActivity.subType].level >= getComponent<CreatureBase>().Skills[dailyActivity.subType].associatedAttribute.getModifiedValue())
            {                                
                mc.addMessage(getComponent<CreatureInfo>().getName() + " has learned all " + getComponent<CreatureInfo>().heShe().ToLower() + " can about " + GameData.getData().skillList[dailyActivity.subType].name + ".");
                dailyActivity = new Activity("NONE", null, null);
                return;
            }

            if (MasterController.lcs.Money < 60) return;

            MasterController.lcs.changeFunds(-60);

            int effectiveness = Math.Max(20 / (getComponent<CreatureBase>().Skills[dailyActivity.subType].level + 1),1);

            getComponent<CreatureBase>().Skills[dailyActivity.subType].addExperience(effectiveness);
        }

        private void activityRepair(bool selfOnly = false)
        {
            Entity armor = null;
            Entity liberalWearing = null;

            //Repair own clothing first, then squad members, then base inventory
            if (getComponent<Inventory>().getArmor().getComponent<Armor>().bloody || getComponent<Inventory>().getArmor().getComponent<Armor>().damaged)
            {
                armor = getComponent<Inventory>().getArmor();
                liberalWearing = owner;
            }
            else if (squad != null && !selfOnly)
            {
                foreach (Entity e in squad)
                {
                    if (e.getComponent<Inventory>().getArmor().getComponent<Armor>().bloody || e.getComponent<Inventory>().getArmor().getComponent<Armor>().damaged)
                    {
                        armor = e.getComponent<Inventory>().getArmor();
                        liberalWearing = e;
                        break;
                    }
                }
            }

            if (selfOnly && armor == null) return;

            if (homeBase.hasComponent<SafeHouse>() && liberalWearing == null)
            {
                Dictionary<Entity, int> difficulties = new Dictionary<Entity, int>();
                List<Entity> sortedList = new List<Entity>();
                foreach (Entity a in homeBase.getComponent<SafeHouse>().getInventory())
                {
                    if (a.hasComponent<Armor>() && (a.getComponent<Armor>().bloody || a.getComponent<Armor>().damaged))
                    {
                        difficulties.Add(a, a.getComponent<Armor>().getMakeDifficulty() + 3 - getComponent<CreatureBase>().Skills[Constants.SKILL_TAILORING].level);
                        sortedList.Add(a);
                    }
                }

                sortedList.Sort((Entity a, Entity b) => { return difficulties[a] - difficulties[b]; });

                if (sortedList.Count > 0) armor = sortedList[0];
            }

            if(armor == null)
            {
                getComponent<CreatureBase>().Skills[Constants.SKILL_TAILORING].addExperience(1);
                string message = "";
                switch (MasterController.GetMC().LCSRandom(4))
                {
                    case 0: message = " tidies up the safehouse."; break;
                    case 1: message = " reorganizes the armor closet."; break;
                    case 2: message = " cleans the kitchen."; break;
                    case 3: message = " peruses some sewing magazines."; break;
                }

                MasterController.GetMC().addMessage(getComponent<CreatureInfo>().getName() + message);
            }
            else
            {
                bool repairfailed = false;
                bool qualityReduction = MasterController.GetMC().LCSRandom(10) == 0;
                string resultText = getComponent<CreatureInfo>().getName();

                if (armor.getComponent<Armor>().damaged)
                {
                    int difficulty = armor.getComponent<Armor>().getMakeDifficulty() + 3 - getComponent<CreatureBase>().Skills[Constants.SKILL_TAILORING].level;
                    difficulty >>= (armor.getComponent<Armor>().quality - 1);
                    getComponent<CreatureBase>().Skills[Constants.SKILL_TAILORING].addExperience(difficulty/2 + 1);
                    if (MasterController.GetMC().LCSRandom(1 + difficulty / 2) != 0) repairfailed = true;
                }
                if (repairfailed) qualityReduction = false;

                bool armorDestroyed = false;
                if (qualityReduction)
                {
                    armor.getComponent<Armor>().decreaseQuality(1);
                    if (armor.getComponent<Armor>().quality > armor.getComponent<Armor>().getQualityLevels()) armorDestroyed = true;
                }

                if (armorDestroyed) resultText += " disposes of ";
                else if (armor.getComponent<Armor>().bloody && (repairfailed || !armor.getComponent<Armor>().damaged)) resultText += " cleans ";
                else if (repairfailed) resultText += " is working to repair ";
                else
                {
                    if (!qualityReduction)
                        resultText += " repairs ";
                    else
                        resultText += " repairs what little can be fixed of ";
                }

                if (liberalWearing == owner) resultText += getComponent<CreatureInfo>().hisHer().ToLower() + " ";
                resultText += armor.getComponent<ItemBase>().getName();
                armor.getComponent<Armor>().bloody = false;
                if (!repairfailed) armor.getComponent<Armor>().damaged = false;

                if (armorDestroyed)
                {
                    if(liberalWearing != null)
                    {
                        liberalWearing.getComponent<Inventory>().dropArmor();
                    }

                    armor.getComponent<ItemBase>().destroyItem();
                }

                MasterController.GetMC().addMessage(resultText);
            }
        }

        private void activityClinic()
        {
            hospitalize(getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("HOSPITAL_CLINIC"));
        }

        public void hospitalize(Entity location)
        {
            int time = getComponent<Body>().getClinicTime();

            if (time > 0)
            {
                status = Status.HOSPITAL;

                if (squad != null) squad.Remove(owner);

                getComponent<Body>().HospitalTime = time;
                getComponent<CreatureBase>().Location = location;

                string message = getComponent<CreatureInfo>().getName() + " will be at ";
                message += getComponent<CreatureBase>().Location.getComponent<SiteBase>().getCurrentName();
                message += " for " + time + (time > 1 ? " months" : " month") + ".";

                MasterController.GetMC().addMessage(message, true);
            }

            dailyActivity = new Activity("NONE", null, null);
        }

        private void activityDisposeBodies()
        {
            MasterController mc = MasterController.GetMC();

            //Someone else may have finished dumping bodies before your turn came around
            if(homeBase.getComponent<SafeHouse>().getBodies().Count == 0)
            {
                setActivity("NONE");
                return;
            }

            //With a car you can load up the trunk with as many bodies as you want
            if (getComponent<Inventory>().vehicle != null)
            {
                foreach(Entity body in homeBase.getComponent<SafeHouse>().getBodies())
                {
                    bool arrestAttempt = false;

                    if (!getComponent<CreatureBase>().Skills[Constants.SKILL_STREET_SENSE].check(Difficulty.EASY))
                    {
                        burialArrest(body);
                        arrestAttempt = true;
                    }

                    //Whether they get caught or not, the body is still removed
                    body.depersist();
                    body.getComponent<CreatureBase>().Location = null;

                    //Don't bury any more bodies if they got caught
                    if (arrestAttempt) break;
                }
            }
            //On foot, things get a bit... tricky
            else
            {
                Entity body = homeBase.getComponent<SafeHouse>().getBodies()[mc.LCSRandom(homeBase.getComponent<SafeHouse>().getBodies().Count)];

                //Need both a trickier street sense check because of generally higher visibility, and a disguise check to Weekend at Bernies the corpse across town on the bus
                if (!(getComponent<CreatureBase>().Skills[Constants.SKILL_STREET_SENSE].check(Difficulty.AVERAGE) && 
                    getComponent<CreatureBase>().Skills[Constants.SKILL_DISGUISE].check(Difficulty.AVERAGE)))
                {
                    burialArrest(body);
                }

                //On the plus side, at least you learn something from the experience.
                getComponent<CreatureBase>().Skills[Constants.SKILL_DISGUISE].addExperience(20);
                getComponent<CreatureBase>().Skills[Constants.SKILL_STREET_SENSE].addExperience(20);

                //Whether they get caught or not, the body is still removed
                body.depersist();
                body.getComponent<CreatureBase>().Location = null;
            }
        }

        private void burialArrest(Entity body)
        {
            MasterController mc = MasterController.GetMC();

            getComponent<CriminalRecord>().addCrime(Constants.CRIME_BURIAL);

            string arrestMessage = getComponent<CreatureInfo>().getName() + " is accosted by police while burying " + body.getComponent<CreatureInfo>().getName() + "'s body.";

            int corpseAge = mc.currentDate.Subtract(body.getComponent<Body>().deathDate).Days;

            //Oh shit was this someone reported missing? Are they still identifiable?
            if ((body.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.KIDNAPPED) != 0 && mc.LCSRandom(25) + 3 > corpseAge)
            {
                arrestMessage += "\nThe police recognize " + body.getComponent<CreatureInfo>().getName() + " from the missing persons report!";
                getComponent<CriminalRecord>().addCrime(Constants.CRIME_KIDNAPPING);
                getComponent<CriminalRecord>().addCrime(Constants.CRIME_MURDER);
            }

            MasterController.GetMC().addAction(() =>
            {
                MasterController.GetMC().addMessage(arrestMessage);
                ChaseScene scene = new ChaseScene();
                scene.startActivityFootChase(5, LocationDef.EnemyType.POLICE, "BURIALARREST", owner, arrestMessage);
                MasterController.GetMC().doNextAction();
            }, "start new chase: activity arrest");
        }

        private void activitySleeperSteal()
        {
            //Can't steal from their location
            if (!homeBase.hasComponent<TroubleSpot>())
            {
                setActivity("SLEEPER_NONE");
                return;
            }

            if (MasterController.GetMC().LCSRandom(100) > infiltration)
            {
                getComponent<CreatureBase>().juiceMe(-1);

                if (getComponent<CreatureBase>().Juice < -2)
                {
                    MasterController.GetMC().addMessage("Sleeper " + getComponent<CreatureInfo>().getName() + " has been arrested while stealing things.", true);
                    homeBase = getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER");
                    status = Status.ACTIVE;
                    setActivity("NONE");
                    getComponent<CriminalRecord>().addCrime(Constants.CRIME_THEFT);
                    getComponent<CriminalRecord>().arrest();
                }
                return;
            }

            getComponent<CreatureBase>().juiceMe(10, 100);

            int numberOfItems = MasterController.GetMC().LCSRandom(infiltration / 10) + 1;

            for(int i = 0; i < numberOfItems; i++)
            {
                Entity stolenItem = homeBase.getComponent<TroubleSpot>().getLootItem();
                getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER").getComponent<SafeHouse>().addItemToInventory(stolenItem);
            }

            MasterController.GetMC().addMessage("Sleeper " + getComponent<CreatureInfo>().getName() + " has dropped a package off at the Homeless Shelter.");
        }

        private void activitySleeperRecruit()
        {
            //If there's no trouble spot definition for their work location, they can't recruit more sleepers.
            if (!homeBase.hasComponent<TroubleSpot>())
            {
                setActivity("SLEEPER_NONE");
                return;
            }

            Dictionary<string, int> encounterList = new Dictionary<string, int>();
            foreach (LocationDef.EncounterDef enc in homeBase.getComponent<TroubleSpot>().getEncounters())
            {
                if (MasterController.GetMC().testCondition(enc.conditions, homeBase.getComponent<TroubleSpot>()))
                {
                    if (!encounterList.ContainsKey(enc.creatureType.type))
                        encounterList[enc.creatureType.type] = enc.weight;
                    else
                        encounterList[enc.creatureType.type] += enc.weight;
                }
            }

            List<Entity> recruitOptions = homeBase.getComponent<TroubleSpot>().generateEncounter(0);
            foreach (Entity creature in recruitOptions)
            {
                if (creature == null) continue;

                // Dogs aren't that useful as sleeper agents.
                if (creature.type.Equals("GUARDDOG")) continue;

                //Liberals are easier to recruit than non-liberals
                if (creature.getComponent<CreatureInfo>().alignment != Alignment.LIBERAL &&
                    MasterController.GetMC().LCSRandom(5) != 0)
                    continue;

                //If they can work here, they do work here. Otherwise, they just work wherever they happened to pick at random, but these are harder to recruit
                if (GameData.getData().creatureDefList[creature.def].work_location.Contains(homeBase.def))
                    creature.getComponent<CreatureInfo>().workLocation = homeBase;
                else if (MasterController.GetMC().LCSRandom(5) != 0)
                    continue;
                else if (creature.getComponent<CreatureInfo>().workLocation.hasComponent<TroubleSpot>())
                    creature.getComponent<CreatureInfo>().workLocation.getComponent<TroubleSpot>().mapped = true;
                
                recruit(creature);
                creature.getComponent<Liberal>().sleeperize();
                if (creature.getComponent<Liberal>().infiltration > infiltration)
                    creature.getComponent<Liberal>().infiltration = infiltration;

                MasterController.GetMC().addMessage("Sleeper " + getComponent<CreatureInfo>().getName() + " has recruited " + creature.getComponent<CreatureInfo>().getName() + " into " + getComponent<CreatureInfo>().hisHer().ToLower() + " network.", true);

                if (getNormalSubordinateCount() >= getSubordinateLimit()) setActivity("SLEEPER_NONE");
                break;
            }
        }

        private void activitySleeperSnoop()
        {
            if (MasterController.GetMC().LCSRandom(100) > infiltration)
            {
                getComponent<CreatureBase>().juiceMe(-1);

                if (getComponent<CreatureBase>().Juice < -2)
                {
                    MasterController.GetMC().addMessage("Sleeper " + getComponent<CreatureInfo>().getName() + " has been caught snooping around.\nThe Liberal is now jobless and homeless...", true);
                    homeBase = getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER");
                    status = Status.ACTIVE;
                    setActivity("NONE");
                    goHome();
                }
                return;
            }

            if (getComponent<CreatureBase>().Juice < 100)
            {
                getComponent<CreatureBase>().juiceMe(10, 100);
            }

            if (homeBase.hasComponent<TroubleSpot>())
                homeBase.getComponent<TroubleSpot>().mapped = true;

            if(GameData.getData().creatureDefList[owner.def].sleeper != null &&
                GameData.getData().creatureDefList[owner.def].sleeper.snoopLoot != null &&
                !getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER").getComponent<SafeHouse>().underSiege)
            {
                if(GameData.getData().creatureDefList[owner.def].sleeper.snoopLoot.type == "LOOT_CCS_BACKERLIST" &&
                    MasterController.ccs.exposure < ConservativeCrimeSquad.Exposure.GOT_DATA)
                {
                    Entity loot = Factories.ItemFactory.create(GameData.getData().creatureDefList[owner.def].sleeper.snoopLoot.type);

                    MasterController.GetMC().addMessage("Sleeper " + getComponent<CreatureInfo>().getName() + " has leaked a list of the CCS's government backers. The disk is stashed at the Homeless Shelter.");
                    getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER").getComponent<SafeHouse>().addItemToInventory(loot);
                    MasterController.ccs.exposure = ConservativeCrimeSquad.Exposure.GOT_DATA;
                }
                if(MasterController.GetMC().LCSRandom((int)MasterController.government.laws[GameData.getData().creatureDefList[owner.def].sleeper.snoopLootLaw.type].alignment + 3) == 0)
                {
                    Entity loot = Factories.ItemFactory.create(GameData.getData().creatureDefList[owner.def].sleeper.snoopLoot.type);

                    MasterController.GetMC().addMessage("Sleeper " + getComponent<CreatureInfo>().getName() + " has leaked " + loot.getComponent<ItemBase>().getName() + ". They are stashed at the Homeless Shelter.");
                    getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER").getComponent<SafeHouse>().addItemToInventory(loot);
                }
            }
        }

        private void activitySleeperEmbezzle()
        {
            if (MasterController.GetMC().LCSRandom(100) > infiltration)
            {
                getComponent<CreatureBase>().juiceMe(-1);

                if (getComponent<CreatureBase>().Juice < -2)
                {
                    MasterController.GetMC().addMessage("Sleeper " + getComponent<CreatureInfo>().getName() + " has been arrested embezzling funds.", true);
                    homeBase = getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER");
                    status = Status.ACTIVE;
                    setActivity("NONE");
                    getComponent<CriminalRecord>().addCrime(Constants.CRIME_COMMERCE);
                    getComponent<CriminalRecord>().arrest();
                }
                return;
            }

            if (getComponent<CreatureBase>().Juice < 100)
            {
                getComponent<CreatureBase>().juiceMe(10, 100);
            }

            int income = (int)(GameData.getData().creatureDefList[owner.def].sleeper.embezzleMultiplier * (infiltration / 100d));
            MasterController.lcs.changeFunds(income);
        }

        private void activitySleeperJoin()
        {
            MasterController.GetMC().addMessage("Sleeper " + getComponent<CreatureInfo>().getName() + " has joined the active LCS.", true);
            homeBase = getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER");
            status = Status.ACTIVE;
            setActivity("NONE");
            goHome();
        }

        private void activityTeach()
        {
            int cost = 0;
            List<string> skillList = new List<string>();

            switch (dailyActivity.type)
            {
                case "TEACH_ACTIVISM":
                    cost = 2;
                    foreach(SkillDef skill in GameData.getData().skillList.Values)
                    {
                        if (skill.category == "activism")
                            skillList.Add(skill.type);
                    }
                    break;
                case "TEACH_INFILTRATION":
                    cost = 6;
                    foreach (SkillDef skill in GameData.getData().skillList.Values)
                    {
                        if (skill.category == "infiltration")
                            skillList.Add(skill.type);
                    }
                    break;
                case "TEACH_WARFARE":
                    cost = 10;
                    foreach (SkillDef skill in GameData.getData().skillList.Values)
                    {
                        if (skill.category == "combat")
                            skillList.Add(skill.type);
                    }
                    break;
            }

            List<Entity> students = new List<Entity>();

            foreach(Entity e in homeBase.getComponent<SafeHouse>().getBasedLiberals())
            {
                if (e == owner) continue;

                foreach(string skill in skillList)
                {
                    if (e.getComponent<CreatureBase>().Skills[skill].level < getComponent<CreatureBase>().Skills[skill].level - 1 &&
                       e.getComponent<CreatureBase>().Skills[skill].level < getComponent<CreatureBase>().Skills[Constants.SKILL_TEACHING].level + 2 &&
                       e.getComponent<CreatureBase>().Skills[skill].level < e.getComponent<CreatureBase>().Skills[skill].associatedAttribute.getModifiedValue())
                        students.Add(e);
                }
            }

            if (students.Count == 0) return;

            //Check if they can afford to teach
            if (MasterController.lcs.Money < Math.Min(10, students.Count) * cost)
                return;

            foreach(Entity student in students)
            {
                foreach (string skill in skillList)
                {
                    if (student.getComponent<CreatureBase>().Skills[skill].level < getComponent<CreatureBase>().Skills[skill].level - 1 &&
                       student.getComponent<CreatureBase>().Skills[skill].level < getComponent<CreatureBase>().Skills[Constants.SKILL_TEACHING].level + 2 &&
                       student.getComponent<CreatureBase>().Skills[skill].level < student.getComponent<CreatureBase>().Skills[skill].associatedAttribute.getModifiedValue())
                    {
                        int teach = getComponent<CreatureBase>().Skills[skill].level +
                        getComponent<CreatureBase>().Skills[Constants.SKILL_TEACHING].level -
                        student.getComponent<CreatureBase>().Skills[skill].level;

                        if (students.Count > 10)
                        {
                            //62.5% speed at 20 students
                            teach = ((teach * 30 / students.Count) + teach) / 4;
                        }

                        if (teach < 1) teach = 1;
                        if (teach > 10) teach = 10;

                        student.getComponent<CreatureBase>().Skills[skill].addExperience(teach);
                    }
                }
            }

            MasterController.lcs.changeFunds(-(cost * Math.Min(students.Count, 10)));
            getComponent<CreatureBase>().Skills[Constants.SKILL_TEACHING].addExperience(Math.Min(students.Count, 10));
        }

        private void activityGetWheelchair()
        {
            MasterController mc = MasterController.GetMC();

            if(mc.LCSRandom(2) == 0)
            {
                mc.addMessage(getComponent<CreatureInfo>().getName() + " has procured a wheelchair.");
                getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.WHEELCHAIR;
                setActivity("NONE");
            }
            else
            {
                mc.addMessage(getComponent<CreatureInfo>().getName() + " was unable to get a wheelchair. Maybe tomorrow...");
            }
        }

        private void activityMakeClothing()
        {
            ItemDef.ArmorDef def = (ItemDef.ArmorDef) GameData.getData().itemList[dailyActivity.subType].components["armor"];
            MasterController mc = MasterController.GetMC();
            int halfCost = def.make_price / 2;
            if (halfCost <= 0) halfCost = 1;
            Entity cloth = null;
            string name = mc.isFuture() ? GameData.getData().itemList[dailyActivity.subType].nameFuture : GameData.getData().itemList[dailyActivity.subType].name;

            foreach (Entity e in homeBase.getComponent<SafeHouse>().getInventory())
            {
                if(e.hasComponent<Loot>() && (e.getComponent<Loot>().getFlags() & ItemDef.LootFlags.CLOTH) != 0)
                {
                    cloth = e;
                    break;
                }
            }

            if((MasterController.lcs.Money < def.make_price && cloth == null) || 
                MasterController.lcs.Money < halfCost)
            {                
                mc.addMessage(getComponent<CreatureInfo>().getName() + " cannot afford material to make " + name + ".");
                return;
            }
            else
            {
                int dif = 3 + def.make_difficulty - getComponent<CreatureBase>().Skills[Constants.SKILL_TAILORING].level;
                if (dif < 0) dif = 0;

                if (cloth != null)
                {
                    cloth.getComponent<ItemBase>().destroyItem();
                    MasterController.lcs.changeFunds(-halfCost);
                }
                else
                {
                    MasterController.lcs.changeFunds(-def.make_price);
                }

                getComponent<CreatureBase>().Skills[Constants.SKILL_TAILORING].addExperience(dif * 2 + 1);
                int quality = 1;
                while (mc.LCSRandom(10) < dif && quality <= def.qualitylevels)
                    quality++;

                if(quality <= def.qualitylevels)
                {
                    mc.addMessage(getComponent<CreatureInfo>().getName() + " has made a " + MasterController.ordinal(quality) + "-Rate " + name + ".");
                    Entity newArmor = Factories.ItemFactory.create(dailyActivity.subType);
                    newArmor.getComponent<Armor>().quality = quality;
                    homeBase.getComponent<SafeHouse>().addItemToInventory(newArmor);
                }
                else
                {
                    mc.addMessage(getComponent<CreatureInfo>().getName() + " has wasted the materials for a " + name + ".");
                }
            }
        }

        private void activityStealVehicle()
        {
            StealCarScene scene = new StealCarScene();
            scene.startScene(owner, dailyActivity.subType);
        }
    }
}
