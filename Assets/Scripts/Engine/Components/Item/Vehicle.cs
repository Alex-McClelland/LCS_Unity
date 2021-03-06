using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using LCS.Engine.Components.Creature;
using LCS.Engine.Data;

namespace LCS.Engine.Components.Item
{
    public class Vehicle : Component
    {
        [SimpleSave]
        public int heat, year;
        [SimpleSave]
        public ItemDef.VehicleColor color;
        [SimpleSave]
        public Entity preferredDriver;

        public Entity driver;
        public bool used;
        public List<Entity> passengers;

        public Vehicle()
        {
            heat = 0;
            passengers = new List<Entity>();
        }

        public Vehicle(int year, ItemDef.VehicleColor color) : this()
        {
            this.color = color;
            this.year = year;
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Vehicle");
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
            used = false;
            driver = null;
            passengers.Clear();

            //Clean out dead or disbanded libs from the preferred driver slot
            if (preferredDriver != null)
            {
                if (!preferredDriver.getComponent<Body>().Alive) preferredDriver = null;
                else if (preferredDriver.hasComponent<Liberal>() && preferredDriver.getComponent<Liberal>().disbanded) preferredDriver = null;
            }
        }

        private int modifiedDodgeSkill(int skillLevel)
        {
            int score = (int)((skillLevel + getVehicleData().dodgeBase) * getVehicleData().dodgeSkill);
            if (score < getVehicleData().dodgeSoftCap) return score;
            if (score > getVehicleData().dodgeSoftCap)
                score = (score + getVehicleData().dodgeSoftCap) / 2;
            return score > getVehicleData().dodgeHardCap ? getVehicleData().dodgeHardCap : score;
        }

        private int modifiedDriveSkill(int skillLevel)
        {
            int score = (int)((skillLevel + getVehicleData().driveBase) * getVehicleData().driveSkill);
            if (score < getVehicleData().driveSoftCap) return score;
            if (score > getVehicleData().driveSoftCap)
                score = (score + getVehicleData().driveSoftCap) / 2;
            return score > getVehicleData().driveHardCap ? getVehicleData().driveHardCap : score;
        }

        public int dodgeRoll()
        {
            if (driver == null) return 0;

            MasterController mc = MasterController.GetMC();
            List<int> rolls = new List<int>();
            CreatureBase.Skill driveSkill = driver.getComponent<CreatureBase>().Skills[Constants.SKILL_DRIVING];

            int skillLevel = driveSkill.level + Math.Min(driveSkill.associatedAttribute.getModifiedValue() / 2, driveSkill.level + 3); ;

            int totalStrength = modifiedDodgeSkill(skillLevel);

            for (int i = 0; i < totalStrength / 3; i++)
            {
                rolls.Add(mc.LCSRandom(6) + 1);
            }

            if (totalStrength % 3 == 1) rolls.Add(mc.LCSRandom(3) + 1);
            else if (totalStrength % 3 == 2) rolls.Add(mc.LCSRandom(5) + 1);

            rolls.Sort();
            rolls.Reverse();

            int total = 0;
            for (int i = 0; i < rolls.Count && i < 3; i++)
            {
                total += rolls[i];
            }

            if (mc.SkillRollDebug)
            {
                string debugMessage = "##DEBUG## Skill DODGEDRIVE Roll: Power=" + totalStrength + " Rolls=";
                foreach (int i in rolls)
                {
                    debugMessage += i + ",";
                }

                debugMessage = debugMessage.TrimEnd(',');
                debugMessage += " Total=" + total;
                
                mc.addCombatMessage(debugMessage);
            }

            return total;
        }

        public int driveRoll()
        {
            if (driver == null) return 0;

            MasterController mc = MasterController.GetMC();
            List<int> rolls = new List<int>();
            CreatureBase.Skill driveSkill = driver.getComponent<CreatureBase>().Skills[Constants.SKILL_DRIVING];

            int skillLevel = driveSkill.level + Math.Min(driveSkill.associatedAttribute.getModifiedValue() / 2, driveSkill.level + 3); ;

            int totalStrength = modifiedDriveSkill(skillLevel);

            for (int i = 0; i < totalStrength / 3; i++)
            {
                rolls.Add(mc.LCSRandom(6) + 1);
            }

            if (totalStrength % 3 == 1) rolls.Add(mc.LCSRandom(3) + 1);
            else if (totalStrength % 3 == 2) rolls.Add(mc.LCSRandom(5) + 1);

            rolls.Sort();
            rolls.Reverse();

            int total = 0;
            for (int i = 0; i < rolls.Count && i < 3; i++)
            {
                total += rolls[i];
            }

            if (mc.SkillRollDebug)
            {
                string debugMessage = "##DEBUG## Skill ESCAPEDRIVE Roll: Power=" + totalStrength + " Rolls=";
                foreach (int i in rolls)
                {
                    debugMessage += i + ",";
                }

                debugMessage = debugMessage.TrimEnd(',');
                debugMessage += " Total=" + total;
                
                mc.addCombatMessage(debugMessage);
            }

            return total;
        }

        public ItemDef.VehicleDef getVehicleData()
        { return (ItemDef.VehicleDef) GameData.getData().itemList[owner.def].components["vehicle"]; }
    }
}
