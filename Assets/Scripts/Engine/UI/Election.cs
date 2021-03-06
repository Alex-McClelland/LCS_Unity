using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.UI
{
    public interface Election : UIBase
    {
        void init(ElectionActions actions);
    }

    public class ElectionActions
    {
        public Action senateElection;
        public Action houseElection;
    }
}
