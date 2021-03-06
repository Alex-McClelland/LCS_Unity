using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.UI
{
    public interface Chase : UIBase
    {
        void init(ChaseActions actions);
        void enableInput();
        void disableInput();
    }

    public class ChaseActions
    {
        public Action run;
        public Action fight;
        public Action surrender;
        public Action advance;
        public Action driveEscape;
        public Action bail;
        public Action driveObstacleRisky;
        public Action driveObstacleSafe;
    }
}
