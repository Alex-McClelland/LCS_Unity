using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace LCS.Engine.Data
{
    public class OrganDef : DataDef
    {
        public enum OrganDamageType
        {
            NONE,
            POKE,
            HEAVY,
            BREAK
        }

        public class OrganAttribute
        {
            public AttributeDef attribute;
            public int value = 1;
        }

        public OrganDef()
        {
            attributes = new List<OrganAttribute>();
            damageVerbs = new Dictionary<string, string>();
            damageVerbs.Add("DEFAULT", "damaged");
        }
        
        public string name;
        public string pluralName;
        public string damageAdjective = "Damaged";
        public int bleed = 0;
        public int maxHealth = 100;
        public int fightPenalty = 0;
        public int clinicTime = 0;
        public int healDiff = 14;
        public OrganDamageType damageRequired = OrganDamageType.NONE;
        public Dictionary<string, string> damageVerbs;
        public List<OrganAttribute> attributes;
        public BodyPartDef.PartFlags flags = 0;

        public override void parseData(XmlNode node)
        {
            name = node.SelectSingleNode("name").InnerText;
            pluralName = node.SelectSingleNode("pluralname").InnerText;
            damageAdjective = node.SelectSingleNode("damagedadjective").InnerText;
            if (node.SelectSingleNode("bleed") != null) bleed = int.Parse(node.SelectSingleNode("bleed").InnerText);
            if (node.SelectSingleNode("maxhealth") != null) maxHealth = int.Parse(node.SelectSingleNode("maxhealth").InnerText);
            if (node.SelectSingleNode("fightpenalty") != null) fightPenalty = int.Parse(node.SelectSingleNode("fightpenalty").InnerText);
            if (node.SelectSingleNode("clinictime") != null) clinicTime = int.Parse(node.SelectSingleNode("clinictime").InnerText);
            if (node.SelectSingleNode("healdiff") != null) healDiff = int.Parse(node.SelectSingleNode("healdiff").InnerText);
            if (node.SelectSingleNode("damagerequired") != null) damageRequired = (OrganDamageType)Enum.Parse(typeof(OrganDamageType), node.SelectSingleNode("damagerequired").InnerText);

            if (node.SelectSingleNode("damageverbs") != null)
            {
                foreach (XmlNode innerNode in node.SelectSingleNode("damageverbs").ChildNodes)
                {
                    damageVerbs[innerNode.Name] = innerNode.InnerText;
                }
            }

            if (node.SelectSingleNode("attributes") != null)
            {
                foreach (XmlNode innerNode in node.SelectSingleNode("attributes").ChildNodes)
                {
                    OrganAttribute attribute = new OrganAttribute();
                    attribute.attribute = GameData.getData().attributeList[innerNode.InnerText];
                    if (innerNode.Attributes["value"] != null) attribute.value = int.Parse(innerNode.Attributes["value"].Value);

                    attributes.Add(attribute);
                }
            }

            if (node.SelectSingleNode("flags") != null)
            {
                foreach (XmlNode innerNode in node.SelectSingleNode("flags").ChildNodes)
                {
                    flags |= (BodyPartDef.PartFlags)Enum.Parse(typeof(BodyPartDef.PartFlags), innerNode.InnerText);
                }
            }
        }
    }
}
