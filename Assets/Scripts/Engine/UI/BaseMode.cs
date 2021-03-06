using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LCS.Engine.Containers;
using LCS.Engine.Components.World;

namespace LCS.Engine.UI
{
    public interface BaseMode : UIBase
    {
        void init(BaseModeActions actions);
    }

    public class BaseModeActions
    {
        public Action waitADay;
        public Action nextSquad;
        public TwoStringAction setDestination;
        public StringAction changeSlogan;        

        public delegate void StringAction(string arg);
        public delegate void TwoStringAction(string arg, string arg2);
    }
}
