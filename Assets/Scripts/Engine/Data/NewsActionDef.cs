using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace LCS.Engine.Data
{
    public class NewsActionDef : DataDef
    {
        [Flags]
        public enum NewsActionFlag
        {
            NONE = 0,
            MAJORCRIME = 1
        }
        
        public string text = "";
        public string guardiantext = "";
        public int priority = 0;
        public int cap = 0;
        public int violence = 0;
        public int politics = 0;
        public NewsActionFlag flags;

        public override void parseData(XmlNode node)
        {
            if (node.SelectSingleNode("priority") != null) priority = int.Parse(node.SelectSingleNode("priority").InnerText);
            if (node.SelectSingleNode("text") != null) text = node.SelectSingleNode("text").InnerText;
            if (node.SelectSingleNode("guardiantext") != null) guardiantext = node.SelectSingleNode("guardiantext").InnerText;
            if (node.SelectSingleNode("cap") != null) cap = int.Parse(node.SelectSingleNode("cap").InnerText);
            if (node.SelectSingleNode("violence") != null) violence = int.Parse(node.SelectSingleNode("violence").InnerText);
            if (node.SelectSingleNode("politics") != null) politics = int.Parse(node.SelectSingleNode("politics").InnerText);
            foreach (XmlNode flagNode in node.SelectNodes("flag"))
            {
                flags |= (NewsActionFlag)Enum.Parse(typeof(NewsActionFlag), flagNode.InnerText);
            }
        }
    }
}
