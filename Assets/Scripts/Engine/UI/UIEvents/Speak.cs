using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.UI.UIEvents
{
    public class Speak : EventArgs
    {
        public Entity speaker { get; set; }
        public string text { get; set; }
        public int duration { get; set; }
        
        public Speak(Entity speaker, string text, int duration = 5)
        {
            this.speaker = speaker;
            this.text = text;
            this.duration = duration;
        }
    }
}
