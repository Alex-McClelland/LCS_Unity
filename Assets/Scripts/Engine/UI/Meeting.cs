using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.UI
{
    public interface Meeting : UIBase
    {
        void init(MeetingActions actions);
        void showMeeting(Entity recruit);
        void showDate(Entity recruit);
        void printText(string text);
        void printTitle(string text);
    }

    public class MeetingActions
    {
        public DiscussionAction discussion;
        public EntityAction endMeetings;
        public EntityAction joinLCS;

        public DiscussionAction normalDate;
        public EntityAction vacation;
        public EntityAction breakUp;
        public EntityAction kidnap;

        public delegate void DiscussionAction(Entity recruit, bool props);
        public delegate void EntityAction(Entity e);
    }
}
