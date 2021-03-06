using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace LCS.Engine.Data
{
    public class AttackDef : DataDef
    {
        [Flags]
        public enum AttackFlags
        {
            NONE = 0,
            RANGED = 2,
            ALWAYS_DESCRIBE_HIT = 4,
            BACKSTAB = 8,
            DAMAGE_ARMOR = 16,
            CAUSE_BLEED = 32,
            SKILL_DAMAGE = 64
        }

        public enum DamageType
        {
            BRUISE,
            TEAR,
            CUT,
            BURN,
            SHOOT,
            MUSIC,
            PERSUASION
        }
        
        public string ammotype = "NONE";
        public string attack_description = "assaults";
        public string sneak_attack_description;
        public string hit_description = "striking";
        public string hit_punctuation = ".";
        public SkillDef skill;
        public int accuracy_bonus = 0;
        public int number_attacks = 1;
        public int successive_attacks_difficulty = 0;
        public int strength_min = 5;
        public int strength_max = 10;
        public int random_damage = 1;
        public int fixed_damage = 1;
        public string severtype = "NONE";
        public int armorpiercing = 0;
        public int no_damage_reduction_for_limbs_chance = 0;
        public int criticalChance = 0;
        public int criticalHitsRequired = 1;
        public int criticalRandomDamage;
        public int criticalFixedDamage;
        public string criticalSeverType;
        public int fireChance = 0;
        public int fireChanceCauseDebris = 0;
        public AttackFlags flags = 0;
        public DamageType damage_type = DamageType.BRUISE;

        public override void parseData(XmlNode node)
        {
            foreach (XmlNode innerNode in node.ChildNodes)
            {
                switch (innerNode.Name)
                {
                    case "critical":
                        if (innerNode.SelectSingleNode("chance") != null) criticalChance = int.Parse(innerNode.SelectSingleNode("chance").InnerText);
                        if (innerNode.SelectSingleNode("hits_required") != null) criticalHitsRequired = int.Parse(innerNode.SelectSingleNode("hits_required").InnerText);
                        if (innerNode.SelectSingleNode("random_damage") != null) criticalRandomDamage = int.Parse(innerNode.SelectSingleNode("random_damage").InnerText);
                        else criticalRandomDamage = random_damage;
                        if (innerNode.SelectSingleNode("fixed_damage") != null) criticalFixedDamage = int.Parse(innerNode.SelectSingleNode("fixed_damage").InnerText);
                        else criticalFixedDamage = fixed_damage;
                        if (innerNode.SelectSingleNode("severtype") != null) criticalSeverType = innerNode.SelectSingleNode("severtype").InnerText;
                        else criticalSeverType = severtype;
                        break;
                    case "fire":
                        if (innerNode.SelectSingleNode("chance") != null) fireChance = int.Parse(innerNode.SelectSingleNode("chance").InnerText);
                        if (innerNode.SelectSingleNode("chance_causes_debris") != null) fireChanceCauseDebris = int.Parse(innerNode.SelectSingleNode("chance_causes_debris").InnerText);
                        break;
                    case "skill":
                        skill = GameData.getData().skillList[innerNode.InnerText];
                        break;
                    case "flags":
                        foreach (XmlNode tag in innerNode.ChildNodes)
                        {
                            flags |= (AttackFlags)Enum.Parse(typeof(AttackFlags), tag.InnerText);
                        }
                        break;
                    default:
                        FieldInfo f = GetType().GetField(innerNode.Name);

                        if(f == null)
                        {
                            MasterController.GetMC().addErrorMessage("Bad tag in Attack: " + node.Attributes["idname"].Value + ", " + innerNode.Name);
                            break;
                        }

                        if (f.FieldType.Name == "Int32")
                        {
                            f.SetValue(this, int.Parse(innerNode.InnerText));
                        }
                        else if (f.FieldType.Name == "DamageType")
                        {
                            f.SetValue(this, Enum.Parse(typeof(DamageType), innerNode.InnerText));
                        }
                        else
                        {
                            f.SetValue(this, innerNode.InnerText);
                        }

                        break;
                }
            }

            if (sneak_attack_description == null)
                sneak_attack_description = attack_description;
        }
    }
}
