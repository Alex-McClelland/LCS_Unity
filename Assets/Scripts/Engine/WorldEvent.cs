namespace LCS.Engine
{
    public abstract class WorldEvent
    {
        public string name { get; set; }
        public string returnText { get; set; }

        public WorldEvent(string name)
        {
            this.name = name;
            returnText = "";
        }
    }
}
