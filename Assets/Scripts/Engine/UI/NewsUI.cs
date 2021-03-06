using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.UI
{
    public interface NewsUI : UIBase
    {
        void init(NewsActions newsActions);
    }

    public class NewsActions
    {
        public Action nextScreen;
    }
}
