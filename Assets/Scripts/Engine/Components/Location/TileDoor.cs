using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Item;

namespace LCS.Engine.Components.Location
{
    public class TileDoor : Component
    {
        public bool open { get; set; }
        public bool locked { get; set; }
        public bool alarm { get; set; }
        public bool triedUnlock { get; set; }

        public Difficulty difficulty { get; set; }

        public void tryUnlock(LiberalCrimeSquad.Squad squad)
        {
            Entity opener = squad.getBestAtSkill("SECURITY");

            if (opener.getComponent<CreatureBase>().Skills["SECURITY"].check(difficulty))
            {
                MasterController.GetMC().addCombatMessage(opener.getComponent<CreatureInfo>().getName() + " opens the lock!");
                open = true;
                opener.getComponent<CreatureBase>().Skills["SECURITY"].addExperience(1 + (int)difficulty - opener.getComponent<CreatureBase>().Skills["SECURITY"].level);
                foreach(Entity e in squad)
                {
                    if (e == opener) continue;

                    e.getComponent<CreatureBase>().Skills["SECURITY"].addExperience((int)difficulty - e.getComponent<CreatureBase>().Skills["SECURITY"].level);
                }

                MasterController.news.currentStory.addCrime("UNLOCKEDDOOR");
                if(MasterController.GetMC().currentSiteModeScene.suspicionTimer > 50 || MasterController.GetMC().currentSiteModeScene.suspicionTimer < 0)
                    MasterController.GetMC().currentSiteModeScene.suspicionTimer = 50;
            }
            else
            {
                bool gainedExp = false;

                for(int i = 0; i < 3; i++)
                {
                    if (opener.getComponent<CreatureBase>().Skills["SECURITY"].check(difficulty))
                    {
                        opener.getComponent<CreatureBase>().Skills["SECURITY"].addExperience(10);
                        MasterController.GetMC().addCombatMessage(opener.getComponent<CreatureInfo>().getName() + " is close, but can't quite get the lock open.");
                        gainedExp = true;
                        break;
                    }
                }

                if (!gainedExp)
                {
                    MasterController.GetMC().addCombatMessage(opener.getComponent<CreatureInfo>().getName() + " can't figure the lock out.");
                }
                                
                if (alarm)
                {
                    MasterController.GetMC().addCombatMessage(opener.getComponent<CreatureInfo>().hisHer() + " tampering sets off the alarm!", true);
                    MasterController.GetMC().currentSiteModeScene.alarmTriggered = true;
                }
                triedUnlock = true;
            }
        }

        public void tryBash(LiberalCrimeSquad.Squad squad)
        {
            Difficulty bashDiff = Difficulty.EASY;
            bool crowable = true;

            switch (difficulty)
            {
                case Difficulty.HARD:
                    bashDiff = Difficulty.FORMIDABLE;
                    crowable = false;
                    break;
                case Difficulty.CHALLENGING:
                    bashDiff = Difficulty.CHALLENGING;
                    break;
            }

            Entity bestBasher = squad[0];

            if (crowable)
            {
                crowable = false;

                foreach(Entity e in squad)
                {
                    if (e.getComponent<Inventory>().canCrowbar())
                    {
                        crowable = true;
                        bestBasher = e;
                        break;
                    }
                }
            }

            if (!crowable) {
                foreach (Entity e in squad)
                {
                    if (e.getComponent<CreatureBase>().BaseAttributes["STRENGTH"].getModifiedValue() +
                        e.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getBashStrengthMod() >
                        bestBasher.getComponent<CreatureBase>().BaseAttributes["STRENGTH"].getModifiedValue() +
                        bestBasher.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getBashStrengthMod())
                    {
                        bestBasher = e;
                    }
                }
            }

            bashDiff = (Difficulty) ((int)bashDiff / (bestBasher.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getBashStrengthMod()/100f));

            if(crowable || bestBasher.getComponent<CreatureBase>().BaseAttributes["STRENGTH"].check(bashDiff))
            {
                string bashText = bestBasher.getComponent<CreatureInfo>().getName();

                if (crowable)
                    bashText += " pries open";
                else if (bestBasher.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getBashStrengthMod() > 100)
                    bashText += " smashes in";
                else if ((bestBasher.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.WHEELCHAIR) != 0)
                    bashText += " rams open";
                else
                    bashText += " kicks in";

                bashText += " the door!";

                MasterController.GetMC().addCombatMessage(bashText);

                int timer = 5;
                if (crowable) timer = 20;

                if (MasterController.GetMC().currentSiteModeScene.suspicionTimer > timer || MasterController.GetMC().currentSiteModeScene.suspicionTimer < 0)
                    MasterController.GetMC().currentSiteModeScene.suspicionTimer = timer;
                else
                    MasterController.GetMC().currentSiteModeScene.suspicionTimer = 0;

                if (alarm)
                {
                    MasterController.GetMC().addCombatMessage("The alarm goes off!", true);
                    MasterController.GetMC().currentSiteModeScene.alarmTriggered = true;
                }

                //High security areas set off alarms right away for bashing doors
                if(difficulty == Difficulty.HARD && !MasterController.GetMC().currentSiteModeScene.alarmTriggered)
                {
                    MasterController.GetMC().addCombatMessage("Alarms go off!", true);
                    MasterController.GetMC().currentSiteModeScene.alarmTriggered = true;
                }

                open = true;
                MasterController.GetMC().currentSiteModeScene.siteCrime++;
                MasterController.news.currentStory.addCrime("BROKEDOWNDOOR");
                foreach(Entity e in squad)
                {
                    e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_BREAKING);
                }
            }
            else
            {
                string bashText = bestBasher.getComponent<CreatureInfo>().getName();

                if (bestBasher.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getBashStrengthMod() > 100)
                    bashText += " bashes";
                else if ((bestBasher.getComponent<CreatureInfo>().flags & CreatureInfo.CreatureFlag.WHEELCHAIR) != 0)
                    bashText += " rams";
                else
                    bashText += " kicks";

                bashText += " the door!";

                MasterController.GetMC().addCombatMessage(bashText);

                if (MasterController.GetMC().currentSiteModeScene.suspicionTimer < 0)
                    MasterController.GetMC().currentSiteModeScene.suspicionTimer = 25;
                else if (MasterController.GetMC().currentSiteModeScene.suspicionTimer > 10)
                    MasterController.GetMC().currentSiteModeScene.suspicionTimer -= 10;
                else
                    MasterController.GetMC().currentSiteModeScene.suspicionTimer = 0;
            }
        }
    }
}
