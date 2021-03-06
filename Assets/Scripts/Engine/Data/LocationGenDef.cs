using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace LCS.Engine.Data
{
    public class LocationGenDef : DataDef
    {
        public class LocationGenParameter
        {
            public LocationGenParameter()
            {
                scriptArguments = new Dictionary<string, int>();
            }

            public string type;
            public string name;
            public bool overwrite = true;
            public Dictionary<string, int> scriptArguments;
            public int xstart = 0;
            public int xend = 0;
            public int ystart = 0;
            public int yend = 0;
            public int zstart = 0;
            public int zend = 0;
            public int freq = 10;
        }

        public LocationGenDef()
        {
            parameters = new List<LocationGenParameter>();
        }
        
        public string use = "";
        public List<LocationGenParameter> parameters;

        public override void parseData(XmlNode node)
        {
            if (node.SelectSingleNode("use") != null) use = node.SelectSingleNode("use").InnerText;

            foreach (XmlNode innerNode in node.ChildNodes)
            {
                if (innerNode.Name == "use") continue;

                LocationGenParameter parameter = new LocationGenParameter();
                parameter.type = innerNode.Name;
                parameter.name = innerNode.Attributes["name"].Value;
                if (innerNode.Attributes["overwrite"] != null) parameter.overwrite = bool.Parse(innerNode.Attributes["overwrite"].Value);
                if (innerNode.SelectSingleNode("xstart") != null) parameter.xstart = int.Parse(innerNode.SelectSingleNode("xstart").InnerText);
                if (innerNode.SelectSingleNode("xend") != null) parameter.xend = int.Parse(innerNode.SelectSingleNode("xend").InnerText);
                if (innerNode.SelectSingleNode("ystart") != null) parameter.ystart = int.Parse(innerNode.SelectSingleNode("ystart").InnerText);
                if (innerNode.SelectSingleNode("yend") != null) parameter.yend = int.Parse(innerNode.SelectSingleNode("yend").InnerText);
                if (innerNode.SelectSingleNode("zstart") != null) parameter.zstart = int.Parse(innerNode.SelectSingleNode("zstart").InnerText);
                if (innerNode.SelectSingleNode("zend") != null) parameter.zend = int.Parse(innerNode.SelectSingleNode("zend").InnerText);
                if (innerNode.SelectSingleNode("freq") != null) parameter.freq = int.Parse(innerNode.SelectSingleNode("freq").InnerText);
                if (innerNode.SelectSingleNode("x") != null)
                {
                    parameter.xstart = int.Parse(innerNode.SelectSingleNode("x").InnerText);
                    parameter.xend = int.Parse(innerNode.SelectSingleNode("x").InnerText);
                }
                if (innerNode.SelectSingleNode("y") != null)
                {
                    parameter.ystart = int.Parse(innerNode.SelectSingleNode("y").InnerText);
                    parameter.yend = int.Parse(innerNode.SelectSingleNode("y").InnerText);
                }
                if (innerNode.SelectSingleNode("z") != null)
                {
                    parameter.zstart = int.Parse(innerNode.SelectSingleNode("z").InnerText);
                    parameter.zend = int.Parse(innerNode.SelectSingleNode("z").InnerText);
                }

                if (innerNode.SelectSingleNode("arguments") != null)
                {
                    foreach (XmlNode argNode in innerNode.SelectSingleNode("arguments").ChildNodes)
                    {
                        parameter.scriptArguments.Add(argNode.Name, int.Parse(argNode.InnerText));
                    }
                }

                parameters.Add(parameter);
            }
        }
    }
}
