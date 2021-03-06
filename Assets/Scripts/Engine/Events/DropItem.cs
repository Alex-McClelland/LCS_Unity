using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.Events
{
    class DropItem : EventArgs
    {
        public readonly Entity item;
        public readonly Entity dropper;

        public DropItem(Entity item, Entity dropper)
        {
            this.item = item;
            this.dropper = dropper;
        }

    }
}
