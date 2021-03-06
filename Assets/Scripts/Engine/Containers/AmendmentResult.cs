using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.Containers
{
    public class AmendmentResult
    {
        public int houseYesVotes;
        public int senateYesVotes;
        public Dictionary<string, bool> stateVotes;
        public bool ratified;
        public bool congressRatified;

        public AmendmentResult()
        {
            houseYesVotes = 0;
            senateYesVotes = 0;
            ratified = false;
            congressRatified = false;
            stateVotes = new Dictionary<string, bool>();
        }
    }
}
