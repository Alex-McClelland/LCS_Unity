using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LCS.Engine.Containers;

namespace LCS.Engine.UI
{
    public interface NationMap : UIBase
    {
        void init(NationMapActions actions);
        void showPresidentialElection(PresidentialElectionResult result);
        void showAmendmentVote(AmendmentResult result, string title, string description, bool congressNeeded);
        void showDemographics();
    }

    public class NationMapActions
    {
        public Action disband;
    }
}
