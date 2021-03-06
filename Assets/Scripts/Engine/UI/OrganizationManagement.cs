using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.UI
{
    public interface OrganizationManagement : UIBase
    {
        void init(OrganizationManagementActions actions);
    }

    public class OrganizationManagementActions
    {
        public QuickActivityAction quickActivity;
        public SelectAction selectChar;

        public delegate void SelectAction(Entity e);
        public delegate void QuickActivityAction(Entity target, string activity);
    }
}
