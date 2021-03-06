using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.Containers
{
    public class SupremeCourtResult
    {
        public string caseName;
        public Alignment lawDir;
        public string lawDef;
        public int yesVotes;

        public SupremeCourtResult(string caseName, Alignment lawDir, string lawDef, int yesVotes)
        {
            this.caseName = caseName;
            this.lawDir = lawDir;
            this.lawDef = lawDef;
            this.yesVotes = yesVotes;
        }
    }
}
