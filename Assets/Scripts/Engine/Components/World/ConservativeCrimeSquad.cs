using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using LCS.Engine.Components.Location;
using LCS.Engine.Components.Creature;
using LCS.Engine.Data;

namespace LCS.Engine.Components.World
{
    public class ConservativeCrimeSquad : Component
    {
        public enum Status
        {
            INACTIVE,
            ACTIVE,
            ATTACK,
            SIEGE
        }

        public enum Exposure
        {
            NONE,
            GOT_DATA,
            EXPOSED,
            NO_BACKERS
        }
              
        [SimpleSave]
        public Status status;
        [SimpleSave]
        public Exposure exposure;
        [SimpleSave]
        public bool defeated;
        [SimpleSave]
        public bool newsCherry;
        [SimpleSave]
        public int baseKills;

        public ConservativeCrimeSquad()
        {
            defeated = false;
            status = Status.INACTIVE;
            newsCherry = false;
            baseKills = 0;
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("ConservativeCrimeSquad");
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
            MasterController.GetMC().nextDay += doDaily;
            MasterController.GetMC().nextMonth += doMonthly;
        }

        public override void unsubscribe()
        {
            MasterController.GetMC().nextDay -= doDaily;
            MasterController.GetMC().nextMonth -= doMonthly;
        }

        private void doDaily(object sender, EventArgs args)
        {

        }

        private void doMonthly(object sender, EventArgs args)
        {
            if (defeated) return;

            if (MasterController.generalPublic.PublicMood > 60 && status == Status.INACTIVE)
            {
                MasterController.generalPublic.PublicOpinion[Constants.VIEW_CONSERVATIVECRIMESQUAD] = 0;
                status = Status.ACTIVE;
            }
            else if (MasterController.generalPublic.PublicMood > 80 && status == Status.ACTIVE)
            {
                status = Status.ATTACK;
            }
            else if (MasterController.generalPublic.PublicMood > 90 && status == Status.ATTACK)
            {
                status = Status.SIEGE;
            }
        }

        public void doRaid()
        {
            if (defeated) return;

            MasterController mc = MasterController.GetMC();

            string storytype = "CCS_SITE";
            if (mc.LCSRandom(10) == 0) storytype = "CCS_KILLED_SITE";

            List<Entity> locationList = new List<Entity>();

            foreach (Entity city in MasterController.nation.cities.Values)
            {
                foreach (string district in city.getComponent<City>().locations.Keys)
                {
                    foreach (Entity e in city.getComponent<City>().locations[district])
                    {
                        if (e.hasComponent<TroubleSpot>() &&
                            (!e.hasComponent<SafeHouse>() ||
                            (!e.getComponent<SafeHouse>().owned && (e.getComponent<SafeHouse>().getFlags() & LocationDef.BaseFlag.CCS_BASE) == 0)))
                        {
                            locationList.Add(e);
                        }
                    }
                }
            }

            News.NewsStory story = MasterController.news.startNewStory(storytype, locationList[mc.LCSRandom(locationList.Count)]);

            story.addCrime("BROKEDOWNDOOR");
            int repeats = mc.LCSRandom(10) + 1;
            for (int i = 0; i < repeats; i++)
                story.addCrime("ATTACKED");
            if (mc.LCSRandom((int)MasterController.ccs.status + 1) != 0)
            {
                repeats = mc.LCSRandom(11);
                for (int i = 0; i < repeats; i++)
                    story.addCrime("KILLEDSOMEBODY");
            }
            if (mc.LCSRandom((int)MasterController.ccs.status + 1) != 0)
            {
                repeats = mc.LCSRandom(11);
                for (int i = 0; i < repeats; i++)
                    story.addCrime("STOLEGROUND");
            }
            if (mc.LCSRandom((int)MasterController.ccs.status + 1) != 0)
            {
                repeats = mc.LCSRandom(11 + 4);
                for (int i = 0; i < repeats; i++)
                    story.addCrime("BREAK_FACTORY");
            }
            if (mc.LCSRandom(2) == 0)
            {
                story.addCrime("CARCHASE");
            }
        }

        public void advanceExposureStoryline()
        {
            switch (exposure)
            {
                case Exposure.NONE:
                case Exposure.GOT_DATA:
                    break;
                case Exposure.EXPOSED:
                    ccsExposureStory();
                    break;
                case Exposure.NO_BACKERS:
                    ccsFBIRaidStory();
                    break;
            }
        }

        private void ccsExposureStory()
        {
            MasterController.news.startNewStory("CCS_NOBACKERS");
            exposure = Exposure.NO_BACKERS;

            int arrestsLeft = 8;

            foreach(string state in MasterController.government.senate.Keys)
            {
                for (int i = 0; i < MasterController.government.senate[state].Count; i++)
                {
                    if (MasterController.government.senate[state][i] <= Alignment.CONSERVATIVE && MasterController.GetMC().LCSRandom(4) == 0)
                    {
                        MasterController.government.senate[state][i] = Alignment.ELITE_LIBERAL;
                        arrestsLeft--;
                    }
                    if (arrestsLeft == 0) break;
                }
                if (arrestsLeft == 0) break;
            }

            arrestsLeft = 17;

            foreach (string state in MasterController.government.house.Keys)
            {
                for (int i = 0; i < MasterController.government.house[state].Count; i++)
                {
                    if (MasterController.government.house[state][i] <= Alignment.CONSERVATIVE && MasterController.GetMC().LCSRandom(4) == 0)
                    {
                        MasterController.government.house[state][i] = Alignment.ELITE_LIBERAL;
                        arrestsLeft--;
                    }
                    if (arrestsLeft == 0) break;
                }
                if (arrestsLeft == 0) break;
            }

            MasterController.government.laws[Constants.LAW_POLICE].alignment += 2;
            if (MasterController.government.laws[Constants.LAW_POLICE].alignment > Alignment.ELITE_LIBERAL)
                MasterController.government.laws[Constants.LAW_POLICE].alignment = Alignment.ELITE_LIBERAL;

            MasterController.generalPublic.changePublicOpinion(Constants.VIEW_POLICE, 50);
            MasterController.generalPublic.changePublicOpinion(Constants.VIEW_CONSERVATIVECRIMESQUAD, 50);

        }

        private void ccsFBIRaidStory()
        {
            MasterController.news.startNewStory("CCS_DEFEATED");

            foreach (Entity e in MasterController.lcs.getAllSleepers())
            {
                if((e.getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.CCS) != 0)
                {
                    e.getComponent<CriminalRecord>().addCrime(Constants.CRIME_RACKETEERING);
                    e.getComponent<CriminalRecord>().arrest();
                }
            }

            MasterController.generalPublic.changePublicOpinion(Constants.VIEW_POLICE, -20);

            destroyCCS();
        }

        public void destroyCCS()
        {
            defeated = true;
            status = Status.INACTIVE;

            //When the CCS is destroyed, hide any safehouses you've discovered but haven't captured
            foreach(Entity e in MasterController.nation.cities.Values)
            {
                City city = e.getComponent<City>();

                foreach(string district in city.locations.Keys)
                {
                    foreach(Entity location in city.locations[district])
                    {
                        if (location.hasComponent<SafeHouse>() &&
                            (location.getComponent<SafeHouse>().getFlags() & LocationDef.BaseFlag.CCS_BASE) != 0 &&
                            !location.getComponent<SafeHouse>().owned)
                            location.getComponent<SiteBase>().hidden = true;
                    }
                }
            }
        }
    }
}
