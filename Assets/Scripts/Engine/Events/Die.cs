using System;

namespace LCS.Engine.Events
{
    public class Die : EventArgs
    {
        public string cause;

        public Die (string cause)
        {
            this.cause = cause;
        }
    }
}
