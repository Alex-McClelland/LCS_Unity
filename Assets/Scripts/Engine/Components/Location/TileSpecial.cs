using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.Components.Location
{
    public class TileSpecial : Component
    {
        public string name { get; set; }
        public bool used { get; set; }

        public TileSpecial(string name)
        {
            this.name = name;
        }

        public bool isPassable()
        {
            if (name == "VAULT_DOOR" && !used) return false;

            return true;
        }

        public bool isFlammable()
        {
            if (name == "VAULT_DOOR" && !used) return false;

            return true;
        }

        public bool linkWalls()
        {
            if (name == "VAULT_DOOR") return true;

            return false;
        }

        public bool isUsable()
        {
            switch (name)
            {
                case "LAB_COSMETICS_CAGEDANIMALS":
                case "LAB_GENETIC_CAGEDANIMALS":
                case "POLICESTATION_LOCKUP":
                case "COURTHOUSE_LOCKUP":
                case "COURTHOUSE_JURYROOM":
                case "PRISON_CONTROL_LOW":
                case "PRISON_CONTROL_MEDIUM":
                case "PRISON_CONTROL_HIGH":
                case "INTEL_SUPERCOMPUTER":
                case "SWEATSHOP_EQUIPMENT":
                case "POLLUTER_EQUIPMENT":
                case "NUCLEAR_ONOFF":
                case "HOUSE_PHOTOS":
                case "CORPORATE_FILES":
                case "RADIO_BROADCASTSTUDIO":
                case "NEWS_BROADCASTSTUDIO":
                case "SIGN_ONE":
                case "SIGN_TWO":
                case "SIGN_THREE":
                case "ARMORY":
                case "DISPLAY_CASE":
                case "BANK_VAULT":
                case "BANK_MONEY":
                    return true;
                default:
                    return false;
            }
        }

        //Special functionality is currently hard coded in the SiteScreenController class, but these stubs are for if I
        //Ever get around to making them dynamic
        public virtual string onEnter() { return ""; }
        public virtual string onUse() { return ""; } 
    }
}
