using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using LCS.Engine.Components.Location;
using LCS.Engine.Data;

namespace LCS.Engine.Components.World
{
    public class City : Component
    {
        [SimpleSave]
        public string name;
        [SimpleSave]
        public string shortname;
        [SimpleSave]
        public string idname;
        public Dictionary<string, List<Entity>> locations { get; set; }

        public City()
        {
            locations = new Dictionary<string, List<Entity>>();
        }

        public override void save(XmlNode entityNode)
        {
            //Nothing changes past the initial setup here so only record once
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("City");
                entityNode.AppendChild(saveNode);

                saveSimpleFields();

                foreach (string s in locations.Keys)
                {
                    XmlNode districtNode = saveNode.OwnerDocument.CreateElement("district");
                    XmlAttribute nameAtt = districtNode.OwnerDocument.CreateAttribute("name");
                    nameAtt.Value = s;
                    districtNode.Attributes.Append(nameAtt);
                    saveNode.AppendChild(districtNode);

                    foreach (Entity e in locations[s])
                    {
                        XmlNode locationNode = districtNode.OwnerDocument.CreateElement("location");
                        locationNode.InnerText = e.guid.ToString();
                        districtNode.AppendChild(locationNode);
                    }
                }
            }
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);
            foreach(XmlNode node in componentData.SelectNodes("district"))
            {
                locations.Add(node.Attributes["name"].Value, new List<Entity>());
                foreach(XmlNode innerNode in node.SelectNodes("location"))
                {
                    try
                    {
                        locations[node.Attributes["name"].Value].Add(entityList[int.Parse(innerNode.InnerText)]);
                    }
                    //Should never happen, locations are not added or removed during gameplay so refs should never become stale
                    catch (KeyNotFoundException)
                    {
                        MasterController.GetMC().addErrorMessage("Entity reference " + int.Parse(innerNode.InnerText) + " not found on object " + owner.def + ":" + componentData.ParentNode.Attributes["guid"].Value + ":" + componentData.Name + ":district:location");
                    }
                }
            }
        }

        protected override void persistExtended()
        {
            foreach(string district in locations.Keys)
            {
                foreach(Entity e in locations[district])
                {
                    e.persist();
                }
            }
        }

        protected override void depersistExtended()
        {
            foreach (string district in locations.Keys)
            {
                foreach (Entity e in locations[district])
                {
                    e.depersist();
                }
            }
        }

        public Entity getLocation(string def)
        {
            foreach(List<Entity> list in locations.Values)
            {
                foreach(Entity e in list)
                {
                    if(e.def == def)
                    {
                        return e;
                    }
                }
            }

            return null;
        }

        public List<Entity> getAllBases(bool ownedOnly = false)
        {
            List<Entity> baseList = new List<Entity>();

            foreach (List<Entity> list in locations.Values)
            {
                foreach (Entity e in list)
                {
                    if (e.hasComponent<SafeHouse>() && (!ownedOnly || e.getComponent<SafeHouse>().owned)) baseList.Add(e);
                }
            }

            return baseList;
        }

        public bool requiresVehicle(string location)
        {
            foreach (NationDef.NationDistrict district in GameData.getData().nationList["USA"].cities[idname].districts)
            {
                if ((district.flags & NationDef.DistrictFlag.NEED_VEHICLE) == 0)
                    continue;

                if (district.sites.ContainsKey(location))
                    return true;
            }

            return false;
        }
    }
}
