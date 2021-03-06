using System;
using System.Collections.Generic;
using System.Xml;
using LCS.Engine.Data;
using LCS.Engine.Events;

namespace LCS.Engine.Components.Item
{
    public class Armor : Component
    {
        public Armor(int quality = 1)
        {
            bloody = false;
            damaged = false;
            this.quality = quality;
        }

        [SimpleSave]
        public int quality;
        [SimpleSave]
        public bool bloody;
        [SimpleSave]
        public bool damaged;

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Armor");
                entityNode.AppendChild(saveNode);
            }

            saveSimpleFields();
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);
        }

        public void decreaseQuality(int damageAmount)
        {
            if (MasterController.GetMC().LCSRandom(getDurability()) < MasterController.GetMC().LCSRandom(damageAmount) / quality)
                quality++;
        }

       public void makeBloody()
        {
            if ((getFlags() & ItemDef.ArmorFlags.NO_BLOODY) == 0) bloody = true;
        }

        public ItemDef.ArmorFlags getFlags()
        { return ((ItemDef.ArmorDef)GameData.getData().itemList[owner.def].components["armor"]).flags; }

        public bool covers(string part)
        { return ((ItemDef.ArmorDef)GameData.getData().itemList[owner.def].components["armor"]).bodyCovering.Contains(GameData.getData().bodyPartList[part]); }

        public int getDurability()
        { return ((ItemDef.ArmorDef)GameData.getData().itemList[owner.def].components["armor"]).durability; }

        public int getQualityLevels()
        { return ((ItemDef.ArmorDef)GameData.getData().itemList[owner.def].components["armor"]).qualitylevels; }

        public int getProfessionalism()
        { return ((ItemDef.ArmorDef)GameData.getData().itemList[owner.def].components["armor"]).professionalism; }

        public int getConcealmentSize()
        { return ((ItemDef.ArmorDef)GameData.getData().itemList[owner.def].components["armor"]).conceal_weapon_size; }

        public List<string> getAppropriateWeapons()
        { return ((ItemDef.ArmorDef)GameData.getData().itemList[owner.def].components["armor"]).appropriateWeapons; }

        public int getStealthValue()
        { return ((ItemDef.ArmorDef)GameData.getData().itemList[owner.def].components["armor"]).stealth_value; }

        public int getInterrogationBasePower()
        { return ((ItemDef.ArmorDef)GameData.getData().itemList[owner.def].components["armor"]).basePower; }

        public int getInterrogationAssaultBonus()
        { return ((ItemDef.ArmorDef)GameData.getData().itemList[owner.def].components["armor"]).assaultBonus; }

        public int getInterrogationDrugBonus()
        { return ((ItemDef.ArmorDef)GameData.getData().itemList[owner.def].components["armor"]).drugBonus; }

        public int getMakeDifficulty()
        { return ((ItemDef.ArmorDef)GameData.getData().itemList[owner.def].components["armor"]).make_difficulty; }
    }
}
