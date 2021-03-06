using System;
using System.Collections.Generic;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Item;
using LCS.Engine.UI.UIEvents;
using LCS.Engine.Events;
using LCS.Engine.Components.Location;
using LCS.Engine.Data;
using System.Xml;

namespace LCS.Engine.Components.Creature
{
    public class CreatureBase : Component
    {
        public Dictionary<string, CreatureAttribute> BaseAttributes { get; set; }
        public Dictionary<string, Skill> Skills { get; set; }

        [SimpleSave]
        public Entity Location;

        private int juice;
        public int Juice
        {
            get
            {
                return juice;
            }
            set
            {
                if(value < int.Parse(GameData.getData().globalVarsList["JUICEMIN"]))
                {
                    value = int.Parse(GameData.getData().globalVarsList["JUICEMIN"]);
                }
                if( value > int.Parse(GameData.getData().globalVarsList["JUICEMAX"]))
                {
                    value = int.Parse(GameData.getData().globalVarsList["JUICEMAX"]);
                }
                juice = value;
            }
        }

        public CreatureBase()
        {
            BaseAttributes = new Dictionary<string, CreatureAttribute>();
            Skills = new Dictionary<string, Skill>();
            Location = null;
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("CreatureBase");
                entityNode.AppendChild(saveNode);
            }

            saveSimpleFields();
            saveField(Juice, "Juice", saveNode);

            foreach(CreatureAttribute a in BaseAttributes.Values)
            {
                a.save(saveNode);
            }

            foreach(Skill s in Skills.Values)
            {
                s.save(saveNode);
            }
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);

            Juice = int.Parse(componentData.SelectSingleNode("Juice").InnerText);

            foreach(XmlNode attributeNode in componentData.SelectNodes("CreatureAttribute"))
            {
                CreatureAttribute c = new CreatureAttribute(attributeNode.SelectSingleNode("Type").InnerText, owner);
                c.Level = int.Parse(attributeNode.SelectSingleNode("Level").InnerText);
                BaseAttributes.Add(c.Type, c);
            }

            foreach(XmlNode skillNode in componentData.SelectNodes("Skill"))
            {
                Skill s = new Skill(skillNode.SelectSingleNode("type").InnerText, BaseAttributes[GameData.getData().skillList[skillNode.SelectSingleNode("type").InnerText].associatedAttribute.type]);
                s.level = int.Parse(skillNode.SelectSingleNode("level").InnerText);
                s.experience = int.Parse(skillNode.SelectSingleNode("experience").InnerText);
                Skills.Add(s.type, s);
            }
        }

        public override void selfSubscribe()
        {
            base.selfSubscribe();
            getAttributeModifiers += doGetAttributeModifiers;
        }

        public override void unsubscribe()
        {
            base.unsubscribe();
            getAttributeModifiers -= doGetAttributeModifiers;
        }

        protected override void depersistExtended()
        {
            foreach (CreatureAttribute a in BaseAttributes.Values)
            {
                a.depersist();
            }

            foreach (Skill s in Skills.Values)
            {
                s.depersist();
            }
        }

        public int getAttributeValue(string attributeName, string[] excludedMods = null)
        {
            return BaseAttributes[attributeName].getModifiedValue(excludedMods);
        }

        public int getSkillValue(string skillName)
        {
            return Skills[skillName].level;
        }

        public int getPower(string[] attributes, string[] skills)
        {
            int totalPower = 0;

            if(attributes != null)
                foreach(string s in attributes)
                    totalPower += getAttributeValue(s);

            if(skills != null)
                foreach(string s in skills)
                    totalPower += getSkillValue(s);

            return totalPower;
        }

        public Skill getBestSkill(List<string> skills = null)
        {
            if(skills == null)
            {
                skills = new List<string>(Skills.Keys);
            }

            if (skills.Count == 0) return null;

            Skill bestSkill = new Skill("BASE", BaseAttributes[Constants.ATTRIBUTE_AGILITY]);

            foreach(string s in skills)
            {
                if (Skills[s].level >= bestSkill.level) bestSkill = Skills[s];
            }

            return bestSkill;
        }

        private void doGetAttributeModifiers(object sender, GetAttributeModifiers args)
        {
            foreach (CreatureAttribute attribute in BaseAttributes.Values)
            {
                //juice shouldn't affect opposite aligned stat
                if (getComponent<CreatureInfo>().alignment == Alignment.LIBERAL && attribute.Type == "WISDOM")
                {
                    continue;
                }
                if (getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE && attribute.Type == "HEART")
                {
                    continue;
                }

                if (Juice <= -50) args.PostMultipliers[attribute.Type]["juice"] = 0; // Damn worthless (All stats 1)
                else if (Juice <= -10) args.PostMultipliers[attribute.Type]["juice"] = 0.6f; // Society's dregs
                else if (Juice < 0) args.PostMultipliers[attribute.Type]["juice"] = 0.8f;    // Punk
                else if (Juice < 10) { /* no changes*/ } //Civilian
                else if (Juice < 50) args.LinearModifiers[attribute.Type]["juice"] = 1; // Activist
                else if (Juice < 100) // Socialist Threat
                {
                    args.PreMultipliers[attribute.Type]["juice"] = 1.1f;
                    args.LinearModifiers[attribute.Type]["juice"] = 2;
                }
                else if (Juice < 200) // Revolutionary
                {
                    args.PreMultipliers[attribute.Type]["juice"] = 1.2f;
                    args.LinearModifiers[attribute.Type]["juice"] = 3;
                }
                else if (Juice < 500) // Urban Guerrilla
                {
                    args.PreMultipliers[attribute.Type]["juice"] = 1.3f;
                    args.LinearModifiers[attribute.Type]["juice"] = 4;
                }
                else if (Juice < 1000) // Liberal Guardian
                {
                    args.PreMultipliers[attribute.Type]["juice"] = 1.4f;
                    args.LinearModifiers[attribute.Type]["juice"] = 5;
                }
                else // Elite Liberal
                {
                    args.PreMultipliers[attribute.Type]["juice"] = 1.5f;
                    args.LinearModifiers[attribute.Type]["juice"] = 6;
                }
            }
        }

        public void addSkill(string type)
        {
            Skills[type] = new Skill(type, BaseAttributes[GameData.getData().skillList[type].associatedAttribute.type]);
        }

        public void addAttribute(string type)
        {
            BaseAttributes[type] = new CreatureAttribute(type, owner);
        }

        public void attack(Entity target, bool mistake = false, bool force_melee = false)
        {
            MasterController mc = MasterController.GetMC();

            string name;

            if (hasComponent<Liberal>())
                name = getComponent<CreatureInfo>().getName();
            else
                name = getComponent<CreatureInfo>().encounterName;

            string targetName;
            if (target.hasComponent<Liberal>())
                targetName = target.getComponent<CreatureInfo>().getName();
            else
                targetName = target.getComponent<CreatureInfo>().encounterName;

            //Check if the creature is incapacitated
            if (getComponent<Body>().incapacitated())
            {
                getIncapacitatedText();
                return;
            }

            if(getComponent<Body>().stunned > 0)
            {
                getComponent<Body>().stunned--;
                getStunnedText();
                return;
            }

            string logText = "";

            //If they can't even hold a weapon, their attack stops here.
            if (!getComponent<Body>().canGrasp())
            {
                switch (mc.LCSRandom(5))
                {
                    case 0: logText += name + " looks on with authority."; break;
                    case 1: logText += name + " waits patiently."; break;
                    case 2: logText += name + " sits in thought."; break;
                    case 3: logText += name + " breathes slowly."; break;
                    case 4: logText += name + " considers the situation."; break;
                }

                mc.addCombatMessage(logText);
                return;
            }

            AttackDef attackInfo = getComponent<Inventory>().getWeapon().getComponent<Weapon>().getAttack(false, force_melee);
            //If they are unarmed but have thrown weapons as ammo, use that as their attack instead (this is mainly to force a reload)
            if(getComponent<Inventory>().weapon == null &&
                getComponent<Inventory>().clips.Count > 0 &&
                getComponent<Inventory>().clips.Peek().hasComponent<Weapon>() &&
                (getComponent<Inventory>().clips.Peek().getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.THROWN) != 0 &&
                !force_melee)
            {
                attackInfo = getComponent<Inventory>().clips.Peek().getComponent<Weapon>().getAttack(false, force_melee);
            }

            if (attackInfo == null) return;

            //Reload check
            if (attackInfo.ammotype != "NONE" && getComponent<Inventory>().getWeapon().getComponent<Weapon>().needsReload())                
            {
                if (getComponent<Inventory>().reload(false))
                {
                    if ((getComponent<Inventory>().getWeapon().getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.THROWN) != 0)
                        logText += name + " readies another " + getComponent<Inventory>().getWeapon().getComponent<ItemBase>().getName();
                    else
                        logText += name + " reloads";
                    mc.addCombatMessage(logText);
                    return;
                }
                else
                {
                    attackInfo = getComponent<Inventory>().getWeapon().getComponent<Weapon>().getAttack(false, force_melee, true);
                }
            }

            if (attackInfo == null) return;

            //Similar to above, except that they only can't attack if they both can't walk and don't have a ranged weapon.
            if ((!getComponent<Body>().canWalk() || (mc.combatModifiers & MasterController.CombatModifiers.CHASE_CAR) != 0) && (attackInfo.flags & AttackDef.AttackFlags.RANGED) == 0)
            {
                switch (mc.LCSRandom(5))
                {
                    case 0: logText += name + " looks on with authority."; break;
                    case 1: logText += name + " waits patiently."; break;
                    case 2: logText += name + " sits in thought."; break;
                    case 3: logText += name + " breathes slowly."; break;
                    case 4: logText += name + " considers the situation."; break;
                }

                mc.addCombatMessage(logText);
                return;
            }

            //Music/persuasion attacks handled special
            if(attackInfo.damage_type == AttackDef.DamageType.MUSIC || attackInfo.damage_type == AttackDef.DamageType.PERSUASION)
            {
                string[] lawDebate =
                {
                    "debates the death penalty with",
                    "debates gay rights with",
                    "debates free speech with",
                    "debates the Second Amendment with"
                };
                string[] businessConDebate =
                {
                    "explains the derivatives market to",
                    "justifies voodoo economics to",
                    "extols the Reagan presidency to",
                    "argues about tax cuts with",
                    "explains Conservative philosophy to",
                    "extends a dinner invitation to",
                    "offers a VP position to",
                    "shows a $1000 bill to",
                    "debates fiscal policy with",
                    "offers stock options to"
                };

                string[] scienceDebate =
                {
                    "debates scientific ethics with",
                    "explains ethical research to",
                    "discusses the scientific method with"
                };

                string[] scienceConDebate =
                {
                    "debates scientific ethics with",
                    "explains the benefits of research to",
                    "discusses the scientific method with"
                };

                string[] businessDebate =
                {
                    "debates fiscal policy with",
                    "derides voodoo economics to",
                    "dismisses the Reagan presidency to",
                    "argues about tax cuts with",
                    "explains Liberal philosophy to"
                };

                string[] politicsConDebate =
                {
                    "debates the death penalty with",
                    "debates gay rights with",
                    "debates free speech with",
                    "debates the Second Amendment with",
                    "justifies voodoo economics to",
                    "extols the Reagan presidency to",
                    "argues about tax cuts with",
                    "explains Conservative philosophy to",
                    "extends a dinner invitation to",
                    "debates fiscal policy with",
                    "chats warmly with",
                    "smiles at"
                };

                string[] politicsDebate = 
                {
                    "debates the death penalty with",
                    "debates gay rights with",
                    "debates free speech with",
                    "debates the Second Amendment with",
                    "derides voodoo economics to",
                    "dismisses the Reagan presidency to",
                    "argues about tax cuts with",
                    "explains Liberal philosophy to",
                    "extends a dinner invitation to",
                    "debates fiscal policy with",
                    "chats warmly with",
                    "smiles at"
                };

                string[] mediaDebate =
                {
                    "winks at",
                    "smiles at",
                    "smirks at",
                    "chats warmly with",
                    "yells slogans at"
                };

                string[] militaryDebate =
                {
                    "recites the Pledge of Allegiance to",
                    "debates national security with",
                    "debates terrorism with",
                    "preaches about veterans to",
                    "explains military spending to"
                };

                int attack = Skills[attackInfo.skill.type].roll();
                string attackDescription = attackInfo.attack_description;
                if (getComponent<CreatureInfo>().alignment == Alignment.LIBERAL)
                {
                    attack += BaseAttributes[Constants.ATTRIBUTE_HEART].roll() + target.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].getModifiedValue();
                }
                else
                {
                    attack += BaseAttributes[Constants.ATTRIBUTE_WISDOM].roll() + target.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].getModifiedValue();
                }
                int resist = 0;
                if (target.getComponent<CreatureInfo>().alignment == Alignment.LIBERAL)
                    resist = target.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].roll();
                else
                    resist = target.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].roll();
                if (attackInfo.damage_type == AttackDef.DamageType.PERSUASION)
                {
                    //Debators get to use additional skills in their debates
                    if ((getFlags() & CreatureDef.CreatureFlag.DEBATE_LAW) != 0)
                    {
                        attack += Skills[Constants.SKILL_LAW].roll();
                        resist += target.getComponent<CreatureBase>().Skills[Constants.SKILL_LAW].roll();
                        attackDescription = lawDebate[mc.LCSRandom(lawDebate.Length)];
                    }
                    else if ((getFlags() & CreatureDef.CreatureFlag.DEBATE_SCIENCE) != 0)
                    {
                        attack += Skills[Constants.SKILL_SCIENCE].roll();
                        resist += target.getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].roll();
                        if (getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
                            attackDescription = scienceConDebate[mc.LCSRandom(scienceConDebate.Length)];
                        else
                            attackDescription = scienceDebate[mc.LCSRandom(scienceDebate.Length)];
                    }
                    else if ((getFlags() & CreatureDef.CreatureFlag.DEBATE_POLITICS) != 0)
                    {
                        attack += Skills[Constants.SKILL_LAW].roll();
                        resist += target.getComponent<CreatureBase>().Skills[Constants.SKILL_LAW].roll();
                        if (getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
                            attackDescription = politicsConDebate[mc.LCSRandom(politicsConDebate.Length)];
                        else
                            attackDescription = politicsDebate[mc.LCSRandom(politicsDebate.Length)];
                    }
                    else if ((getFlags() & CreatureDef.CreatureFlag.DEBATE_BUSINESS) != 0)
                    {
                        attack += Skills[Constants.SKILL_BUSINESS].roll();
                        resist += target.getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].roll();
                        if (getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
                            attackDescription = businessConDebate[mc.LCSRandom(businessConDebate.Length)];
                        else
                            attackDescription = businessDebate[mc.LCSRandom(businessDebate.Length)];
                    }
                    else if ((getFlags() & CreatureDef.CreatureFlag.DEBATE_MEDIA) != 0)
                    {
                        attack += BaseAttributes[Constants.ATTRIBUTE_CHARISMA].roll();
                        resist += target.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_CHARISMA].roll();
                        attackDescription = mediaDebate[mc.LCSRandom(mediaDebate.Length)];
                    }
                    else if ((getFlags() & CreatureDef.CreatureFlag.DEBATE_MILITARY) != 0)
                    { 
                        attack += BaseAttributes[Constants.ATTRIBUTE_CHARISMA].roll();
                        resist += target.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_CHARISMA].roll();
                        attackDescription = militaryDebate[mc.LCSRandom(militaryDebate.Length)];
                    }                    
                }
                Skills[attackInfo.skill.type].addExperience(mc.LCSRandom(resist) + 1);

                logText += name + " " + attackDescription + " " + targetName;
                
                if ((target.hasComponent<Liberal>() && target.getComponent<Liberal>().recruitType == Liberal.RecruitType.ENLIGHTENED) ||
                    (getComponent<Body>().getSpecies().type != "HUMAN" && 
                    !(getComponent<Body>().getSpecies().type == "DOG" && 
                    MasterController.government.laws[Constants.LAW_ANIMAL_RESEARCH].alignment == Alignment.ELITE_LIBERAL)))
                {
                    logText += " but " + targetName + " is immune to the attack!";
                }
                else if(target.getComponent<CreatureInfo>().alignment == getComponent<CreatureInfo>().alignment)
                {
                    logText += ". " + targetName + " already agrees with " + name + "!";
                }
                else if(attack > resist)
                {
                    target.getComponent<Body>().stunned += (attack - resist) / 4;
                    if (getComponent<CreatureInfo>().alignment != Alignment.LIBERAL)
                    {
                        if(target.getComponent<CreatureBase>().juice > 100)
                        {
                            logText += ". " + targetName + " loses Juice!";
                            target.getComponent<CreatureBase>().juiceMe(-50, 100);
                        }
                        else if(mc.LCSRandom(15) > target.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].getModifiedValue() ||
                            target.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].getModifiedValue() < target.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].getModifiedValue())
                        {
                            logText += ". " + targetName + " is tainted with Wisdom!";
                            target.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level++;
                        }
                        else if(target.hasComponent<Liberal>() && target.getComponent<Liberal>().recruitType == Liberal.RecruitType.LOVE_SLAVE)
                        {
                            logText += ". " + targetName + " can't bear to leave " + target.getComponent<Liberal>().leader.getComponent<CreatureInfo>().getName() + "!";
                        }
                        else
                        {
                            if (getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
                            {
                                logText += ". " + targetName + " is turned Conservative!";
                                target.getComponent<Body>().stunned = 0;
                                target.getComponent<CreatureInfo>().alignment = Alignment.CONSERVATIVE;
                                target.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.CONVERTED;
                                target.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;
                                if (target.hasComponent<Liberal>())
                                {
                                    target.getComponent<Liberal>().squad.Remove(target);
                                    if (target.getComponent<Liberal>().hauledUnit != null)
                                    {
                                        Entity haulee = target.getComponent<Liberal>().hauledUnit;
                                        Scenes.Fight.dropHauledUnit(target);
                                        if (haulee.hasComponent<Liberal>())
                                            Scenes.Fight.tryHaulLib(haulee, target.getComponent<Liberal>().squad);
                                    }
                                    if (mc.currentSiteModeScene != null) mc.currentSiteModeScene.encounterEntities.Add(target);
                                    target.getComponent<Liberal>().leaveLCS();
                                }
                            }
                            else
                            {
                                logText += ". " + targetName + " doesn't want to fight anymore.";
                                target.getComponent<Body>().stunned = 0;
                                target.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.CONVERTED;
                                target.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.NO_BLUFF;
                                if (target.hasComponent<Liberal>())
                                {
                                    if (target.getComponent<Liberal>().hauledUnit != null)
                                    {
                                        Entity haulee = target.getComponent<Liberal>().hauledUnit;
                                        Scenes.Fight.dropHauledUnit(target);
                                        if (haulee.hasComponent<Liberal>())
                                            Scenes.Fight.tryHaulLib(haulee, target.getComponent<Liberal>().squad);
                                    }
                                    target.getComponent<Liberal>().squad.Remove(target);
                                    if (mc.currentSiteModeScene != null) mc.currentSiteModeScene.encounterEntities.Add(target);
                                    target.getComponent<Liberal>().leaveLCS();
                                }
                            }
                        }
                    }
                    else
                    {
                        if (target.getComponent<CreatureBase>().juice >= 100)
                        {
                            logText += ". " + targetName + " seems less badass!";
                            target.getComponent<CreatureBase>().juiceMe(-50, 99);
                        }
                        else if (!target.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].check(Difficulty.AVERAGE) ||
                            target.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].getModifiedValue() < target.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].getModifiedValue())
                        {
                            logText += ". " + targetName + "'s Heart swells!";
                            target.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].Level++;
                        }
                        else
                        {
                            logText += ". " + targetName + " has turned Liberal!";
                            target.getComponent<Body>().stunned = 0;
                            target.getComponent<CreatureInfo>().alignment = Alignment.LIBERAL;
                            target.getComponent<CreatureInfo>().flags |= CreatureInfo.CreatureFlag.CONVERTED;
                            target.getComponent<CreatureInfo>().flags &= ~CreatureInfo.CreatureFlag.NO_BLUFF;
                        }
                    }
                }
                else
                {
                    logText += ". " + targetName + " remains strong.";
                }

                mc.addCombatMessage(logText);
                return;
            }

            //Check if they attack the human shield instead
            if (target.hasComponent<Liberal>() && target.getComponent<Liberal>().hauledUnit != null && mc.LCSRandom(2) == 0)
            {
                target = target.getComponent<Liberal>().hauledUnit;
                mistake = true;                
            }

            if(MasterController.news.currentStory != null && hasComponent<Liberal>())
            {
                if (mistake)
                {
                    MasterController.news.currentStory.addCrime("ATTACKED_MISTAKE");
                    mc.currentSiteModeScene.alienateCheck(mistake);
                    if(mc.currentSiteModeScene != null)
                        mc.currentSiteModeScene.siteCrime += 10;
                }
                else
                {
                    MasterController.news.currentStory.addCrime("ATTACKED");
                    if (mc.currentSiteModeScene != null)
                        mc.currentSiteModeScene.siteCrime += 3;
                }
            }

            //recheck targetname in case they attacked a human shield
            if (target.hasComponent<Liberal>())
                targetName = target.getComponent<CreatureInfo>().getName();
            else
                targetName = target.getComponent<CreatureInfo>().encounterName;

            int attackBonus = 0;

            int attackRoll = Skills[attackInfo.skill.type].roll();
            int dodgeRoll = target.getComponent<CreatureBase>().Skills["DODGE"].roll() / 2;

            if((mc.combatModifiers & MasterController.CombatModifiers.CHASE_CAR) != 0)
            {
                Vehicle targetVehicle = target.getComponent<Inventory>().tempVehicle.getComponent<Vehicle>();
                Vehicle shooterVehicle = getComponent<Inventory>().tempVehicle.getComponent<Vehicle>();
                dodgeRoll = targetVehicle.dodgeRoll();
                if (shooterVehicle.driver == owner)
                    attackBonus += shooterVehicle.getVehicleData().attackDriver;
                else
                    attackBonus += shooterVehicle.getVehicleData().attackPassenger;
            }

            target.getComponent<CreatureBase>().Skills["DODGE"].addExperience(attackRoll * 2);
            Skills[attackInfo.skill.type].addExperience(dodgeRoll*2 + 5);

            if (target.hasComponent<Liberal>() && target.getComponent<Liberal>().hauledUnit != null) attackBonus -= mc.LCSRandom(10);
            if (hasComponent<Liberal>() && getComponent<Liberal>().hauledUnit != null) attackRoll -= mc.LCSRandom(10);

            attackRoll -= getComponent<Body>().healthModRoll();
            if ((mc.combatModifiers & MasterController.CombatModifiers.CHASE_CAR) != 0)
                dodgeRoll -= target.getComponent<Inventory>().tempVehicle.getComponent<Vehicle>().driver.getComponent<Body>().healthModRoll();
            else
                dodgeRoll -= target.getComponent<Body>().healthModRoll();
            if ((mc.combatModifiers & MasterController.CombatModifiers.CHASE_FOOT) != 0) dodgeRoll -= target.getComponent<Body>().healthModRoll();

            if (attackRoll < 0) attackRoll = 0;
            if (dodgeRoll < 0) dodgeRoll = 0;

            attackBonus += attackInfo.accuracy_bonus;

            bool sneakAttack = false;
            if (mc.currentSiteModeScene != null && 
                !mc.currentSiteModeScene.alarmTriggered && 
                (attackInfo.flags & AttackDef.AttackFlags.BACKSTAB) != 0 &&
                hasComponent<Liberal>())
            {
                sneakAttack = true;
            }
            int burstHits = 0;

            //Hard-coded Unarmed strike attacks
            if (attackInfo.type == "UNARMED")
            {
                burstHits = 1 + mc.LCSRandom(Skills[attackInfo.skill.type].level / 3 + 1);
                if (burstHits > 5) burstHits = 5;
            }
            else
            {
                if(mc.currentSiteModeScene != null)
                {
                    if(mc.LCSRandom(100) < attackInfo.fireChanceCauseDebris)
                    {
                        //mc.currentSiteModeScene.getSquadTile().getComponent<TileBase>().fireState = TileBase.FireState.DEBRIS;
                    }

                    if(mc.LCSRandom(100) < attackInfo.fireChance &&
                        mc.currentSiteModeScene.getSquadTile().getComponent<TileBase>().fireState == TileBase.FireState.NONE)
                    {
                        mc.currentSiteModeScene.getSquadTile().getComponent<TileBase>().fireState = TileBase.FireState.START;
                        mc.currentSiteModeScene.siteCrime += 3;
                        juiceMe(5, 500);
                        MasterController.news.currentStory.addCrime(Constants.CRIME_ARSON);
                        if (hasComponent<Liberal>())
                        {
                            foreach (Entity e in getComponent<Liberal>().squad)
                                e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_ARSON);
                        }
                    }
                }


                for (int i = 0; i < attackInfo.number_attacks; i++)
                {
                    if (attackInfo.ammotype != "NONE")
                    {
                        if ((getComponent<Inventory>().getWeapon().getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.THROWN) != 0)
                            getComponent<Inventory>().destroyWeapon(false);
                        else if (getComponent<Inventory>().getWeapon().getComponent<Weapon>().clip.getComponent<Clip>().ammo > 0)
                            getComponent<Inventory>().getWeapon().getComponent<Weapon>().clip.getComponent<Clip>().ammo--;
                        else break;
                    }

                    if (attackRoll + attackBonus - i * attackInfo.successive_attacks_difficulty > dodgeRoll)
                        burstHits++;
                }
            }

            bool suspisciousTarget = (mc.currentSiteModeScene != null && mc.currentSiteModeScene.suspicionTimer == 0) || (target.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.NO_BLUFF) != 0;

            if (sneakAttack)
            {
                //Sneak attacks only strike once
                burstHits = 1;

                dodgeRoll = target.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].roll() / 2;
                //Conservatives that are suspicious will be more wary of attack.
                if (suspisciousTarget)
                    dodgeRoll *= 2;
                attackRoll += Skills[Constants.SKILL_STEALTH].roll();
            }

            logText = name + " " + (mistake ? "MISTAKENLY " : "");

            if (attackInfo.type == "UNARMED")
            {
                if (mc.LCSRandom(Skills["MARTIAL_ARTS"].level + 1) == 0)
                    logText += "punches";
                else if (mc.LCSRandom(Skills["MARTIAL_ARTS"].level) == 0)
                    logText += "swings at";
                else if (mc.LCSRandom(Skills["MARTIAL_ARTS"].level - 1) == 0)
                    logText += "grapples with";
                else if (mc.LCSRandom(Skills["MARTIAL_ARTS"].level - 2) == 0)
                    logText += "kicks";
                else if (mc.LCSRandom(Skills["MARTIAL_ARTS"].level - 3) == 0)
                    logText += "strikes at";
                else if (mc.LCSRandom(Skills["MARTIAL_ARTS"].level - 4) == 0)
                    logText += "jump kicks";
                else
                    logText += "gracefully strikes at";
            }
            else if (sneakAttack)
            {
                if (suspisciousTarget)
                    logText += "surprises";
                else
                    logText += "sneaks up on";
            }
            else
                logText += attackInfo.attack_description;

            logText += " " + targetName;

            mc.addCombatMessage("##DEBUG## attackRoll=" + attackRoll + " defenseRoll=" + dodgeRoll + " attackBonus=" + attackBonus);

            if (attackRoll + attackBonus > dodgeRoll)
            {
                logText += target.getComponent<Body>().takeHit(attackRoll - dodgeRoll, burstHits, attackInfo, owner, sneakAttack);
            }
            else
            {
                logText += "\n";
                if ((mc.combatModifiers & MasterController.CombatModifiers.CHASE_CAR) == 0)
                {
                    if (sneakAttack)
                    {
                        logText += targetName;
                        switch (mc.LCSRandom(4))
                        {
                            case 0: logText += " notices at the last moment!"; break;
                            case 1: logText += " wasn't born yesterday!"; break;
                            case 2: logText += " spins and blocks the attack!"; break;
                            default: logText += " jumps back and cries out in alarm!"; break;
                        }
                    }
                    else
                    {
                        if (target.getComponent<CreatureBase>().Skills["DODGE"].check(Difficulty.AVERAGE))
                        {
                            logText += targetName;
                            switch (mc.LCSRandom(4))
                            {
                                case 0: logText += " gracefully dives to avoid the attack!"; break;
                                case 1: logText += " does the Matrix-dodge!"; break;
                                case 2: logText += " leaps for cover!"; break;
                                default: logText += " avoids the attack with no difficulty at all!"; break;
                            }
                        }
                        else
                        {
                            logText += name + " misses.";
                        }
                    }
                }
                else
                {
                    Entity targetDriver = target.getComponent<Inventory>().tempVehicle.getComponent<Vehicle>().driver;
                    if (target.getComponent<Inventory>().tempVehicle.getComponent<Vehicle>().dodgeRoll() >= (int)Difficulty.AVERAGE)
                    {
                        if (targetDriver.hasComponent<Liberal>())
                            logText += target.getComponent<CreatureInfo>().getName();
                        else
                            logText += target.getComponent<CreatureInfo>().encounterName;
                        switch (mc.LCSRandom(4))
                        {
                            case 0: logText += " swerves away from the shot!"; break;
                            case 1: logText += " drops behind a hill in the road!"; break;
                            case 2: logText += " dodges behind a hot dog cart!"; break;
                            default: logText += " changes lanes at the last second!"; break;
                        }
                    }
                    else
                    {
                        logText += name + " misses.";
                    }
                }
            }

            mc.addCombatMessage(logText);
            if (mc.currentSiteModeScene != null)
            {
                //If this was a sneak attack and the target was killed, then the alarm isn't raised. Add to stealth skill.
                if (sneakAttack && !target.getComponent<Body>().Alive)
                {
                    if (mc.currentSiteModeScene.suspicionTimer < 0 ||
                        mc.currentSiteModeScene.suspicionTimer > 10)
                        mc.currentSiteModeScene.suspicionTimer = 10;
                    Skills[Constants.SKILL_STEALTH].addExperience(10);
                }
                else
                {
                    mc.currentSiteModeScene.alarmTriggered = true;
                }
            }
        }

        public string getRankName()
        {
            if (getComponent<CreatureInfo>().alignment == Alignment.LIBERAL)
            {
                if (Juice == -50)
                {
                    return MasterController.GetMC().swearFilter("Damn", "Darn") + " Worthless";
                }
                else if (Juice < -10) return "Society's Dregs";
                else if (Juice < 0) return "Punk";
                else if (Juice < 10) return "Civilian";
                else if (Juice < 50) return "Activist";
                else if (Juice < 100) return "Socialist Threat";
                else if (Juice < 200) return "Revolutionary";
                else if (Juice < 500) return "Urban Commando";
                else if (Juice < 1000) return "Liberal Guardian";
                else if (Juice == 1000) return "Elite Liberal";
            }
            else if (getComponent<CreatureInfo>().alignment == Alignment.MODERATE)
            {
                if (Juice == -50)
                {
                    return MasterController.GetMC().swearFilter("Damn", "Darn") + " Worthless";
                }
                else if (Juice < -10) return "Filthy Neutral"; //I have no strong feelings one way or the other
                else if (Juice < 0) return "Non-Liberal Punk";
                else if (Juice < 10) return "Non-Liberal";
                else if (Juice < 50) return "Hard Working";
                else if (Juice < 100) return "Respected";
                else if (Juice < 200) return "Upstanding Citizen";
                else if (Juice < 500) return "Great Person";
                else if (Juice < 1000) return "Peacemaker";
                else if (Juice == 1000) return "Peace Prize Winner";
            }
            else if (getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
            {
                if (Juice == -50)
                {
                    return MasterController.GetMC().swearFilter("Damn", "Darn") + " Worthless";
                }
                else if (Juice < -10) return "Conservative Dregs";
                else if (Juice < 0) return "Conservative Punk";
                else if (Juice < 10) return "Mindless Conservative";
                else if (Juice < 50) return "Wrong-Thinker";
                else if (Juice < 100) return "Stubborn as " + MasterController.GetMC().swearFilter("Hell", "Heck");
                else if (Juice < 200) return "Heartless " + MasterController.GetMC().swearFilter("Bastard", "Jerk");
                else if (Juice < 500) return "Insane Vigilante";
                else if (Juice < 1000) return "Arch-Conservative";
                else if (Juice == 1000) return "Evil Incarnate";
            }


            return "";
        }

        //Events
        public event EventHandler<GetAttributeModifiers> getAttributeModifiers;
        public void doGetAttributeModifiers(GetAttributeModifiers args)
        {
            if(getAttributeModifiers != null)
                getAttributeModifiers(this, args);
        }

        public event EventHandler<Die> die;
        public void doDie(Die args)
        {
            if(die != null)
                die(this, args);

            MasterController.GetMC().doSomeoneDied(owner);
        }

        public void juiceMe(int juice, int max = int.MaxValue)
        {
            if (juice == 0) return;

            //Cap default should be negative if juice is below 0
            if(juice < 0 && max == int.MaxValue) max = int.MinValue;

            //Send juice up the chain, if positive
            if (hasComponent<Liberal>() && getComponent<Liberal>().leader != null && juice > 0)
            {
                getComponent<Liberal>().leader.getComponent<CreatureBase>().juiceMe(juice / 5, Juice + juice > max ? Juice : Juice + juice);
            }

            if ((juice > 0 && Juice >= max) || (juice < 0 && Juice <= max)) return;

            if (juice > 0 && Juice + juice > max) Juice = max;
            else if (juice < 0 && Juice + juice < max) Juice = max;
            else Juice += juice;
        }

        private void getIncapacitatedText()
        {
            MasterController mc = MasterController.GetMC();

            string name;

            if (hasComponent<Liberal>()) name = getComponent<CreatureInfo>().getName();
            else name = getComponent<CreatureInfo>().encounterName;

            string logText = "<color=yellow>" + name;
            string attackerDialogText = "";

            switch (mc.LCSRandom(54))
            {
                case 0: logText += " desperately cries out to Jesus.";
                    attackerDialogText = "Oh Jesus, help me now!";
                    break;
                case 1:
                    logText += " " + mc.swearFilter("soils the floor", "makes a stinky");
                    break;
                case 2:
                    logText += " whimpers in a corner.";
                    attackerDialogText = "*whimpers*";
                    break;
                case 3:
                    logText += " begins to weep.";
                    attackerDialogText = "*weeps*";
                    break;
                case 4:
                    logText += " vomits.";
                    attackerDialogText = "BARF!";
                    break;
                case 5:
                    logText += " chortles...";
                    break;
                case 6:
                    logText += " screams in pain.";
                    attackerDialogText = "AAAAGGGHH!";
                    break;
                case 7:
                    logText += " asks for mother.";
                    attackerDialogText = "Mother!";
                    break;
                case 8:
                    logText += " prays softly...";
                    attackerDialogText = "Even though I walk through the valley of the shadow of death...";
                    break;
                case 9:
                    logText += " clutches at " + getComponent<CreatureInfo>().hisHer().ToLower() +" wounds."; break;
                case 10:
                    logText += " reaches out and moans.";
                    attackerDialogText = "*moaaaan...*";
                    break;
                case 11:
                    logText += " hollers in pain.";
                    attackerDialogText = "AAAAGGGHH!";
                    break;
                case 12:
                    logText += " groans in agony.";
                    attackerDialogText = "*groan*";
                    break;
                case 13:
                    logText += " begins hyperventilating.";
                    break;
                case 14:
                    logText += " shouts a prayer.";
                    attackerDialogText = "LORD DON'T TAKE ME YET";
                    break;
                case 15:
                    logText += " coughs up blood.";
                    break;
                case 16:
                    if ((mc.combatModifiers & MasterController.CombatModifiers.CHASE_CAR) == 0) logText += " stumbles against a wall.";
                    else logText += " leans against the door.";
                    break;
                case 17:
                    logText += " begs for forgiveness.";
                    attackerDialogText = "Please forgive me!";
                    break;
                case 18:
                    logText += " shouts \"Why have you forsaken me?\"";
                    attackerDialogText = "WHY HAVE YOU FORSAKEN ME?";
                    break;
                case 19:
                    logText += " murmurs \"Why Lord?   Why?\"";
                    attackerDialogText = "Why Lord? Why?";
                    break;
                case 20:
                    logText += " whispers \"Am I dead?\"";
                    attackerDialogText = "Am I dead?";
                    break;
                case 21:                    
                    logText += " " + mc.swearFilter("pisses on the floor", "makes a mess") + ", moaning";
                    attackerDialogText = "*moan*";
                    break;
                case 22:
                    logText += " whispers incoherently.";
                    break;
                case 23:
                    if (getComponent<Body>().canSee() && !getComponent<Body>().missingEyes())
                        logText += " stares off into space.";
                    else if (getComponent<Body>().canSee())
                        logText += " stares off into space with one empty eye.";
                    else
                        logText += " stares out with hollow sockets.";
                    break;
                case 24:
                    logText += " cries softly.";
                    attackerDialogText = "*cries*";
                    break;
                case 25:
                    logText += " yells until the scream cracks dry.";
                    attackerDialogText = "AAAAAAAAAAAAAAAAKKKHKHHKHkhkh...";
                    break;
                case 26:
                    if (getComponent<Body>().getOrganCount("TOOTH") > 1)
                        logText += "'s teeth start chattering.";
                    else if (getComponent<Body>().getOrganCount("TOOTH") == 1)
                        logText += "'s tooth starts chattering.";
                    else
                        logText += "'s gums start chattering.";
                    break;
                case 27:
                    logText += " starts shaking uncontrollably.";
                    break;
                case 28:
                    logText += " looks strangely calm.";
                    break;
                case 29:
                    logText += " nods off for a moment.";
                    break;
                case 30:
                    logText += " starts drooling.";
                    break;
                case 31:
                    logText += " seems lost in memories.";
                    break;
                case 32:
                    logText += " shakes with fear.";
                    break;
                case 33:
                    logText += " murmurs \"I'm so afraid...\"";
                    attackerDialogText = "I'm so afraid...";
                    break;
                case 34:
                    logText += " cries \"It can't be like this...\"";
                    attackerDialogText = "It can't be like this...";
                    break;
                case 35:
                    if (getComponent<Age>().getAge() < 20 && getComponent<Body>().type == "HUMAN")
                    {
                        logText += " cries \"Mommy!\"";
                        attackerDialogText = "Mommy!";
                    }
                    else switch (getComponent<Body>().type)
                        {
                            case "GENETIC":
                                logText += " murmurs \"What about my offspring?\"";
                                attackerDialogText = "What about my offspring?";
                                break;
                            case "DOG":
                                logText += " murmurs \"What about my puppies?\"";
                                attackerDialogText = "What about my puppies?";
                                break;
                            default:
                                logText += " murmurs \"What about my children?\"";
                                attackerDialogText = "What about my children?";
                                break;
                        }
                    break;
                case 36:
                    logText += " shudders quietly.";
                    break;
                case 37:
                    logText += " yowls pitifully.";
                    break;
                case 38:
                    logText += " begins losing faith in God.";
                    break;
                case 39:
                    logText += " muses quietly about death.";
                    break;
                case 40:
                    logText += " asks for a blanket.";
                    attackerDialogText = "I'm so cold... I need a blanket...";
                    break;
                case 41:
                    logText += " shivers softly.";
                    break;
                case 42:
                    logText += " " + mc.swearFilter("vomits up a clot of blood", "makes a mess") + ".";
                    break;
                case 43:
                    logText += " " + mc.swearFilter("spits up a cluster of bloody bubbles", "makes a mess") + ".";
                    break;
                case 44:
                    logText += " pleads for mercy.";
                    attackerDialogText = "Please... have mercy.";
                    break;
                case 45:
                    logText += " quietly asks for coffee.";
                    attackerDialogText = "I'm so tired... please... coffee...";
                    break;
                case 46:
                    logText += " looks resigned.";
                    break;
                case 47:
                    logText += " scratches at the air.";
                    break;
                case 48:
                    logText += " starts to giggle uncontrollably.";
                    attackerDialogText = "*giggles*";
                    break;
                case 49:
                    logText += " wears a look of pain.";
                    break;
                case 50:
                    logText += " questions God.";
                    attackerDialogText = "Why, God?";
                    break;
                case 51:
                    logText += " whispers \"Mama baby.  Baby loves mama.\"";
                    attackerDialogText = "Mama baby. Baby loves mama...";
                    break;
                case 52:
                    logText += " asks for childhood toys frantically.";
                    attackerDialogText = "My toys... where are my toys?";
                    break;
                case 53:
                    logText += " murmurs \"But I go to church...\"";
                    attackerDialogText = "But I go to church...";
                    break;
            }

            logText += "</color>";

            mc.addCombatMessage(logText);
            if (attackerDialogText != "")
            {
                mc.uiController.doSpeak(new Speak(owner, attackerDialogText));
            }
        }

        private void getStunnedText()
        {
            MasterController mc = MasterController.GetMC();

            string name;

            if (hasComponent<Liberal>()) name = getComponent<CreatureInfo>().getName();
            else name = getComponent<CreatureInfo>().encounterName;

            string logText = "<color=yellow>" + name;
            string attackerDialogText = "";

            switch (mc.LCSRandom(11))
            {
                case 0:
                    logText += " seems hesitant.";
                    break;
                case 1:
                    logText += " is caught in self-doubt.";
                    break;
                case 2:
                    logText += " looks around uneasily.";
                    break;
                case 3:
                    logText += " beings to weep.";
                    break;
                case 4:
                    logText += " asks \"Is this right?\"";
                    attackerDialogText = "Is this right?";
                    break;
                case 5:
                    logText += " asks for guidance.";
                    break;
                case 6:
                    logText += " is caught in indecision";
                    break;
                case 7:
                    logText += " feels numb.";
                    break;
                case 8:
                    logText += " prays softly.";
                    break;
                case 9:
                    logText += " searches for the truth.";
                    break;
                case 10:
                    logText += " tears up.";
                    break;
            }

            logText += "</color>";

            mc.addCombatMessage(logText);
            if (attackerDialogText != "")
            {
                mc.uiController.doSpeak(new Speak(owner, attackerDialogText));
            }
        }

        public CreatureDef.CreatureFlag getFlags()
        { return GameData.getData().creatureDefList[owner.def].flags; }

        public class Skill : IComparable<Skill>
        {
            public CreatureAttribute associatedAttribute { get; set; }
            public string type { get; set; }
            public int level { get; set; }
            public int experience { get; set; }

            private XmlNode saveNode;
            private Dictionary<string, XmlNode> saveNodeList;

            public Skill(string type, CreatureAttribute associatedAttribute)
            {
                this.type = type;
                this.level = 0;
                this.experience = 0;
                this.associatedAttribute = associatedAttribute;
            }

            public void save(XmlNode baseNode)
            {
                if (saveNode == null)
                {
                    saveNodeList = new Dictionary<string, XmlNode>();
                    saveNode = baseNode.OwnerDocument.CreateElement("Skill");
                    baseNode.AppendChild(saveNode);
                    XmlNode typeNode = saveNode.OwnerDocument.CreateElement("type");
                    saveNode.AppendChild(typeNode);
                    saveNodeList.Add("type", typeNode);
                    XmlNode levelNode = saveNode.OwnerDocument.CreateElement("level");
                    saveNode.AppendChild(levelNode);
                    saveNodeList.Add("level", levelNode);
                    XmlNode experienceNode = saveNode.OwnerDocument.CreateElement("experience");
                    saveNode.AppendChild(experienceNode);
                    saveNodeList.Add("experience", experienceNode);
                }

                saveNodeList["type"].InnerText = type;
                saveNodeList["level"].InnerText = level + "";
                saveNodeList["experience"].InnerText = experience + "";
            }

            public void depersist()
            {
                saveNode = null;
                if(saveNodeList != null)
                    saveNodeList.Clear();
            }

            public void addExperience(int exp, int limit = int.MaxValue)
            {
                int cap = associatedAttribute.getModifiedValue();

                if (level >= cap || level >= limit || exp < 0) return;

                experience += (int) Math.Max(1, exp * associatedAttribute.getModifiedValue(new string[] { "juice" }) / 6.0);
                while(experience >= 100 + 10*level)
                {
                    if(level >= cap || level >= limit)
                    {
                        experience = 0;
                    }
                    else
                    {
                        experience -= 100 + 10*level;
                        level++;

                        if (level == cap || level == limit) experience = 0;
                    }
                }
            }

            public bool check(Difficulty difficulty, int modifier = 0)
            {
                return roll(modifier) >= (int) difficulty;
            }

            public int roll(int modifier = 0)
            {
                MasterController mc = MasterController.GetMC();

                //Untrained skills always fail if they are too technical to be done by amateurs
                if((GameData.getData().skillList[type].flags & SkillDef.SkillFlag.TRAINED_ONLY) != 0 && level < 1)
                {
                    return 0;
                }

                int totalStrength = level;

                //Specialty skills are too specialized to benefit from natural ability, and so don't use associated attribute for dice total
                if ((GameData.getData().skillList[type].flags & SkillDef.SkillFlag.SPECIALTY) != 0)
                {
                    totalStrength += level;
                }
                else
                {
                    totalStrength += Math.Min(associatedAttribute.getModifiedValue() / 2, level + 3);
                }

                totalStrength += modifier;            

                List<int> rolls = new List<int>();                

                for(int i = 0; i < totalStrength / 3; i++)
                {
                    rolls.Add(mc.LCSRandom(6) + 1);
                }

                if (totalStrength % 3 == 1) rolls.Add(mc.LCSRandom(3) + 1);
                else if (totalStrength % 3 == 2) rolls.Add(mc.LCSRandom(5) + 1);

                rolls.Sort();
                rolls.Reverse();

                int total = 0;
                for(int i=0;i < rolls.Count && i < 3; i++)
                {
                    total += rolls[i];
                }

                if (mc.SkillRollDebug)
                {
                    string debugMessage = "##DEBUG## Skill " + type + " Roll: Power=" + totalStrength + " Rolls=";
                    foreach(int i in rolls)
                    {
                        debugMessage += i + ",";
                    }

                    debugMessage = debugMessage.TrimEnd(',');
                    debugMessage += " Total=" + total;
                
                    mc.addMessage(debugMessage);
                    mc.addCombatMessage(debugMessage);
                }

                return total;
            }

            public int CompareTo(Skill s)
            {
                int compare = s.level.CompareTo(this.level);

                if (compare == 0)
                {
                    compare = s.experience.CompareTo(this.experience);
                }

                if (compare == 0)
                {
                    compare = this.type.CompareTo(s.type);
                }

                return compare;
            }

            public SkillDef.SkillFlag getFlags()
            { return GameData.getData().skillList[type].flags; }
        }

        public class CreatureAttribute
        {
            private Entity owner;
            private XmlNode saveNode;
            private Dictionary<string, XmlNode> saveNodeList;

            public string Type { get; set; }
            public int Level { get; set; }

            public CreatureAttribute(string type, Entity owner)
            {
                Type = type;
                Level = 0;
                this.owner = owner;
            }

            public void save(XmlNode baseNode)
            {
                if (saveNode == null)
                {
                    saveNodeList = new Dictionary<string, XmlNode>();
                    saveNode = baseNode.OwnerDocument.CreateElement("CreatureAttribute");
                    baseNode.AppendChild(saveNode);
                    XmlNode typeNode = saveNode.OwnerDocument.CreateElement("Type");
                    saveNode.AppendChild(typeNode);
                    saveNodeList.Add("Type", typeNode);
                    XmlNode levelNode = saveNode.OwnerDocument.CreateElement("Level");
                    saveNode.AppendChild(levelNode);
                    saveNodeList.Add("Level", levelNode);
                }

                saveNodeList["Type"].InnerText = Type;
                saveNodeList["Level"].InnerText = Level + "";
            }

            public void depersist()
            {
                saveNode = null;
                if(saveNodeList != null)
                    saveNodeList.Clear();
            }

            public bool check(Difficulty difficulty)
            {
                return roll() >= (int)difficulty;
            }

            public int roll()
            {
                List<int> rolls = new List<int>();

                for (int i = 0; i < getModifiedValue() / 3; i++)
                {
                    rolls.Add(MasterController.GetMC().LCSRandom(6) + 1);
                }

                if (getModifiedValue() % 3 == 1) rolls.Add(MasterController.GetMC().LCSRandom(3) + 1);
                else if (getModifiedValue() % 3 == 2) rolls.Add(MasterController.GetMC().LCSRandom(5) + 1);

                rolls.Sort();
                rolls.Reverse();

                int total = 0;
                for (int i = 0; i < rolls.Count && i < 3; i++)
                {
                    total += rolls[i];
                }

                return total;
            }

            public int getModifiedValue(string[] excludedMods = null)
            {
                GetAttributeModifiers mods = new GetAttributeModifiers();
                owner.getComponent<CreatureBase>().doGetAttributeModifiers(mods);

                int totalMods = 0;
                float preMultiplier = 1;
                float postMultiplier = 1;
                int finalValue = 0;

                foreach (string mod in mods.PreMultipliers[Type].Keys)
                {
                    bool exclude = false;
                    if (excludedMods != null)
                    {
                        foreach (string s in excludedMods)
                            if (s == mod)
                            {
                                exclude = true;
                                break;
                            }
                    }

                    if (!exclude) preMultiplier *= mods.PreMultipliers[Type][mod];
                }

                foreach (string mod in mods.LinearModifiers[Type].Keys)
                {
                    bool exclude = false;
                    if (excludedMods != null)
                    {
                        foreach (string s in excludedMods)
                            if (s == mod)
                            {
                                exclude = true;
                                break;
                            }
                    }

                    if (!exclude) totalMods += mods.LinearModifiers[Type][mod];
                }

                foreach (string mod in mods.PostMultipliers[Type].Keys)
                {
                    bool exclude = false;
                    if (excludedMods != null)
                    {
                        foreach (string s in excludedMods)
                            if (s == mod)
                            {
                                exclude = true;
                                break;
                            }
                    }

                    if (!exclude) postMultiplier *= mods.PostMultipliers[Type][mod];
                }

                finalValue = (int)(Level * preMultiplier);
                finalValue += totalMods;
                finalValue = (int)(finalValue * postMultiplier);

                if (finalValue < 1) return 1;
                else if (finalValue > int.Parse(GameData.getData().globalVarsList["MAXATTRIBUTE"])) return int.Parse(GameData.getData().globalVarsList["MAXATTRIBUTE"]);
                else return finalValue;
            }
        }
    }
}
