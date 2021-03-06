using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LCS.Engine.Components.Item;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Location;
using LCS.Engine.Data;

namespace LCS.Engine.Scenes
{
    public class ChaseScene
    {
        public enum ChaseType
        {
            SIEGE,
            FOOT,
            CAR
        }
        public ChaseType chaseType;

        public enum ChasePhase
        {
            SELECTION,
            RUN,
            FIGHT,
            CONCLUSION,
            COMPLETE
        }

        public enum ObstacleType
        {
            NONE,
            FRUITSTAND,
            TRUCK,
            REDLIGHT,
            CHILD
        }

        public ChasePhase chasePhase;
        public LocationDef.EnemyType chaserType;
        public ObstacleType obstacle;

        private List<Entity> liberals;
        private List<Entity> conservatives;
        private List<Entity> conservativeCars;
        private Dictionary<Entity, int> liberalSpeeds;
        private Dictionary<Entity, int> conservativeSpeeds;
        private Dictionary<Entity, int> liberalCarSpeeds;
        private Dictionary<Entity, int> conservativeCarSpeeds;

        private string nameText;
        private bool pluralize;
        
        private bool attacked;
        private bool resistedArrest;
        private bool arrestedThisRound;
        private int escapedLibs;
        private bool activityChase = false;
        private LiberalCrimeSquad.Squad squad;
        private List<Entity> siegeBuffer;

        private ActionQueue chaseRoot;

        public ChaseScene()
        {
            liberals = new List<Entity>();
            siegeBuffer = new List<Entity>();
            conservatives = new List<Entity>();
            liberalSpeeds = new Dictionary<Entity, int>();
            conservativeSpeeds = new Dictionary<Entity, int>();
            liberalCarSpeeds = new Dictionary<Entity, int>();
            conservativeCarSpeeds = new Dictionary<Entity, int>();
        }

        public void startActivityFootChase(int siteCrime, LocationDef.EnemyType chaserType, string storyType, Entity character, string chaseText)
        {
            activityChase = true;

            List<Entity> tempsquad = new List<Entity>();
            tempsquad.Add(character);

            startFootChase(siteCrime, chaserType, storyType, tempsquad, chaseText);
        }

        public void startActivityCarChase(int siteCrime, LocationDef.EnemyType chaserType, string storyType, Entity character, string chaseText)
        {
            activityChase = true;

            List<Entity> tempsquad = new List<Entity>();
            tempsquad.Add(character);

            startCarChase(siteCrime, chaserType, storyType, tempsquad, chaseText);
        }

        public void startFootChase(int siteCrime, LocationDef.EnemyType chaserType, string storyType, List<Entity> libs, string chaseText, ActionQueue parentAction = null)
        {
            MasterController.GetMC().combatModifiers = MasterController.CombatModifiers.CHASE_FOOT;
            chasePhase = ChasePhase.SELECTION;
            chaseType = ChaseType.FOOT;
            escapedLibs = 0;
            this.chaserType = chaserType;

            if (libs.Count > 1)
            {
                nameText = libs[0].getComponent<Liberal>().squad.name;
                pluralize = true;
            }
            else
            {
                nameText = libs[0].getComponent<CreatureInfo>().getName();
                pluralize = false;
            }

            for (int i = 0; i < libs.Count; i++)
            {
                liberals.Add(libs[i]);
                liberalSpeeds.Add(libs[i], 0);
            }

            foreach (Entity a in liberals)
            {
                if (a.getComponent<Inventory>().getWeapon().getComponent<Weapon>().needsReload())
                    a.getComponent<Inventory>().reload(false);
            }

            MasterController mc = MasterController.GetMC();

            if (chaseText == null)
                mc.addCombatMessage("As " + nameText + " exits the site, " + (pluralize ? "they notice " : libs[0].getComponent<CreatureInfo>().heShe().ToLower() + " notices ") + (pluralize ? "they are " : libs[0].getComponent<CreatureInfo>().heShe().ToLower() + " is ") + "being followed by Conservative swine!", true);
            else
                mc.addCombatMessage(chaseText, true);

            resistedArrest = false;
            attacked = false;
            if (storyType != "")
            {
                News.NewsStory story = MasterController.news.startNewStory(storyType);
                if (!pluralize) story.subject = libs[0];
            }

            generateChasers(siteCrime);

            chaseRoot = mc.createSubQueue(() =>
            {
                mc.uiController.closeUI();
                mc.currentChaseScene = this;

                mc.uiController.squadUI.displaySquad(liberals);
                mc.uiController.enemyUI.displaySquad(conservatives);
                mc.uiController.chase.show();
            }, "open chase screen", endChase, "end chase", parentAction);
        }

        public void startCarChase(int siteCrime, LocationDef.EnemyType chaserType, string storyType, List<Entity> libs, string chaseText, ActionQueue parentAction = null)
        {
            MasterController.GetMC().combatModifiers = MasterController.CombatModifiers.CHASE_CAR;
            chasePhase = ChasePhase.SELECTION;
            chaseType = ChaseType.CAR;
            escapedLibs = 0;
            this.chaserType = chaserType;
            obstacle = ObstacleType.NONE;

            if (libs.Count > 1)
            {
                nameText = libs[0].getComponent<Liberal>().squad.name;
                pluralize = true;
            }
            else
            {
                nameText = libs[0].getComponent<CreatureInfo>().getName();
                pluralize = false;
            }

            for (int i = 0; i < libs.Count; i++)
            {
                liberals.Add(libs[i]);
                liberalSpeeds.Add(libs[i], 0);
                
                //Fix for issue where liberated people who join the squad would cause an exception because they never had a car assigned since they didn't travel here with the squad
                if(libs[i].getComponent<Inventory>().tempVehicle == null)
                {
                    foreach (Entity e in libs)
                    {
                        if(e.getComponent<Inventory>().tempVehicle != null)
                        {
                            libs[i].getComponent<Inventory>().tempVehicle = e.getComponent<Inventory>().tempVehicle;
                            e.getComponent<Inventory>().tempVehicle.getComponent<Vehicle>().passengers.Add(libs[i]);
                        }
                    }
                }

                if (!liberalCarSpeeds.ContainsKey(libs[i].getComponent<Inventory>().tempVehicle))
                    liberalCarSpeeds.Add(libs[i].getComponent<Inventory>().tempVehicle, 0);
                if (libs[i].getComponent<Liberal>().hauledUnit != null)
                    libs[i].getComponent<Liberal>().hauledUnit.getComponent<Inventory>().tempVehicle = libs[i].getComponent<Inventory>().tempVehicle;
            }

            foreach (Entity a in liberals)
            {
                if (a.getComponent<Inventory>().getWeapon().getComponent<Weapon>().needsReload())
                    a.getComponent<Inventory>().reload(false);
            }

            MasterController mc = MasterController.GetMC();

            if (chaseText == null)
                mc.addCombatMessage("As " + nameText + " exits the site, " + (pluralize ? "they notice " : libs[0].getComponent<CreatureInfo>().heShe().ToLower() + " notices ") + (pluralize ? "they are " : libs[0].getComponent<CreatureInfo>().heShe().ToLower() + " is ") + "being followed by Conservative swine!", true);
            else
                mc.addCombatMessage(chaseText, true);

            resistedArrest = false;
            attacked = false;
            if (storyType != "")
            {
                News.NewsStory story = MasterController.news.startNewStory(storyType);
                if (!pluralize) story.subject = libs[0];
            }

            generateChasers(siteCrime);

            chaseRoot = mc.createSubQueue(() =>
            {
                mc.uiController.closeUI();
                mc.currentChaseScene = this;

                mc.uiController.squadUI.displaySquad(liberals);
                mc.uiController.enemyUI.displayDriving(conservativeCars);
                mc.uiController.chase.show();
            }, "open chase screen", endChase, "end chase", parentAction);
        }

        public void sallyForth(LiberalCrimeSquad.Squad squad, Entity siegeLocation, ActionQueue parentAction = null)
        {
            MasterController mc = MasterController.GetMC();
            MasterController.GetMC().combatModifiers |= MasterController.CombatModifiers.CHASE_FOOT;
            chasePhase = ChasePhase.SELECTION;
            chaseType = ChaseType.SIEGE;
            chaserType = siegeLocation.getComponent<SafeHouse>().siegeType;
            escapedLibs = 0;
            this.squad = squad;

            chaseRoot = mc.createSubQueue(() =>
            {
                mc.uiController.closeUI();

                mc.uiController.squadUI.displaySquad(liberals);
                mc.uiController.enemyUI.displaySquad(conservatives);
                mc.uiController.chase.show();
            }, "open chase screen", () => { endSiege(siegeLocation); }, "end siege", parentAction);

            if (squad.Count > 1)
            {
                nameText = squad.name;
                pluralize = true;
            }
            else
            {
                nameText = squad[0].getComponent<CreatureInfo>().getName();
                pluralize = false;
            }

            for (int i = 0; i < squad.Count; i++)
            {
                liberals.Add(squad[i]);
                liberalSpeeds.Add(squad[i], 0);
            }

            foreach (Entity a in liberals)
            {
                if (a.getComponent<Inventory>().getWeapon().getComponent<Weapon>().needsReload())
                    a.getComponent<Inventory>().reload(false);
            }

            foreach(Entity e in siegeLocation.getComponent<SafeHouse>().getBasedLiberals())
            {
                if (!liberals.Contains(e)) siegeBuffer.Add(e);
            }

            mc.addCombatMessage("<color=red>UNDER SIEGE: ESCAPE OR ENGAGE</color>\nYou are about to exit the compound to lift the Conservative siege on your safehouse. The enemy is ready for you, and you will have to defeat them all or run away to survive this encounter. Your Squad has filled out to six members if any were available. If you have a larger pool of Liberals, they will provide cover fire from the compound until needed.", true);

            News.NewsStory story = MasterController.news.startNewStory("SQUAD_ESCAPED");
            if (siegeLocation.getComponent<SafeHouse>().underAttack)
                story.type = "SQUAD_FLEDATTACK";
            story.location = siegeLocation;

            Entity con = null;
            int SIEGENUM = 9;

            switch(siegeLocation.getComponent<SafeHouse>().siegeType)
            {
                default:
                    if (siegeLocation.getComponent<SafeHouse>().escalation == SafeHouse.SiegeEscalation.POLICE)
                    {
                        for (int i = 0; i < SIEGENUM; i++)
                        {
                            con = Factories.CreatureFactory.create("SWAT");
                            conservatives.Add(con);
                            conservativeSpeeds.Add(con, 0);
                        }
                    }
                    else if (siegeLocation.getComponent<SafeHouse>().escalation >= SafeHouse.SiegeEscalation.NATIONAL_GUARD)
                    {
                        if (siegeLocation.getComponent<SafeHouse>().escalation >= SafeHouse.SiegeEscalation.TANKS &&
                            (siegeLocation.getComponent<SafeHouse>().investments & SafeHouse.Investments.TANK_TRAPS) == 0)
                        {
                            con = Factories.CreatureFactory.create("TANK");
                            conservatives.Add(con);
                            conservativeSpeeds.Add(con, 0);
                        }
                        else
                        {
                            con = Factories.CreatureFactory.create("SOLDIER");
                            conservatives.Add(con);
                            conservativeSpeeds.Add(con, 0);
                        }

                        for (int i = 0; i < SIEGENUM - 1; i++)
                        {
                            con = Factories.CreatureFactory.create("SOLDIER");
                            conservatives.Add(con);
                            conservativeSpeeds.Add(con, 0);
                        }
                    }
                    break;
                case LocationDef.EnemyType.AGENT:
                    for (int i = 0; i < SIEGENUM; i++)
                    {
                        con = Factories.CreatureFactory.create("AGENT");
                        conservatives.Add(con);
                        conservativeSpeeds.Add(con, 0);
                    }
                    break;
                case LocationDef.EnemyType.CCS:
                    for (int i = 0; i < SIEGENUM; i++)
                    {
                        con = Factories.CreatureFactory.create("CCS_VIGILANTE");
                        conservatives.Add(con);
                        conservativeSpeeds.Add(con, 0);
                    }
                    break;
                case LocationDef.EnemyType.FIREMEN:
                    for (int i = 0; i < SIEGENUM; i++)
                    {
                        con = Factories.CreatureFactory.create("FIREMAN");
                        conservatives.Add(con);
                        conservativeSpeeds.Add(con, 0);
                    }
                    break;
                case LocationDef.EnemyType.MERC:
                    for (int i = 0; i < SIEGENUM; i++)
                    {
                        con = Factories.CreatureFactory.create("MERC");
                        conservatives.Add(con);
                        conservativeSpeeds.Add(con, 0);
                    }
                    break;
                case LocationDef.EnemyType.REDNECK:
                    for (int i = 0; i < SIEGENUM; i++)
                    {
                        con = Factories.CreatureFactory.create("HICK");
                        conservatives.Add(con);
                        conservativeSpeeds.Add(con, 0);
                    }
                    break;
            }
                        
        }

        private void generateChasers(int siteCrime)
        {
            conservativeCars = new List<Entity>();
            int numChasers = MasterController.GetMC().LCSRandom(siteCrime / 5 + 1) + 1;
            if (chaseType == ChaseType.FOOT)
            {
                if (numChasers > 6) numChasers = 6;
            }
            else
            {
                if (numChasers > 16) numChasers = 16;
            }

            Entity con = null;

            //If CCS has advanced to ATTACK status, 50% chance they will replace pursuers after a site action
            if (!activityChase && 
                MasterController.ccs.status >= ConservativeCrimeSquad.Status.ATTACK && 
                MasterController.GetMC().LCSRandom(2) == 0)
                chaserType = LocationDef.EnemyType.CCS;

            int v = 0;
            do
            {
                v++;
                Entity vehicle = null;
                switch (chaserType)
                {
                    case LocationDef.EnemyType.ARMY:
                        vehicle = Factories.ItemFactory.create("VEHICLE_HMMWV");                        
                        break;
                    case LocationDef.EnemyType.AGENT:
                       vehicle = Factories.ItemFactory.create("VEHICLE_AGENTCAR");
                        break;
                    case LocationDef.EnemyType.MERC:
                        if (MasterController.GetMC().LCSRandom(2) == 0)
                            vehicle = Factories.ItemFactory.create("VEHICLE_SUV");
                        else
                            vehicle = Factories.ItemFactory.create("VEHICLE_JEEP");
                        break;
                    case LocationDef.EnemyType.REDNECK:
                        vehicle = Factories.ItemFactory.create("VEHICLE_PICKUP");
                        break;
                    case LocationDef.EnemyType.GANG:
                        vehicle = Factories.ItemFactory.create("VEHICLE_STATIONWAGON");
                        break;
                    case LocationDef.EnemyType.CCS:
                        vehicle = Factories.ItemFactory.create("VEHICLE_SUV");
                        break;
                    case LocationDef.EnemyType.POLICE:
                        vehicle = Factories.ItemFactory.create("VEHICLE_POLICECAR");
                        break;
                }

                conservativeCarSpeeds.Add(vehicle, 0);
                conservativeCars.Add(vehicle);

            } while (v < numChasers / 4);

            for (int i = 0; i < numChasers; i++)
            {
                switch (chaserType)
                {
                    case LocationDef.EnemyType.ARMY:
                        con = Factories.CreatureFactory.create("SOLDIER");
                        conservatives.Add(con);
                        conservativeSpeeds.Add(con, 0);
                        break;
                    case LocationDef.EnemyType.AGENT:
                        con = Factories.CreatureFactory.create("AGENT");
                        conservatives.Add(con);
                        conservativeSpeeds.Add(con, 0);
                        break;
                    case LocationDef.EnemyType.MERC:
                        con = Factories.CreatureFactory.create("MERC");
                        conservatives.Add(con);
                        conservativeSpeeds.Add(con, 0);
                        break;
                    case LocationDef.EnemyType.REDNECK:
                        con = Factories.CreatureFactory.create("HICK");
                        conservatives.Add(con);
                        conservativeSpeeds.Add(con, 0);
                        break;
                    case LocationDef.EnemyType.GANG:
                        con = Factories.CreatureFactory.create("GANGMEMBER");
                        //Gang Members aren't always conservative but they should be if they are chasing you
                        con.getComponent<CreatureInfo>().alignment = Alignment.CONSERVATIVE;
                        conservatives.Add(con);
                        conservativeSpeeds.Add(con, 0);
                        break;
                    case LocationDef.EnemyType.CCS:
                        con = Factories.CreatureFactory.create("CCS_VIGILANTE");
                        //CCS chasers won't be incognito
                        con.getComponent<CreatureInfo>().encounterName = "CCS Vigilante";
                        conservatives.Add(con);
                        conservativeSpeeds.Add(con, 0);
                        break;
                    case LocationDef.EnemyType.POLICE:
                    default:
                        if (MasterController.GetMC().testCondition("LAW:POLICE:=:-2&LAW:DEATH_PENALTY:=:-2"))
                        {
                            con = Factories.CreatureFactory.create("DEATHSQUAD");
                            conservatives.Add(con);
                            conservativeSpeeds.Add(con, 0);
                        }
                        else if (MasterController.GetMC().testCondition("LAW:POLICE:<:0"))
                        {
                            con = Factories.CreatureFactory.create("GANGUNIT");
                            conservatives.Add(con);
                            conservativeSpeeds.Add(con, 0);
                        }
                        else
                        {
                            con = Factories.CreatureFactory.create("COP");
                            conservatives.Add(con);
                            conservativeSpeeds.Add(con, 0);
                        }
                        break;
                }

                con.getComponent<Inventory>().tempVehicle = conservativeCars[i % conservativeCars.Count];
                conservativeCars[i % conservativeCars.Count].getComponent<Vehicle>().passengers.Add(con);
                if (conservativeCars[i % conservativeCars.Count].getComponent<Vehicle>().driver == null)
                    conservativeCars[i % conservativeCars.Count].getComponent<Vehicle>().driver = con;
            }
        }

        #region RUN
        public void runForIt()
        {
            MasterController mc = MasterController.GetMC();
            mc.nextRound();
            chasePhase = ChasePhase.RUN;

            if (chaseType != ChaseType.SIEGE && chaserType == LocationDef.EnemyType.POLICE && !resistedArrest)
            {
                resistedArrest = true;
                foreach (Entity lib in liberals)
                {
                    if (lib != null)
                        lib.getComponent<CriminalRecord>().addCrime(Constants.CRIME_RESIST);
                }
            }

            arrestedThisRound = false;

            mc.addCombatMessage(nameText + " make" + (pluralize ? "" : "s") + " a break for it!");
            chaseRoot.Add(null, "NOP");

            foreach (Entity runner in liberals)
            {
                if (runner != null)
                    chaseRoot.Add(() => { libRun(runner); }, "librun: " + runner.getComponent<CreatureInfo>().getName(true));
            }
            foreach (Entity chaser in conservatives)
            {
                if (chaser != null)
                    chaseRoot.Add(() => { conRun(chaser); }, "conrun" + chaser.getComponent<CreatureInfo>().encounterName);
            }
            foreach (Entity runner in liberals)
            {
                if (runner != null)
                    chaseRoot.Add(() => { checkForStragglers(runner); }, "check for stragglers" + runner.getComponent<CreatureInfo>().getName(true));
            }

            Fight.theyFight(liberals, conservatives, chaseRoot);
            Fight.endOfRound(liberals, conservatives, chaseRoot);

            siegeBufferCheck();
            endCheck();
            MasterController.GetMC().doNextAction();
        }

        private void libRun(Entity runner)
        {
            //This lib has already been arrested or escaped
            if (runner == null) return;
            if (!liberals.Contains(runner)) return;

            string returnString = "";

            if (!runner.getComponent<Body>().canWalk())
                liberalSpeeds[runner] = 0;
            else
                liberalSpeeds[runner] = runner.getComponent<CreatureBase>().BaseAttributes["AGILITY"].roll() +
                    runner.getComponent<CreatureBase>().BaseAttributes["HEALTH"].roll();

            if (!runner.getComponent<Body>().canRun()) liberalSpeeds[runner] /= 2;

            if (runner.getComponent<CreatureBase>().Skills["STREET_SENSE"].check(Difficulty.AVERAGE))
            {
                liberalSpeeds[runner] += MasterController.GetMC().LCSRandom(5) + 3;
                switch (MasterController.GetMC().LCSRandom(liberalSpeeds[runner] / 5))
                {
                    default:
                        returnString = runner.getComponent<CreatureInfo>().getName() + " suddenly darts into an alley!";
                        break;
                    case 1:
                        returnString = runner.getComponent<CreatureInfo>().getName() + " runs as fast as " + runner.getComponent<CreatureInfo>().heShe().ToLower() + " can!";
                        break;
                    case 2:
                        returnString = runner.getComponent<CreatureInfo>().getName() + " climbs a fence in record time!";
                        break;
                    case 3:
                        returnString = runner.getComponent<CreatureInfo>().getName() + " scales a small building and leaps between rooftops!";
                        break;
                }
            }

            MasterController.GetMC().addCombatMessage("##DEBUG## " + runner.getComponent<CreatureInfo>().getName() + " speed=" + liberalSpeeds[runner]);
            if (returnString != "")
                MasterController.GetMC().addCombatMessage(returnString);
            else
                MasterController.GetMC().doNextAction();
        }

        private void conRun(Entity chaser)
        {
            //Con has already fallen short
            if (chaser == null) return;
            if (!conservatives.Contains(chaser)) return;

            if (!chaser.getComponent<Body>().canWalk())
                conservativeSpeeds[chaser] = 0;
            else
                conservativeSpeeds[chaser] = chaser.getComponent<CreatureBase>().BaseAttributes["AGILITY"].roll() +
                    chaser.getComponent<CreatureBase>().BaseAttributes["HEALTH"].roll();

            if (!chaser.getComponent<Body>().canRun()) conservativeSpeeds[chaser] /= 2;

            string returnText = "";

            if (conservativeSpeeds[chaser] < liberalSpeeds[getSlowestLiberal()])
            {
                returnText = "<color=cyan>" + chaser.getComponent<CreatureInfo>().encounterName + " can't keep up!</color>";
                conservatives[conservatives.IndexOf(chaser)] = null;
            }

            if(chaser != null)
                MasterController.GetMC().addCombatMessage("##DEBUG## " + chaser.getComponent<CreatureInfo>().getName() + " speed=" + conservativeSpeeds[chaser]);

            if (returnText != "")
                MasterController.GetMC().addCombatMessage(returnText);
            else
                MasterController.GetMC().doNextAction();
        }

        private void checkForStragglers(Entity runner)
        {
            MasterController mc = MasterController.GetMC();

            if (runner == null || getFastestConservative() == null)
            {
                mc.doNextAction();
                return;
            }

            string returnText = "";

            if (liberalSpeeds[runner] > conservativeSpeeds[getFastestConservative()])
            {
                returnText = "<color=cyan>" + runner.getComponent<CreatureInfo>().getName() + " breaks away!</color>";
                int runnerIndex = liberals.IndexOf(runner);
                liberals[runnerIndex] = null;
                escapedLibs++;
                if(chaseType == ChaseType.SIEGE)
                {
                    runner.getComponent<Liberal>().status = Liberal.Status.AWAY;
                    runner.getComponent<Liberal>().awayTime = 2 + mc.LCSRandom(3);
                    if(runner.getComponent<Liberal>().squad != null)
                        runner.getComponent<Liberal>().squad.Remove(runner);
                }
            }
            else if (liberalSpeeds[runner] < conservativeSpeeds[getFastestConservative()] - 10)
            {
                returnText = "<color=red>" + runner.getComponent<CreatureInfo>().getName() + " is seized, ";
                Entity fastestCon = getFastestConservative();

                switch (fastestCon.def)
                {
                    case "COP":
                        if (MasterController.GetMC().testCondition("LAW:POLICE:>:0"))
                        {
                            returnText += "pushed to the ground, and handcuffed!";
                            runner.getComponent<CriminalRecord>().arrest();
                            MasterController.GetMC().addMessage(runner.getComponent<CreatureInfo>().getName() + " was arrested while fleeing Conservative Swine.");
                        }
                        else
                        {
                            if (runner.getComponent<Body>().Blood <= 10)
                            {
                                returnText += "thrown to the ground, and tazed to death!";
                                runner.getComponent<CreatureBase>().doDie(new Events.Die("was tazed to death by police"));
                            }
                            else
                            {
                                returnText += "thrown to the ground, and tazed repeatedly!";
                                runner.getComponent<Body>().Blood -= 10;
                                runner.getComponent<CriminalRecord>().arrest();
                                MasterController.GetMC().addMessage(runner.getComponent<CreatureInfo>().getName() + " was arrested while fleeing Conservative Swine.");
                            }
                        }
                        
                        conservatives[conservatives.IndexOf(fastestCon)] = null;
                        arrestedThisRound = true;
                        break;
                    case "DEATHSQUAD":
                        returnText += "thrown to the ground, and shot in the head!";
                        runner.getComponent<CreatureBase>().doDie(new Events.Die("was executed by Death Squad"));
                        //Death Squad don't fall behind when executing stragglers - no need to secure a corpse.
                        break;
                    case "TANK":
                        returnText = "<color=red>" + runner.getComponent<CreatureInfo>().getName() + " is crushed beneath the tank's treads!";
                        runner.getComponent<CreatureBase>().doDie(new Events.Die("was run over by a tank"));
                        break;
                    case "GANGUNIT":
                        if (runner.getComponent<Body>().Blood <= 60)
                        {
                            returnText += "thrown to the ground, and beaten to death!";
                            runner.getComponent<CreatureBase>().doDie(new Events.Die("was beaten to death by police"));
                        }
                        else
                        {
                            returnText += "thrown to the ground, and beaten senseless!";
                            runner.getComponent<Body>().Blood -= 60;
                            runner.getComponent<CriminalRecord>().arrest();
                            MasterController.GetMC().addMessage(runner.getComponent<CreatureInfo>().getName() + " was arrested while fleeing Conservative Swine.");
                        }
                        
                        conservatives[conservatives.IndexOf(fastestCon)] = null;
                        arrestedThisRound = true;
                        break;
                    default:
                        if (runner.getComponent<Body>().Blood <= 60)
                        {
                            returnText += "thrown to the ground, and beaten to death!";
                            runner.getComponent<CreatureBase>().doDie(new Events.Die("was beaten to death by pursuers"));
                        }
                        else
                        {
                            returnText += "thrown to the ground, and beaten senseless!";
                            runner.getComponent<Body>().Blood -= 60;
                        }
                        break;
                }

                if (runner.getComponent<Liberal>().hauledUnit != null)
                {
                    chaseRoot.Add(() => 
                    {
                        Entity hauledUnit = runner.getComponent<Liberal>().hauledUnit;
                        Fight.dropHauledUnit(runner);
                        if (hauledUnit.hasComponent<Liberal>() && hauledUnit.getComponent<Body>().Alive)
                        {
                            hauledUnit.getComponent<CriminalRecord>().arrest();
                            mc.addCombatMessage(hauledUnit.getComponent<CreatureInfo>().getName() + " is arrested.");
                        }
                        else
                        {
                            hauledUnit.depersist();
                        }                        
                    }, "arrest hauled unit");
                }
                returnText += "</color>";
                int runnerIndex = liberals.IndexOf(runner);
                liberals[runnerIndex] = null;
            }
            if (returnText != "")
                mc.addCombatMessage(returnText);
            else
                mc.doNextAction();
        }

        private Entity getFastestLiberal()
        {
            Entity theFastest = null;

            foreach (Entity lib in liberals)
            {
                if (lib == null) continue;

                if (theFastest == null) theFastest = lib;
                else if (liberalSpeeds[theFastest] < liberalSpeeds[lib]) theFastest = lib;
            }

            return theFastest;
        }

        private Entity getFastestConservative()
        {
            Entity theFastest = null;

            foreach (Entity con in conservatives)
            {
                if (con == null) continue;

                if (theFastest == null) theFastest = con;
                else if (conservativeSpeeds[theFastest] < conservativeSpeeds[con]) theFastest = con;
            }

            return theFastest;
        }

        private Entity getSlowestLiberal()
        {
            Entity theSlowest = null;

            foreach (Entity lib in liberals)
            {
                if (lib == null) continue;

                if (theSlowest == null) theSlowest = lib;
                else if (liberalSpeeds[theSlowest] > liberalSpeeds[lib]) theSlowest = lib;
            }

            return theSlowest;
        }

        private Entity getSlowestConservative()
        {
            Entity theSlowest = null;

            foreach (Entity con in conservatives)
            {
                if (con == null) continue;

                if (theSlowest == null) theSlowest = con;
                else if (conservativeSpeeds[theSlowest] > conservativeSpeeds[con]) theSlowest = con;
            }

            return theSlowest;
        }
        #endregion

        #region DRIVE
        public void driveEscape()
        {
            int drivingRandomness = 13;

            MasterController mc = MasterController.GetMC();
            mc.nextRound();
            chasePhase = ChasePhase.RUN;

            if (chaseType != ChaseType.SIEGE && chaserType == LocationDef.EnemyType.POLICE && !resistedArrest)
            {
                resistedArrest = true;
                foreach (Entity lib in liberals)
                {
                    if (lib != null)
                        lib.getComponent<CriminalRecord>().addCrime(Constants.CRIME_RESIST);
                }
            }
            int lowest = 10000;
            List<Entity> tempLibCars = new List<Entity>(liberalCarSpeeds.Keys);
            List<Entity> tempConCars = new List<Entity>(conservativeCars);

            foreach(Entity e in tempLibCars)
            {
                liberalCarSpeeds[e] = driveskill(e) + mc.LCSRandom(drivingRandomness);
                e.getComponent<Vehicle>().driver.getComponent<CreatureBase>().Skills[Constants.SKILL_DRIVING].addExperience(20);
                if (liberalCarSpeeds[e] < lowest) lowest = liberalCarSpeeds[e];
            }

            foreach(Entity e in tempConCars)
            {
                conservativeCarSpeeds[e] = driveskill(e) + mc.LCSRandom(drivingRandomness);
            }

            chaseRoot.Add(() =>
            {
                switch (mc.LCSRandom(4))
                {
                    case 0: mc.addCombatMessage("You keep the gas floored!"); break;
                    case 1: mc.addCombatMessage("You swerve around the next corner!"); break;
                    case 2: mc.addCombatMessage("You screech through an empty lot to the next street!"); break;
                    case 3:
                        if(lowest > 15)
                            mc.addCombatMessage("You boldly weave through oncoming traffic!");
                        else
                            mc.addCombatMessage("You make obscene gestures at the pursuers!");
                        break;
                }
            }, "drive description");

            foreach(Entity e in tempConCars)
            {
                Entity libCar = tempLibCars[mc.LCSRandom(tempLibCars.Count)];

                if(conservativeCarSpeeds[e] < liberalCarSpeeds[libCar])
                {
                    chaseRoot.Add(() =>
                    {
                        string result = "<color=cyan>" + e.getComponent<ItemBase>().getName();

                        switch (mc.LCSRandom(liberalCarSpeeds[libCar] / 5))
                        {
                            default: result += " falls behind!"; break;
                            case 1: result += " skids out!"; break;
                            case 2: result += " backs off for safety."; break;
                            case 3: result += " brakes hard and nearly crashes!"; break;
                        }
                        result += "</color>";

                        foreach(Entity con in e.getComponent<Vehicle>().passengers)
                        {
                            conservatives[conservatives.IndexOf(con)] = null;
                        }
                        conservativeCars.Remove(e);

                        mc.addCombatMessage(result);
                    }, "LoseCar");
                }
                else
                {
                    chaseRoot.Add(() =>
                    {
                        mc.addCombatMessage("<color=yellow>" + e.getComponent<ItemBase>().getName() + " is still on your tail!</color>");
                    }, "stillOnTail");
                }
            }
                        
            chaseRoot.Add(() =>
            {
                if (conservativeCars.Count > 0)
                    mc.addCombatMessage("Here they come!");
                else
                    mc.doNextAction();
            }, "stillOnTail");
            

            Fight.theyFight(liberals, conservatives, chaseRoot);
            Fight.youFight(liberals, conservatives, chaseRoot);
            Fight.endOfRound(liberals, conservatives, chaseRoot);
            
            advanceVehicles();
            endCheck();
        }

        public void bail()
        {
            List<Entity> carList = new List<Entity>();

            foreach (Entity e in liberals)
            {
                if (e == null) continue;
                
                if (!carList.Contains(e.getComponent<Inventory>().tempVehicle))
                    carList.Add(e.getComponent<Inventory>().tempVehicle);
                if (e.getComponent<Inventory>().vehicle == e.getComponent<Inventory>().tempVehicle)
                    e.getComponent<Inventory>().vehicle = null;
                e.getComponent<Inventory>().tempVehicle = null;
            }

            foreach(Entity e in carList)
            {
                e.getComponent<ItemBase>().destroyItem();
            }

            changeChaseMode(ChaseType.FOOT);
            
            MasterController.GetMC().addCombatMessage("You bail out and run!");
            if(chasePhase == ChasePhase.SELECTION)
                MasterController.GetMC().uiController.chase.enableInput();
        }

        public void driveObstacleRisky()
        {
            MasterController mc = MasterController.GetMC();
            mc.nextRound();
            chasePhase = ChasePhase.RUN;

            mc.addCombatMessage("You swerve to avoid the obstacle!");
            bool crashed = false;

            foreach(Entity e in liberalCarSpeeds.Keys)
            {
                if(e.getComponent<Vehicle>().driveRoll() < (int)Difficulty.EASY)
                {
                    chaseRoot.Add(() =>
                    {
                        youCrash(e);
                    }, "crash");
                    crashed = true;
                }
            }

            List<Entity> tempConCars = new List<Entity>(conservativeCars);

            foreach(Entity e in tempConCars)
            {
                if(e.getComponent<Vehicle>().driveRoll() < (int)Difficulty.EASY)
                {
                    chaseRoot.Add(() =>
                    {
                        theyCrash(e);
                    }, "crash");
                }
            }

            if (crashed)
            {
                chaseRoot.Add(() =>
                {
                    bool livingSquad = false;
                    foreach (Entity e in liberals)
                    {
                        if (e == null) continue;

                        if (e.getComponent<Body>().Alive)
                        {
                            livingSquad = true;
                            break;
                        }
                    }

                    if (livingSquad)
                        bail();
                }, "bailSurvivors");
            }

            advanceVehicles();
            endCheck();
        }

        public void driveObstacleSafe()
        {
            MasterController mc = MasterController.GetMC();
            mc.nextRound();
            chasePhase = ChasePhase.RUN;
            switch (obstacle)
            {
                case ObstacleType.REDLIGHT:
                    chaseRoot.Add(() =>
                    {
                        mc.addCombatMessage("You slow down, and turn the corner.");
                    }, "drivetext");

                    if(mc.LCSRandom(3) == 0)
                    {
                        chaseRoot.Add(() =>
                        {
                            if (conservativeCars.Count > 0)
                                mc.addCombatMessage("Here they come!");
                            else
                                mc.doNextAction();
                        }, "stillOnTail");

                        Fight.theyFight(liberals, conservatives, chaseRoot);
                        Fight.youFight(liberals, conservatives, chaseRoot);
                    }
                    break;
                case ObstacleType.FRUITSTAND:
                    chaseRoot.Add(() =>
                    {
                        mc.addCombatMessage("Fruit smashes all over the windshield!");
                    }, "drivetext");

                    if(mc.LCSRandom(5) == 0)
                    {
                        mc.addCombatMessage("<color=red>The fruit seller is squashed!</color>");
                        foreach(Entity e in liberals)
                        {
                            if (e == null) continue;
                                 
                            e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_MURDER);
                        }
                    }
                    break;
                case ObstacleType.TRUCK:
                    chaseRoot.Add(() =>
                    {
                        mc.addCombatMessage("You slow down, and carefully evade the truck.");
                    }, "drivetext");

                    if (mc.LCSRandom(3) == 0)
                    {
                        chaseRoot.Add(() =>
                        {
                            if (conservativeCars.Count > 0)
                                mc.addCombatMessage("Here they come!");
                            else
                                mc.doNextAction();
                        }, "stillOnTail");

                        Fight.theyFight(liberals, conservatives, chaseRoot);
                        Fight.youFight(liberals, conservatives, chaseRoot);
                    }
                    break;
                case ObstacleType.CHILD:
                    chaseRoot.Add(() =>
                    {
                        mc.addCombatMessage("You slow down and carefully avoid the kid.");
                    }, "drivetext");
                    /*TODO: Maybe make this depend on what kind of people are chasing you? Less moral = more chance to fire
                    morality:
                    CCS < DEATHSQUAD < MERCS < GANGMEMBERS = AGENTS < REDNECKS < ARMY = POLICE
                    */
                    if (mc.LCSRandom(3) == 0)
                    {
                        chaseRoot.Add(() =>
                        {
                            if (conservativeCars.Count > 0)
                                mc.addCombatMessage("<color=red>The Conservative bastards unleash a hail of gunfire!</color>");
                            else
                                mc.doNextAction();
                        }, "stillOnTail");

                        Fight.theyFight(liberals, conservatives, chaseRoot);
                        Fight.youFight(liberals, conservatives, chaseRoot);
                    }
                    else
                    {
                        chaseRoot.Add(() => 
                        {
                            mc.addCombatMessage("<color=lime>Both sides refrain from endangering the child...</color>");
                        }, "combatText");
                    }
                    break;
            }
                        
            Fight.endOfRound(liberals, conservatives, chaseRoot);

            advanceVehicles();
            endCheck();

            mc.doNextAction();
        }

        private int driveskill(Entity vehicle)
        {
            Vehicle v = vehicle.getComponent<Vehicle>();

            int driveskill = v.driveRoll();
            driveskill -= v.driver.getComponent<Body>().healthModRoll();
            if (driveskill < 0) driveskill = 0;
            driveskill *= (int)(v.driver.getComponent<Body>().Blood / 50f);
            return driveskill;
        }
        #endregion

        private void changeChaseMode(ChaseType newMode)
        {
            chaseType = newMode;
            //HACK: Closing and opening again will reset the display but it's a bit awkward
            MasterController.GetMC().uiController.enemyUI.close();
            switch (newMode)
            {
                case ChaseType.FOOT:
                    MasterController.GetMC().combatModifiers |= MasterController.CombatModifiers.CHASE_FOOT;
                    MasterController.GetMC().combatModifiers &= ~MasterController.CombatModifiers.CHASE_CAR;
                    MasterController.GetMC().uiController.enemyUI.displaySquad(conservatives);
                    break;
                case ChaseType.CAR:
                    MasterController.GetMC().combatModifiers |= MasterController.CombatModifiers.CHASE_CAR;
                    MasterController.GetMC().combatModifiers &= ~MasterController.CombatModifiers.CHASE_FOOT;
                    MasterController.GetMC().uiController.enemyUI.displayDriving(conservativeCars);
                    break;
            }
        }

        public void fight()
        {
            MasterController mc = MasterController.GetMC();
            mc.nextRound();
            chasePhase = ChasePhase.FIGHT;

            if (chaseType != ChaseType.SIEGE && chaserType == LocationDef.EnemyType.POLICE && !resistedArrest)
            {
                resistedArrest = true;
                foreach (Entity lib in liberals)
                {
                    lib.getComponent<CriminalRecord>().addCrime(Constants.CRIME_RESIST);
                }
            }
            if (!attacked)
            {
                attacked = true;
                foreach (Entity lib in liberals)
                {
                    if (lib == null) continue;
                    if (lib.getComponent<Inventory>().weapon == null)
                        lib.getComponent<CriminalRecord>().addCrime(Constants.CRIME_ASSAULT);
                    else
                        lib.getComponent<CriminalRecord>().addCrime(Constants.CRIME_ARMED_ASSAULT);
                }

            }
            arrestedThisRound = false;
            Fight.fight(liberals, conservatives, chaseRoot);

            siegeBufferCheck();
            advanceVehicles();
            endCheck();
            MasterController.GetMC().doNextAction();
        }

        public void surrender()
        {
            MasterController mc = MasterController.GetMC();
            mc.addCombatMessage("You stop and are arrested.");

            if (chaseType == ChaseType.CAR)
            {
                List<Entity> carList = new List<Entity>();

                foreach (Entity e in liberals)
                {
                    if (e == null) continue;

                    if (!carList.Contains(e.getComponent<Inventory>().tempVehicle))
                        carList.Add(e.getComponent<Inventory>().tempVehicle);
                    if (e.getComponent<Inventory>().vehicle == e.getComponent<Inventory>().tempVehicle)
                        e.getComponent<Inventory>().vehicle = null;
                    e.getComponent<Inventory>().tempVehicle = null;
                }

                foreach (Entity e in carList)
                {
                    e.getComponent<ItemBase>().destroyItem();
                }
            }

            foreach (Entity e in liberals)
            {
                if (e != null)
                {
                    liberals[liberals.IndexOf(e)] = null;
                    e.getComponent<CriminalRecord>().arrest();
                }
            }

            chasePhase = ChasePhase.COMPLETE;
            mc.uiController.chase.enableInput();
        }
        
        private void advanceVehicles()
        {
            if (chaseType != ChaseType.CAR) return;

            MasterController mc = MasterController.GetMC();

            bool crashed = false;

            foreach(Entity e in liberals)
            {
                if (e == null) continue;
                chaseRoot.Add(() =>
                {
                    Entity vehicle = e.getComponent<Inventory>().tempVehicle;

                    if((e.getComponent<Body>().Alive && e.getComponent<Body>().canWalk()) ||
                    vehicle.getComponent<Vehicle>().driver != e)
                    {
                        mc.doNextAction();
                    }
                    else
                    {
                        bool foundDriver = false;
                        List<Entity> livingPassengers = new List<Entity>();
                        foreach(Entity p in vehicle.getComponent<Vehicle>().passengers)
                        {
                            if(p.getComponent<Body>().Alive)
                            {
                                livingPassengers.Add(p);
                                if (p.getComponent<Body>().canWalk())
                                {
                                    vehicle.getComponent<Vehicle>().driver = p;
                                    mc.addCombatMessage("<color=yellow>" + p.getComponent<CreatureInfo>().getName() + " takes the wheel</color>");
                                    foundDriver = true;
                                    break;
                                }
                            }
                        }

                        if (!foundDriver)
                        {
                            if (livingPassengers.Count > 0 &&
                            livingPassengers[mc.LCSRandom(livingPassengers.Count)].getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].check(Difficulty.HEROIC))
                            {
                                mc.addCombatMessage("<color=lime>JESUS takes the wheel</color>");
                            }
                            else
                            {
                                youCrash(vehicle);
                                crashed = true;
                            }
                        }
                    }
                }, "libDriverCheck");
            }

            List<Entity> tempConCars = new List<Entity>(conservativeCars);

            foreach(Entity e in tempConCars)
            {
                chaseRoot.Add(() =>
                {
                    if (!e.getComponent<Vehicle>().driver.getComponent<Body>().Alive ||
                        !e.getComponent<Vehicle>().driver.getComponent<Body>().canWalk())
                    {
                        theyCrash(e);
                    }
                    else
                    {
                        mc.doNextAction();
                    }
                }, "crashcheck");
            }

            chaseRoot.Add(() =>
            {
                if (crashed)
                {
                    bool livingSquad = false;
                    foreach (Entity e in liberals)
                    {
                        if (e == null) continue;

                        if (e.getComponent<Body>().Alive)
                        {
                            livingSquad = true;
                            break;
                        }
                    }

                    if (livingSquad)
                        bail();
                }            
                else
                {
                    if (obstacle != ObstacleType.NONE)
                        obstacle = ObstacleType.NONE;
                    else
                    {
                        if (mc.LCSRandom(3) == 0 && liberals.Count(i => i != null && i.getComponent<Liberal>().status == Liberal.Status.ACTIVE && i.getComponent<Body>().Alive) > 0 && conservatives.Count(i => i != null && i.getComponent<Body>().Alive && i.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE) > 0)
                        {
                            switch (mc.LCSRandom(4))
                            {
                                case 0:
                                    obstacle = ObstacleType.FRUITSTAND;
                                    mc.addCombatMessage("You are speeding towards a flimsy fruit stand!", true);
                                    break;
                                case 1:
                                    obstacle = ObstacleType.REDLIGHT;
                                    mc.addCombatMessage("There's a red light with cross traffic ahead!", true);
                                    break;
                                case 2:
                                    obstacle = ObstacleType.TRUCK;
                                    mc.addCombatMessage("A truck pulls out into your path!", true);
                                    break;
                                case 3:
                                    obstacle = ObstacleType.CHILD;
                                    mc.addCombatMessage("A kid runs into the street for his ball!", true);
                                    break;
                            }
                        }
                        else
                        {
                            obstacle = ObstacleType.NONE;
                        }
                    }
                }
                mc.doNextAction();
            }, "bailSurvivors/obstaclecheck");
        }

        private void youCrash(Entity vehicle)
        {
            MasterController mc = MasterController.GetMC();
            vehicle.getComponent<ItemBase>().destroyItem();

            string message = "<color=magenta>Your ";
            message += vehicle.getComponent<ItemBase>().getName() + " ";
            switch (mc.LCSRandom(3))
            {
                case 0: message += "slams into a building!"; break;
                case 1: message += "spins out and crashes!"; break;
                case 2: message += "hits a parked car and flips over!"; break;
            }
            message += "</color>";
            mc.addCombatMessage(message);

            foreach(Entity e in vehicle.getComponent<Vehicle>().passengers)
            {
                foreach(Body.BodyPart part in e.getComponent<Body>().BodyParts)
                {
                    if (!part.isSevered())
                    {
                        if(mc.LCSRandom(2) == 0)
                        {
                            part.Health |= Body.BodyPart.Damage.TEAR | Body.BodyPart.Damage.BLEEDING;
                            e.getComponent<Body>().Blood -= 1 + mc.LCSRandom(25);
                        }
                        if(mc.LCSRandom(3) == 0)
                        {
                            part.Health |= Body.BodyPart.Damage.CUT | Body.BodyPart.Damage.BLEEDING;
                            e.getComponent<Body>().Blood -= 1 + mc.LCSRandom(25);
                        }
                        if (mc.LCSRandom(2) == 0 || part.Health == Body.BodyPart.Damage.FINE)
                        {
                            part.Health |= Body.BodyPart.Damage.BRUISE;
                            e.getComponent<Body>().Blood -= 1 + mc.LCSRandom(10);
                        }
                    }
                }

                if(e.getComponent<Liberal>().hauledUnit != null)
                {
                    e.getComponent<Liberal>().hauledUnit.getComponent<CreatureBase>().doDie(new Events.Die("died in a car crash"));
                    if(!e.getComponent<Liberal>().hauledUnit.hasComponent<Liberal>())
                        message = "<color=red>"  + e.getComponent<Liberal>().hauledUnit.getComponent<CreatureInfo>().encounterName;
                    else
                        message = "<color=red>" + e.getComponent<Liberal>().hauledUnit.getComponent<CreatureInfo>().getName();
                    switch (mc.LCSRandom(3))
                    {
                        case 0: message += " is crushed inside the car."; break;
                        case 1: message += "'s lifeless body smashes through the windshield."; break;
                        case 2: message += " is thrown from the car and killed instantly."; break;
                    }
                    message += "</color>";
                    mc.addCombatMessage(message);
                }

                if(e.getComponent<Body>().Blood <= 0 && e.getComponent<Body>().Alive)
                {
                    e.getComponent<CreatureBase>().doDie(new Events.Die("died in a car crash"));
                    if(liberals.Contains(e)) liberals[liberals.IndexOf(e)] = null;
                    message = "<color=red>" + e.getComponent<CreatureInfo>().getName();
                    switch (mc.LCSRandom(3))
                    {
                        case 0: message += " slumps in " + e.getComponent<CreatureInfo>().hisHer().ToLower() + " seat, out cold, and dies."; break;
                        case 1: message += " is crushed by the impact."; break;
                        case 2: message += " struggles free of the car, then collapses lifelessly."; break;
                    }
                    message += "</color>";
                    mc.addCombatMessage(message);
                }
                else if (e.getComponent<Body>().Alive)
                {
                    message = "<color=yellow>" + e.getComponent<CreatureInfo>().getName();
                    switch (mc.LCSRandom(3))
                    {
                        case 0:
                            message += " grips the ";
                            if (e.getComponent<Inventory>().weapon != null)
                                message += e.getComponent<Inventory>().weapon.getComponent<ItemBase>().getName(true);
                            else
                                message += "car frame";
                            message += " and struggles to " + e.getComponent<CreatureInfo>().hisHer().ToLower();
                            if (e.getComponent<Body>().canWalk())
                                message += " feet.";
                            else if ((e.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.WHEELCHAIR) != 0)
                                message += " wheelchair.";
                            else
                                message += " crawl from the wreckage.";                                
                            break;
                        case 1: message += " gasps in pain, but lives, for now."; break;
                        case 2: message += "crawls free of the car, shivering with pain."; break;
                    }
                    message += "</color>";
                    mc.addCombatMessage(message);
                }
            }
        }

        private void theyCrash(Entity vehicle)
        {
            MasterController mc = MasterController.GetMC();
            string message = "<color=cyan>The ";
            message += vehicle.getComponent<ItemBase>().getName() + " ";
            switch (mc.LCSRandom(3))
            {
                case 0: message += "slams into a building!"; break;
                case 1: message += "spins out and crashes!"; break;
                case 2: message += "hits a parked car and flips over!"; break;
            }
            message += "</color>";
            foreach (Entity e in vehicle.getComponent<Vehicle>().passengers)
                e.getComponent<CreatureBase>().doDie(new Events.Die("died in a car crash"));

            conservativeCars.Remove(vehicle);

            mc.addCombatMessage(message);
        }

        private void siegeBufferCheck()
        {
            if (chaseType == ChaseType.SIEGE)
            {
                chaseRoot.Add(() =>
                {
                    if (siegeBuffer.Count > 0 && liberals.Count(l => l != null) < 6)
                    {
                        for (int i = 0; i < liberals.Count; i++)
                        {
                            if (liberals[i] == null && siegeBuffer.Count > 0)
                            {
                                Entity newLib = siegeBuffer[0];
                                liberals[i] = newLib;
                                siegeBuffer.Remove(newLib);
                                MasterController.GetMC().addCombatMessage(newLib.getComponent<CreatureInfo>().getName() + " joins the fight");
                            }
                        }
                    }
                    else
                    {
                        MasterController.GetMC().doNextAction();
                    }
                }, "siege buffer check");
            }
        }

        private void endCheck()
        {
            MasterController mc = MasterController.GetMC();

            chaseRoot.Add(() =>
            {
                if (liberals.Count(i => i != null && i.getComponent<Liberal>().status == Liberal.Status.ACTIVE && i.getComponent<Body>().Alive) + siegeBuffer.Count > 0 && conservatives.Count(i => i != null && i.getComponent<Body>().Alive && i.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE) > 0)
                {
                    chasePhase = ChasePhase.SELECTION;
                    mc.uiController.chase.enableInput();
                }
                else
                {
                    //If this is a sally forth during a siege, handle the cleanup in the queued endSiege instead as it has the safehouse recorded
                    if (chaseType == ChaseType.SIEGE)
                    {
                        mc.doNextAction();
                        return;
                    }

                    if (liberals.Count(i => i != null && i.getComponent<Liberal>().status == Liberal.Status.ACTIVE && i.getComponent<Body>().Alive) > 0)
                    {
                        chasePhase = ChasePhase.COMPLETE;
                        if (!arrestedThisRound)
                            mc.addCombatMessage("<color=cyan>Looks like you lost them!</color>");
                        else
                            mc.addCombatMessage("<color=cyan>But the rest manage to escape!</color>");
                        mc.uiController.chase.enableInput();
                    }
                    else
                    {
                        chasePhase = ChasePhase.COMPLETE;
                        if (escapedLibs > 0)
                            mc.addCombatMessage("<color=cyan>But the rest managed to escape!</color>");
                        else
                        {
                            List<UI.PopupOption> ok = new List<UI.PopupOption>();
                            ok.Add(new UI.PopupOption("Reflect on your Conservative ineptitude...", mc.doNextAction));
                            mc.uiController.showOptionPopup("The Entire Squad has been eliminated.", ok);
                        }
                        mc.uiController.chase.enableInput();
                    }
                }
            }, "end chase check");
        }

        private void endSiege(Entity siegeLocation)
        {
            MasterController mc = MasterController.GetMC();

            if (liberals.Count(i => i != null) > 0 && chasePhase == ChasePhase.FIGHT)
            {
                mc.addMessage("The siege is broken!", true);
                if (siegeLocation.getComponent<SafeHouse>().siegeType == LocationDef.EnemyType.POLICE)
                {
                    mc.addMessage("The Conservative automatons have been driven back for the time being. While they are regrouping, you might consider abandoning this safe house for a safer location.", true);
                    MasterController.news.currentStory.type = "SQUAD_BROKESIEGE";
                }
                else
                    mc.addMessage("The Conservative automatons have been driven back. Unfortunately, you will never truly be safe from this filth until the Liberal Agenda is realized.", true);

                
                siegeLocation.getComponent<SafeHouse>().underSiege = false;
                siegeLocation.getComponent<SafeHouse>().underAttack = false;
                if (siegeLocation.getComponent<SafeHouse>().siegeType == LocationDef.EnemyType.POLICE)
                {
                    siegeLocation.getComponent<SafeHouse>().escalation++;
                    siegeLocation.getComponent<SafeHouse>().timeUntilLocated = 4 + mc.LCSRandom(4);
                }
            }
            else if((liberals.Count(i => i != null) > 0 && chasePhase == ChasePhase.RUN) || escapedLibs > 0)
            {
                mc.addMessage("You have escaped!\nThe Conservatives thought that the Liberal Crime Squad was finished, but once again, Conservative Thinking has proven itself to be based on Unsound Notions.\nHowever, all is not well. In your haste to escape you have lost everything that you've left behind. You'll have to start from scratch in a new safe house. Your funds remain under your control, fortunately.\nYour flight has given you some time to regroup, but the Conservatives will doubtless be preparing another assault.", true);

                Entity lootLocation = null;

                if (!siegeLocation.getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER").getComponent<SafeHouse>().underSiege &&
                    siegeLocation != siegeLocation.getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER"))
                {
                    lootLocation = siegeLocation.getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER");
                }
                else
                {
                    foreach (Entity e in siegeLocation.getComponent<SiteBase>().city.getComponent<City>().getAllBases(true))
                    {
                        if (e == siegeLocation) continue;
                        if (!e.getComponent<SafeHouse>().underSiege)
                        {
                            lootLocation = e;
                            break;
                        }
                    }
                }

                if (lootLocation != null)
                {
                    foreach (Entity e in squad.inventory)
                    {
                        e.getComponent<ItemBase>().moveItem(lootLocation);
                    }
                }
                else
                {
                    //If there is no safehouse to stash recovered items, they are all lost
                    foreach (Entity e in squad.inventory)
                    {
                        e.depersist();
                    }
                }
                squad.inventory.Clear();

                foreach (Entity e in liberals)
                {
                    if (e == null) continue;
                    e.getComponent<Liberal>().status = Liberal.Status.AWAY;
                    e.getComponent<Liberal>().awayTime = 2 + mc.LCSRandom(3);
                    if(e.getComponent<Liberal>().squad != null)
                        e.getComponent<Liberal>().squad.Remove(e);
                }

                siegeLocation.getComponent<SafeHouse>().giveUpSiege();
                mc.doNextAction();
            }
            else
            {
                if (siegeLocation.getComponent<SafeHouse>().siegeType == LocationDef.EnemyType.POLICE)
                    MasterController.news.currentStory.type = "SQUAD_KILLED_SIEGEESCAPE";

                siegeLocation.getComponent<SafeHouse>().giveUpSiege();
                List<UI.PopupOption> ok = new List<UI.PopupOption>();
                ok.Add(new UI.PopupOption("Reflect on your Conservative ineptitude...", mc.doNextAction));
                mc.uiController.showOptionPopup("The Entire Squad has been eliminated.", ok);
            }

            chasePhase = ChasePhase.COMPLETE;

            mc.addAction(() =>
            {
                endChase();
            }, "next action");
        }

        private void endChase()
        {
            MasterController mc = MasterController.GetMC();
            activityChase = false;

            //If this is a car chase, anyone killed along the way will be brought home (so long as someone survived)
            //TODO: Make this more specific to individual cars
            if (liberals.Count(i => i != null && i.getComponent<Liberal>().status == Liberal.Status.ACTIVE && i.getComponent<Body>().Alive) > 0 && chaseType == ChaseType.CAR)
            {
                foreach(Entity e in liberals)
                {
                    if (e == null) continue;

                    if (!e.getComponent<Body>().Alive)
                    {
                        e.persist();
                        e.getComponent<CreatureBase>().Location = e.getComponent<Liberal>().homeBase;

                        if(e.getComponent<Liberal>().hauledUnit != null)
                        {
                            Entity hauledUnit = e.getComponent<Liberal>().hauledUnit;
                            if (!e.getComponent<Liberal>().hauledUnit.hasComponent<Liberal>())
                            {
                                e.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().addNewHostage(hauledUnit);
                            }
                            else
                            {
                                hauledUnit.getComponent<CreatureBase>().Location = e.getComponent<Liberal>().homeBase;
                            }
                            hauledUnit.persist();
                        }
                    }
                }
            }

            mc.endEncounter();
            mc.uiController.closeUI();
            mc.doNextAction();
        }
    }
}
