using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.Events
{
    public class GetAttributeModifiers : EventArgs
    {
        public Dictionary<string, Dictionary<string, float>> PreMultipliers { get; set; }
        public Dictionary<string, Dictionary<string, int>> LinearModifiers { get; set; }
        public Dictionary<string, Dictionary<string, float>> PostMultipliers { get; set; }

        public GetAttributeModifiers()
        {
            PreMultipliers = new Dictionary<string, Dictionary<string, float>>();
            LinearModifiers = new Dictionary<string, Dictionary<string, int>>();
            PostMultipliers = new Dictionary<string, Dictionary<string, float>>();

            foreach(string attribute in GameData.getData().attributeList.Keys)
            {
                PreMultipliers.Add(attribute, new Dictionary<string, float>());
                LinearModifiers.Add(attribute, new Dictionary<string, int>());
                PostMultipliers.Add(attribute, new Dictionary<string, float>());
            }
        }
    }
}
