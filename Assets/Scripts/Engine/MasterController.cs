using System;
using System.Collections.Generic;
using System.Xml;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Location;
using LCS.Engine.Components.Item;
using LCS.Engine.UI;
using LCS.Engine.Scenes;
using LCS.Engine.Data;

namespace LCS.Engine
{
    public class MasterController
    {
        [Flags]
        public enum GameFlags
        {
            NORMAL = 0,
            NIGHTMARE = 1,
            ACTIVE_CCS = 2,
            NO_CCS = 4
        }
        
        [Flags]
        public enum CombatModifiers
        {
            NONE = 0,
            CHASE_FOOT = 1,
            CHASE_CAR = 2,
            NOCHARGES = 4
        }

        public enum Phase
        {
            BASE,
            TROUBLE,
            ACTIVITY,
            POST_ACTIVITY,
            CCS,
            ELECTIONS,
            ADVANCE_DAY,
            NEWS,
            DAILY_CLEANUP
        }

        public enum EndGame
        {
            NONE,
            WON,
            HICKS,
            CIA,
            POLICE,
            CORP,
            REAGAN,
            DEAD,
            PRISON,
            EXECUTED,
            DATING,
            HIDING,
            DISBANDLOSS,
            DISPERSED,
            CCS,
            FIREMEN
        }

        public const int MAX_LOG_SIZE = 200;
        public const string CURRENT_VERSION = "b15";
        private static MasterController mc = null;

        private Random rand;
        public ActionQueue actionQueue { get; set; }

        public bool DebugMode { get; set; }  
        public bool SkillRollDebug { get; set; }     
        public bool forceChase { get; set; } 
        public bool actionStep { get; set; }
        public bool canSeeThings { get; set; }

        public DateTime currentDate { get; set; }
        public Entity worldState { get; set; }
        public int selectedSquadMember { get; set; }
        public GameFlags gameFlags { get; set; }
        public Phase phase { get; set; }
        public List<LogMessage> messageLog { get; set; }
        public List<LogMessage> combatLog { get; set; }
        public List<LogMessage> debugLog { get; set; }

        public UIController uiController { get; set; }

        public CombatModifiers combatModifiers { get; set; }        
        public ChaseScene currentChaseScene { get; set; }
        public SiteModeScene currentSiteModeScene { get; set; }
        public EndGame endGameState { get; set; }

        public Dictionary<long, Entity> PersistentEntityList { get; set; }

        public static Government government
        {
            get
            {
                return GetMC().worldState.getComponent<Government>();
            }
        }
        public static LiberalCrimeSquad lcs
        {
            get
            {
                return GetMC().worldState.getComponent<LiberalCrimeSquad>();
            }
        }
        public static Nation nation
        {
            get
            {
                return GetMC().worldState.getComponent<Nation>();
            }
        }
        public static Public generalPublic
        {
            get
            {
                return GetMC().worldState.getComponent<Public>();
            }
        }

        public static News news
        {
            get
            {
                return GetMC().worldState.getComponent<News>();
            }
        }

        public static ConservativeCrimeSquad ccs
        {
            get
            {
                return GetMC().worldState.getComponent<ConservativeCrimeSquad>();
            }
        }

        public static HighScore highscore
        {
            get
            {
                return GetMC().worldState.getComponent<HighScore>();
            }
        }

        private bool gaylawyer = false;
        private bool makelawyer = false;
        private Entity stolenCar = null;

        private MasterController()
        {
            actionQueue = new ActionQueue(null, "BASENODE");

            rand = new Random();
            gameFlags = 0;
            phase = Phase.BASE;
            //Default start date, this will be changed when a new game is actually started based on the def file
            currentDate = new DateTime(2017, 1, 1);
            PersistentEntityList = new Dictionary<long, Entity>();
            messageLog = new List<LogMessage>();
            combatLog = new List<LogMessage>();
            debugLog = new List<LogMessage>();
            combatModifiers = CombatModifiers.NONE;
            endGameState = EndGame.NONE;
        }

        public static MasterController GetMC()
        {
            if(mc == null)
            {
                mc = new MasterController();
            }

            return mc;
        }

        #region UI
        //Initialize all the actions on the UI objects
        public void initUIElements()
        {
            //Title
            TitlePageActions titlePageActions = new TitlePageActions();
            titlePageActions.newGame = () => { uiController.doInput(initWorld); };
            titlePageActions.loadGame = () => { uiController.doInput(GameData.getData().loadGame); };
            uiController.titlePage.init(titlePageActions);

            //BaseMode
            BaseModeActions baseModeActions = new BaseModeActions();
            baseModeActions.waitADay = () => { uiController.doInput(nextPhase); };
            baseModeActions.nextSquad = () => { uiController.doInput(worldState.getComponent<LiberalCrimeSquad>().nextSquad); };
            baseModeActions.changeSlogan = (string newSlogan) => { uiController.doInput(() => { worldState.getComponent<LiberalCrimeSquad>().slogan = newSlogan; }); };
            baseModeActions.setDestination = (string city, string destination) =>
            {
                uiController.doInput(() =>
                {
                    if (destination != "NONE")
                    {
                        Entity target = nation.cities[city].getComponent<City>().getLocation(destination);
                        LiberalCrimeSquad.Squad squad = worldState.getComponent<LiberalCrimeSquad>().activeSquad;
                        if (target.hasComponent<TroubleSpot>() && 
                            target.hasComponent<SafeHouse>() && 
                            target.getComponent<SafeHouse>().owned && 
                            (target.getComponent<TroubleSpot>().getFlags() & LocationDef.TroubleSpotFlag.SIEGE_ONLY) == 0)
                        {
                            List<PopupOption> options = new List<PopupOption>();
                            options.Add(new PopupOption(getTranslation("BASE_travel_rebase"), () => { squad.travelAction = LiberalCrimeSquad.Squad.TravelAction.BASE; }));
                            options.Add(new PopupOption(getTranslation("BASE_travel_trouble"), () => { squad.travelAction = LiberalCrimeSquad.Squad.TravelAction.TROUBLE; }));
                            options.Add(new PopupOption(getTranslation("BASE_travel_both"), () => { squad.travelAction = LiberalCrimeSquad.Squad.TravelAction.BOTH; }));

                            uiController.showOptionPopup(getTranslation("BASE_travel_rebase_prompt"), options);
                        }
                        else if (target.hasComponent<SafeHouse>() && target.getComponent<SafeHouse>().owned)
                        {
                            squad.travelAction = LiberalCrimeSquad.Squad.TravelAction.BASE;
                        }
                        else
                        {
                            squad.travelAction = LiberalCrimeSquad.Squad.TravelAction.TROUBLE;
                        }
                        worldState.getComponent<LiberalCrimeSquad>().activeSquad.target = target;
                    }
                    else
                        worldState.getComponent<LiberalCrimeSquad>().activeSquad.target = null;
                });
            };            
            uiController.baseMode.init(baseModeActions);

            //Org Management
            OrganizationManagementActions organizationManagementActions = new OrganizationManagementActions();
            organizationManagementActions.quickActivity = (Entity e, string activity) => { uiController.doInput(() => { e.getComponent<Liberal>().quickSetActivity(activity); }); };
            organizationManagementActions.selectChar = (Entity e) => {
                uiController.doInput(() =>
                {
                    uiController.hideUI();
                    uiController.charInfo.show(e);
                });
            };
            uiController.organizationManagement.init(organizationManagementActions);

            //Safehouse Management
            SafeHouseManagementActions safeHouseManagementActions = new SafeHouseManagementActions();
            safeHouseManagementActions.upgrade = (Entity safehouse, string upgrade) => { uiController.doInput(() => { safehouse.getComponent<SafeHouse>().applyUpgrade(upgrade); }); };
            safeHouseManagementActions.buyRations = (Entity safehouse) => { uiController.doInput(safehouse.getComponent<SafeHouse>().buyFood); };
            safeHouseManagementActions.buyFlag = (Entity safehouse) => { uiController.doInput(safehouse.getComponent<SafeHouse>().buyFlag); };
            safeHouseManagementActions.burnFlag = (Entity safehouse) => { uiController.doInput(safehouse.getComponent<SafeHouse>().burnFlag); };
            safeHouseManagementActions.selectChar = (Entity e) =>
            {
                uiController.doInput(() =>
                {
                    uiController.hideUI();
                    uiController.charInfo.show(e);
                });
            };
            safeHouseManagementActions.giveUpSiege = (Entity safeHouse) => { uiController.doInput(() => { safeHouse.getComponent<SafeHouse>().giveUpSiege(); }); };
            safeHouseManagementActions.escapeEngageSiege = (Entity safeHouse) => { uiController.doInput(() => { safeHouse.getComponent<SafeHouse>().escapeEngage(); }); };
            uiController.safeHouseManagement.init(safeHouseManagementActions);

            //Squad
            SquadActions squadActions = new SquadActions();
            squadActions.selectChar = (Entity e) => 
            {
                uiController.doInput(() =>
                {
                    uiController.hideUI();
                    uiController.charInfo.show(e);
                });
            };
            uiController.squadUI.init(squadActions);

            //Enemy
            SquadActions enemyUIActions = new SquadActions();
            enemyUIActions.selectChar = (Entity e) =>
            {
                uiController.doInput(() =>
                {
                    uiController.hideUI();
                    uiController.charInfo.show(e);
                });
            };
            uiController.enemyUI.init(enemyUIActions);

            //CharInfo
            CharInfoActions charInfoActions = new CharInfoActions();
            charInfoActions.changeGender = (Entity e) => { uiController.doInput(e.getComponent<CreatureInfo>().changeGender); };
            charInfoActions.setAlias = (Entity e, string alias) => { uiController.doInput(() => { e.getComponent<CreatureInfo>().alias = alias; }); };
            charInfoActions.setActivity = (Entity e, string activity) =>
            {
                uiController.doInput(() =>
                {
                    if (activity.StartsWith("LEARN") || activity.StartsWith("MAKE_CLOTHING") || activity.StartsWith("STEAL_VEHICLE"))
                    {
                        e.getComponent<Liberal>().setActivity(activity.Split(' ')[0], activity.Split(' ')[1]);
                    }
                    else
                    {
                        e.getComponent<Liberal>().setActivity(activity);
                    }
                });
            };
            charInfoActions.setActivityInterrogate = (Entity e, Entity target) => { uiController.doInput(() => { e.getComponent<Liberal>().setActivity("INTERROGATE", null, target); }); };
            charInfoActions.toggleInterrogationTactic = (Entity e, Hostage.Tactics tactic) => { uiController.doInput(() => { e.getComponent<Hostage>().toggleTactic(tactic); }); };
            charInfoActions.setSquad = (Entity e, LiberalCrimeSquad.Squad squad) => { uiController.doInput(() => { e.getComponent<Liberal>().changeSquad(squad); }); };
            charInfoActions.newSquad = (Entity e, string squadName) =>
            {
                if (squadName == "") return null;

                LiberalCrimeSquad.Squad newSquad = worldState.getComponent<LiberalCrimeSquad>().newSquad(squadName);
                newSquad.homeBase = e.getComponent<Liberal>().homeBase;

                return newSquad;
            };
            charInfoActions.moveBase = (Entity e, Entity target) => { uiController.doInput(() => { e.getComponent<Liberal>().targetBase = target; }); };
            charInfoActions.back = () =>
            {
                uiController.doInput(() =>
                {
                    uiController.showUI();
                });
            };
            charInfoActions.reload = (Entity e) => { uiController.doInput(() => 
            {
                bool combatReload = false;
                if ((currentSiteModeScene != null && currentSiteModeScene.inEncounter) || currentChaseScene != null) combatReload = true;
                if (!e.getComponent<Inventory>().getWeapon().getComponent<Weapon>().fullAmmo())
                    e.getComponent<Inventory>().reload(combatReload);
            }); };
            charInfoActions.fireLiberal = (Entity e) => { uiController.doInput(() =>
            {
                e.getComponent<Liberal>().fireLiberal();
            }); };
            uiController.charInfo.init(charInfoActions);

            //Chase
            ChaseActions chaseActions = new ChaseActions();
            chaseActions.run = () => { uiController.doInput(currentChaseScene.runForIt); };
            chaseActions.fight = () => { uiController.doInput(currentChaseScene.fight); };
            chaseActions.surrender = () => { uiController.doInput(currentChaseScene.surrender); };
            chaseActions.driveEscape = () => { uiController.doInput(currentChaseScene.driveEscape); };
            chaseActions.bail = () => { uiController.doInput(currentChaseScene.bail); };
            chaseActions.driveObstacleRisky = () => { uiController.doInput(currentChaseScene.driveObstacleRisky); };
            chaseActions.driveObstacleSafe = () => { uiController.doInput(currentChaseScene.driveObstacleSafe); };
            chaseActions.advance = () =>
            {
                uiController.doInput(() =>
                {
                    if (currentChaseScene.chasePhase == ChaseScene.ChasePhase.RUN
                    || currentChaseScene.chasePhase == ChaseScene.ChasePhase.FIGHT
                    || currentChaseScene.chasePhase == ChaseScene.ChasePhase.CONCLUSION)
                    {
                        doNextAction();
                    }
                });
            };
            uiController.chase.init(chaseActions);

            //Trial
            TrialActions trialActions = new TrialActions();
            trialActions.selection = (Entity e, TrialActions.TrialSelection selection) => { uiController.doInput(() => { e.getComponent<CriminalRecord>().mainTrial(selection); }); };
            trialActions.advance = (Entity e) => { mc.uiController.doInput(() => { if (!e.getComponent<CriminalRecord>().selectionMode) mc.doNextAction(); }); };
            uiController.trial.init(trialActions);

            //Elections
            ElectionActions electionActions = new ElectionActions();
            electionActions.houseElection = () => { uiController.doInput(() => { worldState.getComponent<Government>().houseElection(); }); };
            electionActions.senateElection = () => { uiController.doInput(() => { worldState.getComponent<Government>().senateElection(); }); };
            uiController.election.init(electionActions);

            //SiteMode
            SiteModeActions siteModeActions = new SiteModeActions();
            siteModeActions.move = (string dir) => { uiController.doInput(() => { currentSiteModeScene.move(dir); }); };
            siteModeActions.wait = () => { uiController.doInput(() => { currentSiteModeScene.wait(); }); };
            siteModeActions.fight = () => { uiController.doInput(() => { currentSiteModeScene.fight(); }); };
            siteModeActions.advanceRound = () => { uiController.doInput(() => { currentSiteModeScene.advanceRound(); }); };
            siteModeActions.talkDating = (Entity lib, Entity con) => { uiController.doInput(() => { currentSiteModeScene.talkDating(lib, con); }); };
            siteModeActions.talkIssues = (Entity lib, Entity con) => { uiController.doInput(() => { currentSiteModeScene.talkIssues(lib, con); }); };
            siteModeActions.talkRentRoom = () => { uiController.doInput(() => { currentSiteModeScene.talkRentRoom(); }); };
            siteModeActions.kidnap = (Entity lib, Entity con) => { uiController.doInput(() => { currentSiteModeScene.kidnap(lib, con); }); };
            siteModeActions.setEncounterWarnings = (bool value) => { uiController.doInput(() => { currentSiteModeScene.setEncounterWarnings(value); }); };
            siteModeActions.loot = () => { uiController.doInput(() => { currentSiteModeScene.lootOrGraffitiTile(); }); };
            siteModeActions.use = () => { uiController.doInput(() => { currentSiteModeScene.useTile(); }); };
            siteModeActions.talkIntimidate = () => { uiController.doInput(() => { currentSiteModeScene.talkIntimidate(); }); };
            siteModeActions.talkThreatenHostage = () => { uiController.doInput(() => { currentSiteModeScene.talkThreatenHostage(); }); };
            siteModeActions.talkBluff = () => { uiController.doInput(() => { currentSiteModeScene.talkBluff(); }); };
            siteModeActions.releaseOppressed = () => { uiController.doInput(() => { currentSiteModeScene.releaseOppressed(); }); };
            siteModeActions.surrender = () => { uiController.doInput(() => { currentSiteModeScene.talkSurrender(); }); };
            siteModeActions.talkBuyWeapons = () => { uiController.doInput(() => { currentSiteModeScene.talkBuyWeapons(); }); };
            siteModeActions.robBankNote = () => { uiController.doInput(() => { currentSiteModeScene.robBankNote(); }); };
            siteModeActions.robBankThreaten = () => { uiController.doInput(() => { currentSiteModeScene.robBankThreaten(); }); };
            uiController.siteMode.init(siteModeActions);

            //Meeting
            MeetingActions meetingActions = new MeetingActions();
            meetingActions.discussion = (Entity recruit, bool props) => { uiController.doInput(() => { recruit.getComponent<Recruit>().discussion(props); }); };
            meetingActions.endMeetings = (Entity recruit) => { uiController.doInput(() => { recruit.getComponent<Recruit>().callOffMeetings(); }); };
            meetingActions.joinLCS = (Entity recruit) => { uiController.doInput(() => { recruit.getComponent<Recruit>().joinLCS(); }); };
            meetingActions.normalDate = (Entity recruit, bool spendCash) => { uiController.doInput(() => { recruit.getComponent<Dating>().regularDate(spendCash); }); };
            meetingActions.vacation = (Entity recruit) => { uiController.doInput(() => { recruit.getComponent<Dating>().startVacation(); }); };
            meetingActions.breakUp = (Entity recruit) => { uiController.doInput(() => { recruit.getComponent<Dating>().breakUp(); }); };
            meetingActions.kidnap = (Entity recruit) => { uiController.doInput(() => { recruit.getComponent<Dating>().kidnap(); }); };
            uiController.meeting.init(meetingActions);

            //Shop
            ShopActions shopActions = new ShopActions();
            shopActions.buy = (Entity location) => { uiController.doInput(() => { location.getComponent<Shop>().buy(); }); };
            shopActions.sell = (Entity location) => { uiController.doInput(() => { location.getComponent<Shop>().sell(); }); };
            shopActions.addItemToBuyCart = (Entity location, Entity item) => { uiController.doInput(() => { location.getComponent<Shop>().buyCart.Add(item); }); };
            shopActions.addItemToSellCart = (Entity location, Entity item) => { uiController.doInput(() => { location.getComponent<Shop>().sellCart.Add(item); }); };
            shopActions.addAllSimilarItemsToSellCart = (Entity location, Entity item, Entity homeBase) => {
                uiController.doInput(() => 
                    {
                        string refName = item.def;
                        if (item.hasComponent<Armor>()) refName += item.getComponent<Armor>().quality;

                        foreach(Entity e in homeBase.getComponent<SafeHouse>().getInventory())
                        {
                            //Shops won't buy bloody or damaged armor
                            if (e.hasComponent<Armor>())
                            {
                                if (e.getComponent<Armor>().damaged || e.getComponent<Armor>().bloody)
                                    continue;
                            }

                            //If this item is in transit it shouldn't be sold
                            if (e.getComponent<ItemBase>().targetBase != null) continue;

                            string itemName = e.def;
                            if (e.hasComponent<Armor>()) itemName += e.getComponent<Armor>().quality;

                            if (refName == itemName && !location.getComponent<Shop>().sellCart.Contains(e))
                                location.getComponent<Shop>().sellCart.Add(e);
                        }
                    });
            };
            shopActions.removeItemFromBuyCart = (Entity location, Entity item) => { uiController.doInput(() => { location.getComponent<Shop>().buyCart.Remove(item); }); };
            shopActions.removeAllSimilarItemsFromBuyCart = (Entity location, Entity item) => {
                uiController.doInput(() => {
                    string refName = item.def;
                    if (item.hasComponent<Armor>()) refName += item.getComponent<Armor>().quality;

                    location.getComponent<Shop>().buyCart.RemoveAll((Entity e) => 
                    {
                        string itemName = e.def;
                        if (e.hasComponent<Armor>()) itemName += e.getComponent<Armor>().quality;

                        return itemName == refName;
                    });
                });
            };
            shopActions.removeItemFromSellCart = (Entity location, Entity item) => { uiController.doInput(() => { location.getComponent<Shop>().sellCart.Remove(item); }); };
            shopActions.removeAllSimilarItemsFromSellCart = (Entity location, Entity item) => {
                uiController.doInput(() => {
                    string refName = item.def;
                    if (item.hasComponent<Armor>()) refName += item.getComponent<Armor>().quality;

                    location.getComponent<Shop>().sellCart.RemoveAll((Entity e) =>
                    {
                        string itemName = e.def;
                        if (e.hasComponent<Armor>()) itemName += e.getComponent<Armor>().quality;

                        return itemName == refName;
                    });
                });
            };
            shopActions.finishShopping = (Entity location) => { uiController.doInput(() => { location.getComponent<Shop>().finishShopping(); }); };
            uiController.shop.init(shopActions);

            //News
            NewsActions newsActions = new NewsActions();
            newsActions.nextScreen = () => { uiController.doInput(() => { doNextAction(); }); };
            uiController.news.init(newsActions);

            //MapScreen
            NationMapActions mapActions = new NationMapActions();
            mapActions.disband = () => { uiController.doInput(() => { lcs.disband(); }); };
            uiController.nationMap.init(mapActions);

            //FastAdvanceScreen
            FastAdvanceActions fastAdvanceActions = new FastAdvanceActions();
            fastAdvanceActions.reform = () => { uiController.doInput(() => { lcs.reform(); }); };
            uiController.fastAdvance.init(fastAdvanceActions);

            //Generate translations on all translateable fields
            uiController.generateTranslations();
        }        
        #endregion

        //Display Title screen
        public void startGame()
        {
            GameData.getData().loadDefinitions();
            initUIElements();
            
            actionQueue.Add(uiController.titlePage.show, "Show Title");
            doNextAction();
        }

        public void load(XmlDocument doc)
        {
            if(doc.DocumentElement.SelectSingleNode("version") == null)
            {
                uiController.showPopup("This save file is from a version of the game that is too old to be compatible with the current version.", uiController.titlePage.show);
                return;
            }

            if (doc.DocumentElement.SelectSingleNode("version").InnerText != CURRENT_VERSION)
            {
                loadPreprocess(doc);
            }

            XmlNode root = doc.DocumentElement;

            currentDate = DateTime.Parse(root.SelectSingleNode("currentDate").InnerText);
            gameFlags = (GameFlags) Enum.Parse(typeof(GameFlags), root.SelectSingleNode("gameFlags").InnerText);
            canSeeThings = bool.Parse(root.SelectSingleNode("canSeeThings").InnerText);

            foreach(XmlNode node in root.SelectSingleNode("messageLog").ChildNodes)
            {
                LogMessage message = new LogMessage(node.InnerText, bool.Parse(node.Attributes["priority"].Value));
                message.age = int.Parse(node.Attributes["age"].Value);
                message.read = bool.Parse(node.Attributes["read"].Value);
                messageLog.Add(message);
            }

            Dictionary<long, Entity> loadEntities = new Dictionary<long, Entity>();

            XmlNode worldNode = null;

            long maxguid = -1;

            foreach(XmlNode node in root.SelectNodes("Entity"))
            {
                long guid = long.Parse(node.Attributes["guid"].Value);
                if (guid > maxguid) maxguid = guid;

                Entity e = new Entity(node.Attributes["type"].Value, node.Attributes["def"].Value, guid);
                loadEntities.Add(guid, e);
                if (node.Attributes["type"].Value == "world")
                {
                    worldState = e;
                    worldNode = node;
                }

                Entity.setNextGuid(maxguid + 1);
            }

            foreach(XmlNode node in root.SelectNodes("Entity"))
            {
                loadEntities[int.Parse(node.Attributes["guid"].Value)].load(node, loadEntities);
                loadEntities[int.Parse(node.Attributes["guid"].Value)].persist();
            }

            //This has to be done last because Liberal Entities need to be fully constructed before joining squads
            worldState.getComponent<LiberalCrimeSquad>().loadSquads(worldNode.SelectSingleNode("LiberalCrimeSquad"), loadEntities);

            if (ccs.status == ConservativeCrimeSquad.Status.INACTIVE)
                generalPublic.pollData.Remove(Constants.VIEW_CONSERVATIVECRIMESQUAD);

            GameData.getData().saveGame();
            if (canSeeThings)
                actionQueue.Add(uiController.baseMode.show, "Show Base Screen");
            else
                actionQueue.Add(uiController.fastAdvance.show, "Show Fast Advance Screen");
            doNextAction();
        }

        //Preprocess a save file from an older version to make it as compatible as possible with the current version
        private void loadPreprocess(XmlDocument doc)
        {
            if(doc.DocumentElement.SelectSingleNode("version").InnerText == "b10")
            {
                SaveFileProcessor.updateTo_b11(doc);
            }
        }

        public void save(XmlDocument doc)
        {
            if (doc.DocumentElement.SelectSingleNode("currentDate") == null)
            {
                XmlNode dateNode = doc.CreateElement("currentDate");
                dateNode.InnerText = currentDate.ToString("d");
                doc.DocumentElement.AppendChild(dateNode);
            }
            else
            {
                doc.DocumentElement.SelectSingleNode("currentDate").InnerText = currentDate.ToString("d");
            }

            if (doc.DocumentElement.SelectSingleNode("gameFlags") == null)
            {
                XmlNode flagsNode = doc.CreateElement("gameFlags");
                flagsNode.InnerText = gameFlags.ToString();
                doc.DocumentElement.AppendChild(flagsNode);
            }

            if(doc.DocumentElement.SelectSingleNode("canSeeThings") == null)
            {
                XmlNode seeThingsNode = doc.CreateElement("canSeeThings");
                seeThingsNode.InnerText = canSeeThings.ToString();
                doc.DocumentElement.AppendChild(seeThingsNode);
            }
            else
            {
                doc.DocumentElement.SelectSingleNode("canSeeThings").InnerText = canSeeThings.ToString();
            }

            if (doc.DocumentElement.SelectSingleNode("messageLog") != null)
                doc.DocumentElement.RemoveChild(doc.DocumentElement.SelectSingleNode("messageLog"));

            if (doc.DocumentElement.SelectSingleNode("debugLog") != null)
                doc.DocumentElement.RemoveChild(doc.DocumentElement.SelectSingleNode("debugLog"));

            if(doc.DocumentElement.SelectSingleNode("version") == null)
            {
                XmlNode versionNode = doc.CreateElement("version");
                versionNode.InnerText = CURRENT_VERSION;
                doc.DocumentElement.AppendChild(versionNode);
            }
            else
            {
                doc.DocumentElement.SelectSingleNode("version").InnerText = CURRENT_VERSION;
            }

            XmlNode messageNode = doc.CreateElement("messageLog");
            doc.DocumentElement.AppendChild(messageNode);
            foreach (LogMessage m in messageLog)
                m.save(messageNode);

            XmlNode debugNode = doc.CreateElement("debugLog");
            doc.DocumentElement.AppendChild(debugNode);
            foreach (LogMessage m in debugLog)
                m.save(debugNode);
            
            foreach(Entity e in PersistentEntityList.Values)
            {
                e.save(doc);
            }            
        }

        //New game state
        public void initWorld()
        {
            //re-init MC state
            actionQueue = new ActionQueue(null, "BASENODE");
            gameFlags = 0;
            phase = Phase.BASE;
            currentDate = new DateTime(int.Parse(GameData.getData().globalVarsList["STARTYEAR"]), 1, 1);
            combatModifiers = CombatModifiers.NONE;
            endGameState = EndGame.NONE;
            canSeeThings = true;
            messageLog.Clear();
            combatLog.Clear();
            List<Entity> entities = new List<Entity>(PersistentEntityList.Values);
            foreach(Entity e in entities)
            {
                e.depersist();
            }
            PersistentEntityList.Clear();
            worldState = null;
            initUIElements();

            worldState = Factories.WorldFactory.create("USA");
            worldState.persist();
            //This is needed here because not all needed variables will have been initialized during world creation.
            worldState.getComponent<Public>().PresidentApprovalRating = worldState.getComponent<Public>().calculatePresidentApproval();
            foreach (Entity c in nation.cities.Values)
            {
                City city = c.getComponent<City>();
                foreach (string district in city.locations.Keys)
                {
                    foreach (Entity e in city.locations[district])
                    {
                        e.getComponent<SiteBase>().hideCheck();
                    }
                }
            }

            Entity leader = Factories.CreatureFactory.create("FOUNDER");
            leader.persist();
            Liberal lib = new Liberal();
            lib.leader = null;
            leader.setComponent(lib);

            LiberalCrimeSquad lcs = worldState.getComponent<LiberalCrimeSquad>();
            lcs.founder = leader;
            lcs.Money = 7;

            Entity homelessShelter = nation.cities["DC"].getComponent<City>().getLocation("RESIDENTIAL_SHELTER");
            LiberalCrimeSquad.Squad mainSquad = lcs.newSquad("Liberal Crime Squad");
            mainSquad.Add(leader);
            homelessShelter.getComponent<SafeHouse>().moveSquadHere(mainSquad);
            
            worldState.getComponent<LiberalCrimeSquad>().activeSquad = mainSquad;

            makelawyer = false;
            gaylawyer = false;
            stolenCar = null;

            actionQueue.Add(uiController.founderQuestions.show, "New Game Questions");
            doNextAction();

            Entity president = worldState.getComponent<Government>().president;

            string introText = getTranslation("TITLE_intro_text_1");
            addMessage(introText, true);
            introText = getTranslation("TITLE_intro_text_2").Replace("$OLDPRESIDENT", Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.WHITEMALEPATRIARCH)).Replace("$PRESIDENT", president.getComponent<CreatureInfo>().getName());
            addMessage(introText, true);
            introText = getTranslation("TITLE_intro_text_3").Replace("$PRESIDENT", president.getComponent<CreatureInfo>().getName()).Replace("$HISHER", president.getComponent<CreatureInfo>().hisHer().ToLower());
            addMessage(introText, true);

            addMessage(getTranslation("TITLE_intro_text_4").Replace("$DATE", currentDate.ToString("D")), true);
        }

        public void finishQuestions()
        {
            Entity leader = lcs.founder;

            leader.getComponent<Portrait>().forceRegen = false;
            leader.getComponent<Liberal>().joinDate = leader.getComponent<Age>().birthday;

            if((gameFlags & GameFlags.ACTIVE_CCS) != 0)
            {
                worldState.getComponent<ConservativeCrimeSquad>().status = ConservativeCrimeSquad.Status.ATTACK;
                Public.PollData pollData = new Public.PollData();
                pollData.def = Constants.VIEW_CONSERVATIVECRIMESQUAD;
                pollData.age = 50;
                worldState.getComponent<Public>().pollData.Add(Constants.VIEW_CONSERVATIVECRIMESQUAD, pollData);
            }
            if((gameFlags & GameFlags.NO_CCS) != 0)
            {
                worldState.getComponent<ConservativeCrimeSquad>().defeated = true;
            }
            if((gameFlags & GameFlags.NIGHTMARE) != 0)
            {
                GameData dl = GameData.getData();

                //change public opinion to be much lower 
                Public publicOpinion = worldState.getComponent<Public>();
                foreach (ViewDef view in dl.viewList.Values)
                {
                    if (view.type == Constants.VIEW_LIBERALCRIMESQUAD ||
                        view.type == Constants.VIEW_LIBERALCRIMESQUADPOS ||
                        view.type == Constants.VIEW_CONSERVATIVECRIMESQUAD) continue;
                    publicOpinion.PublicOpinion[view.type] = mc.LCSRandom(20);
                }

                //change all laws to be archconservative
                Government g = worldState.getComponent<Government>();
                foreach(string law in g.laws.Keys)
                {
                    g.laws[law].alignment = Alignment.ARCHCONSERVATIVE;
                }

                //Repopulate the house, senate, and supreme court to be more conservative than default
                Factories.WorldFactory.fillCongress(g, "USA", true);
                foreach (Entity e in g.supremeCourt) e.depersist();
                g.supremeCourt.Clear();
                Factories.WorldFactory.fillSupremeCourt(g, "USA", true);
            }

            if (makelawyer)
            {
                Entity lawyer = Factories.CreatureFactory.create("LAWYER");
                lawyer.getComponent<CreatureInfo>().alignment = Alignment.LIBERAL;
                if (lawyer.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level < leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level - 2)
                    lawyer.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level = leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level - 2;
                lawyer.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level = 1;
                lawyer.getComponent<Age>().birthday = new DateTime(1984, lawyer.getComponent<Age>().birthday.Month, lawyer.getComponent<Age>().birthday.Day);
                lawyer.getComponent<CreatureBase>().Location = nation.cities["DC"].getComponent<City>().getLocation("GOVERNMENT_COURTHOUSE");
                lawyer.getComponent<CreatureInfo>().workLocation = nation.cities["DC"].getComponent<City>().getLocation("GOVERNMENT_COURTHOUSE");

                if (gaylawyer)
                {
                    lawyer.getComponent<CreatureInfo>().genderConservative = leader.getComponent<CreatureInfo>().genderConservative;
                }
                else
                {
                    if (leader.getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.MALE)
                        lawyer.getComponent<CreatureInfo>().genderConservative = CreatureInfo.CreatureGender.FEMALE;
                    else if (leader.getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.FEMALE)
                        lawyer.getComponent<CreatureInfo>().genderConservative = CreatureInfo.CreatureGender.MALE;
                    else
                        switch (LCSRandom(3))
                        {
                            case 0: lawyer.getComponent<CreatureInfo>().genderConservative = CreatureInfo.CreatureGender.MALE; break;
                            case 1: lawyer.getComponent<CreatureInfo>().genderConservative = CreatureInfo.CreatureGender.FEMALE; break;
                            case 2: lawyer.getComponent<CreatureInfo>().genderConservative = CreatureInfo.CreatureGender.NEUTRAL; break;
                        }
                }

                lawyer.getComponent<CreatureInfo>().genderLiberal = lawyer.getComponent<CreatureInfo>().genderConservative;
                lawyer.getComponent<Portrait>().makeMyFace();
                lawyer.getComponent<CreatureInfo>().givenName = Factories.CreatureFactory.generateGivenName(lawyer.getComponent<CreatureInfo>().genderConservative);

                leader.getComponent<Liberal>().recruit(lawyer, Liberal.RecruitType.LOVE_SLAVE);
                lawyer.getComponent<Liberal>().sleeperize();
                lawyer.getComponent<Liberal>().infiltration = 30;
            }

            actionQueue.Add(uiController.baseMode.show, "Show Base Screen");
            doNextAction();
        }

        public void gameOver(string fate)
        {
            GameData.getData().clearSave();
            GameData.getData().saveHighScore(fate, worldState.getComponent<LiberalCrimeSquad>().slogan, worldState.getComponent<HighScore>());
            actionQueue.Add(() =>
            {
                uiController.closeUI();
                uiController.highScore.show();
                uiController.GameOver();
            }, "highscore");
            
        }
        
        public void nextPhase()
        {
            if (phase == Phase.DAILY_CLEANUP) phase = Phase.BASE;
            else phase++;

            switch (phase)
            {
                case Phase.BASE:
                    if(canSeeThings) addMessage(dailyAffirmation());
                    if (!uiController.isScreenActive(uiController.baseMode) && canSeeThings)
                    {
                        uiController.closeUI();
                        uiController.baseMode.show();
                        GameData.getData().saveGame();
                    }
                    else if(!canSeeThings && !uiController.isScreenActive(uiController.fastAdvance))
                    {
                        uiController.closeUI();
                        uiController.fastAdvance.show();
                        GameData.getData().saveGame();
                    }
                    else
                    {
                        uiController.refreshUI();
                    }
                    break;
                case Phase.TROUBLE:
                    //expire old messages here
                    foreach (LogMessage m in messageLog)
                    {
                        m.age++;
                        m.read = true;
                    }
                    handleCauseTrouble();
                    actionQueue.Add(nextPhase, "Next Phase: Activity");
                    doNextAction();
                    break;
                case Phase.ACTIVITY:
                    handleActivities();
                    actionQueue.Add(nextPhase, "Next Phase: Post Activity");
                    doNextAction();
                    break;
                case Phase.POST_ACTIVITY:
                    handlePostActivity();
                    actionQueue.Add(nextPhase, "Next Phase: CCS");
                    doNextAction();
                    break;
                case Phase.CCS:
                    handleCCS();
                    actionQueue.Add(nextPhase, "Next Phase: Elections");
                    doNextAction();
                    break;
                case Phase.ELECTIONS:
                    handleElections();
                    actionQueue.Add(nextPhase, "Next Phase: Advance Day");
                    doNextAction();
                    break;
                case Phase.ADVANCE_DAY:
                    advanceDay();
                    actionQueue.Add(nextPhase, "Next Phase: News");
                    doNextAction();
                    break;
                case Phase.NEWS:
                    handleNews();
                    actionQueue.Add(nextPhase, "Next Phase: Daily Cleanup");
                    doNextAction();
                    break;
                case Phase.DAILY_CLEANUP:
                    handleDailyCleanup();
                    actionQueue.Add(nextPhase, "Next Phase: Base");
                    doNextAction();
                    break;
            }
        }

        public ActionQueue createSubQueue(Action firstAction, string firstActionDescription, Action exitAction, string exitActionDescription, ActionQueue parentAction = null, bool exitActionBlocker = false)
        {
            ActionQueue newAction = new ActionQueue(exitAction, exitActionDescription, exitActionBlocker);
            newAction.Add(firstAction, firstActionDescription);
            if (parentAction != null)
                parentAction.Add(newAction);
            else
                actionQueue.Add(newAction);
            
            return newAction;
        }

        public ActionQueue getNextActionParent()
        {
            if (actionQueue.Count > 0)
            {
                ActionQueue aq = actionQueue[0];
                ActionQueue aqParent = actionQueue;

                while (aq.Count > 0)
                {
                    aqParent = aq;
                    aq = aq[0];
                }

                return aqParent;
            }
            else
                return actionQueue;
        }

        public ActionQueue addAction(Action action, string description, bool blocker = false)
        {
            ActionQueue newAction = new ActionQueue(action, description, blocker);
            actionQueue.Add(newAction);
            return newAction;
        }

        public ActionQueue addToCurrentQueue(Action action, string description, bool blocker = false)
        {
            ActionQueue newAction = new ActionQueue(action, description, blocker);
            getNextActionParent().Add(newAction);
            return newAction;
        }

        public ActionQueue getNextAction()
        {
            if (actionQueue.Count > 0)
            {
                ActionQueue aq = actionQueue[0];

                while (aq.Count > 0)
                {
                    aq = aq[0];
                }

                return aq;
            }
            else
            {
                return null;
            }
        }

        public void doNextAction()
        {
            if(!actionStep)
                doNextAction(false);
        }

        public void doNextAction(bool runBlocker)
        {
            if (actionQueue.Count > 0)
            {
                ActionQueue aq = actionQueue[0];
                ActionQueue aqParent = actionQueue;

                while(aq.Count > 0)
                {
                    aqParent = aq;
                    aq = aq[0];
                }

                if (aq.blocker && !runBlocker)
                {
                    addDebugMessage("An action consumer was called but was blocked because it had insufficient privileges: " + aq.description);
                    return;
                }

                Action action = aq.action;
                aqParent.Remove(aq);

                if (action != null) action();
            }
            else
            {
                addDebugMessage("An action consumer was called but there was no action to consume");
            }
        }

        public void addDebugMessage(string text)
        {
            debugLog.Add(new LogMessage(text, false));
            uiController.updateDebugLog();
        }

        public void addErrorMessage(string text)
        {
            actionQueue.Add(() =>
            {
                uiController.showPopup(text, () =>
                {
                    debugLog.Add(new LogMessage(text, true));
                    doNextAction();
                });
            }, "Error Interrupt Popup");
        }

        public void addMessage(string text, bool interrupt = false, bool interruptWhileDisbanded = false)
        {
            if (!interrupt)
            {
                messageLog.Add(new LogMessage(text, interrupt));
                if (messageLog.Count > MAX_LOG_SIZE) messageLog.RemoveAt(0);
            }
            else
            {
                if (interruptWhileDisbanded || canSeeThings)
                {
                    actionQueue.Add(() =>
                    {
                        uiController.showPopup(text, () =>
                        {
                            messageLog.Add(new LogMessage(text, interrupt));
                            if (messageLog.Count > MAX_LOG_SIZE) messageLog.RemoveAt(0);
                            doNextAction();
                        });
                    }, "Interrupt Popup");
                }
                else
                {
                    messageLog.Add(new LogMessage(text, interrupt));
                    if (messageLog.Count > MAX_LOG_SIZE) messageLog.RemoveAt(0);
                }
            }
        }

        public void addCombatMessage(string text, bool interrupt = false)
        {
            if (text.Contains("##DEBUG##") && !DebugMode) return;

            if (!interrupt)
            {
                combatLog.Add(new LogMessage(text.TrimEnd('\n'), interrupt));
                if (combatLog.Count > MAX_LOG_SIZE) combatLog.RemoveAt(0);
            }
            else
            {
                getNextAction().Add(() => 
                { uiController.showPopup(text, ()=> 
                    {
                        combatLog.Add(new LogMessage(text.TrimEnd('\n'), interrupt));
                        if (combatLog.Count > MAX_LOG_SIZE) combatLog.RemoveAt(0);
                        doNextAction();
                    });
                }, "Combat Interrupt Popup");
            }
        }

        public void nextRound()
        {
            foreach(LogMessage message in combatLog)
            {
                message.age++;
                message.read = true;
            }
        }

        public void endEncounter()
        {
            combatModifiers = CombatModifiers.NONE;
            combatLog.Clear();
            currentChaseScene = null;
            currentSiteModeScene = null;
        }

        public class LogMessage
        {
            public string text;
            public int age;
            public bool priority;
            public bool read;

            public LogMessage(string text, bool priority)
            {
                this.text = text;
                this.priority = priority;
                read = false;
                age = 0;
            }

            public void save(XmlNode node)
            {
                XmlNode logNode = node.OwnerDocument.CreateElement("LogMessage");
                logNode.InnerText = text;
                node.AppendChild(logNode);

                XmlAttribute ageAtt = logNode.OwnerDocument.CreateAttribute("age");
                ageAtt.Value = age.ToString();
                logNode.Attributes.Append(ageAtt);

                XmlAttribute priorityAtt = logNode.OwnerDocument.CreateAttribute("priority");
                priorityAtt.Value = priority.ToString();
                logNode.Attributes.Append(priorityAtt);

                XmlAttribute readAtt = logNode.OwnerDocument.CreateAttribute("read");
                readAtt.Value = read.ToString();
                logNode.Attributes.Append(readAtt);
            }
        }

        public void startQuestion(int questionNum, int response)
        {
            Entity leader = lcs.founder;

            switch (questionNum)
            {
                //The day I was born in 1984...
                case 0:
                    switch (response)
                    {
                        //the Polish priest Popieluszko was kidnapped by government agents.
                        case 0:
                            leader.getComponent<Age>().birthday = new DateTime(1984, 10, 19);
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_AGILITY].Level += 2;
                            break;
                        //was the 3rd anniversary of the assassination attempt on Ronald Reagan.
                        case 1:
                            leader.getComponent<Age>().birthday = new DateTime(1984, 3, 3);
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_STRENGTH].Level += 2;
                            break;
                        //the Macintosh was introduced.
                        case 2:
                            leader.getComponent<Age>().birthday = new DateTime(1984, 1, 24);
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].Level += 2;
                            break;
                        //the Nobel Peace Prize went to Desmond Tutu for opposition to apartheid.
                        case 3:
                            leader.getComponent<Age>().birthday = new DateTime(1984, 10, 16);
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level += 2;
                            break;
                        //the Sandanista Front won the elections in Nicaragua.
                        case 4:
                            leader.getComponent<Age>().birthday = new DateTime(1984, 9, 4);
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_CHARISMA].Level += 2;
                            break;
                    }
                    break;
                //When I was bad...
                case 1:
                    switch (response)
                    {
                        //my parents grounded me and hid my toys, but I knew where they put them.
                        case 0:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_SECURITY].level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_AGILITY].Level += 1;
                            break;
                        //my father beat me.  I learned to take a punch earlier than most.
                        case 1:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_MARTIAL_ARTS].level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].Level += 1;
                            break;
                        //I was sent to my room, where I studied quietly by myself, alone.
                        case 2:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_WRITING].level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].Level += 1;
                            break;
                        //my parents argued with each other about me, but I was never punished.
                        case 3:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level += 1;
                            break;
                        //my father lectured me endlessly, trying to make me think like him.
                        case 4:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_PSYCHOLOGY].level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_CHARISMA].Level += 1;
                            break;
                    }
                    break;
                //In elementary school...
                case 2:
                    switch (response)
                    {
                        //I was mischievous, and always up to something.
                        case 0:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_DISGUISE].level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_AGILITY].Level += 1;
                            break;
                        //I had a lot of repressed anger.  I hurt animals.
                        case 1:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_PSYCHOLOGY].level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_STRENGTH].Level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_AGILITY].Level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level -= 1;
                            break;
                        //I was at the head of the class, and I worked very hard.
                        case 2:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_WRITING].level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].Level += 1;
                            break;
                        //I was unruly and often fought with the other children.
                        case 3:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_MARTIAL_ARTS].level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_STRENGTH].Level += 1;
                            break;
                        //I was the class clown.  I even had some friends.
                        case 4:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_CHARISMA].Level += 1;
                            break;
                    }
                    break;
                //When I turned 10...
                case 3:
                    switch (response)
                    {
                        //my parents divorced.  Whenever I talked, they argued, so I stayed quiet.
                        case 0:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_STEALTH].level += 1;
                            break;
                        //my parents divorced.  Violently.
                        case 1:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_MARTIAL_ARTS].level += 1;
                            break;
                        //my parents divorced.  Acrimoniously.  I once tripped over the paperwork!
                        case 2:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_LAW].level += 1;
                            break;
                        //my parents divorced.  Mom slept with the divorce lawyer.
                        case 3:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].level += 1;
                            break;
                        //my parents divorced.  It still hurts to read my old diary.
                        case 4:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_WRITING].level += 1;
                            break;
                    }
                    break;
                //In junior high school...
                case 4:
                    switch (response)
                    {
                        //I was into chemistry.  I wanted to know what made the world tick.
                        case 0:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].Level += 2;
                            break;
                        //I played guitar in a grunge band.  We sucked, but so did life.
                        case 1:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_MUSIC].level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_CHARISMA].Level += 2;
                            break;
                        //I drew things, a lot.  I was drawing a world better than this.
                        case 2:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_ART].level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level += 2;
                            break;
                        //I played violent video games at home.  I was a total outcast.
                        case 3:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_COMPUTERS].level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_AGILITY].Level += 2;
                            break;
                        //I was obsessed with swords, and started lifting weights.
                        case 4:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_SWORD].level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_STRENGTH].Level += 2;
                            break;
                    }
                    break;
                //Things were getting really bad...
                case 5:
                    switch (response)
                    {
                        //when I stole my first car.  I got a few blocks before I totaled it.
                        case 0:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_DRIVING].level += 1;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_SECURITY].level += 1;
                            break;
                        //and I went to live with my dad.  He had been in Nam and he still drank.
                        case 1:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_SHOTGUN].level += 1;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_RIFLE].level += 1;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_PSYCHOLOGY].level += 1;
                            break;
                        //and I went completely goth.  I had no friends and made costumes by myself.
                        case 2:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_TAILORING].level += 2;
                            break;
                        //when I was sent to religious counseling, just stressing me out more.
                        case 3:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].level += 1;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_PSYCHOLOGY].level += 1;
                            break;
                        //and I tried being a teacher's assistant.  It just made me a target.
                        case 4:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_TEACHING].level += 2;
                            break;
                    }
                    break;
                //Well, I knew it had reached a crescendo when...
                case 6:
                    gaylawyer = false;

                    switch (response)
                    {
                        //I stole a cop car when I was only 14.  I went to juvie for 6 months.
                        case 0:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_DRIVING].level += 1;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_SECURITY].level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].Level += 1;
                            break;
                        //my step mom shot her ex-husband, my dad, with a shotgun.  She got off.
                        case 1:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_SHOTGUN].level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_AGILITY].Level += 1;
                            break;
                        //I tried wrestling for a quarter, desperate to fit in.
                        case 2:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_MARTIAL_ARTS].level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_STRENGTH].Level += 1;
                            break;
                        //I got caught making out, and now I needed to be 'cured' of homosexuality.
                        case 3:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].level += 1;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level += 1;
                            gaylawyer = true;
                            break;
                        //I resorted to controlling people.  Had my own clique of outcasts.
                        case 4:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_CHARISMA].Level += 1;
                            break;
                    }
                    break;
                //I was only 15 when I ran away, and...
                case 7:
                    switch (response)
                    {
                        //I started robbing houses:  rich people only.  I was fed up with their crap.
                        case 0:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_SECURITY].level += 1;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_STEALTH].level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_AGILITY].Level += 1;
                            break;
                        //I hung out with thugs and beat the shit out of people.
                        case 1:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_MARTIAL_ARTS].level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_STRENGTH].Level += 1;
                            break;
                        //I got a horrible job working fast food, smiling as people fed the man.
                        case 2:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_CHARISMA].Level += 1;
                            break;
                        //I let people pay me for sex.  I needed the money to survive.
                        case 3:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level -= 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_CHARISMA].Level += 2;
                            break;
                        //I volunteered for a left-wing candidate. It wasn't *real*, though, you know?
                        case 4:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].level += 1;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_LAW].level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].Level += 1;
                            break;
                    }
                    break;
                //Life went on.  On my 18th birthday...
                case 8:
                    switch (response)
                    {
                        //I got my hands on a sports car. The owner must have been pissed.
                        case 0:
                            stolenCar = Factories.ItemFactory.create("VEHICLE_SPORTSCAR");
                            stolenCar.getComponent<Vehicle>().heat = 10;
                            break;
                        //I bought myself an assault rifle.
                        case 1:
                            Entity founder = worldState.getComponent<LiberalCrimeSquad>().founder;

                            founder.getComponent<Inventory>().equipWeapon(Factories.ItemFactory.create("WEAPON_AUTORIFLE_AK47"));
                            for (int i = 0; i < 9; i++)
                                founder.getComponent<Inventory>().equipClip(Factories.ItemFactory.create("CLIP_ASSAULT"));
                            founder.getComponent<Inventory>().reload(false);
                            break;
                        //I celebrated.  I'd saved a thousand bucks!
                        case 2:
                            worldState.getComponent<LiberalCrimeSquad>().Money += 1000;
                            break;
                        //I went to a party and met a cool law student.  We've been dating since.
                        case 3:
                            makelawyer = true;
                            break;
                        //I managed to acquire secret maps of several major buildings downtown.
                        case 4:
                            foreach(Entity e in nation.cities["DC"].getComponent<City>().locations["Downtown"])
                            {
                                if (e.hasComponent<TroubleSpot>())
                                    e.getComponent<TroubleSpot>().mapped = true;
                            }
                            break;
                    }
                    break;
                //For the past decade, I've been...
                case 9:
                    //Homeless Shelter
                    Entity safeHouse = leader.getComponent<Liberal>().homeBase;

                    switch (response)
                    {
                        //stealing from Corporations.  I know they're still keeping more secrets.
                        case 0:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_SECURITY].level += 2;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_STEALTH].level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].Level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_AGILITY].Level += 2;
                            leader.getComponent<Inventory>().equipArmor(Factories.ItemFactory.create("ARMOR_BLACKCLOTHES"));
                            //Downtown apartment +$1500 (one month rent)
                            safeHouse = nation.cities["DC"].getComponent<City>().getLocation("RESIDENTIAL_APARTMENT_UPSCALE");
                            safeHouse.getComponent<SafeHouse>().owned = true;
                            worldState.getComponent<LiberalCrimeSquad>().Money += 1500;
                            break;
                        //a violent criminal.  Nothing can change me, or stand in my way.
                        case 1:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_RIFLE].level += 2;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_PISTOL].level += 2;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_STREET_SENSE].level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_AGILITY].Level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].Level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_STRENGTH].Level += 2;
                            leader.getComponent<Inventory>().equipArmor(Factories.ItemFactory.create("ARMOR_CLOTHES"));
                            //+Crack house (with stockpiled rations) +A crew (four gang members with knives and pistols)
                            for(int i = 0; i < 4; i++)
                            {
                                Entity crew = Factories.CreatureFactory.create("GANGMEMBER");
                                if(crew.getComponent<Inventory>().getWeapon().def == "WEAPON_AUTORIFLE_AK47" ||
                                    crew.getComponent<Inventory>().getWeapon().def == "WEAPON_SMG_MP5" ||
                                    crew.getComponent<Inventory>().getWeapon().def == "WEAPON_NONE")
                                {
                                    crew.getComponent<Inventory>().destroyWeapon();
                                    crew.getComponent<Inventory>().destroyAllClips();
                                    crew.getComponent<Inventory>().equipWeapon(Factories.ItemFactory.create("WEAPON_SEMIPISTOL_9MM"));
                                    for (int j = 0; j < 4; j++)
                                        crew.getComponent<Inventory>().equipClip(Factories.ItemFactory.create("CLIP_9"));
                                }

                                crew.getComponent<CreatureInfo>().alignment = Alignment.LIBERAL;
                                crew.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level += crew.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level / 2;
                                crew.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level -= crew.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level / 2;
                                leader.getComponent<Liberal>().recruit(crew);
                                leader.getComponent<Liberal>().squad.Add(crew);
                            }

                            safeHouse = nation.cities["DC"].getComponent<City>().getLocation("BUSINESS_CRACKHOUSE");
                            safeHouse.getComponent<SafeHouse>().owned = true;
                            safeHouse.getComponent<SafeHouse>().food = 100;
                            break;
                        //taking college courses.  I can see how much the country needs help.
                        case 2:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].level += 2;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_COMPUTERS].level += 2;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_WRITING].level += 2;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_TEACHING].level += 2;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].level += 1;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_LAW].level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].Level += 4;
                            leader.getComponent<Inventory>().equipArmor(Factories.ItemFactory.create("ARMOR_CLOTHES"));
                            //University apartment +650 (one month rent)
                            safeHouse = nation.cities["DC"].getComponent<City>().getLocation("RESIDENTIAL_APARTMENT");
                            safeHouse.getComponent<SafeHouse>().owned = true;
                            worldState.getComponent<LiberalCrimeSquad>().Money += 650;
                            break;
                        //surviving alone, just like anyone.  But we can't go on like this.
                        case 3:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_FIRST_AID].level += 2;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_STREET_SENSE].level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].Level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_AGILITY].Level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].Level += 2;
                            leader.getComponent<Inventory>().equipArmor(Factories.ItemFactory.create("ARMOR_CLOTHES"));
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].Level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_AGILITY].Level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_STRENGTH].Level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].Level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_CHARISMA].Level += 1;
                            break;
                        //writing my manifesto and refining my image.  I'm ready to lead.
                        case 4:
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_LAW].level += 1;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].level += 2;
                            leader.getComponent<CreatureBase>().Skills[Constants.SKILL_WRITING].level += 1;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].Level += 2;
                            leader.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_CHARISMA].Level += 2;
                            leader.getComponent<CreatureBase>().juiceMe(50);
                            leader.getComponent<Inventory>().equipArmor(Factories.ItemFactory.create("ARMOR_CLOTHES"));
                            //Industrial apartment +200 (one month rent)
                            safeHouse = nation.cities["DC"].getComponent<City>().getLocation("RESIDENTIAL_TENEMENT");
                            safeHouse.getComponent<SafeHouse>().owned = true;
                            worldState.getComponent<LiberalCrimeSquad>().Money += 200;
                            break;
                    }
                    safeHouse.getComponent<SafeHouse>().moveSquadHere(leader.getComponent<Liberal>().squad);
                    if (stolenCar != null) safeHouse.getComponent<SafeHouse>().addItemToInventory(stolenCar);
                    break;
            }
        }

        //Send out any Liberals with squad destinations
        private void handleCauseTrouble()
        {
            List<LiberalCrimeSquad.Squad> squads = new List<LiberalCrimeSquad.Squad>(lcs.squads);

            //Rebase liberals with safe houses as their squad target
            foreach(LiberalCrimeSquad.Squad squad in squads)
            {
                if (squad.target != null)
                {
                    if(squad.target.getComponent<SiteBase>().city != squad[0].getComponent<CreatureBase>().Location.getComponent<SiteBase>().city)
                    {
                        if (lcs.Money < 200*squad.Count)
                        {
                            addMessage(getTranslation("BASE_change_city_cannot_afford").Replace("$SQUAD", squad.name));
                            continue;
                        }
                        else
                            lcs.changeFunds(-200*squad.Count);
                    }

                    List<Entity> vehicleList = new List<Entity>();
                    bool squadHasVehicle = false;

                    foreach (Entity e in squad)
                    {
                        if(e.getComponent<Inventory>().vehicle != null &&
                            !e.getComponent<Inventory>().vehicle.getComponent<Vehicle>().used)
                        {
                            e.getComponent<Inventory>().tempVehicle = e.getComponent<Inventory>().vehicle;
                            e.getComponent<Inventory>().vehicle.getComponent<Vehicle>().passengers.Add(e);
                            vehicleList.Add(e.getComponent<Inventory>().vehicle);
                        }
                    }

                    foreach(Entity e in squad)
                    {
                        if(e.getComponent<Inventory>().tempVehicle == null &&
                            vehicleList.Count > 0)
                        {
                            e.getComponent<Inventory>().tempVehicle = vehicleList[mc.LCSRandom(vehicleList.Count)];
                            e.getComponent<Inventory>().tempVehicle.getComponent<Vehicle>().passengers.Add(e);
                        }
                    }

                    foreach(Entity e in vehicleList)
                    {
                        e.getComponent<Vehicle>().used = true;
                        //Assign each vehicle a driver and give them experience. If there's no designated driver, pick the best driver in the car.
                        if (e.getComponent<Vehicle>().preferredDriver != null)
                            e.getComponent<Vehicle>().driver = e.getComponent<Vehicle>().preferredDriver;
                        else
                        {
                            Entity bestDriver = null;
                            foreach(Entity lib in squad)
                            {
                                if (lib.getComponent<Inventory>().tempVehicle == e &&
                                    (bestDriver == null || lib.getComponent<CreatureBase>().Skills[Constants.SKILL_DRIVING].level >
                                    bestDriver.getComponent<CreatureBase>().Skills[Constants.SKILL_DRIVING].level))
                                    bestDriver = lib;
                            }
                            e.getComponent<Vehicle>().driver = bestDriver;
                        }
                        e.getComponent<Vehicle>().driver.getComponent<CreatureBase>().Skills[Constants.SKILL_DRIVING].addExperience(5);
                    }

                    if(vehicleList.Count > 0)
                        squadHasVehicle = true;


                    if (squad.travelAction == LiberalCrimeSquad.Squad.TravelAction.BASE || squad.travelAction == LiberalCrimeSquad.Squad.TravelAction.BOTH)
                    {
                        if(!squad.target.getComponent<SafeHouse>().underSiege)
                            squad.target.getComponent<SafeHouse>().moveSquadHere(squad);
                        else
                        {
                            mc.addMessage(getTranslation("BASE_travel_too_hot").Replace("$SQUAD", squad.name).Replace("$TARGET", squad.target.getComponent<SiteBase>().getCurrentName()));
                        }
                    }

                    if(squad.travelAction == LiberalCrimeSquad.Squad.TravelAction.TROUBLE || squad.travelAction == LiberalCrimeSquad.Squad.TravelAction.BOTH)
                    {
                        if (squad.target.hasComponent<TroubleSpot>())
                        {
                            if (squad.target.getComponent<TroubleSpot>().closed <= 0)
                            {
                                //If this location requires a vehicle and the squad doesn't have one (because perhaps their assigned vehicles were already used by another squad)
                                //try to find an unused vehicle in the base
                                if (squad.target.getComponent<SiteBase>().city.getComponent<City>().requiresVehicle(squad.target.def) && !squadHasVehicle)
                                {
                                    Entity vehicle = null;

                                    foreach(Entity e in squad.homeBase.getComponent<SafeHouse>().getInventory())
                                    {
                                        if (!e.hasComponent<Vehicle>()) continue;

                                        if (!e.getComponent<Vehicle>().used)
                                        {
                                            vehicle = e;
                                            break;
                                        }
                                    }
                                    if (vehicle != null)
                                    {
                                        foreach (Entity e in squad)
                                        {
                                            if (e.getComponent<Inventory>().tempVehicle == null)
                                            {
                                                e.getComponent<Inventory>().tempVehicle = vehicle;
                                                vehicle.getComponent<Vehicle>().passengers.Add(e);
                                            }
                                        }

                                        Entity bestDriver = null;
                                        foreach (Entity lib in squad)
                                        {
                                            if (lib.getComponent<Inventory>().tempVehicle == vehicle &&
                                                (bestDriver == null || lib.getComponent<CreatureBase>().Skills[Constants.SKILL_DRIVING].level >
                                                bestDriver.getComponent<CreatureBase>().Skills[Constants.SKILL_DRIVING].level))
                                                bestDriver = lib;
                                        }
                                        vehicle.getComponent<Vehicle>().driver = bestDriver;
                                        bestDriver.getComponent<CreatureBase>().Skills[Constants.SKILL_DRIVING].addExperience(5);

                                        squadHasVehicle = true;
                                    }
                                }

                                if (squadHasVehicle || !squad.target.getComponent<SiteBase>().city.getComponent<City>().requiresVehicle(squad.target.def))
                                {
                                    addAction(() =>
                                    {
                                        worldState.getComponent<LiberalCrimeSquad>().activeSquad = squad;
                                        SiteModeScene scene = new SiteModeScene();
                                        scene.startCausingTrouble(squad, squad.target);
                                        doNextAction();
                                    }, "StartTrouble");
                                }
                                else
                                {
                                    mc.addMessage(getTranslation("BASE_travel_no_car").Replace("$SQUAD", squad.name).Replace("$TARGET", squad.target.getComponent<SiteBase>().getCurrentName()));
                                }
                            }
                            else
                            {
                                mc.addMessage(getTranslation("BASE_travel_too_hot").Replace("$SQUAD", squad.name).Replace("$TARGET", squad.target.getComponent<SiteBase>().getCurrentName()));
                            }
                        }
                        else if (GameData.getData().locationList[squad.target.def].hospital > 0)
                        {
                            List<Entity> tempSquad = new List<Entity>(squad);

                            foreach(Entity e in tempSquad)
                            {
                                e.getComponent<Liberal>().hospitalize(squad.target);
                            }
                        }
                        else if (squad.target.hasComponent<Shop>())
                        {
                            addAction(() =>
                            {
                                squad.target.getComponent<Shop>().startShopping(squad, false);                                
                            }, "Shop");
                        }
                    }
                }
            }

            //Rebase liberals that have been queued to travel without a squad
            List<Entity> lcsMembers = lcs.getAllMembers();

            foreach (Entity e in lcsMembers)
            {
                if (e.getComponent<Liberal>().targetBase == null) continue;
                if(e.getComponent<Liberal>().targetBase.getComponent<SiteBase>().city != e.getComponent<CreatureBase>().Location.getComponent<SiteBase>().city)
                {
                    if (lcs.Money < 200)
                    {
                        addMessage(getTranslation("BASE_change_city_cannot_afford").Replace("$SQUAD", e.getComponent<CreatureInfo>().getName()));
                        continue;
                    }
                    else
                        lcs.changeFunds(-200);
                }

                e.getComponent<Liberal>().targetBase.getComponent<SafeHouse>().moveLiberalHere(e);
            }
        }

        //Have any Liberals that didn't act with a squad perform their daily activity
        private void handleActivities()
        {
            LiberalCrimeSquad lcs = worldState.getComponent<LiberalCrimeSquad>();

            List<Entity> lcsMembers = lcs.getAllMembers();
            lcs.protestingLiberals.Clear();
            lcs.hackingLiberals.Clear();

            foreach(Entity e in lcsMembers)
            {
                e.getComponent<Liberal>().doActivity();
            }

            //Because these are done as a group, they are handled after each individual activity above.
            lcs.activityLiberalDisobedience();
            lcs.activityHacking();
        }

        //Send everyone home from any encounters that might have occurred during the previous phase
        private void handlePostActivity()
        {
            foreach (LiberalCrimeSquad.Squad squad in worldState.getComponent<LiberalCrimeSquad>().squads)
            {
                squad.goHome();
            }

            foreach (Entity e in worldState.getComponent<LiberalCrimeSquad>().getAllMembers())
            {
                if (e.getComponent<Liberal>().status == Liberal.Status.ACTIVE)
                    e.getComponent<Liberal>().goHome();
            }
        }

        //Have CCS take action if they are active
        private void handleCCS()
        {
            if(LCSRandom(30) < (int) ccs.status)
            {
                ccs.doRaid();
            }

            if(ccs.exposure >= ConservativeCrimeSquad.Exposure.EXPOSED && ccs.status >= ConservativeCrimeSquad.Status.ACTIVE && LCSRandom(60) == 0)
            {
                ccs.advanceExposureStoryline();
            }
        }

        //Do election events if it is an election day
        private void handleElections()
        {
            if (currentDate.Month == 11 && currentDate.DayOfWeek == DayOfWeek.Tuesday && currentDate.Day <= 8 && currentDate.Day > 1)
            {
                if (currentDate.Year % 4 == 0)
                {
                    addAction(worldState.getComponent<Government>().presidentialElection, "Presidential Election");
                }

                if (currentDate.Year % 2 == 0)
                {                    
                    addAction(worldState.getComponent<Government>().midtermElection, "Midterm Elections");
                }

                addAction(worldState.getComponent<Government>().propElections, "Proposition Votes");

                //Test to see if the player has lost the game by trying to pass an ARCHCONSERVATIVE AMENDMENT that repeals the constitution
                addAction(() =>
                {
                    if (government.getHouseCount(Alignment.ARCHCONSERVATIVE) >= government.houseNum * (2f / 3f) &&
                    government.getSenateCount(Alignment.ARCHCONSERVATIVE) >= government.senateNum * (2f / 3f))
                    {
                        string descriptionString = "In recognition of the fact that society is degenerating under the pressure of the elite liberal threat, WE THE PEOPLE HEREBY REPEAL THE CONSTITUTION. The former United States are to be reorganized into the CONFEDERATED STATES OF AMERICA, with new boundaries to be determined by leading theologians. Ronald Reagan is to be King, forever, even after death. People may petition Jesus for a redress of grievances, as He will be the only one listening.\nHave a nice day.";
                        Containers.AmendmentResult result = government.ratify(Alignment.ARCHCONSERVATIVE, true);
                        uiController.nationMap.showAmendmentVote(result, "The Arch-Conservative Congress is proposing an <color=red>ARCH-CONSERVATIVE AMENDMENT</color>!", descriptionString, true);
                        if (result.ratified)
                        {
                            endGameState = EndGame.REAGAN;
                        }
                    }
                    else
                    {
                        doNextAction();
                    }
                }, "checkLost");
            }

            if((currentDate.Month == 3 && currentDate.Day == 1) || (currentDate.Month == 9 && currentDate.Day == 1))
            {
                addAction(worldState.getComponent<Government>().congressBills, "Congress Bills");
            }

            if ((currentDate.Month == 6 && currentDate.Day == 1))
            {
                addAction(worldState.getComponent<Government>().supremeCourtDecisions, "Supreme Court Decisions");
            }

            //Check if the player has WON
            if (government.winCheck())
            {
                endGameState = EndGame.WON;
                addMessage("The Liberal agenda has triumphed! Long have you waited for this day - The Liberal Crime Squad's work is complete; it is finally time to come in from the dark.", true);
            }
        }

        //Actually advance the game clock and call daily and monthly actions on all entities.
        private void advanceDay()
        {
            currentDate = currentDate.AddDays(1);
            doNextDay();
            if (currentDate.Day == 1)
            {
                doNextMonth();
            }
        }

        //Report on any news events that might have occurred during the previous phases.
        private void handleNews()
        {
            news.prepareNewspaper();
            if(currentDate.Day == 1 && canSeeThings)
            {
                news.specialEditionCheck();
            }
        }

        //Peform any final bookkeeping before returning the player to the base screen
        private void handleDailyCleanup()
        {
            List<LiberalCrimeSquad.Squad> tempSquads = new List<LiberalCrimeSquad.Squad>(lcs.squads);

            //Clear squad targets and clean out dead or disbanded Liberals
            foreach (LiberalCrimeSquad.Squad squad in tempSquads)
            {
                List<Entity> squadMembers = new List<Entity>(squad);
                squad.target = null;
                foreach(Entity e in squadMembers)
                {
                    if (!e.getComponent<Body>().Alive || e.getComponent<Liberal>().disbanded)
                        squad.Remove(e);
                }
            }

            foreach(Entity e in lcs.getAllMembers())
            {
                e.getComponent<CreatureInfo>().flags &= ~(CreatureInfo.CreatureFlag.CONVERTED | CreatureInfo.CreatureFlag.JUST_ESCAPED);

                //Clean up any vehicle assignments that are no longer valid
                if (e.getComponent<Inventory>().vehicle != null && !e.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().getInventory().Contains(e.getComponent<Inventory>().vehicle))
                {
                    if (e.getComponent<Inventory>().vehicle.getComponent<Vehicle>().preferredDriver == e)
                        e.getComponent<Inventory>().vehicle.getComponent<Vehicle>().preferredDriver = null;
                    e.getComponent<Inventory>().vehicle = null;
                }

                e.getComponent<Inventory>().tempVehicle = null;

                //Remove libs from squads if they are not active
                if (e.getComponent<Liberal>().squad != null && e.getComponent<Liberal>().status != Liberal.Status.ACTIVE)
                    e.getComponent<Liberal>().squad.Remove(e);

                //Remove paralyzed, wheelchair-less Liberals from squads - they need to get a wheelchair before they can go cause trouble
                if (!e.getComponent<Body>().canWalk() && (e.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.WHEELCHAIR) == 0)
                    if (e.getComponent<Liberal>().squad != null) e.getComponent<Liberal>().squad.Remove(e);

                //Remove dead Liberals from the LCS and handle subordinate promotions/leaving the LCS
                if (!e.getComponent<Body>().Alive)
                    e.getComponent<Liberal>().leaveLCS();
            }

            List<Entity> entities = new List<Entity>(PersistentEntityList.Values);

            foreach(Entity e in entities)
            {
                if(e.hasComponent<ItemBase>() && e.getComponent<ItemBase>().Location == null)
                {
                    addErrorMessage("Item " + e.def + ":" + e.guid + " had null location and was destroyed");
                    e.depersist();
                }

                if(e.hasComponent<CreatureBase>() && !e.hasComponent<Politician>() && e.getComponent<CreatureBase>().Location == null)
                {
                    addErrorMessage("Creature " + e.def + ":" + e.guid + " had null location and was destroyed");
                    e.depersist();
                }
            }

            news.stories.Clear();
            
            //Autosaves now will only happen at the end of the month to improve performance. However, in the nextPhase() method it will also
            //save the game state on entering a new BASE phase if it had left the basemode screen before (this slowdown would be less noticable and it is also an indicator that the player actually did something besides skip ahead)
            //GameData.getData().saveGame();
            if (currentDate.Day == 1)
            {
                GameData.getData().saveGame();
                GameData.getData().saveToDisk();
            }

            //Check for game over
            if(endGameState != EndGame.NONE)
            {
                //TODO: Other lose states
                switch (endGameState)
                {
                    case EndGame.DEAD:
                        gameOver("The Liberal Crime Squad was KIA in " + currentDate.Year);
                        break;
                    case EndGame.POLICE:
                        gameOver("The Liberal Crime Squad was brought to justice in " + currentDate.Year);
                        break;
                    case EndGame.CIA:
                        gameOver("The Liberal Crime Squad was blotted out in " + currentDate.Year);
                        break;
                    case EndGame.HICKS:
                        gameOver("The Liberal Crime Squad was mobbed in " + currentDate.Year);
                        break;
                    case EndGame.CORP:
                        gameOver("The Liberal Crime Squad was downsized in " + currentDate.Year);
                        break;
                    case EndGame.FIREMEN:
                        gameOver("The Liberal Crime Squad was burned in " + currentDate.Year);
                        break;
                    case EndGame.EXECUTED:
                        gameOver("The Liberal Crime Squad was executed in " + currentDate.Year);
                        break;
                    case EndGame.REAGAN:
                        gameOver("The country was Reaganified in " + currentDate.Year);
                        break;
                    case EndGame.CCS:
                        gameOver("The Liberal Crime Squad was out Crime-Squadded in " + currentDate.Year);
                        break;
                    case EndGame.DISBANDLOSS:
                        gameOver("The Liberal Crime Squad was permanently disbanded in " + currentDate.Year);
                        break;
                    case EndGame.WON:
                        //TODO: One final overview of all laws at L++ level
                        gameOver("Liberalized the country in " + currentDate.Year);
                        break;
                }
            }
        }

        private string dailyAffirmation()
        {
            string preamble = "It is " + (isFuture()?"CYBER ":"") + currentDate.ToString("D") + "; ";

            if (currentDate.Month == 11 && currentDate.DayOfWeek == DayOfWeek.Tuesday && currentDate.Day <= 8 && currentDate.Day > 1 && currentDate.Year % 4 == 0)
                return preamble + "<color=cyan>Election Day today, don't forget to vote!</color>";

            if (currentDate.Month == 11 && currentDate.DayOfWeek == DayOfWeek.Tuesday && currentDate.Day <= 8 && currentDate.Day > 1 && currentDate.Year % 2 == 0)
                return preamble + "<color=cyan>Midterms today, don't forget to vote!</color>";

            DateTime founderBirthday = worldState.getComponent<LiberalCrimeSquad>().founder.getComponent<Age>().birthday;

            if (currentDate.Month == founderBirthday.Month && currentDate.Day == founderBirthday.Day)
                return preamble + "<color=lime>happy birthday to our dear Founder!</color>";

            if (currentDate.DayOfWeek == DayOfWeek.Wednesday && LCSRandom(100) == 0)
                return preamble + "my dudes";

            if (currentDate.Month == 4 && currentDate.Day >= 24 && currentDate.DayOfWeek == DayOfWeek.Friday)
                return preamble + "a Liberal green Arbor Day.";
            else if (currentDate.Month == 5 && currentDate.Day == 1)
                return preamble + "a glorious May Day, comrade!";
            else if (currentDate.Month == 9 && currentDate.Day <= 7 && currentDate.DayOfWeek == DayOfWeek.Monday)
                return preamble + "a lovely Liberal Labor Day awaits you.";
            else if (currentDate.Month == 10 && currentDate.Day >= 8 && currentDate.Day <= 14 && currentDate.DayOfWeek == DayOfWeek.Monday)
                return preamble + "a national holiday dedicated to a Conservative tyrant.";
            else if (currentDate.Month == 12 && currentDate.Day == 25)
                return preamble + "'tis the season for crass commercialism and religious intolerance.";
            else if (currentDate.Month == 1 && currentDate.Day == 1)
                return preamble + "a Liberal new year to all!";

            switch (LCSRandom(5))
            {
                case 0:
                    return preamble + "a new Liberal dawn awaits.";
                case 1:
                    return preamble + "the citadels of Conservatism will soon crumble to dust.";
                case 2:
                    return preamble + "the masses still yearn for change.";
                case 3:
                    return preamble + "the light of Liberalism burns bright in the darkness.";
                case 4:
                    return preamble + "the revolution tuns.";
            }

            return preamble + "and I apparently forgot how to count.";
        }

        #region events
        public event EventHandler nextDay;
        private void doNextDay()
        {
            nextDay(this, null);
        }

        public event EventHandler nextMonth;
        private void doNextMonth()
        {
            nextMonth(this, null);
        }

        public event EventHandler someoneDied;
        public void doSomeoneDied(Entity sender)
        {
            if(someoneDied != null)
                someoneDied(sender, null);
        }
        #endregion

        // ///////////////////////////////////////Miscellaneous Useful Functions///////////////////////////////////////////////// //
        #region miscFunctions
        public bool isFuture()
        {
            return currentDate.Year >= int.Parse(GameData.getData().globalVarsList["FUTUREYEAR"]);
        }

        public T WeightedRandom<T>(Dictionary<T,int> weightedList)
        {
            if (weightedList.Count == 0) return default(T);

            int totalWeight = 0;
            foreach (int weight in weightedList.Values) totalWeight += weight;

            int selection = LCSRandom(totalWeight);

            foreach(T o in weightedList.Keys)
            {
                selection -= weightedList[o];
                if (selection < 0) return o;
            }

            //This should never happen
            addErrorMessage("Weighted random overflow error");
            return default(T);
        }

        public int LCSRandom(int range)
        {
            if (range < 0)
                return -rand.Next(Math.Abs(range));
            else
                return rand.Next(range);
        }

        public int LCSRandom(string interval)
        {
            int max = 0;
            int min = 0;

            int dashpos = interval.IndexOf('-');
            if (dashpos == -1 || dashpos == 0) // Just a constant.
            {
                max = min = int.Parse(interval);
            }
            else
            {
                string smin = interval.Substring(0, dashpos);
                string smax = interval.Substring(dashpos + 1);
                min = int.Parse(smin);
                max = int.Parse(smax);
            }

            return LCSRandom(max - min + 1) + min;
        }

        public T pickRandom<T>(List<T> list)
        {
            return list[LCSRandom(list.Count)];
        }

        public bool testCondition(string condition, Component component = null)
        {
            if (condition == "") return true;

            bool returnValue;
            bool moreConditions;

            string individualCondition;
            char[] splitChars = { '&', '|' };
            int index = condition.IndexOfAny(splitChars);

            if(index == -1)
            {
                return conditionCheck(condition, component);
            }
            else
            {
                moreConditions = true;
                individualCondition = condition.Substring(0, index);
                condition = condition.Substring(index);
                returnValue = conditionCheck(individualCondition, component);
            }

            while (moreConditions)
            {
                char splitChar = condition[0];
                condition = condition.Substring(1);
                index = condition.IndexOfAny(splitChars);

                if (index == -1)
                {
                    moreConditions = false;
                    if (splitChar == '&')
                        returnValue &= conditionCheck(condition, component);
                    else if (splitChar == '|')
                        returnValue |= conditionCheck(condition, component);
                }
                else
                {
                    individualCondition = condition.Substring(0, index);
                    condition = condition.Substring(index);
                    if (splitChar == '&')
                        returnValue &= conditionCheck(individualCondition, component);
                    else if (splitChar == '|')
                        returnValue |= conditionCheck(individualCondition, component);
                }
            }

            return returnValue;
        }

        private bool conditionCheck(string condition, Component component)
        {
            string[] conditionSplit = condition.Split(':');

            string type = conditionSplit[0];
            string name = conditionSplit[1];
            string op = conditionSplit[2];
            int intValue;
            bool boolValue;

            bool returnValue = true;

            switch (type)
            {
                case "LAW":
                        intValue = int.Parse(conditionSplit[3]);
                        returnValue = stringToOperator(op, (int)worldState.getComponent<Government>().laws[name].alignment, intValue);
                    break;
                case "VAR":
                    if (component == null)
                    {
                        GetMC().addErrorMessage("VAR condition: " + condition + " missing component to test.");
                        return true;
                    }

                    if (component.GetType().GetField(name).FieldType == typeof(int))
                    {
                        intValue = int.Parse(conditionSplit[3]);
                        returnValue = stringToOperator(op, (int)component.GetType().GetField(name).GetValue(component), intValue);
                    }
                    else if (component.GetType().GetField(name).FieldType == typeof(bool))
                    {
                        boolValue = bool.Parse(conditionSplit[3]);
                        returnValue = stringToOperator(op, (bool)component.GetType().GetField(name).GetValue(component), boolValue);
                    }
                    break;
                case "SPEC":
                    if(name == "RESTRICTED")
                    {
                        if (currentSiteModeScene == null)
                        {
                            GetMC().addErrorMessage("SPEC condition: " + condition + " missing site mode scene to test.");
                            return true;
                        }
                        else
                        {
                            boolValue = bool.Parse(conditionSplit[3]);
                            returnValue = stringToOperator(op, (bool)currentSiteModeScene.getSquadTile().getComponent<TileBase>().restricted, boolValue);
                        }
                    }
                    break;
            }

            return returnValue;
        }

        public string swearFilter(string badWords, string replacement)
        {
            if (worldState.getComponent<Government>().laws["FREE_SPEECH"].alignment == Alignment.ARCHCONSERVATIVE) return "[" + replacement + "]";
            else return badWords;
        }

        public string getTranslation(string key)
        {
            string translation = key;

            if (GameData.getData().translationList.ContainsKey(key))
            {
                translation = GameData.getData().translationList[key];
            }
            else
            {
                addDebugMessage("Missing translation reference: " + key);
            }

            return translation;
        }

        public static bool stringToOperator(string op, int x, int y)
        {
            switch (op)
            {
                case "<":
                    return x < y;
                case ">":
                    return x > y;
                case "=":
                case "==":
                    return x == y;
                case "<=":
                    return x <= y;
                case ">=":
                    return x >= y;
                case "!=":
                    return x != y;
                default:
                    return false;
            }
        }

        public static bool stringToOperator(string op, bool x, bool y)
        {
            switch (op)
            {
                case "=":
                case "==":
                    return x == y;
                case "!=":
                    return x != y;
                default:
                    return false;
            }
        }

        public static string shortOrdinal(int num)
        {
            if (num <= 0) return num.ToString();

            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                    return num + "th";
            }

            switch (num % 10)
            {
                case 1:
                    return num + "st";
                case 2:
                    return num + "nd";
                case 3:
                    return num + "rd";
                default:
                    return num + "th";
            }

        }

        public static string ordinal(int number)
        {
            if (number == 0)
                return "zeroth";

            if (number < 0)
                return "What you're asking for makes no sense";

            string words = "";

            if ((number / 1000000) > 0)
            {
                words += NumberToWords(number / 1000000) + " Million ";
                number %= 1000000;
            }

            if ((number / 1000) > 0)
            {
                words += NumberToWords(number / 1000) + " Thousand ";
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                words += NumberToWords(number / 100) + " Hundred ";
                number %= 100;
            }

            if (number > 0)
            {
                if (words != "")
                    words += "and ";

                var unitsMap = new[] { "Zeroth", "First", "Second", "Third", "Fourth", "Fifth", "Sixth", "Seventh", "Eighth", "Ninth", "Tenth", "Eleventh", "Twelfth", "Thirteenth", "Fourteenth", "Fifteenth", "Sixteeth", "Seventeenth", "Eighteenth", "Nineteenth" };
                var tensMapZero = new[] { "Zeroth", "Tenth", "Twentieth", "Thirtieth", "Fortieth", "Fiftieth", "Sixtieth", "Seventieth", "Eightieth", "Ninetieth" };
                var tensMap = new[] { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

                if (number < 20)
                    words += unitsMap[number];
                else
                {
                    if ((number % 10) > 0)
                    {
                        words += tensMap[number / 10];
                        words += "-" + unitsMap[number % 10];
                    }
                    else
                    {
                        words += tensMapZero[number / 10];
                    }
                }
            }
            else
            {
                words += "th";
            }

            return words;
        }

        public static string NumberToWords(int number)
        {
            if (number == 0)
                return "Zero";

            if (number < 0)
                return "Minus " + NumberToWords(Math.Abs(number));

            string words = "";

            if ((number / 1000000) > 0)
            {
                words += NumberToWords(number / 1000000) + " Million ";
                number %= 1000000;
            }

            if ((number / 1000) > 0)
            {
                words += NumberToWords(number / 1000) + " Thousand ";
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                words += NumberToWords(number / 100) + " Hundred ";
                number %= 100;
            }

            if (number > 0)
            {
                if (words != "")
                    words += "and ";

                var unitsMap = new[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
                var tensMap = new[] { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

                if (number < 20)
                    words += unitsMap[number];
                else
                {
                    words += tensMap[number / 10];
                    if ((number % 10) > 0)
                        words += "-" + unitsMap[number % 10];
                }
            }

            return words;
        }
        #endregion
    }

    public class ActionQueue : List<ActionQueue>
    {
        public Action action { get; set; }
        public string description { get; set; }
        public bool blocker { get; set; }

        public ActionQueue(Action action, string description, bool blocker = false) : base()
        {
            this.action = action;
            this.description = description;
            this.blocker = blocker;
        }

        public ActionQueue Add(Action action, string description, bool blocker = false)
        {
            ActionQueue newAction = new ActionQueue(action, description, blocker);
            Add(newAction);
            return newAction;
        }
    }

    public enum Alignment
    {
        ARCHCONSERVATIVE = -2,
        CONSERVATIVE,
        MODERATE = 0,
        LIBERAL,
        ELITE_LIBERAL
    }

    public enum Difficulty
    {
        AUTOMATIC = 1,
        VERYEASY = 3,
        EASY = 5,
        AVERAGE = 7,
        CHALLENGING = 9,
        HARD = 11,
        FORMIDABLE = 13,
        HEROIC = 15,
        SUPERHEROIC = 17,
        IMPOSSIBLE = 19
    }

    public enum Direction
    {
        UP,
        DOWN,
        LEFT,
        RIGHT
    }

    public class Position
    {
        public int x { get; set; }
        public int y { get; set; }
        public int z { get; set; }

        public Position(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Position(Position position)
        {
            this.x = position.x;
            this.y = position.y;
            this.z = position.z;
        }

        public bool samePos(Position pos)
        {
            return x == pos.x && y == pos.y && z == pos.z;
        }
    }
}
