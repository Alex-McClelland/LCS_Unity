using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LCS.Engine.Containers;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.Item;
using LCS.Engine.Components.Location;
using LCS.Engine.Data;

namespace LCS.Engine.Scenes
{
    public class Fight
    {
        private static readonly string[] escape_crawling =
        {
      " crawls off moaning...",
      " crawls off whimpering...",
      " crawls off trailing blood...",
      " crawls off screaming...",
      " crawls off crying...",
      " crawls off sobbing...",
      " crawls off whispering...",
      " crawls off praying...",
      " crawls off cursing..."
    };

        private static readonly string[] escape_running =
        {
      " makes a break for it!",
      " escapes crying!",
      " runs away!",
      " gets out of there!",
      " runs hollering!",
      " bolts out of there!",
      " runs away screaming!",
   };

        public static void fight(List<Entity> liberals, List<Entity> conservatives, ActionQueue parentAction)
        {
            youFight(liberals, conservatives, parentAction);
            theyFight(liberals, conservatives, parentAction);
            endOfRound(liberals, conservatives, parentAction);
        }

        public static void youFight(List<Entity> liberals, List<Entity> conservatives, ActionQueue parentAction)
        {
            foreach(Entity e in liberals)
            {
                if(e != null)
                {
                    parentAction.Add(() => { libFight(e, liberals, conservatives); }, "libfight");
                }
            }
        }

        public static void theyFight(List<Entity> liberals, List<Entity> conservatives, ActionQueue parentAction)
        {
            foreach (Entity e in conservatives)
            {
                if (e != null)
                {
                    parentAction.Add(() => { conFight(e, liberals, conservatives); },"confight");
                }
            }
        }

        public static void libFight(Entity lib, List<Entity> liberals, List<Entity> conservatives)
        {
            MasterController mc = MasterController.GetMC();
            mc.addCombatMessage("##DEBUG## entering libFight");

            if (!lib.getComponent<Body>().Alive || !liberals.Contains(lib))
            {
                mc.doNextAction();
                return;
            }

            if(lib.getComponent<Inventory>().reloadedThisRound)
            {
                mc.addCombatMessage(lib.getComponent<CreatureInfo>().getName() + " reloaded this round.");
                lib.getComponent<Inventory>().reloadedThisRound = false;
                return;
            }

            //Pick target
            List<Entity> dangerousTarget = new List<Entity>();
            List<Entity> regularTarget = new List<Entity>();
            List<Entity> bystanders = new List<Entity>();

            foreach (Entity a in conservatives)
            {
                if (a != null && a.getComponent<Body>().Alive)
                {
                    if (a.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE || (a.getComponent<CreatureInfo>().alignment == Alignment.MODERATE && a.def == "NEGOTIATOR"))
                    {
                        if ((a.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.THREATENING) != 0
                            && a.getComponent<Body>().Blood >= 40)
                            dangerousTarget.Add(a);
                        else
                            regularTarget.Add(a);
                    }
                    else
                    {
                        bystanders.Add(a);
                    }
                }
            }

            Entity target;

            if (dangerousTarget.Count + regularTarget.Count == 0)
            {
                mc.doNextAction();
                return;
            }

            if (dangerousTarget.Count > 0)
                target = dangerousTarget[mc.LCSRandom(dangerousTarget.Count)];
            else
                target = regularTarget[mc.LCSRandom(regularTarget.Count)];

            bool mistake = false;
            AttackDef.DamageType damageType = lib.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getAttack().damage_type;
            if (bystanders.Count > 0 && mc.LCSRandom(100) == 0 &&  !(damageType == AttackDef.DamageType.MUSIC || damageType == AttackDef.DamageType.PERSUASION))
            {
                target = bystanders[mc.LCSRandom(bystanders.Count)];
                mistake = true;
            }

            lib.getComponent<CreatureBase>().attack(target, mistake);
        }

        public static void conFight(Entity con, List<Entity> liberals, List<Entity> conservatives)
        {
            MasterController mc = MasterController.GetMC();

            mc.addCombatMessage("##DEBUG## entering conFight");

            if (!con.getComponent<Body>().Alive || !conservatives.Contains(con))
            {
                mc.doNextAction();
                return;
            }

            //If this is a successful sneak attack, non-Conservatives won't have noticed the murder, allowing you to still speak to them
            if(mc.currentSiteModeScene != null && !mc.currentSiteModeScene.alarmTriggered && con.getComponent<CreatureInfo>().alignment != Alignment.CONSERVATIVE)
            {
                mc.addCombatMessage(con.getComponent<CreatureInfo>().encounterName + " didn't hear anything.");
                mc.doNextAction();
                return;
            }

            //If there are any living cons in the group after the Liberal round, the alarm will trigger
            if(mc.currentSiteModeScene != null)
            {
                foreach(Entity e in conservatives)
                {
                    if (e == null) continue;
                    if (e.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE &&
                        e.getComponent<Body>().Alive)
                        mc.currentSiteModeScene.alarmTriggered = true;
                }
            }

            con.getComponent<CreatureInfo>().inCombat = true;

            //Once fighting has started, people are no longer interested in discussion, unless they've recently been converted
            if((con.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.CONVERTED) == 0)
                con.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;

            bool incapacitated = con.getComponent<Body>().incapacitated();

            //This needs to be set so that they will be consistantly incapacitated between the withdrawl test below and the attack command later.
            if (incapacitated) con.getComponent<Body>().ForceIncap = true;

            //Withdrawl check;
            if (withdrawlCheck(con, liberals) && !incapacitated && (MasterController.GetMC().combatModifiers & MasterController.CombatModifiers.CHASE_CAR) == 0)
            {
                conservatives[conservatives.IndexOf(con)] = null;

                if (!con.getComponent<Body>().canRun() || con.getComponent<Body>().Blood < 45)
                    mc.addCombatMessage(con.getComponent<CreatureInfo>().encounterName + escape_crawling[MasterController.GetMC().LCSRandom(escape_crawling.Length)]);
                else
                    mc.addCombatMessage(con.getComponent<CreatureInfo>().encounterName + escape_running[MasterController.GetMC().LCSRandom(escape_running.Length)]);
                return;
            }

            //Pick target
            List<Entity> libTarget = new List<Entity>();

            if (con.getComponent<CreatureInfo>().alignment != Alignment.LIBERAL)
            {
                foreach (Entity a in liberals)
                {
                    if (a != null && a.getComponent<Body>().Alive) libTarget.Add(a);
                }
            }
            else
            {
                foreach(Entity a in conservatives)
                {
                    if (a != null && a.getComponent<Body>().Alive && a.getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
                        libTarget.Add(a);
                }
            }

            if (libTarget.Count == 0)
            {
                mc.doNextAction();
                return;
            }

            Entity target = libTarget[mc.LCSRandom(libTarget.Count)];

            con.getComponent<CreatureBase>().attack(target);
        }

        public static void endOfRound(List<Entity> liberals, List<Entity> conservatives, ActionQueue parentAction)
        {
            parentAction.Add(() => { doFirstAid(liberals, conservatives); }, "doFirstAid");
            parentAction.Add(() => { doBleeding(liberals, conservatives); }, "doBleeding");
            parentAction.Add(() => { doBurning(liberals, conservatives); }, "doBurning");
            parentAction.Add(() => { doHauling(liberals); }, "doHauling");
            parentAction.Add(() => { doCleanup(liberals, conservatives); }, "doCleanup");
        }

        private static void doCleanup(List<Entity> liberals, List<Entity> conservatives)
        {
            MasterController.GetMC().addCombatMessage("##DEBUG## Entering doCleanup");

            List<Entity> tempList = new List<Entity>(liberals);

            foreach(Entity e in tempList)
            {
                if (e != null && !e.getComponent<Body>().Alive)
                    liberals[liberals.IndexOf(e)] = null;
            }

            tempList = new List<Entity>(conservatives);

            foreach(Entity e in tempList)
            {
                if (e != null && !e.getComponent<Body>().Alive)
                    conservatives[conservatives.IndexOf(e)] = null;
            }

            MasterController.GetMC().doNextAction();
        }

        private static void doFirstAid(List<Entity> liberals, List<Entity> conservatives)
        {
            MasterController.GetMC().addCombatMessage("##DEBUG## Entering doFirstAid");
            Entity bestHealer = null;

            ActionQueue actionRoot = MasterController.GetMC().getNextAction();

            foreach (Entity a in liberals)
            {
                if (a != null && a.getComponent<Body>().Alive && a.getComponent<Body>().Blood > 40)
                {
                    if (bestHealer == null) bestHealer = a;
                    else
                    {
                        if (a.getComponent<CreatureBase>().Skills["FIRST_AID"].level > bestHealer.getComponent<CreatureBase>().Skills["FIRST_AID"].level)
                            bestHealer = a;
                    }
                }
            }

            if (bestHealer != null)
            {
                foreach (Entity a in liberals)
                {
                    if (a == null) continue;

                    Entity healTarget = a;
                    actionRoot.Add(() => { firstAid(healTarget, bestHealer); }, "first aid: " + healTarget.getComponent<CreatureInfo>().getName());

                    if (a.getComponent<Liberal>().hauledUnit != null && a.getComponent<Liberal>().hauledUnit.getComponent<Body>().Alive)
                    {
                        Entity hauledTarget = a.getComponent<Liberal>().hauledUnit;
                        actionRoot.Add(() => { firstAid(hauledTarget, bestHealer); }, "first aid (hauled unit): " + hauledTarget.getComponent<CreatureInfo>().getName());
                    }
                    
                }
            }

            MasterController.GetMC().doNextAction();
        }

        private static void firstAid(Entity target, Entity healer)
        {
            MasterController.GetMC().addCombatMessage("##DEBUG## Entering firstAid");
            string returnText = "";

            if (target.getComponent<Body>().isBleeding() && healer.getComponent<CreatureBase>().Skills["FIRST_AID"].check(Difficulty.FORMIDABLE))
            {
                target.getComponent<Body>().triage();
                healer.getComponent<CreatureBase>().Skills["FIRST_AID"].addExperience(Math.Max(50 - healer.getComponent<CreatureBase>().Skills["FIRST_AID"].level * 2, 0));
                returnText = "<color=lime>" + healer.getComponent<CreatureInfo>().getName() + " was able to slow the bleeding of " + target.getComponent<CreatureInfo>().getName() + "'s wounds.</color>";
            }

            if (returnText != "")
                MasterController.GetMC().addCombatMessage(returnText);
            else
                MasterController.GetMC().doNextAction();
        }

        private static void doBleeding(List<Entity> liberals, List<Entity> conservatives)
        {
            MasterController.GetMC().addCombatMessage("##DEBUG## Entering doBleeding");
            ActionQueue actionRoot = MasterController.GetMC().getNextAction();

            foreach (Entity a in liberals)
            {
                if (a == null) continue;
                Entity bleeder = a;
                actionRoot.Add(() => { bleed(bleeder, liberals); }, "bleed: " + bleeder.getComponent<CreatureInfo>().getName());
                if (a.getComponent<Liberal>().hauledUnit != null)
                {
                    Entity hauledBleeder = a.getComponent<Liberal>().hauledUnit;
                    actionRoot.Add(() => { bleed(hauledBleeder, liberals); }, "bleed (hauled unit): " + hauledBleeder.getComponent<CreatureInfo>().getName());
                }
            }

            foreach (Entity a in conservatives)
            {
                if (a == null) continue;
                Entity bleeder = a;
                actionRoot.Add(() => { bleed(bleeder, liberals); }, "bleed (con): " + bleeder.getComponent<CreatureInfo>().getName());
            }

            MasterController.GetMC().doNextAction();
        }

        private static void bleed(Entity e, List<Entity> liberals)
        {
            MasterController.GetMC().addCombatMessage("##DEBUG## Entering bleed");
            if (e.getComponent<Body>().bleed())
            {
                MasterController.GetMC().doNextAction();
            }
        }

        private static void doBurning(List<Entity> liberals, List<Entity> conservatives)
        {
            MasterController.GetMC().addCombatMessage("##DEBUG## Entering doBurning");
            ActionQueue actionRoot = MasterController.GetMC().getNextAction();

            if (MasterController.GetMC().currentSiteModeScene == null)
            {
                MasterController.GetMC().doNextAction();
                return;
            }
            else
            {
                if(MasterController.GetMC().currentSiteModeScene.getSquadTile().getComponent<TileBase>().fireState == TileBase.FireState.PEAK ||
                    MasterController.GetMC().currentSiteModeScene.getSquadTile().getComponent<TileBase>().fireState == TileBase.FireState.END)
                {
                    foreach (Entity a in liberals)
                    {
                        if (a == null) continue;
                        Entity burner = a;
                        actionRoot.Add(() => { burn(burner); }, "burn: " + burner.getComponent<CreatureInfo>().getName());
                        if (a.getComponent<Liberal>().hauledUnit != null)
                        {
                            Entity hauledBurner = a.getComponent<Liberal>().hauledUnit;
                            actionRoot.Add(() => { burn(hauledBurner); }, "burn (hauled unit): " + hauledBurner.getComponent<CreatureInfo>().getName());
                        }
                    }

                    foreach (Entity a in conservatives)
                    {
                        if (a == null) continue;
                        Entity burner = a;
                        actionRoot.Add(() => { burn(burner); }, "burn (con): " + burner.getComponent<CreatureInfo>().getName());
                    }
                }
                else
                {
                    MasterController.GetMC().doNextAction();
                    return;
                }
            }

            MasterController.GetMC().doNextAction();
        }

        private static void burn(Entity e)
        {
            MasterController mc = MasterController.GetMC();
            mc.addCombatMessage("##DEBUG## Entering burn");

            int burnDamage = mc.currentSiteModeScene.getSquadTile().getComponent<TileBase>().fireState == TileBase.FireState.PEAK ? mc.LCSRandom(40) : mc.LCSRandom(20);

            if((e.getComponent<Inventory>().getArmor().getComponent<Armor>().getFlags() & ItemDef.ArmorFlags.FIRE_PROTECTION) != 0)
            {
                int denom = 4;
                if (e.getComponent<Inventory>().getArmor().getComponent<Armor>().damaged) denom += 2;
                denom += e.getComponent<Inventory>().getArmor().getComponent<Armor>().quality - 1;
                burnDamage = (int) (burnDamage * (1f - (3f / denom)));
            }

            e.getComponent<Body>().Blood -= burnDamage;

            if(e.getComponent<Body>().Blood <= 0)
            {
                if (e.getComponent<Body>().Alive)
                {
                    e.getComponent<CreatureBase>().doDie(new Events.Die("burned to death"));
                }
            }
            else
            {
                string name = "";
                if (e.hasComponent<Liberal>())
                    name = e.getComponent<CreatureInfo>().getName();
                else
                    name = e.getComponent<CreatureInfo>().encounterName;
                MasterController.GetMC().addCombatMessage(name + " is burned!");
            }
        }

        private static void doHauling(List<Entity> liberals)
        {
            MasterController.GetMC().addCombatMessage("##DEBUG## Entering doHauling");
            if ((MasterController.GetMC().combatModifiers & MasterController.CombatModifiers.CHASE_CAR) != 0)
            {
                MasterController.GetMC().doNextAction();
                return;
            }

            List<Entity> paralyzedLibs = new List<Entity>();
            List<Entity> deadLibs = new List<Entity>();

            ActionQueue actionRoot = MasterController.GetMC().getNextAction();

            foreach (Entity a in liberals)
            {
                if (a == null) continue;
                Entity actor = a;

                if (!a.getComponent<Body>().Alive)
                {
                    if (a.getComponent<Liberal>().hauledUnit != null)
                    {
                        actionRoot.Add(() => { dropHauledUnit(actor); }, "dropHauledUnit");
                        if (a.getComponent<Liberal>().hauledUnit.hasComponent<Liberal>())
                        {
                            if (!a.getComponent<Liberal>().hauledUnit.getComponent<Body>().Alive)
                            {
                                deadLibs.Add(a.getComponent<Liberal>().hauledUnit);
                            }
                            else if (!(a.getComponent<Body>().canWalk() || (a.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.WHEELCHAIR) != 0))
                            {
                                paralyzedLibs.Add(a.getComponent<Liberal>().hauledUnit);
                            }
                        }
                    }
                    deadLibs.Add(a);
                }
                else if (!(a.getComponent<Body>().canWalk() || (a.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.WHEELCHAIR) != 0))
                {
                    if (a.getComponent<Liberal>().hauledUnit != null)
                    {
                        actionRoot.Add(() => { dropHauledUnit(actor); }, "dropHauledUnit");
                        if (a.getComponent<Liberal>().hauledUnit.hasComponent<Liberal>())
                        {
                            if (!a.getComponent<Liberal>().hauledUnit.getComponent<Body>().Alive)
                            {
                                deadLibs.Add(a.getComponent<Liberal>().hauledUnit);
                            }
                            else if (!(a.getComponent<Body>().canWalk() || (a.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.WHEELCHAIR) != 0))
                            {
                                paralyzedLibs.Add(a.getComponent<Liberal>().hauledUnit);
                            }
                        }
                    }
                    paralyzedLibs.Add(a);
                }

                if(a.getComponent<Liberal>().hauledUnit != null &&
                    !a.getComponent<Liberal>().hauledUnit.hasComponent<Liberal>() &&
                    !a.getComponent<Liberal>().hauledUnit.getComponent<Body>().Alive)
                {
                    actionRoot.Add(() => { dropHauledUnit(actor); }, "dropHauledUnit");
                }
            }

            //Prioritize hauling the injured over the dead
            foreach (Entity a in paralyzedLibs)
            {
                Entity haulee = a;
                actionRoot.Add(() => { tryHaulLib(haulee, liberals); }, "tryHaulLib: " + haulee.getComponent<CreatureInfo>().getName());
            }

            foreach (Entity a in deadLibs)
            {
                Entity haulee = a;
                actionRoot.Add(() => { tryHaulLib(haulee, liberals); }, "tryHaulLib: " + haulee.getComponent<CreatureInfo>().getName());
            }

            MasterController.GetMC().doNextAction();
        }

        public static void dropHauledUnit(Entity hauler)
        {
            MasterController.GetMC().addCombatMessage("##DEBUG## Entering dropHauledUnit");
            Entity hauledUnit = hauler.getComponent<Liberal>().hauledUnit;
            string name;
            if (hauledUnit.hasComponent<Liberal>())
                name = hauledUnit.getComponent<CreatureInfo>().getName();
            else
                name = hauledUnit.getComponent<CreatureInfo>().encounterName;

            hauler.getComponent<Liberal>().hauledUnit = null;
            if(!hauler.getComponent<Body>().Alive ||
                !(hauler.getComponent<Body>().canWalk() || (hauler.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.WHEELCHAIR) != 0))
                MasterController.GetMC().addCombatMessage(hauler.getComponent<CreatureInfo>().getName() + " can no longer carry " + name + ".");
            else
                MasterController.GetMC().addCombatMessage(hauler.getComponent<CreatureInfo>().getName() + " drops " + name + ".");
            if (!hauledUnit.hasComponent<Liberal>() && hauledUnit.getComponent<Body>().Alive)
            {
                if(hauledUnit.getComponent<Body>().canRun())
                    MasterController.GetMC().addCombatMessage(name + " runs away screaming!");
                else if(hauledUnit.getComponent<Body>().canWalk())
                    MasterController.GetMC().addCombatMessage(name + " limps away screaming!");
                else
                    MasterController.GetMC().addCombatMessage(name + " crawls away moaning.");
            }
        }

        public static void tryHaulLib(Entity haulee, List<Entity> liberals)
        {
            MasterController.GetMC().addCombatMessage("##DEBUG## Entering tryHaulLib");
            List<Entity> validHaulers = new List<Entity>();

            if(haulee.getComponent<Liberal>().squad != null)
                haulee.getComponent<Liberal>().squad.Remove(haulee);

            foreach (Entity a in liberals)
            {
                if (a == null) continue;
                if (a == haulee) continue;
                if (a.getComponent<Body>().Alive &&
                    (a.getComponent<Body>().canWalk() || (a.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.WHEELCHAIR) != 0) &&
                    a.getComponent<Liberal>().hauledUnit == null)
                {
                    validHaulers.Add(a);
                }
            }

            if (validHaulers.Count == 0) abandon(haulee);
            else
            {
                Entity hauler = validHaulers[MasterController.GetMC().LCSRandom(validHaulers.Count)];

                hauler.getComponent<Liberal>().hauledUnit = haulee;

                MasterController.GetMC().addCombatMessage(hauler.getComponent<CreatureInfo>().getName() + " hauls " + haulee.getComponent<CreatureInfo>().getName() + ".");
            }
        }

        private static void abandon(Entity lib)
        {
            MasterController.GetMC().addCombatMessage("##DEBUG## Entering abandon");
            if (lib.getComponent<Body>().Alive) lib.getComponent<CriminalRecord>().arrest();

            if (lib.getComponent<Body>().Alive)
                MasterController.GetMC().addCombatMessage("Nobody can carry " + lib.getComponent<CreatureInfo>().getName() + ", " + lib.getComponent<CreatureInfo>().heShe().ToLower() + " is left to be captured.");
            else
                MasterController.GetMC().addCombatMessage("Nobody can carry Martyr " + lib.getComponent<CreatureInfo>().getName() + ".");
        }

        private static bool withdrawlCheck(Entity actor, List<Entity> liberals)
        {
            int fire = 0;

            if(MasterController.GetMC().currentSiteModeScene != null)
            { 
                if (MasterController.GetMC().currentSiteModeScene.getSquadTile().getComponent<TileBase>().fireState == TileBase.FireState.START ||
                    MasterController.GetMC().currentSiteModeScene.getSquadTile().getComponent<TileBase>().fireState == TileBase.FireState.END)
                    fire = 1;
                if (MasterController.GetMC().currentSiteModeScene.getSquadTile().getComponent<TileBase>().fireState == TileBase.FireState.PEAK)
                    fire = 2;
            }
            bool liberalsArmed = false;

            //Unbreakable creatures never flee
            if ((actor.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.UNBREAKABLE) != 0) return false;

            //CCS bosses will never flee if defending in a CCS base as this makes the base impossible to capture
            if(MasterController.GetMC().currentSiteModeScene != null && MasterController.GetMC().currentSiteModeScene.location.hasComponent<SafeHouse>())
            {
                if((MasterController.GetMC().currentSiteModeScene.location.getComponent<SafeHouse>().getFlags() & LocationDef.BaseFlag.CCS_BASE) != 0
                    && !MasterController.GetMC().currentSiteModeScene.location.getComponent<SafeHouse>().owned
                    && actor.def == "CCS_ARCHCONSERVATIVE")
                {
                    return false;
                }
            }

            foreach (Entity lib in liberals)
            {
                if (lib == null) continue;
                if (lib.getComponent<Inventory>().isWeaponThreatening())
                {
                    liberalsArmed = true;
                    break;
                }
            }

            //Recent converts are more aggressive than normal people and don't care about the following morale checks
            if ((actor.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.CONVERTED) == 0)
            {
                //Non-conservatives will flee combat as soon as they can, unless they are police negotiators
                if (actor.getComponent<CreatureInfo>().alignment != Alignment.CONSERVATIVE && actor.def != "NEGOTIATOR")
                    return true;
                //Unarmed, no-juice conservatives will flee from armed liberals, based on a morale check affected by blood level
                if (actor.getComponent<CreatureBase>().Juice == 0 &&
                    actor.getComponent<Inventory>().weapon == null &&
                    liberalsArmed &&
                    (actor.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.BRAVE) == 0 &&
                    actor.getComponent<Body>().Blood < 70 + MasterController.GetMC().LCSRandom(61))
                    return true;
            }

            //Heavily wounded people will want to escape unless they are really politically motivated
            if (actor.getComponent<Body>().Blood < 45 && actor.getComponent<CreatureBase>().Juice < 200)
                return true;

            //Fire will scare away most people unless they're firefighters
            if (fire * MasterController.GetMC().LCSRandom(5) >= 3 && (actor.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.BRAVE_FIRE) == 0)
                return true;

            //If all checks fail, the creature is still up for a fight
            return false;
        }
    }
}