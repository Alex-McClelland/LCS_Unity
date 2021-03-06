using System;
using System.Collections.Generic;
using System.Xml;

namespace LCS.Engine.Data
{
    public class SkillDef : DataDef
    {
        [Flags]
        public enum SkillFlag
        {
            NONE = 0,
            TRAINED_ONLY = 1,
            SPECIALTY = 2,
            LEARN_FROM_RECRUITMENT = 4
        }
        
        public string name;
        public string category;
        public SkillFlag flags = 0;
        public AttributeDef associatedAttribute;

        public override void parseData(XmlNode node)
        {
            name = node.SelectSingleNode("name").InnerText;
            category = node.SelectSingleNode("category").InnerText;
            associatedAttribute = GameData.getData().attributeList[node.SelectSingleNode("associatedAttribute").InnerText];

            if (node.SelectSingleNode("flags") != null)
            {
                foreach (XmlNode innerNode in node.SelectSingleNode("flags").ChildNodes)
                {
                    flags |= (SkillFlag)Enum.Parse(typeof(SkillFlag), innerNode.InnerText);
                }
            }
        }
    }
}
