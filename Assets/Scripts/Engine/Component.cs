using System;
using System.Xml;
using System.Reflection;
using System.Collections.Generic;

namespace LCS.Engine
{
    public abstract class Component
    {
        public Entity owner { get; set; }
        //Do they listen to world events? (Only persistent entities should)
        protected bool globalSubscribed = false;
        //Do they listen to their own events? (Everyone should)
        protected bool selfSubscribed = false;

        protected XmlNode saveNode;

        public void persist()
        {
            if(!globalSubscribed) subscribe();
            persistExtended();
        }

        virtual protected void persistExtended() { }

        public void depersist()
        {
            if(globalSubscribed) unsubscribe();
            if (saveNode != null)
            {
                saveNode.ParentNode.RemoveChild(saveNode);
                saveNode = null;
            }

            if(saveNodeList != null) saveNodeList.Clear();
            depersistExtended();
        }

        virtual protected void depersistExtended() { }

        virtual public void selfSubscribe()
        {
            selfSubscribed = true;
        }

        virtual public void subscribe()
        {
            globalSubscribed = true;
            if(!selfSubscribed) selfSubscribe();
        }

        virtual public void unsubscribe()
        {
            globalSubscribed = false;
            selfSubscribed = false;
        }

        public virtual void save(XmlNode entityNode) { }
        public virtual void load(XmlNode componentData, Dictionary<long, Entity> entityList) { }

        private Dictionary<string, XmlNode> saveNodeList;
        protected void saveSimpleFields()
        {
            if (saveNodeList == null) saveNodeList = new Dictionary<string, XmlNode>();
            
            foreach(FieldInfo field in GetType().GetFields())
            {
                Attribute attr = Attribute.GetCustomAttribute(field, typeof(SimpleSave));
                if(attr != null)
                {
                    ((SimpleSave)attr).saveField(field, saveNode, this);
                }
            }
        }
        protected void loadSimpleFields(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            foreach (FieldInfo field in GetType().GetFields())
            {
                Attribute attr = Attribute.GetCustomAttribute(field, typeof(SimpleSave));
                if (attr != null)
                {
                    ((SimpleSave)attr).loadField(field, this, componentData, entityList);
                }
            }
        }
                
        protected void saveField(object field, string name, XmlNode node)
        {
            if (saveNodeList == null) saveNodeList = new Dictionary<string, XmlNode>();

            XmlNode newNode;

            if (!saveNodeList.ContainsKey(name))
            {
                newNode = node.OwnerDocument.CreateElement(name);
                node.AppendChild(newNode);
                saveNodeList.Add(name, newNode);
            }
            else
            {
                newNode = saveNodeList[name];
            }

            if (field != null)
            {
                if (field.GetType() != typeof(Entity))
                    newNode.InnerText = field.ToString();
                else
                    newNode.InnerText = ((Entity)field).guid.ToString();
            }
            else
            {
                newNode.InnerText = "null";
            }
        }

        //shortcut so components on the same object can speak to each other directly
        public T getComponent<T>() where T : Component
        {
            return owner.getComponent<T>();
        }

        public bool hasComponent<T>() where T : Component
        {
            return owner.hasComponent<T>();
        }

        public void removeMe()
        {
            depersist();
            owner.removeComponent(GetType());
        }

        [AttributeUsage(AttributeTargets.Field)]
        public class SimpleSave : Attribute
        {
            public void saveField(FieldInfo field, XmlNode node, Component component)
            {
                //Set invariant culture to avoid any problems with decimal places for floats
                System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

                XmlNode newNode;

                if (!component.saveNodeList.ContainsKey(field.Name))
                {
                    newNode = node.OwnerDocument.CreateElement(field.Name);
                    node.AppendChild(newNode);
                    component.saveNodeList.Add(field.Name, newNode);
                }
                else
                {
                    newNode = component.saveNodeList[field.Name];
                }

                if (field.GetValue(component) != null)
                {
                    object value = field.GetValue(component);
                    if (value.GetType() != typeof(Entity))
                        newNode.InnerText = value.ToString();
                    else
                        newNode.InnerText = ((Entity)value).guid.ToString();
                }
                else
                {
                    newNode.InnerText = "null";
                }
            }

            public void loadField(FieldInfo field, Component component, XmlNode componentData, Dictionary<long, Entity> entityList)
            {
                if (componentData.SelectSingleNode(field.Name) == null)
                {
                    MasterController.GetMC().addErrorMessage(field.Name + " was missing from component " + component.GetType() + " on entity " + component.owner.def + " and set to default. This may be fine or this may break the game.");
                    return;
                }

                if(componentData.SelectSingleNode(field.Name).InnerText != "null")
                {
                    if (field.FieldType == typeof(Entity))
                    {
                        try
                        {
                            field.SetValue(component, entityList[int.Parse(componentData.SelectSingleNode(field.Name).InnerText)]);
                        }
                        catch (KeyNotFoundException)
                        {
                            MasterController.GetMC().addErrorMessage("Entity reference " + int.Parse(componentData.SelectSingleNode(field.Name).InnerText) + " not found on object " + component.owner.def + ":" + componentData.ParentNode.Attributes["guid"].Value + ":" + componentData.Name + ":" + field.Name);
                        }
                    }
                    else
                    {
                        if (field.FieldType == typeof(string))
                            field.SetValue(component, componentData.SelectSingleNode(field.Name).InnerText);
                        else if (field.FieldType == typeof(int))
                            field.SetValue(component, int.Parse(componentData.SelectSingleNode(field.Name).InnerText));
                        else if (field.FieldType == typeof(float))
                            field.SetValue(component, float.Parse(componentData.SelectSingleNode(field.Name).InnerText));
                        else if (field.FieldType == typeof(double))
                            field.SetValue(component, double.Parse(componentData.SelectSingleNode(field.Name).InnerText));
                        else if (field.FieldType == typeof(bool))
                            field.SetValue(component, bool.Parse(componentData.SelectSingleNode(field.Name).InnerText));
                        else if (field.FieldType.IsEnum)
                            field.SetValue(component, Enum.Parse(field.FieldType, componentData.SelectSingleNode(field.Name).InnerText));
                    }
                }
                else
                {
                    field.SetValue(component, null);
                }
            }
        }
    }
}
