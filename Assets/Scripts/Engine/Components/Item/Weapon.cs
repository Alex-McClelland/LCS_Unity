using System;
using System.Collections.Generic;
using System.Xml;
using LCS.Engine.Events;
using LCS.Engine.Data;

namespace LCS.Engine.Components.Item
{
    public class Weapon : Component
    {
        [SimpleSave]
        public Entity clip;

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Weapon");
                entityNode.AppendChild(saveNode);
            }

            saveSimpleFields();
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);
        } 

        public bool needsReload()
        {
            if (clip == null || clip.getComponent<Clip>().ammo == 0) return true;
            else return false;
        }

        public bool fullAmmo()
        {
            if (clip == null) return false;
            else if (clip.getComponent<Clip>().ammo == clip.getComponent<Clip>().getMaxAmmo()) return true;
            else return false;
        }

        public AttackDef getAttack(bool force_ranged = false, bool force_melee = false, bool no_reload = false)
        {
            ItemDef.WeaponDef weapon = (ItemDef.WeaponDef) GameData.getData().itemList[owner.def].components["weapon"];

            foreach(AttackDef attack in weapon.attack)
            {
                if ((attack.flags & AttackDef.AttackFlags.RANGED) != 0 && force_melee) continue;
                if ((attack.flags & AttackDef.AttackFlags.RANGED) == 0 && force_ranged) continue;
                if (attack.ammotype != "NONE" && needsReload() && no_reload) continue;

                return attack;
            }

            return null;
        }

        protected override void persistExtended()
        {
            if (clip != null) clip.persist();
        }

        protected override void depersistExtended()
        {
            if (clip != null) clip.depersist();
        }

        public int getBashStrengthMod()
        { return ((ItemDef.WeaponDef)GameData.getData().itemList[owner.def].components["weapon"]).bashStrengthMod; }

        public ItemDef.WeaponFlags getFlags()
        { return ((ItemDef.WeaponDef)GameData.getData().itemList[owner.def].components["weapon"]).flags; }

        public string getAmmoType()
        { return ((ItemDef.WeaponDef)GameData.getData().itemList[owner.def].components["weapon"]).attack[0].ammotype; }

        public int getSize()
        { return ((ItemDef.WeaponDef)GameData.getData().itemList[owner.def].components["weapon"]).size; }

        public ItemDef getDefaultClip()
        { return ((ItemDef.WeaponDef)GameData.getData().itemList[owner.def].components["weapon"]).defaultClip; }
    }
}
