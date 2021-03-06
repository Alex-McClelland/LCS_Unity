using System;
using System.Collections.Generic;
using System.Linq;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Location;
using LCS.Engine.Components.Item;
using LCS.Engine.Data;

namespace LCS.Engine.Factories
{
    public static class CreatureFactory
    {
        public static Entity create(string idname)
        {
            MasterController mc = MasterController.GetMC();
            GameData dl = GameData.getData();

            //PRISONER is a special designation that is not a real type, but instead represents anyone that can be generated as a prisoner
            //inside the prison or when freeing people from the police/courthouse lockup
            bool prisoner = idname == "PRISONER";
            if (prisoner)
            {
                if (mc.LCSRandom(10) == 0)
                    idname = "THIEF";
                else
                    switch (mc.LCSRandom(5))
                    {
                        case 0:
                            idname = "GANGMEMBER";
                            break;
                        case 1:
                            idname = "PROSTITUTE";
                            break;
                        case 2:
                            idname = "CRACKHEAD";
                            break;
                        case 3:
                            idname = "TEENAGER";
                            break;
                        case 4:
                            idname = "HSDROPOUT";
                            break;
                    }
            }


            if (!dl.creatureDefList.ContainsKey(idname))
            {
                MasterController.GetMC().addErrorMessage("No creature def found for " + idname);
                return null;
            }

            //basic setup
            Entity creature = new Entity("creature", idname);

            //define and associate attributes and skills
            CreatureBase creatureBase = new CreatureBase();
            creature.setComponent(creatureBase);

            foreach (AttributeDef attribute in dl.attributeList.Values)
            {
                creatureBase.addAttribute(attribute.type);
            }

            foreach (SkillDef skill in dl.skillList.Values)
            {
                creatureBase.addSkill(skill.type);
            }

            //Fill in their human qualities
            //Happy birthday!

            Age creatureAge = new Age();
            creature.setComponent(creatureAge);

            int birthMonth = mc.LCSRandom(12) + 1;
            int birthDay = 0;
            switch (birthMonth)
            {
                case 4:
                case 6:
                case 9:
                case 11:
                    birthDay = mc.LCSRandom(30) + 1;
                    break;
                case 2:
                    birthDay = mc.LCSRandom(28) + 1;
                    break;
                default:
                    birthDay = mc.LCSRandom(31) + 1;
                    break;
            }

            int age = 0;

            switch (dl.creatureDefList[idname].age)
            {
                case "DOGYEARS":
                    age = mc.LCSRandom("2-7");
                    break;
                case "CHILD":
                    age = mc.LCSRandom("7-10");
                    break;
                case "TEENAGER":
                    age = mc.LCSRandom("14-17");
                    break;
                case "YOUNGADULT":
                    age = mc.LCSRandom("18-35");
                    break;
                case "MATURE":
                    age = mc.LCSRandom("20-59");
                    break;
                case "GRADUATE":
                    age = mc.LCSRandom("26-59");
                    break;
                case "MIDDLEAGED":
                    age = mc.LCSRandom("35-59");
                    break;
                case "SENIOR":
                    age = mc.LCSRandom("65-94");
                    break;
                default:
                    age = mc.LCSRandom(dl.creatureDefList[idname].age);
                    break;
            }

            int birthYear = mc.currentDate.Year - age;

            //If current time of year is before their birthday then birth year will be off by 1 since age always rounds down.
            if (mc.currentDate.Month < birthMonth || (mc.currentDate.Month == birthMonth && mc.currentDate.Day < birthDay))
            {
                birthYear -= 1;
            }

            if(idname == "FOUNDER") birthYear = 1984;
            
            creatureAge.birthday = new DateTime(birthYear, birthMonth, birthDay);

            CreatureInfo creatureInfo = new CreatureInfo();
            creature.setComponent(creatureInfo);

            //birth gender first, then self-identified gender (same as birth for now)
            CreatureInfo.CreatureGender genderConservative;

            if (dl.creatureDefList[idname].gender.gender == "RANDOM"){
                switch (mc.LCSRandom(2))
                {
                    case 0:
                        genderConservative = CreatureInfo.CreatureGender.MALE;
                        break;
                    default:
                        genderConservative = CreatureInfo.CreatureGender.FEMALE;
                        break;
                }
            }
            else if (dl.creatureDefList[idname].gender.gender == "MALE_BIAS")
            {
                Alignment womensRights = MasterController.government.laws["WOMEN"].alignment;
                if(womensRights == Alignment.ARCHCONSERVATIVE)
                {
                    genderConservative = CreatureInfo.CreatureGender.MALE;
                }
                else if (womensRights == Alignment.CONSERVATIVE && mc.LCSRandom(25) != 0)
                {
                    genderConservative = CreatureInfo.CreatureGender.MALE;
                }
                else if(womensRights == Alignment.MODERATE && mc.LCSRandom(10) != 0)
                {
                    genderConservative = CreatureInfo.CreatureGender.MALE;
                }
                else if(womensRights == Alignment.LIBERAL && mc.LCSRandom(4) != 0)
                {
                    genderConservative = CreatureInfo.CreatureGender.MALE;
                }
                else
                {
                    switch (mc.LCSRandom(2))
                    {
                        case 0:
                            genderConservative = CreatureInfo.CreatureGender.MALE;
                            break;
                        default:
                            genderConservative = CreatureInfo.CreatureGender.FEMALE;
                            break;
                    }
                }  
            }
            else if (dl.creatureDefList[idname].gender.gender == "FEMALE_BIAS")
            {
                Alignment womensRights = MasterController.government.laws["WOMEN"].alignment;
                if (womensRights == Alignment.ARCHCONSERVATIVE)
                {
                    genderConservative = CreatureInfo.CreatureGender.FEMALE;
                }
                else if (womensRights == Alignment.CONSERVATIVE && mc.LCSRandom(25) != 0)
                {
                    genderConservative = CreatureInfo.CreatureGender.FEMALE;
                }
                else if (womensRights == Alignment.MODERATE && mc.LCSRandom(10) != 0)
                {
                    genderConservative = CreatureInfo.CreatureGender.FEMALE;
                }
                else if (womensRights == Alignment.LIBERAL && mc.LCSRandom(4) != 0)
                {
                    genderConservative = CreatureInfo.CreatureGender.FEMALE;
                }
                else
                {
                    switch (mc.LCSRandom(2))
                    {
                        case 0:
                            genderConservative = CreatureInfo.CreatureGender.MALE;
                            break;
                        default:
                            genderConservative = CreatureInfo.CreatureGender.FEMALE;
                            break;
                    }
                }
            }
            else
            {
                genderConservative = (CreatureInfo.CreatureGender) Enum.Parse(typeof(CreatureInfo.CreatureGender), dl.creatureDefList[idname].gender.gender);
            }
                        
            CreatureInfo.CreatureGender genderLiberal = genderConservative;

            if (mc.LCSRandom(100) < dl.creatureDefList[idname].gender.transChance)
            {
                switch (mc.LCSRandom(2))
                {
                    case 0:
                        genderLiberal = CreatureInfo.CreatureGender.FEMALE;
                        break;
                    case 1:
                        genderLiberal = CreatureInfo.CreatureGender.MALE;
                        break;
                }
            }

            creatureInfo.genderLiberal = genderLiberal;
            creatureInfo.genderConservative = genderConservative;

            //political align

            if (dl.creatureDefList[idname].alignment == "PUBLIC_MOOD")
            {
                creatureInfo.alignment = Alignment.CONSERVATIVE;
                if (mc.LCSRandom(100) < MasterController.generalPublic.PublicMood) creatureInfo.alignment++;
                if (mc.LCSRandom(100) < MasterController.generalPublic.PublicMood) creatureInfo.alignment++;
            }
            else if (dl.creatureDefList[idname].alignment == "NON_LIBERAL")
            {
                creatureInfo.alignment = Alignment.CONSERVATIVE;
                if (mc.LCSRandom(100) < MasterController.generalPublic.PublicMood) creatureInfo.alignment++;
            }
            else if (dl.creatureDefList[idname].alignment == "NON_MODERATE")
            {
                creatureInfo.alignment = Alignment.CONSERVATIVE;
                if (mc.LCSRandom(100) < MasterController.generalPublic.PublicMood) creatureInfo.alignment+=2;
            }
            else if (dl.creatureDefList[idname].alignment == "NON_CONSERVATIVE")
            {
                creatureInfo.alignment = Alignment.MODERATE;
                if (mc.LCSRandom(100) < MasterController.generalPublic.PublicMood) creatureInfo.alignment++;
            }
            else
            {
                creatureInfo.alignment = (Alignment) Enum.Parse(typeof(Alignment), dl.creatureDefList[idname].alignment);
            }

            //I mean this is in the def file anyway but if anything should be hard-coded it's this
            if (idname == "FOUNDER") creatureInfo.alignment = Alignment.LIBERAL;

            //Prisoners are always non-conservative
            if (prisoner && creatureInfo.alignment == Alignment.CONSERVATIVE)
            {
                creatureInfo.alignment = Alignment.MODERATE;
                if (mc.LCSRandom(100) < MasterController.generalPublic.PublicMood) creatureInfo.alignment++;
            }

            //Small random chance that this character was born intersex (done here because their alignment needs to be set to determine how they identify)
            if (mc.LCSRandom(50) == 0 && creatureInfo.genderConservative != CreatureInfo.CreatureGender.WHITEMALEPATRIARCH)
            {
                creatureInfo.genderConservative = CreatureInfo.CreatureGender.NEUTRAL;
                //Intersex Liberals will self-identify as such, but non-Liberals are too gender binary to change their gender identity
                if (creatureInfo.alignment == Alignment.LIBERAL)
                {
                    creatureInfo.genderLiberal = CreatureInfo.CreatureGender.NEUTRAL;
                }
            }

            //descriptors
            creatureInfo.type_name = dl.creatureDefList[idname].type_name;
            creatureInfo.encounterName = dl.creatureDefList[idname].encounter_name[mc.LCSRandom(dl.creatureDefList[idname].encounter_name.Count)];
            if (prisoner) creatureInfo.encounterName = "Prisoner";
            //Add "Pet" prefix to genetic monsters encountered outside the lab
            if (idname == "GENETIC" && (mc.currentSiteModeScene == null || mc.currentSiteModeScene.location.def != "LABORATORY_GENETIC"))
                creatureInfo.encounterName = "Pet " + creatureInfo.encounterName;

            //name
            creatureInfo.givenName = generateGivenName(creatureInfo.genderLiberal);
            creatureInfo.surname = generateSurname(creatureInfo.genderLiberal);

            creatureInfo.alias = "";

            //If the City hasn't been built yet then skip this bit
            if (mc.worldState != null)
            {
                if (dl.creatureDefList[idname].work_location.Count == 0)
                {
                    //Default work location if the creature type doesn't have any defined
                    creatureInfo.workLocation = MasterController.nation.cities["DC"].getComponent<City>().getLocation("RESIDENTIAL_SHELTER");
                }
                else
                { 
                    if (mc.phase == MasterController.Phase.TROUBLE && dl.creatureDefList[idname].work_location.Contains(mc.currentSiteModeScene.location.def))
                    {
                        creatureInfo.workLocation = mc.currentSiteModeScene.location;
                    }
                    else
                    {
                        //Special handling for CCS to ensure that they will always generate their work locations "in order"
                        if((dl.creatureDefList[idname].flags & CreatureDef.CreatureFlag.CCS) != 0)
                        {
                            if (MasterController.nation.cities["DC"].getComponent<City>().getLocation("CCS_BAR").getComponent<SiteBase>().hidden)
                            {
                                creatureInfo.workLocation = MasterController.nation.cities["DC"].getComponent<City>().getLocation("CCS_BAR");
                            }
                            else if (MasterController.nation.cities["DC"].getComponent<City>().getLocation("CCS_BOMBSHELTER").getComponent<SiteBase>().hidden)
                            {
                                if(MasterController.nation.cities["DC"].getComponent<City>().getLocation("CCS_BAR").getComponent<SafeHouse>().owned)
                                    creatureInfo.workLocation = MasterController.nation.cities["DC"].getComponent<City>().getLocation("CCS_BOMBSHELTER");
                                else
                                    creatureInfo.workLocation = MasterController.nation.cities["DC"].getComponent<City>().getLocation("CCS_BAR");
                            }
                            else if (MasterController.nation.cities["DC"].getComponent<City>().getLocation("CCS_BUNKER").getComponent<SiteBase>().hidden)
                            {
                                if (MasterController.nation.cities["DC"].getComponent<City>().getLocation("CCS_BOMBSHELTER").getComponent<SafeHouse>().owned)
                                    creatureInfo.workLocation = MasterController.nation.cities["DC"].getComponent<City>().getLocation("CCS_BUNKER");
                                else
                                    creatureInfo.workLocation = MasterController.nation.cities["DC"].getComponent<City>().getLocation("CCS_BOMBSHELTER");
                            }
                            else
                            {
                                //TODO: Make this generate locations in cities other than DC
                                creatureInfo.workLocation = MasterController.nation.cities["DC"].getComponent<City>().getLocation(dl.creatureDefList[idname].work_location[mc.LCSRandom(dl.creatureDefList[idname].work_location.Count)]);
                            }
                        }
                        else
                            //TODO: Make this generate locations in cities other than DC
                            creatureInfo.workLocation = MasterController.nation.cities["DC"].getComponent<City>().getLocation(dl.creatureDefList[idname].work_location[mc.LCSRandom(dl.creatureDefList[idname].work_location.Count)]);
                    }
                }
            }

            if (mc.currentSiteModeScene != null)
                creatureBase.Location = mc.currentSiteModeScene.location;

            //Body
            Body body = new Body(dl.creatureDefList[idname].species);
            creature.setComponent(body);

            Dictionary<string, SpeciesDef.SpeciesBodyPart> bodyParts = dl.speciesList[dl.creatureDefList[idname].species].parts;

            foreach (SpeciesDef.SpeciesBodyPart part in bodyParts.Values)
            {
                body.addPart(part);
            }

            //Criminal Record setup
            CriminalRecord criminalRecord = new CriminalRecord();
            creature.setComponent(criminalRecord);

            foreach (CrimeDef crimeDef in dl.crimeList.Values)
            {
                criminalRecord.CrimesWanted.Add(crimeDef.type, 0);
                criminalRecord.CrimesPunished.Add(crimeDef.type, 0);
                criminalRecord.CrimesAcquitted.Add(crimeDef.type, 0);
            }

            if (!prisoner)
            {
                if (dl.creatureDefList[idname].crimes.Count != 0)
                {
                    foreach (CreatureDef.CreatureCrime crime in dl.creatureDefList[idname].crimes)
                    {
                        if (mc.LCSRandom(100) < crime.chance)
                        {
                            criminalRecord.CrimesWanted[crime.crimes[mc.LCSRandom(crime.crimes.Count)].type]++;
                        }
                    }
                }
            }
            else
            {
                if (dl.creatureDefList[idname].crimes.Count != 0)
                {
                    foreach (CreatureDef.CreatureCrime crime in dl.creatureDefList[idname].crimes)
                    {
                        if (mc.LCSRandom(100) < crime.chance)
                        {
                            criminalRecord.CrimesWanted[crime.crimes[mc.LCSRandom(crime.crimes.Count)].type]++;
                        }
                    }
                }

                //If they aren't actually wanted for anything, give them some random charge that has jailtime associated
                if (!criminalRecord.isCriminal())
                {
                    switch (mc.LCSRandom(5))
                    {
                        case 0: criminalRecord.addCrime(Constants.CRIME_ARMED_ASSAULT); break;
                        case 1: criminalRecord.addCrime(Constants.CRIME_CAR_THEFT); break;
                        case 2: criminalRecord.addCrime(Constants.CRIME_ARSON); break;
                        case 3: criminalRecord.addCrime(Constants.CRIME_BANK_ROBBERY); break;
                        case 4: criminalRecord.addCrime(Constants.CRIME_BROWNIES); break;
                    }
                }

                if (mc.currentSiteModeScene != null && mc.currentSiteModeScene.location.def == "GOVERNMENT_PRISON")
                {
                    criminalRecord.calculateSentence();
                    criminalRecord.Heat = 0;

                    //Prisoners can't still be WANTED for crimes, but should have records
                    List<string> crimes = new List<string>(criminalRecord.CrimesWanted.Keys);
                    foreach (string crime in crimes)
                    {
                        criminalRecord.CrimesPunished[crime] = criminalRecord.CrimesWanted[crime];
                        criminalRecord.CrimesWanted[crime] = 0;
                    }

                    //Randomize some time already served on their sentence
                    if (criminalRecord.LifeSentences == 0)
                    {
                        int timeServed = mc.LCSRandom(criminalRecord.CurrentSentence - 5);
                        if (timeServed < 0) timeServed = 0;
                        criminalRecord.TotalTimeServed = timeServed;
                        criminalRecord.CurrentSentence -= timeServed;
                    }
                    else
                    {
                        int timeServed = mc.LCSRandom(36) + 12;
                        criminalRecord.TotalTimeServed = timeServed;
                    }
                }
            }

            //Give weapon/armor
            Inventory inventory = new Inventory();
            creature.setComponent(inventory);

            inventory.naturalWeapon = ItemFactory.create(dl.speciesList[dl.creatureDefList[idname].species].naturalWeapon[mc.LCSRandom(dl.speciesList[dl.creatureDefList[idname].species].naturalWeapon.Count)].type);
            inventory.naturalArmor = ItemFactory.create(dl.speciesList[dl.creatureDefList[idname].species].naturalArmor[mc.LCSRandom(dl.speciesList[dl.creatureDefList[idname].species].naturalArmor.Count)].type);

            //Debators have natural compelling voice attacks instead of their species default
            if ((dl.creatureDefList[idname].flags & 
                (CreatureDef.CreatureFlag.DEBATE_BUSINESS | 
                CreatureDef.CreatureFlag.DEBATE_LAW | 
                CreatureDef.CreatureFlag.DEBATE_MEDIA | 
                CreatureDef.CreatureFlag.DEBATE_MILITARY | 
                CreatureDef.CreatureFlag.DEBATE_POLITICS | 
                CreatureDef.CreatureFlag.DEBATE_SCIENCE)) != 0)
                inventory.naturalWeapon = ItemFactory.create("WEAPON_VOICE");

            if (!prisoner)
            {
                Dictionary<CreatureDef.CreatureWeapon, int> weaponOptions = new Dictionary<CreatureDef.CreatureWeapon, int>();

                foreach (CreatureDef.CreatureWeapon weapon in dl.creatureDefList[idname].weapon)
                {
                    if (mc.testCondition(weapon.condition, mc.phase == MasterController.Phase.TROUBLE? MasterController.lcs.activeSquad.target.getComponent<TroubleSpot>() : null))
                    {
                        weaponOptions.Add(weapon, weapon.weight);
                    }
                }

                bool CCS = false;
                if (weaponOptions.Count > 0)
                {
                    CreatureDef.CreatureWeapon weaponChoice = mc.WeightedRandom(weaponOptions);
                    if(weaponChoice.weapon.type == "CCS")
                    {
                        CCS = true;
                        switch(mc.LCSRandom(5) + (int)MasterController.ccs.status)
                        {
                            case 0:
                            case 1:
                                inventory.equipArmor(ItemFactory.create("ARMOR_CLOTHES"));
                                break;
                            case 2:
                                inventory.equipArmor(ItemFactory.create("ARMOR_CLOTHES"));
                                inventory.equipWeapon(ItemFactory.create("WEAPON_SEMIPISTOL_9MM"));
                                for (int i = 0; i < 7; i++)
                                    inventory.equipClip(ItemFactory.create("CLIP_9"));
                                inventory.reload(false);
                                break;
                            case 3:
                                inventory.equipArmor(ItemFactory.create("ARMOR_CLOTHES"));
                                inventory.equipWeapon(ItemFactory.create("WEAPON_REVOLVER_44"));
                                for (int i = 0; i < 7; i++)
                                    inventory.equipClip(ItemFactory.create("CLIP_44"));
                                inventory.reload(false);
                                break;
                            case 4:
                                inventory.equipArmor(ItemFactory.create("ARMOR_CLOTHES"));
                                inventory.equipWeapon(ItemFactory.create("WEAPON_SHOTGUN_PUMP"));
                                for (int i = 0; i < 7; i++)
                                    inventory.equipClip(ItemFactory.create("CLIP_BUCKSHOT"));
                                inventory.reload(false);
                                break;
                            case 5:
                                inventory.equipArmor(ItemFactory.create("ARMOR_CIVILIANARMOR"));
                                inventory.equipWeapon(ItemFactory.create("WEAPON_SEMIRIFLE_AR15"));
                                for (int i = 0; i < 7; i++)
                                    inventory.equipClip(ItemFactory.create("CLIP_ASSAULT_CIVILIAN"));
                                inventory.reload(false);
                                break;
                            case 6:
                                inventory.equipArmor(ItemFactory.create("ARMOR_ARMYARMOR"));
                                inventory.equipWeapon(ItemFactory.create("WEAPON_SEMIRIFLE_AR15"));
                                for (int i = 0; i < 7; i++)
                                    inventory.equipClip(ItemFactory.create("CLIP_ASSAULT"));
                                inventory.reload(false);
                                break;
                            case 7:
                                inventory.equipArmor(ItemFactory.create("ARMOR_ARMYARMOR"));
                                inventory.equipWeapon(ItemFactory.create("WEAPON_AUTORIFLE_M16"));
                                for (int i = 0; i < 7; i++)
                                    inventory.equipClip(ItemFactory.create("CLIP_ASSAULT"));
                                inventory.reload(false);
                                break;
                        }
                    }
                    else if (weaponChoice.weapon.type != "WEAPON_NONE")
                    {
                        inventory.equipWeapon(ItemFactory.create(weaponChoice.weapon.type));

                        if (weaponChoice.clipType != null)
                        {
                            for (int i = 0; i < weaponChoice.ammoCount; i++)
                            {
                                inventory.equipClip(ItemFactory.create(weaponChoice.clipType.type));
                            }

                            inventory.reload(false);
                        }
                    }
                }

                //If CCS was selected for weapon type, skip armor since it's already been assigned
                if (!CCS)
                {
                    Dictionary<CreatureDef.CreatureArmor, int> armorOptions = new Dictionary<CreatureDef.CreatureArmor, int>();

                    foreach (CreatureDef.CreatureArmor armor in dl.creatureDefList[idname].armor)
                    {
                        if (mc.testCondition(armor.condition, mc.phase == MasterController.Phase.TROUBLE ? MasterController.lcs.activeSquad.target.getComponent<TroubleSpot>() : null))
                        {
                            if (armor.gender == "" || genderLiberal == CreatureInfo.CreatureGender.NEUTRAL || (CreatureInfo.CreatureGender)Enum.Parse(typeof(CreatureInfo.CreatureGender), armor.gender) == genderLiberal)
                                armorOptions.Add(armor, armor.weight);
                        }
                    }

                    if (armorOptions.Count > 0)
                    {
                        CreatureDef.CreatureArmor armorChoice = mc.WeightedRandom(armorOptions);

                        if (armorChoice.armor.type != "ARMOR_NONE")
                        {
                            inventory.equipArmor(ItemFactory.create(armorChoice.armor.type));
                        }
                    }
                }
            }
            else
            {
                inventory.equipArmor(ItemFactory.create("ARMOR_PRISONER"));
                if (mc.LCSRandom(2) == 0)
                    inventory.equipWeapon(ItemFactory.create("WEAPON_SHANK"));
            }

            //Rename CCS members with a cover identity that will match their equipment
            if(creatureInfo.encounterName == "CCS")
            {
                if (inventory.getArmor().def == "ARMOR_CIVILIANARMOR")
                    creatureInfo.encounterName = "Elite Security";
                else if (inventory.getArmor().def == "ARMOR_ARMYARMOR")
                    creatureInfo.encounterName = "Soldier";
                else if(inventory.getWeapon().def == "WEAPON_SHOTGUN_PUMP" || mc.LCSRandom(2) == 0)
                {
                    switch (mc.LCSRandom(7))
                    {
                        case 0: creatureInfo.encounterName = "Country Boy"; break;
                        case 1: creatureInfo.encounterName = "Good ol' Boy"; break;
                        case 2: creatureInfo.encounterName = "Hick"; break;
                        case 3: creatureInfo.encounterName = "Hillbilly"; break;
                        case 4: creatureInfo.encounterName = "Redneck"; break;
                        case 5: creatureInfo.encounterName = "Rube"; break;
                        case 6: creatureInfo.encounterName = "Yokel"; break;
                    }
                }
                else
                {
                    switch (mc.LCSRandom(10))
                    {
                        case 0: creatureInfo.encounterName = "Biker"; break;
                        case 1: creatureInfo.encounterName = "Transient"; break;
                        case 2: creatureInfo.encounterName = "Crackhead"; break;
                        case 3: creatureInfo.encounterName = "Fast Food Worker"; break;
                        case 4: creatureInfo.encounterName = "Telemarketer"; break;
                        case 5: creatureInfo.encounterName = "Office Worker"; break;
                        case 6: creatureInfo.encounterName = "Mailman"; break;
                        case 7: creatureInfo.encounterName = "Musician"; break;
                        case 8: creatureInfo.encounterName = "Hairstylist"; break;
                        case 9: creatureInfo.encounterName = "Bartender"; break;
                    }
                }
            }

            inventory.naturalArmor.getComponent<ItemBase>().Location = creature;
            inventory.naturalWeapon.getComponent<ItemBase>().Location = creature;

            //distribute attribute points
            creatureBase.Juice = mc.LCSRandom(dl.creatureDefList[idname].juice);

            //total number available
            int attnum = mc.LCSRandom(dl.creatureDefList[idname].attribute_points);
            List<int> possible = new List<int>();

            //set minimum stat values first before adding
            int entry = 0;
            foreach (KeyValuePair<string, string> attRange in dl.creatureDefList[idname].attributes)
            {
                int minValue = 0;

                int dashpos = attRange.Value.IndexOf('-');
                if (dashpos == -1)
                {
                    minValue = int.Parse(attRange.Value);
                }
                else
                {
                    minValue = int.Parse(attRange.Value.Substring(0, dashpos));
                }

                creatureBase.BaseAttributes[attRange.Key].Level = minValue;
                attnum -= Math.Min(4,minValue);
                possible.Add(entry);
                entry++;
            }
            //keep adding points until there are none left to distribute or everything is maxed out.
            while (attnum > 0 && possible.Any())
            {
                entry = mc.LCSRandom(possible.Count);
                int a = possible[entry];
                string attType = dl.attributeList.Keys.ToList()[a];

                //Libs should be much more likely to gain heart than wisdom, and vice-versa for cons.
                if(attType.Equals("WISDOM") && creatureInfo.alignment == Alignment.LIBERAL  && mc.LCSRandom(4) > 0)
                {
                    attType = "HEART";
                }
                if (attType.Equals("HEART") && creatureInfo.alignment == Alignment.CONSERVATIVE && mc.LCSRandom(4) > 0)
                {
                    attType = "WISDOM";
                }

                int maxValue = 10;

                int dashpos = dl.creatureDefList[idname].attributes[attType].IndexOf('-');
                if (dashpos == -1)
                {
                    maxValue = int.Parse(dl.creatureDefList[idname].attributes[attType]);
                }
                else
                {
                    maxValue = int.Parse(dl.creatureDefList[idname].attributes[attType].Substring(dashpos + 1));
                }

                if (creatureBase.BaseAttributes[attType].Level < maxValue)
                {
                    creatureBase.BaseAttributes[attType].Level++;
                    attnum--;
                }
                else
                {
                    possible.RemoveAt(entry);
                }
            }

            //Assign skills
            foreach (KeyValuePair<string, SkillDef> skill in dl.skillList) {
                creatureBase.Skills[skill.Key].level = mc.LCSRandom(dl.creatureDefList[idname].skills[skill.Key]);
                if (creatureBase.Skills[skill.Key].level > creatureBase.Skills[skill.Key].associatedAttribute.Level)
                {
                    creatureBase.Skills[skill.Key].level = creatureBase.Skills[skill.Key].associatedAttribute.Level;
                    creatureBase.Skills[skill.Key].experience = 99 + 10*creatureBase.Skills[skill.Key].level;
                }
            }
            //Random starting skills
            int randomSkills = mc.LCSRandom(4) + 4;
            if (creatureAge.getAge() > 20)
            {
                randomSkills += (int) (randomSkills * ((creatureAge.getAge() - 20.0) / 20.0));
            }
            else
            {
                randomSkills -= (20 - creatureAge.getAge()) / 2;
            }

            possible.Clear();
            for(entry = 0; entry< dl.skillList.Count; entry++)
            {
                possible.Add(entry);
            }
            while(randomSkills>0 && possible.Any())
            {
                entry = mc.LCSRandom(possible.Count);
                int a = possible[entry];

                string skillType = dl.skillList.Keys.ToList()[a];

                // 95% chance of not allowing some skills for anybody...
                if (mc.LCSRandom(20) > 0)
                {
                    if (skillType.Equals("HEAVY_WEAPONS")) continue;
                    if (skillType.Equals("SMG")) continue;
                    if (skillType.Equals("SWORD")) continue;
                    if (skillType.Equals("RIFLE")) continue;
                    if (skillType.Equals("AXE")) continue;
                    if (skillType.Equals("CLUB")) continue;
                    if (skillType.Equals("PSYCHOLOGY")) continue;
                }

                // 90% chance of not allowing some skills for non-conservatives
                if(mc.LCSRandom(10) > 0 && creatureInfo.alignment != Alignment.CONSERVATIVE)
                {
                    if (skillType.Equals("SHOTGUN")) continue;
                    if (skillType.Equals("PISTOL")) continue;
                }

                if(creatureBase.Skills[skillType].level < creatureBase.Skills[skillType].associatedAttribute.Level)
                {
                    creatureBase.Skills[skillType].level++;
                    randomSkills--;

                    while(randomSkills > 0 && mc.LCSRandom(2) > 0)
                    {
                        if (creatureBase.Skills[skillType].level < creatureBase.Skills[skillType].associatedAttribute.Level && creatureBase.Skills[skillType].level < 4)
                        {
                            creatureBase.Skills[skillType].level++;
                            randomSkills--;
                        }
                        else
                        {
                            possible.RemoveAt(entry);
                            break;
                        }
                    }
                }
                else
                {
                    possible.RemoveAt(entry);
                }
            }

            if(idname == "FOUNDER")
            {
                //Forget everything we just did, set attributes manually
                creatureBase.BaseAttributes["HEART"].Level = 8;
                creatureBase.BaseAttributes["INTELLIGENCE"].Level = 3;
                creatureBase.BaseAttributes["WISDOM"].Level = 1;
                creatureBase.BaseAttributes["HEALTH"].Level = 6;
                creatureBase.BaseAttributes["AGILITY"].Level = 5;
                creatureBase.BaseAttributes["STRENGTH"].Level = 4;
                creatureBase.BaseAttributes["CHARISMA"].Level = 4;

                foreach (CreatureBase.Skill skill in creatureBase.Skills.Values)
                {
                    skill.level = 0;
                }
            }

            Portrait portrait = new Portrait();
            creature.setComponent(portrait);
            portrait.makeMyFace();

            return creature;
        }

        public static string generateGivenName(CreatureInfo.CreatureGender gender = CreatureInfo.CreatureGender.NEUTRAL, int seed = 0)
        {
            List<string> nameTable = new List<string>();
            GameData dl = GameData.getData();
            MasterController mc = MasterController.GetMC();

            if (gender == CreatureInfo.CreatureGender.NEUTRAL)
            {
                nameTable.AddRange(dl.nameLists["first_name_male"]);
                nameTable.AddRange(dl.nameLists["first_name_female"]);
                nameTable.AddRange(dl.nameLists["first_name_neutral"]);
            }
            else if (gender == CreatureInfo.CreatureGender.MALE)
            {
                nameTable.AddRange(dl.nameLists["first_name_male"]);
                nameTable.AddRange(dl.nameLists["first_name_neutral"]);
            }
            else if (gender == CreatureInfo.CreatureGender.FEMALE)
            {
                nameTable.AddRange(dl.nameLists["first_name_female"]);
                nameTable.AddRange(dl.nameLists["first_name_neutral"]);
            }
            else if (gender == CreatureInfo.CreatureGender.WHITEMALEPATRIARCH)
            {
                nameTable.AddRange(dl.nameLists["first_name_patriarch"]);
            }

            if (seed == 0)
                return nameTable[mc.LCSRandom(nameTable.Count)];
            else
                return nameTable[new Random(seed).Next(nameTable.Count)];
        }

        public static string generateSurname(CreatureInfo.CreatureGender gender = CreatureInfo.CreatureGender.NEUTRAL, int seed = 0)
        {
            List<string> nameTable = new List<string>();
            GameData dl = GameData.getData();
            MasterController mc = MasterController.GetMC();

            nameTable.Clear();

            nameTable.AddRange(dl.nameLists["surname_archconservative"]);

            if (gender != CreatureInfo.CreatureGender.WHITEMALEPATRIARCH)
            {
                nameTable.AddRange(dl.nameLists["surname_regular"]);
            }

            if (seed == 0)
                return nameTable[mc.LCSRandom(nameTable.Count)];
            else
                return nameTable[new Random(seed).Next(nameTable.Count)];
        }
    }
}
