using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.Containers
{
    public class PresidentialElectionResult
    {
        public PresidentialElectionResult()
        {
            stateResults = new Dictionary<string, Alignment>();
            stateRecounts = new Dictionary<string, bool>();
            stateSpecificResults = new Dictionary<string, int[]>();
            statePopularResults = new Dictionary<string, int[]>();
            stateTiebreakerResults = new Dictionary<string, Alignment>();
        }

        public Dictionary<string, Alignment> stateResults { get; set; }
        public Dictionary<string, int[]> stateSpecificResults { get; set; }
        public Dictionary<string, int[]> statePopularResults { get; set; }
        public Dictionary<string, bool> stateRecounts { get; set; }
        public string liberalCandidateRunningName { get; set; }
        public Entity liberalCandidate { get; set; }
        public string conservativeCandidateRunningName { get; set; }
        public Entity conservativeCandidate { get; set; }

        public Dictionary<string, Alignment> stateTiebreakerResults { get; set; }
        public string winnerName { get; set; }
        public string VPwinnerName { get; set; }
        public Alignment VPwinnerAlignment { get; set; }
    }
}
