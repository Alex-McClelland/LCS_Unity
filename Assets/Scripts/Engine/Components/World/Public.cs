using System;
using System.Collections.Generic;
using System.Xml;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.Location;
using LCS.Engine.Data;

namespace LCS.Engine.Components.World
{
    public class Public : Component
    {
        public int PublicMood { get; set; }
        public int PresidentApprovalRating { get; set; }
        public Dictionary<string, int> PublicOpinion { get; set; }
        public Dictionary<string, int> PublicInterest { get; set; }
        public Dictionary<string, int> BackgroundLiberalInfluence { get; set; }

        public Dictionary<string, PollData> pollData { get; set; }

        public Public()
        {
            PublicMood = 0;
            PublicOpinion = new Dictionary<string, int>();
            PublicInterest = new Dictionary<string, int>();
            BackgroundLiberalInfluence = new Dictionary<string, int>();
            pollData = new Dictionary<string, PollData>();
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Public");
                entityNode.AppendChild(saveNode);

                foreach (string s in PublicOpinion.Keys)
                {
                    XmlNode opinionNode = saveNode.OwnerDocument.CreateElement("PublicOpinion");
                    saveNode.AppendChild(opinionNode);

                    XmlAttribute viewAtt = opinionNode.OwnerDocument.CreateAttribute("view");
                    viewAtt.Value = s;
                    opinionNode.Attributes.Append(viewAtt);
                }
                foreach (string s in PublicInterest.Keys)
                {
                    XmlNode interestNode = saveNode.OwnerDocument.CreateElement("PublicInterest");
                    saveNode.AppendChild(interestNode);

                    XmlAttribute viewAtt = interestNode.OwnerDocument.CreateAttribute("view");
                    viewAtt.Value = s;
                    interestNode.Attributes.Append(viewAtt);
                }
                foreach (string s in BackgroundLiberalInfluence.Keys)
                {
                    XmlNode influenceNode = saveNode.OwnerDocument.CreateElement("BackgroundLiberalInfluence");
                    saveNode.AppendChild(influenceNode);

                    XmlAttribute viewAtt = influenceNode.OwnerDocument.CreateAttribute("view");
                    viewAtt.Value = s;
                    influenceNode.Attributes.Append(viewAtt);
                }
            }

            saveField(PublicMood, "PublicMood", saveNode);
            saveField(PresidentApprovalRating, "PresidentApprovalRating", saveNode);
            foreach (XmlNode node in saveNode.SelectNodes("PublicOpinion"))
            {
                node.InnerText = PublicOpinion[node.Attributes["view"].Value].ToString();
            }
            foreach (XmlNode node in saveNode.SelectNodes("PublicInterest"))
            {
                node.InnerText = PublicInterest[node.Attributes["view"].Value].ToString();
            }
            foreach (XmlNode node in saveNode.SelectNodes("BackgroundLiberalInfluence"))
            {
                node.InnerText = BackgroundLiberalInfluence[node.Attributes["view"].Value].ToString();
            }
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            PublicMood = int.Parse(componentData.SelectSingleNode("PublicMood").InnerText);
            PresidentApprovalRating = int.Parse(componentData.SelectSingleNode("PresidentApprovalRating").InnerText);

            foreach (XmlNode node in componentData.SelectNodes("PublicOpinion"))
                PublicOpinion.Add(node.Attributes["view"].Value, int.Parse(node.InnerText));
            foreach (XmlNode node in componentData.SelectNodes("PublicInterest"))
                PublicInterest.Add(node.Attributes["view"].Value, int.Parse(node.InnerText));
            foreach (XmlNode node in componentData.SelectNodes("BackgroundLiberalInfluence"))
                BackgroundLiberalInfluence.Add(node.Attributes["view"].Value, int.Parse(node.InnerText));

            foreach (string view in PublicOpinion.Keys)
            {
                PollData pollDataNode = new PollData();
                pollDataNode.def = view;
                pollDataNode.age = 50;
                pollData.Add(view, pollDataNode);
            }
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

        public int calculatePresidentApproval()
        {
            int approval = 0;

            Dictionary<string, int> issueArray = buildRandomIssueList(true);

            for (int i = 0; i < 1000; i++)
            {
                if (i % 2 == 0 && MasterController.GetMC().LCSRandom(2) == 0) approval++;
                else if (i % 2 == 1 && MasterController.GetMC().LCSRandom(2) == 0) continue;
                else
                {
                    Alignment vote = getSwingVoter(issueArray);
                    Alignment president = getComponent<Government>().president.getComponent<Politician>().alignment;
                    Alignment presparty = getComponent<Government>().president.getComponent<Politician>().party;

                    // If their views are close to the President's views, they should
                    // approve, but might not if their party leaning conflicts with
                    // the president's
                    // Moderate president from the Conservative party is only supported
                    // by moderates and Conservatives
                    // Moderate president from the Liberal party is only supported
                    // by moderates and Liberals
                    if (Math.Abs(vote - president) <= 1 &&
                        ((presparty == Alignment.LIBERAL && vote >= Alignment.MODERATE) ||
                        (presparty == Alignment.CONSERVATIVE && vote <= Alignment.MODERATE)))
                    {
                        approval++;
                    }
                }
            }

            return approval;
        }

        public int getLawOpinion(string law)
        {
            int mood = 0;
            int numberViews = 0;

            foreach (ViewDef view in GameData.getData().lawList[law].views.Keys)
            {
                mood += MasterController.generalPublic.PublicOpinion[view.type] * GameData.getData().lawList[law].views[view];
                numberViews += GameData.getData().lawList[law].views[view];
            }

            mood /= numberViews;

            //Opinion on laws will be adjusted by overall public mood, but opinions on the specific issue carry more weight
            mood = (mood * 2 + PublicMood) / 3;

            return mood;
        }

        public int getLawInterest(string law)
        {
            int interest = 0;
            int numberViews = 0;

            foreach (ViewDef view in GameData.getData().lawList[law].views.Keys)
            {
                interest += MasterController.generalPublic.PublicInterest[view.type] * GameData.getData().lawList[law].views[view];
                numberViews += GameData.getData().lawList[law].views[view];
            }

            interest /= numberViews;

            return interest;
        }

        public Alignment getSwingVoter()
        {
            int bias = PublicMood - MasterController.GetMC().LCSRandom(100);
            return getSwingVoter(bias);
        }

        public Alignment getSwingVoter(int bias, string issue = null)
        {
            if (bias > 25) bias = 25;
            if (bias < -25) bias = -25;

            Alignment alignment = Alignment.ARCHCONSERVATIVE;

            for (int i = 0; i < 4; i++)
            {
                if (25 + MasterController.GetMC().LCSRandom(50) - bias < PublicOpinion[issue ?? randomissue(true)]) alignment++;
            }

            return alignment;
        }

        public Alignment getSimpleVoter(Alignment leaning)
        {
            Alignment vote = leaning - 1;

            for (int i = 0; i < 2; i++)
            {
                if (MasterController.GetMC().LCSRandom(100) < PublicOpinion[randomissue(true)]) vote++;
            }

            return vote;
        }

        private Alignment getSimpleVoter(Alignment leaning, Dictionary<string, int> issueArray)
        {
            Alignment vote = leaning - 1;

            for (int i = 0; i < 2; i++)
            {
                if (MasterController.GetMC().LCSRandom(100) < PublicOpinion[randomissue(issueArray)]) vote++;
            }

            return vote;
        }

        private Alignment getSwingVoter(Dictionary<string, int> issueArray)
        {
            Alignment alignment = Alignment.ARCHCONSERVATIVE;

            int bias = PublicMood - MasterController.GetMC().LCSRandom(100);

            if (bias > 25) bias = 25;
            if (bias < -25) bias = -25;

            for (int i = 0; i < 4; i++)
            {
                if (25 + MasterController.GetMC().LCSRandom(50) - bias < PublicOpinion[randomissue(issueArray)]) alignment++;
            }

            return alignment;
        }

        public void changePublicOpinion(string viewname, int power, short affect = 1, int cap = 100, bool drift = false)
        {
            if (!drift &&
                viewname != "AM_RADIO" &&
                viewname != "CABLE_NEWS" &&
                viewname != "LIBERALCRIMESQUAD" &&
                viewname != "LIBERALCRIMESQUADPOS" &&
                viewname != "CONSERVATIVECRIMESQUAD")
            {
                BackgroundLiberalInfluence[viewname] += power * 10;
            }

            if (viewname == "LIBERALCRIMESQUAD")
            {
                affect = 0;
            }

            if (viewname == "LIBERALCRIMESQUADPOS")
            {
                affect = 0;
                if (cap > PublicMood + 40) cap = PublicMood + 40;
            }

            int effpower = power;

            // Affect is whether the LCS is publicly known to be behind
            // the circumstances creating the public opinion change
            if (affect == 1)
            {
                // Aff is the % of people who know/care about the LCS
                int aff = PublicOpinion["LIBERALCRIMESQUAD"];
                // Rawpower is the amount of the action proportional
                // to the people who, not having heard of the LCS,
                // do not allow the LCS' reputation to affect their
                // opinions
                int rawpower = (int)(power * (float)(100 - aff) / 100.0f);
                // Affected power is the remainder of the action besides
                // rawpower, the amount of the people who know of the LCS
                // and have it alter their opinion
                int affectedpower = power - rawpower;

                if (affectedpower > 0)
                {
                    // Dist is a combination of the relative popularity of the LCS
                    // to the issue and the absolute popularity of the LCS. Very
                    // popular LCS on a very unpopular issue is very influential.
                    // Very unpopular LCS on a very popular issue has the ability
                    // to actually have a reverse effect.
                    int dist = PublicOpinion["LIBERALCRIMESQUADPOS"] - PublicOpinion[viewname] +
                        PublicOpinion["LIBERALCRIMESQUADPOS"] - 50;

                    // Affected power is then scaled by dist -- if the LCS is
                    // equally popular as the issue, it's equally powerful as
                    // the rawpower. For every 10% up or down past there, it's
                    // 10% more or less powerful.
                    affectedpower = (int)(((float)affectedpower * (100.0 + (float)dist)) / 100.0f);
                }

                // Effpower is then the sum of the rawpower (people who don't know
                // about the LCS) and the affectedpower (people who do know
                // about the LCS and had their judgment swayed by their opinion
                // of it).
                effpower = rawpower + affectedpower;
            }
            else if (affect == -1)
            {
                // Simplifed algorithm for affect by CCS respect
                effpower = power * (100 - PublicOpinion["CONSERVATIVECRIMESQUAD"]) / 100;
            }

            if (viewname == "LIBERALCRIMESQUAD")
            {
                //Only half the country will ever hear about the LCS at one time,
                //and people will only grudgingly lose fear of it
                if (effpower < -5) effpower = -5;
                if (effpower > 50) effpower = 50;
            }
            else if (viewname == "LIBERALCRIMESQUADPOS")
            {
                //Only 50% of the country can be swayed at once in their views
                //of the LCS negatively, 5% positively
                if (effpower < -50) effpower = -50;
                if (effpower > 5) effpower = 5;
            }

            //Scale the magnitude of the effect based on how much
            //people are paying attention to the issue
            effpower = (int)(effpower * (1 + (float)PublicInterest[viewname] / 50));

            //Then affect public interest. Public interest shouldn't be raised by basic montly opinion drift.
            if (!drift)
            {
                if (PublicInterest[viewname] < cap || (viewname == "LIBERALCRIMESQUADPOS" && PublicInterest[viewname] < 100))
                    PublicInterest[viewname] += Math.Abs(effpower);
            }

            if (effpower > 0)
            {
                //Some things will never persuade the last x% of the population.
                //If there's a cap on how many people will be impressed, this
                //is where that's handled.
                if (PublicOpinion[viewname] + effpower > cap)
                {
                    if (PublicOpinion[viewname] > cap) effpower = 0;
                    else effpower = cap - PublicOpinion[viewname];
                }
            }

            //Finally, apply the effect.
            PublicOpinion[viewname] += effpower;

            if (PublicOpinion[viewname] < 0) PublicOpinion[viewname] = 0;
            if (PublicOpinion[viewname] > 100) PublicOpinion[viewname] = 100;
        }

        public string randomissue(bool core_only = false)
        {
            Dictionary<string, int> interestArray = buildRandomIssueList(core_only);

            string result = MasterController.GetMC().WeightedRandom(interestArray);

            return result;
        }

        private string randomissue(Dictionary<string, int> interestArray)
        {
            string result = MasterController.GetMC().WeightedRandom(interestArray);

            return result;
        }

        private Dictionary<string, int> buildRandomIssueList(bool core_only = false)
        {
            Dictionary<string, int> interestArray = new Dictionary<string, int>();

            foreach (string s in PublicInterest.Keys)
            {
                if (core_only)
                {
                    if(s == Constants.VIEW_AM_RADIO ||
                       s == Constants.VIEW_CABLE_NEWS ||
                       s == Constants.VIEW_LIBERALCRIMESQUAD ||
                       s == Constants.VIEW_LIBERALCRIMESQUADPOS ||
                       s == Constants.VIEW_CONSERVATIVECRIMESQUAD)
                    {
                        continue;
                    }
                }

                if (MasterController.ccs.status == ConservativeCrimeSquad.Status.INACTIVE &&
                    s == Constants.VIEW_CONSERVATIVECRIMESQUAD)
                {
                    continue;
                }

                interestArray.Add(s, PublicInterest[s] + 25);
            }

            return interestArray;
        }

        private void doMonthly(object sender, EventArgs args)
        {
            int conPower = 200 - PublicOpinion["AM_RADIO"] - PublicOpinion["CABLE_NEWS"];
            Dictionary<string, int> libPower = new Dictionary<string, int>();

            foreach (string view in PublicOpinion.Keys)
            {
                libPower[view] = 0;
                PublicInterest[view] /= 2;
            }

            //Sleepers
            foreach (Entity e in MasterController.lcs.getAllSleepers())
            {
                if (e.getComponent<Liberal>().dailyActivity.type == "SLEEPER_ADVOCATE")
                {
                    int power = e.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_CHARISMA].getModifiedValue() +
                        e.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_HEART].getModifiedValue() +
                        e.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].getModifiedValue() +
                        e.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].level;

                    foreach (SkillDef skill in GameData.getData().creatureDefList[e.def].sleeper.bonusSkills)
                    {
                        power += e.getComponent<CreatureBase>().Skills[skill.type].level;
                    }
                    power = (int)(power * GameData.getData().creatureDefList[e.def].sleeper.powerMultiplier);

                    power = (int)(power * (e.getComponent<Liberal>().infiltration / 100d));

                    if (e.def == "RADIOPERSONALITY")
                    {
                        changePublicOpinion(Constants.VIEW_AM_RADIO, 1);
                        List<string> viewList = new List<string>(libPower.Keys);
                        foreach (string view in viewList)
                        {
                            if (view == Constants.VIEW_LIBERALCRIMESQUAD ||
                                view == Constants.VIEW_LIBERALCRIMESQUADPOS ||
                                view == Constants.VIEW_CONSERVATIVECRIMESQUAD)
                                continue;

                            libPower[view] += (int)(power * ((100 - PublicOpinion[Constants.VIEW_AM_RADIO]) / 100d));
                        }
                    }
                    else if (e.def == "NEWSANCHOR")
                    {
                        changePublicOpinion(Constants.VIEW_CABLE_NEWS, 1);
                        List<string> viewList = new List<string>(libPower.Keys);
                        foreach (string view in viewList)
                        {
                            if (view == Constants.VIEW_LIBERALCRIMESQUAD ||
                                view == Constants.VIEW_LIBERALCRIMESQUADPOS ||
                                view == Constants.VIEW_CONSERVATIVECRIMESQUAD)
                                continue;

                            libPower[view] += (int)(power * ((100 - PublicOpinion[Constants.VIEW_CABLE_NEWS]) / 100d));
                        }
                    }
                    else if (e.def == "POLITICIAN")
                    {
                        string a = randomissue(true);
                        string b = randomissue(true);
                        while (a == b) b = randomissue(true);
                        string c = randomissue(true);
                        while (a == c || b == c) c = randomissue(true);

                        libPower[a] += power;
                        libPower[b] += power;
                        libPower[c] += power;
                    }
                    else
                    {
                        if (GameData.getData().creatureDefList[e.def].sleeper.affectedViews.Count > 0)
                        {
                            foreach (ViewDef view in GameData.getData().creatureDefList[e.def].sleeper.affectedViews)
                            {
                                libPower[view.type] += power;
                            }
                        }
                        else
                        {
                            libPower[randomissue(true)] += power;
                        }
                    }
                }
            }

            foreach (Entity city in MasterController.nation.cities.Values)
            {
                foreach (List<Entity> district in city.getComponent<City>().locations.Values)
                {
                    foreach (Entity location in district)
                    {
                        if (!location.hasComponent<TroubleSpot>()) continue;

                        foreach (Position p in location.getComponent<TroubleSpot>().graffitiList.Keys)
                        {
                            int power = 0;
                            Alignment align = Alignment.MODERATE;
                            if (location.getComponent<TroubleSpot>().graffitiList[p] == TileBase.Graffiti.CCS) align = Alignment.CONSERVATIVE;
                            else if (location.getComponent<TroubleSpot>().graffitiList[p] == TileBase.Graffiti.LCS) align = Alignment.LIBERAL;

                            //Purge graffiti from high security sites, but let it influence public opinion
                            if ((location.getComponent<TroubleSpot>().getFlags() & (LocationDef.TroubleSpotFlag.MID_SECURITY | LocationDef.TroubleSpotFlag.HIGH_SECURITY)) != 0)
                            {
                                power = 5;
                                location.getComponent<TroubleSpot>().map[p.z][p.x, p.y].getComponent<TileBase>().graffiti = TileBase.Graffiti.NONE;
                            }
                            else
                            {
                                if (location.hasComponent<SafeHouse>())
                                {
                                    if (location.getComponent<SafeHouse>().owned)
                                    {
                                        location.getComponent<TroubleSpot>().map[p.z][p.x, p.y].getComponent<TileBase>().graffiti = TileBase.Graffiti.LCS;
                                    }
                                    else if ((location.getComponent<SafeHouse>().getFlags() & LocationDef.BaseFlag.CCS_BASE) != 0)
                                    {
                                        location.getComponent<TroubleSpot>().map[p.z][p.x, p.y].getComponent<TileBase>().graffiti = TileBase.Graffiti.CCS;
                                    }
                                }
                                else
                                {
                                    power = 1;
                                    //Replace with generic gang tags
                                    if (MasterController.GetMC().LCSRandom(10) == 0)
                                        location.getComponent<TroubleSpot>().map[p.z][p.x, p.y].getComponent<TileBase>().graffiti = TileBase.Graffiti.GNG;
                                    //Replace with CCS tags (if CCS has been activated)
                                    if (MasterController.ccs.status >= ConservativeCrimeSquad.Status.ACTIVE && MasterController.GetMC().LCSRandom(30 / (int)MasterController.ccs.status) == 0)
                                        location.getComponent<TroubleSpot>().map[p.z][p.x, p.y].getComponent<TileBase>().graffiti = TileBase.Graffiti.CCS;
                                    //Clean up graffiti
                                    if (MasterController.GetMC().LCSRandom(30) == 0)
                                        location.getComponent<TroubleSpot>().map[p.z][p.x, p.y].getComponent<TileBase>().graffiti = TileBase.Graffiti.NONE;
                                }
                            }

                            if (align == Alignment.LIBERAL)
                            {
                                BackgroundLiberalInfluence[Constants.VIEW_LIBERALCRIMESQUAD] += power;
                                if (MasterController.ccs.status >= ConservativeCrimeSquad.Status.ACTIVE)
                                    BackgroundLiberalInfluence[Constants.VIEW_CONSERVATIVECRIMESQUAD] += power;
                            }
                            if (align == Alignment.CONSERVATIVE)
                            {
                                BackgroundLiberalInfluence[Constants.VIEW_LIBERALCRIMESQUAD] -= power;
                                if (MasterController.ccs.status >= ConservativeCrimeSquad.Status.ACTIVE)
                                    BackgroundLiberalInfluence[Constants.VIEW_CONSERVATIVECRIMESQUAD] -= power;
                            }
                        }

                        location.getComponent<TroubleSpot>().updateGraffitiList();
                    }
                }
            }

            Dictionary<string, int> issueBalance = new Dictionary<string, int>();

            List<string> views = new List<string>(PublicOpinion.Keys);

            foreach (string view in views)
            {
                libPower[view] += BackgroundLiberalInfluence[view];
                BackgroundLiberalInfluence[view] = (int)(BackgroundLiberalInfluence[view] * .66);

                if (view == "LIBERALCRIMESQUAD" ||
                    view == "LIBERALCRIMESQUADPOS" ||
                    view == "CONSERVATIVECRIMESQUAD")
                    continue;

                if (view != "CABLE_NEWS" && view != "AM_RADIO")
                {
                    issueBalance[view] = libPower[view] - conPower;

                    int roll = issueBalance[view] + MasterController.GetMC().LCSRandom(400) - 200;

                    if (roll < -50)
                        changePublicOpinion(view, -1, 0, 100, true);
                    else if (roll > 50)
                        changePublicOpinion(view, 1, 0, 100, true);
                    else
                        changePublicOpinion(view, MasterController.GetMC().LCSRandom(2) * 2 - 1, 0, 100, true);
                }
                else
                {
                    if (PublicMood < PublicOpinion[view]) changePublicOpinion(view, -1, 0, 100, true);
                    else changePublicOpinion(view, 1, 0, 100, true);
                }
            }

        }

        private void doDaily(object sender, EventArgs args)
        {
            //Re-poll the president's approval rating every week or so.
            if (MasterController.GetMC().LCSRandom(7) == 0) PresidentApprovalRating = calculatePresidentApproval();

            if (pollData.ContainsKey(Constants.VIEW_CONSERVATIVECRIMESQUAD) && MasterController.ccs.status == ConservativeCrimeSquad.Status.INACTIVE)
                pollData.Remove(Constants.VIEW_CONSERVATIVECRIMESQUAD);

            foreach (PollData data in pollData.Values)
            {
                data.age++;
            }

            //Calculate the overall public mood
            int mood = 0;
            foreach (string view in PublicOpinion.Keys)
            {
                if (view == "LIBERALCRIMESQUAD" ||
                    view == "LIBERALCRIMESQUADPOS" ||
                    view == "CONSERVATIVECRIMESQUAD")
                    continue;

                mood += PublicOpinion[view];
            }

            PublicMood = mood / (PublicOpinion.Count - 3);
        }

        public class PollData
        {
            public enum PublicInterest
            {
                UNKNOWN,
                NONE,
                LOW,
                MODERATE,
                HIGH,
                VERY_HIGH
            }

            public string def { get; set; }
            public int percent { get; set; }
            public int noise { get; set; }
            public PublicInterest publicInterest { get; set; }
            public int age { get; set; }
        }
    }
}
