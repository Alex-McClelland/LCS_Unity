using System;
using System.Collections.Generic;
using System.Xml;

namespace LCS.Engine.Data
{
    public class SpeciesDef : DataDef
    {
        public class SpeciesBodyPart
        {
            public BodyPartDef bodyPart;
            public string count = "1";
            public string prefix;
            public string names;
            public SpeciesBodyPartLocation location = SpeciesBodyPartLocation.MID;
        }

        public enum SpeciesBodyPartLocation
        {
            LOW,
            MID,
            HIGH
        }

        public SpeciesDef()
        {
            parts = new Dictionary<string, SpeciesBodyPart>();
            image = new List<string>();
            naturalWeapon = new List<ItemDef>();
            naturalArmor = new List<ItemDef>();
        }
        
        public string name;
        public int oldage = 60;
        public List<string> image;
        public List<ItemDef> naturalWeapon;
        public List<ItemDef> naturalArmor;
        public Dictionary<string, SpeciesBodyPart> parts;

        public override void parseData(XmlNode node)
        {
            name = node.SelectSingleNode("name").InnerText;
            foreach (XmlNode weaponNode in node.SelectNodes("naturalweapon"))
            {
                naturalWeapon.Add(GameData.getData().itemList[weaponNode.InnerText]);
            }
            if (naturalWeapon.Count == 0) naturalWeapon.Add(GameData.getData().itemList["WEAPON_NONE"]);
            foreach (XmlNode armorNode in node.SelectNodes("naturalarmor"))
            {
                naturalArmor.Add(GameData.getData().itemList[armorNode.InnerText]);
            }
            if (naturalArmor.Count == 0) naturalArmor.Add(GameData.getData().itemList["ARMOR_NONE"]);

            if (node.SelectSingleNode("oldage") != null) oldage = int.Parse(node.SelectSingleNode("oldage").InnerText);
            foreach (XmlNode imageNode in node.SelectNodes("image"))
            {
                image.Add(node.SelectSingleNode("image").InnerText);
            }
            if (image.Count == 0) image.Add("GEN");

            foreach (XmlNode innerNode in node.SelectSingleNode("parts").ChildNodes)
            {
                SpeciesBodyPart bodyPartTag = new SpeciesBodyPart();
                bodyPartTag.bodyPart = GameData.getData().bodyPartList[innerNode.InnerText];

                if (innerNode.Attributes["count"] != null)
                    bodyPartTag.count = innerNode.Attributes["count"].Value;
                else
                    bodyPartTag.count = "1";

                if (innerNode.Attributes["prefix"] != null)
                    bodyPartTag.prefix = innerNode.Attributes["prefix"].Value;
                else
                    bodyPartTag.prefix = "";

                if (innerNode.Attributes["names"] != null)
                    bodyPartTag.names = innerNode.Attributes["names"].Value;
                else
                    bodyPartTag.names = "";

                if (innerNode.Attributes["location"] != null)
                    bodyPartTag.location = (SpeciesBodyPartLocation)Enum.Parse(typeof(SpeciesBodyPartLocation), innerNode.Attributes["location"].Value);
                else
                    bodyPartTag.location = SpeciesBodyPartLocation.MID;

                parts.Add(bodyPartTag.bodyPart.type, bodyPartTag);
            }
        }
    }
}
