using System;
using LCS.Engine.Components.Item;
using LCS.Engine.Data;

namespace LCS.Engine.Factories
{
    public static class ItemFactory
    {
        public static Entity create(string idname)
        {
            GameData dl = GameData.getData();

            if (!dl.itemList.ContainsKey(idname))
            {
                MasterController.GetMC().addErrorMessage("No item def found for " + idname);
                return null;
            }

            //basic setup
            Entity item = new Entity("item", idname);
            ItemBase itemBase = new ItemBase();
            item.setComponent(itemBase);

            //Add components
            foreach(ItemDef.ItemComponent component in dl.itemList[idname].components.Values)
            {
                if(component.GetType() == typeof(ItemDef.WeaponDef))
                {
                    Weapon weapon = new Weapon();
                    item.setComponent(weapon);
                    if ((weapon.getFlags() & ItemDef.WeaponFlags.THROWN) != 0) weapon.clip = item;
                }
                else if (component.GetType() == typeof(ItemDef.ArmorDef))
                {
                    Armor armor = new Armor();
                    item.setComponent(armor);
                }
                else if (component.GetType() == typeof(ItemDef.LootDef))
                {
                    Loot loot = new Loot();
                    item.setComponent(loot);
                }
                else if (component.GetType() == typeof(ItemDef.ClipDef))
                {
                    Clip clip = new Clip();
                    clip.ammo = ((ItemDef.ClipDef) component).ammo;
                    item.setComponent(clip);
                }
                else if(component.GetType() == typeof(ItemDef.VehicleDef))
                {
                    ItemDef.VehicleDef vc = (ItemDef.VehicleDef)component;

                    int year;

                    if(vc.startYear == 0)
                        year = MasterController.GetMC().currentDate.Year + MasterController.GetMC().LCSRandom(vc.addRandom) + 1;
                    else
                        year = vc.startYear + MasterController.GetMC().LCSRandom(vc.addRandom);

                    ItemDef.VehicleColor color = vc.colors[MasterController.GetMC().LCSRandom(vc.colors.Count)];

                    Vehicle vehicle = new Vehicle(year, color);
                    item.setComponent(vehicle);
                }
            }
            
            return item;
        }
    }
}
