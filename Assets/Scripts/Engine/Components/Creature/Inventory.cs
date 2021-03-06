using System.Collections.Generic;
using System.Linq;
using LCS.Engine.Events;
using LCS.Engine.Components.Item;
using LCS.Engine.Components.Location;
using LCS.Engine.Components.World;
using System;
using System.Xml;
using LCS.Engine.Data;

namespace LCS.Engine.Components.Creature
{
    public class Inventory : Component
    {
        [SimpleSave]
        public Entity weapon;
        [SimpleSave]
        public Entity armor;
        [SimpleSave]
        public Entity vehicle;
        public Queue<Entity> clips { get; set; }

        //Natural weapon and armor are what is used if nothing is equipped in the appropriate slots
        [SimpleSave]
        public Entity naturalWeapon;
        [SimpleSave]
        public Entity naturalArmor;

        public bool reloadedThisRound;
        public Entity tempVehicle;
        
        public Inventory()
        {
            clips = new Queue<Entity>();
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Inventory");
                entityNode.AppendChild(saveNode);
            }

            saveSimpleFields();

            //Since clips shuffle around a lot the only thing we can really do is dump and refresh them when saving
            if (saveNode.SelectSingleNode("clips") != null)
                saveNode.RemoveChild(saveNode.SelectSingleNode("clips"));

            if (clips.Count > 0)
            {
                XmlNode clipsNode = saveNode.OwnerDocument.CreateElement("clips");
                saveNode.AppendChild(clipsNode);
                foreach (Entity e in clips)
                {
                    XmlNode clipNode = saveNode.OwnerDocument.CreateElement("clip");
                    clipNode.InnerText = e.guid.ToString();
                    clipsNode.AppendChild(clipNode);
                }
            }
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);

            if(componentData.SelectSingleNode("clips") != null)
            {
                foreach(XmlNode node in componentData.SelectSingleNode("clips").ChildNodes)
                    clips.Enqueue(entityList[int.Parse(node.InnerText)]);
            }
        }

        public override void selfSubscribe()
        {
            base.selfSubscribe();
            getComponent<CreatureBase>().die += doDie;
        }

        public override void unsubscribe()
        {
            base.unsubscribe();
            getComponent<CreatureBase>().die -= doDie;
        }

        protected override void persistExtended()
        {
            if (naturalArmor != null) naturalArmor.persist();
            if (naturalWeapon != null) naturalWeapon.persist();
            if (armor != null) armor.persist();
            if (weapon != null) weapon.persist();
            
            foreach(Entity e in clips)
            {
                e.persist();
            }
        }

        protected override void depersistExtended()
        {
            naturalArmor.depersist();
            naturalWeapon.depersist();
            if (armor != null) armor.depersist();
            if (weapon != null) weapon.depersist();

            foreach (Entity e in clips)
            {
                e.depersist();
            }
        }

        public void doDie(object sender, Die args)
        {
            if (MasterController.GetMC().currentChaseScene == null)
            {
                dropWeapon();
                dropArmor();
                dropAllClips();                
                if (MasterController.GetMC().phase == MasterController.Phase.TROUBLE && !hasComponent<Liberal>())
                {
                    Position p = MasterController.GetMC().currentSiteModeScene.squadPosition;
                    int cash = MasterController.GetMC().LCSRandom(GameData.getData().creatureDefList[owner.def].money);
                    if (getComponent<CreatureBase>().Location != null)
                        getComponent<CreatureBase>().Location.getComponent<TroubleSpot>().map[p.z][p.x, p.y].getComponent<TileBase>().cash += cash;
                }
            }
            else
            {
                destroyWeapon();
                destroyArmor();
                destroyAllClips();
            }
            vehicle = null;
        }

        public Entity getWeapon()
        {
            if (weapon != null) return weapon;
            else return naturalWeapon;
        }

        public Entity getArmor()
        {
            if (armor != null) return armor;
            else return naturalArmor;
        }

        public int getArmorValue(Body.BodyPart hitLocation)
        {
            int armorValue = 0;

            if (armor != null)
            {
                if (((ItemDef.ArmorDef)GameData.getData().itemList[armor.def].components["armor"]).armor.ContainsKey(hitLocation.Type))
                {
                    armorValue += ((ItemDef.ArmorDef)GameData.getData().itemList[armor.def].components["armor"]).armor[hitLocation.Type];
                    armorValue -= armor.getComponent<Armor>().quality - 1;
                    if (armor.getComponent<Armor>().damaged) armorValue -= 1;
                }
                else if (hitLocation.getArmorName() != null && ((ItemDef.ArmorDef)GameData.getData().itemList[armor.def].components["armor"]).armor.ContainsKey(hitLocation.getArmorName().type))
                {
                    armorValue += ((ItemDef.ArmorDef)GameData.getData().itemList[armor.def].components["armor"]).armor[hitLocation.getArmorName().type];
                    armorValue -= armor.getComponent<Armor>().quality - 1;
                    if (armor.getComponent<Armor>().damaged) armorValue -= 1;
                }

            }
            if(((ItemDef.ArmorDef)GameData.getData().itemList[naturalArmor.def].components["armor"]).armor.ContainsKey(hitLocation.Type))
                armorValue += ((ItemDef.ArmorDef)GameData.getData().itemList[naturalArmor.def].components["armor"]).armor[hitLocation.Type];
            else if(hitLocation.getArmorName() != null && ((ItemDef.ArmorDef)GameData.getData().itemList[naturalArmor.def].components["armor"]).armor.ContainsKey(hitLocation.getArmorName().type))
                armorValue += ((ItemDef.ArmorDef)GameData.getData().itemList[naturalArmor.def].components["armor"]).armor[hitLocation.getArmorName().type];

            if (armorValue < 0) armorValue = 0;

            return armorValue;
        }

        public int getFireProtection()
        {
            int fireProtection = 0;

            if (armor != null && (armor.getComponent<Armor>().getFlags() & ItemDef.ArmorFlags.FIRE_PROTECTION) != 0)
            {
                if (armor.getComponent<Armor>().damaged) fireProtection = 2;
                else fireProtection = 4;
            }
            if ((naturalArmor.getComponent<Armor>().getFlags() & ItemDef.ArmorFlags.FIRE_PROTECTION) != 0)
            {
                fireProtection = 4;
            }

            return fireProtection;
        }

        public void damageArmor(string part, int damageAmount)
        {
            if (armor == null || (armor.getComponent<Armor>().getFlags() & ItemDef.ArmorFlags.NO_DAMAGE) != 0) return;

            if (armor.getComponent<Armor>().covers(part) &&
            MasterController.GetMC().LCSRandom(armor.getComponent<Armor>().getDurability()) < damageAmount)
            {
                if (armor.getComponent<Armor>().damaged)
                {
                    armor.getComponent<Armor>().decreaseQuality(damageAmount);
                    if (armor.getComponent<Armor>().quality > armor.getComponent<Armor>().getQualityLevels())
                        destroyArmor();
                }
                else
                    armor.getComponent<Armor>().damaged = true;
            }
        }

        public int getProfessionalism()
        {
            if(armor != null)
                return armor.getComponent<Armor>().getProfessionalism();
            else
                return naturalArmor.getComponent<Armor>().getProfessionalism();
        }

        /*
        0 = invalid disugise, 1 = partial disguise, 2 = valid disguise, -1 = naked/bloody/damaged clothing (invalid even in non-restricted areas)
        */
        public int getDisguiseLevel()
        {
            int uniformLevel = 0;

            //Bloody or damaged clothing is never considered a valid disguise
            if (getArmor().getComponent<Armor>().bloody || getArmor().getComponent<Armor>().damaged) return -1;

            if (getArmor().def == "ARMOR_NONE") uniformLevel = -1;
            //A police uniform always conveys some level of trust, even if it's not a valid disguise for the location.
            if ((getArmor().getComponent<Armor>().getFlags() & ItemDef.ArmorFlags.POLICE) != 0) uniformLevel = 1;
            if ((getArmor().getComponent<Armor>().getFlags() & ItemDef.ArmorFlags.DEATHSQUAD) != 0 &&
                (MasterController.government.laws[Constants.LAW_DEATH_PENALTY].alignment == Alignment.ARCHCONSERVATIVE &&
                 MasterController.government.laws[Constants.LAW_POLICE].alignment == Alignment.ARCHCONSERVATIVE)) uniformLevel = 1;
            
            //SWAT and firefighter uniforms will work as disguises if the site has called for them
            if (getComponent<CreatureBase>().Location.getComponent<TroubleSpot>().highSecurity > 0 && 
                (getArmor().getComponent<Armor>().getFlags() & ItemDef.ArmorFlags.SWAT) != 0) uniformLevel = 2;
            if (getComponent<CreatureBase>().Location.getComponent<TroubleSpot>().fireAlarmTriggered &&
                (getArmor().getComponent<Armor>().getFlags() & ItemDef.ArmorFlags.FIRE_PROTECTION) != 0) uniformLevel = 2;

            foreach(LocationDef.DisguiseDef disguise in getComponent<CreatureBase>().Location.getComponent<TroubleSpot>().getPartialDisguises())
            {
                if (!MasterController.GetMC().testCondition(disguise.conditions, getComponent<CreatureBase>().Location.getComponent<TroubleSpot>())) continue;

                if(getArmor().def == disguise.itemType.type)
                {
                    if(uniformLevel < 1)
                        uniformLevel = 1;
                    break;
                }
            }

            foreach (LocationDef.DisguiseDef disguise in getComponent<CreatureBase>().Location.getComponent<TroubleSpot>().getValidDisguises())
            {
                if (!MasterController.GetMC().testCondition(disguise.conditions, getComponent<CreatureBase>().Location.getComponent<TroubleSpot>())) continue;

                if (getArmor().def == disguise.itemType.type)
                {
                    if(uniformLevel < 2)
                        uniformLevel = 2;
                    break;
                }
            }

            //Poor quality clothing will make for a poor disguise
            if ((getArmor().getComponent<Armor>().quality - 1) * 2 > getArmor().getComponent<Armor>().getQualityLevels() && uniformLevel > 0)
                uniformLevel--;

            return uniformLevel;
        }

        public int checkWeaponDisguise()
        {
            int returnValue = 0;

            if ((getWeapon().getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.NOT_SUSPICIOUS) == 0
                && getWeapon().getComponent<Weapon>().getSize() > getArmor().getComponent<Armor>().getConcealmentSize())
            {
                if (getArmor().getComponent<Armor>().getAppropriateWeapons().Contains(getWeapon().def))
                {
                    returnValue = 1;
                }
                else
                {
                    returnValue = 0;
                }
            }
            else
            {
                returnValue = 2;
            }

            return returnValue;
        }

        public bool hasInstrument()
        {
            bool hasInstrument = false;
            if (weapon != null)
                hasInstrument = weapon.getComponent<Weapon>().getAttack().damage_type == AttackDef.DamageType.MUSIC;
            //Maybe some creature has some kind of natural musical organ? I don't know.
            return hasInstrument || naturalWeapon.getComponent<Weapon>().getAttack().damage_type == AttackDef.DamageType.MUSIC;
        }

        public bool canGraffiti()
        {
            bool canGraffiti = false;

            if (weapon != null) canGraffiti = (weapon.getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.GRAFFITI) != 0;
            return canGraffiti || (naturalWeapon.getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.GRAFFITI) != 0;
        }

        public bool canCrowbar()
        {
            bool canCrowbar = false;
            if (weapon != null) canCrowbar = (weapon.getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.BREAK_LOCK) != 0;
            return canCrowbar || (naturalWeapon.getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.BREAK_LOCK) != 0;
        }

        public bool isWeaponThreatening()
        {
            if (weapon != null) return (weapon.getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.THREATENING) != 0;
            else return false;
        }

        public string getAmmoType()
        {
            if (weapon != null)
            {
                return ((ItemDef.WeaponDef)GameData.getData().itemList[weapon.def].components["weapon"]).attack[0].ammotype;
            }

            return null;
        }

        public bool equipWeapon(Entity weapon)
        {
            if (weapon == null || !weapon.hasComponent<Weapon>())
            {
                MasterController.GetMC().addErrorMessage("Invalid weapon: " + weapon);
                return false;
            }

            bool dropClips = false;

            if(clips.Count > 0)
                dropClips = weapon.getComponent<Weapon>().getAmmoType() != clips.Peek().getComponent<Clip>().getAmmoType();

            dropWeapon(dropClips);
            this.weapon = weapon;

            if (owner.persistent)
                weapon.persist();

            weapon.getComponent<ItemBase>().Location = owner;

            return true;
        }

        public bool equipArmor(Entity armor)
        {
            if (armor == null || !armor.hasComponent<Armor>())
            {
                MasterController.GetMC().addErrorMessage("Invalid armor: " + armor);
                return false;
            }

            dropArmor();
            this.armor = armor;
            armor.getComponent<ItemBase>().moveItem(owner);

            if (owner.persistent) armor.persist();

            return true;
        }

        public bool equipClip(Entity clip)
        {
            if(clip == null || !clip.hasComponent<Clip>())
            {
                MasterController.GetMC().addErrorMessage("Invalid clip: " + clip);
                return false;
            }

            if (clips.Count >= 9) return false;

            clips.Enqueue(clip);
            if (owner.persistent) clip.persist();
            clip.getComponent<ItemBase>().Location = owner;

            return true;
        }

        public bool equipVehicle(Entity newVehicle, bool preferredDriver = false)
        {
            if (vehicle != null &&
                vehicle.getComponent<Vehicle>().preferredDriver == owner)
                vehicle.getComponent<Vehicle>().preferredDriver = null;

            vehicle = newVehicle;

            if(preferredDriver)
                newVehicle.getComponent<Vehicle>().preferredDriver = owner;

            return true;
        }

        public void destroyWeapon(bool destroyClips = true)
        {
            if (weapon == null) return;
            weapon.getComponent<ItemBase>().destroyItem();
            weapon = null;

            if (destroyClips)
                destroyAllClips();
        }

        public void dropWeapon(bool dropClips = true)
        {
            if (weapon == null) return;
            weapon.depersist();

            if (MasterController.GetMC().phase == MasterController.Phase.TROUBLE &&
                hasComponent<Liberal>() &&
                getComponent<Body>().Alive &&
                getComponent<Liberal>().squad != null)
            {
                getComponent<Liberal>().squad.inventory.Add(weapon);
            }
            else if (getComponent<CreatureBase>().Location != null && MasterController.GetMC().currentChaseScene == null)
            {
                getComponent<CreatureBase>().Location.getComponent<SiteBase>().doDropItem(new DropItem(weapon, owner));
            }
            weapon = null;

            if (dropClips)
                dropAllClips();
        }

        public void destroyArmor()
        {
            if (armor == null) return;
            armor.getComponent<ItemBase>().destroyItem();
            armor = null;
        }

        public void dropArmor()
        {
            if (armor == null) return;
            armor.depersist();

            if (MasterController.GetMC().phase == MasterController.Phase.TROUBLE &&
                hasComponent<Liberal>() &&
                getComponent<Body>().Alive &&
                getComponent<Liberal>().squad != null)
            {
                getComponent<Liberal>().squad.inventory.Add(armor);
            }
            else if (getComponent<CreatureBase>().Location != null && MasterController.GetMC().currentChaseScene == null)
            {
                getComponent<CreatureBase>().Location.getComponent<SiteBase>().doDropItem(new DropItem(armor, owner));
            }
            armor = null;
        }

        public void destroyClip()
        {
            if (clips.Count == 0) return;
            Entity clip = clips.Dequeue();
            clip.getComponent<ItemBase>().destroyItem();
        }

        public void dropClip()
        {
            if (clips.Count == 0) return;
            Entity clip = clips.Dequeue();
            clip.depersist();

            if (MasterController.GetMC().phase == MasterController.Phase.TROUBLE &&
                hasComponent<Liberal>() &&
                getComponent<Body>().Alive &&
                getComponent<Liberal>().squad != null)
            {
                getComponent<Liberal>().squad.inventory.Add(clip);
            }
            else if (getComponent<CreatureBase>().Location != null && MasterController.GetMC().currentChaseScene == null)
            {
                getComponent<CreatureBase>().Location.getComponent<SiteBase>().doDropItem(new DropItem(clip, owner));
            }
        }

        public void destroyAllClips()
        {
            while (clips.Count > 0)
            {
                destroyClip();
            }
        }

        public void dropAllClips()
        {
            while(clips.Count > 0)
            {
                dropClip();
            }
        }

        public bool reload(bool combatReload)
        {
            if (clips.Count > 0)
            {
                if (weapon != null)
                {
                    Entity clip = weapon.getComponent<Weapon>().clip;

                    weapon.getComponent<Weapon>().clip = clips.Dequeue();

                    if (clip != null)
                    {
                        if (clip.getComponent<Clip>().ammo > 0)
                        {
                            clips.Enqueue(clip);
                        }
                        else
                        {
                            clip.depersist();
                        }
                    }
                }
                else if(clips.Peek().hasComponent<Weapon>() && (clips.Peek().getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.THROWN) != 0)
                {
                    weapon = clips.Dequeue();
                    weapon.getComponent<Weapon>().clip = weapon;
                }
                else
                {
                    return false;
                }

                if(combatReload) reloadedThisRound = true;

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
