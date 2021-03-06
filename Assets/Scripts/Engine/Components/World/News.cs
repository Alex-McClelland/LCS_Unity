using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using LCS.Engine.Components.Location;
using LCS.Engine.Components.Item;
using LCS.Engine.Components.Creature;
using LCS.Engine.Data;

namespace LCS.Engine.Components.World
{
    public class News : Component
    {
        public class NewsStory
        {
            public bool claimed = true;
            public bool positive;
            public string type;
            public string majorstorytype = "";
            public int politicsLevel = 0;
            public int violenceLevel = 0;
            public Entity subject;
            public Entity location;
            public Dictionary<string, int> crimes;
            public int priority;
            public int page;
            public LocationDef.EnemyType siegeType;
            public string headline = "";
            public string text = "";

            public NewsStory()
            {
                crimes = new Dictionary<string, int>();
            }

            public void addCrime(string crime)
            {
                if (crimes.ContainsKey(crime))
                    crimes[crime]++;
                else
                    crimes[crime] = 1;
            }
        }

        public List<NewsStory> stories { get; set; }
        public NewsStory currentStory { get; set; }
        [SimpleSave]
        public bool newsCherryBusted;

        public News()
        {
            stories = new List<NewsStory>();
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("News");
                entityNode.AppendChild(saveNode);
            }

            saveSimpleFields();
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);
        }

        public NewsStory startNewStory(string type, Entity location = null)
        {
            NewsStory story = new NewsStory();
            story.type = type;
            story.location = location;
            story.positive = true;

            stories.Add(story);
            currentStory = story;
            return story;
        }

        public void prepareNewspaper()
        {
            generateRandomEventStory();
            cleanEmptyStories();

            assignPageNumbers();

            stories.Sort((NewsStory x, NewsStory y) => { return x.page.CompareTo(y.page); });

            if (stories.Count > 0)
            {
                MasterController.GetMC().addAction(()=> 
                {
                    bool guardian = false;

                    foreach (Entity e in MasterController.lcs.getAllMembers())
                    {
                        if (e.getComponent<Liberal>().dailyActivity.type == "WRITE_GUARDIAN" && e.getComponent<Liberal>().status == Liberal.Status.ACTIVE)
                        {
                            guardian = true;
                            break;
                        }
                    }

                    if (MasterController.GetMC().canSeeThings)
                    {
                        foreach (NewsStory story in stories)
                        {
                            setText(story, guardian);
                            handlePublicOpinion(story);
                            if ((GameData.getData().newsTypeList[story.type].flags & NewsTypeDef.NewsTypeFlag.NEWSCHERRY) != 0) newsCherryBusted = true;
                            if ((GameData.getData().newsTypeList[story.type].flags & NewsTypeDef.NewsTypeFlag.CCS_ACTION) != 0) MasterController.ccs.newsCherry = true;
                        }
                        MasterController.GetMC().uiController.closeUI();
                        MasterController.GetMC().uiController.news.show();
                    }

                    if (!MasterController.GetMC().canSeeThings)
                        MasterController.GetMC().doNextAction();
                }, "Show News");
            }
        }

        public void specialEditionCheck()
        {
            bool writingLibs = false;

            foreach(Entity lib in MasterController.lcs.getAllMembers())
            {
                if (lib.getComponent<Liberal>().dailyActivity.type == "WRITE_GUARDIAN")
                {
                    writingLibs = true;
                    break;
                }
            }

            if (!writingLibs) return;

            List<Entity> lootToPublish = new List<Entity>();
            List<string> lootTypes = new List<string>();

            foreach(Entity safehouse in MasterController.nation.getAllBases(true))
            {
                foreach(Entity item in safehouse.getComponent<SafeHouse>().getInventory())
                {
                    if(item.hasComponent<Loot>() && item.getComponent<Loot>().getEvidence().Count > 0 && !lootTypes.Contains(item.def))
                    {
                        lootToPublish.Add(item);
                        lootTypes.Add(item.def);
                    }
                }
            }

            if (lootToPublish.Count == 0) return;

            MasterController.GetMC().addAction(() => { MasterController.GetMC().uiController.showGuardianPopup(lootToPublish); }, "Show Guardian Popup");
        }

        public void publishSpecialEdition(Entity item)
        {
            List<Entity> writers = new List<Entity>();

            foreach(Entity e in MasterController.lcs.getAllMembers())
            {
                if (e.getComponent<Liberal>().dailyActivity.type == "WRITE_GUARDIAN") writers.Add(e);
            }

            if(MasterController.government.laws[Constants.VIEW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE)
            {
                MasterController.lcs.offendedFiremen = true;
            }

            MasterController.generalPublic.changePublicOpinion(Constants.VIEW_LIBERALCRIMESQUAD, 10);
            MasterController.generalPublic.changePublicOpinion(Constants.VIEW_LIBERALCRIMESQUADPOS, 10);

            ItemDef.LootEvidence evidence = item.getComponent<Loot>().getEvidence()[MasterController.GetMC().LCSRandom(item.getComponent<Loot>().getEvidence().Count)];
            foreach(ViewDef affectedView in evidence.affectedIssues.Keys)
            {
                MasterController.generalPublic.changePublicOpinion(affectedView.type, evidence.affectedIssues[affectedView]);
            }

            foreach(CrimeDef crime in evidence.lawsBroken)
            {
                foreach(Entity e in writers)
                {
                    e.getComponent<CriminalRecord>().addCrime(crime.type);
                }
            }

            string offendedString = "";

            switch (evidence.offendedGroup)
            {
                case ItemDef.LootEvidence.LootEvidenceOffendedGroups.AMRADIO:
                    MasterController.lcs.offendedAMRadio = true;
                    offendedString = "\nThis is bound to get the Conservative masses a little riled up.";
                    break;
                case ItemDef.LootEvidence.LootEvidenceOffendedGroups.CABLENEWS:
                    MasterController.lcs.offendedCableNews = true;
                    offendedString = "\nThis is bound to get the Conservative masses a little riled up.";
                    break;
                case ItemDef.LootEvidence.LootEvidenceOffendedGroups.CIA:
                    MasterController.lcs.offendedCIA = true;
                    offendedString = "\nThis is bound to get the Government a little riled up.";
                    break;
                case ItemDef.LootEvidence.LootEvidenceOffendedGroups.CORPS:
                    MasterController.lcs.offendedCorps = true;
                    offendedString = "\nThis is bound to get the Corporations a little riled up.";
                    break;
                case ItemDef.LootEvidence.LootEvidenceOffendedGroups.FIREMEN:
                    if (MasterController.government.laws[Constants.VIEW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE)
                    {
                        MasterController.lcs.offendedFiremen = true;
                        offendedString = "\nThis is bound to get the Firemen a little riled up.";
                    }
                    break;
            }

            string description = evidence.descriptionText[MasterController.GetMC().LCSRandom(evidence.descriptionText.Count)];
            if (item.def == "LOOT_CCS_BACKERLIST")
            {
                MasterController.ccs.exposure = ConservativeCrimeSquad.Exposure.EXPOSED;
            }
            else
            {
                description += "\nThe major networks and publications take it up and run it for weeks.";
                description += offendedString;
            }

            item.getComponent<ItemBase>().destroyItem();

            MasterController.GetMC().addMessage(description, true);
        }

        private void generateRandomEventStory()
        {
            MasterController mc = MasterController.GetMC();

            if(mc.LCSRandom(60) == 0)
            {
                NewsStory story = new NewsStory();
                story.type = "MAJOREVENT";
                story.positive = mc.LCSRandom(2) == 0 ? true : false;

                List<string> viewList = new List<string>();

                //TODO: Maybe externalize these somehow
                foreach(string view in MasterController.generalPublic.PublicOpinion.Keys)
                {
                    if (view == Constants.VIEW_LIBERALCRIMESQUAD) continue;
                    if (view == Constants.VIEW_LIBERALCRIMESQUADPOS) continue;
                    if (view == Constants.VIEW_CONSERVATIVECRIMESQUAD) continue;

                    if (view == Constants.VIEW_IMMIGRATION) continue;
                    if (view == Constants.VIEW_DRUGS) continue;
                    if (view == Constants.VIEW_MILITARY) continue;
                    if (view == Constants.VIEW_CIVIL_RIGHTS) continue;
                    if (view == Constants.VIEW_TORTURE) continue;

                    if (story.positive)
                    {
                        //Abortion Banned
                        if (view == Constants.VIEW_WOMEN && MasterController.government.laws[Constants.LAW_ABORTION].alignment == Alignment.ARCHCONSERVATIVE)
                            continue;
                        //Death Penalty Banned
                        if (view == Constants.VIEW_DEATH_PENALTY && MasterController.government.laws[Constants.LAW_DEATH_PENALTY].alignment == Alignment.ELITE_LIBERAL)
                            continue;
                        //Nuclear Power Banned
                        if (view == Constants.VIEW_NUCLEAR_POWER && MasterController.government.laws[Constants.LAW_NUCLEAR_POWER].alignment == Alignment.ELITE_LIBERAL)
                            continue;
                        //Animal Research Banned
                        if (view == Constants.VIEW_ANIMAL_RESEARCH && MasterController.government.laws[Constants.LAW_ANIMAL_RESEARCH].alignment == Alignment.ELITE_LIBERAL)
                            continue;
                        //Police Corruption Eliminated
                        if (view == Constants.VIEW_POLICE && MasterController.government.laws[Constants.LAW_POLICE].alignment == Alignment.ELITE_LIBERAL)
                            continue;
                        //Privacy Rights Respected
                        if (view == Constants.VIEW_INTELLIGENCE && MasterController.government.laws[Constants.LAW_PRIVACY].alignment == Alignment.ELITE_LIBERAL)
                            continue;
                        //Sweatshops Banned
                        if (view == Constants.VIEW_SWEATSHOPS && MasterController.government.laws[Constants.LAW_LABOR].alignment == Alignment.ELITE_LIBERAL)
                            continue;
                        //Pollution Under Control
                        if (view == Constants.VIEW_POLLUTION && MasterController.government.laws[Constants.LAW_POLLUTION].alignment >= Alignment.CONSERVATIVE)
                            continue;
                        //Corporations Regulated
                        if (view == Constants.VIEW_CORPORATE_CULTURE && MasterController.government.laws[Constants.LAW_CORPORATE].alignment == Alignment.ELITE_LIBERAL)
                            continue;
                        //CEO's Aren't Rich
                        if (view == Constants.VIEW_CEO_SALARY && MasterController.government.laws[Constants.LAW_CORPORATE].alignment == Alignment.ELITE_LIBERAL)
                            continue;
                    }
                    else
                    {
                        //Partial-birth Abortion Banned
                        if (view == Constants.VIEW_WOMEN && MasterController.government.laws[Constants.LAW_ABORTION].alignment < Alignment.ELITE_LIBERAL)
                            continue;
                        //AM Radio censored
                        if (view == Constants.VIEW_AM_RADIO && MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE)
                            continue;
                    }

                    viewList.Add(view);
                }

                story.majorstorytype = viewList[mc.LCSRandom(viewList.Count)];
                stories.Add(story);

                if (story.positive) MasterController.generalPublic.changePublicOpinion(story.majorstorytype, 20, 0);
                else MasterController.generalPublic.changePublicOpinion(story.majorstorytype, -20, 0);
                MasterController.generalPublic.PublicInterest[story.majorstorytype] += 50;
            }
        }

        private void cleanEmptyStories()
        {
            List<NewsStory> allStories = new List<NewsStory>(stories);

            foreach(NewsStory story in allStories)
            {
                //Remove site stories where nothing happened
                if((story.type == "SQUAD_SITE" || story.type == "CCS_SITE") && story.crimes.Count == 0)
                {
                    stories.Remove(story);
                    continue;
                }

                //Remove police killed stories if no police were killed
                if((GameData.getData().newsTypeList[story.type].flags & NewsTypeDef.NewsTypeFlag.POLICE_KILLED) != 0 &&
                    !story.crimes.ContainsKey("KILLEDSOMEBODY"))
                {
                    stories.Remove(story);
                    continue;
                }

                //Remove non-police sieges
                if ((GameData.getData().newsTypeList[story.type].flags & NewsTypeDef.NewsTypeFlag.SIEGE) != 0 &&
                    story.siegeType != LocationDef.EnemyType.POLICE)
                {
                    stories.Remove(story);
                    continue;
                }
            }
        }

        private void assignPageNumbers()
        {
            MasterController mc = MasterController.GetMC();

            List<NewsStory> allStories = new List<NewsStory>(stories);

            foreach(NewsStory story in allStories)
            {
                setPriority(story);
                //Suppres minor site stories
                if ((story.type == "SQUAD_SITE" || story.type == "CCS_SITE") &&
                    ((story.priority < 50 &&
                    !story.claimed) ||
                    story.priority < 4))
                {
                    stories.Remove(story);
                }
            }

            int curpage = 1;

            List<NewsStory> sortedStories = new List<NewsStory>(stories);
            sortedStories.Sort((NewsStory x, NewsStory y) => { return x.priority.CompareTo(y.priority); });
            
            foreach(NewsStory story in sortedStories)
            {
                if (story.priority < 30 && curpage == 1) curpage = 2;
                if (story.priority < 25 && curpage < 3) curpage = 3 + mc.LCSRandom(2);
                if (story.priority < 20 && curpage < 5) curpage = 5 + mc.LCSRandom(5);
                if (story.priority < 15 && curpage < 10) curpage = 10 + mc.LCSRandom(10);
                if (story.priority < 10 && curpage < 20) curpage = 20 + mc.LCSRandom(10);
                if (story.priority < 5 && curpage < 30) curpage = 30 + mc.LCSRandom(20);

                story.page = curpage;
                curpage++;
            }
        }

        private void handlePublicOpinion(NewsStory story)
        {
            if ((GameData.getData().newsTypeList[story.type].flags & NewsTypeDef.NewsTypeFlag.SHIFT_PUBLIC_OPINION) == 0)
                return;

            int impact = story.priority;

            // Magnitude of impact will be affected by which page of the newspaper the story appears on
            if (story.page == 1) impact *= 5;
            else if (story.page == 2) impact *= 3;
            else if (story.page == 3) impact *= 2;

            int maxpower = 1;
            if (story.page == 1) maxpower = 100;
            else if (story.page < 5) maxpower = 100 - 10 * story.page;
            else if (story.page < 10) maxpower = 40;
            else if (story.page < 20) maxpower = 20;
            else if (story.page < 30) maxpower = 10;
            else if (story.page < 40) maxpower = 5;
            else maxpower = 1;

            foreach(Entity e in MasterController.lcs.getAllMembers())
            {
                if (e.getComponent<Liberal>().dailyActivity.type == "WRITE_GUARDIAN")
                {
                    impact *= 5;
                    if(story.type == "SQUAD_SITE" || story.type == "SQUAD_KILLED_SITE")
                    {
                        if(story.location.getComponent<TroubleSpot>().getNewsHeader() != null && story.priority > 150)
                        {
                            getComponent<Public>().changePublicOpinion(story.location.getComponent<TroubleSpot>().getNewsHeader().type, 5, 1);
                        }
                    }
                    break;
                }
            }

            if (impact > maxpower)
                impact = maxpower;

            impact = impact / 10 + 1;

            Alignment direction = Alignment.LIBERAL;

            if((GameData.getData().newsTypeList[story.type].flags & NewsTypeDef.NewsTypeFlag.CCS_ACTION) != 0)
            {
                direction = Alignment.CONSERVATIVE;
                if (story.positive)
                    getComponent<Public>().changePublicOpinion("CONSERVATIVECRIMESQUAD", impact, 0);
                else
                    getComponent<Public>().changePublicOpinion("CONSERVATIVECRIMESQUAD", -impact, 0);
            }
            else
            {
                getComponent<Public>().changePublicOpinion("LIBERALCRIMESQUAD", impact+2);
                if(story.positive)
                    getComponent<Public>().changePublicOpinion("LIBERALCRIMESQUADPOS", impact);
                else
                    getComponent<Public>().changePublicOpinion("LIBERALCRIMESQUADPOS", -impact);
            }

            if (direction == Alignment.CONSERVATIVE)
                impact = -impact;

            if (!story.positive) impact /= 4;

            getComponent<Public>().changePublicOpinion("GUN_CONTROL", Math.Abs(impact)/10, 0, Math.Abs(impact)*10);

            if(story.location != null && story.location.hasComponent<TroubleSpot>())
            {
                foreach(ViewDef view in story.location.getComponent<TroubleSpot>().getViews())
                {
                    if(direction == Alignment.LIBERAL)
                        getComponent<Public>().changePublicOpinion(view.type, impact, 1, impact*10);
                    else
                        getComponent<Public>().changePublicOpinion(view.type, impact, -1, impact * 10);
                }
            }
        }

        private void setPriority(NewsStory story)
        {
            story.priority = GameData.getData().newsTypeList[story.type].priority;

            if (story.claimed) story.politicsLevel = 5;

            List<string> crimes = new List<string>(story.crimes.Keys);

            foreach(string crime in crimes)
            {
                if (GameData.getData().newsActionList[crime].cap > 0 && story.crimes[crime] > GameData.getData().newsActionList[crime].cap)
                    story.crimes[crime] = GameData.getData().newsActionList[crime].cap;
                story.priority += story.crimes[crime] * GameData.getData().newsActionList[crime].priority;
                story.politicsLevel += story.crimes[crime] * GameData.getData().newsActionList[crime].politics;
                story.violenceLevel += story.crimes[crime] * GameData.getData().newsActionList[crime].violence;
            }

            if ((GameData.getData().newsTypeList[story.type].flags & NewsTypeDef.NewsTypeFlag.LCSPRIORITY) != 0)
                story.priority += getComponent<Public>().PublicOpinion["LIBERALCRIMESQUAD"] / 3;            

            if (story.claimed) story.priority *= 2;
            if(story.location != null)
            {
                if (story.location.hasComponent<SafeHouse>() && !story.location.getComponent<SafeHouse>().owned && (story.location.getComponent<SafeHouse>().getFlags() & LocationDef.BaseFlag.CCS_BASE) != 0)
                    story.priority = 0;

                if (story.location.hasComponent<TroubleSpot>())
                {
                    //These locations don't even make the news; nobody cares what happens at a crack house
                    if ((story.location.getComponent<TroubleSpot>().getFlags() & LocationDef.TroubleSpotFlag.NEWS_PRIORITY_NONE) != 0)
                        story.priority = 0;
                    //It takes a LOT for actions in a slum to make the news
                    else if ((story.location.getComponent<TroubleSpot>().getFlags() & LocationDef.TroubleSpotFlag.NEWS_PRIORITY_LOW) != 0)
                        story.priority /= 8;
                    //These are high profile locations - either important government locations or high security
                    else if ((story.location.getComponent<TroubleSpot>().getFlags() & LocationDef.TroubleSpotFlag.NEWS_PRIORITY_HIGH) != 0)
                        story.priority *= 2;
                }
            }

            if (story.type == "KIDNAPREPORT" && (story.subject.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.ARCHCONSERVATIVE) != 0)
                story.priority *= 2;

            //Cap priority at 20000 so major events always get precedence
            if (story.type != "MAJOREVENT")
                story.priority = Math.Min(story.priority, 20000);
        }

        private void setText(NewsStory story, bool guardian = false)
        {
            if (story.type != "MAJOREVENT")
            {
                string storyText = parseStory(story, guardian);

                if (storyText != "")
                {
                    storyText = char.ToUpper(storyText[0]) + storyText.Substring(1);
                }

                story.text = storyText;

                if (guardian && GameData.getData().newsTypeList[story.type].guardianHeadline != "")
                {
                    if ((story.type == "SQUAD_SITE" || story.type == "SQUAD_KILLED_SITE") && story.priority > 150 && story.positive)
                    {
                        if (story.location.getComponent<TroubleSpot>().getNewsHeader() != null)
                        {
                            switch (story.location.getComponent<TroubleSpot>().getNewsHeader().type)
                            {
                                case Constants.VIEW_TAXES:
                                case Constants.VIEW_SWEATSHOPS:
                                case Constants.VIEW_CEO_SALARY:
                                    story.headline = "CLASS WAR";
                                    break;
                                case Constants.VIEW_NUCLEAR_POWER:
                                    story.headline = "MELTDOWN RISK";
                                    break;
                                case Constants.VIEW_POLICE:
                                    story.headline = "LCS VS COPS";
                                    break;
                                case Constants.VIEW_DEATH_PENALTY:
                                    story.headline = "PRISON WAR";
                                    break;
                                case Constants.VIEW_INTELLIGENCE:
                                    story.headline = "LCS VS CIA";
                                    break;
                                case Constants.VIEW_ANIMAL_RESEARCH:
                                case Constants.VIEW_GENETICS:
                                    story.headline = "EVIL RESEARCH";
                                    break;
                                case Constants.VIEW_FREE_SPEECH:
                                case Constants.VIEW_GAY:
                                case Constants.VIEW_JUSTICES:
                                    story.headline = "NO JUSTICE";
                                    break;
                                case Constants.VIEW_POLLUTION:
                                    story.headline = "POLLUTER HIT";
                                    break;
                                case Constants.VIEW_CORPORATE_CULTURE:
                                    story.headline = "LCS HITS CORP";
                                    break;
                                case Constants.VIEW_AM_RADIO:
                                    story.headline = "LCS HITS AM";
                                    break;
                                case Constants.VIEW_CABLE_NEWS:
                                    story.headline = "LCS HITS TV";
                                    break;
                            }
                        }
                        else
                        {
                            story.headline = "HEROIC STRIKE";
                        }
                    }
                    else
                        story.headline = parseHeadline(story, GameData.getData().newsTypeList[story.type].guardianHeadline);
                }
                else
                    story.headline = parseHeadline(story, GameData.getData().newsTypeList[story.type].headline);
            }
            else
            {
                MasterController mc = MasterController.GetMC();

                string text = GameData.getData().nationList["USA"].capital + " - ";

                //TODO: Externalize these
                switch (story.majorstorytype)
                {
                    case Constants.VIEW_WOMEN:
                        if (story.positive)
                        {
                            CreatureInfo.CreatureGender gender = mc.LCSRandom(2) == 0 ? CreatureInfo.CreatureGender.MALE : CreatureInfo.CreatureGender.FEMALE;
                            string doctorFirstName = Factories.CreatureFactory.generateGivenName(gender);
                            string doctorLastName = Factories.CreatureFactory.generateSurname(gender);
                            string shooterFirstName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.NEUTRAL);
                            string shooterLastName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);

                            story.headline = "CLINIC MURDER";

                            if (MasterController.government.laws[Constants.LAW_ABORTION].alignment == Alignment.ARCHCONSERVATIVE)
                                text += "A doctor that routinely performed illegal abortion-murders was ruthlessly ";
                            else if (MasterController.government.laws[Constants.LAW_ABORTION].alignment == Alignment.CONSERVATIVE)
                                text += "A doctor that routinely performed illegal abortions was ruthlessly ";
                            else if (MasterController.government.laws[Constants.LAW_ABORTION].alignment == Alignment.MODERATE)
                                text += "A doctor that routinely performed semi-legal abortions was ruthlessly ";
                            else
                                text += "A doctor that routinely performed abortions was ruthlessly ";
                            text += "gunned down outside of the " + Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL) + " clinic yesterday. ";
                            text += "Dr. " + doctorFirstName + " " + doctorLastName + " was walking to ";
                            text += gender == CreatureInfo.CreatureGender.MALE ? "his" : "her";
                            text += " car when, according to police reports, shots were fired from a nearby vehicle. ";
                            text += doctorLastName + " was hit " + (mc.LCSRandom(15) + 3) + " times and died immediately in the parking lot. The suspected shooter, ";
                            text += shooterFirstName + " " + shooterLastName + " is in custody. Witnesses report that " + shooterLastName + " remained at the scene after the shooting, screaming verses of the Bible at the stunned onlookers. Someone called the police on a cellphone and they arrived shortly thereafter.\n\t";
                            text += shooterLastName;
                            if (MasterController.government.laws[Constants.LAW_WOMEN].alignment == Alignment.ARCHCONSERVATIVE)
                                text += " later admitted to being a rogue FBI vigilante, hunting down abortion doctors as opposed to arresting them.";
                            else
                                text += " surrendered without a struggle, reportedly saying that God's work had been completed.";
                            text += "\n\t" + doctorLastName + " is survived by " + (gender == CreatureInfo.CreatureGender.MALE ? "his " : "her ");
                            CreatureInfo.CreatureGender spouse = mc.LCSRandom(2) == 0 ? CreatureInfo.CreatureGender.MALE : CreatureInfo.CreatureGender.FEMALE;
                            if (MasterController.government.laws[Constants.LAW_GAY].alignment <= Alignment.LIBERAL)
                                spouse = gender == CreatureInfo.CreatureGender.FEMALE ? CreatureInfo.CreatureGender.MALE : CreatureInfo.CreatureGender.FEMALE;
                            text += (spouse == CreatureInfo.CreatureGender.MALE ? "husband" : "wife") + " and " + MasterController.NumberToWords(mc.LCSRandom(4) + 2).ToLower() + " children.";
                        }
                        else
                        {
                            story.headline = "FAILED ABORTION SURVIVES";

                            text += "A recent television appearance by a victim of a failed partial-birth abortion has left the nation sympathizing with the poor thing's pain.";
                        }
                        break;
                    case Constants.VIEW_GAY:
                        if (story.positive)
                        {
                            string victimFirstName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.NEUTRAL);
                            string victimLastName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);

                            story.headline = "CRIME OF HATE";

                            text += victimFirstName + " " + victimLastName;
                            if (MasterController.government.laws[Constants.LAW_GAY].alignment == Alignment.ARCHCONSERVATIVE) text += ", a known sexual deviant, was ";
                            else if (MasterController.government.laws[Constants.LAW_GAY].alignment == Alignment.CONSERVATIVE) text += ", a known homosexual, was ";
                            else text += ", a homosexual, was ";
                            switch (mc.LCSRandom(3))
                            {
                                case 0: text += "dragged to death behind a pickup truck"; break;
                                case 1: text += "burned alive"; break;
                                case 2: text += "beaten to death"; break;
                            }
                            text += " here yesterday. A police spokesperson reported that four suspects were apprehended after a high speed chase. Their names have not yet been released.\n\tWitnesses of the freeway chase described the pickup of the alleged murderers swerving wildly, ";
                            switch (mc.LCSRandom(3))
                            {
                                case 0: text += "throwing " + mc.swearFilter("beer bottles", "juice boxes"); break;
                                case 1: text += mc.swearFilter("urinating", "relieving themselves") + " out the window"; break;
                                case 2: text += "taking swipes"; break;
                            }
                            text += " at the pursuing police cruisers. The chase ended when ";
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "the suspects ran out of gas, "; break;
                                case 1: text += "the suspects collided with a manure truck, "; break;
                                case 2: text += "the suspects veered into a ditch, "; break;
                                case 3: text += "the suspects were surrounded by alert citizens, "; break;
                                case 4: text += "the suspects were caught in traffic, "; break;
                            }
                            text += "at which point they were taken into custody. Nobody was seriously injured during the incident.\n\t";
                            text += "Authorities have stated that they will vigorously prosecute this case as a hate crime, due to the aggravated nature of the offense";
                            if (MasterController.government.laws[Constants.LAW_GAY].alignment == Alignment.ARCHCONSERVATIVE)
                                text += ", even though being gay is deviant, as we all know.";
                            else text += ".";
                        }
                        else
                        {
                            story.headline = "KINKY WINKY";

                            string characterFirstName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.NEUTRAL);
                            string characterLastName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);

                            text += "Religious groups condemn popular children's show ";

                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "Happy"; break;
                                case 1: text += "Friendly"; break;
                                case 2: text += "Adventure"; break;
                                case 3: text += "Learning"; break;
                                case 4: text += "Playtime"; break;
                            }
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += " Meadows"; break;
                                case 1: text += " Park"; break;
                                case 2: text += " Street"; break;
                                case 3: text += " Avenue"; break;
                                case 4: text += " Neighbourhood"; break;
                            }
                            text += " as \"promoting the sinful homosexual lifestyle\" after the character " + characterFirstName + " " + characterLastName + " was revealed to be gay in a recent very special episode.";
                            text += "\n\tThe show's writers stand by their decision, stating \"We can't simply pretend that gay people don't exist just because religious bigots don't like it\"";
                        }
                        break;
                    case Constants.VIEW_DEATH_PENALTY:
                        if (story.positive)
                        {
                            story.headline = "JUSTICE DEAD";
                            string victimFirstName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.NEUTRAL);
                            string victimMiddleName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.NEUTRAL);
                            string victimLastName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);

                            text += "An innocent citizen has been put to death in the electric chair. ";
                            text += victimFirstName + " " + victimMiddleName + " " + victimLastName;
                            text += " was pronounced dead at " + (mc.LCSRandom(12) + 1) + ":" + (mc.LCSRandom(60)) + (mc.LCSRandom(2) == 2 ? " AM" : " PM") + " yesterday at the ";
                            text += Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);
                            text += " Correctional Facility.\n\t" + victimLastName + " was convicted in " + (mc.currentDate.Year - mc.LCSRandom(11) - 10) + " of 13 serial murders. Since then, numerous pieces of exculpatory evidence have been produced, including ";
                            switch (mc.LCSRandom(3))
                            {
                                case 0: text += "a confession from another convict. "; break;
                                case 1: text += "a battery of negative DNA tests. "; break;
                                case 2: text += "an admission from a former prosecutor that " + victimLastName + " was framed "; break;
                            }
                            text += "The state still went through with the execution, with a spokesperson for the governor saying, \"";
                            switch (mc.LCSRandom(3))
                            {
                                case 0: text += "Let's not forget the convict is colored. You know how their kind are"; break;
                                case 1: text += "The convict is always referred to by three names. Assassin, serial killer, either way, guilty. End of story"; break;
                                case 2: text += "he family wants closure. We don't have time for another trial"; break;
                            }
                            text += ".\"\n\tCandlelight vigils were held throughout the country last night during the execution, and more events are expected this evening. If there is a bright side to be found from this tragedy, it will be that our nation is now evaluating the ease with which people can be put to death in this country.";
                        }
                        else
                        {
                            story.headline = "LET'S FRY 'EM";

                            string prisonerFirstName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.NEUTRAL);
                            string prisonerMiddleName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.NEUTRAL);
                            string prisonerLastName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);

                            text += "Perhaps parents can rest easier tonight. The authorities have apprehended their primary suspect in the string of brutal child killings that has kept everyone in the area on edge, according to a spokesperson for the police department here. " + prisonerFirstName + " " + prisonerMiddleName + " " + prisonerLastName + " was detained yesterday afternoon, reportedly in possession of ";
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "pieces of another victim"; break;
                                case 1: text += "bloody toys"; break;
                                case 2: text += "a child's clothing stained with DNA evidence"; break;
                                case 3: text += "seven junior high school yearbooks"; break;
                                case 4: text += "two small backpacks"; break;
                            }
                            string deathText = "";
                            switch (mc.LCSRandom(5))
                            {
                                case 0: deathText = "carved with satanic symbols"; break;
                                case 1: deathText = "sexually mutilated"; break;
                                case 2: deathText = "missing all of their teeth"; break;
                                case 3: deathText = "missing all of their fingers"; break;
                                case 4: deathText = "without eyes"; break;
                            }
                            text += ". Over twenty children in the past two years have gone missing, only to turn up later " + mc.swearFilter("dead and " + deathText, "in a better place") + ". Sources say that the police got a break in the case when ";
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "a victim called 911 just prior to being slain while still on the phone"; break;
                                case 1: text += "the suspect allegedly carved an address into one of the bodies"; break;
                                case 2: text += "an eye witness allegedly spotted the suspect luring a victim into a car"; break;
                                case 3: text += "a blood trail was found on a road that led them to the suspect's car trunk"; break;
                                case 4: text += "they found a victim in a ditch, still clinging to life"; break;
                            }
                            text += ".\n\tThe district attorney's office has already repeatedly said it will be seeking ";
                            if (MasterController.government.laws[Constants.LAW_DEATH_PENALTY].alignment == Alignment.ELITE_LIBERAL) text += "life imprisonment";
                            else text += "the death penalty";
                            text += " in this case.";
                        }
                        break;
                    case Constants.VIEW_GUN_CONTROL:
                        if (story.positive)
                        {
                            story.headline = "MASS SHOOTING";

                            CreatureInfo.CreatureGender shooterGender = mc.LCSRandom(2) == 0 ? CreatureInfo.CreatureGender.MALE : CreatureInfo.CreatureGender.FEMALE;
                            string shooterFirstName = Factories.CreatureFactory.generateGivenName(shooterGender);
                            string shooterLastName = Factories.CreatureFactory.generateSurname(shooterGender);
                            string schoolName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);
                            int schtype = mc.LCSRandom(4);
                            int shooterAge = 6 + schtype * 4 + mc.LCSRandom(6);

                            text += "A student has gone on a " + mc.swearFilter("shooting rampage", "hurting spree") + " at a local ";
                            switch (schtype)
                            {
                                case 0: text += "elementary school"; break;
                                case 1: text += "middle school"; break;
                                case 2: text += "high school"; break;
                                case 3: text += "university"; break;
                            }
                            text += ". " + shooterFirstName + " " + shooterLastName + ", " + shooterAge + ", used a variety of guns to " + mc.swearFilter("mow down", "scare") + " more than a dozen classmates and two teachers at " + schoolName;
                            switch (schtype)
                            {
                                case 0: text += "Elementary School"; break;
                                case 1: text += "Middle School"; break;
                                case 2: text += "High School"; break;
                                case 3: text += "University"; break;
                            }
                            text += ". " + shooterLastName + " entered the " + (schtype != 3 ? "school" : "university") + " while classes were in session, then systematically started breaking into classrooms, " + mc.swearFilter("spraying bullets at", "scaring") + " students and teachers inside. When other students tried to wrestle the weapons away from " + (shooterGender == CreatureInfo.CreatureGender.MALE ? "him" : "her") + " they were " + mc.swearFilter("shot", "unfortunately harmed") + " as well.\n\t";
                            text += "When the police arrived, the student had already " + mc.swearFilter("killed " + (mc.LCSRandom(30) + 2) + " and wounded dozens more", "hurt some people") + ". " + shooterLastName + " " + mc.swearFilter("committed suicide", "fell deeply asleep") + " shortly afterwards. Investigators are currently searching the student's belongings, and initial reports indicate that the student kept a journal that showed " + (shooterGender == CreatureInfo.CreatureGender.MALE ? "he" : "she") + " was disturbingly obsessed with guns and death.";
                        }
                        else
                        {
                            story.headline = "ARMED CITIZEN SAVES LIVES";

                            CreatureInfo.CreatureGender suspectGender = mc.LCSRandom(2) == 0 ? CreatureInfo.CreatureGender.MALE : CreatureInfo.CreatureGender.FEMALE;
                            CreatureInfo.CreatureGender heroGender = mc.LCSRandom(2) == 0 ? CreatureInfo.CreatureGender.MALE : CreatureInfo.CreatureGender.FEMALE;
                            string suspectFirstName = Factories.CreatureFactory.generateGivenName(suspectGender);
                            string suspectLastName = Factories.CreatureFactory.generateSurname(suspectGender);
                            string heroFirstName = Factories.CreatureFactory.generateGivenName(heroGender);
                            string heroLastName = Factories.CreatureFactory.generateSurname(heroGender);
                            string locationName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);

                            text += "In a surprising turn, a " + mc.swearFilter("mass shooting", "hurting spree") + " was prevented by a bystander with a gun. After " + suspectFirstName + " " + suspectLastName + " opened fire at the " + locationName;
                            switch (mc.LCSRandom(4))
                            {
                                case 0: text += " Mall"; break;
                                case 1: text += " Theater"; break;
                                case 2: text += " High School"; break;
                                case 3: text += " University"; break;
                            }
                            text += ", " + heroFirstName + " " + heroLastName + " sprung into action. The citizen pulled a concealed handgun and fired once at the shooter, forcing " + suspectLastName + " to take cover while others called the police.\n\tInitially, ";
                            if (heroGender == CreatureInfo.CreatureGender.FEMALE)
                            {
                                if (mc.LCSRandom(4) < ((int)MasterController.government.laws[Constants.LAW_WOMEN].alignment) + 2)
                                    text += "Ms. ";
                                else
                                    text += (mc.LCSRandom(2) == 0 ? "Mrs. " : "Miss ");
                            }
                            else text += "Mr. ";
                            text += heroLastName + " attempted to talk down the shooter, but as " + suspectLastName + " became more agitated, the heroic citizen was forced to engage the shooter in a firefight, " + mc.swearFilter("killing the attacker", "putting the attacker to sleep") + " before " + (suspectGender == CreatureInfo.CreatureGender.MALE ? "he" : "she") + " could hurt anyone else.\n\tThe spokesperson for the police department said, \"We'd have a yet another " + mc.swearFilter("mass shooting", "hurting spree") + " if not for " + heroLastName + "s heroic actions.\"";
                        }
                        break;
                    case Constants.VIEW_TAXES:
                        if (story.positive)
                        {
                            story.headline = "REAGAN FLAWED";
                            CreatureInfo.CreatureGender authorGender = mc.LCSRandom(2) == 0 ? CreatureInfo.CreatureGender.MALE : CreatureInfo.CreatureGender.FEMALE;
                            string authorFirstName = Factories.CreatureFactory.generateGivenName(authorGender);
                            string authorLastName = Factories.CreatureFactory.generateSurname(authorGender);

                            text += "A new tell all memoir from Reagan administration insider " + authorFirstName + " " + authorLastName + " reveals a number of personal problems that plagued the late president.";
                        }
                        else
                        {
                            story.headline = "REAGAN THE MAN";

                            text += "A new biography of the late president details just how great he truly was.";
                        }
                        break;
                    case Constants.VIEW_NUCLEAR_POWER:
                        if (story.positive)
                        {
                            story.headline = "MELTDOWN";

                            string plantName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);

                            text += "A meltdown scare occurred at " + plantName + " Nuclear Plant today, and although a wider breach was eventually prevented, the radiation released into the surrounding environment is reported to have contaminated important salmon spawning pools that could lead to the species extinction in the area.";
                        }
                        else
                        {
                            story.headline = "OIL CRUNCH";

                            text += "Oil prices are expected to hit an all time high this year, and will only continue to increase as the global supply dwindles, forcing nations to start looking towards nuclear power as a cheaper alternative energy source.";
                        }
                        break;
                    case Constants.VIEW_ANIMAL_RESEARCH:
                        if (story.positive)
                        {
                            story.headline = "HELL ON EARTH";

                            text += "A mutant animal escaped from a research facility today, and went on a rampage leaving " + (20 + mc.LCSRandom(15)) + " dead.";
                        }
                        else
                        {
                            story.headline = "APE EXPLORERS";

                            text += "Researchers ";
                            if (MasterController.government.laws[Constants.LAW_ANIMAL_RESEARCH].alignment == Alignment.ELITE_LIBERAL)
                            {
                                text += "from ";
                                switch (mc.LCSRandom(5))
                                {
                                    case 0: text += "Russia"; break;
                                    case 1: text += "North Korea"; break;
                                    case 2: text += "Cuba"; break;
                                    case 3: text += "Iran"; break;
                                    case 4: text += "China"; break;
                                }
                            }
                            else text += "here ";
                            text += "report that they have discovered an amazing new wonder drug. Called ";
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += mc.swearFilter("Anal", "Bum-Bum"); break;
                                case 1: text += "Colo"; break;
                                case 2: text += "Lacta"; break;
                                case 3: text += "Pur"; break;
                                case 4: text += "Loba"; break;
                            }
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "nephrin"; break;
                                case 1: text += "tax"; break;
                                case 2: text += "zac"; break;
                                case 3: text += "thium"; break;
                                case 4: text += "drene"; break;
                            }
                            text += ", the drug apparently ";
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "boosts intelligence in chimpanzees"; break;
                                case 1: text += mc.swearFilter("corrects erectile dysfunction in chimpanzees", "helps chimpanzees reproduce"); break;
                                case 2: text += "allows chimpanzees to move blocks with their minds"; break;
                                case 3: text += "allows chimpanzees to fly short distances"; break;
                                case 4: text += "increases the attention span of young chimpanzees"; break;
                            }
                            text += ".\n\tAlong with bonobos, chimpanzees are our closest cousins. Fielding questions about the ethics of their experiments from reporters during a press conference yesterday,  spokesperson for the research team stated that, \"It really isn't so bad as all that. Chimpanzees are very resilient creatures. ";
                            switch (mc.LCSRandom(3))
                            {
                                case 0: text += "The ones that survived are all doing very well"; break;
                                case 1: text += "They hardly notice when you drill their brains out, if you're fast"; break;
                                case 2: text += "When we started muffling the screams of our subjects, the other chimps all calmed down quite a bit"; break;
                            }
                            text += ". We have a very experienced research team. While we understand your concerns, any worries are entirely unfounded. I think the media should be focusing on the enormous benefits of this drug.\"\n\tThe first phase of human trials is slated to begin in a few months.";
                        }
                        break;
                    case Constants.VIEW_POLICE:
                        if (story.positive)
                        {
                            story.headline = "POLICE BRUTALITY";

                            text += "Police in Los Angeles were once again filmed beating a minority suspect before taking them into custody.";
                        }
                        else
                        {
                            story.headline = mc.swearFilter("BASTARDS", "JERKS");

                            text += "A major terrorist attack struck ";

                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "New York"; break;
                                case 1: text += "Los Angeles"; break;
                                case 2: text += "Philadelphia"; break;
                                case 3: text += "Miami"; break;
                                case 4: text += "Chicago"; break;
                            }

                            text += " today, leaving " + MasterController.NumberToWords((90 + mc.LCSRandom(50))).ToLower() + " dead, in the worst attack on US soil since 9/11";
                        }
                        break;
                    case Constants.VIEW_PRISONS:
                        if (story.positive)
                        {
                            story.headline = "ON THE INSIDE";

                            string prisonerFirstName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.NEUTRAL);
                            string prisonerLastName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);

                            text += "A former prisoner has written a book describing in horrifying detail what goes on behind bars. Although popular culture has used, or perhaps overused, the prison theme lately in its offerings for mass consumption, rarely have these works been as poignant as " + prisonerFirstName + " " + prisonerLastName + "'s new tour-de-force, <i>";
                            switch (mc.LCSRandom(6))
                            {
                                case 0: text += "Nightmare "; break;
                                case 1: text += "Primal "; break;
                                case 2: text += "Animal "; break;
                                case 3: text += "American "; break;
                                case 4: text += "Solitary "; break;
                                case 5: text += "Painful "; break;
                            }
                            switch (mc.LCSRandom(8))
                            {
                                case 0: text += "Packer"; break;
                                case 1: text += "Soap"; break;
                                case 2: text += "Punk"; break;
                                case 3: text += "Kid"; break;
                                case 4: text += "Cell"; break;
                                case 5: text += "Shank"; break;
                                case 6: text += "Lockdown"; break;
                                case 7: text += "Shower"; break;
                            }
                            text += "</i>.\n\tTake this excerpt, \"The steel bars grated forward in their rails, coming to a halt with a deafening clang that said it all. I was trapped with them now. There were three, looking me over with dark glares of bare lust, as football players might stare at a stupefied, drunken, helpless teenager. My shank's under the mattress. Better to be brave and fight or chicken out and let them take it? Maybe lose an eye the one way, maybe catch ";
                            if (MasterController.government.laws[Constants.LAW_GAY].alignment == Alignment.ARCHCONSERVATIVE) text += "GRIDS";
                            else text += "AIDS";
                            text += " the other. A " + mc.swearFilter("helluva", "difficult") + " choice, and I would only have a few seconds before they made it for me.\"";
                        }
                        else
                        {
                            story.headline = "HOSTAGE SLAIN";

                            string locationName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);
                            CreatureInfo.CreatureGender guardGender = mc.LCSRandom(2) == 0 ? CreatureInfo.CreatureGender.MALE : CreatureInfo.CreatureGender.FEMALE;
                            CreatureInfo.CreatureGender prisonerGender = mc.LCSRandom(2) == 0 ? CreatureInfo.CreatureGender.MALE : CreatureInfo.CreatureGender.FEMALE;
                            string guardFirstName = Factories.CreatureFactory.generateGivenName(guardGender);
                            string guardLastName = Factories.CreatureFactory.generateSurname(guardGender);
                            string prisonerFirstName = Factories.CreatureFactory.generateGivenName(prisonerGender);
                            string prisonerLastName = Factories.CreatureFactory.generateSurname(prisonerGender);

                            text += "The hostage crisis at the " + locationName + " Correctional Facility ended tragically yesterday with the death of both the prison guard being held hostage and " + (guardGender == CreatureInfo.CreatureGender.MALE ? "his" : "her") + " captor.\n\t";
                            text += "Two weeks ago, convicted " + mc.swearFilter("rapist", "reproduction fiend") + " " + prisonerFirstName + " " + prisonerLastName + ", an inmate at " + locationName + ", overpowered " + guardFirstName + " " + guardLastName + "  and barricaded " + (prisonerGender == CreatureInfo.CreatureGender.MALE ? "himself" : "herself") + " with the guard in a prison tower. Authorities locked down the prison and attempted to negotiate by phone for " + (5 + mc.LCSRandom(18)) + " days, but talks were cut short when " + prisonerLastName + " reportedly screamed into the receiver \"";
                            switch (mc.LCSRandom(4))
                            {
                                case 0:
                                    if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ELITE_LIBERAL)
                                        text += "Ah, fuck this shit. This punk bitch is fuckin' dead!";
                                    else if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE)
                                        text += "Ah, [no way.] This [police officer will be harmed!]";
                                    else
                                        text += "Ah, f*ck this sh*t. This punk b*tch is f*ckin' dead!";
                                    break;
                                case 1:
                                    if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ELITE_LIBERAL)
                                        text += "Fuck a muthafuckin' bull. I'm killin' this pig shit.";
                                    else if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE)
                                        text += "[Too late. I am going to harm this police officer.]";
                                    else
                                        text += "F*ck a m*th*f*ck*n' bull. I'm killin' this pig sh*t.";
                                    break;
                                case 2:
                                    if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ELITE_LIBERAL)
                                        text += "Why the fuck am I talkin' to you? I'd rather kill this pig.";
                                    else if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE)
                                        text += "Why [am I] talkin' to you? I'd rather [harm this police officer.]";
                                    else
                                        text += "Why the f*ck am I talkin' to you? I'd rather kill this pig.";
                                    break;
                                case 3:
                                    if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ELITE_LIBERAL)
                                        text += "Imma kill all you bitches, startin' with this mothafucker here.";
                                    else if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE)
                                        text += "[I will harm all police officers], startin' with this [one] here.";
                                    else
                                        text += "Imma kill all you b*tches, startin' with this m*th*f*ck*r here.";
                                    break;
                            }
                            text += "\" The tower was breached in an attempt to reach the hostage, but " + prisonerLastName + " had already " + mc.swearFilter("killed", "harmed") + " the guard. The prisoner was " + mc.swearFilter("beaten to death", "also harmed") + " while \"resisting capture\", according to a prison spokesperson.";
                        }
                        break;
                    case Constants.VIEW_INTELLIGENCE:
                        if (story.positive)
                        {
                            story.headline = "THE FBI FILES";

                            text += "The FBI might be keeping tabs on you. This newspaper yesterday received a collection of files from a source in the Federal Bureau of Investigations. The files contain information on which people have been attending demonstrations, organizing unions, working for liberal organizations even ";
                            switch (mc.LCSRandom(2))
                            {
                                case 0: text += "buying music with 'Explicit Lyrics' labels."; break;
                                case 1: text += "helping homeless people"; break;
                            }
                            text += ".\nMore disturbingly, the files make reference to a plan to \"deal with the undesirables\", although this phrase is not clarified.\n\tThe FBI refused to comment initially, but when confronted with the information, a spokesperson stated, \"Well, you know, there's privacy, and there's privacy. It might be a bit presumptive to assume that these files deal with the one and not the other. You think about that before you continue slanging accusations.\"";
                        }
                        else
                        {
                            story.headline = "DODGED BULLET";

                            text += "The CIA announced yesterday that it has averted a terror attack that would have occurred on American soil. According to a spokesperson for the agency, ";
                            switch (mc.LCSRandom(3))
                            {
                                case 0: text += "white supremacists"; break;
                                case 1: text += "Islamic fundamentalists"; break;
                                case 2: text += "outcast goths from a suburban high school"; break;
                            }
                            text += " planned to ";
                            switch (mc.LCSRandom(9))
                            {
                                case 0: text += mc.swearFilter("fly", "land") + " planes " + mc.swearFilter("into skyscrapers", "on apartment buildings"); break;
                                case 1: text += mc.swearFilter("detonate a", "put") + " fetilizer " + mc.swearFilter("bomb", "on plants") + " at a federal building"; break;
                                case 2: text += mc.swearFilter("ram a motorboat loaded with explosives into", "show up uninvited on") + " a warship"; break;
                                case 3: text += mc.swearFilter("detonate explosives on a school bus", "give children owies and boo-boos"); break;
                                case 4: text += mc.swearFilter("blow out a section of", "caues a traffic jam on") + " a major bridge"; break;
                                case 5: text += mc.swearFilter("kidnap", "take") + " the president" + mc.swearFilter("", " on vacation"); break;
                                case 6: text += mc.swearFilter("assassinate", "hurt") + " the president"; break;
                                case 7: text += mc.swearFilter("destroy", "vandalize") + " the Capitol Building"; break;
                                case 8: text += "detonate" + mc.swearFilter("a nuclear bomb", "fireworks") + " in New York"; break;
                            }
                            text += ". However, intelligence garnered from deep within the mysterious terrorist organization allowed the plot to be foiled just days before it was to occur.\n\t";
                            text += "The spokesperson further stated, \"I won't compromise our sources and methods, but let me just say that we are grateful to the Congress and this Administration for providing us with the tools we need to neutralize these enemies of civilization before they can destroy American families. However, let me also say that there's more that needs to be done. The Head of the Agency will be sending a request to Congress for what we feel are the essential tools for combating terrorism in this new age.\"";
                        }
                        break;
                    case Constants.VIEW_FREE_SPEECH:
                        if (story.positive)
                        {
                            story.headline = "BOOK BANNED";

                            string bookFirstName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.NEUTRAL);
                            string bookLastName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);
                            string authorFirstInitial = (char)('A' + mc.LCSRandom(26)) + "";
                            string authorSecondInitial = (char)('A' + mc.LCSRandom(26)) + "";
                            string authorLastName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);

                            text += "A children's story has been removed from libraries here after the city bowed to pressure from religious groups.\n\tThe book, ";
                            text += "<i>" + bookFirstName + " " + bookLastName + " and the ";
                            switch (mc.LCSRandom(7))
                            {
                                case 0: text += "Mysterious"; break;
                                case 1: text += "Magical"; break;
                                case 2: text += "Golden"; break;
                                case 3: text += "Invisible"; break;
                                case 4: text += "Wondrous"; break;
                                case 5: text += "Amazing"; break;
                                case 6: text += "Secret"; break;
                            }
                            text += " ";
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "Thing"; break;
                                case 1: text += "Stuff"; break;
                                case 2: text += "Object"; break;
                                case 3: text += "Whatever"; break;
                                case 4: text += "Something"; break;
                            }
                            text += "</i>, is the third in an immensely popular series by ";
                            switch (mc.LCSRandom(11))
                            {
                                case 0: text += "British"; break;
                                case 1: text += "Indian"; break;
                                case 2: text += "Chinese"; break;
                                case 3: text += "Rwandan"; break;
                                case 4: text += "Palestinian"; break;
                                case 5: text += "Egyptian"; break;
                                case 6: text += "French"; break;
                                case 7: text += "German"; break;
                                case 8: text += "Iraqi"; break;
                                case 9: text += "Bolivian"; break;
                                case 10: text += "Columbian"; break;
                            }
                            text += " author " + authorFirstInitial + "." + authorSecondInitial + ". " + authorLastName;
                            text += ". Although the series is adored by children worldwide, some conservatives feel that the books ";
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "glorify Satan worship and are spawned by demons from the pit. "; break;
                                case 1: text += "teach children to kill their parents and hate life. "; break;
                                case 2: text += "cause violence in schools and are a gateway to cocaine use. "; break;
                                case 3: text += "breed demonic thoughts that manifest themselves as dreams of murder. "; break;
                                case 4: text += "contain step-by-step instructions to summon the Prince of Darkness. "; break;
                            }
                            text += "In their complaint, the groups cited an incident involving ";
                            switch (mc.LCSRandom(3))
                            {
                                case 0: text += "a child that swore in class"; break;
                                case 1: text += "a child that said a magic spell at her parents"; break;
                                case 2:
                                    text += "a child that ";
                                    switch (mc.LCSRandom(5))
                                    {
                                        case 0: text += "pushed "; break;
                                        case 1: text += "hit "; break;
                                        case 2: text += "slapped "; break;
                                        case 3: text += "insulted "; break;
                                        case 4: text += "tripped "; break;
                                    }
                                    switch (mc.LCSRandom(2))
                                    {
                                        case 0: text += "his "; break;
                                        case 1: text += "her "; break;
                                    }
                                    switch (mc.LCSRandom(3))
                                    {
                                        case 0: text += "older "; break;
                                        case 1: text += "younger "; break;
                                        case 2: text += "twin "; break;
                                    }
                                    switch (mc.LCSRandom(2))
                                    {
                                        case 0: text += "brother"; break;
                                        case 1: text += "sister"; break;
                                    }
                                    break;
                            }
                            text += " as key evidence of the dark nature of the book.\n\tWhen the decision to ban the book was announced yesterday, many area children spontaneously broke into tears. One child was heard saying, \"";
                            switch (mc.LCSRandom(2))
                            {
                                case 0: text += "Mamma, is " + bookFirstName + " dead?"; break;
                                case 1: text += "Mamma, why did they kill " + bookFirstName + "?"; break;
                            }
                        }
                        else
                        {
                            story.headline = "HATE RALLY";

                            text += "The Klan marched in Washington D.C. today. Local authorities refused to do anything to shut them down, citing free speech protections.";
                        }
                        break;
                    case Constants.VIEW_GENETICS:
                        if (story.positive)
                        {
                            story.headline = "KILLER FOOD";

                            text += "What if potatoes had actual eyes? Genetically modified food could lead to just such an outcome.";
                        }
                        else
                        {
                            story.headline = "GM FOOD FAIRE";

                            text += "The genetic foods industry staged a major event here yesterday to showcase its upcoming products. Over thirty companies set up booths and gave talks to wide-eyed onlookers.\n\tOne such corporation, ";
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "Altered "; break;
                                case 1: text += "Gene-tech "; break;
                                case 2: text += "DNA "; break;
                                case 3: text += "Proteomic "; break;
                                case 4: text += "Genomic "; break;
                            }
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "Foods"; break;
                                case 1: text += "Agriculture"; break;
                                case 2: text += "Meals"; break;
                                case 3: text += "Farming"; break;
                                case 4: text += "Living"; break;
                            }
                            text += ", presented their product, \"";
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "Mega "; break;
                                case 1: text += "Epic "; break;
                                case 2: text += "Overlord "; break;
                                case 3: text += "Franken "; break;
                                case 4: text += "Transcendent "; break;
                            }
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "Rice"; break;
                                case 1: text += "Beans"; break;
                                case 2: text += "Corn"; break;
                                case 3: text += "Wheat"; break;
                                case 4: text += "Potatoes"; break;
                            }
                            text += "\", during an afternoon PowerPoint presentation. According to the public relations representative speaking, this amazing new product actually ";
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "extends human life by a few minutes every bite"; break;
                                case 1: text += "mends split-ends upon digestion. Hair is also made glossier and thicker"; break;
                                case 2: text += "allows people to see in complete darkness"; break;
                                case 3: text += "causes a person to slowly attain their optimum weight with repeated use"; break;
                                case 4: text += "cures the common cold"; break;
                            }
                            text += ".\n\tSpokespeople for the GM corporations were universal in their dismissal of the criticism which often follows the industry. One in particular said, \"Look, these products are safe. That thing about the ";
                            switch (mc.LCSRandom(4))
                            {
                                case 0: text += "guy going on a killing spree"; break;
                                case 1: text += "gal turning blue and exploding"; break;
                                case 2: text += "guy speaking in tongues and worshiping Satan"; break;
                                case 3: text += "gal having a ruptured intestine"; break;
                            }
                            text += " is just a load of ";
                            switch (mc.LCSRandom(4))
                            {
                                case 0: text += mc.swearFilter("horseshit", "hooey"); break;
                                case 1: text += mc.swearFilter("bullshit", "poppycock"); break;
                                case 2: text += mc.swearFilter("shit", "horse radish"); break;
                                case 3: text += mc.swearFilter("crap", "garbage"); break;
                            }
                            text += ". Would we stake the reputation of our company on unsafe products? No. That's just ridiculous. I mean, sure companies have put unsafe products out, but the GM industry operates at a higher ethical standard. That goes without saying.\"";
                        }
                        break;
                    case Constants.VIEW_JUSTICES:
                        if (story.positive)
                        {
                            story.headline = "IN CONTEMPT";

                            string judgeFirstName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.WHITEMALEPATRIARCH);
                            string judgeLastName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.WHITEMALEPATRIARCH);
                            string prostituteFirstName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.NEUTRAL);
                            string prostituteLastName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);

                            text += "Conservative federal judge " + judgeFirstName + " " + judgeLastName + " has resigned in disgrace after being caught with a " + mc.swearFilter("prostitute", "civil servant") + ".";
                            text += " " + judgeLastName + " who once ";
                            switch (mc.LCSRandom(2))
                            {
                                case 0: text += "defied the federal government by putting a Ten Commandments monument in the local federal building"; break;
                                case 1: text += "stated that, \"Segregation wasn't the bad idea everybody makes it out to be these days\""; break;
                            }
                            text += " was found with " + prostituteFirstName + " " + prostituteLastName + " last week in a hotel during a police sting operation. According to sources familiar with the particulars, when police broke into the hotel room they saw ";
                            switch (mc.LCSRandom(3))
                            {
                                case 0: text += "\"the most perverse and spine-tingling debauchery imaginable, at least with only two people.\""; break;
                                case 1: text += "The Judge " + mc.swearFilter("relieving himself on", "going to the bathroom in the vicinity of") + " the " + mc.swearFilter("prostitute", "civil servant") + "."; break;
                                case 2: text += "The " + mc.swearFilter("prostitute", "civil servant") + " hollering like a cowboy " + mc.swearFilter("astride", "at a respectful distance from") + " the judge."; break;
                            }
                            text += judgeLastName + " reportedly offered ";
                            switch (mc.LCSRandom(3))
                            {
                                case 0: text += "the arresting officers money"; break;
                                case 1: text += "to let the officers join in"; break;
                                case 2: text += "the arresting officers \"favors\""; break;
                            }
                            text += " in exchange for their silence.\n\t";
                            text += judgeLastName + " could not be reached for comment, although an aid stated that the judge would be going on a Bible retreat for a few weeks to \"Make things right with the Almighty Father.\"";
                        }
                        else
                        {
                            story.headline = "JUSTICE AMOK";

                            string prisonerFirstName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.NEUTRAL);
                            string prisonerMiddleName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.NEUTRAL);
                            string prisonerLastName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);
                            CreatureInfo.CreatureGender judgeGender = (mc.LCSRandom(2) == 0 ? CreatureInfo.CreatureGender.MALE : CreatureInfo.CreatureGender.FEMALE);
                            string judgeFirstName = Factories.CreatureFactory.generateGivenName(judgeGender);
                            string judgeLastName = Factories.CreatureFactory.generateSurname(judgeGender);
                            string victimName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);

                            text += "The conviction of confessed serial killer " + prisonerFirstName + " " + prisonerMiddleName + " " + prisonerLastName + " was overturned by a federal judge yesterday. Justice " + judgeFirstName + " " + judgeLastName + " of the notoriously liberal circuit of appeals here made the decision based on ";
                            switch (mc.LCSRandom(7))
                            {
                                case 0: text += "ten-year-old eyewitness testimony"; break;
                                case 1: text += (judgeGender == CreatureInfo.CreatureGender.MALE ? "his" : "her") + " general feeling about police corruption"; break;
                                case 2: text += (judgeGender == CreatureInfo.CreatureGender.MALE ? "his" : "her") + " belief that the crimes were a vast right-wing conspiracy"; break;
                                case 3: text += (judgeGender == CreatureInfo.CreatureGender.MALE ? "his" : "her") + " belief that " + prisonerLastName + " deserved another chance"; break;
                                case 4: text += (judgeGender == CreatureInfo.CreatureGender.MALE ? "his" : "her") + " personal philosophy of liberty"; break;
                                case 5: text += (judgeGender == CreatureInfo.CreatureGender.MALE ? "his" : "her") + " close personal friendship with the " + prisonerLastName + " family"; break;
                                case 6: text += (judgeGender == CreatureInfo.CreatureGender.MALE ? "his" : "her") + " consultations with a Magic 8-Ball"; break;
                            }
                            text += ", despite the confession of " + prisonerLastName + ", which even Justice " + judgeLastName + " grants was not coerced in any way.\n\tTen years ago, " + prisonerLastName + " was convicted of the now-infamous " + victimName + " slayings. After an intensive manhunt, " + prisonerLastName + " was found with the murder weapon, covered in the victims' blood. " + prisonerLastName + " confessed and was sentenced to life, saying \"Thank you for saving me from myself. If I were to be released, I would surely kill again.\"\n\tA spokesperson for the district attorney has stated that the case will not be retried, due to the current economic doldrums that have left the state completely strapped for cash.";
                        }
                        break;
                    case Constants.VIEW_SWEATSHOPS:
                        if (story.positive)
                        {
                            story.headline = "CHILD'S PLEA";

                            text += "A hidden message was discovered sewn into the lining of a jacket discovered at a fast fashion outlet today, detailing horrific conditions at the sweatshop where it was made and that the worker is only 12 years old.";
                        }
                        else
                        {
                            story.headline = "THEY ARE HERE";

                            text += "The new fall fashions are here! Get the latest styles for cheaper than ever before.";
                        }
                        break;
                    case Constants.VIEW_POLLUTION:
                        if (story.positive)
                        {
                            story.headline = "RING OF FIRE";

                            text += "The east river caught on fire again today.";
                        }
                        else
                        {
                            story.headline = "LOOKING UP";

                            text += "Pollution might not be so bad after all. The ";
                            switch (mc.LCSRandom(6))
                            {
                                case 0: text += "American "; break;
                                case 1: text += "United "; break;
                                case 2: text += "Patriot "; break;
                                case 3: text += "Family "; break;
                                case 4: text += "Children's "; break;
                                case 5: text += "National "; break;
                            }
                            switch (mc.LCSRandom(6))
                            {
                                case 0: text += "Heritage "; break;
                                case 1: text += "Enterprise "; break;
                                case 2: text += "Freedom "; break;
                                case 3: text += "Liberty "; break;
                                case 4: text += "Charity "; break;
                                case 5: text += "Equality "; break;
                            }
                            switch (mc.LCSRandom(6))
                            {
                                case 0: text += "Partnership"; break;
                                case 1: text += "Institute"; break;
                                case 2: text += "Consortium"; break;
                                case 3: text += "Forum"; break;
                                case 4: text += "Center"; break;
                                case 5: text += "Association"; break;
                            }
                            text += " recently released a wide-ranging report detailing recent trends and the latest science on the issue. Among the most startling of the think tank's findings is that ";
                            switch (mc.LCSRandom(6))
                            {
                                case 0: text += "a modest intake of radioactive waste"; break;
                                case 1: text += "a healthy dose of radiation"; break;
                                case 2: text += "a bath in raw sewage"; break;
                                case 3: text += "watching animals die in oil slicks"; break;
                                case 4: text += "inhaling carbon monoxide"; break;
                                case 5: text += "drinking a cup of fracking fluid a day"; break;
                            }
                            text += " might actually ";
                            switch (mc.LCSRandom(6))
                            {
                                case 0: text += "purify the soul"; break;
                                case 1: text += "increase test scores"; break;
                                case 2: text += "increase a child's attention span"; break;
                                case 3: text += "make children behave better"; break;
                                case 4: text += "make shy children fit in"; break;
                                case 5: text += "cure everything from abdominal ailments to zygomycosis"; break;
                            }
                            text += ".\n\tWhen questioned about the science behind these results, a spokesperson stated that, \"";
                            switch (mc.LCSRandom(4))
                            {
                                case 0: text += "Research is complicated, and there are always two ways to think about things"; break;
                                case 1: text += "The jury is still out on pollution.  You really have to keep an open mind"; break;
                                case 2: text += "They've got their scientists, and we have ours.  The issue of pollution is wide open as it stands today"; break;
                                case 3: text += "I just tried it myself and I feel like a million bucks!  *Coughs up blood*  I'm OK, that's just ketchup"; break;
                            }
                            text += ". You have to realize that ";
                            switch (mc.LCSRandom(4))
                            {
                                case 0: text += "the elitist liberal media often distorts"; break;
                                case 1: text += "the vast left-wing education machine often distorts"; break;
                                case 2: text += "the fruits, nuts, and flakes of the environmentalist left often distort"; break;
                                case 3: text += "leftists suffering from the mental disorder chemophobia often distort"; break;
                            }
                            text += "these issues to their own advantage. All we've done is introduced a little clarity into the ongoing debate. Why is there contention on the pollution question? It's because there's work left to be done. We should study much more before we urge any action. Society really just needs to take a breather on this one. We don't see why there's such a rush to judgment here.";
                        }
                        break;
                    case Constants.VIEW_CORPORATE_CULTURE:
                        if (story.positive)
                        {
                            story.headline = "BELLY UP";

                            text += "The risky business practices of another major corporation have left thousands out of work.";
                        }
                        else
                        {
                            story.headline = "NEW JOBS";

                            text += "Several major companies have announced at a joint news conference here that they will be expanding their work forces considerably during the next quarter. Over thirty thousand jobs are expected in the first month, with tech giant ";
                            switch (mc.LCSRandom(10))
                            {
                                case 0: text += "Ameri"; break;
                                case 1: text += "Gen"; break;
                                case 2: text += "Oro"; break;
                                case 3: text += "Amelia"; break;
                                case 4: text += "Vivo"; break;
                                case 5: text += "Benji"; break;
                                case 6: text += "Amal"; break;
                                case 7: text += "Ply"; break;
                                case 8: text += "Seli"; break;
                                case 9: text += "Rio"; break;
                            }
                            switch (mc.LCSRandom(10))
                            {
                                case 0: text += "tech"; break;
                                case 1: text += "com"; break;
                                case 2: text += "zap"; break;
                                case 3: text += "cor"; break;
                                case 4: text += "dyne"; break;
                                case 5: text += "bless"; break;
                                case 6: text += "chip"; break;
                                case 7: text += "co"; break;
                                case 8: text += "wire"; break;
                                case 9: text += "rex"; break;
                            }
                            text += " increasing its payrolls by over ten thousand workers alone. Given the state of the economy recently and in light of the tendency of large corporations to export jobs overseas these days, this welcome news is bound to be a pleasant surprise to those in the unemployment lines. The markets reportedly responded to the announcement with mild interest, although the dampened movement might be expected due to the uncertain futures of some of the companies in the tech sector. On the whole, however, analysts suggest that not only does the expansion speak to the health of the tech industry but is also indicative of a full economic recovery.";
                        }
                        break;
                    case Constants.VIEW_CEO_SALARY:
                        if (story.positive)
                        {
                            story.headline = "AMERICAN CEO";
                            string ceoFirstName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.WHITEMALEPATRIARCH);
                            string ceoLastName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.WHITEMALEPATRIARCH);

                            text += "Billionaire CEO " + ceoFirstName + " " + ceoLastName + " ";

                            switch (mc.LCSRandom(10))
                            {
                                case 0:
                                    text += "regularly ";
                                    if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE)
                                    {
                                        if (MasterController.government.laws[Constants.LAW_WOMEN].alignment == Alignment.ARCHCONSERVATIVE)
                                            text += "[visits sperm banks]";
                                        else
                                            text += "visits [working women]";
                                    }
                                    else
                                        text += "visits prostitutes";
                                    break;
                                case 1:
                                    text += "seeks the aid of psychics";
                                    break;
                                case 2:
                                    text += "donated millions to the KKK";
                                    break;
                                case 3:
                                    text += "hasn't paid taxes in over 20 years";
                                    break;
                                case 4:
                                    text += "took out a contract on his wife";
                                    break;
                                case 5:
                                    text += "doesn't know what his company does";
                                    break;
                                case 6:
                                    text += "has a zoo of imported exotic worms";
                                    break;
                                case 7:
                                    text += "paid millions for high-tech bondage gear";
                                    break;
                                case 8:
                                    text += "installed a camera in an office bathroom";
                                    break;
                                case 9:
                                    text += "owns slaves in another country";
                                    break;
                            }

                            text += " according to work done by investigative journalists.";
                        }
                        else
                        {
                            story.headline = "YOUNG BILLIONAIRE";

                            CreatureInfo.CreatureGender billionaireGender = mc.LCSRandom(2) == 0 ? CreatureInfo.CreatureGender.MALE : CreatureInfo.CreatureGender.FEMALE;
                            string billionaireFirstName = Factories.CreatureFactory.generateGivenName(billionaireGender);
                            string billionaireLastName = Factories.CreatureFactory.generateSurname(billionaireGender);

                            text += billionaireFirstName + " " + billionaireLastName + " is America's youngest billionaire, having amassed a personal fortune through " + (billionaireGender == CreatureInfo.CreatureGender.MALE ? "his" : "her") + " ";
                            switch (mc.LCSRandom(3))
                            {
                                case 0: text += "smartphone app"; break;
                                case 1: text += "investment strategy"; break;
                                case 2: text += "web startup"; break;
                            }
                            text += " at an age of only " + (20 + mc.LCSRandom(15)) + " years.";
                        }
                        break;
                    case Constants.VIEW_CABLE_NEWS:
                        if (story.positive)
                        {
                            story.headline = "CABLE NEWS MELTDOWN";

                            CreatureInfo.CreatureGender punditGender = mc.LCSRandom(2) == 0 ? CreatureInfo.CreatureGender.MALE : CreatureInfo.CreatureGender.FEMALE;
                            string punditFirstName = Factories.CreatureFactory.generateGivenName(punditGender);
                            string punditLastName = Factories.CreatureFactory.generateSurname(punditGender);

                            text += "A prominent Liberal stumped cable news pundit " + punditFirstName + " " + punditLastName + " on " + (punditGender == CreatureInfo.CreatureGender.MALE ? "his":"her") +" show ";
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "Cross"; break;
                                case 1: text += "Hard"; break;
                                case 2: text += "Lightning"; break;
                                case 3: text += "Washington"; break;
                                case 4: text += "Capital"; break;
                            }

                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += " Fire"; break;
                                case 1: text += " Ball"; break;
                                case 2: text += " Talk"; break;
                                case 3: text += " Insider"; break;
                                case 4: text += " Gang"; break;
                            }

                            text += " last night, prompting the popular host to respond with a five minute, expletive-laden rant, claiming 'smug Liberals think that Americans care about real logic' and that Americans would 'think what I tell them think!'";
                        }
                        else
                        {
                            story.headline = "THE NEW NETWORK";

                            text += "A new 24 hour news network has debuted, to the excitement of many Conservative viewers who feel that they finally have a voice in the Liberally dominated media.";
                        }
                        break;
                    case Constants.VIEW_AM_RADIO:
                        if (story.positive)
                        {
                            story.headline = "AM IMPLOSION";

                            string hostFirstName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.WHITEMALEPATRIARCH);
                            string hostLastName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.WHITEMALEPATRIARCH);
                            string fanFirstName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.NEUTRAL);
                            string fanLastName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.NEUTRAL);

                            text += "Well-known AM radio personality " + hostFirstName + " " + hostLastName + " went off for fifteen minutes in an inexplicable rant two nights ago during the syndicated radio program \"";
                            switch (mc.LCSRandom(3))
                            {
                                case 0: text += "Straight "; break;
                                case 1: text += "Real "; break;
                                case 2: text += "True "; break;
                            }
                            switch (mc.LCSRandom(3))
                            {
                                case 0: text += "Talk\"."; break;
                                case 1: text += "Chat\"."; break;
                                case 2: text += "Discussion\"."; break;
                            }
                            text += hostLastName + "'s monologue for the evening began the way that fans had come to expect, with attacks on the \"liberal media establishment\" and the \"elite liberal agenda\". But when the radio icon said, \"";
                            switch (mc.LCSRandom(4))
                            {
                                case 0: text += "and the Grays are going to take over the planet in the End Times"; break;
                                case 1: text += "a liberal chupacabra will suck the blood from us like a goat, a goat!, a goat!"; break;
                                case 2: text += "I feel translucent rods passing through my body...  it's like making love to the future"; break;
                                case 3:
                                    text += "and the greatest living example of a reverse racist is the ";
                                    if (MasterController.government.president.getComponent<Politician>().party != Alignment.CONSERVATIVE)
                                        text += "current president!";
                                    else
                                        text += "liberal media establishment!";
                                    break;

                            }
                            text += "\", a former fan of the show, " + fanFirstName + " " + fanLastName + ", knew that \"";
                            switch (mc.LCSRandom(3))
                            {
                                case 0: text += "my old hero"; break;
                                case 1: text += "my old idol"; break;
                                case 2: text += "the legend"; break;
                            }
                            text += " had ";
                            switch (mc.LCSRandom(3))
                            {
                                case 0: text += "lost his " + mc.swearFilter("goddamn", "gosh darn") + " mind"; break;
                                case 1: text += "maybe gone a little off the deep end"; break;
                                case 2: text += "probably been listening to Art Bell in the next studio a little too long"; break;
                            }
                            text += ". After that, it just got worse and worse.\n\t" + hostLastName + " issued an apology later in the program, but the damage might already be done. According to a poll completed yesterday, fully half of the host's most loyal supporters have decided to leave the program for saner pastures. Of these, many said that they would be switching over to the FM band.";
                        }
                        else
                        {
                            story.headline = "THE DEATH OF CULTURE";

                            string hostFirstName = Factories.CreatureFactory.generateGivenName(CreatureInfo.CreatureGender.WHITEMALEPATRIARCH);
                            string hostLastName = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.WHITEMALEPATRIARCH);

                            text += "Infamous FM radio shock jock " + hostFirstName + " " + hostLastName + " has brought radio entertainment to a new low. During yesterday's broadcast of the program \"" + hostLastName + "'s ";
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "Morning "; break;
                                case 1: text += "Commuter "; break;
                                case 2: text += "Jam "; break;
                                case 3: text += "Talk "; break;
                                case 4: text += "Radio "; break;
                            }
                            switch (mc.LCSRandom(5))
                            {
                                case 0: text += "Swamp"; break;
                                case 1: text += "Jolt"; break;
                                case 2: text += "Club"; break;
                                case 3: text += "Show"; break;
                                case 4: text += "Fandango"; break;
                            }
                            text += "\", " + hostLastName + " reportedly ";
                            switch (mc.LCSRandom(5))
                            {
                                case 0:
                                    if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE)
                                        text += "[had consensual intercourse in the missionary position]";
                                    else if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ELITE_LIBERAL)
                                        text += "fucked";
                                    else
                                        text += "had intercourse";
                                    break;
                                case 1:
                                    if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE)
                                        text += "encouraged listeners to call in and [urinate]";
                                    else if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ELITE_LIBERAL)
                                        text += "encouraged listeners to call in and take a piss";
                                    else
                                        text += "encouraged listeners to call in and relieve themselves";
                                    break;
                                case 2:
                                    if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE)
                                        text += "screamed \"[darn] the police those [big dumb jerks]. I got a [stupid] ticket this morning and I'm [so angry].\"";
                                    else if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ELITE_LIBERAL)
                                        text += "screamed \"fuck the police those goddamn motherfuckers. I got a fucking ticket this morning and I'm fucking pissed as shit.\"";
                                    else
                                        text += "screamed \"f*ck the police those g*dd*mn m*th*f*ck*rs. I got a f*cking ticket this morning and I'm f*cking p*ss*d as sh*t.\"";
                                    break;
                                case 3:
                                    if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE &&
                                        MasterController.government.laws[Constants.LAW_WOMEN].alignment == Alignment.ARCHCONSERVATIVE)
                                        text += "[fed] from [an indecent] woman";
                                    if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment != Alignment.ARCHCONSERVATIVE &&
                                        MasterController.government.laws[Constants.LAW_WOMEN].alignment == Alignment.ARCHCONSERVATIVE)
                                        text += "breastfed from an exposed woman";
                                    if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE &&
                                        MasterController.government.laws[Constants.LAW_WOMEN].alignment != Alignment.ARCHCONSERVATIVE)
                                        text += "[fed] from a [woman]";
                                    else
                                        text += "breastfed from a lactating woman";
                                    break;
                                case 4:
                                    text += mc.swearFilter("masturbated", "had fun");
                                    break;
                            }
                            text += " on the air. Although " + hostLastName + " later apologized, the FCC received ";
                            switch (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment)
                            {
                                case Alignment.ARCHCONSERVATIVE: text += "thousands of"; break;
                                case Alignment.CONSERVATIVE: text += "several hundred"; break;
                                case Alignment.MODERATE: text += "hundreds of"; break;
                                case Alignment.LIBERAL: text += "dozens of"; break;
                                case Alignment.ELITE_LIBERAL: text += "some"; break;
                            }
                            text += " complaints from irate listeners ";
                            switch (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment)
                            {
                                case Alignment.ARCHCONSERVATIVE: text += "across the nation. "; break;
                                case Alignment.CONSERVATIVE: text += "from all over the state. "; break;
                                case Alignment.MODERATE: text += "within the county. "; break;
                                case Alignment.LIBERAL: text += "in the neighbouring towns. "; break;
                                case Alignment.ELITE_LIBERAL: text += "within the town. "; break;
                            }
                            text += " A spokesperson for the FCC stated that the incident is under investigation.";
                        }
                        break;
                }
                story.text = text;
            }
        }

        private string parseHeadline(NewsStory story, string headline)
        {
            if (story.location != null)
            {
                if ((GameData.getData().newsTypeList[story.type].flags & NewsTypeDef.NewsTypeFlag.CCS_ACTION) != 0)
                    headline = headline.Replace("$LOCATION", story.location.getComponent<SiteBase>().getCCSName().ToUpper());
                else
                    headline = headline.Replace("$LOCATION", story.location.getComponent<SiteBase>().getCurrentName().ToUpper());
            }
            if (story.subject != null)
            {
                //Only "important" people really get their names in the headlines
                if ((story.subject.getComponent<CreatureBase>().getFlags() & (CreatureDef.CreatureFlag.ARCHCONSERVATIVE | CreatureDef.CreatureFlag.POLICE)) != 0)
                    headline = headline.Replace("$TYPENAME", story.subject.getComponent<CreatureInfo>().type_name.ToUpper());
                else
                    headline = headline.Replace("$TYPENAME", "SOMEONE");
            }

            Regex regex = new Regex("(?<!\\\\)\\$\\w*");

            int i = 0;
            while (regex.IsMatch(headline) && i < 100)
            {
                i++;
                Match match = regex.Match(headline);

                if (match.Value.Contains("$CONDITION"))
                {
                    //Get the full condition
                    match = Regex.Match(headline, "\\$CONDITION{.*?}");
                    char[] trimChars = { '{', '}' };
                    string content = match.Value.Substring(match.Value.IndexOf('{')).Trim(trimChars);
                    string[] sections = content.Split(':');

                    bool conditionValue = false;

                    if (sections[0] == "CRIME")
                    {
                        if (story.crimes.ContainsKey(sections[1]))
                            conditionValue = MasterController.stringToOperator(sections[2], story.crimes[sections[1]], int.Parse(sections[3]));
                        else
                            conditionValue = MasterController.stringToOperator(sections[2], 0, int.Parse(sections[3]));
                    }
                    else if (sections[0] == "LAW")
                    {
                        conditionValue = MasterController.GetMC().testCondition(sections[0] + ":" + sections[1] + ":" + sections[2] + ":" + sections[3]);
                    }
                    else if (sections[0] == "STORY")
                    {
                        switch (sections[1])
                        {
                            case "POSITIVE":
                                conditionValue = MasterController.stringToOperator(sections[2], story.positive, bool.Parse(sections[3]));
                                break;
                            case "CLAIMED":
                                conditionValue = MasterController.stringToOperator(sections[2], story.claimed, bool.Parse(sections[3]));
                                break;
                        }
                    }

                    headline = headline.Replace(match.Value, conditionValue ? sections[4] : sections[5]);
                }
                else
                {
                    string key = match.Value.Substring(1);
                    if (story.crimes.ContainsKey(key))
                        headline = headline.Replace(match.Value, MasterController.NumberToWords(story.crimes[key]).ToLower());
                    else
                        //This shouldn't really come up and should be addressed if it does, but just to avoid the exception
                        headline = headline.Replace(match.Value, "ERR: " + key + " NOT IN CRIME LIST");
                }
            }

            return headline;
        }

        private string parseStory(NewsStory story, bool guardian)
        {
            string baseText = GameData.getData().nationList["USA"].capital + " - ";

            if (guardian && GameData.getData().newsTypeList[story.type].guardianText != "")
                baseText += GameData.getData().newsTypeList[story.type].guardianText;
            else
                baseText += GameData.getData().newsTypeList[story.type].text;

            if ((GameData.getData().newsTypeList[story.type].flags & NewsTypeDef.NewsTypeFlag.CCS_ACTION) != 0)
            {
                if (story.positive && !guardian)
                    baseText = baseText.Replace("$NEWSCHERRY", MasterController.ccs.newsCherry ? "The Conservative Crime Squad has struck again" : "A group of M16-wielding vigilantes calling itself the Conservative Crime Squad burst onto the scene of political activism yesterday, according to a spokesperson from the police department");
                else
                    baseText = baseText.Replace("$NEWSCHERRY", MasterController.ccs.newsCherry ? "The Conservative Crime Squad has gone on a rampage" : "A group of worthless M16-toting hicks calling itself the Conservative Crime Squad went on a rampage yesterday, according to a spokesperson from the police department");
            }
            else
            {
                if (story.positive)
                    baseText = baseText.Replace("$NEWSCHERRY", MasterController.news.newsCherryBusted || guardian ? "The Liberal Crime Squad has struck again" : "A group calling itself the Liberal Crime Squad burst onto the scene of political activism yesterday, according to a spokesperson from the police department");
                else
                {
                    if (!guardian)
                        baseText = baseText.Replace("$NEWSCHERRY", MasterController.news.newsCherryBusted ? "The Liberal Crime Squad has gone on a rampage" : "A group of thugs calling itself the Liberal Crime Squad went on a rampage yesterday, according to a spokesperson from the police department");
                    else
                        baseText = baseText.Replace("$NEWSCHERRY", "A Liberal Crime Squad operation went horribly wrong");
                }
            }
            baseText = baseText.Replace("$GENERATENAME", Factories.CreatureFactory.generateGivenName() + " " + Factories.CreatureFactory.generateSurname());
            baseText = baseText.Replace("$LISTMAJORCRIMES", parseMajorCrimes(story, guardian));
            baseText = baseText.Replace("$LISTMINORCRIMES", parseMinorCrimes(story, guardian));
            if (story.subject != null)
            {
                baseText = baseText.Replace("$SUBJECT", story.subject.getComponent<CreatureInfo>().getName());
                if (story.subject.hasComponent<Hostage>())
                    baseText = baseText.Replace("$DAYSMISSING", story.subject.getComponent<Hostage>().timeInCaptivity + "");
            }
            if (story.location != null)
            {
                if ((GameData.getData().newsTypeList[story.type].flags & NewsTypeDef.NewsTypeFlag.CCS_ACTION) != 0)
                    baseText = baseText.Replace("$LOCATION", story.location.getComponent<SiteBase>().getCCSName());
                else
                    baseText = baseText.Replace("$LOCATION", story.location.getComponent<SiteBase>().getCurrentName());
            }

            Regex regex = new Regex("(?<!\\\\)\\$\\w*");

            int i = 0;
            while (regex.IsMatch(baseText) && i < 100)
            {
                i++;
                Match match = regex.Match(baseText);

                if (match.Value.Contains("$CONDITION"))
                {
                    //Get the full condition
                    match = Regex.Match(baseText, "\\$CONDITION{.*?}");
                    char[] trimChars = { '{', '}' };
                    string content = match.Value.Substring(match.Value.IndexOf('{')).Trim(trimChars);
                    string[] sections = content.Split(':');

                    bool conditionValue = false;

                    if (sections[0] == "CRIME")
                    {
                        if (story.crimes.ContainsKey(sections[1]))
                            conditionValue = MasterController.stringToOperator(sections[2], story.crimes[sections[1]], int.Parse(sections[3]));
                        else
                            conditionValue = MasterController.stringToOperator(sections[2], 0, int.Parse(sections[3]));
                    }
                    else if (sections[0] == "LAW")
                    {
                        conditionValue = MasterController.GetMC().testCondition(sections[0] + ":" + sections[1] + ":" + sections[2] + ":" + sections[3]);
                    }
                    else if (sections[0] == "STORY")
                    {
                        switch (sections[1])
                        {
                            case "POSITIVE":
                                conditionValue = MasterController.stringToOperator(sections[2], story.positive, bool.Parse(sections[3]));
                                break;
                            case "CLAIMED":
                                conditionValue = MasterController.stringToOperator(sections[2], story.claimed, bool.Parse(sections[3]));
                                break;
                        }
                    }

                    baseText = baseText.Replace(match.Value, conditionValue ? sections[4] : sections[5]);
                }
                else
                {
                    string key = match.Value.Substring(1);
                    if (story.crimes.ContainsKey(key))
                        baseText = baseText.Replace(match.Value, MasterController.NumberToWords(story.crimes[key]).ToLower());
                    else
                        //This shouldn't really come up and should be addressed if it does, but just to avoid the exception
                        baseText = baseText.Replace(match.Value, "ERR: " + key + " NOT IN CRIME LIST");
                }
            }

            return baseText;
        }

        private string parseMajorCrimes(News.NewsStory story, bool guardian)
        {
            string majorCrimeString = "";

            foreach (string crime in story.crimes.Keys)
            {
                //Skip mentioning crimes the guardian doesn't want to report on
                if (guardian && GameData.getData().newsActionList[crime].guardiantext == "") continue;

                if ((GameData.getData().newsActionList[crime].flags & NewsActionDef.NewsActionFlag.MAJORCRIME) != 0)
                    majorCrimeString += guardian ? GameData.getData().newsActionList[crime].guardiantext : GameData.getData().newsActionList[crime].text;
            }

            return majorCrimeString;
        }

        private string parseMinorCrimes(News.NewsStory story, bool guardian)
        {
            bool ccsaction = (GameData.getData().newsTypeList[story.type].flags & NewsTypeDef.NewsTypeFlag.CCS_ACTION) != 0;
            string minorCrimeString = "";
            if (guardian && !ccsaction)
                minorCrimeString += " The Liberal Crime Squad ";
            else
                minorCrimeString += " Further details are sketchy, but police sources suggest that the " + (ccsaction ? "CCS" : "LCS") + " engaged in ";
            List<string> minorCrimeList = new List<string>();

            foreach (string crime in story.crimes.Keys)
            {
                if ((GameData.getData().newsActionList[crime].flags & NewsActionDef.NewsActionFlag.MAJORCRIME) == 0)
                {
                    //Skip mentioning crimes the guardian doesn't want to report on (murder, mistakes, etc.)
                    if (GameData.getData().newsActionList[crime].guardiantext == "" && guardian && !ccsaction)
                        continue;
                    else
                        minorCrimeList.Add(crime);
                }
            }

            if (minorCrimeList.Count == 0) return "";

            for (int i = 0; i < minorCrimeList.Count; i++)
            {
                minorCrimeString += (guardian && !ccsaction) ? GameData.getData().newsActionList[minorCrimeList[i]].guardiantext : GameData.getData().newsActionList[minorCrimeList[i]].text;

                if (minorCrimeList.Count - i == 1) minorCrimeString += ".";
                else if (minorCrimeList.Count - i == 2) minorCrimeString += " and ";
                else minorCrimeString += ", ";
            }

            return minorCrimeString;
        }
    }
}
