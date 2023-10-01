using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Location;
using LCS.Engine.Components.Item;
using LCS.Engine.UI;
using LCS.Engine.Data;

namespace LCS.Engine.Scenes
{
    public class SiteModeScene
    {
        public Position squadPosition { get; set; }
        public int suspicionTimer { get; set; }
        public int alarmTimer { get; set; }
        public bool alarmTriggered { get; set; }
        public int siteAlienate { get; set; }
        public int siteCrime { get; set; }
        public int encounterTimer { get; set; }
        public bool encounterWarnings { get; set; }
        public bool processingRound { get; set; }
        public bool bankRobbed { get; set; }
        private int swatCounter = 0;

        private LiberalCrimeSquad.Squad squad;
        public Entity location { get; set; }
        public List<MapEnemy> enemies { get; set; }

        public bool inEncounter;
        public List<Entity> encounterEntities;
        private bool attacked;
        private ActionQueue sceneRoot;
        private int lootedCash;

        private int siegeKills;
        private int siegeAttackTime;
        private bool forceFinish = false;
        private bool rejectedByBouncer = false;
        private List<Entity> siegeBuffer;

        private int bloodyShoes = 0;

        public void startCausingTrouble(LiberalCrimeSquad.Squad squad, Entity target)
        {
            this.squad = squad;
            location = target;
            inEncounter = false;
            siteCrime = 0;
            alarmTriggered = false;
            alarmTimer = 0;
            siteAlienate = 0;
            suspicionTimer = -1;
            encounterTimer = 0;
            encounterEntities = new List<Entity>();
            encounterWarnings = false;
            attacked = false;
            processingRound = false;
            lootedCash = 0;
            enemies = new List<MapEnemy>();
            siegeBuffer = new List<Entity>();
            bankRobbed = false;
            swatCounter = 0;

            MasterController mc = MasterController.GetMC();
            mc.someoneDied += doSomeoneDied;
            sceneRoot = mc.createSubQueue(() =>
            {
                mc.currentSiteModeScene = this;
                squad.startCausingTrouble();
                mc.uiController.closeUI();

                target.getComponent<TroubleSpot>().resetMap();

                squadPosition = new Position(target.getComponent<TroubleSpot>().startX, target.getComponent<TroubleSpot>().startY, target.getComponent<TroubleSpot>().startZ);

                tryMove(squadPosition, false);

                MasterController.news.startNewStory("SQUAD_SITE", target);
                mc.uiController.siteMode.buildMap(target, target.getComponent<TroubleSpot>().startZ);
                mc.uiController.siteMode.show();
                mc.uiController.squadUI.displaySquad(squad);
            }, "Cause Trouble", mc.doNextAction, "End Trouble", mc.getNextAction(), true);
        }

        public void startSiege(LiberalCrimeSquad.Squad squad, Entity siegeLocation)
        {
            this.squad = squad;
            location = siegeLocation;
            inEncounter = false;
            siteCrime = 0;
            alarmTriggered = true;
            alarmTimer = 0;
            siteAlienate = 0;
            suspicionTimer = -1;
            encounterTimer = 0;
            encounterEntities = new List<Entity>();
            encounterWarnings = false;
            attacked = false;
            processingRound = false;
            lootedCash = 0;
            siegeKills = 0;
            siegeAttackTime = 0;
            enemies = new List<MapEnemy>();
            siegeBuffer = new List<Entity>();

            MasterController mc = MasterController.GetMC();
            mc.someoneDied += doSomeoneDied;
            sceneRoot = mc.createSubQueue(() =>
            {
                squad.startCausingTrouble();
                mc.uiController.closeUI();

                if (siegeLocation.getComponent<SafeHouse>().siegeType != LocationDef.EnemyType.POLICE)
                    mc.combatModifiers = MasterController.CombatModifiers.NOCHARGES;
                
                siegeLocation.getComponent<TroubleSpot>().resetMap(true);

                if (siegeLocation.getComponent<TroubleSpot>().map.Count == 1)
                {
                    do
                    {
                        squadPosition = new Position(mc.LCSRandom(25), 15 - mc.LCSRandom(3), 0);
                    } while (!siegeLocation.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x, squadPosition.y].getComponent<TileBase>().isWalkable());
                }
                else
                {
                    do
                    {
                        int floor = mc.LCSRandom(siegeLocation.getComponent<TroubleSpot>().map.Count);
                        int x = mc.LCSRandom(siegeLocation.getComponent<TroubleSpot>().map[floor].GetLength(0));
                        int y = mc.LCSRandom(siegeLocation.getComponent<TroubleSpot>().map[floor].GetLength(1));

                        squadPosition = new Position(x, y, floor);
                    } while (!siegeLocation.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x, squadPosition.y].getComponent<TileBase>().isWalkable());

                }

                tryMove(squadPosition, false);

                if((siegeLocation.getComponent<SafeHouse>().investments & SafeHouse.Investments.TRAPS) != 0)
                {
                    int TRAPNUM = 30;

                    for(int i = 0; i < TRAPNUM; i++)
                    {
                        Position trapPosition = new Position(-1, -1, -1);

                        do
                        {
                            trapPosition = new Position(mc.LCSRandom(siegeLocation.getComponent<TroubleSpot>().map[0].GetLength(0)), mc.LCSRandom(siegeLocation.getComponent<TroubleSpot>().map[0].GetLength(1)), 0);
                        } while (siegeLocation.getComponent<TroubleSpot>().map[trapPosition.z][trapPosition.x, trapPosition.y].getComponent<TileBase>().trapped ||
                                !siegeLocation.getComponent<TroubleSpot>().map[trapPosition.z][trapPosition.x, trapPosition.y].getComponent<TileBase>().isWalkable() ||
                                trapPosition.samePos(new Position(siegeLocation.getComponent<TroubleSpot>().startX, siegeLocation.getComponent<TroubleSpot>().startY, siegeLocation.getComponent<TroubleSpot>().startZ)));

                        siegeLocation.getComponent<TroubleSpot>().map[trapPosition.z][trapPosition.x, trapPosition.y].getComponent<TileBase>().trapped = true;
                    }
                }

                int ENEMY_COUNT = 6;

                List<Position> freePositions = new List<Position>();
                for(int x = (siegeLocation.getComponent<TroubleSpot>().map[0].GetLength(0) / 2) - 5; x < (siegeLocation.getComponent<TroubleSpot>().map[0].GetLength(0) / 2) + 5; x++)
                {
                    for(int y = 0; y < 8; y++)
                    {
                        if (siegeLocation.getComponent<TroubleSpot>().map[0][x, y].getComponent<TileBase>().isWalkable() &&
                         siegeLocation.getComponent<TroubleSpot>().map[0][x, y].getComponent<TileFloor>().type != TileFloor.Type.EXIT &&
                         !siegeLocation.getComponent<TroubleSpot>().map[0][x, y].getComponent<TileBase>().trapped)
                            freePositions.Add(new Position(x, y, 0));
                    }
                }

                for(int i = 0; i < ENEMY_COUNT; i++)
                {
                    if (freePositions.Count == 0) break;
                    int pos = mc.LCSRandom(freePositions.Count);

                    Position enemyPosition = freePositions[pos];
                    freePositions.RemoveAt(pos);

                    MapEnemy enemy = new MapEnemy();
                    enemy.type = MapEnemy.EnemyType.NORMAL;
                    enemy.position = enemyPosition;
                    enemies.Add(enemy);
                }

                if(siegeLocation.getComponent<SafeHouse>().siegeType == LocationDef.EnemyType.POLICE &&
                    siegeLocation.getComponent<SafeHouse>().escalation >= SafeHouse.SiegeEscalation.TANKS &&
                    (siegeLocation.getComponent<SafeHouse>().investments & SafeHouse.Investments.TANK_TRAPS) == 0)
                {
                    MapEnemy enemy = new MapEnemy();
                    enemy.type = MapEnemy.EnemyType.HEAVY;
                    enemy.position = new Position(siegeLocation.getComponent<TroubleSpot>().startX, siegeLocation.getComponent<TroubleSpot>().startY, siegeLocation.getComponent<TroubleSpot>().startZ);
                    enemies.Add(enemy);
                }

                foreach(Entity e in siegeLocation.getComponent<SafeHouse>().getBasedLiberals())
                {
                    if (!squad.Contains(e)) siegeBuffer.Add(e);
                }

                if(siegeLocation.getComponent<SafeHouse>().siegeType == LocationDef.EnemyType.POLICE)
                    MasterController.news.startNewStory("SQUAD_FLEDATTACK", siegeLocation);

                mc.uiController.siteMode.buildMap(siegeLocation, squadPosition.z);
                mc.uiController.siteMode.show();
                mc.uiController.squadUI.displaySquad(squad);
            }, "Defend Siege", mc.doNextAction, "End Defend");

            mc.addCombatMessage("<color=red>UNDER ATTACK: ESCAPE OR ENGAGE</color>\nYou are about to engage Conservative forces in battle. You will find yourself in the Liberal safehouse, and it will be swarming with Conservative units. The Liberal Crime Squad will be located far from the entrance to the safehouse. It is your task to bring your squad out to safety, or fight off the Conservatives within the perimeter. Either way you choose, any equipment from the safehouse which isn't held by a Liberal will be scattered about the compound. Save what you can.", true);
        }

        public void finishSiege()
        {
            MasterController mc = MasterController.GetMC();

            squad.goHome();

            location.getComponent<TroubleSpot>().updateGraffitiList();

            squad = null;
            location = null;
            sceneRoot = null;

            MasterController.lcs.changeFunds(lootedCash);
            mc.someoneDied -= doSomeoneDied;

            mc.addAction(() =>
            {
                mc.doNextAction();
            }, "next action");

            mc.endEncounter();
            mc.doNextAction(true);
        }

        public void finishTrouble()
        {
            MasterController mc = MasterController.GetMC();

            if (siteAlienate > 0) MasterController.news.currentStory.positive = false;

            if (!location.hasComponent<SafeHouse>() || !location.getComponent<SafeHouse>().owned)
            {
                if (siteCrime > 5 + mc.LCSRandom(95))
                {
                    if (location.hasComponent<SafeHouse>() && (location.getComponent<SafeHouse>().getFlags() & LocationDef.BaseFlag.CAPTURABLE) != 0)
                    {
                        captureSafeHouse("You've captured " + location.getComponent<SiteBase>().getCurrentName() + " for use as a Safehouse!");
                    }
                    else {
                        location.getComponent<TroubleSpot>().closed = siteCrime / 10;
                        if ((location.getComponent<TroubleSpot>().getFlags() &
                            LocationDef.TroubleSpotFlag.HIGH_SECURITY | LocationDef.TroubleSpotFlag.MID_SECURITY) != 0)
                        {
                            location.getComponent<TroubleSpot>().highSecurity = 60;
                        }
                    }
                }
                else if (siteCrime > 10)
                {
                    if ((location.getComponent<TroubleSpot>().getFlags() &
                        LocationDef.TroubleSpotFlag.HIGH_SECURITY | LocationDef.TroubleSpotFlag.MID_SECURITY) != 0)
                    {
                        location.getComponent<TroubleSpot>().highSecurity = siteCrime;
                    }
                    else
                    {
                        location.getComponent<TroubleSpot>().closed = 7;
                    }
                }
            }

            foreach(Entity e in squad)
            {
                if (e == null)
                    squad.Remove(e);
            }

            location.getComponent<TroubleSpot>().updateGraffitiList();

            squad = null;
            location = null;
            sceneRoot = null;

            MasterController.lcs.changeFunds(lootedCash);
            mc.someoneDied -= doSomeoneDied;

            mc.endEncounter();
            mc.doNextAction(true);
        }

        public void doSomeoneDied(object sender, EventArgs args)
        {
            Entity whoDied = (Entity)sender;

            if(!whoDied.hasComponent<Liberal>())
            {
                siteCrime += 10;
                if ((GameData.getData().creatureDefList[whoDied.def].flags & CreatureDef.CreatureFlag.ARCHCONSERVATIVE) != 0)
                    siteCrime += 30;

                if(whoDied.def == "CCS_ARCHCONSERVATIVE" && 
                    location.hasComponent<SafeHouse>() && 
                    !location.getComponent<SafeHouse>().owned)
                {
                    forceFinish = true;
                    captureSafeHouse("<color=lime>The CCS has been broken!</color>\nYou've captured " + location.getComponent<SiteBase>().getCurrentName() + " for use as a Safehouse!");
                    MasterController.ccs.baseKills++;
                    MasterController.ccs.status--;

                    if(MasterController.ccs.baseKills >= 3)
                    {
                        MasterController.GetMC().addMessage("<color=lime>The CCS Founder lying dead at their feet, the squad slips away. With its Founder killed in the heart of their own base, the last of the enemy's morale and confidence is shattered.</color>\nThe CCS has been completely destroyed. Now wasn't there a revolution to attend to?", true);
                        MasterController.ccs.destroyCCS();
                        foreach(Entity e in MasterController.lcs.getAllMembers())
                        {
                            e.getComponent<CreatureBase>().juiceMe(200);
                        }
                    }

                    for (int z = 0; z < location.getComponent<TroubleSpot>().map.Count; z++)
                        for (int x = 0; x < location.getComponent<TroubleSpot>().map[z].GetLength(0); x++)
                            for (int y = 0; y < location.getComponent<TroubleSpot>().map[z].GetLength(1); y++)
                            {
                                foreach (Entity i in location.getComponent<TroubleSpot>().map[z][x, y].getComponent<TileBase>().loot)
                                {
                                    location.getComponent<SafeHouse>().addItemToInventory(i);
                                }

                                MasterController.lcs.changeFunds(location.getComponent<TroubleSpot>().map[z][x, y].getComponent<TileBase>().cash);
                            }
                }
            }
            if(location.hasComponent<SafeHouse>() && location.getComponent<SafeHouse>().underSiege)
            {
                if (!whoDied.hasComponent<Liberal>())
                {
                    siegeKills++;
                }
            }

            location.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x, squadPosition.y].getComponent<TileBase>().someoneDiedHere = true;
        }

        public void wait()
        {
            startRound();
            if (!inEncounter)
            {
                newEncounter(false);
                if (location.hasComponent<SafeHouse>() && location.getComponent<SafeHouse>().underSiege)
                    moveSiegeEnemies();
            }
            else
            {
                sceneRoot.Add(disguiseCheck, "Disguise Check");
                string squadTileSpecial = "";
                bool noLeaveEncounter = false;
                if (getSquadTile().hasComponent<TileSpecial>())
                    squadTileSpecial = getSquadTile().getComponent<TileSpecial>().name;

                switch (squadTileSpecial)
                {
                    case "CLUB_BOUNCER":
                        if (!getSquadTile().getComponent<TileSpecial>().used)
                            noLeaveEncounter = true;
                        break;
                    case "BANK_TELLER":
                        if (!getSquadTile().getComponent<TileSpecial>().used)
                            noLeaveEncounter = true;
                        break;
                }

                if (!alarmTriggered && !noLeaveEncounter)
                {
                    sceneRoot.Add(() =>
                    {
                        if (!alarmTriggered)
                        {
                            string encounterText;

                            if (encounterEntities.Count == 1)
                            {
                                encounterText = encounterEntities[0].getComponent<CreatureInfo>().heShe();
                            }
                            else if (encounterEntities.Count <= 6)
                            {
                                encounterText = "The group";
                            }
                            else
                            {
                                encounterText = "The crowd";
                            }
                            encounterText += " moves along.";
                            MasterController.GetMC().addCombatMessage(encounterText);

                            leaveEncounter();
                            MasterController.GetMC().doNextAction();
                        }
                    }, "Leave Encounter");
                }
            }
            sceneRoot.Add(()=>endRound(), "End Round");
            MasterController.GetMC().doNextAction();
        }

        public void move(string dir)
        {
            switch (dir)
            {
                case "UP":
                    if (squadPosition.y > 0)
                        tryMove(new Position(squadPosition.x, squadPosition.y - 1, squadPosition.z));
                    break;
                case "DOWN":
                    if (squadPosition.y < location.getComponent<TroubleSpot>().map[squadPosition.z].GetLength(1) - 1)
                        tryMove(new Position(squadPosition.x, squadPosition.y + 1, squadPosition.z));
                    break;
                case "LEFT":
                    if (squadPosition.x > 0)
                        tryMove(new Position(squadPosition.x - 1, squadPosition.y, squadPosition.z));
                    break;
                case "RIGHT":
                    if (squadPosition.x < location.getComponent<TroubleSpot>().map[squadPosition.z].GetLength(0) - 1)
                        tryMove(new Position(squadPosition.x + 1, squadPosition.y, squadPosition.z));
                    break;
            }
        }

        public void fight()
        {
            if (!attacked)
            {
                attacked = true;
                foreach (Entity e in squad)
                {
                    AttackDef.DamageType damageType = e.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getAttack().damage_type;

                    if (e.getComponent<Body>().Alive && !(damageType == AttackDef.DamageType.MUSIC || damageType == AttackDef.DamageType.PERSUASION))
                    {
                        if (e.getComponent<Inventory>().weapon == null)
                            e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_ASSAULT);
                        else
                            e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_ARMED_ASSAULT);
                    }
                }
            }

            startRound();

            bool subdued = false;
            if (encounterHasPolice())
            {
                foreach(Entity e in encounterEntities)
                {
                    if (e == null) continue;
                    if ((e.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.POLICE) != 0 &&
                        e.getComponent<Body>().Blood > 60)
                    {
                        subdued = true;
                        foreach(Entity l in squad)
                        {
                            if (l == null) continue;
                            if(l.getComponent<Body>().Blood > 40)
                            {
                                subdued = false;
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            if (subdued)
            {
                foreach (Entity item in squad.inventory)
                {
                    foreach (Entity e in squad)
                    {
                        e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_THEFT);
                    }
                }

                MasterController.GetMC().addCombatMessage("The Police subdue and arrest your squad.", true);

                List<Entity> tempSquad = new List<Entity>(squad);

                foreach (Entity e in tempSquad)
                {
                    e.getComponent<CriminalRecord>().arrest();
                }
            }
            else
            {
                Fight.youFight(squad, encounterEntities, sceneRoot);
                Fight.theyFight(squad, encounterEntities, sceneRoot);
            }
            sceneRoot.Add(()=>endRound(), "End Round");
            MasterController.GetMC().doNextAction();
        }

        public void releaseOppressed()
        {
            MasterController mc = MasterController.GetMC();
            
            List<UI.PopupOption> options = new List<UI.PopupOption>();
            processingRound = true;
            options.Add(new PopupOption("Yes", () =>
            {
                startRound();

                handleRelease(true);

                mc.addToCurrentQueue(() => { disguiseCheck(); }, "Disguise Check");
                mc.addToCurrentQueue(() => endRound(), "End Round");
                mc.doNextAction();
            }));

            options.Add(new PopupOption("No", () =>
            {
                startRound();

                handleRelease(false);

                mc.addToCurrentQueue(() => { disguiseCheck(); }, "Disguise Check");
                mc.addToCurrentQueue(() => endRound(), "End Round");
                mc.doNextAction();
            }));

            if (squad.Count < 6)
            {
                mc.uiController.showYesNoPopup("Have them join the Liberal Crime Squad?", options);
            }
            else
            {
                startRound();

                handleRelease(false);

                mc.addToCurrentQueue(() => { disguiseCheck(); }, "Disguise Check");
                mc.addToCurrentQueue(() => endRound(), "End Round");
                mc.doNextAction();
            }
        }

        private void handleRelease(bool join)
        {
            MasterController mc = MasterController.GetMC();

            if (encounterHasEnemies())
            {
                alarmTriggered = true;
            }

            List<Entity> freedLibs = new List<Entity>();

            for (int i = 0; i < encounterEntities.Count; i++)
            {
                if (encounterEntities[i] == null) continue;

                if((encounterEntities[i].getComponent<CreatureInfo>().encounterName == "Prisoner" &&
                    encounterEntities[i].getComponent<CreatureInfo>().alignment == Alignment.LIBERAL) ||
                    (encounterEntities[i].getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.FREEABLE) != 0)
                {
                    freedLibs.Add(encounterEntities[i]);
                    if (encounterEntities[i].getComponent<CreatureInfo>().encounterName == "Prisoner")
                    {
                        alarmTriggered = true;
                        encounterEntities[i].getComponent<CriminalRecord>().addCrime(Constants.CRIME_ESCAPE);
                    }
                    encounterEntities[i] = null;
                }
            }

            foreach(Entity e in squad)
            {
                if(e!= null)
                {
                    e.getComponent<CreatureBase>().juiceMe(freedLibs.Count, 50);
                }
            }

            int joinCount = 0;
            int time = 20 + mc.LCSRandom(10);
            if (time < 1) time = 1;
            if (suspicionTimer > time || suspicionTimer == -1) suspicionTimer = time;

            if (join)
            {
                foreach(Entity e in freedLibs)
                {
                    foreach(Entity l in squad)
                    {
                        if (l.getComponent<Liberal>().canRecruit() && squad.Count < 6)
                        {
                            l.getComponent<Liberal>().recruit(e);
                            e.getComponent<Liberal>().changeSquad(squad);
                            joinCount++;
                            break;
                        }
                    }
                }
            }

            string message = "You free";
            if (freedLibs.Count > 1) message += " some oppressed Liberals";
            else message += " an oppressed Liberal";
            message += " from the Conservatives.";

            if (freedLibs.Count - joinCount > 0)
            {
                if (joinCount == 0 && freedLibs.Count > 1) message += " They all leave";
                else if (freedLibs.Count - joinCount > 1) message += " Some leave";
                else if (joinCount == 0) message += " The Liberal leaves";
                else message += " One Liberal leaves";
                message += " you, feeling safer getting out alone.";
            }

            mc.addCombatMessage(message, true);
        }

        public void bloodblast()
        {
            MasterController mc = MasterController.GetMC();
            if(getSquadTile().getComponent<TileBase>().bloodBlast == TileBase.Bloodstain.NONE)
                getSquadTile().getComponent<TileBase>().bloodBlast = (TileBase.Bloodstain)mc.LCSRandom(3) + 1;

            foreach(Entity e in squad)
            {
                if (e == null) continue;
                if(mc.LCSRandom(2) == 0)
                {
                    e.getComponent<Inventory>().getArmor().getComponent<Armor>().makeBloody();
                }
            }

            foreach(Entity e in encounterEntities)
            {
                if (e == null) continue;
                if (mc.LCSRandom(2) == 0)
                {
                    e.getComponent<Inventory>().getArmor().getComponent<Armor>().makeBloody();
                }
            }
        }

        public void talkDating(Entity lib, Entity con)
        {
            if ((con.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.NO_BLUFF) != 0) return;

            MasterController mc = MasterController.GetMC();
            startRound();
            List<Dialog> dialog = new List<Dialog>();

            int line;

            if(MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE)
            {
                line = mc.LCSRandom(3);
                string text = "";
                switch (line)
                {
                    case 0: text = "[What church do you go to?]"; break;
                    case 1: text = "[Will you marry me?]"; break;
                    case 2: text = "[Do you believe in abstinence education?]"; break;
                }
                dialog.Add(new Dialog(lib, text, lib.getComponent<CreatureInfo>().getName() + " says \"" + text + "\""));
            }
            else
            {
                line = mc.LCSRandom(47);
                string text = "";
                bool genderFemale = con.getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.FEMALE;
                switch (line)
                {
                    case 0: text = "Hey baby, you're kinda ugly. I like that."; break;
                    case 1: text = "I lost my phone number. Could I have yours?"; break;
                    case 2: text = "Hey, you wanna go rub one off?"; break;
                    case 3: text = "Hot damn. You're built like a brick shithouse, honey."; break;
                    case 4: text = "I know I've seen you on the back of a milk carton, cuz you've been missing from my life."; break;
                    case 5: text = "I'm big where it counts."; break;
                    case 6: text = "Daaaaaamn " + (genderFemale?"girl":"boy") + ", I want to wrap your legs around my face and wear you like a feed bag!"; break; // Bill Hicks
                    case 7: text = "Let's play squirrel. I'll bust a nut in your hole."; break;
                    case 8: text = "You know, if I were you, I'd have sex with me."; break;
                    case 9: text = "You don't sweat much for a fat " + (genderFemale ? "chick.":"dude."); break;
                    case 10: text = "Fuck me if I'm wrong but you want to kiss me, right?"; break;
                    case 11: text = "Your parents must be retarded, because you are special."; break;
                    case 12: text = "Let's play trains... you can sit on my face and I will chew chew chew."; break;
                    case 13: text = "Is it hot in here or is it just you?"; break;
                    case 14: text = "I may not be Fred Flintstone, but I can make your bed rock!"; break;
                    case 15: text = "What do you say we go behind a rock and get a little boulder?"; break;
                    case 16: text = "Do you have stars on your " + (genderFemale?"panties":"boxers") + "? Your ass is outta this world!"; break;
                    case 17: text = "Those pants would look great on the floor of my bedroom."; break;
                    case 18: text = "If I said you had a nice body, would you hold it against me?"; break;
                    case 19: text = "Are you tired? You've been running around in my thoughts all day."; break;
                    case 20: text = "If I could change the alphabet baby, I would put the U and I together!"; break;
                    case 21: text = "Your lips look sweet. Can I taste them?"; break;
                    case 22: text = "Nice shoes. Wanna fuck?"; break;
                    case 23: text = "Your sexuality makes me nervous and this frustrates me."; break;
                    case 24: text = "Are you Jamaican? Cuz Jamaican me horny."; break;
                    case 25: text = "Hey pop tart, fancy coming in my toaster of love?"; break;
                    case 26: text = "Wanna play army? You lie down and I'll blow you away."; break;
                    case 27: text = "Can I lick your forehead?"; break;
                    case 28: text = "I have a genital rash. Will you rub this ointment on me?"; break;
                    case 29: text = "What's your sign?"; break;
                    case 30: text = "Do you work for the post office? Because I could have sworn you were checking out my package."; break;
                    case 31: text = "I'm not the most attractive person in here, but I'm the only one talking to you."; break;
                    case 32: text = "Hi. I suffer from amnesia. Do I come here often?"; break;
                    case 33: text = "I'm new in town. Could you give me directions to your apartment?"; break;
                    case 34: text = "Stand still so I can pick you up!"; break;
                    case 35: text = "Your daddy must have been a baker, cuz you've got a nice set of buns."; break;
                    case 36: text = "If you were a laser, you'd be set on 'stunning'."; break;
                    case 37: text = "Is that a keg in your pants? Cuz I'd love to tap that ass."; break;
                    case 38: text = "If I could be anything, I'd love to be your bathwater."; break;
                    case 39: text = "Stop, drop and roll, baby. You are on fire."; break;
                    case 40: text = "Do you want to see something swell?"; break;
                    case 41: text = "Excuse me. Do you want to fuck or should I apologize?"; break;
                    case 42: text = "Say, did we go to different schools together?"; break;
                    case 43: text = "You smell... Let's go take a shower."; break;
                    case 44:
                        text = "Roses are red, violets are blue...";
                        dialog.Add(new Dialog(lib, text, lib.getComponent<CreatureInfo>().getName() + " says \"" + text + "\""));
                        text = "All my base, are belong to you.";
                        break;
                    case 45: text = "Did it hurt?";
                        dialog.Add(new Dialog(lib, text, lib.getComponent<CreatureInfo>().getName() + " says \"" + text + "\""));
                        text = "...Did what hurt?";
                        dialog.Add(new Dialog(con, text, lib.getComponent<CreatureInfo>().getName() + " says \"" + text + "\""));
                        text = "When you fell from Heaven.";
                        break;
                    case 46: text = "Holy shit you're hot! I want to have sex with you RIGHT NOW."; break;
                }

                dialog.Add(new Dialog(lib, text, lib.getComponent<CreatureInfo>().getName() + " says \"" + text + "\""));
            }

            Difficulty diff = Difficulty.HARD;

            //Same Sex seduction is harder when gay rights laws are more conservative
            //Androgenous libs need to pass an easy disguise check to be viewed as the "proper" gender for seduction
            if((lib.getComponent<CreatureInfo>().genderLiberal == con.getComponent<CreatureInfo>().genderLiberal) ||
                (lib.getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.NEUTRAL &&
                !lib.getComponent<CreatureBase>().Skills[Constants.SKILL_DISGUISE].check(Difficulty.EASY)))
            {
                //Liberals don't care about gay rights laws and are always open-minded
                //Moderates only care if the law is below Moderate
                if (con.getComponent<CreatureInfo>().alignment < Alignment.LIBERAL)
                {
                    switch (MasterController.government.laws[Constants.LAW_GAY].alignment)
                    {
                        case Alignment.ARCHCONSERVATIVE:
                            if (con.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE) diff += 4;
                            else diff += 3;
                            break;
                        case Alignment.CONSERVATIVE:
                            if (con.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE) diff += 3;
                            else diff += 1;
                            break;
                        case Alignment.MODERATE:
                            if (con.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE) diff += 2;
                            break;
                        case Alignment.LIBERAL:
                            if (con.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE) diff += 1;
                            break;
                    }
                }
            }

            if ((con.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.ARCHCONSERVATIVE) != 0)
                diff = Difficulty.HEROIC;
            if (lib.getComponent<Inventory>().getArmor().def == "ARMOR_NONE")
                diff -= 4;

            bool succeeded = lib.getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].check(diff);
            
            lib.getComponent<CreatureBase>().Skills[Constants.SKILL_SEDUCTION].addExperience(mc.LCSRandom(5) + 2);

            if (con.getComponent<Body>().getSpecies().type != "HUMAN" &&
                (con.getComponent<Body>().getSpecies().type != "DOG" ||
                MasterController.government.laws[Constants.LAW_ANIMAL_RESEARCH].alignment != Alignment.ELITE_LIBERAL))
            {
                string response = "What the " + mc.swearFilter("fuck", "heck") + " is wrong with you? I'm a " + con.getComponent<Body>().getSpecies().name.ToLower() + "!";

                dialog.Add(new Dialog(con, response , "<color=red>" + con.getComponent<CreatureInfo>().encounterName + " responds \"" + response + "\"</color>"));
                constructConversation(dialog);
                con.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;
            }
            else if (con.hasComponent<Liberal>())
            {
                dialog.Add(new Dialog(con, "Dude, you trying to blow my cover?", "<color=yellow>" + con.getComponent<CreatureInfo>().encounterName + " responds \"Dude, you trying to blow my cover?\"</color>"));
                constructConversation(dialog);
                con.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;
            }
            else {
                //Prostitutes naturally respond a bit differently to pickup attempts than others
                if (con.def == "PROSTITUTE")
                {
                    if ((lib.getComponent<Inventory>().getArmor().getComponent<Armor>().getFlags() & ItemDef.ArmorFlags.POLICE) != 0)
                    {
                        string text = "Dirty. You know that's illegal, officer.";
                        dialog.Add(new Dialog(con, text, "<color=red>" + con.getComponent<CreatureInfo>().encounterName + " responds \"" + text + "\"</color>"));
                        constructConversation(dialog);
                        con.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;
                    }
                    else
                    {
                        int price;

                        int priceClass = con.getComponent<CreatureBase>().Skills["SEDUCTION"].roll();
                        if (priceClass > (int)Difficulty.HEROIC)
                            price = mc.LCSRandom(201) + 200;
                        else
                            price = mc.LCSRandom(10 * priceClass) + 10 * priceClass;

                        if (succeeded)
                        {
                            string text = "Oooh, this one's on me, baby.";
                            string makePlans = "\n" + lib.getComponent<CreatureInfo>().getName() + " and " + con.getComponent<CreatureInfo>().getName() + " make plans for tonight.";
                            dialog.Add(new Dialog(con, text, "<color=cyan>" + con.getComponent<CreatureInfo>().encounterName + " responds \"" + text + "\"</color>" + makePlans));
                            constructConversation(dialog);
                            con.persist();
                            lib.getComponent<Liberal>().plannedDates.Add(con);
                            Dating dating = new Dating();
                            con.setComponent(dating);
                            mc.addToCurrentQueue(() => { encounterEntities[encounterEntities.IndexOf(con)] = null; dating.initDating(lib); }, "Remove Entity");
                        }
                        else
                        {
                            if (price > MasterController.lcs.Money)
                            {
                                string text = "You couldn't afford me, sweetheart.";
                                dialog.Add(new Dialog(con, text, "<color=red>" + con.getComponent<CreatureInfo>().encounterName + " responds \"" + text + "\"</color>"));
                                constructConversation(dialog);
                                con.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;
                            }
                            else
                            {
                                MasterController.lcs.changeFunds(-price);
                                string text = "Cash up front and I'm all yours, honey.";
                                string makePlans = "\n" + lib.getComponent<CreatureInfo>().getName() + " hands over $" + price + " and makes plans with " + con.getComponent<CreatureInfo>().getName() + " for tonight.";
                                dialog.Add(new Dialog(con, text, "<color=yellow>" + con.getComponent<CreatureInfo>().encounterName + " responds \"" + text + "\"</color>" + makePlans));
                                constructConversation(dialog);
                                con.persist();
                                lib.getComponent<Liberal>().plannedDates.Add(con);
                                Dating dating = new Dating();
                                con.setComponent(dating);
                                dating.initDating(lib);
                                mc.addToCurrentQueue(() => { encounterEntities[encounterEntities.IndexOf(con)] = null; }, "Remove Entity");
                            }
                        }
                    }
                }
                else if (succeeded)
                {
                    string text = "";

                    if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE)
                    {
                        switch (line)
                        {
                            case 0: text = "[I go to your church.]"; break;
                            case 1: text = "[Yes.]"; break;
                            case 2: text = "[Yes.  Yes, I do.]"; break;
                        }
                    }
                    else
                    {
                        switch (line)
                        {
                            case 0: text = "You're not so cute yourself. Wanna get a room?"; break;
                            case 1: text = "How sweet! You can call me tonight..."; break;
                            case 2: text = "You bet, baby."; break;
                            case 3: text = "He he, I'll let that one slide. Besides, I like country folk..."; break;
                            case 4: text = "That's sick. I can do sick tonight."; break;
                            case 5: text = "Oooo, let me see!"; break;
                            case 6: text = "Wow, looks like I'm going to have to reward creativity tonight!"; break;
                            case 7: text = "Winter's coming. You'd better bust more than one."; break;
                            case 8: text = "But you're not, so the pleasure's all mine."; break;
                            case 9: text = "Just wait until tonight, baby."; break;
                            case 10: text = "You're wrong."; break;
                            case 11: text = "I can drool on you if you like it that way."; break;
                            case 12: text = "Oooo, all aboard baby!"; break;
                            case 13: text = "Not as hot as we'll be tonight you slut."; break;
                            case 14: text = "Goober. You wanna hook up tonight?"; break;
                            case 15: text = "Oooo, we should get stoned too! He he."; break;
                            case 16: text = "You'll have to whip out your rocket to get some. Let's do it."; break;
                            case 17: text = "So would my underwear."; break;
                            case 18: text = "Yeah, and you're going to repay me tonight."; break;
                            case 19: text = "Then stop *thinking* about it and come over tonight."; break;
                            case 20: text = "As long as you put a condom between them, I'm all for it."; break;
                            case 21: text = "Sure, but you can't use your mouth."; break;
                            case 22: text = "I hope you don't have a foot fetish, but I'm game."; break;
                            case 23: text = "My sex could do even more."; break;
                            case 24: text = "Let me invite you to visit my island paradise. Tonight."; break;
                            case 25: text = "Oh, man... just don't tell anybody I'm seeing you."; break;
                            case 26: text = "I hope we're shooting blanks, soldier. I'm out of condoms."; break;
                            case 27: text = "You can lick all my decals off, baby."; break;
                            case 28: text = "Only if I'm not allowed to use my hands."; break;
                            case 29: text = "The one that says 'Open All Night'."; break;
                            case 30: text = "It looks like a letter bomb to me. Let me blow it up."; break;
                            case 31: text = "Hey, I could do better. But I'm feeling cheap tonight."; break;
                            case 32: text = "Yeah. I hope you remember the lube this time."; break;
                            case 33: text = "But if we use a hotel, you won't get shot by an angry spouse tonight."; break;
                            case 34: text = "I think you'll appreciate the way I move after tonight."; break;
                            case 35: text = "They make a yummy bedtime snack."; break;
                            case 36: text = "Oh... oh, God. I can't believe I'm going to date a Trekkie."; break;
                            case 37: text = "Oh, it isn't safe for you to drive like that. You'd better stay the night."; break;
                            case 38: text = "Come over tonight and I can show you what it's like."; break;
                            case 39: text = "I'll stop, drop and roll if you do it with me."; break;
                            case 40: text = "I'd rather feel something swell."; break;
                            case 41: text = "You can apologize later if it isn't any good."; break;
                            case 42: text = "Yeah, and we tonight can try different positions together."; break;
                            case 43: text = "Don't you like it dirty?"; break;
                            case 44: text = "It's you!! Somebody set up us the bomb. Move 'Zig'. For great justice."; break;
                            case 45: text = "Actually I'm a succubus from hell, and you're my next victim."; break;
                            case 46: text = "Can you wait a couple hours? I got 6 other people to fuck first."; break;
                        }
                    }

                    string makePlans = "\n" + lib.getComponent<CreatureInfo>().getName() + " and " + con.getComponent<CreatureInfo>().getName() + " make plans for tonight";
                    if (con.getComponent<CreatureInfo>().encounterName == "PRISONER")
                    {
                        makePlans += ", and " + con.getComponent<CreatureInfo>().getName() + " breaks for the exit!";
                        con.getComponent<CriminalRecord>().addCrime(Constants.CRIME_ESCAPE);
                    }
                    else
                        makePlans += ".";

                    dialog.Add(new Dialog(con, text, "<color=cyan>" + con.getComponent<CreatureInfo>().encounterName + " responds \"" + text + "\"</color>" + makePlans));
                    constructConversation(dialog);
                    con.persist();
                    lib.getComponent<Liberal>().plannedDates.Add(con);
                    Dating dating = new Dating();
                    con.setComponent(dating);
                    mc.addToCurrentQueue(() => { encounterEntities[encounterEntities.IndexOf(con)] = null; dating.initDating(lib); }, "Remove Entity");
                }
                else
                {
                    string text = "";
                    string action = "";
                    if ((con.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.ARCHCONSERVATIVE) != 0)
                    {
                        if (lib.getComponent<CreatureInfo>().genderLiberal != con.getComponent<CreatureInfo>().genderLiberal)
                            text = "I'm a happily married " + con.getComponent<CreatureInfo>().manWoman().ToLower() + ", sweetie.";
                        else
                            text = "This ain't Brokeback Mountain.";
                    }
                    else if (MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE)
                    {
                        switch (line)
                        {  // all 3 of these lines are from Darth Vader (the 3rd one from back when he's a little kid)
                            case 0: text = "I find your lack of faith disturbing."; break;
                            case 1: text = "No.  I am your father."; break;
                            case 2: text = "Don't count on it, slimeball!"; break;
                        }
                    }
                    else
                    {
                        switch (line)
                        {
                            case 0:
                                text = "You're such an asshole!";
                                action = " <pouts>"; break;
                            case 1:
                                text = "Sure, here ya go...";
                                action = " <writes wrong number>"; break;
                            case 2:
                                text = "I'm.. uh.. waiting for someone.";
                                action = " <turns away>"; break;
                            case 3:
                                text = "Go use a real bathroom, ya hick.";
                                action = " <points towards bathroom>"; break;
                            case 4:
                                text = "That was a very traumatic incident.";
                                action = " <cries>"; break;
                            case 5:
                                text = "You're big everywhere, fatass.";
                                action = " <laughs>"; break;
                            case 6:
                                text = "You're disgusting.";
                                action = " <turns away>"; break;
                            case 7:
                                text = "You fuck squirrels?";
                                action = " <looks dumbfounded>"; break;
                            case 8:
                                text = "So what you're saying is you masturbate a lot.";
                                action = " <wags finger>"; break;
                            case 9:
                                text = "You're a pig.";
                                action = " <turns away>"; break;
                            case 10:
                                text = "Nice try, but no.";
                                action = " <sticks out tongue>"; break;
                            case 11:
                                text = "Are you serious?";
                                action = " <turns away>"; break;
                            case 12:
                                text = "You look like a biter.";
                                action = " <flinches>"; break;
                            case 13:
                                text = "I'm way outta your league, scumbag.";
                                action = " <grabs pepper spray>"; break;
                            case 14:
                                text = "You still watch cartoons?";
                                action = " <laughs>"; break;
                            case 15:
                                text = "I hate puns!  You suck at comedy.";
                                action = " <frowns>"; break;
                            case 16:
                                text = "Yes, I'm an alien, you inferior Earth scum.";
                                action = " <reaches for ray gun>"; break;
                            case 17:
                                text = "Not after I do this.";
                                action = " <shits pants>"; break;
                            case 18:
                                text = "Yes, I can't stand liars.";
                                action = " <crosses flabby arms>"; break;
                            case 19:
                                text = "I don't remember doing that.";
                                action = " <looks confused>"; break;
                            case 20:
                                text = "We got a kindergarten dropout over here!";
                                action = " <points and laughs>"; break;
                            case 21:
                                text = "No, I don't want to infect anyone else with herpes.";
                                action = " <sighs>"; break;
                            case 22:
                                text = "Stop staring at my feet, you freak!";
                                action = " <kicks you>"; break;
                            case 23:
                                text = "You're such a loser.";
                                action = " <makes L sign on forehead>"; break;
                            case 24:
                                text = "I'm about to put a voodoo curse on yo ass...";
                                action = " <starts chanting>"; break;
                            case 25:
                                text = "I don't approve of your hi-carb diet.";
                                action = " <starts ranting about nutrition>"; break;
                            case 26:
                                text = "Go back home to play with your G.I. Joe dolls.";
                                action = " <scoffs>"; break;
                            case 27:
                                text = "No, and stop acting like a lost puppy.";
                                action = " <hisses like a cat>"; break;
                            case 28:
                                text = "Jesus...";
                                action = " <turns away>"; break;
                            case 29:
                                text = "I don't believe in astrology, you ignoramus.";
                                action = " <blinds you with science>"; break;
                            case 30:
                                text = "Yes, and it's practically microscopic.";
                                action = " <puts 2 fingers really close together>"; break;
                            case 31:
                                text = "My spouse will be here soon to straighten things out.";
                                action = " <looks for spouse>"; break;
                            case 32:
                                text = "You're not my type.  I like sane people.";
                                action = " <turns away>"; break;
                            case 33:
                                text = "Yes, here you go...";
                                action = " <writes fake directions>"; break;
                            case 34:
                                text = "Gotta go!  Bye!";
                                action = " <squirms away>"; break;
                            case 35:
                                text = "I don't do anal.";
                                action = " <puts hands over butt>"; break;
                            case 36:
                                text = "Hey, look, a UFO!";
                                action = " <ducks away>"; break;
                            case 37:
                                text = "Go home, you're drunk.";
                                action = " <gestures away>"; break;
                            case 38:
                                text = "At least then you'd be liquidated.";
                                action = " <stares intently>"; break;
                            case 39:
                                text = "Laaaame.";
                                action = " <looks bored>"; break;
                            case 40:
                                text = "Eew, no, gross.";
                                action = " <vomits on you>"; break;
                            case 41:
                                text = "Too late for apologies!";
                                action = " <slaps you>"; break;
                            case 42:
                                text = "What an idiot!";
                                action = " <laughs>"; break;
                            case 43:
                                text = "Nothing works, I can't help it.";
                                action = " <starts crying>"; break;
                            case 44:
                                text = "Hahahahaha!";
                                action = " <shakes head>"; break;
                            case 45:
                                text = "Yes, now go away.";
                                action = " <points to exit>"; break;
                            case 46:
                                text = "Touch me and you'll regret it.";
                                action = " <crosses arms>"; break;
                        }
                    }
                    dialog.Add(new Dialog(con, text, "<color=red>" + con.getComponent<CreatureInfo>().encounterName + " responds \"" + text + "\"</color>" + action));
                    constructConversation(dialog);
                    con.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;
                }
            }

            mc.addToCurrentQueue(() => { disguiseCheck(); }, "Disguise Check");
            mc.addToCurrentQueue(() => endRound(), "End Round");
            mc.doNextAction();
        }

        public void talkIssues(Entity lib, Entity con)
        {
            if ((con.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.NO_BLUFF) != 0) return;

            MasterController mc = MasterController.GetMC();
            startRound();
            List<Dialog> dialog = new List<Dialog>();
            if (con.getComponent<Body>().getSpecies().type == "DOG" && con.getComponent<CreatureInfo>().alignment != Alignment.LIBERAL)
            {
                heyMisterDog(con, dialog);
            }
            else if(con.getComponent<Body>().getSpecies().type == "MONSTER" && con.getComponent<CreatureInfo>().alignment != Alignment.LIBERAL)
            {
                heyMisterMonster(con, dialog);
            }
            else
            {
                string text = "Do you want to hear something disturbing?";
                dialog.Add(new Dialog(lib, text, lib.getComponent<CreatureInfo>().getName() + " says \"" + text + "\""));
                bool interested = (con.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.TALK_RECEPTIVE) != 0;
                if (!interested) interested = lib.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].check(Difficulty.AVERAGE);

                if (con.getComponent<Body>().getSpecies().type != "HUMAN" && 
                    !(con.getComponent<Body>().getSpecies().type == "DOG" && con.getComponent<CreatureInfo>().alignment == Alignment.LIBERAL))
                {
                    switch (con.getComponent<Body>().getSpecies().type)
                    {
                        case "DOG":
                            dialog.Add(new Dialog(con, "Bark!", con.getComponent<CreatureInfo>().encounterName + " barks."));
                            break;
                        case "TANK":
                            dialog.Add(new Dialog(con, "*Rumbles disinterestedly*", con.getComponent<CreatureInfo>().encounterName + " rumbles disinterestedly."));
                            break;
                        default:
                            dialog.Add(new Dialog(con, "???", con.getComponent<CreatureInfo>().encounterName + " doesn't understand"));
                            break;
                    }

                    constructConversation(dialog);
                    con.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;
                }
                else if (interested && con.getComponent<CreatureInfo>().encounterName != "Prisoner")
                {
                    text = "What?";
                    dialog.Add(new Dialog(con, text, con.getComponent<CreatureInfo>().encounterName + " responds <color=cyan>\"" + text + "\"</color>"));
                    string law = mc.pickRandom(MasterController.government.laws.Keys.ToList());
                    bool stupid = !lib.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].check(Difficulty.EASY);
                    bool tooLiberal = MasterController.government.laws[law].alignment == Alignment.ELITE_LIBERAL;
                    if (stupid)
                        text = GameData.getData().lawList[law].issueText["stupid"];
                    else if (tooLiberal)
                        text = GameData.getData().lawList[law].issueText["liberal"];
                    else
                        text = GameData.getData().lawList[law].issueText["normal"];
                    dialog.Add(new Dialog(lib, text, lib.getComponent<CreatureInfo>().getName() + " says \"" + text + "\""));
                    Difficulty diff = Difficulty.VERYEASY;
                    if ((con.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.TALK_RECEPTIVE) == 0)
                        diff += 7;
                    if (con.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
                        diff += 7;
                    if (stupid)
                        diff += 5;
                    if (tooLiberal)
                        diff += 5;
                    if (lib.getComponent<Inventory>().getArmor().def == "ARMOR_NONE")
                        diff += 5;

                    if (lib.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].check(diff) && !con.hasComponent<Liberal>())
                    {
                        string boilerPlate = "\nAfter more discussion, " + con.getComponent<CreatureInfo>().getName() + " agrees to come by later tonight.";
                        if (con.def == "MUTANT" && con.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].getModifiedValue() < 3)
                        {
                            text = "AAAAHHHH...";
                            dialog.Add(new Dialog(con, text, con.getComponent<CreatureInfo>().encounterName + " responds <color=cyan>\"" + text + "\"</color>" + boilerPlate));
                        }
                        else
                        {
                            switch (mc.LCSRandom(10))
                            {
                                case 0:
                                    text = "Dear me! Is there anything we can do?";
                                    dialog.Add(new Dialog(con, text, con.getComponent<CreatureInfo>().encounterName + " responds <color=cyan>\"" + text + "\"</color>" + boilerPlate));
                                    break;
                                case 1:
                                    text = "That *is* disturbing! What can I do?";
                                    dialog.Add(new Dialog(con, text, con.getComponent<CreatureInfo>().encounterName + " responds <color=cyan>\"" + text + "\"</color>" + boilerPlate));
                                    break;
                                case 2:
                                    text = "Gosh! Is there anything I can do?";
                                    dialog.Add(new Dialog(con, text, con.getComponent<CreatureInfo>().encounterName + " responds <color=cyan>\"" + text + "\"</color>" + boilerPlate));
                                    break;
                                case 3:
                                    text = "That's frightening! What can we do?";
                                    dialog.Add(new Dialog(con, text, con.getComponent<CreatureInfo>().encounterName + " responds <color=cyan>\"" + text + "\"</color>" + boilerPlate));
                                    break;
                                case 4:
                                    text = "Oh my Science! We've got to do something!";
                                    dialog.Add(new Dialog(con, text, con.getComponent<CreatureInfo>().encounterName + " responds <color=cyan>\"" + text + "\"</color>" + boilerPlate));
                                    break;
                                case 5:
                                    text = "Dude... that's like... totally bumming me.";
                                    dialog.Add(new Dialog(con, text, con.getComponent<CreatureInfo>().encounterName + " responds <color=cyan>\"" + text + "\"</color>" + boilerPlate));
                                    break;
                                case 6:
                                    text = "Gadzooks! Something must be done!";
                                    dialog.Add(new Dialog(con, text, con.getComponent<CreatureInfo>().encounterName + " responds <color=cyan>\"" + text + "\"</color>" + boilerPlate));
                                    break;
                                case 7:
                                    text = "You got anything to smoke on you? *cough*";
                                    dialog.Add(new Dialog(con, text, con.getComponent<CreatureInfo>().encounterName + " responds <color=cyan>\"" + text + "\"</color>" + boilerPlate));
                                    break;
                                case 8:
                                    text = "Lawks, I don't think we can allow that.";
                                    dialog.Add(new Dialog(con, text, con.getComponent<CreatureInfo>().encounterName + " responds <color=cyan>\"" + text + "\"</color>" + boilerPlate));
                                    break;
                                case 9:
                                    text = "Oh, really?";
                                    dialog.Add(new Dialog(con, text, con.getComponent<CreatureInfo>().encounterName + " responds <color=cyan>\"" + text + "\"</color>"));
                                    text = "Yeah, really!";
                                    dialog.Add(new Dialog(lib, text, lib.getComponent<CreatureInfo>().getName() + " says <color=lime>\"" + text + "\"</color>" + boilerPlate));
                                    break;
                            }
                        }
                        con.persist();
                        lib.getComponent<Liberal>().plannedMeetings.Add(con);
                        Recruit recruitComponent = new Recruit();
                        con.setComponent(recruitComponent);
                        con.getComponent<Recruit>().initRecruitment(lib);

                        constructConversation(dialog);
                        mc.addToCurrentQueue(() => { encounterEntities[encounterEntities.IndexOf(con)] = null; }, "Remove Entity");
                        con.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;
                    }
                    else
                    {
                        if (con.def == "MUTANT" && con.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].getModifiedValue() < 3)
                        {
                            text = "Ugh. Pfft...";
                        }
                        else if (con.hasComponent<Liberal>())
                        {
                            text = "Dude, you trying to blow my cover?";
                        }
                        else
                        {
                            if (con.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE && stupid)
                            {
                                if (con.def == "DEATHSQUAD")
                                    text = "If you don't shut up, I'm going to shoot you.";
                                else if ((con.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.POLICE) != 0)
                                    text = "Do you want me to arrest you?";
                                else
                                {
                                    switch (mc.LCSRandom(10))
                                    {
                                        case 0:
                                            text = "Get away from me, you hippie.";
                                            break;
                                        case 1:
                                            text = "My heart aches for humanity.";
                                            break;
                                        case 2:
                                            text = "I'm sorry, but I think I'm done talking to you.";
                                            break;
                                        case 3:
                                            text = "Do you need some help finding the exit?";
                                            break;
                                        case 4:
                                            text = "People like you are the reason I'm on medication.";
                                            break;
                                        case 5:
                                            text = "Everyone is entitled to be stupid, but you abuse the privilege.";
                                            break;
                                        case 6:
                                            text = "I don't know what you're on, but I hope it's illegal.";
                                            break;
                                        case 7:
                                            text = "Don't you have a parole meeting to get to?";
                                            break;
                                        case 8:
                                            text = "Wow. Why am I talking to you again?";
                                            break;
                                        case 9:
                                            text = "Were you dropped as a child?";
                                            break;
                                    }
                                }
                            }
                            else if (con.getComponent<CreatureInfo>().alignment != Alignment.LIBERAL &&
                                    con.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].check(Difficulty.AVERAGE))
                            {
                                text = GameData.getData().lawList[law].issueText["rejection"];
                            }
                            else
                            {
                                text = "Whatever.";
                            }
                        }

                        dialog.Add(new Dialog(con, text, con.getComponent<CreatureInfo>().encounterName + " responds <color=yellow>\"" + text + "\"</color>\n<turns away>"));
                        constructConversation(dialog);
                        con.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;
                    }
                }
                else
                {
                    if (con.getComponent<CreatureInfo>().encounterName == "Prisoner")
                    {
                        if (con.getComponent<CreatureInfo>().alignment == Alignment.LIBERAL)
                            text = "Now's not the time!";
                        else
                            text = "Leave me alone.";
                    }
                    else
                        text = "No.";
                    dialog.Add(new Dialog(con, text, con.getComponent<CreatureInfo>().encounterName + " responds <color=yellow>\"" + text + "\"</color>\n<turns away>"));
                    constructConversation(dialog);
                }
            }

            mc.addToCurrentQueue(() => { disguiseCheck(); }, "Disguise Check");
            mc.addToCurrentQueue(()=>endRound(), "End Round");
            mc.doNextAction();
        }

        public void talkBluff()
        {
            MasterController mc = MasterController.GetMC();
            Entity speaker = squad[mc.LCSRandom(squad.Count)];

            startRound();

            string bluffstring = "";
            string speakerdialog = "";

            foreach(Entity e in encounterEntities)
            {
                if (e == null) continue;
                if(e.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
                {
                    e.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;
                }
            }

            if (location.hasComponent<SafeHouse>() && location.getComponent<SafeHouse>().underSiege)
            {
                bluffstring = speaker.getComponent<CreatureInfo>().getName() + " ";

                switch (location.getComponent<SafeHouse>().siegeType)
                {
                    case LocationDef.EnemyType.POLICE:
                        bluffstring += "pretends to be part of a police raid."; break;
                    case LocationDef.EnemyType.AGENT:
                        bluffstring += "pretends to be a Secret Agent."; break;
                    case LocationDef.EnemyType.REDNECK:
                        bluffstring += "pretends to be Mountain like Patrick Swayze in Next of Kin."; break;
                    case LocationDef.EnemyType.MERC:
                        bluffstring += "pretends to be a mercenary."; break;
                    case LocationDef.EnemyType.FIREMEN:
                        bluffstring += "lights a match and throws it on the ground.";
                        //TODO: Start a fire!
                        break;
                }
            }
            else
            {
                if ((speaker.getComponent<Inventory>().getArmor().getComponent<Armor>().getFlags() & ItemDef.ArmorFlags.POLICE) != 0)
                {
                    speakerdialog = "The situation is under control.";
                    bluffstring = speaker.getComponent<CreatureInfo>().getName() + " says <color=lime>\"" + speakerdialog + "\"</color>";
                }
                else if ((speaker.getComponent<Inventory>().getArmor().getComponent<Armor>().getFlags() & ItemDef.ArmorFlags.FIRE_PROTECTION) != 0)
                {
                    if (location.getComponent<TroubleSpot>().fireAlarmTriggered)
                    {
                        speakerdialog = "Fire! Evacuate immediately!";
                    }
                    else
                    {
                        speakerdialog = "Everything's in check.";
                    }
                    bluffstring = speaker.getComponent<CreatureInfo>().getName() + " says <color=lime>\"" + speakerdialog + "\"</color>";
                }
                else if (speaker.getComponent<Inventory>().getArmor().def == "ARMOR_LABCOAT")
                {
                    speakerdialog = "Make way, I'm a doctor!";
                    bluffstring = speaker.getComponent<CreatureInfo>().getName() + " says <color=lime>\"" + speakerdialog + "\"</color>";
                }
                else if ((speaker.getComponent<Inventory>().getArmor().getComponent<Armor>().getFlags() & ItemDef.ArmorFlags.DEATHSQUAD) != 0)
                {
                    speakerdialog = "Non-targets please leave the site.";
                    bluffstring = speaker.getComponent<CreatureInfo>().getName() + " says <color=lime>\"" + speakerdialog + "\"</color>";
                }
                else if (speaker.getComponent<Inventory>().getArmor().def == "ARMOR_MITHRIL")
                {
                    bluffstring = speaker.getComponent<CreatureInfo>().getName() + " engraves <color=cyan>Elbereth</color> on the floor.";
                }
                else
                {
                    bluffstring = speaker.getComponent<CreatureInfo>().getName() + " talks like a Conservative and pretends to belong here.";
                }
            }

            sceneRoot.Add(() =>
            {
                mc.addCombatMessage(bluffstring);
                if (speakerdialog != "")
                    mc.uiController.doSpeak(new UI.UIEvents.Speak(speaker, speakerdialog));
            }, "dialogLine");

            //Disguise check
            int weapon = 2;

            foreach (Entity e in squad)
            {
                if (e.getComponent<Inventory>().checkWeaponDisguise() < weapon) weapon = e.getComponent<Inventory>().checkWeaponDisguise();
            }

            List<Entity> noticer = new List<Entity>();
            foreach (Entity e in encounterEntities)
            {
                if (e == null) continue;
                if (e.getComponent<CreatureInfo>().encounterName == "Prisoner") continue;

                if (e.getComponent<Body>().Alive && (e.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE))
                    noticer.Add(e);
            }

            bool noticed = false;
            Entity blewit = null;
            Entity n = null;

            if (noticer.Count > 0)
            {
                do
                {
                    n = mc.pickRandom(noticer);
                    noticer.Remove(n);

                    Difficulty disguise_difficulty = GameData.getData().creatureDefList[n.def].disguise_difficulty;

                    if (suspicionTimer == 0)
                    {
                        disguise_difficulty += 6;
                    }
                    else if (suspicionTimer >= 1)
                    {
                        disguise_difficulty += 3;
                    }

                    foreach (Entity e in squad)
                    {
                        if (e.getComponent<Inventory>().checkWeaponDisguise() == 0)
                        {
                            noticed = true;
                            break;
                        }
                        else
                        {
                            int result = e.getComponent<CreatureBase>().Skills[Constants.SKILL_DISGUISE].roll();

                            //Invalid disguises means roll auto-fails
                            if (e.getComponent<Inventory>().getDisguiseLevel() <= 0) result = 0;
                            //Partial disguises are less effective
                            else if (e.getComponent<Inventory>().getDisguiseLevel() == 1) result /= 2;
                            //Having a hostage makes blending in very hard
                            if (e.getComponent<Liberal>().hauledUnit != null) result /= 4;

                            if (result < (int)disguise_difficulty)
                            {
                                blewit = e;

                                noticed = true;
                                break;
                            }
                        }
                    }

                    if (noticed) break;
                } while (noticer.Count > 0);

                if (blewit == null)
                {
                    foreach (Entity e in squad)
                    {
                        e.getComponent<CreatureBase>().Skills[Constants.SKILL_DISGUISE].addExperience(10);
                    }
                }

                if (noticed)
                {
                    string noticeString = "";

                    if(n.def == "HICK")
                    {
                        noticeString = "<color=red>But " + n.getComponent<CreatureInfo>().encounterName + " weren't born yesterday.</color>";
                    }
                    else
                    {
                        noticeString = "<color=red>" + n.getComponent<CreatureInfo>().encounterName + " isn't fooled by that " + mc.swearFilter("crap", "act") + "</color>"; 
                    }

                    sceneRoot.Add(() =>
                    {
                        mc.addCombatMessage(noticeString);
                    }, "responseFail");

                    Fight.theyFight(squad, encounterEntities, sceneRoot);
                }
                else
                {
                    sceneRoot.Add(() =>
                    {
                        mc.addCombatMessage("<color=lime>The Enemy is fooled and departs.</color>");
                        for (int i = 0; i < encounterEntities.Count; i++)
                        {
                            encounterEntities[i] = null;
                        }

                        if (encounterEntities.Count(e => { return e != null; }) == 0)
                            leaveEncounter();
                    }, "responseSuccess");
                }
            }

            sceneRoot.Add(() => endRound(), "End Round");
            MasterController.GetMC().doNextAction();
        }

        public void talkIntimidate()
        {
            MasterController mc = MasterController.GetMC();
            Entity speaker = squad[mc.LCSRandom(squad.Count)];

            startRound();

            foreach (Entity e in encounterEntities)
            {
                if (e == null) continue;
                if (e.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
                {
                    e.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;
                }
            }

            string shoutstring = "";

            switch (mc.LCSRandom(4))
            {
                case 0:
                    shoutstring = MasterController.lcs.slogan;
                    MasterController.news.currentStory.claimed = true;
                    break;
                case 1:
                    shoutstring = "Die you Conservative dogs!";
                    break;
                case 2:
                    shoutstring = "We're the Liberal Crime Squad!";
                    MasterController.news.currentStory.claimed = true;
                    break;
                case 3:
                    shoutstring = "Praying won't help you now!";
                    break;
            }

            sceneRoot.Add(() =>
            {
                mc.addCombatMessage(speaker.getComponent<CreatureInfo>().getName() + " shouts \"" + shoutstring + "\"");
                mc.uiController.doSpeak(new UI.UIEvents.Speak(speaker, shoutstring));
            }, "dialogLine");

            for(int i=0;i<encounterEntities.Count;i++)
            {
                Entity e = encounterEntities[i];

                if (e == null) continue;

                if(e.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
                {
                    int attack = speaker.getComponent<CreatureBase>().Juice / 50 + MasterController.generalPublic.PublicOpinion[Constants.VIEW_LIBERALCRIMESQUAD] / 10;
                    int defense = e.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].roll();

                    if(attack > defense)
                    {
                        if ((e.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.BRAVE) != 0 &&
                            mc.LCSRandom(3) != 0)
                            continue;

                        string enemyString = e.getComponent<CreatureInfo>().getName();

                        switch (mc.LCSRandom(6))
                        {
                            case 0: enemyString += " chickens out!"; break;
                            case 1: enemyString += " backs off!"; break;
                            case 2: enemyString += " doesn't want to die!"; break;
                            case 3: enemyString += " is out of there!"; break;
                            case 4: enemyString += " has a family!"; break;
                            case 5: enemyString += " is too young to die!"; break;
                        }

                        speaker.getComponent<CreatureBase>().juiceMe(2, 200);

                        int j = i;

                        sceneRoot.Add(() =>
                        {
                            mc.addCombatMessage(enemyString);
                            encounterEntities[j] = null;
                        }, "enemyflees");
                    }
                }
            }

            if (encounterHasEnemies())
            {
                Fight.theyFight(squad, encounterEntities, sceneRoot);
            }

            sceneRoot.Add(() => endRound(), "End Round");
            MasterController.GetMC().doNextAction();
        }

        public void talkThreatenHostage()
        {
            MasterController mc = MasterController.GetMC();
            List<Entity> hostagetakers = new List<Entity>();

            foreach(Entity e in squad)
            {
                if (e == null) continue;
                if (e.getComponent<Liberal>().hauledUnit != null &&
                    !e.getComponent<Liberal>().hauledUnit.hasComponent<Liberal>())
                    hostagetakers.Add(e);
            }

            if (hostagetakers.Count == 0) return;

            Entity speaker = hostagetakers[mc.LCSRandom(hostagetakers.Count)];

            startRound();

            foreach (Entity e in encounterEntities)
            {
                if (e == null) continue;
                if (e.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
                {
                    e.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;
                }
            }

            string threat = "";

            switch (mc.LCSRandom(6))
            {
                case 0: threat = "Back off or the hostage dies!"; break;
                case 1: threat = "Don't push the LCS!"; break;
                case 2: threat = "Hostage says you better leave!"; break;
                case 3: threat = "I'll do it! I'll kill this one!"; break;
                case 4: threat = "You gonna tell the family you pushed me?!"; break;
                case 5: threat = "Don't " + mc.swearFilter("fuck", "play") + " with me!"; break;
            }

            sceneRoot.Add(() =>
            {
                mc.addCombatMessage(speaker.getComponent<CreatureInfo>().getName() + " says \"" + threat + "\"");
                mc.uiController.doSpeak(new UI.UIEvents.Speak(speaker, threat));
            }, "dialogLine");

            siteCrime += 5;
            foreach(Entity e in squad)
            {
                if (e == null) continue;
                e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_KIDNAPPING);
            }
            speaker.getComponent<CreatureBase>().juiceMe(-2, -10);            

            if((speaker.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.TAKE_HOSTAGE) != 0)
            {
                bool noretreat = false;
                Entity responder = null;

                //If the encounter has a police Negotiator, they take charge
                foreach(Entity e in encounterEntities)
                {
                    if (e == null) continue;
                    if (e.def == "NEGOTIATOR")
                    {
                        string response = "";
                        switch (mc.LCSRandom(5))
                        {
                            case 0:
                                if (hostagetakers.Count > 1)
                                    response = "Release your hostages, and nobody gets hurt.";
                                else response = "Let the hostage go, and nobody gets hurt.";
                                break;
                            case 1: response = "You got about five seconds to back down."; break;
                            case 2: response = "You want to do this the hard way?"; break;
                            case 3: response = "Big mistake."; break;
                            case 4: response = "Release them, and I'll let you go."; break;
                        }

                        responder = e;
                        sceneRoot.Add(() =>
                        {
                            mc.addCombatMessage(responder.getComponent<CreatureInfo>().encounterName + " says \"" + response + "\"");
                            mc.uiController.doSpeak(new UI.UIEvents.Speak(responder, response));
                        }, "dialogLine");

                        noretreat = true;
                        break;
                    }
                }

                if (responder == null)
                {
                    foreach (Entity e in encounterEntities)
                    {
                        if (e == null) continue;

                        if (e.getComponent<Body>().Blood <= 70 || !e.getComponent<Body>().Alive) continue;

                        if ((e.getComponent<CreatureBase>().getFlags() & (CreatureDef.CreatureFlag.POLICE | CreatureDef.CreatureFlag.HARDCORE)) != 0 &&
                            mc.LCSRandom(5) != 0)
                        {
                            string response = "";

                            if ((e.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.HARDCORE) != 0)
                            {
                                switch (mc.LCSRandom(5))
                                {
                                    case 0: response = "Hahahaha..."; break;
                                    case 1: response = "You think you can scare me?"; break;
                                    case 2: response = "You're not getting out of here alive."; break;
                                    case 3: response = "What's wrong? Need your diaper changed?"; break;
                                    case 4: response = "Three... two..."; break;
                                }
                            }
                            else
                            {
                                switch (mc.LCSRandom(5))
                                {
                                    case 0:
                                        if (hostagetakers.Count > 1)
                                            response = "Release your hostages, and nobody gets hurt.";
                                        else response = "Let the hostage go, and nobody gets hurt.";
                                        break;
                                    case 1: response = "You got about five seconds to back down."; break;
                                    case 2: response = "You want to do this the hard way?"; break;
                                    case 3: response = "Big mistake."; break;
                                    case 4: response = "Release them, and I'll let you go."; break;
                                }
                            }

                            responder = e;
                            sceneRoot.Add(() =>
                            {
                                mc.addCombatMessage(responder.getComponent<CreatureInfo>().encounterName + " says \"" + response + "\"");
                                mc.uiController.doSpeak(new UI.UIEvents.Speak(responder, response));
                            }, "dialogLine");

                            noretreat = true;
                            break;
                        }
                    }
                }

                if (!noretreat)
                {
                    sceneRoot.Add(() =>
                    {
                        mc.addCombatMessage("The ploy works! The Conservatives back off.");

                        for(int i = 0; i < encounterEntities.Count; i++)
                        {
                            if (encounterEntities[i] == null) continue;

                            if (encounterEntities[i].getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE ||
                                encounterEntities[i].def == "NEGOTIATOR")
                                encounterEntities[i] = null;
                        }
                    }, "Retreat");

                    sceneRoot.Add(() => endRound(), "End Round");
                }
                else
                {
                    List<PopupOption> options = new List<PopupOption>();
                    options.Add(new PopupOption(hostagetakers.Count > 1 ? "Execute a Hostage" : "Execute the Hostage", () =>
                    {
                        if(speaker.getComponent<Inventory>().getWeapon().getComponent<Weapon>().clip != null &&
                            speaker.getComponent<Inventory>().getWeapon().getComponent<Weapon>().clip.getComponent<Clip>().ammo > 0)
                        {
                            mc.addCombatMessage("<color=red>BLAM!</color>");
                            speaker.getComponent<Inventory>().getWeapon().getComponent<Weapon>().clip.getComponent<Clip>().ammo--;
                        }
                        else
                        {
                            mc.addCombatMessage("<color=red>CRUNCH!</color>");
                        }

                        sceneRoot.Add(() =>
                        {
                            mc.addCombatMessage(speaker.getComponent<CreatureInfo>().getName() + " drops " + speaker.getComponent<Liberal>().hauledUnit.getComponent<CreatureInfo>().getName() + "'s body.");
                            speaker.getComponent<Liberal>().hauledUnit.getComponent<CreatureBase>().doDie(new Events.Die("was executed in cold blood by " + speaker.getComponent<CreatureInfo>().getName()));
                            speaker.getComponent<Liberal>().hauledUnit = null;

                            speaker.getComponent<CreatureBase>().juiceMe(-5, -50);
                            speaker.getComponent<CriminalRecord>().addCrime(Constants.CRIME_MURDER);
                            alienateCheck(true);
                        }, "drop hostage");

                        if (hostagetakers.Count > 1 && mc.LCSRandom(2) == 0)
                        {
                            string response = mc.swearFilter("Fuck!", "No!") + " ";

                            switch (mc.LCSRandom(5))
                            {
                                case 0: response += "Okay, okay, you win!"; break;
                                case 1: response += "Don't shoot!"; break;
                                case 2: response += "Do you even care?!"; break;
                                case 3: response += "Heartless!"; break;
                                case 4: response += "It's not worth it!"; break;
                            }

                            sceneRoot.Add(() =>
                            {
                                mc.addCombatMessage(responder.getComponent<CreatureInfo>().encounterName + " says \"" + response + "\"");
                                mc.uiController.doSpeak(new UI.UIEvents.Speak(responder, response));
                            }, "dialogLine");

                            sceneRoot.Add(() =>
                            {
                                for (int i = 0; i < encounterEntities.Count; i++)
                                {
                                    if (encounterEntities[i] == null) continue;

                                    if (encounterEntities[i].getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE ||
                                        encounterEntities[i].def == "NEGOTIATOR")
                                        encounterEntities[i] = null;
                                }
                            }, "retreat");
                        }

                        sceneRoot.Add(disguiseCheck, "disguiseCheck");
                        sceneRoot.Add(() => endRound(), "End Round");
                    }));
                    options.Add(new PopupOption(hostagetakers.Count > 1 ? "Trade the Hostages" : "Trade a Hostage", () =>
                    {
                        string response = "";

                        switch (mc.LCSRandom(5))
                        {
                            case 0:
                                if (hostagetakers.Count > 1) response = "Back off and we'll let the hostages go.";
                                else response = "Back off and the hostage goes free.";
                                break;
                            case 1: response = "Freedom for freedom, understand?"; break;
                            case 2: response = "Let me go in peace, okay?"; break;
                            case 3: response = "Let's make a trade, then."; break;
                            case 4: response = "I just want out of here, yeah?"; break;
                        }

                        mc.addCombatMessage(speaker.getComponent<CreatureInfo>().getName() + " says \"" + response + "\"");
                        mc.uiController.doSpeak(new UI.UIEvents.Speak(speaker, response));

                        if((responder.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.HARDCORE) != 0 &&
                            mc.LCSRandom(2) == 0)
                        {
                            string response2 = "";

                            switch (mc.LCSRandom(5))
                            {
                                case 0: response2 = "Do I look like a loving person?"; break;
                                case 1: response2 = "You don't take a hint, do you?"; break;
                                case 2: response2 = "I'm doing the world a favor."; break;
                                case 3: response2 = "That's so pathetic..."; break;
                                case 4: response2 = "No deal."; break;
                            }

                            sceneRoot.Add(() =>
                            {
                                mc.addCombatMessage(responder.getComponent<CreatureInfo>().encounterName + " says \"" + response2 + "\"");
                                mc.uiController.doSpeak(new UI.UIEvents.Speak(responder, response2));
                            }, "dialogLine");
                        }
                        else
                        {
                            string response2 = "";

                            switch (mc.LCSRandom(4))
                            {
                                case 0: response2 = "Right. Let's do it."; break;
                                case 1: response2 = "No further conditions."; break;
                                case 2: response2 = "Let them go, and we're done."; break;
                                case 3: response2 = "No tricks, okay?"; break;
                            }

                            foreach(Entity e in squad)
                            {
                                if (e == null) continue;
                                e.getComponent<CreatureBase>().juiceMe(15, 200);
                            }

                            sceneRoot.Add(() =>
                            {
                                mc.addCombatMessage(responder.getComponent<CreatureInfo>().encounterName + " says \"" + response2 + "\"");
                                mc.uiController.doSpeak(new UI.UIEvents.Speak(responder, response2));
                            }, "dialogLine");

                            sceneRoot.Add(() =>
                            {
                                if (hostagetakers.Count > 1)
                                    mc.addCombatMessage("The squad releases all hostages in the trade.");
                                else
                                    mc.addCombatMessage("The squad releases the hostage in the trade.");

                                foreach(Entity e in hostagetakers)
                                {
                                    e.getComponent<Liberal>().hauledUnit = null;
                                }

                                for (int i = 0; i < encounterEntities.Count; i++)
                                {
                                    if (encounterEntities[i] == null) continue;

                                    if (encounterEntities[i].getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE ||
                                        encounterEntities[i].def == "NEGOTIATOR")
                                        encounterEntities[i] = null;
                                }

                                mc.doNextAction();
                            }, "retreat");
                        }

                        sceneRoot.Add(disguiseCheck, "disguiseCheck");
                        sceneRoot.Add(() => endRound(), "End Round");
                    }));
                    options.Add(new PopupOption("No Reply", () =>
                    {
                        disguiseCheck();
                        sceneRoot.Add(() => endRound(), "End Round");
                    }));

                    sceneRoot.Add(() =>
                    {
                        mc.uiController.showOptionPopup("How should " + speaker.getComponent<CreatureInfo>().getName() + " respond?", options);
                    }, "hostagePopup");
                }
            }
            else
            {
                sceneRoot.Add(() =>
                {
                    mc.addCombatMessage("The Conservatives aren't interested in your pathetic threats");
                }, "nothreat");
                sceneRoot.Add(disguiseCheck, "disguiseCheck");
                sceneRoot.Add(() => endRound(), "End Round");
            }

            MasterController.GetMC().doNextAction();
        }

        public void talkRentRoom()
        {
            MasterController mc = MasterController.GetMC();
            startRound();

            Entity speaker = squad[mc.LCSRandom(squad.Count)];
            Entity landlord = null;
            foreach (Entity e in encounterEntities)
            {
                if (e.def == "LANDLORD")
                {
                    landlord = e;
                    break;
                }
            }
            string talkString = "";

            if (!location.getComponent<SafeHouse>().owned)
                talkString = "I'd like to rent a room.";
            else
                talkString = "I'd like to cancel my room.";

            sceneRoot.Add(() =>
            {
                mc.addCombatMessage(speaker.getComponent<CreatureInfo>().getName() + " says \"" + talkString + "\"");
                mc.uiController.doSpeak(new UI.UIEvents.Speak(speaker, talkString));
            }, "dialogLine");

            if (speaker.getComponent<Inventory>().getArmor().def == "NONE")
            {
                string responseString = "Put some clothes on before I call the cops.";

                mc.addCombatMessage("The landlord says \"" + responseString + "\"");
                mc.uiController.doSpeak(new UI.UIEvents.Speak(landlord, responseString));

                sceneRoot.Add(() => endRound(), "End Round");
            }
            else
            {
                if (!location.getComponent<SafeHouse>().owned)
                {
                    int rentPrice = location.getComponent<SafeHouse>().getRentPrice();

                    sceneRoot.Add(() =>
                    {
                        List<PopupOption> options = new List<PopupOption>();
                        options.Add(new PopupOption("Accept", () =>
                        {
                            talkString = "I'll take it!";

                            mc.addCombatMessage(speaker.getComponent<CreatureInfo>().getName() + " says \"" + talkString + "\"");
                            mc.uiController.doSpeak(new UI.UIEvents.Speak(speaker, talkString));

                            if (MasterController.lcs.Money >= rentPrice)
                            {
                                MasterController.lcs.changeFunds(-rentPrice);
                                location.getComponent<SafeHouse>().owned = true;
                                forceFinish = true;
                                string responseString = "Rent is due by the first of every month.";
                                squad.homeBase = location;
                                foreach (Entity e in squad)
                                {
                                    e.getComponent<Liberal>().homeBase = location;
                                }

                                sceneRoot.Add(() =>
                                {
                                    mc.addCombatMessage("The landlord says \"" + responseString + "\"");
                                    mc.uiController.doSpeak(new UI.UIEvents.Speak(landlord, responseString));
                                }, "dialogLine");
                            }
                            else
                            {
                                string responseString = "Real funny, deadbeat. Come back when you've got the cash.";

                                sceneRoot.Add(() =>
                                {
                                    mc.addCombatMessage("The landlord says \"" + responseString + "\"");
                                    mc.uiController.doSpeak(new UI.UIEvents.Speak(landlord, responseString));
                                }, "dialogLine");
                            }

                            sceneRoot.Add(() => { mc.addCombatMessage("The landlord turns away"); }, "landlordEnd");
                            sceneRoot.Add(() => endRound(), "End Round");
                        }));
                        options.Add(new PopupOption("Decline", () =>
                        {
                            talkString = "Whoa, I was looking for something cheaper.";

                            mc.addCombatMessage(speaker.getComponent<CreatureInfo>().getName() + " says \"" + talkString + "\"");
                            mc.uiController.doSpeak(new UI.UIEvents.Speak(speaker, talkString));

                            string responseString = "Not my problem...";

                            sceneRoot.Add(() =>
                            {
                                mc.addCombatMessage("The landlord says \"" + responseString + "\"");
                                mc.uiController.doSpeak(new UI.UIEvents.Speak(landlord, responseString));
                            }, "dialogLine");

                            sceneRoot.Add(() => endRound(), "End Round");
                        }));
                        options.Add(new PopupOption("Threaten", () =>
                        {
                            Entity armedLiberal = null;
                            if (speaker.getComponent<Inventory>().isWeaponThreatening())
                                armedLiberal = speaker;
                            else
                            {
                                foreach(Entity e in squad)
                                {
                                    if (e.getComponent<Inventory>().isWeaponThreatening())
                                    {
                                        armedLiberal = e;
                                        break;
                                    }
                                }
                            }

                            talkString = "What's the price for the Liberal Crime Squad?";
                            int roll;

                            if (armedLiberal != null)
                            {
                                mc.addCombatMessage(armedLiberal.getComponent<CreatureInfo>().getName() + " brandishes the " + armedLiberal.getComponent<Inventory>().getWeapon().getComponent<ItemBase>().getName() + " and says \"" + talkString + "\"");
                                mc.uiController.doSpeak(new UI.UIEvents.Speak(armedLiberal, talkString));
                                roll = armedLiberal.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].roll();
                            }
                            else
                            {
                                mc.addCombatMessage(speaker.getComponent<CreatureInfo>().getName() + " says \"" + talkString + "\"");
                                mc.uiController.doSpeak(new UI.UIEvents.Speak(speaker, talkString));
                                roll = speaker.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].roll();
                            }

                            Difficulty dif = Difficulty.FORMIDABLE;

                            if (!MasterController.news.newsCherryBusted)
                                dif += 6;
                            if (armedLiberal == null)
                                dif += 6;

                            if(roll < (int)dif - 1)
                            {
                                string responseString = "I think you'd better leave...";
                                landlord.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;

                                sceneRoot.Add(() =>
                                {
                                    mc.addCombatMessage("The landlord responds \"" + responseString + "\"");
                                    mc.uiController.doSpeak(new UI.UIEvents.Speak(landlord, responseString));
                                }, "dialogLine");
                            }
                            else
                            {
                                string responseString = "Jesus, it's yours...";
                                location.getComponent<SafeHouse>().owned = true;
                                location.getComponent<SafeHouse>().freeRent = true;
                                forceFinish = true;
                                squad.homeBase = location;
                                foreach (Entity e in squad)
                                {
                                    e.getComponent<Liberal>().homeBase = location;
                                }

                                //Landlord will call the cops if you weren't QUITE convincing enough.
                                if (roll < (int)dif)
                                {
                                    location.getComponent<SafeHouse>().timeUntilLocated = 2;
                                    location.getComponent<SafeHouse>().forceEvict = true;
                                    foreach(Entity e in squad)
                                    {
                                        e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_EXTORTION);
                                    }
                                }

                                sceneRoot.Add(() =>
                                {
                                    mc.addCombatMessage("The landlord responds \"" + responseString + "\"");
                                    mc.uiController.doSpeak(new UI.UIEvents.Speak(landlord, responseString));
                                }, "dialogLine");
                            }

                            sceneRoot.Add(() => endRound(), "End Round");
                        }));

                        string popupText = "It'll be $" + rentPrice + " a month. I'll need $" + rentPrice + " now as a security deposit.";

                        mc.uiController.showOptionPopup(popupText, options);
                        mc.addCombatMessage("The landlord says \"" + popupText + "\"");
                    }, "selectionPopup");
                }
                else
                {
                    string responseString = "Alright. Please clear out your room.";
                    forceFinish = true;

                    sceneRoot.Add(() =>
                    {
                        mc.addCombatMessage("The landlord says \"" + responseString + "\"");
                        mc.uiController.doSpeak(new UI.UIEvents.Speak(landlord, responseString));
                    }, "dialogLine");

                    sceneRoot.Add(() =>
                    {
                        location.getComponent<SafeHouse>().evict();
                        mc.addCombatMessage("(Your possessions at this location have been moved to the shelter)");
                    }, "notice");
                    sceneRoot.Add(() => endRound(), "End Round");
                }
            }

            MasterController.GetMC().doNextAction();
        }

        public void talkBuyWeapons()
        {
            Entity bm = Factories.WorldFactory.buildLocation("BLACK_MARKET");
            bm.getComponent<Shop>().startShopping(squad, true);

            sceneRoot.Add(() => 
            {
                MasterController.GetMC().uiController.shop.close();
                MasterController.GetMC().uiController.siteMode.buildMap(location, squadPosition.z);
                MasterController.GetMC().uiController.siteMode.show();
                MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
            }, "Show Site Screen");
        }

        public void talkSurrender()
        {
            List<Entity> tempSquad = new List<Entity>(squad);

            foreach (Entity item in squad.inventory)
            {
                foreach (Entity e in squad)
                {
                    e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_THEFT);
                }
            }

            foreach (Entity e in tempSquad)
            {
                if (e == null) continue;
                e.getComponent<CriminalRecord>().arrest();
            }

            MasterController.GetMC().addMessage("The Squad is arrested", true);
            //End the siege if it was one
            if(location.hasComponent<SafeHouse>() && location.getComponent<SafeHouse>().underSiege)
                location.getComponent<SafeHouse>().raidUnoccupiedSafehouse(location.getComponent<SafeHouse>().siegeType);

            MasterController.GetMC().doNextAction(true);
        }

        public void kidnap(Entity lib, Entity con)
        {
            MasterController mc = MasterController.GetMC();
            startRound();

            //This weapon is not threatening
            if ((lib.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.TAKE_HOSTAGE) == 0)
            {
                int aroll = lib.getComponent<CreatureBase>().Skills[Constants.SKILL_MARTIAL_ARTS].roll();
                int droll = con.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_AGILITY].roll();

                lib.getComponent<CreatureBase>().Skills[Constants.SKILL_MARTIAL_ARTS].addExperience(droll);

                if(aroll > droll)
                {
                    sceneRoot.Add(() =>
                    {
                        mc.addCombatMessage(lib.getComponent<CreatureInfo>().getName() + " snatches " + con.getComponent<CreatureInfo>().encounterName + "!");
                    }, "kidnap grab");
                    sceneRoot.Add(() =>
                    {
                        mc.addCombatMessage(con.getComponent<CreatureInfo>().encounterName + " is struggling and screaming!");
                        encounterEntities[encounterEntities.IndexOf(con)] = null;
                        lib.getComponent<Liberal>().hauledUnit = con;
                        bool othersInEnc = false;
                        foreach (Entity e in encounterEntities)
                        {
                            if (e != null && e.getComponent<Body>().Alive) othersInEnc = true;
                            break;
                        }
                        if (othersInEnc)
                        {
                            alienateCheck(true);
                            alarmTriggered = true;
                            siteCrime += 5;
                            foreach (Entity e in squad)
                                e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_KIDNAPPING);
                            con.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.KIDNAPPED;
                            if (con.def == "NEWSANCHOR")
                                MasterController.lcs.offendedCableNews = true;
                            if (con.def == "RADIOPERSONALITY")
                                MasterController.lcs.offendedAMRadio = true;
                        }
                        else
                        {
                            int time = 20 + mc.LCSRandom(10);
                            if (time < 1) time = 1;
                            if (suspicionTimer > time || suspicionTimer == -1) suspicionTimer = time;
                        }
                        mc.doNextAction();
                    }, "kidnap success");
                }
                else
                {
                    sceneRoot.Add(() =>
                    {
                        mc.addCombatMessage(lib.getComponent<CreatureInfo>().getName() + " grabs at " + con.getComponent<CreatureInfo>().encounterName + " but " + con.getComponent<CreatureInfo>().heShe().ToLower() + " writhes away!");
                    }, "kidnap grab");
                    sceneRoot.Add(() =>
                    {
                        alarmTriggered = true;
                        mc.doNextAction();
                    }, "kidnap fail");
                }
            }
            else
            {
                string threatText = mc.swearFilter("Bitch", "Please") + ", be cool.";
                sceneRoot.Add(() => 
                {
                    mc.addCombatMessage(lib.getComponent<CreatureInfo>().getName() + " shows " + con.getComponent<CreatureInfo>().encounterName + " the " + lib.getComponent<Inventory>().getWeapon().getComponent<ItemBase>().getName() + " and says \"" + threatText + "\"");
                    mc.uiController.doSpeak(new UI.UIEvents.Speak(lib, threatText));
                }, "kidnap threat");
                sceneRoot.Add(() => 
                {
                    int time = 20 + mc.LCSRandom(10);
                    if (time < 1) time = 1;
                    if (suspicionTimer > time || suspicionTimer == -1) suspicionTimer = time;
                    encounterEntities[encounterEntities.IndexOf(con)] = null;
                    lib.getComponent<Liberal>().hauledUnit = con;
                    mc.doNextAction();
                },"kidnap success");               
            }

            sceneRoot.Add(() =>
            {
                if (alarmTriggered)
                    Fight.theyFight(squad, encounterEntities, sceneRoot);

                mc.doNextAction();
            }, "Retaliation");
            sceneRoot.Add(() => endRound(), "End Round");
            mc.doNextAction();
        }

        public void robBankNote()
        {
            MasterController mc = MasterController.GetMC();
            startRound();

            sceneRoot.Add(() =>
            {
                Entity robber = squad[mc.LCSRandom(squad.Count)];

                string robText = robber.getComponent<CreatureInfo>().getName() + " passes the teller a note: ";

                switch (mc.LCSRandom(10))
                {
                    case 0: robText += "KINDLY PUT MONEY IN BAG. OR ELSE."; break;
                    case 1: robText += "I AM LIBERATING YOUR MONEY SUPPLY."; break;
                    case 2: robText += "THIS IS A ROBBERY. GIVE ME THE MONEY."; break;
                    case 3: robText += "I HAVE A GUN. CASH PLEASE."; break;
                    case 4: robText += "THE LIBERAL CRIME SQUAD REQUESTS CASH."; break;
                    case 5: robText += "I AM MAKING A WITHDRAWAL. ALL YOUR MONEY."; break;
                    case 6: robText += "YOU ARE BEING ROBBED. GIVE ME YOUR MONEY."; break;
                    case 7: robText += "PLEASE PLACE LOTS OF DOLLARS IN THIS BAG."; break;
                    case 8: robText += "SAY NOTHING. YOU ARE BEING ROBBED."; break;
                    case 9: robText += "ROBBERY. GIVE ME CASH. NO FUNNY MONEY."; break;
                }

                mc.addCombatMessage(robText);
            }, "pass note");

            sceneRoot.Add(() =>
            {
                string text = "The bank teller reads the note, ";

                if(location.getComponent<TroubleSpot>().highSecurity > 0)
                {
                    switch (mc.LCSRandom(5))
                    {
                        case 0: text += "gestures"; break;
                        case 1: text += "signals"; break;
                        case 2: text += "shouts"; break;
                        case 3: text += "screams"; break;
                        case 4: text += "gives a warning"; break;
                    }

                    text += ", and dives for cover as the guards move in on the squad!";

                    alarmTriggered = true;
                    foreach(Entity e in squad)
                    {
                        e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_BANK_ROBBERY);
                    }

                    siteCrime += 30;

                    for (int i = 0; i < 4; i++)
                        encounterEntities.Add(Factories.CreatureFactory.create("MERC"));
                }
                else
                {
                    Entity teller = encounterEntities.Find(e => e != null && e.def == "BANK_TELLER");

                    switch (mc.LCSRandom(5))
                    {
                        case 0: text += "nods calmly"; break;
                        case 1: text += "looks startled"; break;
                        case 2: text += "bites " + teller.getComponent<CreatureInfo>().hisHer().ToLower() + " lip"; break;
                        case 3: text += "grimaces"; break;
                        case 4: text += "frowns"; break;
                    }

                    text += ", and slips several bricks of cash into the squad's bag.";

                    foreach (Entity e in squad)
                    {
                        e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_BANK_ROBBERY);
                    }

                    siteCrime += 30;
                    suspicionTimer = 0;
                    lootedCash += 5000;
                }

                mc.addCombatMessage(text);                
                MasterController.news.currentStory.addCrime("BANKTELLERROBBERY");
            }, "teller response");

            getSquadTile().getComponent<TileSpecial>().used = true;
            bankRobbed = true;

            sceneRoot.Add(() => endRound(), "End Round");
            mc.doNextAction();
        }

        public void robBankThreaten()
        {
            MasterController mc = MasterController.GetMC();
            startRound();

            Entity armedLiberal = null;

            foreach(Entity e in squad)
            {
                if (e.getComponent<Inventory>().isWeaponThreatening())
                {
                    armedLiberal = e;
                    break;
                }
            }

            sceneRoot.Add(() =>
            {
                string text = "";

                if(armedLiberal != null)
                {
                    text += armedLiberal.getComponent<CreatureInfo>().getName();
                    text += " brandishes the ";
                    text += armedLiberal.getComponent<Inventory>().getWeapon().getComponent<ItemBase>().getName() + " and";
                }
                else
                {
                    text += squad[mc.LCSRandom(squad.Count)].getComponent<CreatureInfo>().getName();
                }

                text += " says \"" + MasterController.lcs.slogan + "\", OPEN THE VAULT, NOW!";

                mc.addCombatMessage(text);              
            }, "threaten");

            sceneRoot.Add(() =>
            {
                int aroll = squad.getBestAtSkill(Constants.SKILL_PERSUASION).getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].roll();
                Difficulty diff = Difficulty.VERYEASY;

                if (armedLiberal == null) diff += 12;
                if (location.getComponent<TroubleSpot>().highSecurity > 0) diff += 12;
                
                if (aroll < (int)diff)
                {
                    mc.addCombatMessage("The bank teller dives for cover as guards move in on the squad!");
                    alarmTriggered = true;
                    siteAlienate = 2;

                    foreach (Entity e in squad)
                    {
                        e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_BANK_ROBBERY);
                    }
                    siteCrime += 50;
                    MasterController.news.currentStory.addCrime("BANKSTICKUP");
                    if(location.getComponent<TroubleSpot>().highSecurity > 0)
                    {
                        for (int i = 0; i < 6; i++)
                            encounterEntities.Add(Factories.CreatureFactory.create("MERC"));
                    }
                    else
                    {
                        for (int i = 0; i < 6; i++)
                            encounterEntities.Add(Factories.CreatureFactory.create("SECURITYGUARD"));
                    }
                }
                else
                {
                    mc.addCombatMessage("The bank employees hesitantly cooperate!");
                    mc.addCombatMessage("The bank vault is open!");
                    alarmTriggered = true;
                    siteAlienate = 2;

                    foreach (Entity e in squad)
                    {
                        e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_BANK_ROBBERY);
                    }
                    siteCrime += 50;
                    MasterController.news.currentStory.addCrime("BANKSTICKUP");

                    for(int z = 0; z < location.getComponent<TroubleSpot>().map.Count; z++)
                        for(int x = 0; x < location.getComponent<TroubleSpot>().map[z].GetLength(0); x++)
                            for(int y = 0; y < location.getComponent<TroubleSpot>().map[z].GetLength(1); y++)
                            {
                                if(location.getComponent<TroubleSpot>().map[z][x,y].hasComponent<TileSpecial>() &&
                                   location.getComponent<TroubleSpot>().map[z][x, y].getComponent<TileSpecial>().name == "VAULT_DOOR")
                                {
                                    location.getComponent<TroubleSpot>().map[z][x, y].getComponent<TileSpecial>().used = true;
                                }
                            }
                }
            }, "response");

            getSquadTile().getComponent<TileSpecial>().used = true;
            bankRobbed = true;

            sceneRoot.Add(() => endRound(), "End Round");
            mc.doNextAction();
        }

        private void tryMove(Position pos, bool allowEncounter = true)
        {
            MasterController mc = MasterController.GetMC();

            if (location.getComponent<TroubleSpot>().map[pos.z][pos.x, pos.y].getComponent<TileBase>().isWalkable())
            {
                startRound();
                sceneRoot.Add(disguiseCheck, "disguiseCheck");
                //Moving happens after the end of the round here, so that hauling and other end of round stuff occurs before.
                sceneRoot.Add(()=>endRound(() =>
                {
                    bool someoneBleeding = false;

                    foreach (Entity e in squad)
                    {
                        if (e.getComponent<Body>().isBleeding())
                        {
                            someoneBleeding = true;
                            break;
                        }

                        if (e.getComponent<Liberal>().hauledUnit != null)
                        {
                            if (e.getComponent<Liberal>().hauledUnit.getComponent<Body>().isBleeding())
                            {
                                someoneBleeding = true;
                                break;
                            }
                        }
                    }

                    if (someoneBleeding)
                    {
                        if (squadPosition.x < pos.x)
                        {
                            if(getSquadTile().getComponent<TileBase>().bloodTrail_E == TileBase.Bloodstain.NONE)
                                getSquadTile().getComponent<TileBase>().bloodTrail_E = (TileBase.Bloodstain) mc.LCSRandom(3) + 1;
                            if(location.getComponent<TroubleSpot>().map[pos.z][pos.x, pos.y].getComponent<TileBase>().bloodTrail_W == TileBase.Bloodstain.NONE)
                                location.getComponent<TroubleSpot>().map[pos.z][pos.x, pos.y].getComponent<TileBase>().bloodTrail_W = (TileBase.Bloodstain)mc.LCSRandom(3) + 1;
                        }

                        if (squadPosition.x > pos.x)
                        {
                            if (getSquadTile().getComponent<TileBase>().bloodTrail_W == TileBase.Bloodstain.NONE)
                                getSquadTile().getComponent<TileBase>().bloodTrail_W = (TileBase.Bloodstain)mc.LCSRandom(3) + 1;
                            if (location.getComponent<TroubleSpot>().map[pos.z][pos.x, pos.y].getComponent<TileBase>().bloodTrail_E == TileBase.Bloodstain.NONE)
                                location.getComponent<TroubleSpot>().map[pos.z][pos.x, pos.y].getComponent<TileBase>().bloodTrail_E = (TileBase.Bloodstain)mc.LCSRandom(3) + 1;
                        }

                        if (squadPosition.y < pos.y)
                        {
                            if (getSquadTile().getComponent<TileBase>().bloodTrail_S == TileBase.Bloodstain.NONE)
                                getSquadTile().getComponent<TileBase>().bloodTrail_S = (TileBase.Bloodstain)mc.LCSRandom(3) + 1;
                            if (location.getComponent<TroubleSpot>().map[pos.z][pos.x, pos.y].getComponent<TileBase>().bloodTrail_N == TileBase.Bloodstain.NONE)
                                location.getComponent<TroubleSpot>().map[pos.z][pos.x, pos.y].getComponent<TileBase>().bloodTrail_N = (TileBase.Bloodstain)mc.LCSRandom(3) + 1;
                        }

                        if (squadPosition.y > pos.y)
                        {
                            if (getSquadTile().getComponent<TileBase>().bloodTrail_N == TileBase.Bloodstain.NONE)
                                getSquadTile().getComponent<TileBase>().bloodTrail_N = (TileBase.Bloodstain)mc.LCSRandom(3) + 1;
                            if (location.getComponent<TroubleSpot>().map[pos.z][pos.x, pos.y].getComponent<TileBase>().bloodTrail_S == TileBase.Bloodstain.NONE)
                                location.getComponent<TroubleSpot>().map[pos.z][pos.x, pos.y].getComponent<TileBase>().bloodTrail_S = (TileBase.Bloodstain)mc.LCSRandom(3) + 1;
                        }
                    }

                    if(bloodyShoes > 0)
                    {
                        if (squadPosition.x < pos.x)
                        {
                            getSquadTile().getComponent<TileBase>().bloodPrints_E_E = true;
                            location.getComponent<TroubleSpot>().map[pos.z][pos.x, pos.y].getComponent<TileBase>().bloodPrints_W_E = true;
                        }

                        if (squadPosition.x > pos.x)
                        {
                            getSquadTile().getComponent<TileBase>().bloodPrints_W_W = true;
                            location.getComponent<TroubleSpot>().map[pos.z][pos.x, pos.y].getComponent<TileBase>().bloodPrints_E_W = true;
                        }

                        if (squadPosition.y < pos.y)
                        {
                            getSquadTile().getComponent<TileBase>().bloodPrints_S_S = true;
                            location.getComponent<TroubleSpot>().map[pos.z][pos.x, pos.y].getComponent<TileBase>().bloodPrints_N_S = true;
                        }

                        if (squadPosition.y > pos.y)
                        {
                            getSquadTile().getComponent<TileBase>().bloodPrints_N_N = true;
                            location.getComponent<TroubleSpot>().map[pos.z][pos.x, pos.y].getComponent<TileBase>().bloodPrints_S_N = true;
                        }

                        bloodyShoes--;
                    }

                    squadPosition = pos;
                    reveal(squadPosition.x, squadPosition.y, squadPosition.z);

                    if (!location.hasComponent<SafeHouse>() || !location.getComponent<SafeHouse>().underSiege)
                    {
                        if (!inEncounter)
                        {
                            if (!checkEntrySpecial() && !checkExit() && allowEncounter)
                            {
                                if (alarmTimer > 80)
                                {
                                    if (mc.LCSRandom(5) == 0)
                                        if(mc.LCSRandom(3) == 0 || (location.getComponent<TroubleSpot>().getFlags() & LocationDef.TroubleSpotFlag.RESIDENTIAL) == 0)
                                        newEncounter();
                                }
                                else if (mc.LCSRandom(10) == 0)
                                {
                                    if (mc.LCSRandom(3) == 0 || (location.getComponent<TroubleSpot>().getFlags() & LocationDef.TroubleSpotFlag.RESIDENTIAL) == 0)
                                        newEncounter();
                                }
                            }
                        }
                        else
                        {
                            leaveEncounter();
                        }
                    }
                    else
                    {
                        moveSiegeEnemies();
                    }

                    activateEntrySpecial();
                    MasterController.GetMC().uiController.siteMode.buildMap(location, squadPosition.z);
                }, "Move"), "End Round");
                
                MasterController.GetMC().doNextAction();
            }
            else if (location.getComponent<TroubleSpot>().map[pos.z][pos.x, pos.y].hasComponent<TileDoor>())
            {
                Entity door = location.getComponent<TroubleSpot>().map[pos.z][pos.x, pos.y];

                if (!door.getComponent<TileDoor>().locked)
                {
                    if (door.getComponent<TileDoor>().alarm)
                    {
                        List<UI.PopupOption> options = new List<UI.PopupOption>();
                        processingRound = true;
                        options.Add(new UI.PopupOption("Yes", () =>
                        {
                            startRound();
                            sceneRoot.Add(disguiseCheck, "disguiseCheck");
                            sceneRoot.Add(() =>
                            {
                                mc.addCombatMessage("The alarm goes off!", true);
                                door.getComponent<TileDoor>().open = true;
                                alarmTriggered = true;
                            }, "TryMove->OpenAlarmDoor");
                            sceneRoot.Add(()=>endRound(), "End Round");
                        }));
                        options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

                        mc.uiController.showYesNoPopup("EMERGENCY EXIT ONLY. ALARM WILL SOUND. Try the door anyway?", options);
                    }
                    else
                    {
                        startRound();
                        sceneRoot.Add(disguiseCheck, "disguiseCheck");
                        sceneRoot.Add(() =>
                        {
                            door.getComponent<TileDoor>().open = true;
                            reveal(squadPosition.x, squadPosition.y, squadPosition.z);
                            mc.doNextAction();
                        }, "TryMove->OpenDoor");
                        sceneRoot.Add(()=>endRound(), "End Round");
                        mc.doNextAction();
                    }
                }
                else
                {
                    string popupText = "";

                    if (door.getComponent<TileDoor>().alarm)
                        popupText = "This door appears to be wired up to an alarm.";
                    else
                        popupText = "You try the door but it is locked.";

                    if (squad.getBestAtSkill("SECURITY").getComponent<CreatureBase>().Skills["SECURITY"].level > 0 &&
                        !door.getComponent<TileDoor>().triedUnlock)
                    {
                        List<UI.PopupOption> options = new List<UI.PopupOption>();
                        processingRound = true;
                        options.Add(new UI.PopupOption("Yes", () =>
                        {
                            startRound();
                            sceneRoot.Add(()=> { noticeCheck(false); }, "noticeCheck");
                            sceneRoot.Add(() =>
                            {
                                door.getComponent<TileDoor>().tryUnlock(squad);
                                MasterController.GetMC().doNextAction();
                            }, "TryMove->OpenLock");
                            sceneRoot.Add(disguiseCheck, "disguiseCheck");
                            sceneRoot.Add(()=>endRound(), "End Round");
                            mc.doNextAction();
                        }));
                        options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

                        mc.uiController.showYesNoPopup(popupText + " Attempt to pick the lock?", options);
                    }
                    else
                    {
                        List<UI.PopupOption> options = new List<UI.PopupOption>();
                        processingRound = true;
                        options.Add(new UI.PopupOption("Yes", () =>
                        {
                            startRound();
                            sceneRoot.Add(() => { noticeCheck(true, Difficulty.HEROIC); }, "noticeCheck");
                            sceneRoot.Add(() =>
                            {
                                door.getComponent<TileDoor>().tryBash(squad);
                                MasterController.GetMC().doNextAction();
                            }, "TryMove->BashLock");
                            sceneRoot.Add(disguiseCheck, "disguiseCheck");
                            sceneRoot.Add(()=>endRound(), "End Round");
                            mc.doNextAction();
                        }));
                        options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

                        mc.uiController.showYesNoPopup(popupText + " Force it open?", options);
                    }
                }
            }
        }

        public MapEnemy enemyInPosition(Position pos)
        {
            foreach(MapEnemy enemy in enemies)
            {
                if (enemy.position.samePos(pos)) return enemy;
            }

            return null;
        }

        public bool encounterHasEnemies()
        {
            foreach(Entity e in encounterEntities)
            {
                if (e == null) continue;
                if (e.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE || (e.getComponent<CreatureInfo>().alignment == Alignment.MODERATE && e.def == "NEGOTIATOR"))
                    return true;
            }

            return false;
        }

        public bool encounterHasPolice()
        {
            foreach(Entity e in encounterEntities)
            {
                if (e == null) continue;
                if ((e.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.POLICE) != 0)
                    return true;
            }

            return false;
        }

        public bool haveHostage()
        {
            foreach(Entity e in squad)
            {
                if (e == null) continue;
                if(e.getComponent<Liberal>().hauledUnit != null &&
                    !e.getComponent<Liberal>().hauledUnit.hasComponent<Liberal>())
                {
                    return true;
                }
            }

            return false;
        }

        public bool canGraffiti()
        {
            bool canGraffiti = false;

            foreach(Entity e in squad)
            {
                if (e.getComponent<Inventory>().canGraffiti())
                {
                    canGraffiti = true;
                    break;
                }
            }

            if (canGraffiti && location.getComponent<TroubleSpot>().hasAdjacentWall(squadPosition) &&
                getSquadTile().getComponent<TileBase>().graffiti != TileBase.Graffiti.LCS)
            {
                return true;
            }
            else return false;
        }

        public void lootOrGraffitiTile()
        {
            MasterController mc = MasterController.GetMC();

            startRound();
            if (getSquadTile().getComponent<TileBase>().loot.Count > 0 ||
                    getSquadTile().getComponent<TileBase>().cash > 0)
            {
                sceneRoot.Add(() =>
                {

                    int time = 20 + mc.LCSRandom(10);
                    if (time < 1) time = 1;
                    if (suspicionTimer > time || suspicionTimer == -1) suspicionTimer = time;

                    Dictionary<string, int> lootedItems = new Dictionary<string, int>();
                    foreach (Entity e in getSquadTile().getComponent<TileBase>().loot)
                    {
                        squad.inventory.Add(e);
                        string itemName = e.getComponent<ItemBase>().getName();

                        if (lootedItems.ContainsKey(itemName))
                            lootedItems[itemName]++;
                        else
                            lootedItems[itemName] = 1;
                    }
                    getSquadTile().getComponent<TileBase>().loot.Clear();

                    foreach (string key in lootedItems.Keys)
                    {
                        mc.addCombatMessage("You found " + (lootedItems[key] == 1 ? "a" : MasterController.NumberToWords(lootedItems[key]).ToLower()) + " " + key);
                    }

                    int tileCash = getSquadTile().getComponent<TileBase>().cash;

                    lootedCash += tileCash;
                    if (tileCash > 0) mc.addCombatMessage("You found $" + tileCash);
                    getSquadTile().getComponent<TileBase>().cash = 0;
                    foreach (Entity e in squad)
                    {
                        e.getComponent<CreatureBase>().juiceMe(1, 200);
                    }
                    if (MasterController.news.currentStory != null) MasterController.news.currentStory.addCrime("STOLEGROUND");
                    siteCrime++;
                    mc.doNextAction();

                }, "loot");

                sceneRoot.Add(() => { noticeCheck(false, Difficulty.EASY, Constants.CRIME_THEFT); }, "noticeCheck");
                sceneRoot.Add(disguiseCheck, "disguiseCheck");
            }
            else
            {
                int time = 20 + mc.LCSRandom(10);
                if (time< 1) time = 1;
                if (suspicionTimer > time || suspicionTimer == -1) suspicionTimer = time;

                siteCrime++;
                if (MasterController.news.currentStory != null)
                {
                    MasterController.news.currentStory.addCrime("TAGGING");
                    MasterController.news.currentStory.claimed = true;
                }
                foreach(Entity e in squad)
                {
                    e.getComponent<CreatureBase>().juiceMe(1, 50);
                    e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_VANDALISM);
                }

                getSquadTile().getComponent<TileBase>().graffiti = TileBase.Graffiti.LCS;

                mc.addCombatMessage("The squad sprays Liberal graffiti!");
                sceneRoot.Add(() => { noticeCheck(false, Difficulty.HARD); }, "noticeCheck");
                sceneRoot.Add(disguiseCheck, "disguiseCheck");
            }

            sceneRoot.Add(() => endRound(), "End Round");
            mc.doNextAction();
        }

        private void newEncounter(bool moved = true, string encounterText = "")
        {
            //No random encounters during sieges
            if (location.hasComponent<SafeHouse>() && location.getComponent<SafeHouse>().underSiege) return;

            encounterEntities.Clear();
            encounterEntities.AddRange(location.getComponent<TroubleSpot>().generateEncounter(alarmTimer));
            foreach(Entity e in encounterEntities)
            {
                if(siteAlienate == 2)
                    e.getComponent<CreatureInfo>().alignment = Alignment.CONSERVATIVE;
                else if (siteAlienate == 1 && e.getComponent<CreatureInfo>().alignment == Alignment.MODERATE)
                    e.getComponent<CreatureInfo>().alignment = Alignment.CONSERVATIVE;
            }

            //Limit one Police Negotiator per encounter
            if(encounterEntities.Count(e => e.def == "NEGOTIATOR") > 1)
            {
                List<int> negotiatorSlots = new List<int>();
                for(int i = 0; i < encounterEntities.Count; i++)
                {
                    if (encounterEntities[i].def == "NEGOTIATOR") negotiatorSlots.Add(i);
                }
                for(int i = 1; i < negotiatorSlots.Count; i++)
                {
                    encounterEntities[negotiatorSlots[i]] = Factories.CreatureFactory.create("COP");
                }
            }

            attacked = false;

            if (encounterText == "")
            {
                if (encounterEntities.Count == 1)
                {
                    encounterText = "There is someone";
                }
                else if (encounterEntities.Count <= 3)
                {
                    encounterText = "There are a few people";
                }
                else if (encounterEntities.Count <= 6)
                {
                    encounterText = "There is a group of people";
                }
                else
                {
                    encounterText = "There is a crowd of people";
                }

                if (moved)
                    encounterText += " up ahead.";
                else
                    encounterText += " passing by.";
            }

            MasterController.GetMC().addCombatMessage(encounterText, encounterWarnings);

            if (encounterWarnings)
            {
                sceneRoot.Add(() =>
                {
                    inEncounter = true;
                    MasterController.GetMC().uiController.siteMode.startEncounter();
                    MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                }, "New Encounter");
                MasterController.GetMC().doNextAction();
            }
            else
            {
                inEncounter = true;
                MasterController.GetMC().uiController.siteMode.startEncounter();
                MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
            }
        }

        private void leaveEncounter()
        {
            inEncounter = false;
            MasterController.GetMC().uiController.siteMode.leaveEncounter();
            MasterController.GetMC().uiController.enemyUI.close();
            encounterEntities.Clear();
        }

        private void reveal(int x, int y, int z)
        {
            int maxX = location.getComponent<TroubleSpot>().map[z].GetLength(0) - 1;
            int maxY = location.getComponent<TroubleSpot>().map[z].GetLength(1) - 1;

            location.getComponent<TroubleSpot>().reveal(x, y, z);

            if (x < maxX)
                location.getComponent<TroubleSpot>().reveal(x + 1, y, z);
            if (x > 0)
                location.getComponent<TroubleSpot>().reveal(x - 1, y, z);
            if (y < maxY)
                location.getComponent<TroubleSpot>().reveal(x, y + 1, z);
            if (y > 0)
                location.getComponent<TroubleSpot>().reveal(x, y - 1, z);

            if (x < maxX && y < maxY)
                location.getComponent<TroubleSpot>().reveal(x + 1, y + 1, z);
            if (x < maxX && y > 0)
                location.getComponent<TroubleSpot>().reveal(x + 1, y - 1, z);
            if (x > 0 && y < maxY)
                location.getComponent<TroubleSpot>().reveal(x - 1, y + 1, z);
            if (x > 0 && y > 0)
                location.getComponent<TroubleSpot>().reveal(x - 1, y - 1, z);
        }

        public Entity getSquadTile()
        {
            return location.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x, squadPosition.y];
        }

        private void noticeCheck(bool mistake, Difficulty difficulty = Difficulty.EASY, string crime = "")
        {
            if (alarmTriggered || !inEncounter)
            {
                //If the alarm has already triggered, people will be watching everything you do
                if (alarmTriggered && inEncounter && crime != "")
                {
                    foreach (Entity lib in squad)
                    {
                        lib.getComponent<CriminalRecord>().addCrime(crime);
                    }
                }
                MasterController.GetMC().doNextAction();
                return;
            }

            Entity topE = null;
            foreach(Entity e in squad)
            {
                if (topE == null) topE = e;
                if (e.getComponent<CreatureBase>().Skills["STEALTH"].level > topE.getComponent<CreatureBase>().Skills["STEALTH"].level)
                    topE = e;
            }

            foreach (Entity e in encounterEntities)
            {  //Prisoners shouldn't shout for help.
                if (e == null) continue;

                if (e.getComponent<CreatureInfo>().encounterName == "Prisoner" || topE.getComponent<CreatureBase>().Skills["STEALTH"].check(difficulty)) continue;
                else
                {
                    string messageText = e.getComponent<CreatureInfo>().getName() + " observes your Liberal activity ";
                    if (e.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
                        messageText += "and lets forth a piercing Conservative alarm cry!";
                    else
                        messageText += "and shouts for help!";
                    MasterController.GetMC().addCombatMessage(messageText, true);

                    if(crime != "")
                    {
                        foreach(Entity lib in squad)
                        {
                            lib.getComponent<CriminalRecord>().addCrime(crime);
                        }
                    }

                    alarmTriggered = true;
                    alienateCheck(mistake);
                    break;
                }
            }

            MasterController.GetMC().doNextAction();
        }

        public void alienateCheck(bool mistake)
        {
            if (!inEncounter) return;
            if (location.hasComponent<SafeHouse>() && location.getComponent<SafeHouse>().underSiege) return;

            bool alienate = false, alienatebig = false;

            int oldsitealienate = siteAlienate;

            List<Entity> noticer = new List<Entity>();
            foreach(Entity e in encounterEntities)
            {
                if (e == null) continue;
                if (e.getComponent<CreatureInfo>().encounterName == "Prisoner") continue;

                if (e.getComponent<Body>().Alive && (e.getComponent<CreatureInfo>().alignment == Alignment.MODERATE || (e.getComponent<CreatureInfo>().alignment == Alignment.LIBERAL && mistake)))
                    noticer.Add(e);
            }

            if (noticer.Count > 0)
            {
                do
                {
                    Entity n = noticer[MasterController.GetMC().LCSRandom(noticer.Count)];
                    noticer.Remove(n);

                    if (n.getComponent<CreatureInfo>().alignment == Alignment.LIBERAL) alienatebig = true;
                    else alienate = true;
                } while (noticer.Count > 0);

                if (alienatebig) siteAlienate = 2;
                if (alienate && siteAlienate != 2) siteAlienate = 1;

                if (oldsitealienate < siteAlienate)
                {
                    string messageText = "";

                    if (siteAlienate == 1) messageText += "We've alienated the masses here!";
                    else messageText += "We've alienated absolutely everyone here!";

                    MasterController.GetMC().addCombatMessage(messageText, true);

                    alarmTriggered = true;

                    foreach (Entity e in encounterEntities)
                    {
                        if (e == null) continue;
                        //TODO: Write a proper "Conservatize" function to handle name changes for certain creature types
                        if (alienatebig)
                            e.getComponent<CreatureInfo>().alignment = Alignment.CONSERVATIVE;
                        else if (alienate && e.getComponent<CreatureInfo>().alignment == Alignment.MODERATE)
                            e.getComponent<CreatureInfo>().alignment = Alignment.CONSERVATIVE;
                    }
                }
            }
        }

        public void useTile()
        {
            if(getSquadTile().getComponent<TileFloor>().type == TileFloor.Type.STAIRS_UP)
            {
                Position stairPosition = null;

                bool breakOut = false;
                for(int x=0;x<location.getComponent<TroubleSpot>().map[squadPosition.z + 1].GetLength(0); x++)
                {
                    for(int y=0;y<location.getComponent<TroubleSpot>().map[squadPosition.z + 1].GetLength(1); y++)
                    {
                        if (location.getComponent<TroubleSpot>().map[squadPosition.z + 1][x, y].def == "NONE") continue;
                        if(location.getComponent<TroubleSpot>().map[squadPosition.z + 1][x,y].getComponent<TileFloor>().type == TileFloor.Type.STAIRS_DOWN)
                        {
                            stairPosition = new Position(x, y, squadPosition.z + 1);
                            breakOut = true;
                        }

                        if (breakOut) break;
                    }
                    if (breakOut) break;
                }

                if (stairPosition != null)
                {
                    tryMove(stairPosition);
                }
            }
            else if (getSquadTile().getComponent<TileFloor>().type == TileFloor.Type.STAIRS_DOWN)
            {
                Position stairPosition = null;

                bool breakOut = false;
                for (int x = 0; x < location.getComponent<TroubleSpot>().map[squadPosition.z - 1].GetLength(0); x++)
                {
                    for (int y = 0; y < location.getComponent<TroubleSpot>().map[squadPosition.z - 1].GetLength(1); y++)
                    {
                        if (location.getComponent<TroubleSpot>().map[squadPosition.z - 1][x, y].def == "NONE") continue;
                        if (location.getComponent<TroubleSpot>().map[squadPosition.z - 1][x, y].getComponent<TileFloor>().type == TileFloor.Type.STAIRS_UP)
                        {
                            stairPosition = new Position(x, y, squadPosition.z - 1);
                            breakOut = true;
                        }

                        if (breakOut) break;
                    }
                    if (breakOut) break;
                }

                if (stairPosition != null)
                {
                    tryMove(stairPosition);
                }
            }
            else if(getSquadTile().hasComponent<TileSpecial>())
            {
                switch(getSquadTile().getComponent<TileSpecial>().name)
                {
                    case "LAB_COSMETICS_CAGEDANIMALS":
                        specialCagedAnimalsCosmetics(); break;
                    case "LAB_GENETIC_CAGEDANIMALS":
                        specialCagedAnimalsGenetics(); break;
                    case "POLICESTATION_LOCKUP":
                        specialLockup(true); break;
                    case "COURTHOUSE_LOCKUP":
                        specialLockup(false); break;
                    case "COURTHOUSE_JURYROOM":
                        specialJuryRoom(); break;
                    case "PRISON_CONTROL_LOW":
                        specialPrisonControl(0); break;
                    case "PRISON_CONTROL_MEDIUM":
                        specialPrisonControl(1); break;
                    case "PRISON_CONTROL_HIGH":
                        specialPrisonControl(2); break;
                    case "INTEL_SUPERCOMPUTER":
                        specialSuperComputer(); break;
                    case "SWEATSHOP_EQUIPMENT":
                        specialSmash("You see some textile equipment."); break;
                    case "POLLUTER_EQUIPMENT":
                        specialSmash("You see some industrial equipment."); break;
                    case "NUCLEAR_ONOFF":
                        specialNuclear(); break;
                    case "HOUSE_PHOTOS":
                        specialHousePhotos(); break;
                    case "CORPORATE_FILES":
                        specialCorporateFiles(); break;
                    case "RADIO_BROADCASTSTUDIO":
                        specialRadioBroadcast(); break;
                    case "NEWS_BROADCASTSTUDIO":
                        specialNewsBroadcast(); break;
                    case "SIGN_ONE":
                    case "SIGN_TWO":
                    case "SIGN_THREE":
                        break;
                    case "ARMORY":
                        specialArmory();
                        break;
                    case "DISPLAY_CASE":
                        specialSmash("You see a display case."); break;
                    case "BANK_VAULT":
                        specialBankVault();
                        break;
                    case "BANK_MONEY":
                        specialBankMoney();
                        break;
                    default:
                        break;
                }
            }
        }

        private void specialHousePhotos()
        {
            MasterController mc = MasterController.GetMC();
            List<UI.PopupOption> options = new List<UI.PopupOption>();
            processingRound = true;
            options.Add(new UI.PopupOption("Yes", () =>
            {
                startRound();

                if (tryUnlock(Difficulty.HEROIC))
                {
                    int itemCount = 0;

                    if(mc.LCSRandom(5) == 0)
                    {
                        itemCount++;
                        mc.addCombatMessage("The squad has found a Desert Eagle.");
                        Entity deagle = Factories.ItemFactory.create("WEAPON_DESERT_EAGLE");
                        deagle.getComponent<Weapon>().clip = Factories.ItemFactory.create(deagle.getComponent<Weapon>().getDefaultClip().type);
                        squad.inventory.Add(deagle);
                        for (int i = 0; i < 9; i++)
                            squad.inventory.Add(Factories.ItemFactory.create(deagle.getComponent<Weapon>().getDefaultClip().type));
                    }

                    if(mc.LCSRandom(2) == 0)
                    {
                        itemCount++;
                        mc.addCombatMessage("This guy sure had a lot of $100 bills.");
                        lootedCash += 100 * (mc.LCSRandom(100) + 10);
                    }

                    if(mc.LCSRandom(2) == 0)
                    {
                        itemCount++;
                        mc.addCombatMessage("The Squad liberates some expensive jewellery.");
                        for (int i = 0; i < 3; i++)
                            squad.inventory.Add(Factories.ItemFactory.create("LOOT_EXPENSIVEJEWELERY"));
                    }

                    if(mc.LCSRandom(3) == 0)
                    {
                        itemCount++;
                        mc.addCombatMessage("There are some... very compromising photos here.");
                        squad.inventory.Add(Factories.ItemFactory.create("LOOT_CEOPHOTOS"));
                    }

                    if(mc.LCSRandom(3) == 0)
                    {
                        mc.addCombatMessage("There are some drugs here.");
                        //Should there be an item for this?
                    }

                    if(mc.LCSRandom(3) == 0)
                    {
                        itemCount++;
                        mc.addCombatMessage("Wow, get a load of these love letters. The squad will take those.");
                        squad.inventory.Add(Factories.ItemFactory.create("LOOT_CEOLOVELETTERS"));
                    }

                    if(mc.LCSRandom(3) == 0)
                    {
                        itemCount++;
                        mc.addCombatMessage("These documents show serious tax evasion.");
                        squad.inventory.Add(Factories.ItemFactory.create("LOOT_CEOTAXPAPERS"));
                    }

                    if (itemCount > 0)
                    {
                        foreach (Entity e in squad)
                        {
                            e.getComponent<CreatureBase>().juiceMe(50, 1000);
                            e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_THEFT);
                        }

                        siteCrime += 40;
                        int time = 20 + mc.LCSRandom(10);
                        if (suspicionTimer > time || suspicionTimer == -1) suspicionTimer = time;
                        MasterController.news.currentStory.addCrime("HOUSE_PHOTOS");

                        if (itemCount > 3)
                            mc.addCombatMessage("Nice haul!");
                    }
                    else
                    {
                        mc.addCombatMessage("Wow, it's empty. That sucks.");
                    }
                }

                getSquadTile().getComponent<TileSpecial>().used = true;
                sceneRoot.Add(() => { noticeCheck(false); }, "noticeCheck");
                sceneRoot.Add(disguiseCheck, "disguiseCheck");
                sceneRoot.Add(() => endRound(), "End Round");
                mc.doNextAction();
            }));
            options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

            mc.uiController.showYesNoPopup("You've found a safe. Open it?", options);
        }

        private void specialRadioBroadcast()
        {
            MasterController mc = MasterController.GetMC();

            if (encounterHasEnemies())
            {
                mc.addCombatMessage("The Conservatives in the room hurry the Squad, so the broadcast never happens.");
                return;
            }

            List<UI.PopupOption> options = new List<UI.PopupOption>();
            processingRound = true;
            options.Add(new UI.PopupOption("Yes", () =>
            {
                startRound();

                string resultText = "The Squad takes control of the microphone and ";
                string view = GameData.getData().viewList.Keys.ToArray()[mc.LCSRandom(GameData.getData().viewList.Count)];
                resultText += GameData.getData().viewList[view].broadcastText;

                int segmentPower = 0;

                foreach (Entity e in squad)
                {
                    CreatureBase ebase = e.getComponent<CreatureBase>();
                    e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_DISTURBANCE);
                    segmentPower += ebase.getAttributeValue(Constants.ATTRIBUTE_INTELLIGENCE);
                    segmentPower += ebase.getAttributeValue(Constants.ATTRIBUTE_HEART);
                    segmentPower += ebase.getAttributeValue(Constants.ATTRIBUTE_CHARISMA);
                    segmentPower += ebase.getSkillValue(Constants.SKILL_MUSIC);
                    segmentPower += ebase.getSkillValue(Constants.SKILL_RELIGION);
                    segmentPower += ebase.getSkillValue(Constants.SKILL_SCIENCE);
                    segmentPower += ebase.getSkillValue(Constants.SKILL_BUSINESS);
                    segmentPower += ebase.getSkillValue(Constants.SKILL_PERSUASION);
                    ebase.Skills[Constants.SKILL_PERSUASION].addExperience(50);
                }

                int segmentBonus = segmentPower / 4;
                segmentPower /= squad.Count;
                segmentPower += segmentBonus;

                if (segmentPower < 25) resultText += "\nThe Squad sounds wholly insane.";
                else if (segmentPower < 35) resultText += "\nThe show really sucks.";
                else if (segmentPower < 45) resultText += "\nIt is a very boring hour.";
                else if (segmentPower < 55) resultText +="\nIt is mediocre radio.";
                else if (segmentPower < 70) resultText +="\nThe show was all right.";
                else if (segmentPower < 85) resultText += "\nThe Squad put on a good show.";
                else if (segmentPower < 100) resultText += "\nIt was thought-provoking, even humorous.";
                else resultText += "\nIt was the best hour of AM radio EVER.";

                MasterController.generalPublic.changePublicOpinion(Constants.VIEW_LIBERALCRIMESQUAD, 10);
                MasterController.generalPublic.changePublicOpinion(Constants.VIEW_LIBERALCRIMESQUADPOS, (segmentPower-50)/2);
                if(view != Constants.VIEW_LIBERALCRIMESQUAD)
                    MasterController.generalPublic.changePublicOpinion(view, (segmentPower - 50) / 2, 1);
                else
                    MasterController.generalPublic.changePublicOpinion(view, segmentPower / 2);

                foreach(Entity e in squad)
                {
                    if(e.getComponent<Liberal>().hauledUnit != null &&
                        e.getComponent<Liberal>().hauledUnit.getComponent<Body>().Alive && 
                        e.getComponent<Liberal>().hauledUnit.def == "RADIOPERSONALITY")
                    {
                        view = GameData.getData().viewList.Keys.ToArray()[mc.LCSRandom(GameData.getData().viewList.Count)];

                        resultText += "\nA hostage is forced on air and ";
                        resultText += GameData.getData().viewList[view].broadcastText;

                        int uSegmentPower = 10;
                        CreatureBase ubase = e.getComponent<Liberal>().hauledUnit.getComponent<CreatureBase>();

                        uSegmentPower += ubase.getAttributeValue(Constants.ATTRIBUTE_INTELLIGENCE);
                        uSegmentPower += ubase.getAttributeValue(Constants.ATTRIBUTE_HEART);
                        uSegmentPower += ubase.getAttributeValue(Constants.ATTRIBUTE_CHARISMA);
                        uSegmentPower += ubase.getSkillValue(Constants.SKILL_PERSUASION);

                        if (view != Constants.VIEW_LIBERALCRIMESQUAD)
                            MasterController.generalPublic.changePublicOpinion(view, (uSegmentPower - 10) / 2, 1, 80);
                        else
                            MasterController.generalPublic.changePublicOpinion(view, uSegmentPower / 2);

                        segmentPower += uSegmentPower;
                    }
                }

                if(siteAlienate != 0 && segmentPower >= 40)
                {
                    siteAlienate = 0;
                    resultText += "\nThe moderates at the station appreciated the show. They no longer feel alienated.";
                }

                if(segmentPower < 90)
                {
                    resultText += "\nSecurity is waiting for the Squad after the show!";

                    sceneRoot.Add(() =>
                    {
                        int count = mc.LCSRandom(8) + 2;
                        for (int i = 0; i < count; i++)
                            encounterEntities.Add(Factories.CreatureFactory.create("SECURITYGUARD"));

                        if (!inEncounter)
                        {
                            inEncounter = true;
                            MasterController.GetMC().uiController.siteMode.startEncounter();
                            MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                        }
                        mc.doNextAction();
                    }, "securitySpawn");
                }
                else
                {
                    resultText += "The show was so good that security listened to it at their desks. The Squad might yet escape.";
                }

                alarmTriggered = true;

                mc.addCombatMessage(resultText, true);

                getSquadTile().getComponent<TileSpecial>().used = true;
                sceneRoot.Add(() => endRound(), "End Round");
                mc.doNextAction();
            }));
            options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

            string popupText = "";
            if (alarmTriggered) popupText = "The radio broadcasters left the equipment on in their rush to get out. Take over the studio?";
            else popupText = "You've found a radio broadcasting room. Interrupt this evening's programming?";

            mc.uiController.showYesNoPopup(popupText, options);
        }

        private void specialNewsBroadcast()
        {
            MasterController mc = MasterController.GetMC();

            if (encounterHasEnemies())
            {
                mc.addCombatMessage("The Conservatives in the room hurry the Squad, so the broadcast never happens.");
                return;
            }

            List<UI.PopupOption> options = new List<UI.PopupOption>();
            processingRound = true;
            options.Add(new UI.PopupOption("Yes", () =>
            {
                startRound();

                string resultText = "The Squad steps in front of the cameras and ";
                string view = GameData.getData().viewList.Keys.ToArray()[mc.LCSRandom(GameData.getData().viewList.Count)];
                resultText += GameData.getData().viewList[view].broadcastText;

                int segmentPower = 0;

                foreach (Entity e in squad)
                {
                    CreatureBase ebase = e.getComponent<CreatureBase>();
                    e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_DISTURBANCE);
                    segmentPower += ebase.getAttributeValue(Constants.ATTRIBUTE_INTELLIGENCE);
                    segmentPower += ebase.getAttributeValue(Constants.ATTRIBUTE_HEART);
                    segmentPower += ebase.getAttributeValue(Constants.ATTRIBUTE_CHARISMA);
                    segmentPower += ebase.getSkillValue(Constants.SKILL_MUSIC);
                    segmentPower += ebase.getSkillValue(Constants.SKILL_RELIGION);
                    segmentPower += ebase.getSkillValue(Constants.SKILL_SCIENCE);
                    segmentPower += ebase.getSkillValue(Constants.SKILL_BUSINESS);
                    segmentPower += ebase.getSkillValue(Constants.SKILL_PERSUASION);
                    ebase.Skills[Constants.SKILL_PERSUASION].addExperience(50);
                }

                int segmentBonus = segmentPower / 4;
                segmentPower /= squad.Count;
                segmentPower += segmentBonus;

                if (segmentPower < 25) resultText += "\nThe Squad sounds wholly insane.";
                else if (segmentPower < 35) resultText += "\nThe show really sucks.";
                else if (segmentPower < 45) resultText += "\nIt is a very boring hour.";
                else if (segmentPower < 55) resultText += "\nIt is mediocre TV.";
                else if (segmentPower < 70) resultText += "\nThe show was all right.";
                else if (segmentPower < 85) resultText += "\nThe Squad put on a good show.";
                else if (segmentPower < 100) resultText += "\nIt was thought-provoking, even humorous.";
                else resultText += "\nIt was the best hour of Cable TV EVER.";

                MasterController.generalPublic.changePublicOpinion(Constants.VIEW_LIBERALCRIMESQUAD, 10);
                MasterController.generalPublic.changePublicOpinion(Constants.VIEW_LIBERALCRIMESQUADPOS, (segmentPower - 50) / 10);
                if (view != Constants.VIEW_LIBERALCRIMESQUAD)
                    MasterController.generalPublic.changePublicOpinion(view, (segmentPower - 50) / 5, 1);
                else
                    MasterController.generalPublic.changePublicOpinion(view, segmentPower / 10);

                foreach (Entity e in squad)
                {
                    if (e.getComponent<Liberal>().hauledUnit != null &&
                        e.getComponent<Liberal>().hauledUnit.getComponent<Body>().Alive &&
                        e.getComponent<Liberal>().hauledUnit.def == "NEWSANCHOR")
                    {
                        view = GameData.getData().viewList.Keys.ToArray()[mc.LCSRandom(GameData.getData().viewList.Count)];

                        resultText += "\nA hostage is forced on and ";
                        resultText += GameData.getData().viewList[view].broadcastText;

                        int uSegmentPower = 10;
                        CreatureBase ubase = e.getComponent<Liberal>().hauledUnit.getComponent<CreatureBase>();

                        uSegmentPower += ubase.getAttributeValue(Constants.ATTRIBUTE_INTELLIGENCE);
                        uSegmentPower += ubase.getAttributeValue(Constants.ATTRIBUTE_HEART);
                        uSegmentPower += ubase.getAttributeValue(Constants.ATTRIBUTE_CHARISMA);
                        uSegmentPower += ubase.getSkillValue(Constants.SKILL_PERSUASION);

                        if (view != Constants.VIEW_LIBERALCRIMESQUAD)
                            MasterController.generalPublic.changePublicOpinion(view, (uSegmentPower - 10) / 2);
                        else
                            MasterController.generalPublic.changePublicOpinion(view, uSegmentPower / 2, 1);

                        segmentPower += uSegmentPower;
                    }
                }

                if (siteAlienate != 0 && segmentPower >= 40)
                {
                    siteAlienate = 0;
                    resultText += "\nThe moderates at the station appreciated the show. They no longer feel alienated.";
                }

                if (segmentPower < 85 && segmentPower >=25)
                {
                    resultText += "\nSecurity is waiting for the Squad after the show!";

                    sceneRoot.Add(() =>
                    {
                        int count = mc.LCSRandom(8) + 2;
                        for (int i = 0; i < count; i++)
                            encounterEntities.Add(Factories.CreatureFactory.create("SECURITYGUARD"));

                        if (!inEncounter)
                        {
                            inEncounter = true;
                            MasterController.GetMC().uiController.siteMode.startEncounter();
                            MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                        }
                        mc.doNextAction();
                    }, "securitySpawn");
                }
                else
                {
                    resultText += "The show was so " + (segmentPower<50?"hilarious":"entertaining") +" that security watched it at their desks. The Squad might yet escape.";
                }

                alarmTriggered = true;

                mc.addCombatMessage(resultText, true);

                getSquadTile().getComponent<TileSpecial>().used = true;
                sceneRoot.Add(() => endRound(), "End Round");
                mc.doNextAction();
            }));
            options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

            string popupText = "";
            if (alarmTriggered) popupText = "The Cable News broadcasters left the equipment on in their rush to get out. Take over the studio?";
            else popupText = "You've found a Cable News broadcasting studio. Start an impromptu news program?";

            mc.uiController.showYesNoPopup(popupText, options);
        }

        private void specialCorporateFiles()
        {
            MasterController mc = MasterController.GetMC();
            List<UI.PopupOption> options = new List<UI.PopupOption>();
            processingRound = true;
            options.Add(new UI.PopupOption("Yes", () =>
            {
                startRound();

                if (tryUnlock(Difficulty.HEROIC))
                {
                    mc.addCombatMessage("The Squad has found some very interesting files.");
                    squad.inventory.Add(Factories.ItemFactory.create("LOOT_CORPFILES"));
                    squad.inventory.Add(Factories.ItemFactory.create("LOOT_CORPFILES"));

                    foreach(Entity e in squad)
                    {
                        e.getComponent<CreatureBase>().juiceMe(50, 1000);
                        e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_THEFT);
                    }

                    siteCrime += 40;
                    int time = 20 + mc.LCSRandom(10);
                    if (suspicionTimer > time || suspicionTimer == -1) suspicionTimer = time;
                }

                siteCrime += 3;
                MasterController.news.currentStory.addCrime("CORP_FILES");

                getSquadTile().getComponent<TileSpecial>().used = true;
                sceneRoot.Add(() => { noticeCheck(false); }, "noticeCheck");
                sceneRoot.Add(disguiseCheck, "disguiseCheck");
                sceneRoot.Add(() => endRound(), "End Round");
                mc.doNextAction();
            }));
            options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

            mc.uiController.showYesNoPopup("You've found a safe. Open it?", options);
        }

        private void specialJuryRoom()
        {
            MasterController mc = MasterController.GetMC();

            if (alarmTriggered)
            {
                mc.addCombatMessage("It appears as if this room has been vacated in a hurry.");
                getSquadTile().getComponent<TileSpecial>().used = true;
            }
            else
            {
                List<UI.PopupOption> options = new List<UI.PopupOption>();
                processingRound = true;
                options.Add(new UI.PopupOption("Yes", () =>
                {
                    startRound();

                    Entity bestAttacker = 
                        squad.getBestAtCombination(
                            new string[] { Constants.ATTRIBUTE_INTELLIGENCE, Constants.ATTRIBUTE_CHARISMA }, 
                            new string[] { Constants.SKILL_PERSUASION, Constants.SKILL_LAW });                    

                    bestAttacker.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].addExperience(20);
                    bestAttacker.getComponent<CreatureBase>().Skills[Constants.SKILL_LAW].addExperience(20);

                    bool success = false;
                    if (bestAttacker.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].check(Difficulty.HARD) &&
                        bestAttacker.getComponent<CreatureBase>().Skills[Constants.SKILL_LAW].check(Difficulty.CHALLENGING))
                        success = true;

                    if (success)
                    {
                        string outputText = bestAttacker.getComponent<CreatureInfo>().getName();
                        outputText += "  works the room like in Twelve Angry Men, and the jury concludes that ";
                        switch (mc.LCSRandom(16))
                        {
                            case 0: outputText += "murder"; break;
                            case 1: outputText += "assault"; break;
                            case 2: outputText += "theft"; break;
                            case 3: outputText += "mugging"; break;
                            case 4: outputText += "burglary"; break;
                            case 5: outputText += "property destruction"; break;
                            case 6: outputText += "vandalism"; break;
                            case 7: outputText += "libel"; break;
                            case 8: outputText += "slander"; break;
                            case 9: outputText += "sodomy"; break;
                            case 10: outputText += "obstruction of justice"; break;
                            case 11: outputText += "breaking and entering"; break;
                            case 12: outputText += "public indecency"; break;
                            case 13: outputText += "arson"; break;
                            case 14: outputText += "resisting arrest"; break;
                            case 15: outputText += "tax evasion"; break;
                        }

                        outputText += " wasn't really wrong here.";

                        bestAttacker.getComponent<CreatureBase>().juiceMe(20, 200);
                        mc.addCombatMessage(outputText);
                    }
                    else
                    {
                        mc.addCombatMessage(bestAttacker.getComponent<CreatureInfo>().getName() + " wasn't quite convincing...");

                        for (int i = 0; i < 12; i++)
                        {
                            encounterEntities.Add(Factories.CreatureFactory.create("JUROR"));
                        }

                        if (!inEncounter)
                        {
                            inEncounter = true;
                            MasterController.GetMC().uiController.siteMode.startEncounter();
                            MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                        }

                        alarmTriggered = true;
                        siteCrime += 10;
                        siteAlienate = 2;
                        MasterController.news.currentStory.addCrime("JURYTAMPERING");
                        foreach(Entity e in squad)
                        {
                            e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_JURY);
                        }
                    }

                    getSquadTile().getComponent<TileSpecial>().used = true;
                    sceneRoot.Add(() => { noticeCheck(false); }, "noticeCheck");
                    sceneRoot.Add(disguiseCheck, "disguiseCheck");
                    sceneRoot.Add(() => endRound(), "End Round");
                    mc.doNextAction();
                }));
                options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

                mc.uiController.showYesNoPopup("You've found a Jury in deliberations! Attempt to influence them?", options);
            }
        }

        enum JailType
        {
            POLICE_LOCKUP,
            COURTHOUSE_LOCKUP,
            PRISON_LOW,
            PRISON_MEDIUM,
            PRISON_HIGH
        }

        private void specialFreeLiberals(JailType jailtype)
        {
            MasterController mc = MasterController.GetMC();

            int freeslots = 6 - squad.Count;
            int hostslots = 0;
            foreach(Entity e in squad)
            {
                if (e.getComponent<Liberal>().hauledUnit == null)
                    hostslots++;
            }

            List<Entity> waitingForRescue = new List<Entity>();
            foreach(Entity e in MasterController.lcs.getAllMembers())
            {
                switch (jailtype)
                {
                    case JailType.POLICE_LOCKUP:
                        if (e.getComponent<Liberal>().status == Liberal.Status.JAIL_POLICE_CUSTODY && !e.getComponent<CriminalRecord>().hospitalArrest)
                            waitingForRescue.Add(e);
                        break;
                    case JailType.COURTHOUSE_LOCKUP:
                        if (e.getComponent<Liberal>().status == Liberal.Status.JAIL_COURT)
                            waitingForRescue.Add(e);
                        break;
                    case JailType.PRISON_LOW:
                        if (e.getComponent<Liberal>().status == Liberal.Status.JAIL_PRISON &&
                            e.getComponent<CriminalRecord>().LifeSentences < 1 &&
                            !e.getComponent<CriminalRecord>().deathPenalty)
                            waitingForRescue.Add(e);
                        break;
                    case JailType.PRISON_MEDIUM:
                        if (e.getComponent<Liberal>().status == Liberal.Status.JAIL_PRISON &&
                            e.getComponent<CriminalRecord>().LifeSentences >= 1 &&
                            !e.getComponent<CriminalRecord>().deathPenalty)
                            waitingForRescue.Add(e);
                        break;
                    case JailType.PRISON_HIGH:
                        if (e.getComponent<Liberal>().status == Liberal.Status.JAIL_PRISON &&
                            e.getComponent<CriminalRecord>().deathPenalty)
                            waitingForRescue.Add(e);
                        break;
                }
            }

            if (waitingForRescue.Count == 0) return;

            foreach(Entity e in waitingForRescue)
            {
                if(freeslots > 0 && mc.LCSRandom(2) == 0)
                {
                    squad.Add(e);
                    e.getComponent<Liberal>().homeBase = squad.homeBase;
                    e.getComponent<Liberal>().status = Liberal.Status.ACTIVE;
                    e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_ESCAPE);
                    //If the squad came in a vehicle, assign it to the newly freed prisoner
                    e.getComponent<Inventory>().tempVehicle = squad[0].getComponent<Inventory>().tempVehicle;
                    e.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.JUST_ESCAPED;
                    freeslots--;
                    hostslots++;

                    mc.addCombatMessage("You've rescued " + e.getComponent<CreatureInfo>().getName() + " from the Conservative Machine.");
                }
                else if(hostslots > 0)
                {
                    Entity hauler = null;

                    foreach (Entity lib in squad)
                    {
                        if (lib.getComponent<Liberal>().hauledUnit == null)
                        {
                            hauler = lib;
                            break;
                        }
                    }

                    if(hauler == null)
                    {
                        //Something's gone wrong, hostslots should be 0
                        hostslots = 0;
                        mc.addCombatMessage("There's nobody who can carry " + e.getComponent<CreatureInfo>().getName() + ". You'll have to come back later.");
                        continue;
                    }

                    hauler.getComponent<Liberal>().hauledUnit = e;
                    hostslots--;
                    e.getComponent<Liberal>().homeBase = squad.homeBase;
                    e.getComponent<Liberal>().status = Liberal.Status.ACTIVE;
                    e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_ESCAPE);
                    mc.addCombatMessage("You've rescued " + e.getComponent<CreatureInfo>().getName() + " from the Conservative Machine.");
                    string responseString = e.getComponent<CreatureInfo>().getName() + " ";
                    switch (mc.LCSRandom(1 + 
                        (MasterController.government.laws[Constants.LAW_POLICE].alignment < Alignment.ELITE_LIBERAL?1:0) + 
                        (MasterController.government.laws[Constants.LAW_TORTURE].alignment < Alignment.ELITE_LIBERAL?1:0)))
                    {
                        case 0:
                            responseString += "was on a hunger strike ";
                            break;                        
                        case 1:
                            responseString += "was beaten severly yesterday ";
                            break;
                        case 2:
                            responseString += "was tortured recently ";
                            break;
                    }

                    responseString += "so " + hauler.getComponent<CreatureInfo>().getName() + " will have to haul a Liberal.";
                    mc.addCombatMessage(responseString);
                }
                else
                {
                    mc.addCombatMessage("There's nobody who can carry " + e.getComponent<CreatureInfo>().getName() + ". You'll have to come back later.");
                }
            }
        }

        private void specialPrisonControl(int level)
        {
            List<UI.PopupOption> options = new List<UI.PopupOption>();
            processingRound = true;
            MasterController mc = MasterController.GetMC();
            options.Add(new UI.PopupOption("Yes", () =>
            {
                startRound();
                sceneRoot.Add(() =>
                {
                    int time = 20 + mc.LCSRandom(10);
                    if (suspicionTimer > time || suspicionTimer == -1) suspicionTimer = time;
                    siteCrime += 30;
                    foreach (Entity e in squad)
                    {
                        e.getComponent<CreatureBase>().juiceMe(50, 1000);
                        e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_HELP_ESCAPE);
                    }
                    MasterController.news.currentStory.addCrime("PRISON_RELEASE");

                    int prisonerCount = mc.LCSRandom(8) + 2;
                    Alignment deathPenalty = MasterController.government.laws[Constants.LAW_DEATH_PENALTY].alignment;

                    //Low security (Normal sentences)
                    if (level == 0)
                    {
                        if(deathPenalty == Alignment.CONSERVATIVE)
                            prisonerCount = mc.LCSRandom(6) + 2;
                        else if(deathPenalty == Alignment.ARCHCONSERVATIVE)
                            prisonerCount = mc.LCSRandom(3) + 1;
                    }
                    //Medium security (Life sentences)
                    else if(level == 1)
                    {
                        if (deathPenalty == Alignment.LIBERAL)
                            prisonerCount = mc.LCSRandom(6) + 1;
                        else if (deathPenalty == Alignment.ELITE_LIBERAL)
                            prisonerCount = mc.LCSRandom(4) + 1;
                    }
                    //High security (Death row)
                    else if(level == 2)
                    {
                        if (deathPenalty == Alignment.ELITE_LIBERAL)
                            prisonerCount = 0;
                        else if (deathPenalty == Alignment.LIBERAL)
                            prisonerCount = mc.LCSRandom(4);
                        else if (deathPenalty == Alignment.CONSERVATIVE)
                            prisonerCount += mc.LCSRandom(4);
                        else if (deathPenalty == Alignment.ARCHCONSERVATIVE)
                            prisonerCount += mc.LCSRandom(4) + 2;
                    }

                    for (int i = 0; i < prisonerCount; i++)
                    {
                        encounterEntities.Add(Factories.CreatureFactory.create("PRISONER"));
                    }

                    if (!inEncounter)
                    {
                        inEncounter = true;
                        MasterController.GetMC().uiController.siteMode.startEncounter();
                        MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                    }

                    switch (level)
                    {
                        case 0:
                            specialFreeLiberals(JailType.PRISON_LOW);
                            break;
                        case 1:
                            specialFreeLiberals(JailType.PRISON_MEDIUM);
                            break;
                        case 2:
                            specialFreeLiberals(JailType.PRISON_HIGH);
                            break;
                    }

                    getSquadTile().getComponent<TileSpecial>().used = true;
                    mc.doNextAction();
                }, "ReleasePrisoners");
                sceneRoot.Add(() => { noticeCheck(true); }, "noticeCheck");
                sceneRoot.Add(disguiseCheck, "disguiseCheck");
                sceneRoot.Add(() => endRound(), "End Round");
                mc.doNextAction();
            }));
            options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

            string popupText = "You've found the ";
            switch (level)
            {
                case 0:
                    popupText += "low security ";
                    break;
                case 1:
                    popupText += "medium security ";
                    break;
                case 2:
                    popupText += "high security ";
                    break;
            }

            popupText += "prison control room. Free the prisoners?";
            mc.uiController.showYesNoPopup(popupText, options);
        }

        private void specialLockup(bool policeStation)
        {
            List<UI.PopupOption> options = new List<UI.PopupOption>();
            processingRound = true;
            MasterController mc = MasterController.GetMC();
            options.Add(new UI.PopupOption("Yes", () =>
            {
                startRound();
                sceneRoot.Add(() =>
                {
                    if (tryUnlock(Difficulty.FORMIDABLE))
                    {
                        int time = 20 + mc.LCSRandom(10);
                        if (suspicionTimer > time || suspicionTimer == -1) suspicionTimer = time;
                        siteCrime += 20;
                        foreach (Entity e in squad)
                        {
                            e.getComponent<CreatureBase>().juiceMe(50, 1000);
                            e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_HELP_ESCAPE);
                        }
                        if(policeStation)
                            MasterController.news.currentStory.addCrime("POLICE_LOCKUP");
                        else
                            MasterController.news.currentStory.addCrime("COURTHOUSE_LOCKUP");

                        int prisonerCount = mc.LCSRandom(8) + 2;
                        for(int i = 0; i < prisonerCount; i++)
                        {
                            encounterEntities.Add(Factories.CreatureFactory.create("PRISONER"));
                        }

                        if (!inEncounter)
                        {
                            inEncounter = true;
                            MasterController.GetMC().uiController.siteMode.startEncounter();
                            MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                        }

                        if (policeStation)
                            specialFreeLiberals(JailType.POLICE_LOCKUP);
                        else
                            specialFreeLiberals(JailType.COURTHOUSE_LOCKUP);
                    }
                    getSquadTile().getComponent<TileSpecial>().used = true;
                    mc.doNextAction();
                }, "OpenCell");
                sceneRoot.Add(() => { noticeCheck(false, Difficulty.HARD); }, "noticeCheck");
                sceneRoot.Add(disguiseCheck, "disguiseCheck");
                sceneRoot.Add(() => endRound(), "End Round");
                mc.doNextAction();
            }));
            options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

            string popupText = "";
            if (policeStation)
                popupText = "You see prisoners in the detention room. Free them?";
            else
                popupText = "You see prisoners in the Courthouse jail. Free them?";

            mc.uiController.showYesNoPopup(popupText, options);
        }

        private void specialSuperComputer()
        {
            MasterController mc = MasterController.GetMC();
            if (alarmTriggered)
            {
                mc.addCombatMessage("The security alert has caused the computer to shut down.");
                getSquadTile().getComponent<TileSpecial>().used = true;
            }
            else
            {
                List<UI.PopupOption> options = new List<UI.PopupOption>();
                processingRound = true;
                options.Add(new UI.PopupOption("Yes", () =>
                {
                    startRound();
                    Entity bestHacker = null;
                    Entity bestBlindHacker = null;
                    int bestHackRoll = 0;
                    int bestBlindRoll = -3;
                    foreach (Entity e in squad)
                    {
                        int roll = e.getComponent<CreatureBase>().Skills[Constants.SKILL_COMPUTERS].roll();
                        if (!e.getComponent<Body>().canSee())
                        {
                            roll -= 3;
                            if(roll > bestBlindRoll)
                            {
                                bestBlindRoll = roll;
                                bestBlindHacker = e;
                            }
                        }
                        else
                        {
                            if(roll > bestHackRoll)
                            {
                                bestHackRoll = roll;
                                bestHacker = e;
                            }
                        }
                    }

                    bool blind = false;
                    if (bestBlindRoll > bestHackRoll) blind = true;
                    else if (bestHacker == null && bestBlindHacker != null) blind = true;

                    if (blind)
                    {
                        bestHacker = bestBlindHacker;
                        bestHackRoll = bestBlindRoll;
                    }

                    if (bestHacker != null && bestHackRoll > 0)
                    {
                        bestHacker.getComponent<CreatureBase>().Skills[Constants.SKILL_COMPUTERS].addExperience(15);
                        if(bestHackRoll > (int) Difficulty.HEROIC)
                        {
                            string responseText = bestHacker.getComponent<CreatureInfo>().getName();
                            if (!blind) responseText += " has";
                            responseText += " burned a disk of top secret files";
                            if(MasterController.ccs.exposure < ConservativeCrimeSquad.Exposure.GOT_DATA &&
                                MasterController.ccs.status > ConservativeCrimeSquad.Status.INACTIVE)
                            {
                                responseText += ", including a list of government backers of the CCS";
                                squad.inventory.Add(Factories.ItemFactory.create("LOOT_CCS_BACKERLIST"));
                                MasterController.ccs.exposure = ConservativeCrimeSquad.Exposure.GOT_DATA;
                            }
                            if (blind) responseText += " despite being blind";
                            responseText += "!";
                            sceneRoot.Add(() => { mc.addCombatMessage(responseText); }, "hackResponse");

                            foreach (Entity e in squad)
                            {
                                e.getComponent<CreatureBase>().juiceMe(50, 1000);
                            }
                            squad.inventory.Add(Factories.ItemFactory.create("LOOT_INTHQDISK"));
                        }
                        else
                        {
                            string responseText = bestHacker.getComponent<CreatureInfo>().getName();
                            responseText += " couldn't";
                            if (blind) responseText += " see how to";
                            responseText += " bypass the supercomputer security.";
                            sceneRoot.Add(() => { mc.addCombatMessage(responseText); }, "hackResponse");
                        }

                        foreach (Entity e in squad)
                        {
                            e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_TREASON);
                        }
                        int time = 20 + mc.LCSRandom(10);
                        if (suspicionTimer > time || suspicionTimer == -1) suspicionTimer = time;
                        siteCrime += 3;
                        MasterController.news.currentStory.addCrime("HACK_INTEL");
                    }
                    else
                    {
                        string responseText = "You couldn't find anyone to do the job.";
                        if (blind) responseText += " Including the BLIND HACKER you brought";
                        sceneRoot.Add(() => { mc.addCombatMessage(responseText); }, "hackResponse");
                    }

                    getSquadTile().getComponent<TileSpecial>().used = true;
                    sceneRoot.Add(() => { noticeCheck(true, Difficulty.HARD); }, "noticeCheck");
                    sceneRoot.Add(disguiseCheck, "disguiseCheck");
                    sceneRoot.Add(() => endRound(), "End Round");
                    mc.doNextAction();
                }));
                options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

                mc.uiController.showYesNoPopup("You've found the Intelligence Supercomputer. Hack it?", options);
            }
        }

        private void specialNuclear()
        {
            List<UI.PopupOption> options = new List<UI.PopupOption>();
            processingRound = true;
            MasterController mc = MasterController.GetMC();
            options.Add(new UI.PopupOption("Yes", () =>
            {
                startRound();
                Entity successfulPresser = null;
                foreach(Entity e in squad)
                {
                    if (e.getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].check(Difficulty.HARD))
                    {
                        successfulPresser = e;
                        break;
                    }
                }

                if(successfulPresser != null)
                {
                    sceneRoot.Add(() => { mc.addCombatMessage(successfulPresser.getComponent<CreatureInfo>().getName() + " presses the big red button!"); }, "pressButton");
                    sceneRoot.Add(() => { mc.addCombatMessage(". . ."); }, "suspense");
                    sceneRoot.Add(() => { mc.addCombatMessage(". . ."); }, "suspense");
                    sceneRoot.Add(() => { mc.addCombatMessage(". . ."); }, "suspense");

                    if(MasterController.government.laws[Constants.LAW_NUCLEAR_POWER].alignment == Alignment.ELITE_LIBERAL)
                    {
                        sceneRoot.Add(() => 
                        {
                            mc.addCombatMessage("The nuclear waste gets released into the state's water supply!");
                            MasterController.generalPublic.changePublicOpinion(Constants.VIEW_NUCLEAR_POWER, 15, 0, 95);
                            MasterController.generalPublic.changePublicOpinion(Constants.VIEW_LIBERALCRIMESQUADPOS, -50, 0, 0);
                            foreach (Entity e in squad)
                            {
                                e.getComponent<CreatureBase>().juiceMe(40, 1000);
                                e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_TERRORISM);
                            }

                            siteCrime += 25;
                            MasterController.news.currentStory.addCrime("SHUTDOWNREACTOR");
                            getSquadTile().getComponent<TileSpecial>().used = true;
                            alarmTriggered = true;
                        }, "releaseWaste");
                    }
                    else
                    {
                        sceneRoot.Add(() =>
                        {
                            mc.addCombatMessage("A deafening alarm sounds! The reactor is overheating!");
                            MasterController.generalPublic.changePublicOpinion(Constants.VIEW_NUCLEAR_POWER, 15, 0, 95);
                            foreach (Entity e in squad)
                            {
                                e.getComponent<CreatureBase>().juiceMe(100, 1000);
                                e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_TERRORISM);
                            }

                            siteCrime += 50;
                            MasterController.news.currentStory.addCrime("SHUTDOWNREACTOR");
                            getSquadTile().getComponent<TileSpecial>().used = true;
                            alarmTriggered = true;
                        }, "releaseWaste");
                    }
                }
                else
                {
                    sceneRoot.Add(() =>
                    {
                        mc.addCombatMessage("After some failed attempts, and a very loud alarm, the Squad resigns to just leaving a threatening note.");
                        foreach (Entity e in squad)
                        {
                            e.getComponent<CreatureBase>().juiceMe(15, 500);
                            e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_TERRORISM);
                        }

                        siteCrime += 5;

                        getSquadTile().getComponent<TileSpecial>().used = true;
                        alarmTriggered = true;
                        mc.doNextAction();
                    }, "scaryNote");
                }

                sceneRoot.Add(() => { alienateCheck(true); mc.doNextAction(); }, "alienateCheck");
                sceneRoot.Add(disguiseCheck, "disguiseCheck");
                sceneRoot.Add(() => endRound(), "End Round");
                mc.doNextAction();
            }));
            options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

            string prompt = "";
            if (MasterController.government.laws[Constants.LAW_NUCLEAR_POWER].alignment == Alignment.ELITE_LIBERAL)
                prompt = "You see the nuclear waste center control room. Attempt to release nuclear waste?";
            else
                prompt = "You see the nuclear power plant control room. Mess with the reactor settings?";

            mc.uiController.showYesNoPopup(prompt, options);
        }

        private void specialSmash(string prompt)
        {
            List<UI.PopupOption> options = new List<UI.PopupOption>();
            processingRound = true;
            MasterController mc = MasterController.GetMC();
            options.Add(new UI.PopupOption("Yes", () =>
            {
                startRound();                
                sceneRoot.Add(() =>
                {
                    int time = 20 + mc.LCSRandom(10);
                    if (suspicionTimer > time || suspicionTimer == -1) suspicionTimer = time;
                    getSquadTile().getComponent<TileSpecial>().used = true;
                    siteCrime++;
                    foreach (Entity e in squad)
                    {
                        e.getComponent<CreatureBase>().juiceMe(5, 100);
                        e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_VANDALISM);
                    }
                    if (MasterController.news.currentStory != null)
                    {
                        if (location.def == "INDUSTRY_SWEATSHOP")
                            MasterController.news.currentStory.addCrime("BREAK_SWEATSHOP");
                        else if (location.def == "INDUSTRY_POLLUTER")
                            MasterController.news.currentStory.addCrime("BREAK_FACTORY");
                        else
                            MasterController.news.currentStory.addCrime("VANDALISM");
                    }
                    MasterController.GetMC().doNextAction();
                }, "Smash");
                sceneRoot.Add(() => { noticeCheck(false, Difficulty.HEROIC); }, "noticeCheck");
                sceneRoot.Add(disguiseCheck, "disguiseCheck");
                sceneRoot.Add(() => endRound(), "End Round");
                mc.doNextAction();
            }));
            options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

            mc.uiController.showYesNoPopup(prompt + " Smash it?", options);
        }

        private void specialCagedAnimalsCosmetics()
        {
            List<UI.PopupOption> options = new List<UI.PopupOption>();
            processingRound = true;
            MasterController mc = MasterController.GetMC();
            options.Add(new UI.PopupOption("Yes", () =>
            {
                startRound();
                sceneRoot.Add(() =>
                {
                    if (tryUnlock(Difficulty.VERYEASY))
                    {
                        int time = 20 + mc.LCSRandom(10);
                        if (suspicionTimer > time || suspicionTimer == -1) suspicionTimer = time;                        
                        siteCrime++;
                        foreach (Entity e in squad)
                        {
                            e.getComponent<CreatureBase>().juiceMe(3, 100);
                            e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_VANDALISM);
                        }
                        MasterController.news.currentStory.addCrime("FREE_RABBITS");
                    }
                    getSquadTile().getComponent<TileSpecial>().used = true;
                    MasterController.GetMC().doNextAction();
                }, "OpenCage");
                sceneRoot.Add(() => { noticeCheck(false, Difficulty.HEROIC); }, "noticeCheck");                
                sceneRoot.Add(disguiseCheck, "disguiseCheck");
                sceneRoot.Add(() => endRound(), "End Round");
                mc.doNextAction();
            }));
            options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

            mc.uiController.showYesNoPopup("You see fluffy white rabbits in a locked cage. Free them?", options);
        }

        private void specialCagedAnimalsGenetics()
        {
            List<UI.PopupOption> options = new List<UI.PopupOption>();
            processingRound = true;
            MasterController mc = MasterController.GetMC();
            options.Add(new UI.PopupOption("Yes", () =>
            {
                bool success = false;
                startRound();
                sceneRoot.Add(() =>
                {
                    if (tryUnlock(Difficulty.AVERAGE))
                    {
                        success = true;
                        int time = 20 + mc.LCSRandom(10);
                        if (suspicionTimer > time || suspicionTimer == -1) suspicionTimer = time;
                        siteCrime++;
                        foreach (Entity e in squad)
                        {
                            e.getComponent<CreatureBase>().juiceMe(5, 200);
                            e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_VANDALISM);
                        }
                        MasterController.news.currentStory.addCrime("FREE_BEASTS");                        
                    }
                    getSquadTile().getComponent<TileSpecial>().used = true;
                    MasterController.GetMC().doNextAction();
                }, "OpenCage");
                sceneRoot.Add(() => { noticeCheck(false, Difficulty.HEROIC); }, "noticeCheck");
                sceneRoot.Add(disguiseCheck, "disguiseCheck");
                sceneRoot.Add(() =>
                {
                    if (success)
                    {
                        if (mc.LCSRandom(2) == 0)
                        {
                            mc.addCombatMessage("Uh, maybe that idea was Conservative in retrospect...");
                            int spawnCount = mc.LCSRandom(6) + 1;
                            for (int i = 0; i < spawnCount; i++)
                            {
                                encounterEntities.Add(Factories.CreatureFactory.create("GENETIC"));
                            }

                            if (!inEncounter)
                            {
                                inEncounter = true;
                                MasterController.GetMC().uiController.siteMode.startEncounter();
                                MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                            }
                        }
                    }

                    mc.doNextAction();
                }, "spawnMonsters");
                sceneRoot.Add(() => endRound(), "End Round");
                mc.doNextAction();
            }));
            options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

            mc.uiController.showYesNoPopup("You see horrible misshapen creatures in a sealed cage. Free them?", options);
        }

        private void specialArmory()
        {
            List<UI.PopupOption> options = new List<UI.PopupOption>();
            processingRound = true;
            MasterController mc = MasterController.GetMC();
            options.Add(new UI.PopupOption("Yes", () =>
            {
                startRound();
                alarmTriggered = true;
                mc.addCombatMessage("Alarms go off!", true);
                sceneRoot.Add(() => { noticeCheck(false); }, "noticeCheck");
                sceneRoot.Add(disguiseCheck, "disguiseCheck");
                sceneRoot.Add(() =>
                {
                    bool empty = true;

                    if(mc.LCSRandom(5) == 0)
                    {
                        empty = false;
                        mc.addCombatMessage("Jackpot! The squad found a M249 Machine Gun!");

                        Entity m249 = Factories.ItemFactory.create("WEAPON_M249_MACHINEGUN");
                        m249.getComponent<Weapon>().clip = Factories.ItemFactory.create(m249.getComponent<Weapon>().getDefaultClip().type);

                        squad.inventory.Add(m249);
                        for (int i = 0; i < 9; i++)
                            squad.inventory.Add(Factories.ItemFactory.create(m249.getComponent<Weapon>().getDefaultClip().type));
                    }

                    if(mc.LCSRandom(2) == 0)
                    {
                        empty = false;
                        mc.addCombatMessage("The squad finds some M16 Assault Rifles.");

                        int num = 0;
                        do
                        {
                            Entity rifle = Factories.ItemFactory.create("WEAPON_AUTORIFLE_M16");
                            rifle.getComponent<Weapon>().clip = Factories.ItemFactory.create(rifle.getComponent<Weapon>().getDefaultClip().type);

                            for(int i = 0; i< 5; i++)
                                squad.inventory.Add(Factories.ItemFactory.create(rifle.getComponent<Weapon>().getDefaultClip().type));
                            squad.inventory.Add(rifle);

                            num++;
                        } while (num < 2 || (num < 5 && mc.LCSRandom(2) == 0));
                    }

                    if(mc.LCSRandom(2) == 0)
                    {
                        empty = false;
                        mc.addCombatMessage("The squad finds some M4 Carbines.");

                        int num = 0;
                        do
                        {
                            Entity rifle = Factories.ItemFactory.create("WEAPON_CARBINE_M4");
                            rifle.getComponent<Weapon>().clip = Factories.ItemFactory.create(rifle.getComponent<Weapon>().getDefaultClip().type);

                            for (int i = 0; i < 5; i++)
                                squad.inventory.Add(Factories.ItemFactory.create(rifle.getComponent<Weapon>().getDefaultClip().type));
                            squad.inventory.Add(rifle);

                            num++;
                        } while (num < 2 || (num < 5 && mc.LCSRandom(2) == 0));
                    }

                    if(mc.LCSRandom(2) == 0)
                    {
                        empty = false;
                        mc.addCombatMessage("The squad finds some body armor.");

                        int num = 0;
                        do
                        {
                            squad.inventory.Add(Factories.ItemFactory.create("ARMOR_ARMYARMOR"));

                            num++;
                        } while (num < 2 || (num < 5 && mc.LCSRandom(2) == 0));
                    }

                    if (!empty)
                    {
                        mc.addCombatMessage("Guards are everywhere!");
                        siteCrime += 40;
                        foreach (Entity e in squad)
                        {
                            e.getComponent<CreatureBase>().juiceMe(50, 1000);
                            e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_TREASON);
                            e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_THEFT);
                        }
                        MasterController.news.currentStory.addCrime("ARMORY");

                        int spawnCount = mc.LCSRandom(4) + 2;
                        for (int i = 0; i < spawnCount; i++)
                        {
                            encounterEntities.Add(Factories.CreatureFactory.create("SOLDIER"));
                        }

                        if (!inEncounter)
                        {
                            inEncounter = true;
                            MasterController.GetMC().uiController.siteMode.startEncounter();
                            MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                        }
                    }
                    else
                    {
                        mc.addCombatMessage("It's a trap! The armory is empty!");
                        foreach(Entity e in squad)
                        {
                            e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_TREASON);
                        }

                        int spawnCount = mc.LCSRandom(8) + 2;
                        for (int i = 0; i < spawnCount; i++)
                        {
                            encounterEntities.Add(Factories.CreatureFactory.create("SOLDIER"));
                        }

                        if (!inEncounter)
                        {
                            inEncounter = true;
                            MasterController.GetMC().uiController.siteMode.startEncounter();
                            MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                        }
                    }

                    getSquadTile().getComponent<TileSpecial>().used = true;
                    mc.doNextAction();
                }, "OpenArmory");
                sceneRoot.Add(() => endRound(), "End Round");
                mc.doNextAction();
            }));
            options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

            mc.uiController.showYesNoPopup("You've found the armory. Break in?", options);
        }

        private bool tryUnlock(Difficulty difficulty)
        {
            Entity opener = squad.getBestAtSkill("SECURITY");

            if (opener.getComponent<CreatureBase>().Skills["SECURITY"].check(difficulty))
            {
                MasterController.GetMC().addCombatMessage(opener.getComponent<CreatureInfo>().getName() + " opens the lock!");
                opener.getComponent<CreatureBase>().Skills["SECURITY"].addExperience(1 + (int)difficulty - opener.getComponent<CreatureBase>().Skills["SECURITY"].level);
                foreach (Entity e in squad)
                {
                    if (e == opener) continue;

                    e.getComponent<CreatureBase>().Skills["SECURITY"].addExperience((int)difficulty - e.getComponent<CreatureBase>().Skills["SECURITY"].level);
                }

                return true;
            }
            else
            {
                bool gainedExp = false;

                for (int i = 0; i < 3; i++)
                {
                    if (opener.getComponent<CreatureBase>().Skills["SECURITY"].check(difficulty))
                    {
                        opener.getComponent<CreatureBase>().Skills["SECURITY"].addExperience(10);
                        MasterController.GetMC().addCombatMessage(opener.getComponent<CreatureInfo>().getName() + " is close, but can't quite get the lock open.");
                        gainedExp = true;
                        break;
                    }
                }

                if (!gainedExp)
                {
                    MasterController.GetMC().addCombatMessage(opener.getComponent<CreatureInfo>().getName() + " can't figure the lock out.");
                }

                return false;
            }
        }

        public void advanceRound()
        {
            MasterController.GetMC().doNextAction();
        }

        public void setEncounterWarnings(bool toggle)
        {
            encounterWarnings = toggle;
        }

        private void startRound()
        {
            MasterController.GetMC().nextRound();
            processingRound = true;

            if (!inEncounter) encounterTimer = 0;
            else encounterTimer++;
        }

        private void endRound(Action extraAction = null, string extraActionDescription = "")
        {
            if(getSquadTile().getComponent<TileBase>().bloodBlast != TileBase.Bloodstain.NONE)
            {
                bloodyShoes = 4;
            }

            if (alarmTriggered)
            {
                alarmTimer++;
            }
            else if (suspicionTimer > 0)
            {
                suspicionTimer--;
                if (suspicionTimer == 0)
                {
                    MasterController.GetMC().addCombatMessage("The Squad smells Conservative panic!", true);
                }
            }

            if (extraAction == null)
            {
                sceneRoot.Add(() =>
                {
                    bool someoneBleeding = false;

                    foreach (Entity e in squad)
                    {
                        if (e.getComponent<Body>().isBleeding())
                        {
                            someoneBleeding = true;
                            break;
                        }

                        if(e.getComponent<Liberal>().hauledUnit != null)
                        {
                            if (e.getComponent<Liberal>().hauledUnit.getComponent<Body>().isBleeding())
                            {
                                someoneBleeding = true;
                                break;
                            }
                        }
                    }

                    if (!someoneBleeding)
                    {
                        foreach (Entity e in encounterEntities)
                        {
                            if (e == null) continue;
                            if (e.getComponent<Body>().isBleeding())
                            {
                                someoneBleeding = true;
                                break;
                            }
                        }
                    }

                    if (someoneBleeding)
                    {
                        getSquadTile().getComponent<TileBase>().bloodTrail_Standing = true;
                    }

                    MasterController.GetMC().doNextAction();
                }, "Bleed check");
            }
            Fight.endOfRound(squad, encounterEntities, sceneRoot);
            sceneRoot.Add(() => 
            {
                if (siegeBuffer.Count > 0 && squad.Count < 6)
                {
                    while (squad.Count < 6 && siegeBuffer.Count > 0)
                    {
                        Entity newLib = siegeBuffer[0];
                        squad.Add(newLib);
                        siegeBuffer.Remove(newLib);
                        MasterController.GetMC().addCombatMessage(newLib.getComponent<CreatureInfo>().getName() + " joins the fight");
                    }
                }
                else
                {
                    MasterController.GetMC().doNextAction();
                }
            }, "siege buffer check");
            sceneRoot.Add(() => { location.getComponent<TroubleSpot>().advanceFires(); MasterController.GetMC().doNextAction(); }, "advanceFires");
            sceneRoot.Add(() => { if (encounterEntities.Count(i => i != null) == 0) leaveEncounter(); MasterController.GetMC().doNextAction(); }, "End Encounter Check");
            sceneRoot.Add(() => { endCheck(extraAction != null); processingRound = false; }, "end check");
            if (extraAction != null) sceneRoot.Add(extraAction, "endRound->" + extraActionDescription);
            MasterController.GetMC().doNextAction();
        }

        private void endCheck(bool doNextAction)
        {
            MasterController mc = MasterController.GetMC();

            if (doNextAction) mc.doNextAction();

            if (squad == null || squad.Count == 0)
            {
                if (!location.hasComponent<SafeHouse>() || !location.getComponent<SafeHouse>().underSiege)
                {
                    MasterController.news.currentStory.type = "SQUAD_KILLED_SITE";
                }
                else
                {
                    if(location.getComponent<SafeHouse>().siegeType == LocationDef.EnemyType.POLICE)
                        MasterController.news.currentStory.type = "SQUAD_KILLED_SIEGEATTACK";
                }

                List<UI.PopupOption> ok = new List<UI.PopupOption>();
                if (!location.hasComponent<SafeHouse>() || !location.getComponent<SafeHouse>().underSiege)
                    ok.Add(new UI.PopupOption("Reflect on your Conservative ineptitude...", finishTrouble));
                else
                {
                    ok.Add(new UI.PopupOption("Reflect on your Conservative ineptitude...", finishSiege));
                    location.getComponent<SafeHouse>().raidUnoccupiedSafehouse(location.getComponent<SafeHouse>().siegeType);
                }
                mc.uiController.showOptionPopup("The Entire Squad has been eliminated.", ok);
            }

            if (!location.hasComponent<SafeHouse>() || !location.getComponent<SafeHouse>().underSiege)
            {
                if (getSquadTile().getComponent<TileFloor>().type == TileFloor.Type.EXIT || forceFinish)
                {
                    int chaseLevel = siteCrime;
                    if (!alarmTriggered) chaseLevel = 0;
                    if (mc.LCSRandom(3) != 0 && chaseLevel < 4) chaseLevel = 0;
                    if (mc.LCSRandom(2) != 0 && chaseLevel < 8) chaseLevel = 0;
                    if (alarmTimer < 10 + mc.LCSRandom(20)) chaseLevel = 0;
                    else if (alarmTimer < 20 + mc.LCSRandom(20) && mc.LCSRandom(3) != 0) chaseLevel = 0;
                    else if (alarmTimer < 40 + mc.LCSRandom(20) && mc.LCSRandom(3) == 0) chaseLevel = 0;

                    bool guilty = false;
                    foreach (Entity e in squad)
                    {
                        if (e.getComponent<CriminalRecord>().isCriminal())
                        {
                            guilty = true;
                            break;
                        }
                    }

                    if (!guilty || forceFinish) chaseLevel = 0;

                    if (mc.forceChase) chaseLevel = 10;
                    if (chaseLevel > 0)
                    {
                        if (squad[0].getComponent<Inventory>().tempVehicle != null)
                        {
                            sceneRoot.Add(() =>
                            {
                                MasterController.news.currentStory.addCrime("CARCHASE");
                                mc.currentChaseScene = new ChaseScene();
                                mc.currentChaseScene.startCarChase(chaseLevel, location.getComponent<TroubleSpot>().getResponseType(), "", squad, null, sceneRoot);
                                sceneRoot.Add(finishTrouble, "finish causing trouble");
                                mc.doNextAction();
                            }, "start new chase: post-trouble chase");
                        }
                        else
                        {
                            sceneRoot.Add(() =>
                            {
                                MasterController.news.currentStory.addCrime("FOOTCHASE");
                                mc.currentChaseScene = new ChaseScene();
                                mc.currentChaseScene.startFootChase(chaseLevel, location.getComponent<TroubleSpot>().getResponseType(), "", squad, null, sceneRoot);
                                sceneRoot.Add(finishTrouble, "finish causing trouble");
                                mc.doNextAction();
                            }, "start new chase: post-trouble chase");
                        }
                        MasterController.GetMC().doNextAction();
                    }
                    else
                    {
                        finishTrouble();
                    }
                }
            }
            else
            {
                if (getSquadTile().getComponent<TileFloor>().type == TileFloor.Type.EXIT || forceFinish)
                {
                    sceneRoot.Add(() =>
                    {
                        mc.currentChaseScene = new ChaseScene();
                        mc.currentChaseScene.sallyForth(squad, location, sceneRoot);
                        mc.someoneDied -= doSomeoneDied;
                        mc.doNextAction();
                    }, "start new chase: post-siege chase");
                    mc.doNextAction();
                }
                else if(siegeKills >= 10 && enemies.Count(x=> { return x.type == MapEnemy.EnemyType.HEAVY; }) == 0 && !inEncounter)
                {
                    if (location.getComponent<SafeHouse>().siegeType == LocationDef.EnemyType.POLICE)
                        MasterController.news.currentStory.type = "SQUAD_DEFENDED";

                    mc.addMessage("The Conservatives have shrunk back under the power of your Liberal Convictions!", true);
                    if (location.getComponent<SafeHouse>().siegeType == LocationDef.EnemyType.POLICE)
                        mc.addMessage("The Conservative automatons have been driven back for the time being. While they are regrouping, you might consider abandoning this safe house for a safer location.", true);
                    else
                        mc.addMessage("The Conservative automatons have been driven back. Unfortunately, you will never truly be safe from this filth until the Liberal Agenda is realized.", true);
                    location.getComponent<SafeHouse>().underSiege = false;
                    location.getComponent<SafeHouse>().underAttack = false;
                    if (location.getComponent<SafeHouse>().siegeType == LocationDef.EnemyType.POLICE)
                    {
                        location.getComponent<SafeHouse>().escalation++;
                        location.getComponent<SafeHouse>().timeUntilLocated = 4 + mc.LCSRandom(4);
                    }
                    finishSiege();
                }
            }
        }

        private void disguiseCheck()
        {
            MasterController mc = MasterController.GetMC();
            mc.addCombatMessage("##DEBUG## entering disguiseCheck");

            string[] blew_stealth_check =
            {
                " coughs.",
                " accidentally mumbles the slogan.",
                " paces uneasily.",
                " stares at the Conservatives.",
                " laughs nervously."
            };

            if (!inEncounter)
            {
                mc.doNextAction();
                return;
            }

            bool forcecheck = false;
            int weapon = 2;
            int timer = encounterTimer - 1;

            if ((location.getComponent<TroubleSpot>().getFlags() & LocationDef.TroubleSpotFlag.RESTRICTED) != 0) forcecheck = true;

            foreach(Entity e in squad)
            {
                if (e == null) continue;
                if (e.getComponent<Inventory>().getDisguiseLevel() < 0) forcecheck = true;
                if (e.getComponent<Inventory>().checkWeaponDisguise() < 2) forcecheck = true;
                if (e.getComponent<Inventory>().checkWeaponDisguise() < weapon) weapon = e.getComponent<Inventory>().checkWeaponDisguise();
            }

            if (suspicionTimer == -1 && !alarmTriggered && !getSquadTile().getComponent<TileBase>().restricted && !forcecheck)
            {
                mc.doNextAction();
                return;
            }

            List<Entity> noticer = new List<Entity>();
            foreach (Entity e in encounterEntities)
            {
                if (e == null) continue;
                if (e.getComponent<CreatureInfo>().encounterName == "Prisoner") continue;

                if (e.getComponent<Body>().Alive && (e.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE))
                    noticer.Add(e);
            }

            bool spotted = false;
            bool noticed = false;
            Entity blewit = null;
            Entity n = null;

            if (noticer.Count > 0)
            {
                do
                {
                    n = mc.pickRandom(noticer);
                    noticer.Remove(n);

                    if (n.getComponent<CreatureInfo>().inCombat)
                    {
                        spotted = true;
                        noticed = true;
                        break;
                    }

                    Difficulty stealth_difficulty = GameData.getData().creatureDefList[n.def].stealth_difficulty;
                    Difficulty disguise_difficulty = GameData.getData().creatureDefList[n.def].disguise_difficulty;

                    if(suspicionTimer == 0)
                    {
                        stealth_difficulty += 6;
                        disguise_difficulty += 6;
                    }
                    else if(suspicionTimer >= 1)
                    {
                        stealth_difficulty += 3;
                        disguise_difficulty += 3;
                    }

                    stealth_difficulty += (squad.Count - 1) * 3;

                    foreach(Entity e in squad)
                    {
                        if (!spotted)
                        {
                            int result = e.getComponent<CreatureBase>().Skills[Constants.SKILL_STEALTH].roll();
                            result -= timer;
                            if (result < (int) stealth_difficulty)
                                spotted = true;
                        }

                        if (spotted)
                        {
                            if(e.getComponent<Inventory>().checkWeaponDisguise() == 0)
                            {
                                noticed = true;
                                break;
                            }
                            else
                            {
                                int result = e.getComponent<CreatureBase>().Skills[Constants.SKILL_DISGUISE].roll();

                                //Invalid disguises means roll auto-fails
                                if (e.getComponent<Inventory>().getDisguiseLevel() <= 0) result = 0;
                                //Partial disguises are less effective
                                else if (e.getComponent<Inventory>().getDisguiseLevel() == 1) result /= 2;
                                //Having a hostage makes blending in very hard
                                if (e.getComponent<Liberal>().hauledUnit != null) result /= 4;

                                result -= timer;

                                if(result < (int) disguise_difficulty)
                                {
                                    blewit = e;

                                    noticed = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (noticed) break;
                } while (noticer.Count > 0);

                if (!spotted)
                {
                    foreach(Entity e in squad)
                    {
                        e.getComponent<CreatureBase>().Skills["STEALTH"].addExperience(10);
                    }

                    if(timer == 0)
                    {
                        string resultText = "<color=cyan>";
                        if (squad.Count > 1) resultText += "The squad";
                        else resultText += squad[0].getComponent<CreatureInfo>().getName();
                        resultText += " fades into the shadows.</color>";
                        mc.addCombatMessage(resultText);
                    }
                }
                else
                {
                    if(blewit == null)
                    {
                        foreach (Entity e in squad)
                        {
                            e.getComponent<CreatureBase>().Skills[Constants.SKILL_DISGUISE].addExperience(10);
                        }
                    }

                    if(blewit != null && mc.LCSRandom(2) == 0)
                    {
                        string resultText = "<color=yellow>";
                        resultText += blewit.getComponent<CreatureInfo>().getName();
                        resultText += blew_stealth_check[mc.LCSRandom(blew_stealth_check.Length)] + "</color>";
                        mc.addCombatMessage(resultText);
                    }
                    else if (!noticed)
                    {
                        string resultText = "<color=cyan>";
                        if (squad.Count > 1) resultText += "The squad";
                        else resultText += squad[0].getComponent<CreatureInfo>().getName();
                        resultText += " acts natural.</color>";
                        mc.addCombatMessage(resultText);
                    }
                }

                if (!noticed) return;

                string noticedText = "<color=red>" + n.getComponent<CreatureInfo>().encounterName;

                //Automatic alarm for trespassing in residential apartments
                if ((location.getComponent<TroubleSpot>().getFlags() & LocationDef.TroubleSpotFlag.RESIDENTIAL) != 0 &&
                    getSquadTile().getComponent<TileBase>().restricted)
                    suspicionTimer = 0;

                if(suspicionTimer != 0 && weapon > 0 && !alarmTriggered)
                {
                    noticedText += " looks at the Squad suspiciously.";
                    noticedText += "</color>";
                    mc.addCombatMessage(noticedText, alarmTriggered);

                    int time;

                    time = 20 + mc.LCSRandom(10) - n.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].getModifiedValue()
                                              - n.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].getModifiedValue();

                    if (time < 1) time = 1;

                    if (suspicionTimer > time || suspicionTimer == -1) suspicionTimer = time;
                    else
                    {
                        if (suspicionTimer > 5) suspicionTimer -= 5;
                        if (suspicionTimer <= 5)
                        {
                            suspicionTimer = 0;
                            mc.addCombatMessage("The Squad smells Conservative panic!", true);
                        }
                    }
                }
                else
                {
                    if (!alarmTriggered)
                    {
                        if ((location.getComponent<TroubleSpot>().getFlags() & LocationDef.TroubleSpotFlag.RESIDENTIAL) != 0 &&
                            getSquadTile().getComponent<TileBase>().restricted)
                        {
                            noticedText += " sees the Squad's Liberal Trespassing ";
                        }
                        else if (weapon == 0)
                        {
                            noticedText += " sees the Squad's Liberal Weapons ";
                        }
                        else
                        {
                            noticedText += " looks at the Squad with Intolerance ";                            
                        }

                        if (n.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
                            noticedText += "and lets forth a piercing Conservative alarm cry!";
                        else
                            noticedText += "and shouts for help!";

                        alarmTriggered = true;
                        noticedText += "</color>";
                        mc.addCombatMessage(noticedText, true);
                        mc.doNextAction();
                    }
                    else
                    {
                        Fight.theyFight(squad, encounterEntities, mc.getNextAction());
                        attacked = true;
                    }               
                }
            }
            else
            {
                mc.doNextAction();
            }
        }

        private void heyMisterDog(Entity con, List<Dialog> dialog)
        {
            MasterController mc = MasterController.GetMC();
            //Ignore the selected lib, find lib with most heart.
            Entity bestLib = squad[0];

            foreach (Entity e in squad)
            {
                if (e.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].getModifiedValue() >
                    bestLib.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].getModifiedValue())
                    bestLib = e;
            }

            string pitch = "";
            string response = "";
            bool success = false;

            if (bestLib.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].getModifiedValue() >= 15)
            {
                success = true;
                switch (mc.LCSRandom(11))
                {
                    case 0:
                        pitch = "I love dogs more than people.";
                        response = "A human after my own heart, in more ways than one.";
                        break;
                    case 1:
                        pitch = "Dogs are the future of humanity.";
                        response = "I don't see it, but I'll hear you out.";
                        break;
                    case 2:
                        pitch = "Power to the canines!";
                        response = "Down with the feline establishment!";
                        break;
                    case 3:
                        pitch = "We need to recruit more dogs.";
                        response = "Oh yeah? I'm a dog. What do you represent?";
                        break;
                    case 4:
                        pitch = "Wanna join the LCS?";
                        response = "Do you have a good veteranary plan?";
                        break;
                    case 5:
                        pitch = "Want me to untie you?";
                        response = "Yes, please! This collar is painful!";
                        break;
                    case 6:
                        pitch = "You deserve better than this.";
                        response = "Finally, a human that understands.";
                        break;
                    case 7:
                        pitch = "Dogs are the best anything ever.";
                        response = "Heheheh, you're funny. Okay, I won't rat you out.";
                        break;
                    case 8:
                        pitch = "Conservatives kick dogs!";
                        response = "That IS disturbing. What can I do?";
                        break;
                    case 9:
                        pitch = "All we are saying is give fleas a chance.";
                        response = "We'll fight the fleas until our dying itch.";
                        break;
                    case 10:
                        pitch = "Dogs are better than humans.";
                        response = "You're pandering, but I love it.";
                        break;
                }
            }
            else
            {
                con.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;
                switch (mc.LCSRandom(11))
                {
                    case 0:
                        pitch = "Hi Mister Dog!";
                        response = "Woof?";
                        break;
                    case 1:
                        pitch = "Good dog!";
                        response = "Bark!";
                        break;
                    case 2:
                        pitch = "Hey there, boy.";
                        response = "Woof!";
                        break;
                    case 3:
                        pitch = "Woof...?";
                        response = "Woof!";
                        break;
                    case 4:
                        pitch = "Bark at the man for me!";
                        response = "Bark! Grr...";
                        break;
                    case 5:
                        pitch = "Down, boy!";
                        response = "Rr...?";
                        break;
                    case 6:
                        pitch = "Don't bite me!";
                        response = "Grrr...!";
                        break;
                    case 7:
                        pitch = "Hi doggy!";
                        response = "Bark!";
                        break;
                    case 8:
                        pitch = "Hi, puppy.";
                        response = "Bark!";
                        break;
                    case 9:
                        pitch = "OH MAN I LOVE DOGS!";
                        response = "Bark!";
                        break;
                    case 10:
                        pitch = "Bark! Bark!";
                        response = "Your accent is atrocious.";
                        break;
                }
            }

            dialog.Add(new Dialog(bestLib, pitch, bestLib.getComponent<CreatureInfo>().getName() + " says \"" + pitch + "\""));
            dialog.Add(new Dialog(con, response, con.getComponent<CreatureInfo>().encounterName + " responds <color=cyan>\"" + response + "\"</color>"));
            constructConversation(dialog);
            if (success)
            {
                mc.addToCurrentQueue(() =>
                {
                    foreach (Entity e in encounterEntities)
                    {
                        if (e == null) continue;
                        if (e.getComponent<Body>().getSpecies().type == "DOG")
                        {
                            e.getComponent<CreatureInfo>().alignment = Alignment.LIBERAL;
                        }
                    }

                    mc.doNextAction();
                }, "Pacify Dogs");
            }
        }

        private void heyMisterMonster(Entity con, List<Dialog> dialog)
        {
            MasterController mc = MasterController.GetMC();
            //Ignore the selected lib, find lib with most heart.
            Entity bestLib = squad[0];

            foreach (Entity e in squad)
            {
                if (e.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].getModifiedValue() >
                    bestLib.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].getModifiedValue())
                    bestLib = e;
            }

            string pitch = "";
            string response = "";
            bool success = false;

            if (bestLib.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].getModifiedValue() >= 15)
            {
                success = true;
                switch (mc.LCSRandom(11))
                {
                    case 0:
                        pitch = "I love diversity in all its forms.";
                        response = "Your tolerance is impressive, human!";
                        break;
                    case 1:
                        pitch = "Your kind are the future of humanity.";
                        response = "Your recognition of our superiority is wise.";
                        break;
                    case 2:
                        pitch = "Power to the genetic monsters!";
                        response = "Down with the human establishment!";
                        break;
                    case 3:
                        pitch = "We need to recruit more genetic monsters.";
                        response = "For what purpose do you seek our aid?";
                        break;
                    case 4:
                        pitch = "Wanna join the LCS?";
                        response = "Maybe. Can we scare small children?";
                        break;
                    case 5:
                        pitch = "You're free! Join us to liberate more!";
                        response = "Is this what compassion is?";
                        break;
                    case 6:
                        pitch = "You deserve better than this.";
                        response = "No beast deserves to be an experiment!";
                        break;
                    case 7:
                        pitch = "You are the best anything ever.";
                        response = "It's okay blokes, this one is friendly.";
                        break;
                    case 8:
                        pitch = "We should flay geneticists together!";
                        response = "My favorite future hobby!";
                        break;
                    case 9:
                        pitch = "All we are saying is give peace a chance.";
                        response = "Will humans ever let us have peace?";
                        break;
                    case 10:
                        pitch = "Monsters are better than humans.";
                        response = "You're a clever one.";
                        break;
                }
            }
            else
            {
                con.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;
                switch (mc.LCSRandom(11))
                {
                    case 0:
                        pitch = "Hi Mister Monster!";
                        response = "Die in a fire!";
                        break;
                    case 1:
                        pitch = "Good monster!";
                        response = "Die in a fire!";
                        break;
                    case 2:
                        pitch = "Woah, uh... shit!";
                        response = "Foolish mortal!";
                        break;
                    case 3:
                        pitch = "Don't kill us!";
                        response = "You're already dead!";
                        break;
                    case 4:
                        pitch = "Oh crap!";
                        response = "Where is your god now, mortal?!";
                        break;
                    case 5:
                        CreatureInfo.CreatureGender monsterGender = con.getComponent<CreatureInfo>().genderLiberal;
                        pitch = "Uhhh... down, " + (monsterGender== CreatureInfo.CreatureGender.FEMALE ? "boy" : "girl") + "!";
                        response = "I'm a " + (monsterGender == CreatureInfo.CreatureGender.FEMALE ? "girl" : "boy") + ", fool!";
                        break;
                    case 6:
                        pitch = "Don't eat me!";
                        response = "I will feast on your flesh!";
                        break;
                    case 7:
                        pitch = "Excuse me, I am, uh...";
                        response = "About to die?!";
                        break;
                    case 8:
                        pitch = "Shh... it's okay... I'm a friend!";
                        response = "We will kill you AND your friends!";
                        break;
                    case 9:
                        pitch = "OH MAN I LOVE MONSTERS!";
                        response = "WHAT A COINCIDENCE, I'M HUNGRY!";
                        break;
                    case 10:
                        pitch = "Slurp! Boom! Raaahgh!";
                        response = "Your mockery will be met with death!";
                        break;
                }
            }

            dialog.Add(new Dialog(bestLib, pitch, bestLib.getComponent<CreatureInfo>().getName() + " says \"" + pitch + "\""));
            dialog.Add(new Dialog(con, response, con.getComponent<CreatureInfo>().encounterName + " responds <color=cyan>\"" + response + "\"</color>"));
            constructConversation(dialog);
            if (success)
            {
                mc.addToCurrentQueue(() =>
                {
                    foreach (Entity e in encounterEntities)
                    {
                        if (e == null) continue;
                        if (e.getComponent<Body>().getSpecies().type == "MONSTER")
                        {
                            e.getComponent<CreatureInfo>().alignment = Alignment.LIBERAL;
                        }
                    }

                    mc.doNextAction();
                }, "Pacify Monsters");
            }
        }

        private void constructConversation(List<Dialog> dialog)
        {
            MasterController mc = MasterController.GetMC();

            foreach(Dialog text in dialog)
            {
                sceneRoot.Add(() =>
                {
                    mc.addCombatMessage(text.logText);
                    mc.uiController.doSpeak(new UI.UIEvents.Speak(text.speaker, text.line));
                }, "dialogLine");
            }
        }

        private class Dialog
        {
            public string line;
            public string logText;
            public Entity speaker;

            public Dialog(Entity speaker, string line, string logText)
            {
                this.speaker = speaker;
                this.line = line;
                this.logText = logText;
            }
        }

        //Is this a special tile that activates automatically?
        private bool checkEntrySpecial()
        {
            if (!getSquadTile().hasComponent<TileSpecial>())
                return false;
            else
            {
                switch (getSquadTile().getComponent<TileSpecial>().name)
                {
                    case "HOUSE_CEO":
                    case "APARTMENT_LANDLORD":
                    case "RESTAURANT_TABLE":
                    case "CAFE_COMPUTER":
                    case "PARK_BENCH":
                    case "CLUB_BOUNCER":
                    case "SECURITY_CHECKPOINT":
                    case "SECURITY_METALDETECTORS":
                    case "CCS_BOSS":
                    case "BANK_TELLER":
                        return true;
                    default:
                        return false;
                }
            }
        }

        private bool checkExit()
        {
            if (getSquadTile().getComponent<TileFloor>().type == TileFloor.Type.EXIT)
                return true;
            else
                return false;
        }

        private void activateEntrySpecial()
        {
            if (!checkEntrySpecial()) return;
            if (getSquadTile().getComponent<TileSpecial>().used) return;

            switch (getSquadTile().getComponent<TileSpecial>().name)
            {
                //Automatic Encounters
                case "RESTAURANT_TABLE":
                case "CAFE_COMPUTER":
                case "PARK_BENCH":
                    specialSpawn(getSquadTile().getComponent<TileSpecial>().name); break;
                //Security checks
                case "CLUB_BOUNCER":
                    specialBouncer(); break;
                case "SECURITY_CHECKPOINT":
                case "SECURITY_METALDETECTORS":
                    break;
                //Special encounters
                case "BANK_TELLER":
                    string encounterText = "";

                    if (alarmTriggered)
                    {
                        encounterText = "The teller window is empty";
                        getSquadTile().getComponent<TileSpecial>().used = true;
                        MasterController.GetMC().addCombatMessage(encounterText, encounterWarnings);
                        if (encounterWarnings)
                            sceneRoot.Add(() => { }, "dummy");
                    }
                    else
                    {
                        encounterText = "A bank teller is available.";
                        MasterController.GetMC().addCombatMessage(encounterText, encounterWarnings);
                        Entity e = Factories.CreatureFactory.create("BANK_TELLER");
                        encounterEntities.Add(e);
                        if (!inEncounter)
                        {
                            if (encounterWarnings)
                            {
                                sceneRoot.Add(() =>
                                {
                                    inEncounter = true;
                                    MasterController.GetMC().uiController.siteMode.startEncounter();
                                    MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                                }, "New Encounter");
                            }
                            else
                            {
                                inEncounter = true;
                                MasterController.GetMC().uiController.siteMode.startEncounter();
                                MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                            }
                        }
                        else
                        {
                            if (encounterWarnings)
                                sceneRoot.Add(() => { }, "dummy");
                        }
                    }
                    if (encounterWarnings) MasterController.GetMC().doNextAction();
                    break;
                case "CCS_BOSS":
                    Entity ccs_leader = Factories.CreatureFactory.create("CCS_ARCHCONSERVATIVE");
                    if (MasterController.ccs.baseKills == 2)
                        ccs_leader.getComponent<CreatureInfo>().encounterName = "CCS Founder";

                    if (!alarmTriggered)
                    {
                        encounterEntities.Add(ccs_leader);
                        MasterController.GetMC().addCombatMessage("The CCS leader is here.", encounterWarnings);

                        if (!inEncounter)
                        {
                            if (encounterWarnings)
                            {
                                sceneRoot.Add(() =>
                                {
                                    inEncounter = true;
                                    MasterController.GetMC().uiController.siteMode.startEncounter();
                                    MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                                }, "New Encounter");
                            }
                            else
                            {
                                inEncounter = true;
                                MasterController.GetMC().uiController.siteMode.startEncounter();
                                MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                            }
                        }                        
                    }
                    else
                    {
                        encounterEntities.Add(ccs_leader);
                        for(int i=0;i<5;i++)
                            encounterEntities.Add(Factories.CreatureFactory.create("CCS_VIGILANTE"));

                        if (!inEncounter)
                        {
                            MasterController.GetMC().addCombatMessage("The CCS leader is ready for you!", encounterWarnings);

                            if (encounterWarnings)
                            {
                                sceneRoot.Add(() =>
                                {
                                    inEncounter = true;
                                    MasterController.GetMC().uiController.siteMode.startEncounter();
                                    MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                                }, "New Encounter");
                            }
                            else
                            {
                                inEncounter = true;
                                MasterController.GetMC().uiController.siteMode.startEncounter();
                                MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                            }
                        }
                        else
                        {
                            if (encounterWarnings)
                                sceneRoot.Add(() => { }, "dummy");
                        }
                    }
                    getSquadTile().getComponent<TileSpecial>().used = true;
                    if (encounterWarnings) MasterController.GetMC().doNextAction();
                    break;
                case "HOUSE_CEO":
                    if (!alarmTriggered)
                    {
                        Entity ceo = null;
                        if (MasterController.government.ceo != null)
                        {
                            ceo = MasterController.government.ceo;
                        }
                        else
                        {
                            ceo = MasterController.government.makeCEO();
                        }


                        if (!ceo.hasComponent<Liberal>() || ceo.getComponent<Liberal>().status == Liberal.Status.SLEEPER)
                        {
                            encounterEntities.Add(ceo);
                            MasterController.GetMC().addCombatMessage("The CEO is in his study.", encounterWarnings);
                            if (!inEncounter)
                            {
                                if (encounterWarnings)
                                {
                                    sceneRoot.Add(() =>
                                    {
                                        inEncounter = true;
                                        MasterController.GetMC().uiController.siteMode.startEncounter();
                                        MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                                    }, "New Encounter");
                                }
                                else
                                {
                                    inEncounter = true;
                                    MasterController.GetMC().uiController.siteMode.startEncounter();
                                    MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                                }
                            }
                            else
                            {
                                if (encounterWarnings)
                                    sceneRoot.Add(() => { }, "dummy");
                            }
                        }
                        else
                        {
                            MasterController.GetMC().addCombatMessage("The CEO study is empty.", encounterWarnings);
                            if (encounterWarnings)
                                sceneRoot.Add(() => { }, "dummy");
                        }
                    }
                    else
                    {
                        MasterController.GetMC().addCombatMessage("The CEO must have fled to a panic room.", encounterWarnings);
                        if (encounterWarnings)
                            sceneRoot.Add(() => { }, "dummy");
                    }
                    getSquadTile().getComponent<TileSpecial>().used = true;
                    if (encounterWarnings) MasterController.GetMC().doNextAction();
                    break;
                case "APARTMENT_LANDLORD":
                    if(alarmTriggered||siteAlienate > 0 || location.getComponent<SafeHouse>().underSiege)
                    {
                        MasterController.GetMC().addCombatMessage("The landlord is out of the office.", encounterWarnings);
                        if (encounterWarnings)
                            sceneRoot.Add(() => { }, "dummy");
                    }
                    else
                    {
                        encounterEntities.Add(Factories.CreatureFactory.create("LANDLORD"));
                        MasterController.GetMC().addCombatMessage("The landlord is in.", encounterWarnings);

                        if (!inEncounter)
                        {
                            if (encounterWarnings)
                            {
                                sceneRoot.Add(() =>
                                {
                                    inEncounter = true;
                                    MasterController.GetMC().uiController.siteMode.startEncounter();
                                    MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                                }, "New Encounter");
                            }
                            else
                            {
                                inEncounter = true;
                                MasterController.GetMC().uiController.siteMode.startEncounter();
                                MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                            }
                        }
                        else
                        {
                            if (encounterWarnings)
                                sceneRoot.Add(() => { }, "dummy");
                        }
                    }
                    getSquadTile().getComponent<TileSpecial>().used = true;
                    if (encounterWarnings) MasterController.GetMC().doNextAction();
                    break;
                default:
                    break;
            }
        }

        enum rejectReason
        {
            ALREADY_REJECTED,
            CCS,            
            NUDE,
            WEAPONS,
            UNDERAGE,
            FEMALEISH,
            FEMALE,
            BLOODYCLOTHES,
            DAMAGEDCLOTHES,
            CROSSDRESSING,            
            DRESSCODE,
            SECONDRATECLOTHES,
            SMELLFUNNY,
            GUESTLIST,
            NOT_REJECTED
        };

        private void specialBouncer()
        {
            MasterController mc = MasterController.GetMC();

            if (alarmTriggered)
            {
                getSquadTile().getComponent<TileSpecial>().used = true;
                mc.addCombatMessage("The Bouncer is gone.");
                return;
            }

            processingRound = true;

            Entity sleeper = null;

            bool ccs = location.hasComponent<SafeHouse>() &&
                    (location.getComponent<SafeHouse>().getFlags() & LocationDef.BaseFlag.CCS_BASE) != 0 &&
                    !location.getComponent<SafeHouse>().owned;


            foreach (Entity e in MasterController.lcs.getAllSleepers())
            {
                if(e.getComponent<Liberal>().homeBase == location && (e.def == "BOUNCER" || e.def == "CCS_VIGILANTE"))
                {
                    sleeper = e;
                    break;
                }
            }

            if (sleeper != null) encounterEntities.Add(sleeper);
            else
            {
                if(ccs)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Entity e = Factories.CreatureFactory.create("CCS_VIGILANTE");
                        e.getComponent<CreatureInfo>().encounterName = "Bouncer";
                        encounterEntities.Add(e);
                    }
                }
                else
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Entity e = Factories.CreatureFactory.create("BOUNCER");
                        if (location.hasComponent<SafeHouse>() && location.getComponent<SafeHouse>().owned)
                            e.getComponent<CreatureInfo>().alignment = Alignment.LIBERAL;
                        encounterEntities.Add(e);
                    }
                }                
            }

            if (!inEncounter)
            {
                inEncounter = true;
                MasterController.GetMC().uiController.siteMode.startEncounter();
                MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
            }

            string bouncerText = "";
            string bouncerDialog = "";
            bool accepted = false;


            if(sleeper != null)
            {
                bouncerText += "Sleeper " + sleeper.getComponent<CreatureInfo>().getName() + " smirks and lets the squad in.";
                accepted = true;
            }
            else
            {
                if(ccs)
                    bouncerText += "The Conservative scum block the door...";
                else
                    bouncerText += "The Bouncer assesses your squad...";

                rejectReason rejected = rejectReason.NOT_REJECTED;

                foreach(Entity e in squad)
                {
                    //If you've already been rejected once you don't get another try
                    if (rejectedByBouncer)
                    {
                        rejected = rejectReason.ALREADY_REJECTED;
                        break;
                    }

                    //If it's your safehouse you can just go in
                    if(location.hasComponent<SafeHouse>() && location.getComponent<SafeHouse>().owned)
                    {
                        rejected = rejectReason.NOT_REJECTED;
                        break;
                    }

                    int result = e.getComponent<CreatureBase>().Skills[Constants.SKILL_DISGUISE].roll();

                    //Invalid disguises means roll auto-fails
                    if (e.getComponent<Inventory>().getDisguiseLevel() <= 0) result = 0;
                    //Partial disguises are less effective
                    else if (e.getComponent<Inventory>().getDisguiseLevel() == 1) result /= 2;
                    //Having a hostage makes blending in very hard
                    if (e.getComponent<Liberal>().hauledUnit != null) result /= 4;

                    if (e.getComponent<Inventory>().getArmor().def == "ARMOR_NONE")
                        if (rejected > rejectReason.NUDE) rejected = rejectReason.NUDE;
                    if (e.getComponent<Inventory>().getDisguiseLevel() <= 0)
                        if (rejected > rejectReason.DRESSCODE) rejected = rejectReason.DRESSCODE;
                    if (e.getComponent<Inventory>().getArmor().getComponent<Armor>().bloody)
                        if (rejected > rejectReason.BLOODYCLOTHES) rejected = rejectReason.BLOODYCLOTHES;
                    if (e.getComponent<Inventory>().getArmor().getComponent<Armor>().damaged)
                        if (rejected > rejectReason.DAMAGEDCLOTHES) rejected = rejectReason.DAMAGEDCLOTHES;
                    if (e.getComponent<Inventory>().getArmor().getComponent<Armor>().quality > 1)
                        if (rejected > rejectReason.SECONDRATECLOTHES) rejected = rejectReason.SECONDRATECLOTHES;
                    if (e.getComponent<Inventory>().checkWeaponDisguise() < 2)
                        if (rejected > rejectReason.WEAPONS) rejected = rejectReason.WEAPONS;
                    if (result < (int)Difficulty.CHALLENGING)
                        if (rejected > rejectReason.SMELLFUNNY) rejected = rejectReason.SMELLFUNNY;
                    if (e.getComponent<Age>().getAge() < 18)
                        if (rejected > rejectReason.UNDERAGE) rejected = rejectReason.UNDERAGE;
                    if (ccs && (location.getComponent<TroubleSpot>().getFlags() & LocationDef.TroubleSpotFlag.RESTRICTED) != 0)
                        if (rejected > rejectReason.CCS) rejected = rejectReason.CCS;
                    if (location.def == "BUSINESS_CIGARBAR")
                    {
                        if (!(e.getComponent<CreatureInfo>().genderConservative == CreatureInfo.CreatureGender.MALE ||
                        e.getComponent<CreatureInfo>().genderConservative == CreatureInfo.CreatureGender.WHITEMALEPATRIARCH) ||
                        (e.getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.FEMALE &&
                        MasterController.government.laws[Constants.LAW_WOMEN].alignment < Alignment.LIBERAL))
                        {
                            if (e.getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.FEMALE)
                            {
                                if (rejected > rejectReason.FEMALE) rejected = rejectReason.FEMALE;
                            }
                            else if (!e.getComponent<CreatureBase>().Skills[Constants.SKILL_DISGUISE].check(Difficulty.HARD) &&
                                    MasterController.government.laws[Constants.LAW_GAY].alignment < Alignment.ELITE_LIBERAL)
                            {
                                if (rejected > rejectReason.FEMALEISH) rejected = rejectReason.FEMALEISH;
                            }
                        }

                        if (location.getComponent<TroubleSpot>().highSecurity > 0)
                            if (rejected > rejectReason.GUESTLIST) rejected = rejectReason.GUESTLIST;
                    }
                }

                if (rejected == rejectReason.NOT_REJECTED) accepted = true;                
                switch (rejected)
                {
                    case rejectReason.NUDE:
                        switch (mc.LCSRandom(4))
                        {
                            case 0: bouncerDialog = "No shirt, no underpants, no service."; break;
                            case 1: bouncerDialog = "Put some clothes on! That's disgusting."; break;
                            case 2: bouncerDialog = "No! No, you can't come in naked! God!!"; break;
                            case 3: bouncerDialog = "No shoes, no shirt and you don't get service"; break;
                        }
                        break;
                    case rejectReason.UNDERAGE:
                        switch (mc.LCSRandom(5))
                        {
                            case 0: bouncerDialog = "Hahaha, come back in a few years."; break;
                            case 1: bouncerDialog = "Find some kiddy club."; break;
                            case 2: bouncerDialog = "You don't look 18 to me."; break;
                            case 3: bouncerDialog = "Go back to your treehouse."; break;
                            case 4: bouncerDialog = "Where's your mother?"; break;
                        }
                        break;
                    case rejectReason.FEMALE:
                        switch (mc.LCSRandom(4))
                        {
                            case 0: bouncerDialog = "Move along ma'am, this club's for men."; break;
                            case 1: bouncerDialog = "This 'ain't no sewing circle, ma'am."; break;
                            case 2: bouncerDialog = "Sorry ma'am, this place is only for the men."; break;
                            case 3: bouncerDialog = "Where's your husband?"; break;
                        }
                        break;
                    case rejectReason.FEMALEISH:
                        switch (mc.LCSRandom(3))
                        {
                            case 0: bouncerDialog = "You /really/ don't look like a man to me..."; break;
                            case 1: bouncerDialog = "Y'know... the \'other\' guys won't like you much."; break;
                            case 2: bouncerDialog = "Uhh... can't let you in, ma'am. Sir. Whatever."; break;
                        }
                        break;
                    case rejectReason.DRESSCODE:
                        switch (mc.LCSRandom(3))
                        {
                            case 0: bouncerDialog = "Check the dress code."; break;
                            case 1: bouncerDialog = "We have a dress code here."; break;
                            case 2: bouncerDialog = "I can't let you in looking like that."; break;
                        }
                        break;
                    case rejectReason.SMELLFUNNY:
                        switch (mc.LCSRandom(6))
                        {
                            case 0: bouncerDialog = "God, you smell."; break;
                            case 1: bouncerDialog = "Not letting you in. Because I said so."; break;
                            case 2: bouncerDialog = "There's just something off about you."; break;
                            case 3: bouncerDialog = "Take a shower."; break;
                            case 4: bouncerDialog = "You'd just harass the others, wouldn't you?"; break;
                            case 5: bouncerDialog = "Get the " + mc.swearFilter("fuck", "heck") + " out of here."; break;
                        }
                        break;
                    case rejectReason.BLOODYCLOTHES:
                        switch (mc.LCSRandom(5))
                        {
                            case 0: bouncerDialog = "Good God! What is wrong with your clothes?"; break;
                            case 1: bouncerDialog = "Absolutely not. Clean up a bit."; break;
                            case 2: bouncerDialog = "This isn't a goth club, bloody clothes don't cut it here."; break;
                            case 3: bouncerDialog = "Uh, maybe you should wash... replace... those clothes."; break;
                            case 4: bouncerDialog = "Did you spill something on your clothes?"; break;
                            case 5: bouncerDialog = "Come back when you get the red wine out of your clothes."; break;
                        }
                        break;
                    case rejectReason.DAMAGEDCLOTHES:
                        switch (mc.LCSRandom(2))
                        {
                            case 0: bouncerDialog = "Good God! What is wrong with your clothes?"; break;
                            case 1: bouncerDialog = "This isn't a goth club, ripped clothes don't cut it here."; break;
                        }
                        break;
                    case rejectReason.SECONDRATECLOTHES:
                        switch (mc.LCSRandom(2))
                        {
                            case 0: bouncerDialog = "That looks like you sewed it yourself."; break;
                            case 1: bouncerDialog = "If badly cut clothing is a hot new trend, I missed it."; break;
                        }
                        break;
                    case rejectReason.WEAPONS:
                        switch (mc.LCSRandom(5))
                        {
                            case 0: bouncerDialog = "No weapons allowed."; break;
                            case 1: bouncerDialog = "I can't let you in carrying that."; break;
                            case 2: bouncerDialog = "I can't let you take that in."; break;
                            case 3: bouncerDialog = "Come to me armed, and I'll tell you to take a hike."; break;
                            case 4: bouncerDialog = "Real men fight with fists. And no, you can't come in."; break;
                        }
                        break;
                    case rejectReason.GUESTLIST:
                        bouncerDialog = "This club is invitation only.";
                        break;
                    case rejectReason.ALREADY_REJECTED:
                        bouncerDialog = "What did I just say to you? You aren't coming in.";
                        break;
                    case rejectReason.CCS:
                        switch (mc.LCSRandom(11))
                        {
                            case 0: bouncerDialog = "Can I see... heh heh... some ID?"; break;
                            case 1: bouncerDialog = "Woah... you think you're coming in here?"; break;
                            case 2: bouncerDialog = "Check out this fool. Heh."; break;
                            case 3: bouncerDialog = "Want some trouble, dumpster breath?"; break;
                            case 4: bouncerDialog = "You're gonna stir up the hornet's nest, fool."; break;
                            case 5: bouncerDialog = "Come on, take a swing at me. Just try it."; break;
                            case 6: bouncerDialog = "You really don't want to fuck with me."; break;
                            case 7: bouncerDialog = "Hey girly, have you written your will?"; break;
                            case 8: bouncerDialog = "Oh, you're trouble. I *like* trouble."; break;
                            case 9: bouncerDialog = "I'll bury you in those planters over there."; break;
                            case 10: bouncerDialog = "Looking to check on the color of your blood?"; break;
                        }
                        break;
                    case rejectReason.NOT_REJECTED:
                        switch (mc.LCSRandom(4))
                        {
                            case 0: bouncerDialog = "Keep it civil and don't drink too much."; break;
                            case 1: bouncerDialog = "Let me get the door for you."; break;
                            case 2: bouncerDialog = "Ehh, alright, go on in."; break;
                            case 3: bouncerDialog = "Come on in."; break;
                        }
                        break;
                }
            }

            mc.addCombatMessage(bouncerText, true);            
            if (accepted)
            {
                sceneRoot.Add(() =>
                {
                    Entity[,] map = location.getComponent<TroubleSpot>().map[squadPosition.z];
                    getSquadTile().getComponent<TileSpecial>().used = true;

                    if (squadPosition.y - 1 >= 0 && map[squadPosition.x, squadPosition.y - 1].hasComponent<TileDoor>())
                        map[squadPosition.x, squadPosition.y - 1].getComponent<TileDoor>().open = true;
                    if (squadPosition.y + 1 < map.GetLength(1) && map[squadPosition.x, squadPosition.y + 1].hasComponent<TileDoor>())
                        map[squadPosition.x, squadPosition.y + 1].getComponent<TileDoor>().open = true;
                    if (squadPosition.x - 1 >= 0 && map[squadPosition.x - 1, squadPosition.y].hasComponent<TileDoor>())
                        map[squadPosition.x - 1, squadPosition.y].getComponent<TileDoor>().open = true;
                    if (squadPosition.x + 1 < map.GetLength(0) && map[squadPosition.x + 1, squadPosition.y].hasComponent<TileDoor>())
                        map[squadPosition.x + 1, squadPosition.y].getComponent<TileDoor>().open = true;
                    mc.doNextAction();
                }, "openDoor");
            }
            else
            {
                rejectedByBouncer = true;
            }

            sceneRoot.Add(() => 
            {
                if (bouncerDialog != "")
                {
                    mc.uiController.doSpeak(new UI.UIEvents.Speak(encounterEntities[0], bouncerDialog));
                    mc.addCombatMessage("<color=" + (accepted?"lime":"red") + ">\"" + bouncerDialog + "\"</color>");
                }
                processingRound = false;
            }, "finishRound");

            mc.doNextAction();
        }

        private void specialBankMoney()
        {
            MasterController mc = MasterController.GetMC();

            startRound();
            //We need the checks to be done early here since otherwise the SWAT team will open fire on the squad the same turn they spawn
            sceneRoot.Add(() => { noticeCheck(false, Difficulty.HEROIC); }, "noticeCheck");
            sceneRoot.Add(disguiseCheck, "disguiseCheck");
            sceneRoot.Add(() =>
            {
                mc.addCombatMessage("<color=lime>The squad loads bricks of cash into a duffel bag.</color>");
                getSquadTile().getComponent<TileSpecial>().used = true;
                lootedCash += 20000;
                siteCrime += 20;
                if (alarmTimer <= 80) swatCounter = 0;
                if (!alarmTriggered && suspicionTimer != 0) suspicionTimer = 0;
                else if (!alarmTriggered && mc.LCSRandom(0) == 0) alarmTriggered = true;
                else if (alarmTriggered && alarmTimer <= 60) alarmTimer += 20;
                else if (alarmTriggered && alarmTimer <= 80 && mc.LCSRandom(2) == 0) alarmTimer = 81;
                else if (alarmTriggered && alarmTimer > 80 && mc.LCSRandom(2) == 0 && swatCounter < 2)
                {
                    if (swatCounter > 0)
                        mc.addCombatMessage("Another SWAT team moves in!");
                    else
                        mc.addCombatMessage("A SWAT team storms the vault!");
                    swatCounter++;

                    for (int i = 0; i < 9; i++)
                        encounterEntities.Add(Factories.CreatureFactory.create("SWAT"));

                    if (!inEncounter)
                    {
                        inEncounter = true;
                        MasterController.GetMC().uiController.siteMode.startEncounter();
                        MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);
                    }
                }

                mc.doNextAction();
            }, "StealMoney");            
            sceneRoot.Add(() => endRound(), "End Round");
            mc.doNextAction();
        }

        private void specialBankVault()
        {
            List<UI.PopupOption> options = new List<UI.PopupOption>();
            processingRound = true;
            MasterController mc = MasterController.GetMC();

            string sleepertext = "";
            Entity sleeper = null;

            foreach (Entity e in MasterController.lcs.getAllSleepers())
            {
                if (e.def == "BANK_MANAGER" && e.getComponent<Liberal>().homeBase == location)
                {
                    sleepertext = " Sleeper " + e.getComponent<CreatureInfo>().getName() + " can handle the biometrics, but you'll still have to crack the other locks.";
                    sleeper = e;
                    break;
                }
            }

            options.Add(new UI.PopupOption("Yes", () =>
            {
                bool failed = false;

                startRound();
                sceneRoot.Add(() =>
                {
                    mc.addCombatMessage("First is the combo lock that will have be cracked by a security expert...");
                }, "securityPrompt");
                sceneRoot.Add(() =>
                {
                    if (!tryUnlock(Difficulty.HEROIC))
                    {
                        mc.addCombatMessage("The squad can only dream of the money on the other side of this door...");
                        getSquadTile().getComponent<TileSpecial>().used = true;
                        failed = true;
                    }
                    else
                    {
                        mc.addCombatMessage("Next is the elecronic lock that will have to be bypassed by a computer expert...");
                    }
                }, "computerPrompt");
                sceneRoot.Add(() =>
                {
                    if (!failed)
                    {
                        Entity bestHacker = null;
                        Entity bestBlindHacker = null;
                        int bestHackRoll = 0;
                        int bestBlindRoll = -3;
                        foreach (Entity e in squad)
                        {
                            int roll = e.getComponent<CreatureBase>().Skills[Constants.SKILL_COMPUTERS].roll();
                            if (!e.getComponent<Body>().canSee())
                            {
                                roll -= 3;
                                if (roll > bestBlindRoll)
                                {
                                    bestBlindRoll = roll;
                                    bestBlindHacker = e;
                                }
                            }
                            else
                            {
                                if (roll > bestHackRoll)
                                {
                                    bestHackRoll = roll;
                                    bestHacker = e;
                                }
                            }
                        }

                        bool blind = false;
                        if (bestBlindRoll > bestHackRoll) blind = true;
                        else if (bestHacker == null && bestBlindHacker != null) blind = true;

                        if (blind)
                        {
                            bestHacker = bestBlindHacker;
                            bestHackRoll = bestBlindRoll;
                        }

                        if (bestHacker != null && bestHackRoll > 0)
                        {
                            bestHacker.getComponent<CreatureBase>().Skills[Constants.SKILL_COMPUTERS].addExperience(15);
                            if (bestHackRoll > (int)Difficulty.CHALLENGING)
                            {
                                string responseText = bestHacker.getComponent<CreatureInfo>().getName();
                                if (!blind) responseText += " has";
                                responseText += " disabled the second layer of security";
                                if (blind) responseText += " despite being blind";
                                responseText += "!";
                                mc.addCombatMessage(responseText);
                            }
                            else
                            {
                                string responseText = bestHacker.getComponent<CreatureInfo>().getName();
                                responseText += " couldn't";
                                if (blind) responseText += " see how to";
                                responseText += " bypass the vault's electronic lock.";
                                mc.addCombatMessage(responseText);
                                failed = true;
                            }
                        }
                        else
                        {
                            string responseText = "You couldn't find anyone to do the job.";
                            if (blind) responseText += " Including the BLIND HACKER you brought";
                            mc.addCombatMessage(responseText);
                            failed = true;
                        }

                        if (failed)
                        {
                            getSquadTile().getComponent<TileSpecial>().used = true;
                            mc.addCombatMessage("The money was so close the squad could taste it!");
                        }
                        else
                        {
                            mc.addCombatMessage("Last is the biometric lock that keyed only to the bank's managers.");
                        }
                    }
                    else
                    {
                        mc.doNextAction();
                    }
                }, "biometricPrompt");
                sceneRoot.Add(() =>
                {
                    if (!failed)
                    {
                        Entity manager = null;
                        bool canbreakin = false;

                        foreach(Entity e in squad)
                        {
                            if (e.def == "BANK_MANAGER")
                            {
                                manager = e;
                                if ((mc.currentDate - e.getComponent<Liberal>().joinDate).Days < 30 &&
                                (e.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.KIDNAPPED) == 0)
                                {
                                    mc.addCombatMessage(e.getComponent<CreatureInfo>().getName() + " opens the vault!");
                                    canbreakin = true;
                                    break;
                                }
                            }
                            else if (e.getComponent<Liberal>().hauledUnit != null &&
                                    e.getComponent<Liberal>().hauledUnit.def == "BANK_MANAGER")
                            {
                                if (!e.getComponent<Liberal>().hauledUnit.hasComponent<Liberal>())
                                {
                                    mc.addCombatMessage("The hostage is forced to open the vault.");
                                    canbreakin = true;
                                    break;
                                }
                                else if ((mc.currentDate - e.getComponent<Liberal>().joinDate).Days < 30 &&
                                        (e.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.KIDNAPPED) == 0)
                                {
                                    mc.addCombatMessage(e.getComponent<CreatureInfo>().getName() + " opens the vault!");
                                    canbreakin = true;
                                    break;
                                }
                            }
                        }

                        if (!canbreakin && sleeper != null)
                        {
                            mc.addCombatMessage("Sleeper " + sleeper.getComponent<CreatureInfo>().getName() + " opens the vault, and will join the active LCS to avoid arrest");
                            canbreakin = true;
                            sleeper.getComponent<Liberal>().homeBase = sleeper.getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getComponent<City>().getLocation("RESIDENTIAL_SHELTER");
                            sleeper.getComponent<Liberal>().status = Liberal.Status.ACTIVE;
                            sleeper.getComponent<Liberal>().setActivity("NONE");
                            sleeper.getComponent<Liberal>().goHome();
                            sleeper.getComponent<CriminalRecord>().addCrime(Constants.CRIME_BANK_ROBBERY);
                        }

                        if (canbreakin)
                        {
                            foreach(Entity e in squad)
                            {
                                e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_BANK_ROBBERY);
                            }
                            siteCrime += 20;
                            MasterController.news.currentStory.addCrime("BANKVAULTROBBERY");                            
                            if(location.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x + 1, squadPosition.y].hasComponent<TileSpecial>() &&
                                location.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x + 1, squadPosition.y].getComponent<TileSpecial>().name == "VAULT_DOOR")
                            {
                                location.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x + 1, squadPosition.y].getComponent<TileSpecial>().used = true;
                            }
                            if (location.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x - 1, squadPosition.y].hasComponent<TileSpecial>() &&
                                location.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x - 1, squadPosition.y].getComponent<TileSpecial>().name == "VAULT_DOOR")
                            {
                                location.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x - 1, squadPosition.y].getComponent<TileSpecial>().used = true;
                            }
                            if (location.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x, squadPosition.y + 1].hasComponent<TileSpecial>() &&
                                location.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x, squadPosition.y + 1].getComponent<TileSpecial>().name == "VAULT_DOOR")
                            {
                                location.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x, squadPosition.y + 1].getComponent<TileSpecial>().used = true;
                            }
                            if (location.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x, squadPosition.y - 1].hasComponent<TileSpecial>() &&
                                location.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x, squadPosition.y - 1].getComponent<TileSpecial>().name == "VAULT_DOOR")
                            {
                                location.getComponent<TroubleSpot>().map[squadPosition.z][squadPosition.x, squadPosition.y - 1].getComponent<TileSpecial>().used = true;
                            }
                        }
                        else
                        {
                            if(manager != null)
                            {
                                mc.addCombatMessage(manager.getComponent<CreatureInfo>().getName() + " is no longer recognized");
                            }
                            else
                            {
                                mc.addCombatMessage("The squad has nobody that can do the job");
                            }
                        }

                        getSquadTile().getComponent<TileSpecial>().used = true;
                    }
                    else
                    {
                        mc.doNextAction();
                    }
                }, "biometricCheck");
                sceneRoot.Add(() => { noticeCheck(false, Difficulty.HEROIC); }, "noticeCheck");
                sceneRoot.Add(disguiseCheck, "disguiseCheck");
                sceneRoot.Add(() => endRound(), "End Round");
                mc.doNextAction();
            }));
            options.Add(new UI.PopupOption("No", () => { processingRound = false; }));

            mc.uiController.showYesNoPopup("The vault door has three layers: A combo lock, an electronic lock, and a biometric lock. You will need a security expert, a computer expert, and one of the bank managers." + sleepertext + " Open the bank vault?", options);
        }

        private void specialSpawn(string type)
        {
            MasterController mc = MasterController.GetMC();
            getSquadTile().getComponent<TileSpecial>().used = true;

            string encounterText = "";

            if (alarmTriggered)
            {
                switch (type)
                {
                    case "RESTAURANT_TABLE":
                        encounterText = "Some people are hiding under the table."; break;
                    case "CAFE_COMPUTER":
                        encounterText = "The PC has been unplugged."; break;
                    case "PARK_BENCH":
                        encounterText = "The bench is empty."; break;
                }

                if(type == "RESTAURANT_TABLE")
                {
                    newEncounter(false, encounterText);
                }
                else
                {
                    mc.addCombatMessage(encounterText);
                }
            }
            else
            {
                switch (type)
                {
                    case "RESTAURANT_TABLE":
                        encounterText = "The table is occupied."; break;
                    case "CAFE_COMPUTER":
                        encounterText = "The computer is occupied."; break;
                    case "PARK_BENCH":
                        encounterText = "There are people sitting here."; break;
                }

                newEncounter(false, encounterText);

                if(type == "CAFE_COMPUTER")
                {
                    encounterEntities.RemoveRange(1, encounterEntities.Count - 1);
                }
            }
        }

        private void captureSafeHouse(string message)
        {
            location.getComponent<SafeHouse>().owned = true;
            location.getComponent<SafeHouse>().floatingHeat = 100;
            location.getComponent<SafeHouse>().heat = 100;
            
            int sleepercount = 0;

            foreach (Entity e in MasterController.lcs.getAllSleepers())
            {
                if (e.getComponent<CreatureBase>().Location == location)
                {
                    e.getComponent<Liberal>().status = Liberal.Status.ACTIVE;
                    e.getComponent<Liberal>().homeBase = location.getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER");
                    e.getComponent<Liberal>().goHome();
                    sleepercount++;
                }
            }

            if (sleepercount > 1) message += "\nYour sleepers have been outed by your bold attack and joined the active LCS.";
            else if (sleepercount == 1) message += "\nYour sleeper has been outed by your bold attack and joined the active LCS.";

            MasterController.GetMC().addMessage(message, true);
        }

        private void moveSiegeEnemies()
        {
            MasterController mc = MasterController.GetMC();
            siegeAttackTime++;

            //Reinforce if enemy count is too low and they haven't been scared off yet
            if (siegeAttackTime > 100 + mc.LCSRandom(10) && enemies.Count < 7)
            {
                Position startPos = new Position(location.getComponent<TroubleSpot>().startX, location.getComponent<TroubleSpot>().startY, location.getComponent<TroubleSpot>().startZ);

                if (enemyInPosition(startPos) == null &&
                    squadPosition != startPos)
                {
                    MapEnemy enemy = new MapEnemy();
                    enemy.type = MapEnemy.EnemyType.NORMAL;
                    enemy.position = startPos;
                    enemies.Add(enemy);

                    siegeAttackTime = 0;
                }
            }

            foreach (MapEnemy enemy in enemies)
            {
                if (enemy.trapped) continue;

                //If enemy is already in player's position, don't do anything
                if (enemyInPosition(squadPosition) != null)
                    break;
                //Move into the player's tile if possible
                if (enemy.position.z == squadPosition.z &&
                (((enemy.position.x == squadPosition.x - 1 || enemy.position.x == squadPosition.x + 1) && enemy.position.y == squadPosition.y) ||
                    ((enemy.position.y == squadPosition.y - 1 || enemy.position.y == squadPosition.y + 1) && enemy.position.x == squadPosition.x)))
                {
                    enemy.position = new Position(squadPosition);
                    break;
                }

                Position targetPosition = enemy.position;

                switch (mc.LCSRandom(4))
                {
                    case 0:
                        if (location.getComponent<TroubleSpot>().map[enemy.position.z].GetLength(0) < enemy.position.x + 1) break;

                        targetPosition = new Position(enemy.position.x + 1, enemy.position.y, enemy.position.z);
                        break;
                    case 1:
                        if (0 > enemy.position.x - 1) break;

                        targetPosition = new Position(enemy.position.x - 1, enemy.position.y, enemy.position.z);
                        break;
                    case 2:
                        if (location.getComponent<TroubleSpot>().map[enemy.position.z].GetLength(1) < enemy.position.y + 1) break;

                        targetPosition = new Position(enemy.position.x, enemy.position.y + 1, enemy.position.z);
                        break;
                    case 3:
                        if (0 > enemy.position.y - 1) break;

                        targetPosition = new Position(enemy.position.x, enemy.position.y - 1, enemy.position.z);
                        break;
                }

                if (location.getComponent<TroubleSpot>().map[targetPosition.z][targetPosition.x, targetPosition.y].hasComponent<TileDoor>() &&
                    !location.getComponent<TroubleSpot>().map[targetPosition.z][targetPosition.x, targetPosition.y].getComponent<TileDoor>().open)
                {
                    location.getComponent<TroubleSpot>().map[targetPosition.z][targetPosition.x, targetPosition.y].getComponent<TileDoor>().open = true;
                }
                else if (location.getComponent<TroubleSpot>().map[targetPosition.z][targetPosition.x, targetPosition.y].getComponent<TileBase>().isWalkable() &&
                        location.getComponent<TroubleSpot>().map[targetPosition.z][targetPosition.x, targetPosition.y].getComponent<TileFloor>().type != TileFloor.Type.EXIT &&
                        enemyInPosition(targetPosition) == null)
                    enemy.position = targetPosition;

            }

            //After moving, handle traps/encounters
            List<MapEnemy> tempEnemies = new List<MapEnemy>(enemies);

            if (inEncounter) leaveEncounter();

            foreach (MapEnemy enemy in tempEnemies)
            {
                if (location.getComponent<TroubleSpot>().map[enemy.position.z][enemy.position.x, enemy.position.y].getComponent<TileBase>().trapped)
                {
                    enemy.trapped = true;
                    location.getComponent<TroubleSpot>().map[enemy.position.z][enemy.position.x, enemy.position.y].getComponent<TileBase>().trapped = false;
                }

                if (enemy.position.samePos(squadPosition))
                {
                    int num = mc.LCSRandom(3) + 4;

                    for (int i = 0; i < num; i++)
                    {
                        switch (location.getComponent<SafeHouse>().siegeType)
                        {
                            case LocationDef.EnemyType.POLICE:
                                if (location.getComponent<SafeHouse>().escalation == SafeHouse.SiegeEscalation.POLICE)
                                    encounterEntities.Add(Factories.CreatureFactory.create("SWAT"));
                                else if (location.getComponent<SafeHouse>().escalation < SafeHouse.SiegeEscalation.BOMBERS)
                                    encounterEntities.Add(Factories.CreatureFactory.create("SOLDIER"));
                                else
                                    encounterEntities.Add(Factories.CreatureFactory.create("SEAL"));
                                break;
                            case LocationDef.EnemyType.AGENT:
                                encounterEntities.Add(Factories.CreatureFactory.create("AGENT"));
                                break;
                            case LocationDef.EnemyType.CCS:
                                if (mc.LCSRandom(12) == 0)
                                {
                                    Entity ccs_boss = Factories.CreatureFactory.create("CCS_ARCHCONSERVATIVE");
                                    ccs_boss.getComponent<CreatureInfo>().encounterName = "CCS Team Leader";
                                    encounterEntities.Add(ccs_boss);
                                }
                                else if (mc.LCSRandom(11) == 0)
                                    encounterEntities.Add(Factories.CreatureFactory.create("CCS_MOLOTOV"));
                                else if (mc.LCSRandom(10) == 0)
                                    encounterEntities.Add(Factories.CreatureFactory.create("CCS_SNIPER"));
                                else
                                    encounterEntities.Add(Factories.CreatureFactory.create("CCS_VIGILANTE"));
                                break;
                            case LocationDef.EnemyType.FIREMEN:
                                encounterEntities.Add(Factories.CreatureFactory.create("FIREMAN"));
                                break;
                            case LocationDef.EnemyType.MERC:
                                encounterEntities.Add(Factories.CreatureFactory.create("MERC"));
                                break;
                            case LocationDef.EnemyType.REDNECK:
                                encounterEntities.Add(Factories.CreatureFactory.create("HICK"));
                                break;
                        }

                    }

                    foreach (Entity e in encounterEntities)
                    {
                        e.getComponent<CreatureBase>().Location = location;
                        //Tank teams will be immobilized by traps but not harmed
                        if (enemy.trapped && enemy.type != MapEnemy.EnemyType.HEAVY)
                        {
                            e.getComponent<Body>().Blood = mc.LCSRandom(75) + 1;
                        }
                    }

                    if (enemy.type == MapEnemy.EnemyType.HEAVY)
                        encounterEntities.Add(Factories.CreatureFactory.create("TANK"));

                    inEncounter = true;
                    MasterController.GetMC().uiController.siteMode.startEncounter();
                    MasterController.GetMC().uiController.enemyUI.displaySquad(encounterEntities);

                    //Regardless of whether this unit is killed or just evaded, remove them from the map
                    enemies.Remove(enemy);
                }
            }
        }

        public class MapEnemy
        {
            public enum EnemyType
            {
                NORMAL,
                HEAVY
            }

            public Position position;
            public bool trapped;
            public EnemyType type;
        }
    }
}
