using System;
using System.Collections.Generic;
using System.Xml;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Item;
using LCS.Engine.Data;

namespace LCS.Engine.Components.Location
{
    public class TroubleSpot : Component
    {
        public bool fireAlarmTriggered;

        [SimpleSave]
        public bool mapped;
        [SimpleSave]
        public int highSecurity;
        [SimpleSave]
        public int closed;        
        
        public List<Entity[,]> map { get; set; }
        [SimpleSave]
        public int startX;
        [SimpleSave]
        public int startY;
        [SimpleSave]
        public int startZ;
        [SimpleSave]
        public int mapSeed;

        public Dictionary<Position, TileBase.Graffiti> graffitiList;

        public TroubleSpot()
        {
            graffitiList = new Dictionary<Position, TileBase.Graffiti>();
        }

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("TroubleSpot");
                entityNode.AppendChild(saveNode);
            }

            saveSimpleFields();

            if (saveNode.SelectSingleNode("graffiti") != null)
                saveNode.RemoveChild(saveNode.SelectSingleNode("graffiti"));

            XmlNode graffitiNode = saveNode.OwnerDocument.CreateElement("graffiti");
            saveNode.AppendChild(graffitiNode);

            foreach(Position p in graffitiList.Keys) { 
                XmlNode graffitiSpotNode = graffitiNode.OwnerDocument.CreateElement("graffitispot");
                XmlNode positionNode = graffitiNode.OwnerDocument.CreateElement("position");
                positionNode.InnerText = p.x + "," + p.y + "," + p.z;
                XmlNode typeNode = graffitiNode.OwnerDocument.CreateElement("type");
                typeNode.InnerText = graffitiList[p] + "";
                graffitiSpotNode.AppendChild(positionNode);
                graffitiSpotNode.AppendChild(typeNode);
                graffitiNode.AppendChild(graffitiSpotNode);                        
            }
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            loadSimpleFields(componentData, entityList);

            LocationDef.TroubleSpotDef troubleDef = ((LocationDef.TroubleSpotDef)GameData.getData().locationList[owner.def].components["trouble"]);

            if (mapSeed == 0)
            {
                Factories.WorldFactory.mapBuilder(troubleDef, this);
            }
            else
            {
                Factories.WorldFactory.generateSite(!troubleDef.map.Contains(".tmx") ? troubleDef.map : "GENERIC_UNSECURE", this, mapSeed);
            }

            foreach(XmlNode node in componentData.SelectSingleNode("graffiti").ChildNodes)
            {
                string[] p = node.SelectSingleNode("position").InnerText.Split(',');

                map[int.Parse(p[2])][int.Parse(p[0]), int.Parse(p[1])].getComponent<TileBase>().graffiti = (TileBase.Graffiti)Enum.Parse(typeof(TileBase.Graffiti), node.SelectSingleNode("type").InnerText);
            }

            updateGraffitiList();
        }

        public override void subscribe()
        {
            MasterController.GetMC().nextDay += doNextDay;
        }

        public override void selfSubscribe()
        {
            base.selfSubscribe();
            getComponent<SiteBase>().dropItem += doDropItem;
        }

        public override void unsubscribe()
        {
            base.unsubscribe();
            getComponent<SiteBase>().dropItem -= doDropItem;
        }

        public void resetMap(bool siege = false)
        {
            MasterController mc = MasterController.GetMC();

            fireAlarmTriggered = false;
            if (hasComponent<SafeHouse>() && getComponent<SafeHouse>().owned)
                mapped = true;
            if (hasComponent<SafeHouse>() && getComponent<SafeHouse>().underSiege && getComponent<SafeHouse>().lightsOff)
                mapped = false;

            int graffiticount = 0;
            List<Position> validGraffitiSpots = new List<Position>();
            foreach(Entity[,] floor in map)
            {
                int z = map.IndexOf(floor);
                for(int x = 0; x < floor.GetLength(0); x++)
                {
                    for(int y = 0;y < floor.GetLength(1); y++)
                    {
                        floor[x, y].getComponent<TileBase>().mapped = mapped;
                        floor[x, y].getComponent<TileBase>().loot.Clear();
                        floor[x, y].getComponent<TileBase>().cash = 0;
                        floor[x, y].getComponent<TileBase>().trapped = false;
                        //TODO: Remember "DEBRIS" state?
                        floor[x, y].getComponent<TileBase>().fireState = TileBase.FireState.NONE;

                        floor[x, y].getComponent<TileBase>().bloodBlast = TileBase.Bloodstain.NONE;
                        floor[x, y].getComponent<TileBase>().bloodTrail_E = TileBase.Bloodstain.NONE;
                        floor[x, y].getComponent<TileBase>().bloodTrail_W = TileBase.Bloodstain.NONE;
                        floor[x, y].getComponent<TileBase>().bloodTrail_N = TileBase.Bloodstain.NONE;
                        floor[x, y].getComponent<TileBase>().bloodTrail_S = TileBase.Bloodstain.NONE;
                        floor[x, y].getComponent<TileBase>().bloodTrail_Standing = false;
                        floor[x, y].getComponent<TileBase>().someoneDiedHere = false;
                        floor[x, y].getComponent<TileBase>().bloodPrints_E_W = false;
                        floor[x, y].getComponent<TileBase>().bloodPrints_W_W = false;
                        floor[x, y].getComponent<TileBase>().bloodPrints_N_N = false;
                        floor[x, y].getComponent<TileBase>().bloodPrints_S_N = false;
                        floor[x, y].getComponent<TileBase>().bloodPrints_E_E = false;
                        floor[x, y].getComponent<TileBase>().bloodPrints_W_E = false;
                        floor[x, y].getComponent<TileBase>().bloodPrints_N_S = false;
                        floor[x, y].getComponent<TileBase>().bloodPrints_S_S = false;

                        if (floor[x, y].hasComponent<TileDoor>())
                        {
                            floor[x, y].getComponent<TileDoor>().open = false;
                            floor[x, y].getComponent<TileDoor>().triedUnlock = false;
                        }

                        if (floor[x, y].hasComponent<TileSpecial>())
                        {
                            if(!siege)
                                floor[x, y].getComponent<TileSpecial>().used = false;
                            else
                                floor[x, y].getComponent<TileSpecial>().used = true;
                            //Hide CCS boss spawns if the CCS no longer owns this safehouse
                            if (floor[x,y].getComponent<TileSpecial>().name == "CCS_BOSS" && 
                                hasComponent<SafeHouse>() && 
                                getComponent<SafeHouse>().owned)
                                floor[x, y].getComponent<TileSpecial>().used = true;
                        }

                        if (!siege)
                        {
                            if (!floor[x, y].hasComponent<TileDoor>() &&
                                !floor[x, y].hasComponent<TileWall>() &&
                                !floor[x,y].hasComponent<TileSpecial>() &&
                                floor[x, y].getComponent<TileBase>().restricted &&
                                MasterController.GetMC().LCSRandom(10) == 0)
                            {
                                Entity loot = getLootItem();
                                if (loot != null) floor[x, y].getComponent<TileBase>().loot.Add(loot);
                            }
                        }

                        if(floor[x,y].getComponent<TileBase>().graffiti != TileBase.Graffiti.NONE)
                        {
                            graffiticount++;
                        }

                        if (hasAdjacentWall(new Position(x, y, z)) && map[z][x, y].getComponent<TileBase>().graffiti == TileBase.Graffiti.NONE && map[z][x, y].getComponent<TileBase>().isWalkable())
                        {
                            validGraffitiSpots.Add(new Position(x, y, z));
                        }

                        //Unlock doors if you own the place and it's not an apartment
                        if (floor[x, y].hasComponent<TileDoor>() && floor[x, y].getComponent<TileDoor>().locked &&
                            hasComponent<SafeHouse>() && getComponent<SafeHouse>().owned &&
                            (getFlags() & LocationDef.TroubleSpotFlag.RESIDENTIAL) == 0)
                            floor[x, y].getComponent<TileDoor>().open = true;
                    }
                }
            }

            //If there is not enough graffiti, add some
            while(graffiticount < getGraffitiQuota() && validGraffitiSpots.Count > 0)
            {
                Position p = validGraffitiSpots[mc.LCSRandom(validGraffitiSpots.Count)];

                if(hasComponent<SafeHouse>() && getComponent<SafeHouse>().owned)
                    map[p.z][p.x, p.y].getComponent<TileBase>().graffiti = TileBase.Graffiti.LCS;
                else
                    map[p.z][p.x, p.y].getComponent<TileBase>().graffiti = TileBase.Graffiti.GNG;
                graffiticount++;

                validGraffitiSpots.Remove(p);
            }

            if (siege)
            {
                List<Entity> openTiles = new List<Entity>();
                foreach (Entity[,] floor in map)
                {
                    for (int x = 0; x < floor.GetLength(0); x++)
                    {
                        for (int y = 0; y < floor.GetLength(1); y++)
                        {
                            if (!floor[x, y].hasComponent<TileDoor>() &&
                                !floor[x, y].hasComponent<TileWall>() &&
                                floor[x,y].hasComponent<TileFloor>() &&
                                floor[x,y].getComponent<TileFloor>().type != TileFloor.Type.EXIT)
                            {
                                openTiles.Add(floor[x, y]);
                            }
                        }
                    }
                }

                List<Entity> lootSpots = new List<Entity>();

                for (int i = 0; i < 10; i++)
                    lootSpots.Add(openTiles[MasterController.GetMC().LCSRandom(openTiles.Count)]);

                foreach (Entity e in getComponent<SafeHouse>().getInventory())
                {
                    lootSpots[MasterController.GetMC().LCSRandom(lootSpots.Count)].getComponent<TileBase>().loot.Add(e);
                }
            }
        }

        public void advanceFires()
        {
            MasterController mc = MasterController.GetMC();
            foreach (Entity[,] floor in map)
            {
                for (int x = 0; x < floor.GetLength(0); x++)
                {
                    for (int y = 0; y < floor.GetLength(1); y++)
                    {
                        if(floor[x,y].getComponent<TileBase>().fireState == TileBase.FireState.END && mc.LCSRandom(15) == 0)
                        {
                            floor[x, y].getComponent<TileBase>().fireState = TileBase.FireState.DEBRIS;
                        }

                        if(floor[x, y].getComponent<TileBase>().fireState == TileBase.FireState.PEAK)
                        {
                            fireAlarmTriggered = true;
                            if(mc.LCSRandom(10) == 0)
                            {
                                floor[x, y].getComponent<TileBase>().fireState = TileBase.FireState.END;
                            }
                            else if(mc.LCSRandom(4) == 0)
                            {
                                int direction = mc.LCSRandom(4);

                                int tries = 0;

                                while(tries < 4)
                                {
                                    int xmod = 0;
                                    int ymod = 0;

                                    switch (direction)
                                    {
                                        case 0: xmod = -1; break;
                                        case 1: xmod = 1; break;
                                        case 2: ymod = -1; break;
                                        case 3: ymod = 1; break;
                                    }

                                    if(x + xmod < floor.GetLength(0) && x + xmod >= 0 && y + ymod < floor.GetLength(1) && y + ymod >= 0 &&
                                        floor[x + xmod, y + ymod].getComponent<TileBase>().fireState == TileBase.FireState.NONE &&
                                        (!floor[x + xmod, y + ymod].hasComponent<TileWall>() || !floor[x + xmod, y + ymod].getComponent<TileWall>().metal) &&
                                        (!floor[x + xmod, y + ymod].hasComponent<TileSpecial>() || floor[x + xmod, y + ymod].getComponent<TileSpecial>().isFlammable()))
                                    {
                                        floor[x + xmod, y + ymod].getComponent<TileBase>().fireState = TileBase.FireState.START;
                                        break;
                                    }

                                    tries++;
                                    direction++;
                                    direction %= 4;
                                }
                                if(tries == 5)
                                {
                                    if(map.IndexOf(floor) + 1 < map.Count)
                                    {
                                        Entity[,] upperFloor = map[map.IndexOf(floor) + 1];
                                        if (x < upperFloor.GetLength(0) && x >= 0 && y < upperFloor.GetLength(1) && y >= 0 &&
                                        upperFloor[x, y].getComponent<TileBase>().fireState == TileBase.FireState.NONE &&
                                        (!upperFloor[x, y].hasComponent<TileWall>() || !upperFloor[x, y].getComponent<TileWall>().metal) &&
                                        (!upperFloor[x, y].hasComponent<TileSpecial>() || upperFloor[x, y].getComponent<TileSpecial>().isFlammable()))
                                        {
                                            upperFloor[x, y].getComponent<TileBase>().fireState = TileBase.FireState.START;
                                        }
                                    }
                                }
                            }
                        }

                        if (floor[x, y].getComponent<TileBase>().fireState == TileBase.FireState.START)
                        {
                            if(mc.LCSRandom(5) == 0)
                            {
                                floor[x, y].getComponent<TileBase>().fireState = TileBase.FireState.PEAK;
                                if (mc.currentSiteModeScene != null)
                                    mc.currentSiteModeScene.siteCrime += 5;
                            }
                        }
                    }
                }
            }
        }

        private void doDropItem(object sender, Events.DropItem args)
        {
            //Some bases are also trouble spots, so this should only fire if they are currently causing trouble.
            if (MasterController.GetMC().phase == MasterController.Phase.TROUBLE || (hasComponent<SafeHouse>() && getComponent<SafeHouse>().underAttack && MasterController.GetMC().currentSiteModeScene != null))
            {
                if (MasterController.GetMC().currentSiteModeScene != null)
                {
                    Position p = MasterController.GetMC().currentSiteModeScene.squadPosition;
                    map[p.z][p.x, p.y].getComponent<TileBase>().loot.Add(args.item);
                    args.item.getComponent<ItemBase>().Location = owner;
                }
            }
        }

        private void doNextDay(object sender, EventArgs args)
        {
            if (closed <= 0 && highSecurity > 0) highSecurity--;
            if (closed > 0) closed--;
        }

        public void reveal(int x, int y, int z)
        {
            int maxX = map[z].GetLength(0) - 1;
            int maxY = map[z].GetLength(1) - 1;

            if ((map[z][x, y].hasComponent<TileWall>() || (map[z][x, y].hasComponent<TileDoor>() && !map[z][x, y].getComponent<TileDoor>().open) ||
                (map[z][x, y].hasComponent<TileSpecial>() && map[z][x, y].getComponent<TileSpecial>().linkWalls()))
                && map[z][x,y].getComponent<TileBase>().fireState < TileBase.FireState.PEAK)
                return;

            map[z][x, y].getComponent<TileBase>().mapped = true;

            if (x < maxX)
                map[z][x + 1, y].getComponent<TileBase>().mapped = true;
            if (x > 0)
                map[z][x - 1, y].getComponent<TileBase>().mapped = true;
            if (y < maxY)
                map[z][x, y + 1].getComponent<TileBase>().mapped = true;
            if (y > 0)
                map[z][x, y - 1].getComponent<TileBase>().mapped = true;

            if (x < maxX && y < maxY)
                map[z][x + 1, y + 1].getComponent<TileBase>().mapped = true;
            if (x < maxX && y > 0)
                map[z][x + 1, y - 1].getComponent<TileBase>().mapped = true;
            if (x > 0 && y < maxY)
                map[z][x - 1, y + 1].getComponent<TileBase>().mapped = true;
            if (x > 0 && y > 0)
                map[z][x - 1, y - 1].getComponent<TileBase>().mapped = true;
        }

        public List<Entity> generateEncounter(int alarmTimer)
        {
            Dictionary<string, int> encounterList = new Dictionary<string, int>();

            LocationDef.EnemyType responseType = getResponseType();
            if (hasComponent<SafeHouse>() && getComponent<SafeHouse>().owned && responseType == LocationDef.EnemyType.CCS)
                responseType = LocationDef.EnemyType.POLICE;

            if (alarmTimer > 80)
            {
                switch (responseType)
                {
                    case LocationDef.EnemyType.ARMY:
                        encounterList["SOLDIER"] = 1000;
                        encounterList["MILITARYPOLICE"] = 300;
                        encounterList["MILITARYOFFICER"] = 150;
                        encounterList["SEAL"] = 150;
                        encounterList["GUARDDOG"] = 100;
                        encounterList["TANK"] = 100;
                        break;
                    case LocationDef.EnemyType.AGENT:
                        encounterList["AGENT"] = 1000;
                        encounterList["MILITARYOFFICER"] = 100;
                        encounterList["GUARDDOG"] = 50;
                        break;
                    case LocationDef.EnemyType.MERC:
                        encounterList["MERC"] = 1000;
                        break;
                    case LocationDef.EnemyType.REDNECK:
                        encounterList["HICK"] = 1000;
                        break;
                    case LocationDef.EnemyType.GANG:
                        encounterList["GANGMEMBER"] = 1000;
                        break;
                    case LocationDef.EnemyType.CCS:
                        encounterList["CCS_VIGILANTE"] = 1000;
                        encounterList["CCS_SNIPER"] = 100;
                        encounterList["CCS_MOLOTOV"] = 100;
                        break;
                    default:
                        if (MasterController.government.laws["POLICE"].alignment == Alignment.ARCHCONSERVATIVE &&
                            MasterController.government.laws["DEATH_PENALTY"].alignment == Alignment.ARCHCONSERVATIVE)
                            encounterList["DEATHSQUAD"] = 1000;
                        else if ((getFlags() & LocationDef.TroubleSpotFlag.HIGH_SECURITY) != 0)
                            encounterList["SWAT"] = 1000;
                        else {
                            if (MasterController.government.laws["POLICE"].alignment <= Alignment.CONSERVATIVE)
                                encounterList["GANGUNIT"] = 1000;
                            else
                            {
                                encounterList["COP"] = 1000;
                                if (MasterController.government.laws["POLICE"].alignment == Alignment.ELITE_LIBERAL)
                                    encounterList["NEGOTIATOR"] = 500;
                            }
                        }
                        break;
                }
                if (fireAlarmTriggered && MasterController.government.laws["FREE_SPEECH"].alignment > Alignment.ARCHCONSERVATIVE)
                    encounterList["FIREFIGHTER"] = 1000;
            }

            //Special CCS handling to add conservative spawns to CCS owned safehouses
            if (hasComponent<SafeHouse>() &&
                !getComponent<SafeHouse>().owned &&
                (getComponent<SafeHouse>().getFlags() & LocationDef.BaseFlag.CCS_BASE) != 0 &&
                MasterController.ccs.status >= ConservativeCrimeSquad.Status.ACTIVE)
            {
                if(alarmTimer > 80 && responseType == LocationDef.EnemyType.CCS)
                    encounterList["CCS_VIGILANTE"] = 1000;
                else
                    encounterList["CCS_VIGILANTE"] = 50;
                encounterList["PROSTITUTE"] = 5;
                encounterList["CRACKHEAD"] = 5;
                encounterList["PRIEST"] = 5;
                encounterList["RADIOPERSONALITY"] = 1;
            }
            else
            {
                foreach (LocationDef.EncounterDef enc in getEncounters())
                {
                    if (MasterController.GetMC().testCondition(enc.conditions, this))
                    {
                        if ((enc.creatureType.flags & CreatureDef.CreatureFlag.CCS) != 0)
                        {
                            if (!encounterList.ContainsKey(enc.creatureType.type))
                                encounterList[enc.creatureType.type] = enc.weight * (int)MasterController.ccs.status;
                            else
                                encounterList[enc.creatureType.type] += enc.weight * (int)MasterController.ccs.status;
                        }
                        else
                        {
                            if (!encounterList.ContainsKey(enc.creatureType.type))
                                encounterList[enc.creatureType.type] = enc.weight;
                            else
                                encounterList[enc.creatureType.type] += enc.weight;
                        }
                    }
                }
            }

            List<Entity> encounterEntities = new List<Entity>();

            for (int i = 0; i < MasterController.GetMC().LCSRandom(6) + 1; i++)
            {
                Entity creature = Factories.CreatureFactory.create(MasterController.GetMC().WeightedRandom(encounterList));
                if (creature != null) encounterEntities.Add(creature);
                creature.getComponent<CreatureBase>().Location = owner;
            }

            //Special handling for crackhouse where all gang members will be conservative until taken over by the LCS
            if(owner.def == "BUSINESS_CRACKHOUSE" && (!getComponent<SafeHouse>().owned || alarmTimer > 80))
            {
                foreach(Entity e in encounterEntities)
                {
                    if(e.def == "GANGMEMBER")
                    {
                        e.getComponent<CreatureInfo>().alignment = Alignment.CONSERVATIVE;
                    }
                }
            }

            return encounterEntities;
        }

        public Entity getLootItem()
        {
            List<LocationDef.TroubleLootDef> weightedSelection = MasterController.GetMC().WeightedRandom(((LocationDef.TroubleSpotDef)GameData.getData().locationList[owner.def].components["trouble"]).lootTable);
            if (weightedSelection == null) return null;
            List<string> itemsToSelect = new List<string>();
            foreach(LocationDef.TroubleLootDef lootDef in weightedSelection)
            {
                if (MasterController.GetMC().testCondition(lootDef.condition))
                    itemsToSelect.Add(lootDef.item.type);
            }

            if (itemsToSelect.Count == 0) return null;

            Entity item = Factories.ItemFactory.create(itemsToSelect[MasterController.GetMC().LCSRandom(itemsToSelect.Count)]);
            if (item.hasComponent<Weapon>())
            {
                if(item.getComponent<Weapon>().getAmmoType() != "NONE" && (MasterController.GetMC().LCSRandom(2) == 0 || (item.getComponent<Weapon>().getFlags() & ItemDef.WeaponFlags.ALWAYS_LOADED) != 0))
                {
                    item.getComponent<Weapon>().clip = Factories.ItemFactory.create(item.getComponent<Weapon>().getDefaultClip().type);
                }
            }

            return item;
        }

        public bool hasAdjacentWall(Position p)
        {
            if(p.x > 0)
            {
                if (map[p.z][p.x - 1, p.y].hasComponent<TileWall>() && map[p.z][p.x - 1, p.y].getComponent<TileBase>().fireState < TileBase.FireState.PEAK)
                    return true;
            }

            if(p.x < map[p.z].GetLength(0) - 1)
            {
                if (map[p.z][p.x + 1, p.y].hasComponent<TileWall>() && map[p.z][p.x + 1, p.y].getComponent<TileBase>().fireState < TileBase.FireState.PEAK)
                    return true;
            }

            if (p.y > 0)
            {
                if (map[p.z][p.x, p.y - 1].hasComponent<TileWall>() && map[p.z][p.x, p.y - 1].getComponent<TileBase>().fireState < TileBase.FireState.PEAK)
                    return true;
            }

            if (p.y < map[p.z].GetLength(1) - 1)
            {
                if (map[p.z][p.x, p.y + 1].hasComponent<TileWall>() && map[p.z][p.x, p.y + 1].getComponent<TileBase>().fireState < TileBase.FireState.PEAK)
                    return true;
            }

            return false;
        }

        public void updateGraffitiList()
        {
            graffitiList.Clear();

            for (int z = 0; z < map.Count; z++)
            {
                for (int x = 0; x < map[z].GetLength(0); x++)
                {
                    for (int y = 0; y < map[z].GetLength(1); y++)
                    {
                        if (map[z][x, y].getComponent<TileBase>().graffiti != TileBase.Graffiti.NONE)
                        {
                            graffitiList.Add(new Position(x, y, z), map[z][x, y].getComponent<TileBase>().graffiti);
                        }
                    }
                }
            }
        }

        public LocationDef.EnemyType getResponseType()
        { return ((LocationDef.TroubleSpotDef)GameData.getData().locationList[owner.def].components["trouble"]).responseType; }

        public List<LocationDef.EncounterDef> getEncounters()
        { return ((LocationDef.TroubleSpotDef)GameData.getData().locationList[owner.def].components["trouble"]).encounters; }

        public LocationDef.TroubleSpotFlag getFlags()
        { return ((LocationDef.TroubleSpotDef)GameData.getData().locationList[owner.def].components["trouble"]).flags; }

        public List<ViewDef> getViews()
        { return ((LocationDef.TroubleSpotDef)GameData.getData().locationList[owner.def].components["trouble"]).affectedViews; }

        public List<LocationDef.DisguiseDef> getValidDisguises()
        { return ((LocationDef.TroubleSpotDef)GameData.getData().locationList[owner.def].components["trouble"]).appropriateDisguises; }

        public List<LocationDef.DisguiseDef> getPartialDisguises()
        { return ((LocationDef.TroubleSpotDef)GameData.getData().locationList[owner.def].components["trouble"]).partialDisguises; }

        public int getGraffitiQuota()
        { return ((LocationDef.TroubleSpotDef)GameData.getData().locationList[owner.def].components["trouble"]).graffiti; }

        public ViewDef getNewsHeader()
        { return ((LocationDef.TroubleSpotDef)GameData.getData().locationList[owner.def].components["trouble"]).newsHeader; }
    }
}
