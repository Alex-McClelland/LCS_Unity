using System;
using System.Collections.Generic;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Item;
using LCS.Engine.Scenes;
using LCS.Engine.Data;
using System.Xml;

namespace LCS.Engine.Components.Location
{
    public class SafeHouse : Component
    {
        [Flags]
        public enum Investments
        {
            NONE = 0,
            FORTIFIED = 1,
            CAMERAS = 2,
            TRAPS = 4,
            TANK_TRAPS = 8,
            GENERATOR = 16,
            AAGUN = 32,
            PRINTING_PRESS = 64,
            BUSINESS_FRONT = 128,
            FLAG = 256
        }

        public enum SiegeEscalation
        {
            POLICE,
            NATIONAL_GUARD,
            TANKS,
            BOMBERS
        }

        [SimpleSave]
        public int heat;
        [SimpleSave]
        public int food;
        [SimpleSave]
        public bool owned, freeRent, forceEvict;
        [SimpleSave]
        public Investments investments;
        [SimpleSave]
        public bool underSiege, underAttack;
        [SimpleSave]
        public float floatingHeat;
        [SimpleSave]
        public int timeUntilLocated, timeUntilMercs, timeUntilAgents, timeUntilFiremen, timeUntilCCS;
        [SimpleSave]
        public SiegeEscalation escalation;
        [SimpleSave]
        public bool lightsOff, camerasOff;
        [SimpleSave]
        public LocationDef.EnemyType siegeType;

        public SafeHouse()
        {
            investments = 0;
            food = 0;
            heat = 0;
            floatingHeat = 0;
            timeUntilLocated = -1;
            timeUntilMercs = -1;
            timeUntilAgents = -1;
            timeUntilFiremen = -1;
            timeUntilCCS = -1;
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("SafeHouse");
                entityNode.AppendChild(saveNode);
            }

            saveSimpleFields();
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);
        }

        public override void selfSubscribe()
        {
            base.selfSubscribe();
            getComponent<SiteBase>().dropItem += doDropItem;
        }

        public override void subscribe()
        {
            base.subscribe();
            MasterController.GetMC().nextDay += doSiege;
            MasterController.GetMC().nextDay += doHeat;
            MasterController.GetMC().nextMonth += doRent;
        }

        public override void unsubscribe()
        {
            base.unsubscribe();
            MasterController.GetMC().nextDay -= doSiege;
            MasterController.GetMC().nextDay -= doHeat;
            MasterController.GetMC().nextMonth -= doRent;
            getComponent<SiteBase>().dropItem -= doDropItem;
        }

        public List<Entity> getInventory()
        {
            MasterController mc = MasterController.GetMC();
            List<Entity> result = new List<Entity>();

            foreach(Entity e in mc.PersistentEntityList.Values)
            {
                if (!e.hasComponent<ItemBase>())
                    continue;

                if (e.getComponent<ItemBase>().Location == owner)
                    result.Add(e);
            }

            result.Sort((Entity a, Entity b) => { return a.def.CompareTo(b.def); });

            return result;
        }

        public List<Entity> getBodies()
        {
            MasterController mc = MasterController.GetMC();
            List<Entity> result = new List<Entity>();

            foreach (Entity e in mc.PersistentEntityList.Values)
            {
                if (!e.hasComponent<CreatureBase>())
                    continue;

                if (e.getComponent<CreatureBase>().Location == owner && !e.getComponent<Body>().Alive)
                    result.Add(e);
            }

            return result;
        }

        public List<Entity> getHostages()
        {
            MasterController mc = MasterController.GetMC();
            List<Entity> result = new List<Entity>();

            foreach (Entity e in mc.PersistentEntityList.Values)
            {
                if (!e.hasComponent<CreatureBase>())
                    continue;

                if (e.getComponent<CreatureBase>().Location == owner && e.hasComponent<Hostage>())
                    result.Add(e);
            }

            return result;
        }

        public void exposeBase()
        {
            floatingHeat += 300;
            heat += 300;
        }

        public void evict()
        {
            Entity shelter = getComponent<SiteBase>().city.getComponent<City>().getLocation("RESIDENTIAL_SHELTER");

            foreach (Entity e in getInventory())
            {
                e.getComponent<ItemBase>().moveItem(shelter);
            }

            foreach (Entity e in getBasedLiberals())
            {
                shelter.getComponent<SafeHouse>().moveLiberalHere(e);
                if (e.getComponent<Liberal>().squad != null)
                    e.getComponent<Liberal>().squad.homeBase = shelter;
            }

            owned = false;
            freeRent = false;
            forceEvict = false;
        }

        public void doRent(object sender, EventArgs args)
        {
            MasterController mc = MasterController.GetMC();

            if (!owned || (freeRent && !forceEvict)) return;

            if(MasterController.lcs.Money >= getRentPrice() && !forceEvict)
                MasterController.lcs.changeFunds(-getRentPrice());
            else
            {
                if (!underSiege)
                {
                    mc.addMessage("<b><color=red>EVICTION NOTICE:</color></b> " + getComponent<SiteBase>().getCurrentName() + ".\nPosessions go to the shelter.", true);
                    evict();
                }
                else
                {
                    //If you couldn't afford rent but couldn't leave due to being under siege, the landlord will kick you out next month.
                    forceEvict = true;
                }
            }
        }

        public void doHeat(object sender, EventArgs args)
        {
            //Libs inside bases under siege can't bleed off heat, and building up heat is pointless if it's already sieged
            if (underSiege) return;

            int crimes = 0;
            int trueCrimes = 0;

            crimes += 10 * getBodies().Count;

            foreach(Entity e in getHostages())
            {
                crimes += 5 * e.getComponent<Hostage>().timeInCaptivity;
            }

            trueCrimes = crimes;

            foreach(Entity e in getBasedLiberals())
            {
                //Liberals laying low contribute less heat to the safehouse
                if(e.getComponent<Liberal>().dailyActivity.type == "NONE")
                    crimes += Math.Max(0, e.getComponent<CriminalRecord>().Heat / 10 - 10)/2;
                else
                    crimes += Math.Max(0, e.getComponent<CriminalRecord>().Heat / 10 - 10);
                trueCrimes += e.getComponent<CriminalRecord>().Heat;
            }

            //Reduce heat quickly if nobody is present (or if those present have 0 heat)
            if(trueCrimes == 0)
            {
                floatingHeat -= 5;
                if (floatingHeat < 0) floatingHeat = 0;
            }

            //floatingHeat allows heat to reduce in increments smaller than 1 if maxHeat has dropped below 10.
            //int maxHeat = (int) (crimes * ((100 - getSecrecy()) / 100f));
            floatingHeat += (crimes - floatingHeat) / 10;
            
            heat = (int)floatingHeat;
        }

        public void doSiege(object sender, EventArgs args)
        {
            if (!MasterController.GetMC().canSeeThings) return;

            MasterController mc = MasterController.GetMC();
            advanceSiege();

            if(getComponent<SiteBase>().city.getComponent<City>().getLocation("GOVERNMENT_POLICE_STATION").getComponent<TroubleSpot>().closed > 0)
            {
                floatingHeat *= 0.95f;
                heat = (int)floatingHeat;
                return;
            }

            if (underSiege) return;
            if (!owned) return;

            //Start planning a siege if heat is high
            if(heat > getSecrecy() && mc.LCSRandom(500) < heat && timeUntilLocated < 0)
            {
                timeUntilLocated = 2 + mc.LCSRandom(6);
            }

            if(timeUntilLocated == -2)
            {
                timeUntilLocated = -1;
            }
            else
            {
                if(timeUntilLocated > 0)
                {
                    if((investments & Investments.BUSINESS_FRONT) == 0 || mc.LCSRandom(2) == 0)
                    {
                        timeUntilLocated--;
                        if(heat > 100)
                        {
                            int huntspeed = heat / 50;
                            while(huntspeed > 0 && timeUntilLocated > 1)
                            {
                                timeUntilLocated--;
                                huntspeed--;
                            }
                        }
                    }

                    if(timeUntilLocated == 1)
                    {
                        bool alreadyWarned = false;

                        foreach(Entity e in MasterController.lcs.getAllSleepers())
                        {
                            if(e.getComponent<Liberal>().homeBase.def == "GOVERNMENT_POLICE_STATION" && !alreadyWarned)
                            {
                                mc.addMessage("You have recieved warning from your sleepers about an imminent police raid on " + getComponent<SiteBase>().getCurrentName() + ".", true);
                                alreadyWarned = true;
                            }
                        }
                    }

                    if(timeUntilLocated == 0)
                    {
                        timeUntilLocated = -2;
                        heat = 0;
                        floatingHeat = 0;

                        if(getBasedLiberals().Count > 0)
                        {
                            string siegeString = "The police have surrounded " + getComponent<SiteBase>().getCurrentName() + "!";
                            if (escalation >= SiegeEscalation.NATIONAL_GUARD)
                            {
                                siegeString += "\nNational Guard troops are replacing normal SWAT units.";
                            }
                            if (escalation >= SiegeEscalation.TANKS) {
                                if ((investments & Investments.TANK_TRAPS) != 0)
                                    siegeString += "\nAn M1 Abrams Tank is stopped by the tank traps.";
                                else
                                    siegeString += "\nAn M1 Abrams Tank takes up position outside the compound.";
                            }
                            if (escalation >= SiegeEscalation.BOMBERS)
                            {
                                siegeString += "\nYou hear jet bombers streak overhead.";
                            }

                            //TODO: State broken laws

                            mc.addMessage(siegeString, true);

                            underSiege = true;
                            siegeType = LocationDef.EnemyType.POLICE;
                            lightsOff = false;
                            camerasOff = false;
                        }
                        else
                        {
                            raidUnoccupiedSafehouse(LocationDef.EnemyType.POLICE);
                        }
                    }
                }
            }

            bool targetIntersting = MasterController.ccs.status >= ConservativeCrimeSquad.Status.SIEGE || (investments & Investments.PRINTING_PRESS) != 0;

            if (targetIntersting && MasterController.ccs.status >= ConservativeCrimeSquad.Status.ACTIVE)
            {
                if (heat > 0 && timeUntilCCS == -1 && !underSiege && mc.LCSRandom(40 + Math.Max(0, getSecrecy() - heat)) == 0 && getBasedLiberals().Count > 0)
                {
                    timeUntilCCS = mc.LCSRandom(3) + 1;

                    bool hasSleeperCCS = false;
                    foreach (Entity e in MasterController.lcs.getAllSleepers())
                    {
                        if ((e.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.CCS) != 0)
                        {
                            hasSleeperCCS = true;
                            break;
                        }
                    }

                    if(hasSleeperCCS)
                    {
                        string message = "You have recieved a sleeper warning that the CCS is gearing up to attack ";
                        message += getComponent<SiteBase>().getCurrentName();
                        message += ".";
                        mc.addMessage(message, true);
                    }
                }
                else if(timeUntilCCS > 0)
                {
                    timeUntilCCS--;
                }
                else if(timeUntilCCS == 0 && !underSiege && getBasedLiberals().Count > 0)
                {
                    timeUntilCCS = -1;

                    string message = "A screeching truck pulls up to " + getComponent<SiteBase>().getCurrentName() + "!";

                    if(mc.LCSRandom(5) == 0 && (investments & Investments.TANK_TRAPS) == 0)
                    {
                        message += "\n<color=red>The truck plows into the building and explodes!</color>";

                        int killed = 0;
                        int injured = 0;

                        foreach(Entity e in getBasedLiberals())
                        {
                            if(mc.LCSRandom(2) == 0)
                            {
                                e.getComponent<Body>().Blood -= mc.LCSRandom(101 - e.getComponent<CreatureBase>().Juice) + 10;

                                if(e.getComponent<Body>().Blood <= 0)
                                {
                                    killed++;
                                    e.getComponent<CreatureBase>().doDie(new Events.Die("was killed by a car bomb"));
                                }
                                else
                                {
                                    injured++;
                                }
                            }
                        }

                        if (killed > 0) message += "\nKILLED: " + killed;
                        if (injured > 0) message += "\nINJURED: " + injured;
                    }
                    else
                    {
                        message += "\n<color=red>CCS members pour out of the truck and shoot in the front doors!</color>";

                        underSiege = true;
                        underAttack = true;
                        siegeType = LocationDef.EnemyType.CCS;
                        lightsOff = false;
                        camerasOff = false;
                    }

                    mc.addMessage(message, true);
                }
                //Silently call off failed CCS raids
                else if (timeUntilCCS == 0) timeUntilCCS = -1;
            }

            if (heat > 0 && timeUntilMercs == -1 && !underSiege && MasterController.lcs.offendedCorps && mc.LCSRandom(600) == 0 && getBasedLiberals().Count > 0)
            {
                timeUntilMercs = mc.LCSRandom(3) + 1;

                bool hasSleeperCEO = false;

                foreach (Entity e in MasterController.lcs.getAllSleepers())
                {
                    if (e.def == "CORPORATE_CEO")
                    {
                        hasSleeperCEO = true;
                        break;
                    }
                }

                if (hasSleeperCEO || mc.LCSRandom(5) == 0)
                {
                    string message = "You have recieved ";
                    if (hasSleeperCEO) message += "your sleeper CEO's warning";
                    else message += "an anonymous tip";
                    message += " that the Corporations are hiring mercenaries to attack ";
                    if (hasSleeperCEO) message += getComponent<SiteBase>().getCurrentName();
                    else message += "the LCS";
                    message += ".";
                    mc.addMessage(message, true);
                }
            }
            else if (timeUntilMercs > 0)
            {
                timeUntilMercs--;
            }
            else if (timeUntilMercs == 0 && !underSiege && MasterController.lcs.offendedCorps && getBasedLiberals().Count > 0)
            {
                timeUntilMercs = -1;

                mc.addMessage("Corporate mercenaries are raiding the " + getComponent<SiteBase>().getCurrentName() + "!", true);

                underSiege = true;
                underAttack = true;
                siegeType = LocationDef.EnemyType.MERC;
                lightsOff = false;
                camerasOff = false;
                MasterController.lcs.offendedCorps = false;
            }
            //Silently call off failed corporate raids
            else if (timeUntilMercs == 0) timeUntilMercs = -1;

            if (heat > 0 && timeUntilAgents == -1 && !underSiege && MasterController.lcs.offendedCIA && mc.LCSRandom(300) == 0 && getBasedLiberals().Count > 0)
            {
                timeUntilAgents = mc.LCSRandom(3) + 1;

                bool hasSleeperAgent = false;

                foreach (Entity e in MasterController.lcs.getAllSleepers())
                {
                    if (e.def == "AGENT")
                    {
                        hasSleeperAgent = true;
                        break;
                    }
                }

                if (hasSleeperAgent)
                {
                    string message = "A sleeper agent has reported that the CIA is planning to launch an attack on " + getComponent<SiteBase>().getCurrentName() + ".";
                    mc.addMessage(message, true);
                }
            }
            else if (timeUntilAgents > 0)
            {
                timeUntilAgents--;
            }
            else if (timeUntilAgents == 0 && !underSiege && MasterController.lcs.offendedCIA && getBasedLiberals().Count > 0)
            {
                timeUntilAgents = -1;

                string message = "Unmarked black vans are surrounding the " + getComponent<SiteBase>().getCurrentName() + "!";
                if((investments & Investments.CAMERAS) != 0)
                {
                    message += "\nThrough some form of high technology, they've managed to shut off the lights and cameras!";
                }
                else if((investments & Investments.GENERATOR) != 0)
                {
                    message += "\nThrough some form of high technology, they've managed to shut off the lights!";
                }
                else
                {
                    message += "\nThey've shut off the lights!";
                }

                mc.addMessage(message, true);

                underSiege = true;
                underAttack = true;
                siegeType = LocationDef.EnemyType.AGENT;
                lightsOff = true;
                camerasOff = true;
                MasterController.lcs.offendedCIA = false;
            }
            //Silently call off failed CIA raids
            else if (timeUntilAgents == 0) timeUntilAgents = -1;

            if(!underSiege&&MasterController.lcs.offendedAMRadio&&MasterController.generalPublic.PublicOpinion[Constants.VIEW_AM_RADIO] <= 35 && mc.LCSRandom(600) == 0 && getBasedLiberals().Count > 0)
            {
                mc.addMessage("Masses dissatisfied with your lack of respect for AM Radio are storming the " + getComponent<SiteBase>().getCurrentName() + "!", true);

                underSiege = true;
                underAttack = true;
                siegeType = LocationDef.EnemyType.REDNECK;
                lightsOff = false;
                camerasOff = false;
                MasterController.lcs.offendedAMRadio = false;
            }
            if (!underSiege && MasterController.lcs.offendedCableNews && MasterController.generalPublic.PublicOpinion[Constants.VIEW_CABLE_NEWS] <= 35 && mc.LCSRandom(600) == 0 && getBasedLiberals().Count > 0)
            {
                mc.addMessage("Masses dissatisfied with your lack of respect for Cable News are storming the " + getComponent<SiteBase>().getCurrentName() + "!", true);

                underSiege = true;
                underAttack = true;
                siegeType = LocationDef.EnemyType.REDNECK;
                lightsOff = false;
                camerasOff = false;
                MasterController.lcs.offendedCableNews = false;
            }

            if(MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE &&
                timeUntilFiremen == -1 && !underSiege && MasterController.lcs.offendedFiremen && getBasedLiberals().Count > 0 &&
                (investments & Investments.PRINTING_PRESS) != 0 && mc.LCSRandom(90) == 0)
            {
                timeUntilFiremen = mc.LCSRandom(3) + 1;

                int firemansleepercount = 0;

                foreach(Entity e in MasterController.lcs.getAllSleepers())
                {
                    if ((e.def == "FIREMAN" || e.def == "FIREFIGHTER") && e.getComponent<CreatureBase>().Location.def == "GOVERNMENT_FIRE_STATION")
                        firemansleepercount++;
                }

                if(mc.LCSRandom(firemansleepercount + 1) > 0 || mc.LCSRandom(10) == 0)
                {
                    string message;

                    if (firemansleepercount > 0) message = "A sleeper Fireman has informed you that ";
                    else message = "Word in the underground is that ";
                    message += "the Firemen are planning to burn " + getComponent<SiteBase>().getCurrentName() + ".";

                    mc.addMessage(message, true);
                }
            }
            else if(timeUntilFiremen > 0)
            {
                timeUntilFiremen--;
            }
            else if(MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE &&
                timeUntilFiremen == 0 && !underSiege && getBasedLiberals().Count > 0)
            {
                timeUntilFiremen = -1;

                string message = "Screaming fire engines pull up to the " + getComponent<SiteBase>().getCurrentName() + "!";
                message += "\nArmored firemen swarm out, pilot lights burning.\nYou hear a screeching voice over the sound of fire engine sirens:\nSurrender yourselves!\nUnacceptable Speech has occurred at this location.\nCome quietly and you will not be harmed.";

                mc.addMessage(message, true);

                underSiege = true;
                underAttack = true;
                siegeType = LocationDef.EnemyType.FIREMEN;
                lightsOff = false;
                camerasOff = false;
                MasterController.lcs.offendedFiremen = false;
            }
            else if(MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment == Alignment.ARCHCONSERVATIVE && timeUntilFiremen == 0)
            {
                raidUnoccupiedSafehouse(LocationDef.EnemyType.FIREMEN);
            }
            else if(MasterController.government.laws[Constants.LAW_FREE_SPEECH].alignment != Alignment.ARCHCONSERVATIVE && timeUntilFiremen == 0)
            {
                timeUntilFiremen = -1;
                MasterController.lcs.offendedFiremen = false;
            }
        }

        public void escapeEngage()
        {
            MasterController mc = MasterController.GetMC();

            LiberalCrimeSquad.Squad tempSquad = MasterController.lcs.newSquad("Siege Breakers");
            tempSquad.homeBase = owner;

            //nuke all squads in this base except for the active squad (if it is based here)
            if (MasterController.lcs.activeSquad != null && 
                MasterController.lcs.activeSquad.homeBase == owner)
            {
                List<Entity> libs = new List<Entity>(MasterController.lcs.activeSquad);

                foreach(Entity e in libs)
                {
                    MasterController.lcs.activeSquad.Remove(e);
                    tempSquad.Add(e);
                }
            }

            List<LiberalCrimeSquad.Squad> squads = new List<LiberalCrimeSquad.Squad>(MasterController.lcs.squads);
            foreach(LiberalCrimeSquad.Squad squad in squads)
            {
                if (squad == tempSquad) continue;
                if (squad.homeBase != owner) continue;

                while (squad.Count > 0)
                    squad.Remove(squad[0]);
            }

            List<Entity> sortedLibs = new List<Entity>(getBasedLiberals());
            sortedLibs.Sort((Entity e1, Entity e2) => { return e1.getComponent<CreatureBase>().Juice.CompareTo(e2.getComponent<CreatureBase>().Juice); });

            foreach(Entity e in sortedLibs)
            {
                if (tempSquad.Count >= 6) break;

                if(!tempSquad.Contains(e)) tempSquad.Add(e);
            }

            MasterController.lcs.activeSquad = tempSquad;
            tempSquad.target = owner;

            if (siegeType == LocationDef.EnemyType.POLICE)
            {
                foreach(Entity e in getBasedLiberals())
                {
                    e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_RESIST);
                }
            }

            if (!underAttack)
            {
                mc.currentChaseScene = new ChaseScene();
                mc.currentChaseScene.sallyForth(tempSquad, owner);
                mc.doNextAction();
            }
            else
            {
                SiteModeScene scene = new SiteModeScene();
                mc.currentSiteModeScene = scene;
                scene.startSiege(tempSquad, owner);
                mc.doNextAction();
            }

            //Queue up the next day so everything gets resolved
            mc.addAction(mc.nextPhase, "Next Day");
        }

        public void addNewHostage(Entity hostage)
        {
            hostage.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.MISSING;
            hostage.getComponent<CreatureBase>().Location = owner;
            Hostage hostageComponent = new Hostage();
            MasterController.highscore.kidnappings++;
            hostage.setComponent(hostageComponent);
            hostage.persist();
        }

        private void doDropItem(object sender, Events.DropItem args)
        {
            //Some bases are also trouble spots, so this should only fire if the item isn't being dropped "in-world"
            if (MasterController.GetMC().phase != MasterController.Phase.TROUBLE && MasterController.GetMC().currentChaseScene == null)
            {
                addItemToInventory(args.item);
            }
        }

        public void giveUpSiege()
        {
            MasterController mc = MasterController.GetMC();
            string text = "";

            switch (siegeType)
            {
                case LocationDef.EnemyType.POLICE:
                    if (escalation < SiegeEscalation.NATIONAL_GUARD)
                        text += "The police";
                    else
                        text += "The soldiers";
                    break;
                case LocationDef.EnemyType.FIREMEN:
                    text += "The Firemen";
                    break;
                default:
                    int killnumber = 0;

                    foreach(Entity e in getBasedLiberals())
                    {
                        killnumber++;
                        string causeofdeath = "was killed by ";
                        switch (siegeType)
                        {
                            case LocationDef.EnemyType.AGENT: causeofdeath += "the CIA"; break;
                            case LocationDef.EnemyType.CCS: causeofdeath += "the CCS"; break;
                            case LocationDef.EnemyType.MERC: causeofdeath += "Corporate mercenaries"; break;
                            case LocationDef.EnemyType.REDNECK: causeofdeath += "the Conservative Masses"; break;
                        }
                        e.getComponent<CreatureBase>().doDie(new Events.Die(causeofdeath));
                    }

                    mc.addMessage("Everyone in the " + getComponent<SiteBase>().getCurrentName() + " is slain.", true);
                    MasterController.news.startNewStory("MASSACRE", owner);
                    MasterController.news.currentStory.crimes.Add("KILLEDSOMEBODY", killnumber);
                    MasterController.news.currentStory.positive = false;
                    MasterController.news.currentStory.siegeType = siegeType;
                    underAttack = false;
                    underSiege = false;
                    mc.doNextAction();
                    return;
            }

            text += " confiscate everything, including Squad weapons.";

            int iCount = 0;
            int mCount = 0;

            foreach(Entity e in getBasedLiberals())
            {
                if ((e.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.ILLEGAL_IMMIGRANT) != 0)
                    iCount++;
                if ((e.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.MISSING) != 0)
                    mCount++;
            }

            foreach(Entity e in getInventory())
            {
                e.getComponent<ItemBase>().destroyItem();
            }

            //Add criminal charges based on evidence discovered at base
            foreach (Entity e in getBasedLiberals())
            {
                if (e.getComponent<Liberal>().status != Liberal.Status.ACTIVE) continue;

                for (int i = 0; i < getHostages().Count; i++) e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_KIDNAPPING);
                for (int i = 0; i < mCount; i++) e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_KIDNAPPING);
                for (int i = 0; i < iCount; i++) e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_HIRE_ILLEGAL);
                for (int i = 0; i < getBodies().Count; i++)
                {
                    /*If kidnapped bodies are found at the base, libs present will all be charged with murder, even if they weren't the ones
                     that killed them, because being caught with a corpse of a missing person in your basement doesn't look good for you*/
                    if ((getBodies()[i].getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.MISSING) != 0)
                        e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_MURDER);
                    else
                        e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_BURIAL);
                }
                if (siegeType == LocationDef.EnemyType.FIREMEN && (investments & Investments.PRINTING_PRESS) != 0) e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_SPEECH);
                e.getComponent<CriminalRecord>().arrest();
            }

            if(getHostages().Count == 1)
            {
                text += "\n" + getHostages()[0].getComponent<CreatureInfo>().getName() + " is rehabilitated and freed.";
            }
            else if(getHostages().Count > 1)
            {
                text += "\nThe hostages are rehabilitated and freed.";
            }

            foreach(Entity e in getHostages())
            {
                e.depersist();
            }

            if(getBodies().Count == 1)
            {
                text += "\n" + getBodies()[0].getComponent<CreatureInfo>().getName() + "'s corpse has been recovered.";
            }
            else if(getBodies().Count > 1)
            {
                text += "\nMultiple corpses are recovered.";
            }

            foreach (Entity e in getBodies())
            {
                e.depersist();
            }

            if (getBasedLiberals().Count == 1)
            {
                Entity lib = getBasedLiberals()[0];
                text += "\n" + lib.getComponent<CreatureInfo>().getName() + " ";
                if (lib.getComponent<CreatureInfo>().alias != "")
                    text += "aka " + lib.getComponent<CreatureInfo>().alias + ", ";
                text += "is taken to the police station.";
            }
            else if(getBasedLiberals().Count > 1)
            {
                text += "\nLiberals are taken to the police station.";
            }

            if(MasterController.lcs.Money > 0)
            {
                if(MasterController.lcs.Money < 2000 || siegeType == LocationDef.EnemyType.FIREMEN)
                {
                    text += "\nFortunately, your funds remain intact.";
                }
                else
                {
                    int confiscated = mc.LCSRandom(mc.LCSRandom(MasterController.lcs.Money - 2000) + 1) + 1000;
                    if (MasterController.lcs.Money - confiscated > 50000)
                        confiscated += MasterController.lcs.Money - 30000 - mc.LCSRandom(20000) - confiscated;
                    text += "\nLaw enforcement has confiscated $" + confiscated + " in LCS funds.";
                    MasterController.lcs.changeFunds(-confiscated);
                }
            }

            if(siegeType == LocationDef.EnemyType.FIREMEN)
            {
                if((investments & Investments.PRINTING_PRESS) != 0)
                {
                    text += "\nThe printing press is dismantled and burned.";
                    investments &= ~Investments.PRINTING_PRESS;
                }
            }
            else
            {
                if((investments & (Investments.AAGUN | Investments.CAMERAS | Investments.FORTIFIED | Investments.GENERATOR | Investments.TANK_TRAPS | Investments.TRAPS)) != 0)
                {
                    text += "\nThe compound is dismantled.";
                    investments &= (Investments.PRINTING_PRESS | Investments.FLAG | Investments.BUSINESS_FRONT);
                }
            }

            if((investments & Investments.BUSINESS_FRONT) != 0)
            {
                text += "\nMaterials relating to the business front have been taken.";
                investments &= ~Investments.BUSINESS_FRONT;
            }

            underAttack = false;
            underSiege = false;
            escalation = SiegeEscalation.POLICE;
            if (getRentPrice() > 0)
            {
                owned = false;
                freeRent = false;
                forceEvict = false;
            }
            mc.addMessage(text, true);
        }

        private void advanceSiege()
        {
            MasterController mc = MasterController.GetMC();

            if (!underSiege) return;

            if(getBasedLiberals().Count == 0)
            {
                raidUnoccupiedSafehouse(siegeType);
                return;
            }

            if (!underAttack)
            {
                bool starving = false;
                if(food == 0 && getBasedLiberals().Count > 0)
                {
                    starving = true;
                    mc.addMessage("Your Liberals are starving!", true);
                }

                if(food > 0)
                {
                    food -= getBasedLiberals().Count;
                    if (food < 0) food = 0;
                }

                if (starving)
                {
                    foreach (Entity e in getBasedLiberals())
                    {
                        e.getComponent<Body>().Blood -= mc.LCSRandom(8) + 4;
                        if(e.getComponent<Body>().Blood < 0)
                        {
                            e.getComponent<CreatureBase>().doDie(new Events.Die("starved to death"));
                        }
                    }
                }

                bool attack = mc.LCSRandom(12) == 0;

                if (attack)
                {
                    underAttack = true;
                    mc.addMessage("The cops are coming!", true);
                }
                else
                {
                    bool noBad = true;

                    if(!lightsOff && (investments & Investments.GENERATOR) == 0 && mc.LCSRandom(10) == 0)
                    {
                        noBad = false;
                        lightsOff = true;
                        mc.addMessage("The cops have cut the lights!", true);
                    }

                    if((investments & Investments.FORTIFIED) == 0 && mc.LCSRandom(5) == 0)
                    {
                        noBad = false;
                        Entity target = getBasedLiberals()[mc.LCSRandom(getBasedLiberals().Count)];

                        if(mc.LCSRandom(50) > target.getComponent<CreatureBase>().Juice)
                        {
                            mc.addMessage("A sniper takes out " + target.getComponent<CreatureInfo>().getName() + "!", true);
                            target.getComponent<CreatureBase>().doDie(new Events.Die("was shot by a police sniper"));
                        }
                        else
                        {
                            mc.addMessage("A sniper nearly hits " + target.getComponent<CreatureInfo>().getName() + "!", true);
                        }
                    }

                    if(escalation >= SiegeEscalation.BOMBERS && mc.LCSRandom(3) == 0)
                    {
                        noBad = false;
                        string bomberString = "You heard planes streak overhead!";
                        bool hit = true;

                        if((investments & Investments.AAGUN) != 0)
                        {
                            bomberString += "\nThe thunder of the anti-aircraft gun shakes the compound!";
                            if(mc.LCSRandom(5) != 0)
                            {
                                hit = false;
                                if(mc.LCSRandom(2) == 0)
                                {
                                    bomberString += "\nYou didn't shoot any down, but you've made them think twice!";
                                }
                                else
                                {
                                    bomberString += "\nHit! One of the bombers slams into to the ground. It's all over the TV. Everyone in the Liberal Crime Squad gains 20 juice!";
                                    foreach (Entity e in MasterController.lcs.getAllMembers())
                                        e.getComponent<CreatureBase>().juiceMe(20, 1000);
                                }
                            }
                            else
                            {
                                bomberString += "\nA skilled pilot gets through!";
                            }
                        }

                        if (hit)
                        {
                            bomberString += "\nExplosions rock the compound!";

                            if((investments & Investments.AAGUN) != 0 && mc.LCSRandom(3) == 0)
                            {
                                bomberString += "\nThe anti-aircraft gun takes a direct hit! There's nothing left but smoking wreckage...";
                                investments &= ~Investments.AAGUN;
                            }
                            else if((investments & Investments.GENERATOR) != 0 && mc.LCSRandom(3) == 0)
                            {
                                bomberString += "\nThe generator has been destroyed! he lights fade and all is dark.";
                                investments &= ~Investments.GENERATOR;
                                lightsOff = true;
                            }
                            if(mc.LCSRandom(2) == 0)
                            {
                                Entity target = getBasedLiberals()[mc.LCSRandom(getBasedLiberals().Count)];

                                if(mc.LCSRandom(100) > target.getComponent<CreatureBase>().Juice)
                                {
                                    bomberString += "\n" + target.getComponent<CreatureInfo>().getName() + " was killed in the bombing!";
                                    target.getComponent<CreatureBase>().doDie(new Events.Die("was killed by an airstrike"));
                                }
                                else
                                {
                                   bomberString += "\n" + target.getComponent<CreatureInfo>().getName() + " narrowly avoided death!";
                                }
                            }
                            else
                            {
                                bomberString += "\nFortunately, no one was hurt.";
                            }
                        }

                        mc.addMessage(bomberString, true);
                    }

                    if((investments & Investments.TANK_TRAPS) != 0 && escalation >= SiegeEscalation.BOMBERS && mc.LCSRandom(15) == 0)
                    {
                        noBad = false;
                        mc.addMessage("Army engineers have removed your tank traps. The tank moves forward to your compound entrance.");
                        investments &= ~Investments.TANK_TRAPS;
                    }

                    if(noBad && mc.LCSRandom(20) == 0 && getBasedLiberals().Count > 0)
                    {
                        string repName = Factories.CreatureFactory.generateGivenName() + " " + Factories.CreatureFactory.generateSurname();

                        string reportString = "Elitist " + repName + " from the ";

                        switch (mc.LCSRandom(5))
                        {
                            case 0: reportString += "news program "; break;
                            case 1: reportString += "news magazine"; break;
                            case 2: reportString += "website"; break;
                            case 3: reportString += "scandal rag"; break;
                            case 4: reportString += "newspaper"; break;
                        }
                        reportString += " ";
                        switch (mc.LCSRandom(12))
                        {
                            case 0: reportString += "Daily"; break;
                            case 1: reportString += "Nightly"; break;
                            case 2: reportString += "Current"; break;
                            case 3: reportString += "Pressing"; break;
                            case 4: reportString += "Socialist"; break;
                            case 5: reportString += "American"; break;
                            case 6: reportString += "National"; break;
                            case 7: reportString += "Union"; break;
                            case 8: reportString += "Foreign"; break;
                            case 9: reportString += "Associated"; break;
                            case 10: reportString += "International"; break;
                            case 11: reportString += "County"; break;
                        }
                        reportString += " ";
                        switch (mc.LCSRandom(11))
                        {
                            case 0: reportString += "Reporter"; break;
                            case 1: reportString += "Issue"; break;
                            case 2: reportString += "Take"; break;
                            case 3: reportString += "Constitution"; break;
                            case 4: reportString += "Times"; break;
                            case 5: reportString += "Post"; break;
                            case 6: reportString += "News"; break;
                            case 7: reportString += "Affair"; break;
                            case 8: reportString += "Statesman"; break;
                            case 9: reportString += "Star"; break;
                            case 10: reportString += "Inquirer"; break;
                        }
                        reportString += " got into the compound somehow!";

                        Entity bestSpeaker = getBasedLiberals()[0];
                        string[] attributes = { Constants.ATTRIBUTE_INTELLIGENCE, Constants.ATTRIBUTE_HEART };
                        string[] skills = { Constants.SKILL_PERSUASION };

                        foreach (Entity e in getBasedLiberals())
                        {
                            if (e.getComponent<CreatureBase>().getPower(attributes, skills) >
                                bestSpeaker.getComponent<CreatureBase>().getPower(attributes, skills))
                                bestSpeaker = e;
                        }

                        reportString += " " + bestSpeaker.getComponent<CreatureInfo>().getName() + " decides to give an interview.";
                        reportString += "\nThe interview is wide-ranging, covering a variety of topics.";

                        int segmentPower = bestSpeaker.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].roll() +
                            bestSpeaker.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].roll() +
                            bestSpeaker.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].roll() +
                            bestSpeaker.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].roll() +
                            bestSpeaker.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].roll();

                        if(segmentPower < 15)
                        {
                            reportString += "\n" + repName + " canceled the interview halfway through and later used the material for a Broadway play called ";
                            switch (mc.LCSRandom(11))
                            {
                                case 0: reportString += "Flaming"; break;
                                case 1:reportString += mc.swearFilter("Retarded", "Dumb"); break;
                                case 2: reportString +="Insane"; break;
                                case 3: reportString +="Crazy"; break;
                                case 4: reportString +="Loopy"; break;
                                case 5: reportString +="Idiot"; break;
                                case 6: reportString +="Empty-Headed"; break;
                                case 7: reportString +="Nutty"; break;
                                case 8: reportString +="Half-Baked"; break;
                                case 9: reportString += "Pot-Smoking"; break;
                                case 10: reportString += "Stoner"; break;
                            }
                            reportString += " ";
                            switch (mc.LCSRandom(10))
                            {
                                case 0: reportString += "Liberal"; break;
                                case 1: reportString += "Socialist"; break;
                                case 2: reportString += "Anarchist"; break;
                                case 3: reportString += "Communist"; break;
                                case 4: reportString += "Marxist"; break;
                                case 5: reportString += "Green"; break;
                                case 6: reportString += "Elite"; break;
                                case 7: reportString += "Guerrilla"; break;
                                case 8: reportString += "Commando"; break;
                                case 9: reportString += "Soldier"; break;
                            }
                            reportString += ".";
                        }
                        else if(segmentPower < 20)
                        {
                            reportString += "\nBut the interview is so boring that " + repName + " falls asleep.";
                        }
                        else if(segmentPower < 25)
                        {
                            reportString += "\nBut " + bestSpeaker.getComponent<CreatureInfo>().getName() + " stutters nervously the whole time.";
                        }
                        else if(segmentPower < 30)
                        {
                            reportString += "\n" + bestSpeaker.getComponent<CreatureInfo>().getName() + "'s verbal finesse leaves something to be desired.";
                        }
                        else if(segmentPower < 45)
                        {
                            reportString += "\n" + bestSpeaker.getComponent<CreatureInfo>().getName() + " represents the LCS well.";
                        }
                        else if(segmentPower < 60)
                        {
                            reportString += "\nThe discussion was exciting and dynamic. Even the Cable News and AM Radio spend days talking about it.";
                        }
                        else
                        {
                            reportString += "\n" + repName + " later went on to win a Pulitzer for it. Virtually everyone in America was moved by " + bestSpeaker.getComponent<CreatureInfo>().getName() + "'s words.";
                        }

                        mc.addMessage(reportString, true);

                        MasterController.generalPublic.changePublicOpinion(Constants.VIEW_LIBERALCRIMESQUAD, 20);
                        MasterController.generalPublic.changePublicOpinion(Constants.VIEW_LIBERALCRIMESQUADPOS, (segmentPower - 25) / 2, 1, segmentPower + 50);
                        for(int i=0;i<5;i++)
                            MasterController.generalPublic.changePublicOpinion(MasterController.generalPublic.randomissue(true), (segmentPower - 25) / 2);
                    }
                }
            }
        }

        public void raidUnoccupiedSafehouse(LocationDef.EnemyType type)
        {
            string typeString = "";
            switch (type)
            {
                case LocationDef.EnemyType.POLICE:
                    typeString = "The cops";
                    break;
                case LocationDef.EnemyType.MERC:
                    typeString = "Corporate mercenaries";
                    break;
                case LocationDef.EnemyType.FIREMEN:
                    typeString = "Firemen";
                    break;
                default:
                    typeString = "Conservatives";
                    break;
            }

            string siegeString = typeString + " have raided the " + getComponent<SiteBase>().getCurrentName() + ", an unoccupied safehouse.";
            foreach (Entity e in getBodies())
            {
                siegeString += "\n" + e.getComponent<CreatureInfo>().getName() + "'s corpse has been recovered.";
                e.depersist();
            }
            foreach (Entity e in getHostages())
            {
                siegeString += "\n" + e.getComponent<CreatureInfo>().getName() + " has been rescued.";
                e.depersist();
            }

            if(type == LocationDef.EnemyType.FIREMEN)
            {
                if ((investments & Investments.PRINTING_PRESS) != 0)
                {
                    investments &= ~Investments.PRINTING_PRESS;
                    siegeString += "\nThe printing press is dismantled and burned.";
                }
                if((investments & Investments.BUSINESS_FRONT) != 0)
                {
                    investments &= ~Investments.BUSINESS_FRONT;
                    siegeString += "\nMaterials relating to the business front have been destroyed.";
                }
            }
            else
            {
                if ((investments & (Investments.AAGUN | Investments.CAMERAS | Investments.FORTIFIED | Investments.GENERATOR | Investments.TANK_TRAPS | Investments.TRAPS)) != 0)
                {
                    siegeString += "\nThe compound is dismantled.";
                    investments &= (Investments.PRINTING_PRESS | Investments.FLAG | Investments.BUSINESS_FRONT);
                }

                if ((investments & Investments.BUSINESS_FRONT) != 0)
                {
                    siegeString += "\nMaterials relating to the business front have been " + (type==LocationDef.EnemyType.POLICE?"taken":"destroyed") + ".";
                    investments &= ~Investments.BUSINESS_FRONT;
                }
            }

            foreach (Entity e in getInventory())
            {
                e.getComponent<ItemBase>().destroyItem();
            }

            MasterController.GetMC().addMessage(siegeString, true);

            underSiege = false;
            underAttack = false;

            switch (siegeType)
            {
                case LocationDef.EnemyType.AGENT:
                    MasterController.lcs.offendedCIA = false;
                    break;
                case LocationDef.EnemyType.FIREMEN:
                    MasterController.lcs.offendedFiremen = false;
                    break;
                case LocationDef.EnemyType.MERC:
                    MasterController.lcs.offendedCorps = false;
                    break;
                case LocationDef.EnemyType.POLICE:
                    escalation = SiegeEscalation.POLICE;
                    break;
            }
        }

        public void addItemToInventory(Entity item)
        {
            item.getComponent<ItemBase>().Location = owner;
            if(!item.hasComponent<Clip>() && (!item.hasComponent<Weapon>() || item.getComponent<Weapon>().getAmmoType() == "NONE"))
            {
                item.persist();
            }
            else
            {
                if(item.hasComponent<Weapon>() && (item.getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.THROWN) == 0 && item.getComponent<Weapon>().clip != null)
                {
                    Entity clip = item.getComponent<Weapon>().clip;
                    item.getComponent<Weapon>().clip = null;
                    item.persist();

                    addItemToInventory(clip);
                }
                else if (item.hasComponent<Clip>())
                {
                    item.persist();
                    consolidateClips();
                }
                else
                {
                    item.persist();
                }
            }
        }

        public void consolidateClips()
        {
            Dictionary<string, List<Entity>> allClips = new Dictionary<string, List<Entity>>();
            Dictionary<string, int> allAmmo = new Dictionary<string, int>();

            foreach(Entity e in getInventory())
            {
                if (!e.hasComponent<Clip>()) continue;
                else if (e.getComponent<Clip>().isFull()) continue;
                else if (e.hasComponent<Weapon>() && (e.getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.THROWN) != 0) continue;

                if (!allClips.ContainsKey(e.getComponent<Clip>().getAmmoType()))
                {
                    allClips.Add(e.getComponent<Clip>().getAmmoType(), new List<Entity>());
                    allAmmo.Add(e.getComponent<Clip>().getAmmoType(), 0);
                }
                allClips[e.getComponent<Clip>().getAmmoType()].Add(e);
                allAmmo[e.getComponent<Clip>().getAmmoType()] += e.getComponent<Clip>().ammo;
                e.getComponent<Clip>().ammo = 0;
            }

            foreach(string ammo in allClips.Keys)
            {
                allClips[ammo].Sort((Entity x, Entity y) => { return x.getComponent<Clip>().getMaxAmmo().CompareTo(y.getComponent<Clip>().getMaxAmmo()); });

                foreach(Entity clip in allClips[ammo])
                {
                    if(allAmmo[ammo] > 0)
                    {
                        clip.getComponent<Clip>().ammo = Math.Min(allAmmo[ammo], clip.getComponent<Clip>().getMaxAmmo());
                    }
                    else
                    {
                        clip.getComponent<ItemBase>().destroyItem();
                    }
                }
            }
        }

        public int getSecrecy()
        {
            int secrecy = ((LocationDef.BaseDef)GameData.getData().locationList[owner.def].components["base"]).secrecy;

            if ((investments & Investments.BUSINESS_FRONT) != 0) secrecy += 12;

            if (MasterController.government.laws[Constants.LAW_FLAG_BURNING].alignment == Alignment.ARCHCONSERVATIVE)
            {
                if ((investments & Investments.FLAG) != 0)
                {
                    secrecy += 6;
                }
                else
                {
                    secrecy -= 2;
                }
            }
            else
            {
                if ((investments & Investments.FLAG) != 0)
                {
                    secrecy += 2;
                }
            }

            secrecy *= 5;

            if (secrecy < 0) secrecy = 0;
            if (secrecy > 95) secrecy = 95;

            return secrecy;
        }

        public void moveLiberalHere(Entity e)
        {
            Liberal lib = e.getComponent<Liberal>();

            lib.homeBase = owner;
            lib.goHome();
        }

        public void moveSquadHere(LiberalCrimeSquad.Squad squad)
        {
            foreach (Entity e in squad)
            {
                moveLiberalHere(e);
                if (e.getComponent<Inventory>().vehicle != null)
                {
                    e.getComponent<Inventory>().vehicle.getComponent<ItemBase>().moveItem(owner);
                }
            }

            squad.homeBase = owner;
            squad.goHome();
        }

        public List<Entity> getBasedLiberals(bool activeOnly = true)
        {
            List<Entity> basedLiberals = new List<Entity>();

            foreach(Entity e in MasterController.lcs.getAllMembers())
            {
                if (e.getComponent<Liberal>().homeBase == owner && (!activeOnly || e.getComponent<Liberal>().status == Liberal.Status.ACTIVE))
                    basedLiberals.Add(e);
            }

            return basedLiberals;
        }

        public Entity getBestHealer()
        {
            Entity bestHealer = null;

            foreach(Entity e in getBasedLiberals())
            {
                if(bestHealer == null || bestHealer.getComponent<CreatureBase>().Skills[Constants.SKILL_FIRST_AID].level < e.getComponent<CreatureBase>().Skills[Constants.SKILL_FIRST_AID].level)
                {
                    //Liberals laying low will provide first aid if it is needed
                    if((e.getComponent<Liberal>().dailyActivity.type == "FIRST_AID" || e.getComponent<Liberal>().dailyActivity.type == "NONE") &&
                        !(e.getComponent<Liberal>().squad != null && e.getComponent<Liberal>().squad.target != null))
                        bestHealer = e;
                }
            }

            return bestHealer;
        }

        public void buyFlag()
        {
            if (MasterController.lcs.Money >= 20)
            {
                investments |= Investments.FLAG;
                MasterController.lcs.changeFunds(-20);
                MasterController.highscore.flagsBought++;
            }
        }

        public void burnFlag()
        {
            investments &= ~Investments.FLAG;
            MasterController.highscore.flagsBurned++;

            foreach (Entity e in getBasedLiberals())
            {
                e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_FLAG_BURNING);
            }

            if (underSiege)
            {
                MasterController.generalPublic.changePublicOpinion(Constants.VIEW_LIBERALCRIMESQUAD, 1);
                MasterController.generalPublic.changePublicOpinion(Constants.VIEW_FREE_SPEECH, 1, 1, 30);

                if(MasterController.government.laws[Constants.LAW_FLAG_BURNING].alignment <= Alignment.MODERATE)
                {
                    MasterController.generalPublic.changePublicOpinion(Constants.VIEW_LIBERALCRIMESQUAD, 1);
                    MasterController.generalPublic.changePublicOpinion(Constants.VIEW_FREE_SPEECH, 1, 1, 50);
                }
                if (MasterController.government.laws[Constants.LAW_FLAG_BURNING].alignment <= Alignment.CONSERVATIVE)
                {
                    MasterController.generalPublic.changePublicOpinion(Constants.VIEW_LIBERALCRIMESQUAD, 5);
                    MasterController.generalPublic.changePublicOpinion(Constants.VIEW_FREE_SPEECH, 2, 1, 70);
                }
                if (MasterController.government.laws[Constants.LAW_FLAG_BURNING].alignment == Alignment.ARCHCONSERVATIVE)
                {
                    MasterController.generalPublic.changePublicOpinion(Constants.VIEW_LIBERALCRIMESQUAD, 15);
                    MasterController.generalPublic.changePublicOpinion(Constants.VIEW_FREE_SPEECH, 5, 1, 90);
                }
            }
        }

        public void buyFood()
        {
            if(MasterController.lcs.Money >= 150)
            {
                food += 20;
                MasterController.lcs.changeFunds(-150);
            }
        }

        public void applyUpgrade(string upgrade)
        {
            Investments investment = (Investments)Enum.Parse(typeof(Investments), upgrade);

            if(investment == Investments.FORTIFIED || investment == Investments.CAMERAS)
            {
                if(MasterController.lcs.Money >= 2000)
                {
                    MasterController.lcs.changeFunds(-2000);
                    investments |= investment;
                }
            }
            else if(investment != Investments.AAGUN)
            {
                if (MasterController.lcs.Money >= 3000)
                {
                    MasterController.lcs.changeFunds(-3000);
                    investments |= investment;
                }
            }
            else
            {
                if(MasterController.government.laws["GUN_CONTROL"].alignment == Alignment.ARCHCONSERVATIVE)
                {
                    if (MasterController.lcs.Money >= 35000)
                    {
                        MasterController.lcs.changeFunds(-35000);
                        investments |= investment;
                    }
                }
                else
                {
                    if (MasterController.lcs.Money >= 200000)
                    {
                        MasterController.lcs.changeFunds(-200000);
                        investments |= investment;
                    }
                }
            }
        }

        public List<Entity> getAllHealers()
        {
            List<Entity> healers = new List<Entity>();

            foreach (Entity e in getBasedLiberals(true))
            {
                //Liberals laying low will provide first aid if it is needed
                if (e.getComponent<Liberal>().dailyActivity.type == "FIRST_AID" &&
                    !(e.getComponent<Liberal>().squad != null && e.getComponent<Liberal>().squad.target != null))
                    healers.Add(e);                
            }

            return healers;
        }

        public LocationDef.BaseFlag getFlags()
        { return ((LocationDef.BaseDef)GameData.getData().locationList[owner.def].components["base"]).flags; }

        public int getRentPrice()
        { return ((LocationDef.BaseDef)GameData.getData().locationList[owner.def].components["base"]).rentPrice; }
    }
}
