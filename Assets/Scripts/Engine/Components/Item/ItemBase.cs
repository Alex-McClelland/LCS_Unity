using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using LCS.Engine.Components.Location;
using LCS.Engine.Data;

namespace LCS.Engine.Components.Item
{
    public class ItemBase : Component
    {
        [SimpleSave]
        public Entity Location = null;
        [SimpleSave]
        public Entity targetBase = null;

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("ItemBase");
                entityNode.AppendChild(saveNode);
            }

            saveSimpleFields();
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);
        }

        public override void subscribe()
        {
            base.subscribe();
            MasterController.GetMC().nextDay += doDaily;
        }

        public override void unsubscribe()
        {
            base.unsubscribe();
            MasterController.GetMC().nextDay -= doDaily;
        }

        private void doDaily(object sender, EventArgs args)
        {
            if(targetBase != null)
            {
                moveItem(targetBase);
                targetBase = null;
            }
        }

        public void moveItem(Entity target)
        {
            Location = target;
        }

        public void destroyItem()
        {
            Location = null;
            owner.depersist();
        }

        public string getName(bool shortname = false)
        {
            string name = "";

            if (hasComponent<Vehicle>())
            {
                if (!shortname) name += (getComponent<Vehicle>().heat > 0?"Stolen ":"") + getColorName(getComponent<Vehicle>().color) + " ";
                name += (getComponent<Vehicle>().heat > 0 ? "(S)" : "") + getComponent<Vehicle>().year + " ";
            }

            if (MasterController.GetMC().isFuture() && GameData.getData().itemList[owner.def].shortnameFuture != "")
            {
                if (shortname) name += GameData.getData().itemList[owner.def].shortnameFuture;
                else name += GameData.getData().itemList[owner.def].nameFuture;
            }
            else
            {
                if (shortname) name += GameData.getData().itemList[owner.def].shortname;
                else name += GameData.getData().itemList[owner.def].name;
            }

            return name;
        }

        private string getColorName(ItemDef.VehicleColor color)
        {
            switch (color)
            {
                case ItemDef.VehicleColor.BLACK:
                    return "Black";
                case ItemDef.VehicleColor.BLUE:
                    return "Blue";
                case ItemDef.VehicleColor.GREEN:
                    return "Green";
                case ItemDef.VehicleColor.POLICE:
                    return "";
                case ItemDef.VehicleColor.RED:
                    return "Red";
                case ItemDef.VehicleColor.TAN:
                    return "Tan";
                case ItemDef.VehicleColor.WHITE:
                    return "White";
                case ItemDef.VehicleColor.YELLOW:
                    return "Yellow";
                default:
                    return "Unpainted";
            }
        }
    }
}
