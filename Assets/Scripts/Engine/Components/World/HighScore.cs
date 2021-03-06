using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace LCS.Engine.Components.World
{
    public class HighScore : Component
    {
        [SimpleSave]
        public int recruits, martyrs, kills, kidnappings, moneyTaxed, moneySpent, flagsBought, flagsBurned;

        public HighScore()
        {
            recruits = 0;
            martyrs = 0;
            kills = 0;
            kidnappings = 0;
            moneyTaxed = 0;
            moneySpent = 0;
            flagsBought = 0;
            flagsBurned = 0;
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("HighScore");
                entityNode.AppendChild(saveNode);
            }

            saveSimpleFields();
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);
        }
    }
}
