using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace LCS.Engine.Data
{
    public class NewsTypeDef : DataDef
    {
        [Flags]
        public enum NewsTypeFlag
        {
            NONE = 0,
            LCSPRIORITY = 1,
            SHIFT_PUBLIC_OPINION = 2,
            CCS_ACTION = 4,
            POLICE_KILLED = 8,
            SIEGE = 16,
            NEWSCHERRY = 32
        }
        
        public string headline = "";
        public string guardianHeadline = "";
        public string text = "";
        public string guardianText = "";
        public int priority = 0;
        public NewsTypeFlag flags = 0;

        public override void parseData(XmlNode node)
        {
            if (node.SelectSingleNode("priority") != null) priority = int.Parse(node.SelectSingleNode("priority").InnerText);
            if (node.SelectSingleNode("text") != null) text = node.SelectSingleNode("text").InnerText;
            if (node.SelectSingleNode("guardiantext") != null) guardianText = node.SelectSingleNode("guardiantext").InnerText;

            if (node.SelectSingleNode("headline") != null) headline = node.SelectSingleNode("headline").InnerText;
            if (node.SelectSingleNode("guardianheadline") != null) guardianHeadline = node.SelectSingleNode("guardianheadline").InnerText;

            foreach (XmlNode innerNode in node.SelectNodes("flag"))
            {
                flags |= (NewsTypeFlag)Enum.Parse(typeof(NewsTypeFlag), innerNode.InnerText);
            }
        }
    }
}
