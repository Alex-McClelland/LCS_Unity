using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LCS.Engine.Components.Location;
using LCS.Engine.Scenes;
using LCS.Engine;

public class WorldSpaceMap : MonoBehaviour {

    public SiteTile p_SiteTile;
    public SiteSprites sprites;
    public SpecialSprites specialSprites;
    public GameObject squad;
    public Camera mapCamera;
    public bool fullMapView;

    private SiteTile[,] tiles;
    private List<SiteTile> tileCache = new List<SiteTile>();
    private Entity[,] map;
    private int currentX = 0;
    private int currentY = 0;
    private int floornum = 0;

    public float smoothTime = 0.3F;
    private Vector3 positionVelocity = Vector3.zero;
    private float sizeVelocity = 0.0f;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (tiles != null)
        {
            if (fullMapView)
            {
                Vector3 targetPosition = new Vector3(tiles[tiles.GetLength(0)/2, tiles.GetLength(1)/2].transform.localPosition.x, tiles[tiles.GetLength(0)/2, tiles.GetLength(1)/2].transform.localPosition.y, mapCamera.transform.localPosition.z);
                mapCamera.transform.localPosition = Vector3.SmoothDamp(mapCamera.transform.localPosition, targetPosition, ref positionVelocity, smoothTime);
                mapCamera.orthographicSize = Mathf.SmoothDamp(mapCamera.orthographicSize, 12f, ref sizeVelocity, smoothTime);
            }
            else
            {
                Vector3 targetPosition = new Vector3(tiles[currentX, currentY].transform.localPosition.x, tiles[currentX, currentY].transform.localPosition.y, mapCamera.transform.localPosition.z);
                mapCamera.transform.localPosition = Vector3.SmoothDamp(mapCamera.transform.localPosition, targetPosition, ref positionVelocity, smoothTime);
                mapCamera.orthographicSize = Mathf.SmoothDamp(mapCamera.orthographicSize, 3f, ref sizeVelocity, smoothTime);
            }
        }
	}

    public void setPosition(int x, int y)
    {
        mapCamera.transform.localPosition = new Vector3(tiles[x, y].transform.localPosition.x,tiles[x, y].transform.localPosition.y, mapCamera.transform.localPosition.z);
        squad.transform.localPosition = new Vector3(tiles[x, y].transform.localPosition.x, tiles[x, y].transform.localPosition.y, squad.transform.localPosition.z);
        currentX = x;
        currentY = y;
    }

    public void refreshMap()
    {
        SiteModeScene scene = MasterController.GetMC().currentSiteModeScene;

        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                SiteTile tile = tiles[x, y];
                int maxX = map.GetLength(0) - 1;
                int maxY = map.GetLength(1) - 1;

                tile.clearTile();

                tile.gameObject.SetActive(map[x, y].getComponent<TileBase>().mapped);

                if (map[x,y].getComponent<TileBase>().loot.Count > 0 || map[x,y].getComponent<TileBase>().cash > 0)
                    tile.Loot.gameObject.SetActive(true);
                else
                    tile.Loot.gameObject.SetActive(false);

                SiteModeScene.MapEnemy enemy = scene.enemyInPosition(new Position(x, y, floornum));

                if (enemy != null)
                {
                    tile.Enemy.gameObject.SetActive(true);
                    if (enemy.type == SiteModeScene.MapEnemy.EnemyType.HEAVY)
                        tile.Enemy.sprite = sprites.Enemy_Heavy;
                    else if (enemy.trapped)
                        tile.Enemy.sprite = sprites.Enemy_Trapped;
                    else
                        tile.Enemy.sprite = sprites.Enemy_Normal;
                }
                else if(map[x,y].getComponent<TileBase>().trapped)
                {
                    tile.Enemy.gameObject.SetActive(true);
                    tile.Enemy.sprite = sprites.Trap;
                }
                else
                {
                    tile.Enemy.gameObject.SetActive(false);
                }

                switch (map[x, y].getComponent<TileBase>().fireState)
                {
                    case TileBase.FireState.NONE:
                        tile.Fire.gameObject.SetActive(false);
                        break;
                    case TileBase.FireState.START:
                        tile.Fire.gameObject.SetActive(true);
                        tile.Fire.sprite = sprites.Fire_Start;
                        break;
                    case TileBase.FireState.PEAK:
                        tile.Fire.gameObject.SetActive(true);
                        tile.Fire.sprite = sprites.Fire_Peak;
                        break;
                    case TileBase.FireState.END:
                        tile.Fire.gameObject.SetActive(true);
                        tile.Fire.sprite = sprites.Fire_End;
                        break;
                    case TileBase.FireState.DEBRIS:
                        tile.Fire.gameObject.SetActive(true);
                        tile.Fire.sprite = sprites.Fire_Debris;
                        break;
                }

                if(map[x,y].def == "NONE")
                {
                    tile.NW_Floor.sprite = null;
                    tile.NE_Floor.sprite = null;
                    tile.SW_Floor.sprite = null;
                    tile.SE_Floor.sprite = null;

                    tile.NE.sprite = null;
                    tile.SE.sprite = null;
                    tile.NW.sprite = null;
                    tile.SW.sprite = null;
                }

                if (map[x, y].hasComponent<TileFloor>())
                {
                    switch (map[x, y].getComponent<TileFloor>().type)
                    {
                        case TileFloor.Type.EXIT:
                            tile.NW_Floor.sprite = sprites.Exit;
                            tile.NE_Floor.sprite = null;
                            tile.SW_Floor.sprite = null;
                            tile.SE_Floor.sprite = null;

                            tile.NE.sprite = null;
                            tile.SE.sprite = null;
                            tile.NW.sprite = null;
                            tile.SW.sprite = null;

                            //Only show exits that are next to walkable tiles so that they don't spam the screen
                            bool floorBorder = false;

                            if (x != maxX && map[x + 1, y].getComponent<TileBase>().isWalkable() && map[x + 1, y].getComponent<TileFloor>().type != TileFloor.Type.EXIT)
                                floorBorder = true;
                            if (x != 0 && map[x - 1, y].getComponent<TileBase>().isWalkable() && map[x - 1, y].getComponent<TileFloor>().type != TileFloor.Type.EXIT)
                                floorBorder = true;
                            if (y != maxY && map[x, y + 1].getComponent<TileBase>().isWalkable() && map[x, y + 1].getComponent<TileFloor>().type != TileFloor.Type.EXIT)
                                floorBorder = true;
                            if (y != 0 && map[x, y - 1].getComponent<TileBase>().isWalkable() && map[x, y - 1].getComponent<TileFloor>().type != TileFloor.Type.EXIT)
                                floorBorder = true;

                            if (floorBorder)
                                tile.gameObject.SetActive(map[x, y].getComponent<TileBase>().mapped);
                            else
                                tile.gameObject.SetActive(false);
                            break;
                        case TileFloor.Type.OUTDOOR:
                            tile.NE_Floor.sprite = sprites.Outdoor_Grass;
                            tile.SE_Floor.sprite = sprites.Outdoor_Grass;
                            tile.NW_Floor.sprite = sprites.Outdoor_Grass;
                            tile.SW_Floor.sprite = sprites.Outdoor_Grass;

                            tile.NE.sprite = null;
                            tile.SE.sprite = null;
                            tile.NW.sprite = null;
                            tile.SW.sprite = null;
                            break;
                        case TileFloor.Type.PATH:
                            tile.NE_Floor.sprite = sprites.Outdoor_Path;
                            tile.SE_Floor.sprite = sprites.Outdoor_Path;
                            tile.NW_Floor.sprite = sprites.Outdoor_Path;
                            tile.SW_Floor.sprite = sprites.Outdoor_Path;

                            tile.NE.sprite = null;
                            tile.SE.sprite = null;
                            tile.NW.sprite = null;
                            tile.SW.sprite = null;
                            break;
                        case TileFloor.Type.INDOOR:
                            if (map[x, y].getComponent<TileBase>().restricted)
                            {
                                tile.NE_Floor.sprite = sprites.Restricted_Floor_Tile;
                                tile.SE_Floor.sprite = sprites.Restricted_Floor_Tile;
                                tile.NW_Floor.sprite = sprites.Restricted_Floor_Tile;
                                tile.SW_Floor.sprite = sprites.Restricted_Floor_Tile;
                            }
                            else
                            {
                                tile.NE_Floor.sprite = sprites.Floor_Tile;
                                tile.SE_Floor.sprite = sprites.Floor_Tile;
                                tile.NW_Floor.sprite = sprites.Floor_Tile;
                                tile.SW_Floor.sprite = sprites.Floor_Tile;
                            }

                            tile.NE.sprite = null;
                            tile.SE.sprite = null;
                            tile.NW.sprite = null;
                            tile.SW.sprite = null;
                            break;
                        case TileFloor.Type.STAIRS_DOWN:
                            if (map[x, y].getComponent<TileBase>().restricted)
                            {
                                tile.NE_Floor.sprite = sprites.Restricted_Floor_Tile;
                                tile.SE_Floor.sprite = sprites.Restricted_Floor_Tile;
                                tile.NW_Floor.sprite = sprites.Restricted_Floor_Tile;
                                tile.SW_Floor.sprite = sprites.Restricted_Floor_Tile;
                            }
                            else
                            {
                                tile.NE_Floor.sprite = sprites.Floor_Tile;
                                tile.SE_Floor.sprite = sprites.Floor_Tile;
                                tile.NW_Floor.sprite = sprites.Floor_Tile;
                                tile.SW_Floor.sprite = sprites.Floor_Tile;
                            }

                            tile.NE.sprite = null;
                            tile.SE.sprite = null;
                            tile.NW.sprite = sprites.Stair_Down;
                            tile.SW.sprite = null;
                            break;
                        case TileFloor.Type.STAIRS_UP:
                            if (map[x, y].getComponent<TileBase>().restricted)
                            {
                                tile.NE_Floor.sprite = sprites.Restricted_Floor_Tile;
                                tile.SE_Floor.sprite = sprites.Restricted_Floor_Tile;
                                tile.NW_Floor.sprite = sprites.Restricted_Floor_Tile;
                                tile.SW_Floor.sprite = sprites.Restricted_Floor_Tile;
                            }
                            else
                            {
                                tile.NE_Floor.sprite = sprites.Floor_Tile;
                                tile.SE_Floor.sprite = sprites.Floor_Tile;
                                tile.NW_Floor.sprite = sprites.Floor_Tile;
                                tile.SW_Floor.sprite = sprites.Floor_Tile;
                            }

                            tile.NE.sprite = null;
                            tile.SE.sprite = null;
                            tile.NW.sprite = sprites.Stair_Up;
                            tile.SW.sprite = null;
                            break;
                    }

                    if(!map[x,y].hasComponent<TileWall>() || map[x,y].getComponent<TileBase>().fireState >= TileBase.FireState.PEAK)
                    {
                        switch (map[x, y].getComponent<TileBase>().bloodBlast)
                        {
                            case TileBase.Bloodstain.NONE:
                                tile.bloodBlast_Floor.sprite = null;
                                break;
                            case TileBase.Bloodstain.BLOOD_1:
                                tile.bloodBlast_Floor.sprite = sprites.BloodFloor_1;
                                break;
                            case TileBase.Bloodstain.BLOOD_2:
                                tile.bloodBlast_Floor.sprite = sprites.BloodFloor_2;
                                break;
                            case TileBase.Bloodstain.BLOOD_3:
                                tile.bloodBlast_Floor.sprite = sprites.BloodFloor_3;
                                break;
                        }

                        switch (map[x, y].getComponent<TileBase>().bloodTrail_N)
                        {
                            case TileBase.Bloodstain.NONE:
                                tile.bloodTrail_N.sprite = null;
                                break;
                            case TileBase.Bloodstain.BLOOD_1:
                                tile.bloodTrail_N.sprite = sprites.BloodTrail_1;
                                break;
                            case TileBase.Bloodstain.BLOOD_2:
                                tile.bloodTrail_N.sprite = sprites.BloodTrail_2;
                                break;
                            case TileBase.Bloodstain.BLOOD_3:
                                tile.bloodTrail_N.sprite = sprites.BloodTrail_3;
                                break;
                        }

                        switch (map[x, y].getComponent<TileBase>().bloodTrail_S)
                        {
                            case TileBase.Bloodstain.NONE:
                                tile.bloodTrail_S.sprite = null;
                                break;
                            case TileBase.Bloodstain.BLOOD_1:
                                tile.bloodTrail_S.sprite = sprites.BloodTrail_1;
                                break;
                            case TileBase.Bloodstain.BLOOD_2:
                                tile.bloodTrail_S.sprite = sprites.BloodTrail_2;
                                break;
                            case TileBase.Bloodstain.BLOOD_3:
                                tile.bloodTrail_S.sprite = sprites.BloodTrail_3;
                                break;
                        }

                        switch (map[x, y].getComponent<TileBase>().bloodTrail_E)
                        {
                            case TileBase.Bloodstain.NONE:
                                tile.bloodTrail_E.sprite = null;
                                break;
                            case TileBase.Bloodstain.BLOOD_1:
                                tile.bloodTrail_E.sprite = sprites.BloodTrail_1;
                                break;
                            case TileBase.Bloodstain.BLOOD_2:
                                tile.bloodTrail_E.sprite = sprites.BloodTrail_2;
                                break;
                            case TileBase.Bloodstain.BLOOD_3:
                                tile.bloodTrail_E.sprite = sprites.BloodTrail_3;
                                break;
                        }

                        switch (map[x, y].getComponent<TileBase>().bloodTrail_W)
                        {
                            case TileBase.Bloodstain.NONE:
                                tile.bloodTrail_W.sprite = null;
                                break;
                            case TileBase.Bloodstain.BLOOD_1:
                                tile.bloodTrail_W.sprite = sprites.BloodTrail_1;
                                break;
                            case TileBase.Bloodstain.BLOOD_2:
                                tile.bloodTrail_W.sprite = sprites.BloodTrail_2;
                                break;
                            case TileBase.Bloodstain.BLOOD_3:
                                tile.bloodTrail_W.sprite = sprites.BloodTrail_3;
                                break;
                        }
                    }
                    
                    tile.bloodTrail_Standing.SetActive(map[x, y].getComponent<TileBase>().bloodTrail_Standing);
                    tile.bodyOutline.SetActive(map[x, y].getComponent<TileBase>().someoneDiedHere);
                    tile.bloodPrints_N_N.SetActive(map[x, y].getComponent<TileBase>().bloodPrints_N_N);
                    tile.bloodPrints_S_N.SetActive(map[x, y].getComponent<TileBase>().bloodPrints_S_N);
                    tile.bloodPrints_E_W.SetActive(map[x, y].getComponent<TileBase>().bloodPrints_E_W);
                    tile.bloodPrints_W_W.SetActive(map[x, y].getComponent<TileBase>().bloodPrints_W_W);
                    tile.bloodPrints_N_S.SetActive(map[x, y].getComponent<TileBase>().bloodPrints_N_S);
                    tile.bloodPrints_S_S.SetActive(map[x, y].getComponent<TileBase>().bloodPrints_S_S);
                    tile.bloodPrints_E_E.SetActive(map[x, y].getComponent<TileBase>().bloodPrints_E_E);
                    tile.bloodPrints_W_E.SetActive(map[x, y].getComponent<TileBase>().bloodPrints_W_E);
                }

                if (map[x, y].hasComponent<TileWall>() && map[x,y].getComponent<TileBase>().fireState < TileBase.FireState.PEAK)
                {
                    bool horizWall;
                    bool vertWall;
                    bool diagWall;
                    bool restricted;
                    TileFloor.Type neighbourFloor;

                    //Graffiti + blood
                    if(x > 0)
                    {
                        switch(map[x - 1, y].getComponent<TileBase>().graffiti)
                        {
                            case TileBase.Graffiti.NONE: tile.Graffiti_W.sprite = null; break;
                            case TileBase.Graffiti.LCS: tile.Graffiti_W.sprite = sprites.Graffiti_LCS; break;
                            case TileBase.Graffiti.GNG: tile.Graffiti_W.sprite = sprites.Graffiti_GNG; break;
                            case TileBase.Graffiti.CCS: tile.Graffiti_W.sprite = sprites.Graffiti_CCS; break;
                        }

                        switch(map[x - 1, y].getComponent<TileBase>().bloodBlast)
                        {
                            case TileBase.Bloodstain.NONE: tile.bloodBlast_Wall_W.sprite = null; break;
                            case TileBase.Bloodstain.BLOOD_1: tile.bloodBlast_Wall_W.sprite = sprites.BloodWall_1; break;
                            case TileBase.Bloodstain.BLOOD_2: tile.bloodBlast_Wall_W.sprite = sprites.BloodWall_2; break;
                            case TileBase.Bloodstain.BLOOD_3: tile.bloodBlast_Wall_W.sprite = sprites.BloodWall_3; break;
                        }
                    }

                    if(x < maxX)
                    {
                        switch (map[x + 1, y].getComponent<TileBase>().graffiti)
                        {
                            case TileBase.Graffiti.NONE: tile.Graffiti_E.sprite = null; break;
                            case TileBase.Graffiti.LCS: tile.Graffiti_E.sprite = sprites.Graffiti_LCS; break;
                            case TileBase.Graffiti.GNG: tile.Graffiti_E.sprite = sprites.Graffiti_GNG; break;
                            case TileBase.Graffiti.CCS: tile.Graffiti_E.sprite = sprites.Graffiti_CCS; break;
                        }

                        switch(map[x + 1, y].getComponent<TileBase>().bloodBlast)
                        {
                            case TileBase.Bloodstain.NONE: tile.bloodBlast_Wall_E.sprite = null; break;
                            case TileBase.Bloodstain.BLOOD_1: tile.bloodBlast_Wall_E.sprite = sprites.BloodWall_1; break;
                            case TileBase.Bloodstain.BLOOD_2: tile.bloodBlast_Wall_E.sprite = sprites.BloodWall_2; break;
                            case TileBase.Bloodstain.BLOOD_3: tile.bloodBlast_Wall_E.sprite = sprites.BloodWall_3; break;
                        }
                    }

                    if(y > 0)
                    {
                        switch (map[x, y - 1].getComponent<TileBase>().graffiti)
                        {
                            case TileBase.Graffiti.NONE: tile.Graffiti_N.sprite = null; break;
                            case TileBase.Graffiti.LCS: tile.Graffiti_N.sprite = sprites.Graffiti_LCS; break;
                            case TileBase.Graffiti.GNG: tile.Graffiti_N.sprite = sprites.Graffiti_GNG; break;
                            case TileBase.Graffiti.CCS: tile.Graffiti_N.sprite = sprites.Graffiti_CCS; break;
                        }

                        switch(map[x, y - 1].getComponent<TileBase>().bloodBlast)
                        {
                            case TileBase.Bloodstain.NONE: tile.bloodBlast_Wall_N.sprite = null; break;
                            case TileBase.Bloodstain.BLOOD_1: tile.bloodBlast_Wall_N.sprite = sprites.BloodWall_1; break;
                            case TileBase.Bloodstain.BLOOD_2: tile.bloodBlast_Wall_N.sprite = sprites.BloodWall_2; break;
                            case TileBase.Bloodstain.BLOOD_3: tile.bloodBlast_Wall_N.sprite = sprites.BloodWall_3; break;
                        }
                    }

                    if(y < maxY)
                    {
                        switch (map[x, y + 1].getComponent<TileBase>().graffiti)
                        {
                            case TileBase.Graffiti.NONE: tile.Graffiti_S.sprite = null; break;
                            case TileBase.Graffiti.LCS: tile.Graffiti_S.sprite = sprites.Graffiti_LCS; break;
                            case TileBase.Graffiti.GNG: tile.Graffiti_S.sprite = sprites.Graffiti_GNG; break;
                            case TileBase.Graffiti.CCS: tile.Graffiti_S.sprite = sprites.Graffiti_CCS; break;
                        }

                        switch(map[x, y + 1].getComponent<TileBase>().bloodBlast)
                        {
                            case TileBase.Bloodstain.NONE: tile.bloodBlast_Wall_S.sprite = null; break;
                            case TileBase.Bloodstain.BLOOD_1: tile.bloodBlast_Wall_S.sprite = sprites.BloodWall_1; break;
                            case TileBase.Bloodstain.BLOOD_2: tile.bloodBlast_Wall_S.sprite = sprites.BloodWall_2; break;
                            case TileBase.Bloodstain.BLOOD_3: tile.bloodBlast_Wall_S.sprite = sprites.BloodWall_3; break;
                        }
                    }

                    //NW
                    horizWall = true;
                    vertWall = true;
                    diagWall = true;
                    restricted = false;
                    neighbourFloor = TileFloor.Type.OUTDOOR;

                    if (x == 0 || !(map[x - 1, y].hasComponent<TileWall>() || map[x - 1, y].hasComponent<TileDoor>() ||
                        (map[x - 1, y].hasComponent<TileSpecial>() && map[x - 1, y].getComponent<TileSpecial>().linkWalls())))
                    {
                        horizWall = false;
                        if (x != 0)
                        {
                            if(map[x-1,y].hasComponent<TileFloor>())
                                neighbourFloor = map[x - 1, y].getComponent<TileFloor>().type;
                            if (map[x - 1, y].getComponent<TileBase>().restricted)
                            {
                                restricted = true;
                            }
                        }
                    }
                    if (y == 0 || !(map[x, y - 1].hasComponent<TileWall>() || map[x, y - 1].hasComponent<TileDoor>() ||
                        (map[x, y - 1].hasComponent<TileSpecial>() && map[x, y - 1].getComponent<TileSpecial>().linkWalls())))
                    {
                        vertWall = false;
                        if (y != 0)
                        {
                            if(map[x, y-1].hasComponent<TileFloor>())
                                neighbourFloor = map[x, y - 1].getComponent<TileFloor>().type;
                            if (map[x, y - 1].getComponent<TileBase>().restricted)
                            {
                                restricted = true;
                            }
                        }
                    }
                    if (x == 0 || y == 0 || !(map[x - 1, y - 1].hasComponent<TileWall>() || map[x - 1, y - 1].hasComponent<TileDoor>() ||
                        (map[x - 1, y - 1].hasComponent<TileSpecial>() && map[x - 1, y - 1].getComponent<TileSpecial>().linkWalls())))
                    {
                        diagWall = false;
                        if (x != 0 && y != 0)
                        {
                            if (map[x - 1, y - 1].hasComponent<TileFloor>())
                                neighbourFloor = map[x - 1, y - 1].getComponent<TileFloor>().type;
                            if (map[x - 1, y - 1].getComponent<TileBase>().restricted)
                            {
                                restricted = true;
                            }
                        }
                    }

                    if (horizWall && vertWall)
                    {
                        if (diagWall)
                            tile.NW.sprite = sprites.BasicWalls_C;
                        else
                        {
                            if (restricted)
                                tile.NW.sprite = sprites.RestrictedWalls_I_SE;
                            else
                                tile.NW.sprite = sprites.BasicWalls_I_SE;
                        }
                    }
                    else if (horizWall)
                    {
                        if (restricted)
                            tile.NW.sprite = sprites.RestrictedWalls_S;
                        else
                            tile.NW.sprite = sprites.BasicWalls_S;
                    }
                    else if (vertWall)
                    {
                        if (restricted)
                            tile.NW.sprite = sprites.RestrictedWalls_E;
                        else
                            tile.NW.sprite = sprites.BasicWalls_E;
                    }
                    else
                    {
                        if (restricted)
                            tile.NW.sprite = sprites.RestrictedWalls_SE;
                        else
                            tile.NW.sprite = sprites.BasicWalls_SE;
                    }

                    if (neighbourFloor == TileFloor.Type.INDOOR ||
                        neighbourFloor == TileFloor.Type.STAIRS_DOWN ||
                        neighbourFloor == TileFloor.Type.STAIRS_UP)
                    {
                        if (restricted)
                            tile.NW_Floor.sprite = sprites.Restricted_Floor_Tile;
                        else
                            tile.NW_Floor.sprite = sprites.Floor_Tile;
                    }
                    else
                    {
                        tile.NW_Floor.sprite = sprites.Outdoor_Grass;
                    }

                    //NE
                    horizWall = true;
                    vertWall = true;
                    diagWall = true;
                    restricted = false;
                    neighbourFloor = TileFloor.Type.OUTDOOR;

                    if (x == maxX || !(map[x + 1, y].hasComponent<TileWall>() || map[x + 1, y].hasComponent<TileDoor>() ||
                        (map[x + 1, y].hasComponent<TileSpecial>() && map[x + 1, y].getComponent<TileSpecial>().linkWalls())))
                    {
                        horizWall = false;
                        if (x != maxX)
                        {
                            if(map[x + 1, y].hasComponent<TileFloor>())
                                neighbourFloor = map[x + 1, y].getComponent<TileFloor>().type;
                            if (map[x + 1, y].getComponent<TileBase>().restricted)
                            {
                                restricted = true;
                            }
                        }
                    }
                    if (y == 0 || !(map[x, y - 1].hasComponent<TileWall>() || map[x, y - 1].hasComponent<TileDoor>() ||
                        (map[x, y - 1].hasComponent<TileSpecial>() && map[x, y - 1].getComponent<TileSpecial>().linkWalls())))
                    {
                        vertWall = false;
                        if (y != 0)
                        {
                            if (map[x, y-1].hasComponent<TileFloor>())
                                neighbourFloor = map[x, y-1].getComponent<TileFloor>().type;
                            if (map[x, y - 1].getComponent<TileBase>().restricted)
                            {
                                restricted = true;
                            }
                        }
                    }
                    if (x == maxX || y == 0 || !(map[x + 1, y - 1].hasComponent<TileWall>() || map[x + 1, y - 1].hasComponent<TileDoor>() ||
                        (map[x + 1, y - 1].hasComponent<TileSpecial>() && map[x + 1, y - 1].getComponent<TileSpecial>().linkWalls())))
                    {
                        diagWall = false;
                        if (x != maxX && y != 0)
                        {
                            if (map[x + 1, y - 1].hasComponent<TileFloor>())
                                neighbourFloor = map[x + 1, y - 1].getComponent<TileFloor>().type;
                            if (map[x + 1, y - 1].getComponent<TileBase>().restricted)
                            {
                                restricted = true;
                            }
                        }
                    }

                    if (horizWall && vertWall)
                    {
                        if (diagWall)
                            tile.NE.sprite = sprites.BasicWalls_C;
                        else
                        {
                            if (restricted)
                                tile.NE.sprite = sprites.RestrictedWalls_I_SW;
                            else
                                tile.NE.sprite = sprites.BasicWalls_I_SW;
                        }
                    }
                    else if (horizWall)
                    {
                        if (restricted)
                            tile.NE.sprite = sprites.RestrictedWalls_S;
                        else
                            tile.NE.sprite = sprites.BasicWalls_S;
                    }
                    else if (vertWall)
                    {
                        if (restricted)
                            tile.NE.sprite = sprites.RestrictedWalls_W;
                        else
                            tile.NE.sprite = sprites.BasicWalls_W;
                    }
                    else
                    {
                        if (restricted)
                            tile.NE.sprite = sprites.RestrictedWalls_SW;
                        else
                            tile.NE.sprite = sprites.BasicWalls_SW;
                    }

                    if (neighbourFloor == TileFloor.Type.INDOOR ||
                        neighbourFloor == TileFloor.Type.STAIRS_DOWN ||
                        neighbourFloor == TileFloor.Type.STAIRS_UP)
                    {
                        if (restricted)
                            tile.NE_Floor.sprite = sprites.Restricted_Floor_Tile;
                        else
                            tile.NE_Floor.sprite = sprites.Floor_Tile;
                    }
                    else
                    {
                        tile.NE_Floor.sprite = sprites.Outdoor_Grass;
                    }

                    //SW
                    horizWall = true;
                    vertWall = true;
                    diagWall = true;
                    restricted = false;
                    neighbourFloor = TileFloor.Type.OUTDOOR;

                    if (x == 0 || !(map[x - 1, y].hasComponent<TileWall>() || map[x - 1, y].hasComponent<TileDoor>() ||
                        (map[x - 1, y].hasComponent<TileSpecial>() && map[x - 1, y].getComponent<TileSpecial>().linkWalls())))
                    {
                        horizWall = false;
                        if (x != 0)
                        {
                            if(map[x-1,y].hasComponent<TileFloor>())
                                neighbourFloor = map[x - 1, y].getComponent<TileFloor>().type;
                            if (map[x - 1, y].getComponent<TileBase>().restricted)
                            {
                                restricted = true;
                            }
                        }
                    }
                    if (y == maxY || !(map[x, y + 1].hasComponent<TileWall>() || map[x, y + 1].hasComponent<TileDoor>() ||
                        (map[x, y + 1].hasComponent<TileSpecial>() && map[x, y + 1].getComponent<TileSpecial>().linkWalls())))
                    {
                        vertWall = false;
                        if (y != maxY)
                        {
                            if (map[x, y + 1].hasComponent<TileFloor>())
                                neighbourFloor = map[x, y + 1].getComponent<TileFloor>().type;
                            if (map[x, y + 1].getComponent<TileBase>().restricted)
                            {
                                restricted = true;
                            }
                        }
                    }
                    if (x == 0 || y == maxY || !(map[x - 1, y + 1].hasComponent<TileWall>() || map[x - 1, y + 1].hasComponent<TileDoor>() ||
                        (map[x - 1, y + 1].hasComponent<TileSpecial>() && map[x - 1, y + 1].getComponent<TileSpecial>().linkWalls())))
                    {
                        diagWall = false;
                        if (x != 0 && y != maxY)
                        {
                            if (map[x - 1, y + 1].hasComponent<TileFloor>())
                                neighbourFloor = map[x - 1, y + 1].getComponent<TileFloor>().type;
                            if (map[x - 1, y + 1].getComponent<TileBase>().restricted)
                            {
                                restricted = true;
                            }
                        }
                    }

                    if (horizWall && vertWall)
                    {
                        if (diagWall)
                            tile.SW.sprite = sprites.BasicWalls_C;
                        else
                        {
                            if (restricted)
                                tile.SW.sprite = sprites.RestrictedWalls_I_NE;
                            else
                                tile.SW.sprite = sprites.BasicWalls_I_NE;
                        }
                    }
                    else if (horizWall)
                    {
                        if (restricted)
                            tile.SW.sprite = sprites.RestrictedWalls_N;
                        else
                            tile.SW.sprite = sprites.BasicWalls_N;
                    }
                    else if (vertWall)
                    {
                        if (restricted)
                            tile.SW.sprite = sprites.RestrictedWalls_E;
                        else
                            tile.SW.sprite = sprites.BasicWalls_E;
                    }
                    else
                    {
                        if (restricted)
                            tile.SW.sprite = sprites.RestrictedWalls_NE;
                        else
                            tile.SW.sprite = sprites.BasicWalls_NE;
                    }

                    if (neighbourFloor == TileFloor.Type.INDOOR ||
                        neighbourFloor == TileFloor.Type.STAIRS_DOWN ||
                        neighbourFloor == TileFloor.Type.STAIRS_UP)
                    {
                        if (restricted)
                            tile.SW_Floor.sprite = sprites.Restricted_Floor_Tile;
                        else
                            tile.SW_Floor.sprite = sprites.Floor_Tile;
                    }
                    else
                    {
                        tile.SW_Floor.sprite = sprites.Outdoor_Grass;
                    }

                    //SE
                    horizWall = true;
                    vertWall = true;
                    diagWall = true;
                    restricted = false;
                    neighbourFloor = TileFloor.Type.OUTDOOR;

                    if (x == maxX || !(map[x + 1, y].hasComponent<TileWall>() || map[x + 1, y].hasComponent<TileDoor>() ||
                        (map[x + 1, y].hasComponent<TileSpecial>() && map[x + 1, y].getComponent<TileSpecial>().linkWalls())))
                    {
                        horizWall = false;
                        if (x != maxX)
                        {
                            if(map[x+1, y].hasComponent<TileFloor>())
                                neighbourFloor = map[x + 1, y].getComponent<TileFloor>().type;
                            if (map[x + 1, y].getComponent<TileBase>().restricted)
                            {
                                restricted = true;
                            }
                        }
                    }
                    if (y == maxY || !(map[x, y + 1].hasComponent<TileWall>() || map[x, y + 1].hasComponent<TileDoor>() ||
                        (map[x, y + 1].hasComponent<TileSpecial>() && map[x, y + 1].getComponent<TileSpecial>().linkWalls())))
                    {
                        vertWall = false;
                        if (y != maxY)
                        {
                            if (map[x, y + 1].hasComponent<TileFloor>())
                                neighbourFloor = map[x, y + 1].getComponent<TileFloor>().type;
                            if (map[x, y + 1].getComponent<TileBase>().restricted)
                            {
                                restricted = true;
                            }
                        }
                    }
                    if (x == maxX || y == maxY || !(map[x + 1, y + 1].hasComponent<TileWall>() || map[x + 1, y + 1].hasComponent<TileDoor>() ||
                        (map[x + 1, y + 1].hasComponent<TileSpecial>() && map[x + 1, y + 1].getComponent<TileSpecial>().linkWalls())))
                    {
                        diagWall = false;
                        if (x != maxX && y != maxY)
                        {
                            if (map[x + 1, y + 1].hasComponent<TileFloor>())
                                neighbourFloor = map[x + 1, y + 1].getComponent<TileFloor>().type;
                            if (map[x + 1, y + 1].getComponent<TileBase>().restricted)
                            {
                                restricted = true;
                            }
                        }
                    }

                    if (horizWall && vertWall)
                    {
                        if (diagWall)
                            tile.SE.sprite = sprites.BasicWalls_C;
                        else
                        {
                            if (restricted)
                                tile.SE.sprite = sprites.RestrictedWalls_I_NW;
                            else
                                tile.SE.sprite = sprites.BasicWalls_I_NW;
                        }
                    }
                    else if (horizWall)
                    {
                        if (restricted)
                            tile.SE.sprite = sprites.RestrictedWalls_N;
                        else
                            tile.SE.sprite = sprites.BasicWalls_N;
                    }
                    else if (vertWall)
                    {
                        if (restricted)
                            tile.SE.sprite = sprites.RestrictedWalls_W;
                        else
                            tile.SE.sprite = sprites.BasicWalls_W;
                    }
                    else
                    {
                        if (restricted)
                            tile.SE.sprite = sprites.RestrictedWalls_NW;
                        else
                            tile.SE.sprite = sprites.BasicWalls_NW;
                    }

                    if (neighbourFloor == TileFloor.Type.INDOOR ||
                        neighbourFloor == TileFloor.Type.STAIRS_DOWN ||
                        neighbourFloor == TileFloor.Type.STAIRS_UP)
                    {
                        if (restricted)
                            tile.SE_Floor.sprite = sprites.Restricted_Floor_Tile;
                        else
                            tile.SE_Floor.sprite = sprites.Floor_Tile;
                    }
                    else
                    {
                        tile.SE_Floor.sprite = sprites.Outdoor_Grass;
                    }
                }

                if (map[x, y].hasComponent<TileDoor>() && map[x,y].getComponent<TileBase>().fireState < TileBase.FireState.PEAK)
                {
                    if (!map[x, y].getComponent<TileDoor>().open)
                    {
                        if (map[x, y].getComponent<TileDoor>().locked)
                            tile.NW.sprite = sprites.Restricted_Door;
                        else
                            tile.NW.sprite = sprites.Door_Plain;
                    }
                    else
                    {
                        tile.NW.sprite = null;
                    }
                    tile.NE.sprite = null;
                    tile.SW.sprite = null;
                    tile.SE.sprite = null;
                }

                if (map[x, y].hasComponent<TileSpecial>() && !map[x,y].getComponent<TileSpecial>().used)
                {
                    tile.NW.sprite = getSpecialSprite(map[x,y].getComponent<TileSpecial>().name);
                    tile.NE.sprite = null;
                    tile.SW.sprite = null;
                    tile.SE.sprite = null;
                }
            }
        }
    }

    private Sprite getSpecialSprite(string name)
    {
        switch (name)
        {
            case "LAB_COSMETICS_CAGEDANIMALS":
            case "LAB_GENETIC_CAGEDANIMALS":
                return specialSprites.CAGE;
            case "POLICESTATION_LOCKUP":
            case "COURTHOUSE_LOCKUP":
                return specialSprites.LOCKUP;
            case "COURTHOUSE_JURYROOM":
                return specialSprites.JURYROOM;
            case "PRISON_CONTROL":
            case "PRISON_CONTROL_LOW":
            case "PRISON_CONTROL_MEDIUM":
            case "PRISON_CONTROL_HIGH":
                return specialSprites.PRISON_CONTROL;
            case "INTEL_SUPERCOMPUTER":
                return specialSprites.INTEL_SUPERCOMPUTER;
            case "SWEATSHOP_EQUIPMENT":
            case "POLLUTER_EQUIPMENT":
                return specialSprites.EQUIPMENT;
            case "NUCLEAR_ONOFF":
                return specialSprites.NUCLEAR_ONOFF;
            case "HOUSE_PHOTOS":
            case "CORPORATE_FILES":
                return specialSprites.SAFE;
            case "HOUSE_CEO":
                return specialSprites.CEO;
            case "RADIO_BROADCASTSTUDIO":
                return specialSprites.RADIO_STUDIO;
            case "NEWS_BROADCASTSTUDIO":
                return specialSprites.TV_STUDIO;
            case "APARTMENT_LANDLORD":
                return specialSprites.LANDLORD;
            case "SIGN_ONE":
            case "SIGN_TWO":
            case "SIGN_THREE":
                return specialSprites.SIGN;
            case "RESTAURANT_TABLE":
                return specialSprites.RESTAURANT_TABLE;
            case "CAFE_COMPUTER":
                return specialSprites.CAFE_COMPUTER;
            case "PARK_BENCH":
                return specialSprites.PARK_BENCH;
            case "CLUB_BOUNCER":
                return specialSprites.BOUNCER;
            case "ARMORY":
                return specialSprites.ARMORY;
            case "DISPLAY_CASE":
                return specialSprites.DISPLAY_CASE;
            case "SECURITY_CHECKPOINT":
                return specialSprites.SECURITY_CHECKPOINT;
            case "SECURITY_METALDETECTORS":
                return specialSprites.SECURITY_METALDETECTOR;
            case "BANK_VAULT":
                //This should blank out the sprite because this tile is actually acts as a trigger for the VAULT_DOOR next to it, which cannot be entered normally
                return null;
            case "BANK_TELLER":
                return specialSprites.BANK_TELLER;
            case "BANK_MONEY":
                return specialSprites.BANK_MONEY;
            case "CCS_BOSS":
                return specialSprites.CCS_BOSS;
            case "VAULT_DOOR":
                return specialSprites.VAULT_DOOR;
            default:
                return specialSprites.TEMP_SPECIAL;
        }
    }

    public void buildMap(Entity[,] map, int floornum)
    {
        this.map = map;
        this.floornum = floornum;
        tiles = new SiteTile[map.GetLength(0), map.GetLength(1)];

        int i = 0;

        for(int x = 0; x< map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                SiteTile tile;
                if (i >= tileCache.Count)
                {
                    tile = Instantiate(p_SiteTile);
                    tile.transform.SetParent(transform, false);
                    tileCache.Add(tile);
                }
                else
                {
                    tile = tileCache[i];
                }

                tile.transform.localPosition = new Vector3(x * 0.64f, y * -0.64f, 0);
                tiles[x, y] = tile;

                i++;
            }
        }

        squad.SetActive(true);

        refreshMap();
    }

    public void cleanMap()
    {
        if (tiles == null) return;

        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            for (int j = 0; j < tiles.GetLength(1); j++)
            {
                tiles[i, j].gameObject.SetActive(false);
            }
        }

        tiles = null;
        map = null;

        squad.SetActive(false);
    }
}
