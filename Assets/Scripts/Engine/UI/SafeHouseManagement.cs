using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.UI
{
    public interface SafeHouseManagement : UIBase
    {
        void init(SafeHouseManagementActions actions);
    }

    public class SafeHouseManagementActions
    {
        public EntityAction buyRations;
        public EntityAction buyFlag;
        public EntityAction burnFlag;
        public safeHouseUpgradeAction upgrade;
        public EntityAction selectChar;
        public EntityAction giveUpSiege;
        public EntityAction escapeEngageSiege;

        public delegate void EntityAction(Entity e);
        public delegate void safeHouseUpgradeAction(Entity safehouse, string upgrade);
    }
}
