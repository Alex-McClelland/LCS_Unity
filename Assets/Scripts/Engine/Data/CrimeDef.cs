using System;
using System.Collections.Generic;
using System.Xml;

namespace LCS.Engine.Data
{
    public class CrimeDef : DataDef
    {
        public class CrimeVariant
        {
            public string condition = "";
            public string name;
            public string courtName;
            public int severity;
            public CrimeDegree degree;
            public bool deathSentence = false;
            public int lifeSentence = 0;
            public string sentence = "0";
        }

        public enum CrimeDegree
        {
            TREASON,
            FELONY,
            MISDEMEANOR
        }

        public CrimeDef()
        {
            variants = new List<CrimeVariant>();
        }

        public List<CrimeVariant> variants;
        public string appearCondition = "";

        public override void parseData(XmlNode node)
        {
            if (node.Attributes["condition"] != null)
            {
                appearCondition = node.Attributes["condition"].Value;
            }

            if (node.SelectSingleNode("variant") == null)
            {
                CrimeVariant variant = new CrimeVariant();
                variant.name = node.SelectSingleNode("name").InnerText;
                if (node.SelectSingleNode("courtname") != null) variant.courtName = node.SelectSingleNode("courtname").InnerText;
                else variant.courtName = variant.name;
                variant.severity = int.Parse(node.SelectSingleNode("severity").InnerText);
                variant.degree = (CrimeDegree)Enum.Parse(typeof(CrimeDegree), node.SelectSingleNode("degree").InnerText);
                variant.sentence = node.SelectSingleNode("sentence").InnerText;
                if (node.SelectSingleNode("deathsentence") != null) variant.deathSentence = true;
                if (node.SelectSingleNode("lifesentence") != null) variant.lifeSentence = int.Parse(node.SelectSingleNode("lifesentence").InnerText);

                variants.Add(variant);
            }
            else
            {
                foreach (XmlNode innerNode in node.SelectNodes("variant"))
                {
                    CrimeVariant variant = new CrimeVariant();
                    variant.condition = innerNode.Attributes["condition"].Value;
                    variant.name = innerNode.SelectSingleNode("name").InnerText;
                    if (innerNode.SelectSingleNode("courtname") != null) variant.courtName = innerNode.SelectSingleNode("courtname").InnerText;
                    else variant.courtName = variant.name;
                    variant.severity = int.Parse(innerNode.SelectSingleNode("severity").InnerText);
                    variant.degree = (CrimeDegree)Enum.Parse(typeof(CrimeDegree), innerNode.SelectSingleNode("degree").InnerText);
                    variant.sentence = innerNode.SelectSingleNode("sentence").InnerText;
                    if (innerNode.SelectSingleNode("deathsentence") != null) variant.deathSentence = true;
                    if (innerNode.SelectSingleNode("lifesentence") != null) variant.lifeSentence = int.Parse(innerNode.SelectSingleNode("lifesentence").InnerText);

                    variants.Add(variant);
                }
            }
        }
    }
}
