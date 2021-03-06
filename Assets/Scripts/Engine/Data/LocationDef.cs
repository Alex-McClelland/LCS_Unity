using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace LCS.Engine.Data
{
    public class LocationDef : DataDef
    {
        public abstract class LocationComponent { }

        public class TroubleSpotDef : LocationComponent
        {
            public TroubleSpotDef()
            {
                encounters = new List<EncounterDef>();
                appropriateDisguises = new List<DisguiseDef>();
                partialDisguises = new List<DisguiseDef>();
                affectedViews = new List<ViewDef>();
                lootTable = new Dictionary<List<TroubleLootDef>, int>();
            }

            public string map = "GENERIC_UNSECURE";
            public int graffiti = 0;
            public ViewDef newsHeader;
            public List<ViewDef> affectedViews;
            public List<DisguiseDef> appropriateDisguises;
            public List<DisguiseDef> partialDisguises;
            public List<EncounterDef> encounters;
            public Dictionary<List<TroubleLootDef>, int> lootTable;
            public EnemyType responseType = EnemyType.POLICE;
            public TroubleSpotFlag flags = 0;
        }

        public enum EnemyType
        {
            POLICE,
            MERC,
            AGENT,
            REDNECK,
            ARMY,
            GANG,
            FIREMEN,
            CCS
        }

        public class TroubleLootDef
        {
            public ItemDef item;
            public string condition = "";
        }

        [Flags]
        public enum TroubleSpotFlag
        {
            NONE = 0,
            HIGH_SECURITY = 1,
            MID_SECURITY = 2,
            NEWS_PRIORITY_NONE = 4,
            NEWS_PRIORITY_LOW = 8,
            NEWS_PRIORITY_HIGH = 16,
            SIEGE_ONLY = 32,
            RESTRICTED = 64,
            RESIDENTIAL = 128
        }

        public class DisguiseDef
        {
            public ItemDef itemType;
            public string conditions = "";
        }

        public class EncounterDef
        {
            public int weight;
            public CreatureDef creatureType;
            public string conditions = "";
        }

        public class BaseDef : LocationComponent
        {
            public int rentPrice = 0;
            public int secrecy;
            public BaseFlag flags = 0;
        }

        [Flags]
        public enum BaseFlag
        {
            NONE = 0,
            UPGRADABLE = 1,
            CCS_BASE = 2,
            CAPTURABLE = 4
        }

        public class ShopDef : LocationComponent
        {
            public List<ShopDepartmentDef> departments;
            public ShopFlag flags = 0;

            public ShopDef()
            {
                departments = new List<ShopDepartmentDef>();
            }
        }

        public class ShopDepartmentDef
        {
            public List<ShopItemDef> items;
            public string name = "";

            public ShopDepartmentDef()
            {
                items = new List<ShopItemDef>();
            }
        }

        public class ShopItemDef
        {
            public ItemDef item;
            public string text = "";
            public int price = 0;
        }

        [Flags]
        public enum ShopFlag
        {
            NONE = 0,
            LEGAL_ONLY = 1,
            SELL_ITEMS = 2,
            VEHICLES = 4
        }

        public LocationDef()
        {
            names = new List<ConditionalName>();
            components = new Dictionary<string, LocationComponent>();
        }
        
        public List<ConditionalName> names;
        public string ccsName;
        public string hide = "";
        public Dictionary<string, LocationComponent> components;
        public int hospital = 0;

        public override void parseData(XmlNode node)
        {
            if (node.SelectSingleNode("hospital") != null) hospital = int.Parse(node.SelectSingleNode("hospital").InnerText);

            foreach (XmlNode nameNode in node.SelectNodes("name"))
            {
                ConditionalName name = new ConditionalName();
                name.name = nameNode.InnerText;
                if (nameNode.Attributes["condition"] != null)
                {
                    name.condition = nameNode.Attributes["condition"].Value;
                }

                if (nameNode.Attributes["shortname"] != null)
                {
                    name.shortName = nameNode.Attributes["shortname"].Value;
                }
                else
                {
                    name.shortName = name.name;
                }

                names.Add(name);
            }

            if (node.SelectSingleNode("ccsname") != null)
            {
                ccsName = node.SelectSingleNode("ccsname").InnerText;
            }

            if (node.SelectSingleNode("hide") != null)
            {
                hide = node.SelectSingleNode("hide").InnerText;
            }

            foreach (XmlNode innerNode in node.ChildNodes)
            {
                LocationComponent componentDef = null;

                switch (innerNode.Name)
                {
                    case "base":
                        componentDef = new BaseDef();
                        BaseDef baseDef = (BaseDef)componentDef;
                        baseDef.secrecy = int.Parse(innerNode.SelectSingleNode("secrecy").InnerText);
                        if (innerNode.SelectSingleNode("rentprice") != null)
                        {
                            baseDef.rentPrice = int.Parse(innerNode.SelectSingleNode("rentprice").InnerText);
                        }
                        foreach (XmlNode flag in innerNode.SelectNodes("flag"))
                        {
                            baseDef.flags |= (BaseFlag)Enum.Parse(typeof(BaseFlag), flag.InnerText);
                        }
                        break;
                    case "trouble":
                        componentDef = new TroubleSpotDef();
                        TroubleSpotDef troubleDef = (TroubleSpotDef)componentDef;

                        troubleDef.map = innerNode.SelectSingleNode("map").InnerText;
                        foreach (XmlNode encounterNode in innerNode.SelectSingleNode("encounters").ChildNodes)
                        {
                            try
                            {
                                EncounterDef encounterDef = new EncounterDef();

                                encounterDef.weight = int.Parse(encounterNode.SelectSingleNode("weight").InnerText);
                                encounterDef.creatureType = GameData.getData().creatureDefList[encounterNode.SelectSingleNode("type").InnerText];
                                if (encounterNode.Attributes["condition"] != null)
                                {
                                    encounterDef.conditions = encounterNode.Attributes["condition"].Value;
                                }

                                troubleDef.encounters.Add(encounterDef);
                            }
                            catch (KeyNotFoundException)
                            {
                                MasterController.GetMC().addErrorMessage("Creature def not found for: " + encounterNode.SelectSingleNode("type").InnerText);
                            }
                        }
                        if (innerNode.SelectSingleNode("views") != null)
                        {
                            foreach (XmlNode viewNode in innerNode.SelectSingleNode("views").ChildNodes)
                            {
                                troubleDef.affectedViews.Add(GameData.getData().viewList[viewNode.InnerText]);
                            }
                        }

                        if (innerNode.SelectSingleNode("newsheader") != null)
                        {
                            troubleDef.newsHeader = GameData.getData().viewList[innerNode.SelectSingleNode("newsheader").InnerText];
                        }

                        foreach (XmlNode disguiseNode in innerNode.SelectNodes("disguise"))
                        {
                            DisguiseDef disguise = new DisguiseDef();
                            if (disguiseNode.Attributes["condition"] != null) disguise.conditions = disguiseNode.Attributes["condition"].Value;
                            disguise.itemType = GameData.getData().itemList[disguiseNode.InnerText];
                            troubleDef.appropriateDisguises.Add(disguise);
                        }

                        foreach (XmlNode disguiseNode in innerNode.SelectNodes("partial_disguise"))
                        {
                            DisguiseDef disguise = new DisguiseDef();
                            if (disguiseNode.Attributes["condition"] != null) disguise.conditions = disguiseNode.Attributes["condition"].Value;
                            disguise.itemType = GameData.getData().itemList[disguiseNode.InnerText];
                            troubleDef.partialDisguises.Add(disguise);
                        }

                        foreach (XmlNode lootNode in innerNode.SelectNodes("loot"))
                        {
                            List<TroubleLootDef> lootList = new List<TroubleLootDef>();
                            int weight = 1;
                            if (lootNode.Attributes["weight"] != null) weight = int.Parse(lootNode.Attributes["weight"].Value);
                            troubleDef.lootTable.Add(lootList, weight);
                            foreach (XmlNode itemNode in lootNode.ChildNodes)
                            {
                                TroubleLootDef tld = new TroubleLootDef();
                                tld.item = GameData.getData().itemList[itemNode.InnerText];
                                if (itemNode.Attributes["condition"] != null) tld.condition = itemNode.Attributes["condition"].Value;
                                lootList.Add(tld);
                            }
                        }

                        if (innerNode.SelectSingleNode("graffiti") != null) troubleDef.graffiti = int.Parse(innerNode.SelectSingleNode("graffiti").InnerText);
                        if (innerNode.SelectSingleNode("responsetype") != null) troubleDef.responseType = (EnemyType)Enum.Parse(typeof(EnemyType), innerNode.SelectSingleNode("responsetype").InnerText);

                        foreach (XmlNode flag in innerNode.SelectNodes("flag"))
                        {
                            troubleDef.flags |= (TroubleSpotFlag)Enum.Parse(typeof(TroubleSpotFlag), flag.InnerText);
                        }
                        break;
                    case "shop":
                        componentDef = new ShopDef();
                        ShopDef shopDef = (ShopDef)componentDef;

                        foreach (XmlNode department in innerNode.SelectNodes("department"))
                        {
                            ShopDepartmentDef departmentDef = new ShopDepartmentDef();
                            if (department.Attributes["name"] != null) departmentDef.name = department.Attributes["name"].Value;
                            foreach (XmlNode item in department.SelectNodes("item"))
                            {
                                ShopItemDef itemDef = new ShopItemDef();
                                itemDef.item = GameData.getData().itemList[item.Attributes["idname"].Value];
                                if (item.SelectSingleNode("text") != null)
                                    itemDef.text = item.SelectSingleNode("text").InnerText;
                                itemDef.price = int.Parse(item.SelectSingleNode("price").InnerText);
                                departmentDef.items.Add(itemDef);
                            }
                            shopDef.departments.Add(departmentDef);
                        }

                        foreach (XmlNode flag in innerNode.SelectNodes("flag"))
                        {
                            shopDef.flags |= (ShopFlag)Enum.Parse(typeof(ShopFlag), flag.InnerText);
                        }
                        break;
                    default:
                        continue;
                }

                components.Add(innerNode.Name, componentDef);
            }
        }
    }
}
