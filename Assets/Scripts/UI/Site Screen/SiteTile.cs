using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SiteTile : MonoBehaviour {

    public SpriteRenderer NW;
    public SpriteRenderer NE;
    public SpriteRenderer SW;
    public SpriteRenderer SE;

    public SpriteRenderer NW_Floor;
    public SpriteRenderer NE_Floor;
    public SpriteRenderer SW_Floor;
    public SpriteRenderer SE_Floor;

    public SpriteRenderer Graffiti_N;
    public SpriteRenderer Graffiti_S;
    public SpriteRenderer Graffiti_E;
    public SpriteRenderer Graffiti_W;

    public SpriteRenderer Loot;
    public SpriteRenderer Enemy;
    public SpriteRenderer Fire;

    public SpriteRenderer bloodTrail_N;
    public SpriteRenderer bloodTrail_S;
    public SpriteRenderer bloodTrail_E;
    public SpriteRenderer bloodTrail_W;
    public SpriteRenderer bloodBlast_Wall_N;
    public SpriteRenderer bloodBlast_Wall_S;
    public SpriteRenderer bloodBlast_Wall_E;
    public SpriteRenderer bloodBlast_Wall_W;
    public SpriteRenderer bloodBlast_Floor;
    public GameObject bloodTrail_Standing;
    public GameObject bodyOutline;
    public GameObject bloodPrints_N_N;
    public GameObject bloodPrints_S_N;
    public GameObject bloodPrints_E_E;
    public GameObject bloodPrints_W_E;
    public GameObject bloodPrints_N_S;
    public GameObject bloodPrints_S_S;
    public GameObject bloodPrints_E_W;
    public GameObject bloodPrints_W_W;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void clearTile()
    {
        NW.sprite = null;
        NE.sprite = null;
        SW.sprite = null;
        SE.sprite = null;

        NW_Floor.sprite = null;
        NE_Floor.sprite = null;
        SW_Floor.sprite = null;
        SE_Floor.sprite = null;

        Graffiti_N.sprite = null;
        Graffiti_S.sprite = null;
        Graffiti_E.sprite = null;
        Graffiti_W.sprite = null;

        bloodBlast_Wall_N.sprite = null;
        bloodBlast_Wall_S.sprite = null;
        bloodBlast_Wall_E.sprite = null;
        bloodBlast_Wall_W.sprite = null;
        bloodBlast_Floor.sprite = null;

        /*Loot.sprite = null;
        Enemy.sprite = null;
        Fire.sprite = null;

        bloodTrail_N.sprite = null;
        bloodTrail_S.sprite = null;
        bloodTrail_E.sprite = null;
        bloodTrail_W.sprite = null;*/
    }
}
