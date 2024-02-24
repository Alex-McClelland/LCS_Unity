using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Location;
using LCS.Engine.Data;
using System.Xml;

namespace LCS.Engine.Components.Creature
{
    public class Recruit : Component
    {
        [SimpleSave]
        public int eagerness;
        [SimpleSave]
        public Entity recruiter;

        private ActionQueue actionRoot { get; set; }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Recruit");
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
            MasterController.GetMC().nextDay += doStartMeeting;
        }

        public override void unsubscribe()
        {
            base.unsubscribe();
            MasterController.GetMC().nextDay -= doStartMeeting;
        }

        public void initRecruitment(Entity recruiter)
        {
            this.recruiter = recruiter;

            MasterController mc = MasterController.GetMC();
            //Has head of the LCS
            if (mc.LCSRandom(100) < MasterController.generalPublic.PublicOpinion[Constants.VIEW_LIBERALCRIMESQUAD])
            {
                //likes the LCS
                if (mc.LCSRandom(100) < MasterController.generalPublic.PublicOpinion[Constants.VIEW_LIBERALCRIMESQUADPOS])
                    eagerness = 3;
                //Doesn't like the LCS
                else
                    eagerness = 0;
            }
            else
                eagerness = 2;

            if (getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE) eagerness -= 4;
            if (getComponent<CreatureInfo>().alignment == Alignment.MODERATE) eagerness -= 2;
        }

        private void doStartMeeting(object sender, EventArgs arg)
        {
            MasterController mc = MasterController.GetMC();

            actionRoot = mc.createSubQueue(() =>
            {
                mc.uiController.closeUI();
                startMeeting();
            }, "Start Meeting",
            () =>
            {
                mc.uiController.closeUI();
                actionRoot = null;
                mc.doNextAction();
            }, "close meeting screen->Next Action");
        }

        private void startMeeting()
        {
            MasterController mc = MasterController.GetMC();

            mc.uiController.meeting.showMeeting(owner);

            string text = getComponent<CreatureInfo>().getName();
            switch (eagerness)
            {
                case 1:
                    text += " will take a lot of persuading.";
                    break;
                case 2:
                    text += " is interested in learning more.";
                    break;
                case 3:
                    text += " feels something needs to be done.";
                    break;
                default:
                    if (eagerness >= 4) text += " is ready to fight for the Liberal Cause.";
                    else text += " kind of regrets agreeing to this.";
                    break;
            }

            text += "\nHow should " + recruiter.getComponent<CreatureInfo>().getName() + " approach the situation?";

            mc.uiController.meeting.printText(text);
        }

        public void discussion(bool props)
        {
            string libText = "\n\n";

            if (props)
            {
                libText += recruiter.getComponent<CreatureInfo>().getName() + " shares " + GameData.getData().viewList[MasterController.generalPublic.randomissue(true)].recruitProp + ".";
                MasterController.lcs.changeFunds(-50);
            }
            else
            {
                libText += recruiter.getComponent<CreatureInfo>().getName() + " explains " + recruiter.getComponent<CreatureInfo>().hisHer().ToLower() + " views on " + GameData.getData().viewList[MasterController.generalPublic.randomissue(true)].name + ".";
            }

            MasterController.GetMC().uiController.meeting.printText(libText);

            recruiter.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].addExperience(Math.Max(5,12-recruiter.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].level));
            foreach(CreatureBase.Skill skill in recruiter.getComponent<CreatureBase>().Skills.Values)
            {
                if ((skill.getFlags() & SkillDef.SkillFlag.LEARN_FROM_RECRUITMENT) == 0) continue;
                recruiter.getComponent<CreatureBase>().Skills[skill.type].addExperience(Math.Max(0, getComponent<CreatureBase>().Skills[skill.type].level - recruiter.getComponent<CreatureBase>().Skills[skill.type].level));
            }

            int persuasiveness = recruiter.getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].level +
                                 recruiter.getComponent<CreatureBase>().Skills[Constants.SKILL_LAW].level +
                                 recruiter.getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].level +
                                 recruiter.getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].level +
                                 recruiter.getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].getModifiedValue();

            int reluctance = 5 +
                             getComponent<CreatureBase>().Skills[Constants.SKILL_BUSINESS].level +
                             getComponent<CreatureBase>().Skills[Constants.SKILL_LAW].level +
                             getComponent<CreatureBase>().Skills[Constants.SKILL_RELIGION].level +
                             getComponent<CreatureBase>().Skills[Constants.SKILL_SCIENCE].level +
                             getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_INTELLIGENCE].getModifiedValue() +
                             getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].getModifiedValue();

            if (persuasiveness > reluctance) reluctance = 0;
            else reluctance -= persuasiveness;

            int difficulty = reluctance;
            if (props) difficulty -= 5;

            //Liberals with juice get their difficulty increased as if it was modifying their wisdom
            if(getComponent<CreatureBase>().Juice >= 10 && getComponent<CreatureInfo>().alignment == Alignment.LIBERAL)
            {
                if (getComponent<CreatureBase>().Juice < 50)
                    difficulty += 1;
                else if (getComponent<CreatureBase>().Juice < 100)
                    difficulty += 2 + (int)(0.1 * getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level);
                else if (getComponent<CreatureBase>().Juice < 200)
                    difficulty += 3 + (int)(0.2 * getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level);
                else if (getComponent<CreatureBase>().Juice < 500)
                    difficulty += 4 + (int)(0.3 * getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level);
                else if (getComponent<CreatureBase>().Juice < 1000)
                    difficulty += 5 + (int)(0.4 * getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level);
                else
                    difficulty += 6 + (int)(0.5 * getComponent<CreatureBase>().BaseAttributes[Constants.ATTRIBUTE_WISDOM].Level);
            }

            if (difficulty > 18) difficulty = 18;

            if (recruiter.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].check((Difficulty)difficulty))
            {
                eagerness++;
                string text1 = "\n<color=cyan>" + getComponent<CreatureInfo>().getName() + " found " + recruiter.getComponent<CreatureInfo>().getName() + "'s views to be insightful.</color>";
                actionRoot.Add(() => { MasterController.GetMC().uiController.meeting.printText(text1); }, "Print Text");
                string text2 = "\n<color=cyan>They'll definitely meet again tomorrow.</color>";
                actionRoot.Add(() => { MasterController.GetMC().uiController.meeting.printText(text2); }, "Print Text");
            }
            //Second chance to persuade them that at least you aren't insane
            else if (recruiter.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].check((Difficulty)difficulty))
            {
                eagerness--;
                string text1 = "\n" + getComponent<CreatureInfo>().getName() + " is skeptical about some of " + recruiter.getComponent<CreatureInfo>().getName() + "'s arguments.";
                actionRoot.Add(() => { MasterController.GetMC().uiController.meeting.printText(text1); }, "Print Text");
                string text2 = "\nThey'll meet again tomorrow.";
                actionRoot.Add(() => { MasterController.GetMC().uiController.meeting.printText(text2); }, "Print Text");
            }
            else
            {
                if((getComponent<CreatureBase>().getFlags() & CreatureDef.CreatureFlag.TALK_RECEPTIVE) != 0 && getComponent<CreatureInfo>().alignment == Alignment.LIBERAL)
                {
                    string text1 = "\n<color=magenta>" + getComponent<CreatureInfo>().getName() + " isn't convinced " + recruiter.getComponent<CreatureInfo>().getName() + " really understands the problem.</color>";
                    actionRoot.Add(() => { MasterController.GetMC().uiController.meeting.printText(text1); }, "Print Text");
                    string text2 = "\n<color=magenta>Maybe " + recruiter.getComponent<CreatureInfo>().getName() + " needs more experience.</color>";
                    actionRoot.Add(() => { MasterController.GetMC().uiController.meeting.printText(text2); }, "Print Text");
                }
                else
                {
                    string text1 = "\n<color=magenta>" + recruiter.getComponent<CreatureInfo>().getName() + " comes off as slightly insane.</color>";
                    actionRoot.Add(() => { MasterController.GetMC().uiController.meeting.printText(text1); }, "Print Text");
                    string text2 = "\n<color=magenta>This whole thing was a mistake. There won't be another meeting.</color>";
                    actionRoot.Add(() => { MasterController.GetMC().uiController.meeting.printText(text2); }, "Print Text");
                }

                endMeetings();
            }
        }

        public void callOffMeetings()
        {
            string text = "\n\n" + recruiter.getComponent<CreatureInfo>().getName() + " decides that these meetings are no longer productive.";
            MasterController.GetMC().uiController.meeting.printText(text);
            endMeetings();
        }

        public void endMeetings()
        {
            recruiter.getComponent<Liberal>().plannedMeetings.Remove(owner);
            owner.depersist();
        }

        public void joinLCS()
        {
            string text = "\n\n" + recruiter.getComponent<CreatureInfo>().getName() + " offers to let " + getComponent<CreatureInfo>().getName() + " join the Liberal Crime Squad.";
            MasterController.GetMC().uiController.meeting.printText(text);

            text = "\n<color=lime>" + getComponent<CreatureInfo>().getName() + " accepts, and is eager to get started.</color>";
            actionRoot.Add(() => { MasterController.GetMC().uiController.meeting.printText(text); }, "Print Text");

            recruiter.getComponent<CreatureBase>().Skills[Constants.SKILL_PERSUASION].addExperience(25);
            recruiter.getComponent<Liberal>().recruit(owner);
            recruiter.getComponent<Liberal>().plannedMeetings.Remove(owner);

            List<UI.PopupOption> options = new List<UI.PopupOption>();
            options.Add(new UI.PopupOption("Sleeper", () => 
            {
                getComponent<Liberal>().sleeperize();
                removeMe();
                MasterController.GetMC().doNextAction();
            }));
            options.Add(new UI.PopupOption("Regular", () => 
            {
                removeMe();
                MasterController.GetMC().doNextAction();
            }));
            string sleeperPrompt = "Should " + getComponent<CreatureInfo>().getName() + " stay at " + getComponent<CreatureInfo>().workLocation.getComponent<SiteBase>().getCurrentName() + " as a sleeper agent or join the LCS as a regular member?";

            actionRoot.Add(() => { MasterController.GetMC().uiController.showOptionPopup(sleeperPrompt, options); }, "Sleeper Prompt");
        }
    }
}
