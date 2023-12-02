using System;
using System.Collections.Generic;
using LCS.Engine.Components.Location;
using LCS.Engine.Components.World;
using LCS.Engine.Containers;
using LCS.Engine.Components.Item;
using LCS.Engine.Data;
using System.Xml;

namespace LCS.Engine.Components.Creature
{
    public class Body : Component
    {
        public List<BodyPart> BodyParts;
        private Dictionary<string, int> totalOrganCount;
        private List<Organ> eyes;
        private List<BodyPart> arms;
        private List<BodyPart> legs;
        private Entity lastAttacker;

        public bool ForceIncap;

        [SimpleSave]
        public string type;
        [SimpleSave]
        public int HospitalTime;
        [SimpleSave]
        public bool BadlyHurt;

        [SimpleSave]
        public int Blood;        
        [SimpleSave]
        public bool Alive;

        public DateTime deathDate;

        public int stunned;

        public Body() : this("")
        { }

        public Body(string type)
        {
            this.Blood = 100;
            this.BodyParts = new List<BodyPart>();
            this.Alive = true;
            this.BadlyHurt = false;
            this.type = type;
            stunned = 0;

            HospitalTime = 0;

            totalOrganCount = new Dictionary<string, int>();
            eyes = new List<Organ>();
            legs = new List<BodyPart>();
            arms = new List<BodyPart>();
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Body");
                entityNode.AppendChild(saveNode);
                XmlNode deathDateNode = saveNode.OwnerDocument.CreateElement("deathDate");
                saveNode.AppendChild(deathDateNode);
            }

            saveSimpleFields();
            
            saveNode.SelectSingleNode("deathDate").InnerText = deathDate.ToString("d");

            foreach(BodyPart part in BodyParts)
            {
                part.save(saveNode);
            }
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);
            deathDate = DateTime.Parse(componentData.SelectSingleNode("deathDate").InnerText);

            foreach(XmlNode partNode in componentData.SelectNodes("BodyPart"))
            {
                BodyPart p = new BodyPart("", "");
                p.load(partNode);
                BodyParts.Add(p);

                if ((GameData.getData().bodyPartList[p.Type].flags & BodyPartDef.PartFlags.WALK) != 0) legs.Add(p);
                if ((GameData.getData().bodyPartList[p.Type].flags & BodyPartDef.PartFlags.GRASP) != 0) arms.Add(p);
                foreach(Organ o in p.Organs)
                {
                    if (!totalOrganCount.ContainsKey(o.Type)) totalOrganCount[o.Type] = 1;
                    else totalOrganCount[o.Type]++;
                    if ((GameData.getData().organList[o.Type].flags & BodyPartDef.PartFlags.VISION) != 0) eyes.Add(o);
                }
            }
        }

        public override void subscribe()
        {
            base.subscribe();
            MasterController.GetMC().nextDay += doDailyHealing;
            MasterController.GetMC().nextDay += doRot;
            MasterController.GetMC().nextMonth += doHospitalTreatment;
        }

        public override void selfSubscribe()
        {
            base.selfSubscribe();
            getComponent<CreatureBase>().getAttributeModifiers += doGetAttributeModifiers;
            getComponent<CreatureBase>().die += doDie;
        }

        public override void unsubscribe()
        {
            base.unsubscribe();
            MasterController.GetMC().nextDay -= doDailyHealing;
            MasterController.GetMC().nextDay -= doRot;
            MasterController.GetMC().nextMonth -= doHospitalTreatment;
            getComponent<CreatureBase>().getAttributeModifiers -= doGetAttributeModifiers;
            getComponent<CreatureBase>().die -= doDie;
        }

        private void doGetAttributeModifiers(object sender, Events.GetAttributeModifiers args)
        {
            args.PostMultipliers["STRENGTH"]["blood"] = Blood / 100f;
            args.PostMultipliers["AGILITY"]["blood"] = Blood / 100f;
            args.PostMultipliers["CHARISMA"]["blood"] = Blood / 100f;
            args.PostMultipliers["INTELLIGENCE"]["blood"] = Blood / 100f;

            bool para_quad = false;
            bool para_para = false;

            foreach (Organ organ in getDamagedOrgans())
            {
                BodyPartDef.PartFlags flags = GameData.getData().organList[organ.Type].flags;
                List<OrganDef.OrganAttribute> attributes = GameData.getData().organList[organ.Type].attributes;

                if (organ.GetType() == typeof(SmallOrgan))
                {
                    SmallOrgan smallOrgan = (SmallOrgan)organ;

                    foreach (OrganDef.OrganAttribute att in attributes)
                    {
                        //Small organs have cumulative effects for losing some, half, or all of them.
                        int mod = 0;

                        if (smallOrgan.Count < smallOrgan.maxCount) mod += att.value;
                        if (smallOrgan.Count < smallOrgan.maxCount / 2) mod += att.value;
                        if (smallOrgan.Count == 0) mod += att.value;

                        args.LinearModifiers[att.attribute.type]["damaged_" + organ.Type] = mod;
                    }
                }
                else
                {
                    foreach (OrganDef.OrganAttribute att in attributes)
                    {
                        //Large organs cause stat loss for each one
                        args.LinearModifiers[att.attribute.type]["damaged" + organ.Name] = att.value;
                    }
                }

                if ((flags & BodyPartDef.PartFlags.PARALYZE_QUAD) != 0) para_quad = true;
                if ((flags & BodyPartDef.PartFlags.PARALYZE_PARA) != 0) para_para = true;
            }

            if (para_quad)
            {
                //Total paralysis reduces physical stats to minimum
                args.PostMultipliers["STRENGTH"]["para_quad"] = -1000;
                args.PostMultipliers["AGILITY"]["para_quad"] = -1000;
                args.PostMultipliers["HEALTH"]["para_quad"] = -1000;
            }
            else if (para_para)
            {
                //Partial paralysis halves physical stats.
                args.PostMultipliers["STRENGTH"]["para_para"] = 0.5f;
                args.PostMultipliers["AGILITY"]["para_para"] = 0.5f;
                args.PostMultipliers["HEALTH"]["para_para"] = 0.5f;
            }
        }

        private void doHospitalTreatment(object sender, EventArgs args)
        {
            if (hasComponent<Liberal>())
            {
                if (getComponent<Liberal>().status == Liberal.Status.HOSPITAL)
                {
                    HospitalTime--;
                    if (getComponent<CreatureBase>().BaseAttributes["HEALTH"].Level <= 0)
                    {
                        getComponent<CreatureBase>().doDie(new Events.Die("died on the operating table"));
                    }

                    if (HospitalTime == 0)
                    {
                        //If somehow they haven't recovered in all this time, just finish healing them automatically.
                        Blood = 100;
                        foreach (BodyPart part in BodyParts)
                        {
                            part.heal();
                            foreach (Organ o in part.Organs)
                            {
                                o.heal();
                                getComponent<Portrait>().forceRegen = true;
                            }
                        }

                        if (!getComponent<CriminalRecord>().hospitalArrest)
                        {
                            getComponent<Liberal>().status = Liberal.Status.ACTIVE;

                            MasterController.GetMC().addMessage(getComponent<CreatureInfo>().getName() + " has left " + getComponent<CreatureBase>().Location.getComponent<SiteBase>().getCurrentName() + ".", true);

                            BadlyHurt = false;
                            getComponent<Liberal>().goHome();
                        }
                        else
                        {
                            getComponent<CriminalRecord>().arrest();
                            MasterController.GetMC().addMessage(getComponent<CreatureInfo>().getName() + " has been returned to Police custody.", true);
                        }
                    }
                }
            }
        }

        private void doDailyHealing(object sender, EventArgs args)
        {
            if (!Alive) return;

            //Might as well expire stunning here
            stunned = 0;

            int healPower = 0;

            foreach (BodyPart part in BodyParts)
            {
                foreach (Organ o in part.Organs)
                {
                    if (o.Health == Organ.Damage.TREATED)
                    {
                        o.healDaysRemaining--;
                        if (o.healDaysRemaining <= 0) o.heal();
                        getComponent<Portrait>().forceRegen = true;
                    }
                }
            }

            if (hasComponent<Liberal>())
            {
                if (getComponent<Liberal>().status == Liberal.Status.HOSPITAL)
                {
                    if (GameData.getData().locationList[getComponent<CreatureBase>().Location.def].hospital < 12 && HospitalTime > 3)
                    {
                        getComponent<CreatureBase>().Location = getComponent<CreatureBase>().Location.getComponent<SiteBase>().city.getComponent<City>().getLocation("HOSPITAL_UNIVERSITY");

                        MasterController.GetMC().addMessage(getComponent<CreatureInfo>().getName() + " has been transferred to " + getComponent<CreatureBase>().Location.getComponent<SiteBase>().getCurrentName() + ".");
                    }

                    healPower = GameData.getData().locationList[getComponent<CreatureBase>().Location.def].hospital;

                    if (getComponent<Inventory>().getArmor().getComponent<Armor>().bloody && !isBleeding())
                    {
                        getComponent<Inventory>().destroyArmor();
                        getComponent<Inventory>().equipArmor(Factories.ItemFactory.create("ARMOR_HOSPITALGOWN"));
                        MasterController.GetMC().addMessage("The nurse at " + getComponent<CreatureBase>().Location.getComponent<SiteBase>().getCurrentName() + " removes " + getComponent<CreatureInfo>().getName() + "'s bloody clothing.");
                    }

                    if (getComponent<Inventory>().armor == null && getSpecies().type == "HUMAN")
                    {
                        Entity item = Factories.ItemFactory.create("ARMOR_HOSPITALGOWN");
                        getComponent<Inventory>().equipArmor(item);
                        MasterController.GetMC().addMessage("The nurse at " + getComponent<CreatureBase>().Location.getComponent<SiteBase>().getCurrentName() + " gives " + getComponent<CreatureInfo>().getName() + " a " + item.getComponent<ItemBase>().getName().ToLower() + " to wear.");
                    }
                }
                //Healing in prison depends on the prison law: more liberal = better treatment.
                else if (getComponent<Liberal>().status == Liberal.Status.JAIL_PRISON)
                {
                    healPower = 2 * ((int)MasterController.government.laws["PRISON"].alignment + 2);
                }
                else if (getComponent<Liberal>().status == Liberal.Status.ACTIVE)
                {
                    if (getComponent<Liberal>().homeBase.getComponent<SafeHouse>().getBestHealer() != null)
                        healPower = getComponent<Liberal>().homeBase.getComponent<SafeHouse>().getBestHealer().getComponent<CreatureBase>().Skills["FIRST_AID"].level;
                }
                else if(getComponent<Liberal>().status == Liberal.Status.SLEEPER)
                {
                    //Sleepers will heal up quickly after being released from captivity, just because there's no other way to treat them
                    healPower = 12;
                }
            }
            else
            {
                //Non-Liberals are free to seek hospital treatment whenever they want
                healPower = 12;
            }

            if (hasComponent<Liberal>() &&
                getComponent<Liberal>().status == Liberal.Status.ACTIVE &&
                getComponent<Liberal>().homeBase.getComponent<SafeHouse>().underSiege && 
                getComponent<Liberal>().homeBase.getComponent<SafeHouse>().food == 0)
            {
                return;
            }

            if (getClinicTime() > 0)
            {
                int healerExp = 100 - Blood;
                int healerExpLimit = 5 + healerExp / 10 + getClinicTime();

                //Treat damaged body parts
                stabilize(healPower);

                //Treat major injuries, one per day (if multiple internal organs are damaged they may need blood transfusions just to stay alive!)
                treatOrgan(healPower);
                if (getComponent<CreatureBase>().BaseAttributes["HEALTH"].Level <= 0)
                {
                    string causeOfDeath = "died for no reason";

                    if (hasComponent<Liberal>())
                    {
                        if (getComponent<Liberal>().status == Liberal.Status.HOSPITAL)
                        {
                            causeOfDeath = "died on the operating table";
                        }
                        else if (getComponent<Liberal>().status == Liberal.Status.ACTIVE)
                        {
                            if (getComponent<Liberal>().homeBase.getComponent<SafeHouse>().getBestHealer() == null)
                                causeOfDeath = "died from lack of treatment";
                            else if (getComponent<Liberal>().homeBase.getComponent<SafeHouse>().getBestHealer() == owner)
                                causeOfDeath = "died while performing surgery on " + getComponent<CreatureInfo>().himHer().ToLower() + "self";
                            else
                                causeOfDeath = "died under Liberal care";
                        }
                    }

                    getComponent<CreatureBase>().doDie(new Events.Die(causeOfDeath));
                }
                else
                {
                    if (Blood <= 0)
                    {
                        if (hasComponent<Liberal>() && getComponent<Liberal>().status == Liberal.Status.HOSPITAL && MasterController.GetMC().LCSRandom(2) == 0)
                        {
                            MasterController.GetMC().addMessage(getComponent<CreatureInfo>().getName() + " recieved a life-saving blood transfusion!");
                            Blood = 10;
                        }
                        else
                        {
                            getComponent<CreatureBase>().doDie(new Events.Die("died of " + getComponent<CreatureInfo>().hisHer().ToLower() + " injuries"));
                        }
                    }
                }

                //Restore health if not still bleeding
                if (Blood < 100 - (getClinicTime() - 1) * 20 && !isBleeding() && !isInternalBleeding())
                {
                    Blood += 1 + MasterController.GetMC().LCSRandom(healPower / 3);
                    if (Blood > 100 - (getClinicTime() - 1) * 20) Blood = 100 - (getClinicTime() - 1) * 20;
                    if (Blood > 100) Blood = 100;
                }

                if (getClinicTime() - 1 > healPower && hasComponent<Liberal>() && getComponent<Liberal>().status == Liberal.Status.ACTIVE && Alive)
                {
                    if (!getComponent<Liberal>().homeBase.getComponent<SafeHouse>().underSiege)
                    {
                        if (getComponent<Liberal>().dailyActivity.type != "MOVE_CLINIC")
                        {
                            getComponent<Liberal>().setActivity("MOVE_CLINIC");
                            MasterController.GetMC().addMessage(getComponent<CreatureInfo>().getName() + "'s injuries require professional treatment.", true);
                            BadlyHurt = true;
                        }
                        else
                        {
                            MasterController.GetMC().addMessage("<color=red>" + getComponent<CreatureInfo>().getName() + "'s injuries require professional treatment, but the siege prevents " + getComponent<CreatureInfo>().himHer().ToLower() + " from getting it.</color>");
                        }
                    }
                }

                //Give experience to active medics
                if (hasComponent<Liberal>())
                {
                    if (getComponent<Liberal>().status == Liberal.Status.ACTIVE)
                    {
                        foreach (Entity e in getComponent<Liberal>().homeBase.getComponent<SafeHouse>().getAllHealers())
                        {
                            e.getComponent<CreatureBase>().Skills["FIRST_AID"].addExperience(healerExp / 5, healerExpLimit);
                        }
                    }
                }
            }
        }

        private void doRot(object sender, EventArgs args)
        {
            if (Alive) return;

            int corpseAge = MasterController.GetMC().currentDate.Subtract(deathDate).Days;

            if (corpseAge > 1)
            {
                foreach (BodyPart part in BodyParts)
                {
                    if (part.Health == BodyPart.Damage.NASTYOFF)
                        part.Health = BodyPart.Damage.CLEANOFF;
                    if ((part.Health & BodyPart.Damage.BLEEDING) != 0)
                        part.Health &= ~BodyPart.Damage.BLEEDING;
                }
            }

            if (15 + MasterController.GetMC().LCSRandom(20) > corpseAge) return;

            List<BodyPart> unrottedParts = new List<BodyPart>();
            List<BodyPart> core = new List<BodyPart>();

            foreach (BodyPart part in BodyParts)
            {
                if (!part.isSevered())
                {
                    if (!part.isCore())
                        unrottedParts.Add(part);
                    else
                        core.Add(part);
                }
            }

            if (unrottedParts.Count > 0)
            {
                unrottedParts[MasterController.GetMC().LCSRandom(unrottedParts.Count)].Health = BodyPart.Damage.CLEANOFF;
            }
            else if (core.Count > 0)
            {
                core[MasterController.GetMC().LCSRandom(core.Count)].Health = BodyPart.Damage.CLEANOFF;
            }
        }

        private void doDie(object sender, Events.Die args)
        {
            if (Alive)
            {
                Alive = false;
                deathDate = MasterController.GetMC().currentDate;
                if (getComponent<CreatureBase>().Location != null && 
                    (getComponent<CreatureBase>().Location.hasComponent<SafeHouse>() &&
                    getComponent<CreatureBase>().Location.getComponent<SafeHouse>().owned) && 
                    MasterController.GetMC().currentChaseScene == null &&
                    MasterController.GetMC().canSeeThings)
                {
                    owner.persist();
                }
                else
                {
                    owner.depersist();
                }

                if (hasComponent<Liberal>())
                    MasterController.highscore.martyrs++;
                else
                    MasterController.highscore.kills++;

                Blood = 0;
            }
        }

        public void severLimb(string partName, string type)
        {
            foreach (BodyPart part in BodyParts)
            {
                if (part.Name == partName)
                {
                    if (type == "NASTY")
                    {
                        part.Health = BodyPart.Damage.NASTYOFF;
                        part.staunched = false;
                    }
                    else
                    {
                        part.Health = BodyPart.Damage.CLEANOFF;
                    }
                }
            }
        }

        public KeyValuePair<string, string> getBodyPartStatus(string partName)
        {
            string healthString = "";
            string status = "FINE";

            foreach (BodyPart part in BodyParts)
            {
                if (part.Name == partName)
                {
                    string criticalDamageText = getCriticalDamageText(part);

                    //Order here is priority based - lower on the list = more serious (so statuses should override earlier ones)
                    if (criticalDamageText != "" && !isVitalDamageFresh(part))
                    {
                        healthString += criticalDamageText;
                        status = "SCARRED";
                    }
                    if ((part.Health & BodyPart.Damage.BLEEDING) != 0)
                    {
                        if (!part.staunched) healthString += "<color=red>";
                        healthString += "Bleeding";
                        if (!part.staunched) healthString += "</color>";
                        else healthString += " (bandaged)";
                        healthString += "\n";
                        status = "INJURED";
                    }
                    if ((part.Health & BodyPart.Damage.BRUISE) != 0)
                    {
                        healthString += "Bruised\n";
                        status = "INJURED";
                    }
                    if ((part.Health & BodyPart.Damage.BURN) != 0)
                    {
                        healthString += "Burned\n";
                        status = "INJURED";
                    }
                    if ((part.Health & BodyPart.Damage.CUT) != 0)
                    {
                        healthString += "Cut\n";
                        status = "INJURED";
                    }
                    if ((part.Health & BodyPart.Damage.SHOOT) != 0)
                    {
                        healthString += "Shot\n";
                        status = "INJURED";
                    }
                    if ((part.Health & BodyPart.Damage.TEAR) != 0)
                    {
                        healthString += "Torn\n";
                        status = "INJURED";
                    }
                    if (criticalDamageText != "" && isVitalDamageFresh(part))
                    {
                        healthString += criticalDamageText;
                        status = "INJURED_VITAL";
                    }
                    if ((part.Health & BodyPart.Damage.CLEANOFF) != 0)
                    {
                        healthString += "Severed\n";
                        status = "SEVERED";
                    }
                    if ((part.Health & BodyPart.Damage.NASTYOFF) != 0)
                    {
                        if (!part.staunched) healthString += "<color=red>";
                        healthString += "Severed";
                        if (!part.staunched) healthString += "</color> (messy)";
                        else healthString += " (tourniquet)";
                        healthString += "\n";
                        status = "SEVERED_NASTY";
                    }
                    break;
                }
            }

            healthString = healthString.TrimEnd('\n');

            return new KeyValuePair<string, string>(healthString, status);
        }

        public bool incapacitated()
        {
            if (Blood <= 20 || (Blood <= 50 && (MasterController.GetMC().LCSRandom(2) == 0 || ForceIncap)))
            {
                ForceIncap = false;
                return true;
            }
            else
            {
                return false;
            }
        }

        public int healthModRoll()
        {
            List<Organ> damagedOrgans = new List<Organ>();

            foreach (BodyPart part in BodyParts)
            {
                foreach (Organ o in part.Organs)
                {
                    if (o.Health != Organ.Damage.FINE)
                    {
                        damagedOrgans.Add(o);
                    }
                    else if (o.GetType() == typeof(SmallOrgan))
                    {
                        if (((SmallOrgan)o).Count < totalOrganCount[o.Type]) damagedOrgans.Add(o);
                    }
                }
            }

            int mod = 0;

            foreach (Organ o in damagedOrgans)
            {
                if (o.GetType() == typeof(SmallOrgan))
                {
                    if (((SmallOrgan)o).Count < totalOrganCount[o.Type]) mod += GameData.getData().organList[o.Type].fightPenalty;
                    if (((SmallOrgan)o).Count < totalOrganCount[o.Type] / 2) mod += GameData.getData().organList[o.Type].fightPenalty;
                    if (((SmallOrgan)o).Count == 0) mod += GameData.getData().organList[o.Type].fightPenalty;
                }
                else
                {
                    mod += MasterController.GetMC().LCSRandom(GameData.getData().organList[o.Type].fightPenalty);
                }
            }

            if (!canSee()) mod += MasterController.GetMC().LCSRandom(20);

            return mod;
        }

        public string takeHit(int accuracy, int numHits, AttackDef attack, Entity attacker, bool sneakAttack)
        {
            string name;
            lastAttacker = attacker;
            bool deathMessage = false;

            if (hasComponent<Liberal>()) name = getComponent<CreatureInfo>().getName();
            else name = getComponent<CreatureInfo>().encounterName;

            Dictionary<BodyPart, int> body = new Dictionary<BodyPart, int>();

            //Special case for skilled martial artists - they are much better at breaking limbs
            bool kungFuMaster = attack.type == "UNARMED" && attacker.getComponent<CreatureBase>().Skills[Constants.SKILL_MARTIAL_ARTS].check(Difficulty.HARD);

            foreach (BodyPart part in BodyParts)
            {
                if (!part.isSevered())
                {
                    int size = GameData.getData().bodyPartList[part.Type].size;

                    //No accuracy bonus for martial arts masters, because they actually WANT to hit limbs, unless the target is practically dead in which case just finish them off.
                    //Sneak attacks will ignore martial arts mastery since they want a killing strike, although at the moment nothing can actually use both martial arts and a sneak attack
                    if (!(kungFuMaster && Blood >= 30) || sneakAttack)
                    {
                        //Medium accuracy - more likely to hit core/head.
                        if (accuracy >= 6 && part.isLimb()) size /= 2;
                        //High accuracy - no limb hits at all. Also applies to sneak attacks
                        if ((accuracy >= 11 || sneakAttack) && part.isLimb()) size = 0;
                        //Perfect accuracy - guaranteed headshot
                        if (accuracy >= 16 && part.isCore()) size = 0;
                    }

                    if (size > 0) body.Add(part, size);
                }
            }

            //Apparently you have no body left?!?
            if (body.Count == 0)
            {
                getComponent<CreatureBase>().doDie(new Events.Die("vanished in a puff of logic"));
                return "";
            }

            BodyPart target = MasterController.GetMC().WeightedRandom(body);
            string logText = "";
            if (!sneakAttack)
                logText += "'s";
            else
                logText += " and " + attack.sneak_attack_description + " " + getComponent<CreatureInfo>().himHer().ToLower() + " in the";
            //Sneak attack flavour for torso hits
            if (sneakAttack) logText += " " + GameData.getData().bodyPartList[target.Type].sneakname.ToLower();
            else logText += " " + target.Name.ToLower();

            if (numHits > 1 && attack.type != "UNARMED")
            {
                logText += ", " + attack.hit_description + " " + (numHits == 2 ? "twice" : MasterController.NumberToWords(numHits).ToLower() + " times");
            }
            else if((attack.flags & AttackDef.AttackFlags.ALWAYS_DESCRIBE_HIT) != 0)
            {
                logText += ", " + attack.hit_description;
            }

            int damageAmount = 0;
            int armorPiercing = 0;

            if ((attack.flags & AttackDef.AttackFlags.SKILL_DAMAGE) != 0)
            {
                while (numHits > 0)
                {
                    damageAmount += MasterController.GetMC().LCSRandom(5 + attacker.getComponent<CreatureBase>().Skills[attack.skill.type].level);
                    damageAmount += 1 + attacker.getComponent<CreatureBase>().Skills[attack.skill.type].level;
                    numHits--;
                }
            }
            else
            {
                bool critical = false;

                if (numHits >= attack.criticalHitsRequired && MasterController.GetMC().LCSRandom(100) < attack.criticalChance) critical = true;

                while (numHits > 0)
                {
                    damageAmount += attack.fixed_damage + (critical ? attack.criticalFixedDamage : 0);
                    if (sneakAttack) damageAmount += 100;
                    damageAmount += MasterController.GetMC().LCSRandom(attack.random_damage + (critical ? attack.criticalRandomDamage : 0));
                    numHits--;
                }
            }

            armorPiercing = attack.armorpiercing;

            //Plot armor for founder
            if (hasComponent<Liberal>() && getComponent<Liberal>().leader == null) damageAmount /= 2;

            int damageMod = 0;

            //Strength only applies to melee attacks
            if ((attack.flags & AttackDef.AttackFlags.RANGED) == 0)
            {
                if (attack.strength_max > attack.strength_min)
                {
                    int strength = attacker.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_STRENGTH].roll();
                    if (strength > attack.strength_max) strength = (attack.strength_max + strength) / 2;
                    damageMod += strength - attack.strength_min;
                    armorPiercing += (strength - attack.strength_min) / 4;
                }
            }

            damageMod += accuracy;
            damageMod -= getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].roll();

            if (damageMod < 0) damageMod = 0;

            int armor = getComponent<Inventory>().getArmorValue(target);
            if ((getComponent<Inventory>().getArmor().getComponent<Armor>().getFlags() & ItemDef.ArmorFlags.FIRE_VULN) != 0 &&
                attack.damage_type == AttackDef.DamageType.BURN)
                armor = (armor / 3) * 2;

            SpeciesDef.SpeciesBodyPartLocation hitLocation = SpeciesDef.SpeciesBodyPartLocation.MID;

            if ((MasterController.GetMC().combatModifiers & MasterController.CombatModifiers.CHASE_CAR) != 0)
            {
                if (target.location == SpeciesDef.SpeciesBodyPartLocation.HIGH)
                {
                    armor += MasterController.GetMC().LCSRandom(getComponent<Inventory>().tempVehicle.getComponent<Vehicle>().getVehicleData().armorHigh);
                    hitLocation = SpeciesDef.SpeciesBodyPartLocation.HIGH;
                }
                else if (target.location == SpeciesDef.SpeciesBodyPartLocation.LOW)
                {
                    armor += MasterController.GetMC().LCSRandom(getComponent<Inventory>().tempVehicle.getComponent<Vehicle>().getVehicleData().armorLow);
                    hitLocation = SpeciesDef.SpeciesBodyPartLocation.LOW;
                }
                else if (MasterController.GetMC().LCSRandom(100) < getComponent<Inventory>().tempVehicle.getComponent<Vehicle>().getVehicleData().armorMidpoint)
                {
                    armor += MasterController.GetMC().LCSRandom(getComponent<Inventory>().tempVehicle.getComponent<Vehicle>().getVehicleData().armorLow);
                    hitLocation = SpeciesDef.SpeciesBodyPartLocation.LOW;
                }
                else
                {
                    armor += MasterController.GetMC().LCSRandom(getComponent<Inventory>().tempVehicle.getComponent<Vehicle>().getVehicleData().armorHigh);
                    hitLocation = SpeciesDef.SpeciesBodyPartLocation.HIGH;
                }
            }

            int armorMod = armor - attack.armorpiercing + MasterController.GetMC().LCSRandom(armor + 1);
            if (armorMod > 0) damageMod -= armorMod * 2;

            if (damageMod > 10) damageMod = 10;

            if (damageMod <= -20) damageAmount /= 256;
            else if (damageMod <= -14) damageAmount /= 128;
            else if (damageMod <= -8) damageAmount /= 64;
            else if (damageMod <= -6) damageAmount /= 32;
            else if (damageMod <= -4) damageAmount /= 16;
            else if (damageMod <= -3) damageAmount /= 8;
            else if (damageMod <= -2) damageAmount /= 4;
            else if (damageMod <= -1) damageAmount /= 2;
            else if (damageMod >= 0) damageAmount += (int)((0.2 * damageMod) * damageAmount);

            if (attack.damage_type == AttackDef.DamageType.BURN && getComponent<Inventory>().getFireProtection() > 0)
            {
                damageAmount /= getComponent<Inventory>().getFireProtection();
            }

            if (damageAmount < 0) damageAmount = 0;

            if (damageAmount > 0)
            {
                //Bullets only cause bruising if the armor managed to block them
                if (damageAmount < 4 && attack.damage_type == AttackDef.DamageType.SHOOT)
                    target.Health |= BodyPart.Damage.BRUISE;
                else
                {
                    target.Health |= (BodyPart.Damage)Enum.Parse(typeof(BodyPart.Damage), attack.damage_type.ToString());
                    if ((attack.flags & AttackDef.AttackFlags.CAUSE_BLEED) != 0)
                    {
                        target.Health |= BodyPart.Damage.BLEEDING;
                        target.staunched = false;
                    }
                }

                if (attack.severtype != "NONE" && damageAmount > GameData.getData().bodyPartList[target.Type].severAmount)
                {
                    target.Health = (BodyPart.Damage)Enum.Parse(typeof(BodyPart.Damage), attack.severtype + "OFF");
                    if (attack.severtype == "NASTY") target.staunched = false;
                }

                if (target.isLimb() && Blood - damageAmount <= 0 && Blood > 0)
                {
                    while (Blood - damageAmount <= 0)
                    {
                        if (MasterController.GetMC().LCSRandom(100) < attack.no_damage_reduction_for_limbs_chance)
                            break;
                        else
                            damageAmount /= 2;
                    }
                }

                if ((attack.flags & AttackDef.AttackFlags.DAMAGE_ARMOR) != 0)
                    getComponent<Inventory>().damageArmor(target.Type, damageAmount);

                if(Alive)
                    Blood -= damageAmount;

                bool vitalSevered = false;
                BodyPart severedPart = null;

                foreach (BodyPart part in BodyParts)
                {
                    if (part.isSevered() && (part.isCore() || part.isHead()))
                    {
                        vitalSevered = true;
                        severedPart = part;
                        break;
                    }
                }

                string color = "<color=white>";

                if (hasComponent<Liberal>()) color = "<color=red>";
                else color = "<color=lime>";                

                if (vitalSevered || Blood <= 0)
                {
                    bool massiveDamage = false;

                    if (Alive)
                    {
                        string killVerb = "";
                        //MASSIVE DAMAGE
                        if (damageAmount > 1000)
                        {
                            killVerb = "was annihilated";
                            massiveDamage = true;
                        }
                        else if (sneakAttack)
                        {
                            killVerb = "was ambushed";
                        }
                        else if (vitalSevered)
                        {
                            if (severedPart.isHead())
                                killVerb = "was decapitated";
                            else if (severedPart.isCore())
                                killVerb = "was bisected";
                        }
                        else
                        {
                            switch (attack.damage_type)
                            {
                                case AttackDef.DamageType.BRUISE:
                                    killVerb = "was beaten to death";
                                    break;
                                case AttackDef.DamageType.BURN:
                                    killVerb = "was immolated";
                                    break;
                                case AttackDef.DamageType.CUT:
                                    killVerb = "was stabbed";
                                    break;
                                case AttackDef.DamageType.SHOOT:
                                    killVerb = "was shot";
                                    break;
                                case AttackDef.DamageType.TEAR:
                                    killVerb = "was gored";
                                    break;
                            }
                        }

                        getComponent<CreatureBase>().doDie(new Events.Die(killVerb + " by " + attacker.getComponent<CreatureInfo>().encounterName));
                        if (MasterController.news.currentStory != null && attacker.hasComponent<Liberal>())
                        {
                            MasterController.news.currentStory.addCrime("KILLEDSOMEBODY");
                        }

                        if ((int)getComponent<CreatureInfo>().alignment == -(int)attacker.getComponent<CreatureInfo>().alignment)
                            attacker.getComponent<CreatureBase>().juiceMe(5 + getComponent<CreatureBase>().Juice / 20);
                        else
                            attacker.getComponent<CreatureBase>().juiceMe(-(5 + getComponent<CreatureBase>().Juice / 20));

                        if (attacker.hasComponent<Liberal>() && 
                            (getSpecies().type == "HUMAN" || 
                                (getSpecies().type == "DOG" && 
                                MasterController.government.laws[Constants.LAW_ANIMAL_RESEARCH].alignment == Alignment.ELITE_LIBERAL)))
                        {
                            if(attacker.getComponent<Liberal>().squad != null)
                            {
                                foreach(Entity e in attacker.getComponent<Liberal>().squad)
                                {
                                    e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_MURDER);
                                }
                            }
                            else
                            {
                                attacker.getComponent<CriminalRecord>().addCrime(Constants.CRIME_MURDER);
                            }
                        }

                        if (!massiveDamage)
                        {
                            deathMessage = true;                            
                        }
                    }

                    if (massiveDamage)
                    {
                        logText += "\n" + color + name;

                        switch (MasterController.GetMC().LCSRandom(3))
                        {
                            case 0:
                                logText += " is OBLITERATED!</color>";
                                break;
                            case 1:
                                logText += " simply stops existing.</color>";
                                break;
                            case 2:
                                logText += " is reduced to a fine pink mist!</color>";
                                break;
                        }
                    }
                    else if ((GameData.getData().bodyPartList[target.Type].flags & BodyPartDef.PartFlags.HEAD) != 0)
                    {
                        if ((target.Health & BodyPart.Damage.CLEANOFF) != 0)
                            logText += ", " + color + "CUTTNG IT OFF!</color>";
                        if ((target.Health & BodyPart.Damage.NASTYOFF) != 0)
                            logText += ", " + color + "BLOWING IT APART!</color>";
                        else if (sneakAttack)
                            logText += "!";
                        else
                            logText += attack.hit_punctuation;

                        if ((target.Health & (BodyPart.Damage.CLEANOFF | BodyPart.Damage.NASTYOFF)) != 0)
                        {
                            if (MasterController.GetMC().currentSiteModeScene != null)
                            {
                                MasterController.GetMC().currentSiteModeScene.bloodblast();
                            }
                        }
                    }
                    else if ((GameData.getData().bodyPartList[target.Type].flags & BodyPartDef.PartFlags.CORE) != 0)
                    {
                        if ((target.Health & BodyPart.Damage.CLEANOFF) != 0)
                            logText += ", " + color + "CUTTNG IT IN HALF!</color>";
                        if ((target.Health & BodyPart.Damage.NASTYOFF) != 0)
                            logText += ", " + color + "BLOWING IT IN HALF!</color>";
                        else if (sneakAttack)
                            logText += "!";
                        else
                            logText += attack.hit_punctuation;

                        if ((target.Health & (BodyPart.Damage.CLEANOFF | BodyPart.Damage.NASTYOFF)) != 0)
                        {
                            if (MasterController.GetMC().currentSiteModeScene != null)
                            {
                                MasterController.GetMC().currentSiteModeScene.bloodblast();
                            }
                        }
                    }
                    else
                    {
                        if ((target.Health & BodyPart.Damage.CLEANOFF) != 0)
                            logText += ", " + color + "CUTTING IT OFF!</color>";
                        else if ((target.Health & BodyPart.Damage.NASTYOFF) != 0)
                            logText += ", " + color + "BLOWING IT OFF!</color>";
                        else if (sneakAttack)
                            logText += "!";
                        else
                            logText += attack.hit_punctuation;

                        if ((target.Health & (BodyPart.Damage.CLEANOFF | BodyPart.Damage.NASTYOFF)) != 0)
                        {
                            if (MasterController.GetMC().currentSiteModeScene != null)
                            {
                                MasterController.GetMC().currentSiteModeScene.bloodblast();
                            }
                        }
                    }
                }
                else
                {
                    if ((target.Health & BodyPart.Damage.CLEANOFF) != 0)
                        logText += ", " + color + "CUTTING IT OFF!</color>";
                    else if ((target.Health & BodyPart.Damage.NASTYOFF) != 0)
                        logText += ", " + color + "BLOWING IT OFF!</color>";
                    else
                        logText += attack.hit_punctuation;

                    if ((target.Health & (BodyPart.Damage.CLEANOFF | BodyPart.Damage.NASTYOFF)) != 0)
                    {
                        if (MasterController.GetMC().currentSiteModeScene != null)
                        {
                            MasterController.GetMC().currentSiteModeScene.bloodblast();
                        }
                    }

                    if (MasterController.GetMC().LCSRandom(200) < damageAmount)
                        getComponent<Portrait>().scarMe();

                    //Special wounds
                    if (kungFuMaster && damageAmount >= 20 && target.isLimb() && !target.isSevered())
                        logText += "\n" + color + "SNAPPING IT LIKE A TWIG!</color>";
                    else if (!target.isSevered() && target.Organs.Count > 0)
                    {
                        bool heavyDam = false;
                        bool pokeDam = false;
                        bool breakDam = false;

                        if (damageAmount >= 12 &&
                            (attack.damage_type == AttackDef.DamageType.SHOOT ||
                            attack.damage_type == AttackDef.DamageType.BURN ||
                            attack.damage_type == AttackDef.DamageType.TEAR ||
                            attack.damage_type == AttackDef.DamageType.CUT))
                            heavyDam = true;

                        if (damageAmount >= 10 &&
                            (attack.damage_type == AttackDef.DamageType.SHOOT ||
                            attack.damage_type == AttackDef.DamageType.TEAR ||
                            attack.damage_type == AttackDef.DamageType.CUT))
                            pokeDam = true;

                        if (damageAmount >= 50 &&
                            (attack.damage_type == AttackDef.DamageType.SHOOT ||
                            attack.damage_type == AttackDef.DamageType.BRUISE ||
                            attack.damage_type == AttackDef.DamageType.TEAR ||
                            attack.damage_type == AttackDef.DamageType.CUT))
                            breakDam = true;

                        Organ targetOrgan = target.Organs[MasterController.GetMC().LCSRandom(target.Organs.Count)];

                        if (kungFuMaster && damageAmount >= 20 && target.isLimb())
                        {
                            foreach (Organ o in target.Organs)
                            {
                                o.damage();
                            }
                        }
                        else if (targetOrgan.Health == Organ.Damage.FINE)
                        {
                            bool damageDone = false;
                            int startingCount = 0;

                            if (targetOrgan.GetType() == typeof(SmallOrgan)) startingCount = ((SmallOrgan)targetOrgan).Count;

                            switch (GameData.getData().organList[targetOrgan.Type].damageRequired)
                            {
                                case OrganDef.OrganDamageType.BREAK:
                                    if (breakDam)
                                    {
                                        damageOrgan(targetOrgan);
                                        damageDone = true;
                                    }
                                    break;
                                case OrganDef.OrganDamageType.HEAVY:
                                    if (heavyDam)
                                    {
                                        damageOrgan(targetOrgan);
                                        damageDone = true;
                                    }
                                    break;
                                case OrganDef.OrganDamageType.POKE:
                                    if (pokeDam)
                                    {
                                        damageOrgan(targetOrgan);
                                        damageDone = true;
                                    }
                                    break;
                                case OrganDef.OrganDamageType.NONE:
                                    damageOrgan(targetOrgan);
                                    damageDone = true;
                                    break;
                            }

                            if (damageDone)
                            {
                                getComponent<Portrait>().forceRegen = true;
                                logText += "\n" + color;

                                if (startingCount > 0)
                                {
                                    SmallOrgan smallOrgan = (SmallOrgan)targetOrgan;
                                    int organMinus = startingCount - smallOrgan.Count;

                                    if (organMinus > 1)
                                    {
                                        if (organMinus == startingCount)
                                            logText += "All " + MasterController.NumberToWords(organMinus).ToLower();
                                        else
                                            logText += MasterController.NumberToWords(organMinus);
                                        logText += " of " + name + "'s " + targetOrgan.Name.ToLower() + " are ";
                                    }
                                    else if (startingCount > 1)
                                    {
                                        logText += "One of " + name + "'s " + targetOrgan.Name.ToLower() + " is ";
                                    }
                                    else
                                    {
                                        logText += name + "'s last " + GameData.getData().organList[targetOrgan.Type].name + " is ";
                                    }
                                }
                                else
                                {
                                    logText += name + "'s " + targetOrgan.Name.ToLower() + " is ";
                                }

                                if (GameData.getData().organList[targetOrgan.Type].damageVerbs.ContainsKey(attack.damage_type.ToString()))
                                    logText += GameData.getData().organList[targetOrgan.Type].damageVerbs[attack.damage_type.ToString()];
                                else
                                    logText += GameData.getData().organList[targetOrgan.Type].damageVerbs["DEFAULT"];

                                logText += "!</color>";
                            }
                        }
                    }
                }
            }
            else
            {
                if((MasterController.GetMC().combatModifiers & MasterController.CombatModifiers.CHASE_CAR) != 0)
                {
                    logText = ", but the attack bounces off the " + getComponent<Inventory>().tempVehicle.getComponent<ItemBase>().getName() + "'s ";
                    if (hitLocation == SpeciesDef.SpeciesBodyPartLocation.HIGH)
                        logText += "window";
                    else
                        logText += "body";
                }
                logText += " to no effect.";
            }

            //If after everything they can no longer carry their weapon, drop it.
            if (getComponent<Inventory>().weapon != null && !canGrasp())
            {
                logText += "\n<color=yellow>" + name + " can no longer carry their " + getComponent<Inventory>().weapon.getComponent<ItemBase>().getName() + ".</color>";

                getComponent<Inventory>().dropWeapon();
                getComponent<Inventory>().dropAllClips();
            }

            MasterController.GetMC().addCombatMessage("##DEBUG## damage=" + damageAmount + " damageMod=" + damageMod + " AP=" + armorPiercing + "armor=" + armor + " armorMod=" + armorMod);

            if(deathMessage)
                logText += "\n" + getDeathMessage();

            return logText;
        }

        public void addPart(SpeciesDef.SpeciesBodyPart bodyPartTag)
        {
            int partCount = MasterController.GetMC().LCSRandom(bodyPartTag.count);

            for (int i = 0; i < partCount; i++)
            {
                BodyPart part;
                List<string> partNameList = null;

                string partName = bodyPartTag.bodyPart.name;
                if (bodyPartTag.names != "")
                {
                    partNameList = new List<string>(bodyPartTag.names.Split(','));
                    partName = partNameList[i % partNameList.Count];
                }

                part = new BodyPart(bodyPartTag.bodyPart.type, nameGenerator(partName, partNameList == null ? partCount : partCount / partNameList.Count, bodyPartTag.prefix, partNameList == null ? i : i / partNameList.Count));
                part.location = bodyPartTag.location;

                foreach (BodyPartDef.BodyPartOrgan organTag in bodyPartTag.bodyPart.organs.Values)
                {
                    if ((organTag.organ.flags & BodyPartDef.PartFlags.SMALL) != 0)
                    {
                        SmallOrgan organ = new SmallOrgan(organTag.organ.type, organTag.count > 1 ? organTag.organ.pluralName : organTag.organ.name, organTag.count);
                        part.Organs.Add(organ);

                        totalOrganCount[organTag.organ.type] = organTag.count;

                        if ((organTag.organ.flags & BodyPartDef.PartFlags.VISION) != 0) eyes.Add(organ);
                    }
                    else {
                        for (int j = 0; j < organTag.count; j++)
                        {
                            List<string> organNameList = null;

                            string organName = organTag.organ.name;
                            if (organTag.names != "")
                            {
                                organNameList = new List<string>(organTag.names.Split(','));
                                organName = organNameList[j % organNameList.Count];
                            }

                            Organ organ = new Organ(organTag.organ.type, nameGenerator(organName, organNameList == null ? organTag.count : organTag.count / organNameList.Count, organTag.prefix, organNameList == null ? j : j / organNameList.Count));
                            part.Organs.Add(organ);

                            if (!totalOrganCount.ContainsKey(organTag.organ.type)) totalOrganCount[organTag.organ.type] = 1;
                            else totalOrganCount[organTag.organ.type]++;

                            if ((organTag.organ.flags & BodyPartDef.PartFlags.VISION) != 0) eyes.Add(organ);
                        }
                    }
                }

                BodyParts.Add(part);

                if ((bodyPartTag.bodyPart.flags & BodyPartDef.PartFlags.WALK) != 0) legs.Add(part);
                if ((bodyPartTag.bodyPart.flags & BodyPartDef.PartFlags.GRASP) != 0) arms.Add(part);
            }
        }

        public string getHealthStatusText(bool shortText = false)
        {
            string healthText = "";

            if (!Alive)
            {
                int corpseAge = MasterController.GetMC().currentDate.Subtract(deathDate).Days;

                if (corpseAge <= 5)
                    return "<color=grey>Deceased</color>";
                else if (corpseAge <= 10)
                    return "<color=yellow>Starting to Smell</color>";
                else if (corpseAge <= 15)
                    return "<color=yellow>REALLY Startng to Smell</color>";
                else if (corpseAge <= 30)
                    return "<color=yellow>The Smell Will Probably\nNever Come Out Now</color>";
                else
                    return "<color=grey>Skeletal</color>";
            }

            if (Blood <= 20)
            {
                if (shortText) healthText = "NearDETH";
                else healthText = "Near Death";
            }
            else if (Blood <= 50)
            {
                if (shortText) healthText = "BadWound";
                else healthText = "Badly Wounded";
            }
            else if (Blood <= 75)
            {
                healthText = "Wounded";
            }
            else if (Blood < 100)
            {
                if (shortText) healthText = "LtWound";
                else healthText = "Lightly Wounded";
            }
            else
            {
                if (getComponent<CreatureInfo>().alignment == Alignment.LIBERAL)
                    healthText = "<color=lime>Liberal</color>";
                else if (getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE)
                    healthText = "<color=red>Conservative</color>";
                else
                    healthText = "<color=yellow>Moderate</color>";
            }

            if (MasterController.GetMC().DebugMode) healthText += "(" + Blood + ")";

            return healthText;
        }

        public bool canSee()
        {
            bool vision = false;

            foreach (Organ o in eyes)
            {
                vision |= o.Health == Organ.Damage.FINE;
            }

            return vision;
        }

        public bool missingEyes()
        {
            int eyeCount = 0;

            foreach (Organ o in eyes)
            {
                if (o.Health != Organ.Damage.FINE) eyeCount++;
            }

            return eyeCount > 0;
        }

        //People with no legs need a wheelchair.
        public bool canWalk()
        {
            //Are they paralyzed?
            foreach (BodyPart part in BodyParts)
            {
                foreach (Organ o in part.Organs)
                {
                    if ((GameData.getData().organList[o.Type].flags & (BodyPartDef.PartFlags.PARALYZE_QUAD | BodyPartDef.PartFlags.PARALYZE_PARA)) != 0 &&
                        o.Health != Organ.Damage.FINE)
                    {
                        return false;
                    }
                }
            }

            //Do they have at least one leg?
            bool walk = false;
            foreach (BodyPart part in legs)
            {
                walk |= !part.isSevered();
            }

            return walk;
        }

        //People with a missing or broken leg can still walk, but can't flee running.
        public bool canRun()
        {
            if (!canWalk()) return false;

            bool run = true;

            foreach (BodyPart part in legs)
            {
                run &= !(part.isSevered() || part.isBroken());
            }

            return run;
        }

        public bool canGrasp()
        {
            //Are they fully paralyzed?
            foreach (BodyPart part in BodyParts)
            {
                foreach (Organ o in part.Organs)
                {
                    if ((GameData.getData().organList[o.Type].flags & BodyPartDef.PartFlags.PARALYZE_QUAD) != 0 &&
                        o.Health != Organ.Damage.FINE)
                    {
                        return false;
                    }
                }
            }

            //Do they have at least one arm?
            bool grasp = false;
            foreach (BodyPart part in arms)
            {
                grasp |= !(part.Health == BodyPart.Damage.CLEANOFF || part.Health == BodyPart.Damage.NASTYOFF);
            }

            return grasp;
        }

        public int getClinicTime()
        {
            int time = 0;

            if (Blood <= 10) time++;
            if (Blood <= 50) time++;
            if (Blood < 100) time++;

            foreach (BodyPart part in BodyParts)
            {
                if (part.Health == BodyPart.Damage.NASTYOFF && Blood < 100) time++;

                foreach (Organ o in part.Organs)
                {
                    if (o.Health == Organ.Damage.DAMAGED)
                    {
                        if (o.GetType() == typeof(SmallOrgan))
                        {
                            if (((SmallOrgan)o).Count < ((SmallOrgan)o).maxCount) time += GameData.getData().organList[o.Type].clinicTime;
                        }
                        else
                        {
                            time += GameData.getData().organList[o.Type].clinicTime;
                        }
                    }
                }
            }

            return time;
        }

        public bool destroyOrgan(string organType)
        {
            if ((GameData.getData().organList[organType].flags & BodyPartDef.PartFlags.SMALL) == 0) return damageOrgan(organType);
            else
            {
                SmallOrgan organ = (SmallOrgan)getHealthyOrgan(organType);

                if (organ == null) return false;

                organ.destroy();

                return true;
            }
        }

        private bool damageOrgan(Organ organ)
        {
            if (organ == null) return false;

            if (organ.damage() && Blood > GameData.getData().organList[organ.Type].maxHealth)
                Blood = GameData.getData().organList[organ.Type].maxHealth;

            return true;
        }

        public bool damageOrgan(string organType)
        {
            Organ organ = getHealthyOrgan(organType);
            getComponent<Portrait>().forceRegen = true;

            return damageOrgan(organ);
        }

        public int getOrganCount(string organType)
        {
            foreach (BodyPart part in BodyParts)
            {
                foreach (Organ o in part.Organs)
                {
                    if (o.Type == organType && o.GetType() == typeof(SmallOrgan))
                    {
                        return ((SmallOrgan)o).Count;
                    }
                }
            }

            return totalOrganCount[organType];
        }

        public void stabilize(int healPower)
        {
            foreach (BodyPart part in BodyParts)
            {
                //Need proper treatment to cure NASTYOFF limbs
                if ((part.Health & BodyPart.Damage.NASTYOFF) != 0)
                {
                    if (healPower + MasterController.GetMC().LCSRandom(10) > 12)
                        part.Health = BodyPart.Damage.CLEANOFF;
                    else
                        Blood -= 4;
                }

                //Bleeding limbs have a 1/10 chance to heal on their own.
                if ((part.Health & BodyPart.Damage.BLEEDING) != 0)
                {
                    if (healPower + MasterController.GetMC().LCSRandom(10) > 8)
                        part.Health &= ~BodyPart.Damage.BLEEDING;
                    else
                        Blood -= 1;
                }

                //If almost fully healed, clear up the rest of wounds except for severed limbs.
                if (Blood >= 95)
                    part.Health &= BodyPart.Damage.CLEANOFF;
            }
        }

        public void triage()
        {
            foreach (BodyPart part in BodyParts)
            {
                if ((part.Health & BodyPart.Damage.NASTYOFF) != 0)
                {
                    part.staunched = true;
                }

                if ((part.Health & BodyPart.Damage.BLEEDING) != 0)
                {
                    part.staunched = true;
                }
            }
        }

        public bool bleed()
        {
            bool allPartsStaunched = true;

            foreach (BodyPart part in BodyParts)
            {
                if (!part.isSevered())
                {
                    //Internal bleeding CANNOT be treated on-site
                    foreach (Organ o in part.Organs)
                    {
                        if (o.isInjured())
                        {
                            Blood -= GameData.getData().organList[o.Type].bleed;
                        }
                    }
                }

                if (part.staunched) continue;

                allPartsStaunched = false;

                if ((part.Health & BodyPart.Damage.NASTYOFF) != 0)
                {
                    Blood -= 4;
                    getComponent<Inventory>().getArmor().getComponent<Armor>().makeBloody();
                }

                //Bleeding wounds have a small chance to heal on their own based on HEALTH
                if ((part.Health & BodyPart.Damage.BLEEDING) != 0)
                {
                    if (MasterController.GetMC().LCSRandom(500) < getComponent<CreatureBase>().BaseAttributes["HEALTH"].getModifiedValue())
                        part.staunched = true;
                    else
                    {
                        Blood -= 1;
                        getComponent<Inventory>().getArmor().getComponent<Armor>().makeBloody();
                    }
                }
            }

            string deathText = "bled to death";
            if (allPartsStaunched) deathText += " from internal injuries";

            if (Blood <= 0 && Alive)
            {
                getComponent<CreatureBase>().doDie(new Events.Die(deathText));
                if (lastAttacker != null)
                {
                    if ((int)getComponent<CreatureInfo>().alignment == -(int)lastAttacker.getComponent<CreatureInfo>().alignment)
                        lastAttacker.getComponent<CreatureBase>().juiceMe(5 + getComponent<CreatureBase>().Juice / 20);
                    else
                        lastAttacker.getComponent<CreatureBase>().juiceMe(-(5 + getComponent<CreatureBase>().Juice / 20));

                    if (lastAttacker.hasComponent<Liberal>() && lastAttacker.getComponent<Body>().Alive &&
                            (getSpecies().type == "HUMAN" ||
                                (getSpecies().type == "DOG" &&
                                MasterController.government.laws[Constants.LAW_ANIMAL_RESEARCH].alignment == Alignment.ELITE_LIBERAL)))
                    {
                        //This has a bit of a bug in siege escapes where a Lib can be removed from the squad but trigger this check later.
                        //The only solution currently is to just not apply the murder charge to members of the active squad, even though it really should
                        if (lastAttacker.getComponent<Liberal>().squad != null)
                        {
                            foreach (Entity a in lastAttacker.getComponent<Liberal>().squad)
                            {
                                a.getComponent<CriminalRecord>().addCrime(Constants.CRIME_MURDER);
                            }
                        }
                    }
                    MasterController.GetMC().addCombatMessage(getDeathMessage());
                }

                return false;
            }

            return true;
        }

        public bool treatOrgan(int healPower)
        {
            List<Organ> organs = new List<Organ>();

            int mostBleeding = 0;

            foreach (BodyPart part in BodyParts)
            {
                foreach (Organ o in part.Organs)
                {
                    //Prioritize treatment of heaviest bleeding organs first
                    if (o.isInjured())
                    {
                        if (GameData.getData().organList[o.Type].bleed > mostBleeding)
                        {
                            mostBleeding = GameData.getData().organList[o.Type].bleed;
                            organs.Clear();
                            organs.Add(o);
                        }
                        else if (GameData.getData().organList[o.Type].bleed == mostBleeding)
                        {
                            organs.Add(o);
                        }
                    }
                }
            }

            if (organs.Count == 0) return false;
            else
            {
                Organ organ = organs[MasterController.GetMC().LCSRandom(organs.Count)];

                if (healPower + MasterController.GetMC().LCSRandom(10) > GameData.getData().organList[organ.Type].healDiff)
                {
                    if ((GameData.getData().organList[organ.Type].flags & BodyPartDef.PartFlags.VITAL) != 0)
                    {
                        if (MasterController.GetMC().LCSRandom(20) > healPower)
                        {
                            getComponent<CreatureBase>().BaseAttributes["HEALTH"].Level--;
                        }
                    }

                    //We are ignoring the blood loss modifier here since that is temporary and shouldn't affect healing rate.
                    organ.treat(getComponent<CreatureBase>().BaseAttributes["HEALTH"].getModifiedValue());
                    getComponent<Portrait>().forceRegen = true;
                }
                else
                {
                    Blood -= GameData.getData().organList[organ.Type].bleed;
                }
                return true;
            }
        }

        public bool isBleeding()
        {
            //Regardless of what their actual part status is, at this point they are just out of blood.
            if (Blood < -30) return false;

            foreach (BodyPart part in BodyParts)
            {
                if (part.isBleeding())
                    return true;
            }

            return false;
        }

        public bool isInternalBleeding()
        {
            foreach(BodyPart part in BodyParts)
            {
                foreach(Organ o in part.Organs)
                {
                    if (o.isInjured() && GameData.getData().organList[o.Type].bleed > 0)
                        return true;
                }
            }

            return false;
        }

        public string getDeathMessage()
        {
            string deathMessage = "";

            //They should already be dead when this method is called, but just in case
            if (!Alive)
            {
                deathMessage += "<color=yellow>";

                string name;

                if (hasComponent<Liberal>())
                    name = getComponent<CreatureInfo>().getName();
                else
                    name = getComponent<CreatureInfo>().encounterName;

                bool severedHead = false;
                bool severedTorso = false;

                foreach (BodyPart part in BodyParts)
                {
                    if (part.isHead() && part.isSevered()) severedHead = true;
                    if (part.isCore() && part.isSevered()) severedTorso = true;
                }

                if (severedHead)
                {
                    switch (MasterController.GetMC().LCSRandom(4))
                    {
                        case 0:
                            deathMessage += name + " reaches once where there is no head, and slumps over.";
                            break;
                        case 1:
                            deathMessage += name + " stands headless for a moment, then crumples over.";
                            break;
                        case 2:
                            deathMessage += name + " squirts " + MasterController.GetMC().swearFilter("blood", "red water") + " out of the neck and runs down the hall.";
                            break;
                        case 3:
                            deathMessage += name + " sucks a last breath through the neck hole, then is quiet.";
                            break;
                    }
                }
                else if (severedTorso)
                {
                    switch (MasterController.GetMC().LCSRandom(2))
                    {
                        case 0:
                            deathMessage += name + " breaks into pieces.";
                            break;
                        case 1:
                            deathMessage += name + " falls apart and is dead.";
                            break;
                    }
                }
                else
                {
                    switch (MasterController.GetMC().LCSRandom(11))
                    {
                        case 0:
                            deathMessage += name + " cries out one last time then is quiet.";
                            break;
                        case 1:
                            deathMessage += name + " gasps a last breath and " + MasterController.GetMC().swearFilter("soils the floor", "makes a mess") + ".";
                            break;
                        case 2:
                            deathMessage += name + " murmurs quietly, breathing softly. Then all is silent.";
                            break;
                        case 3:
                            deathMessage += name + " shouts \"FATHER! Why have you forsaken me?\" and dies in a heap.";
                            MasterController.GetMC().uiController.doSpeak(new UI.UIEvents.Speak(owner, "FATHER! Why have you forsaken me?"));
                            break;
                        case 4:
                            deathMessage += name + " cries silently for mother, breathing slowly, then not at all.";
                            break;
                        case 5:
                            deathMessage += name + " breathes heavily, coughing up blood... then is quiet.";
                            break;
                        case 6:
                            deathMessage += name + " silently drifts away, and is gone.";
                            break;
                        case 7:
                            deathMessage += name + " sweats profusely, murmurs something " + MasterController.GetMC().swearFilter("", "good") + " about Jesus, and dies.";
                            break;
                        case 8:
                            deathMessage += name + " whines loudly, voice crackling, then curls into a ball, unmoving.";
                            break;
                        case 9:
                            deathMessage += name + " shivers silently, whispering a prayer, then all is still.";
                            break;
                        case 10:
                            deathMessage += name + " speaks these final words: ";
                            string dialog = "";
                            switch (getComponent<CreatureInfo>().alignment)
                            {                                
                                case Alignment.LIBERAL:
                                    dialog = MasterController.lcs.slogan;
                                    break;
                                case Alignment.CONSERVATIVE:
                                    switch (MasterController.GetMC().LCSRandom(2))
                                    {
                                        case 0: dialog = "Better dead than liberal..."; break;
                                        case 1: dialog = "So much for the tolerant left..."; break;
                                    }
                                    break;
                                default:
                                    dialog = "A plague on both your houses...";
                                    break;
                            }
                            deathMessage += "\"" + dialog + "\"";
                            MasterController.GetMC().uiController.doSpeak(new UI.UIEvents.Speak(owner, dialog));
                            break;
                    }
                }

                deathMessage += "</color>";
            }

            return deathMessage;
        }

        public SpeciesDef getSpecies()
        {
            return GameData.getData().speciesList[GameData.getData().creatureDefList[owner.def].species];
        }

        private bool isVitalDamageFresh(BodyPart part)
        {
            foreach (Organ o in part.Organs)
            {
                if (o.isInjured())
                {
                    return true;
                }
            }

            return false;
        }

        private string getCriticalDamageText(BodyPart part)
        {
            string text = "";

            //Who gives a shit about the internal organs of a body part that's no longer attached?
            if (part.isSevered()) return text;

            List<Organ> damagedOrgans = new List<Organ>();

            foreach (Organ o in part.Organs)
            {
                if (o.Health != Organ.Damage.FINE)
                {
                    damagedOrgans.Add(o);
                }
                else if (o.GetType() == typeof(SmallOrgan))
                {
                    if (((SmallOrgan)o).Count < totalOrganCount[o.Type]) damagedOrgans.Add(o);
                }
            }

            foreach (Organ o in damagedOrgans)
            {
                if (o.GetType() == typeof(SmallOrgan))
                {
                    if (((SmallOrgan)o).Count == totalOrganCount[o.Type] - 1) text += GameData.getData().organList[o.Type].name;
                    else
                    {
                        if (((SmallOrgan)o).Count == 0) text += "All ";
                        text += GameData.getData().organList[o.Type].pluralName;
                    }

                    text += " " + GameData.getData().organList[o.Type].damageAdjective;
                }
                else
                {
                    text += GameData.getData().organList[o.Type].damageAdjective + " " + o.Name;
                }

                if (o.Health == Organ.Damage.TREATED)
                {
                    text += " (Healing)";
                    if (MasterController.GetMC().DebugMode) text += " " + o.healDaysRemaining;
                }

                text += "\n";
            }

            return text;
        }

        private Organ getHealthyOrgan(string organType)
        {
            List<Organ> organs = new List<Organ>();

            foreach (BodyPart part in BodyParts)
            {
                foreach (Organ o in part.Organs)
                {
                    if (o.Type == organType && (o.Health == Organ.Damage.FINE || o.Health == Organ.Damage.SCARRED))
                    {
                        organs.Add(o);
                    }
                    else if (o.GetType() == typeof(SmallOrgan) && o.Type == organType)
                    {
                        if (((SmallOrgan)o).Count > 0) organs.Add(o);
                    }
                }
            }

            if (organs.Count != 0) return organs[MasterController.GetMC().LCSRandom(organs.Count)];
            else return null;
        }

        private List<Organ> getDamagedOrgans()
        {
            List<Organ> organs = new List<Organ>();

            foreach (BodyPart part in BodyParts)
            {
                foreach (Organ o in part.Organs)
                {
                    if (o.Health != Organ.Damage.FINE)
                    {
                        organs.Add(o);
                    }
                }
            }

            return organs;
        }

        private string nameGenerator(string baseName, int count, string prefixString, int i)
        {
            List<string> prefixList = new List<string>(prefixString.Split(','));

            if (count == 1)
            {
                return baseName;
            }
            else if (prefixString == "")
            {
                return MasterController.ordinal(i + 1) + " " + baseName;
            }
            else if (count == prefixList.Count)
            {
                return prefixList[i % prefixList.Count] + " " + baseName;
            }
            else if ((count - prefixList.Count) < count)
            {
                //You just had to be difficult, didn't you?
                if (i > (count - prefixList.Count))
                    return prefixList[i % prefixList.Count] + " " + baseName;
                else
                    return MasterController.ordinal(i / prefixList.Count + 1) + " " + prefixList[i % prefixList.Count] + " " + baseName;
            }
            else
            {
                return MasterController.ordinal(i / prefixList.Count + 1) + " " + prefixList[i % prefixList.Count] + " " + baseName;
            }
        }

        public class BodyPart
        {
            [Flags]
            public enum Damage
            {
                FINE = 0,
                SHOOT = 1,
                CUT = 2,
                BRUISE = 4,
                BURN = 8,
                TEAR = 16,
                BLEEDING = 32,
                NASTYOFF = 64,
                CLEANOFF = 128
            }

            public string Type { get; set; }
            public string Name { get; set; }
            public Damage Health { get; set; }
            public List<Organ> Organs { get; set; }
            public bool staunched { get; set; }
            public SpeciesDef.SpeciesBodyPartLocation location { get; set; }

            private XmlNode saveNode;
            private Dictionary<string, XmlNode> saveNodeList;

            public BodyPart(string type, string name)
            {
                this.Type = type;
                this.Name = name;
                this.Health = 0;
                this.Organs = new List<Organ>();
                staunched = true;
            }

            public void save(XmlNode bodyNode)
            {
                if (saveNode == null)
                {
                    saveNodeList = new Dictionary<string, XmlNode>();
                    saveNode = bodyNode.OwnerDocument.CreateElement("BodyPart");
                    bodyNode.AppendChild(saveNode);
                    XmlNode typeNode = saveNode.OwnerDocument.CreateElement("Type");
                    saveNode.AppendChild(typeNode);
                    saveNodeList.Add("Type", typeNode);
                    XmlNode nameNode = saveNode.OwnerDocument.CreateElement("Name");
                    saveNode.AppendChild(nameNode);
                    saveNodeList.Add("Name", nameNode);
                    XmlNode healthNode = saveNode.OwnerDocument.CreateElement("Health");
                    saveNode.AppendChild(healthNode);
                    saveNodeList.Add("Health", healthNode);
                    XmlNode staunchedNode = saveNode.OwnerDocument.CreateElement("staunched");
                    saveNode.AppendChild(staunchedNode);
                    saveNodeList.Add("staunched", staunchedNode);
                    XmlNode locationNode = saveNode.OwnerDocument.CreateElement("location");
                    saveNode.AppendChild(locationNode);
                    saveNodeList.Add("location", locationNode);
                }

                saveNodeList["Type"].InnerText = Type;
                saveNodeList["Name"].InnerText = Name;
                saveNodeList["Health"].InnerText = Health.ToString();
                saveNodeList["staunched"].InnerText = staunched.ToString();
                saveNodeList["location"].InnerText = location.ToString();

                foreach(Organ o in Organs)
                {
                    o.save(saveNode);
                }
            }

            public void load(XmlNode node)
            {
                Type = node.SelectSingleNode("Type").InnerText;
                Name = node.SelectSingleNode("Name").InnerText;
                Health = (Damage) Enum.Parse(typeof(Damage), node.SelectSingleNode("Health").InnerText);
                staunched = bool.Parse(node.SelectSingleNode("staunched").InnerText);
                location = (SpeciesDef.SpeciesBodyPartLocation)Enum.Parse(typeof(SpeciesDef.SpeciesBodyPartLocation), node.SelectSingleNode("location").InnerText);

                foreach(XmlNode organNode in node.SelectNodes("Organ"))
                {
                    //HACK: Saving doesn't differentiate between organs and small organs. Maybe fix this later.
                    if(organNode.SelectSingleNode("Count") != null)
                    {
                        SmallOrgan o = new SmallOrgan("", "", 0);
                        o.load(organNode);
                        Organs.Add(o);
                    }
                    else
                    {
                        Organ o = new Organ("", "");
                        o.load(organNode);
                        Organs.Add(o);
                    }
                }
            }

            public void heal()
            {
                if ((Health & Damage.NASTYOFF) != 0)
                    Health = Damage.CLEANOFF;
                else if ((Health & Damage.CLEANOFF) != 0)
                    Health = Damage.CLEANOFF;
                else
                    Health = Damage.FINE;
            }

            public bool isSevered()
            {
                return (Health & (Damage.CLEANOFF | Damage.NASTYOFF)) != 0;
            }

            public bool isBroken()
            {
                if (!isLimb()) return false;

                foreach (Organ o in Organs)
                {
                    if (o.isInjured()) return true;
                }

                return false;
            }

            public bool isBleeding()
            {
                return (Health & (Damage.BLEEDING | Damage.NASTYOFF)) != 0 && !staunched;
            }

            public bool isLimb()
            { return (GameData.getData().bodyPartList[Type].flags & BodyPartDef.PartFlags.LIMB) != 0; }

            public bool isCore()
            { return (GameData.getData().bodyPartList[Type].flags & BodyPartDef.PartFlags.CORE) != 0; }

            public bool isHead()
            { return (GameData.getData().bodyPartList[Type].flags & BodyPartDef.PartFlags.HEAD) != 0; }

            public BodyPartDef getArmorName()
            { return GameData.getData().bodyPartList[Type].armorname; }
        }

        public class Organ
        {
            public enum Damage
            {
                FINE,
                TREATED,
                DAMAGED,
                SCARRED,
                DESTROYED,
                DESTROYED_RECENT
            }

            public string Type { get; set; }
            public string Name { get; set; }
            public Damage Health { get; set; }
            public int healDaysRemaining { get; set; }

            protected XmlNode saveNode;
            protected Dictionary<string, XmlNode> saveNodeList;

            public Organ(string type, string name)
            {
                this.Type = type;
                this.Name = name;
                this.Health = Damage.FINE;
            }

            public virtual XmlNode save(XmlNode bodypartNode)
            {
                if (saveNode == null)
                {
                    saveNodeList = new Dictionary<string, XmlNode>();
                    saveNode = bodypartNode.OwnerDocument.CreateElement("Organ");
                    bodypartNode.AppendChild(saveNode);
                    XmlNode typeNode = saveNode.OwnerDocument.CreateElement("Type");
                    saveNode.AppendChild(typeNode);
                    saveNodeList.Add("Type", typeNode);
                    XmlNode nameNode = saveNode.OwnerDocument.CreateElement("Name");
                    saveNode.AppendChild(nameNode);
                    saveNodeList.Add("Name", nameNode);
                    XmlNode healthNode = saveNode.OwnerDocument.CreateElement("Health");
                    saveNode.AppendChild(healthNode);
                    saveNodeList.Add("Health", healthNode);
                    XmlNode healDaysRemainingNode = saveNode.OwnerDocument.CreateElement("healDaysRemaining");
                    saveNode.AppendChild(healDaysRemainingNode);
                    saveNodeList.Add("healDaysRemaining", healDaysRemainingNode);

                }

                saveNodeList["Type"].InnerText = Type;
                saveNodeList["Name"].InnerText = Name;
                saveNodeList["Health"].InnerText = Health.ToString();
                saveNodeList["healDaysRemaining"].InnerText = healDaysRemaining + "";

                return saveNode;
            }

            public virtual void load(XmlNode node)
            {
                Type = node.SelectSingleNode("Type").InnerText;
                Name = node.SelectSingleNode("Name").InnerText;
                Health = (Damage)Enum.Parse(typeof(Damage), node.SelectSingleNode("Health").InnerText);
                healDaysRemaining = int.Parse(node.SelectSingleNode("healDaysRemaining").InnerText);
            }

            public virtual bool damage()
            {
                if (Health == Damage.DAMAGED || Health == Damage.DESTROYED) return false;

                BodyPartDef.PartFlags flags = GameData.getData().organList[Type].flags;

                if ((flags & (BodyPartDef.PartFlags.SMELL | BodyPartDef.PartFlags.TASTE | BodyPartDef.PartFlags.VISION)) != 0 && (flags & BodyPartDef.PartFlags.NO_DESTROY) == 0)
                {
                    Health = Damage.DESTROYED_RECENT;
                }
                else
                {
                    Health = Damage.DAMAGED;
                }

                return true;
            }

            public void treat(int healthAttribute)
            {
                if (Health == Damage.DESTROYED_RECENT) heal();
                if (Health != Damage.DAMAGED) return;

                Health = Damage.TREATED;

                int healMonths = GameData.getData().organList[Type].clinicTime;

                healDaysRemaining = (int)(healMonths * 30 * (20f / (healthAttribute + 20f)));
            }

            public virtual bool heal()
            {
                if (Health == Damage.FINE || Health == Damage.SCARRED || Health == Damage.DESTROYED) return false;

                BodyPartDef.PartFlags flags = GameData.getData().organList[Type].flags;

                if ((flags & (BodyPartDef.PartFlags.PARALYZE_PARA | BodyPartDef.PartFlags.PARALYZE_QUAD)) != 0 || GameData.getData().organList[Type].attributes.Count != 0)
                {
                    Health = Damage.SCARRED;
                }
                else if (Health == Damage.DESTROYED_RECENT)
                {
                    Health = Damage.DESTROYED;
                }
                else
                {
                    Health = Damage.FINE;
                }

                return true;
            }

            public bool isInjured()
            { return Health == Damage.DAMAGED || Health == Damage.DESTROYED_RECENT; }
        }

        private class SmallOrgan : Organ
        {
            public int Count { get; set; }
            public int maxCount { get; set; }

            public SmallOrgan(string type, string name, int count) : base(type, name)
            {
                Count = count;
                maxCount = count;
            }

            public override XmlNode save(XmlNode bodypartNode)
            {
                if (saveNode == null)
                {
                    saveNode = base.save(bodypartNode);
                    XmlNode countNode = saveNode.OwnerDocument.CreateElement("Count");
                    saveNode.AppendChild(countNode);
                    saveNodeList.Add("Count", countNode);
                    XmlNode maxCountNode = saveNode.OwnerDocument.CreateElement("maxCount");
                    saveNode.AppendChild(maxCountNode);
                    saveNodeList.Add("maxCount", maxCountNode);
                }
                else
                {
                    base.save(bodypartNode);
                }

                saveNodeList["Count"].InnerText = Count + "";
                saveNodeList["maxCount"].InnerText = maxCount + "";

                return saveNode;
            }

            public override void load(XmlNode node)
            {
                base.load(node);
                Count = int.Parse(node.SelectSingleNode("Count").InnerText);
                maxCount = int.Parse(node.SelectSingleNode("Count").InnerText);
            }

            public bool destroy()
            {
                if (Health == Damage.DESTROYED || Count == 0) return false;

                int count = Count;

                BodyPartDef.PartFlags flags = GameData.getData().organList[Type].flags;

                if ((flags & (BodyPartDef.PartFlags.SMELL | BodyPartDef.PartFlags.TASTE | BodyPartDef.PartFlags.VISION)) != 0 && (flags & BodyPartDef.PartFlags.NO_DESTROY) == 0)
                {
                    Health = Damage.DESTROYED;
                }
                else
                {
                    Health = Damage.DAMAGED;

                    if ((flags & BodyPartDef.PartFlags.SMALL) != 0)
                    {
                        Count -= count;
                        if (Count <= 0)
                        {
                            Count = 0;
                            if ((flags & BodyPartDef.PartFlags.NO_DESTROY) == 0)
                                Health = Damage.DESTROYED;
                        }
                    }
                }

                return true;
            }

            public override bool damage()
            {
                if (Health == Damage.DESTROYED || Count == 0) return false;

                int count = MasterController.GetMC().LCSRandom(Count) + 1;

                BodyPartDef.PartFlags flags = GameData.getData().organList[Type].flags;

                if ((flags & (BodyPartDef.PartFlags.SMELL | BodyPartDef.PartFlags.TASTE | BodyPartDef.PartFlags.VISION)) != 0 && (flags & BodyPartDef.PartFlags.NO_DESTROY) == 0)
                {
                    Health = Damage.DESTROYED;
                }
                else
                {
                    Health = Damage.DAMAGED;

                    if ((flags & BodyPartDef.PartFlags.SMALL) != 0)
                    {
                        Count -= count;
                        if (Count <= 0)
                        {
                            Count = 0;
                            if ((flags & BodyPartDef.PartFlags.NO_DESTROY) == 0)
                                Health = Damage.DESTROYED;
                        }
                    }
                }

                return true;
            }

            public override bool heal()
            {
                if (Health == Damage.FINE || Health == Damage.SCARRED || Health == Damage.DESTROYED) return false;

                BodyPartDef.PartFlags flags = GameData.getData().organList[Type].flags;

                if ((flags & (BodyPartDef.PartFlags.PARALYZE_PARA | BodyPartDef.PartFlags.PARALYZE_QUAD)) != 0 || GameData.getData().organList[Type].attributes.Count != 0)
                {
                    Health = Damage.SCARRED;
                }
                else
                {
                    Health = Damage.FINE;
                    Count = maxCount;
                }

                return true;
            }
        }
    }
}
