using System;
using System.Collections.Generic;
using System.Xml;

namespace LCS.Engine.Data
{
    public class BodyPartDef : DataDef
    {
        public struct BodyPartOrgan
        {
            public OrganDef organ;
            public int count;
            public string prefix;
            public string names;
        }

        [Flags]
        public enum PartFlags
        {
            NONE = 0,
            VITAL = 1,
            SMALL = 2,
            VISION = 4,
            SMELL = 8,
            TASTE = 16,
            BREATH = 32,
            PARALYZE_QUAD = 64,
            PARALYZE_PARA = 128,
            GRASP = 256,
            WALK = 512,
            NO_DESTROY = 1024,
            LIMB = 2048,
            CORE = 4096,
            HEAD = 8192
        }

        public BodyPartDef()
        {
            organs = new Dictionary<string, BodyPartOrgan>();
        }
        
        public string name;
        public string sneakname;
        public BodyPartDef armorname;
        public int size;
        public int severAmount = 100;
        public Dictionary<string, BodyPartOrgan> organs;
        public PartFlags flags = 0;

        public override void parseData(XmlNode node)
        {
            name = node.SelectSingleNode("name").InnerText;
            size = int.Parse(node.SelectSingleNode("size").InnerText);
            severAmount = int.Parse(node.SelectSingleNode("severamount").InnerText);

            if (node.SelectSingleNode("armorname") != null)
                armorname = GameData.getData().bodyPartList[node.SelectSingleNode("armorname").InnerText];

            if (node.SelectSingleNode("sneakname") != null)
                sneakname = node.SelectSingleNode("sneakname").InnerText;
            else
                sneakname = name;

            if (node.SelectSingleNode("organs") != null)
            {
                foreach (XmlNode innerNode in node.SelectSingleNode("organs").ChildNodes)
                {
                    BodyPartOrgan organTag = new BodyPartOrgan();
                    organTag.organ = GameData.getData().organList[innerNode.InnerText];

                    if (innerNode.Attributes["count"] != null)
                        organTag.count = int.Parse(innerNode.Attributes["count"].Value);
                    else
                        organTag.count = 1;

                    if (innerNode.Attributes["prefix"] != null)
                        organTag.prefix = innerNode.Attributes["prefix"].Value;
                    else organTag.prefix = "";

                    if (innerNode.Attributes["names"] != null)
                        organTag.names = innerNode.Attributes["names"].Value;
                    else
                        organTag.names = "";

                    organs.Add(organTag.organ.type, organTag);
                }
            }

            if (node.SelectSingleNode("flags") != null)
            {
                foreach (XmlNode innerNode in node.SelectSingleNode("flags").ChildNodes)
                {
                    flags |= (PartFlags)Enum.Parse(typeof(PartFlags), innerNode.InnerText);
                }
            }
        }
    }
}
