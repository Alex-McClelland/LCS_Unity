using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace LCS.Engine.Components.World
{
    public class Nation : Component
    {
        public Dictionary<string, Entity> cities;

        public Nation()
        {
            cities = new Dictionary<string, Entity>();
        }

        public override void save(XmlNode entityNode)
        {
            //Nothing changes past the initial setup here so only record once
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Nation");
                entityNode.AppendChild(saveNode);

                foreach (string s in cities.Keys)
                {
                    XmlNode cityNode = saveNode.OwnerDocument.CreateElement("city");
                    XmlAttribute idAtt = cityNode.OwnerDocument.CreateAttribute("idname");
                    idAtt.Value = s;
                    cityNode.Attributes.Append(idAtt);
                    saveNode.AppendChild(cityNode);

                    cityNode.InnerText = cities[s].guid.ToString();
                }
            }
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            foreach (XmlNode node in componentData.SelectNodes("city"))
            {
                cities.Add(node.Attributes["idname"].Value, entityList[int.Parse(node.InnerText)]);
            }
        }

        protected override void persistExtended()
        {
            foreach(Entity e in cities.Values)
                e.persist();
        }

        protected override void depersistExtended()
        {
            foreach (Entity e in cities.Values)
                e.depersist();
        }

        public List<Entity> getAllBases(bool ownedonly = false)
        {
            List<Entity> bases = new List<Entity>();

            foreach(Entity city in cities.Values)
            {
                bases.AddRange(city.getComponent<City>().getAllBases(ownedonly));
            }

            return bases;
        }
    }
}
