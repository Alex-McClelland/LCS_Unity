using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.Components.Location
{
    public class TileFloor : Component
    {
        public Type type { get; set; }

        public enum Type
        {
            INDOOR,
            OUTDOOR,
            PATH,
            STAIRS_UP,
            STAIRS_DOWN,
            EXIT
        }
    }
}
