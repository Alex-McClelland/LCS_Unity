using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LCS.Engine.UI
{
    public interface Trial : UIBase
    {
        void init(TrialActions actions);
        void show(Entity defendant);
        void printText(string text);
        void printTitle(string text);
        void generateTrialButtons();
        void clearText();
    }

    public class TrialActions
    {
        public TrialSelectionAction selection;
        public EntityAction advance;

        public delegate void TrialSelectionAction(Entity e, TrialSelection selection);
        public delegate void EntityAction(Entity e);
        public enum TrialSelection
        {
            PUBLIC_DEFENDER,
            DEFEND_SELF,
            PLEAD_GUILTY,
            ACE_ATTORNEY,
            SLEEPER_ATTORNEY
        }
    }
}
