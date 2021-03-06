using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;

using LCS.Engine.Components.Creature;
using LCS.Engine.Components.Item;
using LCS.Engine.Components.Location;
using LCS.Engine.Components.World;

namespace LCS.Engine
{
    public class Entity : IComparable<Entity>
    {
        private static long nextGuid = 0;

        public string type { get; set; }
        public string def { get; set; }
        public readonly long guid;

        public bool persistent { get; set; }

        private Dictionary<Type, Component> components;
        private XmlNode saveNode;

        public Entity(string type, string def)
        {
            guid = nextGuid;
            nextGuid++;

            components = new Dictionary<Type, Component>();
            this.type = type;
            this.def = def;
            persistent = false;
        }

        //This should only be used when loading data from the save file
        public Entity(string type, string def, long guid)
        {
            this.guid = guid;

            components = new Dictionary<Type, Component>();
            this.type = type;
            this.def = def;
            persistent = false;
        }

        //This should only be used when loading data from the save file
        public static void setNextGuid(long nextGuid)
        {
            Entity.nextGuid = nextGuid;
        }

        public void persist()
        {
            if (!MasterController.GetMC().PersistentEntityList.ContainsKey(guid))
            {
                persistent = true;
                MasterController.GetMC().PersistentEntityList.Add(guid, this);

                foreach(Component c in components.Values)
                {
                    c.persist();
                }
            }
        }

        public void depersist()
        {
            if (MasterController.GetMC().PersistentEntityList.ContainsKey(guid))
            {
                persistent = false;
                MasterController.GetMC().PersistentEntityList.Remove(guid);

                foreach (Component c in components.Values)
                {
                    c.depersist();
                }
            }

            if (saveNode != null)
            {
                saveNode.OwnerDocument.DocumentElement.RemoveChild(saveNode);
                saveNode = null;
            }
        }

        public bool hasComponent<T>() where T : Component
        {
            return components.ContainsKey(typeof(T));
        }

        public T getComponent<T>() where T : Component
        {
            if (components.ContainsKey(typeof(T)))
            {
                return (T) components[typeof(T)];
            } else
            {
                return null;
            }
        }

        public void save(XmlDocument doc)
        {
            if (saveNode == null)
            {
                saveNode = doc.CreateElement("Entity");
                XmlAttribute guidAtt = doc.CreateAttribute("guid");
                XmlAttribute typeAtt = doc.CreateAttribute("type");
                XmlAttribute defAtt = doc.CreateAttribute("def");
                saveNode.Attributes.Append(guidAtt);
                saveNode.Attributes.Append(typeAtt);
                saveNode.Attributes.Append(defAtt);
                doc.DocumentElement.AppendChild(saveNode);
            }
            
            saveNode.Attributes["guid"].Value = "" + guid;
            saveNode.Attributes["type"].Value = type;
            saveNode.Attributes["def"].Value = def;

            foreach(Component c in components.Values)
            {
                long startTime = DateTime.Now.Ticks;
                c.save(saveNode);
            }
        }

        public void load(XmlNode node, Dictionary<long, Entity> entityList)
        {
            foreach(XmlNode componentNode in node.ChildNodes)
            {
                Component c = null;

                //HACK: There has to be a better way to do this than just naming all the classes
                switch (componentNode.Name)
                {
                    case "Age":
                        c = new Age();
                        break;
                    case "Body":
                        c = new Body();
                        break;
                    case "CreatureBase":
                        c = new CreatureBase();
                        break;
                    case "CreatureInfo":
                        c = new CreatureInfo();
                        break;
                    case "CriminalRecord":
                        c = new CriminalRecord();
                        break;
                    case "Dating":
                        c = new Dating();
                        break;
                    case "Hostage":
                        c = new Hostage();
                        break;
                    case "Inventory":
                        c = new Inventory();
                        break;
                    case "Liberal":
                        c = new Liberal();
                        break;
                    case "Politician":
                        c = new Politician();
                        break;
                    case "Recruit":
                        c = new Recruit();
                        break;
                    case "Armor":
                        c = new Armor();
                        break;
                    case "Clip":
                        c = new Clip();
                        break;
                    case "Loot":
                        c = new Loot();
                        break;
                    case "Weapon":
                        c = new Weapon();
                        break;
                    case "SafeHouse":
                        c = new SafeHouse();
                        break;
                    case "Shop":
                        c = new Shop();
                        break;
                    case "SiteBase":
                        c = new SiteBase();
                        break;
                    case "TroubleSpot":
                        c = new TroubleSpot();
                        break;
                    case "City":
                        c = new City();
                        break;
                    case "Government":
                        c = new Government();
                        break;
                    case "LiberalCrimeSquad":
                        c = new LiberalCrimeSquad();
                        break;
                    case "News":
                        c = new News();
                        break;
                    case "Public":
                        c = new Public();
                        break;
                    case "Portrait":
                        c = new Portrait();
                        break;
                    case "ItemBase":
                        c = new ItemBase();
                        break;
                    case "HighScore":
                        c = new HighScore();
                        break;
                    case "Vehicle":
                        c = new Vehicle();
                        break;
                    case "ConservativeCrimeSquad":
                        c = new ConservativeCrimeSquad();
                        break;
                    case "Nation":
                        c = new Nation();
                        break;
                }
                
                setComponent(c);
                c.load(componentNode, entityList);
            }
        }

        public void setComponent(Component component)
        {
            if (components.ContainsKey(component.GetType()))
            {
                components[component.GetType()].owner = null;
                components[component.GetType()].depersist();
                components[component.GetType()].unsubscribe();
            }
            component.owner = this;
            components[component.GetType()] = component;
            component.selfSubscribe();
            if (persistent)
                component.persist();
        }

        public void removeComponent(Type component)
        {
            if(components.ContainsKey(component))
            {
                components[component].depersist();
                components[component].unsubscribe();
                components[component].owner = null;
                components.Remove(component);
            }
        }

        public int CompareTo(Entity e)
        {
            return def.CompareTo(e.def);
        }
    }
}
