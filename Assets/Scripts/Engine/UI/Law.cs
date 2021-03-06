using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LCS.Engine.Containers;

namespace LCS.Engine.UI
{
    public interface Law : UIBase
    {
        void show(List<PropositionResult> result);
        void show(List<CongressBillResult> result);
        void show(List<SupremeCourtResult> result);
    }
}
