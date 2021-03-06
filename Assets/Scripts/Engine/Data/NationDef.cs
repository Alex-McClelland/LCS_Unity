using System;
using System.Collections.Generic;
using System.Xml;

namespace LCS.Engine.Data
{
    public class NationDef : DataDef
    {
        public class NationCity
        {
            public NationCity()
            {
                districts = new List<NationDistrict>();
            }

            public string name;
            public string shortname;
            public string id;
            public List<NationDistrict> districts;
        }

        public class NationDistrict
        {
            public NationDistrict()
            {
                sites = new Dictionary<string, LocationDef>();
            }

            public string name;
            public Dictionary<string, LocationDef> sites;
            public DistrictFlag flags = 0;
        }

        [Flags]
        public enum DistrictFlag
        {
            NONE = 0,
            NEED_VEHICLE = 1
        }

        public class StateDef
        {
            public string name;
            public string shortname;
            public int congress;
            public int electoralVotes;
            public int alignment;
            public int population;
            public int voteOrder;
            public stateFlags flags = 0;
        }

        public enum stateFlags
        {
            NONE = 0,
            NONSTATE = 1,
            PROPORTIONAL_ELECTORAL_VOTES = 2
        }

        public NationDef()
        {
            parties = new Dictionary<Alignment, string>();
            states = new List<StateDef>();
            cities = new Dictionary<string, NationCity>();
        }
        
        public string name;
        public string shortname;
        public string adjective;
        public string capital;
        public int supremeCourtSeats;
        public Dictionary<string, NationCity> cities;
        public Dictionary<Alignment, string> parties;
        public List<StateDef> states;

        public override void parseData(XmlNode node)
        {
            name = node.SelectSingleNode("name").InnerText;
            shortname = node.SelectSingleNode("shortname").InnerText;
            adjective = node.SelectSingleNode("adjective").InnerText;
            capital = node.SelectSingleNode("capital").InnerText;
            supremeCourtSeats = int.Parse(node.SelectSingleNode("supremecourtseats").InnerText);
            foreach (XmlNode innerNode in node.SelectSingleNode("parties").ChildNodes)
            {
                parties.Add((Alignment)Enum.Parse(typeof(Alignment), innerNode.Name), innerNode.InnerText);
            }

            foreach (XmlNode innerNode in node.SelectSingleNode("states").ChildNodes)
            {
                StateDef state = new StateDef();
                state.name = innerNode.SelectSingleNode("name").InnerText;
                state.shortname = innerNode.SelectSingleNode("shortname").InnerText;
                state.electoralVotes = int.Parse(innerNode.SelectSingleNode("electoralvotes").InnerText);
                state.alignment = int.Parse(innerNode.SelectSingleNode("alignment").InnerText);
                state.population = int.Parse(innerNode.SelectSingleNode("population").InnerText);
                state.voteOrder = int.Parse(innerNode.SelectSingleNode("voteorder").InnerText);
                if (innerNode.SelectSingleNode("congress") != null) state.congress = int.Parse(innerNode.SelectSingleNode("congress").InnerText);
                else state.congress = 0;

                if (innerNode.SelectSingleNode("flags") != null)
                {
                    foreach (XmlNode tag in innerNode.SelectSingleNode("flags").ChildNodes)
                    {
                        state.flags |= (stateFlags)Enum.Parse(typeof(stateFlags), tag.InnerText);
                    }
                }

                states.Add(state);
            }

            foreach (XmlNode cityNode in node.SelectSingleNode("locations").ChildNodes)
            {
                NationCity city = new NationCity();
                city.name = cityNode.SelectSingleNode("name").InnerText;
                if (cityNode.SelectSingleNode("shortname") != null)
                    city.shortname = cityNode.SelectSingleNode("shortname").InnerText;
                else
                    city.shortname = city.name;
                city.id = cityNode.Attributes["idname"].Value;

                foreach (XmlNode innerNode in cityNode.SelectNodes("district"))
                {
                    NationDistrict district = new NationDistrict();

                    district.name = innerNode.SelectSingleNode("name").InnerText;
                    foreach (XmlNode siteNode in innerNode.SelectSingleNode("sites").ChildNodes)
                    {
                        try
                        {
                            district.sites.Add(siteNode.InnerText, GameData.getData().locationList[siteNode.InnerText]);
                        }
                        catch (KeyNotFoundException)
                        {
                            MasterController.GetMC().addErrorMessage("No def found for location: " + siteNode.InnerText);
                        }
                    }

                    if (innerNode.SelectSingleNode("flags") != null)
                    {
                        foreach (XmlNode flagNode in innerNode.SelectSingleNode("flags").ChildNodes)
                        {
                            district.flags |= (DistrictFlag)Enum.Parse(typeof(DistrictFlag), flagNode.InnerText);
                        }
                    }

                    city.districts.Add(district);
                }

                cities.Add(city.id, city);
            }
        }
    }
}
