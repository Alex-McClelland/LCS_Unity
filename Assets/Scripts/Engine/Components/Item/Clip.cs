using System;
using System.Collections.Generic;
using System.Xml;
using LCS.Engine.Events;
using LCS.Engine.Data;

namespace LCS.Engine.Components.Item
{
    public class Clip : Component
    {
        [SimpleSave]
        public int ammo;

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Clip");
                entityNode.AppendChild(saveNode);
            }

            saveSimpleFields();
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);
        }

        public bool isFull()
        {
            return ammo == getMaxAmmo();
        }

        public int getMaxAmmo()
        { return ((ItemDef.ClipDef)GameData.getData().itemList[owner.def].components["clip"]).ammo; }

        public string getAmmoType()
        { return ((ItemDef.ClipDef)GameData.getData().itemList[owner.def].components["clip"]).ammoType; }
    }
}
