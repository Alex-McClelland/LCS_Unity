using System;
using System.Collections.Generic;
using System.Xml;
using LCS.Engine.Components.World;

namespace LCS.Engine.Components.Creature
{
    public class CreatureInfo : Component
    {
        public enum CreatureGender
        {
            NEUTRAL,
            MALE,
            FEMALE,

            // Used to get some more specific names.
            WHITEMALEPATRIARCH,

            // Used in creature creation.
            MALE_BIAS,
            FEMALE_BIAS,
            RANDOM
        };

        [Flags]
        public enum CreatureFlag
        {
            NONE = 0,
            MISSING = 1,
            ILLEGAL_IMMIGRANT = 2,
            WHEELCHAIR = 4,
            CONVERTED = 8,
            NO_BLUFF = 16,
            KIDNAPPED = 32,
            JUST_ESCAPED = 64
        }

        [SimpleSave]
        public Alignment alignment;
        [SimpleSave]
        public string givenName;
        [SimpleSave]
        public string surname;
        [SimpleSave]
        public string alias;
        [SimpleSave]
        public CreatureGender genderConservative;
        [SimpleSave]
        public CreatureGender genderLiberal;
        [SimpleSave]
        public string type_name;
        [SimpleSave]
        public string encounterName;
        [SimpleSave]
        public CreatureFlag flags;
        [SimpleSave]
        public Entity workLocation;

        public bool inCombat;

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("CreatureInfo");
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
            MasterController.GetMC().nextDay += doClearFlags;
        }

        public override void unsubscribe()
        {
            base.unsubscribe();
            MasterController.GetMC().nextDay -= doClearFlags;
        }

        private void doClearFlags(object sender, EventArgs args)
        {
            flags &= ~CreatureFlag.NO_BLUFF;
            if (MasterController.government.laws[Constants.LAW_IMMIGRATION].alignment == Alignment.ELITE_LIBERAL)
                flags &= ~CreatureFlag.ILLEGAL_IMMIGRANT;
        }

        public string hisHer()
        {
            MasterController mc = MasterController.GetMC();

            switch (genderLiberal)
            {
                case CreatureGender.FEMALE:
                    return mc.getTranslation("GENDER_feminine_possessive");
                case CreatureGender.MALE:
                case CreatureGender.WHITEMALEPATRIARCH:
                    return mc.getTranslation("GENDER_masculine_possessive");
                case CreatureGender.NEUTRAL:
                    return mc.getTranslation("GENDER_neutral_possessive");
                default:
                    return "NO GENDER DEFINED";
            }
        }

        public string himHer()
        {
            MasterController mc = MasterController.GetMC();

            switch (genderLiberal)
            {
                case CreatureGender.FEMALE:
                    return mc.getTranslation("GENDER_feminine_object_pronoun");
                case CreatureGender.MALE:
                case CreatureGender.WHITEMALEPATRIARCH:
                    return mc.getTranslation("GENDER_masculine_object_pronoun");
                case CreatureGender.NEUTRAL:
                    return mc.getTranslation("GENDER_neutral_object_pronoun"); ;
                default:
                    return "NO GENDER DEFINED";
            }
        }

        public string heShe()
        {
            MasterController mc = MasterController.GetMC();

            switch (genderLiberal)
            {
                case CreatureGender.FEMALE:
                    return mc.getTranslation("GENDER_feminine_subject_pronoun");
                case CreatureGender.MALE:
                case CreatureGender.WHITEMALEPATRIARCH:
                    return mc.getTranslation("GENDER_masculine_subject_pronoun");
                case CreatureGender.NEUTRAL:
                    return mc.getTranslation("GENDER_neutral_subject_pronoun");
                default:
                    return "NO GENDER DEFINED";
            }
        }

        public string manWoman()
        {
            MasterController mc = MasterController.GetMC();

            switch (genderLiberal)
            {
                case CreatureGender.FEMALE:
                    return mc.getTranslation("GENDER_feminine_gender_descriptor");
                case CreatureGender.MALE:
                case CreatureGender.WHITEMALEPATRIARCH:
                    return mc.getTranslation("GENDER_masculine_gender_descriptor");
                case CreatureGender.NEUTRAL:
                    return mc.getTranslation("GENDER_neutral_gender_descriptor");
                default:
                    return "NO GENDER DEFINED";
            }
        }

        public string getName(bool shortname = false)
        {
            if (alias != "") return alias;
            else if (shortname) return surname;
            else return givenName + " " + surname;
        }

        public void changeGender()
        {
            switch (genderLiberal)
            {
                case CreatureGender.FEMALE:
                    genderLiberal = CreatureGender.MALE;
                    break;
                case CreatureGender.MALE:
                case CreatureGender.WHITEMALEPATRIARCH:
                   genderLiberal = CreatureGender.NEUTRAL;
                    break;
                case CreatureGender.NEUTRAL:
                    genderLiberal = CreatureGender.FEMALE;
                    break;
            }
        }
    }
}
