using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.Item;
using LCS.Engine.Components.Location;
using LCS.Engine.Data;

namespace LCS.Engine.Scenes
{
    public class StealCarScene
    {
        private bool sensealarm = false;
        private bool touchalarm = false;
        private bool alarmon = false;
        private int windowdamage = 0;
        private int nervousCounter = 0;
        private bool keysInCar = false;
        private int keyLocation = 0;
        private int keySearchTotal = 0;

        private Entity lib;
        private string cartype;

        public void startScene(Entity lib, string targetCartype)
        {
            this.lib = lib;
            MasterController mc = MasterController.GetMC();
            bool randomcar = false;

            List<ItemDef> vehicleList = new List<ItemDef>();

            foreach (ItemDef item in GameData.getData().itemList.Values)
            {
                if (item.components.ContainsKey("vehicle") && ((ItemDef.VehicleDef)item.components["vehicle"]).stealDifficulty < 10)
                {
                    vehicleList.Add(item);
                }
            }

            if (targetCartype == "RANDOM")
            {
                targetCartype = vehicleList[mc.LCSRandom(vehicleList.Count)].type;
                randomcar = true;
            }

            ItemDef.VehicleDef vehicleDef = (ItemDef.VehicleDef)GameData.getData().itemList[targetCartype].components["vehicle"];
            int diff = vehicleDef.stealDifficulty * 2;
            cartype = targetCartype;

            lib. getComponent<CreatureBase>().Skills[Constants.SKILL_STREET_SENSE].addExperience(5);

            string description = "Adventures in Liberal Car Theft\n" + lib.getComponent<CreatureInfo>().getName() + " looks around for an accessible vehicle...\n";

            if (!lib.getComponent<CreatureBase>().Skills[Constants.SKILL_STREET_SENSE].check((Difficulty)diff))
            {
                do
                {
                    cartype = vehicleList[mc.LCSRandom(vehicleList.Count)].type;
                } while (cartype == targetCartype || mc.LCSRandom(10) < vehicleDef.stealDifficulty);
            }

            if (cartype != targetCartype && !randomcar)
                description += lib.getComponent<CreatureInfo>().getName() + " was unable to find a " + GameData.getData().itemList[targetCartype].name + " but did find a " + GameData.getData().itemList[cartype].name + "\n";
            else
                description += lib.getComponent<CreatureInfo>().getName() + " found a " + GameData.getData().itemList[cartype].name + "\n";

            vehicleDef = (ItemDef.VehicleDef)GameData.getData().itemList[cartype].components["vehicle"];

            sensealarm = mc.LCSRandom(100) < vehicleDef.stealSenseAlarm;
            touchalarm = mc.LCSRandom(100) < vehicleDef.stealTouchAlarm;
            keysInCar = mc.LCSRandom(3) > 0;
            keyLocation = mc.LCSRandom(5);

            List<UI.PopupOption> options = new List<UI.PopupOption>();
            options.Add(new UI.PopupOption("Approach", () =>
            {
                stealVehicleAtDoor("");
            }));
            options.Add(new UI.PopupOption("Call it a Day", () =>
            {
                mc.doNextAction();
            }));

            mc.addAction(() =>
            {
                mc.uiController.showOptionPopup(description, options);
            }, "initial approach");
        }

        private void stealVehicleAtDoor(string result)
        {
            MasterController mc = MasterController.GetMC();
            string description = "";

            if (result != "")
                description += result + "\n";

            if (alarmon)
            {
                if (sensealarm)
                    description += "<color=red>STAND AWAY FROM THE VEHICLE! <BEEP!!> <BEEP!!></color>";
                else
                    description += "<color=red><BEEP!!> <BEEP!!> <BEEP!!> <BEEP!!></color>";
            }
            else if (sensealarm)
                description += "<color=red>THIS IS THE VIPER! STAND AWAY!</color>";
            else
                description += lib.getComponent<CreatureInfo>().getName() + " stands by the " + GameData.getData().itemList[cartype].name;

            List<UI.PopupOption> options = new List<UI.PopupOption>();
            options.Add(new UI.PopupOption("Pick the Lock", () =>
            {
                if (lib.getComponent<CreatureBase>().Skills[Constants.SKILL_SECURITY].check(Difficulty.AVERAGE))
                {
                    lib.getComponent<CreatureBase>().Skills[Constants.SKILL_SECURITY].addExperience(Math.Max(0, 5 - lib.getComponent<CreatureBase>().Skills[Constants.SKILL_SECURITY].level));
                    string pickresult = lib.getComponent<CreatureInfo>().getName() + " jimmies the car door open";

                    bool noticed = noticeCheck();

                    if (touchalarm || sensealarm)
                    {
                        if (!alarmon)
                        {
                            pickresult += "\n<color=yellow>An alarm suddenly starts blaring</color>";
                        }
                        alarmon = true;
                    }

                    if(!noticed) stealVehicleEntered(pickresult);
                }
                else
                {
                    string pickresult = lib.getComponent<CreatureInfo>().getName() + " fiddles with the lock with no luck.";

                    bool noticed = noticeCheck();

                    if (touchalarm || sensealarm)
                    {
                        if (!alarmon)
                        {
                            pickresult += "\n<color=yellow>An alarm suddenly starts blaring</color>";
                        }
                        alarmon = true;
                    }

                    if (!noticed) stealVehicleAtDoor(pickresult);
                }
            }));
            options.Add(new UI.PopupOption("Break the Window", () =>
            {
                ItemDef.VehicleDef vehicleDef = (ItemDef.VehicleDef)GameData.getData().itemList[cartype].components["vehicle"];
                int difficulty = (int)(((int)Difficulty.HARD) / (lib.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getBashStrengthMod() / 100f)) + mc.LCSRandom(vehicleDef.armorHigh) - windowdamage;

                if (lib.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_STRENGTH].check((Difficulty)difficulty))
                {
                    string bashstring = lib.getComponent<CreatureInfo>().getName() + " smashes the window";
                    if (lib.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getBashStrengthMod() > 100)
                    {
                        bashstring += " with a " + lib.getComponent<Inventory>().getWeapon().getComponent<ItemBase>().getName();
                    }

                    bool noticed = noticeCheck();

                    if (touchalarm || sensealarm)
                    {
                        if (!alarmon)
                        {
                            bashstring += "\n<color=yellow>An alarm suddenly starts blaring</color>";
                        }
                        alarmon = true;
                    }

                    windowdamage = 10;
                    if (!noticed) stealVehicleEntered(bashstring);
                }
                else
                {
                    string bashstring = lib.getComponent<CreatureInfo>().getName() + " cracks the window";
                    if (lib.getComponent<Inventory>().getWeapon().getComponent<Weapon>().getBashStrengthMod() > 100)
                    {
                        bashstring += " with a " + lib.getComponent<Inventory>().getWeapon().getComponent<ItemBase>().getName();
                    }
                    bashstring += " but it is still somewhat intact";
                    windowdamage++;

                    bool noticed = noticeCheck();

                    if (touchalarm || sensealarm)
                    {
                        if (!alarmon)
                        {
                            bashstring += "\n<color=yellow>An alarm suddenly starts blaring</color>";
                        }
                        alarmon = true;
                    }

                    if (!noticed) stealVehicleAtDoor(bashstring);
                }
            }));
            options.Add(new UI.PopupOption("Call it a Day", () =>
            {
                if (sensealarm)
                {
                    if (!alarmon)
                        mc.addCombatMessage("THE VIPER? " + lib.getComponent<CreatureInfo>().getName() + " is deterred", true);
                    else
                        mc.addCombatMessage("THE VIPER has deterred " + lib.getComponent<CreatureInfo>().getName(), true);
                }
                mc.doNextAction();
            }));

            mc.uiController.showOptionPopup(description, options);
        }

        private void stealVehicleEntered(string result)
        {
            MasterController mc = MasterController.GetMC();
            string description = "";
            nervousCounter++;

            if (result != "")
                description += result + "\n";

            if (alarmon)
            {
                if (sensealarm)
                    description += "<color=red>REMOVE YOURSELF FROM THE VEHICLE! <BEEP!!> <BEEP!!></color>";
                else
                    description += "<color=red><BEEP!!> <BEEP!!> <BEEP!!> <BEEP!!></color>";
            }
            else if (sensealarm)
                description += "<color=red>THIS IS THE VIPER! STAND AWAY!</color>";
            else
                description += lib.getComponent<CreatureInfo>().getName() + " is behind the wheel of a " + GameData.getData().itemList[cartype].name;

            List<UI.PopupOption> options = new List<UI.PopupOption>();

            options.Add(new UI.PopupOption("Hotwire the Car", () =>
            {
                if (lib.getComponent<CreatureBase>().Skills[Constants.SKILL_SECURITY].check(Difficulty.CHALLENGING))
                {
                    lib.getComponent<CreatureBase>().Skills[Constants.SKILL_SECURITY].addExperience(Math.Max(0, 10 - lib.getComponent<CreatureBase>().Skills[Constants.SKILL_SECURITY].level));
                    mc.addCombatMessage(lib.getComponent<CreatureInfo>().getName() + " hotwires the car!", true);

                    getCar();

                    mc.doNextAction();
                }
                else
                {
                    int textSelection = 0;
                    string text = lib.getComponent<CreatureInfo>().getName();

                    if (lib.getComponent<CreatureBase>().Skills[Constants.SKILL_SECURITY].level < 4) textSelection = mc.LCSRandom(3);
                    else textSelection = mc.LCSRandom(5);

                    switch (textSelection)
                    {
                        case 0: text += " fiddles with the ignition, but the car doesn't start."; break;
                        case 1: text += " digs around in the steering column, but the car doesn't start."; break;
                        case 2: text += " touches some wires together, but the car doesn't start."; break;
                        case 3: text += " makes something in the engine click, but the car doesn't start."; break;
                        case 4: text += " manages to turn on some dash lights, but the car doesn't start."; break;
                    }

                    if(nervousCounter > 5 + mc.LCSRandom(7))
                    {
                        text += "\n<color=yellow>" + lib.getComponent<CreatureInfo>().getName();

                        switch (mc.LCSRandom(3))
                        {
                            case 0: text += " hears someone nearby making a phone call."; break;
                            case 1: text += " is getting nervous being out here this long."; break;
                            case 2: text += " sees a police car driving around a few blocks away."; break;
                        }

                        text += "</color>";

                        nervousCounter = 0;
                    }

                    if(!noticeCheck()) stealVehicleEntered(text);
                }
            }));
            options.Add(new UI.PopupOption("Search for Keys", () =>
            {
                Difficulty diff = Difficulty.IMPOSSIBLE;
                string keyDescription = "";

                if (!keysInCar)
                {
                    diff = Difficulty.IMPOSSIBLE;
                    keyDescription = "in SPACE. With ALIENS. Seriously.";
                }
                else
                {
                    switch (keyLocation)
                    {
                        case 0:
                            diff = Difficulty.AUTOMATIC;
                            keyDescription = "in the ignition. Damn.";
                            break;
                        case 1:
                            diff = Difficulty.EASY;
                            keyDescription = "above the pull-down sunblock thingy!";
                            break;
                        case 2:
                            diff = Difficulty.AVERAGE;
                            keyDescription = "in the glove compartment!";
                            break;
                        case 3:
                            diff = Difficulty.CHALLENGING;
                            keyDescription = "under the front seat!";
                            break;
                        case 4:
                            diff = Difficulty.HARD;
                            keyDescription = "under the back seat!";
                            break;
                    }
                }

                if (lib.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].check(diff))
                {
                    mc.addCombatMessage("Holy " + mc.swearFilter("shit", "car keys") + "! " + lib.getComponent<CreatureInfo>().getName() + " found the keys " + keyDescription, true);

                    getCar();

                    mc.doNextAction();
                }
                else
                {
                    keySearchTotal++;

                    string text = "<rummaging> ";
                    if (keySearchTotal == 5)
                        text += "<color=lime>Are they even in here?</color>";
                    else if (keySearchTotal == 10)
                        text += "<color=yellow>I don't think they're in here...</color>";
                    else if (keySearchTotal == 15)
                        text += "<color=red>If they were here, I'd have found them by now.</color>";
                    else if(keySearchTotal > 15)
                    {
                        switch (mc.LCSRandom(5))
                        {
                            case 0: text += "This isn't working!"; break;
                            case 1: text += "Why me?"; break;
                            case 2: text += "What do I do now?"; break;
                            case 3: text += "Oh no..."; break;
                            case 4: text += "I'm going to get arrested, aren't I?"; break;
                        }
                    }
                    else
                    {
                        switch (mc.LCSRandom(5))
                        {
                            case 0: text += "Please be in here somewhere..."; break;
                            case 1: text += mc.swearFilter("Fuck","Shoot") + "! Where are they?!"; break;
                            case 2: text += "Come on, baby, come to me..."; break;
                            case 3: text += mc.swearFilter("Dammit","Darn it"); break;
                            case 4: text += "I wish I could hotwire this thing..."; break;
                        }
                    }

                    if (nervousCounter > 5 + mc.LCSRandom(7))
                    {
                        text += "\n<color=yellow>" + lib.getComponent<CreatureInfo>().getName();

                        switch (mc.LCSRandom(3))
                        {
                            case 0: text += " hears someone nearby making a phone call."; break;
                            case 1: text += " is getting nervous being out here this long."; break;
                            case 2: text += " sees a police car driving around a few blocks away."; break;
                        }

                        text += "</color>";

                        nervousCounter = 0;
                    }

                    if (!noticeCheck()) stealVehicleEntered(text);
                }
            }));
            options.Add(new UI.PopupOption("Call it a Day", () =>
            {
                if (sensealarm) mc.addCombatMessage("THE VIPER has finally deterred " + lib.getComponent<CreatureInfo>().getName(), true);
                mc.doNextAction();
            }));

            mc.uiController.showOptionPopup(description, options);
        }

        private bool noticeCheck()
        {
            MasterController mc = MasterController.GetMC();

            if(mc.LCSRandom(50) == 0 || (alarmon && mc.LCSRandom(5) == 0))
            {
                mc.addAction(() =>
                {
                    ChaseScene scene = new ChaseScene();
                    scene.startActivityFootChase(5, LocationDef.EnemyType.POLICE, "CARTHEFT", lib, lib.getComponent<CreatureInfo>().getName() + " has been spotted by a passerby!");
                    MasterController.GetMC().doNextAction();
                }, "start new chase: activity arrest");

                mc.doNextAction();

                return true;
            }

            return false;
        }

        private void getCar()
        {
            Entity newCar = Factories.ItemFactory.create(cartype);
            newCar.getComponent<Vehicle>().heat = 14 + newCar.getComponent<Vehicle>().getVehicleData().stealHeat;
            newCar.getComponent<Vehicle>().preferredDriver = lib;
            newCar.persist();
            lib.getComponent<Inventory>().equipVehicle(newCar, true);
            lib.getComponent<Liberal>().homeBase.getComponent<SafeHouse>().addItemToInventory(newCar);
            lib.getComponent<CreatureBase>().juiceMe(newCar.getComponent<Vehicle>().getVehicleData().stealJuice, 100);

            if(MasterController.GetMC().LCSRandom(13 - windowdamage) == 0 || (newCar.def == "VEHICLE_POLICECAR" && MasterController.GetMC().LCSRandom(2) == 0))
            {
                newCar.getComponent<Vehicle>().heat += 10;
                lib.getComponent<Inventory>().tempVehicle = newCar;
                newCar.getComponent<Vehicle>().driver = lib;
                newCar.getComponent<Vehicle>().passengers.Add(lib);

                MasterController.GetMC().addAction(() =>
                {
                    ChaseScene scene = new ChaseScene();
                    scene.startActivityCarChase(5, LocationDef.EnemyType.POLICE, "CARTHEFT", lib, null);
                    MasterController.GetMC().addAction(() =>
                    {
                        //After the chase ends we'll check if the Liberal has been arrested - if they were charge them with Grand Theft Auto
                        if (lib.getComponent<Liberal>().status == Liberal.Status.JAIL_POLICE_CUSTODY ||
                            (lib.getComponent<Liberal>().status == Liberal.Status.HOSPITAL &&
                            lib.getComponent<CriminalRecord>().hospitalArrest))
                        {
                            lib.getComponent<CriminalRecord>().addCrime(Constants.CRIME_CAR_THEFT);
                        }
                        MasterController.GetMC().doNextAction();
                    }, "arrestcheck");
                    MasterController.GetMC().doNextAction();
                }, "start new chase: activity arrest");
            }
        }
    }
}
