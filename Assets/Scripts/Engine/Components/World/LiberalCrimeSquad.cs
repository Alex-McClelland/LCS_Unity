using System;
using System.Collections.Generic;
using System.Xml;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.Item;
using LCS.Engine.Components.Location;
using LCS.Engine.Scenes;
using LCS.Engine.Data;

namespace LCS.Engine.Components.World
{
    public class LiberalCrimeSquad : Component
    {
        [SimpleSave]
        public int Money;
        [SimpleSave]
        public string slogan;
        [SimpleSave]
        public Entity founder;
        [SimpleSave]
        public bool offendedAMRadio;
        [SimpleSave]
        public bool offendedCableNews;
        [SimpleSave]
        public bool offendedFiremen;
        [SimpleSave]
        public bool offendedCIA;
        [SimpleSave]
        public bool offendedCorps;
        
        public List<Squad> squads { get; set; }
        public Squad activeSquad { get; set; }

        public List<Entity> protestingLiberals { get; set; }
        public List<Entity> hackingLiberals { get; set; }
        public List<Memorial> liberalMartyrs { get; set; }

        public List<FinanceMonth> financials = new List<FinanceMonth>();

        private const int FINANCIAL_COUNT = 12;

        public LiberalCrimeSquad()
        {
            squads = new List<Squad>();
            Money = 0;

            for (int i = 0; i < FINANCIAL_COUNT; i++) financials.Add(new FinanceMonth());

            protestingLiberals = new List<Entity>();
            hackingLiberals = new List<Entity>();
            liberalMartyrs = new List<Memorial>();
            slogan = "We need a slogan!";
        }

        public override void subscribe()
        {
            base.subscribe();
            MasterController.GetMC().nextMonth += doMonthly;
        }

        public override void unsubscribe()
        {
            base.unsubscribe();
            MasterController.GetMC().nextMonth -= doMonthly;
        }

        private void doMonthly(object sender, EventArgs args)
        {
            for(int i = FINANCIAL_COUNT - 1; i > 0; i--)
            {
                financials[i] = new FinanceMonth(financials[i - 1]);
            }

            financials[0] = new FinanceMonth();
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("LiberalCrimeSquad");
                entityNode.AppendChild(saveNode);
            }

            saveSimpleFields();
            saveField(squads.IndexOf(activeSquad), "activeSquad", saveNode);

            if (saveNode.SelectSingleNode("squads") != null)
                saveNode.RemoveChild(saveNode.SelectSingleNode("squads"));

            if (saveNode.SelectSingleNode("martyrs") != null)
                saveNode.RemoveChild(saveNode.SelectSingleNode("martyrs"));

            if (saveNode.SelectSingleNode("financials") != null)
                saveNode.RemoveChild(saveNode.SelectSingleNode("financials"));

            XmlNode squadNode = saveNode.OwnerDocument.CreateElement("squads");
            saveNode.AppendChild(squadNode);

            foreach (Squad s in squads)
                s.save(squadNode);

            XmlNode martyrNode = saveNode.OwnerDocument.CreateElement("martyrs");
            saveNode.AppendChild(martyrNode);

            foreach (Memorial m in liberalMartyrs)
                m.save(martyrNode);

            XmlNode financeNode = saveNode.OwnerDocument.CreateElement("financials");
            saveNode.AppendChild(financeNode);

            foreach (FinanceMonth f in financials)
                f.save(financeNode);
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);

            foreach(XmlNode node in componentData.SelectSingleNode("martyrs").ChildNodes)
            {
                Memorial m = new Memorial();
                m.load(node, entityList);
                liberalMartyrs.Add(m);
            }

            if (componentData.SelectSingleNode("financials") != null)
            {
                financials.Clear();

                foreach (XmlNode node in componentData.SelectSingleNode("financials").ChildNodes)
                {
                    FinanceMonth f = new FinanceMonth();
                    f.load(node);
                    financials.Add(f);
                }

                while (financials.Count < FINANCIAL_COUNT)
                    financials.Add(new FinanceMonth());
            }
        }

        public void loadSquads(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            foreach (XmlNode node in componentData.SelectSingleNode("squads").ChildNodes)
            {
                Squad squad = new Squad(node.SelectSingleNode("name").InnerText);
                if (node.SelectSingleNode("homeBase").InnerText != "null")
                    squad.homeBase = entityList[int.Parse(node.SelectSingleNode("homeBase").InnerText)];
                if (node.SelectSingleNode("target").InnerText != "null")
                    squad.target = entityList[int.Parse(node.SelectSingleNode("target").InnerText)];
                squad.travelAction = (Squad.TravelAction)Enum.Parse(typeof(Squad.TravelAction), node.SelectSingleNode("travelAction").InnerText);
                foreach (XmlNode innerNode in node.SelectSingleNode("members").ChildNodes)
                {
                    squad.Add(entityList[int.Parse(innerNode.InnerText)]);
                }

                squads.Add(squad);
            }

            if (int.Parse(componentData.SelectSingleNode("activeSquad").InnerText) != -1)
                activeSquad = squads[int.Parse(componentData.SelectSingleNode("activeSquad").InnerText)];
            else
                activeSquad = null;
        }

        public Squad newSquad(string name)
        {
            Squad newSquad = new Squad(name);
            squads.Add(newSquad);

            return newSquad;
        }

        public List<Entity> getAllMembers()
        {
            List<Entity> memberList = new List<Entity>();

            populateMemberList(founder, memberList);

            return memberList;
        }

        public List<Entity> getAllSleepers()
        {
            List<Entity> sleeperList = getAllMembers();
            List<Entity> sleeperListTemp = new List<Entity>(sleeperList);

            foreach(Entity e in sleeperListTemp)
            {
                if (e.getComponent<Liberal>().status != Liberal.Status.SLEEPER)
                    sleeperList.Remove(e);
            }

            return sleeperList;
        }

        public bool checkForActiveMembers()
        {
            bool active = false;

            foreach(Entity e in getAllMembers())
            {
                if(e.getComponent<Liberal>().status == Liberal.Status.ACTIVE || e.getComponent<Liberal>().status == Liberal.Status.SLEEPER)
                {
                    active = true;
                }
            }

            return active;
        }

        public bool changeFunds(int amt)
        {
            if (amt > 0)
            {
                Money += amt;
                financials[0].income += amt;
                MasterController.highscore.moneyTaxed += amt;
                return true;
            }
            else
            {
                //Cannot afford
                if(Money < amt) return false;

                Money += amt;
                financials[0].expenses += -amt;
                MasterController.highscore.moneySpent += -amt;
                return true;
            }
        }

        public void nextSquad()
        {
            if (squads.Count == 0) return;

            if (activeSquad == null && squads.Count > 0)
            {
                activeSquad = squads[0];
                return;
            }

            int squadNum = squads.IndexOf(activeSquad);

            if (squadNum + 1 >= squads.Count)
            {
                activeSquad = squads[0];
            }
            else
            {
                activeSquad = squads[squadNum + 1];
            }
        }

        public void disband()
        {
            MasterController.GetMC().uiController.closeUI();
            MasterController.GetMC().uiController.fastAdvance.show();
            MasterController.GetMC().canSeeThings = false;
            //When disbanding, all sleepers will switch to advocating liberalism to help push the country over the edge
            foreach (Entity e in getAllSleepers())
            {
                e.getComponent<Liberal>().setActivity("SLEEPER_ADVOCATE");
            }
            foreach (Entity e in getAllMembers())
            {
                if (e.getComponent<Liberal>().status == Liberal.Status.ACTIVE ||
                    e.getComponent<Liberal>().status == Liberal.Status.AWAY)
                {
                    e.getComponent<Liberal>().status = Liberal.Status.AWAY;
                    e.getComponent<Liberal>().awayTime = -1;
                    e.getComponent<Liberal>().setActivity("NONE");
                    foreach (Entity p in e.getComponent<Liberal>().plannedDates)
                    {
                        p.depersist();
                    }
                    e.getComponent<Liberal>().plannedDates.Clear();
                    foreach (Entity p in e.getComponent<Liberal>().plannedMeetings)
                    {
                        p.depersist();
                    }
                    e.getComponent<Liberal>().plannedMeetings.Clear();
                }
            }
        }

        public void reform()
        {
            MasterController.GetMC().canSeeThings = true;
            foreach (Entity e in getAllMembers())
            {
                //Nuke any members who aren't dedicated enough to the cause to return, as well as cut off contact for any of their subordinates
                if (e.getComponent<CreatureBase>().Juice < 100 && e.getComponent<Liberal>().leader != null)
                {
                    e.getComponent<Liberal>().leaveLCS(true);
                }

                if (e.getComponent<Liberal>().status == Liberal.Status.AWAY)
                {
                    e.getComponent<Liberal>().awayTime = MasterController.GetMC().LCSRandom(10) + 5;
                }
            }
            MasterController.GetMC().uiController.closeUI();
            MasterController.GetMC().uiController.baseMode.show();
        }

        public void activityLiberalDisobedience()
        {
            if (protestingLiberals.Count == 0) return;

            MasterController mc = MasterController.GetMC();

            string preamble;

            if (protestingLiberals.Count == 1) preamble = protestingLiberals[0].getComponent<CreatureInfo>().getName() + " has ";
            else preamble = "Your activists have ";

            int power = 0;

            foreach(Entity e in protestingLiberals)
            {
                power += e.getComponent<CreatureBase>().Skills["PERSUASION"].roll() + e.getComponent<CreatureBase>().Skills["STREET_SENSE"].roll();
            }

            int mod = 1;

            if (mc.LCSRandom(10) < power) mod++;
            if (mc.LCSRandom(20) < power) mod++;
            if (mc.LCSRandom(40) < power) mod++;
            if (mc.LCSRandom(60) < power) mod++;
            if (mc.LCSRandom(80) < power) mod++;
            if (mc.LCSRandom(100) < power) mod++;

            List<string> protestOptions = new List<string>();

            foreach(ViewDef view in GameData.getData().viewList.Values)
            {
                if(view.protestText != "")
                {
                    if(view.protestLaw == null || MasterController.government.laws[view.protestLaw.type].alignment < Alignment.ELITE_LIBERAL)
                    {
                        protestOptions.Add(view.type);
                    }
                }
            }

            int juice = 0;
            string crime = "";

            string protestSelection = protestOptions[mc.LCSRandom(protestOptions.Count)];

            if (protestingLiberals.Count > 1) mc.addMessage(preamble + GameData.getData().viewList[protestSelection].protestText);
            else mc.addMessage(preamble + GameData.getData().viewList[protestSelection].protestText);

            if (GameData.getData().viewList[protestSelection].protestCrime != null && 
                mc.testCondition(GameData.getData().viewList[protestSelection].protestCrime.condition))
                crime = GameData.getData().viewList[protestSelection].protestCrime.crime.type;

            MasterController.generalPublic.changePublicOpinion("LIBERALCRIMESQUAD", mod);
            //People don't like being assaulted - less positive opinion boost
            if(crime == "ASSAULT") MasterController.generalPublic.changePublicOpinion("LIBERALCRIMESQUADPOS", mod/2, 0, 70);
            else MasterController.generalPublic.changePublicOpinion("LIBERALCRIMESQUADPOS", mod, 0, 70);

            MasterController.generalPublic.PublicInterest[protestSelection] += mod;
            MasterController.generalPublic.BackgroundLiberalInfluence[protestSelection] += mod;
            juice = crime != "" ? 2 : 1;

            if(crime != "")
            {
                foreach(Entity e in protestingLiberals)
                {
                    if(mc.LCSRandom(30) == 0 && !e.getComponent<CreatureBase>().Skills["STREET_SENSE"].check(Difficulty.AVERAGE))
                    {
                        if(mc.LCSRandom(4) == 0)
                        {
                            string arrestMessage = e.getComponent<CreatureInfo>().getName() + " is accosted by police while causing trouble.";
                            MasterController.GetMC().addAction(() =>
                            {
                                MasterController.GetMC().addMessage(arrestMessage);
                                MasterController.GetMC().currentChaseScene = new ChaseScene();
                                MasterController.GetMC().currentChaseScene.startActivityFootChase(5, LocationDef.EnemyType.POLICE, "TROUBLEARREST", e, arrestMessage);
                                MasterController.GetMC().doNextAction();
                            }, "start new chase: activity arrest");
                            e.getComponent<CriminalRecord>().addCrime(crime);
                        }
                        else
                        {
                            bool wonFight = false;
                            string combatString = "";

                            combatString += e.getComponent<CreatureInfo>().getName() + " is cornered by a mob of angry rednecks.";

                            if (e.getComponent<Inventory>().isWeaponThreatening())
                            {
                                combatString += "\n<b>" + e.getComponent<CreatureInfo>().heShe() + " brandishes " + 
                                    e.getComponent<CreatureInfo>().hisHer().ToLower() + " " + 
                                    e.getComponent<Inventory>().weapon.getComponent<ItemBase>().getName() + "!</b>";
                                combatString += "\nThe mob scatters!";

                                wonFight = true;

                                e.getComponent<CreatureBase>().juiceMe(5, 20);
                            }
                            else
                            {
                                int libScore = 0;
                                int mobScore = 0;

                                int fightLength = mc.LCSRandom(5) + 2;

                                for (int count = 0; count <= fightLength; count++)
                                {
                                    if (e.getComponent<CreatureBase>().Skills["MARTIAL_ARTS"].roll() > mc.LCSRandom(6) + count)
                                    {
                                        combatString += "\n<color=cyan>" + e.getComponent<CreatureInfo>().getName();

                                        switch (mc.LCSRandom(8))
                                        {
                                            case 0: combatString += " breaks the arm of the nearest person!</color>"; break;
                                            case 1: combatString += " knees a guy in the balls!</color>"; break;
                                            case 2: combatString += " knocks one out with a fist to the face!</color>"; break;
                                            case 3: combatString += " bites some hick's ear off!</color>"; break;
                                            case 4: combatString += " smashes one of them in the jaw!</color>"; break;
                                            case 5: combatString += " shakes off a grab from behind!</color>"; break;
                                            case 6: combatString += " yells the slogan!</color>"; break;
                                            case 7: combatString += " knocks two of their heads together!</color>"; break;
                                        }

                                        //Early hits are less decisive but can add up so one loss at the end doesn't mean you lose the fight.
                                        libScore += count + 1;
                                    }
                                    else
                                    {
                                        combatString += "\n<color=yellow>" + e.getComponent<CreatureInfo>().getName();

                                        switch (mc.LCSRandom(8))
                                        {
                                            case 0: combatString += " is held down and kicked by three guys!</color>"; break;
                                            case 1: combatString += " gets pummeled!</color>"; break;
                                            case 2: combatString += " gets hit by a sharp rock!</color>"; break;
                                            case 3: combatString += " is thrown against the sidewalk!</color>"; break;
                                            case 4: combatString += " is bashed in the face with a shovel!</color>"; break;
                                            case 5: combatString += " is forced into a headlock!</color>"; break;
                                            case 6: combatString += " crumples under a flurry of blows!</color>"; break;
                                            case 7: combatString += " is hit in the chest with a pipe!</color>"; break;
                                        }

                                        mobScore += count + 1;

                                        count++; //Fight ends faster when you're losing
                                    }
                                }

                                //Libs lose on ties - mob > individual
                                if (libScore > mobScore)
                                {
                                    wonFight = true;

                                    combatString += "\n<color=lime>" + e.getComponent<CreatureInfo>().getName();

                                    switch (mc.LCSRandom(3)) {
                                        case 0:
                                            combatString += " beat the " + mc.swearFilter("shit", "tar") + " out of everyone who got close!</color>";
                                            break;
                                        case 1:
                                            combatString += " lets out a primal scream!</color>";
                                            break;
                                        case 2:
                                            combatString += " shouts \"Now who else want to " + mc.swearFilter("fuck", "mess") + " with Hollywood " + e.getComponent<CreatureInfo>().getName(true) + "?!\"</color>";
                                            break;
                                    }
                                    e.getComponent<CreatureBase>().juiceMe(30, 300);
                                    if (e.getComponent<Body>().Blood > 70) e.getComponent<Body>().Blood = 70;
                                }
                            }

                            if (!wonFight)
                            {
                                combatString += "\n<color=red>" + e.getComponent<CreatureInfo>().getName() + " is severely beaten before the mob is broken up.</color>";

                                e.getComponent<CreatureBase>().juiceMe(-10, -50);

                                if (e.getComponent<Body>().Blood > 10) e.getComponent<Body>().Blood = 10;
 
                                if (mc.LCSRandom(5) == 0)
                                {
                                    int preDamageCount;

                                    switch (mc.LCSRandom(10))
                                    {
                                        case 0:
                                            if (e.getComponent<Body>().damageOrgan("SPINE_LOWER"))
                                            {
                                                combatString += "\n<color=red>" + e.getComponent<CreatureInfo>().getName();
                                                combatString += "'s lower spine has been broken!</color>";
                                            }
                                            break;
                                        case 1:
                                            if (e.getComponent<Body>().damageOrgan("SPINE_UPPER"))
                                            {
                                                combatString += "\n<color=red>" + e.getComponent<CreatureInfo>().getName();
                                                combatString += "'s upper spine has been broken!</color>";
                                            }
                                            break;
                                        case 2:
                                            if (e.getComponent<Body>().damageOrgan("NECK"))
                                            {
                                                combatString += "\n<color=red>" + e.getComponent<CreatureInfo>().getName();
                                                combatString += "'s neck has been broken!</color>";
                                            }
                                            break;
                                        case 3:
                                            preDamageCount = e.getComponent<Body>().getOrganCount("TOOTH");

                                            if (e.getComponent<Body>().destroyOrgan("TOOTH"))
                                            {
                                                combatString += "\n<color=red>" + e.getComponent<CreatureInfo>().getName();

                                                if (preDamageCount > 1) combatString += "'s teeth have been smashed out on the curb.</color>";
                                                else combatString += "'s tooth has been pulled out with pliers!</color>";
                                            }
                                            break;
                                        default:
                                            preDamageCount = e.getComponent<Body>().getOrganCount("RIB");

                                            if (e.getComponent<Body>().damageOrgan("RIB"))
                                            {
                                                int ribMinus = preDamageCount - e.getComponent<Body>().getOrganCount("RIB");

                                                combatString += "\n<color=red>";
                                                if (ribMinus > 1)
                                                {
                                                    if (ribMinus == preDamageCount)
                                                        combatString += "All " + MasterController.NumberToWords(ribMinus).ToLower();
                                                    else
                                                        combatString += MasterController.NumberToWords(ribMinus);
                                                    combatString += " of " + e.getComponent<CreatureInfo>().getName() + "'s ribs are ";
                                                }
                                                else if(preDamageCount > 1)
                                                {
                                                    combatString += "One of " + e.getComponent<CreatureInfo>().getName() + "'s ribs is ";
                                                }
                                                else
                                                {
                                                    combatString += e.getComponent<CreatureInfo>().getName() + "'s last unbroken rib is ";
                                                }

                                                combatString += "broken!</color>";
                                            }
                                            break;
                                    }
                                }
                            }

                            mc.addMessage(combatString, true);
                        }
                    }
                }
            }

            foreach(Entity e in protestingLiberals)
            {
                e.getComponent<CreatureBase>().juiceMe(juice, 40);
            }
        }

        public void activityHacking()
        {
            if (hackingLiberals.Count == 0) return;

            MasterController mc = MasterController.GetMC();

            int hackSkill = 0;
            Entity bestHacker = null;
            //Get best hacker's skill roll.
            foreach (Entity e in hackingLiberals)
            {
                int roll = e.getComponent<CreatureBase>().Skills["COMPUTERS"].roll();
                e.getComponent<CreatureBase>().Skills["COMPUTERS"].addExperience(4);

                if(roll > hackSkill)
                {
                    hackSkill = roll;
                    bestHacker = e;
                }
            }

            string hackString = "";

            if((int) Difficulty.HEROIC <= hackSkill + hackingLiberals.Count - 1)
            {
                int juice = 0;
                string crime = "";
                Difficulty trackDif = Difficulty.AUTOMATIC;

                if (hackingLiberals.Count > 1)
                    hackString += "Your hackers have ";
                else
                    hackString += hackingLiberals[0].getComponent<CreatureInfo>().getName() + " has ";

                switch (mc.LCSRandom(11))
                {
                    case 0:
                        hackString += "pilfered files from a Corporate server.";

                        bestHacker.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().addItemToInventory(Factories.ItemFactory.create("LOOT_CORPFILES"));
                        trackDif = Difficulty.FORMIDABLE;
                        crime = "INFORMATION";
                        juice = 10;
                        break;
                    case 1:
                        hackString += "caused a scare by breaking into a CIA network.";

                        trackDif = Difficulty.SUPERHEROIC;
                        crime = "INFORMATION";
                        juice = 25;
                        MasterController.generalPublic.changePublicOpinion("INTELLIGENCE", 10, 0, 75);
                        break;
                    case 2:
                        hackString += "sabotaged a genetics research company's network.";

                        trackDif = Difficulty.FORMIDABLE;
                        crime = "INFORMATION";
                        juice = 10;
                        MasterController.generalPublic.changePublicOpinion("GENETICS", 2, 0, 75);
                        break;
                    case 3:
                        hackString += "intercepted internal media emails.";

                        switch (mc.LCSRandom(2)) {
                            case 0:
                                bestHacker.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().addItemToInventory(Factories.ItemFactory.create("LOOT_AMRADIOFILES"));
                                break;
                            case 1:
                                bestHacker.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().addItemToInventory(Factories.ItemFactory.create("LOOT_CABLENEWSFILES"));
                                break;
                        }
                        trackDif = Difficulty.FORMIDABLE;
                        crime = "INFORMATION";
                        juice = 10;
                        break;
                    case 4:
                        hackString += "broke into military networks leaving LCS slogans.";

                        trackDif = Difficulty.SUPERHEROIC;
                        crime = "INFORMATION";
                        juice = 10;
                        MasterController.generalPublic.changePublicOpinion("LIBERALCRIMESQUAD", 5, 0, 75);
                        break;
                    case 5:
                        hackString += "uncovered information on dangerous research.";

                        bestHacker.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().addItemToInventory(Factories.ItemFactory.create("LOOT_RESEARCHFILES"));
                        trackDif = Difficulty.FORMIDABLE;
                        crime = "INFORMATION";
                        juice = 10;
                        break;
                    case 6:
                        hackString += "discovered evidence of judicial corruption.";

                        bestHacker.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().addItemToInventory(Factories.ItemFactory.create("LOOT_JUDGEFILES"));
                        trackDif = Difficulty.FORMIDABLE;
                        crime = "INFORMATION";
                        juice = 10;
                        break;
                    case 7:
                        hackString += "subverted a Conservative family forum.";

                        trackDif = Difficulty.FORMIDABLE;
                        crime = "INFORMATION";
                        juice = 10;
                        MasterController.generalPublic.changePublicOpinion("GAY", 2, 0, 75);
                        MasterController.generalPublic.changePublicOpinion("WOMEN", 2, 0, 75);
                        break;
                    case 8:
                        hackString += "spread videos of racist police brutality.";

                        trackDif = Difficulty.FORMIDABLE;
                        crime = "INFORMATION";
                        juice = 10;
                        MasterController.generalPublic.changePublicOpinion("POLICE", 2, 0, 75);
                        MasterController.generalPublic.changePublicOpinion("CIVIL_RIGHTS", 2, 0, 75);
                        break;
                    case 9:
                        hackString += "published emails revealing CEO tax evasion.";

                        trackDif = Difficulty.FORMIDABLE;
                        crime = "INFORMATION";
                        juice = 10;
                        MasterController.generalPublic.changePublicOpinion("CEO_SALARY", 2, 0, 75);
                        MasterController.generalPublic.changePublicOpinion("TAXES", 2, 0, 75);
                        break;
                    case 10:
                        hackString += "revealed huge political bias in INS processes.";

                        trackDif = Difficulty.FORMIDABLE;
                        crime = "INFORMATION";
                        juice = 10;
                        MasterController.generalPublic.changePublicOpinion("IMMIGRATION", 2, 0, 75);
                        MasterController.generalPublic.changePublicOpinion("FREE_SPEECH", 2, 0, 75);
                        break;
                }

                if ((int)trackDif > hackSkill + mc.LCSRandom(5) - 2)
                    foreach (Entity e in hackingLiberals)
                        e.getComponent<CriminalRecord>().addCrime(crime);

                foreach (Entity e in hackingLiberals)
                    e.getComponent<CreatureBase>().juiceMe(juice, 200);
            }
            else if ((int)Difficulty.FORMIDABLE <= hackSkill + hackingLiberals.Count - 1)
            {
                string issue = MasterController.generalPublic.randomissue(true);
                string crime = "INFORMATION";

                if (hackingLiberals.Count > 1)
                    hackString += "Your hackers have ";
                else
                    hackString += hackingLiberals[0].getComponent<CreatureInfo>().getName() + " has ";

                switch (mc.LCSRandom(4))
                {
                    case 0: hackString += "defaced"; crime = "INFORMATION"; break;
                    case 1: hackString += "knocked out"; crime = "COMMERCE"; break;
                    case 2: hackString += "threatened"; crime = "SPEECH"; break;
                    case 3: hackString += "hacked"; crime = "INFORMATION"; break;
                }
                hackString += " a ";
                switch (mc.LCSRandom(5))
                {
                    case 0: hackString += "corporate website"; break;
                    case 1: hackString += "Conservative forum"; break;
                    case 2: hackString += "Conservative blog"; break;
                    case 3: hackString += "news website"; break;
                    case 4: hackString += "government website"; break;
                }
                hackString += ".";

                MasterController.generalPublic.changePublicOpinion(issue, 1);

                if ((int)Difficulty.FORMIDABLE > hackSkill + mc.LCSRandom(5) - 2)
                    foreach (Entity e in hackingLiberals)
                        e.getComponent<CriminalRecord>().addCrime(crime);

                foreach (Entity e in hackingLiberals)
                    e.getComponent<CreatureBase>().juiceMe(5, 200);
            }

            if(hackString != "")
                mc.addMessage(hackString);
        }

        private void populateMemberList(Entity member, List<Entity> list)
        {
            list.Add(member);

            foreach(Entity e in member.getComponent<Liberal>().subordinates)
            {
                populateMemberList(e, list);
            }
        }

        public class Squad : List<Entity>
        {
            public enum TravelAction
            {
                BASE,
                TROUBLE,
                BOTH
            }

            public string name { get; set; }
            public List<Entity> inventory { get; set; }
            public Entity homeBase { get; set; }

            public Entity target { get; set; }
            public TravelAction travelAction { get; set; }

            public Squad(string name) : base()
            {
                this.name = name;
                inventory = new List<Entity>();
            }

            public void save(XmlNode node)
            {
                XmlNode saveNode = node.OwnerDocument.CreateElement("Squad");
                node.AppendChild(saveNode);
                XmlNode nameNode = saveNode.OwnerDocument.CreateElement("name");
                saveNode.AppendChild(nameNode);
                XmlNode homeNode = saveNode.OwnerDocument.CreateElement("homeBase");
                saveNode.AppendChild(homeNode);
                XmlNode targetNode = saveNode.OwnerDocument.CreateElement("target");
                saveNode.AppendChild(targetNode);
                XmlNode travelActionNode = saveNode.OwnerDocument.CreateElement("travelAction");
                saveNode.AppendChild(travelActionNode);

                saveNode.SelectSingleNode("name").InnerText = name;
                saveNode.SelectSingleNode("homeBase").InnerText = homeBase.guid.ToString();

                if (target != null) saveNode.SelectSingleNode("target").InnerText = target.guid.ToString();
                else saveNode.SelectSingleNode("target").InnerText = "null";

                saveNode.SelectSingleNode("travelAction").InnerText = travelAction.ToString();

                if (saveNode.SelectSingleNode("members") != null)
                    saveNode.RemoveChild(saveNode.SelectSingleNode("members"));

                XmlNode membersNode = saveNode.OwnerDocument.CreateElement("members");
                saveNode.AppendChild(membersNode);

                foreach (Entity e in this)
                {
                    XmlNode memberNode = saveNode.OwnerDocument.CreateElement("member");
                    memberNode.InnerText = e.guid.ToString();
                    membersNode.AppendChild(memberNode);
                }
            }

            public void startCausingTrouble()
            {
                foreach (Entity e in this)
                {
                    e.getComponent<CreatureBase>().Location = target;
                    if (e.getComponent<Inventory>().getWeapon().getComponent<Weapon>().needsReload())
                        e.getComponent<Inventory>().reload(false);
                }
            }

            public void goHome()
            {
                foreach(Entity e in inventory)
                {
                    homeBase.getComponent<SafeHouse>().addItemToInventory(e);
                }
                inventory.Clear();

                foreach (Entity e in this)
                {
                    e.getComponent<Liberal>().goHome();
                }
                
            }

            public Entity getBestAtSkill(string skillName)
            {
                Entity best = this[0];

                foreach(Entity e in this)
                {
                    if (e.getComponent<CreatureBase>().Skills[skillName].level >
                        best.getComponent<CreatureBase>().Skills[skillName].level)
                        best = e;
                }

                return best;
            }

            public Entity getBestAtCombination(string[] attributes, string[] skills)
            {
                Entity best = this[0];

                foreach(Entity e in this)
                {
                    if (e.getComponent<CreatureBase>().getPower(attributes, skills) >
                        best.getComponent<CreatureBase>().getPower(attributes, skills))
                        best = e;
                }

                return best;
            }

            new public void Add(Entity e)
            {
                if (Count < 6)
                {
                    e.getComponent<Liberal>().squad = this;
                    base.Add(e);
                }
                else
                    return;
            }

            new public void Insert(int i, Entity e)
            {
                if (Count < 6)
                {
                    e.getComponent<Liberal>().squad = this;
                    base.Insert(i,e);
                }
                else
                    return;
            }

            public void Remove(Entity e, bool noDestroy)
            {
                if(noDestroy == false)
                {
                    Remove(e);
                    return;
                }
                else
                {
                    if (Contains(e))
                    {
                        e.getComponent<Liberal>().squad = null;
                        base.Remove(e);
                    }
                }
            }

            new public void Remove(Entity e)
            {
                if (Contains(e))
                {
                    e.getComponent<Liberal>().squad = null;
                    base.Remove(e);
                    if(Count == 0)
                    {
                        MasterController.lcs.squads.Remove(this);
                        if(MasterController.lcs.activeSquad == this)
                        {
                            if (MasterController.lcs.squads.Count > 0)
                            {
                                MasterController.lcs.activeSquad = MasterController.lcs.squads[0];
                            }
                            else
                            {
                                MasterController.lcs.activeSquad = null;
                            }
                        }

                        foreach(Entity i in inventory)
                        {
                            i.depersist();
                        }
                    }
                }
            }
        }

        public class Memorial
        {
            public Portrait portrait;
            public string name;
            public string causeOfDeath;
            public DateTime timeOfDeath;
            public bool old;
            public List<string> damagedOrgans;

            public Memorial()
            {
                name = "";
                causeOfDeath = "";
                damagedOrgans = new List<string>();
            }

            public Memorial(Portrait portrait, bool old, string name, string causeOfDeath, DateTime timeOfDeath, List<string> damagedOrgans)
            {
                this.portrait = portrait;
                this.old = old;
                this.name = name;
                this.causeOfDeath = causeOfDeath;
                this.timeOfDeath = timeOfDeath;
                this.damagedOrgans = new List<string>(damagedOrgans);
            }

            public void save(XmlNode parent)
            {
                XmlNode saveNode = parent.OwnerDocument.CreateElement("Memorial");
                parent.AppendChild(saveNode);

                XmlNode nameNode = saveNode.OwnerDocument.CreateElement("name");
                saveNode.AppendChild(nameNode);
                XmlNode causeNode = saveNode.OwnerDocument.CreateElement("causeofdeath");
                saveNode.AppendChild(causeNode);
                XmlNode dateNode = saveNode.OwnerDocument.CreateElement("timeofdeath");
                saveNode.AppendChild(dateNode);
                XmlNode oldNode = saveNode.OwnerDocument.CreateElement("old");
                saveNode.AppendChild(oldNode);

                foreach(string s in damagedOrgans)
                {
                    XmlNode organNode = saveNode.OwnerDocument.CreateElement("damagedorgan");
                    organNode.InnerText = s;
                    saveNode.AppendChild(organNode);
                }

                portrait.save(saveNode);
                nameNode.InnerText = name;
                causeNode.InnerText = causeOfDeath;
                dateNode.InnerText = timeOfDeath.ToString("d");
                oldNode.InnerText = old.ToString();
            }

            public void load(XmlNode memorialNode, Dictionary<long, Entity> entityList)
            {
                portrait = new Portrait();
                portrait.load(memorialNode.SelectSingleNode("Portrait"), entityList);
                name = memorialNode.SelectSingleNode("name").InnerText;
                causeOfDeath = memorialNode.SelectSingleNode("causeofdeath").InnerText;
                timeOfDeath = DateTime.Parse(memorialNode.SelectSingleNode("timeofdeath").InnerText);
                if(memorialNode.SelectSingleNode("old") != null)
                    old = bool.Parse(memorialNode.SelectSingleNode("old").InnerText);

                foreach (XmlNode organNode in memorialNode.SelectNodes("damagedorgan"))
                    damagedOrgans.Add(organNode.InnerText);
            }
        }

        public class FinanceMonth
        {
            public int income;
            public int expenses;

            public FinanceMonth()
            {
                income = 0;
                expenses = 0;
            }

            public FinanceMonth(int income, int expenses)
            {
                this.income = income;
                this.expenses = expenses;
            }

            public FinanceMonth(FinanceMonth input)
            {
                income = input.income;
                expenses = input.expenses;
            }

            public void save(XmlNode parent)
            {
                XmlNode saveNode = parent.OwnerDocument.CreateElement("FinanceMonth");
                parent.AppendChild(saveNode);

                XmlNode incomeNode = saveNode.OwnerDocument.CreateElement("income");
                saveNode.AppendChild(incomeNode);

                XmlNode expenseNode = saveNode.OwnerDocument.CreateElement("expenses");
                saveNode.AppendChild(expenseNode);

                incomeNode.InnerText = income.ToString();
                expenseNode.InnerText = expenses.ToString();
            }

            public void load(XmlNode financialNode)
            {
                income = int.Parse(financialNode.SelectSingleNode("income").InnerText);
                expenses = int.Parse(financialNode.SelectSingleNode("expenses").InnerText);
            }
        };
    }
}
