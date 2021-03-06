﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.UI
{
    public interface UIBase
    {
        void show();
        void close();
        void hide();
        void refresh();
    }
}
