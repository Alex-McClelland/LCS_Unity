using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using LCS.Engine.Events;
using LCS.Engine.Components.World;

namespace LCS.Engine.Components.Location
{
    class SiteBase : Component
    {
        public Name currentName { get; set; }
        public Name standardName { get; set; }
        public Dictionary<string, Name> conditionalNames { get; set; }
        [SimpleSave]
        public bool hidden;
        [SimpleSave]
        public Entity city;

        public SiteBase()
        {
            conditionalNames = new Dictionary<string, Name>();
            hidden = false;
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("SiteBase");
                entityNode.AppendChild(saveNode);

                //This really only needs to be recorded once, so we can include it here
                foreach (string s in conditionalNames.Keys)
                {
                    XmlNode conditionalNameNode = saveNode.OwnerDocument.CreateElement("conditionalName");
                    saveNode.AppendChild(conditionalNameNode);

                    XmlNode conditionNode = conditionalNameNode.OwnerDocument.CreateElement("condition");
                    conditionNode.InnerText = s;
                    conditionalNameNode.AppendChild(conditionNode);

                    conditionalNames[s].save(conditionalNameNode, "Name");
                }
            }

            saveSimpleFields();

            currentName.save(saveNode, "currentName");
            standardName.save(saveNode, "standardName");
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);

            currentName = new Name(componentData.SelectSingleNode("currentName").Attributes["name"].Value, componentData.SelectSingleNode("currentName").Attributes["shortName"].Value);
            standardName = new Name(componentData.SelectSingleNode("standardName").Attributes["name"].Value, componentData.SelectSingleNode("standardName").Attributes["shortName"].Value);
            
            foreach(XmlNode node in componentData.SelectNodes("conditionalName"))
            {
                Name conditionalName = new Name(node.SelectSingleNode("Name").Attributes["name"].Value, node.SelectSingleNode("Name").Attributes["shortName"].Value);
                conditionalNames.Add(node.SelectSingleNode("condition").InnerText, conditionalName);
            }
        }

        public override void subscribe()
        {
            base.subscribe();
            MasterController.GetMC().nextDay += nameCheck;
            MasterController.GetMC().nextDay += hideCheck;
        }

        public override void unsubscribe()
        {
            base.unsubscribe();
            MasterController.GetMC().nextDay -= nameCheck;
            MasterController.GetMC().nextDay -= hideCheck;
        }

        public void hideCheck()
        {
            if (GameData.getData().locationList[owner.def].hide == "") return;

            if (MasterController.GetMC().testCondition(GameData.getData().locationList[owner.def].hide))
                hidden = true;
            else
                hidden = false;
        }

        private void hideCheck(object sender, EventArgs args)
        {
            if (GameData.getData().locationList[owner.def].hide == "") return;

            if (MasterController.GetMC().testCondition(GameData.getData().locationList[owner.def].hide))
                hidden = true;
            else
                hidden = false;
        }

        private void nameCheck(object sender, EventArgs args)
        {
            bool conditionTrue = false;

            //Check laws to see if locations should have their name changed
            foreach(string condition in conditionalNames.Keys)
            {
                if (MasterController.GetMC().testCondition(condition))
                {
                    currentName.name = conditionalNames[condition].name;
                    currentName.shortName = conditionalNames[condition].shortName;
                    conditionTrue = true;
                    break;
                }
            }

            if (!conditionTrue)
            {
                currentName.name = standardName.name;
                currentName.shortName = standardName.shortName;
            }
        }

        public class Name
        {
            public Name(string name, string shortName)
            {
                this.name = name;
                this.shortName = shortName;
            }

            public string name { get; set; }
            public string shortName { get; set; }

            private XmlNode saveNode;

            public void save(XmlNode node, string nodeName)
            {
                if (saveNode == null)
                {
                    saveNode = node.OwnerDocument.CreateElement(nodeName);
                    XmlAttribute nameAtt = saveNode.OwnerDocument.CreateAttribute("name");
                    XmlAttribute shortnameAtt = saveNode.OwnerDocument.CreateAttribute("shortName");
                    saveNode.Attributes.Append(nameAtt);
                    saveNode.Attributes.Append(shortnameAtt);
                    node.AppendChild(saveNode);
                }

                saveNode.Attributes["name"].Value = name;
                saveNode.Attributes["shortName"].Value = shortName;
            }
        }

        public string getCCSName()
        {
            if (GameData.getData().locationList[owner.def].ccsName != null)
                return GameData.getData().locationList[owner.def].ccsName;
            else
                return currentName.name;
        }

        public string getCurrentName(bool shortname = false)
        {
            string name = shortname ? currentName.shortName : currentName.name;
            return name + (MasterController.nation.cities.Count > 1 ? " (" + city.getComponent<City>().shortname + ")" : "");
        }

        //Events
        public event EventHandler<DropItem> dropItem;
        public void doDropItem(DropItem args)
        {
            if(dropItem != null)
                dropItem(this, args);
        }
    }
}
