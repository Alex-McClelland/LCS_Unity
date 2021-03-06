using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace LCS.Engine.Data
{
    public abstract class DataDef
    {
        public string type;

        public abstract void parseData(XmlNode node);
    }
}
