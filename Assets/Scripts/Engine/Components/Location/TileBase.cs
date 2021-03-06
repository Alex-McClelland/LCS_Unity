using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.Components.Location
{
    public class TileBase : Component
    {
        public enum FireState
        {
            NONE,
            START,
            PEAK,
            END,
            DEBRIS
        }

        public enum Graffiti
        {
            NONE,
            LCS,
            GNG,
            CCS
        }

        public enum Bloodstain
        {
            NONE,
            BLOOD_1,
            BLOOD_2,
            BLOOD_3
        }

        public int x { get; set; }
        public int y { get; set; }
        public bool mapped { get; set; }
        public bool restricted { get; set; }

        public TroubleSpot location { get; set; }

        public int cash { get; set; }
        public List<Entity> loot { get; set; }
        public bool trapped { get; set; }
        public FireState fireState { get; set; }
        public Graffiti graffiti { get; set; }

        public Bloodstain bloodTrail_S { get; set; }
        public Bloodstain bloodTrail_N { get; set; }
        public Bloodstain bloodTrail_W { get; set; }
        public Bloodstain bloodTrail_E { get; set; }        
        public Bloodstain bloodBlast { get; set; }
        public bool bloodTrail_Standing { get; set; }
        public bool someoneDiedHere { get; set; }
        public bool bloodPrints_S_N { get; set; }
        public bool bloodPrints_N_N { get; set; }
        public bool bloodPrints_W_W { get; set; }
        public bool bloodPrints_E_W { get; set; }
        public bool bloodPrints_S_S { get; set; }
        public bool bloodPrints_N_S { get; set; }
        public bool bloodPrints_W_E { get; set; }
        public bool bloodPrints_E_E { get; set; }

        public TileBase()
        {
            cash = 0;
            loot = new List<Entity>();
            fireState = FireState.NONE;
            graffiti = Graffiti.NONE;
        }

        public bool isWalkable()
        {
            if (!hasComponent<TileFloor>())
                return false;
            else if (hasComponent<TileWall>() && fireState < FireState.PEAK)
                return false;
            else if (hasComponent<TileDoor>() && !getComponent<TileDoor>().open && fireState < FireState.PEAK)
                return false;
            else if (hasComponent<TileSpecial>() && !getComponent<TileSpecial>().isPassable())
                return false;
            else
                return true;
        }
    }
}
