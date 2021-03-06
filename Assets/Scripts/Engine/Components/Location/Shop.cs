using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using LCS.Engine.Components.Item;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Creature;
using LCS.Engine.Data;

namespace LCS.Engine.Components.Location
{
    public class Shop : Component
    {
        public List<Entity> buyCart;
        public List<Entity> sellCart;
        public Dictionary<string, int> itemPrices;

        public LiberalCrimeSquad.Squad shoppingSquad;

        private bool siteShopping;

        public Shop()
        {
            buyCart = new List<Entity>();
            sellCart = new List<Entity>();
            itemPrices = new Dictionary<string, int>();
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Shop");
                entityNode.AppendChild(saveNode);
            }
        }

        public void startShopping(LiberalCrimeSquad.Squad squad, bool siteShopping)
        {
            itemPrices.Clear();
            shoppingSquad = squad;
            bool hasSleeper = false;
            this.siteShopping = siteShopping;
            foreach(Entity e in MasterController.lcs.getAllSleepers())
            {
                if(e.getComponent<CreatureBase>().Location == owner)
                {
                    hasSleeper = true;
                    break;
                }
            }

            foreach(LocationDef.ShopDepartmentDef department in getDepartments())
            {
                foreach(LocationDef.ShopItemDef item in department.items)
                {
                    int price = item.price;
                    if (item.item.components.ContainsKey("weapon") &&
                        ((ItemDef.WeaponDef)item.item.components["weapon"]).legality < (int)MasterController.government.laws[Constants.LAW_GUN_CONTROL].alignment)
                    {
                        price *= (int) Math.Pow(2, (int)(MasterController.government.laws[Constants.LAW_GUN_CONTROL].alignment - ((ItemDef.WeaponDef)item.item.components["weapon"]).legality));
                    }
                    else if (item.item.components.ContainsKey("clip") &&
                        ((ItemDef.ClipDef)item.item.components["clip"]).legality < (int)MasterController.government.laws[Constants.LAW_GUN_CONTROL].alignment)
                    {
                        price *= (int)Math.Pow(2, (int)(MasterController.government.laws[Constants.LAW_GUN_CONTROL].alignment - ((ItemDef.ClipDef)item.item.components["clip"]).legality));
                    }
                    if (hasSleeper) price = (int)(price * 0.8d);
                    itemPrices[item.item.type] = price;
                }
            }

            MasterController.GetMC().uiController.closeUI();
            MasterController.GetMC().uiController.shop.startShopping(owner, squad);
        }

        public void finishShopping()
        {
            buyCart.Clear();
            sellCart.Clear();
            if(!siteShopping)
                shoppingSquad.goHome();
            shoppingSquad = null;
            MasterController.GetMC().doNextAction();
        }

        public int getTotalBuyValue()
        {
            int value = 0;

            foreach(Entity item in buyCart)
            {
                value += itemPrices[item.def];
            }

            return value;
        }

        public int getTotalSellValue()
        {
            int value = 0;

            foreach(Entity item in sellCart)
            {
                if (item.hasComponent<Armor>())
                {
                    //Armor of lower quality is worth less
                    value += item.getComponent<Loot>().getFenceValue() / item.getComponent<Armor>().quality;
                }
                else
                {
                    value += item.getComponent<Loot>().getFenceValue();
                    //Stolen cars are worth SIGNIFICANTLY less
                    if (item.hasComponent<Vehicle>() && item.getComponent<Vehicle>().heat > 0)
                        value /= 10;
                }                
            }

            return value;
        }

        public void buy()
        {
            if (getTotalBuyValue() > MasterController.lcs.Money) return;

            MasterController.lcs.changeFunds(-getTotalBuyValue());
            shoppingSquad.inventory.AddRange(buyCart);
            buyCart.Clear();
        }

        public void sell()
        {
            MasterController.lcs.changeFunds(getTotalSellValue());
            foreach(Entity item in sellCart)
            {
                item.getComponent<ItemBase>().destroyItem();

                //Prevent a squad from using a vehicle later in the day if it's been sold
                if (item.hasComponent<Vehicle>()) item.getComponent<Vehicle>().used = true;
            }
            sellCart.Clear();
        }

        public LocationDef.ShopFlag getFlags()
        { return ((LocationDef.ShopDef)GameData.getData().locationList[owner.def].components["shop"]).flags; }

        public List<LocationDef.ShopDepartmentDef> getDepartments()
        { return ((LocationDef.ShopDef)GameData.getData().locationList[owner.def].components["shop"]).departments; }
    }
}
