using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

namespace LCS.Engine.Data
{
    public class ItemDef : DataDef
    {
        public abstract class ItemComponent { }

        public class WeaponDef : ItemComponent
        {
            public WeaponDef()
            {
                attack = new List<AttackDef>();
            }
            public int legality = 2;
            public int size = 15;
            public int bashStrengthMod = 100;
            public List<AttackDef> attack;
            public WeaponFlags flags;
            public ItemDef defaultClip;
        }

        [Flags]
        public enum WeaponFlags
        {
            NONE = 0,
            TAKE_HOSTAGE = 1,
            THREATENING = 2,
            INSTRUMENT = 4,
            GRAFFITI = 8,
            BREAK_LOCK = 16,
            NOT_SUSPICIOUS = 32,
            THROWN = 64,
            ALWAYS_LOADED = 128
        }

        public class ArmorDef : ItemComponent
        {
            public ArmorDef()
            {
                bodyCovering = new List<BodyPartDef>();
                armor = new Dictionary<string, int>();
                appropriateWeapons = new List<string>();
            }

            public int make_difficulty = 0;
            public int make_price = 0;
            public int stealth_value = 0;
            public int professionalism = 2;
            public int conceal_weapon_size = 5;
            public int qualitylevels = 4;
            public int durability = 10;
            public List<BodyPartDef> bodyCovering;
            public List<string> appropriateWeapons;
            public Dictionary<string, int> armor;
            public ArmorFlags flags = 0;
            public int basePower = 0;
            public int assaultBonus = 0;
            public int drugBonus = 0;
        }

        [Flags]
        public enum ArmorFlags
        {
            NONE = 0,
            DEATHSQUAD = 1,
            NO_BLOODY = 2,
            NO_DAMAGE = 4,
            FIRE_PROTECTION = 8,
            CONCEAL_FACE = 16,
            POLICE = 32,
            SWAT = 64,
            FIRE_VULN = 128,
            DATE_APPROPRIATE = 256
        }

        public class ClipDef : ItemComponent
        {
            public string ammoType;
            public int ammo = 1;
            public int legality = 2;
        }

        public class LootDef : ItemComponent
        {

            public int fenceValue = 0;
            public LootFlags flags = 0;
            public List<LootEvidence> evidence;

            public LootDef()
            {
                evidence = new List<LootEvidence>();
            }
        }

        [Flags]
        public enum LootFlags
        {
            NONE = 0,
            QUICK_FENCE = 1,
            CLOTH = 2
        }

        public class LootEvidence
        {
            public Dictionary<ViewDef, int> affectedIssues;
            public List<string> descriptionText;
            public List<CrimeDef> lawsBroken;
            public LootEvidenceOffendedGroups offendedGroup = LootEvidenceOffendedGroups.NONE;

            public LootEvidence()
            {
                affectedIssues = new Dictionary<ViewDef, int>();
                lawsBroken = new List<CrimeDef>();
                descriptionText = new List<string>();
            }

            public enum LootEvidenceOffendedGroups
            {
                NONE,
                AMRADIO,
                CABLENEWS,
                FIREMEN,
                CIA,
                CORPS
            }
        }

        public class VehicleDef : ItemComponent
        {
            public VehicleDef()
            {
                colors = new List<VehicleColor>();
            }
            
            public UnityEngine.Sprite vehicleIcon = null;
            //If this value = 0 then the current year will be used
            public int startYear = 0;
            //If this value = 0 and startYear != 0, then a random year between startYear and the current year will be chosen.
            public int addRandom = 0;
            public List<VehicleColor> colors;
            public int driveBase = 0;
            public double driveSkill = 1;
            public int driveSoftCap = 8;
            public int driveHardCap = 99;
            public int dodgeBase = 0;
            public double dodgeSkill = 1;
            public int dodgeSoftCap = 8;
            public int dodgeHardCap = 99;
            public int attackDriver = -2;
            public int attackPassenger = 0;
            public int stealDifficulty = 1;
            public int stealJuice = 0;
            public int stealHeat = 0;
            public int stealSenseAlarm = 0;
            public int stealTouchAlarm = 0;
            public string armorLow = "5";
            public string armorHigh = "1";
            public int armorMidpoint = 50;
            public int size = 2;
            public VehicleFlags flags = VehicleFlags.NONE;
        }

        [Flags]
        public enum VehicleFlags
        {
            NONE = 0
        }

        public enum VehicleColor
        {
            RED,
            WHITE,
            BLUE,
            TAN,
            BLACK,
            YELLOW,
            GREEN,
            POLICE
        }

        public ItemDef()
        {
            components = new Dictionary<string, ItemComponent>();
        }
        
        public string name;
        public string shortname;
        public string nameFuture = "";
        public string shortnameFuture = "";
        public string description = "";
        public UnityEngine.Sprite icon = null;
        public UnityEngine.Sprite futureicon = null;
        public Dictionary<string, ItemComponent> components;

        public override void parseData(XmlNode node)
        {
            name = node.SelectSingleNode("name").InnerText;
            if (node.SelectSingleNode("icon") != null)
            {
                string iconName = node.SelectSingleNode("icon").InnerText;
                if (GameData.getData().itemGraphicList.ContainsKey(iconName))
                    icon = GameData.getData().itemGraphicList[iconName];
                else
                    MasterController.GetMC().addErrorMessage("icon " + iconName + " not found for item " + type);
            }

            if (node.SelectSingleNode("icon_future") != null)
            {
                string futureiconName = node.SelectSingleNode("icon_future").InnerText;
                if (GameData.getData().itemGraphicList.ContainsKey(futureiconName))
                    futureicon = GameData.getData().itemGraphicList[futureiconName];
                else
                    MasterController.GetMC().addErrorMessage("icon_future " + futureiconName + " not found for item " + type);
            }

            if (node.SelectSingleNode("name_future") != null) nameFuture = node.SelectSingleNode("name_future").InnerText;
            if (node.SelectSingleNode("shortname") != null) shortname = node.SelectSingleNode("shortname").InnerText;
            if (node.SelectSingleNode("shortname_future") != null) shortnameFuture = node.SelectSingleNode("shortname_future").InnerText;
            if (node.SelectSingleNode("description") != null) description = node.SelectSingleNode("description").InnerText;

            if (shortname == null) shortname = name;
            if (shortnameFuture == "") shortnameFuture = nameFuture;

            foreach (XmlNode innerNode in node.ChildNodes)
            {
                ItemComponent component = null;

                switch (innerNode.Name)
                {
                    default:
                        continue;
                    case "vehicle":
                        component = parseVehicle(innerNode);
                        break;
                    case "weapon":
                        component = parseWeapon(innerNode);
                        break;
                    case "armor":
                        component = parseArmor(innerNode);
                        break;
                    case "loot":
                        component = new LootDef();
                        LootDef lootDef = (LootDef)component;
                        if (innerNode.SelectSingleNode("fencevalue") != null) lootDef.fenceValue = int.Parse(innerNode.SelectSingleNode("fencevalue").InnerText);
                        if (innerNode.SelectSingleNode("flags") != null)
                        {
                            foreach (XmlNode tag in innerNode.SelectSingleNode("flags").ChildNodes)
                            {
                                lootDef.flags |= (LootFlags)Enum.Parse(typeof(LootFlags), tag.InnerText);
                            }
                        }
                        foreach (XmlNode evidenceNode in innerNode.SelectNodes("evidence"))
                        {
                            LootEvidence evidence = new LootEvidence();
                            foreach (XmlNode issueNode in evidenceNode.SelectNodes("affectedissue"))
                            {
                                evidence.affectedIssues.Add(GameData.getData().viewList[issueNode.SelectSingleNode("issue").InnerText], int.Parse(issueNode.SelectSingleNode("strength").InnerText));
                            }
                            foreach (XmlNode descriptionNode in evidenceNode.SelectNodes("description"))
                            {
                                evidence.descriptionText.Add(descriptionNode.InnerText);
                            }
                            foreach (XmlNode lawNode in evidenceNode.SelectNodes("lawbroken"))
                            {
                                evidence.lawsBroken.Add(GameData.getData().crimeList[lawNode.InnerText]);
                            }
                            if (evidenceNode.SelectSingleNode("offendedgroup") != null)
                            {
                                evidence.offendedGroup = (LootEvidence.LootEvidenceOffendedGroups)Enum.Parse(typeof(LootEvidence.LootEvidenceOffendedGroups), evidenceNode.SelectSingleNode("offendedgroup").InnerText);
                            }

                            lootDef.evidence.Add(evidence);
                        }
                        break;
                    case "clip":
                        component = new ClipDef();
                        ClipDef clipDef = (ClipDef)component;
                        clipDef.ammo = int.Parse(innerNode.SelectSingleNode("ammo").InnerText);
                        clipDef.ammoType = innerNode.SelectSingleNode("ammotype").InnerText;
                        if (innerNode.SelectSingleNode("legality") != null) clipDef.legality = int.Parse(innerNode.SelectSingleNode("legality").InnerText);
                        break;
                }

                components.Add(innerNode.Name, component);
            }
        }

        private WeaponDef parseWeapon(XmlNode node)
        {
            WeaponDef def = new WeaponDef();

            if (node.SelectSingleNode("legality") != null) def.legality = int.Parse(node.SelectSingleNode("legality").InnerText);
            if (node.SelectSingleNode("size") != null) def.size = int.Parse(node.SelectSingleNode("size").InnerText);
            if (node.SelectSingleNode("bashstrengthmod") != null) def.bashStrengthMod = int.Parse(node.SelectSingleNode("bashstrengthmod").InnerText);
            if (node.SelectSingleNode("default_clip") != null) def.defaultClip = GameData.getData().itemList[node.SelectSingleNode("default_clip").InnerText];

            //This SHOULD be defined but just in case
            if (node.SelectSingleNode("attacks") != null)
            {
                foreach (XmlNode attack in node.SelectSingleNode("attacks").ChildNodes)
                {
                    def.attack.Add(GameData.getData().attackList[attack.InnerText]);
                }
            }

            if (node.SelectSingleNode("flags") != null)
            {
                foreach (XmlNode tag in node.SelectSingleNode("flags").ChildNodes)
                {
                    def.flags |= (WeaponFlags)Enum.Parse(typeof(WeaponFlags), tag.InnerText);
                }
            }

            return def;
        }

        private ArmorDef parseArmor(XmlNode node)
        {
            ArmorDef def = new ArmorDef();

            if (node.SelectSingleNode("body_covering") == null)
            {
                def.bodyCovering.Add(GameData.getData().bodyPartList["HUMAN_TORSO"]);
                def.bodyCovering.Add(GameData.getData().bodyPartList["HUMAN_ARM"]);
                def.bodyCovering.Add(GameData.getData().bodyPartList["HUMAN_LEG"]);
            }

            foreach (XmlNode armorNode in node.ChildNodes)
            {
                switch (armorNode.Name)
                {
                    case "body_covering":
                        foreach (XmlNode childNode in armorNode.ChildNodes)
                        {
                            def.bodyCovering.Add(GameData.getData().bodyPartList[childNode.InnerText]);
                        }
                        break;
                    case "armor":
                        foreach (XmlNode childNode in armorNode.ChildNodes)
                        {
                            def.armor.Add(childNode.Name, int.Parse(childNode.InnerText));
                        }
                        break;
                    case "flags":
                        foreach (XmlNode childNode in armorNode.ChildNodes)
                        {
                            def.flags |= (ArmorFlags)Enum.Parse(typeof(ArmorFlags), childNode.InnerText);
                        }
                        break;
                    case "appropriate_weapon":
                        foreach (XmlNode childNode in armorNode.ChildNodes)
                        {
                            def.appropriateWeapons.Add(childNode.InnerText);
                        }
                        break;
                    case "interrogation":
                        if (armorNode.SelectSingleNode("basepower") != null) def.basePower = int.Parse(armorNode.SelectSingleNode("basepower").InnerText);
                        if (armorNode.SelectSingleNode("assaultbonus") != null) def.assaultBonus = int.Parse(armorNode.SelectSingleNode("assaultbonus").InnerText);
                        if (armorNode.SelectSingleNode("drugbonus") != null) def.drugBonus = int.Parse(armorNode.SelectSingleNode("drugbonus").InnerText);
                        break;
                    default:
                        object p = (object)def;
                        FieldInfo f = p.GetType().GetField(armorNode.Name);

                        if (f == null)
                        {
                            MasterController.GetMC().addErrorMessage("Bad tag in Armor: " + node.Attributes["idname"].Value + ", " + armorNode.Name);
                            break;
                        }

                        f.SetValue(p, int.Parse(armorNode.InnerText));
                        def = (ArmorDef)p;
                        break;
                }
            }

            return def;
        }

        private VehicleDef parseVehicle(XmlNode node)
        {
            VehicleDef def = new VehicleDef();

            if (node.SelectSingleNode("icon") != null)
            {
                string iconName = node.SelectSingleNode("icon").InnerText;
                if (GameData.getData().itemGraphicList.ContainsKey(iconName))
                    def.vehicleIcon = GameData.getData().itemGraphicList[iconName];
                else
                    MasterController.GetMC().addErrorMessage("vehicle icon " + iconName + " not found for item " + type);
            }

            if (node.SelectSingleNode("year/start_at_year") != null) def.startYear = int.Parse(node.SelectSingleNode("year/start_at_year").InnerText);
            if (node.SelectSingleNode("year/add_random") != null) def.addRandom = int.Parse(node.SelectSingleNode("year/add_random").InnerText);

            foreach (XmlNode innerNode in node.SelectSingleNode("colors").ChildNodes)
            {
                def.colors.Add((VehicleColor)Enum.Parse(typeof(VehicleColor), innerNode.InnerText));
            }

            def.driveBase = int.Parse(node.SelectSingleNode("drivebonus/base").InnerText);
            def.driveSkill = double.Parse(node.SelectSingleNode("drivebonus/skillfactor").InnerText);
            def.driveSoftCap = int.Parse(node.SelectSingleNode("drivebonus/softlimit").InnerText);
            def.driveHardCap = int.Parse(node.SelectSingleNode("drivebonus/hardlimit").InnerText);

            def.dodgeBase = int.Parse(node.SelectSingleNode("dodgebonus/base").InnerText);
            def.dodgeSkill = double.Parse(node.SelectSingleNode("dodgebonus/skillfactor").InnerText);
            def.dodgeSoftCap = int.Parse(node.SelectSingleNode("dodgebonus/softlimit").InnerText);
            def.dodgeHardCap = int.Parse(node.SelectSingleNode("dodgebonus/hardlimit").InnerText);

            def.attackDriver = int.Parse(node.SelectSingleNode("attackbonus/driver").InnerText);
            def.attackPassenger = int.Parse(node.SelectSingleNode("attackbonus/passenger").InnerText);

            def.armorLow = node.SelectSingleNode("armor/low_armor").InnerText;
            def.armorHigh = node.SelectSingleNode("armor/high_armor").InnerText;
            def.armorMidpoint = int.Parse(node.SelectSingleNode("armor/armor_midpoint").InnerText);

            def.size = int.Parse(node.SelectSingleNode("size").InnerText);

            def.stealDifficulty = int.Parse(node.SelectSingleNode("stealing/difficulty_to_find").InnerText);
            def.stealJuice = int.Parse(node.SelectSingleNode("stealing/juice").InnerText);
            def.stealHeat = int.Parse(node.SelectSingleNode("stealing/extra_heat").InnerText);
            def.stealTouchAlarm = int.Parse(node.SelectSingleNode("stealing/touch_alarm_chance").InnerText);
            def.stealSenseAlarm = int.Parse(node.SelectSingleNode("stealing/sense_alarm_chance").InnerText);

            if (node.SelectSingleNode("flags") != null)
            {
                foreach (XmlNode innerNode in node.SelectSingleNode("flags").ChildNodes)
                {
                    def.flags |= (VehicleFlags)Enum.Parse(typeof(VehicleFlags), innerNode.InnerText);
                }
            }

            return def;
        }
    }
}
