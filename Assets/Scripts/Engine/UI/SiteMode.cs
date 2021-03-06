using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.UI
{
    public interface SiteMode : UIBase
    {
        void init(SiteModeActions actions);
        void buildMap(Entity location, int z);
        void startEncounter();
        void leaveEncounter();
    }

    public class SiteModeActions
    {
        public stringAction move;
        public Action wait;
        public Action fight;
        public Action advanceRound;
        public EntityInteraction talkIssues;
        public EntityInteraction talkDating;
        public Action talkRentRoom;
        public Action talkBuyWeapons;
        public EntityInteraction kidnap;
        public Action talkBluff;
        public Action talkIntimidate;
        public Action talkThreatenHostage;
        public boolAction setEncounterWarnings;
        public Action loot;
        public Action use;
        public Action releaseOppressed;
        public Action surrender;
        public Action robBankNote;
        public Action robBankThreaten;

        public delegate void boolAction(bool value);
        public delegate void stringAction(string dir);
        public delegate void EntityInteraction(Entity lib, Entity con);
    }
}
