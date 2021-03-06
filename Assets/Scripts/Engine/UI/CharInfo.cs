using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Creature;

namespace LCS.Engine.UI
{
    public interface CharInfo
    {
        void init(CharInfoActions actions);
        void show(Entity selectedChar);
        void hide();
    }

    public class CharInfoActions
    {
        public StringAction setAlias;
        public SelfAction changeGender;
        public StringAction setActivity;
        public NewSquadAction newSquad;
        public SquadAction setSquad;
        public EntityAction moveBase;
        public Action back;
        public SelfAction reload;
        public InterrogateAction setActivityInterrogate;
        public InterrogateTacticAction toggleInterrogationTactic;
        public SelfAction fireLiberal;

        public delegate void SelfAction(Entity entity);
        public delegate void EntityAction(Entity entity, Entity item);
        public delegate void StringAction(Entity entity, string text);
        public delegate void SquadAction(Entity entity, LiberalCrimeSquad.Squad squad);
        public delegate void InterrogateAction(Entity entity, Entity target);
        public delegate void InterrogateTacticAction(Entity entity, Hostage.Tactics tactics);
        public delegate LiberalCrimeSquad.Squad NewSquadAction(Entity e, string name);
    }
}
