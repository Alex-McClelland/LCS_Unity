using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.Creature;
using LCS.Engine.Data;

public class PortraitBuilder : MonoBehaviour {

    public Camera sourceCam;
    public RenderTexture sourceTex;

    public SpriteRenderer faceBase;
    public SpriteRenderer faceAcc;
    public SpriteRenderer neck;
    public SpriteRenderer neckAcc;
    public SpriteRenderer jaw;

    public SpriteRenderer ear_L;
    public SpriteRenderer earAcc_L;
    public SpriteRenderer ear_R;
    public SpriteRenderer earAcc_R;

    public SpriteRenderer hair;
    public SpriteRenderer hairAcc;
    public SpriteRenderer hair_back;
    public SpriteRenderer hair_backAcc;

    public SpriteRenderer eye_L;
    public SpriteRenderer eyeAcc_L;
    public SpriteMask eyeMask_L;
    public SpriteRenderer eyeIris_L;
    public SpriteRenderer eyebrow_L;
    public SpriteRenderer eyebrowAcc_L;

    public SpriteRenderer eye_R;
    public SpriteRenderer eyeAcc_R;
    public SpriteMask eyeMask_R;    
    public SpriteRenderer eyeIris_R;
    public SpriteRenderer eyebrow_R;
    public SpriteRenderer eyebrowAcc_R;

    public SpriteRenderer nose;
    public SpriteRenderer noseAcc;

    public SpriteRenderer mouth;
    public SpriteRenderer mouthAcc;

    public SpriteRenderer facial_hair_mustache;
    public SpriteRenderer facial_hair_beard;
    public SpriteMask beardMask;

    public SpriteRenderer scars;

    public List<GameObject> ageLines;

    public GameObject eyeBandage_L;
    public GameObject eyeBandage_R;
    public Sprite destroyedEye_recent;
    public Sprite destroyedNose;
    public Sprite destroyedNose_recent;

    public Color32[] skinTones;
    public Color32[] eyeColors;
    public Color32[] hairColors;
    public Color32[] hairDyeColors;

    private Dictionary<Entity, Texture> gennedPortraits;

	// Use this for initialization
	void Start () {
        gennedPortraits = new Dictionary<Entity, Texture>();
        MasterController.GetMC().nextDay += cleanGennedPortraits;
        
        foreach(Color32 color in skinTones)
        {
            Portrait.skinTones.Add(new Portrait.Color(color.r, color.g, color.b, color.a));
        }

        foreach (Color32 color in eyeColors)
        {
            Portrait.eyeColors.Add(new Portrait.Color(color.r, color.g, color.b, color.a));
        }

        foreach (Color32 color in hairColors)
        {
            Portrait.hairColors.Add(new Portrait.Color(color.r, color.g, color.b, color.a));
        }

        foreach(Color32 color in hairDyeColors)
        {
            Portrait.dyedHairColors.Add(new Portrait.Color(color.r, color.g, color.b, color.a));
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public Texture buildPortrait(Entity character)
    {
        GameData data = GameData.getData();
        Portrait p = character.getComponent<Portrait>();        

        if (gennedPortraits.ContainsKey(character) && !p.forceRegen) return gennedPortraits[character];

        if (data.speciesList[data.creatureDefList[character.def].species].image[0] != "GEN")
        {
            string image = data.speciesList[data.creatureDefList[character.def].species].image[MasterController.GetMC().LCSRandom(data.speciesList[data.creatureDefList[character.def].species].image.Count)];
            gennedPortraits[character] = data.portraitGraphicList[image].texture;

            return data.portraitGraphicList[image].texture;
        }

        Dictionary<string, string> damagedOrgans = new Dictionary<string, string>();

        foreach(Body.BodyPart part in character.getComponent<Body>().BodyParts)
        {
            foreach(Body.Organ o in part.Organs)
            {
                if (o.Health != Body.Organ.Damage.FINE)
                    damagedOrgans.Add(o.Name, o.Health.ToString());
            }
        }

        Texture returnTex = buildPortrait(p, p.getComponent<Age>().getAge() >= p.getComponent<Body>().getSpecies().oldage - p.getComponent<Body>().getSpecies().oldage / 6, damagedOrgans);

        gennedPortraits[character] = returnTex;

        return returnTex;
    }

    public Texture buildPortrait(Portrait p, bool old, Dictionary<string, string> damagedOrgans)
    {
        GameData data = GameData.getData();

        //If someone ages into this range during the game, this will only actually kick in if something forces the portrait to regenerate, but this is fine
        if (old)
        {
            foreach (GameObject g in ageLines)
                g.SetActive(true);
        }
        else
        {
            foreach (GameObject g in ageLines)
                g.SetActive(false);
        }

        faceBase.color = new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a);
        if (p.faceAcc != "")
            faceAcc.sprite = data.portraitPartList[p.faceAcc].imageVariations[p.faceAccImageNum].image;
        else
            faceAcc.sprite = null;
        neck.color = Color.Lerp(new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a), Color.black, 0.1f);
        if (p.neckAcc != "")
            neckAcc.sprite = data.portraitPartList[p.neckAcc].imageVariations[p.neckAccImageNum].image;
        else
            neckAcc.sprite = null;
        jaw.sprite = data.portraitPartList[p.jaw].imageVariations[p.jawImageNum].image;
        jaw.color = new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a);
        jaw.transform.localPosition = new Vector3(p.jawOffset.x * 0.01f, p.jawOffset.y * 0.01f);
        beardMask.sprite = data.portraitPartList[p.jaw].imageVariations[p.jawImageNum].mask;

        ear_L.sprite = data.portraitPartList[p.ear].imageVariations[p.earImageNum].image;
        ear_L.color = new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a);
        if (data.portraitPartList[p.ear].imageVariations[p.earImageNum].image2 != null)
            ear_R.sprite = data.portraitPartList[p.ear].imageVariations[p.earImageNum].image2;
        else
            ear_R.sprite = data.portraitPartList[p.ear].imageVariations[p.earImageNum].image;
        ear_R.color = new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a);
        if (p.earAcc != "")
        {
            earAcc_L.sprite = data.portraitPartList[p.ear].accessories[p.earAcc].imageVariations[p.earAccImageNum].image;
            if (data.portraitPartList[p.ear].accessories[p.earAcc].imageVariations[p.earAccImageNum].image2 != null)
                earAcc_R.sprite = data.portraitPartList[p.ear].accessories[p.earAcc].imageVariations[p.earAccImageNum].image2;
            else
                earAcc_R.sprite = data.portraitPartList[p.ear].accessories[p.earAcc].imageVariations[p.earAccImageNum].image;
        }
        else
        {
            earAcc_L.sprite = null;
            earAcc_R.sprite = null;
        }

        nose.color = new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a);
        nose.transform.localPosition = new Vector3(p.noseOffset.x * 0.01f, p.noseOffset.y * 0.01f);
        nose.sprite = data.portraitPartList[p.nose].imageVariations[p.noseImageNum].image;
        if (p.noseAcc != "")
            noseAcc.sprite = data.portraitPartList[p.nose].accessories[p.noseAcc].imageVariations[p.noseAccImageNum].image;
        else
            noseAcc.sprite = null;

        //Left and Right are reversed here, because the variable names refer to location on screen while the organ names refer to the character's perspective
        if(damagedOrgans.ContainsKey("Right Eye"))
        {
            if (damagedOrgans["Right Eye"] == "DESTROYED_RECENT")
            {
                eye_L.sprite = destroyedEye_recent;
                eyeIris_L.gameObject.SetActive(false);
                eyeBandage_L.gameObject.SetActive(false);
            }
            else
            {
                eye_L.sprite = null;
                eyeIris_L.gameObject.SetActive(false);
                eyeBandage_L.gameObject.SetActive(true);
            }
        }
        else
        {
            eyeIris_L.gameObject.SetActive(true);
            eyeBandage_L.gameObject.SetActive(false);
            eye_L.sprite = data.portraitPartList[p.eye].imageVariations[p.eyeImageNum].image;
            eyeMask_L.sprite = data.portraitPartList[p.eye].imageVariations[p.eyeImageNum].mask;
        }

        if (damagedOrgans.ContainsKey("Left Eye"))
        {
            if (damagedOrgans["Left Eye"] == "DESTROYED_RECENT")
            {
                eye_R.sprite = destroyedEye_recent;
                eyeIris_R.gameObject.SetActive(false);
                eyeBandage_R.gameObject.SetActive(false);
            }
            else
            {
                eye_R.sprite = null;
                eyeIris_R.gameObject.SetActive(false);
                eyeBandage_R.gameObject.SetActive(true);
            }
        }
        else
        {
            eyeIris_R.gameObject.SetActive(true);
            eyeBandage_R.gameObject.SetActive(false);
            if (data.portraitPartList[p.eye].imageVariations[p.eyeImageNum].image2 != null)
                eye_R.sprite = data.portraitPartList[p.eye].imageVariations[p.eyeImageNum].image2;
            else
                eye_R.sprite = data.portraitPartList[p.eye].imageVariations[p.eyeImageNum].image;
            eyeMask_R.sprite = data.portraitPartList[p.eye].imageVariations[p.eyeImageNum].mask;
        }

        eye_L.transform.localPosition = new Vector3(-p.eyeOffset.x * 0.01f, p.eyeOffset.y * 0.01f);        
        eye_R.transform.localPosition = new Vector3(p.eyeOffset.x * 0.01f, p.eyeOffset.y * 0.01f);
        eyeIris_L.color = new Color32(p.eyeColor.r, p.eyeColor.g, p.eyeColor.b, p.eyeColor.a);
        eyeIris_R.color = new Color32(p.eyeColor.r, p.eyeColor.g, p.eyeColor.b, p.eyeColor.a);

        if (p.eyeAcc != "")
        {
            eyeAcc_L.sprite = data.portraitPartList[p.eye].accessories[p.eyeAcc].imageVariations[p.eyeAccImageNum].image;
            if (data.portraitPartList[p.eye].accessories[p.eyeAcc].imageVariations[p.eyeAccImageNum].image2 != null)
                eyeAcc_R.sprite = data.portraitPartList[p.eye].accessories[p.eyeAcc].imageVariations[p.eyeAccImageNum].image2;
            else
                eyeAcc_R.sprite = data.portraitPartList[p.eye].accessories[p.eyeAcc].imageVariations[p.eyeAccImageNum].image;
        }
        else
        {
            eyeAcc_L.sprite = null;
            eyeAcc_R.sprite = null;
        }

        eyebrow_L.sprite = data.portraitPartList[p.eyebrow].imageVariations[p.eyebrowImageNum].image;
        eyebrow_L.color = new Color32(p.hairColor.r, p.hairColor.g, p.hairColor.b, p.hairColor.a);
        if (data.portraitPartList[p.eyebrow].imageVariations[p.eyebrowImageNum].image2 != null)
            eyebrow_R.sprite = data.portraitPartList[p.eyebrow].imageVariations[p.eyebrowImageNum].image2;
        else
            eyebrow_R.sprite = data.portraitPartList[p.eyebrow].imageVariations[p.eyebrowImageNum].image;
        eyebrow_R.color = new Color32(p.hairColor.r, p.hairColor.g, p.hairColor.b, p.hairColor.a);
        if (p.eyebrowAcc != "")
        {
            eyebrowAcc_L.sprite = data.portraitPartList[p.eyebrow].accessories[p.eyebrowAcc].imageVariations[p.eyebrowAccImageNum].image;
            if (data.portraitPartList[p.eyebrow].accessories[p.eyebrowAcc].imageVariations[p.eyebrowAccImageNum].image2 != null)
                eyebrowAcc_R.sprite = data.portraitPartList[p.eyebrow].accessories[p.eyebrowAcc].imageVariations[p.eyebrowAccImageNum].image2;
            else
                eyebrowAcc_R.sprite = data.portraitPartList[p.eyebrow].accessories[p.eyebrowAcc].imageVariations[p.eyebrowAccImageNum].image;
        }
        else
        {
            eyebrowAcc_L.sprite = null;
            eyebrowAcc_R.sprite = null;
        }

        mouth.transform.localPosition = new Vector3(p.mouthOffset.x * 0.01f, p.mouthOffset.y * 0.01f);
        mouth.sprite = data.portraitPartList[p.mouth].imageVariations[p.mouthImageNum].image;
        mouth.color = Color.Lerp(new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a), Color.red, 0.1f);
        if (p.mouthAcc != "")
            mouthAcc.sprite = data.portraitPartList[p.mouth].accessories[p.mouthAcc].imageVariations[p.mouthImageNum].image;
        else
            mouthAcc.sprite = null;

        hair.sprite = data.portraitPartList[p.hair].imageVariations[p.hairImageNum].image;
        hair.color = new Color32(p.hairColor.r, p.hairColor.g, p.hairColor.b, p.hairColor.a);
        hair_back.sprite = data.portraitPartList[p.hair].imageVariations[p.hairImageNum].image2;
        hair_back.color = new Color32(p.hairColor.r, p.hairColor.g, p.hairColor.b, p.hairColor.a);
        if (p.hairAcc != "")
        {
            hairAcc.sprite = data.portraitPartList[p.hair].accessories[p.hairAcc].imageVariations[p.hairAccImageNum].image;
            hair_backAcc.sprite = data.portraitPartList[p.hair].accessories[p.hairAcc].imageVariations[p.hairAccImageNum].image2;

            if ((GameData.getData().portraitPartList[p.hair].accessories[p.hairAcc].flags & PortraitPartDef.PortraitPartFlags.DYE) != 0)
                hairAcc.color = new Color32(p.hairDyeColor.r, p.hairDyeColor.g, p.hairDyeColor.b, p.hairDyeColor.a);
            else
                hairAcc.color = Color.white;
            if ((GameData.getData().portraitPartList[p.hair].accessories[p.hairAcc].flags & PortraitPartDef.PortraitPartFlags.DYE) != 0)
                hair_backAcc.color = new Color32(p.hairDyeColor.r, p.hairDyeColor.g, p.hairDyeColor.b, p.hairDyeColor.a);
            else
                hair_backAcc.color = Color.white;
        }
        else
        {
            hairAcc.sprite = null;
            hair_backAcc.sprite = null;
        }

        if (p.hairFacial != "")
        {
            facial_hair_mustache.sprite = data.portraitPartList[p.hairFacial].imageVariations[p.hairFacialImageNum].image;
            facial_hair_mustache.color = new Color32(p.hairColor.r, p.hairColor.g, p.hairColor.b, p.hairColor.a);
            facial_hair_beard.sprite = data.portraitPartList[p.hairFacial].imageVariations[p.hairFacialImageNum].image2;
            facial_hair_beard.color = new Color32(p.hairColor.r, p.hairColor.g, p.hairColor.b, p.hairColor.a);
        }
        else
        {
            facial_hair_mustache.sprite = null;
            facial_hair_beard.sprite = null;
        }

        if (p.scar != "")
        {
            scars.sprite = data.portraitPartList[p.scar].imageVariations[p.scarLevel].image;
            Color32 scarColor = new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a);
            float h;
            float s;
            float v;
            Color.RGBToHSV(scarColor, out h, out s, out v);
            s += 0.2f;
            v -= 0.2f;

            scarColor = Color.Lerp(Color.HSVToRGB(h, s, v), Color.red, p.freshScar * 0.1f);
            scars.color = new Color32(scarColor.r, scarColor.g, scarColor.b, (byte)(150 + p.freshScar*10));
        }
        else
            scars.sprite = null;

        sourceCam.Render();

        RenderTexture.active = sourceTex;
        Texture2D tex = new Texture2D(200, 200, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Point;
        tex.ReadPixels(new Rect(0, 0, 200, 200), 0, 0, false);
        tex.Apply();
        RenderTexture.active = null;

        Texture2D returnTex = new Texture2D(200, 200, TextureFormat.ARGB32, false);
        returnTex.filterMode = FilterMode.Point;
        Graphics.CopyTexture(tex, returnTex);

        if (p.forceRegen) p.forceRegen = false;

        return returnTex;
    }

    private void cleanGennedPortraits(object sender, EventArgs args)
    {
        List<Entity> tempList = new List<Entity>(gennedPortraits.Keys);

        foreach(Entity key in tempList)
        {
            if (!key.persistent)
                gennedPortraits.Remove(key);
        }
    }
}
