using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.Containers
{
    public class PropositionResult
    {
        public int propNum;
        public Alignment lawDir;
        public string lawDef;
        public int yesVotes;

        public PropositionResult(int propNum, Alignment lawDir, string lawDef, int yesVotes)
        {
            this.propNum = propNum;
            this.lawDir = lawDir;
            this.lawDef = lawDef;
            this.yesVotes = yesVotes;
        }
    }
}
