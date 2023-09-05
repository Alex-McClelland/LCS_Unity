using System;
using System.Collections.Generic;
using LCS.Engine.Events;
using LCS.Engine.Components.Creature;
using LCS.Engine.Containers;
using LCS.Engine.Data;
using System.Xml;

namespace LCS.Engine.Components.World
{
    public class Government : Component
    {
        [SimpleSave]
        public Entity president;
        [SimpleSave]
        public int presidentTerm;
        [SimpleSave]
        public Entity vicePresident;
        [SimpleSave]
        public Entity secretaryOfState;
        [SimpleSave]
        public Entity attorneyGeneral;
        [SimpleSave]
        public Entity ceo;

        public Dictionary<string, Law> laws { get; set; }
        public List<Entity> supremeCourt { get; set; }        
        public Dictionary<string, List<Alignment>> senate { get; set; }
        public Dictionary<string, List<Alignment>> house { get; set; }

        public readonly int senateNum;
        public readonly int houseNum;

        private bool dirtyCongress = true;

        public Government()
        {
            laws = new Dictionary<string, Law>();
            supremeCourt = new List<Entity>();
            senate = new Dictionary<string, List<Alignment>>();
            house = new Dictionary<string, List<Alignment>>();

            senateNum = 0;
            houseNum = 0;

            foreach(NationDef.StateDef state in GameData.getData().nationList["USA"].states)
            {
                if ((state.flags & NationDef.stateFlags.NONSTATE) != 0) continue;

                senateNum += 2;
                houseNum += state.congress;
            }
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Government");
                entityNode.AppendChild(saveNode);

                foreach (string state in senate.Keys)
                {
                    XmlNode stateNode = saveNode.OwnerDocument.CreateElement("state");
                    XmlAttribute nameAtt = stateNode.OwnerDocument.CreateAttribute("name");
                    stateNode.Attributes.Append(nameAtt);
                    nameAtt.Value = state;
                    saveNode.AppendChild(stateNode);

                    for(int i=0;i<senate[state].Count;i++)
                    {
                        XmlNode senatorNode = stateNode.OwnerDocument.CreateElement("senator");
                        stateNode.AppendChild(senatorNode);
                    }
                    for(int i=0;i<house[state].Count;i++)
                    {
                        XmlNode congressNode = stateNode.OwnerDocument.CreateElement("congressperson");
                        stateNode.AppendChild(congressNode);
                    }
                }
            }

            saveSimpleFields();

            if (saveNode.SelectSingleNode("supremeCourt") != null)
                saveNode.RemoveChild(saveNode.SelectSingleNode("supremeCourt"));

            XmlNode supremeCourtNode = saveNode.OwnerDocument.CreateElement("supremeCourt");
            saveNode.AppendChild(supremeCourtNode);

            foreach (Entity e in supremeCourt)
            {
                XmlNode supNode = saveNode.OwnerDocument.CreateElement("justice");
                supNode.InnerText = e.guid.ToString();
                supremeCourtNode.AppendChild(supNode);
            }
            saveField(presidentTerm, "presidentTerm", saveNode);
            foreach (Law l in laws.Values)
                l.save(saveNode);

            if (dirtyCongress)
            {
                foreach (XmlNode stateNode in saveNode.SelectNodes("state"))
                {
                    string stateName = stateNode.Attributes["name"].Value;
                    XmlNodeList senateNodes = stateNode.SelectNodes("senator");
                    XmlNodeList houseNodes = stateNode.SelectNodes("congressperson");

                    for (int i = 0; i < senate[stateName].Count; i++)
                        senateNodes[i].InnerText = senate[stateName][i].ToString();
                    for (int i = 0; i < house[stateName].Count; i++)
                        houseNodes[i].InnerText = house[stateName][i].ToString();
                }

                dirtyCongress = false;
            }
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);

            foreach (XmlNode node in componentData.SelectSingleNode("supremeCourt").ChildNodes)
            {
                try
                {
                    supremeCourt.Add(entityList[int.Parse(node.InnerText)]);
                }
                catch (KeyNotFoundException)
                {
                    MasterController.GetMC().addErrorMessage("Entity reference " + int.Parse(node.InnerText) + " not found on object " + owner.def + ":" + componentData.ParentNode.Attributes["guid"].Value + ":" + componentData.Name + ":supremeCourt");
                }
            }
            foreach (XmlNode node in componentData.SelectNodes("Law"))
                laws.Add(node.Attributes["type"].Value, new Law(node.Attributes["type"].Value, (Alignment)Enum.Parse(typeof(Alignment), node.Attributes["alignment"].Value)));
            foreach(XmlNode node in componentData.SelectNodes("state"))
            {
                senate.Add(node.Attributes["name"].Value, new List<Alignment>());
                house.Add(node.Attributes["name"].Value, new List<Alignment>());
                foreach (XmlNode innerNode in node.SelectNodes("senator"))
                    senate[node.Attributes["name"].Value].Add((Alignment)Enum.Parse(typeof(Alignment), innerNode.InnerText));
                foreach (XmlNode innerNode in node.SelectNodes("congressperson"))
                    house[node.Attributes["name"].Value].Add((Alignment)Enum.Parse(typeof(Alignment), innerNode.InnerText));
            }
        }

        public class Law
        {
            public string type;
            public Alignment alignment;

            private XmlNode saveNode;

            public Law(string type, Alignment alignment)
            {
                this.type = type;
                this.alignment = alignment;
            }

            public void save(XmlNode node)
            {
                if (saveNode == null)
                {
                    saveNode = node.OwnerDocument.CreateElement("Law");
                    XmlAttribute typeAtt = saveNode.OwnerDocument.CreateAttribute("type");
                    XmlAttribute alignmentAtt = saveNode.OwnerDocument.CreateAttribute("alignment");
                    saveNode.Attributes.Append(typeAtt);
                    saveNode.Attributes.Append(alignmentAtt);
                    node.AppendChild(saveNode);
                }

                saveNode.Attributes["type"].Value = type;
                saveNode.Attributes["alignment"].Value = alignment.ToString();
            }
        }

        public Entity primary(Alignment party, bool presidentCanRun = true)
        {
            Alignment candidateAlignment = Alignment.MODERATE;

            Dictionary<Alignment, int> totalVotes = new Dictionary<Alignment, int>();
            
            totalVotes[Alignment.ARCHCONSERVATIVE] = 0;
            totalVotes[Alignment.CONSERVATIVE] = 0;
            totalVotes[Alignment.MODERATE] = 0;
            totalVotes[Alignment.LIBERAL] = 0;
            totalVotes[Alignment.ELITE_LIBERAL] = 0;

            int approvePres = 0;
            int approveVP = 0;

            for(int i = 0; i < 100; i++)
            {
                Alignment voter = MasterController.generalPublic.getSimpleVoter(party);

                if (voter == president.getComponent<Politician>().alignment ||
                    (Math.Abs(president.getComponent<Politician>().alignment - voter) == 1 && MasterController.GetMC().LCSRandom(2) == 0))
                    approvePres++;
                if (voter == vicePresident.getComponent<Politician>().alignment ||
                    (Math.Abs(vicePresident.getComponent<Politician>().alignment - voter) == 1 && MasterController.GetMC().LCSRandom(3) == 0))
                    approveVP++;

                totalVotes[voter]++;
            }

            foreach(Alignment alignment in totalVotes.Keys)
            {
                if (totalVotes[alignment] > totalVotes[candidateAlignment])
                    candidateAlignment = alignment;
            }

            //Create a candidate based on the result
            Entity candidate;

            if (candidateAlignment == Alignment.ARCHCONSERVATIVE) candidate = Factories.CreatureFactory.create("EXECUTIVE_ARCHCONSERVATIVE");
            else if (candidateAlignment == Alignment.CONSERVATIVE) candidate = Factories.CreatureFactory.create("EXECUTIVE_CONSERVATIVE");
            else
            {
                candidate = Factories.CreatureFactory.create("EXECUTIVE_NONCONSERVATIVE");
                //Need to manually set their alignment since the factory just picks randomly
                if (candidateAlignment == Alignment.MODERATE) candidate.getComponent<CreatureInfo>().alignment = Alignment.MODERATE;
                else candidate.getComponent<CreatureInfo>().alignment = Alignment.LIBERAL;
            }

            Politician politicianComponent = new Politician();
            politicianComponent.position = "PRESIDENT";
            politicianComponent.alignment = candidateAlignment;
            politicianComponent.party = party;
            candidate.setComponent(politicianComponent);            

            //Special incumbent rules - if the president has a high approval rating within their party (and is allowed to run), they win automatically.
            //They also win the primary if their alignment matches the one the party chose anyway
            if (presidentCanRun && president.getComponent<Politician>().party == party)
            {
                if (approvePres >= 50) candidate = president;
                else if (president.getComponent<Politician>().alignment == candidateAlignment) candidate = president;
            }

            if(candidate != president && vicePresident.getComponent<Politician>().party == party)
            {
                if (approveVP >= 50) candidate = vicePresident;
            }

            if(candidate == president)
                candidate.getComponent<CreatureInfo>().encounterName = "President " + candidate.getComponent<CreatureInfo>().surname;
            else if(candidate == vicePresident)
                candidate.getComponent<CreatureInfo>().encounterName = "Vice President " + candidate.getComponent<CreatureInfo>().surname;
            else
            {
                if(MasterController.GetMC().LCSRandom(2) == 0)
                    candidate.getComponent<CreatureInfo>().encounterName = "Governor " + candidate.getComponent<CreatureInfo>().surname;
                else if (MasterController.GetMC().LCSRandom(2) == 0)
                    candidate.getComponent<CreatureInfo>().encounterName = "Senator " + candidate.getComponent<CreatureInfo>().surname;
                else if (MasterController.GetMC().LCSRandom(2) == 0)
                    candidate.getComponent<CreatureInfo>().encounterName = "Ret. General " + candidate.getComponent<CreatureInfo>().surname;
                else if (MasterController.GetMC().LCSRandom(2) == 0)
                    candidate.getComponent<CreatureInfo>().encounterName = "Representative " + candidate.getComponent<CreatureInfo>().surname;
                else
                {
                    if (candidate.getComponent<CreatureInfo>().alignment == Alignment.LIBERAL)
                    {
                        if (candidate.getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.FEMALE)
                            candidate.getComponent<CreatureInfo>().encounterName = "Mrs. " + candidate.getComponent<CreatureInfo>().surname;
                        else if (candidate.getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.MALE ||
                            candidate.getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.WHITEMALEPATRIARCH)
                            candidate.getComponent<CreatureInfo>().encounterName = "Mr. " + candidate.getComponent<CreatureInfo>().surname;
                        else
                            candidate.getComponent<CreatureInfo>().encounterName = "Mx. " + candidate.getComponent<CreatureInfo>().surname;
                    }
                    else
                    {
                        if (candidate.getComponent<CreatureInfo>().genderConservative == CreatureInfo.CreatureGender.FEMALE)
                            candidate.getComponent<CreatureInfo>().encounterName = "Mrs. " + candidate.getComponent<CreatureInfo>().surname;
                        else if (candidate.getComponent<CreatureInfo>().genderConservative == CreatureInfo.CreatureGender.MALE ||
                            candidate.getComponent<CreatureInfo>().genderConservative == CreatureInfo.CreatureGender.WHITEMALEPATRIARCH)
                            candidate.getComponent<CreatureInfo>().encounterName = "Mr. " + candidate.getComponent<CreatureInfo>().surname;
                        else
                            candidate.getComponent<CreatureInfo>().encounterName = "Mx. " + candidate.getComponent<CreatureInfo>().surname;
                    }
                }
            }

            return candidate;
        }

        public void presidentialElection()
        {
            NationDef nation = GameData.getData().nationList["USA"];
            MasterController mc = MasterController.GetMC();

            bool presCanRun = presidentTerm < 2;
            Entity liberalCandidate = primary(Alignment.LIBERAL, presCanRun);
            Entity conservativeCandidate = primary(Alignment.CONSERVATIVE, presCanRun);

            PresidentialElectionResult result = new PresidentialElectionResult();

            //Depersist the old executive (although if the president wins re-election they may get added back again)
            president.depersist();
            vicePresident.depersist();
            secretaryOfState.depersist();
            attorneyGeneral.depersist();

            result.liberalCandidateRunningName = liberalCandidate.getComponent<CreatureInfo>().encounterName;
            result.liberalCandidate = liberalCandidate;
            result.conservativeCandidateRunningName = conservativeCandidate.getComponent<CreatureInfo>().encounterName;
            result.conservativeCandidate = conservativeCandidate;

            int liberalElectoralVotes = 0;
            int conservativeElectoralVotes = 0;
            int liberalPopularVote = 0;
            int conservativePopularVote = 0;

            foreach(NationDef.StateDef state in nation.states)
            {
                int libVote = 0;
                int conVote = 0;

                for(int i = 0; i < 200; i++)
                {
                    int bias = getStateBias(state);

                    //Partyline Liberals (~25%)
                    if (i % 2 == 0 && mc.LCSRandom(2) != 0) libVote++;
                    //Partyline Conservatives (~25%)
                    else if (i % 2 == 1 && mc.LCSRandom(2) != 0) conVote++;
                    else {
                        Alignment vote = MasterController.generalPublic.getSwingVoter(bias);

                        //If the voter is as or more liberal than the liberal party candidate, and disagrees with the conservative party candidate, vote for the liberal.
                        if (vote >= liberalCandidate.getComponent<Politician>().alignment && vote != conservativeCandidate.getComponent<Politician>().alignment)
                            libVote++;
                        //If the voter is as or more conservative than the conservative party candidate, and disagrees with the liberal party candidate, vote for the conservative.
                        else if (vote <= conservativeCandidate.getComponent<Politician>().alignment && vote != liberalCandidate.getComponent<Politician>().alignment)
                            conVote++;
                        //If they don't really agree with either candidate, just pick randomly. They may also vote 3rd party (which functionally just means they don't vote)
                        else
                        {
                            switch (mc.LCSRandom(3))
                            {
                                case 0:
                                    libVote++;
                                    break;
                                case 1:
                                    conVote++;
                                    break;
                            }
                        }
                    }
                }

                //distribute the votes!
                liberalPopularVote += (int) (state.population * (libVote / 200f));
                conservativePopularVote += (int)(state.population * (conVote / 200f));

                int[] voteArray = { libVote, conVote };

                result.statePopularResults.Add(state.name, voteArray);

                if((state.flags & NationDef.stateFlags.PROPORTIONAL_ELECTORAL_VOTES) != 0)
                {
                    result.stateRecounts.Add(state.name, false);

                    int stateLiberalElectoralVotes = 0;
                    int stateConservativeElectoralVotes = 0;

                    for (int i = 0; i < state.electoralVotes; i++)
                    {
                        if(libVote > conVote)
                        {
                            stateLiberalElectoralVotes++;
                            libVote -= 200 / state.electoralVotes;
                        }
                        else if(conVote > libVote)
                        {
                            stateConservativeElectoralVotes++;
                            conVote -= 200 / state.electoralVotes;
                        }
                        else
                        {
                            if(mc.LCSRandom(2) == 0)
                            {
                                stateLiberalElectoralVotes++;
                                libVote -= 200 / state.electoralVotes;
                            }
                            else
                            {
                                stateConservativeElectoralVotes++;
                                conVote -= 200 / state.electoralVotes;
                            }
                        }
                    }

                    liberalElectoralVotes += stateLiberalElectoralVotes;
                    conservativeElectoralVotes += stateConservativeElectoralVotes;

                    int[] resultArray = { stateLiberalElectoralVotes, stateConservativeElectoralVotes };
                    result.stateSpecificResults.Add(state.name, resultArray);
                }
                else
                {
                    result.stateRecounts.Add(state.name, false);

                    if (libVote > conVote)
                    {
                        liberalElectoralVotes += state.electoralVotes;
                        result.stateResults.Add(state.name, Alignment.LIBERAL);
                    }
                    else if (conVote > libVote)
                    {
                        conservativeElectoralVotes += state.electoralVotes;
                        result.stateResults.Add(state.name, Alignment.CONSERVATIVE);
                    }
                    else
                    {
                        if (mc.LCSRandom(2) == 0)
                        {
                            liberalElectoralVotes += state.electoralVotes;
                            result.stateResults.Add(state.name, Alignment.LIBERAL);
                        }
                        else
                        {
                            conservativeElectoralVotes += state.electoralVotes;
                            result.stateResults.Add(state.name, Alignment.CONSERVATIVE);
                        }

                        result.stateRecounts[state.name] = true;
                    }
                }
            }

            //All the votes are in, get the final tally!
            if(liberalElectoralVotes > conservativeElectoralVotes)
            {
                if (liberalCandidate == president) presidentTerm++;
                else
                {
                    president = liberalCandidate;
                    vicePresident = getCabinetMember();
                    vicePresident.getComponent<Politician>().position = "VICE_PRESIDENT";
                    vicePresident.getComponent<CreatureInfo>().encounterName = "Vice President";
                    presidentTerm = 1;
                }
            }
            else if(conservativeElectoralVotes > liberalElectoralVotes)
            {
                if (conservativeCandidate == president) presidentTerm++;
                else
                {
                    president = conservativeCandidate;
                    vicePresident = getCabinetMember();
                    vicePresident.getComponent<Politician>().position = "VICE_PRESIDENT";
                    vicePresident.getComponent<CreatureInfo>().encounterName = "Vice President";
                    presidentTerm = 1;
                }
            }
            else
            {
                //Dear America: Your system is stupid.

                //Pick the President first, because in this system, of course the VP is chosen by a separate vote in this specific case only
                int conservativeStateVotes = 0;
                int liberalStateVotes = 0;

                foreach(string state in house.Keys)
                {
                    int libVote = 0;
                    int conVote = 0;

                    foreach(Alignment vote in house[state])
                    {
                        if (vote >= liberalCandidate.getComponent<Politician>().alignment && vote != conservativeCandidate.getComponent<Politician>().alignment)
                            libVote++;
                        else if (vote <= conservativeCandidate.getComponent<Politician>().alignment && vote != liberalCandidate.getComponent<Politician>().alignment)
                            conVote++;
                        else
                        {
                            if (mc.LCSRandom(2) != 0) libVote++;
                            else conVote++;
                        }
                    }

                    if (libVote > conVote)
                    {
                        liberalStateVotes++;
                        result.stateTiebreakerResults[state] = Alignment.LIBERAL;
                    }
                    else if (conVote > libVote)
                    {
                        conservativeStateVotes++;
                        result.stateTiebreakerResults[state] = Alignment.CONSERVATIVE;
                    }
                    else
                    {
                        //ANOTHER tie???
                        if (mc.LCSRandom(2) == 0)
                        {
                            liberalStateVotes++;
                            result.stateTiebreakerResults[state] = Alignment.LIBERAL;
                        }
                        else
                        {
                            conservativeStateVotes++;
                            result.stateTiebreakerResults[state] = Alignment.CONSERVATIVE;
                        }
                    }
                }

                if (liberalStateVotes > conservativeStateVotes)
                {
                    if (liberalCandidate == president) presidentTerm++;
                    else
                    {
                        president = liberalCandidate;
                        presidentTerm = 1;
                    }
                }
                else if (conservativeStateVotes > liberalStateVotes)
                {
                    if (conservativeCandidate == president) presidentTerm++;
                    else
                    {
                        president = conservativeCandidate;
                        presidentTerm = 1;
                    }
                }
                else
                {
                    //JESUS CHRIST FLIP A FUCKING COIN
                    if (mc.LCSRandom(2) == 0)
                    {
                        result.winnerName = liberalCandidate.getComponent<CreatureInfo>().encounterName;

                        if (liberalCandidate == president) presidentTerm++;
                        else
                        {
                            president = liberalCandidate;
                            presidentTerm = 1;
                        }
                    }
                    else
                    {
                        result.winnerName = conservativeCandidate.getComponent<CreatureInfo>().encounterName;

                        if (conservativeCandidate == president) presidentTerm++;
                        else
                        {
                            president = conservativeCandidate;
                            presidentTerm = 1;
                        }
                    }
                }

                //Senate picks a VP from two fresh candidates (run a primary again, but without the current president in the running)
                Entity conservativeVPCandidate = primary(Alignment.CONSERVATIVE, false);
                Entity liberalVPCandidate = primary(Alignment.LIBERAL, false);

                conservativeVPCandidate.getComponent<Politician>().position = "VICE_PRESIDENT";
                liberalVPCandidate.getComponent<Politician>().position = "VICE_PRESIDENT";

                int conVPVotes = 0;
                int libVPVotes = 0;

                foreach(string state in senate.Keys)
                {
                    foreach(Alignment vote in senate[state])
                    {
                        if (vote >= liberalVPCandidate.getComponent<Politician>().alignment && vote != conservativeVPCandidate.getComponent<Politician>().alignment)
                            libVPVotes++;
                        else if (vote <= conservativeVPCandidate.getComponent<Politician>().alignment && vote != liberalVPCandidate.getComponent<Politician>().alignment)
                            conVPVotes++;
                        else
                        {
                            if (mc.LCSRandom(2) != 0) libVPVotes++;
                            else conVPVotes++;
                        }
                    }
                }

                if (libVPVotes > conVPVotes)
                {
                    result.VPwinnerName = liberalVPCandidate.getComponent<CreatureInfo>().encounterName;
                    result.VPwinnerAlignment = liberalVPCandidate.getComponent<Politician>().alignment;
                    vicePresident = liberalVPCandidate;
                }
                else if (conVPVotes > libVPVotes)
                {
                    result.VPwinnerName = conservativeVPCandidate.getComponent<CreatureInfo>().encounterName;
                    result.VPwinnerAlignment = conservativeVPCandidate.getComponent<Politician>().alignment;
                    vicePresident = conservativeVPCandidate;
                }
                else
                {
                    if (mc.LCSRandom(2) == 0)
                    {
                        result.VPwinnerName = liberalVPCandidate.getComponent<CreatureInfo>().encounterName;
                        result.VPwinnerAlignment = liberalVPCandidate.getComponent<Politician>().alignment;
                        vicePresident = liberalVPCandidate;
                    }
                    else
                    {
                        result.VPwinnerName = conservativeVPCandidate.getComponent<CreatureInfo>().encounterName;
                        result.VPwinnerAlignment = conservativeVPCandidate.getComponent<Politician>().alignment;
                        vicePresident = conservativeVPCandidate;
                    }
                }

                vicePresident.getComponent<CreatureInfo>().encounterName = "Vice President " + vicePresident.getComponent<CreatureInfo>().surname;
            }
            
            if(presidentTerm == 1)
            {
                secretaryOfState = getCabinetMember();
                secretaryOfState.getComponent<Politician>().position = "SECRETARY_OF_STATE";
                secretaryOfState.getComponent<CreatureInfo>().encounterName = "Secretary of State";
                attorneyGeneral = getCabinetMember();
                attorneyGeneral.getComponent<Politician>().position = "ATTORNEY_GENERAL";
                attorneyGeneral.getComponent<CreatureInfo>().encounterName = "Attorney General";
            }

            president.getComponent<CreatureInfo>().encounterName = "President " + president.getComponent<CreatureInfo>().surname;

            president.persist();
            vicePresident.persist();
            secretaryOfState.persist();
            attorneyGeneral.persist();

            if (mc.canSeeThings)
                mc.uiController.nationMap.showPresidentialElection(result);
            else
                mc.doNextAction();
        }

        public Entity getCabinetMember()
        {
            Alignment presAlign = president.getComponent<Politician>().alignment;

            Entity cabinetMember;

            //Extremes always appoint like-minded cabinet members.
            if(presAlign == Alignment.ARCHCONSERVATIVE)
            {
                cabinetMember = Factories.CreatureFactory.create("EXECUTIVE_ARCHCONSERVATIVE");
                Politician politicianComponent = new Politician();
                politicianComponent.alignment = Alignment.ARCHCONSERVATIVE;
                politicianComponent.party = president.getComponent<Politician>().party;
                cabinetMember.setComponent(politicianComponent);
            }
            else if(presAlign == Alignment.ELITE_LIBERAL)
            {
                cabinetMember = Factories.CreatureFactory.create("EXECUTIVE_NONCONSERVATIVE");
                cabinetMember.getComponent<CreatureInfo>().alignment = Alignment.LIBERAL;
                Politician politicianComponent = new Politician();
                politicianComponent.alignment = Alignment.ELITE_LIBERAL;
                politicianComponent.party = president.getComponent<Politician>().party;
                cabinetMember.setComponent(politicianComponent);
            }
            else
            {
                Alignment cabinetMemberAlignment = (Alignment) MasterController.GetMC().LCSRandom(3) - 1;

                if(cabinetMemberAlignment == Alignment.CONSERVATIVE) cabinetMember = Factories.CreatureFactory.create("EXECUTIVE_CONSERVATIVE");
                else cabinetMember = Factories.CreatureFactory.create("EXECUTIVE_NONCONSERVATIVE");

                cabinetMember.getComponent<CreatureInfo>().alignment = cabinetMemberAlignment;
                Politician politicianComponent = new Politician();
                politicianComponent.alignment = cabinetMemberAlignment;
                politicianComponent.party = president.getComponent<Politician>().party;
                cabinetMember.setComponent(politicianComponent);
            }

            return cabinetMember;
        }

        public void appointNewJustice(bool stepDown = false, string messageText = "")
        {
            MasterController mc = MasterController.GetMC();

            Entity retiree = null;

            if (stepDown)
            {
                Dictionary<Entity, int> retirementOdds = new Dictionary<Entity, int>();
                int courtBalance = 0;

                foreach(Entity judge in supremeCourt)
                {
                    courtBalance += (int) judge.getComponent<Politician>().alignment;
                    retirementOdds.Add(judge, 10);
                }

                foreach(Entity judge in supremeCourt)
                {
                    //Judges who respresent a minority on the court will hold on to their positions longer, especially if they are extreme partisans
                    //ESPECIALLY if the current president is counter-aligned
                    if(courtBalance < 0 && judge.getComponent<Politician>().alignment >= Alignment.LIBERAL)
                    {
                        retirementOdds[judge] -= Math.Abs((int)judge.getComponent<Politician>().alignment * courtBalance);
                        if(president.getComponent<Politician>().alignment <= Alignment.CONSERVATIVE)
                            retirementOdds[judge] -= Math.Abs((int)president.getComponent<Politician>().alignment * 2);
                    }
                    else if(courtBalance > 0 && judge.getComponent<Politician>().alignment <= Alignment.CONSERVATIVE)
                    {
                        retirementOdds[judge] -= Math.Abs((int)judge.getComponent<Politician>().alignment * courtBalance);
                        if (president.getComponent<Politician>().alignment >= Alignment.LIBERAL)
                            retirementOdds[judge] -= Math.Abs((int)president.getComponent<Politician>().alignment * 2);
                    }

                    //Aging justices are more likely to retire
                    if(judge.getComponent<Age>().getAge() >= 65)
                    {
                        retirementOdds[judge] += (judge.getComponent<Age>().getAge() - 65);
                    }

                    if (retirementOdds[judge] <= 0) retirementOdds[judge] = 1;
                }

                retiree = mc.WeightedRandom(retirementOdds);
                removeJustice(retiree);
            }

            float senateConsensus = 0;

            foreach(string state in senate.Keys)
            {
                foreach(Alignment align in senate[state])
                {
                    //Non-Extreme senators can be swayed by public opinion on appointing liberal justices
                    Alignment senateAlign = align;
                    if(senateAlign >= Alignment.CONSERVATIVE && senateAlign <= Alignment.LIBERAL)
                    {
                        if (mc.LCSRandom(100) < MasterController.generalPublic.PublicOpinion[Constants.VIEW_JUSTICES])
                            senateAlign++;
                        if (mc.LCSRandom(100) > MasterController.generalPublic.PublicOpinion[Constants.VIEW_JUSTICES])
                            senateAlign--;
                    }

                    senateConsensus += (int)senateAlign;
                }
            }

            senateConsensus /= senateNum;

            float consensus = ((int)president.getComponent<Politician>().alignment + senateConsensus) / 2;

            Alignment newJusticeAlign;

            if (consensus < -1.5) newJusticeAlign = Alignment.ARCHCONSERVATIVE;
            else if (consensus < -.5) newJusticeAlign = Alignment.CONSERVATIVE;
            else if (consensus < .5) newJusticeAlign = Alignment.MODERATE;
            else if (consensus < 1.5) newJusticeAlign = Alignment.LIBERAL;
            else newJusticeAlign = Alignment.ELITE_LIBERAL;

            Entity newJustice;

            if (newJusticeAlign == Alignment.ARCHCONSERVATIVE || newJusticeAlign == Alignment.CONSERVATIVE) newJustice = Factories.CreatureFactory.create("SUPREME_COURT_CONSERVATIVE");
            else if (newJusticeAlign == Alignment.MODERATE) newJustice = Factories.CreatureFactory.create("SUPREME_COURT_MODERATE");
            else newJustice = Factories.CreatureFactory.create("SUPREME_COURT_LIBERAL");

            Politician politicianComponent = new Politician();
            politicianComponent.position = "SUPREME_COURT";
            politicianComponent.alignment = newJusticeAlign;
            newJustice.setComponent(politicianComponent);
            newJustice.getComponent<CreatureInfo>().encounterName = "Justice " + newJustice.getComponent<CreatureInfo>().surname;
            newJustice.persist();
            supremeCourt.Add(newJustice);

            string color;

            if (retiree != null)
            {
                color = UIControllerImpl.getAlignmentColor(retiree.getComponent<Politician>().alignment);
                messageText += color + "Justice " + retiree.getComponent<CreatureInfo>().getName() + "</color> is stepping down.\n";
            }

            color = UIControllerImpl.getAlignmentColor(newJusticeAlign);

            messageText += "After much debate and televised testimony, a new justice, " + color +"the Honorable " + newJustice.getComponent<CreatureInfo>().getName() + "</color>, is appointed to the bench.";
            mc.addMessage(messageText, true);
        }

        public void removeJustice(Entity justice)
        {
            supremeCourt.Remove(justice);
            justice.depersist();
        }

        public void forceNewJustice(Alignment newJusticeAlign)
        {
            Entity newJustice;

            if (newJusticeAlign == Alignment.ARCHCONSERVATIVE || newJusticeAlign == Alignment.CONSERVATIVE) newJustice = Factories.CreatureFactory.create("SUPREME_COURT_CONSERVATIVE");
            else if (newJusticeAlign == Alignment.MODERATE) newJustice = Factories.CreatureFactory.create("SUPREME_COURT_MODERATE");
            else newJustice = Factories.CreatureFactory.create("SUPREME_COURT_LIBERAL");

            Politician politicianComponent = new Politician();
            politicianComponent.position = "SUPREME_COURT";
            politicianComponent.alignment = newJusticeAlign;
            newJustice.setComponent(politicianComponent);
            newJustice.getComponent<CreatureInfo>().encounterName = "Justice " + newJustice.getComponent<CreatureInfo>().surname;
            newJustice.persist();
            supremeCourt.Add(newJustice);
        }

        public int getHouseCount(Alignment align)
        {
            int count = 0;

            foreach(string state in house.Keys)
            {
                foreach(Alignment test in house[state])
                {
                    if (test == align)
                        count++;
                }
            }

            return count;
        }

        public int getSenateCount(Alignment align)
        {
            int count = 0;

            foreach (string state in senate.Keys)
            {
                foreach (Alignment test in senate[state])
                {
                    if (test == align)
                        count++;
                }
            }

            return count;
        }

        public void midtermElection()
        {
            if (MasterController.GetMC().canSeeThings)
            {
                MasterController.GetMC().uiController.closeUI();
                MasterController.GetMC().uiController.election.show();
            }
            else
            {
                houseElection();
                senateElection();
                MasterController.GetMC().doNextAction();
            }
        }

        public void senateElection(bool termlimits = false)
        {
            dirtyCongress = true;
            NationDef nation = GameData.getData().nationList["USA"];

            int senmod = (MasterController.GetMC().currentDate.Year % 6) / 2;
            Public pub = MasterController.generalPublic;

            int k = -1;

            foreach (NationDef.StateDef state in nation.states)
            {
                if ((state.flags & NationDef.stateFlags.NONSTATE) != 0) continue;

                for (int i = 0; i < senate[state.name].Count; i++)
                {
                    k++;
                    if (k % 3 != senmod) continue;

                    int bias = getStateBias(state);

                    Alignment vote = pub.getSwingVoter(bias);

                    if (termlimits) senate[state.name][i] = vote;
                    else
                    {
                        switch (laws["ELECTION"].alignment)
                        {
                            // 2/3 chance of incumbent winning regardless of public mood.
                            case Alignment.ARCHCONSERVATIVE:
                                if (MasterController.GetMC().LCSRandom(3) == 0) senate[state.name][i] = vote;
                                break;
                            // 1/2 chance of incumbent winning regardless of public mood.
                            case Alignment.CONSERVATIVE:
                                if (MasterController.GetMC().LCSRandom(2) == 0) senate[state.name][i] = vote;
                                break;
                            // 1/3 chance of incumbent winning regardless of public mood.
                            case Alignment.MODERATE:
                                if (MasterController.GetMC().LCSRandom(3) != 0) senate[state.name][i] = vote;
                                break;
                            // 1/5 chance of incumbent winning regardless of public mood.
                            case Alignment.LIBERAL:
                                if (MasterController.GetMC().LCSRandom(5) == 0) senate[state.name][i] = vote;
                                break;
                            // 1/8 chance of incumbent winning regardless of public mood.
                            case Alignment.ELITE_LIBERAL:
                                if (MasterController.GetMC().LCSRandom(8) == 0) senate[state.name][i] = vote;
                                break;
                        }
                    }
                }
            }
        }

        public void houseElection(bool termlimits = false)
        {
            dirtyCongress = true;
            
            Public pub = MasterController.generalPublic;
            NationDef nation = GameData.getData().nationList["USA"];

            foreach (NationDef.StateDef state in nation.states)
            {
                if ((state.flags & NationDef.stateFlags.NONSTATE) != 0) continue;

                for (int i = 0; i < house[state.name].Count; i++)
                {
                    int bias = getStateBias(state);
                    Alignment vote = pub.getSwingVoter(bias);

                    if (termlimits) house[state.name][i] = vote;
                    else
                    {
                        switch (laws["ELECTION"].alignment)
                        {
                            // 2/3 chance of incumbent winning regardless of public mood.
                            case Alignment.ARCHCONSERVATIVE:
                                if (MasterController.GetMC().LCSRandom(3) == 0) house[state.name][i] = vote;
                                break;
                            // 1/2 chance of incumbent winning regardless of public mood.
                            case Alignment.CONSERVATIVE:
                                if (MasterController.GetMC().LCSRandom(2) == 0) house[state.name][i] = vote;
                                break;
                            // 1/3 chance of incumbent winning regardless of public mood.
                            case Alignment.MODERATE:
                                if (MasterController.GetMC().LCSRandom(3) != 0) house[state.name][i] = vote;
                                break;
                            // 1/5 chance of incumbent winning regardless of public mood.
                            case Alignment.LIBERAL:
                                if (MasterController.GetMC().LCSRandom(5) == 0) house[state.name][i] = vote;
                                break;
                            // 1/8 chance of incumbent winning regardless of public mood.
                            case Alignment.ELITE_LIBERAL:
                                if (MasterController.GetMC().LCSRandom(8) == 0) house[state.name][i] = vote;
                                break;
                        }
                    }
                }
            }
        }

        //November election day
        public void propElections()
        {
            List<PropositionResult> propResults = new List<PropositionResult>();
            MasterController mc = MasterController.GetMC();

            Dictionary<string, Alignment> lawDir = new Dictionary<string, Alignment>();
            Dictionary<string, int> lawPriority = new Dictionary<string, int>();
            List<string> sortedLaws = new List<string>();
            List<string> chosenLaws = new List<string>();

            int pnum = mc.LCSRandom(4) + 4;

            foreach (string law in laws.Keys)
            {
                int mood = getComponent<Public>().getLawOpinion(law);
                int interest = getComponent<Public>().getLawInterest(law);

                Alignment pvote = Alignment.ARCHCONSERVATIVE;
                for (int i = 0; i < 4; i++) if (mc.LCSRandom(100) < mood) pvote++;

                if (laws[law].alignment < pvote) lawDir[law] = Alignment.LIBERAL;
                else if (laws[law].alignment >= pvote) lawDir[law] = Alignment.CONSERVATIVE;

                if (laws[law].alignment == Alignment.ARCHCONSERVATIVE) lawDir[law] = Alignment.LIBERAL;
                else if (laws[law].alignment == Alignment.ELITE_LIBERAL) lawDir[law] = Alignment.CONSERVATIVE;

                int priority = Math.Abs((((int)laws[law].alignment + 2) * 25) - mood);

                lawPriority[law] = priority + mc.LCSRandom(10) + interest;
            }

            sortedLaws.AddRange(lawPriority.Keys);
            sortedLaws.Sort((string x, string y) => { return lawPriority[x].CompareTo(lawPriority[y]); });

            int lowestLaw = pnum - 1;

            while (lowestLaw < sortedLaws.Count)
            {
                if (lawPriority[sortedLaws[lowestLaw]] < lawPriority[sortedLaws[lowestLaw - 1]])
                {
                    lowestLaw--;
                    break;
                }

                lowestLaw++;
            }

            while (lowestLaw + 1 < sortedLaws.Count)
            {
                sortedLaws.RemoveAt(lowestLaw + 1);
            }

            List<int> propNums = new List<int>();

            for (int i = 0; i < pnum; i++)
            {
                int propnum = mc.LCSRandom(10000);
                //While it is incredibly unlikely that it will roll two identical proposition numbers, we should check just to make sure.
                while (propNums.Contains(propnum)) propnum = mc.LCSRandom(10000);

                propNums.Add(propnum);
            }

            propNums.Sort();

            for (int i = 0; i < pnum; i++)
            {
                chosenLaws.Add(sortedLaws[mc.LCSRandom(sortedLaws.Count)]);
                sortedLaws.Remove(chosenLaws[i]);

                int yesVotes = propElection(chosenLaws[i], lawDir[chosenLaws[i]]);
                propResults.Add(new PropositionResult(propNums[i], lawDir[chosenLaws[i]], chosenLaws[i], yesVotes));
            }

            if (mc.canSeeThings)
            {
                mc.uiController.closeUI();
                mc.uiController.law.show(propResults);
            }
            else
            {
                mc.doNextAction();
            }
        }

        private int propElection(string law, Alignment direction)
        {
            int yesVotes = 0;

            int mood = MasterController.generalPublic.getLawOpinion(law);

            for(int i = 0; i < 1000; i++)
            {
                if (MasterController.GetMC().LCSRandom(100) < mood ? direction == Alignment.LIBERAL : direction == Alignment.CONSERVATIVE)
                    yesVotes++;
            }

            if(yesVotes > 500)
            {
                shiftLaw(law, direction);
            }
            else if(yesVotes == 500)
            {
                if(MasterController.GetMC().LCSRandom(2) == 0)
                {
                    shiftLaw(law, direction);
                    yesVotes++;
                }
            }

            return yesVotes;
        }

        //March and September 1st
        public void congressBills()
        {
            MasterController mc = MasterController.GetMC();
            List<CongressBillResult>  billResults = new List<CongressBillResult>();

            Dictionary<string, Alignment> lawDir = new Dictionary<string, Alignment>();
            Dictionary<string, int> lawPriority = new Dictionary<string, int>();
            List<string> sortedLaws = new List<string>();
            List<string> chosenLaws = new List<string>();

            int cnum = mc.LCSRandom(3) + 4;

            foreach (string law in laws.Keys)
            {
                int pup = 0;
                int pdown = 0;
                int pprior = 0;

                foreach (string state in house.Keys)
                {
                    //Consult House
                    foreach (Alignment align in house[state])
                    {
                        if (laws[law].alignment < align) pup++;
                        else if (laws[law].alignment > align) pdown++;
                        pprior += Math.Abs(align - laws[law].alignment);
                    }

                    //Consult Senate
                    foreach (Alignment align in senate[state])
                    {
                        if (laws[law].alignment < align) pup += 4;
                        else if (laws[law].alignment > align) pdown += 4;
                        pprior += Math.Abs(align - laws[law].alignment) * 4;
                    }

                    //Consult Public Opinion
                    int mood = getComponent<Public>().getLawOpinion(law);
                    Alignment publicPosition = Alignment.ARCHCONSERVATIVE;

                    for (int i = 0; i < 4; i++) if (10 + (20 * i) < mood) publicPosition++;

                    //Public opinion doesn't have a huge impact on the direction of bills, but does heavily influence priority
                    if (laws[law].alignment < publicPosition) pup += 50;
                    else if (laws[law].alignment > publicPosition) pdown += 50;
                    pprior += Math.Abs(publicPosition - laws[law].alignment) * 600;
                }

                if (pup > pdown) lawDir[law] = Alignment.LIBERAL;
                else if (pup == pdown)
                {
                    if (mc.LCSRandom(2) == 0) lawDir[law] = Alignment.LIBERAL;
                    else lawDir[law] = Alignment.CONSERVATIVE;
                }
                else lawDir[law] = Alignment.CONSERVATIVE;

                if (laws[law].alignment == Alignment.ARCHCONSERVATIVE) lawDir[law] = Alignment.LIBERAL;
                if (laws[law].alignment == Alignment.ELITE_LIBERAL) lawDir[law] = Alignment.CONSERVATIVE;

                lawPriority[law] = pprior;
            }

            sortedLaws.AddRange(lawPriority.Keys);
            sortedLaws.Sort((string x, string y) => { return lawPriority[x].CompareTo(lawPriority[y]); });

            int lowestBill = cnum;

            while (lowestBill < sortedLaws.Count)
            {
                if (lawPriority[sortedLaws[lowestBill]] < lawPriority[sortedLaws[lowestBill - 1]])
                {
                    lowestBill--;
                    break;
                }

                lowestBill++;
            }

            while (lowestBill + 1 < sortedLaws.Count)
            {
                sortedLaws.RemoveAt(lowestBill + 1);
            }

            for (int i = 0; i < cnum; i++)
            {
                chosenLaws.Add(sortedLaws[mc.LCSRandom(sortedLaws.Count)]);
                sortedLaws.Remove(chosenLaws[i]);

                CongressBillResult result = new CongressBillResult(chosenLaws[i], lawDir[chosenLaws[i]]);
                congressVote(chosenLaws[i], lawDir[chosenLaws[i]], result);
                billResults.Add(result);
            }

            if (mc.canSeeThings)
            {
                mc.uiController.closeUI();
                mc.uiController.law.show(billResults);
            }
            else
            {
                mc.doNextAction();
            }
        }

        private void congressVote(string law, Alignment direction, CongressBillResult result)
        {
            result.houseYesVotes = 0;
            result.senateYesVotes = 0;
            result.presidentVeto = false;
            //A VPVote of "true" means they vote in favour of the bill, "false" against. It doesn't matter unless the senate ties, anyway.
            result.vpVote = false;

            foreach(string state in house.Keys)
            {
                foreach(Alignment align in house[state])
                {
                    Alignment vote = determinePoliticianVote(align, law);
                    if (direction == Alignment.CONSERVATIVE && laws[law].alignment > vote) result.houseYesVotes++;
                    else if (direction == Alignment.LIBERAL && laws[law].alignment < vote) result.houseYesVotes++;
                }

                foreach(Alignment align in senate[state])
                {
                    Alignment vote = determinePoliticianVote(align, law);
                    if (direction == Alignment.CONSERVATIVE && laws[law].alignment > vote) result.senateYesVotes++;
                    else if (direction == Alignment.LIBERAL && laws[law].alignment < vote) result.senateYesVotes++;
                }
            }

            //In the event of a senate tie, does the VP support the bill?
            Alignment VPVote;

            if (vicePresident.getComponent<Politician>().alignment == Alignment.ARCHCONSERVATIVE ||
                vicePresident.getComponent<Politician>().alignment == Alignment.ELITE_LIBERAL) VPVote = vicePresident.getComponent<Politician>().alignment;
            else
            {
                VPVote = (Alignment) (((int)president.getComponent<Politician>().alignment +
                    (int)vicePresident.getComponent<Politician>().alignment +
                    (int)secretaryOfState.getComponent<Politician>().alignment +
                    (int)attorneyGeneral.getComponent<Politician>().alignment +
                    MasterController.GetMC().LCSRandom(9) - 4) / 4);
            }

            if (result.senateYesVotes == senateNum / 2)
            {
                if (direction == Alignment.CONSERVATIVE && laws[law].alignment >= VPVote) result.vpVote = true;
                else if (direction == Alignment.LIBERAL && laws[law].alignment <= VPVote) result.vpVote = true;
            }

            //If the bill passes, will the president veto?
            //If the VP supports the bill, and they are the same party as the president, the president will sign.
            if (result.vpVote && president.getComponent<Politician>().party == vicePresident.getComponent<Politician>().party) result.presidentVeto = false;
            else
            {
                Alignment presVote;

                if (president.getComponent<Politician>().alignment == Alignment.ARCHCONSERVATIVE ||
                president.getComponent<Politician>().alignment == Alignment.ELITE_LIBERAL) presVote = president.getComponent<Politician>().alignment;
                else
                {
                    presVote = (Alignment)(((int)president.getComponent<Politician>().alignment +
                        (int)vicePresident.getComponent<Politician>().alignment +
                        (int)secretaryOfState.getComponent<Politician>().alignment +
                        (int)attorneyGeneral.getComponent<Politician>().alignment +
                        MasterController.GetMC().LCSRandom(9) - 4) / 4);
                }

                //President will veto if they can (this is ignored if the bill passed with a veto-proof majority)
                if (direction == Alignment.CONSERVATIVE && laws[law].alignment <= presVote) result.presidentVeto = true;
                else if (direction == Alignment.LIBERAL && laws[law].alignment >= presVote) result.presidentVeto = true;
            }

            if(result.houseYesVotes > houseNum/2 && (result.senateYesVotes > senateNum / 2 || (result.senateYesVotes == senateNum / 2 && result.vpVote)))
            {
                //Bill passes, no extra checks needed
                if (!result.presidentVeto)
                {
                    shiftLaw(law, direction);
                }
                //President vetoed bill - does congress have the power to override?
                else
                {
                    if(result.houseYesVotes >= houseNum*(2/3f) && result.senateYesVotes >= senateNum * (2 / 3f))
                    {
                        shiftLaw(law, direction);
                    }
                }
            }
        }

        public bool winCheck()
        {
            foreach(Law law in laws.Values)
            {
                if (law.alignment < Alignment.ELITE_LIBERAL) return false;
            }

            bool haveHouse = houseWinCheck();
            bool haveSenate = senateWinCheck();

            int judgeCount = 0;
            foreach(Entity judge in supremeCourt)
            {
                if (judge.getComponent<Politician>().alignment == Alignment.ELITE_LIBERAL) judgeCount++;
            }

            if (!(judgeCount > supremeCourt.Count / 2) || !haveHouse || !haveSenate) return false;

            return true;
        }

        public bool houseWinCheck()
        {
            //Need a bare minimum of 50% of the house to be elite liberal to win
            if (getHouseCount(Alignment.ELITE_LIBERAL) < houseNum / 2) return false;
            //Need elite liberals + half the count of moderate liberals to be a comfortable 3/5th majority to win
            if (getHouseCount(Alignment.ELITE_LIBERAL) + getHouseCount(Alignment.LIBERAL) / 2 >= houseNum * (3f / 5f)) return true;

            return false;
        }

        public bool senateWinCheck()
        {
            //Need a bare minimum of 50% of the senate to be elite liberal to win
            if (getSenateCount(Alignment.ELITE_LIBERAL) < senateNum / 2) return false;
            //Need elite liberals + half the count of moderate liberals to be a comfortable 3/5th majority to win
            if (getSenateCount(Alignment.ELITE_LIBERAL) + getSenateCount(Alignment.LIBERAL) / 2 >= senateNum * (3f / 5f)) return true;

            return false;
        }

        public AmendmentResult ratify(Alignment align, bool congressNeeded = true)
        {
            bool ratified = false;
            bool congressRatified = false;
            int congressYesVotes = 0;
            int senateYesVotes = 0;
            int stateYesVotes = 0;
            NationDef nation = GameData.getData().nationList["USA"];
            MasterController mc = MasterController.GetMC();

            AmendmentResult result = new AmendmentResult();

            if (congressNeeded)
            {
                foreach(NationDef.StateDef state in nation.states)
                {
                    if ((state.flags & NationDef.stateFlags.NONSTATE) != 0) continue;

                    foreach(Alignment a in house[state.name])
                    {
                        Alignment vote = a;
                        if (vote >= Alignment.CONSERVATIVE && vote <= Alignment.LIBERAL)
                            vote += mc.LCSRandom(3) - 1;

                        if (vote == align) congressYesVotes++;
                    }
                }

                foreach (NationDef.StateDef state in nation.states)
                {
                    if ((state.flags & NationDef.stateFlags.NONSTATE) != 0) continue;

                    foreach (Alignment a in senate[state.name])
                    {
                        Alignment vote = a;
                        if (vote >= Alignment.CONSERVATIVE && vote <= Alignment.LIBERAL)
                            vote += mc.LCSRandom(3) - 1;

                        if (vote == align) senateYesVotes++;
                    }
                }

                if (congressYesVotes >= (houseNum * 2) / 3 && senateYesVotes >= (senateNum * 2) / 3)
                    congressRatified = true;
            }
            else congressRatified = true;

            result.houseYesVotes = congressYesVotes;
            result.senateYesVotes = senateYesVotes;
            result.congressRatified = congressRatified;

            if (congressRatified)
            {
                foreach(NationDef.StateDef state in nation.states)
                {
                    if ((state.flags & NationDef.stateFlags.NONSTATE) != 0) continue;

                    int bias = getStateBias(state);

                    Alignment vote = MasterController.generalPublic.getSwingVoter(bias);

                    if (vote == align)
                    {
                        stateYesVotes++;
                        result.stateVotes[state.name] = true;
                    }
                    else
                        result.stateVotes[state.name] = false;
                }

                if (stateYesVotes >= (nation.states.Count * 3) / 4) ratified = true;
            }

            result.ratified = ratified;

            return result;
        }

        //June 1st
        public void supremeCourtDecisions()
        {
            MasterController mc = MasterController.GetMC();
            List<SupremeCourtResult> decisionResults = new List<SupremeCourtResult>();

            Dictionary<string, Alignment> lawDir = new Dictionary<string, Alignment>();

            int snum = mc.LCSRandom(5) + 2;

            Dictionary<string, int> caseChoices = new Dictionary<string, int>();

            /*This is a deviation from the original source and the reasoning is as follows - although the supreme court does not consult
            public opinion to make their decisions, they also do not select cases that they try. 
            Issues with higher public interest are more likely to be challenged in court, and thus more likely 
            to appear in front of the supreme court.*/
            foreach (string law in MasterController.government.laws.Keys)
            {
                int odds = getComponent<Public>().getLawInterest(law) + 25;
                caseChoices.Add(law, odds);
            }

            for (int i = 0; i < snum; i++)
            {
                string law = mc.WeightedRandom(caseChoices);
                caseChoices.Remove(law);

                if (laws[law].alignment == Alignment.ARCHCONSERVATIVE)
                    lawDir[law] = Alignment.LIBERAL;
                else if (laws[law].alignment == Alignment.ELITE_LIBERAL)
                    lawDir[law] = Alignment.CONSERVATIVE;
                else
                {
                    if (GameData.getData().lawList[law].supremeCourtBias != Alignment.MODERATE)
                        lawDir[law] = GameData.getData().lawList[law].supremeCourtBias;
                    else if (mc.LCSRandom(2) == 0) lawDir[law] = Alignment.LIBERAL;
                    else lawDir[law] = Alignment.CONSERVATIVE;
                }

                string plaintiff = "United States";
                string defendant = Factories.CreatureFactory.generateSurname();

                if (mc.LCSRandom(5) == 0)
                    plaintiff = Factories.CreatureFactory.generateSurname();

                if ((GameData.getData().lawList[law].flags & LawDef.LawFlag.CORPORATE) != 0 && mc.LCSRandom(5) != 0)
                {
                    defendant = Factories.CreatureFactory.generateSurname(CreatureInfo.CreatureGender.WHITEMALEPATRIARCH);
                    switch (mc.LCSRandom(5))
                    {
                        case 0:
                            defendant += " Inc.";
                            break;
                        case 1:
                            defendant += " L.L.C.";
                            break;
                        case 2:
                            defendant += " Corp.";
                            break;
                        case 3:
                            defendant += " Co.";
                            break;
                        case 4:
                            defendant += " Ltd.";
                            break;
                    }
                }

                string caseName = plaintiff + " v. " + defendant;

                SupremeCourtResult result = new SupremeCourtResult(caseName, lawDir[law], law, supremeCourtDecision(law, lawDir[law]));
                decisionResults.Add(result);
            }

            if (mc.canSeeThings)
            {
                mc.uiController.closeUI();
                mc.uiController.law.show(decisionResults);
            }
            else
            {
                if (MasterController.GetMC().LCSRandom(10) >= 6)
                    appointNewJustice(true);
                mc.doNextAction();
            }
        }

        private int supremeCourtDecision(string law, Alignment direction)
        {
            int yesVotes = 0;

            foreach(Entity e in supremeCourt)
            {
                Alignment vote = e.getComponent<Politician>().alignment;

                if (vote != Alignment.ARCHCONSERVATIVE && vote != Alignment.ELITE_LIBERAL)
                    vote += (int) GameData.getData().lawList[law].supremeCourtBias;

                if (laws[law].alignment > vote && direction == Alignment.CONSERVATIVE) yesVotes++;
                if (laws[law].alignment < vote && direction == Alignment.LIBERAL) yesVotes++;                
            }

            if (yesVotes > supremeCourt.Count / 2)
            {
                shiftLaw(law, direction);
            }

            return yesVotes;
        }

        private void shiftLaw(string law, Alignment direction)
        {
            if (direction == Alignment.LIBERAL)
            {
                laws[law].alignment++;
                if(laws[law].alignment == Alignment.ELITE_LIBERAL)
                    MasterController.GetMC().addMessage(GameData.getData().lawList[law].name + " became <color=lime><b>ELITE LIBERAL</b></color>.");
                else
                    MasterController.GetMC().addMessage(GameData.getData().lawList[law].name + " became <color=lime>more Liberal</color>.");
            }
            else
            {
                laws[law].alignment--;
                MasterController.GetMC().addMessage(GameData.getData().lawList[law].name + " became <color=red>more Conservative</color>.");
            }
        }

        private Alignment determinePoliticianVote(Alignment alignment, string law)
        {
            Alignment vote = alignment;

            //Extremes will always vote their alignment regardless of public opinion.
            if(alignment == Alignment.ARCHCONSERVATIVE || alignment == Alignment.ELITE_LIBERAL)
            {
                return vote;
            }
            //Partisans will consult public opinion, but won't vote on the opposite end of the spectrum
            else if(alignment == Alignment.CONSERVATIVE || alignment == Alignment.LIBERAL)
            {
                int mood = MasterController.generalPublic.getLawOpinion(law);
                vote = Alignment.ARCHCONSERVATIVE;

                for (int i = 0; i < 4; i++) if (MasterController.GetMC().LCSRandom(100) < mood) vote++;
                if (Math.Abs(vote - alignment) > 1) vote = Alignment.MODERATE;
                return vote;
            }
            //Moderates always consult public opinion, but won't vote on the extremes
            else
            {
                int mood = MasterController.generalPublic.getLawOpinion(law);
                vote = Alignment.ARCHCONSERVATIVE;

                for (int i = 0; i < 4; i++) if (MasterController.GetMC().LCSRandom(100) < mood) vote++;
                if (Math.Abs(vote - alignment) > 1) vote = (Alignment) ((int)vote/2);
                return vote;
            }
        }

        public void politicianDied(Entity whoDied)
        {
            MasterController mc = MasterController.GetMC();

            if (whoDied.getComponent<Politician>().position == "PRESIDENT")
            {
                promoteVP();

                mc.addMessage("With the death of President " + whoDied.getComponent<CreatureInfo>().getName() + ", Vice President " + president.getComponent<CreatureInfo>().getName() + " has ascended to the Presidency.", true);
                if (mc.currentDate.Year % 4 < 2) presidentTerm = 0;
                else presidentTerm = 1;
            }
            else if (whoDied.getComponent<Politician>().position == "VICE_PRESIDENT")
            {
                vicePresident = getCabinetMember();
                vicePresident.persist();

                mc.addMessage("Due to the untimely death of Vice President " + whoDied.getComponent<CreatureInfo>().getName() + ", " + vicePresident.getComponent<CreatureInfo>().getName() + " has been appointed in their stead.", true);
            }
            //The other cabinent members aren't really important enough to bother informing the player about their replacement
            else if(whoDied.getComponent<Politician>().position == "SECRETARY_OF_STATE")
            {
                secretaryOfState = getCabinetMember();
                secretaryOfState.persist();
            }
            else if(whoDied.getComponent<Politician>().position == "ATTORNEY_GENERAL")
            {
                attorneyGeneral = getCabinetMember();
                attorneyGeneral.persist();
            }
            else if(whoDied.getComponent<Politician>().position == "SUPREME_COURT")
            {
                string color = UIControllerImpl.getAlignmentColor(whoDied.getComponent<Politician>().alignment);
                appointNewJustice(false, "With the death of " + color + "Justice " + whoDied.getComponent<CreatureInfo>().getName() + "</color>, a new vacancy has opened in the Supreme Court\n");
                supremeCourt.Remove(whoDied);                
            }
            else if(whoDied.getComponent<Politician>().position == "CEO")
            {
                makeCEO();
            }

            whoDied.depersist();
        }

        public Entity makeCEO()
        {
            ceo = Factories.CreatureFactory.create("CORPORATE_CEO");
            ceo.getComponent<CreatureInfo>().encounterName = "CEO " + ceo.getComponent<CreatureInfo>().getName();
            Politician polComponent = new Politician();
            polComponent.alignment = Alignment.ARCHCONSERVATIVE;
            polComponent.position = "CEO";
            polComponent.party = Alignment.CONSERVATIVE;
            ceo.setComponent(polComponent);
            ceo.persist();

            return ceo;
        }

        public int getStateBias(NationDef.StateDef state)
        {
            //Red states are always going to be more conservative than the average public mood - the opposite for blue states.
            MasterController mc = MasterController.GetMC();
            double adjustedPublicMood = ((double)MasterController.generalPublic.PublicMood) / 100;
            double basePublicMood = adjustedPublicMood;
            double stateCoefficient = 0.1;

            //These formulas look like nightmares but they are really just formulas to grab specific quadrants of a superellipse, because it
            //gives a good curve to the data while still ending on the correct points. The goal is to use state alignment to convert the general
            //public mood into a state specific public mood. The curve is such that blue/red state inclination is more impactful when public mood is
            //neutral than when it is at the extreme ends.
            if (state.alignment >= 0)
            {
                //The actual formula in a slightly easier to read form is:
                //y = (-|x-1|^(|ab|+1) + 1)^(1/(|ab|+1))
                //Where a is the state cofficient (helps flatten the curve) and b is the state base alignment
                adjustedPublicMood = Math.Pow(-Math.Pow(Math.Abs(basePublicMood - 1), Math.Abs(stateCoefficient * state.alignment) + 1) + 1, 1 / (Math.Abs(stateCoefficient * state.alignment) + 1));
            }
            else
            {
                //The actual formula in a slightly easier to read form is:
                //y = -(-|x|^(|ab|+1) + 1)^(1/(|ab|+1)) + 1
                //Where a is the state cofficient (helps flatten the curve) and b is the state base alignment
                adjustedPublicMood = -Math.Pow(-Math.Pow(Math.Abs(basePublicMood), Math.Abs(stateCoefficient * state.alignment) + 1) + 1, 1 / (Math.Abs(stateCoefficient * state.alignment) + 1)) + 1;
            }

            int bias = (int)(adjustedPublicMood*100 - mc.LCSRandom(100));
            return bias;
        }

        private void promoteVP()
        {
            //Depersist the whole executive just so we don't have to worry about keeping track of who is replaced
            president.depersist();
            vicePresident.depersist();
            secretaryOfState.depersist();
            attorneyGeneral.depersist();

            //If the VP and President had different parties (possible in the event of an electoral college tie), the VP replaces the entire cabinet.
            if(president.getComponent<Politician>().party != vicePresident.getComponent<Politician>().party)
            {
                president = vicePresident;
                vicePresident = getCabinetMember();
                secretaryOfState = getCabinetMember();
                attorneyGeneral = getCabinetMember();
            }
            //If they were in the same party, but the VP is extreme aligned, they will fill the cabinet with loyalists.
            else if(vicePresident.getComponent<Politician>().alignment == Alignment.ELITE_LIBERAL ||
                vicePresident.getComponent<Politician>().alignment == Alignment.ARCHCONSERVATIVE)
            {
                president = vicePresident;
                vicePresident = getCabinetMember();
                if (secretaryOfState.getComponent<Politician>().alignment != president.getComponent<Politician>().alignment)
                    secretaryOfState = getCabinetMember();
                if (attorneyGeneral.getComponent<Politician>().alignment != president.getComponent<Politician>().alignment)
                    attorneyGeneral = getCabinetMember();
            }
            //If they are not extreme aligned, they will only replace cabinet members whose views differ greatly from their own
            else
            {
                president = vicePresident;
                vicePresident = getCabinetMember();
                if (Math.Abs(secretaryOfState.getComponent<Politician>().alignment - president.getComponent<Politician>().alignment) > 1)
                    secretaryOfState = getCabinetMember();
                if (Math.Abs(attorneyGeneral.getComponent<Politician>().alignment - president.getComponent<Politician>().alignment) > 1)
                    attorneyGeneral = getCabinetMember();
            }

            president.persist();
            vicePresident.persist();
            secretaryOfState.persist();
            attorneyGeneral.persist();
        }
    }
}
