using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace LCS.Engine.Data
{
    public class AttributeDef : DataDef
    {
        public string name;

        public override void parseData(XmlNode node)
        {
            name = node.SelectSingleNode("name").InnerText;
        }
    }
}
