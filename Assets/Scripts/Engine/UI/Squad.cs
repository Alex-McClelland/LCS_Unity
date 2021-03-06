using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.UI
{
    public interface Squad : UIBase
    {
        void init(SquadActions actions);
        bool displaySquad(List<Entity> squad);
        bool displayDriving(List<Entity> vehicles);
    }

    public class SquadActions
    {
        public SelectAction selectChar;

        public delegate void SelectAction(Entity e);
    }
}
