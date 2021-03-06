using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.UI
{
    public interface TitlePage : UIBase
    {
        void init(TitlePageActions actions);
    }

    public class TitlePageActions
    {
        public Action newGame;
        public Action loadGame;
    }
}
