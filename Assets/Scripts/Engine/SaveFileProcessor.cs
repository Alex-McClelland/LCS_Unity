using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace LCS.Engine
{
    //All "updateTo" methods will apply updates that will upgrade a save file from the version prior to the named version, to the named version.
    //If no "updateTo" method exists, then this update did not make any changes that would break save compatibility and doesn't need any extra
    //preprocessing.
    public class SaveFileProcessor
    {
        public static void updateTo_b11(XmlDocument doc)
        {
            XmlNode root = doc.DocumentElement;

            //Process all items and change the name of the "currentBase" node, which is no longer used, to "Location"
            foreach(XmlNode node in root.SelectNodes("Entity"))
            {
                if(node.SelectSingleNode("ItemBase") != null)
                {
                    XmlNode oldNode = node.SelectSingleNode("ItemBase").SelectSingleNode("currentBase");
                    XmlNode newNode = doc.CreateElement("Location");
                    newNode.InnerXml = oldNode.InnerXml;
                    node.SelectSingleNode("ItemBase").ReplaceChild(newNode, oldNode);
                }
            }

            foreach(XmlNode node in root.SelectNodes("Entity"))
            {
                string guid = node.Attributes["guid"].InnerText;

                //Process creature inventories and tell equipped items that they are located on that creature (previously this would have been null)
                if(node.SelectSingleNode("Inventory") != null)
                {
                    XmlNode inventoryNode = node.SelectSingleNode("Inventory");
                    if (inventoryNode.SelectSingleNode("weapon").InnerText != "null")
                    {
                        getEntityById(doc, inventoryNode.SelectSingleNode("weapon").InnerText).SelectSingleNode("ItemBase").SelectSingleNode("Location").InnerText = guid;
                    }
                    if (inventoryNode.SelectSingleNode("armor").InnerText != "null")
                    {
                        getEntityById(doc, inventoryNode.SelectSingleNode("armor").InnerText).SelectSingleNode("ItemBase").SelectSingleNode("Location").InnerText = guid;
                    }
                    getEntityById(doc, inventoryNode.SelectSingleNode("naturalWeapon").InnerText).SelectSingleNode("ItemBase").SelectSingleNode("Location").InnerText = guid;
                    getEntityById(doc, inventoryNode.SelectSingleNode("naturalArmor").InnerText).SelectSingleNode("ItemBase").SelectSingleNode("Location").InnerText = guid;
                }

                //Process all safehouse inventories since these are no longer used
                if(node.SelectSingleNode("SafeHouse") != null)
                {
                    foreach(XmlNode item in node.SelectSingleNode("SafeHouse").SelectSingleNode("inventory").ChildNodes)
                    {
                        //skip items that are bad data, that was caused to a bug that was fixed in this version
                        if (getEntityById(doc, item.InnerText) == null) continue;
                        getEntityById(doc, item.InnerText).SelectSingleNode("ItemBase").SelectSingleNode("Location").InnerText = guid;
                    }

                    //These should already be set, but just in case
                    foreach(XmlNode hostage in node.SelectSingleNode("SafeHouse").SelectSingleNode("hostages").ChildNodes)
                    {
                        getEntityById(doc, hostage.InnerText).SelectSingleNode("CreatureBase").SelectSingleNode("Location").InnerText = guid;
                    }
                    foreach (XmlNode body in node.SelectSingleNode("SafeHouse").SelectSingleNode("bodies").ChildNodes)
                    {
                        XmlNode bodyNode = getEntityById(doc, body.InnerText);
                        bodyNode.SelectSingleNode("CreatureBase").SelectSingleNode("Location").InnerText = guid;
                        //Remove the Hostage node if it has one, to avoid confusion with live hostages
                        if(bodyNode.SelectSingleNode("Hostage") != null)
                        {
                            bodyNode.RemoveChild(bodyNode.SelectSingleNode("Hostage"));
                        }                        
                    }

                    node.SelectSingleNode("SafeHouse").RemoveChild(node.SelectSingleNode("SafeHouse/inventory"));
                    node.SelectSingleNode("SafeHouse").RemoveChild(node.SelectSingleNode("SafeHouse/hostages"));
                    node.SelectSingleNode("SafeHouse").RemoveChild(node.SelectSingleNode("SafeHouse/bodies"));
                }
            }
        }

        private static XmlNode getEntityById(XmlDocument doc, string guid)
        {
            return doc.DocumentElement.SelectSingleNode("Entity[@guid=\"" + guid + "\"]");
        }
    }
}
