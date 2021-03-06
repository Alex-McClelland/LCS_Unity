using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

namespace LCS.Engine.Data
{
    public class CreatureDef : DataDef
    {
        [Flags]
        public enum CreatureFlag
        {
            NONE = 0,
            KIDNAP_RESIST = 1,
            SNITCH = 2,
            ARCHCONSERVATIVE = 4,
            POLICE = 8,
            ILLEGAL_ALIEN = 16,
            TALK_RECEPTIVE = 32,
            BRAVE = 64,
            BRAVE_FIRE = 128,
            DEBATE_LAW = 256,
            DEBATE_SCIENCE = 512,
            DEBATE_POLITICS = 1024,
            DEBATE_BUSINESS = 2048,
            DEBATE_MEDIA = 4096,
            DEBATE_MILITARY = 8192,
            FREEABLE = 16384,
            UNBREAKABLE = 32768,
            HARDCORE = 65536,
            CCS = 131072
        }

        public class CreatureGender
        {
            public string gender = "RANDOM";
            public int transChance = 0;
        }

        public struct CreatureWeapon
        {
            public ItemDef weapon;
            public ItemDef clipType;
            public int ammoCount;
            public string condition;
            public int weight;
        }

        public struct CreatureArmor
        {
            public ItemDef armor;
            public string condition;
            public string quality;
            public string gender;
            public int weight;
        }

        public class CreatureCrime
        {
            public CreatureCrime()
            {
                crimes = new List<CrimeDef>();
            }

            public List<CrimeDef> crimes;
            public int chance = 0;
        }

        public class CreatureSleeper
        {
            public CreatureSleeper()
            {
                bonusSkills = new List<SkillDef>();
                affectedViews = new List<ViewDef>();
            }

            public List<SkillDef> bonusSkills;
            public double powerMultiplier = 2.0;
            public List<ViewDef> affectedViews;
            public ItemDef snoopLoot;
            public LawDef snoopLootLaw;
            public int embezzleMultiplier = 500;
        }

        public CreatureDef()
        {
            weapon = new List<CreatureWeapon>();
            armor = new List<CreatureArmor>();
            attributes = new Dictionary<string, string>();
            skills = new Dictionary<string, string>();
            work_location = new List<string>();
            crimes = new List<CreatureCrime>();
            encounter_name = new List<string>();
            gender = new CreatureGender();
            portraitFlags = new List<string>();
        }
        
        public string type_name = "UNDEFINED";
        public List<string> encounter_name;
        public List<string> work_location;
        public Difficulty stealth_difficulty = Difficulty.VERYEASY;
        public Difficulty disguise_difficulty = Difficulty.VERYEASY;
        public List<CreatureWeapon> weapon;
        public List<CreatureArmor> armor;
        public string age = "18-57";
        public string alignment = "PUBLIC_MOOD";
        public string attribute_points = "40";
        public Dictionary<string, string> attributes;
        public CreatureGender gender;
        public string juice = "0";
        public Dictionary<string, string> skills;
        public string species = "HUMAN";
        public string infiltration = "0";
        public string money = "20-40";
        public CreatureFlag flags = 0;
        public List<string> portraitFlags;
        public List<CreatureCrime> crimes;
        public CreatureSleeper sleeper;
        public int min_accessories = 0;
        public string scars = "0";

        public override void parseData(XmlNode node)
        {
            //defaults
            attributes = new Dictionary<string, string>();
            foreach (string att in GameData.getData().attributeList.Keys)
            {
                attributes[att] = "1-10";
            }
            skills = new Dictionary<string, string>();
            foreach (string skill in GameData.getData().skillList.Keys)
            {
                skills[skill] = "0";
            }
            sleeper = new CreatureSleeper();

            foreach (XmlNode tryNode in node.ChildNodes)
            {
                switch (tryNode.Name)
                {
                    case "gender":
                        gender.gender = tryNode.InnerText;
                        if (tryNode.Attributes["transchance"] != null)
                        {
                            gender.transChance = int.Parse(tryNode.Attributes["transchance"].Value);
                        }
                        break;
                    case "crimes":
                        CreatureCrime crime = new CreatureCrime();
                        crime.chance = int.Parse(tryNode.Attributes["chance"].Value);
                        foreach (XmlNode crimeNode in tryNode.ChildNodes)
                        {
                            crime.crimes.Add(GameData.getData().crimeList[crimeNode.InnerText]);
                        }
                        crimes.Add(crime);
                        break;
                    case "stealth_difficulty":
                        stealth_difficulty = (Difficulty)Enum.Parse(typeof(Difficulty), tryNode.InnerText);
                        break;
                    case "disguise_difficulty":
                        disguise_difficulty = (Difficulty)Enum.Parse(typeof(Difficulty), tryNode.InnerText);
                        break;
                    case "attributes":
                        foreach (XmlNode attribute in tryNode.ChildNodes)
                        {
                            attributes[attribute.SelectSingleNode("type").InnerText] = attribute.SelectSingleNode("value").InnerText;
                        }
                        break;
                    case "skills":
                        foreach (XmlNode skill in tryNode.ChildNodes)
                        {
                            skills[skill.SelectSingleNode("type").InnerText] = skill.SelectSingleNode("value").InnerText;
                        }
                        break;
                    case "weapon":
                        if (tryNode.SelectSingleNode("type").InnerText == "CIVILIAN")
                        {
                            CreatureWeapon revolver38 = new CreatureWeapon();
                            CreatureWeapon semipistol9 = new CreatureWeapon();
                            CreatureWeapon semipistol45 = new CreatureWeapon();
                            CreatureWeapon unarmed1 = new CreatureWeapon();
                            CreatureWeapon unarmed2 = new CreatureWeapon();

                            revolver38.weapon = GameData.getData().itemList["WEAPON_REVOLVER_38"];
                            semipistol9.weapon = GameData.getData().itemList["WEAPON_SEMIPISTOL_9MM"];
                            semipistol45.weapon = GameData.getData().itemList["WEAPON_SEMIPISTOL_45"];
                            unarmed1.weapon = GameData.getData().itemList["WEAPON_NONE"];
                            unarmed2.weapon = GameData.getData().itemList["WEAPON_NONE"];

                            revolver38.condition = "LAW:GUN_CONTROL:=:-1";
                            semipistol9.condition = "LAW:GUN_CONTROL:=:-2";
                            semipistol45.condition = "LAW:GUN_CONTROL:=:-2";
                            unarmed1.condition = "LAW:GUN_CONTROL:=:-1";
                            unarmed2.condition = "LAW:GUN_CONTROL:=:-2";

                            revolver38.weight = 1;
                            semipistol9.weight = 1;
                            semipistol45.weight = 1;
                            unarmed1.weight = 29;
                            unarmed2.weight = 8;

                            revolver38.ammoCount = 4;
                            semipistol9.ammoCount = 4;
                            semipistol45.ammoCount = 4;
                            unarmed1.ammoCount = 0;
                            unarmed2.ammoCount = 0;

                            revolver38.clipType = GameData.getData().itemList["CLIP_38"];
                            semipistol9.clipType = GameData.getData().itemList["CLIP_9"];
                            semipistol45.clipType = GameData.getData().itemList["CLIP_45"];
                            unarmed1.clipType = null;
                            unarmed2.clipType = null;

                            weapon.Add(revolver38);
                            weapon.Add(semipistol9);
                            weapon.Add(semipistol45);
                            weapon.Add(unarmed1);
                            weapon.Add(unarmed2);
                        }
                        else
                        {
                            CreatureWeapon weaponDef = new CreatureWeapon();
                            weaponDef.weapon = GameData.getData().itemList[tryNode.SelectSingleNode("type").InnerText];
                            if (tryNode.Attributes["condition"] != null) weaponDef.condition = tryNode.Attributes["condition"].Value;
                            else weaponDef.condition = "";
                            if (tryNode.Attributes["weight"] != null) weaponDef.weight = int.Parse(tryNode.Attributes["weight"].Value);
                            else weaponDef.weight = 1;
                            if (tryNode.SelectSingleNode("ammo_count") != null) weaponDef.ammoCount = int.Parse(tryNode.SelectSingleNode("ammo_count").InnerText);
                            else weaponDef.ammoCount = 0;
                            if (tryNode.SelectSingleNode("cliptype") != null) weaponDef.clipType = GameData.getData().itemList[tryNode.SelectSingleNode("cliptype").InnerText];
                            else weaponDef.clipType = null;
                            weapon.Add(weaponDef);
                        }
                        break;
                    case "armor":
                        CreatureArmor armorDef = new CreatureArmor();
                        armorDef.armor = GameData.getData().itemList[tryNode.InnerText];
                        if (tryNode.Attributes["condition"] != null) armorDef.condition = tryNode.Attributes["condition"].Value;
                        else armorDef.condition = "";
                        if (tryNode.Attributes["weight"] != null) armorDef.weight = int.Parse(tryNode.Attributes["weight"].Value);
                        else armorDef.weight = 1;
                        if (tryNode.Attributes["quality"] != null) armorDef.quality = tryNode.Attributes["quality"].Value;
                        else armorDef.quality = "1";
                        if (tryNode.Attributes["gender"] != null) armorDef.gender = tryNode.Attributes["gender"].Value;
                        else armorDef.gender = "";
                        armor.Add(armorDef);
                        break;
                    case "flags":
                        foreach (XmlNode flag in tryNode.ChildNodes)
                        {
                            flags |= (CreatureFlag)Enum.Parse(typeof(CreatureFlag), flag.InnerText);
                        }
                        break;
                    case "portrait":
                        if (tryNode.SelectSingleNode("portrait_flags") != null)
                            foreach (XmlNode flag in tryNode.SelectSingleNode("portrait_flags").ChildNodes)
                            {
                                portraitFlags.Add(flag.InnerText);
                            }
                        if (tryNode.SelectSingleNode("min_accessories") != null)
                            min_accessories = int.Parse(tryNode.SelectSingleNode("min_accessories").InnerText);
                        if (tryNode.SelectSingleNode("scars") != null)
                            scars = tryNode.SelectSingleNode("scars").InnerText;
                        break;
                    case "work_location":
                        foreach (XmlNode location in tryNode.ChildNodes)
                        {
                            work_location.Add(location.InnerText);
                        }
                        break;
                    case "encounter_name":
                        encounter_name.Add(tryNode.InnerText);
                        break;
                    case "sleeper":
                        if (tryNode.SelectSingleNode("skills") != null)
                        {
                            foreach (XmlNode sleeperNode in tryNode.SelectSingleNode("skills").ChildNodes)
                            {
                                sleeper.bonusSkills.Add(GameData.getData().skillList[sleeperNode.InnerText]);
                            }
                        }
                        if (tryNode.SelectSingleNode("views") != null)
                        {
                            foreach (XmlNode sleeperNode in tryNode.SelectSingleNode("views").ChildNodes)
                            {
                                sleeper.affectedViews.Add(GameData.getData().viewList[sleeperNode.InnerText]);
                            }
                        }
                        if (tryNode.SelectSingleNode("power") != null) sleeper.powerMultiplier = double.Parse(tryNode.SelectSingleNode("power").InnerText);
                        if (tryNode.SelectSingleNode("snoop") != null)
                        {
                            sleeper.snoopLoot = GameData.getData().itemList[tryNode.SelectSingleNode("snoop/loot").InnerText];
                            sleeper.snoopLootLaw = GameData.getData().lawList[tryNode.SelectSingleNode("snoop/law").InnerText];
                        }
                        if (tryNode.SelectSingleNode("embezzle") != null) sleeper.embezzleMultiplier = int.Parse(tryNode.SelectSingleNode("embezzle").InnerText);
                        break;
                    default:
                        FieldInfo f = GetType().GetField(tryNode.Name);

                        if (f == null)
                        {
                            MasterController.GetMC().addErrorMessage("Bad tag in Creature: " + node.Attributes["idname"].Value + ", " + tryNode.Name);
                            break;
                        }

                        f.SetValue(this, tryNode.InnerText);
                        break;
                }
            }

            if (encounter_name.Count == 0)
            {
                encounter_name.Add(type_name);
            }
        }
    }
}
