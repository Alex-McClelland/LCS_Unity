using LCS.Engine.Events;
using LCS.Engine.Components.World;
using System;
using System.Xml;
using System.Collections.Generic;

namespace LCS.Engine.Components.Creature
{
    public class Politician : Component
    {
        //Politicians, in addition to their Entity alignment, have more nuanced legal alignments.
        [SimpleSave]
        public Alignment alignment;
        //Position should be used to determine which encounters they can appear in, as well as who to replace when they die
        [SimpleSave]
        public string position;
        [SimpleSave]
        public Alignment party;

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Politician");
                entityNode.AppendChild(saveNode);
            }

            saveSimpleFields();
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);
        }

        public override void selfSubscribe()
        {
            base.selfSubscribe();
            getComponent<CreatureBase>().die += doDie;
        }

        public override void unsubscribe()
        {
            base.unsubscribe();
            getComponent<CreatureBase>().die -= doDie;
        }

        private void doDie(object sender, Die args)
        {
            MasterController.government.politicianDied(owner);
        }
    }
}
