using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace LCS.Engine.Components.Creature
{
    public class Age : Component
    {
        public DateTime birthday { get; set; }

        public const float OLDAGE_HUMAN = 60;

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Age");
                entityNode.AppendChild(saveNode);
                XmlNode birthdayNode = saveNode.OwnerDocument.CreateElement("birthday");
                saveNode.AppendChild(birthdayNode);
            }

            saveNode.SelectSingleNode("birthday").InnerText = birthday.ToString("d");
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            birthday = DateTime.Parse(componentData.SelectSingleNode("birthday").InnerText);
        }

        public override void subscribe()
        {
            base.subscribe();
            MasterController.GetMC().nextDay += doGetOlder;
        }

        public override void selfSubscribe()
        {
            base.selfSubscribe();
            getComponent<CreatureBase>().getAttributeModifiers += doGetAttributeModifiers;
        }

        public override void unsubscribe()
        {
            base.unsubscribe();
            MasterController.GetMC().nextDay -= doGetOlder;
            getComponent<CreatureBase>().getAttributeModifiers -= doGetAttributeModifiers;
        }

        public bool isYoung()
        {
            int oldage = getComponent<Body>().getSpecies().oldage;

            return getAge() < (int)(oldage * (18f / OLDAGE_HUMAN));
        }

        private void doGetOlder(object sender, EventArgs args)
        {
            if (!getComponent<Body>().Alive) return;

            int oldage = getComponent<Body>().getSpecies().oldage;
            //They should "age" roughly once every three years (at default elderly threshold of 60). Thus, a person with a base health of 10 will live to an average age of 90.
            float yearMod = (oldage / 20f);

            //Start going grey a bit early
            if (getAge() > oldage - oldage/6)
            {
                if (MasterController.GetMC().LCSRandom((int)(365 * yearMod)) == 0)
                {
                    getComponent<Portrait>().hairColor.lerp(new Portrait.Color(255, 255, 255), 0.2f);
                    getComponent<Portrait>().forceRegen = true;
                }
            }

            if (getAge() <= oldage) return;
            
            if(MasterController.GetMC().LCSRandom((int)(365*yearMod)) == 0)
            {
                getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].Level--;
            }

            if(getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEALTH].Level <=0)
            {
                getComponent<CreatureBase>().doDie(new Events.Die("died of old age"));
                if (hasComponent<Liberal>())
                    MasterController.GetMC().addMessage(getComponent<CreatureInfo>().getName() + " has passed away at the age of " + getAge() + ". The Liberal will be missed.", true);
            }
        }

        private void doGetAttributeModifiers(object sender, Events.GetAttributeModifiers args)
        {
            int oldage = getComponent<Body>().getSpecies().oldage;
            int age = getAge();

            //The fractions in here are due to potential elderly thresholds other than 60 years (dogs, etc.). For humans, they will cancel out.
            //Children have reduced stats all around, except charisma and heart
            if (age <= (int)(oldage * (11f / OLDAGE_HUMAN)))
            {
                args.PostMultipliers["STRENGTH"]["age"] = 0.5f;
                args.LinearModifiers["HEALTH"]["age"] = -2;
                args.LinearModifiers["CHARISMA"]["age"] = 2;
                args.LinearModifiers["INTELLIGENCE"]["age"] = -3;
                args.LinearModifiers["WISDOM"]["age"] = -2;
                args.LinearModifiers["HEART"]["age"] = 2;
            }
            //Teens are generally tougher and smarter than children, but lose charisma due to hormones
            else if (age <= (int)(oldage * (16f / OLDAGE_HUMAN)))
            {
                args.LinearModifiers["STRENGTH"]["age"] = -1;
                args.LinearModifiers["HEALTH"]["age"] = -1;
                args.LinearModifiers["CHARISMA"]["age"] = -1;
                args.LinearModifiers["INTELLIGENCE"]["age"] = -1;
                args.LinearModifiers["WISDOM"]["age"] = -1;
                args.LinearModifiers["HEART"]["age"] = 1;
            }
            //Prime of life, no modifiers positive or negative
            else if (age <= (int)(oldage * (35f / OLDAGE_HUMAN)))
            {
                //No changes
            }
            //Middle age - a little slower, a little wiser. Note that health will not be modified since aging will reduce the BASE stat instead.
            else if (age <= (int)(oldage * (52f / OLDAGE_HUMAN)))
            {
                args.LinearModifiers["STRENGTH"]["age"] = -1;
                args.LinearModifiers["AGILITY"]["age"] = -1;
                args.LinearModifiers["CHARISMA"]["age"] = 1;
                args.LinearModifiers["INTELLIGENCE"]["age"] = 1;
            }
            //Retirement - Physical stats degrade, become a little more set in their ways, but wisdom grants experience.
            else if (age <= (int)(oldage * (70f / OLDAGE_HUMAN)))
            {
                args.LinearModifiers["STRENGTH"]["age"] = -3;
                args.LinearModifiers["AGILITY"]["age"] = -3;
                args.LinearModifiers["CHARISMA"]["age"] = 2;
                args.LinearModifiers["INTELLIGENCE"]["age"] = 2;
                args.LinearModifiers["WISDOM"]["age"] = 1;
                args.LinearModifiers["HEART"]["age"] = -1;
            }
            //Elderly - physical stats heavily degraded, health will start to fail.
            else if (age > (int)(oldage * (70f / OLDAGE_HUMAN)))
            {
                args.LinearModifiers["STRENGTH"]["age"] = -6;
                args.LinearModifiers["AGILITY"]["age"] = -6;
                args.LinearModifiers["CHARISMA"]["age"] = 3;
                args.LinearModifiers["INTELLIGENCE"]["age"] = 3;
                args.LinearModifiers["WISDOM"]["age"] = 2;
                args.LinearModifiers["HEART"]["age"] = -2;
            }
        }

        public int getAge()
        {
            MasterController mc = MasterController.GetMC();

            int age = mc.currentDate.Year - birthday.Year;

            if (mc.currentDate.Month < birthday.Month || (mc.currentDate.Month == birthday.Month && mc.currentDate.Day < birthday.Day))
            {
                age = age - 1;
            }

            return age;
        }

        public string getRoughAge()
        {
            if (getComponent<Body>().getSpecies().type != "HUMAN") return "???";

            int age = getAge();

            if(age < 20)
            {
                return (age + (birthday.Day % 3) - 1) + "?";
            }
            else
            {
                if (age < 30)
                    return "20s";
                else if (age < 40)
                    return "30s";
                else if (age < 50)
                    return "40s";
                else if (age < 60)
                    return "50s";
                else if (age < 70)
                    return "60s";
                else if (age < 80)
                    return "70s";
                else if (age < 90)
                    return "80s";
                else
                    return MasterController.GetMC().getTranslation("INFO_very_old");
            }
        }
    }
}
