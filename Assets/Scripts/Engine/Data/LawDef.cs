using System;
using System.Collections.Generic;
using System.Xml;

namespace LCS.Engine.Data
{
    public class LawDef : DataDef
    {
        [Flags]
        public enum LawFlag
        {
            NONE = 0,
            CORPORATE = 1
        }

        public LawDef()
        {
            description = new Dictionary<Alignment, string>();
            electionText = new Dictionary<Alignment, string>();
            issueText = new Dictionary<string, string>();
            views = new Dictionary<ViewDef, int>();
        }
        
        public string name;
        public Dictionary<ViewDef, int> views;
        public Alignment supremeCourtBias = Alignment.MODERATE;
        public Dictionary<Alignment, string> description;
        public Dictionary<Alignment, string> electionText;
        public Dictionary<string, string> issueText;
        public LawFlag flags = 0;

        public override void parseData(XmlNode node)
        {
            name = node.SelectSingleNode("name").InnerText;
            if (node.SelectSingleNode("supremecourtbias") != null) supremeCourtBias = (Alignment)Enum.Parse(typeof(Alignment), node.SelectSingleNode("supremecourtbias").InnerText);
            foreach (XmlNode innerNode in node.SelectNodes("view"))
            {
                if (innerNode.Attributes["weight"] != null)
                    views.Add(GameData.getData().viewList[innerNode.InnerText], int.Parse(innerNode.Attributes["weight"].Value));
                else
                    views.Add(GameData.getData().viewList[innerNode.InnerText], 1);
            }

            foreach (XmlNode innerNode in node.SelectSingleNode("descriptions").ChildNodes)
            {
                switch (innerNode.Name)
                {
                    case "archconservative":
                        description.Add(Alignment.ARCHCONSERVATIVE, innerNode.InnerText);
                        break;
                    case "conservative":
                        description.Add(Alignment.CONSERVATIVE, innerNode.InnerText);
                        break;
                    case "moderate":
                        description.Add(Alignment.MODERATE, innerNode.InnerText);
                        break;
                    case "liberal":
                        description.Add(Alignment.LIBERAL, innerNode.InnerText);
                        break;
                    case "eliteliberal":
                        description.Add(Alignment.ELITE_LIBERAL, innerNode.InnerText);
                        break;
                }
            }

            foreach (XmlNode innerNode in node.SelectSingleNode("electiontext").ChildNodes)
            {
                if (innerNode.Name == "liberal")
                    electionText.Add(Alignment.LIBERAL, innerNode.InnerText);
                else if (innerNode.Name == "conservative")
                    electionText.Add(Alignment.CONSERVATIVE, innerNode.InnerText);
            }

            foreach (XmlNode innerNode in node.SelectSingleNode("issuestalk").ChildNodes)
            {
                issueText.Add(innerNode.Name, innerNode.InnerText);
            }

            if (node.SelectSingleNode("flags") != null)
            {
                foreach (XmlNode innerNode in node.SelectSingleNode("flags").ChildNodes)
                {
                    flags |= (LawFlag)Enum.Parse(typeof(LawFlag), innerNode.InnerText);
                }
            }
        }
    }
}
