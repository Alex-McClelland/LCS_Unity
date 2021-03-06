using System;
using System.Collections.Generic;
using System.Xml;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Creature;
using LCS.Engine.Components.Location;
using LCS.Engine.Data;

namespace LCS.Engine.Factories
{
    public static class WorldFactory
    {
        public static Entity create(string idname)
        {
            MasterController mc = MasterController.GetMC();
            GameData dl = GameData.getData();

            if (!dl.nationList.ContainsKey(idname))
            {
                MasterController.GetMC().addErrorMessage("No world def found for " + idname);
                return null;
            }

            //basic setup
            Entity world = new Entity("world", idname);

            HighScore highscore = new HighScore();
            world.setComponent(highscore);

            Government government = new Government();
            world.setComponent(government);

            foreach (LawDef law in dl.lawList.Values)
            {
                government.laws.Add(law.type, new Government.Law(law.type, Alignment.MODERATE));
            }

            setLawValues(government);
            
            //A lot of this stuff is just hard-coded for now, but could be externalized later
            government.president = CreatureFactory.create("EXECUTIVE_ARCHCONSERVATIVE");
            government.president.persist();
            Politician politicianComponent = new Politician();
            politicianComponent.position = "PRESIDENT";
            politicianComponent.alignment = Alignment.ARCHCONSERVATIVE;
            politicianComponent.party = Alignment.CONSERVATIVE;
            government.president.setComponent(politicianComponent);
            government.presidentTerm = 1;
            government.president.getComponent<CreatureInfo>().encounterName = "President " + government.president.getComponent<CreatureInfo>().surname;

            government.vicePresident = CreatureFactory.create("EXECUTIVE_ARCHCONSERVATIVE");
            government.vicePresident.persist();
            politicianComponent = new Politician();
            politicianComponent.position = "VICE_PRESIDENT";
            politicianComponent.alignment = Alignment.ARCHCONSERVATIVE;
            politicianComponent.party = Alignment.CONSERVATIVE;
            government.vicePresident.setComponent(politicianComponent);
            government.vicePresident.getComponent<CreatureInfo>().encounterName = "Vice President " + government.vicePresident.getComponent<CreatureInfo>().surname;

            government.secretaryOfState = CreatureFactory.create("EXECUTIVE_ARCHCONSERVATIVE");
            government.secretaryOfState.persist();
            politicianComponent = new Politician();
            politicianComponent.position = "SECRETARY_OF_STATE";
            politicianComponent.alignment = Alignment.ARCHCONSERVATIVE;
            politicianComponent.party = Alignment.CONSERVATIVE;
            government.secretaryOfState.setComponent( politicianComponent);
            government.secretaryOfState.getComponent<CreatureInfo>().encounterName = "Secretary of State";

            government.attorneyGeneral = CreatureFactory.create("EXECUTIVE_ARCHCONSERVATIVE");
            government.attorneyGeneral.persist();
            politicianComponent = new Politician();
            politicianComponent.position = "ATTORNEY_GENERAL";
            politicianComponent.alignment = Alignment.ARCHCONSERVATIVE;
            politicianComponent.party = Alignment.CONSERVATIVE;
            government.attorneyGeneral.setComponent(politicianComponent);
            government.attorneyGeneral.getComponent<CreatureInfo>().encounterName = "Attorney General";

            fillCongress(government, idname);
            fillSupremeCourt(government, idname);            

            Public publicOpinion = new Public();
            world.setComponent(publicOpinion);
            publicOpinion.PublicMood = 30 + mc.LCSRandom(25);

            foreach(ViewDef view in dl.viewList.Values)
            {
                publicOpinion.PublicOpinion.Add(view.type, 30 + mc.LCSRandom(25));
                publicOpinion.PublicInterest.Add(view.type, 0);
                publicOpinion.BackgroundLiberalInfluence.Add(view.type, 0);

                //Skip populating poll data for the CCS issue since they won't be activated yet
                if (view.type == Constants.VIEW_CONSERVATIVECRIMESQUAD) continue;
                Public.PollData pollData = new Public.PollData();
                pollData.def = view.type;
                pollData.age = 50;
                publicOpinion.pollData.Add(view.type, pollData);
            }

            publicOpinion.PublicOpinion[Constants.VIEW_LIBERALCRIMESQUAD] = 0;
            publicOpinion.PublicOpinion[Constants.VIEW_LIBERALCRIMESQUADPOS] = 5;
            publicOpinion.PublicOpinion[Constants.VIEW_CONSERVATIVECRIMESQUAD] = 0;

            LiberalCrimeSquad lcs = new LiberalCrimeSquad();
            world.setComponent(lcs);
            lcs.slogan = "We need a slogan!";

            if (mc.LCSRandom(20) == 0)
            {
                switch (mc.LCSRandom(7))
                {
                    case 0: lcs.slogan = "To Rogues and Revolution!";
                        break;
                    case 1: lcs.slogan = "Hell yes, LCS!";
                        break;
                    case 2: lcs.slogan = "Striking high, standing tall!";
                        break;
                    case 3: lcs.slogan = "Revolution never comes with a warning!";
                        break;
                    case 4: lcs.slogan = "True Liberal Justice!";
                        break;
                    case 5: lcs.slogan = "Laissez ain't fair!";
                        break;
                    case 6: lcs.slogan = "This is a slogan!";
                        break;
                }
            }

            ConservativeCrimeSquad ccs = new ConservativeCrimeSquad();
            world.setComponent(ccs);

            Nation nation = new Nation();
            world.setComponent(nation);

            foreach(NationDef.NationCity cityDef in dl.nationList[idname].cities.Values)
            {
                Entity city = new Entity("city", cityDef.id);
                city.setComponent(new City());
                city.getComponent<City>().idname = cityDef.id;
                city.getComponent<City>().name = cityDef.name;
                city.getComponent<City>().shortname = cityDef.shortname;

                foreach (NationDef.NationDistrict districtDef in cityDef.districts)
                {
                    city.getComponent<City>().locations.Add(districtDef.name, new List<Entity>());

                    foreach (LocationDef location in districtDef.sites.Values)
                    {
                        Entity site = buildLocation(location.type);
                        site.getComponent<SiteBase>().city = city;
                        city.getComponent<City>().locations[districtDef.name].Add(site);
                    }
                }

                nation.cities.Add(cityDef.id, city);
            }

            News news = new News();
            news.newsCherryBusted = false;
            world.setComponent(news);
                        
            return world;
        }

        private static void setLawValues(Government g)
        {
            g.laws[Constants.LAW_ABORTION].alignment = Alignment.LIBERAL;
            g.laws[Constants.LAW_ANIMAL_RESEARCH].alignment = Alignment.CONSERVATIVE;
            g.laws[Constants.LAW_POLICE].alignment = Alignment.CONSERVATIVE;
            g.laws[Constants.LAW_PRIVACY].alignment = Alignment.CONSERVATIVE;
            g.laws[Constants.LAW_DEATH_PENALTY].alignment = Alignment.CONSERVATIVE;
            g.laws[Constants.LAW_NUCLEAR_POWER].alignment = Alignment.CONSERVATIVE;
            g.laws[Constants.LAW_POLLUTION].alignment = Alignment.CONSERVATIVE;
            g.laws[Constants.LAW_LABOR].alignment = Alignment.MODERATE;
            g.laws[Constants.LAW_GAY].alignment = Alignment.LIBERAL;
            g.laws[Constants.LAW_CORPORATE].alignment = Alignment.MODERATE;
            g.laws[Constants.LAW_FREE_SPEECH].alignment = Alignment.MODERATE;
            g.laws[Constants.LAW_FLAG_BURNING].alignment = Alignment.LIBERAL;
            g.laws[Constants.LAW_GUN_CONTROL].alignment = Alignment.CONSERVATIVE;
            g.laws[Constants.LAW_TAX].alignment = Alignment.MODERATE;
            g.laws[Constants.LAW_WOMEN].alignment = Alignment.LIBERAL;
            g.laws[Constants.LAW_CIVIL_RIGHTS].alignment = Alignment.LIBERAL;
            g.laws[Constants.LAW_DRUGS].alignment = Alignment.CONSERVATIVE;
            g.laws[Constants.LAW_IMMIGRATION].alignment = Alignment.MODERATE;
            g.laws[Constants.LAW_ELECTION].alignment = Alignment.MODERATE;
            g.laws[Constants.LAW_MILITARY].alignment = Alignment.CONSERVATIVE;
            g.laws[Constants.LAW_PRISON].alignment = Alignment.MODERATE;
            g.laws[Constants.LAW_TORTURE].alignment = Alignment.CONSERVATIVE;
        }

        public static Entity buildLocation(string idname)
        {
            MasterController mc = MasterController.GetMC();

            Entity location = new Entity("location", idname);

            LocationDef locationDef = GameData.getData().locationList[idname];

            SiteBase site = new SiteBase();

            List<SiteBase.Name> names = new List<SiteBase.Name>();

            foreach(ConditionalName name in locationDef.names)
            {
                string newName = name.name;

                if (newName.Contains("$SURNAME")){
                    List<string> nameTable = new List<string>();
                    nameTable.AddRange(GameData.getData().nameLists["surname_archconservative"]);
                    nameTable.AddRange(GameData.getData().nameLists["surname_regular"]);

                    newName = newName.Replace("$SURNAME", nameTable[mc.LCSRandom(nameTable.Count)]);
                }

                if (newName.Contains("|")){
                    string[] temp = newName.Split(' ');

                    newName = "";

                    for(int i =0;i<temp.Length;i++)
                    {
                        if (temp[i].Contains("|"))
                        {
                            string[] temp2 = temp[i].Split('|');

                            temp[i] = temp2[MasterController.GetMC().LCSRandom(temp2.Length)];
                        }

                        newName += temp[i] + " ";
                    }

                    newName = newName.TrimEnd(' ');                    
                }

                if(name.condition == "")
                {
                    names.Add(new SiteBase.Name(newName, name.shortName));
                }
                else
                {
                    site.conditionalNames.Add(name.condition, new SiteBase.Name(newName, name.shortName));
                }
            }

            site.currentName = names[mc.LCSRandom(names.Count)];
            //Save the original name so it can be restored if laws change back
            site.standardName = new SiteBase.Name(site.currentName.name, site.currentName.shortName);

            location.setComponent(site);

            foreach(LocationDef.LocationComponent component in locationDef.components.Values)
            {
                if(component.GetType() == typeof(LocationDef.BaseDef))
                {
                    LocationDef.BaseDef baseDef = (LocationDef.BaseDef) component;
                    SafeHouse safeHouse = new SafeHouse();

                    if(baseDef.rentPrice != 0 || (baseDef.flags & (LocationDef.BaseFlag.CAPTURABLE | LocationDef.BaseFlag.CCS_BASE)) != 0)
                    {
                        safeHouse.owned = false;
                    }
                    else
                    {
                        safeHouse.owned = true;
                    }

                    if((baseDef.flags & LocationDef.BaseFlag.CCS_BASE) != 0)
                    {
                        site.hidden = true;
                    }

                    safeHouse.freeRent = false;
                    safeHouse.forceEvict = false;

                    location.setComponent(safeHouse);
                }
                else if(component.GetType() == typeof(LocationDef.TroubleSpotDef))
                {
                    LocationDef.TroubleSpotDef troubleSpotDef = (LocationDef.TroubleSpotDef) component;
                    TroubleSpot troubleSpot = new TroubleSpot();
                    location.setComponent(troubleSpot);
                    mapBuilder(troubleSpotDef, troubleSpot);
                }
                else if(component.GetType() == typeof(LocationDef.ShopDef))
                {
                    Shop shop = new Shop();
                    location.setComponent(shop);
                }
            }            

            return location;
        }

        public static void mapBuilder(LocationDef.TroubleSpotDef def, TroubleSpot spot)
        {
            if(def.map.Contains(".tmx"))
            {
                if (!GameData.getData().mapList.ContainsKey(def.map))
                {
                    MasterController.GetMC().addErrorMessage(def.map + " not found in maps directory.");
                    generateSite("GENERIC_UNSECURE", spot, MasterController.GetMC().LCSRandom(int.MaxValue));
                    return;
                }

                MasterController.GetMC().addDebugMessage("Building map " + def.map);

                XmlDocument doc = GameData.getData().mapList[def.map];

                XmlNode root = doc.DocumentElement;

                int width = int.Parse(root.Attributes["width"].Value);
                int height = int.Parse(root.Attributes["height"].Value);

                Dictionary<int, Dictionary<string, string>> tileParams = new Dictionary<int, Dictionary<string, string>>();

                tileParams.Add(0, new Dictionary<string, string>());
                tileParams[0].Add("id", "NONE");

                foreach (XmlNode node in root.SelectNodes("tileset"))
                {
                    int firstgid = int.Parse(node.Attributes["firstgid"].Value);
                    string source = "";

                    if(node.Attributes["source"] != null)
                        source = node.Attributes["source"].Value;

                    if (source != "")
                    {
                        try
                        {
                            XmlReaderSettings readerSettings = new XmlReaderSettings();
                            readerSettings.IgnoreComments = true;
                            XmlReader reader = XmlReader.Create(GameData.MapPath + "/" + source, readerSettings);

                            XmlDocument sourceDoc = new XmlDocument();
                            sourceDoc.Load(reader);

                            XmlNode sourceRoot = sourceDoc.DocumentElement;

                            foreach (XmlNode innerNode in sourceRoot.SelectNodes("tile"))
                            {
                                int id = int.Parse(innerNode.Attributes["id"].Value) + firstgid;
                                tileParams.Add(id, new Dictionary<string, string>());

                                foreach(XmlNode paramNode in innerNode.SelectSingleNode("properties").ChildNodes)
                                {
                                    string name = paramNode.Attributes["name"].Value;
                                    string value = paramNode.Attributes["value"].Value;

                                    tileParams[id].Add(name, value);
                                }
                            }
                        }
                        catch (System.IO.FileNotFoundException)
                        {
                            MasterController.GetMC().addErrorMessage("Error loading tileset " + source);
                        }
                    }
                    else
                    {
                        foreach (XmlNode innerNode in node.SelectNodes("tile"))
                        {
                            int id = int.Parse(innerNode.Attributes["id"].Value) + firstgid;
                            tileParams.Add(id, new Dictionary<string, string>());

                            foreach (XmlNode paramNode in innerNode.SelectSingleNode("properties").ChildNodes)
                            {
                                string name = paramNode.Attributes["name"].Value;
                                string value = paramNode.Attributes["value"].Value;

                                tileParams[id].Add(name, value);
                            }
                        }
                    }
                }

                List<Entity[,]> map = new List<Entity[,]>();

                for (int i = 0; i < root.SelectNodes("layer").Count; i+=2)
                {
                    Entity[,] floor = new Entity[width, height];                    

                    //Base terrain
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int typeID = int.Parse(root.SelectNodes("layer")[i].SelectSingleNode("data").ChildNodes[y * width + x].Attributes["gid"].Value);
                            Entity tile = buildTile(tileParams[typeID]["id"], def);
                            //tile.persist();
                            floor[x, y] = tile;
                            tile.getComponent<TileBase>().location = spot;
                            tile.getComponent<TileBase>().x = x;
                            tile.getComponent<TileBase>().y = y;                            
                        }
                    }

                    //Special tiles
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int type = int.Parse(root.SelectNodes("layer")[i+1].SelectSingleNode("data").ChildNodes[y * width + x].Attributes["gid"].Value);
                            if (type == 0) continue;

                            switch (tileParams[type]["id"])
                            {
                                case "START":
                                    //Start location (should only be one on the map, otherwise problems
                                    spot.startX = x;
                                    spot.startY = y;
                                    spot.startZ = i / 2;
                                    break;
                                case "LOCK":
                                    //Locked door
                                    if (floor[x, y].hasComponent<TileDoor>())
                                    {
                                        floor[x, y].getComponent<TileDoor>().locked = true;
                                        floor[x, y].getComponent<TileBase>().restricted = true;
                                    }
                                    break;
                                    //Locked door, but not a restricted tile
                                case "LOCK_NORESTRICT":
                                    if (floor[x, y].hasComponent<TileDoor>())
                                    {
                                        floor[x, y].getComponent<TileDoor>().locked = true;
                                    }
                                    break;
                                case "RESTRICTED":
                                    //Restricted area
                                    floor[x, y].getComponent<TileBase>().restricted = true;
                                    break;
                                case "LOCK_ALARM":
                                    //Locked + Alarmed door
                                    if (floor[x, y].hasComponent<TileDoor>())
                                    {
                                        floor[x, y].getComponent<TileDoor>().locked = true;
                                        floor[x, y].getComponent<TileDoor>().alarm = true;
                                        floor[x, y].getComponent<TileBase>().restricted = true;
                                    }
                                    break;
                                case "ALARM":
                                    //Alarmed door (emergency exit)
                                    if(floor[x, y].hasComponent<TileDoor>())
                                    {
                                        floor[x, y].getComponent<TileDoor>().alarm = true;
                                    }
                                    break;
                                case "METAL":
                                    //Reinforced wall
                                    if (floor[x, y].hasComponent<TileWall>())
                                    {
                                        floor[x, y].getComponent<TileWall>().metal = true;
                                    }
                                    break;
                                default:
                                    floor[x, y].setComponent(new TileSpecial(tileParams[type]["id"]));
                                    if (tileParams[type].ContainsKey("restricted"))
                                        floor[x, y].getComponent<TileBase>().restricted = true;
                                    break;
                            }
                        }
                    }

                    if(map.Count == spot.startZ)
                    {
                        Entity[,] exits = new Entity[width + 2, height + 2];

                        for(int j = 0; j < exits.GetLength(0); j++)
                        {
                            for(int k=0;k< exits.GetLength(1); k++)
                            {
                                if(j==0 || k==0 || j == width + 1 || k == height + 1)
                                {
                                    Entity exitTile = new Entity("tile", "EXIT");
                                    //exitTile.persist();
                                    TileBase tileBase = new TileBase();
                                    exitTile.setComponent(tileBase);
                                    tileBase.x = j;
                                    tileBase.y = k;
                                    TileFloor tileFloor = new TileFloor();
                                    exitTile.setComponent(tileFloor);
                                    tileFloor.type = TileFloor.Type.EXIT;
                                    exits[j, k] = exitTile;
                                }
                                else
                                {
                                    exits[j, k] = floor[j - 1, k - 1];
                                }
                            }
                        }

                        spot.startX += 1;
                        spot.startY += 1;
                        floor = exits;
                    }

                    map.Add(floor);
                }

                spot.map = map;
            }
            else
            {
                generateSite(def.map, spot, MasterController.GetMC().LCSRandom(int.MaxValue));
            }
        }

        private static Entity buildTile(string tileType, LocationDef.TroubleSpotDef def)
        {
            Entity tile = new Entity("tile", tileType);
            TileBase tileBase = new TileBase();
            tile.setComponent(tileBase);
            if (tileType == "NONE")
                return tile;

            TileFloor tileFloor = new TileFloor();
            tile.setComponent(tileFloor);
            tileFloor.type = TileFloor.Type.INDOOR;

            switch (tileType)
            {
                case "EXIT":
                    tileFloor.type = TileFloor.Type.EXIT;
                    break;
                case "WALL":
                    TileWall tileWall = new TileWall();
                    tile.setComponent(tileWall);
                    break;
                case "DOOR":
                    TileDoor tileDoor = new TileDoor();
                    tile.setComponent(tileDoor);
                    if ((def.flags & LocationDef.TroubleSpotFlag.HIGH_SECURITY) != 0)
                        tileDoor.difficulty = Difficulty.HARD;
                    else if ((def.flags & LocationDef.TroubleSpotFlag.MID_SECURITY) != 0)
                        tileDoor.difficulty = Difficulty.CHALLENGING;
                    else
                        tileDoor.difficulty = Difficulty.EASY;
                    break;
                case "FLOOR_INDOOR":
                    //This is a bit redundant but might as well keep it here for clarity
                    tileFloor.type = TileFloor.Type.INDOOR;
                    break;
                case "FLOOR_OUTDOOR":
                    tileFloor.type = TileFloor.Type.OUTDOOR;
                    break;
                case "FLOOR_PATH":
                    tileFloor.type = TileFloor.Type.PATH;
                    break;
                case "STAIRS_UP":
                    tileFloor.type = TileFloor.Type.STAIRS_UP;
                    break;
                case "STAIRS_DOWN":
                    tileFloor.type = TileFloor.Type.STAIRS_DOWN;
                    break;
            }

            return tile;
        }

        public static void generateSite(string siteType, TroubleSpot spot, int seed)
        {
            Random siteRand = new Random(seed);
            spot.mapSeed = seed;

            if (!GameData.getData().locationGenList.ContainsKey(siteType))
                generateSite("GENERIC_UNSECURE", spot, seed);
            else
            {
                int xmax = 0;
                int xmin = 0;
                int ymax = 0;
                int zmax = 0;
                
                string use = siteType;

                while(use != "")
                {
                    if (!GameData.getData().locationGenList.ContainsKey(use)) break;

                    foreach (LocationGenDef.LocationGenParameter parameter in GameData.getData().locationGenList[use].parameters)
                    {
                        if (parameter.xstart < xmin) xmin = parameter.xstart;
                        if (parameter.xend > xmax) xmax = parameter.xend;
                        if (parameter.yend > ymax) ymax = parameter.yend;
                        if (parameter.zend > zmax) zmax = parameter.zend;
                    }

                    use = GameData.getData().locationGenList[use].use;
                }

                spot.map = new List<Entity[,]>();

                for(int i=0; i < zmax + 1; i++)
                {
                    spot.map.Add(new Entity[Math.Abs(xmin) + Math.Abs(xmax) + 1, ymax + 1]);
                }

                List<LocationGenDef.LocationGenParameter> specialList = build(siteType, spot, siteRand);

                //Fill in any gaps left after generation finishes with wall tiles
                for(int z = 0; z < spot.map.Count; z++)
                {
                    for(int x = 0; x < spot.map[z].GetLength(0); x++)
                    {
                        for(int y = 0; y < spot.map[z].GetLength(1); y++)
                        {
                            if(spot.map[z][x,y] == null)
                            {
                                spot.map[z][x, y] = buildTile("WALL", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                            }
                        }
                    }
                }

                //CLEAR AWAY BLOCKED DOORWAYS
                for (int z = 0; z < spot.map.Count; z++)
                    for (int x = 0; x < spot.map[z].GetLength(0); x++)
                        for (int y = 0; y < spot.map[z].GetLength(1); y++)
                            if (spot.map[z][x,y].hasComponent<TileDoor>())
                            {  // Check what sides are blocked around the door
                                bool[] block = { true, true, true, true };
                                if (x > 0) if (!(spot.map[z][x - 1, y].hasComponent<TileWall>() || spot.map[z][x - 1, y].hasComponent<TileDoor>())) { block[1] = false; }
                                if (x < spot.map[z].GetLength(0) - 1) if (!(spot.map[z][x + 1, y].hasComponent<TileWall>() || spot.map[z][x + 1, y].hasComponent<TileDoor>())) { block [2] = false; }
                                if (y > 0) if (!(spot.map[z][x, y - 1].hasComponent<TileWall>() || spot.map[z][x, y - 1].hasComponent<TileDoor>())) { block[0] = false; }
                                if (y < spot.map[z].GetLength(1) - 1) if (!(spot.map[z][x, y + 1].hasComponent<TileWall>() || spot.map[z][x, y + 1].hasComponent<TileDoor>())) { block[3] = false; }
                                // Blast open everything around a totally blocked door
                                // (door will later be deleted)
                                if (block[0] && block[1] && block[2] && block[3])
                                {
                                    if (x > 0)
                                    {
                                        int x1 = x - 1;
                                        do
                                        {
                                            bool restricted = false;
                                            if (spot.map[z][x1, y].getComponent<TileBase>().restricted) restricted = true;
                                            spot.map[z][x1, y] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                            spot.map[z][x1, y].getComponent<TileBase>().restricted = restricted;
                                            x1--;
                                        } while (!(y == spot.map[z].GetLength(1) - 1 || spot.map[z][x1, y + 1].hasComponent<TileWall>() || spot.map[z][x1, y + 1].hasComponent<TileDoor>()) &&
                                                !(y == 0 || spot.map[z][x1, y - 1].hasComponent<TileWall>() || spot.map[z][x1, y - 1].hasComponent<TileDoor>()));
                                    }
                                    else
                                    {
                                        bool restricted = false;
                                        if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                        spot.map[z][x, y] = buildTile("WALL", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                        spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                                    }

                                    if (x < spot.map[z].GetLength(0) - 1)
                                    {
                                        int x1 = x + 1;
                                        do
                                        {
                                            bool restricted = false;
                                            if (spot.map[z][x1, y].getComponent<TileBase>().restricted) restricted = true;
                                            spot.map[z][x1, y] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                            spot.map[z][x1, y].getComponent<TileBase>().restricted = restricted;
                                            x1++;
                                        } while (!(y == spot.map[z].GetLength(1) - 1 || spot.map[z][x1, y + 1].hasComponent<TileWall>() || spot.map[z][x1, y + 1].hasComponent<TileDoor>()) &&
                                                !(y == 0 || spot.map[z][x1, y - 1].hasComponent<TileWall>() || spot.map[z][x1, y - 1].hasComponent<TileDoor>()));
                                    }
                                    else
                                    {
                                        bool restricted = false;
                                        if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                        spot.map[z][x, y] = buildTile("WALL", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                        spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                                    }

                                    if (y > 0)
                                    {
                                        int y1 = y - 1;
                                        do
                                        {
                                            bool restricted = false;
                                            if (spot.map[z][x, y1].getComponent<TileBase>().restricted) restricted = true;
                                            spot.map[z][x, y1] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                            spot.map[z][x, y1].getComponent<TileBase>().restricted = restricted;
                                            y1--;
                                        } while (!(x == spot.map[z].GetLength(0) - 1 || spot.map[z][x + 1, y1].hasComponent<TileWall>() || spot.map[z][x + 1, y1].hasComponent<TileDoor>()) &&
                                                !(x == 0 || spot.map[z][x - 1, y1].hasComponent<TileWall>() || spot.map[z][x - 1, y1].hasComponent<TileDoor>()));
                                    }
                                    else
                                    {
                                        bool restricted = false;
                                        if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                        spot.map[z][x, y] = buildTile("WALL", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                        spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                                    }

                                    if (y < spot.map[z].GetLength(1) - 1)
                                    {
                                        int y1 = y + 1;
                                        do
                                        {
                                            bool restricted = false;
                                            if (spot.map[z][x, y1].getComponent<TileBase>().restricted) restricted = true;
                                            spot.map[z][x, y1] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                            spot.map[z][x, y1].getComponent<TileBase>().restricted = restricted;
                                            y1++;
                                        } while (!(x == spot.map[z].GetLength(0) - 1 || spot.map[z][x + 1, y1].hasComponent<TileWall>() || spot.map[z][x + 1, y1].hasComponent<TileDoor>()) &&
                                                !(x == 0 ||spot.map[z][x - 1, y1].hasComponent<TileWall>() || spot.map[z][x - 1, y1].hasComponent<TileDoor>()));
                                    }
                                    else
                                    {
                                        bool restricted = false;
                                        if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                        spot.map[z][x, y] = buildTile("WALL", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                        spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                                    }
                                }
                                // Open up past doors that lead to walls
                                if (block[0] == false)
                                {
                                    if (y < spot.map[z].GetLength(1) - 1)
                                    {
                                        int y1 = y + 1;
                                        do
                                        {
                                            bool restricted = false;
                                            if (spot.map[z][x, y1].getComponent<TileBase>().restricted) restricted = true;
                                            spot.map[z][x, y1] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                            spot.map[z][x, y1].getComponent<TileBase>().restricted = restricted;
                                            y1++;
                                        } while (!(x == spot.map[z].GetLength(0) - 1 || spot.map[z][x + 1, y1].hasComponent<TileWall>() || spot.map[z][x + 1, y1].hasComponent<TileDoor>()) &&
                                                !(x == 0 || spot.map[z][x - 1, y1].hasComponent<TileWall>() || spot.map[z][x - 1, y1].hasComponent<TileDoor>()));
                                    }
                                    else
                                    {
                                        bool restricted = false;
                                        if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                        spot.map[z][x, y] = buildTile("WALL", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                        spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                                    }
                                }
                                else if (block[3] == false)
                                {
                                    if (y > 0)
                                    {
                                        int y1 = y - 1;
                                        do
                                        {
                                            bool restricted = false;
                                            if (spot.map[z][x, y1].getComponent<TileBase>().restricted) restricted = true;
                                            spot.map[z][x, y1] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                            spot.map[z][x, y1].getComponent<TileBase>().restricted = restricted;
                                            y1--;
                                        } while (!(x == spot.map[z].GetLength(0) - 1 || spot.map[z][x + 1, y1].hasComponent<TileWall>() || spot.map[z][x + 1, y1].hasComponent<TileDoor>()) &&
                                                !(x == 0 || spot.map[z][x - 1, y1].hasComponent<TileWall>() || spot.map[z][x - 1, y1].hasComponent<TileDoor>()));
                                    }
                                    else
                                    {
                                        bool restricted = false;
                                        if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                        spot.map[z][x, y] = buildTile("WALL", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                        spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                                    }
                                }
                                else if (block[1] == false)
                                {
                                    if (x < spot.map[z].GetLength(0) - 1)
                                    {
                                        int x1 = x + 1;
                                        do
                                        {
                                            bool restricted = false;
                                            if (spot.map[z][x1, y].getComponent<TileBase>().restricted) restricted = true;
                                            spot.map[z][x1, y] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                            spot.map[z][x1, y].getComponent<TileBase>().restricted = restricted;
                                            x1++;
                                        } while (!(y == spot.map[z].GetLength(1) - 1 || spot.map[z][x1, y + 1].hasComponent<TileWall>() || spot.map[z][x1, y + 1].hasComponent<TileDoor>()) &&
                                                !(y == 0 || spot.map[z][x1, y - 1].hasComponent<TileWall>() || spot.map[z][x1, y - 1].hasComponent<TileDoor>()));
                                    }
                                    else
                                    {
                                        bool restricted = false;
                                        if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                        spot.map[z][x, y] = buildTile("WALL", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                        spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                                    }
                                }
                                else if (block[2] == false)
                                {
                                    if (x > 0)
                                    {
                                        int x1 = x - 1;
                                        do
                                        {
                                            bool restricted = false;
                                            if (spot.map[z][x1, y].getComponent<TileBase>().restricted) restricted = true;
                                            spot.map[z][x1, y] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                            spot.map[z][x1, y].getComponent<TileBase>().restricted = restricted;
                                            x1--;
                                        } while (!(y == spot.map[z].GetLength(1) - 1 || spot.map[z][x1, y + 1].hasComponent<TileWall>() || spot.map[z][x1, y + 1].hasComponent<TileDoor>()) &&
                                                !(y == 0 || spot.map[z][x1, y - 1].hasComponent<TileWall>() || spot.map[z][x1, y - 1].hasComponent<TileDoor>()));
                                    }
                                    else
                                    {
                                        bool restricted = false;
                                        if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                        spot.map[z][x, y] = buildTile("WALL", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                        spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                                    }
                                }
                            }

                //DELETE NON-DOORS
                for (int z = 0; z < spot.map.Count; z++)
                    for (int x = 0; x < spot.map[z].GetLength(0); x++)
                        for (int y = 0; y < spot.map[z].GetLength(1); y++)
                            if (spot.map[z][x,y].hasComponent<TileDoor>())
                            {
                                bool[] block = { true, true, true, true };
                                if (x > 0) if (!spot.map[z][x - 1,y].hasComponent<TileWall>()) block[1] = false;
                                if (x < spot.map[z].GetLength(0) - 1) if (!spot.map[z][x + 1, y].hasComponent<TileWall>()) block[2] = false;
                                if (y > 0) if (!spot.map[z][x, y - 1].hasComponent<TileWall>()) block[0] = false;
                                if (y < spot.map[z].GetLength(1) - 1) if (!spot.map[z][x, y + 1].hasComponent<TileWall>()) block[3] = false;
                                if (block[0] && block[3]) continue;
                                if (block[1] && block[2]) continue;
                                bool restricted = false;
                                if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                spot.map[z][x, y] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                            }

                //Clear away wall stubs. This runs multiple times because the process of clearing away a stub may itself leave other stubs (although it's very unlikely that it will take more than two passes)
                bool acted = true;
                while (acted)
                {
                    acted = false;
                    for (int z = 0; z < spot.map.Count; z++)
                        for (int x = 0; x < spot.map[z].GetLength(0); x++)
                            for (int y = 0; y < spot.map[z].GetLength(1); y++)
                                if (spot.map[z][x, y].hasComponent<TileWall>() || spot.map[z][x, y].hasComponent<TileDoor>())
                                {
                                    bool[] block = { true, true, true, true };
                                    int openCount = 0;
                                    bool restricted = false;
                                    if (x > 0) if (!(spot.map[z][x - 1, y].hasComponent<TileWall>() || spot.map[z][x - 1, y].hasComponent<TileDoor>())) { block[1] = false; openCount++; }
                                    if (x < spot.map[z].GetLength(0) - 1) if (!(spot.map[z][x + 1, y].hasComponent<TileWall>() || spot.map[z][x + 1, y].hasComponent<TileDoor>())) { block[2] = false; openCount++; }
                                    if (y > 0) if (!(spot.map[z][x, y - 1].hasComponent<TileWall>() || spot.map[z][x, y - 1].hasComponent<TileDoor>())) { block[0] = false; openCount++; }
                                    if (y < spot.map[z].GetLength(1) - 1) if (!(spot.map[z][x, y + 1].hasComponent<TileWall>() || spot.map[z][x, y + 1].hasComponent<TileDoor>())) { block[3] = false; openCount++; }
                                    if (openCount < 3) continue;

                                    if (block[0] == false)
                                    {
                                        if (y < spot.map[z].GetLength(1) - 1)
                                        {
                                            int y1 = y + 1;
                                            do
                                            {
                                                restricted = false;
                                                if (spot.map[z][x, y1].getComponent<TileBase>().restricted) restricted = true;
                                                spot.map[z][x, y1] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                                spot.map[z][x, y1].getComponent<TileBase>().restricted = restricted;
                                                y1++;
                                            } while (!(x == spot.map[z].GetLength(0) - 1 || spot.map[z][x + 1, y1].hasComponent<TileWall>() || spot.map[z][x + 1, y1].hasComponent<TileDoor>()) &&
                                                    !(x == 0 || spot.map[z][x - 1, y1].hasComponent<TileWall>() || spot.map[z][x - 1, y1].hasComponent<TileDoor>()));
                                        }
                                        else
                                        {
                                            restricted = false;
                                            if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                            spot.map[z][x, y] = buildTile("WALL", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                            spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                                        }

                                        restricted = false;
                                        if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                        spot.map[z][x, y] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                        spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                                        acted = true;
                                    }
                                    else if (block[3] == false)
                                    {
                                        if (y > 0)
                                        {
                                            int y1 = y - 1;
                                            do
                                            {
                                                restricted = false;
                                                if (spot.map[z][x, y1].getComponent<TileBase>().restricted) restricted = true;
                                                spot.map[z][x, y1] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                                spot.map[z][x, y1].getComponent<TileBase>().restricted = restricted;
                                                y1--;
                                            } while (!(x == spot.map[z].GetLength(0) - 1 || spot.map[z][x + 1, y1].hasComponent<TileWall>() || spot.map[z][x + 1, y1].hasComponent<TileDoor>()) &&
                                                    !(x == 0 || spot.map[z][x - 1, y1].hasComponent<TileWall>() || spot.map[z][x - 1, y1].hasComponent<TileDoor>()));
                                        }
                                        else
                                        {
                                            restricted = false;
                                            if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                            spot.map[z][x, y] = buildTile("WALL", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                            spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                                        }

                                        restricted = false;
                                        if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                        spot.map[z][x, y] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                        spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                                        acted = true;
                                    }
                                    else if (block[1] == false)
                                    {
                                        if (x < spot.map[z].GetLength(0) - 1)
                                        {
                                            int x1 = x + 1;
                                            do
                                            {
                                                restricted = false;
                                                if (spot.map[z][x1, y].getComponent<TileBase>().restricted) restricted = true;
                                                spot.map[z][x1, y] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                                spot.map[z][x1, y].getComponent<TileBase>().restricted = restricted;
                                                x1++;
                                            } while (!(y == spot.map[z].GetLength(1) - 1 || spot.map[z][x1, y + 1].hasComponent<TileWall>() || spot.map[z][x1, y + 1].hasComponent<TileDoor>()) &&
                                                    !(y == 0 || spot.map[z][x1, y - 1].hasComponent<TileWall>() || spot.map[z][x1, y - 1].hasComponent<TileDoor>()));
                                        }
                                        else
                                        {
                                            restricted = false;
                                            if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                            spot.map[z][x, y] = buildTile("WALL", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                            spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                                        }

                                        restricted = false;
                                        if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                        spot.map[z][x, y] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                        spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                                        acted = true;
                                    }
                                    else if (block[2] == false)
                                    {
                                        if (x > 0)
                                        {
                                            int x1 = x - 1;
                                            do
                                            {
                                                restricted = false;
                                                if (spot.map[z][x1, y].getComponent<TileBase>().restricted) restricted = true;
                                                spot.map[z][x1, y] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                                spot.map[z][x1, y].getComponent<TileBase>().restricted = restricted;
                                                x1--;
                                            } while (!(y == spot.map[z].GetLength(1) - 1 || spot.map[z][x1, y + 1].hasComponent<TileWall>() || spot.map[z][x1, y + 1].hasComponent<TileDoor>()) &&
                                                    !(y == 0 || spot.map[z][x1, y - 1].hasComponent<TileWall>() || spot.map[z][x1, y - 1].hasComponent<TileDoor>()));
                                        }
                                        else
                                        {
                                            restricted = false;
                                            if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                            spot.map[z][x, y] = buildTile("WALL", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                            spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                                        }

                                        restricted = false;
                                        if (spot.map[z][x, y].getComponent<TileBase>().restricted) restricted = true;
                                        spot.map[z][x, y] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                        spot.map[z][x, y].getComponent<TileBase>().restricted = restricted;
                                        acted = true;
                                    }
                                }
                }

                //Clear out restrictions
                do
                {
                    acted = false;
                    for (int z = 0; z < spot.map.Count; z++)
                        for (int x = 1; x < spot.map[z].GetLength(0) - 1; x++)
                            for (int y = 1; y < spot.map[z].GetLength(1) - 1; y++)                            
                            {  //Un-restrict blocks if they have neighboring
                               //unrestricted blocks
                                if (spot.map[z][x, y].getComponent<TileBase>().restricted &&
                                    !spot.map[z][x,y].hasComponent<TileWall>() &&
                                    !spot.map[z][x, y].hasComponent<TileDoor>())
                                {
                                    if ((x > 0 &&
                                        !spot.map[z][x - 1, y].getComponent<TileBase>().restricted &&
                                        !spot.map[z][x - 1, y].hasComponent<TileWall>()) ||
                                       (x < spot.map[z].GetLength(0) - 1 &&
                                       !spot.map[z][x + 1, y].getComponent<TileBase>().restricted &&
                                        !spot.map[z][x + 1, y].hasComponent<TileWall>()) ||
                                       (y > 0 &&
                                       !spot.map[z][x, y - 1].getComponent<TileBase>().restricted &&
                                        !spot.map[z][x, y - 1].hasComponent<TileWall>()) ||
                                       (y < spot.map[z].GetLength(1) - 1 &&
                                       !spot.map[z][x, y + 1].getComponent<TileBase>().restricted &&
                                        !spot.map[z][x, y + 1].hasComponent<TileWall>()))
                                    {
                                        spot.map[z][x, y].getComponent<TileBase>().restricted = false;
                                        acted = true;
                                        continue;
                                    }
                                }
                                //Un-restrict and unlock doors if they lead between two
                                //unrestricted sections. If they lead between one
                                //unrestricted section and a restricted section, lock
                                //them instead.
                                else if (spot.map[z][x, y].hasComponent<TileDoor>() &&
                                        (spot.map[z][x, y].getComponent<TileBase>().restricted ||
                                        spot.map[z][x,y].getComponent<TileDoor>().locked))
                                {  //Unrestricted on two opposite sides?
                                    if (((x > 0 && !spot.map[z][x - 1, y].getComponent<TileBase>().restricted && !spot.map[z][x - 1, y].hasComponent<TileWall>()) &&
                                        (x < spot.map[z].GetLength(0) - 1 && !spot.map[z][x + 1, y].getComponent<TileBase>().restricted && !spot.map[z][x + 1, y].hasComponent<TileWall>())) ||
                                       ((y > 0 && !spot.map[z][x, y - 1].getComponent<TileBase>().restricted && !spot.map[z][x, y - 1].hasComponent<TileWall>()) &&
                                        (y < spot.map[z].GetLength(1) - 1 && !spot.map[z][x, y + 1].getComponent<TileBase>().restricted && !spot.map[z][x, y + 1].hasComponent<TileWall>())))
                                    {  //Unlock and unrestrict
                                        spot.map[z][x, y].getComponent<TileDoor>().locked = false;
                                        spot.map[z][x, y].getComponent<TileBase>().restricted = false;
                                        acted = true;
                                        continue;
                                    }
                                    //Unrestricted on at least one side and I'm not locked?
                                    else if (((x > 0 && !spot.map[z][x - 1, y].getComponent<TileBase>().restricted) ||
                                             (x < spot.map[z].GetLength(0) - 1 && !spot.map[z][x + 1, y].getComponent<TileBase>().restricted) ||
                                             (y > 0 && !spot.map[z][x, y - 1].getComponent<TileBase>().restricted) ||
                                             (y < spot.map[z].GetLength(1) - 1 && !spot.map[z][x, y + 1].getComponent<TileBase>().restricted)) &&
                                             !spot.map[z][x, y].getComponent<TileDoor>().locked)
                                    {  //Lock doors leading to restricted areas
                                        spot.map[z][x, y].getComponent<TileDoor>().locked = true;
                                        spot.map[z][x, y].getComponent<TileBase>().restricted = true;
                                        acted = true;
                                        continue;
                                    }
                                }
                            }
                } while (acted);
                
                //Put specials on map
                if (specialList != null && specialList.Count > 0)
                {
                    foreach (LocationGenDef.LocationGenParameter special in specialList)
                    {
                        List<Entity> secure = new List<Entity>();
                        List<Entity> unsecure = new List<Entity>();
                        List<Entity> combined = new List<Entity>();

                        for (int z=special.zstart; z <= special.zend; z++)
                        {
                            for(int x=special.xstart; x <= special.xend; x++)
                            {
                                for(int y=special.ystart; y <= special.yend; y++)
                                {
                                    int adjustedx = x + spot.map[z].GetLength(0) / 2;

                                    if (!spot.map[z][adjustedx, y].hasComponent<TileWall>() &&
                                        !spot.map[z][adjustedx, y].hasComponent<TileDoor>() &&
                                        !spot.map[z][adjustedx, y].hasComponent<TileSpecial>() &&
                                        spot.map[z][adjustedx,y].getComponent<TileFloor>().type != TileFloor.Type.EXIT)
                                    {
                                        if(spot.map[z][adjustedx, y].getComponent<TileBase>().restricted)
                                            secure.Add(spot.map[z][adjustedx, y]);
                                        else
                                            unsecure.Add(spot.map[z][adjustedx, y]);
                                    }
                                }
                            }
                        }

                        combined.AddRange(secure);
                        combined.AddRange(unsecure);

                        if (special.type == "unique")
                        {
                            Entity specialLocation;
                            if (secure.Count > 0)
                            {
                                specialLocation = secure[siteRand.Next(secure.Count)];
                                specialLocation.setComponent(new TileSpecial(special.name));
                            }
                            else if (unsecure.Count > 0)
                            {
                                specialLocation = unsecure[siteRand.Next(unsecure.Count)];
                                specialLocation.setComponent(new TileSpecial(special.name));
                            }
                            else continue;
                        }
                        else if(special.type == "nonunique")
                        {
                            foreach(Entity specialLocation in combined)
                            {
                                if(siteRand.Next(special.freq) == 0)
                                {
                                    specialLocation.setComponent(new TileSpecial(special.name));
                                }
                            }
                        }
                    }
                }

                //Place stairs on multi-floored buildings
                if(spot.map.Count > 1)
                {
                    for(int z = 0; z < spot.map.Count; z++)
                    {
                        List<Entity> secure = new List<Entity>();
                        List<Entity> unsecure = new List<Entity>();

                        for (int x = 0; x < spot.map[z].GetLength(0); x++)
                        {
                            for (int y = 0; y < spot.map[z].GetLength(1); y++)
                            {
                                if (!spot.map[z][x, y].hasComponent<TileWall>() &&
                                    !spot.map[z][x, y].hasComponent<TileDoor>() &&
                                    !spot.map[z][x, y].hasComponent<TileSpecial>() &&
                                    spot.map[z][x, y].getComponent<TileFloor>().type != TileFloor.Type.EXIT)
                                {
                                    if (spot.map[z][x, y].getComponent<TileBase>().restricted)
                                        secure.Add(spot.map[z][x, y]);
                                    else
                                        unsecure.Add(spot.map[z][x, y]);
                                }
                            }
                        }

                        if (z < spot.map.Count - 1)
                        {
                            if(secure.Count > 0)
                            {
                                Entity stairLocation = secure[siteRand.Next(secure.Count)];
                                stairLocation.getComponent<TileFloor>().type = TileFloor.Type.STAIRS_UP;
                                secure.Remove(stairLocation);
                            }
                            else if(unsecure.Count > 0)
                            {
                                Entity stairLocation = unsecure[siteRand.Next(unsecure.Count)];
                                stairLocation.getComponent<TileFloor>().type = TileFloor.Type.STAIRS_UP;
                                unsecure.Remove(stairLocation);
                            }
                        }
                        if(z > 0)
                        {
                            if (secure.Count > 0)
                            {
                                Entity stairLocation = secure[siteRand.Next(secure.Count)];
                                stairLocation.getComponent<TileFloor>().type = TileFloor.Type.STAIRS_DOWN;
                                secure.Remove(stairLocation);
                            }
                            else if (unsecure.Count > 0)
                            {
                                Entity stairLocation = unsecure[siteRand.Next(unsecure.Count)];
                                stairLocation.getComponent<TileFloor>().type = TileFloor.Type.STAIRS_DOWN;
                                unsecure.Remove(stairLocation);
                            }
                        }
                    }
                }

                for (int z = 0; z < spot.map.Count; z++)
                {
                    for (int x = 0; x < spot.map[z].GetLength(0); x++)
                    {
                        for (int y = 0; y < spot.map[z].GetLength(1); y++)
                        {
                            spot.map[z][x, y].getComponent<TileBase>().x = x;
                            spot.map[z][x, y].getComponent<TileBase>().y = y;
                            spot.map[z][x, y].getComponent<TileBase>().location = spot;
                        }
                    }
                }
            }
        }

        private static List<LocationGenDef.LocationGenParameter> build(string sitetype, TroubleSpot spot, Random siteRand)
        {
            if (sitetype == "") return null;

            List<LocationGenDef.LocationGenParameter> specialList = new List<LocationGenDef.LocationGenParameter>();

            List<LocationGenDef.LocationGenParameter> useList = build(GameData.getData().locationGenList[sitetype].use, spot, siteRand);

            if (useList != null && useList.Count > 0) specialList.AddRange(useList);

            foreach(LocationGenDef.LocationGenParameter parameter in GameData.getData().locationGenList[sitetype].parameters)
            {
                //Note: gen params use x=0 as the middle of the map, but game coordinates use x=0 as the left edge, so it needs to be converted.
                if (parameter.type == "tile")
                {
                    for (int z = parameter.zstart; z <= parameter.zend; z++)
                    {
                        for (int x = parameter.xstart; x <= parameter.xend; x++)
                        {
                            for (int y = parameter.ystart; y <= parameter.yend; y++)
                            {
                                if (spot.map[z][x + spot.map[z].GetLength(0) / 2, y] == null || parameter.overwrite)
                                {
                                    Entity tile = buildTile(parameter.name, (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                                    spot.map[z][x + spot.map[z].GetLength(0) / 2, y] = tile;
                                }
                            }
                        }
                    }
                }
                else if(parameter.type == "special")
                {
                    if(parameter.name == "START")
                    {
                        //If START was specified with a range, it was used wrong. So we'll ignore the range and just use the start values anyway.                        
                        spot.startX = parameter.xstart + spot.map[parameter.zstart].GetLength(0)/2;
                        spot.startY = parameter.ystart;
                        spot.startZ = parameter.zstart;
                    }
                    else if(parameter.name == "RESTRICTED")
                    {
                        for (int z = parameter.zstart; z <= parameter.zend; z++)
                        {
                            for (int x = parameter.xstart; x <= parameter.xend; x++)
                            {
                                for (int y = parameter.ystart; y <= parameter.yend; y++)
                                {
                                    if(spot.map[z][x + spot.map[z].GetLength(0) / 2, y] != null)
                                        spot.map[z][x + spot.map[z].GetLength(0) / 2, y].getComponent<TileBase>().restricted = true;
                                }
                            }
                        }
                    }
                    else if (parameter.name == "!RESTRICTED")
                    {
                        //Unrestrict a previously restricted tile (useful if applying a template over another one)
                        for (int z = parameter.zstart; z <= parameter.zend; z++)
                        {
                            for (int x = parameter.xstart; x <= parameter.xend; x++)
                            {
                                for (int y = parameter.ystart; y <= parameter.yend; y++)
                                {
                                    if (spot.map[z][x + spot.map[z].GetLength(0) / 2, y] != null)
                                        spot.map[z][x + spot.map[z].GetLength(0) / 2, y].getComponent<TileBase>().restricted = false;
                                }
                            }
                        }
                    }
                }
                else if(parameter.type == "script")
                {
                    if(parameter.name == "ROOM")
                    {
                        for(int z = parameter.zstart; z <= parameter.zend; z++)
                        {
                            int adjustedXStart = 1 + parameter.xstart + spot.map[z].GetLength(0) / 2;
                            int adjustedXEnd = parameter.xend + spot.map[z].GetLength(0) / 2;
                            int roomDimension = 2; //Default value
                            if (parameter.scriptArguments.ContainsKey("ROOMDIMENSION"))
                                roomDimension = parameter.scriptArguments["ROOMDIMENSION"];
                            generateRoom(adjustedXStart, parameter.ystart, adjustedXEnd - adjustedXStart, parameter.yend - parameter.ystart, z, spot, roomDimension, siteRand);
                        }
                    }
                }
                else if(parameter.type == "unique" || parameter.type == "nonunique")
                {
                    specialList.Add(parameter);
                }
            }

            return specialList;
        }

        private static void generateRoom(int rx, int ry, int dx, int dy, int z, TroubleSpot spot, int ROOMDIMENSION, Random siteRand)
        {
            for (int x = rx; x < rx + dx; x++)
            {
                for (int y = ry; y < ry + dy; y++)
                {
                    spot.map[z][x,y] = buildTile("FLOOR_INDOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                }
            }
            // Chance to stop iterating for large rooms
            if ((dx <= (ROOMDIMENSION + 1) || dy <= (ROOMDIMENSION + 1)) &&
               dx < dy * 2 && dy < dx * 2 && siteRand.Next(2) == 0) return;
            // Very likely to stop iterating for small rooms
            if (dx <= ROOMDIMENSION && dy <= ROOMDIMENSION) return;
            // Guaranteed to stop iterating for hallways
            if (dx <= 1 || dy <= 1) return;
            //LAY DOWN WALL AND ITERATE
            if ((siteRand.Next(2) == 0 || dy <= ROOMDIMENSION) && dx > ROOMDIMENSION)
            {
                int wx = rx + siteRand.Next(dx - ROOMDIMENSION) + 1;
                for (int wy = 0; wy < dy; wy++) spot.map[z][wx,ry + wy] = buildTile("WALL", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                int rny = siteRand.Next(dy);
                spot.map[z][wx, ry + rny] = buildTile("DOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                if (siteRand.Next(3) == 0) spot.map[z][wx, ry + rny].getComponent<TileDoor>().locked = true;
                generateRoom(rx, ry, wx - rx, dy, z, spot, ROOMDIMENSION, siteRand);
                generateRoom(wx + 1, ry, rx + dx - wx - 1, dy, z, spot, ROOMDIMENSION, siteRand);
            }
            else
            {
                int wy = ry + siteRand.Next(dy - ROOMDIMENSION) + 1;
                for (int wx = 0; wx < dx; wx++) spot.map[z][rx + wx, wy] = buildTile("WALL", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                int rnx = siteRand.Next(dx);
                spot.map[z][rx + rnx, wy] = buildTile("DOOR", (LocationDef.TroubleSpotDef)GameData.getData().locationList[spot.owner.def].components["trouble"]);
                if (siteRand.Next(3) == 0) spot.map[z][rx + rnx, wy].getComponent<TileDoor>().locked = true;
                generateRoom(rx, ry, dx, wy - ry, z, spot, ROOMDIMENSION, siteRand);
                generateRoom(rx, wy + 1, dx, ry + dy - wy - 1, z, spot, ROOMDIMENSION, siteRand);
            }
        }

        public static void fillCongress(Government government, string idname, bool nightmare = false)
        {
            MasterController mc = MasterController.GetMC();

            int senateSize = 0;
            int houseSize = 0;

            Dictionary<string, int> senateWeightList = new Dictionary<string, int>();
            Dictionary<string, int> houseWeightList = new Dictionary<string, int>();

            Dictionary<string, float> conservativeStateModList = new Dictionary<string, float>();
            Dictionary<string, float> liberalStateModList = new Dictionary<string, float>();

            foreach (NationDef.StateDef state in GameData.getData().nationList[idname].states)
            {
                if ((state.flags & NationDef.stateFlags.NONSTATE) == 0)
                {
                    government.senate[state.name] = new List<Alignment>();
                    government.house[state.name] = new List<Alignment>();
                    senateSize += 2;
                    senateWeightList.Add(state.name, 2);
                    houseSize += state.congress;
                    houseWeightList.Add(state.name, state.congress);

                    conservativeStateModList.Add(state.name, 1 - (state.alignment * 0.25f));
                    liberalStateModList.Add(state.name, 1 + (state.alignment * 0.25f));

                    if (conservativeStateModList[state.name] <= 0) conservativeStateModList[state.name] = 0.25f;
                    if (liberalStateModList[state.name] <= 0) liberalStateModList[state.name] = 0.25f;
                }
            }

            int archconservativeSenators = nightmare ? (int)(Convert.ToDouble(senateSize) * 0.55) : (int)(Convert.ToDouble(senateSize) * 0.25);
            int conservativeSenators = nightmare ? (int)(Convert.ToDouble(senateSize) * 0.15) : (int)(Convert.ToDouble(senateSize) * 0.35);
            int moderateSenators = nightmare ? (int)(Convert.ToDouble(senateSize) * 0.17) : (int)(Convert.ToDouble(senateSize) * 0.20);
            int liberalSenators = nightmare ? (int)(Convert.ToDouble(senateSize) * 0.10) : (int)(Convert.ToDouble(senateSize) * 0.15);
            int eliteliberalSenators = nightmare ? (int)(Convert.ToDouble(senateSize) * 0.03) : (int)(Convert.ToDouble(senateSize) * 0.05);

            Dictionary<string, int> moddedWeights = new Dictionary<string, int>();

            //Red states slightly more likely to recieve conservative senators, and vice-versa for blue
            for (int i = 0; i < archconservativeSenators; i++)
            {
                foreach(string state in senateWeightList.Keys)
                {
                    moddedWeights[state] = (int) (senateWeightList[state] * conservativeStateModList[state]);
                    if (moddedWeights[state] == 0) moddedWeights[state] = 1;
                }

                string chosenState = mc.WeightedRandom(moddedWeights);

                government.senate[chosenState].Add(Alignment.ARCHCONSERVATIVE);
                senateWeightList[chosenState]--;

                if (senateWeightList[chosenState] == 0)
                {
                    senateWeightList.Remove(chosenState);
                    moddedWeights.Remove(chosenState);
                }
            }
            for (int i = 0; i < conservativeSenators; i++)
            {
                foreach (string state in senateWeightList.Keys)
                {
                    moddedWeights[state] = (int)(senateWeightList[state] * conservativeStateModList[state]);
                    if (moddedWeights[state] == 0) moddedWeights[state] = 1;
                }

                string chosenState = mc.WeightedRandom(moddedWeights);

                government.senate[chosenState].Add(Alignment.CONSERVATIVE);
                senateWeightList[chosenState]--;

                if (senateWeightList[chosenState] == 0)
                {
                    senateWeightList.Remove(chosenState);
                    moddedWeights.Remove(chosenState);
                }
            }
            for (int i = 0; i < moderateSenators; i++)
            {
                string chosenState = mc.WeightedRandom(senateWeightList);

                government.senate[chosenState].Add(Alignment.MODERATE);
                senateWeightList[chosenState]--;

                if (senateWeightList[chosenState] == 0)
                {
                    senateWeightList.Remove(chosenState);
                    moddedWeights.Remove(chosenState);
                }
            }
            for (int i = 0; i < liberalSenators; i++)
            {
                foreach (string state in senateWeightList.Keys)
                {
                    moddedWeights[state] = (int)(senateWeightList[state] * liberalStateModList[state]);
                    if (moddedWeights[state] == 0) moddedWeights[state] = 1;
                }

                string chosenState = mc.WeightedRandom(moddedWeights);

                government.senate[chosenState].Add(Alignment.LIBERAL);
                senateWeightList[chosenState]--;

                if (senateWeightList[chosenState] == 0)
                {
                    senateWeightList.Remove(chosenState);
                    moddedWeights.Remove(chosenState);
                }
            }
            for (int i = 0; i < eliteliberalSenators; i++)
            {
                foreach (string state in senateWeightList.Keys)
                {
                    moddedWeights[state] = (int)(senateWeightList[state] * liberalStateModList[state]);
                    if (moddedWeights[state] == 0) moddedWeights[state] = 1;
                }

                string chosenState = mc.WeightedRandom(moddedWeights);

                government.senate[chosenState].Add(Alignment.ELITE_LIBERAL);
                senateWeightList[chosenState]--;

                if (senateWeightList[chosenState] == 0)
                {
                    senateWeightList.Remove(chosenState);
                    moddedWeights.Remove(chosenState);
                }
            }
            //Top off with moderates if there was a rounding error somewhere
            while(senateWeightList.Count > 0)
            {
                string chosenState = mc.WeightedRandom(senateWeightList);

                government.senate[chosenState].Add(Alignment.MODERATE);
                senateWeightList[chosenState]--;

                if (senateWeightList[chosenState] == 0) senateWeightList.Remove(chosenState);
            }

            int archconservativeCongress = nightmare ? (int)(Convert.ToDouble(houseSize) * (220D / 435D)) : (int)(Convert.ToDouble(houseSize) * (50D / 435D));
            int conservativeCongress = nightmare ? (int)(Convert.ToDouble(houseSize) * (130D / 435D)) : (int)(Convert.ToDouble(houseSize) * (200D / 435D));
            int moderateCongress = nightmare ? (int)(Convert.ToDouble(houseSize) * (50D / 435D)) : (int)(Convert.ToDouble(houseSize) * (100D / 435D));
            int liberalCongress = nightmare ? (int)(Convert.ToDouble(houseSize) * (25D / 435D)) : (int)(Convert.ToDouble(houseSize) * (50D / 435D));
            int eliteliberalCongress = nightmare ? (int)(Convert.ToDouble(houseSize) * (10D / 435D)) : (int)(Convert.ToDouble(houseSize) * (35D / 435D));

            for (int i = 0; i < archconservativeCongress; i++)
            {
                foreach (string state in houseWeightList.Keys)
                {
                    moddedWeights[state] = (int)(houseWeightList[state] * conservativeStateModList[state]);
                    if (moddedWeights[state] == 0) moddedWeights[state] = 1;
                }

                string chosenState = mc.WeightedRandom(moddedWeights);

                government.house[chosenState].Add(Alignment.ARCHCONSERVATIVE);
                houseWeightList[chosenState]--;

                if (houseWeightList[chosenState] == 0)
                {
                    houseWeightList.Remove(chosenState);
                    moddedWeights.Remove(chosenState);
                }
            }
            for (int i = 0; i < conservativeCongress; i++)
            {
                foreach (string state in houseWeightList.Keys)
                {
                    moddedWeights[state] = (int)(houseWeightList[state] * conservativeStateModList[state]);
                    if (moddedWeights[state] == 0) moddedWeights[state] = 1;
                }

                string chosenState = mc.WeightedRandom(moddedWeights);

                government.house[chosenState].Add(Alignment.CONSERVATIVE);
                houseWeightList[chosenState]--;

                if (houseWeightList[chosenState] == 0)
                {
                    houseWeightList.Remove(chosenState);
                    moddedWeights.Remove(chosenState);
                }
            }
            for (int i = 0; i < moderateCongress; i++)
            {
                string chosenState = mc.WeightedRandom(moddedWeights);

                government.house[chosenState].Add(Alignment.MODERATE);
                houseWeightList[chosenState]--;

                if (houseWeightList[chosenState] == 0)
                {
                    houseWeightList.Remove(chosenState);
                    moddedWeights.Remove(chosenState);
                }
            }
            for (int i = 0; i < liberalCongress; i++)
            {
                foreach (string state in houseWeightList.Keys)
                {
                    moddedWeights[state] = (int)(houseWeightList[state] * liberalStateModList[state]);
                    if (moddedWeights[state] == 0) moddedWeights[state] = 1;
                }

                string chosenState = mc.WeightedRandom(moddedWeights);

                government.house[chosenState].Add(Alignment.LIBERAL);
                houseWeightList[chosenState]--;

                if (houseWeightList[chosenState] == 0)
                {
                    houseWeightList.Remove(chosenState);
                    moddedWeights.Remove(chosenState);
                }
            }
            for (int i = 0; i < eliteliberalCongress; i++)
            {
                foreach (string state in houseWeightList.Keys)
                {
                    moddedWeights[state] = (int)(houseWeightList[state] * liberalStateModList[state]);
                    if (moddedWeights[state] == 0) moddedWeights[state] = 1;
                }

                string chosenState = mc.WeightedRandom(moddedWeights);

                government.house[chosenState].Add(Alignment.ELITE_LIBERAL);
                houseWeightList[chosenState]--;

                if (houseWeightList[chosenState] == 0)
                {
                    houseWeightList.Remove(chosenState);
                    moddedWeights.Remove(chosenState);
                }
            }
            //Top off with moderates if there was a rounding error somewhere
            while(houseWeightList.Count > 0)
            {
                string chosenState = mc.WeightedRandom(moddedWeights);

                government.house[chosenState].Add(Alignment.MODERATE);
                houseWeightList[chosenState]--;

                if (houseWeightList[chosenState] == 0) houseWeightList.Remove(chosenState);
            }
        }

        public static void fillSupremeCourt(Government government, string idname, bool nightmare = false)
        {
            int archconservativeCourt = nightmare ? (int)(Convert.ToDouble(GameData.getData().nationList[idname].supremeCourtSeats) * (5D / 9D)) : (int)(Convert.ToDouble(GameData.getData().nationList[idname].supremeCourtSeats) * (3D / 9D));
            int conservativeCourt = nightmare ? (int)(Convert.ToDouble(GameData.getData().nationList[idname].supremeCourtSeats) * (2D / 9D)) : (int)(Convert.ToDouble(GameData.getData().nationList[idname].supremeCourtSeats) * (2D / 9D));
            int moderateCourt = nightmare ? (int)(Convert.ToDouble(GameData.getData().nationList[idname].supremeCourtSeats) * (1D / 9D)) : (int)(Convert.ToDouble(GameData.getData().nationList[idname].supremeCourtSeats) * (0D / 9D));
            int liberalCourt = nightmare ? (int)(Convert.ToDouble(GameData.getData().nationList[idname].supremeCourtSeats) * (0D / 9D)) : (int)(Convert.ToDouble(GameData.getData().nationList[idname].supremeCourtSeats) * (3D / 9D));
            int eliteliberalCourt = nightmare ? (int)(Convert.ToDouble(GameData.getData().nationList[idname].supremeCourtSeats) * (1D / 9D)) : (int)(Convert.ToDouble(GameData.getData().nationList[idname].supremeCourtSeats) * (1D / 9D));
            Politician politicianComponent = new Politician();

            for (int i = 0; i < archconservativeCourt; i++)
            {
                politicianComponent = new Politician();
                politicianComponent.position = "SUPREME_COURT";
                politicianComponent.alignment = Alignment.ARCHCONSERVATIVE;

                Entity judge = CreatureFactory.create("SUPREME_COURT_CONSERVATIVE");
                judge.persist();

                judge.setComponent(politicianComponent);

                judge.getComponent<CreatureInfo>().encounterName = "Justice " + judge.getComponent<CreatureInfo>().surname;

                government.supremeCourt.Add(judge);
            }
            for (int i = 0; i < conservativeCourt; i++)
            {
                politicianComponent = new Politician();
                politicianComponent.position = "SUPREME_COURT";
                politicianComponent.alignment = Alignment.CONSERVATIVE;

                Entity judge = CreatureFactory.create("SUPREME_COURT_CONSERVATIVE");
                judge.persist();

                judge.setComponent(politicianComponent);

                judge.getComponent<CreatureInfo>().encounterName = "Justice " + judge.getComponent<CreatureInfo>().surname;

                government.supremeCourt.Add(judge);
            }
            for (int i = 0; i < moderateCourt; i++)
            {
                politicianComponent = new Politician();
                politicianComponent.position = "SUPREME_COURT";
                politicianComponent.alignment = Alignment.MODERATE;

                Entity judge = CreatureFactory.create("SUPREME_COURT_MODERATE");
                judge.persist();

                judge.setComponent(politicianComponent);

                judge.getComponent<CreatureInfo>().encounterName = "Justice " + judge.getComponent<CreatureInfo>().surname;

                government.supremeCourt.Add(judge);
            }
            for (int i = 0; i < liberalCourt; i++)
            {
                politicianComponent = new Politician();
                politicianComponent.position = "SUPREME_COURT";
                politicianComponent.alignment = Alignment.LIBERAL;

                Entity judge = CreatureFactory.create("SUPREME_COURT_LIBERAL");
                judge.persist();

                judge.setComponent(politicianComponent);

                judge.getComponent<CreatureInfo>().encounterName = "Justice " + judge.getComponent<CreatureInfo>().surname;

                government.supremeCourt.Add(judge);
            }
            for (int i = 0; i < eliteliberalCourt; i++)
            {
                politicianComponent = new Politician();
                politicianComponent.position = "SUPREME_COURT";
                politicianComponent.alignment = Alignment.ELITE_LIBERAL;

                Entity judge = CreatureFactory.create("SUPREME_COURT_LIBERAL");
                judge.persist();

                judge.setComponent(politicianComponent);

                judge.getComponent<CreatureInfo>().encounterName = "Justice " + judge.getComponent<CreatureInfo>().surname;

                government.supremeCourt.Add(judge);
            }
            //Fill with moderates if there was a rounding error
            for (int i = government.supremeCourt.Count; i < GameData.getData().nationList[idname].supremeCourtSeats; i++)
            {
                politicianComponent = new Politician();
                politicianComponent.position = "SUPREME_COURT";
                politicianComponent.alignment = Alignment.MODERATE;

                Entity judge = CreatureFactory.create("SUPREME_COURT_MODERATE");
                judge.persist();

                judge.setComponent(politicianComponent);

                judge.getComponent<CreatureInfo>().encounterName = "Justice " + judge.getComponent<CreatureInfo>().surname;

                government.supremeCourt.Add(judge);
            }
        }
    }
}
