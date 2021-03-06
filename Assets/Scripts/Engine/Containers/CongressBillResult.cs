using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.Containers
{
    public class CongressBillResult
    {
        public string lawDef;
        public Alignment lawDir;
        public int houseYesVotes;
        public int senateYesVotes;
        public bool vpVote;
        public bool presidentVeto;

        public CongressBillResult(string lawDef, Alignment lawDir)
        {
            this.lawDef = lawDef;
            this.lawDir = lawDir;
        }
    }
}
