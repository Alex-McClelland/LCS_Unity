using System;
using System.Collections.Generic;
using System.Xml;

namespace LCS.Engine.Data
{
    public class ViewDef : DataDef
    {
        public class ProtestCrime
        {
            public string condition = "";
            public CrimeDef crime;
        }

        public ViewDef()
        {
            issueText = new List<ConditionalName>();
            liberalText = new List<ConditionalName>();
            conservativeText = new List<ConditionalName>();
        }
        
        public string name;
        public List<ConditionalName> issueText;
        public List<ConditionalName> liberalText;
        public List<ConditionalName> conservativeText;
        public string protestText = "";
        public string protestSingleText = "";
        public ProtestCrime protestCrime;
        public LawDef protestLaw;
        public string recruitProp = "";
        public string broadcastText = "";

        public override void parseData(XmlNode node)
        {
            name = node.SelectSingleNode("name").InnerText;

            foreach (XmlNode innernode in node.SelectNodes("issuetext"))
            {
                ConditionalName issuetext = new ConditionalName();

                issuetext.name = innernode.InnerText;
                if (innernode.Attributes["condition"] != null) issuetext.condition = innernode.Attributes["condition"].Value;
                else issuetext.condition = "";

                issueText.Add(issuetext);
            }

            foreach (XmlNode innernode in node.SelectNodes("liberaltext"))
            {
                ConditionalName liberaltext = new ConditionalName();

                liberaltext.name = innernode.InnerText;
                if (innernode.Attributes["condition"] != null) liberaltext.condition = innernode.Attributes["condition"].Value;
                else liberaltext.condition = "";

                liberalText.Add(liberaltext);
            }

            foreach (XmlNode innernode in node.SelectNodes("conservativetext"))
            {
                ConditionalName conservativetext = new ConditionalName();

                conservativetext.name = innernode.InnerText;
                if (innernode.Attributes["condition"] != null) conservativetext.condition = innernode.Attributes["condition"].Value;
                else conservativetext.condition = "";

                conservativeText.Add(conservativetext);
            }

            if (node.SelectSingleNode("protest") != null)
            {
                protestText = node.SelectSingleNode("protest/text").InnerText;
                if (node.SelectSingleNode("protest/singletext") != null)
                    protestSingleText = node.SelectSingleNode("protest/singletext").InnerText;
                else
                    protestSingleText = protestText;
                if (node.SelectSingleNode("protest/law") != null)
                    protestLaw = GameData.getData().lawList[node.SelectSingleNode("protest/law").InnerText];
                if (node.SelectSingleNode("protest/crime") != null)
                {
                    ProtestCrime crime = new ProtestCrime();

                    crime.crime = GameData.getData().crimeList[node.SelectSingleNode("protest/crime").InnerText];
                    if (node.SelectSingleNode("protest/crime").Attributes["condition"] != null)
                        crime.condition = node.SelectSingleNode("protest/crime").Attributes["condition"].Value;

                    protestCrime = crime;
                }
            }

            if (node.SelectSingleNode("recruitprop") != null)
            {
                recruitProp = node.SelectSingleNode("recruitprop").InnerText;
            }

            if (node.SelectSingleNode("broadcasttext") != null)
            {
                broadcastText = node.SelectSingleNode("broadcasttext").InnerText;
            }
        }
    }
}
