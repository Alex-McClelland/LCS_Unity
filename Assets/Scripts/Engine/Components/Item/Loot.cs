using System;
using System.Xml;
using System.Collections.Generic;
using LCS.Engine.Events;
using LCS.Engine.Data;

namespace LCS.Engine.Components.Item
{
    public class Loot : Component
    {
        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Loot");
                entityNode.AppendChild(saveNode);
            }
        }

        public int getFenceValue()
        { return ((ItemDef.LootDef)GameData.getData().itemList[owner.def].components["loot"]).fenceValue; }

        public ItemDef.LootFlags getFlags()
        { return ((ItemDef.LootDef)GameData.getData().itemList[owner.def].components["loot"]).flags; }

        public List<ItemDef.LootEvidence> getEvidence()
        { return ((ItemDef.LootDef)GameData.getData().itemList[owner.def].components["loot"]).evidence; }
    }
}
